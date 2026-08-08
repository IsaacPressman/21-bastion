using System.Reflection;
using Bastion.Core.Board;
using Bastion.Core.Cards;
using Bastion.Core.Resolve;

namespace Bastion.Core.Tests.Resolve;

/// <summary>
/// The two forecast contracts, verified as two separate claims.
/// </summary>
/// <remarks>
/// docs/prototype/VALIDATION.md step 4: "Verify Final-Forecast-versus-resolution equivalence on the
/// scripted fixtures, <b>and</b> verify that Visible Threat matches a resolver run against the
/// revealed force alone." Both must be checked independently, because they are different claims.
/// </remarks>
public sealed class EquivalenceTests
{
    private static readonly Rank[] DealerHand = [Rank.Ten, Rank.Six, Rank.Seven];

    private static BoardState Defended() => Fixture.Board(
        Fixture.Tower(Rank.Six, Family.Club, Fixture.Socket(0, 1), formation: 1.05),
        Fixture.Tower(Rank.Eight, Family.Club, Fixture.Socket(0, 2), formation: 1.05),
        Fixture.Tower(Rank.Four, Family.Spade, Fixture.Socket(1, 0), formation: 1.30));

    [Fact]
    public void The_final_forecast_is_what_the_timeline_actually_does()
    {
        // The combat contract, made testable: if it says a lane leaks two, the recording of the
        // wave leaks two. Playback animates these events, so a mismatch here is the forecast
        // becoming a lie.
        FinalForecast forecast = Fixture.ResolveComplete(
            Fixture.Example, Defended(), Fixture.Complete(Fixture.Example, DealerHand));

        foreach (LaneOutcome lane in forecast.Lanes)
        {
            int leakedInTimeline = forecast.Timeline.Events
                .OfType<LeakEvent>()
                .Where(e => e.LaneIndex == lane.LaneIndex)
                .Sum(e => e.LeakDamage);

            Assert.Equal(lane.PredictedDamage, leakedInTimeline);
        }
    }

    [Fact]
    public void Every_unit_in_the_timeline_either_dies_or_leaks()
    {
        // Nothing is quietly dropped. A unit that vanished would make predicted damage look better
        // than the wave, in the direction that erodes trust rather than the one that gets noticed.
        FinalForecast forecast = Fixture.ResolveComplete(
            Fixture.Example, Defended(), Fixture.Complete(Fixture.Example, DealerHand));

        IReadOnlyList<TimelineEvent> events = forecast.Timeline.Events;

        HashSet<int> spawned = [.. events.OfType<SpawnEvent>().Select(e => e.SpawnIndex)];
        HashSet<int> died = [.. events.OfType<DeathEvent>().Select(e => e.SpawnIndex)];
        HashSet<int> leaked = [.. events.OfType<LeakEvent>().Select(e => e.SpawnIndex)];

        Assert.NotEmpty(spawned);
        Assert.Empty(died.Intersect(leaked));
        Assert.Equal(spawned.Count, died.Count + leaked.Count);
    }

    [Fact]
    public void The_visible_threat_matches_a_run_against_the_revealed_force_alone()
    {
        // The second half of VALIDATION step 4. The revealed force is the base wave plus the
        // Vanguard, and nothing about the resolver changes when it is asked the smaller question.
        RevealedArmy revealed = Fixture.Revealed(Fixture.Example, Rank.Ten);

        VisibleThreat threat = Fixture.ResolveRevealed(Fixture.Example, Defended(), revealed);

        // The same spawns, asked as the complete-army question. Identical outcomes prove there is
        // one simulation path and that the split is in the contract, not in the arithmetic.
        FinalForecast asComplete = Fixture.ResolveComplete(
            Fixture.Example, Defended(), new CompleteArmy { Spawns = revealed.Spawns });

        Assert.Equal(asComplete.Lanes, threat.Lanes);
    }

    [Fact]
    public void The_visible_threat_is_not_a_prediction_of_the_wave()
    {
        // The movement the design insists is not a broken promise: the two figures answer different
        // questions. Revision 7 showed exactly this movement while calling both "the forecast",
        // which is how trust in a forecast is lost.
        VisibleThreat threat = Fixture.ResolveRevealed(
            Fixture.Example, Defended(), Fixture.Revealed(Fixture.Example, Rank.Ten));

        FinalForecast forecast = Fixture.ResolveComplete(
            Fixture.Example, Defended(), Fixture.Complete(Fixture.Example, DealerHand));

        int revealedTotal = threat.Lanes.Sum(l => l.PredictedDamage);
        int completeTotal = forecast.Lanes.Sum(l => l.PredictedDamage);

        Assert.True(completeTotal > revealedTotal,
            $"Reinforcements landed and the threat did not grow: {revealedTotal} then {completeTotal}.");
    }

