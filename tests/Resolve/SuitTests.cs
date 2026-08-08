using Bastion.Core.Board;
using Bastion.Core.Cards;
using Bastion.Core.Config;
using Bastion.Core.Resolve;

namespace Bastion.Core.Tests.Resolve;

/// <summary>
/// Club splash and Spade slow - the two behaviours that make family locking a real commitment.
/// </summary>
/// <remarks>
/// The magnitudes are NOT SPECIFIED BY THE DESIGN and are expected to be wrong. What these tests
/// defend is the shape: a Club spreads a shot, a Spade slows what it hits, and neither does the
/// other's job. If family choice stops mattering, the prototype's primary claim - that decision
/// density comes from what a card becomes - loses its main support.
/// </remarks>
public sealed class SuitTests
{
    // Armored soldiers spawn 1.50 s apart at speed 0.65, so consecutive units sit 0.975 path units
    // apart - just inside the 1.0 splash radius, and outside it at two units' remove.
    private static readonly EncounterTuning ThreeSoldiers = Fixture.Solo("armored_soldier", 3);

    private static BoardState With(Family family, Rank rank = Rank.Two) =>
        Fixture.Board(Fixture.Tower(rank, family, Fixture.Socket(0, 0)));

    [Fact]
    public void A_club_splashes_and_a_spade_does_not()
    {
        Assert.Contains(Fixture.ShotsInLaneZero(ThreeSoldiers, With(Family.Club)), s => s.IsSplash);
        Assert.DoesNotContain(Fixture.ShotsInLaneZero(ThreeSoldiers, With(Family.Spade)), s => s.IsSplash);
    }

    [Fact]
    public void A_splash_hit_is_half_the_shot()
    {
        IReadOnlyList<ShotEvent> shots = Fixture.ShotsInLaneZero(ThreeSoldiers, With(Family.Club));

        double primary = shots.First(s => !s.IsSplash).DamageBeforeArmor;

        Assert.All(
            shots.Where(s => s.IsSplash),
            s => Assert.Equal(primary * Fixture.Tuning.Suits.Clubs.SplashFraction, s.DamageBeforeArmor, precision: 6));
    }

    [Fact]
    public void A_splash_hit_pays_armor_and_the_floor_on_its_own()
    {
        // A two splashes 0.8 into 1.5 flat armor, so every secondary lands on the floor. Applying
        // armor once for the whole blast would quietly make Clubs an anti-armor family.
        IReadOnlyList<ShotEvent> splash =
            [.. Fixture.ShotsInLaneZero(ThreeSoldiers, With(Family.Club)).Where(s => s.IsSplash)];

        Assert.NotEmpty(splash);
        Assert.All(splash, s => Assert.Equal(
            Fixture.Tuning.Combat.ArmorDamageFloor, s.DamageApplied, precision: 6));
    }

    [Fact]
    public void Splash_reaches_a_column_tighter_than_its_radius()
    {
        // Armored soldiers spawn 1.50 s apart at speed 0.65 and hold 0.975 units of separation -
        // inside the 1.0 radius.
        Assert.Contains(Fixture.ShotsInLaneZero(Fixture.Solo("armored_soldier", 2), With(Family.Club)),
            s => s.IsSplash);
    }

    [Fact]
    public void Splash_does_not_reach_a_column_looser_than_its_radius()
    {
        // Fast raiders spawn 0.75 s apart at speed 1.60 and hold 1.2 units - outside it. A column
        // that outruns the blast is the counterplay to artillery, and it falls out of the geometry
        // rather than needing a rule.
        Assert.DoesNotContain(Fixture.ShotsInLaneZero(Fixture.Solo("fast_raider", 2), With(Family.Club)),
            s => s.IsSplash);
    }

    [Fact]
    public void A_spade_slows_and_a_club_does_not()
    {
        Assert.Contains(Fixture.EventsInLaneZero(ThreeSoldiers, With(Family.Spade)), e => e is SlowEvent);
        Assert.DoesNotContain(Fixture.EventsInLaneZero(ThreeSoldiers, With(Family.Club)), e => e is SlowEvent);
    }

    [Fact]
    public void A_second_application_refreshes_the_slow_rather_than_extending_it()
    {
        // Every application expires exactly slowSeconds after it lands, never further. Duration
        // that compounded would let one Spade firing steadily hold a unit indefinitely, which is a
        // hard stop wearing the word "slow".
        IEnumerable<SlowEvent> slows =
            Fixture.EventsInLaneZero(ThreeSoldiers, With(Family.Spade)).OfType<SlowEvent>();

        Assert.NotEmpty(slows);
        Assert.All(slows, s => Assert.Equal(
            s.Time + Fixture.Tuning.Suits.Spades.SlowSeconds, s.UntilTime, precision: 6));
    }

    [Fact]
    public void A_slowed_unit_arrives_later_than_an_unslowed_one()
    {
        // The siege engine's 2.0 armor swallows an Ace's shot down to the floor either way, so the
        // two families deal identical damage here and the only difference left is the slow.
        EncounterTuning siege = Fixture.Solo("siege_engine", 1);

        double clubbed = LeakTime(siege, With(Family.Club, Rank.Ace));
        double slowed = LeakTime(siege, With(Family.Spade, Rank.Ace));

        Assert.True(slowed > clubbed, $"Slowed leak at {slowed}s was not later than {clubbed}s.");
    }

    private static double LeakTime(EncounterTuning encounter, BoardState board) =>
        Fixture.EventsInLaneZero(encounter, board).OfType<LeakEvent>().Single().Time;
}
