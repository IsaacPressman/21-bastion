namespace Bastion.Core.Board;

/// <summary>
/// Which socket a tower occupies: a lane socket, or the one shared junction.
/// </summary>
/// <remarks>
/// <para>
/// The junction is <b>one</b> socket, not one per lane. It fires into both lanes at reduced
/// contribution and is adjacent to neither for run purposes - a run island that buys breadth and
/// forfeits synergy (docs/design/05-battlefield.md).
/// </para>
/// <para>
/// Modelled as a struct with a sentinel lane rather than a class hierarchy so it can be a
/// dictionary key and compare by value, which the resolver's deterministic ordering relies on.
/// </para>
/// </remarks>
public readonly record struct SocketRef
{
    private const int JunctionLane = -1;

    private SocketRef(int laneIndex, int socketIndex)
    {
        LaneIndex = laneIndex;
        SocketIndex = socketIndex;
    }

    /// <summary>Lane index, or -1 for the junction.</summary>
    public int LaneIndex { get; }

    /// <summary>Socket index within the lane, or -1 for the junction.</summary>
    public int SocketIndex { get; }

    public bool IsJunction => LaneIndex == JunctionLane;

    /// <summary>The shared junction socket.</summary>
    public static SocketRef Junction { get; } = new(JunctionLane, JunctionLane);

    public static SocketRef InLane(int laneIndex, int socketIndex)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(laneIndex);
        ArgumentOutOfRangeException.ThrowIfNegative(socketIndex);

        return new SocketRef(laneIndex, socketIndex);
    }

    /// <summary>
    /// Sort key that puts lane sockets first in lane-then-depth order and the junction last.
    /// </summary>
    /// <remarks>
    /// Firing order has to be fixed and stated somewhere, because two towers that would each kill
    /// the same enemy produce different timelines depending which fires first. This is that
    /// statement. It is arbitrary; it is not allowed to be incidental.
    /// </remarks>
    public (int Lane, int Socket) FiringOrder => IsJunction ? (int.MaxValue, int.MaxValue) : (LaneIndex, SocketIndex);

    public override string ToString() => IsJunction ? "junction" : $"lane {LaneIndex} socket {SocketIndex}";
}
