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
    /// Which socket geometry closes the deep-placement margin?
    /// </summary>
    /// <remarks>
    /// <para>
    /// The remedy step for Open Decision 2. The design permits exactly three remedies - <b>uneven
    /// spacing, range differences by position, or lane-specific leak thresholds - and explicitly
    /// not the march curve</b> (docs/design/03-march-clock.md,
    /// docs/prototype/RISKS-AND-ADDBACKS.md). The first two are geometry and are swept here; the
    /// third changes what a lane is worth rather than what a socket is worth, so it is not a depth
    /// remedy at all.
    /// </para>
    /// <para>
    /// Candidates keep <c>pathLength</c> at 12.0 and mean socket spacing at 3.0, because the march
    /// step sizes are derived from socket spacing (docs/design/03-march-clock.md § The geometry
    /// problem) and the three arms are pre-committed test arms. A candidate that shifted mean
    /// spacing would silently require re-deriving the arms, which is out of bounds.
    /// </para>
    /// <para>
    /// <b>The selection rule is committed before the numbers are read</b>, per the project's
    /// standing practice of deciding what a measurement means before taking it: pick the candidate
    /// whose mean depth effect is closest to zero <i>across all three arms at once</i>, break ties
    /// toward the smaller spread between arms, and reject any candidate that merely inverts the
    /// bias into strong shallow dominance - over-correcting is the same failure wearing the other
    /// hat. <see cref="Score"/> is that rule in code.
    /// </para>
    /// </remarks>
    [Fact]
    public void Sweep_candidate_geometries()
    {
        bool instrumented = DebugGate.IsEnabled;

        if (!instrumented)
        {
            return;
        }

        StringBuilder csv = new();
        csv.AppendLine("# Candidate socket geometries against the deep-placement margin.");
        csv.AppendLine("# meanDeepMinusShallow < 0 means deep placement still wins. Nearest zero across all arms wins.");
        csv.AppendLine("candidate,positions,ranges,arm,shapesCompared,meanDeepMinusShallow,verdict");

        List<(Candidate Candidate, double Worst, double Spread, bool Shallow)> scored = [];

        foreach (Candidate candidate in Candidates())
        {
            TuningData tuning = candidate.Apply(Fixture.Tuning);

            // The candidate is built with a with-expression, which bypasses every cross-field rule
            // the file was checked against. Assert it is a configuration the game would accept
            // before measuring it, or the result describes an impossible board.
            TuningLoader.Validate(tuning, candidate.Name);

            IReadOnlyList<ArmSummary> arms = MeanByArm(DepthDeltas(CollectRows(
                tuning, rankAt: _ => Rank.Seven, modelRuns: false, csv: null)));

            foreach (ArmSummary arm in arms)
            {
                csv.AppendLine(CultureInfo.InvariantCulture,
                    $"{candidate.Name},{Join(candidate.Positions)},{Join(candidate.Ranges)}," +
                    $"{arm.Arm},{arm.ShapesCompared},{arm.MeanDelta:F3},{Verdict(arm.MeanDelta)}");
            }

            (double worst, double spread, bool shallow) = Score(arms);
            scored.Add((candidate, worst, spread, shallow));
        }

        AppendCandidateVerdict(csv, scored);
        Sweeps.Write("geometry-candidates.csv", csv.ToString());

        Assert.NotEmpty(scored);
    }

    /// <summary>
    /// The committed selection rule, as three comparable numbers.
    /// </summary>
    /// <remarks>
    /// <b>Worst</b> is the largest absolute depth effect over the three arms - not the mean of them,
    /// because a candidate that is neutral in A and badly biased in C has not fixed anything; the
    /// remedy has to hold wherever the clock is set. <b>Spread</b> is the tie-break: how differently
    /// the arms behave. <b>Shallow</b> flags an over-correction, where every arm has flipped to
    /// favouring shallow placement.
    /// </remarks>
    private static (double Worst, double Spread, bool Shallow) Score(IReadOnlyList<ArmSummary> arms)
    {
        double[] means = [.. arms.Select(a => a.MeanDelta)];

        return (means.Max(Math.Abs), means.Max() - means.Min(), means.All(m => m > 0.5));
    }

    private static void AppendCandidateVerdict(
        StringBuilder csv, IReadOnlyList<(Candidate Candidate, double Worst, double Spread, bool Shallow)> scored)
    {
        csv.AppendLine();
        csv.AppendLine("# Selection rule, committed before reading: smallest worst-arm |depth effect|,");
        csv.AppendLine("# tie-break on the smaller spread between arms, rejecting strong shallow inversion.");
        csv.AppendLine("rank,candidate,worstArmAbsEffect,spreadBetweenArms,rejected");

        var ranked = scored
            .OrderBy(s => s.Shallow)          // rejected candidates sort last
            .ThenBy(s => s.Worst)
            .ThenBy(s => s.Spread)
            .ToList();

        for (int i = 0; i < ranked.Count; i++)
        {
            csv.AppendLine(CultureInfo.InvariantCulture,
                $"{i + 1},{ranked[i].Candidate.Name},{ranked[i].Worst:F3},{ranked[i].Spread:F3}," +
                $"{(ranked[i].Shallow ? "shallow-inversion" : "")}");
        }
    }

    /// <summary>A socket layout to measure: positions along the path, and range at each.</summary>
    private sealed record Candidate(string Name, IReadOnlyList<double> Positions, IReadOnlyList<double> Ranges)
    {
        /// <summary>
        /// This geometry applied to base tuning, with everything derived from it brought along.
        /// </summary>
        /// <remarks>
        /// <c>march.entryClampMax</c> is the rear socket's position and
        /// <c>towers.junctionPathPosition</c> is the middle socket's, both by the loader's own
        /// rules. Moving sockets without moving these produces a config that would be rejected on
        /// load, so they are derived here rather than restated per candidate.
        /// </remarks>
        public TuningData Apply(TuningData baseTuning) => baseTuning with
        {
            Geometry = baseTuning.Geometry with { SocketPositions = Positions, RangeBySocket = Ranges },
            March = baseTuning.March with { EntryClampMax = Positions.Max() },
            Towers = baseTuning.Towers with { JunctionPathPosition = Positions[Positions.Count / 2] },
        };
    }

    /// <summary>
    /// The candidate set: the shipped control, range profiles, spacing profiles, and the two
    /// combined.
    /// </summary>
    /// <remarks>
    /// Every spacing candidate keeps a mean gap of 3.0 - <c>[3,5,9]</c> and <c>[3,7,9]</c> have gaps
    /// of 2+4 and 4+2 against the control's 3+3 - so the march arms stay derivable from spacing as
    /// the design requires.
    /// </remarks>
    private static IEnumerable<Candidate> Candidates() =>
    [
        new("control",            [3.0, 6.0, 9.0], [3.0, 3.0, 3.0]),   // what ships today
        new("range-soft",         [3.0, 6.0, 9.0], [3.5, 3.0, 2.5]),
        new("range-mid",          [3.0, 6.0, 9.0], [4.0, 3.0, 2.5]),
        new("range-hard",         [3.0, 6.0, 9.0], [4.0, 3.0, 2.0]),
        new("range-steep",        [3.0, 6.0, 9.0], [4.5, 3.0, 1.5]),
        new("spacing-forward",    [3.0, 5.0, 9.0], [3.0, 3.0, 3.0]),
        new("spacing-rear",       [3.0, 7.0, 9.0], [3.0, 3.0, 3.0]),
        new("forward-plus-range", [3.0, 5.0, 9.0], [4.0, 3.0, 2.0]),
        new("rear-plus-range",    [3.0, 7.0, 9.0], [4.0, 3.0, 2.0]),
    ];

    private static string Join(IEnumerable<double> values) =>
        string.Join("|", values.Select(v => v.ToString("0.##", CultureInfo.InvariantCulture)));

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

        StringBuilder csv = new();
        csv.AppendLine("arm,towers,sockets,shape,entry,meanDepth,lane0Leak,lane1Leak,totalLeak");

        IReadOnlyList<Row> rows = CollectRows(Fixture.Tuning, rankAt, modelRuns, csv);

        AppendSummary(csv, rows);
        Sweeps.Write(fileName, csv.ToString());

        // The sweep is a measurement, so it asserts only that it produced something to read. The
        // reading itself is a judgement recorded in the roadmap, not a pass/fail condition - and
        // wiring a threshold in here would be renegotiating a pre-committed reading in code.
        Assert.NotEmpty(rows);
    }

    /// <summary>
    /// Every board configuration's leakage, across all three arms, for one geometry.
    /// </summary>
    /// <remarks>
    /// Split out from <see cref="Sweep"/> so the candidate-geometry sweep measures with exactly the
    /// same code rather than a second implementation that could drift from it. Appends a per-row
    /// line to <paramref name="csv"/> when one is supplied; the candidate sweep passes null,
    /// because nine candidates x 273 configurations is a file nobody reads.
    /// </remarks>
    private static IReadOnlyList<Row> CollectRows(
        TuningData baseTuning, Func<SocketRef, Rank> rankAt, bool modelRuns, StringBuilder? csv)
    {
        EncounterTuning encounter = baseTuning.Encounter("example_wave");
        Card[] dealerHand = [new Card(Rank.Ten), new Card(Rank.Six), new Card(Rank.Seven)];

        List<Row> rows = [];

        foreach (string arm in baseTuning.MarchPresets.Keys.Order(StringComparer.Ordinal))
        {
            TuningData tuning = baseTuning with
            {
                March = baseTuning.March with { ActivePreset = arm },
            };

            IReadOnlyList<SocketRef> sockets = Sweeps.AllSockets(tuning);

            for (int boardSize = 2; boardSize <= 4; boardSize++)
            {
                // A hand of N cards places N towers and has paid N-2 march steps for the privilege.
                // Tying the entry point to the board size is what makes this a test of the clock
                // and the geometry together rather than of the geometry alone.
                double entry = MarchClock.EntryAfter(tuning, boardSize, reachedExactly21: false);

                foreach (SocketRef[] choice in Sweeps.Combinations(sockets, boardSize))
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

                    double meanDepth = choice.Average(s => Sweeps.DepthOf(tuning, s));
                    int lane0 = forecast.Lanes[0].PredictedDamage;
                    int lane1 = forecast.Lanes[1].PredictedDamage;

                    // How the towers are spread between the lanes and the junction. Depth and
                    // spread are correlated across the raw sweep - a junction tower sits at 6.0
                    // and also covers the lane nobody defended - so comparing depth without
                    // holding spread fixed measures spread and calls it depth.
                    string shape = $"{choice.Count(s => !s.IsJunction && s.LaneIndex == 0)}"
                                 + $"-{choice.Count(s => !s.IsJunction && s.LaneIndex == 1)}"
                                 + $"-{choice.Count(s => s.IsJunction)}";

                    csv?.Append(CultureInfo.InvariantCulture, $"{arm},{boardSize},{Describe(choice)},{shape},")
                        .Append(CultureInfo.InvariantCulture, $"{entry:F2},{meanDepth:F3},{lane0},{lane1},{lane0 + lane1}")
                        .AppendLine();

                    rows.Add(new Row(arm, boardSize, shape, entry, meanDepth, lane0 + lane1));
                }
            }
        }

        return rows;
    }

    /// <summary>One configuration's result.</summary>
    private sealed record Row(
        string Arm, int Towers, string Shape, double Entry, double MeanDepth, int TotalLeak);

    /// <summary>
    /// Writes the within-shape depth comparison and its per-arm means beneath the raw rows.
    /// </summary>
    private static void AppendSummary(StringBuilder csv, IReadOnlyList<Row> rows)
    {
        csv.AppendLine();
        csv.AppendLine("# Depth compared within a fixed board shape (towers in lane 0 - lane 1 - junction).");
        csv.AppendLine("# deepMinusShallow > 0 means deep placement leaked MORE, i.e. shallow won.");
        csv.AppendLine("arm,towers,shape,entry,configs,shallowestLeak,deepestLeak,deepMinusShallow");

        IReadOnlyList<ShapeDelta> deltas = DepthDeltas(rows);

        foreach (ShapeDelta d in deltas)
        {
            csv.AppendLine(CultureInfo.InvariantCulture,
                $"{d.Arm},{d.Towers},{d.Shape},{d.Entry:F2}," +
                $"{d.Configs},{d.ShallowestLeak:F1},{d.DeepestLeak:F1},{d.Delta:F1}");
        }

        csv.AppendLine();
        csv.AppendLine("# Mean of the above per arm. The pre-committed reading in prototype/VALIDATION.md:");
        csv.AppendLine("# if deep placement wins across every arm, fix socket geometry before the march curve.");
        csv.AppendLine("arm,shapesCompared,meanDeepMinusShallow,verdict");

        foreach (ArmSummary arm in MeanByArm(deltas))
        {
            csv.AppendLine(CultureInfo.InvariantCulture,
                $"{arm.Arm},{arm.ShapesCompared},{arm.MeanDelta:F3},{Verdict(arm.MeanDelta)}");
        }
    }

    /// <summary>One board shape's shallowest-to-deepest leak difference, within one arm.</summary>
    private sealed record ShapeDelta(
        string Arm, int Towers, string Shape, double Entry,
        int Configs, double ShallowestLeak, double DeepestLeak, double Delta);

    /// <summary>One arm's mean depth effect for a geometry.</summary>
    private sealed record ArmSummary(string Arm, int ShapesCompared, double MeanDelta);

    private static string Verdict(double mean) =>
        mean > 0 ? "shallow wins" : mean < 0 ? "DEEP WINS" : "neutral";

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
    private static IReadOnlyList<ShapeDelta> DepthDeltas(IReadOnlyList<Row> rows)
    {
        List<ShapeDelta> deltas = [];

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

            deltas.Add(new ShapeDelta(
                group.Key.Arm, group.Key.Towers, group.Key.Shape, ordered[0].Entry,
                ordered.Count, ordered[0].TotalLeak, ordered[^1].TotalLeak,
                ordered[^1].TotalLeak - ordered[0].TotalLeak));
        }

        return deltas;
    }

    private static IReadOnlyList<ArmSummary> MeanByArm(IReadOnlyList<ShapeDelta> deltas) =>
    [
        .. deltas
            .GroupBy(d => d.Arm, StringComparer.Ordinal)
            .OrderBy(g => g.Key, StringComparer.Ordinal)
            .Select(g => new ArmSummary(g.Key, g.Count(), g.Average(d => d.Delta))),
    ];

    private static string Describe(IEnumerable<SocketRef> sockets) =>
        string.Join("|", sockets.Select(s => s.IsJunction ? "J" : $"L{s.LaneIndex}S{s.SocketIndex}"));
}
