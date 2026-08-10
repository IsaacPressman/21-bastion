using System.Globalization;
using System.Text;
using Bastion.Core.Cards;
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
    private Label _forceTitle = null!;
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

        _forceTitle = new Label { Text = "BASE WAVE", ThemeTypeVariation = BastionTheme.PanelTitle };
        column.AddChild(_forceTitle);

        // Autowrapped even though it is a mono column: without it the longest army line sets the
        // label's minimum width, which widens the whole info column past the region it is anchored
        // to and clips every panel in it against the screen edge.
        _baseWave = new Label
        {
            ThemeTypeVariation = BastionTheme.Mono,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
        };
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
                _header.Text = "What each lane takes undefended. Threat appears at the draw decision.";
                _header.AddThemeColorOverride("font_color", Palette.TextDim);
                _body.Text = PreDrawLines(session);
                break;
        }

        // Before the Dealer resolves the player can only be shown the force on the field; afterwards
        // the whole army is knowable and § Shown requires it, before the lock rather than in hindsight.
        bool resolved = session.DealerCards is not null;

        _forceTitle.Text = resolved ? "THE ARMY  —  DEALER RESOLVED" : "BASE WAVE";
        _baseWave.Text = resolved ? ArmyLines(session) : BaseWaveLines(session);
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

    /// <summary>
    /// Stakes and the cost of ignoring them, before the opening deal.
    /// </summary>
    /// <remarks>
    /// The stake word on its own does not triage - a bastion lane that would take 3 and a vault lane
    /// that would take 16 pull in opposite directions. § Shown asks for empty-lane damage here, and
    /// the session answers it with a third type that is neither forecast.
    /// </remarks>
    private static string PreDrawLines(WaveSession session)
    {
        var sb = new StringBuilder();

        foreach (LaneBaseline lane in session.OpeningStakes().Lanes)
        {
            sb.AppendLine($"Lane {lane.LaneIndex}  {lane.Stake,-8} undefended {lane.EmptyLaneDamage}");
        }

        return sb.ToString().TrimEnd();
    }

    /// <summary>
    /// The complete army, per lane, and the Dealer's hand that produced it.
    /// </summary>
    /// <remarks>
    /// Grouped in spawn order rather than sorted, so the list reads in the order the lane will meet
    /// them. The Dealer's cards are named because the Dealer's hand <i>is</i> the army and the player
    /// has been counting it - but <b>no Dealer total is shown</b>: comparing totals is suspended for
    /// the prototype (docs/prototype/RISKS-AND-ADDBACKS.md).
    /// </remarks>
    private string ArmyLines(WaveSession session)
    {
        TuningData tuning = _controller.Tuning;
        IReadOnlyList<EnemySpawn> spawns = session.ResolvedArmy().Spawns;
        var sb = new StringBuilder();

        for (int lane = 0; lane < tuning.Geometry.Lanes; lane++)
        {
            List<(string Label, int Count)> groups = [];

            foreach (EnemySpawn spawn in spawns.Where(s => s.LaneIndex == lane).OrderBy(s => s.SpawnIndex))
            {
                string label = $"{tuning.Enemy(spawn.EnemyId).DisplayName}  {SourceLabel(spawn.Source)}";

                // Consecutive units of the same type and origin collapse into a count; a change of
                // either starts a new entry, so reinforcements never hide inside the base wave's tally.
                if (groups.Count > 0 && groups[^1].Label == label)
                {
                    groups[^1] = (label, groups[^1].Count + 1);
                }
                else
                {
                    groups.Add((label, 1));
                }
            }

            sb.AppendLine($"Lane {lane}");

            if (groups.Count == 0)
            {
                sb.AppendLine("   —");
                continue;
            }

            // One unit type per line. Run together on one line they outgrow the column, and the
            // whole point of the block is that the drawn reinforcements are picked out from the
            // base wave rather than buried in a run-on tally.
            foreach ((string label, int count) in groups)
            {
                sb.AppendLine($"   {count,2}× {label}");
            }
        }

        sb.AppendLine();
        sb.Append($"Dealer's hand  {string.Join(" ", session.DealerCards!.Select(RankGlyph))}");

        return sb.ToString();
    }

    private static string SourceLabel(SpawnSource source) => source switch
    {
        SpawnSource.Vanguard => "vanguard",
        SpawnSource.DealerDraw => "drawn",
        _ => "base",
    };

    private static string RankGlyph(Card card) => card.Rank switch
    {
        Rank.Ace => card.AceHigh ? "A" : "1",
        Rank.Jack => "J",
        Rank.Queen => "Q",
        Rank.King => "K",
        _ => ((int)card.Rank).ToString(CultureInfo.InvariantCulture),
    };

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
