using System.Reflection;
using Bastion.Core.Config;
using Bastion.Core.Validation;
using Bastion.Core.Wave;

namespace Bastion.Core.Tests.Validation;

/// <summary>
/// The instrumentation record honours the two corrections that make it worth having.
/// </summary>
/// <remarks>
/// docs/prototype/VALIDATION.md notes two changes from Revision 7: engagement is logged <b>per
/// socket</b>, not as a single number, and the forecast comparison is against the <b>Final</b>
/// Forecast. Both are easy to undo by accident later - a helpful summed field, a forecast read in
/// the wrong phase - and neither would break anything visibly. They are checked here instead.
/// </remarks>
public sealed class SessionSnapshotTests
{
    private static readonly Battery Battery = BatteryLoader.LoadFromRepositoryRoot();
    private static readonly TuningData Tuning = Battery.Apply(TuningLoader.LoadFromRepositoryRoot());

    private static WaveSession Open(string id) =>
        (Battery.Find(id) ?? throw new InvalidOperationException(id)).Open(Tuning);

    [Fact]
    public void Engagement_is_recorded_per_socket_and_never_summed()
    {
        StateRecord record = SessionSnapshot.Capture(Open("1-severe"));

        // Every socket appears, occupied or not - an empty socket's window is what makes taking it
        // worth something, so leaving them out would lose the placement decision entirely.
        Assert.Equal(Tuning.Geometry.TotalSockets, record.Sockets.Count);
        Assert.All(record.Sockets, socket => Assert.True(socket.WindowRemaining >= 0.0));

        // And no property anywhere on the record offers the withdrawn summed scalar. The figure
        // treats sockets as interchangeable when they are not, and it is doubly wrong now that
        // range varies by socket (docs/design/03-march-clock.md).
        string[] suspicious = [.. typeof(StateRecord).GetProperties()
            .Concat(typeof(MarchRecord).GetProperties())
            .Select(p => p.Name)
            .Where(name => name.Contains("Engagement", StringComparison.OrdinalIgnoreCase)
                        || name.Contains("TotalWindow", StringComparison.OrdinalIgnoreCase))];

        Assert.Empty(suspicious);
    }

    [Fact]
    public void The_next_step_cost_is_shown_as_what_it_takes_from_each_socket()
    {
        // The design shows march cost on the lane, never as one number - "which socket windows the
        // next march step would cut into". The record carries the same pair the screen does.
        StateRecord record = SessionSnapshot.Capture(Open("1-severe"));

        Assert.All(record.Sockets, socket =>
            Assert.True(socket.WindowAfterNextStep <= socket.WindowRemaining,
                "A march step can only ever take window away."));

        Assert.Contains(record.Sockets, socket => socket.WindowAfterNextStep < socket.WindowRemaining);
    }

    [Theory]
    [InlineData("1-severe", SessionSnapshot.RevealedForce)]
    [InlineData("10-onemove", SessionSnapshot.CombatContract)]
    [InlineData("9-blindfamily", SessionSnapshot.RevealedForce)]
    public void The_lane_reading_names_which_forecast_it_holds(string id, string expected)
    {
        // The two forecasts are different claims and the log has to say which one it has, for the
        // same reason the UI must (docs/design/09-information-and-ui.md). A Visible Threat filed
        // under the Final Forecast's name would corrupt the result-versus-forecast comparison.
        StateRecord record = SessionSnapshot.Capture(Open(id));

        Assert.Equal(expected, record.LaneReading);
    }

    [Fact]
    public void A_state_awaiting_placement_records_the_revealed_force_it_was_read_against()
    {
        // Milestone 6: the revealed force is legal before the card goes down, because Read and
        // Diagnose precede Commit (docs/design/01-core-loop.md § The tactical loop). This is the
        // reading a player forms an intention from, so a log that omitted it could not answer
        // whether they had one - which is the milestone's whole success criterion.
        StateRecord record = SessionSnapshot.Capture(Open("9-blindfamily"));

        Assert.Equal(SessionSnapshot.RevealedForce, record.LaneReading);
        Assert.NotEmpty(record.Lanes);
    }

    [Fact]
    public void A_reading_and_its_lanes_never_disagree_about_whether_one_was_taken()
    {
        // Lanes are reported if and only if a contract was named. A record carrying lanes under
        // "none", or naming a contract and then reporting nothing, would leave the log claiming a
        // reading it does not hold - which is the same failure as rendering one forecast in the
        // other's slot, arriving through the log instead of the screen.
        foreach (BatteryFixture fixture in Battery.Fixtures)
        {
            StateRecord record = SessionSnapshot.Capture(fixture.Open(Tuning));

            Assert.Equal(record.LaneReading == SessionSnapshot.NoReading, record.Lanes.Count == 0);
        }
    }

    [Fact]
    public void The_pile_marks_busting_ranks_and_carries_no_percentage()
    {
        StateRecord record = SessionSnapshot.Capture(Open("7-onlyrank"));

        Assert.Equal(1, record.Pile.Count(entry => entry.ReachesTwentyOne && entry.Remaining > 0));
        Assert.Contains(record.Pile, entry => entry.WouldBust);

        // The player-facing half of the record is the marked pile, not a probability. The exact
        // figure exists only on the oracle, which a player build does not compile.
        Assert.DoesNotContain(
            typeof(PileEntry).GetProperties().Select(p => p.Name),
            name => name.Contains("Probability", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void The_dealer_upcard_is_recorded_as_a_unit_as_well_as_a_rank()
    {
        // A success criterion is that players read the upcard as a unit on the field, not a number,
        // so the log has to carry the thing they were actually looking at.
        StateRecord record = SessionSnapshot.Capture(Open("8-king"));

        Assert.Equal("K", record.Dealer.Upcard);
        Assert.Equal("siege_engine", record.Dealer.UpcardUnit);
    }

    [Fact]
    public void The_oracle_is_absent_unless_the_build_opted_in()
    {
        // The gate is compile-time, so this test reads differently in the two builds - and that is
        // the point being pinned: an instrumented run must produce the values, and a player build
        // must not be able to, even by calling directly.
        OracleRecord? oracle = Oracle.For(Open("1-severe"));

        // Copied to a local so the other arm is not folded away as unreachable at compile time.
        bool instrumented = Bastion.Core.Diagnostics.DebugGate.IsEnabled;

        if (instrumented)
        {
            Assert.NotNull(oracle);
            Assert.InRange(oracle!.BustProbability, 0.0, 1.0);
        }
        else
        {
            Assert.Null(oracle);
        }
    }

    [Fact]
    public void The_snapshot_carries_the_arm_it_was_taken_under()
    {
        // Comparing arms is the entire exercise, so every line has to say which one it came from.
        TuningData armA = Tuning with { March = Tuning.March with { ActivePreset = "A" } };

        BatteryFixture fixture = Battery.Find("1-severe")!;

        Assert.Equal("A", SessionSnapshot.Capture(fixture.Open(armA)).Arm);
        Assert.Equal(Tuning.March.ActivePreset, SessionSnapshot.Capture(fixture.Open(Tuning)).Arm);
    }
}
