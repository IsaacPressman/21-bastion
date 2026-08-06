using Bastion.Core.Config;
using Bastion.Core.March;

namespace Bastion.Core.Tests.March;

/// <summary>
/// The published march curve, the entry clamp, and the exactly-21 pullback.
/// </summary>
public sealed class MarchClockTests
{
    private static readonly TuningData Tuning = TuningLoader.LoadFromRepositoryRoot();

    [Theory]
    [InlineData(2, 0.0)]   // the opening two cards are free
    [InlineData(3, 1.5)]
    [InlineData(4, 4.0)]
    [InlineData(5, 7.5)]
    public void Entry_matches_the_published_arm_C_curve(int cardCount, double expected)
    {
        Assert.Equal(expected, MarchClock.EntryAfter(Tuning, cardCount, reachedExactly21: false), precision: 6);
    }

    [Theory]
    [InlineData(3, 0.0)]   // 3-card 21 - fully back to the start
    [InlineData(4, 1.0)]
    [InlineData(5, 4.5)]
    public void Exactly_21_pulls_the_army_back_three_units(int cardCount, double expected)
    {
        Assert.Equal(expected, MarchClock.EntryAfter(Tuning, cardCount, reachedExactly21: true), precision: 6);
    }

    [Fact]
    public void Entry_clamps_at_the_rear_socket()
    {
        // Unclamped, repeating +3.5 would put a 6-card hand at 11.0 and a 7-card hand past the
        // end of the 12.0 path - enemies spawning at the Bastion for a guaranteed full leak.
        Assert.Equal(9.0, Tuning.March.EntryClampMax);

        foreach (int cardCount in (int[])[6, 7, 8, 12])
        {
            Assert.Equal(9.0, MarchClock.EntryAfter(Tuning, cardCount, reachedExactly21: false), precision: 6);
        }
    }

    [Fact]
    public void The_clamp_applies_before_the_pullback_so_a_six_card_21_still_recovers()
    {
        // Clamped to 9.0, then 3.0 back. Stranding it at the clamp would kill the rescue exactly
        // where the player most needs one.
        Assert.Equal(6.0, MarchClock.EntryAfter(Tuning, 6, reachedExactly21: true), precision: 6);
    }

    [Fact]
    public void Entry_never_leaves_the_path()
    {
        foreach (int cardCount in Enumerable.Range(0, 15))
        {
            foreach (bool is21 in (bool[])[false, true])
            {
                double entry = MarchClock.EntryAfter(Tuning, cardCount, is21);

                Assert.InRange(entry, Tuning.March.EntryClampMin, Tuning.March.EntryClampMax);
            }
        }
    }

    [Theory]
    [InlineData(2, 1.5)]   // third card
    [InlineData(3, 2.5)]   // fourth
    [InlineData(4, 3.5)]   // fifth
    [InlineData(5, 1.5)]   // sixth: 7.5 -> clamped 9.0, so only 1.5 of the 3.5 lands
    [InlineData(6, 0.0)]   // seventh onward: free on the clock
    [InlineData(7, 0.0)]
    public void Next_step_cost_reflects_the_clamp(int cardsHeld, double expected)
    {
        Assert.Equal(expected, MarchClock.NextStepCost(Tuning, cardsHeld), precision: 6);
    }

    [Fact]
    public void Cards_past_the_clamp_are_free_on_the_clock()
    {
        // Accepted, not engineered around: at six-plus cards the bust probability is enormous and
        // every further card forces a replacement of one of the player's own towers.
        Assert.Equal(0.0, MarchClock.NextStepCost(Tuning, 7), precision: 6);
    }

    [Theory]
    [InlineData("A", 6, 4.0)]   // flat 1.0 - far from the clamp
    [InlineData("B", 6, 6.5)]   // soft: 1.0 + 1.5 + 2.0 + 2.0
    [InlineData("B", 8, 9.0)]   // soft reaches the clamp eventually
    [InlineData("C", 6, 9.0)]   // hard clamps immediately at six
    public void Every_arm_respects_the_clamp(string arm, int cardCount, double expected)
    {
        TuningData armTuning = Tuning with { March = Tuning.March with { ActivePreset = arm } };

        Assert.Equal(expected, MarchClock.EntryAfter(armTuning, cardCount, reachedExactly21: false), precision: 6);
    }
}
