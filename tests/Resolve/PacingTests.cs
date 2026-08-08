using Bastion.Core.Board;
using Bastion.Core.Cards;
using Bastion.Core.Resolve;

namespace Bastion.Core.Tests.Resolve;

/// <summary>
/// How long a wave takes to resolve.
/// </summary>
/// <remarks>
/// <c>combat.waveResolutionSecondsMin/Max</c> is a design target of 12–20 s at normal speed, and it is the
/// one number the invented spawn schedule can most easily contradict: group gaps, pack sizes, and enemy
/// speeds all feed it, and none of the three came from the design.
/// </remarks>
public sealed class PacingTests
{
    private static double DurationOf(params TowerState[] towers)
    {
        BoardState board = Fixture.Board(towers);

        return Fixture.ResolveComplete(
                Fixture.Example, board,
                Fixture.Complete(Fixture.Example, [Rank.Ten, Rank.Six, Rank.Seven]))
            .Timeline.DurationSeconds;
    }

    [Fact]
    public void The_worked_example_resolves_inside_the_tuned_window()
    {
        double defended = DurationOf(
            Fixture.Tower(Rank.Six, Family.Club, Fixture.Socket(0, 1), formation: 1.30),
            Fixture.Tower(Rank.Eight, Family.Club, Fixture.Socket(0, 2), formation: 1.30),
            Fixture.Tower(Rank.Four, Family.Spade, Fixture.Socket(1, 0), formation: 1.30));

        Assert.InRange(
            defended,
            Fixture.Tuning.Combat.WaveResolutionSecondsMin,
            Fixture.Tuning.Combat.WaveResolutionSecondsMax);
    }

    [Fact]
    public void A_full_armored_pack_cannot_fit_the_window_under_any_schedule()
    {
        // An undefended worked example runs 25.4 s, over the 20 s target - and the spawn schedule
        // is not the cause. An armored soldier crosses the 12-unit path in 18.46 s on its own, and
        // a pack of three at 1.50 s spacing cannot finish before 21.46 s even if it is the only
        // group in the lane and starts at t=0.
        //
        // So combat.waveResolutionSecondsMax and the armored soldier's speed disagree, and no
        // scheduling choice reconciles them. Both are design numbers; this is logged as a
        // discrepancy in reference/tuning-constants.md rather than resolved by quietly retuning
        // one. If this test starts failing, somebody fixed it - update the discrepancy log.
        Bastion.Core.Config.EnemyTuning soldier = Fixture.Tuning.Enemy("armored_soldier");

        double floor = ((soldier.Count - 1) * soldier.SpacingSeconds!.Value)
                       + (Fixture.Tuning.Geometry.PathLength / soldier.Speed);

        Assert.True(floor > Fixture.Tuning.Combat.WaveResolutionSecondsMax,
            $"An armored pack now clears in {floor:F2}s, inside the {Fixture.Tuning.Combat.WaveResolutionSecondsMax}s " +
            "target. The pacing discrepancy is resolved - remove it from the discrepancy log.");
    }

    [Fact]
    public void Defenses_shorten_a_wave_rather_than_lengthening_it()
    {
        // Killing things early is the only thing that brings the worked example inside the window,
        // which is worth knowing: the pacing target implicitly assumes a defended board.
        double undefended = DurationOf();

        double defended = DurationOf(
            Fixture.Tower(Rank.Six, Family.Club, Fixture.Socket(0, 1), formation: 1.30),
            Fixture.Tower(Rank.Eight, Family.Club, Fixture.Socket(0, 2), formation: 1.30),
            Fixture.Tower(Rank.Four, Family.Spade, Fixture.Socket(1, 0), formation: 1.30));

        Assert.True(defended < undefended, $"Defended {defended:F1}s, undefended {undefended:F1}s.");
    }
}
