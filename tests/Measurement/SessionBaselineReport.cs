using System.Globalization;
using System.Text;
using Bastion.Core.Config;
using Bastion.Core.Validation;

namespace Bastion.Core.Tests.Measurement;

/// <summary>
/// Reduces the playtest session logs to the Milestone 5 baseline the stacking pass is read against.
/// </summary>
/// <remarks>
/// <para>
/// docs/ROADMAP.md § Milestone 6: the stacking pass is compared against the Milestone 5 baseline on
/// forced-replacement frequency, stack-at-capacity rate, run frequency, placement depth, and
/// many-card viability. This writes that baseline to <c>telemetry/session-baseline.csv</c>, and
/// the <b>same</b> report is what the stacking pass re-runs - the comparison is only worth anything
/// if both halves are reduced identically.
/// </para>
/// <para>
/// Unlike the resolver sweeps beside it, this reads no tuning and simulates nothing. It reports what
/// people actually did, which is the half of the primary measurement that
/// docs/prototype/VALIDATION.md asks for separately and that no sweep can supply.
/// </para>
/// <para>
/// <b>It does not fail when there are no sessions</b>, because session logs are gitignored and a
/// clean checkout legitimately has none. It writes what coverage exists and says so; an empty
/// baseline is a finding to read, not a broken test.
/// </para>
/// </remarks>
public sealed class SessionBaselineReport
{
    /// <summary>The arms the baseline has to cover before the stacking pass can be read.</summary>
    private static readonly string[] Arms = ["A", "B", "C"];

    [Fact]
    public void Reduce_the_session_logs_to_the_stacking_baseline()
    {
        string root = TuningLoader.FindRepositoryRoot();
        string directory = Path.Combine(root, "telemetry", "sessions");

        string[] logs = Directory.Exists(directory)
            ? [.. Directory.EnumerateFiles(directory, "*.jsonl").Order(StringComparer.Ordinal)]
            : [];

        SessionMetrics[] sessions =
        [
            .. logs.Select(path => SessionAnalysis.Reduce(Path.GetFileName(path), File.ReadLines(path))),
        ];

        StringBuilder csv = new();

        csv.AppendLine("# Milestone 5 baseline, reduced from telemetry/sessions/*.jsonl.");
        csv.AppendLine("# The stacking pass re-runs THIS report with the flag on and compares.");
        csv.AppendLine("# stackAtCapacityRate is absent by construction with stacking off - it is not zero, it is unmeasured.");
        csv.AppendLine("# Readings are pre-committed in docs/prototype/VALIDATION.md - do not renegotiate them after reading this.");
        csv.AppendLine("# MACHINE-DRIVEN SESSIONS ARE EXCLUDED and listed below by name. A capture run produces");
        csv.AppendLine("# states nobody decided; pooling them measures CaptureRun, not a player.");
        csv.AppendLine();

        AppendArms(csv, sessions);
        AppendSessions(csv, sessions);
        AppendCoverage(csv, sessions);

        Sweeps.Write("session-baseline.csv", csv.ToString());

        // Asserts only that the reduction ran. Whether the baseline is adequate is a judgement made
        // against § Coverage below, not a pass/fail condition - wiring a session-count threshold in
        // here would turn "we have not played enough yet" into a red test on every checkout.
        Assert.All(sessions, s => Assert.True(s.UnreadableLines <= 1,
            $"{s.Session} has {s.UnreadableLines} unreadable lines; one truncated final line is expected, more is corruption."));
    }

