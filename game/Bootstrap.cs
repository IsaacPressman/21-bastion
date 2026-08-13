using Bastion.Core.Config;
using Bastion.Core.Diagnostics;
using Bastion.Core.Validation;
using Bastion.Game.Input;
using Bastion.Game.Presentation;
using Bastion.Game.Startup;
using Godot;

namespace Bastion.Game;

/// <summary>
/// The composition root: loads tuning once, builds the presentation tree, and wires it to the core.
/// </summary>
/// <remarks>
/// <para>
/// Tuning is loaded here and passed down; nothing below reads the file again and nothing reads a
/// tuning value from a literal. This is deliberately <b>not</b> an Autoload singleton - global
/// reachability is a service locator, and depending on one from inside a system is what would quietly
/// break the headless core/game split (CLAUDE.md, docs/ARCHITECTURE.md).
/// </para>
/// <para>
/// The whole UI is built in code rather than authored in the scene file. That keeps every view under
/// version control as C#, keeps the one wiring path in one place, and means the scene is just an entry
/// node. Views are added first (so their <c>_Ready</c> builds their widgets), then bound to the
/// controller, and only then is the first wave begun - so the opening <c>StateChanged</c> reaches
/// every already-connected view.
/// </para>
/// <para>
/// The screen is three anchored regions around a drawn board: a phase banner on top, an information
/// column down the right, and an action bar along the bottom holding whichever primary action the
/// current phase offers. Everything anchors rather than sitting at fixed offsets, so the board grows
/// with the window instead of stranding a panel mid-screen. The regions themselves are declared in
/// <see cref="Layout"/>, which the board reads too.
/// </para>
/// </remarks>
public partial class Bootstrap : Node2D
{
    private const int Seed = 20240808;
    private const string EncounterId = "example_wave";

    public override void _Ready()
    {
        LaunchOptions options = LaunchOptions.FromCommandLine();

        TuningData tuning;
        Battery battery;
        try
        {
            tuning = TuningLoader.Load(ProjectSettings.GlobalizePath($"res://{TuningLoader.DefaultRelativePath}"));
            battery = BatteryLoader.Load(ProjectSettings.GlobalizePath($"res://{BatteryLoader.DefaultRelativePath}"));

            // The battery's mirrored encounters are derived, not authored, so they only exist once
            // merged in. Do this before selecting the arm so both edits land on the same value.
            tuning = battery.Apply(tuning);
            tuning = SelectArm(tuning, options.Arm);
        }
        catch (TuningValidationException ex)
        {
            // Tuning is hand-edited between playtests; a typo must fail loudly, not resolve oddly later.
            GD.PrintErr($"[21 Bastion] Data rejected:\n{ex.Message}");
            return;
        }

        GD.Print($"[21 Bastion] Design revision {tuning.Revision}, march arm {tuning.March.ActivePreset}");

        // Copied to a local so the branch is not folded away as unreachable in a non-instrumented build.
        bool instrumented = DebugGate.IsEnabled;
        if (instrumented)
        {
            GD.Print("[21 Bastion] Oracle-tier instrumentation is COMPILED IN. Not a player build.");
        }

        BuildAndWire(tuning, battery, options);
    }

    /// <summary>
    /// Applies <c>--arm</c> by rebuilding tuning, never by writing the file.
    /// </summary>
    /// <remarks>
    /// The arms are presets in one config file, not three builds
    /// (docs/prototype/VALIDATION.md § Test arms), so switching is a different read of the same
    /// data. Returning a new value rather than mutating keeps tuning immutable all the way down,
    /// which is what lets the measurement sweeps hold several arms at once.
    /// </remarks>
    private static TuningData SelectArm(TuningData tuning, string? arm)
    {
        if (arm is null || arm == tuning.March.ActivePreset)
        {
            return tuning;
        }

        if (!tuning.MarchPresets.ContainsKey(arm))
        {
            throw new TuningValidationException(
                $"--arm '{arm}' is not one of: {string.Join(", ", tuning.MarchPresets.Keys.Order(StringComparer.Ordinal))}.");
        }

        return tuning with { March = tuning.March with { ActivePreset = arm } };
    }

