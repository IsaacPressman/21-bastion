using System.Globalization;
using System.Text;
using Bastion.Core.Board;
using Bastion.Core.Cards;
using Bastion.Core.Config;
using Bastion.Core.Hand;
using Bastion.Core.March;
using Bastion.Core.Resolve;
using Bastion.Core.Tests.Resolve;

namespace Bastion.Core.Tests.Regression;

/// <summary>
/// Regression procedure 1: re-run the benchmark hand set and <b>flag sign changes</b>.
/// </summary>
/// <remarks>
/// <para>
/// A small, named set of hands spanning the decisions the design turns on - the live 14-19 band, the
/// two ways to reach 16, the soft/hard 17 pair, a natural, and the long hands. Each is resolved
/// against a fixed board and compared with a committed baseline.
/// </para>
/// <para>
/// <b>A sign change fails; a magnitude change reports.</b> That asymmetry is the whole point of the
/// procedure. Every number in this design is first-pass and expected to move, so a suite that failed
/// on any drift would be re-baselined until it was ignored. A sign change is different in kind: it
/// means a hand that used to defend a lane no longer does, or a comparison between two hands has
/// reversed - which is a claim about the design breaking, not a number shifting.
/// </para>
/// </remarks>
public sealed class BenchmarkHandSet
{
    private const string BaselineFile = "benchmark-hands.csv";

    private static readonly TuningData Tuning = TuningLoader.LoadFromRepositoryRoot();

    /// <summary>
    /// The hands worth watching, and why each is on the list.
    /// </summary>
    /// <remarks>
    /// Deliberately short. A benchmark set that covered everything would be the enumeration in
    /// procedure 2; this one exists to be read by a person when it changes.
    /// </remarks>
    private static IEnumerable<(string Name, Rank[] Cards)> Hands() =>
    [
        ("natural-21",      [Rank.Ace, Rank.King]),
        ("hard-20",         [Rank.King, Rank.Ten]),
        ("soft-19",         [Rank.Ace, Rank.Eight]),
        ("hard-19",         [Rank.Ten, Rank.Nine]),
        ("hard-18",         [Rank.Ten, Rank.Eight]),
        ("soft-18",         [Rank.Ace, Rank.Seven]),
        ("hard-17",         [Rank.Ten, Rank.Seven]),
        ("soft-17",         [Rank.Ace, Rank.Six]),
        ("hard-16-two",     [Rank.Ten, Rank.Six]),
        ("hard-16-four",    [Rank.Three, Rank.Three, Rank.Five, Rank.Five]),
        ("hard-15",         [Rank.Ten, Rank.Five]),
        ("hard-14",         [Rank.Ten, Rank.Four]),
        ("hard-13",         [Rank.Ten, Rank.Three]),
        ("hard-12",         [Rank.Ten, Rank.Two]),
        ("low-8",           [Rank.Five, Rank.Three]),
        ("three-card-21",   [Rank.Seven, Rank.Seven, Rank.Seven]),
        ("four-card-21",    [Rank.Five, Rank.Five, Rank.Five, Rank.Six]),
        ("five-card-20",    [Rank.Two, Rank.Three, Rank.Four, Rank.Five, Rank.Six]),
        ("five-card-21",    [Rank.Ace, Rank.Two, Rank.Three, Rank.Four, Rank.Ace]),
        ("run-3-lane",      [Rank.Four, Rank.Five, Rank.Six]),
        ("face-heavy-20",   [Rank.King, Rank.Queen]),
        ("ace-pair",        [Rank.Ace, Rank.Ace]),
    ];

    [Fact]
    [Trait(Regression.Trait, Regression.Category)]
    public void The_benchmark_hand_set_has_not_changed_sign()
    {
        IReadOnlyList<Record> current = [.. Measure()];

        if (Regression.Regenerating)
        {
            File.WriteAllText(Regression.BaselinePath(BaselineFile), Serialize(current));
            Assert.Fail("Baselines regenerated. Review the diff, commit it, and re-run without BASTION_REGEN_BASELINES.");
        }

        IReadOnlyDictionary<string, Record> baseline = Deserialize(
            File.ReadAllText(Regression.BaselinePath(BaselineFile)));

        List<string> signChanges = [];
        List<string> drift = [];

        foreach (Record now in current)
        {
            if (!baseline.TryGetValue(now.Key, out Record? was))
            {
                signChanges.Add($"{now.Key}: absent from the baseline. Regenerate deliberately if it is a new benchmark.");
                continue;
            }

            // A lane that used to be defended and now leaks, or the reverse. This is the sign change
            // the procedure names: not a number moving, but a claim reversing.
            if (was.Leaks != now.Leaks)
            {
                signChanges.Add($"{now.Key}: leaked {was.TotalLeak} -> {now.TotalLeak} (defended={!was.Leaks} -> {!now.Leaks}).");
            }

            if (Math.Sign(was.Entry) != Math.Sign(now.Entry))
            {
                signChanges.Add($"{now.Key}: entry {was.Entry:F2} -> {now.Entry:F2} crossed zero.");
            }

            if (was.TotalLeak != now.TotalLeak || Math.Abs(was.RawOutput - now.RawOutput) > 1e-6)
            {
                drift.Add($"{now.Key}: leak {was.TotalLeak} -> {now.TotalLeak}, raw output {was.RawOutput:F2} -> {now.RawOutput:F2}.");
            }
        }

        // Magnitude drift is reported, never failed on. Every number here is first-pass and expected
        // to move; a gate that fired on drift would be re-baselined until nobody read it.
        if (drift.Count > 0)
        {
            Console.WriteLine($"[benchmark] {drift.Count} hand(s) moved without changing sign:");
            drift.ForEach(line => Console.WriteLine($"  {line}"));
        }

        Assert.True(signChanges.Count == 0,
            $"Benchmark hands changed sign:{Environment.NewLine}  {string.Join($"{Environment.NewLine}  ", signChanges)}");
    }

