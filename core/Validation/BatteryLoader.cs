using System.Text.Json;
using System.Text.Json.Serialization;
using Bastion.Core.Board;
using Bastion.Core.Cards;
using Bastion.Core.Config;

namespace Bastion.Core.Validation;

/// <summary>
/// The scripted battery, and the mirrored encounters its variant-B cases need.
/// </summary>
/// <remarks>
/// A case appears here twice: once as authored and once mirrored. Both are ordinary
/// <see cref="BatteryFixture"/> values by the time anything reads them, so no consumer needs to know
/// which is which - the picker lists them, the log names them, and the equivalence suite replays
/// them all identically.
/// </remarks>
public sealed class Battery
{
    private readonly IReadOnlyDictionary<string, BatteryFixture> _byId;

    internal Battery(IReadOnlyList<BatteryFixture> fixtures)
    {
        Fixtures = fixtures;
        _byId = fixtures.ToDictionary(f => f.Id, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>Every presentable case, authored and mirrored, in battery-item order.</summary>
    public IReadOnlyList<BatteryFixture> Fixtures { get; }

    /// <summary>
    /// Tuning with the mirrored encounters merged in, which is what a fixture must be opened
    /// against.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Mirrored encounters are derived from the authored ones rather than written out by hand -
    /// hand-authoring both halves is exactly how the two presentations quietly stop being the same
    /// decision. They still have to reach <see cref="TuningData.Encounter"/>, and merging them into
    /// a copy of the tuning keeps that in one place instead of teaching the resolver about the
    /// battery.
    /// </para>
    /// <para>
    /// Derived here rather than at load because the authored encounters live in tuning, which the
    /// loader does not have. Several cases share an encounter - the two halves of a contrast often
    /// differ only in the cards - so each is mirrored once, not once per case.
    /// </para>
    /// </remarks>
    public TuningData Apply(TuningData tuning)
    {
        ArgumentNullException.ThrowIfNull(tuning);

        IEnumerable<string> authoredEncounters = Fixtures
            .Where(f => !f.Id.EndsWith(LaneMirror.VariantSuffix, StringComparison.Ordinal))
            .Select(f => f.EncounterId)
            .Distinct(StringComparer.Ordinal);

        EncounterTuning[] mirrored =
            [.. authoredEncounters.Select(id => LaneMirror.Mirror(tuning.Encounter(id)))];

        return tuning with { Encounters = [.. tuning.Encounters, .. mirrored] };
    }

    /// <summary>The case with this id, or null.</summary>
    public BatteryFixture? Find(string id) =>
        _byId.TryGetValue(id, out BatteryFixture? fixture) ? fixture : null;
}

/// <summary>
/// Loads and validates the scripted battery from JSON.
/// </summary>
/// <remarks>
/// Deliberately shaped like <see cref="TuningLoader"/>: same options, same accumulate-then-throw
/// validation, same repository-root anchoring. The battery is hand-edited between playtests for the
/// same reason tuning is, and deserves the same "fail loudly at load" treatment - a fixture that
/// silently offers a different decision than the one it names is worse than one that will not open.
/// </remarks>
public static class BatteryLoader
{
    /// <summary>Default location relative to the repository root.</summary>
    public const string DefaultRelativePath = "data/battery.json";

    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        // Order matters: the first converter that claims a type wins, and JsonStringEnumConverter is
        // a factory that claims every enum - including Rank, where it would read "6" as Rank.Six by
        // its numeric value and then choke on "K". The specific converters have to come first.
        Converters = { new RankJsonConverter(), new SocketRefJsonConverter(), new JsonStringEnumConverter() },
    };

    public static Battery Load(string path)
    {
        if (!File.Exists(path))
        {
            throw new TuningValidationException($"Battery data not found at '{Path.GetFullPath(path)}'.");
        }

        using FileStream stream = File.OpenRead(path);
        return Load(stream, path);
    }

    public static Battery LoadFromRepositoryRoot() =>
        Load(Path.Combine(TuningLoader.FindRepositoryRoot(), DefaultRelativePath));

    public static Battery Load(Stream stream, string sourceLabel = "<stream>")
    {
        BatteryFile file;
        try
        {
            file = JsonSerializer.Deserialize<BatteryFile>(stream, Options)
                   ?? throw new TuningValidationException($"Battery data in '{sourceLabel}' deserialized to null.");
        }
        catch (JsonException ex)
        {
            throw new TuningValidationException($"Battery data in '{sourceLabel}' is not valid JSON: {ex.Message}", ex);
        }

        Validate(file, sourceLabel);

        // Each authored case is immediately followed by its mirror, so the picker lists the two
        // presentations of one decision together.
        List<BatteryFixture> all = [];

        foreach (BatteryFixture authored in file.Fixtures)
        {
            all.Add(authored);
            all.Add(LaneMirror.Mirror(authored));
        }

        return new Battery(all);
    }

    /// <summary>
    /// Checks the battery against itself. Cross-checks against tuning need the encounters in scope
    /// and happen when a fixture is opened.
    /// </summary>
    private static void Validate(BatteryFile file, string source)
    {
        List<string> errors = [];

        if (file.Fixtures.Count == 0)
        {
            errors.Add("battery.fixtures is empty.");
        }

        foreach (IGrouping<string, BatteryFixture> duplicate in
                 file.Fixtures.GroupBy(f => f.Id, StringComparer.OrdinalIgnoreCase).Where(g => g.Count() > 1))
        {
            errors.Add($"battery.fixtures has {duplicate.Count()} cases with id '{duplicate.Key}'; ids name a case in the log and must be unique.");
        }

        foreach (BatteryFixture fixture in file.Fixtures)
        {
            if (fixture.Id.EndsWith(LaneMirror.VariantSuffix, StringComparison.OrdinalIgnoreCase))
            {
                errors.Add($"battery fixture '{fixture.Id}' ends in '{LaneMirror.VariantSuffix}', which is reserved for the mirrored variant generated from it.");
            }

            if (fixture.BatteryItem is < 1 or > 10)
            {
                errors.Add($"battery fixture '{fixture.Id}' serves item {fixture.BatteryItem}; VALIDATION.md lists ten, numbered 1-10.");
            }

            // Begin draws four before the script runs at all: upcard, hole card, and the opening two.
            if (fixture.CardOrder.Count < 4)
            {
                errors.Add($"battery fixture '{fixture.Id}' lists {fixture.CardOrder.Count} cards; the opening deal alone needs four.");
            }

            if (string.IsNullOrWhiteSpace(fixture.Question))
            {
                errors.Add($"battery fixture '{fixture.Id}' has no question; the facilitator screen and the log both show it.");
            }
        }

        if (errors.Count > 0)
        {
            throw new TuningValidationException(
                $"Battery data in '{source}' is inconsistent:{Environment.NewLine}- {string.Join($"{Environment.NewLine}- ", errors)}");
        }
    }

    private sealed record BatteryFile
    {
        public IReadOnlyList<BatteryFixture> Fixtures { get; init; } = [];
    }
}

/// <summary>Reads a rank as the notation the tuning file already uses: A, 2-10, J, Q, K.</summary>
internal sealed class RankJsonConverter : JsonConverter<Rank>
{
    public override Rank Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        string? text = reader.GetString();

