using Bastion.Core.Board;
using Bastion.Core.Cards;
using Bastion.Core.Config;
using Bastion.Core.Dealer;
using Bastion.Core.Hand;
using Bastion.Core.March;
using Bastion.Core.Resolve;

namespace Bastion.Core.Wave;

/// <summary>
/// One wave, driven through its phases: deal, place, hit or stand, Dealer resolution, adjustment,
/// lock.
/// </summary>
/// <remarks>
/// <para>
/// The orchestration layer over Milestone 1 and Milestone 2. It owns none of the game logic - the
/// shoe, the hand, the March Clock, placement, run links, the Dealer's deployment, and the resolver
/// are all complete and tested - it drives them in the order <c>docs/design/01-core-loop.md</c> lays
/// out and enforces the phase boundaries the design is built on.
/// </para>
/// <para>
/// Immutable, like everything it drives: each transition returns a new session, so a caller can hold
/// an earlier state to compare against. The two forecasts stay separate types from separate methods
/// - <see cref="VisibleThreatNow"/> during the draw, <see cref="Forecast"/> after the Dealer resolves
/// - and the session never combines a hand consequence with a battlefield one; that is the player's
/// job (docs/design/09-information-and-ui.md).
/// </para>
/// </remarks>
public sealed record WaveSession
{
    private WaveSession()
    {
    }

    /// <summary>The tuning this wave runs against.</summary>
    public required TuningData Tuning { get; init; }

    /// <summary>The encounter: lane stakes, base wave, and the Vanguard's lane.</summary>
    public required EncounterTuning Encounter { get; init; }

    /// <summary>The shoe as it stands, after every card drawn so far.</summary>
    public required Shoe Shoe { get; init; }

    /// <summary>The player's placement producer.</summary>
    public required WaveDraft Draft { get; init; }

    /// <summary>Towers carried in from earlier waves, at ×1.00 Formation Strength.</summary>
    public required IReadOnlyList<TowerState> Persisted { get; init; }

    /// <summary>The Dealer's upcard, deployed as the Vanguard before the opening deal.</summary>
    public required Card Vanguard { get; init; }

    /// <summary>The Dealer's hidden hole card, dealt at the opening and revealed on resolution.</summary>
    public required Rank HoleCard { get; init; }

    /// <summary>Cards drawn but not yet placed, in draw order. The opening two, then one per hit.</summary>
    public required IReadOnlyList<Rank> PendingRanks { get; init; }

    /// <summary>The current phase.</summary>
    public required WavePhase Phase { get; init; }

    /// <summary>The Dealer's finished hand, upcard first, or null before it has resolved.</summary>
    public IReadOnlyList<Card>? DealerCards { get; init; }

    /// <summary>The Overload struck on a bust, or null on a stand.</summary>
    public OverloadStrike? Overload { get; init; }

    /// <summary>Whether the single adjustment move has been spent.</summary>
    public required bool MoveSpent { get; init; }

    /// <summary>The blackjack hand as it stands.</summary>
    public HandState Hand => Draft.Hand;

    /// <summary>The entry point the hand's length and total put the army at.</summary>
    public double Entry => Draft.Entry(Tuning);

    /// <summary>The formation-wide multiplier the hand currently earns.</summary>
    public double FormationMultiplier => Draft.Hand.FormationMultiplier(Tuning);

    /// <summary>
    /// Opens a wave: draws the Vanguard and the hidden card, deals the opening two, and stands
    /// ready for the first placement. The march has not begun - the opening two are free.
    /// </summary>
    public static WaveSession Begin(
        TuningData tuning,
        EncounterTuning encounter,
        Shoe shoe,
        IReadOnlyList<TowerState>? persisted = null)
    {
        ArgumentNullException.ThrowIfNull(tuning);
        ArgumentNullException.ThrowIfNull(encounter);
        ArgumentNullException.ThrowIfNull(shoe);

        // Draw order: the Vanguard is revealed first, the hole card is dealt and hidden next, then
        // the player's opening two. All come from the one shoe, so the pile count stays honest.
        (Rank up, Shoe s1) = shoe.Draw();
        (Rank hole, Shoe s2) = s1.Draw();
        (Rank first, Shoe s3) = s2.Draw();
        (Rank second, Shoe s4) = s3.Draw();

        return new WaveSession
        {
            Tuning = tuning,
            Encounter = encounter,
            Shoe = s4,
            Draft = WaveDraft.Empty,
            Persisted = persisted ?? [],

            // A lone visible Ace reads as 11, so the Vanguard's Ace is high until a hole card could
            // force it down. Only matters if the upcard is an Ace; harmless otherwise.
            Vanguard = new Card(up, up == Rank.Ace),
            HoleCard = hole,
            PendingRanks = [first, second],
            Phase = WavePhase.AwaitingPlacement,
            MoveSpent = false,
        };
    }

