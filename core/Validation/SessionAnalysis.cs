using System.Text.Json;
using System.Text.Json.Serialization;
using Bastion.Core.Wave;

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

        // Placement states only. A placement is the decision the candidate preview serves, and
        // pooling it with hit/stand and lock states would bury exactly the number being watched
        // (docs/prototype/VALIDATION.md § Improved-encounter instrumentation).
        Entry[] placementStates =
        [
            .. entries.Where(e => e.State!.Phase == nameof(WavePhase.AwaitingPlacement)),
        ];

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
            PlacementMilliseconds = [.. placementStates.Select(e => (double)e.DecisionMilliseconds)],
            CandidateCombinationsHovered = [.. placementStates.Select(e => (double)e.CandidateCombinationsHovered)],
            CandidateRevisits = [.. placementStates.Select(e => (double)e.CandidateRevisits)],
            ExhaustiveSearchStates = placementStates.Count(LooksExhaustive),
        };
    }

    /// <summary>
    /// Whether a state had nearly the whole candidate space inspected before a card was committed.
    /// </summary>
    /// <remarks>
    /// <b>The hover-brute-force signal, made measurable rather than assumed absent</b>
    /// (docs/design/14-encounter-timeline.md § The solvable-puzzle risk). The candidate space is
    /// every socket times the two families; sweeping most of it is a player searching rather than
    /// judging. The pre-committed response if this is common is to <b>reduce sortable outputs</b> -
    /// not to hide information, and not to add a mechanic.
    /// </remarks>
    private static bool LooksExhaustive(Entry entry)
    {
        int sockets = entry.State!.Sockets.Count;

        if (sockets == 0)
        {
            return false;
        }

        // Two families per socket: Club and Spade are the whole prototype roster
        // (docs/prototype/SCOPE.md cuts Hearts and Diamonds).
        return entry.CandidateCombinationsHovered >= ExhaustiveSearchFraction * sockets * 2;
    }

    /// <summary>
    /// How much of the candidate space counts as having swept it.
    /// </summary>
    /// <remarks>
    /// A first pass with no measurement behind it, like every other number in the design. It is set
    /// below one because a player who inspects most combinations is already searching - waiting for
    /// literally all of them would only catch the most thorough version of the failure.
    /// </remarks>
    private const double ExhaustiveSearchFraction = 0.75;

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

        /// <summary>Zero on a log written before Milestone 6, which is indistinguishable from "did not look".</summary>
        public int CandidateSocketsHovered { get; init; }

        public int CandidateCombinationsHovered { get; init; }

        public int CandidateRevisits { get; init; }
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
    /// Time on screen for each placement state, kept raw so quantiles pool honestly.
    /// </summary>
    /// <remarks>
    /// The failure signal is <i>placement times explode</i>, and its response is
    /// <b>do not add decisions</b> - simplify presentation, reduce candidate forms, or make the
    /// timeline more legible (docs/prototype/VALIDATION.md § Failure signals). A median of medians
    /// would smear the tail that signal lives in, so the values travel and
    /// <see cref="ArmMetrics.Pool"/> takes the quantiles.
    /// </remarks>
    public required IReadOnlyList<double> PlacementMilliseconds { get; init; }

    /// <summary>Distinct family-and-socket combinations inspected, per placement state.</summary>
    public required IReadOnlyList<double> CandidateCombinationsHovered { get; init; }

    /// <summary>Returns to an already-inspected combination, per placement state.</summary>
    /// <remarks>
    /// Read <i>with</i> the sweep count, not instead of it. Revisiting two or three candidates is
    /// the target behaviour - "forward-left kills the Standard Bearer early; middle-right completes
    /// my run" - while sweeping the whole space is the oracle failure. High revisits with a low
    /// sweep is the encounter working.
    /// </remarks>
    public required IReadOnlyList<double> CandidateRevisits { get; init; }

    /// <summary>Placement states where nearly the whole candidate space was inspected.</summary>
    public required int ExhaustiveSearchStates { get; init; }

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

    /// <summary>Median and 90th-percentile time to place a card. The tail is the signal.</summary>
    /// <remarks>
    /// <i>Placement times explode</i> is a failure signal whose response is <b>do not add
    /// decisions</b>. It shows up in the 90th percentile long before it shows up in the median,
    /// which is why both travel.
    /// </remarks>
    public required double? MedianPlacementMilliseconds { get; init; }

    public required double? NinetiethPlacementMilliseconds { get; init; }

    /// <summary>Mean distinct family-and-socket combinations inspected per placement.</summary>
    public required double? MeanCandidatesInspected { get; init; }

    /// <summary>Mean returns to an already-inspected candidate. Comparison, not sweeping.</summary>
    public required double? MeanCandidateRevisits { get; init; }

    /// <summary>
    /// Placements where nearly the whole candidate space was swept.
    /// </summary>
    /// <remarks>
    /// The oracle measurement. If this is common the candidate preview is functioning as an oracle,
    /// and the pre-committed response is to reduce sortable outputs - never to hide information, and
    /// never to add a mechanic (docs/prototype/VALIDATION.md § Failure signals).
    /// </remarks>
    public required double? ExhaustiveSearchFraction { get; init; }

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

        // Pooled over the raw per-placement values, not over per-session summaries, for the same
        // reason the fractions are: a mean of medians is not the median, and sessions are short.
        double[] placementTimes = [.. real.SelectMany(s => s.PlacementMilliseconds)];
        double[] inspected = [.. real.SelectMany(s => s.CandidateCombinationsHovered)];
        double[] revisits = [.. real.SelectMany(s => s.CandidateRevisits)];

        return new ArmMetrics
        {
            MedianPlacementMilliseconds = Quantile(placementTimes, 0.5),
            NinetiethPlacementMilliseconds = Quantile(placementTimes, 0.9),
            MeanCandidatesInspected = inspected.Length > 0 ? inspected.Average() : null,
            MeanCandidateRevisits = revisits.Length > 0 ? revisits.Average() : null,
            ExhaustiveSearchFraction = Ratio(real.Sum(s => s.ExhaustiveSearchStates), inspected.Length),
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

    /// <summary>
    /// A quantile by nearest rank, or null when nothing was observed.
    /// </summary>
    /// <remarks>
    /// Nearest rank rather than interpolated: these are counts of milliseconds from a handful of
    /// placements, and an interpolated 90th percentile over eleven values invents a precision the
    /// sample does not have.
    /// </remarks>
    private static double? Quantile(double[] values, double fraction)
    {
        if (values.Length == 0)
        {
            return null;
        }

        double[] sorted = [.. values.Order()];
        int index = Math.Clamp((int)Math.Ceiling(fraction * sorted.Length) - 1, 0, sorted.Length - 1);

        return sorted[index];
    }
}
