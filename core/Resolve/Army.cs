namespace Bastion.Core.Resolve;

/// <summary>Where a unit came from. Reported so a leak can be explained.</summary>
public enum SpawnSource
{
    /// <summary>The encounter's own composition.</summary>
    BaseWave,

    /// <summary>The Dealer's upcard, visible from the opening deal.</summary>
    Vanguard,

    /// <summary>A card the Dealer drew while resolving. Not revealed during the draw.</summary>
    DealerDraw,
}

/// <summary>
/// One unit's entry into the wave: what it is, where, and when.
/// </summary>
/// <remarks>
/// <see cref="SpawnIndex"/> is the tie-break of record for the entire simulation. Every ordering
/// question the resolver faces - which of two equidistant enemies a tower shoots, which of two
/// simultaneous events is written first - resolves through it, per docs/design/05-battlefield.md:
/// "Ties resolve by spawn order."
/// </remarks>
public sealed record EnemySpawn
{
    public required int SpawnIndex { get; init; }
    public required string EnemyId { get; init; }
    public required int LaneIndex { get; init; }
    public required double SpawnTime { get; init; }
    public required double StartPosition { get; init; }
    public required SpawnSource Source { get; init; }
}

/// <summary>
/// The force revealed during the draw: the base wave plus the Dealer's Vanguard.
/// </summary>
/// <remarks>
/// <para>
/// A separate type from <see cref="CompleteArmy"/>, not a flag, because it is the input half of the
/// forecast split. The Dealer's hidden card and subsequent draws have not resolved, so a run
/// against this force is exact about a <i>smaller question</i> - what is currently on the field -
/// and cannot be a prediction of the wave.
/// </para>
/// <para>
/// Making the army types distinct as well as the forecast types means a Final Forecast cannot be
/// produced from the revealed force even by mistake.
/// </para>
/// </remarks>
public sealed record RevealedArmy
{
    public required IReadOnlyList<EnemySpawn> Spawns { get; init; }
}

/// <summary>
/// The complete army: base wave, Vanguard, and every card the Dealer drew.
/// </summary>
/// <remarks>
/// Available only after the Dealer resolves - which happens on stand <b>or bust</b>, because
/// resolution is purely "deploy" and there is no outcome for a bust to dodge.
/// </remarks>
public sealed record CompleteArmy
{
    public required IReadOnlyList<EnemySpawn> Spawns { get; init; }
}
