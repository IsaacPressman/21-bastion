namespace Bastion.Core.Cards;

/// <summary>
/// A printed rank. The prototype shoe is neutral - suits are not printed on cards, because family
/// is chosen at placement rather than dealt (docs/design/04-cards-as-defenses.md).
/// </summary>
public enum Rank
{
    Ace = 1,
    Two = 2,
    Three = 3,
    Four = 4,
    Five = 5,
    Six = 6,
    Seven = 7,
    Eight = 8,
    Nine = 9,
    Ten = 10,
    Jack = 11,
    Queen = 12,
    King = 13,
}

/// <summary>
/// Rank properties the Dealer mapping and the power curve need.
/// </summary>
/// <remarks>
/// Deliberately thin. The shoe, hand totals, hard/soft handling, and the Ace's mid-hand 11-to-1
/// transformation are Milestone 2; this exists so Milestone 1 can name a Dealer card and look up a
/// tower's power without inventing a card model that Milestone 2 would then have to replace.
/// </remarks>
public static class RankExtensions
{
    /// <summary>
    /// The key used by <c>dealerCardUnits</c> in tuning data.
    /// </summary>
    public static string TuningKey(this Rank rank) => rank switch
    {
        Rank.Ace => "A",
        Rank.Jack => "J",
        Rank.Queen => "Q",
        Rank.King => "K",
        _ => ((int)rank).ToString(System.Globalization.CultureInfo.InvariantCulture),
    };

    /// <summary>
    /// Blackjack value, with an Ace at 1. Face cards are 10.
    /// </summary>
    /// <remarks>
    /// An Ace held high is 11, which is why <see cref="Card"/> carries the state rather than the
    /// rank doing so: the same rank is two different battlefield objects depending on the hand.
    /// </remarks>
    public static int LowValue(this Rank rank) => rank switch
    {
        Rank.Jack or Rank.Queen or Rank.King => 10,
        _ => (int)rank,
    };
}
