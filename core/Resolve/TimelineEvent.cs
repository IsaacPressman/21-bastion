using Bastion.Core.Board;

namespace Bastion.Core.Resolve;

/// <summary>
/// Something that happened during a wave, stamped with when.
/// </summary>
/// <remarks>
/// <para>
/// <b>This is what combat playback animates.</b> docs/ARCHITECTURE.md: "the visual wave is a
/// presentation of a resolver run - replaying a recorded timeline, not re-simulating." A renderer
/// that reads these and draws them cannot drift from the forecast, because there is nothing for it
/// to drift from: it is not computing anything.
/// </para>
/// <para>
/// Events are emitted in tick order, and within a tick in the order the resolver's pinned phase
/// order produces them. That total order is part of the determinism contract.
/// </para>
/// </remarks>
public abstract record TimelineEvent
{
    /// <summary>Simulation tick this happened on.</summary>
    public required int Tick { get; init; }

    /// <summary>Wall time within the wave, in seconds.</summary>
    public required double Time { get; init; }

    public required int LaneIndex { get; init; }
}

/// <summary>A unit entered the lane.</summary>
public sealed record SpawnEvent : TimelineEvent
{
    public required int SpawnIndex { get; init; }
    public required string EnemyId { get; init; }
    public required SpawnSource Source { get; init; }
    public required double Position { get; init; }
}

/// <summary>A tower hit something.</summary>
public sealed record ShotEvent : TimelineEvent
{
    public required SocketRef Socket { get; init; }
    public required int TargetSpawnIndex { get; init; }

    /// <summary>Shot damage in this lane, before armor. Already carries any junction penalty.</summary>
    public required double DamageBeforeArmor { get; init; }

    /// <summary>What the target actually lost, after armor and the damage floor.</summary>
    public required double DamageApplied { get; init; }

    /// <summary>True for a Club's secondary hits, false for the primary target.</summary>
    public required bool IsSplash { get; init; }
}

/// <summary>A Spade slowed something.</summary>
public sealed record SlowEvent : TimelineEvent
{
    public required SocketRef Socket { get; init; }
    public required int SpawnIndex { get; init; }

    /// <summary>When the slow lapses. A second application moves this out rather than compounding.</summary>
    public required double UntilTime { get; init; }
}

/// <summary>A unit died.</summary>
public sealed record DeathEvent : TimelineEvent
{
    public required int SpawnIndex { get; init; }
    public required string EnemyId { get; init; }

    /// <summary>The tower that landed the killing hit.</summary>
    public required SocketRef KilledBy { get; init; }
}

/// <summary>A unit reached the end of the path.</summary>
public sealed record LeakEvent : TimelineEvent
{
    public required int SpawnIndex { get; init; }
    public required string EnemyId { get; init; }
    public required int LeakDamage { get; init; }
    public required LeakCause Cause { get; init; }
}

/// <summary>
/// The recorded run of a wave, start to finish.
/// </summary>
/// <remarks>
/// Reachable only from a <see cref="FinalForecast"/>. A Visible Threat has no timeline, because
/// only the Final Forecast is the combat contract and only it describes a wave that will actually
/// run - so there is nothing on a Visible Threat for playback to animate. That asymmetry is what
/// makes the forecast split structural rather than documentary.
/// </remarks>
public sealed record WaveTimeline
{
    public required IReadOnlyList<TimelineEvent> Events { get; init; }

    /// <summary>How long the wave took. Compare against <c>combat.waveResolutionSeconds*</c>.</summary>
    public required double DurationSeconds { get; init; }

    /// <summary>
    /// Structural equality, including the event list.
    /// </summary>
    /// <remarks>
    /// See <see cref="LaneOutcome.Equals(LaneOutcome?)"/> for why the synthesised version is not
    /// good enough: a determinism check that compares list references can never fail.
    /// </remarks>
    public bool Equals(WaveTimeline? other) =>
        other is not null
        && DurationSeconds.Equals(other.DurationSeconds)
        && Events.SequenceEqual(other.Events);

    public override int GetHashCode() => HashCode.Combine(DurationSeconds, Events.Count);
}
