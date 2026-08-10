namespace Bastion.Core.Resolve;

/// <summary>What one lane would take if nothing defended it.</summary>
public sealed record LaneBaseline
{
    public required int LaneIndex { get; init; }

    /// <summary>bastion or vault.</summary>
    public required string Stake { get; init; }

    /// <summary>Damage the revealed force does to this lane with no towers in it.</summary>
    public required int EmptyLaneDamage { get; init; }
}

/// <summary>
/// What the lanes are worth before a card is dealt: the stake and the undefended cost.
/// </summary>
/// <remarks>
/// <para>
/// docs/design/09-information-and-ui.md § Shown asks for "lane stakes, base wave, and <b>empty-lane
/// damage</b> before the opening deal". Empty-lane damage is what makes a stake mean something: a
/// bastion lane that would take 3 and a vault lane that would take 16 triage in opposite directions,
/// and the stake word alone does not say that.
/// </para>
/// <para>
/// <b>A third type, deliberately - not a Visible Threat and not a Final Forecast.</b> It answers a
/// question neither of them asks: what happens if the player does nothing. It carries no predicted
/// damage, no leaked units, and no timeline, so it cannot be rendered where either forecast is
/// expected, which is the same type-system guarantee the forecast split relies on
/// (docs/design/05-battlefield.md § Implementation).
/// </para>
/// </remarks>
public sealed record OpeningStakes
{
    public required IReadOnlyList<LaneBaseline> Lanes { get; init; }

    public bool Equals(OpeningStakes? other) =>
        other is not null && Lanes.SequenceEqual(other.Lanes);

    public override int GetHashCode() => Lanes.Count.GetHashCode();
}
