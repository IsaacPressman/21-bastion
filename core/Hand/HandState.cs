using Bastion.Core.Cards;
using Bastion.Core.Config;

namespace Bastion.Core.Hand;

/// <summary>
/// The player's blackjack hand: the ranks drawn so far and the totals that follow from them.
/// </summary>
/// <remarks>
/// <para>
/// Immutable. <see cref="Hit"/> returns a new hand, and every total is derived from the ranks
/// rather than stored - so the Ace's mid-hand 11-to-1 transformation falls out of one recomputation
/// rather than a mutation. A hit that forces an Ace down changes <see cref="AceHighCount"/>, and any
/// tower placed for that Ace reads its state from here at board-build time
/// (docs/design/04-cards-as-defenses.md § Aces).
/// </para>
/// <para>
/// Blackjack and family are orthogonal at the arithmetic level: choosing Club or Spade never changes
/// a card's value, so this type knows nothing about placement (docs/design/02-blackjack-and-formation.md).
/// </para>
/// </remarks>
public sealed record HandState
{
    private HandState(IReadOnlyList<Rank> cards) => Cards = cards;

    /// <summary>The ranks drawn, in draw order.</summary>
    public IReadOnlyList<Rank> Cards { get; }

    public int CardCount => Cards.Count;

    /// <summary>An opening two-card deal.</summary>
    public static HandState Opening(Rank first, Rank second) => new([first, second]);

    /// <summary>An empty hand, before the deal. Total 0.</summary>
    public static HandState Empty { get; } = new([]);

    /// <summary>Advance-the-march-then-draw is the caller's job; this only adds the drawn card.</summary>
    public HandState Hit(Rank card) => new([.. Cards, card]);

    /// <summary>Number of Aces currently counted as 11 in <see cref="Total"/>.</summary>
    /// <remarks>
    /// Zero once the hand would bust with any Ace high. This is what a placed Ace tower reads to
    /// decide whether it is the 5.4-power anchor or the 1.0-power utility.
    /// </remarks>
    public int AceHighCount => Resolve().AceHigh;

    /// <summary>
    /// The hand's value: the best total at or under 21, or the hard sum when that is impossible.
    /// </summary>
    /// <remarks>
    /// When the hand busts this is the hard sum (all Aces low), which is above 21. Formation Strength
    /// still resolves it to the bust multiplier via <see cref="FormationMultiplier"/>.
    /// </remarks>
    public int Total => Resolve().Total;

    /// <summary>An Ace is being counted as 11: the total is "soft" and can absorb a hit.</summary>
    public bool IsSoft => AceHighCount > 0;

    /// <summary>The hand exceeds 21 even with every Ace counted as 1.</summary>
    public bool IsBust => Cards.Sum(r => r.LowValue()) > 21;

    /// <summary>Exactly 21, at any card count - the pullback trigger and the ceiling of the curve.</summary>
    public bool IsExactly21 => Total == 21;

    /// <summary>
    /// A two-card 21: an Ace beside a ten-value card. Grants the Ace Bastion
    /// (docs/design/02-blackjack-and-formation.md § Natural blackjack).
    /// </summary>
    public bool IsNaturalBlackjack => Cards.Count == 2 && Total == 21;

    /// <summary>
    /// The formation-wide multiplier this hand's final total earns.
    /// </summary>
    /// <remarks>
    /// Bust maps to the bust multiplier regardless of the numeric sum; otherwise the total indexes
    /// the Formation Strength curve. This is the whole of the hand's contribution to a tower's power.
    /// </remarks>
    public double FormationMultiplier(TuningData tuning)
    {
        ArgumentNullException.ThrowIfNull(tuning);

        return IsBust ? tuning.FormationStrength.Bust : tuning.FormationStrength.ForTotal(Total);
    }

    /// <summary>
    /// Standard blackjack Ace handling: start every Ace at 1, then promote as many to 11 as keep the
    /// total at or under 21.
    /// </summary>
    private (int Total, int AceHigh) Resolve()
    {
        int total = Cards.Sum(r => r.LowValue());
        int aces = Cards.Count(r => r == Rank.Ace);

        int high = 0;
        while (high < aces && total + 10 <= 21)
        {
            total += 10;
            high++;
        }

        return (total, high);
    }
}
