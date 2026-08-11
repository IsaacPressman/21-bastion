using Bastion.Core.Board;
using Bastion.Core.Cards;
using Bastion.Core.Config;
using Bastion.Core.Resolve;
using Bastion.Core.Validation;
using Bastion.Core.Wave;

namespace Bastion.Core.Tests.Validation;

/// <summary>
/// The scripted battery opens, and each case's two presentations are the same decision.
/// </summary>
/// <remarks>
/// <para>
/// docs/prototype/VALIDATION.md § Scripted battery. Two claims are worth testing and they are
/// different: that a case reaches <b>the state it says it offers</b>, and that variant B is
/// <b>the same decision</b> as variant A rather than a second one that merely looks similar.
/// </para>
/// <para>
/// The second is the one that could rot silently. A mirrored fixture that quietly changed the
/// problem would still run, still log, and would turn "presented twice so players cannot answer
/// from memory" into two unrelated measurements averaged together.
/// </para>
/// </remarks>
public sealed class BatteryTests
{
    private static readonly Battery Battery = BatteryLoader.LoadFromRepositoryRoot();
    private static readonly TuningData Tuning = Battery.Apply(TuningLoader.LoadFromRepositoryRoot());

    public static TheoryData<string> EveryCase()
    {
        TheoryData<string> data = [];

        foreach (BatteryFixture fixture in Battery.Fixtures)
        {
            data.Add(fixture.Id);
        }

        return data;
    }

    /// <summary>Authored cases only - each is paired with the mirror generated from it.</summary>
    public static TheoryData<string> EveryAuthoredCase()
    {
        TheoryData<string> data = [];

        foreach (BatteryFixture fixture in Battery.Fixtures
                     .Where(f => !f.Id.EndsWith(LaneMirror.VariantSuffix, StringComparison.Ordinal)))
        {
            data.Add(fixture.Id);
        }

        return data;
    }

    private static BatteryFixture Case(string id) =>
        Battery.Find(id) ?? throw new InvalidOperationException($"No battery case '{id}'.");

    [Fact]
    public void All_ten_battery_items_are_covered()
    {
        // VALIDATION.md lists ten states. Several name a contrast, so the case count is higher -
        // but every one of the ten must be represented or the battery has a hole in it.
        int[] covered = [.. Battery.Fixtures.Select(f => f.BatteryItem).Distinct().Order()];

        Assert.Equal(Enumerable.Range(1, 10), covered);
    }

    [Fact]
    public void Every_case_is_presented_twice()
    {
        // "Each state presented at least twice with different presentation so players cannot answer
        // from memory." The mirror is generated, so this asserts the generation actually happened.
        foreach (BatteryFixture authored in Battery.Fixtures
                     .Where(f => !f.Id.EndsWith(LaneMirror.VariantSuffix, StringComparison.Ordinal)))
        {
            Assert.NotNull(Battery.Find(authored.Id + LaneMirror.VariantSuffix));
        }

        Assert.Equal(0, Battery.Fixtures.Count % 2);
    }

    [Theory]
    [MemberData(nameof(EveryCase))]
    public void A_case_opens_at_the_state_it_claims_to_offer(string id)
    {
        // BatteryFixture.Open throws if the script lands anywhere else. A fixture offering a
        // different decision than the one it names is worse than one that will not run.
        WaveSession session = Case(id).Open(Tuning);

        Assert.Equal(Case(id).OfferedAt, session.Phase);
    }

    [Theory]
    [MemberData(nameof(EveryCase))]
    public void A_case_offers_a_readable_state(string id)
    {
        BatteryFixture fixture = Case(id);
        WaveSession session = fixture.Open(Tuning);

        // Whatever the phase, the two things a facilitator's log entry turns on must be present.
        Assert.False(session.Hand.IsBust, "A battery case offers a decision; a busted hand has none left.");
        Assert.NotEmpty(fixture.Question);

        // And the state must be the kind the phase promises.
        switch (session.Phase)
        {
            case WavePhase.AwaitingPlacement:
                Assert.NotEmpty(session.PendingRanks);
                break;

            case WavePhase.DrawDecision:
                Assert.Empty(session.PendingRanks);
                Assert.NotEmpty(session.VisibleThreatNow().Lanes);
                break;

            case WavePhase.AdjustmentWindow:
                Assert.NotNull(session.DealerCards);
                Assert.False(session.MoveSpent, "The adjustment case must open with its one move unspent.");
                break;

            default:
                Assert.Fail($"'{id}' offers {session.Phase}, which is not a decision state.");
                break;
        }
    }

    [Theory]
    [MemberData(nameof(EveryAuthoredCase))]
    public void The_mirrored_variant_is_the_same_decision_with_the_lanes_exchanged(string id)
    {
        // The load-bearing claim behind "presented twice". Both variants are driven to their offered
        // state, then to a locked wave, and the mirrored Final Forecast must be the original's with
        // lane 0 and lane 1 swapped. Predicted damage is the combat contract, so if these agree the
        // player is facing the same problem from the other side.
        // A case offered at AwaitingPlacement has cards still to seat, and where they go must be
        // mirrored too - completing each variant independently would compare two different boards
        // and blame the mirror for the difference.
        (FinalForecast original, IReadOnlyList<SocketRef> seated) = ForecastOf(Case(id), seatAt: null);

        (FinalForecast mirrored, _) = ForecastOf(
            Case(id + LaneMirror.VariantSuffix), seatAt: [.. seated.Select(LaneMirror.Mirror)]);

        Assert.Equal(original.Lanes[0].PredictedDamage, mirrored.Lanes[1].PredictedDamage);
        Assert.Equal(original.Lanes[1].PredictedDamage, mirrored.Lanes[0].PredictedDamage);

        Assert.Equal(original.Lanes[0].EmptyLaneDamage, mirrored.Lanes[1].EmptyLaneDamage);
        Assert.Equal(original.Lanes[1].EmptyLaneDamage, mirrored.Lanes[0].EmptyLaneDamage);

        // The stakes travel with their lane, so triage reads the same way from either side.
        Assert.Equal(original.Lanes[0].Stake, mirrored.Lanes[1].Stake);
        Assert.Equal(original.Lanes[1].Stake, mirrored.Lanes[0].Stake);
    }