    /// <summary>
    /// Places the pending card, committing its family and socket. Family is now locked for the wave.
    /// </summary>
    public WaveSession Place(Family family, SocketRef socket)
    {
        Require(Phase == WavePhase.AwaitingPlacement, "There is no card waiting to be placed.");

        Rank rank = PendingRanks[0];
        IReadOnlyList<Rank> rest = [.. PendingRanks.Skip(1)];

        return this with
        {
            Draft = Draft.Place(rank, family, socket),
            PendingRanks = rest,
            Phase = rest.Count > 0 ? WavePhase.AwaitingPlacement : WavePhase.DrawDecision,
        };
    }

    /// <summary>
    /// Pays the march step and draws a card. A card that busts the hand locks the wave immediately;
    /// otherwise it waits to be placed.
    /// </summary>
    /// <remarks>
    /// The step is paid at the draw, before the card is revealed - here, the entry advance is a
    /// function of card count, so it is already fixed the moment the card joins the hand. A player
    /// commits to the cost without knowing what it bought.
    /// </remarks>
    public WaveSession Hit()
    {
        Require(Phase == WavePhase.DrawDecision, "A hit is only offered at the draw decision.");

        (Rank rank, Shoe afterDraw) = Shoe.Draw();

        return Draft.Hand.Hit(rank).IsBust
            ? Bust(rank, afterDraw)
            : this with { Shoe = afterDraw, PendingRanks = [rank], Phase = WavePhase.AwaitingPlacement };
    }

    /// <summary>
    /// Stands: the Dealer reveals the hole card, draws to 17, and deploys every card. Opens the
    /// one-move adjustment window against the now-complete army.
    /// </summary>
    public WaveSession Stand()
    {
        Require(Phase == WavePhase.DrawDecision, "A stand is only offered at the draw decision.");

        (IReadOnlyList<Card> dealer, Shoe afterDealer) = DealerHand.Resolve(Tuning, Vanguard.Rank, HoleCard, Shoe);

        return this with
        {
            Shoe = afterDealer,
            DealerCards = dealer,
            Phase = WavePhase.AdjustmentWindow,
        };
    }

    /// <summary>
    /// Relocates a tower one socket, spending the wave's single adjustment move. Family is unchanged.
    /// </summary>
    public WaveSession RelocateTower(SocketRef from, SocketRef to)
    {
        Require(Phase == WavePhase.AdjustmentWindow, "Relocation is only offered in the adjustment window.");
        Require(!MoveSpent, "The adjustment window is one move for the whole board, and it is already spent.");
        Require(AreAdjacent(from, to), "A relocation moves a tower one socket.");

        return this with { Draft = Draft.WithTowerMoved(from, to), MoveSpent = true };
    }

    /// <summary>
    /// Swaps two adjacent towers, spending the wave's single adjustment move.
    /// </summary>
    public WaveSession SwapTowers(SocketRef a, SocketRef b)
    {
        Require(Phase == WavePhase.AdjustmentWindow, "A swap is only offered in the adjustment window.");
        Require(Tuning.Rules.AdjustmentAllowsAdjacentSwap, "This tuning does not allow the adjacent swap.");
        Require(!MoveSpent, "The adjustment window is one move for the whole board, and it is already spent.");
        Require(AreAdjacent(a, b), "A swap exchanges two adjacent towers.");

        return this with { Draft = Draft.WithTowersSwapped(a, b), MoveSpent = true };
    }

    /// <summary>
    /// Sets a tower's standing order. Free - it does not consume the single move.
    /// </summary>
    public WaveSession SetStandingOrder(SocketRef socket, StandingOrder order)
    {
        Require(Phase == WavePhase.AdjustmentWindow, "Standing orders are set in the adjustment window.");
        Require(!Tuning.Rules.StandingOrderChangesConsumeMove, "This tuning charges a move for a standing-order change, which the prototype does not.");

        return this with { Draft = Draft.WithOrder(socket, order) };
    }

    /// <summary>
    /// Locks placement. The Final Forecast is now the combat contract.
    /// </summary>
    public WaveSession Lock()
    {
        Require(Phase == WavePhase.AdjustmentWindow, "Only an open adjustment window can be locked; a bust locks itself.");

        return this with { Phase = WavePhase.Locked };
    }

    /// <summary>
    /// The Visible Threat: what the revealed force would do, read at the draw decision.
    /// </summary>
    /// <remarks>
    /// Exact against the base wave plus the Vanguard only - <b>not</b> a prediction of the wave. Read
    /// at the decision point, where every drawn card is placed and the entry is exact.
    /// </remarks>
    public VisibleThreat VisibleThreatNow()
    {
        Require(Phase == WavePhase.DrawDecision, "The Visible Threat is a draw-decision reading.");

        double entry = Entry;
        BoardState board = CombinedBoard(Draft, entry);
        RevealedArmy army = ArmyBuilder.Revealed(Tuning, Encounter, Vanguard, entry);

        return Resolver.ResolveRevealed(Tuning, Encounter, board, army);
    }

