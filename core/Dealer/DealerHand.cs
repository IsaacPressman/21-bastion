using Bastion.Core.Cards;
using Bastion.Core.Config;
using Bastion.Core.Hand;

namespace Bastion.Core.Dealer;

/// <summary>
/// The Dealer's draw policy: reveal the hole card and draw to 17.
/// </summary>
/// <remarks>
/// <para>
/// This is the piece <see cref="DealerDeployment"/> deliberately left for Milestone 3 - it turns a
/// two-card opening plus a shoe into the Dealer's finished hand. There is no total comparison
/// anywhere: the Dealer draws to a number only to decide <i>how many units arrive</i>, never to be
/// beaten (docs/design/06-dealer-and-enemies.md).
/// </para>
/// <para>
/// The prototype Dealer stands on all 17s, including soft 17, per <c>rules.dealerStandsOnAll17s</c>.
/// Reached on the player's stand <b>or bust</b> alike: resolution is purely "deploy", so a bust
/// changes the player's formation and nothing about the army.
/// </para>
/// </remarks>
public static class DealerHand
{
    /// <summary>
    /// Resolves the Dealer from an upcard and a hole card, drawing to 17, and returns the finished
    /// hand as deployable cards alongside the shoe that remains.
    /// </summary>
    public static (IReadOnlyList<Card> Cards, Shoe Remaining) Resolve(
        TuningData tuning, Rank upcard, Rank holeCard, Shoe shoe)
    {
        ArgumentNullException.ThrowIfNull(tuning);
        ArgumentNullException.ThrowIfNull(shoe);

        HandState hand = HandState.Opening(upcard, holeCard);
        Shoe remaining = shoe;

        while (ShouldHit(tuning, hand))
        {
            (Rank rank, Shoe next) = remaining.Draw();
            hand = hand.Hit(rank);
            remaining = next;
        }

        return (ToCards(hand), remaining);
    }

    /// <summary>Whether the Dealer draws another card in its current state.</summary>
    /// <remarks>
    /// Below 17 always hits; at a soft 17 it stands unless the tuning switches off stand-on-all-17s;
    /// at 18 or more (including a bust, whose hard total is above 21) it always stands.
    /// </remarks>
    private static bool ShouldHit(TuningData tuning, HandState hand) =>
        hand.Total < 17
        || (hand.Total == 17 && hand.IsSoft && !tuning.Rules.DealerStandsOnAll17s);

    /// <summary>
    /// Stamps each Ace high or low from the finished hand, high in draw order.
    /// </summary>
    /// <remarks>
    /// The Ace state is what splits the Herald into an elite (11) or a scout (1) at deployment, so it
    /// has to travel on the card - a bare rank cannot say which one arrived. The assignment among
    /// several Aces is not observable in blackjack, so it is made deterministically rather than left
    /// to iteration order.
    /// </remarks>
    public static IReadOnlyList<Card> ToCards(HandState hand)
    {
        ArgumentNullException.ThrowIfNull(hand);

        int high = hand.AceHighCount;
        int assigned = 0;
        List<Card> cards = new(hand.CardCount);

        foreach (Rank rank in hand.Cards)
        {
            bool aceHigh = rank == Rank.Ace && assigned < high;
            if (aceHigh)
            {
                assigned++;
            }

            cards.Add(new Card(rank, aceHigh));
        }

        return cards;
    }
}
