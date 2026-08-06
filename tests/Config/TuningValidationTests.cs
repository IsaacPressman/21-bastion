using System.Text;
using Bastion.Core.Config;

namespace Bastion.Core.Tests.Config;

/// <summary>
/// The loader rejects tuning files that are internally inconsistent.
/// </summary>
/// <remarks>
/// Tuning data is edited by hand between playtests. A typo should stop the launch with a specific
/// message, not surface later as a wave that resolves oddly.
/// </remarks>
public sealed class TuningValidationTests
{
    private static Stream Json(string text) => new MemoryStream(Encoding.UTF8.GetBytes(text));

    private static string ValidJsonWith(string find, string replace)
    {
        string path = Path.Combine(TuningLoader.FindRepositoryRoot(), TuningLoader.DefaultRelativePath);
        string json = File.ReadAllText(path);

        Assert.Contains(find, json, StringComparison.Ordinal);

        return json.Replace(find, replace, StringComparison.Ordinal);
    }

    [Fact]
    public void Rejects_an_active_preset_that_does_not_exist()
    {
        string json = ValidJsonWith("\"activePreset\": \"C\"", "\"activePreset\": \"Z\"");

        TuningValidationException ex = Assert.Throws<TuningValidationException>(
            () => TuningLoader.Load(Json(json)));

        Assert.Contains("activePreset", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Rejects_a_shoe_that_is_not_a_whole_number_of_rank_copies()
    {
        string json = ValidJsonWith("\"shoeSize\": 26", "\"shoeSize\": 25");

        TuningValidationException ex = Assert.Throws<TuningValidationException>(
            () => TuningLoader.Load(Json(json)));

        Assert.Contains("shoeSize", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Rejects_a_socket_placed_off_the_path()
    {
        string json = ValidJsonWith("\"socketPositions\": [3.0, 6.0, 9.0]", "\"socketPositions\": [3.0, 6.0, 99.0]");

        TuningValidationException ex = Assert.Throws<TuningValidationException>(
            () => TuningLoader.Load(Json(json)));

        Assert.Contains("socketPositions", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Rejects_overload_scaling_with_excess()
    {
        // Busting at 28 must never be better than busting at 22.
        string json = ValidJsonWith("\"overloadScalesWithExcess\": false", "\"overloadScalesWithExcess\": true");

        TuningValidationException ex = Assert.Throws<TuningValidationException>(
            () => TuningLoader.Load(Json(json)));

        Assert.Contains("overloadScalesWithExcess", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Rejects_a_dealer_that_does_not_resolve_on_player_bust()
    {
        // Bust never dodges the wave; the hidden card was always marching.
        string json = ValidJsonWith("\"dealerResolvesOnPlayerBust\": true", "\"dealerResolvesOnPlayerBust\": false");

        TuningValidationException ex = Assert.Throws<TuningValidationException>(
            () => TuningLoader.Load(Json(json)));

        Assert.Contains("dealerResolvesOnPlayerBust", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Rejects_a_negative_march_step()
    {
        // The march never retreats except via the exactly-21 pullback.
        string json = ValidJsonWith("\"steps\": [1.5, 2.5, 3.5]", "\"steps\": [1.5, -2.5, 3.5]");

        TuningValidationException ex = Assert.Throws<TuningValidationException>(
            () => TuningLoader.Load(Json(json)));

        Assert.Contains("negative step", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Rejects_a_stepBeyondListed_that_diverges_from_the_last_step()
    {
        string json = ValidJsonWith("\"stepBeyondListed\": 3.5", "\"stepBeyondListed\": 1.0");

        TuningValidationException ex = Assert.Throws<TuningValidationException>(
            () => TuningLoader.Load(Json(json)));

        Assert.Contains("stepBeyondListed", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Rejects_a_dealer_card_unit_not_in_the_enemies_list()
    {
        string json = ValidJsonWith("\"K\": \"siege_engine\"", "\"K\": \"typo_engine\"");

        TuningValidationException ex = Assert.Throws<TuningValidationException>(
            () => TuningLoader.Load(Json(json)));

        Assert.Contains("typo_engine", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Reports_a_missing_file_with_the_full_path()
    {
        TuningValidationException ex = Assert.Throws<TuningValidationException>(
            () => TuningLoader.Load("does/not/exist.json"));

        Assert.Contains("exist.json", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Reports_malformed_json_rather_than_throwing_a_raw_parse_error()
    {
        TuningValidationException ex = Assert.Throws<TuningValidationException>(
            () => TuningLoader.Load(Json("{ not json")));

        Assert.Contains("not valid JSON", ex.Message, StringComparison.Ordinal);
    }
}
