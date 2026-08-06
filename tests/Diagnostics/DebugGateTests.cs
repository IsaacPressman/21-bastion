using Bastion.Core.Diagnostics;

namespace Bastion.Core.Tests.Diagnostics;

/// <summary>
/// The oracle tier behaves coherently in both build modes.
/// </summary>
/// <remarks>
/// These assertions are written against whichever mode the current build used, because
/// <see cref="DebugGate"/> compiles into Bastion.Core and the test project cannot flip it on
/// Bastion.Core's behalf. Run both:
/// <code>
/// dotnet test                                   # player-shaped: oracle compiled out
/// dotnet test -p:BastionInstrumentation=true    # instrumented: oracle compiled in
/// </code>
/// </remarks>
public sealed class DebugGateTests
{
    [Fact]
    public void Oracle_only_computes_exactly_when_instrumentation_is_compiled_in()
    {
        bool computed = false;

        double? result = DebugGate.OracleOnly(() =>
        {
            computed = true;
            return 0.42;
        });

        // Copied to a local so the branches are not folded away as unreachable at compile time.
        bool instrumented = DebugGate.IsEnabled;

        if (instrumented)
        {
            Assert.True(computed);
            Assert.Equal(0.42, result);
        }
        else
        {
            // Not merely discarded - never evaluated. A player build does not pay for, or
            // contain, the computation.
            Assert.False(computed);
            Assert.Null(result);
        }
    }

    [Fact]
    public void Requiring_instrumentation_throws_only_in_a_player_build()
    {
        // Copied to a local so the branches are not folded away as unreachable at compile time.
        bool instrumented = DebugGate.IsEnabled;

        if (instrumented)
        {
            DebugGate.RequireInstrumented("bust probability");
            return;
        }

        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(
            () => DebugGate.RequireInstrumented("bust probability"));

        Assert.Contains("bust probability", ex.Message, StringComparison.Ordinal);
        Assert.Contains("BastionInstrumentation", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Player_builds_are_the_default()
    {
        // Guards the safe default: a Godot *debug export* is still a player build, so the gate
        // is not tied to the Debug configuration. Enabling the oracle must be a conscious act.
        // This assertion is inverted under -p:BastionInstrumentation=true, which is expected.
        bool instrumented = DebugGate.IsEnabled;

        if (Environment.GetEnvironmentVariable("BASTION_EXPECT_INSTRUMENTED") == "1")
        {
            Assert.True(instrumented);
        }
        else
        {
            Assert.False(instrumented);
        }
    }
}
