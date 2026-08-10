using Bastion.Core.Board;
using Bastion.Core.Cards;

namespace Bastion.Core.Validation;

/// <summary>
/// Everything about an offered state that can be read from the session alone.
/// </summary>
/// <remarks>
/// <para>
/// The core half of docs/prototype/VALIDATION.md § Instrumentation. What is <i>not</i> here is
/// everything only the interface knows - time to decide, whether placement changed before the
/// choice, which adjustment move was wanted, whether combat was watched or skipped - because none
/// of it is derivable from a <c>WaveSession</c>. That split is what lets the regression suite build
/// these headlessly.
/// </para>
/// <para>
/// Two Revision 7.1 corrections are honoured by shape rather than by discipline. Engagement is
/// recorded <b>per socket</b> (<see cref="SocketRecord.WindowRemaining"/>) and there is no summed
/// scalar anywhere on this type, because the summed figure was withdrawn - it treats sockets as
/// interchangeable when they are not, and now that range varies by socket it would be doubly
/// misleading. And the forecast comparison is against the <b>Final</b> Forecast; the Visible Threat
/// recorded here is labelled as the revealed force and is not a prediction of the wave.
/// </para>
/// </remarks>
public sealed record StateRecord
{
    /// <summary>Which battery case this state belongs to, or null in free play.</summary>
    public string? FixtureId { get; init; }

    /// <summary>The march arm in force. The whole point of the exercise.</summary>
    public required string Arm { get; init; }

    /// <summary>The phase the player is being asked to act in.</summary>
    public required string Phase { get; init; }

    public required HandRecord Hand { get; init; }

    /// <summary>What is left in the pile, by rank, with the busting ranks marked.</summary>
    public required IReadOnlyList<PileEntry> Pile { get; init; }

    public required MarchRecord March { get; init; }

    /// <summary>One entry per socket, occupied or not.</summary>
    public required IReadOnlyList<SocketRecord> Sockets { get; init; }

    /// <summary>Per-lane readings. Visible Threat during the draw, Final Forecast after the Dealer.</summary>
    public required IReadOnlyList<LaneRecord> Lanes { get; init; }

    /// <summary>Which of the two forecast contracts <see cref="Lanes"/> carries.</summary>
    public required string LaneReading { get; init; }

    public required DealerRecord Dealer { get; init; }

    /// <summary>Cards drawn and not yet placed, in draw order.</summary>
    public required IReadOnlyList<string> PendingRanks { get; init; }

    /// <summary>Whether the wave's single adjustment move has been spent.</summary>
    public required bool MoveSpent { get; init; }
}

public sealed record HandRecord
{
    /// <summary>Every card in the hand, in deal order.</summary>
    public required IReadOnlyList<string> Cards { get; init; }

    public required int Total { get; init; }

    /// <summary>Soft means an Ace is currently counted as 11.</summary>
    public required bool IsSoft { get; init; }

    /// <summary>How many Aces are held high. The Ace state VALIDATION.md asks for.</summary>
    public required int AcesHigh { get; init; }

    public required bool IsBust { get; init; }
    public required double FormationMultiplier { get; init; }
}

/// <summary>One rank's remaining count, and whether drawing it would bust the current hand.</summary>
/// <remarks>
/// The bust mark is a hand computation, not a shoe one, and it is what the design shows in place of
/// a percentage: six safe cards in a pile of twenty-two, read rather than looked up
/// (docs/design/09-information-and-ui.md). Logging the same thing the player sees is what makes
/// "the choice made" interpretable afterwards.
/// </remarks>
public sealed record PileEntry
{
    public required string Rank { get; init; }
    public required int Remaining { get; init; }
    public required bool WouldBust { get; init; }

    /// <summary>Whether this rank would land the hand on exactly 21.</summary>
    public required bool ReachesTwentyOne { get; init; }
}

public sealed record MarchRecord
{
    public required double Entry { get; init; }

    /// <summary>What the next card would cost in entry advance, before it is revealed.</summary>
    public required double NextStepCost { get; init; }

    public required int CardCount { get; init; }
}

/// <summary>
/// One socket: where it is, what stands there, and how much of its window survives the march.
/// </summary>
/// <remarks>
/// <see cref="WindowRemaining"/> is this socket's engagement, and it is deliberately never summed
/// on this type. <see cref="Depth"/> is the placement-depth measurement with the pre-committed
/// reading attached (docs/prototype/VALIDATION.md § Specific instrumentation) - it is how the
/// residual shallow lean left by the geometry remedy gets watched in playtest.
/// </remarks>
public sealed record SocketRecord
{
    /// <summary>The socket, as <c>L0S2</c> or <c>J</c>.</summary>
    public required string Socket { get; init; }

    /// <summary>Path position. Larger is deeper, i.e. further from the spawn side.</summary>
    public required double Depth { get; init; }

    /// <summary>
    /// Effective firing range here: the standing tower's, including its face-card allowance, or the
    /// socket's own range when nothing stands there.
    /// </summary>
    /// <remarks>
    /// An empty socket still has a window - that is what makes it worth taking - so the figure is
    /// never blank, and <see cref="WindowRemaining"/> is computed from whichever range applies.
    /// </remarks>
    public required double Range { get; init; }

    public required bool Occupied { get; init; }

    public string? Rank { get; init; }
    public string? Family { get; init; }

    /// <summary>Run-link bonus this tower is earning, 0 when it is in no run.</summary>
    public double RunBonus { get; init; }

    /// <summary>Units of path this socket can still fire over, given where the army enters.</summary>
    public required double WindowRemaining { get; init; }

    /// <summary>What the window would fall to if the player took one more card.</summary>
    public required double WindowAfterNextStep { get; init; }
}

public sealed record LaneRecord
{
    public required int LaneIndex { get; init; }
    public required string Stake { get; init; }
    public required int EmptyLaneDamage { get; init; }
    public required int PredictedDamage { get; init; }

    /// <summary>The one permitted piece of interpretation, on a plain threshold.</summary>
    public required bool IsOpen { get; init; }
}

public sealed record DealerRecord
{
    /// <summary>The upcard, standing on the field as the Vanguard from the start.</summary>
    public required string Upcard { get; init; }

    /// <summary>The unit the upcard deployed - what the player is meant to read it as.</summary>
    public required string UpcardUnit { get; init; }

    public required int VanguardLane { get; init; }

    /// <summary>The Dealer's full hand once it has resolved, or null before that.</summary>
    public IReadOnlyList<string>? Cards { get; init; }

    /// <summary>Units the Dealer's cards deployed, once resolved.</summary>
    public IReadOnlyList<string>? DeployedUnits { get; init; }
}
