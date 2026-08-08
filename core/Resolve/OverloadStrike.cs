namespace Bastion.Core.Resolve;

/// <summary>
/// A bust's Overload: an immediate burst struck at one lane before combat begins.
/// </summary>
/// <remarks>
/// <para>
/// Passed into <see cref="Resolver.ResolveComplete"/> only on a bust; a normal wave passes none, so
/// the Overload is not a mode the resolver is ever in - it is an optional opening event. The lane is
/// the highest current Visible Threat (ties toward the Bastion stake) and the damage is the busting
/// card's base power, both decided by the caller: the resolver applies what it is handed and steers
/// nothing (docs/design/07-bust-and-overload.md).
/// </para>
/// <para>
/// <b>How the burst distributes is NOT SPECIFIED BY THE DESIGN</b> - "one enemy, all enemies,
/// splash?" is left open. See <see cref="Resolver"/> and docs/reference/tuning-constants.md.
/// </para>
/// </remarks>
public sealed record OverloadStrike
{
    public required int LaneIndex { get; init; }

    /// <summary>The busting card's base power. Does not scale with the amount over 21.</summary>
    public required double Damage { get; init; }
}
