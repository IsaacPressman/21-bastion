using Bastion.Core.Board;
using Bastion.Core.Cards;
using Bastion.Core.Config;

namespace Bastion.Core.Resolve;

/// <summary>
/// One unit that gets through, and what it would have taken to stop it.
/// </summary>
/// <remarks>
/// <para>
/// <b>The requirement, never the answer.</b> docs/design/14-encounter-timeline.md § Shortfall is
/// stated in battlefield language, never card language: display "needs 2.1 more armor-effective
/// damage", never "a mid-rank Siege Club here will solve this". The mental step the design protects
/// is <i>battlefield requirement + drawn rank + available forms + geometry → player judgment</i>,
/// and naming the answer deletes the last arrow.
/// </para>
/// <para>
/// <b>The line is the unit itself, and that is a Milestone 6 stand-in.</b> The design anchors the
/// requirement on a spatial breakpoint - "before socket 6" - and breakpoints are Milestone 7
/// (docs/ROADMAP.md). Until one exists, the line is the unit's own survival: <see cref="Required"/>
/// is what it takes to kill it at all. The sentence keeps its shape when the real line arrives; only
/// what <see cref="Required"/> is measured against changes.
/// </para>
/// </remarks>
public sealed record LeakerConsequence
{
    public required int SpawnIndex { get; init; }
    public required string EnemyId { get; init; }

    /// <summary>The unit's name, so a lane can be described in units rather than ids.</summary>
    public required string DisplayName { get; init; }

    public required int LeakDamage { get; init; }
    public required LeakCause Cause { get; init; }

    /// <summary>When it reaches the end.</summary>
    public required double LeakTime { get; init; }

    /// <summary>Armor-effective damage needed to stop it: its full health.</summary>
    public required double Required { get; init; }

    /// <summary>Armor-effective damage the current formation actually lands on it.</summary>
    public required double Delivered { get; init; }

    /// <summary>What is still missing. Zero would mean it did not leak.</summary>
    public double Shortfall => Required - Delivered;
}

/// <summary>What one tower does in one lane, stated as attacks rather than as a damage total.</summary>
/// <remarks>
/// "Attacks or triggers each tower receives" is one of the exact committed-state statistics
/// (docs/design/14-encounter-timeline.md). It is the figure the March step's cost is expressed in -
/// <b>"this cannon loses two shots"</b> - which is why the count leads and the damage follows.
/// </remarks>
public sealed record TowerAttacks
{
    public required SocketRef Socket { get; init; }
    public required Card Card { get; init; }
    public required Family Family { get; init; }
    public required int Shots { get; init; }
    public required int Kills { get; init; }
    public required double DamageApplied { get; init; }
    public required double? FirstFireTime { get; init; }
}

/// <summary>
/// Everything the player may be told about one lane's committed state, in battlefield language.
/// </summary>
/// <remarks>
/// <para>
/// A pure projection over a <see cref="LaneOutcome"/> the resolver already produced - it computes no
/// combat and it is not a third forecast. It exists so the interface does no arithmetic of its own:
/// a view that derived "needs 2.1 more" itself would be a second account of the resolver's output.
/// </para>
/// <para>
/// <b>Nothing here crosses a lane boundary and nothing here is summed.</b> Sockets are not
/// interchangeable and neither are lanes - a bastion lane taking 3 and a vault lane taking 16 pull
/// in opposite directions, which is exactly why multiple stakes are a guardrail against the
/// encounter collapsing into one number to minimise (docs/design/14-encounter-timeline.md § The
/// solvable-puzzle risk). Do not add a property that totals these across lanes.
/// </para>
/// </remarks>
public sealed record LaneConsequence
{
    public required int LaneIndex { get; init; }
    public required string Stake { get; init; }

    /// <summary>What this lane takes with none of its towers.</summary>
    public required int EmptyLaneDamage { get; init; }

    /// <summary>What it takes under the current plan.</summary>
    public required int PredictedDamage { get; init; }

