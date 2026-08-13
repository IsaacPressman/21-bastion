using Bastion.Core.Board;
using Bastion.Core.Cards;
using Bastion.Core.Config;
using Bastion.Core.Resolve;

namespace Bastion.Core.Tests.Resolve;

/// <summary>
/// The Visible Threat carries a schedule, and it still cannot be played back.
/// </summary>
/// <remarks>
/// <para>
/// Milestone 6 needs the encounter timeline during the draw - "if you draw again, this cannon loses
/// two shots" is the whole March decision and is unshowable once the Dealer has resolved. So a
/// <see cref="VisibleThreat"/> now carries a <see cref="RevealedTimeline"/>.
/// </para>
/// <para>
/// What must survive that is Hard Invariant 4: <b>a Visible Threat must not be renderable where a
/// Final Forecast is expected.</b> These tests pin both halves - the schedule is real and it is the
/// same run the lane outcomes came from, and the type is not a <see cref="WaveTimeline"/> and cannot
/// be turned into one.
/// </para>
/// </remarks>
public sealed class RevealedTimelineTests
{
    private static BoardState TwoTowers() => Fixture.Board(
        Fixture.Tower(Rank.Eight, Family.Club, Fixture.Socket(0, 0)),
        Fixture.Tower(Rank.Six, Family.Spade, Fixture.Socket(0, 1)));

    [Fact]
    public void The_schedule_describes_the_same_run_as_the_lane_outcomes()
    {
        EncounterTuning encounter = Fixture.Solo("armored_soldier", 3);
        BoardState board = TwoTowers();

        VisibleThreat threat = Fixture.ResolveRevealed(
            encounter, board, Fixture.Revealed(encounter, Rank.Ten, board.Entry));

        // Every unit the lane says leaked has a leak event at the time the lane says, and no other
        // unit does. If the schedule and the outcome could disagree, the timeline would be a second
        // account of the wave rather than a drawing of the one the resolver ran.
        LeakEvent[] leaks = [.. threat.Schedule.Events.OfType<LeakEvent>().Where(e => e.LaneIndex == 0)];

        Assert.Equal(
            threat.Lanes[0].LeakedUnits.Select(u => u.SpawnIndex).Order(),
            leaks.Select(e => e.SpawnIndex).Order());

        foreach (LeakedUnit unit in threat.Lanes[0].LeakedUnits)
        {
            Assert.Equal(unit.LeakTime, leaks.Single(e => e.SpawnIndex == unit.SpawnIndex).Time);
        }
    }

    [Fact]
    public void Shots_on_the_schedule_agree_with_the_towers_own_activity_count()
    {
        EncounterTuning encounter = Fixture.Solo("armored_soldier", 3);
        BoardState board = TwoTowers();

        VisibleThreat threat = Fixture.ResolveRevealed(
            encounter, board, Fixture.Revealed(encounter, Rank.Ten, board.Entry));

        foreach (TowerActivity activity in threat.Lanes[0].TowerActivity)
        {
            // Primary shots only: a Club's splash lands on the same shot and adds damage, not another
            // attack. A timeline that counted them would disagree with the lane's own readout about
            // how many times a tower fired, which is the figure the March cost is expressed in.
            int primary = threat.Schedule.Events
                .OfType<ShotEvent>()
                .Count(e => e.LaneIndex == 0 && e.Socket == activity.Socket && !e.IsSplash);

            Assert.Equal(activity.Shots, primary);
        }
    }

    [Fact]
    public void A_revealed_schedule_is_not_a_wave_timeline_and_offers_no_way_to_become_one()
    {
        // Hard Invariant 4, checked structurally rather than remembered. The two share no base class
        // and no interface, and nothing on either converts to the other - which is what makes
        // TimelinePlayer's WaveTimeline-only constructor an actual guarantee rather than a habit.
        Assert.False(typeof(WaveTimeline).IsAssignableFrom(typeof(RevealedTimeline)));
        Assert.False(typeof(RevealedTimeline).IsAssignableFrom(typeof(WaveTimeline)));

        Assert.Equal(typeof(object), typeof(RevealedTimeline).BaseType);
        Assert.Empty(typeof(RevealedTimeline).GetInterfaces().Where(i => i != typeof(IEquatable<RevealedTimeline>)));

        Assert.DoesNotContain(
            typeof(RevealedTimeline).GetMethods().Select(m => m.Name),
            name => name is "op_Implicit" or "op_Explicit");

        Assert.DoesNotContain(
            typeof(RevealedTimeline).GetProperties().Select(p => p.PropertyType),
            type => type == typeof(WaveTimeline));
    }

    [Fact]
    public void Playback_accepts_only_the_combat_contract()
    {
        // The single load-bearing signature. If this ever widens to accept both, a revealed force
        // becomes animatable as though it were a promise about combat - and players who read the
        // Visible Threat as a promise feel the game break it when reinforcements land.
        Type[] parameters = [.. typeof(TimelinePlayer)
            .GetConstructors()
            .Single()
            .GetParameters()
            .Select(p => p.ParameterType)];

        Assert.Equal([typeof(WaveTimeline), typeof(TuningData)], parameters);
    }

    [Fact]
    public void A_visible_threat_still_carries_no_final_forecast_anywhere_on_it()
    {
        Assert.DoesNotContain(
            typeof(VisibleThreat).GetProperties().Select(p => p.PropertyType),
            type => type == typeof(FinalForecast) || type == typeof(WaveTimeline));
    }
}
