using Godot;

namespace Bastion.Game.Startup;

/// <summary>
/// How this session was launched: which march arm, which battery case, which seed, where to log.
/// </summary>
/// <remarks>
/// <para>
/// Milestone 5's done-when clause is that <b>a playtest session can be run, logged, and analyzed
/// without code changes between arms</b> (docs/ROADMAP.md). These flags are that clause: a
/// facilitator switches arm or case from the command line, and everything else follows.
/// </para>
/// <para>
/// Read from <see cref="OS.GetCmdlineUserArgs"/> - everything after <c>--</c> - following the
/// convention <c>game/devtools/CaptureRun.cs</c> already set, so the flags cannot collide with
/// Godot's own.
/// </para>
/// <code>
/// godot --path . -- --arm B --fixture 2-split
/// godot --path . -- --arm A --fixture 7-onlyrank-b --seed 4242
/// godot --path . -- --arm C                          # no case: the picker opens
/// </code>
/// <para>
/// Deliberately <b>not</b> gated behind <c>BastionDevTools</c>. That flag keeps a dev harness that
/// drives the game by itself out of a player build; this is the validation build's reason for
/// existing. The oracle tier stays gated where it always was - see
/// <see cref="Bastion.Core.Diagnostics.DebugGate"/>.
/// </para>
/// </remarks>
public sealed record LaunchOptions
{
    /// <summary>Fallback seed, used when no <c>--seed</c> is given and no case is selected.</summary>
    public const int DefaultSeed = 20240808;

    private const string ArmFlag = "--arm";
    private const string FixtureFlag = "--fixture";
    private const string SeedFlag = "--seed";
    private const string LogFlag = "--log-out";
    private const string NoLogFlag = "--no-log";

    /// <summary>Owned by <c>game/devtools/CaptureRun.cs</c>; read here only to skip the picker.</summary>
    private const string CaptureFlag = "--capture";

    /// <summary>March preset key, or null to use whatever <c>data/tuning.json</c> selects.</summary>
    public string? Arm { get; init; }

    /// <summary>Battery case id, or null to open the picker.</summary>
    public string? FixtureId { get; init; }

    /// <summary>Shoe seed for free play. A battery case scripts its own cards and ignores this.</summary>
    public int Seed { get; init; } = DefaultSeed;

    /// <summary>Directory for session logs, as a Godot path.</summary>
    public string LogDirectory { get; init; } = "res://telemetry/sessions";

    /// <summary>Whether to write a session log at all. On by default - this is the logging build.</summary>
    public bool Logging { get; init; } = true;

    /// <summary>
    /// Start a wave immediately rather than opening the picker, even with no case named.
    /// </summary>
    /// <remarks>
    /// Set by <c>--capture</c>. The capture run drives the game itself and exists to photograph the
    /// build a player would get; a facilitator screen is neither, and waiting on one would hang it.
    /// The flag is read here rather than in the devtools node because this is where the decision to
    /// show the picker is made, and <c>--capture</c> is inert in a build without devtools anyway.
    /// </remarks>
    public bool SkipPicker { get; init; }

    public static LaunchOptions FromCommandLine() => Parse(OS.GetCmdlineUserArgs());

    /// <summary>
    /// Parses the user arguments. Unknown flags are ignored rather than rejected, because Godot's own
    /// tooling and the capture run both pass arguments through the same list.
    /// </summary>
    internal static LaunchOptions Parse(string[] args)
    {
        ArgumentNullException.ThrowIfNull(args);

        string? seedText = ArgumentAfter(args, SeedFlag);

        return new LaunchOptions
        {
            Arm = ArgumentAfter(args, ArmFlag)?.ToUpperInvariant(),
            FixtureId = ArgumentAfter(args, FixtureFlag),
            Seed = int.TryParse(seedText, out int seed) ? seed : DefaultSeed,
            LogDirectory = ArgumentAfter(args, LogFlag) ?? "res://telemetry/sessions",
            Logging = System.Array.IndexOf(args, NoLogFlag) < 0,
            SkipPicker = System.Array.IndexOf(args, CaptureFlag) >= 0,
        };
    }

    /// <summary>The value following a flag, or null if the flag is absent or ends the list.</summary>
    private static string? ArgumentAfter(string[] args, string flag)
    {
        int index = System.Array.IndexOf(args, flag);

        return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
    }
}
