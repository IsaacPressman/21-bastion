using System.Globalization;
using Bastion.Core.Validation;

namespace Bastion.Core.Tests.Validation;

/// <summary>
/// The session-log reduction behind the Milestone 5 baseline.
/// </summary>
/// <remarks>
/// Driven by synthetic log lines rather than by recorded sessions: the sessions are gitignored, and
/// a reducer tested only against whatever happens to be on one machine cannot be trusted to mean the
/// same thing when the stacking pass re-runs it (docs/ROADMAP.md § Milestone 6).
/// </remarks>
public sealed class SessionAnalysisTests
{
    [Fact]
    public void A_placement_that_does_not_widen_the_board_is_a_forced_replacement()
    {
        // Three towers before, three after: the card went onto an occupied socket.
        string[] lines =
        [
            Line(Phase: "AwaitingPlacement", Choice: "place", Occupied: 3),
            Line(Phase: "DrawDecision", Choice: "stand", Occupied: 3),
        ];

        SessionMetrics metrics = SessionAnalysis.Reduce("synthetic", lines);

        Assert.Equal(1, metrics.Placements);
        Assert.Equal(1, metrics.ForcedReplacements);
    }

    [Fact]
    public void A_placement_that_widens_the_board_is_not_a_replacement()
    {
        string[] lines =
        [
            Line(Phase: "AwaitingPlacement", Choice: "place", Occupied: 2),
            Line(Phase: "DrawDecision", Choice: "stand", Occupied: 3),
        ];

        SessionMetrics metrics = SessionAnalysis.Reduce("synthetic", lines);

        Assert.Equal(1, metrics.Placements);
        Assert.Equal(0, metrics.ForcedReplacements);
    }

    [Fact]
    public void Same_rank_replacing_same_rank_still_counts()
    {
        // The case a rank comparison would miss, and the one rank stacking makes interesting.
        string[] lines =
        [
            Line(Phase: "AwaitingPlacement", Choice: "place", Occupied: 4, Rank: "7"),
            Line(Phase: "DrawDecision", Choice: "stand", Occupied: 4, Rank: "7"),
        ];

        Assert.Equal(1, SessionAnalysis.Reduce("synthetic", lines).ForcedReplacements);
    }

    [Fact]
    public void Capacity_is_every_socket_occupied()
    {
        string[] lines =
        [
            Line(Phase: "DrawDecision", Choice: "hit", Occupied: 7),
            Line(Phase: "DrawDecision", Choice: "stand", Occupied: 6),
        ];

        Assert.Equal(1, SessionAnalysis.Reduce("synthetic", lines).StatesAtCapacity);
    }

    [Fact]
    public void A_wave_is_read_once_at_the_moment_its_hand_closed()
    {
        // Two logged states in the same locked wave must not count the board twice.
        string[] lines =
        [
            Line(Phase: "Locked", Choice: "advance", Occupied: 2, RunBonus: 0.2, CardCount: 4),
            Line(Phase: "Locked", Choice: "next-wave", Occupied: 2, RunBonus: 0.2, CardCount: 4),
            Line(Phase: "AwaitingPlacement", Choice: "place", Occupied: 0, CardCount: 0),
            Line(Phase: "BustLocked", Choice: null, Occupied: 3, CardCount: 5),
        ];

        SessionMetrics metrics = SessionAnalysis.Reduce("synthetic", lines);

        Assert.Equal(2, metrics.SettledWaves);
        Assert.Equal(1, metrics.WavesWithRun);
        Assert.Equal([4, 5], metrics.CardCountsAtLock);
    }

    [Fact]
    public void A_truncated_final_line_is_counted_not_thrown_on()
    {
        string[] lines =
        [
            Line(Phase: "DrawDecision", Choice: "stand", Occupied: 1),
            "{\"Sequence\":1,\"State\":{\"Arm\":\"C\"",
        ];

        SessionMetrics metrics = SessionAnalysis.Reduce("synthetic", lines);

        Assert.Equal(1, metrics.States);
        Assert.Equal(1, metrics.UnreadableLines);
    }

    [Fact]
    public void An_arm_that_was_never_played_reports_absent_metrics_not_zero()
    {
        ArmMetrics metrics = ArmMetrics.Pool("B", []);

        Assert.Null(metrics.ForcedReplacementFraction);
        Assert.Null(metrics.RunFraction);
        Assert.Null(metrics.MeanPlacementDepth);
        Assert.Null(metrics.MeanCardsAtLock);
    }

    [Fact]
    public void Stack_at_capacity_is_absent_with_stacking_off()
    {
        // Not zero. With the flag off there are no stacks to count, and a zero here would let an
        // unmeasured pass read as a measured one against VALIDATION.md's pre-committed reading.
        Assert.Null(ArmMetrics.Pool("C", []).StackAtCapacityRate);
    }