    [Theory]
    [MemberData(nameof(EveryAuthoredCase))]
    public void The_mirrored_variant_deals_the_same_hand_in_a_different_order(string id)
    {
        BatteryFixture authored = Case(id);
        BatteryFixture mirrored = Case(id + LaneMirror.VariantSuffix);

        // Same cards - a different multiset would be a different hand, not a presentation of it.
        Assert.Equal(
            authored.CardOrder.Order().ToArray(),
            mirrored.CardOrder.Order().ToArray());

        // ...dealt the other way round, so the opening two do not read identically.
        Assert.Equal(authored.CardOrder[2], mirrored.CardOrder[3]);
        Assert.Equal(authored.CardOrder[3], mirrored.CardOrder[2]);
    }

    [Theory]
    [MemberData(nameof(EveryAuthoredCase))]
    public void The_mirrored_variant_puts_every_tower_in_the_other_lane(string id)
    {
        BoardState original = Case(id).Open(Tuning).Board();
        BoardState mirrored = Case(id + LaneMirror.VariantSuffix).Open(Tuning).Board();

        Assert.Equal(original.Towers.Count, mirrored.Towers.Count);

        foreach (TowerState tower in original.Towers)
        {
            SocketRef expected = LaneMirror.Mirror(tower.Socket);

            TowerState counterpart = Assert.Single(mirrored.Towers, t => t.Socket == expected);

            Assert.Equal(tower.Card.Rank, counterpart.Card.Rank);
            Assert.Equal(tower.Family, counterpart.Family);
            Assert.Equal(tower.Range, counterpart.Range, precision: 6);
        }
    }

    [Fact]
    public void The_only_twenty_one_in_the_single_rank_case_really_is_a_single_rank()
    {
        // Item 7 is the one case whose remaining pile is part of the state, so it is worth asserting
        // rather than trusting the hand-written card order. The rank display is the whole mechanism
        // here - risk as a reading skill, with no percentage shown.
        WaveSession session = Case("7-onlyrank").Open(Tuning);

        Assert.Equal(18, session.Hand.Total);

        IReadOnlyDictionary<Rank, int> pile = session.Shoe.RemainingComposition();

        int reaching21 = pile
            .Where(entry => session.Hand.Hit(entry.Key).Total == 21)
            .Sum(entry => entry.Value);

        Assert.Equal(1, reaching21);
    }

    [Fact]
    public void The_capacity_case_offers_no_empty_socket()
    {
        // Item 5 only exists if the board is genuinely full - otherwise the "forced" replacement is
        // just a placement, and the hesitation the criterion looks for has nothing to be about.
        WaveSession session = Case("5-capacity").Open(Tuning);

        Assert.Equal(Tuning.Geometry.TotalSockets, session.Board().Towers.Count);
        Assert.NotEmpty(session.PendingRanks);

        // And the board must not be stocked with anchors. A King cannot be displaced, so a socket
        // holding one is not a choice the player weighs - it is one they cannot make. Enough of them
        // and item 5 stops asking "which of your towers goes?" and starts asking nothing at all.
        Assert.Contains(session.Board().Towers, t => !t.IsAnchor);
    }

    /// <summary>
    /// Drives a case from its offered state to a locked wave, and reads the contract.
    /// </summary>
    /// <remarks>
    /// The equivalence comparison needs a Final Forecast, which exists only once the Dealer has
    /// resolved - so a case offered mid-placement has to be finished off first. Pass
    /// <paramref name="seatAt"/> to replay a known set of sockets; pass null to choose them and have
    /// them reported back, so the mirrored run can be given their reflections.
    /// </remarks>
    private static (FinalForecast Forecast, IReadOnlyList<SocketRef> Seated) ForecastOf(
        BatteryFixture fixture, IReadOnlyList<SocketRef>? seatAt)
    {
        WaveSession session = fixture.Open(Tuning);
        List<SocketRef> seated = [];

        while (session.Phase == WavePhase.AwaitingPlacement)
        {
            SocketRef socket = seatAt is not null ? seatAt[seated.Count] : FirstFreeSocket(session);

            session = session.Place(Family.Club, socket);
            seated.Add(socket);
        }

        if (session.Phase == WavePhase.DrawDecision)
        {
            session = session.Stand();
        }

        return (session.Forecast(), seated);
    }

    private static SocketRef FirstFreeSocket(WaveSession session)
    {
        HashSet<SocketRef> taken = [.. session.Board().Towers.Select(t => t.Socket)];

        for (int lane = 0; lane < Tuning.Geometry.Lanes; lane++)
        {
            for (int socket = 0; socket < Tuning.Geometry.SocketPositions.Count; socket++)
            {
                if (!taken.Contains(SocketRef.InLane(lane, socket)))
                {
                    return SocketRef.InLane(lane, socket);
                }
            }
        }

        // At capacity the placement replaces something; the rear socket is a fixed, arbitrary choice.
        return taken.Contains(SocketRef.Junction)
            ? SocketRef.InLane(0, Tuning.Geometry.SocketPositions.Count - 1)
            : SocketRef.Junction;
    }
}
