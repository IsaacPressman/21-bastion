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

    /// <summary>Which wave of the encounter this is, from one.</summary>
    public required int WaveNumber { get; init; }

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
    /// <param name="waveNumber">
    /// Which wave of the encounter this is, from one. The encounter resets the board when it runs
    /// out, so the count is what bounds persistence.
    /// </param>
    public static WaveSession Begin(
        TuningData tuning,
        EncounterTuning encounter,
        Shoe shoe,
        IReadOnlyList<TowerState>? persisted = null,
        int waveNumber = 1)
    {
        ArgumentNullException.ThrowIfNull(tuning);
        ArgumentNullException.ThrowIfNull(encounter);
        ArgumentNullException.ThrowIfNull(shoe);

        if (waveNumber < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(waveNumber), waveNumber, "Waves are numbered from one.");
        }

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
            WaveNumber = waveNumber,
        };
    }

    /// <summary>
    /// Places the pending card, committing its family and socket. Family is now locked for the wave.
    /// </summary>
    /// <remarks>
    /// Forced replacement reaches carried-over towers too. Persistence exists precisely so that
    /// "sockets fill during the second wave, and every card after that forces a replacement"
    /// (docs/design/05-battlefield.md § Persistence), so a placement onto a persisted tower's socket
    /// evicts it exactly as it would evict one of this hand's - the removed tower's power, links, and
    /// multiplier go with it. <see cref="WaveDraft"/> can only see its own half of the board, so the
    /// eviction happens here, where both halves are visible.
    /// </remarks>
    public WaveSession Place(Family family, SocketRef socket)
    {
        Require(Phase == WavePhase.AwaitingPlacement, "There is no card waiting to be placed.");

        Rank rank = PendingRanks[0];
        IReadOnlyList<Rank> rest = [.. PendingRanks.Skip(1)];
        bool busts = Draft.Hand.Hit(rank).IsBust;

        // The King is an anchor and forced replacement cannot evict it. Checked here rather than in
        // WaveDraft because an anchor may be a carried-over tower, and the draft can only see its own
        // half of the board.
        //
        // The bust exemption mirrors the defensive branch below rather than a reachable path: a
        // busting card never arrives here, because Hit locks the wave outright. It is kept so the two
        // reads of "a busting card displaces nothing" cannot drift apart.
        Require(busts || !IsAnchored(socket),
            $"{socket} holds a King. An anchor cannot be displaced - replace a different tower.");

        // A busting card is destroyed and never placed, so it displaces nothing - the same rule
        // WaveDraft.Place applies to the draft's own towers, held to here for persisted ones.
        IReadOnlyList<TowerState> remaining = busts
            ? Persisted
            : [.. Persisted.Where(t => t.Socket != socket)];

        return this with
        {
            Draft = Draft.Place(rank, family, socket),
            Persisted = remaining,
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
    /// <remarks>
    /// Carried-over towers move on equal terms with this hand's. The rule is "one move total -
    /// relocate one tower one socket, or swap two adjacent towers", unqualified as to whose tower
    /// (docs/design/05-battlefield.md § The adjustment window). By the third wave most of the board
    /// is persisted, so a window that could not touch them would be a window over almost nothing.
    /// </remarks>
    public WaveSession RelocateTower(SocketRef from, SocketRef to)
    {
        Require(Phase == WavePhase.AdjustmentWindow, "Relocation is only offered in the adjustment window.");
        Require(!MoveSpent, "The adjustment window is one move for the whole board, and it is already spent.");
        Require(AreAdjacent(from, to), "A relocation moves a tower one socket.");
        Require(IsOccupied(from), $"There is no tower at {from} to relocate.");
        Require(!IsOccupied(to), $"Socket {to} is occupied; a relocation needs an empty socket.");

        // Whichever half holds it moves it; the other is left alone by construction.
        return this with
        {
            Persisted = [.. Persisted.Select(t => t.Socket == from ? t with { Socket = to } : t)],
            Draft = Draft.WithSocketReassigned(from, to),
            MoveSpent = true,
        };
    }

    /// <summary>
    /// Swaps two adjacent towers, spending the wave's single adjustment move.
    /// </summary>
    /// <remarks>
    /// Either end may be a carried-over tower, including one of each: each half of the board remaps
    /// only the socket it holds, which composes to the whole exchange.
    /// </remarks>
    public WaveSession SwapTowers(SocketRef a, SocketRef b)
    {
        Require(Phase == WavePhase.AdjustmentWindow, "A swap is only offered in the adjustment window.");
        Require(Tuning.Rules.AdjustmentAllowsAdjacentSwap, "This tuning does not allow the adjacent swap.");
        Require(!MoveSpent, "The adjustment window is one move for the whole board, and it is already spent.");
        Require(AreAdjacent(a, b), "A swap exchanges two adjacent towers.");
        Require(IsOccupied(a) && IsOccupied(b), $"A swap needs a tower at both {a} and {b}.");

        return this with
        {
            Persisted =
            [
                .. Persisted.Select(t =>
                    t.Socket == a ? t with { Socket = b }
                    : t.Socket == b ? t with { Socket = a }
                    : t),
            ],
            Draft = Draft.WithSocketsExchanged(a, b),
            MoveSpent = true,
        };
    }

    /// <summary>
    /// Sets a tower's standing order. Free - it does not consume the single move.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Carried-over towers take orders too. An order is a pre-committed conditional about how a
    /// tower fights this wave, not a property of the hand that placed it, and combat has no live
    /// input - so a board of persisted towers with no settable orders would be a board with no
    /// tactics at all (docs/design/05-battlefield.md § Standing orders).
    /// </para>
    /// <para>
    /// <b>Editable throughout the wave, locking only when combat begins.</b> This widens Revision
    /// 7.1, which offered orders in the adjustment window alone: they are encounter skill rather
    /// than a secondary menu, and being able to tell a Siege Club to hold for the armored target
    /// <i>at the moment it is placed</i> - instead of several decisions later - is the whole point
    /// of the change (docs/design/05-battlefield.md § They are encounter skill, not a secondary
    /// menu). A locked wave refuses, and so does a bust, which locks placement immediately.
    /// </para>
    /// </remarks>
    public WaveSession SetStandingOrder(SocketRef socket, StandingOrder order)
    {
        ArgumentNullException.ThrowIfNull(order);

        Require(
            Phase is WavePhase.AwaitingPlacement or WavePhase.DrawDecision or WavePhase.AdjustmentWindow,
            "Standing orders lock when combat begins.");
        Require(!Tuning.Rules.StandingOrderChangesConsumeMove, "This tuning charges a move for a standing-order change, which the prototype does not.");
        Require(IsOccupied(socket), $"There is no tower at {socket} to give an order.");

        return IsPersisted(socket)
            ? this with { Persisted = [.. Persisted.Select(t => t.Socket == socket ? t with { Order = order } : t)] }
            : this with { Draft = Draft.WithOrder(socket, order) };
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
    /// The Visible Threat: what the revealed force would do against the board as it currently stands.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Exact against the base wave plus the Vanguard only - <b>not</b> a prediction of the wave.
    /// </para>
    /// <para>
    /// <b>Legal while cards are still waiting to be placed, as well as at the draw decision.</b>
    /// Earlier this refused outside <see cref="WavePhase.DrawDecision"/>, on the grounds that a card
    /// in hand meant a board the player was midway through building. The tactical loop supersedes
    /// that: <i>Read</i> and <i>Diagnose</i> are steps one and two and <i>Commit</i> is step three
    /// (docs/design/01-core-loop.md § The tactical loop), so the reading is wanted precisely
    /// <b>before</b> the card goes down - a player who cannot see what the lane currently does
    /// cannot form an intention for the card in their hand, which is the failure this whole
    /// milestone exists to fix.
    /// </para>
    /// <para>
    /// The board it describes is real but incomplete, and the interface must say so - the count of
    /// <see cref="PendingRanks"/> is what it says it with.
    /// </para>
    /// </remarks>
    public VisibleThreat VisibleThreatNow()
    {
        Require(
            Phase is WavePhase.AwaitingPlacement or WavePhase.DrawDecision,
            "The Visible Threat is a pre-Dealer reading; afterwards read the Final Forecast.");

        return RevealedAt(Entry);
    }

    /// <summary>
    /// The same revealed-force reading, one march step later. <b>The cost of a Hit, as consequence.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// Against the same board and the same revealed force, with the army entering where the next
    /// card would put it. The difference between this and <see cref="VisibleThreatNow"/> is what the
    /// step costs - which the timeline renders as attacks lost rather than as a change in a number:
    /// <b>"if you draw again, this cannon loses two shots"</b>, never "entry moves to 4.0"
    /// (docs/design/14-encounter-timeline.md).
    /// </para>
    /// <para>
    /// <b>This is not a next-draw preview.</b> It says nothing about the card - it prices the
    /// <i>step</i>, which is paid before the card is revealed and is certain. The baseline next-draw
    /// preview stays cut (docs/design/09-information-and-ui.md § Next-draw preview is cut from the
    /// baseline); the certainty is the battlefield's and the uncertainty is still the draw's.
    /// </para>
    /// <para>
    /// Null past the entry clamp, where a further card costs nothing on the clock and there is
    /// therefore no consequence to draw (docs/ROADMAP.md Open Decision 5).
    /// </para>
    /// </remarks>
    public VisibleThreat? NextStepThreat()
    {
        Require(
            Phase is WavePhase.AwaitingPlacement or WavePhase.DrawDecision,
            "The next step's cost is a pre-Dealer reading.");

        double entry = Entry;
        double next = Math.Min(entry + NextStepCost(), Tuning.March.EntryClampMax);

        return next - entry <= 1e-9 ? null : RevealedAt(next);
    }

    /// <summary>
    /// The lane the Dealer's hidden card will enter. <b>Its rank stays unknown.</b>
    /// </summary>
    /// <remarks>
    /// Baseline information: the player knows something unknown is coming to a named lane, which is
    /// uncertainty that does not prevent intention (docs/design/06-dealer-and-enemies.md § The
    /// hidden card's lane is visible from the start). The rule is the Dealer's own lane assignment,
    /// asked of card index one - the hole card - so there is no second copy of it here.
    /// </remarks>
    public int HiddenCardLane => DealerDeployment.LaneForCard(Tuning, Encounter, cardIndex: 1);

    /// <summary>The revealed-force resolution against the current board, with the army entering at
    /// <paramref name="entry"/>.</summary>
    /// <remarks>
    /// Board and army are both built at the same entry. They have to be: the resolver rejects a pair
    /// that disagrees about where the march has reached, because a mismatch resolves cleanly and
    /// answers the wrong question (<see cref="Resolver"/>).
    /// </remarks>
    private VisibleThreat RevealedAt(double entry) => Resolver.ResolveRevealed(
        Tuning,
        Encounter,
        CombinedBoard(Draft, entry),
        ArmyBuilder.Revealed(Tuning, Encounter, Vanguard, entry));

    /// <summary>
    /// What committing the pending card here would change, as causal facts. Null if it is not legal.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>It runs the real transition and re-reads the resolver.</b> The candidate session is built
    /// by calling <see cref="Place"/> and the readings come from
    /// <see cref="VisibleThreatNow"/> - so a preview cannot disagree with what actually happens,
    /// because it <i>is</i> what happens, discarded. Sessions are immutable, which is what makes
    /// that cheap enough to do per hover. Do not replace this with an estimator: a preview that
    /// drifts from the resolver is worse than no preview at all.
    /// </para>
    /// <para>
    /// Returns null rather than throwing when the placement is illegal - a King's socket is anchored
    /// against forced replacement - because the pointer resting on a socket is not an attempt to use
    /// it. <see cref="Place"/> still refuses if one is actually tried, and that refusal is recorded
    /// as a wanted move.
    /// </para>
    /// <para>
    /// <b>No aggregate comes out of here.</b> See <see cref="CandidateDelta"/>.
    /// </para>
    /// </remarks>
    public CandidateDelta? PreviewPlacement(Family family, SocketRef socket)
    {
        if (Phase != WavePhase.AwaitingPlacement || PendingRanks.Count == 0 || IsAnchored(socket))
        {
            return null;
        }

        WaveSession after = Place(family, socket);

        VisibleThreat before = VisibleThreatNow();
        VisibleThreat committed = after.VisibleThreatNow();
        VisibleThreat? stepped = after.NextStepThreat();

        TowerState? displaced = Board().Towers.FirstOrDefault(t => t.Socket == socket);

        return new CandidateDelta
        {
            Rank = PendingRanks[0],
            Family = family,
            Socket = socket,
            Displaces = displaced is null ? null : new DisplacedTower
            {
                Card = displaced.Card,
                Family = displaced.Family,
                ShotDamage = displaced.ShotDamageInLane(Tuning),
                RunBonus = displaced.RunBonus,
            },
            Lanes = [.. before.Lanes.Zip(committed.Lanes, LaneDeltaFor)],
            FateChanges = FateChanges(before, committed),
            TowerShots = ShotDeltas(before, committed, stepped),
            Runs = RunDeltaBetween(Board(), after.Board()),
        };
    }

    private static LaneDelta LaneDeltaFor(LaneOutcome before, LaneOutcome after) => new()
    {
        LaneIndex = after.LaneIndex,
        Stake = after.Stake,
        LeakCountBefore = before.LeakedUnits.Count,
        LeakCountAfter = after.LeakedUnits.Count,
        PredictedDamageBefore = before.PredictedDamage,
        PredictedDamageAfter = after.PredictedDamage,
        FirstLeakTimeBefore = FirstLeak(before),
        FirstLeakTimeAfter = FirstLeak(after),
    };

    private static double? FirstLeak(LaneOutcome lane) =>
        lane.LeakedUnits.Count == 0 ? null : lane.LeakedUnits.Min(u => u.LeakTime);

    /// <summary>Units that stop getting through, or start. Named, because a name is what a player repeats back.</summary>
    private IReadOnlyList<UnitFateChange> FateChanges(VisibleThreat before, VisibleThreat after)
    {
        Dictionary<int, LeakedUnit> was = before.Lanes
            .SelectMany(l => l.LeakedUnits)
            .ToDictionary(u => u.SpawnIndex);

        Dictionary<int, LeakedUnit> now = after.Lanes
            .SelectMany(l => l.LeakedUnits)
            .ToDictionary(u => u.SpawnIndex);

        List<UnitFateChange> changes = [];

        foreach (int spawnIndex in was.Keys.Union(now.Keys).Order())
        {
            bool leaksBefore = was.ContainsKey(spawnIndex);
            bool leaksAfter = now.ContainsKey(spawnIndex);

            if (leaksBefore == leaksAfter)
            {
                continue;
            }

            LeakedUnit unit = leaksBefore ? was[spawnIndex] : now[spawnIndex];

            changes.Add(new UnitFateChange
            {
                SpawnIndex = spawnIndex,
                EnemyId = unit.EnemyId,
                DisplayName = Tuning.Enemy(unit.EnemyId).DisplayName,
                LaneIndex = LaneOf(leaksBefore ? before : after, spawnIndex),
                LeaksBefore = leaksBefore,
                LeaksAfter = leaksAfter,
            });
        }

        return changes;
    }

    private static int LaneOf(VisibleThreat threat, int spawnIndex) =>
        threat.Lanes.First(l => l.LeakedUnits.Any(u => u.SpawnIndex == spawnIndex)).LaneIndex;

    /// <summary>
    /// Every tower's attack count before, after, and one march step later.
    /// </summary>
    /// <remarks>
    /// Keyed on socket and lane, so a junction tower keeps its two separate entries - which is the
    /// honest reading of buying breadth and forfeiting synergy.
    /// </remarks>
    private static IReadOnlyList<TowerShotDelta> ShotDeltas(
        VisibleThreat before, VisibleThreat after, VisibleThreat? stepped)
    {
        Dictionary<(int Lane, SocketRef Socket), int> was = ShotsBySocket(before);
        Dictionary<(int Lane, SocketRef Socket), int> now = ShotsBySocket(after);
        Dictionary<(int Lane, SocketRef Socket), int>? next = stepped is null ? null : ShotsBySocket(stepped);

        return
        [
            .. now.Keys
                .Union(was.Keys)
                .OrderBy(key => key.Lane)
                .ThenBy(key => key.Socket.IsJunction)
                .ThenBy(key => key.Socket.SocketIndex)
                .Select(key => new TowerShotDelta
                {
                    Socket = key.Socket,
                    LaneIndex = key.Lane,
                    Before = was.GetValueOrDefault(key),
                    After = now.GetValueOrDefault(key),
                    AfterNextStep = next?.GetValueOrDefault(key),
                }),
        ];
    }

    private static Dictionary<(int Lane, SocketRef Socket), int> ShotsBySocket(VisibleThreat threat) =>
        threat.Lanes
            .SelectMany(lane => lane.TowerActivity.Select(a => (Key: (lane.LaneIndex, a.Socket), a.Shots)))
            .ToDictionary(entry => entry.Key, entry => entry.Shots);

    private static RunDelta RunDeltaBetween(BoardState before, BoardState after) => new()
    {
        LinkedTowersBefore = before.Towers.Count(t => t.RunBonus > 0.0),
        LinkedTowersAfter = after.Towers.Count(t => t.RunBonus > 0.0),
        BestBonusBefore = before.Towers.Count == 0 ? 0.0 : before.Towers.Max(t => t.RunBonus),
        BestBonusAfter = after.Towers.Count == 0 ? 0.0 : after.Towers.Max(t => t.RunBonus),
    };

    /// <summary>
    /// The undefended cost of each lane against the revealed force.
    /// </summary>
    /// <remarks>
    /// Legal before the Dealer resolves, which includes the moment before the opening deal - the point
    /// docs/design/09-information-and-ui.md § Shown asks for it. Afterwards the Final Forecast carries
    /// the same figure against the complete army, and that is the one to read.
    /// </remarks>
    public OpeningStakes OpeningStakes()
    {
        Require(DealerCards is null, "The Dealer has resolved; read empty-lane damage from the Final Forecast.");

        double entry = Entry;

        return Resolver.ResolveEmptyLanes(
            Tuning, Encounter, ArmyBuilder.Revealed(Tuning, Encounter, Vanguard, entry), entry);
    }

    /// <summary>
    /// The complete army, once the Dealer has resolved: everything that will walk in.
    /// </summary>
    /// <remarks>
    /// § Shown asks for the "full army after the Dealer resolves, before lock". The Dealer's hand
    /// <i>is</i> the army (docs/design/06-dealer-and-enemies.md), so the player who has been counting
    /// it needs to see what it turned into before spending the adjustment move.
    /// </remarks>
    public CompleteArmy ResolvedArmy()
    {
        Require(DealerCards is not null, "The Dealer has not resolved yet; there is no complete army.");

        return ArmyBuilder.Complete(Tuning, Encounter, DealerCards!, Entry);
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
    /// Whether an anchor stands on <paramref name="socket"/> and forced replacement cannot reach it.
    /// </summary>
    /// <remarks>
    /// Public so the interface can decline to <i>offer</i> a placement the session would refuse. It is
    /// not a second copy of the rule - <see cref="Place"/> still enforces it, and a refusal from the
    /// board is surfaced as a wanted-move signal rather than swallowed.
    /// </remarks>
    public bool IsAnchored(SocketRef socket) =>
        Board().Towers.Any(t => t.Socket == socket && t.IsAnchor);

    /// <summary>
    /// Whether this is the encounter's last wave, after which the board resets.
    /// </summary>
    /// <remarks>
    /// docs/design/05-battlefield.md § Persistence: towers persist across the waves of an encounter
    /// and <b>reset at the encounter boundary</b>. Persistence exists to create scarcity, not to bank
    /// power - so it is bounded, and the bound is the encounter.
    /// </remarks>
    public bool IsFinalWaveOfEncounter => WaveNumber >= Encounter.Waves;

    /// <summary>The wave number the next wave carries: one again once the encounter has run out.</summary>
    public int NextWaveNumber => IsFinalWaveOfEncounter ? 1 : WaveNumber + 1;

    /// <summary>
    /// This wave with a different shoe, for counterfactual analysis.
    /// </summary>
    /// <remarks>
    /// Internal because it is not a move: nothing a player does replaces the shoe. It exists so the
    /// oracle-tier instrumentation can ask "what if the next card were a 3?" by pairing this with
    /// <see cref="Shoe.WithNextCard"/>, and then drive the resulting session through the ordinary
    /// transitions rather than reimplementing them.
    /// </remarks>
    internal WaveSession WithShoe(Shoe shoe) => this with { Shoe = shoe };

    /// <summary>
    /// Ends the wave: reverts the surviving current-hand towers to ×1.00 for the next wave and
    /// reshuffles the shoe if it has run low.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Persisted towers carry forward at ×1.00 (docs/design/05-battlefield.md § Persistence), which
    /// is also why a bust is scoped to the current hand: earlier waves' towers are already at ×1.00,
    /// so the bust multiplier cannot drag them down.
    /// </para>
    /// <para>
    /// <b>Nothing carries past the encounter boundary.</b> Persistence is bounded on purpose: it
    /// exists so that sockets fill during the second wave and every card after that forces a
    /// replacement, which is scarcity rather than banked power. Unbounded, the board saturates and
    /// then stops changing - the draw keeps happening but nothing it produces can matter.
    /// </para>
    /// </remarks>
    public (IReadOnlyList<TowerState> Persisted, Shoe Shoe) Settle()
    {
        Require(Phase is WavePhase.Locked or WavePhase.BustLocked, "A wave settles only after it is locked.");

        if (IsFinalWaveOfEncounter)
        {
            return ([], Shoe.ReshuffleIfLow(Tuning));
        }

        double persistedMultiplier = Tuning.FormationStrength.Persisted;

        // No socket filter: the two halves are disjoint by construction, because a placement onto an
        // occupied socket evicts whatever stood there at placement time. Filtering here instead would
        // resolve a collision backwards - keeping the tower that was replaced and discarding the one
        // that replaced it.
        List<TowerState> carried =
        [
            .. Persisted,
            .. Draft.BuildBoard(Tuning, PersistedSockets).Towers
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
        BoardState current = draft.BuildBoard(Tuning, PersistedSockets);

        // WithEntry rather than the draft's own board as-is. BuildBoard stamps the entry the draft's
        // hand implies, which is right for every reading of *now* and wrong for the only reading that
        // is not: NextStepThreat asks the same board what it does one march step later. Leaving the
        // draft's entry in place there produced a board and an army that disagreed about where the
        // march had reached - which the resolver refuses outright, exactly as it is meant to.
        //
        // It moves the entry without rebuilding the towers, so run links, Ace states, and the
        // Formation multiplier are untouched. That matters for the persisted branch below: those
        // towers already reverted to x1.00 with their run bonuses cleared, and re-deriving links over
        // the combined board would quietly hand them back.
        return Persisted.Count == 0
            ? current.WithEntry(entry)
            : BoardState.Create(Tuning, [.. Persisted, .. current.Towers], entry);
    }

    /// <summary>
    /// Sockets held by carried-over towers, which the draft must not seat anything on top of.
    /// </summary>
    private IReadOnlyCollection<SocketRef> PersistedSockets => [.. Persisted.Select(t => t.Socket)];

    /// <summary>Whether any tower stands at this socket, this hand's or a carried-over one.</summary>
    private bool IsOccupied(SocketRef socket) =>
        IsPersisted(socket) || Draft.Placements.Any(p => p.Socket == socket);

    /// <summary>Whether the tower at this socket is one carried in from an earlier wave.</summary>
    private bool IsPersisted(SocketRef socket) => Persisted.Any(t => t.Socket == socket);

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
