using Bastion.Core.Board;
using Bastion.Core.Cards;
using Bastion.Core.Resolve;

namespace Bastion.Core.Tests.Resolve;

/// <summary>
/// The shared junction socket: breadth bought with contribution.
/// </summary>
/// <remarks>
/// The junction is one socket, not one per lane. It is adjacent to neither lane for run purposes -
/// a run island - and a tower there fires into both lanes at a reduced contribution each. Face
/// cards escape the penalty, which is the whole of their claim on the socket.
/// </remarks>
public sealed class JunctionTests
{
    private static ShotEvent FirstShotFromJunction(Rank rank)
    {
        BoardState board = Fixture.Board(Fixture.Tower(rank, Family.Club, SocketRef.Junction));

        return Fixture.ShotsInLaneZero(Fixture.Solo("armored_soldier", 1), board)[0];
    }

    [Fact]
    public void A_junction_tower_fires_at_half_strength()
    {
        // A nine is 4.7 power; halved it lands 2.35 before armor, then 0.85 after.
        ShotEvent shot = FirstShotFromJunction(Rank.Nine);

        Assert.Equal(2.35, shot.DamageBeforeArmor, precision: 6);
        Assert.Equal(0.85, shot.DamageApplied, precision: 6);
    }

    [Fact]
    public void A_face_card_escapes_the_junction_penalty()
    {
        // "May occupy the shared junction socket without the usual contribution penalty."
        ShotEvent shot = FirstShotFromJunction(Rank.Ten);

        Assert.Equal(5.0, shot.DamageBeforeArmor, precision: 6);
        Assert.Equal(3.5, shot.DamageApplied, precision: 6);
    }

    [Fact]
    public void A_junction_tower_appears_in_both_lanes_activity()
    {
        // Two entries, each at the reduced contribution. That is the honest rendering of "buys
        // breadth and forfeits synergy" - the breadth is visible, and so is what it cost.
        BoardState board = Fixture.Board(Fixture.Tower(Rank.Nine, Family.Club, SocketRef.Junction));

        FinalForecast forecast = Fixture.ResolveComplete(
            Fixture.Example, board, Fixture.Complete(Fixture.Example, [Rank.Ten]));

        Assert.All(forecast.Lanes, lane =>
            Assert.Contains(lane.TowerActivity, a => a.Socket.IsJunction));

        Assert.All(forecast.Lanes, lane =>
            Assert.True(lane.TowerActivity.Single(a => a.Socket.IsJunction).Shots > 0));
    }

    [Fact]
    public void A_lane_socket_out_damages_the_junction_in_its_own_lane()
    {
        // The trade the design wants: the junction covers two lanes and neither of them well. Both
        // towers here sit at path position 6.0 with the same range, so contribution is the only
        // difference between them.
        double inLane = SingleLaneDamage(Fixture.Tower(Rank.Nine, Family.Club, Fixture.Socket(0, 1)));
        double atJunction = SingleLaneDamage(Fixture.Tower(Rank.Nine, Family.Club, SocketRef.Junction));

        Assert.True(inLane > atJunction, $"Lane socket dealt {inLane}, junction dealt {atJunction}.");
    }

    [Fact]
    public void The_junction_is_excluded_from_a_lanes_counterfactual()
    {
        // Empty-lane damage is what the lane takes with none of its defenses - and a junction tower
        // is one of its defenses. Leaving it in the baseline would understate what it prevented.
        BoardState board = Fixture.Board(Fixture.Tower(Rank.King, Family.Club, SocketRef.Junction));

        LaneOutcome lane = Fixture.LaneZero(Fixture.Solo("swarm_unit", 8), board);

        Assert.Equal(8, lane.EmptyLaneDamage);
        Assert.True(lane.DamagePrevented > 0, "A King at the junction prevented nothing.");
    }

    private static double SingleLaneDamage(TowerState tower) =>
        Fixture.LaneZero(Fixture.Solo("armored_soldier", 1), Fixture.Board(tower))
            .TowerActivity.Single().DamageApplied;
}
