using System.Globalization;
using System.Text;
using Bastion.Core.Board;
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

        // Autowrapped even though it is a mono column, for the same reason the army block is: without
        // it the longest consequence line sets the label's minimum width, which widens the whole info
        // column past the region it is anchored to and clips every panel in it against the screen
        // edge. The lines below are written to fit; this is the guard for when one is not.
        _body = new Label
        {
            ThemeTypeVariation = BastionTheme.Mono,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
        };
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
            case WavePhase.AwaitingPlacement:
                // The revealed force is legal here too, and it is the reading the player forms an
                // intention from: Read and Diagnose precede Commit (docs/design/01-core-loop.md).
                // The board it describes is real but incomplete, so the header says how incomplete.
                _header.Text = session.PendingRanks.Count == 1
                    ? "Visible Threat — the revealed force only, with one card still to place."
                    : $"Visible Threat — the revealed force only, with {session.PendingRanks.Count} cards still to place.";
                _header.AddThemeColorOverride("font_color", Palette.VisibleThreat);
                _body.Text = ConsequenceLines();
                break;

            case WavePhase.DrawDecision:
                _header.Text = "Visible Threat — the revealed force only. NOT a prediction of the wave.";
                _header.AddThemeColorOverride("font_color", Palette.VisibleThreat);
                _body.Text = ConsequenceLines();
                break;

            case WavePhase.AdjustmentWindow:
            case WavePhase.Locked:
            case WavePhase.BustLocked:
                _header.Text = "Final Forecast — the combat contract.";
                _header.AddThemeColorOverride("font_color", Palette.FinalForecast);
                _body.Text = ConsequenceLines();
                break;

            default:
                _header.Text = "What each lane takes undefended.";
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

    /// <summary>
    /// Per lane: the numbers, then the battlefield problem that remains.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The exact committed-state statistics (docs/design/14-encounter-timeline.md § Exact
    /// consequences for the committed state): which enemy leaks, when the lane first breaks, what it
    /// would take to stop it, what the formation currently delivers, and what each tower does.
    /// </para>
    /// <para>
    /// <b>A requirement and a shortfall, never the card that closes them.</b> "Needs 2.1 more
    /// armor-effective damage" leaves every step of the player's judgment intact; "a mid-rank Siege
    /// Club here will solve this" deletes the last one.
    /// </para>
    /// </remarks>
    private string ConsequenceLines()
    {
        var sb = new StringBuilder();

        foreach (LaneConsequence lane in _controller.Consequences)
        {
            // Raw number primary; the Open/Held glance-read second. This is the only interpretation
            // the game is permitted to do for the player.
            sb.AppendLine($"Lane {lane.LaneIndex}  {lane.Stake,-8} {lane.CoverageLabel}");
            sb.AppendLine($"   takes {lane.PredictedDamage,3} of {lane.EmptyLaneDamage,-3}  prevented {lane.DamagePrevented}");

            AppendShortfall(sb, lane);
            AppendTowerAttacks(sb, lane);
            sb.AppendLine();
        }

        return sb.ToString().TrimEnd();
    }

    private static void AppendShortfall(StringBuilder sb, LaneConsequence lane)
    {
        if (lane.LeadLeaker is not { } lead)
        {
            sb.AppendLine("   nothing gets through");
            return;
        }

        // Named and counted: "1 Armored Soldier leaks for 2 damage" is a battlefield problem, where
        // "predicted damage 2" is a number. The lead leaker is the one the shortfall is about - a
        // later one may be answered by the same tower, and stating every shortfall at once turns a
        // diagnosis into a list.
        //
        // Kept under about forty characters a line. The column is a fixed width and this is the
        // block most likely to outgrow it, which clips every panel below rather than just this one.
        int sameType = lane.Leakers.Count(l => l.EnemyId == lead.EnemyId);

        sb.AppendLine($"   {sameType}× {lead.DisplayName} gets through");
        sb.AppendLine($"   first at {lead.LeakTime:0.0}s");
        sb.AppendLine($"   armor-effective {lead.Delivered:0.0} of {lead.Required:0.0}");
        sb.AppendLine($"   short by {lead.Shortfall:0.0}");
    }

    private static void AppendTowerAttacks(StringBuilder sb, LaneConsequence lane)
    {
        TowerAttacks[] firing = [.. lane.Towers.Where(t => t.Shots > 0)];

        if (firing.Length == 0)
        {
            return;
        }

        sb.AppendLine("   " + string.Join("  ", firing.Select(t => $"{Describe(t.Socket)} {t.Shots}×")));
    }

    private static string Describe(SocketRef socket) =>
        socket.IsJunction ? "junc" : $"S{socket.SocketIndex}";

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

    /// <summary>
    /// The base wave, fully known before the opening hand: type, count, order, and timing.
    /// </summary>
    /// <remarks>
    /// <para>
    /// docs/design/09-information-and-ui.md § Shown asks for "the complete authored base wave before
    /// the opening hand: enemy types, spawn order, spawn timing, lane assignment". Counts alone are
    /// not that - a player who knows six raiders are coming but not when they arrive still cannot
    /// form an intention, which is the failure the whole encounter pass exists to fix.
    /// </para>
    /// <para>
    /// The times come from the same schedule the timeline draws, so the block and the strip cannot
    /// disagree about when a lane meets something. The timeline shows the shape; this states the
    /// numbers behind it.
    /// </para>
    /// </remarks>
    private string BaseWaveLines(WaveSession session)
    {
        TuningData tuning = _controller.Tuning;
        var sb = new StringBuilder();

        foreach (LaneStrip lane in _controller.Strip.Lanes)
        {
            sb.AppendLine($"Lane {lane.LaneIndex}");

            UnitTrack[] units = [.. lane.Units.OrderBy(u => u.SpawnTime).ThenBy(u => u.SpawnIndex)];

            if (units.Length == 0)
            {
                sb.AppendLine("   —");
                continue;
            }

            // Grouped in arrival order rather than sorted by type, so the list reads in the order
            // the lane will meet them - which is the order the player has to answer them in.
            foreach (ArrivalRun run in ArrivalRuns(units))
            {
                string window = run.Count == 1
                    ? $"at {run.From:0.0}s"
                    : $"{run.From:0.0}–{run.To:0.0}s";

                sb.AppendLine(
                    $"   {run.Count,2}× {tuning.Enemy(run.EnemyId).DisplayName}  {SourceLabel(run.Source)}  {window}");
            }
        }

        // The located unknown, named before the deal. Its rank stays hidden; its lane does not
        // (docs/design/06-dealer-and-enemies.md § The hidden card's lane is visible from the start).
        sb.AppendLine();
        sb.Append($"Hidden card → lane {session.HiddenCardLane}, rank unknown");

        return sb.ToString();
    }

    /// <summary>A consecutive stretch of one enemy type from one origin, and when it arrives.</summary>
    private readonly record struct ArrivalRun(
        string EnemyId, SpawnSource Source, int Count, double From, double To);

    /// <summary>
    /// Consecutive runs of the same type and origin, in arrival order.
    /// </summary>
    /// <remarks>
    /// A run rather than a total, so a type that arrives, pauses, and arrives again reads as two
    /// waves instead of one lump - and a drawn reinforcement never hides inside the base wave's
    /// tally. Same rule the army block already uses; here it carries the timing as well.
    /// </remarks>
    private static List<ArrivalRun> ArrivalRuns(IReadOnlyList<UnitTrack> units)
    {
        List<ArrivalRun> runs = [];

        foreach (UnitTrack unit in units)
        {
            if (runs.Count > 0
                && runs[^1].EnemyId == unit.EnemyId
                && runs[^1].Source == unit.Source)
            {
                runs[^1] = runs[^1] with { Count = runs[^1].Count + 1, To = unit.SpawnTime };
                continue;
            }

            runs.Add(new ArrivalRun(unit.EnemyId, unit.Source, 1, unit.SpawnTime, unit.SpawnTime));
        }

        return runs;
    }
}
