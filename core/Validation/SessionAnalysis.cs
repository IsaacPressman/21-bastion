using System.Text.Json;
using System.Text.Json.Serialization;

namespace Bastion.Core.Validation;

/// <summary>
/// Reduces playtest session logs to the metrics the stacking pass is read against.
/// </summary>
/// <remarks>
/// <para>
/// docs/ROADMAP.md § Milestone 6 names five comparison metrics - forced-replacement frequency,
/// stack-at-capacity rate, run frequency, placement depth, and many-card viability - and says the
/// stacking pass is compared against <b>the Milestone 5 baseline</b>. A baseline that cannot be
/// reduced to those five numbers is not a baseline, so this is the reducer, and it is deliberately
/// the <i>same</i> reducer for both passes: two passes summarised by two different scripts would
/// produce numbers that look comparable and are not.
/// </para>
/// <para>
/// It reads the log rather than the session, so it takes no position on how a state was reached and
/// works unchanged on logs already written. Everything it needs is in <see cref="StateRecord"/>
/// today - socket occupancy, socket depth, run bonus, card count - which is why the baseline needs
/// no instrumentation change. <b>Stack-at-capacity is the one metric that is not derivable yet</b>,
/// because with the flag off there are no stacks; it arrives with the Milestone 6 fields and is
/// reported as absent rather than as zero, so an unmeasured pass cannot read as a measured one.
/// </para>
/// <para>
/// Engine-free, so the reduction runs headlessly in the test suite rather than inside a scene tree.
/// The log envelope is re-declared here as a read model instead of being shared with
/// <c>game/telemetry/PlaytestLog.cs</c>: the writer is a Godot node, and coupling the analysis to it
/// would drag the engine into the core.
/// </para>
/// </remarks>
public static class SessionAnalysis
{
    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    /// <summary>Phases in which the hand is closed and the board is final for the wave.</summary>
    private static readonly string[] SettledPhases = ["Locked", "BustLocked"];

    /// <summary>
    /// Below this median decision time, a session was driven by a machine rather than a person.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>game/devtools/CaptureRun.cs</c> drives the controller directly, so its states close in tens of
    /// milliseconds. Before <c>--capture</c> suppressed logging, those runs wrote session files
    /// indistinguishable from real ones, and **nine synthetic sessions pooled with two real ones** into
    /// the first Milestone 5 baseline - which is how a mean hand size came to describe a script.
    /// </para>
    /// <para>
    /// The observed separation is wide and not close to arbitrary: capture runs sat at a median of
    /// <b>134-137 ms</b>, the two human sessions at <b>3890</b> and <b>9301 ms</b>. One second is an
    /// order of magnitude above the machine and a quarter of the slower human, so nothing plausible
    /// lands near it. The median rather than the mean, because a human who clicks through a locked
    /// state quickly should not read as a robot, and a robot that paused on one frame should not read
    /// as a human.
    /// </para>
    /// <para>
    /// This is a screen for logs <i>already written</i>. New capture runs do not log at all.
    /// </para>
    /// </remarks>
    private const double SyntheticMedianMillisecondCeiling = 1000.0;

    /// <summary>
    /// Reduces one session's lines. Malformed lines are skipped and counted, never thrown on.
    /// </summary>
    /// <remarks>
    /// A session log is written by a process that may have been killed by closing the window, so a
    /// truncated final line is an ordinary outcome rather than a corruption. Failing the whole
    /// reduction over it would throw away a session that is otherwise complete.
    /// </remarks>
    public static SessionMetrics Reduce(string sessionName, IEnumerable<string> lines)
    {
        ArgumentNullException.ThrowIfNull(lines);

        List<Entry> entries = [];
        int unreadable = 0;

        foreach (string line in lines)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            Entry? entry = null;

            try
            {
                entry = JsonSerializer.Deserialize<Entry>(line, Json);
            }
            catch (JsonException)
            {
                // Deliberately swallowed - see the remarks above.
            }

            if (entry?.State is null)
            {
                unreadable++;
                continue;
            }

            entries.Add(entry);
        }

