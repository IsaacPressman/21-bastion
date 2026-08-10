using Bastion.Core.Board;
using Bastion.Core.Config;
using Bastion.Core.Resolve;
using Bastion.Core.Validation;
using Bastion.Core.Wave;

namespace Bastion.Core.Tests.Regression;

/// <summary>
/// Regression procedure 4, over the scripted fixtures rather than one hand-built board.
/// </summary>
/// <remarks>
/// <para>
/// docs/prototype/VALIDATION.md step 4: "Verify Final-Forecast-versus-resolution equivalence
/// <b>on the scripted fixtures</b>, and verify that Visible Threat matches a resolver run against
/// the revealed force alone." <c>tests/Resolve/EquivalenceTests.cs</c> proves both claims against a
/// single fixed board; this runs the same two claims across every state the battery will actually
/// present to a player.
/// </para>
/// <para>
/// The distinction matters because the two forecasts are <b>different claims</b>. Only the Final
/// Forecast is the combat contract - if it says a lane leaks two, the wave leaks two. The Visible
/// Threat is exact about the revealed force and is <i>not</i> a prediction of the wave, so it is
/// checked against a resolver run over that force alone and nothing more.
/// </para>
/// </remarks>
public sealed class FixtureEquivalence
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

    private static BatteryFixture Case(string id) =>
        Battery.Find(id) ?? throw new InvalidOperationException($"No battery case '{id}'.");

    [Theory]
    [MemberData(nameof(EveryCase))]
    [Trait(Regression.Trait, Regression.Category)]
    public void The_final_forecast_is_what_the_timeline_does_in_every_fixture(string id)
    {
        // The combat contract, checked on every state the battery offers. A forecast that disagreed
        // with its own timeline would be a lie told to the player at the moment they commit.
        FinalForecast forecast = Locked(Case(id)).Forecast();

        foreach (LaneOutcome lane in forecast.Lanes)
        {
            int leakedInTimeline = forecast.Timeline.Events
                .OfType<LeakEvent>()
                .Where(e => e.LaneIndex == lane.LaneIndex)
                .Sum(e => e.LeakDamage);

            Assert.Equal(lane.PredictedDamage, leakedInTimeline);
        }
    }

    [Theory]
    [MemberData(nameof(EveryCase))]
    [Trait(Regression.Trait, Regression.Category)]
    public void Nothing_is_quietly_dropped_from_a_fixture_wave(string id)
    {
        // Every unit either dies or leaks. A vanished unit would make predicted damage look better
        // than the wave - in the direction that erodes trust rather than the one that gets noticed.
        FinalForecast forecast = Locked(Case(id)).Forecast();
        IReadOnlyList<TimelineEvent> events = forecast.Timeline.Events;

        HashSet<int> spawned = [.. events.OfType<SpawnEvent>().Select(e => e.SpawnIndex)];
        HashSet<int> died = [.. events.OfType<DeathEvent>().Select(e => e.SpawnIndex)];
        HashSet<int> leaked = [.. events.OfType<LeakEvent>().Select(e => e.SpawnIndex)];

        Assert.NotEmpty(spawned);
        Assert.Empty(died.Intersect(leaked));
        Assert.Equal(spawned.Count, died.Count + leaked.Count);
    }

    [Theory]
    [MemberData(nameof(EveryCase))]
    [Trait(Regression.Trait, Regression.Category)]
    public void The_visible_threat_matches_a_run_against_the_revealed_force_alone(string id)
    {
        BatteryFixture fixture = Case(id);
        WaveSession session = fixture.Open(Tuning);

        // Only legal at the draw decision - which is itself the contract being checked. A case
        // offered elsewhere has no Visible Threat to verify.
        if (session.Phase != WavePhase.DrawDecision)
        {
            return;
        }

        VisibleThreat reported = session.VisibleThreatNow();

        VisibleThreat direct = Resolver.ResolveRevealed(
            Tuning,
            Tuning.Encounter(fixture.EncounterId),
            session.Board(),
            ArmyBuilder.Revealed(Tuning, Tuning.Encounter(fixture.EncounterId), session.Vanguard, session.Entry));

        Assert.Equal(direct.Lanes.Count, reported.Lanes.Count);

        foreach (LaneOutcome lane in reported.Lanes)
        {
            LaneOutcome expected = direct.Lanes[lane.LaneIndex];

            Assert.Equal(expected.PredictedDamage, lane.PredictedDamage);
            Assert.Equal(expected.EmptyLaneDamage, lane.EmptyLaneDamage);
        }
    }

    [Theory]
    [MemberData(nameof(EveryCase))]
    [Trait(Regression.Trait, Regression.Category)]
    public void A_fixture_resolves_identically_every_time(string id)
    {
        // Determinism over the fixtures specifically: the battery is presented twice per case, and a
        // resolver that drifted between runs would make the two presentations differ for a reason
        // that has nothing to do with the mirror.
        FinalForecast first = Locked(Case(id)).Forecast();
        FinalForecast second = Locked(Case(id)).Forecast();

        Assert.Equal(
            first.Lanes.Select(l => l.PredictedDamage),
            second.Lanes.Select(l => l.PredictedDamage));

        Assert.Equal(first.Timeline.Events.Count, second.Timeline.Events.Count);
    }

    /// <summary>
    /// Drives a case from its offered state to a locked wave.
    /// </summary>
    /// <remarks>
    /// Whatever a case offers, the contract only exists once the Dealer has resolved. Placements are
    /// made forward-first - a fixed rule, since this procedure checks the forecast against itself
    /// rather than comparing two boards.
    /// </remarks>
    private static WaveSession Locked(BatteryFixture fixture)
    {
        WaveSession session = fixture.Open(Tuning);

        while (session.Phase == WavePhase.AwaitingPlacement)
        {
            session = session.Place(Family.Club, FirstFreeSocket(session));
        }

        if (session.Phase == WavePhase.DrawDecision)
        {
            session = session.Stand();
        }

        return session;
    }

    private static SocketRef FirstFreeSocket(WaveSession session)
    {
        HashSet<SocketRef> taken = [.. session.Board().Towers.Select(t => t.Socket)];

        foreach (SocketRef socket in Measurement.Sweeps.AllSockets(Tuning))
        {
            if (!taken.Contains(socket))
            {
                return socket;
            }
        }

        // At capacity a placement replaces something; the rear socket is a fixed, arbitrary choice.
        return SocketRef.InLane(0, Tuning.Geometry.SocketPositions.Count - 1);
    }
}
