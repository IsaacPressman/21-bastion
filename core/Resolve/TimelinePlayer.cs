using Bastion.Core.Config;

namespace Bastion.Core.Resolve;

/// <summary>
/// A unit as playback should draw it at one instant: where it is, how hurt, whether slowed.
/// </summary>
/// <remarks>
/// Reconstructed from the recorded <see cref="WaveTimeline"/>, never re-simulated. Every consequential
/// fact here - that the unit is alive at all, its remaining health, when it dies or leaks - is read
/// straight from timeline events, so a renderer using this can no more contradict the
/// <see cref="FinalForecast"/> than the timeline can contradict itself. Only <see cref="Position"/>
/// between a unit's spawn and its exit is interpolated, and that is cosmetic.
/// </remarks>
public sealed record EnemyPlaybackState
{
    public required int SpawnIndex { get; init; }
    public required string EnemyId { get; init; }
    public required int LaneIndex { get; init; }
    public required SpawnSource Source { get; init; }

    /// <summary>Path position, entry to <c>pathLength</c>. Cosmetically interpolated between anchors.</summary>
    public required double Position { get; init; }

    /// <summary>Remaining health as a fraction of maximum, from summed applied damage. 1.0 = untouched.</summary>
    public required double HealthFraction { get; init; }

    /// <summary>Whether a Spade slow is in effect at this instant.</summary>
    public required bool IsSlowed { get; init; }
}

/// <summary>
/// One lane's running leak total at an instant of playback.
/// </summary>
/// <remarks>
/// The Final Forecast promises per lane - "Lane 0 takes 9 of 19" - so the wave has to be watchable
/// per lane for the promise to be checkable at all. A single summed counter cannot be compared
/// against a contract that was never stated as a sum, and lanes are not interchangeable
/// (docs/design/05-battlefield.md).
/// </remarks>
public sealed record LaneLeakProgress
{
    public required int LaneIndex { get; init; }

    /// <summary>Units of this lane that have leaked at or before the frame's time.</summary>
    public required int LeakedCount { get; init; }

    /// <summary>Damage this lane has taken at or before the frame's time.</summary>
    public required int LeakDamage { get; init; }
}

/// <summary>
/// The drawable state of the whole wave at one instant of playback.
/// </summary>
public sealed record PlaybackFrame
{
    public required double Time { get; init; }
    public required IReadOnlyList<EnemyPlaybackState> LiveEnemies { get; init; }

    /// <summary>Units that have leaked at or before <see cref="Time"/>.</summary>
    public required int LeakedCount { get; init; }

    /// <summary>Total leak damage dealt at or before <see cref="Time"/>.</summary>
    public required int LeakDamageSoFar { get; init; }

    /// <summary>The same totals split by lane, in lane order. One entry per tuned lane, always.</summary>
    public required IReadOnlyList<LaneLeakProgress> Lanes { get; init; }

    /// <summary>True once the cursor is at or past the wave's end.</summary>
    public required bool IsComplete { get; init; }
}

/// <summary>
/// Replays a recorded <see cref="WaveTimeline"/> as a function of a time cursor, for combat playback.
/// </summary>
/// <remarks>
/// <para>
/// <b>This is the presentation reading a recording, not a second simulation.</b> docs/ARCHITECTURE.md:
/// "the visual wave is a presentation of a resolver run - replaying a recorded timeline, not
/// re-simulating." There is exactly one simulation path (the resolver); this class only interprets its
/// output over time. It lives in the engine-free core so it is unit-testable headless, and so that a
/// Godot renderer above it can stay a thin view.
/// </para>
/// <para>
/// Immutable and precomputed: the constructor indexes the timeline once, and <see cref="FrameAt"/> is a
/// pure query. Feed the cursor forward for normal playback, jump it to <see cref="Duration"/> to skip.
/// Discrete effects - shots, slows, deaths, leaks, the Overload burst - come from
/// <see cref="EventsBetween"/>, so a renderer flashes exactly the events the resolver recorded.
/// </para>
/// <para>
/// <b>It takes a <see cref="WaveTimeline"/> and nothing else.</b> That signature is what keeps a
/// <see cref="VisibleThreat"/> unplayable: its <see cref="RevealedTimeline"/> will not compile here, so
/// the revealed force can be drawn on the encounter timeline and can still never be animated as though
/// it were the combat contract.
/// </para>
/// <para>
/// The unit tracks come from <see cref="TimelineStrip"/> rather than being rebuilt here. Both surfaces
/// read the same recording, and a second walker over the same events would be a second account of it -
/// the exact drift the recording exists to make impossible.
/// </para>
/// </remarks>
public sealed class TimelinePlayer
{
    private readonly WaveTimeline _timeline;
    private readonly double _pathLength;
    private readonly int _lanes;
    private readonly IReadOnlyList<UnitTrack> _tracks;