    /// <summary>The five comparison metrics, per arm. This is the table the stacking pass diffs.</summary>
    private static void AppendArms(StringBuilder csv, IReadOnlyList<SessionMetrics> sessions)
    {
        csv.AppendLine("# THE COMPARISON TABLE. Empty cells mean the arm was never played, not that the metric is zero.");
        csv.AppendLine("# 'sessions' counts human sessions only; 'syntheticExcluded' is what was screened out.");
        csv.AppendLine("arm,sessions,syntheticExcluded,states,settledWaves,forcedReplacementFraction,stackAtCapacityRate," +
                       "capacityStateFraction,runFraction,meanPlacementDepth,meanCardsAtLock,fiveOrMoreCardFraction");

        foreach (string arm in Arms)
        {
            SessionMetrics[] forArm = [.. sessions.Where(s => string.Equals(s.Arm, arm, StringComparison.Ordinal))];
            ArmMetrics m = ArmMetrics.Pool(arm, forArm);

            csv.AppendLine(CultureInfo.InvariantCulture,
                $"{m.Arm},{m.Sessions},{m.SyntheticSessionsExcluded},{m.States},{m.SettledWaves}," +
                $"{F(m.ForcedReplacementFraction)}," +
                $"{F(m.StackAtCapacityRate)},{F(m.CapacityStateFraction)},{F(m.RunFraction)}," +
                $"{F(m.MeanPlacementDepth)},{F(m.MeanCardsAtLock)},{F(m.FiveOrMoreCardFraction)}");
        }

        csv.AppendLine();
    }

    private static void AppendSessions(StringBuilder csv, IReadOnlyList<SessionMetrics> sessions)
    {
        csv.AppendLine("# Per session, so a short or abandoned run can be spotted rather than pooled away.");
        csv.AppendLine("# driver=machine rows are EXCLUDED from the table above. They are listed, never deleted.");
        csv.AppendLine("session,arm,driver,medianDecisionMs,states,settledWaves,placements," +
                       "forcedReplacements,statesAtCapacity,wavesWithRun,unreadableLines");

        foreach (SessionMetrics s in sessions)
        {
            csv.AppendLine(CultureInfo.InvariantCulture,
                $"{s.Session},{s.Arm},{(s.IsSynthetic ? "machine" : "human")},{F(s.MedianDecisionMilliseconds)}," +
                $"{s.States},{s.SettledWaves},{s.Placements}," +
                $"{s.ForcedReplacements},{s.StatesAtCapacity},{s.WavesWithRun},{s.UnreadableLines}");
        }

        csv.AppendLine();
    }

    /// <summary>
    /// What the baseline does not yet cover.
    /// </summary>
    /// <remarks>
    /// Reported in the artifact itself rather than left to whoever reads it. The failure mode this
    /// guards against is comparing a stacking pass against a baseline that turns out to have been
    /// one arm and a handful of abandoned sessions - which is exactly the state this report was
    /// first run in.
    /// </remarks>
    private static void AppendCoverage(StringBuilder csv, IReadOnlyList<SessionMetrics> sessions)
    {
        SessionMetrics[] human = [.. sessions.Where(s => !s.IsSynthetic)];

        string[] missing =
        [
            .. Arms.Where(arm => !human.Any(s =>
                string.Equals(s.Arm, arm, StringComparison.Ordinal) && s.SettledWaves > 0)),
        ];

        csv.AppendLine("# COVERAGE, human sessions only. The stacking pass cannot be read against an arm");
        csv.AppendLine("# with no settled waves, and a machine-driven wave is not a settled wave.");
        csv.AppendLine("metric,value");
        csv.AppendLine(CultureInfo.InvariantCulture, $"humanSessions,{human.Length}");
        csv.AppendLine(CultureInfo.InvariantCulture, $"syntheticExcluded,{sessions.Count - human.Length}");
        csv.AppendLine(CultureInfo.InvariantCulture, $"settledWaves,{human.Sum(s => s.SettledWaves)}");
        csv.AppendLine(CultureInfo.InvariantCulture,
            $"armsWithNoSettledWave,{(missing.Length == 0 ? "none" : string.Join(" ", missing))}");
    }

    /// <summary>An absent metric is an empty cell, never a zero.</summary>
    private static string F(double? value) =>
        value is null ? string.Empty : value.Value.ToString("F3", CultureInfo.InvariantCulture);
}
