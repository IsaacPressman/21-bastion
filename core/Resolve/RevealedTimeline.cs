namespace Bastion.Core.Resolve;

/// <summary>
/// The schedule the <b>revealed force</b> would run to. <b>Not a wave that will run.</b>
/// </summary>
/// <remarks>
/// <para>
/// Carried by <see cref="VisibleThreat"/>, and produced by the same resolver run that produces its
/// lane outcomes - the events were always computed and were simply discarded. Nothing here is a
/// second simulation.
/// </para>
/// <para>
/// <b>This is a different type from <see cref="WaveTimeline"/>, with no base class, no interface,
/// and no conversion between them</b>, and that separation is the whole point. The encounter
/// timeline has to be readable during the draw - "if you draw again, this cannon loses two shots"
/// is the entire March decision, and it is unshowable once the Dealer has resolved
/// (docs/design/14-encounter-timeline.md). But combat playback must remain unable to animate a
/// Visible Threat, because a Visible Threat is exact about a <i>smaller question</i> and is not a
/// promise about combat.
/// </para>
/// <para>
/// Both constraints are met structurally rather than by convention:
/// <see cref="TimelinePlayer(WaveTimeline, Config.TuningData)"/> takes a
/// <see cref="WaveTimeline"/> and will not accept this, so the only thing that can be played back
/// is the combat contract. What <i>both</i> may produce is a <see cref="TimelineStrip"/> - a
/// drawing model over raw events, which is a presentation concern rather than a forecast.
/// </para>
/// </remarks>
public sealed record RevealedTimeline
{
    /// <summary>The scheduled run of the revealed force, in the resolver's pinned emission order.</summary>
    public required IReadOnlyList<TimelineEvent> Events { get; init; }

    /// <summary>How long the revealed force takes to resolve.</summary>
    public required double DurationSeconds { get; init; }

    /// <summary>
    /// Structural equality, including the event list.
    /// </summary>
    /// <remarks>
    /// See <see cref="LaneOutcome.Equals(LaneOutcome?)"/> for why the synthesised version is not
    /// good enough: a determinism check that compares list references can never fail, so it would
    /// pass a broken resolver by never being able to detect a working one.
    /// </remarks>
    public bool Equals(RevealedTimeline? other) =>
        other is not null
        && DurationSeconds.Equals(other.DurationSeconds)
        && Events.SequenceEqual(other.Events);

    public override int GetHashCode() => HashCode.Combine(DurationSeconds, Events.Count);
}
