namespace Bastion.Core.March;

/// <summary>
/// Path distance over which a tower can fire, given where the enemy entry point sits.
/// </summary>
/// <remarks>
/// <para>
/// Early Milestone 1 slice. Pure and closed-form, with published tables to check against - which
/// is why it is worth writing first: the equivalent arithmetic done by hand in Revision 7 was
/// wrong for the five-card case, and the error survived into output estimates that were built on
/// top of it.
/// </para>
/// <para>
/// <b>Summed engagement is explanatory, not a balance number.</b> It treats sockets as fungible
/// when they are not - three units taken from a 5.0-power King is not three units taken from a
/// 1.6-power two. Never multiply board power by an engagement fraction to estimate output.
/// Balance comes from the resolver. See docs/design/03-march-clock.md.
/// </para>
/// </remarks>
public static class Engagement
{
    /// <summary>
    /// Engagement for one occupied socket.
    /// </summary>
    /// <remarks>
    /// The <c>Min(socket + range, pathLength)</c> term is the one Revision 7 dropped: it summed
    /// the rear socket's full window against a remaining path shorter than the window itself.
    /// </remarks>
    public static double ForSocket(double socketPosition, double range, double entry, double pathLength)
    {
        double firstContact = Math.Max(socketPosition - range, entry);
        double lastContact = Math.Min(socketPosition + range, pathLength);

        return Math.Max(0.0, lastContact - firstContact);
    }

    /// <summary>
    /// Engagement summed across occupied sockets. Empty sockets contribute nothing.
    /// </summary>
    public static double Total(
        IEnumerable<double> occupiedSocketPositions,
        double range,
        double entry,
        double pathLength)
    {
        ArgumentNullException.ThrowIfNull(occupiedSocketPositions);

        return occupiedSocketPositions.Sum(position => ForSocket(position, range, entry, pathLength));
    }
}
