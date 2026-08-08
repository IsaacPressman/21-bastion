using Bastion.Core.Config;

namespace Bastion.Core.Board;

/// <summary>
/// The board the resolver runs against: every placed tower, plus where the army enters.
/// </summary>
/// <remarks>
/// <para>
/// Immutable. docs/ARCHITECTURE.md: "the resolver consumes an immutable board state and returns a
/// timeline of events plus per-lane outcomes." Nothing in the resolver may mutate this, because the
/// same board is resolved several times per forecast - once live and once per lane counterfactual -
/// and a mutation would make the second run disagree with the first.
/// </para>
/// <para>
/// Note what is <b>not</b> here: no hand, no total, no shoe, no phase. The board is what the
/// resolver needs and nothing else, so a fixture can build one directly without a wave in progress.
/// </para>
/// </remarks>
public sealed record BoardState
{
    private BoardState(IReadOnlyList<TowerState> towers, double entry)
    {
        Towers = towers;
        Entry = entry;
    }

    /// <summary>Placed towers, in firing order (lanes by depth, junction last).</summary>
    public IReadOnlyList<TowerState> Towers { get; }

    /// <summary>Path position the army enters at, from the March Clock.</summary>
    public double Entry { get; }

    /// <summary>An empty board at the default entry. The baseline every counterfactual leans on.</summary>
    public static BoardState Empty(TuningData tuning)
    {
        ArgumentNullException.ThrowIfNull(tuning);

        return new BoardState([], tuning.Geometry.DefaultEntry);
    }

    /// <summary>
    /// Builds a board, rejecting anything the placement rules could not have produced.
    /// </summary>
    /// <exception cref="ArgumentException">
    /// If two towers share a socket, a socket index is off the board, or the entry point is off the
    /// path.
    /// </exception>
    public static BoardState Create(TuningData tuning, IEnumerable<TowerState> towers, double entry)
    {
        ArgumentNullException.ThrowIfNull(tuning);
        ArgumentNullException.ThrowIfNull(towers);

        List<TowerState> placed = [.. towers];

        foreach (TowerState tower in placed)
        {
            if (tower.Socket.IsJunction)
            {
                continue;
            }

            if (tower.Socket.LaneIndex >= tuning.Geometry.Lanes)
            {
                throw new ArgumentException(
                    $"Tower at {tower.Socket} is outside the {tuning.Geometry.Lanes} tuned lanes.", nameof(towers));
            }

            if (tower.Socket.SocketIndex >= tuning.Geometry.SocketPositions.Count)
            {
                throw new ArgumentException(
                    $"Tower at {tower.Socket} is outside the {tuning.Geometry.SocketPositions.Count} tuned sockets per lane.", nameof(towers));
            }
        }

        // Board width is capped by socket count, which is what makes forced replacement bite.
        List<SocketRef> duplicates = [.. placed
            .GroupBy(t => t.Socket)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)];

        if (duplicates.Count > 0)
        {
            throw new ArgumentException(
                $"Two towers occupy the same socket: {string.Join(", ", duplicates)}.", nameof(towers));
        }

        if (entry < 0 || entry > tuning.Geometry.PathLength)
        {
            throw new ArgumentException(
                $"Entry {entry} is outside the path 0..{tuning.Geometry.PathLength}.", nameof(entry));
        }

        // Sorted once, here, so every downstream iteration is in firing order without having to
        // remember to sort. Ordering is part of the determinism contract, not a presentation detail.
        placed.Sort((a, b) => a.Socket.FiringOrder.CompareTo(b.Socket.FiringOrder));

        return new BoardState(placed, entry);
    }

    /// <summary>
    /// The towers that shoot into one lane: that lane's sockets, plus the shared junction.
    /// </summary>
    /// <remarks>
    /// <b>This is why lanes resolve independently in Milestone 1.</b> Enemies never leave their
    /// lane, and a junction tower fires into both at a reduced contribution each - so a lane's
    /// outcome depends on nothing outside it. The one behaviour that would couple two lanes is the
    /// Jack-rank Skirmisher changing lanes at the junction, which is out of Milestone 1 scope and
    /// stubbed in <c>Resolve/UnmodelledBehaviour</c>. If that lands, this assumption is the thing
    /// it breaks.
    /// </remarks>
    public IReadOnlyList<TowerState> TowersFiringInto(int laneIndex) =>
        [.. Towers.Where(t => t.Socket.IsJunction || t.Socket.LaneIndex == laneIndex)];

    /// <summary>Whether any tower covers the given lane. Used to explain an undefended leak.</summary>
    public bool IsLaneUndefended(int laneIndex) => TowersFiringInto(laneIndex).Count == 0;

    /// <summary>The same board with the entry point moved, as the March Clock advances it.</summary>
    public BoardState WithEntry(double entry) => new(Towers, entry);
}
