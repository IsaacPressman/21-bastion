using System.Reflection;
using Bastion.Core.Board;
using Bastion.Core.Cards;
using Bastion.Core.Config;
using Bastion.Core.Resolve;

namespace Bastion.Core.Tests.Resolve;

/// <summary>
/// The strip reconstructs the recording, and both forecasts produce the same shape from it.
/// </summary>
/// <remarks>
/// The strip is a drawing model, not a third forecast. It is built from a raw event list, which is
/// what lets the encounter timeline be drawn during the draw and after the Dealer resolves without
/// either forecast type gaining a conversion to the other.
/// </remarks>
public sealed class TimelineStripTests
{
    private static BoardState TwoTowers() => Fixture.Board(
        Fixture.Tower(Rank.Nine, Family.Club, Fixture.Socket(0, 0)),
        Fixture.Tower(Rank.Five, Family.Spade, Fixture.Socket(0, 1)));

    private static FinalForecast Forecast(out EncounterTuning encounter, out BoardState board)
    {
        encounter = Fixture.Solo("armored_soldier", 3);
        board = TwoTowers();

        return Fixture.ResolveComplete(
            encounter, board, Fixture.Complete(encounter, [Rank.Two], board.Entry));
    }

    [Fact]
    public void Every_spawned_unit_gets_a_track_and_every_track_has_an_exit()
    {
        FinalForecast forecast = Forecast(out _, out _);
        TimelineStrip strip = TimelineStrip.From(forecast.Timeline, Fixture.Tuning);

        int spawns = forecast.Timeline.Events.OfType<SpawnEvent>().Count();

        Assert.Equal(spawns, strip.Units.Count);
        Assert.All(strip.Units, unit => Assert.True(unit.ExitTime >= unit.SpawnTime));

        // A leaker's exit is stamped with the recorded leak, so the strip and the lane readout name
        // the same moment as the lane breaking.
        foreach (UnitTrack leaked in strip.Units.Where(u => u.Exit == UnitExit.Leaked))
        {
            LeakEvent recorded = forecast.Timeline.Events
                .OfType<LeakEvent>()
                .Single(e => e.SpawnIndex == leaked.SpawnIndex);

            Assert.Equal(recorded.Time, leaked.ExitTime);
            Assert.Equal(recorded.LeakDamage, leaked.LeakDamage);
        }
    }

    [Fact]
    public void A_lane_row_exists_for_every_tuned_lane_even_when_nothing_happens_in_it()
    {
        // Lane 1 is empty in an isolated encounter. It still gets a row: a lane that vanishes from
        // the strip reads as a lane with no stake, and the stakes are the reason triage is a
        // decision at all.
        FinalForecast forecast = Forecast(out _, out _);
        TimelineStrip strip = TimelineStrip.From(forecast.Timeline, Fixture.Tuning);

        Assert.Equal(Fixture.Tuning.Geometry.Lanes, strip.Lanes.Count);
        Assert.Equal([.. Enumerable.Range(0, strip.Lanes.Count)], [.. strip.Lanes.Select(l => l.LaneIndex)]);
    }

    [Fact]
    public void Tower_bands_count_primary_shots_and_agree_with_the_lane_readout()
    {
        FinalForecast forecast = Forecast(out _, out _);
        TimelineStrip strip = TimelineStrip.From(forecast.Timeline, Fixture.Tuning);

        foreach (TowerActivity activity in forecast.Lanes[0].TowerActivity.Where(a => a.Shots > 0))
        {
            TowerBand band = strip.Lanes[0].Towers.Single(b => b.Socket == activity.Socket);

            Assert.Equal(activity.Shots, band.Shots);
            Assert.Equal(activity.FirstFireTime, band.FirstFire);
            Assert.Equal(activity.LastFireTime, band.LastFire);
        }
    }

    [Fact]
    public void The_revealed_schedule_and_the_recorded_wave_build_the_same_strip_from_the_same_run()
    {
        // A Dealer hand of just the upcard makes the revealed force and the complete army the same
        // force. The two strips must then be identical, because they are two drawings of one run -
        // which is the property that lets the timeline stay one surface across the phase change.
        EncounterTuning encounter = Fixture.Solo("armored_soldier", 3);
        BoardState board = TwoTowers();

        TimelineStrip revealed = TimelineStrip.From(
            Fixture.ResolveRevealed(encounter, board, Fixture.Revealed(encounter, Rank.Ten, board.Entry)).Schedule,
            Fixture.Tuning);

        TimelineStrip recorded = TimelineStrip.From(
            Fixture.ResolveComplete(encounter, board, Fixture.Complete(encounter, [Rank.Ten], board.Entry)).Timeline,
            Fixture.Tuning);

        Assert.Equal(revealed.DurationSeconds, recorded.DurationSeconds);
        Assert.Equal(
            revealed.Units.Select(u => (u.SpawnIndex, u.Exit, u.ExitTime)),
            recorded.Units.Select(u => (u.SpawnIndex, u.Exit, u.ExitTime)));

        Assert.Equal(
            revealed.Lanes[0].Towers.Select(b => (b.Socket, b.Shots)),
            recorded.Lanes[0].Towers.Select(b => (b.Socket, b.Shots)));
    }

    [Fact]
    public void A_track_cannot_be_edited_from_outside_the_core()
    {
        // The mutating members are internal on purpose. A track a renderer could edit after the fact
        // would be a way for the presentation to contradict the resolver, which is precisely what
        // replaying a recording instead of re-simulating exists to prevent.
        //
        // Init-only setters are exempt: they are public but only reachable from an object
        // initializer, so they build a track rather than edit one.
        string[] editable =
        [
            .. typeof(UnitTrack)
                .GetProperties()
                .Where(p => p.SetMethod is { IsPublic: true } setter && !IsInitOnly(setter))
                .Select(p => p.Name),
        ];

        Assert.Empty(editable);

        // And the two event lists are exposed as read-only views, not as the lists themselves.
        Assert.Equal(typeof(IReadOnlyList<(double, double)>), typeof(UnitTrack).GetProperty("Damage")!.PropertyType);
        Assert.Null(typeof(UnitTrack).GetProperty("Damage")!.SetMethod);
    }

    private static bool IsInitOnly(MethodInfo setter) =>
        setter.ReturnParameter.GetRequiredCustomModifiers().Any(m => m.Name == "IsExternalInit");
}