    public int DamagePrevented => EmptyLaneDamage - PredictedDamage;

    /// <summary>The one permitted piece of interpretation, on a plain threshold. Never shown alone.</summary>
    public required bool IsOpen { get; init; }

    /// <summary>The glance-read label. Never show it without the number.</summary>
    public required string CoverageLabel { get; init; }

    /// <summary>When the lane first breaks, or null if nothing gets through.</summary>
    public double? FirstLeakTime => Leakers.Count == 0 ? null : Leakers[0].LeakTime;

    /// <summary>Units that get through, earliest first.</summary>
    public required IReadOnlyList<LeakerConsequence> Leakers { get; init; }

    /// <summary>
    /// The first unit through: the one the lane's shortfall sentence is about.
    /// </summary>
    /// <remarks>
    /// The earliest leak is the problem the player is actually being asked to solve. A later one may
    /// be solved by the same tower, and stating every shortfall at once turns a diagnosis into a
    /// list.
    /// </remarks>
    public LeakerConsequence? LeadLeaker => Leakers.Count == 0 ? null : Leakers[0];

    /// <summary>Towers firing into this lane, deepest last. A junction tower appears in both lanes.</summary>
    public required IReadOnlyList<TowerAttacks> Towers { get; init; }

    /// <summary>Reads one lane's outcome into the statements the interface is allowed to make.</summary>
    public static LaneConsequence For(LaneOutcome lane, TuningData tuning, double thresholdFraction)
    {
        ArgumentNullException.ThrowIfNull(lane);
        ArgumentNullException.ThrowIfNull(tuning);

        return new LaneConsequence
        {
            LaneIndex = lane.LaneIndex,
            Stake = lane.Stake,
            EmptyLaneDamage = lane.EmptyLaneDamage,
            PredictedDamage = lane.PredictedDamage,
            IsOpen = lane.IsOpen(thresholdFraction),
            CoverageLabel = lane.CoverageLabel(thresholdFraction),
            Leakers =
            [
                .. lane.LeakedUnits
                    .OrderBy(u => u.LeakTime)
                    .ThenBy(u => u.SpawnIndex)
                    .Select(unit => Describe(unit, tuning)),
            ],
            Towers =
            [
                .. lane.TowerActivity.Select(activity => new TowerAttacks
                {
                    Socket = activity.Socket,
                    Card = activity.Card,
                    Family = activity.Family,
                    Shots = activity.Shots,
                    Kills = activity.Kills,
                    DamageApplied = activity.DamageApplied,
                    FirstFireTime = activity.FirstFireTime,
                }),
            ],
        };
    }

    /// <summary>All lanes of a reading, in lane order.</summary>
    public static IReadOnlyList<LaneConsequence> ForAll(
        IReadOnlyList<LaneOutcome> lanes, TuningData tuning, double thresholdFraction)
    {
        ArgumentNullException.ThrowIfNull(lanes);

        return [.. lanes.Select(lane => For(lane, tuning, thresholdFraction))];
    }

    /// <summary>
    /// Turns a leaked unit into a requirement and a shortfall.
    /// </summary>
    /// <remarks>
    /// The resolver records what fraction of its health a leaker arrived with, so delivered damage is
    /// that fraction inverted against the unit's health from tuning. Reading it back out this way
    /// rather than re-summing shot events keeps one source: the shots the resolver counted <i>are</i>
    /// the health the unit lost.
    /// </remarks>
    private static LeakerConsequence Describe(LeakedUnit unit, TuningData tuning)
    {
        EnemyTuning type = tuning.Enemy(unit.EnemyId);
        double required = type.Health;

        return new LeakerConsequence
        {
            SpawnIndex = unit.SpawnIndex,
            EnemyId = unit.EnemyId,
            DisplayName = type.DisplayName,
            LeakDamage = unit.LeakDamage,
            Cause = unit.Cause,
            LeakTime = unit.LeakTime,
            Required = required,
            Delivered = required * (1.0 - unit.RemainingHealthFraction),
        };
    }
}
