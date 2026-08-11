using Bastion.Core.Board;
using Bastion.Core.Cards;
using Bastion.Core.Config;
using Bastion.Core.March;

namespace Bastion.Core.Hand;

/// <summary>
/// One card committed to the board: its rank, the family it was locked into, and its socket.
/// </summary>
/// <remarks>
/// Stores the <see cref="Rank"/>, not a <see cref="Card"/>: an Ace's 1-or-11 state is derived from
/// the hand at board-build time, so the hand stays the single source of truth for it.
/// </remarks>
/// <remarks>
/// Carries the tower's <see cref="StandingOrder"/> so the adjustment window can set one without a
/// separate board type: orders are set here, and <see cref="WaveDraft.BuildBoard"/> folds them into
/// the towers it produces. Null means the default nearest-target rule.
/// </remarks>
public readonly record struct Placement(Rank Rank, Family Family, SocketRef Socket, StandingOrder? Order = null);

/// <summary>
/// A hand being drawn and placed - the producer that turns cards into the board the resolver runs.
/// </summary>
/// <remarks>
/// <para>
/// This is the Milestone 2 / Milestone 3 seam. It builds the final <see cref="BoardState"/> and
/// nothing past it: the Dealer's draw, the adjustment window, bust's Overload strike, and the phase
/// machine are Milestone 3. Immutable - every action returns a new draft.
/// </para>
/// <para>
/// The hand and the board are tracked separately because forced replacement decouples them: a drawn
/// card always counts toward the blackjack total, but at capacity placing it destroys an existing
/// tower, so beyond seven cards the board holds fewer towers than the hand holds cards
/// (docs/design/05-battlefield.md § Forced replacement).
/// </para>
/// </remarks>
public sealed class WaveDraft
{
    private readonly IReadOnlyList<Placement> _placements;

    private WaveDraft(HandState hand, IReadOnlyList<Placement> placements)
    {
        Hand = hand;
        _placements = placements;
    }

    /// <summary>Nothing drawn yet.</summary>
    public static WaveDraft Empty { get; } = new(HandState.Empty, []);

    /// <summary>The blackjack hand: every card drawn, whether or not it still has a tower.</summary>
    public HandState Hand { get; }

    /// <summary>The towers currently on the board, one per occupied socket.</summary>
    public IReadOnlyList<Placement> Placements => _placements;

    /// <summary>
    /// Draws a card and places it, committing its family permanently.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Placing onto an occupied socket <b>replaces</b> its tower: the removed tower's power, links,
    /// and multiplier go with it. At capacity every socket is full, so a placement is necessarily a
    /// replacement - which is the forced-replacement rule falling out of geometry rather than a
    /// special case (docs/design/05-battlefield.md).
    /// </para>
    /// <para>
    /// A card that busts the hand is <b>destroyed, not placed</b>: it counts toward the (busting)
    /// total but leaves no tower, and <paramref name="family"/> and <paramref name="socket"/> are
    /// moot. Overload - the bust's consolation strike - is Milestone 3.
    /// </para>
    /// </remarks>
    public WaveDraft Place(Rank rank, Family family, SocketRef socket)
    {
        HandState hand = Hand.Hit(rank);

        if (hand.IsBust)
        {
            return new WaveDraft(hand, _placements);   // busting card destroyed, board unchanged
        }

        List<Placement> placed = [.. _placements.Where(p => p.Socket != socket), new Placement(rank, family, socket)];
        return new WaveDraft(hand, placed);
    }

    /// <summary>
    /// Moves this draft's tower at <paramref name="from"/> to <paramref name="to"/>, keeping its
    /// rank, family, and order. The adjustment window's relocate half.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Family never changes here - only position - so family locking is preserved by construction.
    /// The move re-derives the whole board through <see cref="BuildBoard"/>, so a relocation that
    /// forms or breaks a run is reflected in the run bonuses, not patched onto a fixed board.
    /// </para>
    /// <para>
    /// <b>Operates on this draft's half of the board only, and is deliberately tolerant:</b> if the
    /// draft holds no tower at <paramref name="from"/> the relocation belongs to a carried-over
    /// tower and this returns unchanged. Adjacency, occupancy, and the single-move rule are checked
    /// against the <i>whole</i> board - persisted towers included - in <c>WaveSession</c>, which is
    /// the only caller and the only place that can see both halves.
    /// </para>
    /// </remarks>
    public WaveDraft WithSocketReassigned(SocketRef from, SocketRef to)
    {
        List<Placement> moved = [.. _placements.Select(p => p.Socket == from ? p with { Socket = to } : p)];
        return new WaveDraft(Hand, moved);
    }

