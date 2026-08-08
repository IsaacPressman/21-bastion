using Bastion.Core.Cards;
using Bastion.Core.Config;
using Bastion.Core.Hand;

namespace Bastion.Core.Tests.Hand;

/// <summary>
/// Blackjack totals, hard/soft handling, the Ace 11-to-1 transformation, natural detection, and the
/// Formation Strength lookup (docs/design/02-blackjack-and-formation.md §§ Blackjack, Formation).
/// </summary>
public sealed class HandStateTests
{
    private static readonly TuningData Tuning = TuningLoader.LoadFromRepositoryRoot();

    private static HandState Hand(params Rank[] ranks) =>
        ranks.Aggregate(HandState.Empty, (hand, rank) => hand.Hit(rank));

    [Fact]
    public void A_number_hand_totals_hard()
    {
        HandState hand = Hand(Rank.Six, Rank.Eight);

        Assert.Equal(14, hand.Total);
        Assert.False(hand.IsSoft);
        Assert.False(hand.IsBust);
    }

    [Fact]
    public void An_ace_is_held_high_when_it_fits()
    {
        HandState hand = Hand(Rank.Ace, Rank.Nine);

        Assert.Equal(20, hand.Total);
        Assert.True(hand.IsSoft);
        Assert.Equal(1, hand.AceHighCount);
    }

    [Fact]
    public void A_hit_that_would_bust_drops_the_ace_to_one()
    {
        // The transformation the design cares about: soft 20 becomes hard 15, and any Ace tower
        // placed for it flips from the 5.4 anchor to the 1.0 utility on the next recomputation.
        HandState soft = Hand(Rank.Ace, Rank.Nine);
        HandState afterHit = soft.Hit(Rank.Five);

        Assert.Equal(15, afterHit.Total);
        Assert.False(afterHit.IsSoft);
        Assert.Equal(0, afterHit.AceHighCount);
    }

    [Fact]
    public void Two_aces_hold_exactly_one_high()
    {
        HandState hand = Hand(Rank.Ace, Rank.Ace);

        Assert.Equal(12, hand.Total);
        Assert.Equal(1, hand.AceHighCount);
        Assert.True(hand.IsSoft);
    }

    [Fact]
    public void Two_aces_and_a_nine_reach_a_soft_twenty_one()
    {
        HandState hand = Hand(Rank.Ace, Rank.Ace, Rank.Nine);

        Assert.Equal(21, hand.Total);
        Assert.Equal(1, hand.AceHighCount);
        Assert.True(hand.IsExactly21);
    }

    [Theory]
    [InlineData(Rank.King)]
    [InlineData(Rank.Ten)]
    [InlineData(Rank.Queen)]
    public void An_ace_beside_a_ten_value_card_is_a_natural(Rank tenValue)
    {
        HandState hand = Hand(Rank.Ace, tenValue);

        Assert.Equal(21, hand.Total);
        Assert.True(hand.IsExactly21);
        Assert.True(hand.IsNaturalBlackjack);
    }

    [Fact]
    public void A_three_card_twenty_one_is_not_a_natural()
    {
        HandState hand = Hand(Rank.Seven, Rank.Six, Rank.Eight);

        Assert.True(hand.IsExactly21);
        Assert.False(hand.IsNaturalBlackjack);
    }

    [Fact]
    public void A_five_card_twenty_one_still_counts()
    {
        // The signature long hand: 2+3+4+5+7. Exactly 21, so the pullback fires.
        HandState hand = Hand(Rank.Two, Rank.Three, Rank.Four, Rank.Five, Rank.Seven);

        Assert.Equal(21, hand.Total);
        Assert.True(hand.IsExactly21);
        Assert.Equal(5, hand.CardCount);
    }

    [Fact]
    public void A_hard_hand_over_twenty_one_busts()
    {
        HandState hand = Hand(Rank.Ten, Rank.Eight, Rank.Five);

        Assert.True(hand.IsBust);
        Assert.Equal(23, hand.Total);
        Assert.False(hand.IsExactly21);
    }

    [Theory]
    [InlineData(1.60, Rank.Ace, Rank.King)]                 // 21
    [InlineData(1.05, Rank.Six, Rank.Eight)]                // 14
    [InlineData(0.90, Rank.Five, Rank.Five)]                // 10, eleven-or-below
    public void Formation_multiplier_indexes_the_curve(double expected, params Rank[] ranks)
    {
        Assert.Equal(expected, Hand(ranks).FormationMultiplier(Tuning), precision: 6);
    }

    [Fact]
    public void A_bust_takes_the_bust_multiplier_whatever_the_sum()
    {
        HandState hand = Hand(Rank.Ten, Rank.Eight, Rank.Five);

        Assert.Equal(Tuning.FormationStrength.Bust, hand.FormationMultiplier(Tuning), precision: 6);
    }
}
