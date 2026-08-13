namespace Bastion.Core.Resolve;

/// <summary>
/// What the currently revealed force would do. <b>Not a prediction of the wave.</b>
/// </summary>
/// <remarks>
/// <para>
/// Shown during the draw, modelled against the base wave plus the Dealer's Vanguard. It is exact
/// about what is standing on the field right now, and the Dealer's hidden card and subsequent draws
/// have not resolved - so it is exact about a <i>smaller question</i> than the one the player
/// wants answered.
/// </para>
/// <para>
/// <b>This shares no base class and no interface with <see cref="FinalForecast"/>, and there is no
/// conversion between them.</b> That is a type-system requirement rather than a convention because
/// the failure is silent and expensive: a number that keeps its name while changing its meaning
/// mid-hand is the cheapest possible way to lose trust in the forecast, and trust in the forecast
/// is a foundational claim of this design. Revision 7 described one contract and then demonstrated
/// it changing mid-example.
/// </para>
/// <para>
/// Note what is present and what it is <i>not</i>. This carries a <see cref="RevealedTimeline"/>,
/// because the encounter timeline has to be readable during the draw - "if you draw again, this
/// cannon loses two shots" is the entire March decision and cannot be shown after the Dealer has
/// resolved (docs/design/14-encounter-timeline.md, Milestone 6). It is emphatically <b>not</b> a
/// <see cref="WaveTimeline"/>: different type, no base class, no interface, no conversion. Combat
/// playback takes a <see cref="WaveTimeline"/> and therefore cannot animate a Visible Threat, so a
/// Visible Threat still cannot be rendered in a slot expecting a Final Forecast even by accident.
/// The asymmetry moved from "one has a timeline" to "one has a timeline playback will accept", and
/// both are enforced by the type system rather than by convention.
/// </para>
/// <para>
/// The interface must label this plainly as revealed-force-only. Players who read it as a promise
/// will feel the game break it when reinforcements land.
/// </para>
/// </remarks>
public sealed record VisibleThreat
{
    public required IReadOnlyList<LaneOutcome> Lanes { get; init; }

    /// <summary>
    /// What the revealed force is scheduled to do. Drawable, never playable.
    /// </summary>
    /// <remarks>
    /// Read by the encounter timeline during the draw. See <see cref="RevealedTimeline"/> for why it
    /// is a separate type from the Final Forecast's <see cref="WaveTimeline"/>.
    /// </remarks>
    public required RevealedTimeline Schedule { get; init; }

    /// <summary>Structural equality. See <see cref="LaneOutcome.Equals(LaneOutcome?)"/> for why.</summary>
    public bool Equals(VisibleThreat? other) =>
        other is not null && Schedule == other.Schedule && Lanes.SequenceEqual(other.Lanes);

    public override int GetHashCode() => HashCode.Combine(Schedule, Lanes.Count);

    /// <summary>The lane carrying the highest predicted damage; ties toward the Bastion stake.</summary>
    /// <remarks>
    /// This is Overload's target on a bust. The busting card is destroyed and never placed, so it
    /// inherits no lane - targeting the highest current Visible Threat is deterministic, needs no
    /// declaration during the draw, and is unsteerable. Overload's application itself is Milestone
    /// 3; this is the lane it will ask for. See docs/design/07-bust-and-overload.md.
    /// </remarks>
    public LaneOutcome HighestThreatLane(string tieBreakStake) =>
        Lanes
            .OrderByDescending(l => l.PredictedDamage)
            .ThenByDescending(l => string.Equals(l.Stake, tieBreakStake, StringComparison.Ordinal))
            .ThenBy(l => l.LaneIndex)
            .First();
}
