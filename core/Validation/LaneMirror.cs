using Bastion.Core.Board;
using Bastion.Core.Cards;
using Bastion.Core.Config;

namespace Bastion.Core.Validation;

/// <summary>
/// Variant B of a battery case: the same decision, reflected so it cannot be answered from memory.
/// </summary>
/// <remarks>
/// <para>
/// docs/prototype/VALIDATION.md requires <b>each state presented at least twice with different
/// presentation</b>. The hard part is that the two presentations must be the <i>same decision</i> -
/// a variant that quietly changes the problem measures two different things and reports them as a
/// consistency check.
/// </para>
/// <para>
/// Mirroring the lanes is the transform that guarantees it. Everything lane-indexed swaps together -
/// stakes, base wave, the Vanguard's lane, every socket in the script and in the carried-over
/// towers - so the mirrored state is <b>isomorphic by construction</b>: its Final Forecast is the
/// original's with the two lanes exchanged, and a test asserts exactly that. The junction is shared
/// between lanes and therefore maps to itself.
/// </para>
/// <para>
/// The opening deal order is swapped as well, and the first two placements with it, so the same card
/// still lands on the same (mirrored) socket. That changes what the player watches being dealt
/// without changing the board it produces - blackjack totals are order-independent, and an Ace's
/// state is derived from the hand multiset rather than the sequence.
/// </para>
/// <para>
/// Two lanes is the prototype's whole world, so this is a swap rather than a general permutation.
/// A third lane would make it one.
/// </para>
/// </remarks>
public static class LaneMirror
{
    /// <summary>Suffix on a mirrored fixture's id.</summary>
    public const string VariantSuffix = "-b";

    /// <summary>The other lane. The junction belongs to both, so it maps to itself.</summary>
    public static SocketRef Mirror(SocketRef socket) =>
        socket.IsJunction ? socket : SocketRef.InLane(1 - socket.LaneIndex, socket.SocketIndex);

    /// <summary>
    /// The mirrored encounter, under a distinct id so the two never collide in a log.
    /// </summary>
    public static EncounterTuning Mirror(EncounterTuning encounter)
    {
        ArgumentNullException.ThrowIfNull(encounter);

        Require(encounter.LaneStakes.Count == 2, "Mirroring assumes the prototype's two lanes.");
        Require(encounter.BaseWave.Count == 2, "Mirroring assumes the prototype's two lanes.");

        return encounter with
        {
            Id = encounter.Id + VariantSuffix,
            VanguardLane = 1 - encounter.VanguardLane,
            LaneStakes = [encounter.LaneStakes[1], encounter.LaneStakes[0]],
            BaseWave = [encounter.BaseWave[1], encounter.BaseWave[0]],
        };
    }

    /// <summary>
    /// The mirrored fixture: every socket reflected, and the opening two cards dealt the other way
    /// round.
    /// </summary>
    /// <remarks>
    /// Swapping the deal order alone would put the wrong card on the wrong socket, so the first two
    /// <see cref="ScriptAction.Place"/> steps swap with it. Everything after the opening two is left
    /// in order - those cards arrive one at a time in response to a hit, so there is no pair to
    /// exchange.
    /// </remarks>
    public static BatteryFixture Mirror(BatteryFixture fixture)
    {
        ArgumentNullException.ThrowIfNull(fixture);

        return fixture with
        {
            Id = fixture.Id + VariantSuffix,
            EncounterId = fixture.EncounterId + VariantSuffix,
            CardOrder = SwapOpeningDeal(fixture.CardOrder),
            Persisted = [.. fixture.Persisted.Select(p => p with { Socket = Mirror(p.Socket) })],
            Script = SwapOpeningPlacements([.. fixture.Script.Select(MirrorStep)]),
        };
    }

    private static ScriptStep MirrorStep(ScriptStep step) =>
        step.Action == ScriptAction.Place ? step with { Socket = Mirror(step.Socket) } : step;

    /// <summary>
    /// The player's opening two cards exchanged. Positions 0 and 1 are the Dealer's upcard and hole
    /// card, so the player's opening pair is at 2 and 3.
    /// </summary>
    private static IReadOnlyList<Rank> SwapOpeningDeal(IReadOnlyList<Rank> order)
    {
        if (order.Count < 4)
        {
            return order;
        }

        List<Rank> swapped = [.. order];
        (swapped[2], swapped[3]) = (swapped[3], swapped[2]);

        return swapped;
    }

    private static IReadOnlyList<ScriptStep> SwapOpeningPlacements(IReadOnlyList<ScriptStep> script)
    {
        int first = IndexOfPlace(script, after: -1);
        int second = IndexOfPlace(script, after: first);

        if (first < 0 || second < 0)
        {
            // A fixture that hands over before placing anything - the family-commitment case - has
            // no pair to exchange, and its mirrored deal order is what varies the presentation.
            return script;
        }

        List<ScriptStep> swapped = [.. script];
        (swapped[first], swapped[second]) = (swapped[second], swapped[first]);

        return swapped;
    }

    private static int IndexOfPlace(IReadOnlyList<ScriptStep> script, int after)
    {
        for (int i = after + 1; i < script.Count; i++)
        {
            if (script[i].Action == ScriptAction.Place)
            {
                return i;
            }
        }

        return -1;
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
