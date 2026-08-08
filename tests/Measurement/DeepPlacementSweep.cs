using System.Globalization;
using System.Text;
using Bastion.Core.Board;
using Bastion.Core.Cards;
using Bastion.Core.Config;
using Bastion.Core.Diagnostics;
using Bastion.Core.March;
using Bastion.Core.Resolve;
using Bastion.Core.Tests.Resolve;

namespace Bastion.Core.Tests.Measurement;

/// <summary>
/// Is deep placement weakly dominant?
/// </summary>
/// <remarks>
/// <para>
/// <b>This is the first question the resolver exists to answer</b> (docs/ROADMAP.md Open Decision 2,
/// docs/design/03-march-clock.md). The engagement arithmetic says yes: entry advances from the spawn
/// side, so it eats the forward socket's window first and the rear socket's last, which taxes
/// forward placement rather than drawing. But the pushback - traps that need early application,
/// enemies that must be stopped before a leak threshold - <b>lives in the resolver, not the
/// arithmetic</b>, so it cannot be settled on paper.
/// </para>
/// <para>
/// The reading is pre-committed by docs/prototype/VALIDATION.md and must not be renegotiated after
/// seeing the numbers: <b>if towers cluster at socket 9 across every arm, the socket geometry needs
/// work before the march curve does.</b> The remedy is uneven spacing, range differences by
/// position, or lane-specific leak thresholds - not the march curve.
/// </para>
/// <para>
/// Gated behind <see cref="DebugGate"/>: this is instrumentation, not a regression gate. Run it with
/// <c>dotnet test -p:BastionInstrumentation=true</c>.
/// </para>
/// </remarks>
public sealed class DeepPlacementSweep
{
    /// <summary>Every socket a tower can occupy: three per lane, plus the shared junction.</summary>
    private static IReadOnlyList<SocketRef> AllSockets(TuningData tuning)
    {
        List<SocketRef> sockets = [];

        for (int lane = 0; lane < tuning.Geometry.Lanes; lane++)
        {
            for (int socket = 0; socket < tuning.Geometry.SocketPositions.Count; socket++)
            {
                sockets.Add(SocketRef.InLane(lane, socket));
            }
        }

        sockets.Add(SocketRef.Junction);

        return sockets;
    }

    [Fact]
    public void Sweep_placement_depth_against_lane_leakage()
    {
        // The Milestone 1 baseline: identical cards throughout, so neither card power nor a run link
        // can explain a difference this sweep is trying to attribute to position.
        Sweep(rankAt: _ => Rank.Seven, modelRuns: false, fileName: "deep-placement.csv");
    }

    [Fact]
    public void Sweep_placement_depth_with_run_links_modelled()
    {
        // Open Decision 2, re-measured now that runs exist (Milestone 2). Deep placement was found
        // weakly dominant with runs absent; run-link adjacency is named as a pushback that might
        // shrink that margin (docs/ROADMAP.md § 2). Ranks follow a depth-symmetric valley - 6 at the
        // outer sockets, 5 in the middle - so any contiguous same-lane pair forms a 2-run at
        // identical total power whether it sits shallow or deep. That deliberately avoids a
        // power gradient confounding the depth comparison: what is left is the run interaction alone.
        // Output only; it informs the later geometry decision and does NOT overwrite the baseline.
        Sweep(rankAt: s => (Rank)(!s.IsJunction && s.SocketIndex == 1 ? 5 : 6), modelRuns: true,
              fileName: "deep-placement-runs.csv");
    }