        return Reduce(sessionName, entries, unreadable);
    }

    private static SessionMetrics Reduce(string sessionName, IReadOnlyList<Entry> entries, int unreadable)
    {
        string arm = entries.Count > 0 ? entries[0].State!.Arm : "unset";

        int placements = 0;
        int forcedReplacements = 0;
        int statesAtCapacity = 0;

        for (int i = 0; i < entries.Count; i++)
        {
            StateRecord state = entries[i].State!;

            if (state.Sockets.Count > 0 && state.Sockets.All(s => s.Occupied))
            {
                statesAtCapacity++;
            }

            if (entries[i].Choice != "place" || i + 1 >= entries.Count)
            {
                continue;
            }

            placements++;

            // A placement that did not widen the board replaced something. Counted by occupancy
            // rather than by comparing ranks, because two towers of the same rank are legal and a
            // rank comparison would silently miss a 7 replacing a 7 - which is precisely the case
            // rank stacking is about to make interesting.
            int before = state.Sockets.Count(s => s.Occupied);
            int after = entries[i + 1].State!.Sockets.Count(s => s.Occupied);

            if (after <= before)
            {
                forcedReplacements++;
            }
        }

        SettledWave[] settled = [.. SettledWaves(entries)];

        return new SessionMetrics
        {
            Session = sessionName,
            Arm = arm,
            States = entries.Count,
            UnreadableLines = unreadable,
            Placements = placements,
            ForcedReplacements = forcedReplacements,
            StatesAtCapacity = statesAtCapacity,
            SettledWaves = settled.Length,
            WavesWithRun = settled.Count(w => w.HasRun),
            OccupiedSocketDepths = [.. settled.SelectMany(w => w.OccupiedDepths)],
            CardCountsAtLock = [.. settled.Select(w => w.CardCount)],
            MedianDecisionMilliseconds = Median([.. entries.Select(e => (double)e.DecisionMilliseconds)]),
        };
    }

    /// <summary>The middle value, or null when there is nothing to take a middle of.</summary>
    private static double? Median(double[] values)
    {
        if (values.Length == 0)
        {
            return null;
        }

        double[] sorted = [.. values.Order()];
        int mid = sorted.Length / 2;

        return sorted.Length % 2 == 1 ? sorted[mid] : (sorted[mid - 1] + sorted[mid]) / 2.0;
    }

    /// <summary>Whether a median decision time is too fast to have come from a person.</summary>
    /// <remarks>
    /// Exposed so the report can <b>name</b> what it excluded. A reducer that silently dropped sessions
    /// would be indistinguishable from one that lost them, which is the failure this whole screen exists
    /// to correct.
    /// </remarks>
    internal static bool LooksSynthetic(double? medianMilliseconds) =>
        medianMilliseconds is { } median && median < SyntheticMedianMillisecondCeiling;

    /// <summary>
    /// One reading per wave, taken at the moment the hand closed.
    /// </summary>
    /// <remarks>
    /// Run frequency and placement depth are per <i>hand</i>, not per logged state: a player who sat
    /// on a locked board for four states would otherwise count that board four times, and a player
    /// who locked and moved on immediately would count it once. The first settled state of each wave
    /// is the board the wave actually fought with.
    /// </remarks>
    private static IEnumerable<SettledWave> SettledWaves(IReadOnlyList<Entry> entries)
    {
        bool armed = true;

        foreach (Entry entry in entries)
        {
            StateRecord state = entry.State!;
            bool isSettled = SettledPhases.Contains(state.Phase, StringComparer.Ordinal);

            if (!isSettled)
            {
                armed = true;
                continue;
            }

            if (!armed)
            {
                continue;
            }

            armed = false;

            yield return new SettledWave(
                state.Sockets.Any(s => s.Occupied && s.RunBonus > 0.0),
                [.. state.Sockets.Where(s => s.Occupied).Select(s => s.Depth)],
                state.March.CardCount);
        }
    }

    private sealed record SettledWave(bool HasRun, IReadOnlyList<double> OccupiedDepths, int CardCount);

    /// <summary>The subset of the log envelope the reduction reads.</summary>
    /// <remarks>
    /// Written by <c>game/telemetry/PlaytestLog.cs</c>. Only two fields are needed beyond the state
    /// itself, so the rest - timings, wanted moves, playback disposition, the oracle block - are
    /// left unmapped rather than mirrored into a second copy that could drift.
    /// </remarks>
    private sealed class Entry
    {
        public StateRecord? State { get; init; }

        /// <summary>What closed the offered state, or null when the session ended in it.</summary>
        public string? Choice { get; init; }

        /// <summary>How long the state was on screen. The one field that reveals who was driving.</summary>
        public long DecisionMilliseconds { get; init; }

        [JsonPropertyName("Abandoned")]
        public bool Abandoned { get; init; }
    }
}

/// <summary>One session, reduced. Counts rather than fractions, so sessions can be summed.</summary>
/// <remarks>
/// Deliberately raw: a mean of per-session fractions is not the fraction over the pooled sessions,
/// and with sessions this short the difference is large. Aggregation happens in
/// <see cref="ArmMetrics.Pool"/>, over the counts.
/// </remarks>
public sealed record SessionMetrics
{
    public required string Session { get; init; }
    public required string Arm { get; init; }
    public required int States { get; init; }

    /// <summary>Lines that could not be read. A truncated last line is ordinary; more is not.</summary>
    public required int UnreadableLines { get; init; }

    public required int Placements { get; init; }

    /// <summary>Placements that did not widen the board, so they evicted a tower.</summary>
    public required int ForcedReplacements { get; init; }

