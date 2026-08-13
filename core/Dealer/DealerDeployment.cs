using Bastion.Core.Cards;
using Bastion.Core.Config;

namespace Bastion.Core.Dealer;

/// <summary>
/// One Dealer card, turned into the units it puts on the field and the lane they enter.
/// </summary>
public sealed record DealerUnits
{
    public required Card Card { get; init; }
    public required string EnemyId { get; init; }
    public required int Count { get; init; }
    public required int LaneIndex { get; init; }

    /// <summary>True for the upcard, which is deployed before the opening deal.</summary>
    public required bool IsVanguard { get; init; }
}

/// <summary>
/// Maps a Dealer's hand onto the battlefield.
/// </summary>
/// <remarks>
/// <para>
/// <b>The Dealer is a wave generator. Their hand is their army. There is no comparison between
/// totals</b> (docs/design/06-dealer-and-enemies.md). Nothing here reads a Dealer total, and
/// nothing should be added that does - any payout on beating the Dealer pulls play back toward
/// basic strategy, which is the exact failure the whole design exists to avoid.
/// </para>
/// <para>
/// Draw policy is not here. Standing on all 17s and drawing after the player stands is Milestone 3;
/// this takes an already-known hand and deploys it.
/// </para>
/// </remarks>
public static class DealerDeployment
{
    /// <summary>
    /// Deploys a Dealer hand. The first card is the Vanguard.
    /// </summary>
    /// <remarks>
    /// <b>Lane assignment is NOT SPECIFIED BY THE DESIGN.</b> The Vanguard takes the encounter's
    /// vanguard lane - "an armored soldier already standing at the head of lane one" - and every
    /// later card alternates. Deterministic, unsteerable, and it keeps both lanes live so lane
    /// triage stays a real decision rather than resolving itself from the upcard.
    /// </remarks>
    public static IReadOnlyList<DealerUnits> Deploy(
        TuningData tuning,
        EncounterTuning encounter,
        IReadOnlyList<Card> dealerHand)
    {
        ArgumentNullException.ThrowIfNull(tuning);
        ArgumentNullException.ThrowIfNull(encounter);
        ArgumentNullException.ThrowIfNull(dealerHand);

        List<DealerUnits> deployed = new(dealerHand.Count);

        for (int i = 0; i < dealerHand.Count; i++)
        {
            Card card = dealerHand[i];
            string enemyId = UnitFor(tuning, card);

            deployed.Add(new DealerUnits
            {
                Card = card,
                EnemyId = enemyId,

                // A card deploys the whole pack from its enemy row: a 3 is eight swarm units, a
                // King is one siege engine. NOT SPECIFIED BY THE DESIGN - but it is what "Swarm
                // pack - many, fragile" against a singular "Siege engine" describes.
                Count = tuning.Waves.DealerCardDeploysFullPack ? tuning.Enemy(enemyId).Count : 1,

                LaneIndex = LaneRule(tuning, encounter, cardIndex: i),
                IsVanguard = i == 0,
            });
        }

        return deployed;
    }

    /// <summary>The enemy type a card's rank produces.</summary>
    /// <remarks>
    /// Keyed on printed rank, not blackjack value: a Jack is a Skirmisher and a King is a siege
    /// engine, though both count as ten in the Dealer's hand. The one exception is the Ace's Herald
    /// split - an Ace held low (a fragile scout at 1) deploys a different unit than one held high (an
    /// elite at 11), so a low Ace is looked up under the <c>A_low</c> key. If a tuning omits that key
    /// the high-Ace unit stands in, so the split is opt-in per data (docs/design/06-dealer-and-enemies.md).
    /// </remarks>
    public static string UnitFor(TuningData tuning, Card card)
    {
        ArgumentNullException.ThrowIfNull(tuning);

        string key = card.Rank == Rank.Ace && !card.AceHigh ? "A_low" : card.Rank.TuningKey();

        if (tuning.DealerCardUnits.TryGetValue(key, out string? enemyId))
        {
            return enemyId;
        }

        if (key == "A_low" && tuning.DealerCardUnits.TryGetValue("A", out string? fallback))
        {
            return fallback;
        }

        throw new TuningValidationException($"dealerCardUnits has no entry for rank '{key}'.");
    }

    /// <summary>
    /// The lane the Dealer's card at <paramref name="cardIndex"/> enters. The upcard is index 0, the
    /// hidden card index 1.
    /// </summary>
    /// <remarks>
    /// <b>Public because the hidden card's destination lane is baseline information.</b> Its rank
    /// stays unknown; its lane does not (docs/design/06-dealer-and-enemies.md § The hidden card's
    /// lane is visible from the start). The player knows "something unknown is coming to lane two",
    /// which is uncertainty that does not prevent intention - and is what gives the junction its job
    /// as a hedge. Exposed here rather than recomputed by a caller so there is one lane rule, not
    /// two that can drift.
    /// </remarks>
    public static int LaneForCard(TuningData tuning, EncounterTuning encounter, int cardIndex)
    {
        ArgumentNullException.ThrowIfNull(tuning);
        ArgumentNullException.ThrowIfNull(encounter);

        if (cardIndex < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(cardIndex), cardIndex, "Dealer cards are indexed from zero.");
        }

        return LaneRule(tuning, encounter, cardIndex);
    }

    private static int LaneRule(TuningData tuning, EncounterTuning encounter, int cardIndex) =>
        tuning.Waves.DealerLaneAssignment switch
        {
            "alternate_from_vanguard" => (encounter.VanguardLane + cardIndex) % tuning.Geometry.Lanes,
            _ => throw new TuningValidationException(
                $"waves.dealerLaneAssignment '{tuning.Waves.DealerLaneAssignment}' has no deployment rule."),
        };
}
