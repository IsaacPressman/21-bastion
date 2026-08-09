using Bastion.Core.Board;
using Bastion.Core.Cards;
using Bastion.Core.Config;
using Bastion.Core.Dealer;
using Bastion.Core.Hand;
using Bastion.Core.Wave;

namespace Bastion.Core.Tests.Wave;

/// <summary>
/// The wave loop's mechanics: the Dealer's draw policy, the phase boundaries, the single adjustment
/// move, and persistence across a wave boundary.
/// </summary>
public sealed class WaveSessionTests
{
    private static readonly TuningData Tuning = TuningLoader.LoadFromRepositoryRoot();
    private static EncounterTuning Encounter => Tuning.Encounter("example_wave");

    private static SocketRef Bastion(int socket) => SocketRef.InLane(0, socket);
    private static SocketRef Vault(int socket) => SocketRef.InLane(1, socket);

    private static WaveSession Begin(params Rank[] shoe) =>
        WaveSession.Begin(Tuning, Encounter, Shoe.FromOrder(shoe));

    [Fact]
    public void The_dealer_hits_a_hard_sixteen_and_stands_on_the_result()
    {
        // 10 and 6 is a hard 16; the Dealer draws once and, at 18, stands.
        (IReadOnlyList<Card> cards, _) = DealerHand.Resolve(Tuning, Rank.Ten, Rank.Six, Shoe.FromOrder([Rank.Two]));

        Assert.Equal([Rank.Ten, Rank.Six, Rank.Two], cards.Select(c => c.Rank));
    }

    [Fact]
    public void The_dealer_stands_on_a_soft_seventeen()
    {
        // Ace and 6 is a soft 17: stands on all 17s means no draw, and the Ace is held high.
        (IReadOnlyList<Card> cards, _) = DealerHand.Resolve(Tuning, Rank.Ace, Rank.Six, Shoe.FromOrder([Rank.Ten]));

        Assert.Equal([Rank.Ace, Rank.Six], cards.Select(c => c.Rank));
        Assert.True(cards[0].AceHigh);
    }

    [Fact]
    public void A_dealer_ace_forced_low_deploys_the_scout_not_the_elite()
    {
        // King, 6, then a drawn Ace: 16 plus an Ace held low is 17, so the Ace is a 1 - a scout.
        (IReadOnlyList<Card> cards, _) = DealerHand.Resolve(Tuning, Rank.King, Rank.Six, Shoe.FromOrder([Rank.Ace]));

        Card ace = cards.Single(c => c.Rank == Rank.Ace);
        Assert.False(ace.AceHigh);
        Assert.Equal("herald_scout", DealerDeployment.UnitFor(Tuning, ace));
    }

    [Fact]
    public void Placing_walks_the_opening_two_then_opens_the_decision()
    {
        WaveSession opened = Begin(Rank.Ten, Rank.Five, Rank.Six, Rank.Eight);

        Assert.Equal(WavePhase.AwaitingPlacement, opened.Phase);

        WaveSession one = opened.Place(Family.Club, Bastion(0));
        Assert.Equal(WavePhase.AwaitingPlacement, one.Phase);   // one opening card still to place

        WaveSession two = one.Place(Family.Club, Bastion(1));
        Assert.Equal(WavePhase.DrawDecision, two.Phase);        // both placed: hit or stand
    }

    [Fact]
    public void The_phase_boundaries_are_enforced()
    {
        // Dealer 10 + 7 is a hard 17: it stands, so no draw card is needed in the shoe.
        WaveSession opened = Begin(Rank.Ten, Rank.Seven, Rank.Six, Rank.Eight);

        // A hit is not offered while a card waits to be placed.
        Assert.Throws<InvalidOperationException>(() => opened.Hit());

        WaveSession decision = opened.Place(Family.Club, Bastion(0)).Place(Family.Club, Bastion(1));

        // No Final Forecast before the Dealer resolves, and no adjustment before the stand.
        Assert.Throws<InvalidOperationException>(() => decision.Forecast());
        Assert.Throws<InvalidOperationException>(() => decision.RelocateTower(Bastion(0), Bastion(2)));

        // The Visible Threat is a draw-decision reading, not an adjustment-window one.
        WaveSession adjusting = decision.Stand();
        Assert.Throws<InvalidOperationException>(() => adjusting.VisibleThreatNow());
    }

