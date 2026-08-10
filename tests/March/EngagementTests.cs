using Bastion.Core.Config;
using Bastion.Core.March;

namespace Bastion.Core.Tests.March;

/// <summary>
/// Engagement against the tables published in docs/design/03-march-clock.md.
/// </summary>
/// <remarks>
/// Written first and deliberately: the equivalent arithmetic done by hand in Revision 7 was wrong
/// for the five-card case, and the error propagated into output estimates built on top of it.
/// </remarks>
public sealed class EngagementTests
{
    private static readonly TuningData Tuning = TuningLoader.LoadFromRepositoryRoot();

    /// <summary>Every socket occupied, each at its own tuned range.</summary>
    private static IEnumerable<(double Position, double Range)> AllSockets() =>
        Tuning.Geometry.SocketPositions.Select((p, i) => (p, Tuning.Geometry.RangeBySocket[i]));

    private static double TotalAt(double entry) =>
        Engagement.Total(AllSockets(), entry, Tuning.Geometry.PathLength);

    [Fact]
    public void Full_occupancy_at_entry_zero_gives_seventeen()
    {
        // 7.0 + 6.0 + 4.0. Was a flat 18.0 before the geometry remedy; the total mattering less
        // than its distribution is the whole point of that change.
        Assert.Equal(17.0, TotalAt(0.0), precision: 6);
    }

    [Theory]
    [InlineData(0, 7.0)]   // 3.0 +/- 4.0, clipped at the spawn end
    [InlineData(1, 6.0)]   // 6.0 +/- 3.0
    [InlineData(2, 4.0)]   // 9.0 +/- 2.0, clipped at the Bastion end
    public void Socket_windows_differ_by_depth_at_entry_zero(int socketIndex, double expected)
    {
        // The remedy for deep-placement dominance (docs/ROADMAP.md Open Decision 2). Under the
        // former flat range every socket opened with an identical 6.0 window, so the first unit of
        // advancement could only ever come out of the forward one - and deep placement paid nothing
        // for it. Forward sockets now start ahead and have more to lose, which is what makes the
        // trade a decision rather than a default.
        double window = Engagement.ForSocket(
            Tuning.Geometry.SocketPositions[socketIndex],
            Tuning.Geometry.RangeBySocket[socketIndex],
            entry: 0.0,
            Tuning.Geometry.PathLength);

        Assert.Equal(expected, window, precision: 6);
    }

    [Theory]
    [InlineData(1.5, 15.5)]   // 3rd card
    [InlineData(4.0, 12.0)]   // 4th card
    [InlineData(7.5, 5.0)]    // 5th card
    [InlineData(1.0, 16.0)]   // 4-card 21
    [InlineData(4.5, 11.0)]   // 5-card 21
    [InlineData(9.0, 2.0)]    // the clamp: brutal but survivable
    [InlineData(6.0, 8.0)]    // 6-card 21, after the pullback off the clamp
    public void Total_engagement_matches_the_published_tables(double entry, double expected)
    {
        Assert.Equal(expected, TotalAt(entry), precision: 6);
    }

    [Fact]
    public void The_fifth_card_still_costs_roughly_seventy_percent()
    {
        // The remedy changed where engagement sits, not how hard the clock bites: the fifth card
        // cost -67% under the flat geometry and -71% now. That matters, because the three march
        // arms are pre-committed test arms - a geometry that quietly softened the curve would have
        // answered the fifth-card question before the playtest got to ask it.
        double remaining = TotalAt(7.5) / TotalAt(0.0);

        Assert.Equal(0.294118, remaining, precision: 5);
        Assert.InRange(1.0 - remaining, 0.65, 0.75);
    }

    [Fact]
    public void A_socket_window_never_extends_past_the_end_of_the_path()
    {
        // A socket at 9.0 would reach 13.0 on range 4.0, past the end of a 12.0 path. Range is
        // tunable per socket and carries a face-card allowance on top, so this clip is what stops
        // either of those manufacturing engagement beyond the Bastion.
        double window = Engagement.ForSocket(
            socketPosition: 9.0, range: 4.0, entry: 0.0, pathLength: 12.0);

        Assert.Equal(7.0, window, precision: 6);   // 5.0 to 12.0, not 5.0 to 13.0
    }