    private void BuildAndWire(TuningData tuning, Battery battery, LaunchOptions options)
    {
        var controller = new WaveController { Name = "WaveController" };
        AddChild(controller);

        var interaction = new BoardInteraction(controller);

        // The spatial battlefield draws in world space, beneath the UI layer.
        var battlefield = new BattlefieldView
        {
            Name = "Battlefield",
            LabelFont = BastionTheme.UiFont,
            MonoFont = BastionTheme.MonoFont,
        };
        AddChild(battlefield);

        // The encounter timeline sits under the board and shares its world space: the board owns
        // path, this owns time, and Layout keeps the two regions stacked rather than overlapping.
        var timeline = new TimelineView
        {
            Name = "Timeline",
            LabelFont = BastionTheme.UiFont,
            MonoFont = BastionTheme.MonoFont,
        };
        AddChild(timeline);

        // Panels live on a CanvasLayer so they float above the board regardless of any camera.
        var ui = new CanvasLayer { Name = "UI" };
        AddChild(ui);

        // One themed root Control: Godot propagates a theme down the Control tree, and a CanvasLayer
        // is not part of one. MouseFilter.Ignore is load-bearing - a full-rect Control defaults to
        // swallowing mouse input, which would leave the board unclickable.
        var root = new Control { Name = "Root", Theme = BastionTheme.Create(), MouseFilter = Control.MouseFilterEnum.Ignore };
        root.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        ui.AddChild(root);

        PhaseHeader header = BuildHeader(root);
        (BattlefieldPanel battlefieldPanel, CounterfactualPanel counterfactual, HandPanel handPanel, PostWaveView postWave) =
            BuildInfoColumn(root);
        (PhaseControls phaseControls, CombatPlaybackView playback) = BuildActionBar(root);

        // Bind after the widgets exist (their _Ready has run on AddChild) and before the first wave, so
        // the opening StateChanged reaches every connected view.
        battlefield.Bind(controller, interaction);
        timeline.Bind(controller);
        header.Bind(controller);
        battlefieldPanel.Bind(controller);
        counterfactual.Bind(controller);
        handPanel.Bind(controller);
        postWave.Bind(controller);
        phaseControls.Bind(controller, interaction);
        playback.Bind(controller, battlefield, postWave, timeline);

        // Attached before the first wave so the opening state is logged like any other, and after
        // the playback view exists so it can hear how combat was consumed.
        if (Telemetry.PlaytestLog.Attach(
                this, controller, interaction, options.LogDirectory, options.Logging) is { } log)
        {
            playback.PlaybackFinished += log.RecordPlayback;
            GD.Print($"[21 Bastion] Session log: {options.LogDirectory}");
        }

        Start(root, controller, tuning, battery, options);

#if BASTION_DEVTOOLS
        // Absent from a normal build, and inert in a dev build unless --capture is passed. The
        // composition root is the only place holding the wired-up nodes it needs.
        DevTools.CaptureRun.AttachIfRequested(this, controller, battlefield, root, battery);
#endif
    }

    /// <summary>
    /// Opens the wave the launch options asked for, or the picker when they did not ask for one.
    /// </summary>
    /// <remarks>
    /// The picker is a facilitator screen and is gone before the first card is dealt, so the views
    /// below it simply have not seen a <c>StateChanged</c> yet - which they already tolerate, since
    /// they are driven by that signal rather than polling.
    /// </remarks>
    private static void Start(
        Control root, WaveController controller, TuningData tuning, Battery battery, LaunchOptions options)
    {
        if (options.FixtureId is { } id)
        {
            if (battery.Find(id) is not { } fixture)
            {
                GD.PrintErr($"[21 Bastion] No battery case '{id}'. Known cases: "
                            + string.Join(", ", battery.Fixtures.Select(f => f.Id)));
                return;
            }

            GD.Print($"[21 Bastion] Battery case {fixture.Id} (item {fixture.BatteryItem}): {fixture.Question}");
            controller.ConfigureFromFixture(tuning, fixture);
            return;
        }

        if (options.SkipPicker)
        {
            GD.Print($"[21 Bastion] Free play on arm {tuning.March.ActivePreset}, seed {options.Seed}.");
            controller.Configure(tuning, tuning.Encounter(EncounterId), options.Seed);
            return;
        }

        FixturePicker? picker = null;

        picker = new FixturePicker(tuning, battery, tuning.March.ActivePreset, (arm, fixture) =>
        {
            TuningData chosen = SelectArm(tuning, arm);

            if (fixture is null)
            {
                GD.Print($"[21 Bastion] Free play on arm {arm}, seed {options.Seed}.");
                controller.Configure(chosen, chosen.Encounter(EncounterId), options.Seed);
            }
            else
            {
                GD.Print($"[21 Bastion] Battery case {fixture.Id} on arm {arm}: {fixture.Question}");
                controller.ConfigureFromFixture(chosen, fixture);
            }

            // Freed rather than hidden: a facilitator screen must not be able to linger over a wave,
            // and nothing needs it again - a second case means a fresh launch.
            picker!.QueueFree();
        });

        root.AddChild(picker);
    }

