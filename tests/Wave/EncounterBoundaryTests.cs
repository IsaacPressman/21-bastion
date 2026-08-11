using Bastion.Core.Board;
using Bastion.Core.Cards;
using Bastion.Core.Config;
using Bastion.Core.Wave;

namespace Bastion.Core.Tests.Wave;

/// <summary>
/// The bound on persistence: towers carry across the waves of an encounter and reset at its boundary.
/// </summary>
/// <remarks>
/// docs/design/05-battlefield.md § Persistence. Persistence exists to create scarcity, not to bank
/// power - so it has to end somewhere, and it ends at the encounter. Unbounded, the board saturates
/// after a few waves and then nothing a draw produces can change it.
/// </remarks>
public sealed class EncounterBoundaryTests
{
    private static readonly TuningData Tuning = TuningLoader.LoadFromRepositoryRoot();
    private static EncounterTuning Encounter => Tuning.Encounter("example_wave");

    private static SocketRef Bastion(int socket) => SocketRef.InLane(0, socket);

    /// <summary>A wave played to a lock, with two Clubs placed, at the given wave of the encounter.</summary>
    private static WaveSession PlayedWave(int waveNumber) =>
        WaveSession.Begin(
                Tuning,
                Encounter,
                Shoe.FromOrder([Rank.Ten, Rank.Seven, Rank.Six, Rank.Eight]),
                persisted: null,
                waveNumber)
            .Place(Family.Club, Bastion(1))
            .Place(Family.Club, Bastion(2))
            .Stand()
            .Lock();

    [Fact]
    public void A_wave_inside_the_encounter_carries_its_towers_forward()
    {
        WaveSession wave = PlayedWave(1);

        Assert.False(wave.IsFinalWaveOfEncounter);
        Assert.Equal(2, wave.Settle().Persisted.Count);
    }

    [Fact]
    public void The_encounters_last_wave_carries_nothing_forward()
    {
        WaveSession wave = PlayedWave(Encounter.Waves);

        Assert.True(wave.IsFinalWaveOfEncounter);
        Assert.Empty(wave.Settle().Persisted);
    }

    [Fact]
    public void The_wave_count_advances_inside_an_encounter_and_restarts_after_it()
    {
        Assert.Equal(2, PlayedWave(1).NextWaveNumber);
        Assert.Equal(1, PlayedWave(Encounter.Waves).NextWaveNumber);
    }

    /// <summary>
    /// The shoe is the run's deck, not the encounter's, so it survives a boundary the board does not.
    /// </summary>
    /// <remarks>
    /// Played on a padded shoe so it stays above the reshuffle threshold: a short scripted shoe runs
    /// dry and <c>ReshuffleIfLow</c> refills it, which would hide whether the boundary itself reset it.
    /// </remarks>
    [Fact]
    public void The_shoe_crosses_the_boundary_even_though_the_board_does_not()
    {
        Rank[] padded = [Rank.Ten, Rank.Seven, Rank.Six, Rank.Eight, .. Enumerable.Repeat(Rank.Five, 12)];

        WaveSession last = WaveSession
            .Begin(Tuning, Encounter, Shoe.FromOrder(padded), persisted: null, Encounter.Waves)
            .Place(Family.Club, Bastion(1))
            .Place(Family.Club, Bastion(2))
            .Stand()
            .Lock();

        (IReadOnlyList<TowerState> carried, Shoe shoe) = last.Settle();

        Assert.Empty(carried);
        Assert.Equal(last.Shoe.Count, shoe.Count);
        Assert.Equal(last.Shoe.Generation, shoe.Generation);
    }

    /// <summary>
    /// The point of the bound: a fresh encounter is played on an empty board, so socket capacity is
    /// something the player reaches near the end of an encounter rather than permanently.
    /// </summary>
    [Fact]
    public void The_next_encounter_opens_on_an_empty_board()
    {
        (IReadOnlyList<TowerState> carried, Shoe shoe) = PlayedWave(Encounter.Waves).Settle();

        WaveSession fresh = WaveSession.Begin(Tuning, Encounter, shoe, carried, waveNumber: 1);

        Assert.Empty(fresh.Board().Towers);
        Assert.Equal(1, fresh.WaveNumber);
    }

    [Fact]
    public void Waves_are_numbered_from_one()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            WaveSession.Begin(Tuning, Encounter, Shoe.Create(Tuning, seed: 1), persisted: null, waveNumber: 0));
    }
}