    [Fact]
    public void A_machine_driven_session_is_flagged_as_synthetic()
    {
        // The signature of a capture run: every state closes in tens of milliseconds. The real ones
        // sat at 134-137 ms median against 3890 and 9301 ms for the two human sessions.
        string[] lines =
        [
            Line(Phase: "AwaitingPlacement", Choice: "place", Occupied: 0, DecisionMilliseconds: 41),
            Line(Phase: "AwaitingPlacement", Choice: "place", Occupied: 1, DecisionMilliseconds: 125),
            Line(Phase: "DrawDecision", Choice: "stand", Occupied: 2, DecisionMilliseconds: 137),
        ];

        SessionMetrics metrics = SessionAnalysis.Reduce("capture", lines);

        Assert.True(metrics.IsSynthetic);
        Assert.Equal(125, metrics.MedianDecisionMilliseconds);
    }

    [Fact]
    public void A_human_session_is_not_flagged_even_with_some_fast_clicks()
    {
        // A player clicking through a locked state quickly must not read as a robot, which is why the
        // screen is on the median rather than the minimum or the mean.
        string[] lines =
        [
            Line(Phase: "Locked", Choice: "advance", Occupied: 2, DecisionMilliseconds: 200),
            Line(Phase: "AwaitingPlacement", Choice: "place", Occupied: 0, DecisionMilliseconds: 14918),
            Line(Phase: "DrawDecision", Choice: "stand", Occupied: 1, DecisionMilliseconds: 3890),
        ];

        Assert.False(SessionAnalysis.Reduce("human", lines).IsSynthetic);
    }

    [Fact]
    public void Synthetic_sessions_are_excluded_from_the_pool_and_counted()
    {
        SessionMetrics robot = SessionAnalysis.Reduce("capture",
        [
            Line(Phase: "AwaitingPlacement", Choice: "place", Occupied: 3, DecisionMilliseconds: 40),
            Line(Phase: "DrawDecision", Choice: "stand", Occupied: 3, DecisionMilliseconds: 137),
        ]);

        SessionMetrics human = SessionAnalysis.Reduce("human",
        [
            Line(Phase: "AwaitingPlacement", Choice: "place", Occupied: 0, DecisionMilliseconds: 8000),
            Line(Phase: "DrawDecision", Choice: "stand", Occupied: 1, DecisionMilliseconds: 4000),
        ]);

        ArmMetrics pooled = ArmMetrics.Pool("C", [robot, human]);

        // The robot's forced replacement must not reach the baseline - pooled would be 1/2 with it.
        Assert.Equal(1, pooled.Sessions);
        Assert.Equal(1, pooled.SyntheticSessionsExcluded);
        Assert.Equal(0.0, pooled.ForcedReplacementFraction!.Value, 6);
    }

    [Fact]
    public void An_arm_of_only_machine_sessions_reports_absent_metrics_not_zero()
    {
        // The failure this screen exists to prevent: nine capture runs reading as a played arm.
        SessionMetrics robot = SessionAnalysis.Reduce("capture",
        [
            Line(Phase: "Locked", Choice: "advance", Occupied: 2, DecisionMilliseconds: 130),
        ]);

        ArmMetrics pooled = ArmMetrics.Pool("A", [robot]);

        Assert.Equal(0, pooled.Sessions);
        Assert.Null(pooled.RunFraction);
        Assert.Null(pooled.MeanPlacementDepth);
    }

    [Fact]
    public void Pooling_is_over_counts_not_over_per_session_fractions()
    {
        // One session replaced on its only placement, the other on none of three. Pooled, that is
        // 1/4; a mean of the two fractions would say 1/2.
        SessionMetrics heavy = SessionAnalysis.Reduce("heavy",
        [
            Line(Phase: "AwaitingPlacement", Choice: "place", Occupied: 3),
            Line(Phase: "DrawDecision", Choice: "stand", Occupied: 3),
        ]);

        SessionMetrics light = SessionAnalysis.Reduce("light",
        [
            Line(Phase: "AwaitingPlacement", Choice: "place", Occupied: 0),
            Line(Phase: "AwaitingPlacement", Choice: "place", Occupied: 1),
            Line(Phase: "AwaitingPlacement", Choice: "place", Occupied: 2),
            Line(Phase: "DrawDecision", Choice: "stand", Occupied: 3),
        ]);

        ArmMetrics pooled = ArmMetrics.Pool("C", [heavy, light]);

        Assert.Equal(0.25, pooled.ForcedReplacementFraction!.Value, 6);
    }

    /// <summary>
    /// One log line, with only the fields the reduction reads varied.
    /// </summary>
    /// <remarks>
    /// Built as text rather than by serializing the writer's own type, so the test pins the reducer
    /// against the <b>on-disk format</b>. A shared type would keep passing if both sides changed
    /// together, which is the one thing that must not happen to logs already written.
    /// </remarks>
    [Fact]
    public void Candidate_inspection_is_measured_over_placement_states_only()
    {
        // The candidate preview serves the placement decision. Pooling hit/stand and lock states in
        // would divide the number being watched by however many other states a session happened to
        // produce, which is exactly how a signal gets buried.
        string[] lines =
        [
            Line(Phase: "AwaitingPlacement", Choice: "place", Occupied: 2, Inspected: 4, Revisits: 2),
            Line(Phase: "DrawDecision", Choice: "stand", Occupied: 3, Inspected: 0),
            Line(Phase: "AdjustmentWindow", Choice: "lock", Occupied: 3, Inspected: 0),
        ];

        SessionMetrics metrics = SessionAnalysis.Reduce("synthetic", lines);

        Assert.Equal([4.0], metrics.CandidateCombinationsHovered);
        Assert.Equal([2.0], metrics.CandidateRevisits);
        Assert.Single(metrics.PlacementMilliseconds);
    }

