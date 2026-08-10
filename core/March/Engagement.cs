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
        (double first, double last) = WindowForSocket(socketPosition, range, entry, pathLength);

        return Math.Max(0.0, last - first);
    }

    /// <summary>
    /// The path interval a socket can fire over, as its two endpoints.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Pure geometry: where the window starts and ends on the path, not how much output it yields.
    /// The presentation draws this on the lane so the player sees <b>which socket windows the next
    /// march step cuts into</b> - the entry advances, the window's near end moves with it, and the
    /// shaded slice between the two entries is what the step costs that socket
    /// (docs/design/09-information-and-ui.md § March cost is shown on the lane, not as a scalar).
    /// </para>
    /// <para>
    /// This is emphatically <b>not</b> the withdrawn effective-output estimate. It returns an
    /// interval, never power multiplied by a length, so there is nothing here to sum into a fungible
    /// board number. <see cref="ForSocket"/> is this interval's length, clamped at zero.
    /// </para>
    /// </remarks>
    public static (double First, double Last) WindowForSocket(
        double socketPosition, double range, double entry, double pathLength)
    {
        double firstContact = Math.Max(socketPosition - range, entry);
        double lastContact = Math.Min(socketPosition + range, pathLength);

        return (firstContact, lastContact);
    }

    /// <summary>
    /// Engagement summed across occupied sockets. Empty sockets contribute nothing.
    /// </summary>
    /// <remarks>
    /// Each socket brings its own range: range varies by position (the geometry remedy for
    /// deep-placement dominance, docs/ROADMAP.md Open Decision 2), so a single scalar range here
    /// would silently average sockets that are no longer alike. Explanatory only - see the type
    /// remarks on why this figure must never be multiplied by board power.
    /// </remarks>
    public static double Total(
        IEnumerable<(double Position, double Range)> occupiedSockets,
        double entry,
        double pathLength)
    {
        ArgumentNullException.ThrowIfNull(occupiedSockets);

        return occupiedSockets.Sum(s => ForSocket(s.Position, s.Range, entry, pathLength));
    }
}
