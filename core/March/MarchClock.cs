using Bastion.Core.Config;

namespace Bastion.Core.March;

/// <summary>
/// Where the enemy entry point sits for a hand of a given length.
/// </summary>
/// <remarks>
/// Early Milestone 1 slice. Pure, deterministic, engine-free. The step sizes are the most
/// important numbers in the game and the most likely to be wrong, so they arrive from tuning
/// data and never from a literal here.
/// </remarks>
public static class MarchClock
{
    /// <summary>
    /// Entry position after drawing <paramref name="cardCount"/> cards.
    /// </summary>
    /// <param name="reachedExactly21">
    /// Whether the hand totals exactly 21, at any card count. This is the entire bonus for a
    /// perfect formation - there is no separate multiplier bump or card-count table.
    /// </param>
    /// <remarks>
    /// Order matters: the length clamp applies first, then the pullback comes off the clamped
    /// value. A six-card 21 therefore lands at 6.0 rather than being stranded at the clamp, which
    /// keeps the rescue real at the point where it is most needed.
    /// </remarks>
    public static double EntryAfter(TuningData tuning, int cardCount, bool reachedExactly21)
    {
        ArgumentNullException.ThrowIfNull(tuning);
        ArgumentOutOfRangeException.ThrowIfNegative(cardCount);

        MarchTuning march = tuning.March;

        double entry = tuning.ActiveMarchPreset.CumulativeEntry(cardCount, march.FreeCards);

        // Clamp before the pullback, not after.
        entry = Math.Min(entry, march.EntryClampMax);

        if (reachedExactly21)
        {
            entry -= march.Exactly21Pullback;
        }

        return Math.Clamp(entry, march.EntryClampMin, march.EntryClampMax);
    }

    /// <summary>
    /// What the next card would cost in entry advance, before it is revealed.
    /// </summary>
    /// <remarks>
    /// Zero once the clamp is reached. Past that point further cards are free on the clock - a
    /// mild perverse incentive that the design accepts rather than engineers around, because at
    /// six-plus cards the bust probability is enormous and every further card forces the player
    /// to replace one of their own towers.
    /// </remarks>
    public static double NextStepCost(TuningData tuning, int cardsHeld)
    {
        ArgumentNullException.ThrowIfNull(tuning);

        double current = EntryAfter(tuning, cardsHeld, reachedExactly21: false);
        double next = EntryAfter(tuning, cardsHeld + 1, reachedExactly21: false);

        return next - current;
    }
}
