namespace Bastion.Core.Resolve;

/// <summary>
/// Behaviours the design specifies that the resolver does not yet model.
/// </summary>
/// <remarks>
/// <para>
/// This type holds no logic. It exists so the gap is a named thing in the codebase rather than an
/// absence somebody has to notice, and so that the moment one of these lands there is one obvious
/// place to look for what it interacts with.
/// </para>
/// <para>
/// <b>Every one of these is scoped out of Milestone 1 deliberately, not forgotten.</b> None of them
/// changes the shape of the resolver; each adds a rule inside a phase that already exists.
/// </para>
/// </remarks>
internal static class UnmodelledBehaviour
{
    /// <summary>
    /// <b>Jack - mobile.</b> Relocates to an adjacent socket once mid-wave, automatically, when
    /// nothing is in range (docs/design/04-cards-as-defenses.md).
    /// </summary>
    /// <remarks>
    /// A combat-time behaviour, not a board-production one: it needs a precise trigger - "when
    /// nothing is in range", measured when? - which the design leaves open, and it would live in
    /// phase 2 before targeting, making <c>TowerRuntime.Position</c> mutable. So it stays deferred
    /// past the Milestone 2 hand-and-formation work, which stops at the final board.
    /// </remarks>
    internal const string JackMobility = "Jack relocates once mid-wave when nothing is in range.";

    /// <summary>
    /// <b>Standard bearer (Dealer Q) - buffs nearby enemy units.</b>
    /// </summary>
    /// <remarks>
    /// Magnitude and radius are not specified anywhere. It would live in phase 2 as a modifier read
    /// before damage is applied, and it is the first thing that would make an enemy's effective
    /// stats depend on another enemy's position.
    /// </remarks>
    internal const string StandardBearerBuff = "Standard bearer buffs nearby enemies.";

    /// <summary>
    /// <b>Skirmisher (Dealer J) - changes lanes at the route junction.</b>
    /// </summary>
    /// <remarks>
    /// <b>This is the one that matters structurally.</b> Lanes currently resolve independently -
    /// see <c>BoardState.TowersFiringInto</c> - and a unit that crosses between them breaks that
    /// assumption, turning two independent simulations into one coupled simulation. It is not a
    /// rule added inside a phase; it is a change to how the lane loop is shaped. Land it
    /// deliberately.
    /// </remarks>
    internal const string SkirmisherLaneChange = "Skirmisher changes lanes at the junction - couples the lanes.";

    /// <summary>
    /// <b>Herald (Dealer A) - an elite at 11, a fragile scout at 1.</b>
    /// </summary>
    /// <remarks>
    /// The enemy roster carries one herald row, so the two states are not yet distinguishable. It
    /// needs a second tuned row, not resolver logic.
    /// </remarks>
    internal const string HeraldAceState = "Herald is an elite at 11 and a fragile scout at 1.";

    /// <summary>
    /// <b>Overload.</b> Immediate damage equal to the busting card's base power, struck at the lane
    /// with the highest current Visible Threat.
    /// </summary>
    /// <remarks>
    /// Milestone 3, with bust handling. The lane it targets is already available -
    /// <see cref="VisibleThreat.HighestThreatLane"/> - but how the damage applies (one enemy, all
    /// enemies, splash?) is not specified by the design.
    /// </remarks>
    internal const string OverloadApplication = "Overload strikes the highest-Visible-Threat lane on a bust.";
}
