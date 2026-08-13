using Bastion.Core.Board;
using Bastion.Core.Config;

namespace Bastion.Core.Resolve;

/// <summary>How a unit left the field.</summary>
public enum UnitExit
{
    /// <summary>Still standing when the wave ended.</summary>
    Survived,

    /// <summary>Killed by a tower, or by the Overload burst.</summary>
    Died,

    /// <summary>Reached the end of the path.</summary>
    Leaked,
}

/// <summary>
/// Everything one unit did, reconstructed from recorded events.
/// </summary>
/// <remarks>
/// <para>
/// Every consequential fact here - that the unit existed, its remaining health, when it died or
/// leaked - is read straight from timeline events. Nothing is simulated. Only
/// <see cref="PositionAt"/> between a unit's spawn and its exit is interpolated, and that is
/// cosmetic.
/// </para>
/// <para>
/// The mutating members are <c>internal</c>: the build pass inside this assembly fills a track in,
/// and everything outside reads it. A track that could be edited after the fact would be a way for
/// a renderer to contradict the resolver, which is the one thing the recording exists to prevent.
/// </para>
/// </remarks>
public sealed class UnitTrack
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

    /// <summary>When it left the field, or the wave's end if it never did.</summary>
    public double ExitTime { get; internal set; }

    public UnitExit Exit { get; internal set; }

    /// <summary>Damage it does on leaking. Zero unless <see cref="Exit"/> is <see cref="UnitExit.Leaked"/>.</summary>
    public int LeakDamage { get; internal set; }

    /// <summary>Every hit it took, in event order: when, and how much got through armor.</summary>
    public IReadOnlyList<(double Time, double Amount)> Damage => DamageMarks;

    /// <summary>Every slow applied to it, as the interval each one covered.</summary>
    public IReadOnlyList<(double Start, double Until)> Slows => SlowSpans;

    internal List<(double Time, double Amount)> DamageMarks { get; } = [];

    internal List<(double Start, double Until)> SlowSpans { get; } = [];

    internal void SetExit(double time, UnitExit exit, int leakDamage)
    {
        ExitTime = time;
        Exit = exit;
        LeakDamage = leakDamage;
    }

    /// <summary>Remaining health fraction from damage recorded up to and including <paramref name="time"/>.</summary>
    public double HealthFractionAt(double time)
    {
        double dealt = DamageMarks.Where(d => d.Time <= time).Sum(d => d.Amount);
        return Math.Max(0.0, (MaxHealth - dealt) / MaxHealth);
    }

    public bool IsSlowedAt(double time) => SlowSpans.Any(s => time >= s.Start && time < s.Until);

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

/// <summary>
/// When one tower was actually shooting into one lane, and when each shot landed.
/// </summary>
/// <remarks>
/// <para>
/// This is the timeline's <i>tower engagement window</i>: not the geometric window
/// (<see cref="March.Engagement.WindowForSocket"/>, which is path distance) but the stretch of the
/// clock the tower spent firing. The March step's cost reads off the difference between two of
/// these - <b>"this cannon loses two shots"</b> rather than "entry moves to 4.0"
/// (docs/design/14-encounter-timeline.md).
/// </para>
/// <para>
/// <see cref="Shots"/> counts <b>primary</b> shots only, so it agrees with
/// <see cref="TowerActivity.Shots"/>. A Club's splash lands on the same shot and adds damage, not
/// another attack; counting it here would make the timeline disagree with the lane's own tower
/// readout about how many times a tower fired.
/// </para>
/// <para>
/// A tower that never fired has no events and therefore no band. That is deliberate - the strip is
/// built from the recording and invents nothing - so a view that wants a row per placed tower reads
/// the roster off the board and matches by socket.
/// </para>
/// </remarks>
public sealed record TowerBand
{
    public required SocketRef Socket { get; init; }
    public required int LaneIndex { get; init; }

    /// <summary>When each primary shot landed, in order.</summary>
    public required IReadOnlyList<double> ShotTimes { get; init; }

    /// <summary>Primary shots fired into this lane. Agrees with <see cref="TowerActivity.Shots"/>.</summary>
    public int Shots => ShotTimes.Count;

    public double FirstFire => ShotTimes[0];

    public double LastFire => ShotTimes[^1];
}

/// <summary>One lane's row of the strip: the units walking it, and the towers firing into it.</summary>
public sealed record LaneStrip
{
    public required int LaneIndex { get; init; }

    /// <summary>Units in spawn order - which is the order the lane meets them.</summary>
    public required IReadOnlyList<UnitTrack> Units { get; init; }

    /// <summary>Towers that fired into this lane, in socket order. A junction tower appears in both lanes.</summary>
    public required IReadOnlyList<TowerBand> Towers { get; init; }
}