    /// <summary>Offered states in which every socket was occupied.</summary>
    public required int StatesAtCapacity { get; init; }

    /// <summary>Waves whose hand closed. A wave abandoned mid-draw is not one.</summary>
    public required int SettledWaves { get; init; }

    public required int WavesWithRun { get; init; }

    /// <summary>Depth of every occupied socket, at lock, across settled waves.</summary>
    public required IReadOnlyList<double> OccupiedSocketDepths { get; init; }

    /// <summary>Cards in hand at lock, one entry per settled wave.</summary>
    public required IReadOnlyList<int> CardCountsAtLock { get; init; }

    /// <summary>Median time a state stayed on screen. Null when the session recorded none.</summary>
    public required double? MedianDecisionMilliseconds { get; init; }

    /// <summary>
    /// Whether this session was driven by a machine and must not enter the baseline.
    /// </summary>
    /// <remarks>
    /// A capture run produces states nobody decided. Pooling them answers the question "what does
    /// <c>CaptureRun</c> do?", which nothing in the validation architecture is asking.
    /// </remarks>
    public bool IsSynthetic => SessionAnalysis.LooksSynthetic(MedianDecisionMilliseconds);
}

/// <summary>The five comparison metrics for one arm, pooled over its sessions.</summary>
public sealed record ArmMetrics
{
    public required string Arm { get; init; }
    public required int Sessions { get; init; }
    public required int States { get; init; }
    public required int SettledWaves { get; init; }

    /// <summary>Metric 1. Placements that evicted a tower, over all placements.</summary>
    public required double? ForcedReplacementFraction { get; init; }

    /// <summary>Metric 2. Not derivable with stacking off - null, never zero.</summary>
    public double? StackAtCapacityRate => null;

    /// <summary>Offered states at full occupancy. Context for metric 1, not one of the five.</summary>
    public required double? CapacityStateFraction { get; init; }

    /// <summary>Metric 3. Settled waves carrying at least one run link.</summary>
    public required double? RunFraction { get; init; }

    /// <summary>Metric 4. Mean depth of occupied sockets at lock. Larger is deeper.</summary>
    public required double? MeanPlacementDepth { get; init; }

    /// <summary>Metric 5. Mean cards at lock, and how often the hand ran to five or more.</summary>
    public required double? MeanCardsAtLock { get; init; }

    public required double? FiveOrMoreCardFraction { get; init; }

    /// <summary>Machine-driven sessions excluded from this pool.</summary>
    public required int SyntheticSessionsExcluded { get; init; }

    /// <summary>
    /// Pools one arm's sessions over counts, never by averaging fractions.
    /// </summary>
    /// <remarks>
    /// <b>Synthetic sessions are excluded here</b>, not by the caller, so that every consumer of this
    /// type gets the same answer. The count of what was dropped travels with the result rather than
    /// being discarded - an exclusion nobody can see is a silent data loss.
    /// </remarks>
    public static ArmMetrics Pool(string arm, IReadOnlyList<SessionMetrics> sessions)
    {
        ArgumentNullException.ThrowIfNull(sessions);

        SessionMetrics[] real = [.. sessions.Where(s => !s.IsSynthetic)];

        int placements = real.Sum(s => s.Placements);
        int states = real.Sum(s => s.States);
        int waves = real.Sum(s => s.SettledWaves);

        double[] depths = [.. real.SelectMany(s => s.OccupiedSocketDepths)];
        int[] cards = [.. real.SelectMany(s => s.CardCountsAtLock)];

        return new ArmMetrics
        {
            Arm = arm,
            Sessions = real.Length,
            SyntheticSessionsExcluded = sessions.Count - real.Length,
            States = states,
            SettledWaves = waves,
            ForcedReplacementFraction = Ratio(real.Sum(s => s.ForcedReplacements), placements),
            CapacityStateFraction = Ratio(real.Sum(s => s.StatesAtCapacity), states),
            RunFraction = Ratio(real.Sum(s => s.WavesWithRun), waves),
            MeanPlacementDepth = depths.Length > 0 ? depths.Average() : null,
            MeanCardsAtLock = cards.Length > 0 ? cards.Average() : null,
            FiveOrMoreCardFraction = Ratio(cards.Count(c => c >= 5), cards.Length),
        };
    }

    /// <summary>
    /// A fraction, or null when nothing was observed.
    /// </summary>
    /// <remarks>
    /// Null rather than zero throughout. Zero forced replacements out of zero placements is not a
    /// finding, and the whole point of a pre-committed reading like "forced-replacement frequency
    /// drops sharply" is that it must not be readable off a pass that never measured it.
    /// </remarks>
    private static double? Ratio(int numerator, int denominator) =>
        denominator > 0 ? (double)numerator / denominator : null;
}