    [Fact]
    [Trait(Regression.Trait, Regression.Category)]
    public void The_benchmark_set_covers_the_live_decision_band()
    {
        // The design's narrowed claim is that hit/stand is live in 14-19. A benchmark set that did
        // not cover that band would be watching everywhere except where the design makes its claim.
        int[] totals = [.. Hands()
            .Select(h => h.Cards.Aggregate(HandState.Empty, (s, r) => s.Hit(r)).Total)
            .Distinct()];

        foreach (int total in Enumerable.Range(14, 6))
        {
            Assert.Contains(total, totals);
        }
    }

    /// <summary>One benchmark hand's resolved reading.</summary>
    private sealed record Record(string Key, double RawOutput, double Entry, int TotalLeak)
    {
        /// <summary>Whether anything got through at all - the quantity whose sign is watched.</summary>
        public bool Leaks => TotalLeak > 0;
    }

    /// <summary>
    /// Resolves each hand against one fixed board shape, at the entry its length earns.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The board is a fixed policy here, not an optimum: this procedure watches for change over
    /// time, so the only requirement is that the policy is the same on both sides of a comparison.
    /// </para>
    /// <para>
    /// The order <b>alternates lanes</b> rather than filling one first. Filling forward-first puts
    /// both towers of a two-card hand in lane 0, leaving lane 1 undefended and its full leak
    /// swamping every other difference - which collapsed most of the set to one number and left the
    /// sign-change check with almost nothing to see.
    /// </para>
    /// </remarks>
    private static IEnumerable<Record> Measure()
    {
        EncounterTuning encounter = Tuning.Encounter("example_wave");
        Card[] dealerHand = [new Card(Rank.Ten), new Card(Rank.Six), new Card(Rank.Seven)];

        SocketRef[] sockets =
        [
            SocketRef.InLane(0, 1), SocketRef.InLane(1, 1),
            SocketRef.InLane(0, 2), SocketRef.InLane(1, 2),
            SocketRef.InLane(0, 0), SocketRef.InLane(1, 0),
            SocketRef.Junction,
        ];

        foreach ((string name, Rank[] ranks) in Hands())
        {
            HandState hand = ranks.Aggregate(HandState.Empty, (s, r) => s.Hit(r));
            double entry = MarchClock.EntryAfter(Tuning, hand.CardCount, hand.IsExactly21);
            double multiplier = hand.FormationMultiplier(Tuning);

            (Card Card, SocketRef Socket)[] placed =
                [.. Resolved(hand).Select((card, i) => (card, sockets[i % sockets.Length]))];

            IReadOnlyDictionary<SocketRef, double> runBonus = RunLinks.BonusBySocket(Tuning, placed);

            BoardState board = BoardState.Create(
                Tuning,
                placed.Select(p => TowerState.Place(
                    Tuning, p.Card, Family.Club, p.Socket, multiplier,
                    runBonus.GetValueOrDefault(p.Socket, 0.0))),
                entry);

            FinalForecast forecast = Resolver.ResolveComplete(
                Tuning, encounter, board,
                ArmyBuilder.Complete(Tuning, encounter, dealerHand, entry));

            // Raw output and entry, never a derived engagement-adjusted figure - the same rule
            // procedure 2 follows, and for the same reason.
            double raw = placed.Sum(p => Tuning.CardPower.ForValue(p.Card.Value)) * multiplier;

            yield return new Record(name, raw, entry, forecast.Lanes.Sum(l => l.PredictedDamage));
        }
    }

    /// <summary>The hand's cards with each Ace's high-or-low state resolved from the hand.</summary>
    private static Card[] Resolved(HandState hand)
    {
        int high = hand.AceHighCount;
        int seen = 0;

        return [.. hand.Cards.Select(r => r == Rank.Ace ? new Card(Rank.Ace, seen++ < high) : new Card(r))];
    }

    private static string Serialize(IEnumerable<Record> records)
    {
        StringBuilder csv = new();
        csv.AppendLine("# Regression procedure 1. Committed baseline - regenerate deliberately, never on failure.");
        csv.AppendLine("hand,rawOutput,entry,totalLeak");

        foreach (Record record in records)
        {
            csv.AppendLine(CultureInfo.InvariantCulture,
                $"{record.Key},{record.RawOutput:F4},{record.Entry:F4},{record.TotalLeak}");
        }

        return csv.ToString();
    }

    private static IReadOnlyDictionary<string, Record> Deserialize(string text)
    {
        Dictionary<string, Record> records = [];

        foreach (string line in text.Split('\n').Select(l => l.Trim()))
        {
            if (line.Length == 0 || line.StartsWith('#') || line.StartsWith("hand,", StringComparison.Ordinal))
            {
                continue;
            }

            string[] parts = line.Split(',');

            records[parts[0]] = new Record(
                parts[0],
                double.Parse(parts[1], CultureInfo.InvariantCulture),
                double.Parse(parts[2], CultureInfo.InvariantCulture),
                int.Parse(parts[3], CultureInfo.InvariantCulture));
        }

        return records;
    }
}
