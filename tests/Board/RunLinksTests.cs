using Bastion.Core.Board;
using Bastion.Core.Cards;
using Bastion.Core.Config;

namespace Bastion.Core.Tests.Board;

/// <summary>
/// Run-link detection against every rule in docs/design/04-cards-as-defenses.md § Run links:
/// adjacency, direction-agnosticism, the Queen's wildness and guard, Ace state, the junction
/// island, no cross-lane runs, one run per tower, and the equal-length tie-break.
/// </summary>
public sealed class RunLinksTests
{
    private static readonly TuningData Tuning = TuningLoader.LoadFromRepositoryRoot();
    private static readonly double Two = Tuning.RunLinks.BonusForRunLength(2);    // 0.15
    private static readonly double Three = Tuning.RunLinks.BonusForRunLength(3);  // 0.25

    private static IReadOnlyDictionary<SocketRef, double> Detect(params (Rank Rank, bool AceHigh, int Lane, int Socket)[] towers) =>
        RunLinks.BonusBySocket(
            Tuning,
            [.. towers.Select(t => (new Card(t.Rank, t.AceHigh), SocketRef.InLane(t.Lane, t.Socket)))]);

    private static (Rank, bool, int, int) At(Rank rank, int socket, int lane = 0) => (rank, false, lane, socket);

    private static double Bonus(IReadOnlyDictionary<SocketRef, double> runs, int socket, int lane = 0) =>
        runs.GetValueOrDefault(SocketRef.InLane(lane, socket), 0.0);

    [Fact]
    public void Consecutive_values_in_adjacent_sockets_form_a_two_run()
    {
        var runs = Detect(At(Rank.Five, 0), At(Rank.Six, 1));

        Assert.Equal(Two, Bonus(runs, 0), precision: 6);
        Assert.Equal(Two, Bonus(runs, 1), precision: 6);
    }

    [Fact]
    public void Three_consecutive_values_form_a_three_run()
    {
        var runs = Detect(At(Rank.Four, 0), At(Rank.Five, 1), At(Rank.Six, 2));

        Assert.Equal(Three, Bonus(runs, 0), precision: 6);
        Assert.Equal(Three, Bonus(runs, 1), precision: 6);
        Assert.Equal(Three, Bonus(runs, 2), precision: 6);
    }

    [Fact]
    public void Direction_does_not_matter()
    {
        var runs = Detect(At(Rank.Six, 0), At(Rank.Five, 1), At(Rank.Four, 2));

        Assert.Equal(Three, Bonus(runs, 0), precision: 6);
        Assert.Equal(Three, Bonus(runs, 2), precision: 6);
    }

    [Fact]
    public void A_gap_breaks_adjacency()
    {
        // 3-6 and 6-9 are adjacent; 3-9 is not. Sockets 0 and 2 with 1 empty do not link.
        var runs = Detect(At(Rank.Five, 0), At(Rank.Six, 2));

        Assert.Equal(0.0, Bonus(runs, 0), precision: 6);
        Assert.Equal(0.0, Bonus(runs, 2), precision: 6);
    }

    [Fact]
    public void Non_consecutive_values_do_not_link()
    {
        var runs = Detect(At(Rank.Four, 0), At(Rank.Six, 1));

        Assert.Empty(runs);
    }

    [Fact]
    public void Face_cards_cannot_run_with_each_other()
    {
        // All value 10, so never consecutive - they buy their edge another way.
        var runs = Detect(At(Rank.King, 0), At(Rank.Jack, 1));

        Assert.Empty(runs);
    }

    [Fact]
    public void The_five_six_five_case_splits_and_the_forward_run_wins()
    {
        // The worked example: 5-6-5 across sockets 0/1/2 yields 5-6 and 6-5, both 2-runs sharing the
        // middle. The run at the lower sockets wins; the trailing five is unlinked.
        var runs = Detect(At(Rank.Five, 0), At(Rank.Six, 1), At(Rank.Five, 2));

        Assert.Equal(Two, Bonus(runs, 0), precision: 6);
        Assert.Equal(Two, Bonus(runs, 1), precision: 6);
        Assert.Equal(0.0, Bonus(runs, 2), precision: 6);
    }

    [Fact]
    public void A_queen_bridges_a_gap_in_the_sequence()
    {
        // 4-Q-6 resolves as 4-5-6, a full 3-run. That bridging case is the whole reason she is wild.
        var runs = Detect(At(Rank.Four, 0), At(Rank.Queen, 1), At(Rank.Six, 2));

        Assert.Equal(Three, Bonus(runs, 0), precision: 6);
        Assert.Equal(Three, Bonus(runs, 1), precision: 6);
        Assert.Equal(Three, Bonus(runs, 2), precision: 6);
    }

    [Fact]
    public void A_run_may_not_be_made_of_queens_alone()
    {
        // Two adjacent Queens could take 5 and 6 and form a run out of nothing. The guard forbids it.
        var runs = Detect(At(Rank.Queen, 0), At(Rank.Queen, 1));

        Assert.Empty(runs);
    }

    [Fact]
    public void A_low_ace_runs_with_a_two()
    {
        var runs = Detect((Rank.Ace, false, 0, 0), At(Rank.Two, 1));

        Assert.Equal(Two, Bonus(runs, 0), precision: 6);
        Assert.Equal(Two, Bonus(runs, 1), precision: 6);
    }

    [Fact]
    public void A_high_ace_runs_with_a_ten_value_card()
    {
        // Ace at 11 is consecutive with a 10-value card: K-A reads as 10-11.
        var runs = Detect(At(Rank.King, 0), (Rank.Ace, true, 0, 1));

        Assert.Equal(Two, Bonus(runs, 0), precision: 6);
        Assert.Equal(Two, Bonus(runs, 1), precision: 6);
    }

    [Fact]
    public void The_junction_never_joins_a_run()
    {
        var runs = RunLinks.BonusBySocket(Tuning,
        [
            (new Card(Rank.Five), SocketRef.InLane(0, 0)),
            (new Card(Rank.Six), SocketRef.InLane(0, 1)),
            (new Card(Rank.Seven), SocketRef.Junction),
        ]);

        Assert.Equal(Two, Bonus(runs, 0), precision: 6);
        Assert.Equal(Two, Bonus(runs, 1), precision: 6);
        Assert.Equal(0.0, runs.GetValueOrDefault(SocketRef.Junction, 0.0), precision: 6);
    }

    [Fact]
    public void Runs_do_not_span_lanes()
    {
        // Matching depth in different lanes is not adjacency - it would reward splitting coverage.
        var runs = Detect(At(Rank.Five, 0, lane: 0), At(Rank.Six, 0, lane: 1));

        Assert.Empty(runs);
    }

    [Fact]
    public void Separate_lanes_each_score_their_own_run()
    {
        var runs = Detect(
            At(Rank.Five, 0, lane: 0), At(Rank.Six, 1, lane: 0),
            At(Rank.Eight, 0, lane: 1), At(Rank.Nine, 1, lane: 1));

        Assert.Equal(Two, Bonus(runs, 0, lane: 0), precision: 6);
        Assert.Equal(Two, Bonus(runs, 1, lane: 0), precision: 6);
        Assert.Equal(Two, Bonus(runs, 0, lane: 1), precision: 6);
        Assert.Equal(Two, Bonus(runs, 1, lane: 1), precision: 6);
    }
}
