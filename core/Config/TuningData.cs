using System.Text.Json.Serialization;

namespace Bastion.Core.Config;

/// <summary>
/// Every tuning value in the game, loaded from data. Mirrors docs/reference/tuning-constants.md.
/// </summary>
/// <remarks>
/// Nothing in this type is a constant in code. The design states plainly that every number is
/// first-pass and expected to be wrong, and two of them (the march curve, the pullback) must be
/// swappable without recompiling because they are the test arms.
/// </remarks>
public sealed record TuningData
{
    public required string Revision { get; init; }
    public required GeometryTuning Geometry { get; init; }
    public required MarchTuning March { get; init; }
    public required IReadOnlyDictionary<string, MarchPreset> MarchPresets { get; init; }
    public required FormationStrengthTuning FormationStrength { get; init; }
    public required CardPowerTuning CardPower { get; init; }
    public required AceBastionTuning AceBastion { get; init; }
    public required RunLinkTuning RunLinks { get; init; }
    public required IReadOnlyList<EnemyTuning> Enemies { get; init; }
    public required CombatTuning Combat { get; init; }
    public required IReadOnlyDictionary<string, string> DealerCardUnits { get; init; }
    public required RulesTuning Rules { get; init; }

    /// <summary>The march curve currently selected by <see cref="MarchTuning.ActivePreset"/>.</summary>
    [JsonIgnore]
    public MarchPreset ActiveMarchPreset => MarchPresets[March.ActivePreset];
}

/// <summary>
/// Path and socket layout. The march step sizes are derived from socket spacing, not chosen
/// independently of it - if this changes, the march curve must be re-derived.
/// </summary>
public sealed record GeometryTuning
{
    public required double PathLength { get; init; }
    public required IReadOnlyList<double> SocketPositions { get; init; }
    public required int Lanes { get; init; }
    public required int JunctionSockets { get; init; }
    public required double DefaultEntry { get; init; }
    public required double DefaultRange { get; init; }
    public required double FaceCardRange { get; init; }

    /// <summary>Sockets across all lanes plus the shared junction. Seven in the prototype.</summary>
    [JsonIgnore]
    public int TotalSockets => (SocketPositions.Count * Lanes) + JunctionSockets;
}

public sealed record MarchTuning
{
    /// <summary>Preset key into <see cref="TuningData.MarchPresets"/>. Also selects the test arm.</summary>
    public required string ActivePreset { get; init; }

    /// <summary>Cards drawn before the march begins. The opening two are free.</summary>
    public required int FreeCards { get; init; }

    /// <summary>Units the entry point retreats on reaching exactly 21, at any card count.</summary>
    public required double Exactly21Pullback { get; init; }

    public required double EntryClampMin { get; init; }

    /// <summary>
    /// Furthest the entry point may advance, regardless of hand length.
    /// </summary>
    /// <remarks>
    /// The rear socket's own position, so enemies never spawn past the player's last defense.
    /// Without it a seven-card hand spawns enemies at the Bastion for a guaranteed full leak -
    /// an automatic loss for a legal, rare, impressive hand. The clamp applies before the
    /// exactly-21 pullback, so a six-card 21 still recovers real ground.
    /// </remarks>
    public required double EntryClampMax { get; init; }
}

/// <summary>
/// One march curve. All three presets ship in every build; they are the prototype's test arms.
/// </summary>
public sealed record MarchPreset
{
    public required string Label { get; init; }
    public string? Note { get; init; }

    /// <summary>Step paid for each card past the free ones. Index 0 is the third card.</summary>
    public required IReadOnlyList<double> Steps { get; init; }

    /// <summary>
    /// Step for cards beyond the listed ones.
    /// </summary>
    /// <remarks>
    /// NOT SPECIFIED BY THE DESIGN. The handoff defines steps for the 3rd through 5th card only,
    /// but hands longer than five cards are legal (A-A-A-A-2-2-3-3 reaches eight). Repeating the
    /// last listed step is an implementation assumption, flagged in the roadmap as an open question.
    /// </remarks>
    public required double StepBeyondListed { get; init; }

    /// <summary>
    /// Entry position after drawing <paramref name="cardCount"/> cards, before any 21 pullback.
    /// </summary>
    public double CumulativeEntry(int cardCount, int freeCards)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(cardCount);

        double entry = 0.0;
        for (int card = freeCards + 1; card <= cardCount; card++)
        {
            int stepIndex = card - freeCards - 1;
            entry += stepIndex < Steps.Count ? Steps[stepIndex] : StepBeyondListed;
        }

        return entry;
    }
}

public sealed record FormationStrengthTuning
{
    /// <summary>Multiplier by final hand total, for totals 12 through 21.</summary>
    public required IReadOnlyDictionary<int, double> ByTotal { get; init; }

