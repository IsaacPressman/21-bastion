using System.Globalization;
using System.Text;
using Bastion.Core.Board;
using Bastion.Core.Cards;
using Bastion.Core.Config;
using Bastion.Core.Diagnostics;
using Bastion.Core.Hand;
using Bastion.Core.March;
using Bastion.Core.Resolve;
using Bastion.Core.Tests.Resolve;

namespace Bastion.Core.Tests.Measurement;

/// <summary>
/// How often is a safe fifth card nonetheless the better play?
/// </summary>
/// <remarks>
/// <para>
/// <b>The primary measurement of the whole validation exercise</b> — "the primary measurement is the
/// shape of the fifth-card outcome, not aggregate output" (docs/prototype/VALIDATION.md § The
/// primary measurement). It reports, for each arm, how often a <b>safe miss</b> beats the
/// <b>stand-at-four counterfactual</b> by resolver output.
/// </para>
/// <para>
/// A safe miss is a fifth card that neither busts nor reaches 21. Both other branches are already
/// understood: reaching 21 pulls the army back and is a rescue, busting costs the card and x0.80.
/// The open question is the middle - whether a fifth tower is worth the march step that bought it -
/// and Arm C is expected to make it <b>functionally dead</b>. If it does,
/// <b>Arm B is the design</b>; the design is not defending Arm C, it is testing whether Arm C is too
/// sharp.
/// </para>
/// <para>
/// Output is <b>predicted leak, so lower is better</b>, taken from the resolver rather than from
/// board power times an engagement fraction - that estimate was withdrawn and this measurement is
/// exactly where it would be most tempting to reach for it.
/// </para>
/// <para>
/// Gated behind <see cref="DebugGate"/> and slow by design. Run it with
/// <c>dotnet test -p:BastionInstrumentation=true</c>.
/// </para>
/// </remarks>
public sealed class FifthCardOutcomeSweep
{
    /// <summary>
    /// Four-card totals worth asking the question about.
    /// </summary>
    /// <remarks>
    /// The design's narrowed claim is that hit/stand is live in the 14-19 band, "roughly where
    /// blackjack has always put it". The band is widened by two at each end here so the measurement
    /// can show the edges going quiet rather than asserting where they are - but not to 4, because
    /// the design explicitly declines to manufacture tension at 8.
    /// </remarks>
    private const int LowestTotal = 12;
    private const int HighestTotal = 20;

    [Fact]
    public void Sweep_the_fifth_card_against_standing_at_four()
    {
        // Copied to a local so the branch is not folded away as unreachable at compile time.
        bool instrumented = DebugGate.IsEnabled;

        if (!instrumented)
        {
            return;
        }

        TuningData baseTuning = Fixture.Tuning;
        EncounterTuning encounter = baseTuning.Encounter("example_wave");
        Card[] dealerHand = [new Card(Rank.Ten), new Card(Rank.Six), new Card(Rank.Seven)];

        StringBuilder csv = new();
        csv.AppendLine("# Does a safe fifth card beat standing at four? Output is predicted leak - lower is better.");
        csv.AppendLine("# A safe miss neither busts nor reaches 21. Placement is optimised over socket choices on BOTH sides.");
        csv.AppendLine("arm,fourCardHand,fourCardTotal,fifthRank,fiveCardTotal,standLeak,hitLeak,delta,verdict");

        List<Row> rows = [];

        foreach (string arm in baseTuning.MarchPresets.Keys.Order(StringComparer.Ordinal))
        {
            TuningData tuning = baseTuning with { March = baseTuning.March with { ActivePreset = arm } };

            double entryAtFour = MarchClock.EntryAfter(tuning, 4, reachedExactly21: false);
            double entryAtFive = MarchClock.EntryAfter(tuning, 5, reachedExactly21: false);

            foreach (IReadOnlyList<Rank> four in FourCardHands(tuning))
            {
                HandState hand = four.Aggregate(HandState.Empty, (h, r) => h.Hit(r));

                // The counterfactual is the same for every fifth card, so it is resolved once.
                double standLeak = BestLeak(tuning, encounter, dealerHand, four, hand, entryAtFour);

                foreach (Rank fifth in SafeMisses(tuning, hand))
                {
                    HandState after = hand.Hit(fifth);
                    IReadOnlyList<Rank> five = [.. four, fifth];

                    double hitLeak = BestLeak(tuning, encounter, dealerHand, five, after, entryAtFive);
                    double delta = hitLeak - standLeak;

                    csv.AppendLine(CultureInfo.InvariantCulture,
                        $"{arm},{Describe(four)},{hand.Total},{fifth.TuningKey()},{after.Total}," +
                        $"{standLeak:F1},{hitLeak:F1},{delta:F1},{Verdict(delta)}");

                    rows.Add(new Row(arm, hand.Total, delta));
                }
            }
        }

        AppendSummary(csv, rows);
        Sweeps.Write("fifth-card.csv", csv.ToString());

        // A measurement, so it asserts only that it produced something to read. The reading is a
        // pre-committed judgement in VALIDATION.md, not a pass/fail condition, and wiring a
        // threshold in here would be renegotiating it in code.
        Assert.NotEmpty(rows);
    }

