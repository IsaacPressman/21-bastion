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

    /// <summary>
    /// Owned by <c>game/devtools/CaptureRun.cs</c>; read here to skip the picker and to suppress logging.
    /// </summary>
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
    /// <remarks>
    /// <para>
    /// <b>Forced off by <c>--capture</c>.</b> A capture run drives the controller itself, so every state
    /// it produces carries a decision time measured in tens of milliseconds and a choice nobody made.
    /// Those lines are indistinguishable from a real session on disk, and they pooled into the Milestone
    /// 5 baseline as if a person had played them - nine synthetic sessions against two real ones, which
    /// is how <c>meanCardsAtLock</c> came to describe a script.
    /// </para>
    /// <para>
    /// Suppressed here rather than filtered later because a capture run is a smoke test that already has
    /// its own output, and the cheapest fix for data that should not exist is not to write it.
    /// <see cref="Bastion.Core.Validation.SessionAnalysis"/> still screens for synthetic runs, for the
    /// logs written before this existed.
    /// </para>
    /// </remarks>
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
        bool capturing = System.Array.IndexOf(args, CaptureFlag) >= 0;

        return new LaunchOptions
        {
            Arm = ArgumentAfter(args, ArmFlag)?.ToUpperInvariant(),
            FixtureId = ArgumentAfter(args, FixtureFlag),
            Seed = int.TryParse(seedText, out int seed) ? seed : DefaultSeed,
            LogDirectory = ArgumentAfter(args, LogFlag) ?? "res://telemetry/sessions",

            // --capture wins over the absence of --no-log: a capture run must never write a session
            // log, and requiring the operator to remember both flags is how the nine synthetic
            // sessions got written in the first place.
            Logging = System.Array.IndexOf(args, NoLogFlag) < 0 && !capturing,
            SkipPicker = capturing,
        };
    }

    /// <summary>The value following a flag, or null if the flag is absent or ends the list.</summary>
    private static string? ArgumentAfter(string[] args, string flag)
    {
        int index = System.Array.IndexOf(args, flag);

        return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
    }
}