    [Fact]
    public void The_two_forecasts_share_no_type()
    {
        // A type-system requirement rather than a convention, because the failure is silent: a
        // number that keeps its name while changing its meaning mid-hand is the cheapest possible
        // way to lose trust in the forecast. This test exists to fail a refactor that "helpfully"
        // unifies them.
        Type visible = typeof(VisibleThreat);
        Type final = typeof(FinalForecast);

        Assert.Equal(typeof(object), visible.BaseType);
        Assert.Equal(typeof(object), final.BaseType);
        Assert.Empty(visible.GetInterfaces().Intersect(final.GetInterfaces()));
        Assert.False(visible.IsAssignableFrom(final));
        Assert.False(final.IsAssignableFrom(visible));

        Assert.DoesNotContain(
            visible.GetMethods(BindingFlags.Public | BindingFlags.Static)
                .Concat(final.GetMethods(BindingFlags.Public | BindingFlags.Static)),
            m => m.Name is "op_Implicit" or "op_Explicit");
    }

    [Fact]
    public void Only_the_final_forecast_carries_a_timeline()
    {
        // A Visible Threat describes a force, not a wave that will run - so there is nothing on it
        // for combat playback to animate, and it cannot be rendered where a Final Forecast is
        // expected even by accident.
        Assert.DoesNotContain(
            typeof(VisibleThreat).GetProperties(),
            p => p.PropertyType == typeof(WaveTimeline));

        Assert.Contains(
            typeof(FinalForecast).GetProperties(),
            p => p.PropertyType == typeof(WaveTimeline));
    }

    [Fact]
    public void Damage_prevented_is_the_gap_between_the_empty_lane_and_the_plan()
    {
        FinalForecast forecast = Fixture.ResolveComplete(
            Fixture.Example, Defended(), Fixture.Complete(Fixture.Example, DealerHand));

        foreach (LaneOutcome lane in forecast.Lanes)
        {
            Assert.Equal(lane.EmptyLaneDamage - lane.PredictedDamage, lane.DamagePrevented);
            Assert.True(lane.PredictedDamage <= lane.EmptyLaneDamage,
                $"Lane {lane.LaneIndex} leaked more defended ({lane.PredictedDamage}) than undefended ({lane.EmptyLaneDamage}).");
        }
    }

    [Fact]
    public void An_undefended_lane_leaks_everything_and_says_why()
    {
        VisibleThreat threat = Fixture.ResolveRevealed(
            Fixture.Example, BoardState.Empty(Fixture.Tuning), Fixture.Revealed(Fixture.Example, Rank.Ten));

        foreach (LaneOutcome lane in threat.Lanes)
        {
            Assert.Equal(lane.EmptyLaneDamage, lane.PredictedDamage);
            Assert.Equal(0, lane.DamagePrevented);
            Assert.All(lane.LeakedUnits, u => Assert.Equal(LeakCause.LaneUndefended, u.Cause));
        }
    }

    [Fact]
    public void Open_and_held_read_off_the_plain_threshold()
    {
        // The maximum interpretation the game is permitted to do. The number is primary; this is a
        // glance-read, and it is not allowed to grow into a verdict.
        double threshold = Fixture.Tuning.Rules.OpenHeldThresholdFraction;

        LaneOutcome undefended = Fixture.ResolveRevealed(
            Fixture.Example, BoardState.Empty(Fixture.Tuning),
            Fixture.Revealed(Fixture.Example, Rank.Ten)).Lanes[1];

        Assert.True(undefended.IsOpen(threshold));
        Assert.Equal("Open", undefended.CoverageLabel(threshold));

        LaneOutcome held = Fixture.LaneZero(
            Fixture.Solo("swarm_unit", 8),
            Fixture.Board(Fixture.Tower(Rank.King, Family.Club, Fixture.Socket(0, 0), formation: 1.60)));

        Assert.False(held.IsOpen(threshold));
        Assert.Equal("Held", held.CoverageLabel(threshold));
    }
}
