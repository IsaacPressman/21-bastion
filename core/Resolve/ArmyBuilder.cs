using Bastion.Core.Cards;
using Bastion.Core.Config;
using Bastion.Core.Dealer;

namespace Bastion.Core.Resolve;

/// <summary>
/// Assembles the wave: the encounter's base composition plus whatever the Dealer has deployed.
/// </summary>
/// <remarks>
/// <para>
/// <b>The spawn schedule is NOT SPECIFIED BY THE DESIGN.</b> Per-type spacing exists on the enemy
/// rows; nothing says when groups start or how mixed types interleave. Groups run sequentially
/// within a lane - base wave, then Vanguard, then Dealer draws in draw order - each at its own
/// spacing, separated by <c>waves.groupGapSeconds</c>. See data/tuning.json's resolver comment
/// block.
/// </para>
/// <para>
/// Two entry points, not one with a flag. This is where the forecast split originates: the revealed
/// force and the complete army are different questions, and they produce different types all the
/// way through to <see cref="VisibleThreat"/> and <see cref="FinalForecast"/>.
/// </para>
/// </remarks>
public static class ArmyBuilder
{
    /// <summary>
    /// The force on the field during the draw: base wave plus the Dealer's upcard.
    /// </summary>
    /// <remarks>
    /// The hidden card is deliberately absent. It has been dealt and the player knows it is coming,
    /// but it cannot be identified - so a run against this force is exact about what is standing
    /// there now and is <b>not</b> a prediction of the wave.
    /// </remarks>
    public static RevealedArmy Revealed(
        TuningData tuning,
        EncounterTuning encounter,
        Card vanguard,
        double entry)
    {
        ArgumentNullException.ThrowIfNull(tuning);
        ArgumentNullException.ThrowIfNull(encounter);

        IReadOnlyList<DealerUnits> deployed = DealerDeployment.Deploy(tuning, encounter, [vanguard]);

        return new RevealedArmy { Spawns = Schedule(tuning, encounter, deployed, entry) };
    }

    /// <summary>
    /// The whole wave, after the Dealer has revealed and drawn to 17.
    /// </summary>
    /// <param name="dealerHand">The Dealer's full hand, upcard first.</param>
    /// <remarks>
    /// Reached on stand <b>or bust</b>: the Dealer resolves in full either way, so a bust changes
    /// the player's formation and nothing about the army.
    /// </remarks>
    public static CompleteArmy Complete(
        TuningData tuning,
        EncounterTuning encounter,
        IReadOnlyList<Card> dealerHand,
        double entry)
    {
        ArgumentNullException.ThrowIfNull(tuning);
        ArgumentNullException.ThrowIfNull(encounter);
        ArgumentNullException.ThrowIfNull(dealerHand);

        if (dealerHand.Count == 0)
        {
            throw new ArgumentException("The Dealer always has at least an upcard.", nameof(dealerHand));
        }

        IReadOnlyList<DealerUnits> deployed = DealerDeployment.Deploy(tuning, encounter, dealerHand);

        return new CompleteArmy { Spawns = Schedule(tuning, encounter, deployed, entry) };
    }

    /// <summary>
    /// Lays every group out on the clock and stamps the spawn indices.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Spawn indices are global and monotonic, assigned lane by lane and group by group. Lanes
    /// resolve independently, so a cross-lane tie never actually arises in combat - but the merged
    /// timeline needs a total order, and defining one here is cheaper than defining it twice later.
    /// </para>
    /// <para>
    /// Everything enters at the current entry point, including the Vanguard. "Already standing on
    /// the field" is presentation - the upcard is visible on the board during the draw - not a
    /// separate movement rule, and giving it a head start would be a second march system.
    /// </para>
    /// </remarks>
    private static List<EnemySpawn> Schedule(
        TuningData tuning,
        EncounterTuning encounter,
        IReadOnlyList<DealerUnits> deployed,
        double entry)
    {
        List<EnemySpawn> spawns = [];
        int spawnIndex = 0;

        for (int lane = 0; lane < tuning.Geometry.Lanes; lane++)
        {
            double cursor = 0.0;

            foreach (SpawnGroupTuning group in encounter.BaseWave[lane])
            {
                cursor = AddGroup(tuning, spawns, ref spawnIndex, group.EnemyId, group.Count,
                                  lane, cursor, entry, SpawnSource.BaseWave);
            }

            foreach (DealerUnits units in deployed.Where(u => u.LaneIndex == lane))
            {
                cursor = AddGroup(tuning, spawns, ref spawnIndex, units.EnemyId, units.Count,
                                  lane, cursor, entry,
                                  units.IsVanguard ? SpawnSource.Vanguard : SpawnSource.DealerDraw);
            }
        }

        return spawns;
    }

    /// <summary>
    /// Spawns one group at its type's spacing and returns the next group's start time.
    /// </summary>
    private static double AddGroup(
        TuningData tuning,
        List<EnemySpawn> spawns,
        ref int spawnIndex,
        string enemyId,
        int count,
        int lane,
        double groupStart,
        double entry,
        SpawnSource source)
    {
        EnemyTuning enemy = tuning.Enemy(enemyId);

        // Single-unit types carry no spacing. The loader rejects a group of more than one of them,
        // so the zero fallback is never actually used to stack units on one another.
        double spacing = enemy.SpacingSeconds ?? 0.0;

        for (int i = 0; i < count; i++)
        {
            spawns.Add(new EnemySpawn
            {
                SpawnIndex = spawnIndex++,
                EnemyId = enemyId,
                LaneIndex = lane,
                SpawnTime = groupStart + (i * spacing),
                StartPosition = entry,
                Source = source,
            });
        }

        return groupStart + ((count - 1) * spacing) + tuning.Waves.GroupGapSeconds;
    }
}