    [Fact]
    public void Advancement_still_eats_forward_sockets_first()
    {
        // The direction Revision 7 stated backwards. It is a fact about the path, not about the
        // tuning, so the geometry remedy does not repeal it: entry advances from the spawn side, so
        // it always reaches the forward socket's window before the rear one's.
        double forwardLost = WindowAt(0, entry: 0.0) - WindowAt(0, entry: 4.0);
        double rearLost = WindowAt(2, entry: 0.0) - WindowAt(2, entry: 4.0);

        Assert.True(forwardLost > rearLost,
            "Advancement must degrade forward sockets before rear ones.");
        Assert.Equal(0.0, rearLost, precision: 6);
    }

    [Fact]
    public void The_forward_socket_starts_far_enough_ahead_to_be_worth_the_tax()
    {
        // The other half of the remedy, and the half that makes placement a decision. Advancement
        // costs the forward socket everything and the rear socket nothing (above), so under the
        // former flat range deep placement was weakly dominant the moment entry left zero
        // (docs/ROADMAP.md Open Decision 2). Forward sockets now open with a wider window, so a
        // short hand is better off forward and a long one better off deep - the crossover is the
        // decision.
        Assert.True(WindowAt(0, entry: 0.0) > WindowAt(2, entry: 0.0),
            "A short hand must be better off placing forward.");

        Assert.True(WindowAt(2, entry: 7.5) > WindowAt(0, entry: 7.5),
            "A hand that paid for a fifth card must be better off placing deep.");
    }

    /// <summary>One socket's window at its own tuned range.</summary>
    private static double WindowAt(int socketIndex, double entry) => Engagement.ForSocket(
        Tuning.Geometry.SocketPositions[socketIndex],
        Tuning.Geometry.RangeBySocket[socketIndex],
        entry,
        Tuning.Geometry.PathLength);

    [Fact]
    public void An_empty_board_has_no_engagement()
    {
        Assert.Equal(0.0, Engagement.Total([], 0.0, 12.0), precision: 6);
    }

    [Theory]
    [InlineData(3.0, 3.0, 0.0, 0.0, 6.0)]    // front socket, entry 0: 0 to 6
    [InlineData(9.0, 3.0, 0.0, 6.0, 12.0)]   // rear socket, entry 0: 6 to 12
    [InlineData(9.0, 4.0, 0.0, 5.0, 12.0)]   // face range clipped at the path end, not 13
    [InlineData(3.0, 3.0, 4.0, 4.0, 6.0)]    // entry advanced past the socket's near edge
    public void Window_endpoints_are_the_engagement_interval(
        double socket, double range, double entry, double first, double last)
    {
        (double gotFirst, double gotLast) = Engagement.WindowForSocket(socket, range, entry, 12.0);

        Assert.Equal(first, gotFirst, precision: 6);
        Assert.Equal(last, gotLast, precision: 6);
    }

    [Theory]
    [InlineData(3.0, 3.0, 0.0)]
    [InlineData(9.0, 3.0, 4.0)]
    [InlineData(3.0, 3.0, 9.0)]   // socket fully behind entry: window collapses, length clamps to 0
    public void Window_length_agrees_with_ForSocket(double socket, double range, double entry)
    {
        (double first, double last) = Engagement.WindowForSocket(socket, range, entry, 12.0);
        double byLength = Engagement.ForSocket(socket, range, entry, 12.0);

        Assert.Equal(byLength, Math.Max(0.0, last - first), precision: 6);
    }

    [Fact]
    public void A_socket_fully_behind_the_entry_point_contributes_nothing()
    {
        Assert.Equal(0.0, Engagement.ForSocket(3.0, 3.0, entry: 9.0, pathLength: 12.0), precision: 6);
    }
}