    private static PhaseHeader BuildHeader(Control root)
    {
        var header = new PhaseHeader { Name = "PhaseHeader" };
        header.SetAnchorsPreset(Control.LayoutPreset.TopWide);
        header.OffsetBottom = Layout.HeaderHeight;
        root.AddChild(header);

        return header;
    }

    private static (BattlefieldPanel, CounterfactualPanel, HandPanel, PostWaveView) BuildInfoColumn(Control root)
    {
        var scroll = new ScrollContainer { Name = "InfoColumn" };
        scroll.SetAnchorsPreset(Control.LayoutPreset.RightWide);
        scroll.OffsetLeft = -Layout.RightColumnWidth;
        scroll.OffsetTop = Layout.HeaderHeight;
        scroll.HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled;
        root.AddChild(scroll);

        // The gutter goes on a MarginContainer inside the scroll rather than on each panel, so the
        // cards stay aligned with one another and with the scrollbar.
        var margins = new MarginContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        margins.AddThemeConstantOverride("margin_left", 4);
        margins.AddThemeConstantOverride("margin_right", 14);
        margins.AddThemeConstantOverride("margin_top", 10);
        margins.AddThemeConstantOverride("margin_bottom", 10);
        scroll.AddChild(margins);

        var column = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        column.AddThemeConstantOverride("separation", 10);
        margins.AddChild(column);

        var battlefieldPanel = new BattlefieldPanel { Name = "BattlefieldPanel" };
        var counterfactual = new CounterfactualPanel { Name = "Counterfactual" };
        var handPanel = new HandPanel { Name = "HandPanel" };
        var postWave = new PostWaveView { Name = "PostWave" };

        // The memory of the last card sits between the two consequence surfaces, next to the
        // battlefield facts it is a delta of - and above the hand, which it says nothing about.
        column.AddChild(battlefieldPanel);
        column.AddChild(counterfactual);
        column.AddChild(handPanel);
        column.AddChild(postWave);

        return (battlefieldPanel, counterfactual, handPanel, postWave);
    }

    /// <summary>
    /// The bottom bar. Both occupants fill it and only one is visible at a time - decision controls
    /// during the draw and the adjustment window, playback transport during combat - so the primary
    /// action always sits in the same place on screen.
    /// </summary>
    private static (PhaseControls, CombatPlaybackView) BuildActionBar(Control root)
    {
        var bar = new Panel { Name = "ActionBar" };
        bar.SetAnchorsPreset(Control.LayoutPreset.BottomWide);
        bar.OffsetTop = -Layout.ActionBarHeight;
        bar.OffsetRight = -Layout.RightColumnWidth;
        root.AddChild(bar);

        // A Panel is not a container, so its theme content margins do not reach children: inset by hand.
        var stack = new Control { Name = "Stack", MouseFilter = Control.MouseFilterEnum.Ignore };
        stack.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        stack.OffsetLeft = 16f;
        stack.OffsetRight = -16f;
        stack.OffsetTop = 12f;
        stack.OffsetBottom = -12f;
        bar.AddChild(stack);

        var phaseControls = new PhaseControls { Name = "PhaseControls" };
        phaseControls.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        stack.AddChild(phaseControls);

        var playback = new CombatPlaybackView { Name = "Playback" };
        playback.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        stack.AddChild(playback);

        return (phaseControls, playback);
    }
}
