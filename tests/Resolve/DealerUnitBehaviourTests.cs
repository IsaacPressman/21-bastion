using Bastion.Core.Board;
using Bastion.Core.Resolve;

namespace Bastion.Core.Tests.Resolve;

/// <summary>
/// The two Dealer face-card behaviours Milestone 3 models: the Standard bearer's aura and the
/// Herald's Ace split. Jack mobility and the Skirmisher's lane change stay deferred.
/// </summary>
public sealed class DealerUnitBehaviourTests
{
    private static EnemySpawn Spawn(int index, string enemyId) => new()
    {
        SpawnIndex = index,
        EnemyId = enemyId,
        LaneIndex = 0,
        SpawnTime = 0.0,
        StartPosition = 0.0,
        Source = SpawnSource.DealerDraw,
    };

    private static double CrossingSeconds(params EnemySpawn[] spawns)
    {
        FinalForecast forecast = Resolver.ResolveComplete(
            Fixture.Tuning,
            Fixture.Example,
            BoardState.Empty(Fixture.Tuning),
            new CompleteArmy { Spawns = spawns });

        return forecast.Timeline.DurationSeconds;
    }

    [Fact]
    public void A_standard_bearers_aura_hastens_a_nearby_same_lane_unit()
    {
        // A lone Standard bearer crosses the 12-unit path at its own 0.80 speed.
        double solo = CrossingSeconds(Spawn(0, "standard_bearer"));

        // A second one at the same spot buffs the first (and vice versa): both move at ×1.5 the whole
        // way, so the pair crosses markedly sooner. The aura reads only same-lane neighbours, so it
        // never couples the two lanes.
        double pair = CrossingSeconds(Spawn(0, "standard_bearer"), Spawn(1, "standard_bearer"));

        Assert.True(pair < solo, $"the aura should hasten the pair ({pair}s) below the solo crossing ({solo}s).");
        Assert.True(solo - pair > 3.0, $"the aura's effect should be substantial (solo {solo}s, pair {pair}s).");
    }

    [Fact]
    public void The_aura_does_not_buff_the_carrier_itself()
    {
        // A single bearer gets no aura - it does not buff itself - so it crosses at its base speed.
        double solo = CrossingSeconds(Spawn(0, "standard_bearer"));
        double baseline = Fixture.Tuning.Geometry.PathLength / Fixture.Tuning.Enemy("standard_bearer").Speed;

        Assert.Equal(baseline, solo, precision: 1);
    }
}
