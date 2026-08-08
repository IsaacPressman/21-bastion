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
    public required SimTuning Sim { get; init; }
    public required TowerTuning Towers { get; init; }
    public required SuitTuning Suits { get; init; }
    public required StandingOrderTuning StandingOrders { get; init; }
    public required WaveTuning Waves { get; init; }
    public required IReadOnlyList<EncounterTuning> Encounters { get; init; }
    public required IReadOnlyDictionary<string, string> DealerCardUnits { get; init; }
    public required RulesTuning Rules { get; init; }

    /// <summary>The enemy row with the given id.</summary>
    /// <exception cref="TuningValidationException">If no such enemy is defined.</exception>
    public EnemyTuning Enemy(string id) =>
        Enemies.FirstOrDefault(e => string.Equals(e.Id, id, StringComparison.Ordinal))
        ?? throw new TuningValidationException($"No enemy defined with id '{id}'.");

    /// <summary>The encounter with the given id.</summary>
    /// <exception cref="TuningValidationException">If no such encounter is defined.</exception>
    public EncounterTuning Encounter(string id) =>
        Encounters.FirstOrDefault(e => string.Equals(e.Id, id, StringComparison.Ordinal))
        ?? throw new TuningValidationException($"No encounter defined with id '{id}'.");

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

/// <summary>
/// Simulation timing. NOT SPECIFIED BY THE DESIGN - see the $comment block in data/tuning.json.
/// </summary>
/// <remarks>
/// ARCHITECTURE.md requires fixed ticks and names rounding as part of the forecast contract, but
/// gives neither. Nothing in the resolver rounds: positions, health, and damage stay double, and
/// the tick only decides when things are sampled.
/// </remarks>
public sealed record SimTuning
{
    public required double TickSeconds { get; init; }

    /// <summary>Whole ticks in <paramref name="seconds"/>, rounded to nearest.</summary>
    /// <remarks>
    /// The loader rejects any tuned duration that is not a whole multiple of the tick, so this
    /// never silently truncates a value the designer meant to be exact.
    /// </remarks>
    public int TicksIn(double seconds) => (int)Math.Round(seconds / TickSeconds, MidpointRounding.AwayFromZero);
}

/// <summary>
/// The tower side of combat. NOT SPECIFIED BY THE DESIGN.
/// </summary>
/// <remarks>
/// Card power is damage per shot. One shared cooldown rather than per-family cooldowns: the
/// resolver contract in design/05-battlefield.md names cooldown as a shared input, so it must
/// exist, and a single value keeps a first pass honest about being invented.
/// </remarks>
public sealed record TowerTuning
{
    public required double CooldownSeconds { get; init; }

    /// <summary>Where a junction tower sits on both lanes' paths. Validated against the middle socket.</summary>
    public required double JunctionPathPosition { get; init; }

    /// <summary>
    /// Damage scale for a junction tower, applied in each lane it fires into.
    /// </summary>
    /// <remarks>
    /// design/05-battlefield.md says the junction fires into either lane "at reduced contribution"
    /// and never quantifies it. At 0.5 the junction's total throughput matches a lane socket's while
    /// being split across two lanes - it buys breadth and forfeits focus, on top of forfeiting runs.
    /// </remarks>
    public required double JunctionContributionFraction { get; init; }

    /// <summary>
    /// Whether face cards escape the junction penalty. True per design/04-cards-as-defenses.md:
    /// "may occupy the shared junction socket without the usual contribution penalty."
    /// </summary>
    public required bool JunctionFaceCardExempt { get; init; }
}

/// <summary>Club and Spade keyword magnitudes. NOT SPECIFIED BY THE DESIGN.</summary>
public sealed record SuitTuning
{
    public required ClubTuning Clubs { get; init; }
    public required SpadeTuning Spades { get; init; }
}

/// <summary>Artillery. Keyword: splash.</summary>
public sealed record ClubTuning
{
    /// <summary>Path units either side of the target that also take damage.</summary>
    public required double SplashRadius { get; init; }

    /// <summary>Fraction of the shot dealt to each secondary. Armor and the damage floor apply per hit.</summary>
    public required double SplashFraction { get; init; }
}

/// <summary>Traps and control. Keyword: slow.</summary>
public sealed record SpadeTuning
{
    public required double SlowMultiplier { get; init; }
    public required double SlowSeconds { get; init; }

    /// <summary>
    /// False: a second application refreshes the duration rather than compounding the multiplier.
    /// Stacking would let two Spades approximate a hard stop, which is a different mechanic.
    /// </summary>
    public required bool SlowStacks { get; init; }
}

/// <summary>
/// Tuned parameters for standing orders. NOT SPECIFIED BY THE DESIGN.
/// </summary>
/// <remarks>
/// Hold's socket threshold and Focus's mode are per-tower board state, not tuning - only
/// trigger-on-group needs numbers. Standing orders must be "modeled exactly by the resolver or
/// they do not ship" (design/05-battlefield.md); there is no approximate tier.
/// </remarks>
public sealed record StandingOrderTuning
{
    public required int TriggerGroupMinEnemies { get; init; }
    public required double TriggerGroupRadius { get; init; }
}

/// <summary>Spawn scheduling and Dealer deployment. NOT SPECIFIED BY THE DESIGN.</summary>
public sealed record WaveTuning
{
    /// <summary>Pause between one group finishing its spawns and the next group starting, per lane.</summary>
    public required double GroupGapSeconds { get; init; }

    /// <summary>How Dealer cards past the Vanguard pick a lane.</summary>
    public required string DealerLaneAssignment { get; init; }

    /// <summary>
    /// Whether one Dealer card deploys its enemy row's whole <see cref="EnemyTuning.Count"/>.
    /// </summary>
    /// <remarks>
    /// True: a 3 deploys eight swarm units and a King deploys one siege engine. This is what the
    /// design's own wording describes - "Swarm pack - many, fragile" and "Fast raiders" against a
    /// singular "Siege engine" - and it is what makes the upcard readable as a threat shape.
    /// </remarks>
    public required bool DealerCardDeploysFullPack { get; init; }
}

/// <summary>
/// One encounter's lane stakes and base wave. The Dealer's hand is added to this.
/// </summary>
/// <remarks>
/// NOT SPECIFIED BY THE DESIGN as data - base wave composition exists only as prose in
/// design/example-wave.md, which is the Milestone 3 acceptance test, so it ships as an encounter.
/// </remarks>
public sealed record EncounterTuning
{
    public required string Id { get; init; }

    /// <summary>Lane the Dealer's upcard deploys into.</summary>
    public required int VanguardLane { get; init; }

    /// <summary>One stake per lane, in lane order. Prototype uses bastion and vault only.</summary>
    public required IReadOnlyList<string> LaneStakes { get; init; }

    /// <summary>Base-wave groups per lane, in lane order then spawn order.</summary>
    public required IReadOnlyList<IReadOnlyList<SpawnGroupTuning>> BaseWave { get; init; }
}

/// <summary>
/// A run of one enemy type spawning at that type's own spacing.
/// </summary>
/// <remarks>
/// <see cref="Count"/> overrides the roster's count, because the roster count is the Dealer pack
/// size. Lane two of the worked example needs six fast raiders to forecast the 6 damage the doc
/// states, where the roster says five.
/// </remarks>
public sealed record SpawnGroupTuning
{
    public required string EnemyId { get; init; }
    public required int Count { get; init; }
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