    [Fact]
    public void A_swap_exchanges_two_adjacent_towers_and_spends_the_move()
    {
        WaveSession adjusting = Begin(Rank.Ten, Rank.Seven, Rank.Six, Rank.Eight)
            .Place(Family.Club, Bastion(0))
            .Place(Family.Spade, Bastion(1))
            .Stand();

        WaveSession swapped = adjusting.SwapTowers(Bastion(0), Bastion(1));

        Assert.True(swapped.MoveSpent);

        // The 6-Club now sits where the 8-Spade was, and vice versa; families travel with them.
        TowerState atOne = swapped.Board().Towers.Single(t => t.Socket == Bastion(1));
        Assert.Equal(Rank.Six, atOne.Card.Rank);
        Assert.Equal(Family.Club, atOne.Family);

        // A non-adjacent swap is refused even before the move is spent.
        Assert.Throws<InvalidOperationException>(() => adjusting.SwapTowers(Bastion(0), Bastion(2)));
    }

    [Fact]
    public void Persisted_towers_carry_forward_at_one_and_seat_beside_the_new_hand()
    {
        // Wave one: two Clubs in the Bastion lane at hard 14 (×1.05), then lock.
        WaveSession wave1 = Begin(Rank.Ten, Rank.Seven, Rank.Six, Rank.Eight)
            .Place(Family.Club, Bastion(1))
            .Place(Family.Club, Bastion(2))
            .Stand()
            .Lock();

        (IReadOnlyList<TowerState> persisted, Shoe shoe2) = wave1.Settle();

        Assert.Equal(2, persisted.Count);
        Assert.All(persisted, t => Assert.Equal(Tuning.FormationStrength.Persisted, t.FormationMultiplier, precision: 6));
        Assert.All(persisted, t => Assert.Equal(1.00, t.FormationMultiplier, precision: 6));

        // Wave two: a fresh 9+9 (hard 18, ×1.30) placed into the free Vault lane, beside the carried
        // towers - which stay at ×1.00 while the new towers run at the new hand's multiplier.
        BoardState board = WaveSession.Begin(Tuning, Encounter, Shoe.FromOrder([Rank.Ten, Rank.Two, Rank.Nine, Rank.Nine]), persisted)
            .Place(Family.Club, Vault(0))
            .Place(Family.Club, Vault(1))
            .Board();

        Assert.Equal(4, board.Towers.Count);
        Assert.Equal(2, board.Towers.Count(t => Math.Abs(t.FormationMultiplier - 1.00) < 1e-9));
        Assert.Equal(2, board.Towers.Count(t => Math.Abs(t.FormationMultiplier - 1.30) < 1e-9));
    }

    /// <summary>Wave one, settled: a 6-Club at Bastion(1) and an 8-Club at Bastion(2), both now ×1.00.</summary>
    private static IReadOnlyList<TowerState> AfterFirstWave()
    {
        (IReadOnlyList<TowerState> persisted, _) = Begin(Rank.Ten, Rank.Seven, Rank.Six, Rank.Eight)
            .Place(Family.Club, Bastion(1))
            .Place(Family.Club, Bastion(2))
            .Stand()
            .Lock()
            .Settle();

        return persisted;
    }

    /// <summary>Wave two: a Ten up, a Two in the hole, the player's 9 and 9, then the Dealer's draw off 12.</summary>
    private static readonly Rank[] SecondWaveShoe = [Rank.Ten, Rank.Two, Rank.Nine, Rank.Nine, Rank.Six];

    /// <summary>
    /// Wave two with its first card placed on <paramref name="socket"/> - which is what persistence
    /// exists to force: "sockets fill during the second wave, and every card after that forces a
    /// replacement" (docs/design/05-battlefield.md § Persistence).
    /// </summary>
    private static WaveSession SecondWaveOnto(SocketRef socket) =>
        WaveSession.Begin(Tuning, Encounter, Shoe.FromOrder(SecondWaveShoe), AfterFirstWave())
            .Place(Family.Spade, socket);

    [Fact]
    public void A_card_placed_on_a_carried_over_tower_replaces_it()
    {
        // The 9-Spade lands on the socket the wave-one 8-Club holds. Forced replacement does not
        // care which hand put the tower there.
        BoardState board = SecondWaveOnto(Bastion(2)).Board();

        TowerState replaced = board.Towers.Single(t => t.Socket == Bastion(2));
        Assert.Equal(Rank.Nine, replaced.Card.Rank);
        Assert.Equal(Family.Spade, replaced.Family);

        // The evicted tower is gone outright, not shadowed: two towers, not three.
        Assert.Equal(2, board.Towers.Count);
        Assert.DoesNotContain(board.Towers, t => t.Card.Rank == Rank.Eight);
    }

    [Fact]
    public void A_replaced_carried_over_tower_does_not_come_back_at_the_next_settle()
    {
        // The collision used to be resolved backwards here - the replaced tower kept, the card that
        // replaced it discarded.
        (IReadOnlyList<TowerState> carried, _) = SecondWaveOnto(Bastion(2))
            .Place(Family.Club, Vault(0))
            .Stand()
            .Lock()
            .Settle();

        Assert.Equal(Rank.Nine, carried.Single(t => t.Socket == Bastion(2)).Card.Rank);
        Assert.All(carried, t => Assert.Equal(1.00, t.FormationMultiplier, precision: 6));
        Assert.Equal(carried.Select(t => t.Socket).Distinct().Count(), carried.Count);
    }

