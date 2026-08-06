using Bastion.Core.Config;

namespace Bastion.Core.Tests.Config;

/// <summary>
/// Milestone 0's acceptance test: tuning data loads and can be asserted against, with no Godot
/// window opened. Values are checked against docs/reference/tuning-constants.md.
/// </summary>
public sealed class TuningDataTests
{
    private static readonly TuningData Tuning = TuningLoader.LoadFromRepositoryRoot();

    [Fact]
    public void Loads_from_repository_root_without_an_engine()
    {
        Assert.Equal("7.1", Tuning.Revision);
    }

    [Fact]
    public void Geometry_matches_the_design()
    {
        Assert.Equal(12.0, Tuning.Geometry.PathLength);
        Assert.Equal([3.0, 6.0, 9.0], Tuning.Geometry.SocketPositions);
        Assert.Equal(3.0, Tuning.Geometry.DefaultRange);
        Assert.Equal(4.0, Tuning.Geometry.FaceCardRange);

        // Two lanes of three, plus the shared junction.
        Assert.Equal(7, Tuning.Geometry.TotalSockets);
    }

    [Fact]
    public void All_three_march_arms_ship_in_every_build()
    {
        // The arms are presets in one config file, not three builds.
        Assert.Equal(["A", "B", "C"], Tuning.MarchPresets.Keys.Order());
    }

    [Theory]
    // Arm A - flat control, Revision 6's step.
    [InlineData("A", 3, 1.0)]
    [InlineData("A", 4, 2.0)]
    [InlineData("A", 5, 3.0)]
    // Arm B - soft escalation.
    [InlineData("B", 3, 1.0)]
    [InlineData("B", 4, 2.5)]
    [InlineData("B", 5, 4.5)]
    // Arm C - hard escalation, the curve the design specifies.
    [InlineData("C", 3, 1.5)]
    [InlineData("C", 4, 4.0)]
    [InlineData("C", 5, 7.5)]
    public void Cumulative_entry_matches_the_published_curves(string arm, int cardCount, double expectedEntry)
    {
        MarchPreset preset = Tuning.MarchPresets[arm];

        Assert.Equal(expectedEntry, preset.CumulativeEntry(cardCount, Tuning.March.FreeCards), precision: 6);
    }

    [Fact]
    public void The_opening_two_cards_are_free_in_every_arm()
    {
        Assert.Equal(2, Tuning.March.FreeCards);

        foreach (MarchPreset preset in Tuning.MarchPresets.Values)
        {
            Assert.Equal(0.0, preset.CumulativeEntry(1, Tuning.March.FreeCards));
            Assert.Equal(0.0, preset.CumulativeEntry(2, Tuning.March.FreeCards));
        }
    }

    [Fact]
    public void Arm_C_is_active_by_default()
    {
        Assert.Equal("C", Tuning.March.ActivePreset);
        Assert.Equal("Hard escalation", Tuning.ActiveMarchPreset.Label);
    }

    [Fact]
    public void Exactly_21_pullback_is_held_at_its_revision_7_value()
    {
        // Deliberately unchanged in 7.1: tuning the pullback and the step curve together would
        // mean moving two coupled levers against a number just shown to be untrustworthy.
        Assert.Equal(3.0, Tuning.March.Exactly21Pullback);
        Assert.Equal(0.0, Tuning.March.EntryClampMin);
    }

    [Fact]
    public void Entry_clamp_is_the_rear_socket_position()
    {
        // Derived from geometry, not chosen independently of it: enemies must never spawn past
        // the player's last defense.
        Assert.Equal(Tuning.Geometry.SocketPositions.Max(), Tuning.March.EntryClampMax);
        Assert.Equal(9.0, Tuning.March.EntryClampMax);
    }

    [Fact]
    public void Overload_targets_the_most_threatened_lane()
    {
        // The busting card is destroyed and never placed, so there is no placement to inherit a
        // lane from. Deterministic and unsteerable.
        Assert.Equal("highest_visible_threat", Tuning.Rules.OverloadTargetLane);
        Assert.Equal("bastion", Tuning.Rules.OverloadTieBreakStake);
    }

    [Theory]
    [InlineData(21, 1.60)]
    [InlineData(20, 1.50)]
    [InlineData(18, 1.30)]
    [InlineData(16, 1.15)]
    [InlineData(13, 1.00)]
    [InlineData(12, 0.95)]
    [InlineData(11, 0.90)]
    [InlineData(4, 0.90)]
    public void Formation_strength_curve_matches_the_design(int total, double expected)
    {
        Assert.Equal(expected, Tuning.FormationStrength.ForTotal(total));
    }

    [Fact]
    public void Formation_strength_spans_two_times_from_bust_to_21()
    {
        Assert.Equal(0.80, Tuning.FormationStrength.Bust);
        Assert.Equal(2.0, Tuning.FormationStrength.ForTotal(21) / Tuning.FormationStrength.Bust, precision: 6);
    }

    [Fact]
    public void Persisted_towers_revert_to_one()
    {
        Assert.Equal(1.00, Tuning.FormationStrength.Persisted);
    }

