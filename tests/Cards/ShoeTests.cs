using Bastion.Core.Cards;
using Bastion.Core.Config;

namespace Bastion.Core.Tests.Cards;

/// <summary>
/// The 26-card shoe: composition, deterministic seeding, draw-without-replacement, persistence,
/// and the reshuffle-under-8 rule (docs/reference/tuning-constants.md § Rules and thresholds).
/// </summary>
public sealed class ShoeTests
{
    private static readonly TuningData Tuning = TuningLoader.LoadFromRepositoryRoot();

    private static List<Rank> DrainAll(Shoe shoe)
    {
        List<Rank> drawn = [];
        while (shoe.Count > 0)
        {
            (Rank card, shoe) = shoe.Draw();
            drawn.Add(card);
        }

        return drawn;
    }

    [Fact]
    public void A_fresh_shoe_holds_the_tuned_number_of_cards()
    {
        Assert.Equal(Tuning.Rules.ShoeSize, Shoe.Create(Tuning, seed: 1).Count);
    }

    [Fact]
    public void The_shoe_holds_the_tuned_copies_of_every_rank()
    {
        List<Rank> all = DrainAll(Shoe.Create(Tuning, seed: 7));

        Assert.Equal(Tuning.Rules.ShoeSize, all.Count);
        foreach (Rank rank in Enum.GetValues<Rank>())
        {
            Assert.Equal(Tuning.Rules.CopiesPerRank, all.Count(r => r == rank));
        }
    }

    [Fact]
    public void The_same_seed_deals_the_same_order()
    {
        // The scripted battery depends on this - a fixture is only reproducible if the seed is.
        Assert.Equal(DrainAll(Shoe.Create(Tuning, seed: 42)), DrainAll(Shoe.Create(Tuning, seed: 42)));
    }

    [Fact]
    public void Different_seeds_deal_different_orders()
    {
        Assert.NotEqual(DrainAll(Shoe.Create(Tuning, seed: 1)), DrainAll(Shoe.Create(Tuning, seed: 2)));
    }

    [Fact]
    public void Drawing_leaves_the_original_shoe_untouched()
    {
        Shoe shoe = Shoe.Create(Tuning, seed: 3);

        (Rank first, Shoe rest) = shoe.Draw();

        Assert.Equal(Tuning.Rules.ShoeSize, shoe.Count);          // unchanged
        Assert.Equal(Tuning.Rules.ShoeSize - 1, rest.Count);
        Assert.Equal(first, shoe.Draw().Card);                     // the same top card every time
    }

    [Fact]
    public void Draws_do_not_repeat_a_card_beyond_its_copies()
    {
        // Draw-without-replacement: the whole shoe drains to exactly the tuned multiset, no more.
        List<Rank> all = DrainAll(Shoe.Create(Tuning, seed: 9));

        Assert.All(all.GroupBy(r => r), g => Assert.True(g.Count() <= Tuning.Rules.CopiesPerRank));
    }

    [Fact]
    public void A_shoe_above_the_threshold_is_not_reshuffled()
    {
        Shoe shoe = Shoe.Create(Tuning, seed: 5);

        Shoe after = shoe.ReshuffleIfLow(Tuning);

        Assert.Same(shoe, after);
        Assert.Equal(0, after.Generation);
    }

    [Fact]
    public void A_shoe_below_the_threshold_reshuffles_to_a_full_shoe()
    {
        Shoe shoe = Shoe.Create(Tuning, seed: 5);

        // Draw down to just under the reshuffle threshold, as a run of waves would.
        int toDraw = Tuning.Rules.ShoeSize - (Tuning.Rules.ReshuffleBelowCards - 1);
        for (int i = 0; i < toDraw; i++)
        {
            shoe = shoe.Draw().Remaining;
        }

        Assert.True(shoe.Count < Tuning.Rules.ReshuffleBelowCards);

        Shoe reshuffled = shoe.ReshuffleIfLow(Tuning);

        Assert.Equal(Tuning.Rules.ShoeSize, reshuffled.Count);     // discards returned
        Assert.Equal(1, reshuffled.Generation);
    }

    [Fact]
    public void Reshuffling_is_reproducible_across_the_same_seed()
    {
        static Shoe DrawToReshuffle(TuningData tuning)
        {
            Shoe shoe = Shoe.Create(tuning, seed: 11);
            while (shoe.Count >= tuning.Rules.ReshuffleBelowCards)
            {
                shoe = shoe.Draw().Remaining;
            }

            return shoe.ReshuffleIfLow(tuning);
        }

        Assert.Equal(DrainAll(DrawToReshuffle(Tuning)), DrainAll(DrawToReshuffle(Tuning)));
    }

    [Fact]
    public void An_empty_shoe_refuses_to_draw()
    {
        Shoe shoe = Shoe.Create(Tuning, seed: 1);
        for (int i = 0; i < Tuning.Rules.ShoeSize; i++)
        {
            shoe = shoe.Draw().Remaining;
        }

        Assert.Throws<InvalidOperationException>(() => shoe.Draw());
    }
}
