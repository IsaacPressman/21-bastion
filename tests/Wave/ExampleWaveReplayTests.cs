using Bastion.Core.Board;
using Bastion.Core.Cards;
using Bastion.Core.Config;
using Bastion.Core.Resolve;
using Bastion.Core.Wave;

namespace Bastion.Core.Tests.Wave;

/// <summary>
/// The Milestone 3 done-when: docs/design/example-wave.md replayed through the wave loop end to end.
/// </summary>
/// <remarks>
/// <para>
/// Asserts the <b>reproducible</b> quantities the doc turns on - march entries and step costs,
/// Formation multipliers, family locks, the Visible-Threat-then-Final-Forecast movement, the Dealer
/// busting yet deploying, and the one-move adjustment - rather than the cosmetic figures the doc's own
/// discrepancy log carves out: leakage is integer-governed (reproduce the shape, not 3.8/5.1/3.4),
/// the pile is 21 not 22, and the 14 s timing is not asserted (see tuning-constants.md § Known
/// Discrepancies and PacingTests).
/// </para>
/// <para>
/// Lane 0 is the Bastion, lane 1 the Vault - the doc's "lane one" and "lane two".
/// </para>
/// </remarks>
public sealed class ExampleWaveReplayTests
{
    private static readonly TuningData Tuning = TuningLoader.LoadFromRepositoryRoot();

    private static SocketRef Bastion(int socket) => SocketRef.InLane(0, socket);
    private static SocketRef Vault(int socket) => SocketRef.InLane(1, socket);

    /// <summary>
    /// The shoe the doc scripts: upcard 10, hole 6, player 6 and 8, the hit 4, the Dealer's drawn 7.
    /// </summary>
    /// <remarks>Draw order matches <see cref="WaveSession.Begin"/>: upcard, hole, then the opening two.</remarks>
    private static WaveSession Opened() => WaveSession.Begin(
        Tuning,
        Tuning.Encounter("example_wave"),
        Shoe.FromOrder([Rank.Ten, Rank.Six, Rank.Six, Rank.Eight, Rank.Four, Rank.Seven]));

    [Fact]
    public void The_opening_deal_is_fourteen_at_1_05_with_the_army_at_zero()
    {
        WaveSession s = Opened()
            .Place(Family.Club, Bastion(1))    // 6 -> lane one socket 6
            .Place(Family.Club, Bastion(2));   // 8 -> lane one socket 9

        Assert.Equal(WavePhase.DrawDecision, s.Phase);
        Assert.Equal(14, s.Hand.Total);
        Assert.Equal(1.05, s.FormationMultiplier, precision: 6);
        Assert.Equal(0.0, s.Entry, precision: 6);

        // The Vault lane, undefended, reads Open at its full 6 (six raiders, one leak each).
        LaneOutcome vault = s.VisibleThreatNow().Lanes[1];
        Assert.Equal("vault", vault.Stake);
        Assert.Equal(6, vault.PredictedDamage);
        Assert.True(vault.IsOpen(Tuning.Rules.OpenHeldThresholdFraction));
    }

    [Fact]
    public void The_third_card_pays_the_step_before_it_is_seen_and_lands_at_hard_eighteen()
    {
        WaveSession decided = Opened()
            .Place(Family.Club, Bastion(1))
            .Place(Family.Club, Bastion(2));

        // At two cards the next step - the third card - costs 1.5, which is what the hit will pay.
        Assert.Equal(1.5, decided.NextStepCost(), precision: 6);

        WaveSession afterHit = decided.Hit();
        Assert.Equal(WavePhase.AwaitingPlacement, afterHit.Phase);

        WaveSession s = afterHit.Place(Family.Spade, Vault(0));   // 4 -> lane two socket 3
        Assert.Equal(18, s.Hand.Total);
        Assert.Equal(1.30, s.FormationMultiplier, precision: 6);
        Assert.Equal(1.5, s.Entry, precision: 6);   // the third card's step, paid for the draw

        // At three cards the doc's "next march step costs 2.5" holds: entry 1.5 now, 4.0 at a fourth.
        Assert.Equal(2.5, s.NextStepCost(), precision: 6);

        // The lone Spade pulls the Vault's Visible Threat down from its undefended 6.
        LaneOutcome vault = s.VisibleThreatNow().Lanes[1];
        Assert.Equal("vault", vault.Stake);
        Assert.True(vault.PredictedDamage < 6, $"the Spade should reduce the Vault threat below 6, was {vault.PredictedDamage}");
    }