    [Fact]
    public void Sweeping_nearly_the_whole_candidate_space_is_flagged_as_a_search()
    {
        // The oracle measurement. Seven sockets and two families is fourteen combinations; inspecting
        // eleven of them is a player searching rather than judging, and the pre-committed response is
        // to reduce sortable outputs - not to hide information, and not to add a mechanic.
        string[] lines =
        [
            Line(Phase: "AwaitingPlacement", Choice: "place", Occupied: 2, Inspected: 11),
            Line(Phase: "AwaitingPlacement", Choice: "place", Occupied: 3, Inspected: 3),
        ];

        SessionMetrics metrics = SessionAnalysis.Reduce("synthetic", lines);

        Assert.Equal(1, metrics.ExhaustiveSearchStates);
        Assert.Equal(0.5, ArmMetrics.Pool("C", [metrics]).ExhaustiveSearchFraction);
    }

    [Fact]
    public void A_log_written_before_the_hover_counts_existed_still_reduces()
    {
        // StateRecord and the log entry are a schema, and Milestone 5 sessions still answer the
        // March-arm question (docs/ROADMAP.md § Milestone 9). A field added later must read as absent
        // rather than make the whole line unreadable.
        string[] lines =
        [
            "{\"Sequence\":0,\"OfferedAtUtc\":\"2026-08-10T00:00:00Z\",\"State\":{" +
            "\"Arm\":\"C\",\"Phase\":\"DrawDecision\"," +
            "\"Hand\":{\"Cards\":[],\"Total\":17,\"IsSoft\":false,\"AcesHigh\":0,\"IsBust\":false,\"FormationMultiplier\":1.0}," +
            "\"Pile\":[],\"March\":{\"Entry\":0,\"NextStepCost\":1.5,\"CardCount\":2}," +
            "\"Sockets\":[],\"Lanes\":[],\"LaneReading\":\"none\"," +
            "\"Dealer\":{\"Upcard\":\"K\",\"UpcardUnit\":\"siege_engine\",\"VanguardLane\":0}," +
            "\"PendingRanks\":[],\"MoveSpent\":false},\"Choice\":\"stand\"," +
            "\"DecisionMilliseconds\":5000,\"Abandoned\":false}",
        ];

        SessionMetrics metrics = SessionAnalysis.Reduce("legacy", lines);

        Assert.Equal(0, metrics.UnreadableLines);
        Assert.Equal(1, metrics.States);
    }

    private static string Line(
        string Phase,
        string? Choice,
        int Occupied,
        double RunBonus = 0.0,
        int CardCount = 2,
        string Rank = "9",
        long DecisionMilliseconds = 5000,
        int Inspected = 0,
        int Revisits = 0)
    {
        string sockets = string.Join(",", Enumerable.Range(0, 7).Select(i =>
        {
            bool occupied = i < Occupied;
            string rank = occupied ? $",\"Rank\":\"{Rank}\",\"Family\":\"Club\"" : string.Empty;
            double bonus = occupied ? RunBonus : 0.0;

            return string.Create(CultureInfo.InvariantCulture,
                $"{{\"Socket\":\"S{i}\",\"Depth\":{3 + i},\"Range\":3,\"Occupied\":{(occupied ? "true" : "false")}" +
                $"{rank},\"RunBonus\":{bonus},\"WindowRemaining\":5,\"WindowAfterNextStep\":4}}");
        }));

        string choice = Choice is null ? string.Empty : $",\"Choice\":\"{Choice}\"";

        return string.Create(CultureInfo.InvariantCulture,
            $"{{\"Sequence\":0,\"OfferedAtUtc\":\"2026-08-10T00:00:00Z\",\"State\":{{" +
            $"\"Arm\":\"C\",\"Phase\":\"{Phase}\"," +
            $"\"Hand\":{{\"Cards\":[],\"Total\":17,\"IsSoft\":false,\"AcesHigh\":0,\"IsBust\":false,\"FormationMultiplier\":1.0}}," +
            $"\"Pile\":[],\"March\":{{\"Entry\":0,\"NextStepCost\":1.5,\"CardCount\":{CardCount}}}," +
            $"\"Sockets\":[{sockets}],\"Lanes\":[],\"LaneReading\":\"none\"," +
            $"\"Dealer\":{{\"Upcard\":\"K\",\"UpcardUnit\":\"siege_engine\",\"VanguardLane\":0}}," +
            $"\"PendingRanks\":[],\"MoveSpent\":false}}{choice}," +
            $"\"DecisionMilliseconds\":{DecisionMilliseconds},\"Abandoned\":false," +
            $"\"CandidateSocketsHovered\":{Inspected},\"CandidateCombinationsHovered\":{Inspected}," +
            $"\"CandidateRevisits\":{Revisits}}}");
    }
}
