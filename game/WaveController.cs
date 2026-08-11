using Bastion.Core.Board;
using Bastion.Core.Cards;
using Bastion.Core.Config;
using Bastion.Core.Resolve;
using Bastion.Core.Validation;
using Bastion.Core.Wave;
using Godot;

namespace Bastion.Game;

/// <summary>
/// The one adapter between the Godot layer and the engine-free core.
/// </summary>
/// <remarks>
/// <para>
/// Holds the current immutable <see cref="WaveSession"/> and exposes the intent methods the UI drives
/// it with. Every transition replaces the held session and raises <see cref="StateChanged"/>; views
/// re-read the session on that signal and rebuild. That is the whole update loop, and it is why the
/// Visible Threat "updating on every draw and placement" needs no special plumbing - a placement is a
/// state change like any other.
/// </para>
/// <para>
/// The controller adds no game rules. Phase boundaries, family locking, and the one-move adjustment
/// window are all enforced inside <see cref="WaveSession"/>; this only forwards calls and reports the
/// result. The UI disables controls per phase so an illegal call is never issued, but if one were, the
/// session would throw rather than the controller quietly permitting it - and <see cref="Apply"/>
/// catches that rather than letting it escape into a signal handler mid-emission.
/// </para>
/// <para>
/// It is also where the session's derived reads are cached. <see cref="WaveSession.Forecast"/> is a
/// full resolver run - every lane simulated live and again empty for the counterfactual - and several
/// views want it from the same signal; <see cref="WaveSession.Board"/> re-derives run links on every
/// call and one view asks for it on every redraw. The session is immutable, so each is computed at
/// most once per state and thrown away with it. Views read these, never the session's methods.
/// </para>
/// </remarks>
public partial class WaveController : Node
{
    private int _seed;

    private BoardState? _board;
    private FinalForecast? _forecast;
    private VisibleThreat? _visibleThreat;

    [Signal]
    public delegate void StateChangedEventHandler();

    /// <summary>
    /// A move the player tried to make and the core refused.
    /// </summary>
    /// <remarks>
    /// This is the <b>wanted-move</b> instrumentation. VALIDATION.md asks for
    /// "adjustment-window usage, including which move was <i>wanted</i> where the interface can
    /// capture it", with a pre-committed reading: players consistently wanting two moves opens the
    /// relic path, and never using the window at all makes it a candidate for deletion. A second
    /// attempt inside the adjustment window is precisely that signal, and the core already announces
    /// it by refusing - so the refusal is surfaced rather than swallowed.
    /// </remarks>
    [Signal]
    public delegate void MoveRejectedEventHandler(string intent, string reason);

    /// <summary>The tuning this run uses. Loaded once by the composition root and passed in.</summary>
    public TuningData Tuning { get; private set; } = null!;

    /// <summary>The encounter being played.</summary>
    public EncounterTuning Encounter { get; private set; } = null!;

    /// <summary>The current wave. Set by <see cref="Configure"/> before any view reads it.</summary>
    public WaveSession Session { get; private set; } = null!;

    /// <summary>
    /// The battery case this session is running, or null for free play.
    /// </summary>
    /// <remarks>
    /// Held so the log can name the state a decision was made in - "the choice made" is only
    /// interpretable against the state it was offered in (docs/prototype/VALIDATION.md
    /// § Instrumentation). No view reads it; it is not player-facing information.
    /// </remarks>
    public BatteryFixture? Fixture { get; private set; }

    /// <summary>The Open/Held threshold, read once from tuning.</summary>
    public double Threshold => Tuning.Rules.OpenHeldThresholdFraction;

    /// <summary>The current board: this hand's towers plus the carried-over ones. Cached per state.</summary>
    public BoardState Board => _board ??= Session.Board();

    /// <summary>
    /// The Final Forecast - the combat contract. Only legal once the Dealer has resolved; the caller
    /// checks the phase, exactly as it would calling the session directly.
    /// </summary>
    public FinalForecast Forecast => _forecast ??= Session.Forecast();

    /// <summary>The Visible Threat. Only legal at the draw decision, for the same reason.</summary>
    public VisibleThreat VisibleThreat => _visibleThreat ??= Session.VisibleThreatNow();

