using Bastion.Core.Board;
using Bastion.Core.Cards;
using Bastion.Core.Config;
using Bastion.Core.Resolve;

namespace Bastion.Core.Tests.Resolve;

/// <summary>
/// What a tower shoots at, and the three standing orders.
/// </summary>
/// <remarks>
/// Standing orders are <b>modelled exactly by the resolver or they do not ship</b> - there is no
/// approximately-forecast tier. Since combat takes no live input, these are the whole of the
/// player's tactical expression during a wave.
/// </remarks>
public sealed class TargetingTests
{
    private static readonly EncounterTuning TwoSoldiers = Fixture.Solo("armored_soldier", 2);

    /// <summary>An Ace is weak enough against 1.5 armor that nothing dies and the wave runs long.</summary>
    private static BoardState Watchtower(int socketIndex, StandingOrder? order = null) =>
        Fixture.Board(Fixture.Tower(Rank.Ace, Family.Club, Fixture.Socket(0, socketIndex), order: order));

    /// <summary>
    /// Primary hits on one unit. Splash is excluded deliberately.
    /// </summary>
    /// <remarks>
    /// A Club's blast lands on whoever is beside the target, so counting splash would report a
    /// tower as "shooting at" a unit it never chose - which is exactly the thing these tests are
    /// trying to measure.
    /// </remarks>
    private static int ShotsAt(EncounterTuning encounter, BoardState board, int spawnIndex) =>
        Fixture.ShotsInLaneZero(encounter, board)
            .Count(s => !s.IsSplash && s.TargetSpawnIndex == spawnIndex);

    [Fact]
    public void By_default_a_tower_shoots_whatever_is_nearest_to_it()
    {
        // Nearest-to-the-tower is the chosen default precisely so that both Focus modes stay
        // meaningful. A leading-target default would make one of the design's own orders a no-op.
        BoardState board = Watchtower(0);

        int leader = ShotsAt(TwoSoldiers, board, spawnIndex: 0);
        int follower = ShotsAt(TwoSoldiers, board, spawnIndex: 1);

        // The follower is behind the socket while the leader is past it, so it takes the later fire.
        Assert.True(follower > 0, "The trailing soldier was never shot at.");
        Assert.True(leader > 0, "The leading soldier was never shot at.");
    }

    [Fact]
    public void Focus_on_the_leading_target_shifts_fire_forward()
    {
        StandingOrder leading = new() { Focus = FocusMode.PreferLeading };

        int withoutOrder = ShotsAt(TwoSoldiers, Watchtower(0), spawnIndex: 0);
        int withOrder = ShotsAt(TwoSoldiers, Watchtower(0, leading), spawnIndex: 0);

        Assert.True(withOrder > withoutOrder,
            $"PreferLeading put {withOrder} shots on the leader against {withoutOrder} by default.");
    }

    [Fact]
    public void Focus_on_armor_picks_the_armored_target_out_of_a_mixed_column()
    {
        // Swarm units spawn first and swamp the lane; the single armored soldier arrives behind
        // them. Under the default rule the swarm soaks the fire.
        EncounterTuning mixed = Fixture.Isolated(("swarm_unit", 6), ("armored_soldier", 1));
        StandingOrder armored = new() { Focus = FocusMode.PreferArmored };

        int defaultShots = ShotsOnArmor(mixed, Watchtower(1));
        int focusedShots = ShotsOnArmor(mixed, Watchtower(1, armored));

        Assert.True(focusedShots > defaultShots,
            $"PreferArmored landed {focusedShots} shots on armor against {defaultShots} by default.");
    }

    [Fact]
    public void Focus_on_armor_still_fires_when_nothing_in_range_is_armored()
    {
        // A preference, not a restriction. A tower that idles beside a swarm because it is holding
        // out for armor would be a trap rather than an order.
        StandingOrder armored = new() { Focus = FocusMode.PreferArmored };

        LaneOutcome lane = Fixture.LaneZero(Fixture.Solo("swarm_unit", 8), Watchtower(1, armored));

        Assert.True(lane.TowerActivity.Single().Shots > 0);
    }

