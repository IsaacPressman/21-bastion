using Bastion.Core.Board;
using Bastion.Core.Cards;
using Bastion.Core.Config;
using Bastion.Core.Wave;

namespace Bastion.Core.Tests.Wave;

/// <summary>
/// Standing orders are editable throughout the wave and lock only when combat begins.
/// </summary>
/// <remarks>
/// docs/design/05-battlefield.md § They are encounter skill, not a secondary menu - DECIDED. This
/// widens Revision 7.1, which offered them in the adjustment window alone. Being able to tell a
/// Siege Club to hold for the armored target <i>at the moment it is placed</i>, rather than several
/// decisions later, is the whole point of the change. They still never consume the single move.
/// </remarks>
public sealed class StandingOrderWindowTests
{
    private static readonly TuningData Tuning = TuningLoader.LoadFromRepositoryRoot();
    private static EncounterTuning Encounter => Tuning.Encounter("example_wave");

    private static SocketRef Bastion(int socket) => SocketRef.InLane(0, socket);

    /// <summary>
    /// A wave over a scripted shoe, padded so the Dealer always has cards left to draw.
    /// </summary>
    /// <remarks>
    /// The Dealer draws to 17 out of the same shoe, so a shoe holding only the cards a test names
    /// runs dry at the stand. The padding is deliberately mid-rank: it lands the Dealer's hand
    /// without busting the arithmetic these tests care about, which is the standing orders.
    /// </remarks>
    private static WaveSession Begin(params Rank[] shoe) =>
        WaveSession.Begin(Tuning, Encounter, Shoe.FromOrder([.. shoe, .. Enumerable.Repeat(Rank.Seven, 8)]));

    private static StandingOrder Hold => new() { HoldPastPosition = 6.0 };

    [Fact]
    public void An_order_can_be_set_while_another_card_is_still_waiting_to_be_placed()
    {
        WaveSession session = Begin(Rank.Ten, Rank.Six, Rank.Nine, Rank.Eight)
            .Place(Family.Club, Bastion(0));

        Assert.Equal(WavePhase.AwaitingPlacement, session.Phase);

        WaveSession ordered = session.SetStandingOrder(Bastion(0), Hold);

        Assert.Equal(6.0, ordered.Board().Towers.Single(t => t.Socket == Bastion(0)).Order.HoldPastPosition);
    }

    [Fact]
    public void An_order_can_be_set_at_the_draw_decision()
    {
        WaveSession session = Begin(Rank.Ten, Rank.Six, Rank.Nine, Rank.Eight)
            .Place(Family.Club, Bastion(0))
            .Place(Family.Spade, Bastion(1));

        Assert.Equal(WavePhase.DrawDecision, session.Phase);

        WaveSession ordered = session.SetStandingOrder(Bastion(1), Hold);

        Assert.Equal(6.0, ordered.Board().Towers.Single(t => t.Socket == Bastion(1)).Order.HoldPastPosition);
    }

    [Fact]
    public void An_order_set_during_the_draw_changes_the_visible_threat_it_is_read_against()
    {
        // The reason the widening matters. An order whose consequence the player cannot see is a
        // menu; this is the reading that has to move when they set one.
        WaveSession session = Begin(Rank.Ten, Rank.Six, Rank.Nine, Rank.Eight)
            .Place(Family.Club, Bastion(0))
            .Place(Family.Club, Bastion(1));

        // Hold past the far end of the path: the towers never fire, so the lane must get worse.
        var neverFire = new StandingOrder { HoldPastPosition = Tuning.Geometry.PathLength };

        int before = session.VisibleThreatNow().Lanes[0].PredictedDamage;
        int after = session
            .SetStandingOrder(Bastion(0), neverFire)
            .SetStandingOrder(Bastion(1), neverFire)
            .VisibleThreatNow()
            .Lanes[0]
            .PredictedDamage;

        Assert.True(after > before, "Holding fire until the wall should let more through.");
    }

    [Fact]
    public void Setting_an_order_never_consumes_the_single_adjustment_move()
    {
        WaveSession session = Begin(Rank.Ten, Rank.Six, Rank.Nine, Rank.Eight)
            .Place(Family.Club, Bastion(0))
            .Place(Family.Spade, Bastion(1))
            .Stand()
            .SetStandingOrder(Bastion(0), Hold)
            .SetStandingOrder(Bastion(1), Hold);

        Assert.False(session.MoveSpent);

        // And the move itself is still there to spend.
        Assert.True(session.RelocateTower(Bastion(1), Bastion(2)).MoveSpent);
    }

    [Fact]
    public void Orders_lock_when_the_wave_locks()
    {
        WaveSession locked = Begin(Rank.Ten, Rank.Six, Rank.Nine, Rank.Eight)
            .Place(Family.Club, Bastion(0))
            .Place(Family.Spade, Bastion(1))
            .Stand()
            .Lock();

        Assert.Throws<InvalidOperationException>(() => locked.SetStandingOrder(Bastion(0), Hold));
    }

    [Fact]
    public void A_bust_locks_orders_with_the_placement_it_locks()
    {
        // A bust skips the adjustment window and locks placement immediately, so there is no window
        // for orders either. Combat has begun as far as the player's commitments are concerned.
        WaveSession busted = Begin(Rank.Ten, Rank.Six, Rank.Ten, Rank.Nine)
            .Place(Family.Club, Bastion(0))
            .Place(Family.Spade, Bastion(1))
            .Hit();

        Assert.Equal(WavePhase.BustLocked, busted.Phase);
        Assert.Throws<InvalidOperationException>(() => busted.SetStandingOrder(Bastion(0), Hold));
    }
}