    /// <summary>
    /// Exchanges whichever of <paramref name="a"/> and <paramref name="b"/> this draft holds. The
    /// adjustment window's swap half.
    /// </summary>
    /// <remarks>
    /// Tolerant for the same reason as <see cref="WithSocketReassigned"/>, and here it is load-bearing
    /// rather than defensive: a swap may pair a current-hand tower with a carried-over one, and the
    /// two live in different lists. Each list remaps only its own end, which composes to the whole
    /// exchange without either half needing to know about the other.
    /// </remarks>
    public WaveDraft WithSocketsExchanged(SocketRef a, SocketRef b)
    {
        List<Placement> swapped =
        [
            .. _placements.Select(p =>
                p.Socket == a ? p with { Socket = b }
                : p.Socket == b ? p with { Socket = a }
                : p),
        ];

        return new WaveDraft(Hand, swapped);
    }

    /// <summary>
    /// Sets the standing order on the tower at <paramref name="socket"/>. Free in the adjustment
    /// window - it does not consume the single move.
    /// </summary>
    public WaveDraft WithOrder(SocketRef socket, StandingOrder order)
    {
        ArgumentNullException.ThrowIfNull(order);

        if (_placements.All(p => p.Socket != socket))
        {
            throw new InvalidOperationException($"There is no tower at {socket} to give an order.");
        }

        List<Placement> ordered = [.. _placements.Select(p => p.Socket == socket ? p with { Order = order } : p)];
        return new WaveDraft(Hand, ordered);
    }

    /// <summary>The entry point this hand's length and total put the army at.</summary>
    public double Entry(TuningData tuning)
    {
        ArgumentNullException.ThrowIfNull(tuning);

        return MarchClock.EntryAfter(tuning, Hand.CardCount, Hand.IsExactly21);
    }

    /// <summary>
    /// Assembles the board: every placed tower at the hand's multiplier and its run bonus, plus the
    /// Ace Bastion on a natural, entering where the March Clock says.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The one place a <see cref="BoardState"/> is produced, so the resolver has exactly one code
    /// path feeding it. The same call serves the provisional board during the draw and the final
    /// board at stand - the only difference is that the hand has more cards.
    /// </para>
    /// <para>
    /// <paramref name="reservedSockets"/> are sockets held by towers outside this draft - carried-over
    /// towers from earlier waves. The draft cannot place into them (forced replacement evicts them at
    /// placement instead, in <c>WaveSession</c>), but the Ace Bastion picks its own socket, so it has
    /// to be told which ones are already spoken for or a natural in a later wave would seat it on top
    /// of a persisted tower.
    /// </para>
    /// </remarks>
    public BoardState BuildBoard(TuningData tuning, IReadOnlyCollection<SocketRef>? reservedSockets = null)
    {
        ArgumentNullException.ThrowIfNull(tuning);

        double multiplier = Hand.FormationMultiplier(tuning);
        IReadOnlyList<(Card Card, Placement Placement)> resolved = ResolveAceStates();

        IReadOnlyDictionary<SocketRef, double> runBonus =
            RunLinks.BonusBySocket(tuning, [.. resolved.Select(r => (r.Card, r.Placement.Socket))]);

        List<TowerState> towers =
        [
            .. resolved.Select(r => TowerState.Place(
                tuning,
                r.Card,
                r.Placement.Family,
                r.Placement.Socket,
                multiplier,
                runBonus.GetValueOrDefault(r.Placement.Socket, 0.0),
                r.Placement.Order)),
        ];

        if (Hand.IsNaturalBlackjack && AceBastion(tuning, multiplier, towers, reservedSockets ?? []) is { } anchor)
        {
            towers.Add(anchor);
        }

        return BoardState.Create(tuning, towers, Entry(tuning));
    }