    public TimelinePlayer(WaveTimeline timeline, TuningData tuning)
    {
        ArgumentNullException.ThrowIfNull(timeline);
        ArgumentNullException.ThrowIfNull(tuning);

        _timeline = timeline;
        _pathLength = tuning.Geometry.PathLength;
        _lanes = tuning.Geometry.Lanes;
        _tracks = TimelineStrip.From(timeline, tuning).Units;
    }

    /// <summary>The wave's length. Jump the cursor here to skip playback to its recorded end.</summary>
    public double Duration => _timeline.DurationSeconds;

    /// <summary>The drawable state of the wave at <paramref name="time"/> seconds.</summary>
    public PlaybackFrame FrameAt(double time)
    {
        List<EnemyPlaybackState> live = new();
        int leakedCount = 0;
        int leakDamage = 0;

        // Sized to the tuned lane count rather than to whatever lanes happen to have leaked, so a lane
        // that held reports a zero instead of vanishing from the readout.
        int[] laneCount = new int[_lanes];
        int[] laneDamage = new int[_lanes];

        foreach (UnitTrack track in _tracks)
        {
            if (track.Exit == UnitExit.Leaked && time >= track.ExitTime)
            {
                leakedCount++;
                leakDamage += track.LeakDamage;

                if (track.LaneIndex >= 0 && track.LaneIndex < _lanes)
                {
                    laneCount[track.LaneIndex]++;
                    laneDamage[track.LaneIndex] += track.LeakDamage;
                }
            }

            bool onField = time >= track.SpawnTime && time < track.ExitTime;
            if (!onField)
            {
                continue;
            }

            live.Add(new EnemyPlaybackState
            {
                SpawnIndex = track.SpawnIndex,
                EnemyId = track.EnemyId,
                LaneIndex = track.LaneIndex,
                Source = track.Source,
                Position = track.PositionAt(time, _pathLength),
                HealthFraction = track.HealthFractionAt(time),
                IsSlowed = track.IsSlowedAt(time),
            });
        }

        return new PlaybackFrame
        {
            Time = time,
            LiveEnemies = live,
            LeakedCount = leakedCount,
            LeakDamageSoFar = leakDamage,
            Lanes = [.. Enumerable.Range(0, _lanes).Select(lane => new LaneLeakProgress
            {
                LaneIndex = lane,
                LeakedCount = laneCount[lane],
                LeakDamage = laneDamage[lane],
            })],
            IsComplete = time >= Duration,
        };
    }

    /// <summary>
    /// The recorded events in the half-open interval <c>(from, to]</c>, in timeline order.
    /// </summary>
    /// <remarks>
    /// For per-step effect rendering: pass the previous and current cursor times and flash whatever
    /// crossed. Half-open so advancing the cursor never replays or drops an event at a boundary.
    /// </remarks>
    public IReadOnlyList<TimelineEvent> EventsBetween(double from, double to) =>
        [.. _timeline.Events.Where(e => e.Time > from && e.Time <= to)];
}
