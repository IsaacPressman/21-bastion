namespace Bastion.Core.Diagnostics;

/// <summary>
/// The single entry point for oracle-tier values, which must never reach a player-facing build.
/// </summary>
/// <remarks>
/// <para>
/// docs/design/09-information-and-ui.md forbids showing the player a combined verdict. Three
/// values exist for instrumentation only: exact bust probability, stand/hit expected output, and
/// combined utility. Each is one arithmetic step from the oracle the design pillars prohibit.
/// </para>
/// <para>
/// The gate is compile-time, not a runtime flag, so the values are absent from the binary rather
/// than merely unreachable. <c>BASTION_DEBUG</c> is defined only when the build opts in:
/// </para>
/// <code>
/// dotnet build -p:BastionInstrumentation=true
/// dotnet test  -p:BastionInstrumentation=true
/// </code>
/// <para>
/// The property must be passed on the command line so it reaches every project in the graph. It
/// cannot be set by one project on another's behalf: this gate compiles into Bastion.Core, so it
/// is Bastion.Core's build that decides. See Directory.Build.props for why this is deliberately
/// not tied to the Debug configuration.
/// </para>
/// <para>
/// This is the oracle tier only. Ordinary playtest telemetry - the choice made, time to decide,
/// forecasts, placement depth - is always available and does not belong behind this gate.
/// </para>
/// </remarks>
public static class DebugGate
{
#if BASTION_DEBUG
    /// <summary>True when this build opted into oracle-tier instrumentation.</summary>
    public const bool IsEnabled = true;
#else
    /// <summary>True when this build opted into oracle-tier instrumentation.</summary>
    public const bool IsEnabled = false;
#endif

    /// <summary>
    /// Evaluates an oracle-tier value, or returns null when instrumentation is compiled out.
    /// </summary>
    /// <remarks>
    /// Takes a delegate so the computation is skipped entirely in a player build, not merely its
    /// result discarded. Callers must handle null rather than asserting a value - which is the
    /// point: presentation code cannot accidentally come to depend on an oracle being there.
    /// </remarks>
    public static T? OracleOnly<T>(Func<T> compute) where T : struct
    {
#if BASTION_DEBUG
        return compute();
#else
        _ = compute;
        return null;
#endif
    }

    /// <summary>
    /// Throws unless this is an instrumented build.
    /// </summary>
    /// <remarks>
    /// Guards a code path that may only exist when instrumentation is compiled in. Put this at
    /// the top of any routine that derives bust probability, expected output, or combined utility,
    /// so that reaching it from a player build fails loudly at the source rather than silently
    /// producing a number that the design forbids showing.
    /// </remarks>
    public static void RequireInstrumented(string what)
    {
        // Preprocessor rather than "if (IsEnabled)": branching on a const would leave the
        // compiler reporting the dead arm as unreachable code on every build.
#if !BASTION_DEBUG
        throw new InvalidOperationException(
            $"'{what}' is an oracle-tier value and this build is not instrumented. " +
            "Bust percentage, expected output, and combined utility are instrumentation only; " +
            "rebuild with -p:BastionInstrumentation=true. See docs/design/09-information-and-ui.md.");
#endif
    }
}
