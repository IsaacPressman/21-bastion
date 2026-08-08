using Bastion.Core.Config;

namespace Bastion.Core.Cards;

/// <summary>
/// The shoe the player draws from: two copies of each rank, dealt without replacement, persisting
/// across the waves of an encounter and reshuffled when it runs low.
/// </summary>
/// <remarks>
/// <para>
/// Immutable. <see cref="Draw"/> and <see cref="ReshuffleIfLow"/> return a new shoe rather than
/// mutating this one, matching the rest of the core - so a caller can hold a pre-draw shoe to
/// compare against, and nothing shares draw state by accident.
/// </para>
/// <para>
/// Deterministic from an explicit seed: the scripted validation battery depends on the same seed
/// producing the same order every run (docs/ARCHITECTURE.md § Determinism). The shuffle uses a
/// fixed arithmetic seed derivation rather than <see cref="HashCode"/>, whose per-process
/// randomization would break that guarantee.
/// </para>
/// <para>
/// Suit is not dealt: a draw yields a <see cref="Rank"/>, and the player chooses Club or Spade at
/// placement (docs/design/04-cards-as-defenses.md). The neutral shoe is why family is a commitment
/// rather than a card property.
/// </para>
/// </remarks>
public sealed class Shoe
{
    private readonly IReadOnlyList<Rank> _remaining;
    private readonly int _seed;
    private readonly int _generation;

    private Shoe(IReadOnlyList<Rank> remaining, int seed, int generation)
    {
        _remaining = remaining;
        _seed = seed;
        _generation = generation;
    }

    /// <summary>Cards left before the next reshuffle.</summary>
    public int Count => _remaining.Count;

    /// <summary>How many times this shoe has been reshuffled. Zero for a fresh shoe.</summary>
    public int Generation => _generation;

    /// <summary>A full, shuffled shoe seeded for reproducibility.</summary>
    public static Shoe Create(TuningData tuning, int seed)
    {
        ArgumentNullException.ThrowIfNull(tuning);

        return new Shoe(Shuffle(FullDeck(tuning), DeriveSeed(seed, generation: 0)), seed, generation: 0);
    }

    /// <summary>
    /// Draws the top card, returning it alongside the shoe that remains.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// If the shoe is empty. Callers reshuffle at the wave boundary via <see cref="ReshuffleIfLow"/>,
    /// so a mid-hand empty shoe is a scheduling bug rather than an expected state.
    /// </exception>
    public (Rank Card, Shoe Remaining) Draw()
    {
        if (_remaining.Count == 0)
        {
            throw new InvalidOperationException(
                "The shoe is empty. Reshuffle at the wave boundary before drawing.");
        }

        Rank card = _remaining[0];
        Shoe rest = new([.. _remaining.Skip(1)], _seed, _generation);
        return (card, rest);
    }

    /// <summary>
    /// A freshly shuffled full shoe if this one has dropped below the reshuffle threshold, otherwise
    /// this shoe unchanged. Called at a wave boundary, never mid-hand.
    /// </summary>
    /// <remarks>
    /// Reshuffling rebuilds the whole shoe: discards return, which is what "reshuffle under 8" means
    /// (docs/reference/tuning-constants.md). The new order is derived from the original seed and the
    /// bumped generation, so a run is reproducible across reshuffles.
    /// </remarks>
    public Shoe ReshuffleIfLow(TuningData tuning)
    {
        ArgumentNullException.ThrowIfNull(tuning);

        if (_remaining.Count >= tuning.Rules.ReshuffleBelowCards)
        {
            return this;
        }

        int nextGeneration = _generation + 1;
        return new Shoe(Shuffle(FullDeck(tuning), DeriveSeed(_seed, nextGeneration)), _seed, nextGeneration);
    }

    /// <summary>Two copies of each rank A-K, in rank order before shuffling.</summary>
    private static List<Rank> FullDeck(TuningData tuning)
    {
        List<Rank> deck = new(tuning.Rules.ShoeSize);

        foreach (Rank rank in Enum.GetValues<Rank>())
        {
            for (int copy = 0; copy < tuning.Rules.CopiesPerRank; copy++)
            {
                deck.Add(rank);
            }
        }

        return deck;
    }

    /// <summary>Fisher-Yates with a fixed-seed <see cref="Random"/>, so the order is reproducible.</summary>
    private static IReadOnlyList<Rank> Shuffle(List<Rank> deck, int seed)
    {
        Random rng = new(seed);

        for (int i = deck.Count - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            (deck[i], deck[j]) = (deck[j], deck[i]);
        }

        return deck;
    }

    /// <summary>
    /// Mixes the seed with the reshuffle generation deterministically, so each reshuffle draws a
    /// different but reproducible order. Not <see cref="HashCode.Combine(int, int)"/>, which is
    /// randomized per process and would make runs disagree.
    /// </summary>
    private static int DeriveSeed(int seed, int generation) => unchecked((seed * 486187739) + generation);
}
