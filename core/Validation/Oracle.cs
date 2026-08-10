using Bastion.Core.Board;
using Bastion.Core.Cards;
using Bastion.Core.Config;
using Bastion.Core.Diagnostics;
using Bastion.Core.Resolve;
using Bastion.Core.Wave;

namespace Bastion.Core.Validation;

/// <summary>
/// The three values the design forbids showing and the instrumentation needs.
/// </summary>
/// <remarks>
/// <para>
/// <b>Exact bust probability, stand and hit expected output, and combined utility</b>
/// (docs/prototype/VALIDATION.md § Instrumentation, docs/design/09-information-and-ui.md
/// § Debug-only information). Each is one arithmetic step from the oracle the pillars prohibit -
/// the player gets the marked rank display and reads the risk themselves.
/// </para>
/// <para>
/// Compiled out unless the build opts in, so in a player build this type's work is <b>absent from
/// the binary</b> rather than merely unreached. <see cref="For"/> returns null there, and the log's
/// <c>oracle</c> field is omitted entirely - which is the round trip worth checking when verifying
/// the gate is real rather than remembered.
/// </para>
/// <para>
/// "Output" here is <b>predicted leak, so lower is better.</b> Leak is what the resolver actually
/// contracts to (docs/design/05-battlefield.md § Implementation) and what a lane's stakes are paid
/// in. It is deliberately not board power times an engagement fraction - that estimate was withdrawn
/// and must not reappear behind a debug flag.
/// </para>
/// </remarks>
public static class Oracle
{
    /// <summary>
    /// The oracle reading for a state, or null in a player build.
    /// </summary>
    public static OracleRecord? For(WaveSession session)
    {
        ArgumentNullException.ThrowIfNull(session);

#if BASTION_DEBUG
        // Only the draw decision has a hit and a stand to compare. Anywhere else there is no choice
        // for a utility to be about, and inventing one would put noise in the column.
        if (session.Phase != WavePhase.DrawDecision)
        {
            return null;
        }

        return Compute(session);
#else
        _ = session;
        return null;
#endif
    }

#if BASTION_DEBUG
    private static OracleRecord Compute(WaveSession session)
    {
        DebugGate.RequireInstrumented(nameof(Oracle));

        IReadOnlyDictionary<Rank, int> pile = session.Shoe.RemainingComposition();
        int total = pile.Values.Sum();

        double bustWeight = pile
            .Where(entry => session.Hand.Hit(entry.Key).IsBust)
            .Sum(entry => entry.Value);

        double standLeak = TotalLeak(session.Stand().Forecast());

        double hitLeak = 0.0;
        double weighed = 0.0;

        foreach ((Rank rank, int count) in pile.Where(e => e.Value > 0))
        {
            if (BestLeakAfterDrawing(session, rank) is not { } leak)
            {
                continue;
            }

            hitLeak += leak * count;
            weighed += count;
        }

        double expectedHitLeak = weighed > 0 ? hitLeak / weighed : standLeak;

        return new OracleRecord
        {
            BustProbability = total > 0 ? bustWeight / total : 0.0,
            StandExpectedLeak = standLeak,
            HitExpectedLeak = expectedHitLeak,

            // Positive means hitting is expected to leak less, i.e. hitting is the better play.
            // This is the single combined verdict the player must never be shown.
            CombinedUtility = standLeak - expectedHitLeak,
        };
    }

    /// <summary>
    /// The best leak reachable if the next card were <paramref name="rank"/>, over every placement.
    /// </summary>
    /// <remarks>
    /// Optimal play, not a policy: the counterfactual is only meaningful against the best the player
    /// could do with the card, and a fixed placement rule would measure the rule instead. Both
    /// families are tried, because family is locked at placement and is part of the same decision.
    /// A busting card is destroyed and never placed, so that branch has nothing to search.
    /// </remarks>
    private static double? BestLeakAfterDrawing(WaveSession session, Rank rank)
    {
        if (session.Shoe.WithNextCard(rank) is not { } stacked)
        {
            return null;
        }

        WaveSession afterHit = session.WithShoe(stacked).Hit();

        if (afterHit.Phase == WavePhase.BustLocked)
        {
            return TotalLeak(afterHit.Forecast());
        }

        double? best = null;

        foreach (SocketRef socket in AllSockets(session.Tuning))
        {
            foreach (Family family in new[] { Family.Club, Family.Spade })
            {
                double leak = TotalLeak(afterHit.Place(family, socket).Stand().Forecast());

                best = best is null ? leak : Math.Min(best.Value, leak);
            }
        }

        return best;
    }

    private static double TotalLeak(FinalForecast forecast) =>
        forecast.Lanes.Sum(lane => lane.PredictedDamage);

    private static IEnumerable<SocketRef> AllSockets(TuningData tuning)
    {
        for (int lane = 0; lane < tuning.Geometry.Lanes; lane++)
        {
            for (int socket = 0; socket < tuning.Geometry.SocketPositions.Count; socket++)
            {
                yield return SocketRef.InLane(lane, socket);
            }
        }

        yield return SocketRef.Junction;
    }
#endif
}

/// <summary>
/// Oracle-tier values for one state. Never rendered, only logged.
/// </summary>
/// <remarks>
/// This type exists in every build - a nullable field is easier to reason about than a conditionally
/// compiled one - but <see cref="Oracle.For"/> only ever produces an instance in an instrumented
/// build, so the field is absent from a player build's log.
/// </remarks>
public sealed record OracleRecord
{
    /// <summary>Exact, from the remaining pile. The player sees marked ranks and no percentage.</summary>
    public required double BustProbability { get; init; }

    /// <summary>Predicted total leak if the player stands here. Lower is better.</summary>
    public required double StandExpectedLeak { get; init; }

    /// <summary>Predicted total leak from hitting, averaged over the pile at optimal placement.</summary>
    public required double HitExpectedLeak { get; init; }

    /// <summary>Stand minus hit: positive favours hitting. The forbidden combined verdict.</summary>
    public required double CombinedUtility { get; init; }
}
