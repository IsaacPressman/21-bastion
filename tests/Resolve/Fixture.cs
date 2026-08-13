using Bastion.Core.Board;
using Bastion.Core.Cards;
using Bastion.Core.Config;
using Bastion.Core.Resolve;

namespace Bastion.Core.Tests.Resolve;

/// <summary>
/// Shared scaffolding for resolver tests.
/// </summary>
/// <remarks>
/// Boards are built directly rather than played into existence, because Milestone 1 has no hand,
/// no shoe, and no phase machine. That is the point of <see cref="BoardState"/> taking resolved
/// numbers: a fixture can state "a King at socket 9 at x1.30" without a wave in progress.
/// </remarks>
internal static class Fixture
{
    internal static readonly TuningData Tuning = TuningLoader.LoadFromRepositoryRoot();

    internal static EncounterTuning Example => Tuning.Encounter("example_wave");

    /// <summary>An encounter with one enemy type in lane 0 and nothing in lane 1.</summary>
    /// <remarks>
    /// Lets a test isolate one interaction - armor, splash, a standing order - without the worked
    /// example's twelve units arriving on top of it.
    /// </remarks>
    internal static EncounterTuning Solo(string enemyId, int count) =>
        Isolated((enemyId, count));

    /// <summary>
    /// An encounter whose lane 0 holds the given groups and whose Dealer deploys into lane 1.
    /// </summary>
    /// <remarks>
    /// The vanguard lane is deliberately the <i>other</i> lane. A Complete army needs an upcard,
    /// and putting it in lane 1 keeps lane 0's timeline to exactly the units the test asked for -
    /// which is what makes an exact assertion about a shot possible.
    /// </remarks>
    internal static EncounterTuning Isolated(params (string EnemyId, int Count)[] groups) => new()
    {
        Id = $"isolated_{string.Join("_", groups.Select(g => $"{g.EnemyId}x{g.Count}"))}",
        VanguardLane = 1,
        Waves = Example.Waves,
        LaneStakes = ["bastion", "vault"],
        BaseWave =
        [
            [.. groups.Select(g => new SpawnGroupTuning { EnemyId = g.EnemyId, Count = g.Count })],
            [],
        ],
    };

    internal static TowerState Tower(
        Rank rank,
        Family family,
        SocketRef socket,
        double formation = 1.0,
        double runBonus = 0.0,
        StandingOrder? order = null,
        bool aceHigh = false) =>
        TowerState.Place(Tuning, new Card(rank, aceHigh), family, socket, formation, runBonus, order);

    internal static BoardState Board(params TowerState[] towers) =>
        BoardState.Create(Tuning, towers, Tuning.Geometry.DefaultEntry);

    internal static BoardState BoardAt(double entry, params TowerState[] towers) =>
        BoardState.Create(Tuning, towers, entry);

    /// <summary>Socket 0/1/2 in a lane, at path positions 3.0 / 6.0 / 9.0.</summary>
    internal static SocketRef Socket(int lane, int index) => SocketRef.InLane(lane, index);

    /// <summary>
    /// The army with no Dealer draws beyond the upcard - the state during the draw.
    /// </summary>
    internal static RevealedArmy Revealed(EncounterTuning encounter, Rank vanguard, double entry = 0.0) =>
        ArmyBuilder.Revealed(Tuning, encounter, new Card(vanguard), entry);

    internal static CompleteArmy Complete(EncounterTuning encounter, Rank[] dealerHand, double entry = 0.0) =>
        ArmyBuilder.Complete(Tuning, encounter, [.. dealerHand.Select(r => new Card(r))], entry);

    internal static VisibleThreat ResolveRevealed(EncounterTuning encounter, BoardState board, RevealedArmy army) =>
        Resolver.ResolveRevealed(Tuning, encounter, board, army);

    internal static FinalForecast ResolveComplete(EncounterTuning encounter, BoardState board, CompleteArmy army) =>
        Resolver.ResolveComplete(Tuning, encounter, board, army);

    /// <summary>Total damage this tower landed across the lane, after armor.</summary>
    internal static double DamageBy(LaneOutcome lane, SocketRef socket) =>
        lane.TowerActivity.Single(a => a.Socket == socket).DamageApplied;

    /// <summary>
    /// Resolves an isolated lane-0 encounter and hands back that lane's events.
    /// </summary>
    /// <remarks>
    /// Goes through <see cref="Resolver.ResolveComplete"/> because these tests are about the wave
    /// that will run. A Visible Threat carries a <see cref="RevealedTimeline"/> as of Milestone 6,
    /// but it is a schedule for the revealed force rather than a recording of combat - and it is a
    /// separate type precisely so the two cannot be swapped. A one-card Dealer hand keeps the army
    /// complete while its units land in the other lane.
    /// </remarks>
    internal static IReadOnlyList<TimelineEvent> EventsInLaneZero(
        EncounterTuning encounter, BoardState board) =>
        [.. Forecast(encounter, board).Timeline.Events.Where(e => e.LaneIndex == 0)];

    internal static IReadOnlyList<ShotEvent> ShotsInLaneZero(EncounterTuning encounter, BoardState board) =>
        [.. EventsInLaneZero(encounter, board).OfType<ShotEvent>()];

    /// <summary>Lane 0's outcome from a Final Forecast over an isolated encounter.</summary>
    internal static LaneOutcome LaneZero(EncounterTuning encounter, BoardState board) =>
        Forecast(encounter, board).Lanes[0];

    /// <summary>
    /// Resolves an isolated encounter against a board, entering where the board says.
    /// </summary>
    /// <remarks>
    /// The army is built at <see cref="BoardState.Entry"/> rather than at the default, because the
    /// resolver rejects a board and an army that disagree about where the march has reached.
    /// </remarks>
    private static FinalForecast Forecast(EncounterTuning encounter, BoardState board) =>
        ResolveComplete(encounter, board, Complete(encounter, [Rank.Two], board.Entry));
}
