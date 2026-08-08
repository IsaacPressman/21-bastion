using Bastion.Core.Board;
using Bastion.Core.Cards;
using Bastion.Core.Config;
using Bastion.Core.Resolve;

namespace Bastion.Core.Tests.Resolve;

/// <summary>
/// Flat armor, the 0.25 floor, and the half-armor bypass.
/// </summary>
/// <remarks>
/// Every number here is arithmetic the design states directly, so these are the resolver tests
/// closest to being a reading of the documents rather than a check on an invented rule.
/// </remarks>
public sealed class ArmorTests
{
    private static readonly EncounterTuning OneSoldier = Fixture.Solo("armored_soldier", 1);

    /// <summary>The first hit landed on the armored soldier, which is the one with no history.</summary>
    private static ShotEvent FirstShot(Rank rank, Family family)
    {
        BoardState board = Fixture.Board(Fixture.Tower(rank, family, Fixture.Socket(0, 0)));

        return Fixture.ShotsInLaneZero(OneSoldier, board)[0];
    }

    [Theory]
    // A two is 1.6 power against 1.5 flat armor: 0.1 would get through, so the floor carries it.
    [InlineData(Rank.Two, 1.6, 0.25)]
    // A ten is 5.0 against 1.5: 3.5, comfortably clear of the floor.
    [InlineData(Rank.Ten, 5.0, 3.5)]
    public void A_club_pays_full_armor(Rank rank, double raw, double expected)
    {
        ShotEvent shot = FirstShot(rank, Family.Club);

        Assert.Equal(raw, shot.DamageBeforeArmor, precision: 6);
        Assert.Equal(expected, shot.DamageApplied, precision: 6);
    }

    [Theory]
    // Spade traps ignore half of flat armor: 1.5 becomes 0.75, so 1.6 - 0.75 = 0.85.
    [InlineData(Rank.Two, 0.85)]
    [InlineData(Rank.Ten, 4.25)]
    public void A_spade_ignores_half_of_flat_armor(Rank rank, double expected)
    {
        Assert.Equal(expected, FirstShot(rank, Family.Spade).DamageApplied, precision: 6);
    }

    [Fact]
    public void The_king_ignores_half_of_flat_armor_whatever_family_it_is_committed_to()
    {
        // "King - Anchor. Ignores half of flat armor" is a property of the card, not the family,
        // so a King committed as a Club still bypasses. 5.0 - 0.75 = 4.25.
        Assert.Equal(4.25, FirstShot(Rank.King, Family.Club).DamageApplied, precision: 6);
        Assert.Equal(4.25, FirstShot(Rank.King, Family.Spade).DamageApplied, precision: 6);
    }

    [Fact]
    public void Armor_never_reduces_a_hit_below_the_floor()
    {
        // Against the siege engine's 2.0 armor even a King's bypassed 1.0 leaves nothing, so every
        // hit in this matchup lands on the floor exactly.
        BoardState board = Fixture.Board(Fixture.Tower(Rank.Ace, Family.Club, Fixture.Socket(0, 0)));

        IReadOnlyList<ShotEvent> shots = Fixture.ShotsInLaneZero(Fixture.Solo("siege_engine", 1), board);

        Assert.NotEmpty(shots);
        Assert.All(shots, s => Assert.Equal(
            Fixture.Tuning.Combat.ArmorDamageFloor, s.DamageApplied, precision: 6));
    }

    [Fact]
    public void The_floor_limits_what_armor_takes_away_rather_than_raising_a_small_hit()
    {
        // An Ace low is 1.0 power against an unarmored swarm unit. Armor did not reduce it, so it
        // stays 1.0 - the floor is a limit on subtraction, not a minimum damage rule. Reading it
        // the other way would quietly buff every weak tower against every unarmored target.
        BoardState board = Fixture.Board(Fixture.Tower(Rank.Ace, Family.Club, Fixture.Socket(0, 0)));

        ShotEvent shot = Fixture.ShotsInLaneZero(Fixture.Solo("swarm_unit", 1), board)[0];

        Assert.Equal(1.0, shot.DamageApplied, precision: 6);
    }

    [Fact]
    public void Formation_strength_and_run_links_multiply_the_shot_before_armor()
    {
        // 5.0 base x 1.30 formation x 1.25 for a 3-run = 8.125, then 1.5 armor off = 6.625.
        BoardState board = Fixture.Board(
            Fixture.Tower(Rank.Ten, Family.Club, Fixture.Socket(0, 0), formation: 1.30, runBonus: 0.25));

        ShotEvent shot = Fixture.ShotsInLaneZero(OneSoldier, board)[0];

        Assert.Equal(8.125, shot.DamageBeforeArmor, precision: 6);
        Assert.Equal(6.625, shot.DamageApplied, precision: 6);
    }

    [Fact]
    public void Activity_reports_what_armor_ate()
    {
        BoardState board = Fixture.Board(Fixture.Tower(Rank.Ten, Family.Club, Fixture.Socket(0, 0)));

        TowerActivity activity = Fixture.LaneZero(OneSoldier, board).TowerActivity.Single();

        // 1.5 absorbed per shot. Reported so a bad matchup can be explained after the wave without
        // the interface having to draw a conclusion from it.
        Assert.Equal(activity.Shots * 1.5, activity.DamageLostToArmor, precision: 6);
    }
}
