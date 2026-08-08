namespace Bastion.Core.Resolve;

/// <summary>
/// What the wave will do. <b>This alone is the combat contract.</b>
/// </summary>
/// <remarks>
/// <para>
/// Produced after the Dealer resolves - on stand <b>or bust</b> - and modelled against the complete
/// army. <b>If it says a lane leaks two, the wave leaks two.</b>
/// </para>
/// <para>
/// It carries the <see cref="Timeline"/> because it describes a wave that will actually run.
/// Combat playback animates that recording; it never re-simulates. There is exactly one simulation
/// path, and this is its output.
/// </para>
/// <para>
/// Shares no base class and no interface with <see cref="VisibleThreat"/>. See that type for why.
/// </para>
/// </remarks>
public sealed record FinalForecast
{
    public required IReadOnlyList<LaneOutcome> Lanes { get; init; }

    /// <summary>The recorded wave. Play it back; do not re-run it.</summary>
    public required WaveTimeline Timeline { get; init; }

    /// <summary>Structural equality. See <see cref="LaneOutcome.Equals(LaneOutcome?)"/> for why.</summary>
    public bool Equals(FinalForecast? other) =>
        other is not null && Timeline == other.Timeline && Lanes.SequenceEqual(other.Lanes);

    public override int GetHashCode() => HashCode.Combine(Timeline, Lanes.Count);
}
