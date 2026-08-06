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

    private static double TotalAt(double entry) => Engagement.Total(
        Tuning.Geometry.SocketPositions,
        Tuning.Geometry.DefaultRange,
        entry,
        Tuning.Geometry.PathLength);

    [Fact]
    public void Full_occupancy_at_entry_zero_gives_eighteen()
    {
        Assert.Equal(18.0, TotalAt(0.0), precision: 6);
    }

    [Theory]
    [InlineData(3.0, 6.0)]
    [InlineData(6.0, 6.0)]
    [InlineData(9.0, 6.0)]
    public void Every_socket_window_is_six_units_at_entry_zero(double socket, double expected)
    {
        double window = Engagement.ForSocket(
            socket, Tuning.Geometry.DefaultRange, entry: 0.0, Tuning.Geometry.PathLength);

        Assert.Equal(expected, window, precision: 6);
    }

    [Theory]
    [InlineData(1.5, 16.5)]   // 3rd card
    [InlineData(4.0, 13.0)]   // 4th card
    [InlineData(7.5, 6.0)]    // 5th card - Revision 7 reported 7.5 here; it is 6.0
    [InlineData(1.0, 17.0)]   // 4-card 21
    [InlineData(4.5, 12.0)]   // 5-card 21
    [InlineData(9.0, 3.0)]    // the clamp: brutal but survivable
    [InlineData(6.0, 9.0)]    // 6-card 21, after the pullback off the clamp
    public void Total_engagement_matches_the_published_tables(double entry, double expected)
    {
        Assert.Equal(expected, TotalAt(entry), precision: 6);
    }

    [Fact]
    public void The_fifth_card_costs_sixty_seven_percent_not_fifty_eight()
    {
        double remaining = TotalAt(7.5) / TotalAt(0.0);

        Assert.Equal(0.333333, remaining, precision: 5);

        // Revision 7's arithmetic dropped the path-length term and summed the rear socket's full
        // 6.0 window against only 4.5 units of remaining path.
        Assert.NotEqual(0.42, remaining, precision: 2);
    }

    [Fact]
    public void A_socket_window_never_extends_past_the_end_of_the_path()
    {
        // The rear socket sits at 9.0 with range 3.0, so its window would reach 12.0 exactly.
        // Any range increase must not manufacture engagement beyond the Bastion.
        double window = Engagement.ForSocket(
            socketPosition: 9.0, range: 4.0, entry: 0.0, pathLength: 12.0);

        Assert.Equal(7.0, window, precision: 6);   // 5.0 to 12.0, not 5.0 to 13.0
    }

    [Fact]
    public void Advancement_eats_forward_sockets_first()
    {
        // The direction Revision 7 stated backwards, and the reason deep placement may be weakly
        // dominant. At entry 4.0 the forward socket has lost most of its window while the rear
        // socket has lost none.
        double forward = Engagement.ForSocket(3.0, 3.0, entry: 4.0, pathLength: 12.0);
        double rear = Engagement.ForSocket(9.0, 3.0, entry: 4.0, pathLength: 12.0);

        Assert.Equal(2.0, forward, precision: 6);
        Assert.Equal(6.0, rear, precision: 6);
        Assert.True(rear > forward, "Advancement must degrade forward sockets before rear ones.");
    }

    [Fact]
    public void An_empty_board_has_no_engagement()
    {
        Assert.Equal(0.0, Engagement.Total([], 3.0, 0.0, 12.0), precision: 6);
    }

    [Fact]
    public void A_socket_fully_behind_the_entry_point_contributes_nothing()
    {
        Assert.Equal(0.0, Engagement.ForSocket(3.0, 3.0, entry: 9.0, pathLength: 12.0), precision: 6);
    }
}