    [Fact]
    public void A_busting_card_displaces_no_carried_over_tower()
    {
        // The busting card is destroyed and never placed, so it cannot evict anything - including a
        // tower from an earlier wave sitting on the socket it was aimed at.
        (IReadOnlyList<TowerState> persisted, _) = Begin(Rank.Ten, Rank.Seven, Rank.Six, Rank.Eight)
            .Place(Family.Club, Bastion(1))
            .Place(Family.Club, Bastion(2))
            .Stand()
            .Lock()
            .Settle();

        // Wave two: 10 + 9 is 19, the hit draws a King, and the Dealer still resolves off its 12.
        WaveSession busted = WaveSession.Begin(
                Tuning, Encounter,
                Shoe.FromOrder([Rank.Ten, Rank.Two, Rank.Ten, Rank.Nine, Rank.King, Rank.Six]), persisted)
            .Place(Family.Club, Vault(0))
            .Place(Family.Club, Vault(1))
            .Hit();

        Assert.Equal(WavePhase.BustLocked, busted.Phase);
        Assert.Equal(Rank.Eight, busted.Board().Towers.Single(t => t.Socket == Bastion(2)).Card.Rank);
    }

    [Fact]
    public void The_single_move_can_relocate_a_carried_over_tower()
    {
        // The adjustment window is one move for the whole board, not one for this hand's half.
        WaveSession adjusting = SecondWaveOnto(Vault(0))
            .Place(Family.Club, Vault(2))
            .Stand();

        WaveSession moved = adjusting.RelocateTower(Bastion(1), Bastion(0));

        Assert.True(moved.MoveSpent);
        Assert.Equal(Rank.Six, moved.Board().Towers.Single(t => t.Socket == Bastion(0)).Card.Rank);
        Assert.DoesNotContain(moved.Board().Towers, t => t.Socket == Bastion(1));

        // Still at ×1.00: moving a carried tower does not re-enlist it into this hand's formation.
        Assert.Equal(1.00, moved.Board().Towers.Single(t => t.Socket == Bastion(0)).FormationMultiplier, precision: 6);

        // And it is one move for the board: the second is refused.
        Assert.Throws<InvalidOperationException>(() => moved.RelocateTower(Vault(0), Vault(1)));
    }

    [Fact]
    public void A_swap_can_pair_a_carried_over_tower_with_one_from_this_hand()
    {
        // Bastion(1) is carried (the wave-one 6-Club); Bastion(2) is this hand's 9-Spade. The two
        // ends live in different halves of the board, which is the case that used to fail silently.
        WaveSession swapped = SecondWaveOnto(Bastion(2))
            .Place(Family.Club, Vault(0))
            .Stand()
            .SwapTowers(Bastion(1), Bastion(2));

        Assert.Equal(Rank.Nine, swapped.Board().Towers.Single(t => t.Socket == Bastion(1)).Card.Rank);
        Assert.Equal(Rank.Six, swapped.Board().Towers.Single(t => t.Socket == Bastion(2)).Card.Rank);
        Assert.True(swapped.MoveSpent);
    }

    [Fact]
    public void A_carried_over_tower_takes_a_standing_order()
    {
        WaveSession ordered = SecondWaveOnto(Vault(0))
            .Place(Family.Club, Vault(2))
            .Stand()
            .SetStandingOrder(Bastion(1), new StandingOrder { Focus = FocusMode.PreferArmored });

        Assert.Equal(FocusMode.PreferArmored, ordered.Board().Towers.Single(t => t.Socket == Bastion(1)).Order.Focus);

        // Orders are free, so the relocate move is still in hand.
        Assert.False(ordered.MoveSpent);
    }

    [Fact]
    public void An_adjustment_naming_an_empty_socket_is_refused_rather_than_ignored()
    {
        WaveSession adjusting = SecondWaveOnto(Vault(0))
            .Place(Family.Club, Vault(2))
            .Stand();

        Assert.Throws<InvalidOperationException>(() => adjusting.SetStandingOrder(Bastion(0), StandingOrder.None));
        Assert.Throws<InvalidOperationException>(() => adjusting.RelocateTower(Bastion(0), Bastion(1)));

        // Bastion(1) holds a carried tower, so relocating onto it is refused as occupied.
        Assert.Throws<InvalidOperationException>(() => adjusting.RelocateTower(Bastion(2), Bastion(1)));
    }
}