    /// <summary>
    /// The Final Forecast against the complete army: the combat contract. Reflects the current board,
    /// so it updates as the adjustment window changes it.
    /// </summary>
    public FinalForecast Forecast()
    {
        Require(DealerCards is not null, "The Dealer has not resolved yet; there is no Final Forecast.");

        double entry = Entry;
        BoardState board = CombinedBoard(Draft, entry);
        CompleteArmy army = ArmyBuilder.Complete(Tuning, Encounter, DealerCards!, entry);

        return Resolver.ResolveComplete(Tuning, Encounter, board, army, Overload);
    }

    /// <summary>
    /// The board the resolver runs against right now: persisted towers plus the current draft's,
    /// entering where the March Clock says. The same board both forecasts are read from.
    /// </summary>
    public BoardState Board() => CombinedBoard(Draft, Entry);

    /// <summary>What the next card would cost in entry advance, before it is revealed.</summary>
    public double NextStepCost() => MarchClock.NextStepCost(Tuning, Draft.Hand.CardCount);

    /// <summary>
    /// Ends the wave: reverts the surviving current-hand towers to ×1.00 for the next wave and
    /// reshuffles the shoe if it has run low.
    /// </summary>
    /// <remarks>
    /// Persisted towers carry forward at ×1.00 (docs/design/05-battlefield.md § Persistence), which
    /// is also why a bust is scoped to the current hand: earlier waves' towers are already at ×1.00,
    /// so the bust multiplier cannot drag them down.
    /// </remarks>
    public (IReadOnlyList<TowerState> Persisted, Shoe Shoe) Settle()
    {
        Require(Phase is WavePhase.Locked or WavePhase.BustLocked, "A wave settles only after it is locked.");

        double persistedMultiplier = Tuning.FormationStrength.Persisted;

        List<TowerState> carried =
        [
            .. Persisted,
            .. Draft.BuildBoard(Tuning).Towers
                .Where(t => !Persisted.Any(p => p.Socket == t.Socket))
                .Select(t => t with { FormationMultiplier = persistedMultiplier, RunBonus = 0.0 }),
        ];

        return (carried, Shoe.ReshuffleIfLow(Tuning));
    }

    private WaveSession Bust(Rank bustRank, Shoe afterDraw)
    {
        // The busting card advances the march but is destroyed, never placed. Place handles that:
        // its bust branch adds the card to the hand and returns the board unchanged, so the family
        // and socket here are inert.
        WaveDraft busted = Draft.Place(bustRank, Family.Club, SocketRef.Junction);

        // The Dealer resolves in full regardless - a bust changes the formation, not the army.
        (IReadOnlyList<Card> dealer, Shoe afterDealer) = DealerHand.Resolve(Tuning, Vanguard.Rank, HoleCard, afterDraw);

        // Overload strikes the highest current Visible Threat lane, ties toward the Bastion stake,
        // for the busting card's base power. "Current" is the reading shown before the hit - the
        // "Bust -> Overload: Lane X" the hand panel carries - so it is read against the pre-bust
        // board, not the busted one: the player is told where it lands before choosing. It is
        // deterministic and unsteerable - the busting card is destroyed and never placed, so it
        // inherits no lane (docs/design/07-bust-and-overload.md).
        double preEntry = Draft.Entry(Tuning);
        BoardState preBoard = CombinedBoard(Draft, preEntry);
        VisibleThreat threat = Resolver.ResolveRevealed(
            Tuning, Encounter, preBoard, ArmyBuilder.Revealed(Tuning, Encounter, Vanguard, preEntry));

        int lane = threat.HighestThreatLane(Tuning.Rules.OverloadTieBreakStake).LaneIndex;
        double burst = Tuning.CardPower.ForValue(new Card(bustRank).Value);

        return this with
        {
            Draft = busted,
            Shoe = afterDealer,
            DealerCards = dealer,
            Overload = new OverloadStrike { LaneIndex = lane, Damage = burst },
            Phase = WavePhase.BustLocked,
            MoveSpent = true,
        };
    }

    /// <summary>
    /// The board the resolver runs against: persisted towers plus the current draft's, entering
    /// where the March Clock says.
    /// </summary>
    private BoardState CombinedBoard(WaveDraft draft, double entry)
    {
        BoardState current = draft.BuildBoard(Tuning);

        return Persisted.Count == 0
            ? current
            : BoardState.Create(Tuning, [.. Persisted, .. current.Towers], entry);
    }

    /// <summary>
    /// Whether two sockets are one apart: neighbouring depths in a lane, or the junction and the
    /// middle socket it shares a path position with.
    /// </summary>
    private bool AreAdjacent(SocketRef a, SocketRef b)
    {
        if (a == b || (a.IsJunction && b.IsJunction))
        {
            return false;
        }

        if (a.IsJunction || b.IsJunction)
        {
            SocketRef lane = a.IsJunction ? b : a;
            int middle = Tuning.Geometry.SocketPositions.Count / 2;
            return lane.SocketIndex == middle;
        }

        return a.LaneIndex == b.LaneIndex && Math.Abs(a.SocketIndex - b.SocketIndex) == 1;
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
