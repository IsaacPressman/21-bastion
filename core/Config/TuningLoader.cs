using System.Text.Json;
using System.Text.Json.Serialization;
using Bastion.Core.Cards;

namespace Bastion.Core.Config;

/// <summary>
/// Loads and validates <see cref="TuningData"/> from JSON.
/// </summary>
/// <remarks>
/// JSON rather than a Godot resource: the core must load its own tuning without the engine, or
/// the headless regression suites cannot run. See docs/ARCHITECTURE.md.
/// </remarks>
public static class TuningLoader
{
    /// <summary>Default location relative to the repository root.</summary>
    public const string DefaultRelativePath = "data/tuning.json";

    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        NumberHandling = JsonNumberHandling.AllowReadingFromString,
    };

    public static TuningData Load(string path)
    {
        if (!File.Exists(path))
        {
            throw new TuningValidationException($"Tuning data not found at '{Path.GetFullPath(path)}'.");
        }

        using FileStream stream = File.OpenRead(path);
        return Load(stream, path);
    }

    public static TuningData Load(Stream stream, string sourceLabel = "<stream>")
    {
        TuningData data;
        try
        {
            data = JsonSerializer.Deserialize<TuningData>(stream, Options)
                   ?? throw new TuningValidationException($"Tuning data in '{sourceLabel}' deserialized to null.");
        }
        catch (JsonException ex)
        {
            throw new TuningValidationException($"Tuning data in '{sourceLabel}' is not valid JSON: {ex.Message}", ex);
        }

        Validate(data, sourceLabel);
        return data;
    }

    public static TuningData LoadFromRepositoryRoot()
    {
        string root = FindRepositoryRoot();
        return Load(Path.Combine(root, DefaultRelativePath));
    }

    /// <summary>
    /// Walks up from the running assembly to find the repository root.
    /// </summary>
    /// <remarks>
    /// Test assemblies run from bin/Debug/net8.0/, so a relative path from the working directory
    /// is not dependable. Anchored on the solution file.
    /// </remarks>
    public static string FindRepositoryRoot()
    {
        DirectoryInfo? dir = new(AppContext.BaseDirectory);

        while (dir is not null)
        {
            if (dir.EnumerateFiles("*.sln").Any() || dir.EnumerateFiles("project.godot").Any())
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new TuningValidationException(
            $"Could not locate the repository root above '{AppContext.BaseDirectory}'.");
    }

    /// <summary>
    /// Checks the invariants a hand-edited or in-memory tuning value can plausibly violate.
    /// </summary>
    /// <remarks>
    /// A <c>with</c>-expression over loaded tuning bypasses every cross-field rule the file was
    /// checked against - move the sockets and <c>march.entryClampMax</c> and the junction position
    /// no longer follow them. The geometry sweeps do exactly that, so they need a way to assert
    /// their candidates are configurations the game would actually accept, rather than measuring an
    /// inconsistent one and reading the result as geometry.
    /// </remarks>
    /// <exception cref="TuningValidationException">If any check fails.</exception>
    public static void Validate(TuningData data, string sourceLabel = "<constructed>")
    {
        ArgumentNullException.ThrowIfNull(data);

        ValidateCore(data, sourceLabel);
    }

    private static void ValidateCore(TuningData d, string source)
    {
        List<string> errors = [];

        // Geometry
        if (d.Geometry.PathLength <= 0)
        {
            errors.Add("geometry.pathLength must be positive.");
        }

        if (d.Geometry.SocketPositions.Count == 0)
        {
            errors.Add("geometry.socketPositions must not be empty.");
        }

        foreach (double position in d.Geometry.SocketPositions)
        {
            if (position < 0 || position > d.Geometry.PathLength)
            {
                errors.Add($"geometry.socketPositions contains {position}, outside the path 0..{d.Geometry.PathLength}.");
            }
        }

        // Two places read the middle socket by index (GeometryTuning.MiddleSocketIndex, and the
        // junction adjacency rule in Wave/WaveSession.cs) while the junction-position check below
        // reads it by sorted position. Those agree only while the positions ascend, so require it
        // rather than leave a silent dependency - uneven spacing makes this easy to get wrong.
        for (int i = 1; i < d.Geometry.SocketPositions.Count; i++)
        {
            if (d.Geometry.SocketPositions[i] <= d.Geometry.SocketPositions[i - 1])
            {
                errors.Add($"geometry.socketPositions must ascend, spawn side first; index {i} ({d.Geometry.SocketPositions[i]}) does not follow {d.Geometry.SocketPositions[i - 1]}.");
                break;
            }
        }

        // Range varies by socket: the geometry remedy for deep-placement dominance
        // (docs/ROADMAP.md Open Decision 2). One entry per socket, or a placement reads off the end.
        if (d.Geometry.RangeBySocket.Count != d.Geometry.SocketPositions.Count)
        {
            errors.Add($"geometry.rangeBySocket has {d.Geometry.RangeBySocket.Count} entries but there are {d.Geometry.SocketPositions.Count} sockets; it is indexed by socket.");
        }

        if (d.Geometry.RangeBySocket.Any(range => range <= 0))
        {
            errors.Add("geometry.rangeBySocket contains a non-positive range; a tower that cannot reach the path is not a tower.");
        }

        if (d.Geometry.FaceCardRangeBonus < 0)
        {
            errors.Add($"geometry.faceCardRangeBonus ({d.Geometry.FaceCardRangeBonus}) must not be negative; face cards see further, not less far (docs/design/04-cards-as-defenses.md).");
        }

        // Shoe presets. Each must hold the shoe at its tuned size, or a preset changes how often
        // the shoe reshuffles and the 10,000-hand runs stop being comparable with one another.
        foreach ((string key, ShoePreset preset) in d.ShoePresets)
        {
            if (preset.CopiesByRank is not { } counts)
            {
                continue;
            }

            foreach (string rank in counts.Keys)
            {
                if (!Enum.GetValues<Rank>().Any(r => string.Equals(r.TuningKey(), rank, StringComparison.Ordinal)))
                {
                    errors.Add($"shoePresets.{key}.copiesByRank has '{rank}', which is not a rank. Use A, 2-10, J, Q, or K.");
                }
            }

            if (counts.Values.Any(count => count < 0))
            {
                errors.Add($"shoePresets.{key}.copiesByRank has a negative count.");
            }

            int total = counts.Values.Sum();
            if (total != d.Rules.ShoeSize)
            {
                errors.Add($"shoePresets.{key} holds {total} cards but rules.shoeSize is {d.Rules.ShoeSize}; presets must be the same size or their bust rates are not comparable.");
            }
        }

        // March
        if (!d.MarchPresets.ContainsKey(d.March.ActivePreset))
        {
            errors.Add($"march.activePreset '{d.March.ActivePreset}' is not one of: {string.Join(", ", d.MarchPresets.Keys)}.");
        }

        if (d.March.FreeCards < 0)
        {
            errors.Add("march.freeCards must not be negative.");
        }

        foreach ((string key, MarchPreset preset) in d.MarchPresets)
        {
            if (preset.Steps.Count == 0)
            {
                errors.Add($"marchPresets.{key}.steps must not be empty.");
            }

            if (preset.Steps.Any(step => step < 0) || preset.StepBeyondListed < 0)
            {
                errors.Add($"marchPresets.{key} has a negative step; the march never retreats except via the 21 pullback.");
            }

            if (preset.Steps.Count > 0 && Math.Abs(preset.StepBeyondListed - preset.Steps[^1]) > 1e-9)
            {
                errors.Add($"marchPresets.{key}.stepBeyondListed ({preset.StepBeyondListed}) must equal the last listed step ({preset.Steps[^1]}); the design says 'repeat the final step.'");
            }
        }

        // The entry clamp is the rear socket's position, so enemies never spawn past the player's
        // last defense. It is derived from geometry, not chosen independently of it.
        if (d.Geometry.SocketPositions.Count > 0)
        {
            double rearSocket = d.Geometry.SocketPositions.Max();
            if (Math.Abs(d.March.EntryClampMax - rearSocket) > 1e-9)
            {
                errors.Add($"march.entryClampMax ({d.March.EntryClampMax}) must equal the rear socket position ({rearSocket}), or enemies can spawn past the last defense.");
            }
        }

        if (d.March.EntryClampMax <= d.March.EntryClampMin)
        {
            errors.Add("march.entryClampMax must exceed march.entryClampMin.");
        }

        // Formation Strength: totals 12..21 must all be present, since ForTotal only special-cases <= 11.
        for (int total = 12; total <= 21; total++)
        {
            if (!d.FormationStrength.ByTotal.ContainsKey(total))
            {
                errors.Add($"formationStrength.byTotal is missing total {total}.");
            }
        }

        // Card power: values 1..11 are all reachable (Ace low through Ace high).
        for (int value = 1; value <= 11; value++)
        {
            if (!d.CardPower.ByValue.ContainsKey(value))
            {
                errors.Add($"cardPower.byValue is missing value {value}.");
            }
        }

        // Run links. With no cross-lane adjacency and the junction excluded, the longest possible
        // run is one lane's socket count - so the tuned table must cover exactly the reachable
        // lengths. A tier beyond reach is dead tuning; a reachable tier left untuned would
        // silently pay nothing. This is what makes adding a socket (the Surveyor relic) fail the
        // load until its link tier is restored, rather than quietly degrade.
        int longestPossibleRun = d.Geometry.SocketPositions.Count;

        for (int length = 2; length <= longestPossibleRun; length++)
        {
            if (!d.RunLinks.BonusByRunLength.ContainsKey(length))
            {
                errors.Add($"runLinks.bonusByRunLength is missing length {length}, which is reachable with {longestPossibleRun} sockets per lane.");
            }
        }

        foreach (int length in d.RunLinks.BonusByRunLength.Keys)
        {
            if (length < 2)
            {
                errors.Add($"runLinks.bonusByRunLength contains length {length}; a run is two or more towers.");
            }
            else if (length > longestPossibleRun)
            {
                errors.Add($"runLinks.bonusByRunLength contains length {length}, unreachable with {longestPossibleRun} sockets per lane and no cross-lane adjacency.");
            }
        }

        if (d.RunLinks.JunctionParticipatesInRuns)
        {
            errors.Add("runLinks.junctionParticipatesInRuns must be false: the junction is a run island, or it becomes the auto-best socket.");
        }

        if (d.RunLinks.CrossLaneAdjacency)
        {
            errors.Add("runLinks.crossLaneAdjacency must be false: cross-lane runs reward splitting coverage, which fights lane triage.");
        }

        // Overload
        string[] overloadTargets = ["highest_visible_threat"];
        if (!overloadTargets.Contains(d.Rules.OverloadTargetLane, StringComparer.Ordinal))
        {
            errors.Add($"rules.overloadTargetLane '{d.Rules.OverloadTargetLane}' is not one of: {string.Join(", ", overloadTargets)}.");
        }

        // Shoe
        if (d.Rules.ShoeSize != d.Rules.CopiesPerRank * 13)
        {
            errors.Add($"rules.shoeSize ({d.Rules.ShoeSize}) does not equal copiesPerRank ({d.Rules.CopiesPerRank}) x 13 ranks.");
        }

        // Enemies
        if (d.Enemies.Count == 0)
        {
            errors.Add("enemies must not be empty.");
        }

        HashSet<string> enemyIds = d.Enemies.Select(e => e.Id).ToHashSet(StringComparer.Ordinal);
        if (enemyIds.Count != d.Enemies.Count)
        {
            errors.Add("enemies contains duplicate ids.");
        }

        // Dealer card → unit mapping must reference defined enemy types.
        foreach ((string rank, string unitId) in d.DealerCardUnits)
        {
            if (!enemyIds.Contains(unitId))
            {
                errors.Add($"dealerCardUnits.{rank} references '{unitId}', which is not in the enemies list.");
            }
        }

        // Combat
        if (d.Combat.ArmorDamageFloor <= 0)
        {
            errors.Add("combat.armorDamageFloor must be positive, or armor can zero out a hit entirely.");
        }

        if (d.Rules.OpenHeldThresholdFraction is <= 0 or > 1)
        {
            errors.Add("rules.openHeldThresholdFraction must be in (0, 1].");
        }

        // Rules the design fixes rather than tunes. These are stated as invariants in CLAUDE.md;
        // flipping one in data would silently contradict the documentation.
        if (d.Rules.OverloadScalesWithExcess)
        {
            errors.Add("rules.overloadScalesWithExcess must be false: scaling Overload with excess makes busting at 28 better than at 22.");
        }

        if (!d.Rules.DealerResolvesOnPlayerBust)
        {
            errors.Add("rules.dealerResolvesOnPlayerBust must be true: bust never dodges the wave.");
        }

        ValidateResolver(d, enemyIds, errors);

        if (errors.Count > 0)
        {
            throw new TuningValidationException(
                $"Tuning data in '{source}' is invalid:{Environment.NewLine}  - {string.Join($"{Environment.NewLine}  - ", errors)}");
        }
    }

    /// <summary>
    /// Checks the sections the design does not specify, which are therefore the ones most likely to
    /// be edited casually.
    /// </summary>
    /// <remarks>
    /// The timing checks matter more than they look. The resolver samples on a fixed tick, so a
    /// duration that is not a whole number of ticks silently becomes a different duration - and it
    /// would do so consistently, which means the forecast and the wave would agree with each other
    /// while both disagreeing with the tuning file. Rejecting it at load is the only place that
    /// catches it.
    /// </remarks>
    private static void ValidateResolver(TuningData d, HashSet<string> enemyIds, List<string> errors)
    {
        double tick = d.Sim.TickSeconds;

        if (tick <= 0)
        {
            errors.Add("sim.tickSeconds must be positive.");
            return; // Every check below divides by it.
        }

        void RequireWholeTicks(string name, double seconds)
        {
            if (seconds <= 0)
            {
                errors.Add($"{name} ({seconds}) must be positive.");
            }
            else if (Math.Abs(Math.Round(seconds / tick) - (seconds / tick)) > 1e-9)
            {
                errors.Add($"{name} ({seconds}) is not a whole multiple of sim.tickSeconds ({tick}); it would resolve as a different duration than the one tuned.");
            }
        }

        RequireWholeTicks("towers.cooldownSeconds", d.Towers.CooldownSeconds);
        RequireWholeTicks("waves.groupGapSeconds", d.Waves.GroupGapSeconds);
        RequireWholeTicks("suits.spades.slowSeconds", d.Suits.Spades.SlowSeconds);

        foreach (EnemyTuning enemy in d.Enemies)
        {
            if (enemy.SpacingSeconds is double spacing)
            {
                RequireWholeTicks($"enemies.{enemy.Id}.spacingSeconds", spacing);
            }

            if (enemy.Health <= 0)
            {
                errors.Add($"enemies.{enemy.Id}.health must be positive.");
            }

            if (enemy.Speed <= 0)
            {
                errors.Add($"enemies.{enemy.Id}.speed must be positive, or the unit never reaches the end and the wave never resolves.");
            }

            if (enemy.FlatArmor < 0)
            {
                errors.Add($"enemies.{enemy.Id}.flatArmor must not be negative.");
            }

            if (enemy.Count <= 0)
            {
                errors.Add($"enemies.{enemy.Id}.count must be positive; it is the Dealer pack size.");
            }

            // Aura (Standard bearer). Invented for the resolver; a zero radius or a sub-1 multiplier
            // would be a buff that does nothing or a secret slow, neither of which is what it is for.
            if (enemy.Aura is { } aura)
            {
                if (aura.Radius <= 0)
                {
                    errors.Add($"enemies.{enemy.Id}.aura.radius ({aura.Radius}) must be positive, or the aura buffs nothing.");
                }

                if (aura.SpeedMultiplier <= 0)
                {
                    errors.Add($"enemies.{enemy.Id}.aura.speedMultiplier ({aura.SpeedMultiplier}) must be positive.");
                }
            }
        }

        // Towers. The junction position is derived from geometry the same way the entry clamp is:
        // it is the middle socket, so a junction tower covers the same ground as a mid-lane one.
        if (d.Towers.JunctionPathPosition < 0 || d.Towers.JunctionPathPosition > d.Geometry.PathLength)
        {
            errors.Add($"towers.junctionPathPosition ({d.Towers.JunctionPathPosition}) is outside the path 0..{d.Geometry.PathLength}.");
        }
        else if (d.Geometry.SocketPositions.Count > 0)
        {
            double middleSocket = d.Geometry.SocketPositions.OrderBy(p => p).ElementAt(d.Geometry.SocketPositions.Count / 2);
            if (Math.Abs(d.Towers.JunctionPathPosition - middleSocket) > 1e-9)
            {
                errors.Add($"towers.junctionPathPosition ({d.Towers.JunctionPathPosition}) must equal the middle socket position ({middleSocket}); the junction covers the same ground as a mid-lane socket, it just splits its fire.");
            }
        }

        if (d.Towers.JunctionContributionFraction is <= 0 or > 1)
        {
            errors.Add("towers.junctionContributionFraction must be in (0, 1]; above 1 the junction would out-damage a lane socket in both lanes at once, which makes it the auto-best pick.");
        }

        // Suits
        if (d.Suits.Clubs.SplashRadius < 0)
        {
            errors.Add("suits.clubs.splashRadius must not be negative.");
        }

        if (d.Suits.Clubs.SplashFraction is < 0 or > 1)
        {
            errors.Add("suits.clubs.splashFraction must be in [0, 1]; splash never exceeds the primary hit.");
        }

        if (d.Suits.Spades.SlowMultiplier is <= 0 or > 1)
        {
            errors.Add("suits.spades.slowMultiplier must be in (0, 1]; at 0 a Spade is a hard stop, above 1 it is a hasten.");
        }

        if (d.Suits.Spades.SlowStacks)
        {
            errors.Add("suits.spades.slowStacks must be false: stacking multipliers lets two Spades approximate a hard stop, which is a different mechanic than 'slow'.");
        }

        // Standing orders
        if (d.StandingOrders.TriggerGroupMinEnemies < 1)
        {
            errors.Add("standingOrders.triggerGroupMinEnemies must be at least 1, or the order never holds fire.");
        }

        if (d.StandingOrders.TriggerGroupRadius <= 0)
        {
            errors.Add("standingOrders.triggerGroupRadius must be positive.");
        }

        // Waves
        string[] laneAssignments = ["alternate_from_vanguard"];
        if (!laneAssignments.Contains(d.Waves.DealerLaneAssignment, StringComparer.Ordinal))
        {
            errors.Add($"waves.dealerLaneAssignment '{d.Waves.DealerLaneAssignment}' is not one of: {string.Join(", ", laneAssignments)}.");
        }

        // Encounters
        string[] prototypeStakes = ["bastion", "vault"];

        if (d.Encounters.Count == 0)
        {
            errors.Add("encounters must not be empty; the resolver has nothing to run against.");
        }

        if (d.Encounters.Select(e => e.Id).Distinct(StringComparer.Ordinal).Count() != d.Encounters.Count)
        {
            errors.Add("encounters contains duplicate ids.");
        }

        foreach (EncounterTuning encounter in d.Encounters)
        {
            if (encounter.LaneStakes.Count != d.Geometry.Lanes)
            {
                errors.Add($"encounters.{encounter.Id}.laneStakes has {encounter.LaneStakes.Count} entries but geometry.lanes is {d.Geometry.Lanes}.");
            }

            foreach (string stake in encounter.LaneStakes)
            {
                if (!prototypeStakes.Contains(stake, StringComparer.Ordinal))
                {
                    // The Works stake exists in the design but is full-game only - see
                    // docs/prototype/SCOPE.md. Accepting it here would let it reach the resolver
                    // with no behaviour behind it.
                    errors.Add($"encounters.{encounter.Id}.laneStakes contains '{stake}'; the prototype supports only: {string.Join(", ", prototypeStakes)}.");
                }
            }

            if (encounter.VanguardLane < 0 || encounter.VanguardLane >= d.Geometry.Lanes)
            {
                errors.Add($"encounters.{encounter.Id}.vanguardLane ({encounter.VanguardLane}) is not a lane index in 0..{d.Geometry.Lanes - 1}.");
            }

            // Zero would mean an encounter that resets before it is played; a negative count is
            // nonsense. Persistence has to be bounded by something, and this is the bound.
            if (encounter.Waves < 1)
            {
                errors.Add($"encounters.{encounter.Id}.waves is {encounter.Waves}; an encounter runs at least one wave.");
            }

            if (encounter.BaseWave.Count != d.Geometry.Lanes)
            {
                errors.Add($"encounters.{encounter.Id}.baseWave has {encounter.BaseWave.Count} lanes but geometry.lanes is {d.Geometry.Lanes}.");
            }

            foreach (SpawnGroupTuning group in encounter.BaseWave.SelectMany(lane => lane))
            {
                if (!enemyIds.Contains(group.EnemyId))
                {
                    errors.Add($"encounters.{encounter.Id}.baseWave references enemy '{group.EnemyId}', which is not in the enemies list.");
                }

                if (group.Count <= 0)
                {
                    errors.Add($"encounters.{encounter.Id}.baseWave has a group of '{group.EnemyId}' with count {group.Count}.");
                }
                else if (group.Count > 1
                         && enemyIds.Contains(group.EnemyId)
                         && d.Enemy(group.EnemyId).SpacingSeconds is null)
                {
                    // Without a spacing there is nothing to separate the units, so they would all
                    // enter on the same tick at the same position. The roster only leaves spacing
                    // null for types whose count is 1, so this is only reachable from encounter data.
                    errors.Add($"encounters.{encounter.Id}.baseWave asks for {group.Count} of '{group.EnemyId}', which has no spacingSeconds; they would all enter on the same tick at the same position.");
                }
            }
        }
    }
}
