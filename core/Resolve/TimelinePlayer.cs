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
        _tracks = BuildTracks(timeline, tuning);
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

    private static IReadOnlyList<UnitTrack> BuildTracks(WaveTimeline timeline, TuningData tuning)
    {
        double slowMultiplier = tuning.Suits.Spades.SlowMultiplier;

        Dictionary<int, UnitTrack> tracks = new();

        // Spawns anchor every track. A unit with no spawn cannot appear, so this is the whole roster.
        foreach (SpawnEvent spawn in timeline.Events.OfType<SpawnEvent>())
        {
            EnemyTuning type = tuning.Enemy(spawn.EnemyId);
            tracks[spawn.SpawnIndex] = new UnitTrack
            {
                SpawnIndex = spawn.SpawnIndex,
                EnemyId = spawn.EnemyId,
                LaneIndex = spawn.LaneIndex,
                Source = spawn.Source,
                SpawnTime = spawn.Time,
                StartPosition = spawn.Position,
                MaxHealth = type.Health,
                BaseSpeed = type.Speed,
                SlowMultiplier = slowMultiplier,
                // Absent an exit event, the unit is treated as surviving to the wave's end.
                ExitTime = timeline.DurationSeconds,
                Exit = UnitExit.Survived,
            };
        }

        foreach (TimelineEvent evt in timeline.Events)
        {
            switch (evt)
            {
                case ShotEvent shot when tracks.TryGetValue(shot.TargetSpawnIndex, out UnitTrack? hit):
                    hit.Damage.Add((shot.Time, shot.DamageApplied));
                    break;

                case SlowEvent slow when tracks.TryGetValue(slow.SpawnIndex, out UnitTrack? slowed):
                    slowed.Slows.Add((slow.Time, slow.UntilTime));
                    break;

                case DeathEvent death when tracks.TryGetValue(death.SpawnIndex, out UnitTrack? dead):
                    dead.SetExit(death.Time, UnitExit.Died, leakDamage: 0);
                    break;

                case LeakEvent leak when tracks.TryGetValue(leak.SpawnIndex, out UnitTrack? leaked):
                    leaked.SetExit(leak.Time, UnitExit.Leaked, leak.LeakDamage);
                    break;

                case OverloadEvent overload:
                    // The burst removes its victims without a DeathEvent (see OverloadEvent): it is the
                    // record that they died, so credit it as their exit. Every unit it touched is
                    // credited its own recorded share, so the survivor the spill stopped short of keeps
                    // the reduced health the resolver gave it rather than reading as untouched.
                    foreach (OverloadHit hit in overload.Hits)
                    {
                        if (!tracks.TryGetValue(hit.SpawnIndex, out UnitTrack? struck))
                        {
                            continue;
                        }

                        struck.Damage.Add((overload.Time, hit.DamageApplied));

                        if (hit.Killed)
                        {
                            struck.SetExit(overload.Time, UnitExit.Died, leakDamage: 0);
                        }
                    }

                    break;
            }
        }

        return [.. tracks.Values.OrderBy(t => t.SpawnIndex)];
    }

    private enum UnitExit
    {
        Survived,
        Died,
        Leaked,
    }

    /// <summary>Everything known about one unit's life, precomputed for O(1)-per-query frames.</summary>
    private sealed class UnitTrack
    {
        public required int SpawnIndex { get; init; }
        public required string EnemyId { get; init; }
        public required int LaneIndex { get; init; }
        public required SpawnSource Source { get; init; }
        public required double SpawnTime { get; init; }
        public required double StartPosition { get; init; }
        public required double MaxHealth { get; init; }
        public required double BaseSpeed { get; init; }
        public required double SlowMultiplier { get; init; }

        public double ExitTime { get; set; }
        public UnitExit Exit { get; set; }
        public int LeakDamage { get; set; }

        public List<(double Time, double Amount)> Damage { get; } = new();
        public List<(double Start, double Until)> Slows { get; } = new();

        public void SetExit(double time, UnitExit exit, int leakDamage)
        {
            ExitTime = time;
            Exit = exit;
            LeakDamage = leakDamage;
        }

        /// <summary>Remaining health fraction from applied damage recorded up to and including <paramref name="time"/>.</summary>
        public double HealthFractionAt(double time)
        {
            double dealt = Damage.Where(d => d.Time <= time).Sum(d => d.Amount);
            return Math.Max(0.0, (MaxHealth - dealt) / MaxHealth);
        }

        public bool IsSlowedAt(double time) => Slows.Any(s => time >= s.Start && time < s.Until);

        /// <summary>
        /// Cosmetic path position. A leaker is interpolated exactly between its spawn and the wall so it
        /// arrives when the recorded leak says (which already reflects any slow); everything else moves
        /// at base speed and is clamped at the wall - its precise sub-path position at death is not a
        /// consequence the forecast depends on.
        /// </summary>
        public double PositionAt(double time, double pathLength)
        {
            double elapsed = time - SpawnTime;

            if (Exit == UnitExit.Leaked && ExitTime > SpawnTime)
            {
                double span = ExitTime - SpawnTime;
                double progress = Math.Clamp(elapsed / span, 0.0, 1.0);
                return StartPosition + ((pathLength - StartPosition) * progress);
            }

            return Math.Clamp(StartPosition + (BaseSpeed * elapsed), StartPosition, pathLength);
        }
    }
}
