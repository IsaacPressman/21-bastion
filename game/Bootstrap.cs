using Bastion.Core.Config;
using Bastion.Core.Diagnostics;
using Bastion.Core.March;
using Godot;

namespace Bastion.Game;

/// <summary>
/// Loads tuning data at startup and reports what the build is configured to do.
/// </summary>
/// <remarks>
/// <para>
/// Scaffold entry point. Attach to a Node in the main scene; it does nothing but prove the wiring
/// - the Godot layer references the engine-free core, and the core loads its own data without the
/// scene tree having any say in it.
/// </para>
/// <para>
/// Tuning is loaded once here and passed down. Nothing below this point reads the file again, and
/// nothing anywhere reads a tuning value from a literal.
/// </para>
/// </remarks>
public partial class Bootstrap : Node
{
    private TuningData? _tuning;

    /// <summary>The loaded tuning data. Null only if loading failed.</summary>
    public TuningData? Tuning => _tuning;

    public override void _Ready()
    {
        try
        {
            _tuning = TuningLoader.Load(ProjectSettings.GlobalizePath($"res://{TuningLoader.DefaultRelativePath}"));
        }
        catch (TuningValidationException ex)
        {
            // Fail loudly and specifically. Tuning is hand-edited between playtests, and a typo
            // must not degrade quietly into a wave that resolves oddly three hours later.
            GD.PrintErr($"[21 Bastion] Tuning data rejected:\n{ex.Message}");
            return;
        }

        MarchPreset arm = _tuning.ActiveMarchPreset;

        GD.Print($"[21 Bastion] Design revision {_tuning.Revision}");
        GD.Print($"[21 Bastion] March arm {_tuning.March.ActivePreset} - {arm.Label}");
        GD.Print($"[21 Bastion] Entry after 3/4/5 cards: " +
                 $"{MarchClock.EntryAfter(_tuning, 3, false):F1} / " +
                 $"{MarchClock.EntryAfter(_tuning, 4, false):F1} / " +
                 $"{MarchClock.EntryAfter(_tuning, 5, false):F1}" +
                 $"  (clamped at {_tuning.March.EntryClampMax:F1})");

        // Copied to a local so the branch is not folded away as unreachable at compile time.
        bool instrumented = DebugGate.IsEnabled;

        if (instrumented)
        {
            // Never in a player build. See docs/design/09-information-and-ui.md.
            GD.Print("[21 Bastion] Oracle-tier instrumentation is COMPILED IN. Not a player build.");
        }
    }
}
