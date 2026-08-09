using System.Text;
using Bastion.Core.Config;
using Bastion.Core.Resolve;
using Bastion.Core.Wave;
using Godot;

namespace Bastion.Game.Presentation;

/// <summary>
/// The battlefield-consequences panel: lane stakes, the base wave, and per-lane threat.
/// </summary>
/// <remarks>
/// <para>
/// One of the two deliberately separate surfaces. It carries <b>only</b> battlefield facts; the hand's
/// total, bust risk, and Formation Strength live in the other panel, and the two are never merged into
/// a combined verdict (docs/design/09-information-and-ui.md). They are drawn as two distinct cards
/// rather than two stretches of one column so the separation is visible, not merely structural.
/// </para>
/// <para>
/// It reads <b>Visible Threat</b> during the draw and <b>Final Forecast</b> after the Dealer resolves,
/// and labels each as what it is. They come from different session methods returning different types,
/// so one can never be rendered in the other's place; the header spells out the difference so the
/// player never reads a revealed-force number as a promise about the wave.
/// </para>
/// </remarks>
public partial class BattlefieldPanel : PanelContainer
{
    private WaveController _controller = null!;
    private Label _header = null!;
    private Label _body = null!;
    private Label _baseWave = null!;

    public void Bind(WaveController controller)
    {
        _controller = controller;
        _controller.StateChanged += Refresh;
    }

    public override void _Ready()
    {
        var column = new VBoxContainer();
        column.AddThemeConstantOverride("separation", 6);
        AddChild(column);

        column.AddChild(new Label { Text = "BATTLEFIELD", ThemeTypeVariation = BastionTheme.PanelTitle });

        _header = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart };
        _header.AddThemeFontSizeOverride("font_size", 13);
        column.AddChild(_header);

        _body = new Label { ThemeTypeVariation = BastionTheme.Mono };
        column.AddChild(_body);

        column.AddChild(new HSeparator());
        column.AddChild(new Label { Text = "BASE WAVE", ThemeTypeVariation = BastionTheme.PanelTitle });

        _baseWave = new Label { ThemeTypeVariation = BastionTheme.Mono };
        column.AddChild(_baseWave);
    }

    private void Refresh()
    {
        WaveSession session = _controller.Session;

        switch (session.Phase)
        {
            case WavePhase.DrawDecision:
                _header.Text = "Visible Threat — the revealed force only. NOT a prediction of the wave.";
                _header.AddThemeColorOverride("font_color", Palette.VisibleThreat);
                _body.Text = ForecastLines(_controller.VisibleThreat.Lanes);
                break;

            case WavePhase.AdjustmentWindow:
            case WavePhase.Locked:
            case WavePhase.BustLocked:
                _header.Text = "Final Forecast — the combat contract.";
                _header.AddThemeColorOverride("font_color", Palette.FinalForecast);
                _body.Text = ForecastLines(_controller.Forecast.Lanes);
                break;

            default:
                _header.Text = "Threat appears at the draw decision.";
                _header.AddThemeColorOverride("font_color", Palette.TextDim);
                _body.Text = PreDrawLines(session);
                break;
        }

        _baseWave.Text = BaseWaveLines(session);
    }

    private string ForecastLines(IReadOnlyList<LaneOutcome> lanes)
    {
        double threshold = _controller.Threshold;
        var sb = new StringBuilder();

        foreach (LaneOutcome lane in lanes)
        {
            // Raw number primary; the Open/Held glance-read second. This is the only interpretation
            // the game is permitted to do for the player.
            sb.AppendLine($"Lane {lane.LaneIndex}  {lane.Stake,-8} {lane.CoverageLabel(threshold)}");
            sb.AppendLine($"   takes {lane.PredictedDamage,3} of {lane.EmptyLaneDamage,-3}  prevented {lane.DamagePrevented}");
        }

        return sb.ToString().TrimEnd();
    }

    private static string PreDrawLines(WaveSession session)
    {
        var sb = new StringBuilder();

        for (int lane = 0; lane < session.Encounter.LaneStakes.Count; lane++)
        {
            sb.AppendLine($"Lane {lane}  {session.Encounter.LaneStakes[lane]}");
        }

        return sb.ToString().TrimEnd();
    }

    private string BaseWaveLines(WaveSession session)
    {
        TuningData tuning = _controller.Tuning;
        var sb = new StringBuilder();

        for (int lane = 0; lane < session.Encounter.BaseWave.Count; lane++)
        {
            IReadOnlyList<SpawnGroupTuning> groups = session.Encounter.BaseWave[lane];
            string units = groups.Count == 0
                ? "—"
                : string.Join(", ", groups.Select(g => $"{g.Count}× {tuning.Enemy(g.EnemyId).DisplayName}"));

            sb.AppendLine($"Lane {lane}  {units}");
        }

        return sb.ToString().TrimEnd();
    }
}