    [Fact]
    public void Hold_keeps_a_tower_quiet_until_the_line_is_crossed()
    {
        // Socket 9 covers 6.0 to 12.0. Holding past 9.0 discards the first half of that window, so
        // the tower opens fire later and lands fewer shots.
        StandingOrder hold = new() { HoldPastPosition = 9.0 };

        TowerActivity free = Fixture.LaneZero(TwoSoldiers, Watchtower(2)).TowerActivity.Single();
        TowerActivity held = Fixture.LaneZero(TwoSoldiers, Watchtower(2, hold)).TowerActivity.Single();

        Assert.NotNull(free.FirstFireTime);
        Assert.NotNull(held.FirstFireTime);
        Assert.True(held.FirstFireTime > free.FirstFireTime,
            $"Held tower opened at {held.FirstFireTime}s, free tower at {free.FirstFireTime}s.");
        Assert.True(held.Shots < free.Shots);
    }

    [Fact]
    public void A_held_tower_stays_off_cooldown_while_it_waits()
    {
        // Holding fire is not the same as having no target. The tower banks nothing, but it fires
        // the instant its condition is met - which is the entire value of pre-committing.
        StandingOrder hold = new() { HoldPastPosition = 9.0 };

        TowerActivity held = Fixture.LaneZero(TwoSoldiers, Watchtower(2, hold)).TowerActivity.Single();

        Assert.True(held.IdleTicks > 0, "A tower that held fire recorded no idle ticks.");
    }

    [Fact]
    public void Trigger_on_group_never_fires_at_a_column_too_thin_to_trigger()
    {
        // Two units cannot make a group of three, so the trap waits out the whole wave.
        StandingOrder trap = new() { TriggerOnGroup = true };

        LaneOutcome lane = Fixture.LaneZero(Fixture.Solo("swarm_unit", 2), Watchtower(1, trap));

        Assert.Equal(0, lane.TowerActivity.Single().Shots);
        Assert.Equal(2, lane.PredictedDamage);
    }

    [Fact]
    public void Trigger_on_group_fires_once_the_group_arrives()
    {
        StandingOrder trap = new() { TriggerOnGroup = true };

        LaneOutcome lane = Fixture.LaneZero(Fixture.Solo("swarm_unit", 8), Watchtower(1, trap));

        Assert.True(lane.TowerActivity.Single().Shots > 0);
    }

    [Fact]
    public void A_tower_never_shoots_past_its_range()
    {
        // Socket 3 with range 3.0 covers 0.0 to 6.0. An army entering at 6.5 has already walked
        // past the far edge of that window and only ever moves further away.
        BoardState board = Fixture.BoardAt(6.5, Fixture.Tower(Rank.Ace, Family.Club, Fixture.Socket(0, 0)));

        LaneOutcome lane = Fixture.LaneZero(TwoSoldiers, board);

        Assert.Equal(0, lane.TowerActivity.Single().Shots);
        Assert.All(lane.LeakedUnits, u => Assert.Equal(LeakCause.NeverInRange, u.Cause));
    }

    [Fact]
    public void A_face_cards_wider_range_reaches_where_a_plain_card_cannot()
    {
        // Range 4.0 stretches socket 3's window to 7.0, so the same army at entry 6.5 is inside it.
        // This is the geometric half of what a face card buys, and it interacts directly with the
        // March Clock: a wider window is a window the march takes longer to eat.
        BoardState board = Fixture.BoardAt(6.5, Fixture.Tower(Rank.Jack, Family.Club, Fixture.Socket(0, 0)));

        Assert.True(Fixture.LaneZero(TwoSoldiers, board).TowerActivity.Single().Shots > 0);
    }

    private static int ShotsOnArmor(EncounterTuning encounter, BoardState board)
    {
        IReadOnlyList<TimelineEvent> events = Fixture.EventsInLaneZero(encounter, board);

        HashSet<int> armored = [.. events
            .OfType<SpawnEvent>()
            .Where(e => e.EnemyId == "armored_soldier")
            .Select(e => e.SpawnIndex)];

        // Primary hits only - a blast that happens to catch the soldier is not the tower choosing it.
        return events.OfType<ShotEvent>().Count(s => !s.IsSplash && armored.Contains(s.TargetSpawnIndex));
    }
}