    /// <summary>
    /// Stamps each Ace placement high or low from the hand's current resolution.
    /// </summary>
    /// <remarks>
    /// Which Aces are high is fixed by blackjack, not chosen for runs (unlike the Queen). When a
    /// split hand holds some Aces high and some low, the assignment among placed Ace towers is not
    /// observable in blackjack, so it is made deterministically - high in firing order - rather than
    /// left to iteration chance.
    /// </remarks>
    private IReadOnlyList<(Card Card, Placement Placement)> ResolveAceStates()
    {
        HashSet<SocketRef> highSockets =
        [
            .. _placements
                .Where(p => p.Rank == Rank.Ace)
                .OrderBy(p => p.Socket.FiringOrder)
                .Take(Hand.AceHighCount)
                .Select(p => p.Socket),
        ];

        return
        [
            .. _placements.Select(p =>
                (new Card(p.Rank, p.Rank == Rank.Ace && highSockets.Contains(p.Socket)), p)),
        ];
    }

    /// <summary>
    /// The free King-class anchor a natural earns: does not count as a hand card, shares the hand's
    /// multiplier (docs/design/02-blackjack-and-formation.md § Natural blackjack).
    /// </summary>
    /// <remarks>
    /// <para>
    /// NOT SPECIFIED BY THE DESIGN: which socket it takes and which family it wears. First pass -
    /// the junction if free, else the deepest empty lane socket, because a King-class anchor has
    /// face-card range and the junction exemption, so it covers both lanes at full power; and Club,
    /// as a neutral placeholder. Flagged in docs/reference/tuning-constants.md § Invented, to revisit
    /// in Milestone 3 with the rest of bust and stakes.
    /// </para>
    /// <para>
    /// ALSO NOT SPECIFIED: what a natural earns when every socket is already taken, which persistence
    /// makes reachable from the third wave on - two fresh towers plus five carried ones fill the
    /// board. Returns null and the natural simply goes unanchored, because the anchor is a bonus and
    /// the alternative is destroying a tower the player never chose to replace. Revisit with the rest
    /// of the anchor's rules.
    /// </para>
    /// </remarks>
    private TowerState? AceBastion(
        TuningData tuning,
        double handMultiplier,
        IEnumerable<TowerState> placed,
        IReadOnlyCollection<SocketRef> reservedSockets)
    {
        HashSet<SocketRef> occupied = [.. placed.Select(t => t.Socket), .. reservedSockets];

        if (FreeAnchorSocket(tuning, occupied) is not { } socket)
        {
            return null;
        }

        return new TowerState
        {
            Card = new Card(Rank.King),   // King-class: representative rank, power comes from tuning below
            Family = Family.Club,
            Socket = socket,
            BasePower = tuning.AceBastion.Power,
            Range = TowerState.RangeFor(tuning, socket, faceCard: true),   // King-class
            FormationMultiplier = tuning.AceBastion.SharesHandMultiplier ? handMultiplier : 1.0,
            RunBonus = 0.0,               // not a hand card; it anchors rather than links
            IgnoresHalfArmor = true,      // King-class
            ExemptFromJunctionPenalty = true,

            // King-class for range and the junction exemption, but NOT an anchor. It is not a placed
            // card: it picks a free socket afresh every time the board is derived, so protecting its
            // socket from replacement would block a placement the player is entitled to make.
            IsAnchor = false,
            Order = StandingOrder.None,
        };
    }

    /// <summary>Junction first, then the deepest empty lane socket, or null if the board is full.</summary>
    private static SocketRef? FreeAnchorSocket(TuningData tuning, HashSet<SocketRef> occupied)
    {
        if (!occupied.Contains(SocketRef.Junction))
        {
            return SocketRef.Junction;
        }

        for (int socketIndex = tuning.Geometry.SocketPositions.Count - 1; socketIndex >= 0; socketIndex--)
        {
            for (int lane = 0; lane < tuning.Geometry.Lanes; lane++)
            {
                SocketRef candidate = SocketRef.InLane(lane, socketIndex);
                if (!occupied.Contains(candidate))
                {
                    return candidate;
                }
            }
        }

        return null;
    }
}
