using Bastion.Core.Board;
using Bastion.Core.Cards;
using Bastion.Core.Resolve;

namespace Bastion.Core.Tests.Resolve;

/// <summary>
/// The pre-deal reading: what each lane takes if nobody defends it.
/// </summary>
/// <remarks>
/// docs/design/09-information-and-ui.md § Shown requires empty-lane damage before the opening deal,
/// alongside the stake. The point of these tests is that it is the <i>same</i> number the forecasts
/// report - a second, differently-derived estimate of the same quantity is how an interface starts
/// contradicting itself.
/// </remarks>
public sealed class OpeningStakesTests
{
    private static OpeningStakes Stakes(double entry = 0.0) =>
        Resolver.ResolveEmptyLanes(
            Fixture.Tuning, Fixture.Example, Fixture.Revealed(Fixture.Example, Rank.Ten, entry), entry);

    [Fact]
    public void Every_tuned_lane_is_reported_with_its_stake()
    {
        OpeningStakes stakes = Stakes();

        Assert.Equal(Fixture.Tuning.Geometry.Lanes, stakes.Lanes.Count);

        for (int lane = 0; lane < stakes.Lanes.Count; lane++)
        {
            Assert.Equal(lane, stakes.Lanes[lane].LaneIndex);
            Assert.Equal(Fixture.Example.LaneStakes[lane], stakes.Lanes[lane].Stake);
        }
    }

    /// <summary>
    /// The load-bearing one. The pre-deal figure and the Visible Threat's empty-lane baseline are the
    /// same claim about the same force, so they must be the same number.
    /// </summary>
    [Fact]
    public void It_agrees_with_the_empty_lane_damage_the_visible_threat_reports()
    {
        BoardState board = Fixture.Board(
            Fixture.Tower(Rank.Nine, Family.Club, Fixture.Socket(0, 0)),
            Fixture.Tower(Rank.Five, Family.Spade, Fixture.Socket(1, 1)));

        VisibleThreat threat = Fixture.ResolveRevealed(
            Fixture.Example, board, Fixture.Revealed(Fixture.Example, Rank.Ten));

        OpeningStakes stakes = Stakes();

        Assert.Equal(
            threat.Lanes.Select(l => l.EmptyLaneDamage),
            stakes.Lanes.Select(l => l.EmptyLaneDamage));
    }

    [Fact]
    public void An_undefended_lane_takes_something_from_the_opening_force()
    {
        Assert.All(Stakes().Lanes, lane => Assert.True(lane.EmptyLaneDamage > 0));
    }

    /// <summary>
    /// An advanced march does not change the undefended cost, and should not appear to.
    /// </summary>
    /// <remarks>
    /// With nothing firing, every unit reaches the wall whatever entry it started from - the march
    /// costs the player firing windows, not the enemy's arrival. So this figure is stable across the
    /// hand, which is what makes it a statement about the <i>lane</i> rather than a second threat
    /// reading that happens to drift as cards are drawn.
    /// </remarks>
    [Fact]
    public void The_undefended_cost_does_not_move_with_the_march()
    {
        Assert.Equal(
            Stakes().Lanes.Select(l => l.EmptyLaneDamage),
            Stakes(entry: 6.0).Lanes.Select(l => l.EmptyLaneDamage));
    }
}
