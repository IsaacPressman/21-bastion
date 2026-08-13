using Bastion.Core.Board;
using Bastion.Core.Cards;
using Bastion.Core.Config;
using Bastion.Core.March;
using Bastion.Core.Resolve;
using Bastion.Core.Wave;

namespace Bastion.Core.Validation;

/// <summary>
/// Reads an offered state off a <see cref="WaveSession"/> into a loggable record.
/// </summary>
/// <remarks>
/// <para>
/// Engine-free and pure, so the same capture runs under the Godot layer during a playtest and under
/// the regression suite headlessly. Nothing here decides anything - it reads what the session
/// already knows and names it.
/// </para>
/// <para>
/// The lane reading is whichever forecast the phase actually permits, and it is <b>labelled</b>:
/// Visible Threat at the draw decision, Final Forecast once the Dealer has resolved. The two are
/// different claims and the log has to say which one it holds, for exactly the reason the UI does
/// (docs/design/09-information-and-ui.md § Two forecasts must never share a surface). A reading is
/// omitted entirely rather than guessed at when the phase offers neither.
/// </para>
/// </remarks>
public static class SessionSnapshot
{
    /// <summary>The Visible Threat's label in a log. Exact about the revealed force only.</summary>
    public const string RevealedForce = "visible-threat";

    /// <summary>The Final Forecast's label. The combat contract.</summary>
    public const string CombatContract = "final-forecast";

    /// <summary>No forecast is legal in this phase, so none was read.</summary>
    public const string NoReading = "none";

    public static StateRecord Capture(WaveSession session, string? fixtureId = null)
    {
        ArgumentNullException.ThrowIfNull(session);

        TuningData tuning = session.Tuning;
        BoardState board = session.Board();

        (string reading, IReadOnlyList<LaneOutcome> lanes) = ReadLanes(session);

        return new StateRecord
        {
            FixtureId = fixtureId,
            Arm = tuning.March.ActivePreset,
            Phase = session.Phase.ToString(),
            Hand = CaptureHand(session, tuning),
            Pile = CapturePile(session),
            March = new MarchRecord
            {
                Entry = session.Entry,
                NextStepCost = session.NextStepCost(),
                CardCount = session.Hand.CardCount,
            },
            Sockets = CaptureSockets(session, board),
            Lanes = [.. lanes.Select(lane => new LaneRecord
            {
                LaneIndex = lane.LaneIndex,
                Stake = lane.Stake,
                EmptyLaneDamage = lane.EmptyLaneDamage,
                PredictedDamage = lane.PredictedDamage,
                IsOpen = lane.IsOpen(tuning.Rules.OpenHeldThresholdFraction),
                FirstLeakTime = lane.LeakedUnits.Count == 0
                    ? null
                    : lane.LeakedUnits.Min(u => u.LeakTime),
            })],
            LaneReading = reading,
            Dealer = CaptureDealer(session, tuning),
            PendingRanks = [.. session.PendingRanks.Select(r => r.TuningKey())],
            MoveSpent = session.MoveSpent,
        };
    }

    /// <summary>
    /// The forecast this phase permits, and its name. Never both, and never the wrong one.
    /// </summary>
    /// <remarks>
    /// Calling the wrong one throws by design, so the phase is checked rather than the exception
    /// caught: a log that quietly recorded a Visible Threat under the Final Forecast's name would
    /// corrupt the "result versus Final Forecast" comparison that VALIDATION.md step 4 rests on.
    /// </remarks>
    private static (string Reading, IReadOnlyList<LaneOutcome> Lanes) ReadLanes(WaveSession session) =>
        session.Phase switch
        {
            // AwaitingPlacement reads too. The revealed force is legal before the card goes down -
            // Read and Diagnose precede Commit - and it is the reading a player forms an intention
            // from, so a log that omitted it could not answer whether they had one
            // (docs/design/01-core-loop.md § The tactical loop).
            WavePhase.AwaitingPlacement or WavePhase.DrawDecision =>
                (RevealedForce, session.VisibleThreatNow().Lanes),

            WavePhase.AdjustmentWindow or WavePhase.Locked or WavePhase.BustLocked =>
                (CombatContract, session.Forecast().Lanes),

            _ => (NoReading, []),
        };

    private static HandRecord CaptureHand(WaveSession session, TuningData tuning) => new()
    {
        Cards = [.. session.Hand.Cards.Select(r => r.TuningKey())],
        Total = session.Hand.Total,
        IsSoft = session.Hand.IsSoft,
        AcesHigh = session.Hand.AceHighCount,
        IsBust = session.Hand.IsBust,
        FormationMultiplier = session.FormationMultiplier,
    };