        foreach (Rank rank in Enum.GetValues<Rank>())
        {
            if (string.Equals(rank.TuningKey(), text, StringComparison.OrdinalIgnoreCase))
            {
                return rank;
            }
        }

        throw new JsonException($"'{text}' is not a rank. Use A, 2-10, J, Q, or K.");
    }

    public override void Write(Utf8JsonWriter writer, Rank value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value.TuningKey());
}

/// <summary>
/// Reads a socket as <c>L&lt;lane&gt;S&lt;index&gt;</c> or <c>J</c> for the junction.
/// </summary>
/// <remarks>
/// The same notation the placement sweeps already write into their CSVs, so a socket reads the same
/// way in a fixture, a measurement, and a log.
/// </remarks>
internal sealed class SocketRefJsonConverter : JsonConverter<SocketRef>
{
    public override SocketRef Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        string text = reader.GetString() ?? throw new JsonException("A socket may not be null.");

        if (string.Equals(text, "J", StringComparison.OrdinalIgnoreCase))
        {
            return SocketRef.Junction;
        }

        if (text.Length == 4
            && char.ToUpperInvariant(text[0]) == 'L'
            && char.ToUpperInvariant(text[2]) == 'S'
            && int.TryParse(text[1..2], out int lane)
            && int.TryParse(text[3..4], out int socket))
        {
            return SocketRef.InLane(lane, socket);
        }

        throw new JsonException($"'{text}' is not a socket. Use L<lane>S<index>, e.g. L0S2, or J for the junction.");
    }

    public override void Write(Utf8JsonWriter writer, SocketRef value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value.IsJunction ? "J" : $"L{value.LaneIndex}S{value.SocketIndex}");
}
