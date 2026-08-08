namespace Bastion.Core.Cards;

/// <summary>
/// A card in play, with its Ace state resolved.
/// </summary>
/// <remarks>
/// <para>
/// Aces count as 1 or 11 and mirror that on the field - 1.0 power compact utility, or 5.4 power
/// formation-defining (docs/design/04-cards-as-defenses.md). The state travels with the card
/// because a hit that forces an Ace from 11 down to 1 transforms the battlefield object
/// immediately, so the resolver must be able to see which one it is looking at.
/// </para>
/// <para>
/// Milestone 1 takes the state as given. Deciding it - and updating it mid-hand - is Milestone 2.
/// </para>
/// </remarks>
public readonly record struct Card(Rank Rank, bool AceHigh = false)
{
    /// <summary>Blackjack and power-curve value. 1..11.</summary>
    public int Value => Rank == Rank.Ace && AceHigh ? 11 : Rank.LowValue();

    /// <summary>
    /// Whether this card gets the face-card range and the junction penalty exemption.
    /// </summary>
    /// <remarks>
    /// Keyed on value 10, not on the rank being a court card: docs/design/04-cards-as-defenses.md
    /// says "All 10/J/Q/K", and the printed Ten is included deliberately - face cards buy their
    /// advantage by being value 10, which is also why they cannot form runs with each other.
    /// </remarks>
    public bool HasFaceCardRange => Value == 10;

    /// <summary>The King is the anchor: it ignores half of flat armor and cannot be displaced.</summary>
    public bool IsKing => Rank == Rank.King;

    public override string ToString() => Rank == Rank.Ace ? $"A({Value})" : Rank.TuningKey();
}
