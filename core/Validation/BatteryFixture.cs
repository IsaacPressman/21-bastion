using Bastion.Core.Board;
using Bastion.Core.Cards;
using Bastion.Core.Config;
using Bastion.Core.Wave;

namespace Bastion.Core.Validation;

/// <summary>
/// One scripted state from the validation battery, and the script that reaches it.
/// </summary>
/// <remarks>
/// <para>
/// docs/prototype/VALIDATION.md § Scripted battery lists ten states the playtest must offer. Several
/// of them name a <i>contrast</i> rather than a single state - "hard 16 as 10+6 versus 3+3+5+5",
/// "a Dealer showing a King versus a Dealer showing a 3" - so a battery item becomes one or more
/// cases, and a case is what a player is actually sat down in front of. <see cref="BatteryItem"/>
/// records which of the ten this case serves.
/// </para>
/// <para>
/// Each case is presented <b>twice, with different presentation, so players cannot answer from
/// memory</b>. That is <see cref="LaneMirror"/>: variant B mirrors the two lanes wholesale and swaps
/// the opening deal order, which leaves the decision isomorphic and nothing on screen the same.
/// </para>
/// <para>
/// Fixtures are data (<c>data/battery.json</c>), not code, so a facilitator can retune one between
/// sessions. Engine-free, so the regression suite can replay every fixture headlessly - which is
/// what VALIDATION.md step 4 means by "on the scripted fixtures".
/// </para>
/// </remarks>
public sealed record BatteryFixture
{
    /// <summary>Stable id, used by <c>--fixture</c> and in the log. Lower-case, hyphenated.</summary>
    public required string Id { get; init; }

    /// <summary>Which of the ten battery items in VALIDATION.md this case serves, 1-10.</summary>
    public required int BatteryItem { get; init; }

    /// <summary>What this state is asking the player, for the facilitator's screen and the log.</summary>
    public required string Question { get; init; }

    /// <summary>Encounter id, resolved against <see cref="TuningData.Encounters"/>.</summary>
    public required string EncounterId { get; init; }

    /// <summary>
    /// The shoe, top first: the Dealer's upcard, the hole card, then the player's cards, then
    /// whatever the Dealer draws.
    /// </summary>
    /// <remarks>
    /// Fully scripted rather than seeded. No seed is guaranteed to produce "a hand at 18 where the
    /// only 21 is a single surviving rank", and the remaining composition is part of the state being
    /// offered - so the pile has to be stated, not searched for.
    /// </remarks>
    public required IReadOnlyList<Rank> CardOrder { get; init; }

    /// <summary>Towers carried in from an imagined earlier wave, at x1.00.</summary>
    public IReadOnlyList<PersistedTower> Persisted { get; init; } = [];

    /// <summary>Actions to run before handing control over. The state they end in is the offered one.</summary>
    public IReadOnlyList<ScriptStep> Script { get; init; } = [];

    /// <summary>The phase the script is expected to end in. Checked when the fixture is opened.</summary>
    public required WavePhase OfferedAt { get; init; }

    /// <summary>
    /// Opens this fixture: builds the scripted shoe, seats the carried-over towers, and runs the
    /// script up to the offered state.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// If the script does not reach <see cref="OfferedAt"/>. A fixture that lands somewhere else is
    /// offering a different decision than the one it claims to, which is worse than not running.
    /// </exception>
    public WaveSession Open(TuningData tuning)
    {
        ArgumentNullException.ThrowIfNull(tuning);

        WaveSession session = WaveSession.Begin(
            tuning,
            tuning.Encounter(EncounterId),
            Shoe.FromOrder(CardOrder),
            [.. Persisted.Select(p => p.Build(tuning))]);

        foreach (ScriptStep step in Script)
        {
            session = step.ApplyTo(session);
        }

        if (session.Phase != OfferedAt)
        {
            throw new InvalidOperationException(
                $"Battery fixture '{Id}' claims to offer a decision at {OfferedAt} but its script ends at {session.Phase}.");
        }

        return session;
    }
}

/// <summary>A tower already on the board when the fixture opens.</summary>
/// <remarks>
/// Carried-over towers are at x1.00 Formation Strength and carry no run bonus, exactly as
/// <see cref="WaveSession.Settle"/> leaves them (docs/design/05-battlefield.md § Persistence). The
/// socket-capacity fixture is the reason this exists: forced replacement is only reachable from a
/// board that is already full.
/// </remarks>
public sealed record PersistedTower
{
    public required Rank Rank { get; init; }
    public required Family Family { get; init; }
    public required SocketRef Socket { get; init; }

    public TowerState Build(TuningData tuning) => TowerState.Place(
        tuning,
        new Card(Rank),
        Family,
        Socket,
        formationMultiplier: tuning.FormationStrength.Persisted);
}

/// <summary>One scripted action: place the pending card, hit, or stand.</summary>
public sealed record ScriptStep
{
    public required ScriptAction Action { get; init; }

    /// <summary>Where a <see cref="ScriptAction.Place"/> puts the card. Ignored otherwise.</summary>
    public SocketRef Socket { get; init; }

    /// <summary>The family a <see cref="ScriptAction.Place"/> locks in. Ignored otherwise.</summary>
    public Family Family { get; init; }

    public WaveSession ApplyTo(WaveSession session)
    {
        ArgumentNullException.ThrowIfNull(session);

        return Action switch
        {
            ScriptAction.Place => session.Place(Family, Socket),
            ScriptAction.Hit => session.Hit(),
            ScriptAction.Stand => session.Stand(),
            _ => throw new InvalidOperationException($"Unknown script action {Action}."),
        };
    }
}

public enum ScriptAction
{
    Place,
    Hit,
    Stand,
}
