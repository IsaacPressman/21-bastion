using Bastion.Core.Board;
using Bastion.Core.Cards;

namespace Bastion.Core.Resolve;

/// <summary>Why a unit got through. Reported so a leak can be explained after the wave.</summary>
public enum LeakCause
{
    /// <summary>No tower fires into this lane at all.</summary>
    LaneUndefended,

    /// <summary>Towers exist, but this unit never entered any of their windows.</summary>
    NeverInRange,

    /// <summary>It was shot at and survived anyway.</summary>
    OutDamaged,
}

/// <summary>A unit that reached the end, and why.</summary>
public sealed record LeakedUnit
{
    public required int SpawnIndex { get; init; }
    public required string EnemyId { get; init; }
    public required int LeakDamage { get; init; }
    public required LeakCause Cause { get; init; }

    /// <summary>Health it arrived with, as a fraction of its maximum. 1.0 means it was never touched.</summary>
    public required double RemainingHealthFraction { get; init; }

    /// <summary>
    /// When it reached the end, in seconds.
    /// </summary>
    /// <remarks>
    /// The lane's <b>first leak time</b> is the earliest of these, and it is one of the exact
    /// committed-state statistics the encounter is required to state
    /// (docs/design/14-encounter-timeline.md § Exact consequences for the committed state). It comes
    /// from the same tick the <see cref="LeakEvent"/> is stamped with, so the readout and the
    /// timeline cannot disagree about when a lane breaks.
    /// </remarks>
    public required double LeakTime { get; init; }
}

/// <summary>
/// What one tower did during the wave, in one lane.
/// </summary>
/// <remarks>
/// A junction tower appears in both lanes' activity, each at its reduced contribution. That is the
/// honest reading of "buys breadth and forfeits synergy": the breadth is visible as two entries.
/// </remarks>
public sealed record TowerActivity
{
    public required SocketRef Socket { get; init; }
    public required Card Card { get; init; }
    public required Family Family { get; init; }
    public required int Shots { get; init; }
    public required double DamageBeforeArmor { get; init; }
    public required double DamageApplied { get; init; }
    public required int Kills { get; init; }

    /// <summary>Ticks the tower was off cooldown with nothing it was willing to shoot.</summary>
    public required int IdleTicks { get; init; }

    public required double? FirstFireTime { get; init; }
    public required double? LastFireTime { get; init; }

    /// <summary>How much of the shot damage armor ate. Explains a bad matchup without a verdict.</summary>
    public double DamageLostToArmor => DamageBeforeArmor - DamageApplied;
}

/// <summary>
/// Everything the resolver reports about one lane.
/// </summary>
/// <remarks>
/// The five outputs docs/design/05-battlefield.md requires: empty-lane damage, predicted damage
/// under the current plan, damage prevented, per-tower activity, and the cause of remaining
/// leakage.
/// </remarks>
public sealed record LaneOutcome
{
    public required int LaneIndex { get; init; }

    /// <summary>bastion or vault. A player who is healthy but poor triages differently.</summary>
    public required string Stake { get; init; }

    /// <summary>What this lane would take with none of its towers, the other lane untouched.</summary>
    public required int EmptyLaneDamage { get; init; }

    /// <summary>What it takes under the current plan.</summary>
    public required int PredictedDamage { get; init; }

    public required IReadOnlyList<LeakedUnit> LeakedUnits { get; init; }
    public required IReadOnlyList<TowerActivity> TowerActivity { get; init; }

    public int DamagePrevented => EmptyLaneDamage - PredictedDamage;

    /// <summary>
    /// Open when predicted leakage is at least half of empty-lane damage; Held below that.
    /// </summary>
    /// <remarks>
    /// <b>This is the maximum amount of interpretation the game is permitted to do for the
    /// player</b> (docs/design/09-information-and-ui.md). The number is primary and the label is a
    /// glance-read. Nothing here may grow into a recommended action, a hit/stand edge, or a
    /// combined verdict - hand consequences and battlefield consequences are displayed separately,
    /// and combining them is the player's job.
    /// </remarks>
    public bool IsOpen(double thresholdFraction) =>
        EmptyLaneDamage > 0 && PredictedDamage >= thresholdFraction * EmptyLaneDamage;

    /// <summary>The glance-read label. Never show it without the number.</summary>
    public string CoverageLabel(double thresholdFraction) => IsOpen(thresholdFraction) ? "Open" : "Held";

    /// <summary>
    /// Structural equality, including the two lists.
    /// </summary>
    /// <remarks>
    /// A record's synthesised equality compares list <i>references</i>, so two identical
    /// resolutions would report as different. That matters here specifically: the regression
    /// procedures in docs/prototype/VALIDATION.md compare outcomes across runs to prove the
    /// resolver is deterministic, and a comparison that always says "different" cannot fail - so it
    /// would pass a broken resolver by never being able to detect a working one.
    /// </remarks>
    public bool Equals(LaneOutcome? other) =>
        other is not null
        && LaneIndex == other.LaneIndex
        && string.Equals(Stake, other.Stake, StringComparison.Ordinal)
        && EmptyLaneDamage == other.EmptyLaneDamage
        && PredictedDamage == other.PredictedDamage
        && LeakedUnits.SequenceEqual(other.LeakedUnits)
        && TowerActivity.SequenceEqual(other.TowerActivity);

    public override int GetHashCode() =>
        HashCode.Combine(LaneIndex, Stake, EmptyLaneDamage, PredictedDamage, LeakedUnits.Count, TowerActivity.Count);
}