    /// <summary>
    /// Sweeps every socket permutation for boards of 2-4 towers across all three arms, writing the
    /// per-configuration leakage and the within-shape depth summary to <paramref name="fileName"/>.
    /// </summary>
    private static void Sweep(Func<SocketRef, Rank> rankAt, bool modelRuns, string fileName)
    {
        // Copied to a local so the branch is not folded away as unreachable at compile time.
        bool instrumented = DebugGate.IsEnabled;

        if (!instrumented)
        {
            // A player build has no business running a 273-configuration sweep, and the oracle
            // gate is compile-time precisely so the code is absent rather than merely unreachable.
            return;
        }

        TuningData baseTuning = Fixture.Tuning;
        EncounterTuning encounter = baseTuning.Encounter("example_wave");
        Card[] dealerHand = [new Card(Rank.Ten), new Card(Rank.Six), new Card(Rank.Seven)];

        StringBuilder csv = new();
        csv.AppendLine("arm,towers,sockets,shape,entry,meanDepth,lane0Leak,lane1Leak,totalLeak");

        List<Row> rows = [];

        foreach (string arm in baseTuning.MarchPresets.Keys.Order(StringComparer.Ordinal))
        {
            TuningData tuning = baseTuning with
            {
                March = baseTuning.March with { ActivePreset = arm },
            };

            IReadOnlyList<SocketRef> sockets = AllSockets(tuning);

            for (int boardSize = 2; boardSize <= 4; boardSize++)
            {
                // A hand of N cards places N towers and has paid N-2 march steps for the privilege.
                // Tying the entry point to the board size is what makes this a test of the clock
                // and the geometry together rather than of the geometry alone.
                double entry = MarchClock.EntryAfter(tuning, boardSize, reachedExactly21: false);

                foreach (SocketRef[] choice in Combinations(sockets, boardSize))
                {
                    (Card Card, SocketRef Socket)[] placed = [.. choice.Select(s => (new Card(rankAt(s)), s))];

                    // When runs are modelled, contiguous same-lane placements link; the baseline
                    // passes uniform ranks that never form a run, so its bonuses are all zero.
                    IReadOnlyDictionary<SocketRef, double> runBonus = modelRuns
                        ? RunLinks.BonusBySocket(tuning, placed)
                        : new Dictionary<SocketRef, double>();

                    BoardState board = BoardState.Create(
                        tuning,
                        placed.Select(p => TowerState.Place(
                            tuning, p.Card, Family.Club, p.Socket, formationMultiplier: 1.0,
                            runBonus: runBonus.GetValueOrDefault(p.Socket, 0.0))),
                        entry);

                    FinalForecast forecast = Resolver.ResolveComplete(
                        tuning, encounter, board,
                        ArmyBuilder.Complete(tuning, encounter, dealerHand, entry));

                    double meanDepth = choice.Average(s => DepthOf(tuning, s));
                    int lane0 = forecast.Lanes[0].PredictedDamage;
                    int lane1 = forecast.Lanes[1].PredictedDamage;

                    // How the towers are spread between the lanes and the junction. Depth and
                    // spread are correlated across the raw sweep - a junction tower sits at 6.0
                    // and also covers the lane nobody defended - so comparing depth without
                    // holding spread fixed measures spread and calls it depth.
                    string shape = $"{choice.Count(s => !s.IsJunction && s.LaneIndex == 0)}"
                                 + $"-{choice.Count(s => !s.IsJunction && s.LaneIndex == 1)}"
                                 + $"-{choice.Count(s => s.IsJunction)}";

                    csv.Append(CultureInfo.InvariantCulture, $"{arm},{boardSize},{Describe(choice)},{shape},")
                       .Append(CultureInfo.InvariantCulture, $"{entry:F2},{meanDepth:F3},{lane0},{lane1},{lane0 + lane1}")
                       .AppendLine();

                    rows.Add(new Row(arm, boardSize, shape, entry, meanDepth, lane0 + lane1));
                }
            }
        }

        AppendSummary(csv, rows);

        string path = Path.Combine(TuningLoader.FindRepositoryRoot(), "telemetry", fileName);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, csv.ToString());

