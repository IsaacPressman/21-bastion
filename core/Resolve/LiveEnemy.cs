using Bastion.Core.Config;

namespace Bastion.Core.Resolve;

/// <summary>
/// A unit while the simulation is running.
/// </summary>
/// <remarks>
/// <para>
/// The one mutable type in the resolver, and deliberately internal: the board state and the army
/// going in are immutable, and the timeline and outcomes coming out are immutable, so a resolver
/// run leaves nothing behind. That is what lets the same board be resolved several times per
/// forecast - once live and once per lane counterfactual - and get the same answer every time.
/// </para>
/// <para>
/// A class rather than a struct because the tick loop hands the same instance to targeting, damage,
/// and movement in turn, and copying semantics there would be a silent source of lost damage.
/// </para>
/// </remarks>
internal sealed class LiveEnemy
{
    internal required int SpawnIndex { get; init; }
    internal required EnemyTuning Type { get; init; }
    internal required SpawnSource Source { get; init; }

    internal double Position { get; set; }
    internal double Health { get; set; }
    internal bool Alive { get; set; } = true;

    /// <summary>Wall time the current slow lapses. Refreshed, never compounded.</summary>
    internal double SlowUntil { get; set; } = double.NegativeInfinity;

    /// <summary>Whether any tower ever had it as a legal in-range candidate.</summary>
    internal bool WasEverInRange { get; set; }

    /// <summary>The tower that landed the killing hit, once it is dead.</summary>
    internal Board.SocketRef KilledBy { get; set; }

    internal double HealthFraction => Math.Max(0.0, Health) / Type.Health;

    /// <summary>Speed this tick, after any Spade slow.</summary>
    internal double SpeedAt(double time, double slowMultiplier) =>
        time < SlowUntil ? Type.Speed * slowMultiplier : Type.Speed;
}
