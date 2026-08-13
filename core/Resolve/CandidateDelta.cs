using Bastion.Core.Board;
using Bastion.Core.Cards;

namespace Bastion.Core.Resolve;

/// <summary>What one lane's reading would become if this candidate were committed.</summary>
/// <remarks>
/// Per lane, and <b>against a different stake in each</b>. That is the guardrail, not an accident of
/// layout: two lanes carrying bastion and vault stakes cannot be traded off by subtracting one
/// number from another, which is what stops a candidate list collapsing into a single quantity to
/// minimise (docs/design/14-encounter-timeline.md § The solvable-puzzle risk).
/// </remarks>
public sealed record LaneDelta
{
    public required int LaneIndex { get; init; }
    public required string Stake { get; init; }

    public required int LeakCountBefore { get; init; }
    public required int LeakCountAfter { get; init; }

    public required int PredictedDamageBefore { get; init; }
    public required int PredictedDamageAfter { get; init; }

    /// <summary>When the lane first breaks, before and after. Null means it does not.</summary>
    public required double? FirstLeakTimeBefore { get; init; }
    public required double? FirstLeakTimeAfter { get; init; }

    /// <summary>True when this candidate changes nothing in this lane.</summary>
    public bool IsUnchanged =>
        LeakCountBefore == LeakCountAfter
        && PredictedDamageBefore == PredictedDamageAfter
        && Nullable.Equals(FirstLeakTimeBefore, FirstLeakTimeAfter);
}

/// <summary>A named unit whose fate the candidate changes: <c>survives → killed</c>.</summary>
/// <remarks>
/// The handoff's own example of a causal delta. A unit changing from getting through to being
/// stopped is the sentence a player can repeat back afterwards; "predicted value 5.1 → 3.4" is not.
/// </remarks>
public sealed record UnitFateChange
{
    public required int SpawnIndex { get; init; }
    public required string EnemyId { get; init; }
    public required string DisplayName { get; init; }
    public required int LaneIndex { get; init; }

    /// <summary>Whether it got through before this candidate.</summary>
    public required bool LeaksBefore { get; init; }

    /// <summary>Whether it gets through after it.</summary>
    public required bool LeaksAfter { get; init; }
}

/// <summary>
/// How many attacks a tower gets, now and after the next march step, before and after the candidate.
/// </summary>
/// <remarks>
/// <see cref="AfterNextStep"/> is the March cost of this specific arrangement - "Club 8 attacks:
/// 3 → 2 after next March step" - which is how the step is priced in consequence rather than in
/// entry position.
/// </remarks>
public sealed record TowerShotDelta
{
    public required SocketRef Socket { get; init; }
    public required int LaneIndex { get; init; }
    public required int Before { get; init; }
    public required int After { get; init; }

    /// <summary>Attacks this tower would get if the player then took another card. Null past the clamp.</summary>
    public required int? AfterNextStep { get; init; }
}

/// <summary>Run links this candidate makes or breaks: <c>inactive → 3-card run</c>.</summary>
public sealed record RunDelta
{
    public required int LinkedTowersBefore { get; init; }
    public required int LinkedTowersAfter { get; init; }
    public required double BestBonusBefore { get; init; }
    public required double BestBonusAfter { get; init; }

    public bool IsUnchanged =>
        LinkedTowersBefore == LinkedTowersAfter
        && BestBonusBefore.Equals(BestBonusAfter);
}

/// <summary>The tower this candidate would evict, and what it is worth. Null on an empty socket.</summary>
/// <remarks>
/// What a card displaces is one of the three clauses of the design's narrowed claim, so a candidate
/// that costs a 9 and one that costs a 2 must not read as the same move.
/// </remarks>
public sealed record DisplacedTower
{
    public required Card Card { get; init; }
    public required Family Family { get; init; }
    public required double ShotDamage { get; init; }
    public required double RunBonus { get; init; }
}

/// <summary>
/// What committing the pending card to one family and socket would change, as causal facts.
/// </summary>
/// <remarks>
/// <para>
/// <b>Consequences of a candidate action, never the answer.</b> docs/design/14-encounter-timeline.md
/// § Candidate placements show causal deltas: <i>"Before drawing, show the requirement. After
/// drawing, show the consequences of candidate actions. Do not show the answer."</i>
/// </para>
/// <para>
/// <b>There is deliberately no aggregate on this type, and none may be added.</b> Not a projected
/// value, not a total damage prevented, not a score - because "a single comparable number lets the
/// player brute-force every socket until the smallest one appears", and <i>a combined verdict is no
/// less a verdict for being computed per candidate</i> (Hard Invariant 2). Everything here is either
/// per lane, per unit, or per tower. <c>tests/Resolve/CandidateDeltaTests.cs</c> pins the property
/// list so a convenience total cannot arrive quietly.
/// </para>
/// <para>
/// It is produced by running the real placement transition and re-reading the resolver, not by an
/// estimator - see <c>WaveSession.PreviewPlacement</c>. There is one simulation path, and a preview
/// that disagreed with what actually happens would be worse than no preview.
/// </para>
/// </remarks>
public sealed record CandidateDelta
{
    /// <summary>The card being considered.</summary>
    public required Rank Rank { get; init; }

    /// <summary>The family this candidate would lock in. Permanent for the wave once placed.</summary>
    public required Family Family { get; init; }

    public required SocketRef Socket { get; init; }

    /// <summary>What it would evict, or null if the socket is empty.</summary>
    public DisplacedTower? Displaces { get; init; }

    /// <summary>One entry per lane, in lane order.</summary>
    public required IReadOnlyList<LaneDelta> Lanes { get; init; }

    /// <summary>Units whose fate changes, in spawn order. Empty when none does.</summary>
    public required IReadOnlyList<UnitFateChange> FateChanges { get; init; }

    /// <summary>Every tower's attack count before and after, including the candidate itself.</summary>
    public required IReadOnlyList<TowerShotDelta> TowerShots { get; init; }

    public required RunDelta Runs { get; init; }
}
