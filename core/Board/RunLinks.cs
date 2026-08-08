using Bastion.Core.Cards;
using Bastion.Core.Config;

namespace Bastion.Core.Board;

/// <summary>
/// Detects run links: consecutive card values in adjacent sockets, evaluated at lock.
/// </summary>
/// <remarks>
/// <para>
/// A pure function producing the <see cref="TowerState.RunBonus"/> each socket earns. The resolver
/// reads that bonus and never asks how it was found, so this is the whole of the run system - see
/// docs/design/04-cards-as-defenses.md § Run links for every rule reproduced here.
/// </para>
/// <para>
/// Runs are linear within a lane; the junction is a run island; lanes never link across
/// (<c>runLinks.junctionParticipatesInRuns</c> and <c>crossLaneAdjacency</c> are both false, and the
/// loader enforces it). Direction does not matter, so 4-5-6 and 6-5-4 both count. An Ace uses its
/// current 1-or-11 state; a Queen is wild, taking one value chosen to maximise run length; and a run
/// must contain at least one non-Queen card.
/// </para>
/// </remarks>
public static class RunLinks
{
    /// <summary>
    /// The run bonus each occupied socket earns. Sockets not in a run are absent (treat as 0).
    /// </summary>
    public static IReadOnlyDictionary<SocketRef, double> BonusBySocket(
        TuningData tuning, IReadOnlyList<(Card Card, SocketRef Socket)> towers)
    {
        ArgumentNullException.ThrowIfNull(tuning);
        ArgumentNullException.ThrowIfNull(towers);

        Dictionary<SocketRef, double> bonuses = [];
        int socketCount = tuning.Geometry.SocketPositions.Count;

        for (int lane = 0; lane < tuning.Geometry.Lanes; lane++)
        {
            int laneForClosure = lane;
            (Card Card, SocketRef Socket)[] inLane =
                [.. towers.Where(t => !t.Socket.IsJunction && t.Socket.LaneIndex == laneForClosure)];

            foreach ((SocketRef socket, double bonus) in BestLaneRun(tuning, inLane, socketCount))
            {
                bonuses[socket] = bonus;
            }
        }

        return bonuses;
    }

    /// <summary>
    /// Resolves one lane, choosing Queen values to maximise the lane's total run bonus.
    /// </summary>
    /// <remarks>
    /// A Queen is the only wild card, so she is the only free variable. With at most three sockets a
    /// lane she is brute-forced over every value; ties in total bonus keep the lowest Queen value,
    /// which is deterministic and never observable (her own power is fixed at the face-card value).
    /// </remarks>
    private static Dictionary<SocketRef, double> BestLaneRun(
        TuningData tuning, IReadOnlyList<(Card Card, SocketRef Socket)> inLane, int socketCount)
    {
        // Lay the lane out by depth so adjacency is index +/- 1. Empty sockets break adjacency.
        int?[] value = new int?[socketCount];
        bool[] isQueen = new bool[socketCount];
        SocketRef[] socketAt = new SocketRef[socketCount];

        List<int> queenSlots = [];
        foreach ((Card card, SocketRef socket) in inLane)
        {
            int i = socket.SocketIndex;
            socketAt[i] = socket;
            if (card.Rank == Rank.Queen)
            {
                isQueen[i] = true;
                queenSlots.Add(i);
            }
            else
            {
                value[i] = card.Value;   // Ace already carries its 1-or-11 state.
            }
        }

        Dictionary<SocketRef, double> best = [];
        double bestTotal = -1.0;

        // Every combination of Queen values in [1..13]. Fixed order, so the choice is deterministic.
        foreach (int[] queenValues in QueenValueCombinations(queenSlots.Count))
        {
            for (int q = 0; q < queenSlots.Count; q++)
            {
                value[queenSlots[q]] = queenValues[q];
            }

            Dictionary<SocketRef, double> awarded = AwardRuns(tuning, value, isQueen, socketAt, socketCount);
            double total = awarded.Values.Sum();

            if (total > bestTotal)
            {
                bestTotal = total;
                best = awarded;
            }
        }

        return best;
    }

    /// <summary>
    /// Finds the maximal same-direction runs, then hands each socket to at most one of them: the
    /// longest run containing it, ties broken toward the smallest lowest socket index.
    /// </summary>
    private static Dictionary<SocketRef, double> AwardRuns(
        TuningData tuning, int?[] value, bool[] isQueen, SocketRef[] socketAt, int socketCount)
    {
        // A link joins two adjacent filled sockets whose values differ by exactly one. A run is a
        // maximal stretch of links sharing a direction; opposite directions (5-6-5) split into two
        // runs that overlap at the shared socket, which the tie-break below then separates.
        List<(int Start, int End)> runs = [];
        int runStart = -1;
        int direction = 0;

        for (int i = 0; i < socketCount - 1; i++)
        {
            int step = LinkStep(value, i);

            if (step != 0 && step == direction)
            {
                continue;   // extend the current run
            }

            if (runStart >= 0 && direction != 0)
            {
                runs.Add((runStart, i));   // close the run that ended at socket i
            }

            direction = step;
            runStart = step != 0 ? i : -1;
        }

        if (runStart >= 0 && direction != 0)
        {
            runs.Add((runStart, socketCount - 1));
        }

        // Longest first, then lowest socket index - the design's tie-break, applied by greedily
        // claiming each run's sockets and rejecting any later run that wants an already-claimed one.
        Dictionary<SocketRef, double> awarded = [];
        HashSet<int> claimed = [];

        foreach ((int start, int end) in runs.OrderByDescending(r => r.End - r.Start).ThenBy(r => r.Start))
        {
            int length = end - start + 1;
            bool hasNonQueen = Enumerable.Range(start, length).Any(i => !isQueen[i]);
            bool free = Enumerable.Range(start, length).All(i => !claimed.Contains(i));

            if (!hasNonQueen || !free)
            {
                continue;
            }

            double bonus = tuning.RunLinks.BonusForRunLength(length);
            for (int i = start; i <= end; i++)
            {
                claimed.Add(i);
                awarded[socketAt[i]] = bonus;
            }
        }

        return awarded;
    }

    /// <summary>+1 or -1 if sockets i and i+1 are both filled and one apart in value, else 0.</summary>
    private static int LinkStep(int?[] value, int i)
    {
        if (value[i] is not int a || value[i + 1] is not int b)
        {
            return 0;
        }

        return b - a is 1 or -1 ? b - a : 0;
    }

    private static IEnumerable<int[]> QueenValueCombinations(int queenCount)
    {
        if (queenCount == 0)
        {
            yield return [];
            yield break;
        }

        int[] current = new int[queenCount];
        for (int i = 0; i < queenCount; i++)
        {
            current[i] = 1;
        }

        while (true)
        {
            yield return (int[])current.Clone();

            int slot = queenCount - 1;
            while (slot >= 0 && current[slot] == 13)
            {
                current[slot] = 1;
                slot--;
            }

            if (slot < 0)
            {
                yield break;
            }

            current[slot]++;
        }
    }
}