    [Fact]
    public void The_dealer_busts_but_deploys_and_the_final_forecast_exceeds_the_visible_threat()
    {
        WaveSession stood = Opened()
            .Place(Family.Club, Bastion(1))
            .Place(Family.Club, Bastion(2))
            .Hit()
            .Place(Family.Spade, Vault(0));

        int visibleVault = stood.VisibleThreatNow().Lanes[1].PredictedDamage;

        WaveSession s = stood.Stand();

        Assert.Equal(WavePhase.AdjustmentWindow, s.Phase);

        // 10 up, 6 hole = 16, draws a 7 -> 23. The Dealer busts, and deploys all three anyway.
        Assert.NotNull(s.DealerCards);
        Assert.Equal([Rank.Ten, Rank.Six, Rank.Seven], s.DealerCards!.Select(c => c.Rank));

        // The Dealer's reinforcements land in the Vault, so the Final Forecast is worse than the
        // Visible Threat was - the movement the doc insists is not a broken promise.
        LaneOutcome finalVault = s.Forecast().Lanes[1];
        Assert.Equal("vault", finalVault.Stake);
        Assert.True(finalVault.PredictedDamage > visibleVault,
            $"Final Forecast ({finalVault.PredictedDamage}) should exceed the Visible Threat ({visibleVault}).");
    }

    [Fact]
    public void The_adjustment_window_is_one_move_with_free_standing_orders_and_locked_families()
    {
        WaveSession adjusting = Opened()
            .Place(Family.Club, Bastion(1))
            .Place(Family.Club, Bastion(2))
            .Hit()
            .Place(Family.Spade, Vault(0))
            .Stand();

        // Relocate the Spade one socket, then set a Hold order - which is free, not a second move.
        WaveSession moved = adjusting
            .RelocateTower(Vault(0), Vault(1))
            .SetStandingOrder(Vault(1), new StandingOrder { HoldPastPosition = 6.0 });

        Assert.True(moved.MoveSpent);

        // Family is locked: the relocated tower is still a Spade, and it now fires from the new socket.
        TowerActivity spade = moved.Forecast().Lanes[1].TowerActivity.Single(a => a.Socket == Vault(1));
        Assert.Equal(Family.Spade, spade.Family);

        // One move for the whole board: a second relocation, or a swap, is refused.
        Assert.Throws<InvalidOperationException>(() => moved.RelocateTower(Bastion(1), Bastion(0)));
        Assert.Throws<InvalidOperationException>(() => moved.SwapTowers(Bastion(1), Bastion(2)));

        // The Final Forecast is a real combat contract against the moved board.
        WaveSession locked = moved.Lock();
        Assert.Equal(WavePhase.Locked, locked.Phase);
        Assert.NotEmpty(locked.Forecast().Timeline.Events);
    }

    [Fact]
    public void The_bastion_lane_holds_better_than_the_vault()
    {
        FinalForecast forecast = Opened()
            .Place(Family.Club, Bastion(1))
            .Place(Family.Club, Bastion(2))
            .Hit()
            .Place(Family.Spade, Vault(0))
            .Stand()
            .Forecast();

        LaneOutcome bastion = forecast.Lanes[0];
        LaneOutcome vault = forecast.Lanes[1];

        // The two Clubs defend the Bastion; its towers prevent damage, and it fares better than the
        // lightly held Vault the player chose to give ground in.
        Assert.Equal("bastion", bastion.Stake);
        Assert.True(bastion.DamagePrevented > 0, "the Bastion's Clubs should prevent some damage.");
        Assert.True(bastion.PredictedDamage < vault.PredictedDamage,
            $"the Bastion ({bastion.PredictedDamage}) should hold better than the Vault ({vault.PredictedDamage}).");
    }
}
