using Bastion.Core.Board;
using Bastion.Core.Cards;
using Bastion.Core.Config;
using Bastion.Core.Resolve;

namespace Bastion.Core.Tests.Resolve;

/// <summary>
/// The exact committed-state statistics, stated in battlefield language.
/// </summary>
/// <remarks>
/// docs/design/14-encounter-timeline.md § Exact consequences for the committed state. The lane
/// states a requirement and a shortfall; it never names the card that closes them. These check the
/// arithmetic behind the sentence and the absence of the thing the sentence must not become.
/// </remarks>
public sealed class LaneConsequenceTests
{
    private const double Threshold = 0.5;

    private static LaneConsequence UndefendedLaneZero(string enemyId, int count)
    {
        EncounterTuning encounter = Fixture.Solo(enemyId, count);

        return LaneConsequence.For(
            Fixture.LaneZero(encounter, Fixture.Board()), Fixture.Tuning, Threshold);
    }

    [Fact]
    public void An_undefended_lane_needs_a_units_full_health_and_delivers_none_of_it()
    {
        LaneConsequence lane = UndefendedLaneZero("armored_soldier", 3);
        EnemyTuning type = Fixture.Tuning.Enemy("armored_soldier");

        Assert.Equal(3, lane.Leakers.Count);

        LeakerConsequence lead = Assert.IsType<LeakerConsequence>(lane.LeadLeaker);
        Assert.Equal(type.DisplayName, lead.DisplayName);
        Assert.Equal(type.Health, lead.Required);
        Assert.Equal(0.0, lead.Delivered);
        Assert.Equal(type.Health, lead.Shortfall);
        Assert.Equal(LeakCause.LaneUndefended, lead.Cause);
    }

    [Fact]
    public void A_shot_at_unit_reports_what_landed_and_what_is_still_missing()
    {
        // One small tower, deliberately not enough. The point of the sentence is the gap: "needs 2.1
        // more armor-effective damage" is a requirement the player can act on, where "this lane is
        // weak" is not.
        EncounterTuning encounter = Fixture.Solo("armored_soldier", 3);
        BoardState board = Fixture.Board(Fixture.Tower(Rank.Three, Family.Club, Fixture.Socket(0, 0)));

        LaneConsequence lane = LaneConsequence.For(
            Fixture.LaneZero(encounter, board), Fixture.Tuning, Threshold);

        Assert.NotEmpty(lane.Leakers);

        foreach (LeakerConsequence leaker in lane.Leakers)
        {
            Assert.Equal(Fixture.Tuning.Enemy(leaker.EnemyId).Health, leaker.Required);
            Assert.InRange(leaker.Delivered, 0.0, leaker.Required);
            Assert.Equal(leaker.Required - leaker.Delivered, leaker.Shortfall, 9);
        }

        // Something got shot at, or the fixture is not testing what it claims to.
        Assert.Contains(lane.Leakers, l => l.Delivered > 0.0);
    }

    [Fact]
    public void The_lead_leaker_is_the_earliest_one_and_sets_the_lanes_first_leak_time()
    {
        LaneConsequence lane = UndefendedLaneZero("armored_soldier", 3);

        Assert.Equal(lane.Leakers.Min(l => l.LeakTime), lane.FirstLeakTime);
        Assert.Equal(lane.LeadLeaker!.LeakTime, lane.FirstLeakTime);
    }

    [Fact]
    public void A_held_lane_reports_no_leakers_and_no_first_leak_time()
    {
        // A King forward of a small swarm. Nothing gets through, so there is no requirement to
        // state - the lane's problem is solved and the readout says so by having nothing to say.
        EncounterTuning encounter = Fixture.Solo("swarm_unit", 2);
        BoardState board = Fixture.Board(
            Fixture.Tower(Rank.King, Family.Club, Fixture.Socket(0, 0)),
            Fixture.Tower(Rank.King, Family.Club, Fixture.Socket(0, 1)));

        LaneConsequence lane = LaneConsequence.For(
            Fixture.LaneZero(encounter, board), Fixture.Tuning, Threshold);

        Assert.Empty(lane.Leakers);
        Assert.Null(lane.FirstLeakTime);
        Assert.Null(lane.LeadLeaker);
        Assert.Equal("Held", lane.CoverageLabel);
    }

    [Fact]
    public void Attacks_per_tower_are_carried_as_counts_because_that_is_what_a_march_step_costs()
    {
        EncounterTuning encounter = Fixture.Solo("armored_soldier", 3);
        SocketRef socket = Fixture.Socket(0, 0);
        BoardState board = Fixture.Board(Fixture.Tower(Rank.Nine, Family.Club, socket));

        LaneOutcome outcome = Fixture.LaneZero(encounter, board);
        LaneConsequence lane = LaneConsequence.For(outcome, Fixture.Tuning, Threshold);

        TowerAttacks tower = lane.Towers.Single(t => t.Socket == socket);

        Assert.Equal(outcome.TowerActivity.Single(a => a.Socket == socket).Shots, tower.Shots);
        Assert.True(tower.Shots > 0);
    }

    [Fact]
    public void Nothing_on_the_consequence_types_totals_across_lanes()
    {
        // Hard Invariant 5 and § Total engagement stays out of it. Lanes carry different stakes and
        // sockets are not interchangeable, so any figure summing them teaches a model the design has
        // explicitly rejected - and a single quantity is also the thing a brute-forcer sorts on.
        string[] banned = ["Total", "Sum", "Overall", "Combined", "Score", "Engagement"];

        string[] offenders =
        [
            .. new[] { typeof(LaneConsequence), typeof(LeakerConsequence), typeof(TowerAttacks) }
                .SelectMany(type => type.GetProperties().Select(p => $"{type.Name}.{p.Name}"))
                .Where(name => banned.Any(word => name.Contains(word, StringComparison.OrdinalIgnoreCase))),
        ];

        Assert.Empty(offenders);
    }
}
