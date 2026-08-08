using Bastion.Core.Board;
using Bastion.Core.Config;

namespace Bastion.Core.Resolve;

/// <summary>
/// What a tower shoots at, given its standing order.
/// </summary>
/// <remarks>
/// <para>
/// <b>The default rule is NOT SPECIFIED BY THE DESIGN.</b> Only tie-breaking (spawn order) and the
/// two Focus alternatives are. Nearest-to-the-tower is the chosen default, because it keeps both
/// Focus modes meaningful: "prefer the leading target" and "prefer armored" are each a real
/// departure from it. A leading-target default would have made one of the design's own standing
/// orders a no-op.
/// </para>
/// <para>
/// Every step below breaks ties by spawn index, per docs/design/05-battlefield.md: "Ties resolve by
/// spawn order." That is stated once, here, and relied on everywhere.
/// </para>
/// </remarks>
internal static class Targeting
{
    /// <summary>
    /// Records every alive enemy currently inside the tower's window as having been in range.
    /// </summary>
    /// <remarks>
    /// Called every tick for every tower, <b>including ticks the tower is on cooldown</b>. That is
    /// the whole point: <see cref="LiveEnemy.WasEverInRange"/> feeds the leak-cause explanation
    /// (<c>NeverInRange</c> versus <c>OutDamaged</c>), and if it were only set while choosing a
    /// target it would depend on whether a tower happened to be ready when an enemy crossed its
    /// window - so a fast unit slipping through during a cooldown gap would be mislabelled as never
    /// having been reachable. The classification is diagnostic, not a contract number, but it is
    /// shown, so it should not lie.
    /// </remarks>
    internal static void MarkInRange(
        TowerState tower,
        double towerPosition,
        IReadOnlyList<LiveEnemy> enemies)
    {
        foreach (LiveEnemy enemy in enemies)
        {
            if (enemy.Alive && Math.Abs(enemy.Position - towerPosition) <= tower.Range)
            {
                enemy.WasEverInRange = true;
            }
        }
    }

    /// <summary>
    /// Picks a target, or null when the tower holds fire.
    /// </summary>
    /// <remarks>
    /// Holding fire is not the same as having no target: a tower under a Hold or trigger-on-group
    /// order stays off cooldown and fires the instant its condition is met. That is the whole value
    /// of a pre-committed conditional in a wave that takes no live input.
    /// </remarks>
    internal static LiveEnemy? Choose(
        TuningData tuning,
        TowerState tower,
        double towerPosition,
        IReadOnlyList<LiveEnemy> enemies)
    {
        // 1. In range and alive. The list is ordered by spawn index and stays that way throughout,
        //    so every "first match" below is already the spawn-order tie-break. WasEverInRange is
        //    not set here - MarkInRange owns it, and runs every tick regardless of cooldown.
        List<LiveEnemy> candidates = [];

        foreach (LiveEnemy enemy in enemies)
        {
            if (!enemy.Alive)
            {
                continue;
            }

            if (Math.Abs(enemy.Position - towerPosition) > tower.Range)
            {
                continue;
            }

            candidates.Add(enemy);
        }

        if (candidates.Count == 0)
        {
            return null;
        }

        StandingOrder order = tower.Order;

        if (order.IsDefault)
        {
            return Nearest(candidates, towerPosition);
        }

        // 2. Hold: fire only at enemies past a chosen socket.
        if (order.HoldPastPosition is double hold)
        {
            candidates = [.. candidates.Where(e => e.Position >= hold)];

            if (candidates.Count == 0)
            {
                return null;
            }
        }

        // 3. Trigger on group: a trap waits for a minimum number of enemies in radius.
        if (order.TriggerOnGroup)
        {
            LiveEnemy? cluster = DensestCluster(tuning, candidates);

            if (cluster is null)
            {
                return null;
            }

            candidates = [cluster];
        }

        // 4. Focus, layered over whatever survived.
        candidates = order.Focus switch
        {
            FocusMode.PreferArmored => PreferOrKeep(candidates, e => e.Type.FlatArmor > 0),
            FocusMode.PreferLeading => [Leading(candidates)],
            _ => candidates,
        };

        // 5. Default rule decides among the rest.
        return Nearest(candidates, towerPosition);
    }

    /// <summary>Smallest path distance to the socket; ties to the lowest spawn index.</summary>
    private static LiveEnemy Nearest(IReadOnlyList<LiveEnemy> candidates, double towerPosition)
    {
        LiveEnemy best = candidates[0];
        double bestDistance = Math.Abs(best.Position - towerPosition);

        foreach (LiveEnemy enemy in candidates)
        {
            double distance = Math.Abs(enemy.Position - towerPosition);

            // Strictly less: an equal distance keeps the earlier spawn index, since the list is
            // already in spawn order.
            if (distance < bestDistance)
            {
                best = enemy;
                bestDistance = distance;
            }
        }

        return best;
    }

    /// <summary>Furthest along the path; ties to the lowest spawn index.</summary>
    private static LiveEnemy Leading(IReadOnlyList<LiveEnemy> candidates)
    {
        LiveEnemy best = candidates[0];

        foreach (LiveEnemy enemy in candidates)
        {
            if (enemy.Position > best.Position)
            {
                best = enemy;
            }
        }

        return best;
    }

    /// <summary>
    /// Narrows to matching candidates, or leaves the list alone when none match.
    /// </summary>
    /// <remarks>
    /// A preference, not a restriction: a Focus-armored tower with no armored target in range still
    /// shoots. The design says "prefer", and a tower that idles beside a swarm because it is
    /// holding out for armor would be a trap rather than an order.
    /// </remarks>
    private static List<LiveEnemy> PreferOrKeep(List<LiveEnemy> candidates, Func<LiveEnemy, bool> predicate)
    {
        List<LiveEnemy> matching = [.. candidates.Where(predicate)];

        return matching.Count > 0 ? matching : candidates;
    }

    /// <summary>
    /// The candidate with the most neighbours within the trigger radius, or null if none reaches
    /// the threshold.
    /// </summary>
    private static LiveEnemy? DensestCluster(TuningData tuning, IReadOnlyList<LiveEnemy> candidates)
    {
        int required = tuning.StandingOrders.TriggerGroupMinEnemies;
        double radius = tuning.StandingOrders.TriggerGroupRadius;

        LiveEnemy? best = null;
        int bestCount = 0;

        foreach (LiveEnemy centre in candidates)
        {
            int count = candidates.Count(e => Math.Abs(e.Position - centre.Position) <= radius);

            // Strictly greater keeps the lowest spawn index on a tie.
            if (count > bestCount)
            {
                best = centre;
                bestCount = count;
            }
        }

        return bestCount >= required ? best : null;
    }
}
