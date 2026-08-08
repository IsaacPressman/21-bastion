namespace Bastion.Core.Wave;

/// <summary>
/// Where a wave has reached in its state machine.
/// </summary>
/// <remarks>
/// <para>
/// The wave is a small, explicit state machine with a <b>hard boundary</b> between the draw phase,
/// where decisions are made, and the adjustment window, where they are refined. Blurring that
/// boundary is the failure mode the design guards against (docs/design/01-core-loop.md).
/// </para>
/// <para>
/// Bust has its own terminal phase rather than a flag on the adjustment window: a bust skips the
/// window entirely - placement locks immediately - so there is no state in which a busted wave is
/// "adjusting" (docs/design/07-bust-and-overload.md).
/// </para>
/// </remarks>
public enum WavePhase
{
    /// <summary>A drawn card is waiting to be placed: its family and socket are still to be chosen.</summary>
    AwaitingPlacement,

    /// <summary>Every drawn card is placed; the player chooses to hit or stand.</summary>
    DrawDecision,

    /// <summary>The Dealer has resolved on a stand; one adjustment move is available before the lock.</summary>
    AdjustmentWindow,

    /// <summary>Placement is locked and the wave will resolve as the Final Forecast says.</summary>
    Locked,

    /// <summary>A bust locked placement with no adjustment window; the Dealer still resolved in full.</summary>
    BustLocked,
}
