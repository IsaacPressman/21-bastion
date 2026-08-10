using System.Globalization;
using System.Text;
using Bastion.Core.Board;
using Bastion.Core.Cards;
using Bastion.Core.Config;
using Bastion.Core.Hand;
using Bastion.Core.March;

namespace Bastion.Core.Tests.Regression;

/// <summary>
/// Regression procedure 3: 10,000 hands each for the baseline, face-heavy, and many-card shoes.
/// </summary>
/// <remarks>
/// <para>
/// Reports <b>output, bust rate, board width, run frequency, and final entry position</b>
/// (docs/prototype/VALIDATION.md § Regression). Written to
/// <c>telemetry/shoe-simulation.csv</c> for reading, and asserted only where a number moving would
/// mean the simulation had stopped simulating - the figures themselves are first-pass.
/// </para>
/// <para>
/// The three shoes exist because they load different archetypes. Face-heavy pushes toward short,
/// powerful, easily-busted hands; many-card makes long hands reachable, which is the archetype the
/// <b>three arms exist to disambiguate</b> (§ The secondary measurement) - if it is unviable in
/// every arm, links and board width are insufficient alone and the archetype needs a mechanism
/// designed against a measured deficit rather than guessed at.
/// </para>
/// <para>
/// The player policy is a fixed <b>stand on 17</b>, stated rather than optimised. This procedure
/// measures the <i>shoe</i>, so the policy only has to be identical across the three - an optimal
/// policy would differ per shoe and turn a shoe comparison into a policy comparison.
/// </para>
/// </remarks>
public sealed class ShoeSimulation
{
    private const int Hands = 10_000;
    private const int StandOn = 17;
    private const int Seed = 615243;

    private static readonly TuningData Tuning = TuningLoader.LoadFromRepositoryRoot();

    [Fact]
    [Trait(Regression.Trait, Regression.Category)]
    public void Ten_thousand_hands_across_every_shoe_preset()
    {
        StringBuilder csv = new();
        csv.AppendLine("# Regression procedure 3. Ten thousand hands per shoe, player stands on 17.");
        csv.AppendLine($"# {Hands} hands, seed {Seed}. Deterministic: the same seed gives the same table.");
        csv.AppendLine("shoe,hands,meanRawOutput,bustRate,meanBoardWidth,runFrequency,meanFinalEntry,meanCardCount");

        List<Summary> summaries = [];

        foreach (string preset in Tuning.ShoePresets.Keys.Order(StringComparer.Ordinal))
        {
            Summary summary = Simulate(preset);
            summaries.Add(summary);

            csv.AppendLine(CultureInfo.InvariantCulture,
                $"{summary.Shoe},{Hands},{summary.MeanRawOutput:F3},{summary.BustRate:F4}," +
                $"{summary.MeanBoardWidth:F3},{summary.RunFrequency:F4},{summary.MeanFinalEntry:F3},{summary.MeanCardCount:F3}");
        }

        Measurement.Sweeps.Write("shoe-simulation.csv", csv.ToString());

        Assert.Equal(Tuning.ShoePresets.Count, summaries.Count);

        // Sanity, not balance. Each of these failing would mean the simulation had stopped
        // simulating rather than that a number had moved - the numbers are expected to move.
        Assert.All(summaries, s =>
        {
            Assert.InRange(s.BustRate, 0.0, 1.0);
            Assert.InRange(s.MeanBoardWidth, 1.0, Tuning.Geometry.TotalSockets);
            Assert.InRange(s.MeanFinalEntry, Tuning.March.EntryClampMin, Tuning.March.EntryClampMax);
            Assert.True(s.MeanRawOutput > 0.0);
        });

        // The presets have to actually differ, or three runs are measuring one shoe. Face-heavy
        // busts more than many-card - that is what the two compositions are for.
        Summary faceHeavy = summaries.Single(s => s.Shoe == "faceHeavy");
        Summary manyCard = summaries.Single(s => s.Shoe == "manyCard");

        Assert.True(faceHeavy.BustRate > manyCard.BustRate,
            $"faceHeavy should bust more than manyCard; got {faceHeavy.BustRate:F3} against {manyCard.BustRate:F3}.");

        Assert.True(manyCard.MeanCardCount > faceHeavy.MeanCardCount,
            $"manyCard should reach longer hands; got {manyCard.MeanCardCount:F2} against {faceHeavy.MeanCardCount:F2}.");
    }

