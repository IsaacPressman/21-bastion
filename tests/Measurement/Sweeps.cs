using Bastion.Core.Board;
using Bastion.Core.Config;

namespace Bastion.Core.Tests.Measurement;

/// <summary>
/// Scaffolding shared by the resolver measurement sweeps.
/// </summary>
/// <remarks>
/// Both sweeps enumerate socket choices and both write a CSV to <c>telemetry/</c>. Keeping one copy
/// matters more than usual here: two sweeps that enumerated sockets differently would produce
/// numbers that look comparable and are not.
/// </remarks>
internal static class Sweeps
{
    /// <summary>Every socket a tower can occupy: the lane sockets, then the shared junction.</summary>
    internal static IReadOnlyList<SocketRef> AllSockets(TuningData tuning)
    {
        List<SocketRef> sockets = [];

        for (int lane = 0; lane < tuning.Geometry.Lanes; lane++)
        {
            for (int socket = 0; socket < tuning.Geometry.SocketPositions.Count; socket++)
            {
                sockets.Add(SocketRef.InLane(lane, socket));
            }
        }

        sockets.Add(SocketRef.Junction);

        return sockets;
    }

    /// <summary>Path position of a socket. Deeper means further from the spawn side.</summary>
    internal static double DepthOf(TuningData tuning, SocketRef socket) =>
        socket.IsJunction
            ? tuning.Towers.JunctionPathPosition
            : tuning.Geometry.SocketPositions[socket.SocketIndex];

    /// <summary>Every choice of <paramref name="size"/> sockets, in a fixed order.</summary>
    internal static IEnumerable<SocketRef[]> Combinations(IReadOnlyList<SocketRef> sockets, int size)
    {
        if (size > sockets.Count)
        {
            yield break;
        }

        int[] indices = [.. Enumerable.Range(0, size)];

        while (true)
        {
            yield return [.. indices.Select(i => sockets[i])];

            int pivot = size - 1;

            while (pivot >= 0 && indices[pivot] == sockets.Count - size + pivot)
            {
                pivot--;
            }

            if (pivot < 0)
            {
                yield break;
            }

            indices[pivot]++;

            for (int i = pivot + 1; i < size; i++)
            {
                indices[i] = indices[i - 1] + 1;
            }
        }
    }

    /// <summary>Writes a sweep's output under <c>telemetry/</c>.</summary>
    internal static void Write(string fileName, string contents)
    {
        string path = Path.Combine(TuningLoader.FindRepositoryRoot(), "telemetry", fileName);

        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, contents);
    }
}
