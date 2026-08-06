using System.Text.Json;
using System.Text.Json.Serialization;

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
    /// Checks the invariants that a hand-edited tuning file can plausibly violate.
    /// </summary>
    private static void Validate(TuningData d, string source)
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

        if (errors.Count > 0)
        {
            throw new TuningValidationException(
                $"Tuning data in '{source}' is invalid:{Environment.NewLine}  - {string.Join($"{Environment.NewLine}  - ", errors)}");
        }
    }
}
