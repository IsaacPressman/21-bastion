using Bastion.Core.Board;
using Bastion.Core.Cards;
using Bastion.Core.Config;
using Bastion.Core.Resolve;
using Bastion.Core.Wave;

namespace Bastion.Core.Tests.Wave;

/// <summary>
/// A candidate preview is the resolver, and it never becomes a score.
/// </summary>
/// <remarks>
/// <para>
/// docs/design/14-encounter-timeline.md § Candidate placements show causal deltas: <i>"Before
/// drawing, show the requirement. After drawing, show the consequences of candidate actions. Do not
/// show the answer."</i>
/// </para>
/// <para>
/// Two things have to hold. The preview must agree with what actually happens - a preview that
/// drifts from the resolver is worse than no preview - and it must carry no single comparable
/// number, because that is what turns hovering every socket into brute-force optimization.
/// </para>
/// </remarks>
public sealed class CandidateDeltaTests
{
    private static readonly TuningData Tuning = TuningLoader.LoadFromRepositoryRoot();
    private static EncounterTuning Encounter => Tuning.Encounter("example_wave");

    private static SocketRef Bastion(int socket) => SocketRef.InLane(0, socket);

    /// <summary>A wave over a scripted shoe, padded so hitting and standing never run it dry.</summary>
    private static WaveSession Begin(params Rank[] shoe) =>
        WaveSession.Begin(Tuning, Encounter, Shoe.FromOrder([.. shoe, .. Enumerable.Repeat(Rank.Two, 10)]));

    /// <summary>
    /// An opened wave holding two cards to place, so a candidate is legal.
    /// </summary>
    /// <remarks>
    /// Draw order is Vanguard, hole card, then the player's opening two - so the first two ranks
    /// here belong to the Dealer and the player is holding the <b>Nine</b> and the Eight.
    /// </remarks>
    private static WaveSession Opened() => Begin(Rank.Ten, Rank.Six, Rank.Nine, Rank.Eight);

    /// <summary>
    /// A wave awaiting placement of a <i>third</i> card, where the next one is no longer free.
    /// </summary>
    /// <remarks>
    /// The opening two are free on the clock, so a candidate placed from them has no next-step cost
    /// to report. Anything asserting about the March step has to be past that.
    /// </remarks>
    private static WaveSession AwaitingThirdCard() => Opened()
        .Place(Family.Club, Bastion(0))
        .Place(Family.Club, Bastion(1))
        .Hit();

    [Fact]
    public void The_preview_is_exactly_what_placing_the_card_produces()
    {
        // The load-bearing test of the whole surface. PreviewPlacement runs the real transition and
        // re-reads the resolver, so this can only fail if someone replaces it with an estimator -
        // which is the one change that must never be made quietly.
        WaveSession session = Opened();
        CandidateDelta delta = session.PreviewPlacement(Family.Club, Bastion(0))!;

        VisibleThreat actual = session.Place(Family.Club, Bastion(0)).VisibleThreatNow();

        Assert.Equal(
            actual.Lanes.Select(l => (l.LaneIndex, l.PredictedDamage, l.LeakedUnits.Count)),
            delta.Lanes.Select(l => (l.LaneIndex, l.PredictedDamageAfter, l.LeakCountAfter)));
    }

    [Fact]
    public void The_before_half_is_the_reading_the_player_is_currently_looking_at()
    {
        WaveSession session = Opened();
        CandidateDelta delta = session.PreviewPlacement(Family.Spade, Bastion(1))!;

        VisibleThreat current = session.VisibleThreatNow();

        Assert.Equal(
            current.Lanes.Select(l => (l.LaneIndex, l.PredictedDamage, l.LeakedUnits.Count)),
            delta.Lanes.Select(l => (l.LaneIndex, l.PredictedDamageBefore, l.LeakCountBefore)));
    }

    [Fact]
    public void A_candidate_that_stops_a_unit_names_it()
    {
        // "Banner: survives -> killed" is the sentence a player repeats back afterwards. A delta that
        // reported only a smaller number would not survive being asked "what did that card buy you?"
        WaveSession session = Begin(Rank.King, Rank.King, Rank.Nine, Rank.Eight);
        CandidateDelta delta = session.PreviewPlacement(Family.Club, Bastion(0))!;

        Assert.NotEmpty(delta.FateChanges);
        Assert.All(delta.FateChanges, change =>
        {
            Assert.NotEqual(change.LeaksBefore, change.LeaksAfter);
            Assert.False(string.IsNullOrWhiteSpace(change.DisplayName));
        });
    }

