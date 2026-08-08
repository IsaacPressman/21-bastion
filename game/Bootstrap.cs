using Bastion.Core.Board;
using Bastion.Core.Cards;
using Bastion.Core.Config;
using Bastion.Core.Diagnostics;
using Bastion.Core.March;
using Bastion.Core.Resolve;
using Bastion.Core.Wave;
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

        ReportForecast(_tuning);
        ReportWaveLoop(_tuning);

        // Copied to a local so the branch is not folded away as unreachable at compile time.
        bool instrumented = DebugGate.IsEnabled;

        if (instrumented)
        {
            // Never in a player build. See docs/design/09-information-and-ui.md.
            GD.Print("[21 Bastion] Oracle-tier instrumentation is COMPILED IN. Not a player build.");
        }
    }

    /// <summary>
    /// Resolves the worked example once and prints both forecasts.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Still a smoke test, not gameplay. It exists to prove the one link the headless suites cannot
    /// check: that the Godot layer reaches the engine-free resolver and runs a full wave through
    /// it. There is no board yet - Milestone 2 builds one by playing a hand.
    /// </para>
    /// <para>
    /// Note that the two figures are printed under different headings and are never added together
    /// or reconciled. They answer different questions, and a UI that merges them is how trust in
    /// the forecast is lost. See docs/design/05-battlefield.md.
    /// </para>
    /// </remarks>
    private static void ReportForecast(TuningData tuning)
    {
        EncounterTuning encounter = tuning.Encounter("example_wave");
        double entry = tuning.Geometry.DefaultEntry;
        double threshold = tuning.Rules.OpenHeldThresholdFraction;

        // A stand-in board: two Clubs in the Bastion lane, one Spade in the Vault lane, at the
        // worked example's x1.30. Placement is Milestone 2.
        BoardState board = BoardState.Create(tuning,
        [
            TowerState.Place(tuning, new Card(Rank.Six), Family.Club, SocketRef.InLane(0, 1), 1.30),
            TowerState.Place(tuning, new Card(Rank.Eight), Family.Club, SocketRef.InLane(0, 2), 1.30),
            TowerState.Place(tuning, new Card(Rank.Four), Family.Spade, SocketRef.InLane(1, 0), 1.30),
        ], entry);

        VisibleThreat threat = Resolver.ResolveRevealed(
            tuning, encounter, board,
            ArmyBuilder.Revealed(tuning, encounter, new Card(Rank.Ten), entry));

        FinalForecast forecast = Resolver.ResolveComplete(
            tuning, encounter, board,
            ArmyBuilder.Complete(tuning, encounter,
                [new Card(Rank.Ten), new Card(Rank.Six), new Card(Rank.Seven)], entry));

        GD.Print("[21 Bastion] Visible Threat (revealed force only - NOT a prediction of the wave):");
        foreach (LaneOutcome lane in threat.Lanes)
        {
            GD.Print($"[21 Bastion]   Lane {lane.LaneIndex} ({lane.Stake}): " +
                     $"{lane.CoverageLabel(threshold)}, {lane.PredictedDamage} of {lane.EmptyLaneDamage}");
        }

        GD.Print("[21 Bastion] Final Forecast (the combat contract):");
        foreach (LaneOutcome lane in forecast.Lanes)
        {
            GD.Print($"[21 Bastion]   Lane {lane.LaneIndex} ({lane.Stake}): " +
                     $"{lane.CoverageLabel(threshold)}, {lane.PredictedDamage} of {lane.EmptyLaneDamage}, " +
                     $"prevented {lane.DamagePrevented}");
        }

        GD.Print($"[21 Bastion] Wave resolves in {forecast.Timeline.DurationSeconds:F1}s " +
                 $"over {forecast.Timeline.Events.Count} events " +
                 $"(target {tuning.Combat.WaveResolutionSecondsMin:F0}-{tuning.Combat.WaveResolutionSecondsMax:F0}s)");
    }

    /// <summary>
    /// Drives a short wave through the Milestone 3 state machine and prints its phases.
    /// </summary>
    /// <remarks>
    /// Still a smoke test, not gameplay: it proves the Godot layer reaches the engine-free
    /// <see cref="WaveSession"/> and can walk it from the opening deal to a locked, forecast wave. The
    /// cards come from a seeded shoe, so the printed hand varies with the seed; only the placement
    /// sockets are scripted.
    /// </remarks>
    private static void ReportWaveLoop(TuningData tuning)
    {
        EncounterTuning encounter = tuning.Encounter("example_wave");

        WaveSession session = WaveSession.Begin(tuning, encounter, Shoe.Create(tuning, seed: 20240808))
            .Place(Family.Club, SocketRef.InLane(0, 1))
            .Place(Family.Club, SocketRef.InLane(0, 2));

        GD.Print($"[21 Bastion] Wave loop: Vanguard {session.Vanguard}, opening hand {session.Hand.Total} " +
                 $"(×{session.FormationMultiplier:F2}), phase {session.Phase}");

        // The Visible Threat during the draw - the revealed force only, never a wave prediction.
        double threshold = tuning.Rules.OpenHeldThresholdFraction;
        LaneOutcome vaultThreat = session.VisibleThreatNow().Lanes[1];
        GD.Print($"[21 Bastion]   Visible Threat, Vault lane: {vaultThreat.CoverageLabel(threshold)}, {vaultThreat.PredictedDamage}");

        WaveSession locked = session.Stand().Lock();
        GD.Print($"[21 Bastion]   Stood; Dealer deployed {locked.DealerCards!.Count} cards; phase {locked.Phase}");

        foreach (LaneOutcome lane in locked.Forecast().Lanes)
        {
            GD.Print($"[21 Bastion]   Final Forecast, lane {lane.LaneIndex} ({lane.Stake}): " +
                     $"{lane.CoverageLabel(threshold)}, {lane.PredictedDamage} of {lane.EmptyLaneDamage}");
        }
    }
}