    private sealed record Row(string Arm, int FourCardTotal, double Delta);

    /// <summary>Hitting leaked less, more, or exactly the same.</summary>
    private static string Verdict(double delta) =>
        delta < 0 ? "HIT BETTER" : delta > 0 ? "stand better" : "no difference";

    /// <summary>
    /// The best leak this hand can reach, over every choice of sockets.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Optimised on both sides so the comparison is best-play against best-play. A fixed placement
    /// policy would have measured the policy: any rule that favours depth flatters the five-card
    /// board, which has more towers to put there.
    /// </para>
    /// <para>
    /// Within a chosen set of sockets the cards are assigned <b>most powerful to deepest</b>, held
    /// constant on both sides rather than searched. Searching assignments as well would multiply the
    /// run count by 4! and 5!, and the assignment rule is not what the arms differ in - the entry
    /// point is.
    /// </para>
    /// </remarks>
    private static double BestLeak(
        TuningData tuning,
        EncounterTuning encounter,
        Card[] dealerHand,
        IReadOnlyList<Rank> ranks,
        HandState hand,
        double entry)
    {
        double multiplier = hand.FormationMultiplier(tuning);
        Card[] cards = Resolved(hand);
        IReadOnlyList<SocketRef> sockets = Sweeps.AllSockets(tuning);

        double best = double.MaxValue;

        foreach (SocketRef[] choice in Sweeps.Combinations(sockets, ranks.Count))
        {
            // Strongest card at the deepest socket, consistently, on both sides of the comparison.
            SocketRef[] byDepth = [.. choice.OrderByDescending(s => Sweeps.DepthOf(tuning, s))];
            Card[] byPower = [.. cards.OrderByDescending(c => tuning.CardPower.ForValue(c.Value))];

            (Card Card, SocketRef Socket)[] placed =
                [.. byPower.Select((card, i) => (card, byDepth[i]))];

            IReadOnlyDictionary<SocketRef, double> runBonus = RunLinks.BonusBySocket(tuning, placed);

            BoardState board = BoardState.Create(
                tuning,
                placed.Select(p => TowerState.Place(
                    tuning, p.Card, Family.Club, p.Socket, multiplier,
                    runBonus.GetValueOrDefault(p.Socket, 0.0))),
                entry);

            FinalForecast forecast = Resolver.ResolveComplete(
                tuning, encounter, board,
                ArmyBuilder.Complete(tuning, encounter, dealerHand, entry));

            best = Math.Min(best, forecast.Lanes.Sum(lane => lane.PredictedDamage));
        }

        return best;
    }

    /// <summary>
    /// The hand's cards with each Ace's high-or-low state resolved.
    /// </summary>
    /// <remarks>
    /// An Ace's state is a property of the hand, not the card, and it changes what the tower is -
    /// which is the point of the immediate battlefield transformation. Reading it off the hand keeps
    /// the board honest about a fifth card that forced an 11 down to a 1.
    /// </remarks>
    private static Card[] Resolved(HandState hand)
    {
        int high = hand.AceHighCount;
        int acesSeen = 0;

        List<Card> cards = [];

        foreach (Rank rank in hand.Cards)
        {
            cards.Add(rank == Rank.Ace ? new Card(Rank.Ace, acesSeen++ < high) : new Card(rank));
        }

        return [.. cards];
    }