    [Fact]
    public void The_candidate_tower_appears_in_the_shot_deltas_with_a_before_of_zero()
    {
        WaveSession session = Opened();
        CandidateDelta delta = session.PreviewPlacement(Family.Club, Bastion(0))!;

        TowerShotDelta placed = delta.TowerShots.Single(t => t.Socket == Bastion(0));

        Assert.Equal(0, placed.Before);
        Assert.True(placed.After > 0);
    }

    [Fact]
    public void Shot_deltas_carry_what_the_next_march_step_would_cost_this_arrangement()
    {
        // "Club 8 attacks: 3 -> 2 after next March step". The step is priced against the board the
        // candidate would create, not against the board as it stands - which is the only version of
        // the number that answers the question the player is actually asking.
        CandidateDelta delta = AwaitingThirdCard().PreviewPlacement(Family.Club, Bastion(2))!;

        Assert.All(delta.TowerShots, shots => Assert.NotNull(shots.AfterNextStep));
    }

    [Fact]
    public void The_opening_cards_report_no_step_cost_because_the_march_has_not_begun()
    {
        // Not an omission: the opening two are free on the clock, so there is no consequence to
        // draw. Null rather than a number, so a view cannot render a cost of nothing as though it
        // were one.
        CandidateDelta delta = Opened().PreviewPlacement(Family.Club, Bastion(0))!;

        Assert.All(delta.TowerShots, shots => Assert.Null(shots.AfterNextStep));
    }

    [Fact]
    public void A_candidate_onto_an_occupied_socket_names_what_it_displaces_and_what_that_costs()
    {
        // "Replaces 2" and "replaces 9" are not the same move. What a card displaces is one of the
        // three clauses of the design's narrowed claim, so it is worth a number.
        CandidateDelta delta = AwaitingThirdCard().PreviewPlacement(Family.Spade, Bastion(0))!;

        Assert.NotNull(delta.Displaces);
        Assert.Equal(Rank.Nine, delta.Displaces!.Card.Rank);
        Assert.True(delta.Displaces.ShotDamage > 0.0);
    }

    [Fact]
    public void An_anchored_socket_previews_as_nothing_rather_than_throwing()
    {
        // The pointer resting on a socket is not an attempt to use it, so hovering a King must not
        // throw. Place still refuses if one is actually tried, and that refusal is recorded as a
        // wanted move rather than swallowed.
        WaveSession session = Begin(Rank.Two, Rank.Two, Rank.King, Rank.Four)
            .Place(Family.Club, Bastion(0));

        Assert.True(session.IsAnchored(Bastion(0)));
        Assert.Null(session.PreviewPlacement(Family.Club, Bastion(0)));
    }

    [Fact]
    public void There_is_no_candidate_to_preview_outside_placement()
    {
        WaveSession session = Opened()
            .Place(Family.Club, Bastion(0))
            .Place(Family.Club, Bastion(1));

        Assert.Equal(WavePhase.DrawDecision, session.Phase);
        Assert.Null(session.PreviewPlacement(Family.Club, Bastion(2)));
    }

    [Fact]
    public void The_delta_carries_no_sortable_score_anywhere_on_it()
    {
        // Hard Invariant 2, arriving through a new door: a combined verdict is no less a verdict for
        // being computed per candidate. One comparable number lets the player brute-force every
        // socket until the smallest one appears, without ever understanding why.
        //
        // The property list is pinned rather than pattern-matched, so a convenience total cannot
        // arrive quietly - adding one fails here and forces a deliberate decision.
        string[] expected =
            ["Displaces", "Family", "FateChanges", "Lanes", "Rank", "Runs", "Socket", "TowerShots"];

        Assert.Equal(
            expected.Order(StringComparer.Ordinal),
            typeof(CandidateDelta).GetProperties().Select(p => p.Name).Order(StringComparer.Ordinal));
    }

    [Fact]
    public void Every_scalar_on_the_delta_belongs_to_one_lane_one_unit_or_one_tower()
    {
        // The same rule stated as a shape: nothing numeric hangs off the delta itself. Multiple
        // stakes are the guardrail - a bastion lane and a vault lane cannot be traded off by
        // subtracting one number from another (docs/design/14-encounter-timeline.md).
        Type[] permitted =
        [
            typeof(Rank), typeof(Family), typeof(SocketRef), typeof(DisplacedTower), typeof(RunDelta),
            typeof(IReadOnlyList<LaneDelta>), typeof(IReadOnlyList<UnitFateChange>),
            typeof(IReadOnlyList<TowerShotDelta>),
        ];

        Assert.All(
            typeof(CandidateDelta).GetProperties(),
            property => Assert.Contains(property.PropertyType, permitted));
    }
}
