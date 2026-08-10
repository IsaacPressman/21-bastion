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
    public void Rejects_a_cooldown_that_is_not_a_whole_number_of_ticks()
    {
        // The resolver samples on a fixed tick, so an off-tick duration silently becomes a
        // different duration - consistently, in both the forecast and the wave, so nothing
        // downstream would ever disagree and reveal it.
        string json = ValidJsonWith("\"cooldownSeconds\": 1.0", "\"cooldownSeconds\": 1.03");

        TuningValidationException ex = Assert.Throws<TuningValidationException>(
            () => TuningLoader.Load(Json(json)));

        Assert.Contains("cooldownSeconds", ex.Message, StringComparison.Ordinal);
        Assert.Contains("tickSeconds", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Rejects_an_enemy_spacing_that_is_not_a_whole_number_of_ticks()
    {
        string json = ValidJsonWith("\"spacingSeconds\": 0.45", "\"spacingSeconds\": 0.47");

        TuningValidationException ex = Assert.Throws<TuningValidationException>(
            () => TuningLoader.Load(Json(json)));

        Assert.Contains("spacingSeconds", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Rejects_a_junction_position_that_is_not_the_middle_socket()
    {
        // Derived from geometry, like the entry clamp. If the sockets move, this must follow.
        string json = ValidJsonWith("\"junctionPathPosition\": 6.0", "\"junctionPathPosition\": 7.0");

        TuningValidationException ex = Assert.Throws<TuningValidationException>(
            () => TuningLoader.Load(Json(json)));

        Assert.Contains("junctionPathPosition", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Rejects_a_junction_contribution_above_one()
    {
        // Above 1 the junction out-damages a lane socket in both lanes at once, which makes it the
        // auto-best pick and collapses the placement decision the prototype exists to test.
        string json = ValidJsonWith("\"junctionContributionFraction\": 0.5", "\"junctionContributionFraction\": 1.5");

        TuningValidationException ex = Assert.Throws<TuningValidationException>(
            () => TuningLoader.Load(Json(json)));

        Assert.Contains("junctionContributionFraction", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Rejects_stacking_slow()
    {
        // Two Spades compounding to 0.36 speed is a hard stop wearing the word "slow".
        string json = ValidJsonWith("\"slowStacks\": false", "\"slowStacks\": true");

        TuningValidationException ex = Assert.Throws<TuningValidationException>(
            () => TuningLoader.Load(Json(json)));

        Assert.Contains("slowStacks", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Rejects_a_splash_fraction_above_one()
    {
        string json = ValidJsonWith("\"splashFraction\": 0.5", "\"splashFraction\": 1.4");

        TuningValidationException ex = Assert.Throws<TuningValidationException>(
            () => TuningLoader.Load(Json(json)));

        Assert.Contains("splashFraction", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Rejects_the_Works_stake_which_is_full_game_only()
    {
        string json = ValidJsonWith("[\"bastion\", \"vault\"]", "[\"bastion\", \"works\"]");

        TuningValidationException ex = Assert.Throws<TuningValidationException>(
            () => TuningLoader.Load(Json(json)));

        Assert.Contains("works", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Rejects_a_base_wave_referencing_an_unknown_enemy()
    {
        string json = ValidJsonWith("\"enemyId\": \"armored_soldier\"", "\"enemyId\": \"armoured_soldier\"");

        TuningValidationException ex = Assert.Throws<TuningValidationException>(
            () => TuningLoader.Load(Json(json)));

        Assert.Contains("armoured_soldier", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Rejects_a_base_wave_with_the_wrong_number_of_lanes()
    {
        string json = ValidJsonWith("\"lanes\": 2", "\"lanes\": 3");

        TuningValidationException ex = Assert.Throws<TuningValidationException>(
            () => TuningLoader.Load(Json(json)));

        Assert.Contains("baseWave", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Rejects_a_range_list_that_does_not_cover_every_socket()
    {
        // rangeBySocket is indexed by socket, so a short list is an index off the end waiting to
        // happen - at whichever socket the player happens to pick.
        string json = ValidJsonWith("\"rangeBySocket\": [4.0, 3.0, 2.0]", "\"rangeBySocket\": [4.0, 3.0]");

        TuningValidationException ex = Assert.Throws<TuningValidationException>(
            () => TuningLoader.Load(Json(json)));

        Assert.Contains("rangeBySocket", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Rejects_a_socket_that_cannot_reach_the_path()
    {
        string json = ValidJsonWith("\"rangeBySocket\": [4.0, 3.0, 2.0]", "\"rangeBySocket\": [4.0, 0.0, 2.0]");

        TuningValidationException ex = Assert.Throws<TuningValidationException>(
            () => TuningLoader.Load(Json(json)));

        Assert.Contains("rangeBySocket", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Rejects_a_face_card_allowance_that_would_shorten_range()
    {
        // Face cards see further (docs/design/04-cards-as-defenses.md). A negative bonus would
        // quietly invert that rule now that the allowance is added rather than substituted.
        string json = ValidJsonWith("\"faceCardRangeBonus\": 1.0", "\"faceCardRangeBonus\": -1.0");

        TuningValidationException ex = Assert.Throws<TuningValidationException>(
            () => TuningLoader.Load(Json(json)));

        Assert.Contains("faceCardRangeBonus", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Rejects_socket_positions_that_do_not_ascend()
    {
        // Two readings of "the middle socket" exist - by index and by sorted position - and they
        // agree only while the list ascends. Uneven spacing makes this easy to get wrong, so the
        // dependency is checked rather than assumed.
        string json = ValidJsonWith("\"socketPositions\": [3.0, 6.0, 9.0]", "\"socketPositions\": [3.0, 9.0, 6.0]");

        TuningValidationException ex = Assert.Throws<TuningValidationException>(
            () => TuningLoader.Load(Json(json)));

        Assert.Contains("ascend", ex.Message, StringComparison.Ordinal);
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
