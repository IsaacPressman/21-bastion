using Bastion.Core.Board;
using Bastion.Core.Cards;
using Bastion.Core.Config;
using Bastion.Core.Hand;

namespace Bastion.Core.Tests.Hand;

/// <summary>
/// The placement producer: family locking, forced replacement, the March Clock entry, the Ace's
/// mid-hand transformation on the field, bust destroying the card, and the natural's Ace Bastion.
/// </summary>
public sealed class WaveDraftTests
{
    private static readonly TuningData Tuning = TuningLoader.LoadFromRepositoryRoot();

    private static SocketRef Lane0(int socket) => SocketRef.InLane(0, socket);
    private static SocketRef Lane1(int socket) => SocketRef.InLane(1, socket);

    private static TowerState TowerAt(BoardState board, SocketRef socket) =>
        board.Towers.Single(t => t.Socket == socket);

    [Fact]
    public void A_placement_commits_its_family_socket_and_power()
    {
        // The worked example's opening: 6 and 8, both Clubs, at lane one's sockets.
        BoardState board = WaveDraft.Empty
            .Place(Rank.Six, Family.Club, Lane0(1))
            .Place(Rank.Eight, Family.Club, Lane0(2))
            .BuildBoard(Tuning);

        Assert.Equal(2, board.Towers.Count);
        Assert.Equal(Family.Club, TowerAt(board, Lane0(1)).Family);
        Assert.Equal(Tuning.CardPower.ForValue(6), TowerAt(board, Lane0(1)).BasePower, precision: 6);
        Assert.Equal(Tuning.CardPower.ForValue(8), TowerAt(board, Lane0(2)).BasePower, precision: 6);
    }

    [Fact]
    public void Placing_onto_an_occupied_socket_replaces_the_tower()
    {
        WaveDraft draft = WaveDraft.Empty
            .Place(Rank.Ace, Family.Club, Lane0(0))
            .Place(Rank.Two, Family.Spade, Lane0(1))
            .Place(Rank.Five, Family.Spade, Lane0(0));    // replaces the Ace's tower

        BoardState board = draft.BuildBoard(Tuning);

        Assert.Equal(2, board.Towers.Count);                               // still two towers
        Assert.Equal(Rank.Five, TowerAt(board, Lane0(0)).Card.Rank);       // the Ace's tower is gone
        Assert.Equal(3, draft.Hand.CardCount);                            // but the Ace still counts in the hand
    }

    [Theory]
    [InlineData(2, false, 0.0)]    // opening two are free
    [InlineData(3, false, 1.5)]    // arm C third step
    [InlineData(3, true, 0.0)]     // a three-card 21 pulls fully back
    public void Entry_follows_the_march_clock(int cards, bool reach21, double expected)
    {
        WaveDraft draft = WaveDraft.Empty;
        // Build a hand of the requested length that does or does not hit 21.
        Rank[] ranks = (cards, reach21) switch
        {
            (2, false) => [Rank.Six, Rank.Eight],
            (3, false) => [Rank.Six, Rank.Eight, Rank.Two],
            (3, true) => [Rank.Seven, Rank.Six, Rank.Eight],
            _ => throw new ArgumentException("unhandled case"),
        };

        int socket = 0;
        foreach (Rank rank in ranks)
        {
            draft = draft.Place(rank, Family.Club, socket < 3 ? Lane0(socket) : Lane1(socket - 3));
            socket++;
        }

        Assert.Equal(expected, draft.Entry(Tuning), precision: 6);
    }

    [Fact]
    public void A_natural_earns_a_shared_multiplier_ace_bastion()
    {
        BoardState board = WaveDraft.Empty
            .Place(Rank.Ace, Family.Club, Lane0(0))
            .Place(Rank.King, Family.Club, Lane0(1))
            .BuildBoard(Tuning);

        Assert.Equal(3, board.Towers.Count);   // two hand towers plus the free anchor

        TowerState bastion = TowerAt(board, SocketRef.Junction);
        Assert.Equal(Tuning.AceBastion.Power, bastion.BasePower, precision: 6);
        Assert.Equal(1.60, bastion.FormationMultiplier, precision: 6);   // shares the 21 multiplier
    }

    [Fact]
    public void A_non_natural_twenty_one_earns_no_bastion()
    {
        // Three-card 21: the pullback is the whole bonus, there is no anchor.
        BoardState board = WaveDraft.Empty
            .Place(Rank.Seven, Family.Club, Lane0(0))
            .Place(Rank.Six, Family.Club, Lane0(1))
            .Place(Rank.Eight, Family.Club, Lane0(2))
            .BuildBoard(Tuning);

        Assert.Equal(3, board.Towers.Count);
        Assert.DoesNotContain(board.Towers, t => t.Socket == SocketRef.Junction);
    }

    [Fact]
    public void An_ace_tower_flips_from_anchor_to_utility_when_the_hand_forces_it_down()
    {
        // Soft 20: the Ace is the 5.4 anchor.
        WaveDraft soft = WaveDraft.Empty
            .Place(Rank.Ace, Family.Club, Lane0(0))
            .Place(Rank.Nine, Family.Club, Lane0(1));

        Assert.Equal(Tuning.CardPower.ForValue(11), TowerAt(soft.BuildBoard(Tuning), Lane0(0)).BasePower, precision: 6);

        // Hit a 5 to hard 15: the same tower is now the 1.0 utility, immediately.
        WaveDraft hard = soft.Place(Rank.Five, Family.Club, Lane0(2));

        Assert.Equal(Tuning.CardPower.ForValue(1), TowerAt(hard.BuildBoard(Tuning), Lane0(0)).BasePower, precision: 6);
    }

    [Fact]
    public void A_busting_card_is_destroyed_and_the_board_runs_at_the_bust_multiplier()
    {
        WaveDraft draft = WaveDraft.Empty
            .Place(Rank.Ten, Family.Club, Lane0(0))
            .Place(Rank.Eight, Family.Club, Lane0(1))    // hard 18
            .Place(Rank.King, Family.Club, Lane0(2));    // 28 - busts, King destroyed

        BoardState board = draft.BuildBoard(Tuning);

        Assert.True(draft.Hand.IsBust);
        Assert.Equal(2, board.Towers.Count);             // the King never took a socket
        Assert.All(board.Towers, t => Assert.Equal(Tuning.FormationStrength.Bust, t.FormationMultiplier, precision: 6));
    }
}
