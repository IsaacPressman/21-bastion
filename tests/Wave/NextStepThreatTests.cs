using Bastion.Core.Board;
using Bastion.Core.Cards;
using Bastion.Core.Config;
using Bastion.Core.Resolve;
using Bastion.Core.Wave;

namespace Bastion.Core.Tests.Wave;

/// <summary>
/// The cost of one more card, priced as attacks rather than as an entry position.
/// </summary>
/// <remarks>
/// <para>
/// docs/design/14-encounter-timeline.md: the intended read is <b>not</b> "entry moves from 1.5 to
/// 4.0", it is <i>"if you draw again, this cannon loses two shots before the Siege Engine crosses
/// socket 9."</i> That sentence needs the same revealed force resolved one march step later, which
/// is what this reading is.
/// </para>
/// <para>
/// It is not a next-draw preview. It says nothing about the card - the step is paid before the card
/// is revealed and its cost is certain, which is precisely the asymmetry the encounter thesis rests
/// on (docs/design/09-information-and-ui.md § Next-draw preview is cut from the baseline).
/// </para>
/// </remarks>
public sealed class NextStepThreatTests
{
    private static readonly TuningData Tuning = TuningLoader.LoadFromRepositoryRoot();
    private static EncounterTuning Encounter => Tuning.Encounter("example_wave");

    private static SocketRef Bastion(int socket) => SocketRef.InLane(0, socket);

    /// <summary>A wave over a scripted shoe, padded so hitting and standing never run it dry.</summary>
    private static WaveSession Begin(params Rank[] shoe) =>
        WaveSession.Begin(Tuning, Encounter, Shoe.FromOrder([.. shoe, .. Enumerable.Repeat(Rank.Ace, 12)]));

    /// <summary>A wave at the draw decision with two towers forward in the bastion lane.</summary>
    private static WaveSession AtDecision() =>
        Begin(Rank.Ten, Rank.Six, Rank.Nine, Rank.Eight)
            .Place(Family.Club, Bastion(0))
            .Place(Family.Club, Bastion(1));

    [Fact]
    public void A_march_step_redistributes_attacks_and_does_not_merely_subtract_them()
    {
        // MEASURED, and it constrains how the timeline may label the step.
        //
        // A march step always takes engagement *window* away - that is closed-form geometry, and
        // SessionSnapshotTests pins it. Per-tower *attacks* are a different quantity and they are
        // NOT monotonic: on the worked example the forward tower drops 12 shots to 10 while the rear
        // tower rises 2 to 4, because the forward tower now kills less and leaves the rear one more
        // to shoot at.
        //
        // So the encounter timeline must draw the step as a change, never as a uniform loss. "L0.S0
        // loses 2 shots, L0.S1 gains 2" is the true sentence and the more useful one - it shows work
        // shifting backwards down the lane, which is the actual shape of the cost. A view that
        // labelled every ghost band "attacks lost" would be stating something false.
        WaveSession session = AtDecision();

        VisibleThreat now = session.VisibleThreatNow();
        VisibleThreat next = Assert.IsType<VisibleThreat>(session.NextStepThreat());

        SocketRef forward = Bastion(0);
        SocketRef behind = Bastion(1);

        int forwardBefore = Shots(now, 0, forward);
        int forwardAfter = Shots(next, 0, forward);
        int behindBefore = Shots(now, 0, behind);
        int behindAfter = Shots(next, 0, behind);

        Assert.True(forwardAfter < forwardBefore, "The forward tower pays for the step.");
        Assert.True(behindAfter > behindBefore, "The rear tower picks up what the forward one dropped.");
    }

    private static int Shots(VisibleThreat threat, int lane, SocketRef socket) =>
        threat.Lanes[lane].TowerActivity.Single(a => a.Socket == socket).Shots;

    [Fact]
    public void The_reading_is_the_same_board_and_the_same_force_one_step_deeper()
    {
        // The board and the army must agree about where the march has reached - the resolver rejects
        // a pair that does not, because a mismatch resolves cleanly and answers the wrong question.
        // Reaching a reading at all is the assertion; it throws otherwise.
        WaveSession session = AtDecision();

        VisibleThreat next = Assert.IsType<VisibleThreat>(session.NextStepThreat());

        double expectedEntry = Math.Min(
            session.Entry + session.NextStepCost(), Tuning.March.EntryClampMax);

        Assert.All(
            next.Schedule.Events.OfType<SpawnEvent>(),
            spawn => Assert.Equal(expectedEntry, spawn.Position, 9));
    }

    [Fact]
    public void A_charged_step_always_lands_somewhere_a_player_can_see()
    {
        // The whole surface is pointless if the step reads as free. What must change is deliberately
        // left open - some tower's attack count, or a lane's damage - because which one moves is the
        // interesting part and pinning a particular one would be pinning this fixture, not the rule.
        WaveSession session = AtDecision();

        Assert.True(session.NextStepCost() > 0.0);

        VisibleThreat now = session.VisibleThreatNow();
        VisibleThreat next = session.NextStepThreat()!;

        bool somethingMoved =
            now.Lanes.Any(lane => lane.PredictedDamage != next.Lanes[lane.LaneIndex].PredictedDamage)
            || now.Lanes.Any(lane => lane.TowerActivity.Any(a =>
                a.Shots != next.Lanes[lane.LaneIndex].TowerActivity.Single(b => b.Socket == a.Socket).Shots));

        Assert.True(somethingMoved, "A charged march step must have a visible consequence somewhere.");
    }

    [Fact]
    public void Past_the_clamp_there_is_no_step_and_therefore_no_consequence_to_draw()
    {
        // Entry clamps at the rear socket so enemies never spawn past the player's last defense, and
        // past it a further card is free on the clock (docs/ROADMAP.md Open Decision 5). Null rather
        // than an unchanged reading, so a view cannot draw a cost of nothing as though it were one.
        WaveSession session = AtDecision();

        while (session.Phase == WavePhase.DrawDecision
               && session.Entry < Tuning.March.EntryClampMax
               && session.Hand.CardCount < 8)
        {
            session = session.Hit();

            if (session.Phase == WavePhase.AwaitingPlacement)
            {
                session = session.Place(Family.Club, Bastion(2));
            }
        }

        if (session.Phase == WavePhase.DrawDecision && session.NextStepCost() <= 0.0)
        {
            Assert.Null(session.NextStepThreat());
        }
    }

    [Fact]
    public void The_step_cost_is_unreadable_once_the_dealer_has_resolved()
    {
        // It is a pre-Dealer reading. Afterwards there is no next card to price, and the Final
        // Forecast is the only thing that may be read.
        WaveSession stood = AtDecision().Stand();

        Assert.Throws<InvalidOperationException>(() => stood.NextStepThreat());
    }
}