    [Fact]
    [Trait(Regression.Trait, Regression.Category)]
    public void The_simulation_is_reproducible_from_its_seed()
    {
        // Steps 1-3 are only worth running if a difference between two runs means a change to the
        // game rather than a change to the weather.
        Assert.Equal(Simulate("baseline"), Simulate("baseline"));
    }

    private sealed record Summary(
        string Shoe,
        double MeanRawOutput,
        double BustRate,
        double MeanBoardWidth,
        double RunFrequency,
        double MeanFinalEntry,
        double MeanCardCount);

    /// <summary>
    /// Deals and plays <see cref="Hands"/> hands from one shoe preset.
    /// </summary>
    /// <remarks>
    /// Placement fills sockets forward-first and wraps at capacity, which is a policy rather than
    /// good play - see the type remarks. Run frequency counts hands that formed <i>any</i> run,
    /// which is the figure with a pre-committed reading attached: too rare to shape placement
    /// triggers Add-Back 3.
    /// </remarks>
    private static Summary Simulate(string preset)
    {
        IReadOnlyList<SocketRef> sockets = Measurement.Sweeps.AllSockets(Tuning);

        double output = 0.0;
        int busts = 0;
        int width = 0;
        int withRuns = 0;
        double entryTotal = 0.0;
        int cards = 0;

        Shoe shoe = Shoe.Create(Tuning, Seed, preset);

        for (int i = 0; i < Hands; i++)
        {
            // A hand needs at most a handful of cards; reshuffling at the hand boundary matches how
            // the wave loop uses the shoe, and keeps the composition fixed for the whole run.
            shoe = shoe.ReshuffleIfLow(Tuning);

            (HandState hand, shoe) = DealAndPlay(shoe);

            cards += hand.CardCount;

            if (hand.IsBust)
            {
                busts++;
            }

            double multiplier = hand.IsBust
                ? Tuning.FormationStrength.Bust
                : hand.FormationMultiplier(Tuning);

            // A busting card is destroyed and never placed, so the board is one short of the hand.
            int placeable = Math.Min(hand.IsBust ? hand.CardCount - 1 : hand.CardCount, sockets.Count);

            (Card Card, SocketRef Socket)[] placed =
                [.. Resolved(hand).Take(placeable).Select((card, index) => (card, sockets[index]))];

            IReadOnlyDictionary<SocketRef, double> runBonus = RunLinks.BonusBySocket(Tuning, placed);

            output += placed.Sum(p => Tuning.CardPower.ForValue(p.Card.Value)) * multiplier;
            width += placed.Length;
            entryTotal += MarchClock.EntryAfter(Tuning, hand.CardCount, hand.IsExactly21);

            if (runBonus.Values.Any(bonus => bonus > 0.0))
            {
                withRuns++;
            }
        }

        return new Summary(
            preset,
            output / Hands,
            (double)busts / Hands,
            (double)width / Hands,
            (double)withRuns / Hands,
            entryTotal / Hands,
            (double)cards / Hands);
    }

    /// <summary>Deals two and hits to <see cref="StandOn"/>, stopping on a bust.</summary>
    private static (HandState Hand, Shoe Shoe) DealAndPlay(Shoe shoe)
    {
        (Rank first, shoe) = shoe.Draw();
        (Rank second, shoe) = shoe.Draw();

        HandState hand = HandState.Opening(first, second);

        while (hand.Total < StandOn && !hand.IsBust)
        {
            if (shoe.Count == 0)
            {
                break;
            }

            (Rank next, shoe) = shoe.Draw();
            hand = hand.Hit(next);
        }

        return (hand, shoe);
    }

    private static Card[] Resolved(HandState hand)
    {
        int high = hand.AceHighCount;
        int seen = 0;

        return [.. hand.Cards.Select(r => r == Rank.Ace ? new Card(Rank.Ace, seen++ < high) : new Card(r))];
    }
}