    /// <summary>Wires the controller to its tuning and opens the first wave.</summary>
    public void Configure(TuningData tuning, EncounterTuning encounter, int seed)
    {
        Tuning = tuning;
        Encounter = encounter;
        _seed = seed;

        SetSession(WaveSession.Begin(tuning, encounter, Shoe.Create(tuning, seed)));
    }

    /// <summary>
    /// Opens a scripted battery case instead of a seeded wave.
    /// </summary>
    /// <remarks>
    /// The case runs its own script to the state it offers and hands control over there, so the
    /// player meets the decision rather than playing up to it. Its cards are scripted, so the seed
    /// is irrelevant - the fixture states the pile because for some cases the pile <i>is</i> the
    /// state (docs/prototype/VALIDATION.md § Scripted battery).
    /// </remarks>
    public void ConfigureFromFixture(TuningData tuning, BatteryFixture fixture)
    {
        ArgumentNullException.ThrowIfNull(tuning);
        ArgumentNullException.ThrowIfNull(fixture);

        Tuning = tuning;
        Encounter = tuning.Encounter(fixture.EncounterId);
        Fixture = fixture;
        _seed = 0;

        SetSession(fixture.Open(tuning));
    }

    /// <summary>The rank waiting to be placed, or null when none is pending.</summary>
    public Rank? PendingRank =>
        Session.Phase == WavePhase.AwaitingPlacement && Session.PendingRanks.Count > 0
            ? Session.PendingRanks[0]
            : null;

    public void Place(Family family, SocketRef socket) =>
        Apply(s => s.Place(family, socket), $"place {family} at {socket}");

    public void Hit() => Apply(s => s.Hit(), "hit");

    public void Stand() => Apply(s => s.Stand(), "stand");

    public void Relocate(SocketRef from, SocketRef to) =>
        Apply(s => s.RelocateTower(from, to), $"relocate {from} to {to}");

    public void Swap(SocketRef a, SocketRef b) =>
        Apply(s => s.SwapTowers(a, b), $"swap {a} with {b}");

    public void SetOrder(SocketRef socket, StandingOrder order) =>
        Apply(s => s.SetStandingOrder(socket, order), $"order {socket}: {order}");

    public void Lock() => Apply(s => s.Lock(), "lock");

    /// <summary>
    /// Settles the finished wave and opens the next, carrying towers and the shoe forward.
    /// </summary>
    /// <remarks>
    /// Both halves come from the session, which is the only thing that knows where in the encounter
    /// this wave sat: at the boundary it carries nothing forward and the count restarts at one. The
    /// shoe crosses the boundary either way - it is the run's deck, not the encounter's.
    /// </remarks>
    public void SettleAndBeginNextWave()
    {
        int nextWave = Session.NextWaveNumber;
        (IReadOnlyList<TowerState> carried, Shoe shoe) = Session.Settle();

        SetSession(WaveSession.Begin(Tuning, Encounter, shoe, carried, nextWave));
    }

    /// <summary>
    /// Runs a transition and announces the result, or reports a rejected move and changes nothing.
    /// </summary>
    /// <remarks>
    /// The session is the authority on what is legal, and it says so by throwing. Catching here rather
    /// than at each call site means a rejected move costs a log line instead of an exception unwinding
    /// through a Godot signal handler - and because the transition is applied only on success, the
    /// session is never left half-changed.
    /// </remarks>
    private void Apply(Func<WaveSession, WaveSession> transition, string intent)
    {
        WaveSession next;

        try
        {
            next = transition(Session);
        }
        catch (InvalidOperationException ex)
        {
            GD.PrintErr($"[21 Bastion] Move rejected by the core: {ex.Message}");
            EmitSignal(SignalName.MoveRejected, intent, ex.Message);
            return;
        }

        SetSession(next);
    }

    /// <summary>Adopts a new session, drops everything derived from the old one, and announces it.</summary>
    private void SetSession(WaveSession session)
    {
        Session = session;
        _board = null;
        _forecast = null;
        _visibleThreat = null;

        EmitSignal(SignalName.StateChanged);
    }
}