        // The sweep is a measurement, so it asserts only that it produced something to read. The
        // reading itself is a judgement recorded in the roadmap, not a pass/fail condition - and
        // wiring a threshold in here would be renegotiating a pre-committed reading in code.
        Assert.NotEmpty(rows);
    }

    /// <summary>One configuration's result.</summary>
    private sealed record Row(
        string Arm, int Towers, string Shape, double Entry, double MeanDepth, int TotalLeak);

    /// <summary>
    /// Compares depth <i>within</i> a fixed board shape, which is the only comparison that answers
    /// the question.
    /// </summary>
    /// <remarks>
    /// A naive deepest-versus-shallowest split over the whole sweep reports that deep placement
    /// wins - but that is spread wearing depth's clothes. The junction sits at path position 6.0,
    /// which reads as mid-depth, and it also covers whichever lane the player neglected, so
    /// junction configurations leak less for a reason that has nothing to do with how deep they
    /// are. Holding the shape fixed removes that.
    /// </remarks>
    private static void AppendSummary(StringBuilder csv, IReadOnlyList<Row> rows)
    {
        csv.AppendLine();
        csv.AppendLine("# Depth compared within a fixed board shape (towers in lane 0 - lane 1 - junction).");
        csv.AppendLine("# deepMinusShallow > 0 means deep placement leaked MORE, i.e. shallow won.");
        csv.AppendLine("arm,towers,shape,entry,configs,shallowestLeak,deepestLeak,deepMinusShallow");

        List<(string Arm, double Delta)> deltas = [];

        IEnumerable<IGrouping<(string, int, string), Row>> groups = rows
            .GroupBy(r => (r.Arm, r.Towers, r.Shape))
            .OrderBy(g => g.Key.Item1, StringComparer.Ordinal)
            .ThenBy(g => g.Key.Item2)
            .ThenBy(g => g.Key.Item3, StringComparer.Ordinal);

        foreach (IGrouping<(string Arm, int Towers, string Shape), Row> group in groups)
        {
            List<Row> ordered = [.. group.OrderBy(r => r.MeanDepth)];

            // A shape whose sockets all sit at the same mean depth cannot say anything about depth.
            if (ordered[0].MeanDepth >= ordered[^1].MeanDepth)
            {
                continue;
            }

            double shallowest = ordered[0].TotalLeak;
            double deepest = ordered[^1].TotalLeak;
            double delta = deepest - shallowest;

            csv.AppendLine(CultureInfo.InvariantCulture,
                $"{group.Key.Arm},{group.Key.Towers},{group.Key.Shape},{ordered[0].Entry:F2}," +
                $"{ordered.Count},{shallowest:F1},{deepest:F1},{delta:F1}");

            deltas.Add((group.Key.Arm, delta));
        }

        csv.AppendLine();
        csv.AppendLine("# Mean of the above per arm. The pre-committed reading in prototype/VALIDATION.md:");
        csv.AppendLine("# if deep placement wins across every arm, fix socket geometry before the march curve.");
        csv.AppendLine("arm,shapesCompared,meanDeepMinusShallow,verdict");

        foreach (string arm in deltas.Select(d => d.Arm).Distinct().Order(StringComparer.Ordinal))
        {
            double[] armDeltas = [.. deltas.Where(d => d.Arm == arm).Select(d => d.Delta)];
            double mean = armDeltas.Average();

            string verdict = mean > 0 ? "shallow wins" : mean < 0 ? "DEEP WINS" : "neutral";

            csv.AppendLine(CultureInfo.InvariantCulture,
                $"{arm},{armDeltas.Length},{mean:F3},{verdict}");
        }
    }

    /// <summary>Path position of a socket. Deeper means further from the spawn side.</summary>
    private static double DepthOf(TuningData tuning, SocketRef socket) =>
        socket.IsJunction
            ? tuning.Towers.JunctionPathPosition
            : tuning.Geometry.SocketPositions[socket.SocketIndex];

    private static string Describe(IEnumerable<SocketRef> sockets) =>
        string.Join("|", sockets.Select(s => s.IsJunction ? "J" : $"L{s.LaneIndex}S{s.SocketIndex}"));

    /// <summary>Every choice of <paramref name="size"/> sockets, in a fixed order.</summary>
    private static IEnumerable<SocketRef[]> Combinations(IReadOnlyList<SocketRef> sockets, int size)
    {
        int[] indices = [.. Enumerable.Range(0, size)];

        while (true)
        {
            yield return [.. indices.Select(i => sockets[i])];

            int pivot = size - 1;

            while (pivot >= 0 && indices[pivot] == sockets.Count - size + pivot)
            {
                pivot--;
            }

            if (pivot < 0)
            {
                yield break;
            }

            indices[pivot]++;

            for (int i = pivot + 1; i < size; i++)
            {
                indices[i] = indices[i - 1] + 1;
            }
        }
    }
}
