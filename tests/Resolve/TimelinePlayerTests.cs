using Bastion.Core.Board;
using Bastion.Core.Cards;
using Bastion.Core.Resolve;

namespace Bastion.Core.Tests.Resolve;

/// <summary>
/// <see cref="TimelinePlayer"/> replaying a recorded wave: the presentation reads the timeline, it
/// never re-simulates.
/// </summary>
/// <remarks>
/// The load-bearing check is <see cref="Playback_at_the_end_reproduces_the_forecasts_leak_ledger"/>:
/// if playback and the Final Forecast ever disagree about what leaked, the combat contract is a lie.
/// Everything else pins the cursor's mechanics.
/// </remarks>
public sealed class TimelinePlayerTests
{
    // The worked-example board from the composition root: real spawns, shots, deaths, and leaks.
    private static readonly BoardState Board = Fixture.Board(
        Fixture.Tower(Rank.Six, Family.Club, Fixture.Socket(0, 1), formation: 1.30),
        Fixture.Tower(Rank.Eight, Family.Club, Fixture.Socket(0, 2), formation: 1.30),
        Fixture.Tower(Rank.Four, Family.Spade, Fixture.Socket(1, 0), formation: 1.30));

    private static FinalForecast Forecast() =>
        Fixture.ResolveComplete(
            Fixture.Example,
            Board,
            Fixture.Complete(Fixture.Example, [Rank.Ten, Rank.Six, Rank.Seven], Board.Entry));

    private static TimelinePlayer Player(FinalForecast forecast) =>
        new(forecast.Timeline, Fixture.Tuning);

    [Fact]
    public void Nothing_has_leaked_at_the_opening_instant()
    {
        PlaybackFrame frame = Player(Forecast()).FrameAt(0.0);

        Assert.Equal(0, frame.LeakedCount);
        Assert.Equal(0, frame.LeakDamageSoFar);
        Assert.False(frame.IsComplete);
    }

    [Fact]
    public void The_field_is_clear_once_the_cursor_reaches_the_end()
    {
        TimelinePlayer player = Player(Forecast());
        PlaybackFrame frame = player.FrameAt(player.Duration);

        Assert.Empty(frame.LiveEnemies);
        Assert.True(frame.IsComplete);
    }

    [Fact]
    public void Playback_at_the_end_reproduces_the_forecasts_leak_ledger()
    {
        FinalForecast forecast = Forecast();
        TimelinePlayer player = Player(forecast);

        int forecastLeaks = forecast.Lanes.Sum(l => l.LeakedUnits.Count);
        int forecastLeakDamage = forecast.Lanes.Sum(l => l.LeakedUnits.Sum(u => u.LeakDamage));

        PlaybackFrame end = player.FrameAt(player.Duration);

        Assert.Equal(forecastLeaks, end.LeakedCount);
        Assert.Equal(forecastLeakDamage, end.LeakDamageSoFar);

        // And the same total the timeline itself records, so the reconstruction is exact.
        Assert.Equal(
            forecast.Timeline.Events.OfType<LeakEvent>().Sum(e => e.LeakDamage),
            end.LeakDamageSoFar);
    }

    /// <summary>
    /// The per-lane split is what the combat screen shows against a contract stated per lane, so it
    /// has to reproduce the forecast lane by lane - not merely add up to the same total.
    /// </summary>
    [Fact]
    public void Playback_reproduces_the_forecasts_leak_ledger_lane_by_lane()
    {
        FinalForecast forecast = Forecast();
        PlaybackFrame end = Player(forecast).FrameAt(forecast.Timeline.DurationSeconds);

        Assert.Equal(Fixture.Tuning.Geometry.Lanes, end.Lanes.Count);

        foreach (LaneOutcome lane in forecast.Lanes)
        {
            LaneLeakProgress played = end.Lanes[lane.LaneIndex];

            Assert.Equal(lane.LaneIndex, played.LaneIndex);
            Assert.Equal(lane.LeakedUnits.Count, played.LeakedCount);
            Assert.Equal(lane.LeakedUnits.Sum(u => u.LeakDamage), played.LeakDamage);
        }

        // And the split is a split, not a second tally alongside the first.
        Assert.Equal(end.LeakedCount, end.Lanes.Sum(l => l.LeakedCount));
        Assert.Equal(end.LeakDamageSoFar, end.Lanes.Sum(l => l.LeakDamage));
    }

    /// <summary>A lane that held reports a zero rather than dropping out of the readout.</summary>
    [Fact]
    public void Every_lane_appears_at_every_instant_including_before_anything_leaks()
    {
        PlaybackFrame opening = Player(Forecast()).FrameAt(0.0);

        Assert.Equal(Fixture.Tuning.Geometry.Lanes, opening.Lanes.Count);
        Assert.All(opening.Lanes, lane => Assert.Equal(0, lane.LeakDamage));
    }

    [Fact]
    public void The_leak_count_never_decreases_as_the_cursor_advances()
    {
        TimelinePlayer player = Player(Forecast());

        int previous = 0;
        for (double t = 0.0; t <= player.Duration + 1.0; t += 0.25)
        {
            int now = player.FrameAt(t).LeakedCount;
            Assert.True(now >= previous, $"Leak count dropped from {previous} to {now} at t={t}.");
            previous = now;
        }
    }

    [Fact]
    public void A_unit_is_on_the_field_up_to_its_death_and_gone_at_it()
    {
        FinalForecast forecast = Forecast();
        TimelinePlayer player = Player(forecast);

        DeathEvent death = forecast.Timeline.Events.OfType<DeathEvent>().First();

        bool aliveJustBefore = player.FrameAt(death.Time - 0.01).LiveEnemies
            .Any(e => e.SpawnIndex == death.SpawnIndex);
        bool goneAtDeath = player.FrameAt(death.Time).LiveEnemies
            .All(e => e.SpawnIndex != death.SpawnIndex);

        Assert.True(aliveJustBefore, "The unit should still be drawn the instant before it dies.");
        Assert.True(goneAtDeath, "The unit should be removed exactly at its recorded death.");
    }

    [Fact]
    public void Health_fractions_stay_within_bounds_and_someone_takes_a_hit()
    {
        TimelinePlayer player = Player(Forecast());

        bool anyoneDamaged = false;
        for (double t = 0.0; t <= player.Duration; t += 0.25)
        {
            foreach (EnemyPlaybackState enemy in player.FrameAt(t).LiveEnemies)
            {
                Assert.InRange(enemy.HealthFraction, 0.0, 1.0);
                if (enemy.HealthFraction < 1.0)
                {
                    anyoneDamaged = true;
                }
            }
        }

        Assert.True(anyoneDamaged, "A wave with towers that fire must show at least one damaged unit.");
    }

    [Fact]
    public void EventsBetween_partitions_the_timeline_without_loss_or_repeat()
    {
        FinalForecast forecast = Forecast();
        TimelinePlayer player = Player(forecast);

        // A window spanning the whole run returns every event exactly once (half-open at the low end,
        // so start below the first event's time).
        IReadOnlyList<TimelineEvent> all = player.EventsBetween(-1.0, player.Duration);
        Assert.Equal(forecast.Timeline.Events.Count, all.Count);

        // Splitting the window in two neither drops nor double-counts an event.
        double mid = player.Duration / 2.0;
        int firstHalf = player.EventsBetween(-1.0, mid).Count;
        int secondHalf = player.EventsBetween(mid, player.Duration).Count;
        Assert.Equal(all.Count, firstHalf + secondHalf);
    }
}
