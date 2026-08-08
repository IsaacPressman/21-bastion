using Bastion.Core.Board;
using Bastion.Core.Cards;
using Bastion.Core.Config;
using Bastion.Core.Resolve;
using Bastion.Core.Wave;

namespace Bastion.Core.Tests.Wave;

/// <summary>
/// Bust handling: the card destroyed, the formation at ×0.80, Overload struck unsteerably, no
/// adjustment window, and the Dealer resolving in full anyway (docs/design/07-bust-and-overload.md).
/// </summary>
/// <remarks>
/// Follows the worked example's counterfactual: had the player hit and busted on a King, entry would
/// have advanced to 4.0, Overload would have dealt 5.0 to the Vault, and the Dealer would have
/// deployed the same army.
/// </remarks>
public sealed class BustTests
{
    private static readonly TuningData Tuning = TuningLoader.LoadFromRepositoryRoot();

    private static SocketRef Bastion(int socket) => SocketRef.InLane(0, socket);
    private static SocketRef Vault(int socket) => SocketRef.InLane(1, socket);

    /// <summary>
    /// The worked example up to the fourth-card decision - three cards placed, about to hit.
    /// </summary>
    private static WaveSession AtTheDecision() => WaveSession.Begin(
            Tuning,
            Tuning.Encounter("example_wave"),
            Shoe.FromOrder([Rank.Ten, Rank.Six, Rank.Six, Rank.Eight, Rank.Four, Rank.King, Rank.Seven]))
        .Place(Family.Club, Bastion(1))
        .Place(Family.Club, Bastion(2))
        .Hit()
        .Place(Family.Spade, Vault(0));

    /// <summary>The counterfactual: the fourth card is a King, and the hand busts on it.</summary>
    private static WaveSession BustedOnAKing() => AtTheDecision().Hit();

    [Fact]
    public void A_bust_locks_immediately_with_no_adjustment_window()
    {
        WaveSession s = BustedOnAKing();

        Assert.Equal(WavePhase.BustLocked, s.Phase);
        Assert.True(s.Hand.IsBust);

        // No adjustment window on a bust - placement is already locked.
        Assert.Throws<InvalidOperationException>(() => s.RelocateTower(Vault(0), Vault(1)));
        Assert.Throws<InvalidOperationException>(() => s.SetStandingOrder(Vault(0), new StandingOrder()));
    }

    [Fact]
    public void The_busting_card_is_destroyed_and_the_formation_runs_at_the_bust_multiplier()
    {
        WaveSession s = BustedOnAKing();

        // Four cards in the hand, but the King never took a socket: the board keeps its three towers.
        Assert.Equal(4, s.Hand.CardCount);
        Assert.Equal(3, s.Board().Towers.Count);
        Assert.All(s.Board().Towers, t => Assert.Equal(Tuning.FormationStrength.Bust, t.FormationMultiplier, precision: 6));

        // The paid step still advanced the march to the four-card entry.
        Assert.Equal(4.0, s.Entry, precision: 6);
    }

    [Fact]
    public void Overload_strikes_the_highest_visible_threat_lane_at_the_cards_base_power()
    {
        WaveSession decision = AtTheDecision();

        // The lane is the one the player is shown before hitting: the highest current Visible Threat.
        int shown = decision.VisibleThreatNow().HighestThreatLane(Tuning.Rules.OverloadTieBreakStake).LaneIndex;

        WaveSession s = decision.Hit();   // busts on the King
        Assert.NotNull(s.Overload);

        // Overload lands where the panel said it would - unsteerable, not aimed by the busting card.
        Assert.Equal(shown, s.Overload!.LaneIndex);

        // A King's base power is 5.0, and Overload does not scale with the amount over 21.
        Assert.Equal(Tuning.CardPower.ForValue(new Card(Rank.King).Value), s.Overload.Damage, precision: 6);
        Assert.Equal(5.0, s.Overload.Damage, precision: 6);
    }

    [Fact]
    public void The_dealer_still_resolves_in_full_and_the_burst_kills_at_the_front()
    {
        WaveSession s = BustedOnAKing();

        // Resolution is purely deploy: the Dealer reveals and draws to 17 even on the player's bust.
        Assert.NotNull(s.DealerCards);
        Assert.Equal([Rank.Ten, Rank.Six, Rank.Seven], s.DealerCards!.Select(c => c.Rank));

        // The burst is in the Final Forecast's timeline, in the struck lane, and it took a unit.
        FinalForecast forecast = s.Forecast();
        OverloadEvent burst = forecast.Timeline.Events.OfType<OverloadEvent>().Single();

        Assert.Equal(s.Overload!.LaneIndex, burst.LaneIndex);
        Assert.Equal(5.0, burst.Damage, precision: 6);
        Assert.NotEmpty(burst.KilledSpawnIndices);
    }
}
