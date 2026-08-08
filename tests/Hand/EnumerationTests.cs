using Bastion.Core.Cards;
using Bastion.Core.Config;
using Bastion.Core.Hand;
using Bastion.Core.March;

namespace Bastion.Core.Tests.Hand;

/// <summary>
/// The enumeration regression gate (docs/ARCHITECTURE.md § Testing, docs/prototype/VALIDATION.md
/// § Regression step 2): every legal non-bust two-to-five-card hand, recorded by <b>raw output and
/// entry position</b> - never a derived engagement-adjusted output.
/// </summary>
/// <remarks>
/// "Legal" honours the shoe: a rank appears at most <c>copiesPerRank</c> times. This is the set the
/// benchmark re-runs before any change to the power curve, Formation Strength, or the march curve;
/// here it stands as a determinism-and-sanity gate over the whole space the producer must cover.
/// </remarks>
public sealed class EnumerationTests
{
    private static readonly TuningData Tuning = TuningLoader.LoadFromRepositoryRoot();

    /// <summary>Raw output: base power summed over resolved card values, times the Formation multiplier.</summary>
    private static double RawOutput(HandState hand)
    {
        int high = hand.AceHighCount;
        int acesSeen = 0;
        double sum = 0.0;

        foreach (Rank rank in hand.Cards)
        {
            int value = rank == Rank.Ace ? (acesSeen++ < high ? 11 : 1) : rank.LowValue();
            sum += Tuning.CardPower.ForValue(value);
        }

        return sum * hand.FormationMultiplier(Tuning);
    }

    private static Dictionary<string, (double Raw, double Entry)> Enumerate()
    {
        Dictionary<string, (double, double)> records = [];

        foreach (IReadOnlyList<Rank> ranks in LegalHands(minSize: 2, maxSize: 5, copies: Tuning.Rules.CopiesPerRank))
        {
            HandState hand = ranks.Aggregate(HandState.Empty, (h, r) => h.Hit(r));
            if (hand.IsBust)
            {
                continue;   // a busting card is destroyed and never placed; the bust board is tested elsewhere
            }

            string key = string.Join(",", ranks.Select(r => (int)r));
            records[key] = (RawOutput(hand), MarchClock.EntryAfter(Tuning, hand.CardCount, hand.IsExactly21));
        }

        return records;
    }

    /// <summary>All rank multisets of a size in range, each rank capped at the shoe's copy count.</summary>
    private static List<IReadOnlyList<Rank>> LegalHands(int minSize, int maxSize, int copies)
    {
        Rank[] ranks = Enum.GetValues<Rank>();
        List<IReadOnlyList<Rank>> hands = [];
        List<Rank> acc = [];

        void Dfs(int rankIndex)
        {
            if (acc.Count > maxSize)
            {
                return;
            }

            if (rankIndex == ranks.Length)
            {
                if (acc.Count >= minSize)
                {
                    hands.Add([.. acc]);
                }

                return;
            }

            for (int count = 0; count <= copies; count++)
            {
                for (int c = 0; c < count; c++)
                {
                    acc.Add(ranks[rankIndex]);
                }

                Dfs(rankIndex + 1);

                acc.RemoveRange(acc.Count - count, count);
            }
        }

        Dfs(0);
        return hands;
    }

    [Fact]
    public void Every_legal_non_bust_hand_has_a_sane_raw_output_and_entry()
    {
        Dictionary<string, (double Raw, double Entry)> records = Enumerate();

        Assert.True(records.Count > 1000, $"expected the full 2-5 card space, got {records.Count}");

        foreach ((double raw, double entry) in records.Values)
        {
            Assert.True(raw > 0 && double.IsFinite(raw), $"raw output {raw} is not a positive finite number");
            Assert.InRange(entry, Tuning.March.EntryClampMin, Tuning.March.EntryClampMax);
        }
    }

    [Fact]
    public void The_enumeration_is_deterministic()
    {
        // Byte-identical output for identical input is the core's central promise (ARCHITECTURE.md).
        Assert.Equal(Enumerate(), Enumerate());
    }

    [Theory]
    [InlineData(9.775, 0.0, Rank.Ten, Rank.Six)]                                        // 16
    [InlineData(12.19, 4.0, Rank.Three, Rank.Three, Rank.Five, Rank.Five)]              // 16, deeper
    [InlineData(15.00, 0.0, Rank.King, Rank.Queen)]                                     // 20
    [InlineData(21.44, 4.5, Rank.Two, Rank.Three, Rank.Four, Rank.Five, Rank.Seven)]    // 5-card 21
    public void Landmark_hands_appear_with_their_raw_output_and_entry(double raw, double entry, params Rank[] ranks)
    {
        Dictionary<string, (double Raw, double Entry)> records = Enumerate();
        // Enumeration keys are rank-sorted (the DFS walks ranks ascending), so match that here.
        string key = string.Join(",", ranks.OrderBy(r => (int)r).Select(r => (int)r));

        Assert.True(records.TryGetValue(key, out (double Raw, double Entry) record), $"hand {key} was not enumerated");
        Assert.Equal(raw, record.Raw, precision: 6);
        Assert.Equal(entry, record.Entry, precision: 6);
    }
}
