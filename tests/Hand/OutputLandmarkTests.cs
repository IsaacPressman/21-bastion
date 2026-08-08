using Bastion.Core.Board;
using Bastion.Core.Cards;
using Bastion.Core.Config;
using Bastion.Core.Hand;

namespace Bastion.Core.Tests.Hand;

/// <summary>
/// The output-landmarks table from docs/design/02-blackjack-and-formation.md, reproduced exactly -
/// the Milestone 2 acceptance target - plus the signature 3+3+5+5 versus 10+6 comparison.
/// </summary>
/// <remarks>
/// These are <b>raw</b> outputs: base power times the Formation multiplier, before links and before
/// engagement (hard invariant 5 / docs/design/03-march-clock.md § Total engagement is explanatory).
/// Cards are placed in non-adjacent, non-consecutive sockets so no run bonus enters the figure - run
/// links are proven separately in <see cref="Board.RunLinksTests"/>.
/// </remarks>
public sealed class OutputLandmarkTests
{
    private static readonly TuningData Tuning = TuningLoader.LoadFromRepositoryRoot();

    private static SocketRef S(int lane, int socket) => SocketRef.InLane(lane, socket);

    private static WaveDraft Draft(params (Rank Rank, SocketRef Socket)[] placements) =>
        placements.Aggregate(WaveDraft.Empty, (d, p) => d.Place(p.Rank, Family.Club, p.Socket));

    /// <summary>Raw output = the sum of every tower's per-shot damage, before junction and engagement.</summary>
    private static double Raw(BoardState board) => board.Towers.Sum(t => t.ShotDamage);

    [Fact]
    public void Natural_ace_king_with_the_bastion_totals_24_64()
    {
        // A(11) and K(10) are consecutive, so they are split across lanes to keep the figure link-free.
        WaveDraft draft = Draft((Rank.Ace, S(0, 0)), (Rank.King, S(1, 0)));

        Assert.Equal(24.64, Raw(draft.BuildBoard(Tuning)), precision: 6);
        Assert.Equal(0.0, draft.Entry(Tuning), precision: 6);
    }

    [Fact]
    public void Five_card_21_totals_21_44_at_entry_4_5()
    {
        // 2-3-4-5 are a running sequence; placed non-adjacently so the raw figure excludes links.
        WaveDraft draft = Draft(
            (Rank.Two, S(0, 0)), (Rank.Four, S(0, 1)), (Rank.Seven, S(0, 2)),
            (Rank.Three, S(1, 0)), (Rank.Five, S(1, 1)));

        Assert.Equal(21.44, Raw(draft.BuildBoard(Tuning)), precision: 6);
        Assert.Equal(4.5, draft.Entry(Tuning), precision: 6);
    }

    [Fact]
    public void Three_card_21_totals_18_72_at_entry_0()
    {
        WaveDraft draft = Draft((Rank.Six, S(0, 0)), (Rank.Eight, S(0, 1)), (Rank.Seven, S(1, 0)));

        Assert.Equal(18.72, Raw(draft.BuildBoard(Tuning)), precision: 6);
        Assert.Equal(0.0, draft.Entry(Tuning), precision: 6);
    }

    [Fact]
    public void Four_card_20_totals_18_00_at_entry_4()
    {
        WaveDraft draft = Draft(
            (Rank.Two, S(0, 0)), (Rank.Four, S(0, 1)), (Rank.Six, S(0, 2)), (Rank.Eight, S(1, 0)));

        Assert.Equal(18.00, Raw(draft.BuildBoard(Tuning)), precision: 6);
        Assert.Equal(4.0, draft.Entry(Tuning), precision: 6);
    }

    [Fact]
    public void King_queen_20_totals_15_00()
    {
        // Split across lanes: the Queen is wild, so beside the King she would bridge into a 10-run.
        WaveDraft draft = Draft((Rank.King, S(0, 0)), (Rank.Queen, S(1, 0)));

        Assert.Equal(15.00, Raw(draft.BuildBoard(Tuning)), precision: 6);
        Assert.Equal(0.0, draft.Entry(Tuning), precision: 6);
    }

    [Fact]
    public void Three_card_18_totals_13_52_at_entry_1_5()
    {
        WaveDraft draft = Draft((Rank.Six, S(0, 0)), (Rank.Eight, S(0, 1)), (Rank.Four, S(0, 2)));

        Assert.Equal(13.52, Raw(draft.BuildBoard(Tuning)), precision: 6);
        Assert.Equal(1.5, draft.Entry(Tuning), precision: 6);
    }

    [Fact]
    public void The_signature_pair_ten_six_and_three_three_five_five_fail_differently()
    {
        // Two hands both totalling 16, one with 25% more raw output, the other placed four cards deep
        // and four units further back - "they should never play the same" (docs/design/02).
        WaveDraft tenSix = Draft((Rank.Ten, S(0, 0)), (Rank.Six, S(0, 1)));
        WaveDraft threes = Draft(
            (Rank.Three, S(0, 0)), (Rank.Three, S(0, 1)), (Rank.Five, S(1, 0)), (Rank.Five, S(1, 1)));

        BoardState tenSixBoard = tenSix.BuildBoard(Tuning);
        BoardState threesBoard = threes.BuildBoard(Tuning);

        // 8.5 x 1.15 and 10.6 x 1.15.
        Assert.Equal(9.775, Raw(tenSixBoard), precision: 6);
        Assert.Equal(12.19, Raw(threesBoard), precision: 6);

        // Visibly different boards: more towers, more raw output, and four units deeper entry.
        Assert.Equal(2, tenSixBoard.Towers.Count);
        Assert.Equal(4, threesBoard.Towers.Count);
        Assert.Equal(0.0, tenSix.Entry(Tuning), precision: 6);
        Assert.Equal(4.0, threes.Entry(Tuning), precision: 6);
    }
}
