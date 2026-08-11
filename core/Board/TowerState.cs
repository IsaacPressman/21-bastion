using Bastion.Core.Cards;
using Bastion.Core.Config;

namespace Bastion.Core.Board;

/// <summary>
/// A placed tower, as the resolver sees it.
/// </summary>
/// <remarks>
/// <para>
/// Immutable and fully resolved: the resolver reads these fields and never asks how they were
/// arrived at. Placement, family locking, forced replacement at capacity, and run detection are
/// Milestone 2 and <i>write</i> these fields.
/// </para>
/// <para>
/// <see cref="FormationMultiplier"/> and <see cref="RunBonus"/> are kept apart rather than folded
/// into one number on purpose. Milestone 2 can attach without touching the resolver, and per-tower
/// activity reporting can still explain <i>why</i> a tower hit as hard as it did - which is the
/// point of reporting it at all.
/// </para>
/// </remarks>
public sealed record TowerState
{
    public required Card Card { get; init; }
    public required Family Family { get; init; }
    public required SocketRef Socket { get; init; }

    /// <summary>Base power from the card power curve.</summary>
    public required double BasePower { get; init; }

    /// <summary>Path units either side of the socket this tower can reach.</summary>
    public required double Range { get; init; }

    /// <summary>The hand's Formation Strength. Persisted towers revert to 1.00 at the wave boundary.</summary>
    public required double FormationMultiplier { get; init; }

    /// <summary>Fractional run-link bonus: 0.15 for a 2-run, 0.25 for a 3-run, 0 for an unlinked tower.</summary>
    public required double RunBonus { get; init; }

    /// <summary>Spade traps and Kings ignore half of flat armor (docs/design/06-dealer-and-enemies.md).</summary>
    public required bool IgnoresHalfArmor { get; init; }

    /// <summary>Face cards occupy the junction without the usual contribution penalty.</summary>
    public required bool ExemptFromJunctionPenalty { get; init; }

    /// <summary>
    /// The King is the anchor: <b>forced replacement cannot evict it</b>
    /// (docs/design/04-cards-as-defenses.md § Face cards).
    /// </summary>
    /// <remarks>
    /// <para>
    /// A property rather than a read of <c>Card.IsKing</c>, because not every King-carded tower is an
    /// anchor: the Ace Bastion is built King-class for its range and junction exemption but re-seats
    /// itself every time the board is derived, so protecting its socket would block a placement for
    /// no reason.
    /// </para>
    /// <para>
    /// It blocks <i>eviction</i>, not movement. The adjustment window may still relocate or swap an
    /// anchor: the player chose that, the tower survives it, and reading "cannot be displaced" as
    /// "cannot be moved" would turn a face card's advantage into a restriction, which is the opposite
    /// of what the face-card table is describing.
    /// </para>
    /// </remarks>
    public required bool IsAnchor { get; init; }

    public required StandingOrder Order { get; init; }

    /// <summary>
    /// Damage of one shot, before armor and before any junction penalty.
    /// </summary>
    /// <remarks>
    /// NOT SPECIFIED BY THE DESIGN that power means damage per shot - see data/tuning.json's
    /// resolver comment block. What the design does specify is that this must never be multiplied
    /// by an engagement fraction to estimate output; sockets are not interchangeable, and three
    /// units of coverage taken from a 5.0-power King is not three taken from a 1.6-power two.
    /// Balance comes from resolver output (docs/design/03-march-clock.md).
    /// </remarks>
    public double ShotDamage => BasePower * FormationMultiplier * (1.0 + RunBonus);

    /// <summary>
    /// Builds a tower from a card, applying every rule that turns a card into a battlefield object.
    /// </summary>
    /// <remarks>
    /// These rules live here rather than at a call site so they are impossible to violate through a
    /// code path, which is what docs/ARCHITECTURE.md asks for family locking specifically.
    /// </remarks>
    public static TowerState Place(
        TuningData tuning,
        Card card,
        Family family,
        SocketRef socket,
        double formationMultiplier,
        double runBonus = 0.0,
        StandingOrder? order = null)
    {
        ArgumentNullException.ThrowIfNull(tuning);

        return new TowerState
        {
            Card = card,
            Family = family,
            Socket = socket,
            BasePower = tuning.CardPower.ForValue(card.Value),
            Range = RangeFor(tuning, socket, card.HasFaceCardRange),
            FormationMultiplier = formationMultiplier,
            RunBonus = runBonus,
            IgnoresHalfArmor = family == Family.Spade || card.IsKing,
            ExemptFromJunctionPenalty = card.HasFaceCardRange && tuning.Towers.JunctionFaceCardExempt,
            IsAnchor = card.IsKing,
            Order = order ?? StandingOrder.None,
        };
    }

    /// <summary>
    /// Firing range for a tower at <paramref name="socket"/>, the one derivation both construction
    /// sites share.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Range varies by socket - the geometry remedy for deep-placement dominance
    /// (docs/ROADMAP.md Open Decision 2). A junction tower takes the range of the socket whose
    /// ground it shares, which is the same middle socket the loader pins
    /// <c>towers.junctionPathPosition</c> to; deriving it rather than tuning it separately keeps the
    /// junction's position and its reach from drifting apart.
    /// </para>
    /// <para>
    /// The face-card allowance is added to the socket's range rather than replacing it
    /// (docs/design/04-cards-as-defenses.md: value-10 cards see further).
    /// </para>
    /// </remarks>
    public static double RangeFor(TuningData tuning, SocketRef socket, bool faceCard)
    {
        ArgumentNullException.ThrowIfNull(tuning);

        int index = socket.IsJunction ? tuning.Geometry.MiddleSocketIndex : socket.SocketIndex;

        return tuning.Geometry.RangeBySocket[index] + (faceCard ? tuning.Geometry.FaceCardRangeBonus : 0.0);
    }

    /// <summary>
    /// Where this tower sits on the given lane's path.
    /// </summary>
    /// <remarks>
    /// A junction tower answers for either lane, at the tuned junction position.
    /// </remarks>
    public double PositionOn(TuningData tuning)
    {
        ArgumentNullException.ThrowIfNull(tuning);

        return Socket.IsJunction
            ? tuning.Towers.JunctionPathPosition
            : tuning.Geometry.SocketPositions[Socket.SocketIndex];
    }

    /// <summary>
    /// Damage this tower lands per shot in a single lane, after the junction penalty.
    /// </summary>
    public double ShotDamageInLane(TuningData tuning)
    {
        ArgumentNullException.ThrowIfNull(tuning);

        if (!Socket.IsJunction || ExemptFromJunctionPenalty)
        {
            return ShotDamage;
        }

        return ShotDamage * tuning.Towers.JunctionContributionFraction;
    }
}
