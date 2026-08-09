using Bastion.Core.Config;
using Bastion.Core.Diagnostics;
using Bastion.Game.Input;
using Bastion.Game.Presentation;
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
        TuningData tuning;
        try
        {
            tuning = TuningLoader.Load(ProjectSettings.GlobalizePath($"res://{TuningLoader.DefaultRelativePath}"));
        }
        catch (TuningValidationException ex)
        {
            // Tuning is hand-edited between playtests; a typo must fail loudly, not resolve oddly later.
            GD.PrintErr($"[21 Bastion] Tuning data rejected:\n{ex.Message}");
            return;
        }

        GD.Print($"[21 Bastion] Design revision {tuning.Revision}, march arm {tuning.March.ActivePreset}");

        // Copied to a local so the branch is not folded away as unreachable in a non-instrumented build.
        bool instrumented = DebugGate.IsEnabled;
        if (instrumented)
        {
            GD.Print("[21 Bastion] Oracle-tier instrumentation is COMPILED IN. Not a player build.");
        }

        BuildAndWire(tuning);
    }

    private void BuildAndWire(TuningData tuning)
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
        (BattlefieldPanel battlefieldPanel, HandPanel handPanel, PostWaveView postWave) = BuildInfoColumn(root);
        (PhaseControls phaseControls, CombatPlaybackView playback) = BuildActionBar(root);

        // Bind after the widgets exist (their _Ready has run on AddChild) and before the first wave, so
        // the opening StateChanged reaches every connected view.
        battlefield.Bind(controller, interaction);
        header.Bind(controller);
        battlefieldPanel.Bind(controller);
        handPanel.Bind(controller);
        postWave.Bind(controller);
        phaseControls.Bind(controller, interaction);
        playback.Bind(controller, battlefield, postWave);

        controller.Configure(tuning, tuning.Encounter(EncounterId), Seed);

#if BASTION_DEVTOOLS
        // Absent from a normal build, and inert in a dev build unless --capture is passed. The
        // composition root is the only place holding the wired-up nodes it needs.
        DevTools.CaptureRun.AttachIfRequested(this, controller, battlefield);
#endif
    }

    private static PhaseHeader BuildHeader(Control root)
    {
        var header = new PhaseHeader { Name = "PhaseHeader" };
        header.SetAnchorsPreset(Control.LayoutPreset.TopWide);
        header.OffsetBottom = Layout.HeaderHeight;
        root.AddChild(header);

        return header;
    }

    private static (BattlefieldPanel, HandPanel, PostWaveView) BuildInfoColumn(Control root)
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
        var handPanel = new HandPanel { Name = "HandPanel" };
        var postWave = new PostWaveView { Name = "PostWave" };

        column.AddChild(battlefieldPanel);
        column.AddChild(handPanel);
        column.AddChild(postWave);

        return (battlefieldPanel, handPanel, postWave);
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