    [Theory]
    [InlineData(1, 1.0)]   // Ace low
    [InlineData(2, 1.6)]
    [InlineData(5, 3.1)]
    [InlineData(9, 4.7)]
    [InlineData(10, 5.0)]  // all face cards
    [InlineData(11, 5.4)]  // Ace high
    public void Card_power_curve_matches_the_design(int value, double expected)
    {
        Assert.Equal(expected, Tuning.CardPower.ForValue(value));
    }

    [Fact]
    public void Card_power_is_sublinear_in_card_value()
    {
        // A ten is five times the blackjack value of a two but only three times the tower power.
        double ratio = Tuning.CardPower.ForValue(10) / Tuning.CardPower.ForValue(2);

        Assert.InRange(ratio, 3.0, 3.2);
    }

    [Theory]
    [InlineData(2, 0.15)]
    [InlineData(3, 0.25)]
    [InlineData(1, 0.0)]   // a lone tower is not a run
    [InlineData(4, 0.0)]   // geometrically impossible in the prototype - see below
    public void Run_link_bonuses_match_the_design(int runLength, double expected)
    {
        Assert.Equal(expected, Tuning.RunLinks.BonusForRunLength(runLength));
    }

    [Fact]
    public void Runs_cap_at_the_longest_the_geometry_can_produce()
    {
        // Three sockets per lane, no cross-lane adjacency, junction excluded: a run of four
        // cannot be built. The +35% tier is absent on purpose and returns when socket counts
        // grow, which is what makes the Surveyor relic a link tier rather than a coverage bump.
        Assert.False(Tuning.RunLinks.JunctionParticipatesInRuns);
        Assert.False(Tuning.RunLinks.CrossLaneAdjacency);

        Assert.Equal(Tuning.Geometry.SocketPositions.Count, Tuning.RunLinks.MaxRewardedRunLength);
        Assert.Equal(3, Tuning.RunLinks.MaxRewardedRunLength);
    }

    [Fact]
    public void Enemy_roster_covers_all_dealer_card_units()
    {
        string[] expected = [
            "armored_soldier", "fast_raider", "herald",
            "siege_engine", "skirmisher", "standard_bearer", "swarm_unit"
        ];

        Assert.Equal(expected, Tuning.Enemies.Select(e => e.Id).Order());

        Assert.All(
            Tuning.DealerCardUnits.Values.Distinct(),
            unitId => Assert.Contains(Tuning.Enemies, e => e.Id == unitId));
    }

    [Fact]
    public void Siege_engine_matches_the_design()
    {
        EnemyTuning siege = Tuning.Enemies.Single(e => e.Id == "siege_engine");

        Assert.Equal(1, siege.Count);
        Assert.Equal(30.0, siege.Health);
        Assert.Equal(0.40, siege.Speed);
        Assert.Equal(2.0, siege.FlatArmor);
        Assert.Equal(5, siege.LeakDamage);

        // A single unit has no spacing.
        Assert.Null(siege.SpacingSeconds);
    }

    [Fact]
    public void Armor_can_never_zero_out_a_hit()
    {
        Assert.Equal(0.25, Tuning.Combat.ArmorDamageFloor);
        Assert.Equal(0.5, Tuning.Combat.HalfArmorBypassFraction);
    }

    [Fact]
    public void Shoe_is_two_copies_of_thirteen_ranks()
    {
        Assert.Equal(26, Tuning.Rules.ShoeSize);
        Assert.Equal(2, Tuning.Rules.CopiesPerRank);
        Assert.Equal(8, Tuning.Rules.ReshuffleBelowCards);
    }

    [Fact]
    public void Adjustment_window_is_one_move_for_the_whole_board()
    {
        Assert.Equal(1, Tuning.Rules.AdjustmentMovesPerWave);
        Assert.True(Tuning.Rules.AdjustmentAllowsAdjacentSwap);

        // Standing orders are free and do not compete with the move.
        Assert.False(Tuning.Rules.StandingOrderChangesConsumeMove);

        // Bust locks placement immediately.
        Assert.False(Tuning.Rules.AdjustmentWindowOnBust);
    }

    [Fact]
    public void Dealer_rules_match_the_prototype()
    {
        Assert.True(Tuning.Rules.DealerStandsOnAll17s);
        Assert.True(Tuning.Rules.DealerResolvesOnPlayerBust);
    }

    [Fact]
    public void Overload_is_capped_at_base_power()
    {
        Assert.True(Tuning.Rules.OverloadEqualsBasePower);
        Assert.False(Tuning.Rules.OverloadScalesWithExcess);
    }

    [Fact]
    public void Every_dealer_rank_maps_to_a_unit()
    {
        string[] ranks = ["2", "3", "4", "5", "6", "7", "8", "9", "10", "J", "Q", "K", "A"];

        Assert.Equal(ranks.Length, Tuning.DealerCardUnits.Count);
        Assert.All(ranks, rank => Assert.True(
            Tuning.DealerCardUnits.ContainsKey(rank),
            $"No unit mapped for Dealer rank {rank}."));

        Assert.Equal("siege_engine", Tuning.DealerCardUnits["K"]);
        Assert.Equal("herald", Tuning.DealerCardUnits["A"]);
    }

    [Fact]
    public void Open_held_threshold_is_half_of_empty_lane_damage()
    {
        Assert.Equal(0.5, Tuning.Rules.OpenHeldThresholdFraction);
    }
}
