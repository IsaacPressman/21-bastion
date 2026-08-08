using Bastion.Core.Cards;
using Bastion.Core.Config;
using Bastion.Core.Dealer;
using Bastion.Core.Resolve;

namespace Bastion.Core.Tests.Resolve;

/// <summary>
/// The wave the resolver is handed: who enters, in which lane, and when.
/// </summary>
public sealed class ArmyBuilderTests
{
    [Fact]
    public void A_dealer_card_deploys_its_whole_pack()
    {
        // Three is a swarm pack of eight; a King is one siege engine. NOT SPECIFIED BY THE DESIGN,
        // but it is what "Swarm pack - many, fragile" against a singular "Siege engine" describes.
        IReadOnlyList<DealerUnits> swarm =
            DealerDeployment.Deploy(Fixture.Tuning, Fixture.Example, [new Card(Rank.Three)]);
        IReadOnlyList<DealerUnits> siege =
            DealerDeployment.Deploy(Fixture.Tuning, Fixture.Example, [new Card(Rank.King)]);

        Assert.Equal("swarm_unit", swarm[0].EnemyId);
        Assert.Equal(8, swarm[0].Count);
        Assert.Equal("siege_engine", siege[0].EnemyId);
        Assert.Equal(1, siege[0].Count);
    }

    [Theory]
    [InlineData(Rank.Two, "swarm_unit")]
    [InlineData(Rank.Four, "swarm_unit")]
    [InlineData(Rank.Five, "fast_raider")]
    [InlineData(Rank.Seven, "fast_raider")]
    [InlineData(Rank.Eight, "armored_soldier")]
    [InlineData(Rank.Ten, "armored_soldier")]
    [InlineData(Rank.Jack, "skirmisher")]
    [InlineData(Rank.Queen, "standard_bearer")]
    [InlineData(Rank.King, "siege_engine")]
    [InlineData(Rank.Ace, "herald")]
    public void Every_rank_maps_to_its_unit(Rank rank, string expected)
    {
        // Keyed on printed rank, not blackjack value: a Jack is a Skirmisher and a King is a siege
        // engine, though both count as ten in the Dealer's hand.
        Assert.Equal(expected, DealerDeployment.UnitFor(Fixture.Tuning, new Card(rank)));
    }

    [Fact]
    public void The_vanguard_takes_the_encounters_lane_and_later_cards_alternate()
    {
        IReadOnlyList<DealerUnits> deployed = DealerDeployment.Deploy(
            Fixture.Tuning, Fixture.Example, [new Card(Rank.Ten), new Card(Rank.Six), new Card(Rank.Seven)]);

        Assert.Equal([0, 1, 0], deployed.Select(d => d.LaneIndex));
        Assert.True(deployed[0].IsVanguard);
        Assert.All(deployed.Skip(1), d => Assert.False(d.IsVanguard));
    }

    [Fact]
    public void Everything_enters_at_the_current_entry_point_including_the_vanguard()
    {
        // "Already standing on the field" is presentation - the upcard is visible on the board
        // during the draw - not a separate movement rule. A head start would be a second march.
        RevealedArmy army = Fixture.Revealed(Fixture.Example, Rank.Ten, entry: 4.0);

        Assert.All(army.Spawns, s => Assert.Equal(4.0, s.StartPosition, precision: 6));
    }

    [Fact]
    public void Groups_run_sequentially_at_their_own_spacing_with_a_gap_between()
    {
        // Lane 0 of the worked example: three armored soldiers at 1.50 s, a 1.0 s gap, then the
        // Vanguard's pack of three at 1.50 s.
        RevealedArmy army = Fixture.Revealed(Fixture.Example, Rank.Ten);

        double[] lane0 = [.. army.Spawns.Where(s => s.LaneIndex == 0).Select(s => s.SpawnTime)];

        Assert.Equal([0.0, 1.5, 3.0, 4.0, 5.5, 7.0], lane0);
    }

    [Fact]
    public void The_base_wave_spawns_before_the_vanguard()
    {
        RevealedArmy army = Fixture.Revealed(Fixture.Example, Rank.Ten);

        IEnumerable<EnemySpawn> lane0 = army.Spawns.Where(s => s.LaneIndex == 0);

        Assert.Equal(
            [SpawnSource.BaseWave, SpawnSource.BaseWave, SpawnSource.BaseWave,
             SpawnSource.Vanguard, SpawnSource.Vanguard, SpawnSource.Vanguard],
            lane0.Select(s => s.Source));
    }

    [Fact]
    public void Spawn_indices_are_global_and_monotonic()
    {
        // The tie-break of record for the whole simulation. Every ordering question resolves
        // through it, so it has to be a total order with no gaps and no repeats.
        CompleteArmy army = Fixture.Complete(Fixture.Example, [Rank.Ten, Rank.Six, Rank.Seven]);

        Assert.Equal(
            Enumerable.Range(0, army.Spawns.Count),
            army.Spawns.Select(s => s.SpawnIndex).Order());
    }

    [Fact]
    public void The_revealed_force_excludes_the_dealers_later_draws()
    {
        // The hidden card has been dealt and the player knows it is coming, but it cannot be
        // identified - so it is not on the field, and the Visible Threat must not model it.
        RevealedArmy revealed = Fixture.Revealed(Fixture.Example, Rank.Ten);
        CompleteArmy complete = Fixture.Complete(Fixture.Example, [Rank.Ten, Rank.Six, Rank.Seven]);

        Assert.DoesNotContain(revealed.Spawns, s => s.Source == SpawnSource.DealerDraw);
        Assert.Contains(complete.Spawns, s => s.Source == SpawnSource.DealerDraw);
        Assert.True(complete.Spawns.Count > revealed.Spawns.Count);
    }

    [Fact]
    public void The_dealer_always_has_at_least_an_upcard()
    {
        Assert.Throws<ArgumentException>(
            () => ArmyBuilder.Complete(Fixture.Tuning, Fixture.Example, [], entry: 0.0));
    }

    [Fact]
    public void Undefended_lanes_of_the_worked_example_leak_what_the_document_says()
    {
        // design/example-wave.md: lane two forecasts 6 damage undefended. Lane one's three armored
        // soldiers leak 2 each, also 6. This is the base wave only - the Vanguard is extra.
        EncounterTuning encounter = Fixture.Example;

        int lane0 = encounter.BaseWave[0].Sum(g => g.Count * Fixture.Tuning.Enemy(g.EnemyId).LeakDamage);
        int lane1 = encounter.BaseWave[1].Sum(g => g.Count * Fixture.Tuning.Enemy(g.EnemyId).LeakDamage);

        Assert.Equal(6, lane0);
        Assert.Equal(6, lane1);
    }
}
