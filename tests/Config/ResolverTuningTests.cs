using Bastion.Core.Config;

namespace Bastion.Core.Tests.Config;

/// <summary>
/// The tuning sections the design does not specify, pinned so a casual edit is a failing test.
/// </summary>
/// <remarks>
/// <para>
/// Every value here was decided rather than derived: the handoff specifies the enemy side of combat
/// completely and the tower side not at all. See data/tuning.json's resolver comment block and
/// docs/reference/tuning-constants.md section "Invented for the resolver".
/// </para>
/// <para>
/// These assertions are not defending the numbers - the numbers are first-pass and expected to be
/// wrong. They defend the fact that changing one is a deliberate act, because a silent change here
/// moves the resolver's output without moving anything the design documents.
/// </para>
/// </remarks>
public sealed class ResolverTuningTests
{
    private static readonly TuningData Tuning = TuningLoader.LoadFromRepositoryRoot();

    [Fact]
    public void The_simulation_ticks_at_twenty_hertz()
    {
        Assert.Equal(0.05, Tuning.Sim.TickSeconds, precision: 6);
    }

    [Fact]
    public void Every_tuned_duration_is_a_whole_number_of_ticks()
    {
        // The loader enforces this; asserting it here states why it matters. A duration that is not
        // a whole number of ticks resolves as a different duration in both the forecast and the
        // wave - so they agree with each other while both disagree with the tuning file, and
        // nothing downstream ever reveals the drift.
        Assert.Equal(20, Tuning.Sim.TicksIn(Tuning.Towers.CooldownSeconds));
        Assert.Equal(20, Tuning.Sim.TicksIn(Tuning.Waves.GroupGapSeconds));
        Assert.Equal(30, Tuning.Sim.TicksIn(Tuning.Suits.Spades.SlowSeconds));

        foreach (EnemyTuning enemy in Tuning.Enemies)
        {
            if (enemy.SpacingSeconds is double spacing)
            {
                Assert.Equal(spacing, Tuning.Sim.TicksIn(spacing) * Tuning.Sim.TickSeconds, precision: 9);
            }
        }
    }

    [Fact]
    public void Towers_share_one_cooldown()
    {
        Assert.Equal(1.0, Tuning.Towers.CooldownSeconds, precision: 6);
    }

    [Fact]
    public void The_junction_sits_at_the_middle_socket_and_fires_at_half_strength()
    {
        double middleSocket = Tuning.Geometry.SocketPositions.OrderBy(p => p).ElementAt(1);

        Assert.Equal(middleSocket, Tuning.Towers.JunctionPathPosition, precision: 6);
        Assert.Equal(0.5, Tuning.Towers.JunctionContributionFraction, precision: 6);
    }

    [Fact]
    public void Face_cards_escape_the_junction_penalty()
    {
        // design/04-cards-as-defenses.md: face cards "may occupy the shared junction socket without
        // the usual contribution penalty."
        Assert.True(Tuning.Towers.JunctionFaceCardExempt);
    }

    [Fact]
    public void Splash_reaches_a_third_of_the_socket_spacing()
    {
        double spacing = Tuning.Geometry.SocketPositions[1] - Tuning.Geometry.SocketPositions[0];

        // Deliberate: splash rewards hitting a clustered swarm without reaching from one socket's
        // kill zone into another's, which would blur the placement decision.
        Assert.Equal(spacing / 3.0, Tuning.Suits.Clubs.SplashRadius, precision: 6);
        Assert.Equal(0.5, Tuning.Suits.Clubs.SplashFraction, precision: 6);
    }

    [Fact]
    public void Slow_refreshes_rather_than_stacking()
    {
        Assert.Equal(0.6, Tuning.Suits.Spades.SlowMultiplier, precision: 6);
        Assert.Equal(1.5, Tuning.Suits.Spades.SlowSeconds, precision: 6);
        Assert.False(Tuning.Suits.Spades.SlowStacks);
    }

    [Fact]
    public void Trigger_on_group_waits_for_three_enemies_within_the_splash_radius()
    {
        Assert.Equal(3, Tuning.StandingOrders.TriggerGroupMinEnemies);
        Assert.Equal(Tuning.Suits.Clubs.SplashRadius, Tuning.StandingOrders.TriggerGroupRadius, precision: 6);
    }

    [Fact]
    public void A_dealer_card_deploys_its_whole_pack()
    {
        // "Swarm pack - many, fragile" and "Fast raiders" against a singular "Siege engine" is what
        // the design's own table describes, and it is what makes the upcard readable as a shape.
        Assert.True(Tuning.Waves.DealerCardDeploysFullPack);
        Assert.Equal("alternate_from_vanguard", Tuning.Waves.DealerLaneAssignment);
        Assert.Equal(1.0, Tuning.Waves.GroupGapSeconds, precision: 6);
    }

    [Fact]
    public void The_worked_example_ships_as_an_encounter()
    {
        EncounterTuning encounter = Tuning.Encounter("example_wave");

        Assert.Equal(0, encounter.VanguardLane);
        Assert.Equal(["bastion", "vault"], encounter.LaneStakes);
        Assert.Equal(Tuning.Geometry.Lanes, encounter.BaseWave.Count);
    }

    [Theory]
    [InlineData(0, "armored_soldier", 3, 6)]
    [InlineData(1, "fast_raider", 6, 6)]
    public void Each_worked_example_lane_leaks_six_undefended(
        int lane, string enemyId, int count, int expectedLeak)
    {
        // design/example-wave.md states lane two forecasts 6 damage undefended. Fast raiders leak 1
        // each, so the package is six raiders - the roster's count of five is the Dealer pack size,
        // which is a different question. Lane one's three armored soldiers leak 2 each, also 6.
        SpawnGroupTuning group = Assert.Single(Tuning.Encounter("example_wave").BaseWave[lane]);

        Assert.Equal(enemyId, group.EnemyId);
        Assert.Equal(count, group.Count);
        Assert.Equal(expectedLeak, group.Count * Tuning.Enemy(enemyId).LeakDamage);
    }
}
