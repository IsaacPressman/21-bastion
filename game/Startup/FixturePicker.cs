using Bastion.Core.Config;
using Bastion.Core.Validation;
using Bastion.Game.Presentation;
using Godot;

namespace Bastion.Game.Startup;

/// <summary>
/// The facilitator's opening screen: pick a march arm and a battery case, or start free play.
/// </summary>
/// <remarks>
/// <para>
/// This is <b>facilitator-facing, not gameplay UI</b>. It exists so a playtest session can move
/// between arms and states without a rebuild - the Milestone 5 done-when clause - and it is gone
/// before the first card is dealt. Nothing here is on screen while a decision is being made, so the
/// information rules in docs/design/09-information-and-ui.md are not in play: naming the arm to the
/// person running the session is not telling the player anything about their hand.
/// </para>
/// <para>
/// Skipped entirely when <c>--fixture</c> names a case, which is the normal way to run a scripted
/// state. The picker is for the facilitator who wants to choose at the table.
/// </para>
/// </remarks>
public partial class FixturePicker : Control
{
    private readonly Battery _battery;
    private readonly TuningData _tuning;
    private readonly string _initialArm;
    private readonly Action<string, BatteryFixture?> _onChosen;

    private string _arm;
    private HBoxContainer _armRow = null!;

    public FixturePicker(
        TuningData tuning, Battery battery, string initialArm, Action<string, BatteryFixture?> onChosen)
    {
        _tuning = tuning;
        _battery = battery;
        _initialArm = initialArm;
        _arm = initialArm;
        _onChosen = onChosen;
    }

    public override void _Ready()
    {
        Name = "FixturePicker";

        // SetAnchorsAndOffsetsPreset, not SetAnchorsPreset. The latter sets anchors but leaves
        // offsets alone, which is harmless for a child of an already-sized parent - what every
        // panel in Bootstrap is - but not for children added during _Ready, when this Control has
        // not been laid out yet and its rect is still zero. That shipped a picker sized to its
        // minimum, with an invisible backdrop, floating over a live board.
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);

        // Stop, not the inherited Ignore: this screen must swallow clicks, or the board underneath
        // takes them and the facilitator places a tower while choosing a case.
        MouseFilter = MouseFilterEnum.Stop;

        var backdrop = new ColorRect { Color = Palette.Background };
        AddChild(backdrop);
        backdrop.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);

        var margins = new MarginContainer();
        foreach (string side in new[] { "margin_left", "margin_right", "margin_top", "margin_bottom" })
        {
            margins.AddThemeConstantOverride(side, 28);
        }

        AddChild(margins);
        margins.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);

        var column = new VBoxContainer();
        column.AddThemeConstantOverride("separation", 14);
        margins.AddChild(column);

        column.AddChild(Heading("21 Bastion — validation build"));
        column.AddChild(Hint("Facilitator screen. Pick a march arm and a scripted state, or start free play."));

        column.AddChild(SectionLabel("March arm"));
        _armRow = BuildArmRow();
        column.AddChild(_armRow);

        column.AddChild(SectionLabel("Scripted battery"));
        column.AddChild(BuildCaseList());

        var freePlay = new Button { Text = "Free play (unscripted wave)" };
        freePlay.Pressed += () => _onChosen(_arm, null);
        column.AddChild(freePlay);
    }

    private HBoxContainer BuildArmRow()
    {
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 8);

        foreach (string arm in _tuning.MarchPresets.Keys.Order(StringComparer.Ordinal))
        {
            MarchPreset preset = _tuning.MarchPresets[arm];

            var button = new Button
            {
                Text = $"{arm} — {preset.Label}",
                ToggleMode = true,
                ButtonPressed = arm == _initialArm,
                Name = $"Arm{arm}",
            };

            string chosen = arm;
            button.Pressed += () => SelectArm(chosen);

            row.AddChild(button);
        }

        return row;
    }

    /// <summary>Radio behaviour by hand: exactly one arm is active, and it cannot be deselected.</summary>
    private void SelectArm(string arm)
    {
        _arm = arm;

        foreach (Node child in _armRow.GetChildren())
        {
            if (child is Button button)
            {
                button.ButtonPressed = button.Name == $"Arm{arm}";
            }
        }
    }

    /// <summary>
    /// Every case, grouped by the VALIDATION.md item it serves, with both presentations side by side.
    /// </summary>
    /// <remarks>
    /// The two variants are deliberately adjacent here and never adjacent in a session: a facilitator
    /// needs to see that a pair exists, and a player must not be shown the same decision twice in a
    /// row. Keeping the two presentations one button apart is a scheduling matter for the person
    /// running the session, which is why the picker shows the structure plainly rather than hiding it.
    /// </remarks>
    private ScrollContainer BuildCaseList()
    {
        var scroll = new ScrollContainer
        {
            SizeFlagsVertical = SizeFlags.ExpandFill,
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
        };

        var list = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        list.AddThemeConstantOverride("separation", 10);
        scroll.AddChild(list);

        foreach (IGrouping<int, BatteryFixture> item in _battery.Fixtures.GroupBy(f => f.BatteryItem).OrderBy(g => g.Key))
        {
            list.AddChild(SectionLabel($"Item {item.Key}"));

            foreach (IGrouping<string, BatteryFixture> pair in item
                         .GroupBy(f => f.Id.EndsWith(LaneMirror.VariantSuffix, StringComparison.Ordinal)
                             ? f.Id[..^LaneMirror.VariantSuffix.Length]
                             : f.Id)
                         .OrderBy(g => g.Key, StringComparer.Ordinal))
            {
                list.AddChild(CaseRow(pair.Key, [.. pair.OrderBy(f => f.Id, StringComparer.Ordinal)]));
            }
        }

        return scroll;
    }

    private Control CaseRow(string caseId, IReadOnlyList<BatteryFixture> variants)
    {
        var row = new VBoxContainer();
        row.AddThemeConstantOverride("separation", 2);

        row.AddChild(new Label
        {
            Text = variants[0].Question,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
        });

        var buttons = new HBoxContainer();
        buttons.AddThemeConstantOverride("separation", 6);

        foreach (BatteryFixture variant in variants)
        {
            bool mirrored = variant.Id.EndsWith(LaneMirror.VariantSuffix, StringComparison.Ordinal);

            var button = new Button { Text = mirrored ? $"{caseId} · B (mirrored)" : $"{caseId} · A" };

            BatteryFixture chosen = variant;
            button.Pressed += () => _onChosen(_arm, chosen);

            buttons.AddChild(button);
        }

        row.AddChild(buttons);

        return row;
    }

    private static Label Heading(string text)
    {
        var label = new Label { Text = text };
        label.AddThemeFontSizeOverride("font_size", 22);

        return label;
    }

    private static Label SectionLabel(string text)
    {
        var label = new Label { Text = text.ToUpperInvariant(), ThemeTypeVariation = BastionTheme.PanelTitle };
        label.AddThemeColorOverride("font_color", Palette.TextDim);

        return label;
    }

    private static Label Hint(string text)
    {
        var label = new Label { Text = text, ThemeTypeVariation = BastionTheme.Hint };
        label.AddThemeColorOverride("font_color", Palette.TextFaint);

        return label;
    }
}