    /// <summary>Ranks that neither bust the hand nor land it on 21.</summary>
    private static IEnumerable<Rank> SafeMisses(TuningData tuning, HandState hand)
    {
        foreach (Rank rank in Enum.GetValues<Rank>())
        {
            // Honour the shoe: a fifth card must still exist to be drawn.
            if (hand.Cards.Count(r => r == rank) >= tuning.Rules.CopiesPerRank)
            {
                continue;
            }

            HandState after = hand.Hit(rank);

            if (!after.IsBust && after.Total != 21)
            {
                yield return rank;
            }
        }
    }

    /// <summary>Every legal four-card hand in the band, honouring the shoe's copy limit.</summary>
    private static IEnumerable<IReadOnlyList<Rank>> FourCardHands(TuningData tuning)
    {
        Rank[] ranks = Enum.GetValues<Rank>();
        int copies = tuning.Rules.CopiesPerRank;

        foreach (int a in Enumerable.Range(0, ranks.Length))
        {
            foreach (int b in Enumerable.Range(a, ranks.Length - a))
            {
                foreach (int c in Enumerable.Range(b, ranks.Length - b))
                {
                    foreach (int d in Enumerable.Range(c, ranks.Length - c))
                    {
                        Rank[] hand = [ranks[a], ranks[b], ranks[c], ranks[d]];

                        if (hand.GroupBy(r => r).Any(g => g.Count() > copies))
                        {
                            continue;
                        }

                        HandState state = hand.Aggregate(HandState.Empty, (h, r) => h.Hit(r));

                        if (!state.IsBust && state.Total >= LowestTotal && state.Total <= HighestTotal)
                        {
                            yield return hand;
                        }
                    }
                }
            }
        }
    }

    private static string Describe(IEnumerable<Rank> ranks) =>
        string.Join("+", ranks.Select(r => r.TuningKey()));

    /// <summary>
    /// Per arm, and then per four-card total: how often the fifth card was worth taking.
    /// </summary>
    /// <remarks>
    /// The per-total breakdown is what shows the <i>shape</i> the measurement is named for. An arm
    /// where the fifth card is uniformly bad reads differently from one where it is worth taking at
    /// 14 and never at 18, and only the second is a live decision band.
    /// </remarks>
    private static void AppendSummary(StringBuilder csv, IReadOnlyList<Row> rows)
    {
        csv.AppendLine();
        csv.AppendLine("# THE PRIMARY MEASUREMENT: how often a safe fifth card beat standing at four.");
        csv.AppendLine("# Arm C is expected to be near-zero - 'functionally dead on a safe miss'.");
        csv.AppendLine("# If it is, prototype/VALIDATION.md says Arm B is the design.");
        csv.AppendLine("arm,cases,hitBetter,noDifference,standBetter,hitBetterFraction,meanLeakDelta");

        foreach (IGrouping<string, Row> arm in rows.GroupBy(r => r.Arm, StringComparer.Ordinal)
                     .OrderBy(g => g.Key, StringComparer.Ordinal))
        {
            Row[] all = [.. arm];

            csv.AppendLine(CultureInfo.InvariantCulture,
                $"{arm.Key},{all.Length},{all.Count(r => r.Delta < 0)},{all.Count(r => r.Delta == 0)}," +
                $"{all.Count(r => r.Delta > 0)},{(double)all.Count(r => r.Delta < 0) / all.Length:F3}," +
                $"{all.Average(r => r.Delta):F3}");
        }

        csv.AppendLine();
        csv.AppendLine("# The same, by four-card total. This is the SHAPE - a live band looks different from a dead one.");
        csv.AppendLine("arm,fourCardTotal,cases,hitBetter,hitBetterFraction,meanLeakDelta");

        foreach (IGrouping<(string Arm, int FourCardTotal), Row> group in rows
                     .GroupBy(r => (r.Arm, r.FourCardTotal))
                     .OrderBy(g => g.Key.Arm, StringComparer.Ordinal)
                     .ThenBy(g => g.Key.FourCardTotal))
        {
            Row[] all = [.. group];

            csv.AppendLine(CultureInfo.InvariantCulture,
                $"{group.Key.Arm},{group.Key.FourCardTotal},{all.Length},{all.Count(r => r.Delta < 0)}," +
                $"{(double)all.Count(r => r.Delta < 0) / all.Length:F3},{all.Average(r => r.Delta):F3}");
        }
    }
}