    private static IReadOnlyList<PileEntry> CapturePile(WaveSession session) =>
    [
        .. session.Shoe.RemainingComposition()
            .OrderBy(entry => (int)entry.Key)
            .Select(entry => new PileEntry
            {
                Rank = entry.Key.TuningKey(),
                Remaining = entry.Value,
                WouldBust = session.Hand.Hit(entry.Key).IsBust,
                ReachesTwentyOne = session.Hand.Hit(entry.Key).Total == 21,
            }),
    ];

    /// <summary>
    /// Every socket, with its window now and its window one march step from now.
    /// </summary>
    /// <remarks>
    /// Both numbers are per socket and neither is summed. The pair is what the design shows the
    /// player on the lane - "which socket windows the next march step would cut into" - so logging
    /// the same pair keeps the log and the screen describing the same thing
    /// (docs/design/09-information-and-ui.md § March cost is shown on the lane).
    /// </remarks>
    private static IReadOnlyList<SocketRecord> CaptureSockets(WaveSession session, BoardState board)
    {
        TuningData tuning = session.Tuning;
        double entry = session.Entry;
        double nextEntry = Math.Min(entry + session.NextStepCost(), tuning.March.EntryClampMax);
        double path = tuning.Geometry.PathLength;

        Dictionary<SocketRef, TowerState> occupants = board.Towers.ToDictionary(t => t.Socket);

        List<SocketRecord> records = [];

        foreach (SocketRef socket in AllSockets(tuning))
        {
            occupants.TryGetValue(socket, out TowerState? tower);

            double position = socket.IsJunction
                ? tuning.Towers.JunctionPathPosition
                : tuning.Geometry.SocketPositions[socket.SocketIndex];

            // An empty socket still has a window - that is what makes it worth taking - so the
            // socket's own range is used when nothing stands there, and the tower's when one does
            // (a face card reaches further than its socket alone would).
            double range = tower?.Range ?? TowerState.RangeFor(tuning, socket, faceCard: false);

            records.Add(new SocketRecord
            {
                Socket = Describe(socket),
                Depth = position,
                Range = range,
                Occupied = tower is not null,
                Rank = tower?.Card.Rank.TuningKey(),
                Family = tower?.Family.ToString(),
                RunBonus = tower?.RunBonus ?? 0.0,
                WindowRemaining = Engagement.ForSocket(position, range, entry, path),
                WindowAfterNextStep = Engagement.ForSocket(position, range, nextEntry, path),
            });
        }

        return records;
    }

    private static DealerRecord CaptureDealer(WaveSession session, TuningData tuning) => new()
    {
        Upcard = session.Vanguard.Rank.TuningKey(),
        UpcardUnit = UnitFor(tuning, session.Vanguard),
        VanguardLane = session.Encounter.VanguardLane,
        HiddenCardLane = session.HiddenCardLane,
        Cards = session.DealerCards is null ? null : [.. session.DealerCards.Select(c => c.Rank.TuningKey())],
        DeployedUnits = session.DealerCards is null ? null : [.. session.DealerCards.Select(c => UnitFor(tuning, c))],
    };

    /// <summary>
    /// The unit a Dealer card deploys.
    /// </summary>
    /// <remarks>
    /// A low Ace deploys the scout rather than the elite, which is the Herald's 1-vs-11 split. One
    /// of the success criteria is that players read the upcard <b>as a unit on the field, not a
    /// number</b>, so the log records the unit alongside the rank.
    /// </remarks>
    private static string UnitFor(TuningData tuning, Card card)
    {
        string key = card.Rank == Rank.Ace && !card.AceHigh ? "A_low" : card.Rank.TuningKey();

        return tuning.DealerCardUnits.TryGetValue(key, out string? unit) ? unit : "unknown";
    }

    private static IEnumerable<SocketRef> AllSockets(TuningData tuning)
    {
        for (int lane = 0; lane < tuning.Geometry.Lanes; lane++)
        {
            for (int socket = 0; socket < tuning.Geometry.SocketPositions.Count; socket++)
            {
                yield return SocketRef.InLane(lane, socket);
            }
        }

        yield return SocketRef.Junction;
    }

    /// <summary>
    /// The notation the fixtures and the placement sweeps already use.
    /// </summary>
    /// <remarks>
    /// Public so the interface-side log names sockets the same way the core does. The hover counts
    /// are compared against socket occupancy and depth from the same line, and two notations for one
    /// socket would make that join a manual step in every analysis pass.
    /// </remarks>
    public static string Describe(SocketRef socket) =>
        socket.IsJunction ? "J" : $"L{socket.LaneIndex}S{socket.SocketIndex}";
}