    public required double ElevenOrBelow { get; init; }
    public required double Bust { get; init; }

    /// <summary>Towers surviving into the next wave of an encounter revert to this.</summary>
    public required double Persisted { get; init; }

    public double ForTotal(int total) =>
        total <= 11 ? ElevenOrBelow
        : ByTotal.TryGetValue(total, out double multiplier) ? multiplier
        : throw new TuningValidationException($"No Formation Strength defined for total {total}.");
}

public sealed record CardPowerTuning
{
    /// <summary>
    /// Base tower power by card value. Key 10 covers every face card; key 11 is an Ace held high.
    /// </summary>
    public required IReadOnlyDictionary<int, double> ByValue { get; init; }

    public double ForValue(int value) =>
        ByValue.TryGetValue(value, out double power)
            ? power
            : throw new TuningValidationException($"No base power defined for card value {value}.");
}

public sealed record AceBastionTuning
{
    public required double Power { get; init; }
    public required bool CountsAsHandCard { get; init; }
    public required bool SharesHandMultiplier { get; init; }
}

public sealed record RunLinkTuning
{
    /// <summary>
    /// Whether a tower in the shared junction socket can join a run.
    /// </summary>
    /// <remarks>
    /// False: the junction is a run island. Adjacent to both lanes would make it a run hub able
    /// to join chains in two lanes at once - the auto-best socket, which collapses the placement
    /// decision. Adjacent to one lane is arbitrary and asymmetric. Neither gives the socket a
    /// clean identity: the junction buys breadth and forfeits synergy.
    /// </remarks>
    public required bool JunctionParticipatesInRuns { get; init; }

    /// <summary>
    /// Whether sockets at matching depths in different lanes count as adjacent.
    /// </summary>
    /// <remarks>
    /// False. Runs spanning lanes would reward splitting coverage, which fights lane triage.
    /// </remarks>
    public required bool CrossLaneAdjacency { get; init; }

    /// <summary>Fractional power bonus by run length. 2 -> 0.15 means +15%.</summary>
    public required IReadOnlyDictionary<int, double> BonusByRunLength { get; init; }

    /// <summary>Longest run the tuned table rewards.</summary>
    public int MaxRewardedRunLength => BonusByRunLength.Count == 0 ? 0 : BonusByRunLength.Keys.Max();

    /// <summary>Zero for lengths that do not form a link, rather than throwing.</summary>
    public double BonusForRunLength(int length) =>
        BonusByRunLength.TryGetValue(length, out double bonus) ? bonus : 0.0;
}

public sealed record EnemyTuning
{
    public required string Id { get; init; }
    public required string DisplayName { get; init; }
    public required int Count { get; init; }
    public required double Health { get; init; }
    public required double Speed { get; init; }
    public required double FlatArmor { get; init; }

    /// <summary>Null for single-unit types, which have no spacing.</summary>
    public double? SpacingSeconds { get; init; }

    public required int LeakDamage { get; init; }
}

public sealed record CombatTuning
{
    /// <summary>Armor may never reduce a hit below this.</summary>
    public required double ArmorDamageFloor { get; init; }

    /// <summary>Spade traps and Kings ignore this fraction of flat armor.</summary>
    public required double HalfArmorBypassFraction { get; init; }

    public required double WaveResolutionSecondsMin { get; init; }
    public required double WaveResolutionSecondsMax { get; init; }
}

public sealed record RulesTuning
{
    public required int ShoeSize { get; init; }
    public required int CopiesPerRank { get; init; }
    public required int ReshuffleBelowCards { get; init; }
    public required bool DealerStandsOnAll17s { get; init; }
    public required bool DealerResolvesOnPlayerBust { get; init; }
    public required bool OverloadEqualsBasePower { get; init; }
    public required bool OverloadScalesWithExcess { get; init; }

    /// <summary>
    /// Which lane Overload strikes. The busting card is destroyed and never placed, so there is
    /// no placement for it to inherit a lane from.
    /// </summary>
    public required string OverloadTargetLane { get; init; }

    /// <summary>Stake preferred when two lanes tie on Visible Threat.</summary>
    public required string OverloadTieBreakStake { get; init; }

    /// <summary>One move for the whole board, not one per tower.</summary>
    public required int AdjustmentMovesPerWave { get; init; }

    public required bool AdjustmentAllowsAdjacentSwap { get; init; }
    public required bool StandingOrderChangesConsumeMove { get; init; }
    public required bool AdjustmentWindowOnBust { get; init; }

    /// <summary>A lane reads Open when predicted leakage is at least this fraction of empty-lane damage.</summary>
    public required double OpenHeldThresholdFraction { get; init; }
}
