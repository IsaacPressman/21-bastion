using Bastion.Core.Cards;
using Bastion.Core.Config;

namespace Bastion.Core.Tests.Cards;

/// <summary>
/// <see cref="Shoe.RemainingComposition"/>: the per-rank pile the rank-composition display reads.
/// </summary>
/// <remarks>
/// The information rules show what is left in the pile but never the draw order
/// (docs/design/09-information-and-ui.md). These tests pin the counts and, by only ever exposing
/// counts, confirm there is no order to leak.
/// </remarks>
public sealed class ShoeCompositionTests
{
    private static readonly TuningData Tuning = TuningLoader.LoadFromRepositoryRoot();

    [Fact]
    public void A_fresh_shoe_holds_the_configured_copies_of_every_rank()
    {
        IReadOnlyDictionary<Rank, int> composition = Shoe.Create(Tuning, seed: 1).RemainingComposition();

        Assert.All(Enum.GetValues<Rank>(), rank =>
            Assert.Equal(Tuning.Rules.CopiesPerRank, composition[rank]));
    }

    [Fact]
    public void Every_rank_is_present_as_a_key_even_when_drawn_to_zero()
    {
        Shoe shoe = Shoe.Create(Tuning, seed: 2);

        // Drain the whole shoe; a rank at zero must read as an explicit zero, never a missing key.
        while (shoe.Count > 0)
        {
            (_, shoe) = shoe.Draw();
        }

        IReadOnlyDictionary<Rank, int> composition = shoe.RemainingComposition();

        Assert.Equal(Enum.GetValues<Rank>().Length, composition.Count);
        Assert.All(Enum.GetValues<Rank>(), rank => Assert.Equal(0, composition[rank]));
    }

    [Fact]
    public void The_counts_always_sum_to_the_card_count()
    {
        Shoe shoe = Shoe.Create(Tuning, seed: 3);

        for (int drawn = 0; drawn < 10; drawn++)
        {
            IReadOnlyDictionary<Rank, int> composition = shoe.RemainingComposition();
            Assert.Equal(shoe.Count, composition.Values.Sum());

            (_, shoe) = shoe.Draw();
        }
    }

    [Fact]
    public void Drawing_a_rank_decrements_exactly_that_rank()
    {
        Shoe shoe = Shoe.Create(Tuning, seed: 4);
        IReadOnlyDictionary<Rank, int> before = shoe.RemainingComposition();

        (Rank drawn, Shoe rest) = shoe.Draw();
        IReadOnlyDictionary<Rank, int> after = rest.RemainingComposition();

        Assert.Equal(before[drawn] - 1, after[drawn]);
        Assert.All(Enum.GetValues<Rank>().Where(r => r != drawn),
            rank => Assert.Equal(before[rank], after[rank]));
    }
}