/// <summary>
/// The drawing model behind the encounter timeline: recorded events, arranged per lane over time.
/// </summary>
/// <remarks>
/// <para>
/// <b>A presentation concern, not a forecast.</b> It is built from a raw event list, so both
/// <see cref="WaveTimeline"/> and <see cref="RevealedTimeline"/> can produce one - which is what
/// lets the timeline be drawn during the draw and after the Dealer resolves without either forecast
/// type gaining a conversion to the other. What a strip cannot do is animate: playback goes through
/// <see cref="TimelinePlayer"/>, which takes a <see cref="WaveTimeline"/> and nothing else.
/// </para>
/// <para>
/// It reconstructs, it never simulates. <see cref="TimelinePlayer"/> builds on the same tracks
/// rather than walking the events a second time: a second account of the same recording is exactly
/// the drift the recording exists to make impossible.
/// </para>
/// </remarks>
public sealed record TimelineStrip
{
    /// <summary>How long the run takes, end to end. The strip's time axis.</summary>
    public required double DurationSeconds { get; init; }

    /// <summary>One entry per tuned lane, in lane order, whether or not anything happened in it.</summary>
    public required IReadOnlyList<LaneStrip> Lanes { get; init; }

    /// <summary>Every unit, in spawn order, across all lanes.</summary>
    public required IReadOnlyList<UnitTrack> Units { get; init; }

    /// <summary>The schedule of the revealed force. Drawable during the draw; never playable.</summary>
    public static TimelineStrip From(RevealedTimeline timeline, TuningData tuning)
    {
        ArgumentNullException.ThrowIfNull(timeline);

        return Build(timeline.Events, timeline.DurationSeconds, tuning);
    }

    /// <summary>The recorded wave. The same strip shape, over the combat contract.</summary>
    public static TimelineStrip From(WaveTimeline timeline, TuningData tuning)
    {
        ArgumentNullException.ThrowIfNull(timeline);

        return Build(timeline.Events, timeline.DurationSeconds, tuning);
    }

    /// <summary>
    /// Reconstructs tracks and bands from an event list.
    /// </summary>
    /// <remarks>
    /// Takes the events rather than either wrapper type on purpose: a method accepting both wrappers
    /// would be a conversion between them in all but name, and the two forecasts are kept apart by
    /// the type system rather than by discipline (docs/design/05-battlefield.md § Implementation).
    /// </remarks>
    internal static TimelineStrip Build(
        IReadOnlyList<TimelineEvent> events, double durationSeconds, TuningData tuning)
    {
        ArgumentNullException.ThrowIfNull(events);
        ArgumentNullException.ThrowIfNull(tuning);

        IReadOnlyList<UnitTrack> tracks = BuildTracks(events, durationSeconds, tuning);
        Dictionary<(int Lane, SocketRef Socket), List<double>> shots = [];

        foreach (ShotEvent shot in events.OfType<ShotEvent>())
        {
            // Primary shots only - a splash hit is part of the shot that caused it. See TowerBand.
            if (shot.IsSplash)
            {
                continue;
            }

            (int, SocketRef) key = (shot.LaneIndex, shot.Socket);

            if (!shots.TryGetValue(key, out List<double>? times))
            {
                times = [];
                shots[key] = times;
            }

            times.Add(shot.Time);
        }

        List<LaneStrip> lanes = [];

        for (int lane = 0; lane < tuning.Geometry.Lanes; lane++)
        {
            int index = lane;

            lanes.Add(new LaneStrip
            {
                LaneIndex = index,
                Units = [.. tracks.Where(t => t.LaneIndex == index)],
                Towers =
                [
                    .. shots
                        .Where(entry => entry.Key.Lane == index)
                        .OrderBy(entry => entry.Key.Socket.IsJunction)
                        .ThenBy(entry => entry.Key.Socket.SocketIndex)
                        .Select(entry => new TowerBand
                        {
                            Socket = entry.Key.Socket,
                            LaneIndex = index,
                            ShotTimes = entry.Value,
                        }),
                ],
            });
        }

        return new TimelineStrip
        {
            DurationSeconds = durationSeconds,
            Lanes = lanes,
            Units = tracks,
        };
    }

    private static IReadOnlyList<UnitTrack> BuildTracks(
        IReadOnlyList<TimelineEvent> events, double durationSeconds, TuningData tuning)
    {
        double slowMultiplier = tuning.Suits.Spades.SlowMultiplier;

        Dictionary<int, UnitTrack> tracks = [];

        // Spawns anchor every track. A unit with no spawn cannot appear, so this is the whole roster.
        foreach (SpawnEvent spawn in events.OfType<SpawnEvent>())
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
                ExitTime = durationSeconds,
                Exit = UnitExit.Survived,
            };
        }

        foreach (TimelineEvent evt in events)
        {
            switch (evt)
            {
                case ShotEvent shot when tracks.TryGetValue(shot.TargetSpawnIndex, out UnitTrack? hit):
                    hit.DamageMarks.Add((shot.Time, shot.DamageApplied));
                    break;

                case SlowEvent slow when tracks.TryGetValue(slow.SpawnIndex, out UnitTrack? slowed):
                    slowed.SlowSpans.Add((slow.Time, slow.UntilTime));
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

                        struck.DamageMarks.Add((overload.Time, hit.DamageApplied));

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
}
