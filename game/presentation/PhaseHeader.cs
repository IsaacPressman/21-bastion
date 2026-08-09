using Bastion.Core.Wave;
using Godot;

namespace Bastion.Game.Presentation;

/// <summary>
/// The banner across the top: which phase this is, and what the phase asks of the player.
/// </summary>
/// <remarks>
/// <para>
/// Added because the previous build left the phase to be inferred from which buttons happened to
/// exist. The wave loop is a state machine with five states and quite different stakes in each, and
/// the player should not have to reverse-engineer which one they are in.
/// </para>
/// <para>
/// It carries no numbers. Totals, multipliers, and per-lane damage belong to the two consequence
/// panels, which are kept apart on purpose - a banner spanning the whole screen is exactly where a
/// combined reading would creep in (docs/design/09-information-and-ui.md).
/// </para>
/// </remarks>
public partial class PhaseHeader : Panel
{
    private WaveController _controller = null!;
    private Label _phase = null!;
    private Label _instruction = null!;

    public void Bind(WaveController controller)
    {
        _controller = controller;
        _controller.StateChanged += Refresh;
    }

    public override void _Ready()
    {
        // A Panel is not a container, so its theme content margins do not reach children: inset by hand.
        var row = new HBoxContainer();
        row.SetAnchorsPreset(LayoutPreset.FullRect);
        row.OffsetLeft = 16f;
        row.OffsetRight = -16f;
        row.AddThemeConstantOverride("separation", 18);
        AddChild(row);

        _phase = new Label { VerticalAlignment = VerticalAlignment.Center };
        _phase.AddThemeFontOverride("font", BastionTheme.MonoFont);
        _phase.AddThemeFontSizeOverride("font_size", 16);
        row.AddChild(_phase);

        _instruction = new Label
        {
            VerticalAlignment = VerticalAlignment.Center,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        _instruction.AddThemeColorOverride("font_color", Palette.TextDim);
        row.AddChild(_instruction);
    }

    private void Refresh()
    {
        (string phase, string instruction, Color colour) = _controller.Session.Phase switch
        {
            WavePhase.AwaitingPlacement => (
                "PLACEMENT",
                "Choose a family, then click a socket on the board. Family is locked for the wave.",
                Palette.Text),

            WavePhase.DrawDecision => (
                "DRAW  DECISION",
                "Hitting pays the next march step before the card is revealed.",
                Palette.Text),

            WavePhase.AdjustmentWindow => (
                "ADJUSTMENT",
                _controller.Session.MoveSpent
                    ? "Move spent. Standing orders are still free."
                    : "One move for the whole board: click a tower, then its destination.",
                Palette.Text),

            WavePhase.Locked => (
                "COMBAT",
                "The Final Forecast is the contract. Watch it play out.",
                Palette.FinalForecast),

            WavePhase.BustLocked => (
                "BUST  —  COMBAT",
                "The hand went over. Overload struck, and the Dealer resolves in full regardless.",
                Palette.Bust),

            _ => (_controller.Session.Phase.ToString().ToUpperInvariant(), string.Empty, Palette.Text),
        };

        _phase.Text = phase;
        _phase.AddThemeColorOverride("font_color", colour);
        _instruction.Text = instruction;
    }
}
