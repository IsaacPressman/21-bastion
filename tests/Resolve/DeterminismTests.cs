using Bastion.Core.Board;
using Bastion.Core.Cards;
using Bastion.Core.Config;
using Bastion.Core.Resolve;

namespace Bastion.Core.Tests.Resolve;

/// <summary>
/// Same input, same output, every run.
/// </summary>
/// <remarks>
/// docs/ARCHITECTURE.md § 3: no wall-clock time, no frame-rate coupling, no unseeded randomness,
/// ties by spawn order everywhere. This is also what makes the regression suites possible at all -
/// enumerating every legal hand and simulating ten thousand of them is only meaningful if the
/// answers do not move.
/// </remarks>
public sealed class DeterminismTests
{
    private static BoardState Crowded() => Fixture.Board(
        Fixture.Tower(Rank.Six, Family.Club, Fixture.Socket(0, 0), formation: 1.30, runBonus: 0.15),
        Fixture.Tower(Rank.Seven, Family.Spade, Fixture.Socket(0, 1), formation: 1.30, runBonus: 0.15),
        Fixture.Tower(Rank.King, Family.Club, Fixture.Socket(0, 2), formation: 1.30),
        Fixture.Tower(Rank.Four, Family.Spade, Fixture.Socket(1, 0), formation: 1.30,
            order: new StandingOrder { HoldPastPosition = 6.0, Focus = FocusMode.PreferArmored }),
        Fixture.Tower(Rank.Nine, Family.Club, SocketRef.Junction, formation: 1.30,
            order: new StandingOrder { TriggerOnGroup = true }));

    private static readonly Rank[] DealerHand = [Rank.Ten, Rank.Six, Rank.Seven];

    [Fact]
    public void Two_runs_of_the_same_input_produce_identical_timelines()
    {
        FinalForecast first = Fixture.ResolveComplete(
            Fixture.Example, Crowded(), Fixture.Complete(Fixture.Example, DealerHand));
        FinalForecast second = Fixture.ResolveComplete(
            Fixture.Example, Crowded(), Fixture.Complete(Fixture.Example, DealerHand));

        Assert.Equal(first.Timeline.Events.Count, second.Timeline.Events.Count);
        Assert.Equal(first.Timeline.Events, second.Timeline.Events);
        Assert.Equal(first.Timeline.DurationSeconds, second.Timeline.DurationSeconds, precision: 9);
    }

    [Fact]
    public void Two_runs_of_the_same_input_produce_identical_outcomes()
    {
        FinalForecast first = Fixture.ResolveComplete(
            Fixture.Example, Crowded(), Fixture.Complete(Fixture.Example, DealerHand));
        FinalForecast second = Fixture.ResolveComplete(
            Fixture.Example, Crowded(), Fixture.Complete(Fixture.Example, DealerHand));

        Assert.Equal(first.Lanes, second.Lanes);
    }

    [Fact]
    public void The_order_towers_are_handed_to_the_board_does_not_change_the_result()
    {
        // Firing order is a property of the socket, not of how the list happened to be built. If
        // this ever failed, a board rebuilt from saved state could resolve differently from the
        // one the player looked at.
        TowerState[] towers =
        [
            Fixture.Tower(Rank.Six, Family.Club, Fixture.Socket(0, 0), formation: 1.30),
            Fixture.Tower(Rank.Seven, Family.Spade, Fixture.Socket(0, 1), formation: 1.30),
            Fixture.Tower(Rank.King, Family.Club, Fixture.Socket(0, 2), formation: 1.30),
        ];

        FinalForecast forward = Resolve(BoardState.Create(Fixture.Tuning, towers, 0.0));
        FinalForecast reversed = Resolve(BoardState.Create(Fixture.Tuning, Enumerable.Reverse(towers), 0.0));

        Assert.Equal(forward.Timeline.Events, reversed.Timeline.Events);
        Assert.Equal(forward.Lanes, reversed.Lanes);
    }

    [Fact]
    public void Every_march_arm_resolves_and_none_of_them_hangs()
    {
        // All three arms ship in one build; the resolver has to survive every entry position they
        // produce, including the clamp at 9.0 where three units of engagement are all that is left.
        foreach (string arm in Fixture.Tuning.MarchPresets.Keys.Order(StringComparer.Ordinal))
        {
            TuningData armed = Fixture.Tuning with
            {
                March = Fixture.Tuning.March with { ActivePreset = arm },
            };

            for (int cards = 2; cards <= 8; cards++)
            {
                double entry = Bastion.Core.March.MarchClock.EntryAfter(armed, cards, reachedExactly21: false);

                BoardState board = BoardState.Create(armed,
                    [Fixture.Tower(Rank.King, Family.Club, Fixture.Socket(0, 2), formation: 1.30)], entry);

                FinalForecast forecast = Resolver.ResolveComplete(
                    armed, Fixture.Example, board,
                    ArmyBuilder.Complete(armed, Fixture.Example, [.. DealerHand.Select(r => new Card(r))], entry));

                Assert.Equal(2, forecast.Lanes.Count);
                Assert.NotEmpty(forecast.Timeline.Events);
            }
        }
    }

    [Fact]
    public void A_board_and_an_army_that_disagree_about_the_entry_point_are_rejected()
    {
        // Both are downstream of the same March Clock reading. A mismatch resolves cleanly and
        // answers the wrong question, which is the one failure a forecast contract cannot survive.
        BoardState board = Fixture.BoardAt(4.0,
            Fixture.Tower(Rank.King, Family.Club, Fixture.Socket(0, 2), formation: 1.0));

        CompleteArmy army = Fixture.Complete(Fixture.Example, DealerHand, entry: 0.0);

        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(
            () => Fixture.ResolveComplete(Fixture.Example, board, army));

        Assert.Contains("entry point", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Resolving_does_not_disturb_the_board_it_was_given()
    {
        // The same board is resolved several times per forecast - once live and once per lane
        // counterfactual - so a run that left anything behind would make the second answer differ
        // from the first.
        BoardState board = Crowded();

        Fixture.ResolveComplete(Fixture.Example, board, Fixture.Complete(Fixture.Example, DealerHand));

        Assert.Equal(Crowded().Towers, board.Towers);
        Assert.Equal(Crowded().Entry, board.Entry, precision: 9);
    }

    private static FinalForecast Resolve(BoardState board) =>
        Fixture.ResolveComplete(Fixture.Example, board, Fixture.Complete(Fixture.Example, DealerHand));
}
