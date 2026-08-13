using System.Text.Json;
using System.Text.Json.Serialization;
using Bastion.Core.Board;
using Bastion.Core.Diagnostics;
using Bastion.Core.Validation;
using Bastion.Core.Wave;
using Godot;

namespace Bastion.Game.Telemetry;

/// <summary>
/// Writes one JSON line per offered state, for the session that follows it.
/// </summary>
/// <remarks>
/// <para>
/// docs/prototype/VALIDATION.md § Instrumentation, made concrete. <see cref="SessionSnapshot"/>
/// supplies everything derivable from the session; this adds the four things only the interface
/// knows - <b>time to decide</b>, <b>whether placement changed before the choice</b>, <b>which
/// adjustment move was wanted</b>, and <b>whether combat was watched, fast-forwarded, or
/// skipped</b> - and the choice that was actually made.
/// </para>
/// <para>
/// JSON Lines rather than CSV: a state carries seven sockets, thirteen pile entries, and two lanes,
/// and flattening that into columns would either lose the per-socket detail or produce a header
/// nobody can read. One line per state still appends cheaply and still streams into an analysis
/// script.
/// </para>
/// <para>
/// <b>Records the state as offered, then the choice made in it.</b> A line is written when a state
/// is presented; the decision that closed it is attached to that same line when the next state
/// arrives. Logging the choice on its own would leave "the choice made and time to decide"
/// uninterpretable, because a decision only means anything against the state it answered.
/// </para>
/// <para>
/// Oracle-tier values - bust probability, hit and stand expected output, combined utility - are
/// gated by <see cref="DebugGate"/> and land in a separate <c>oracle</c> object. They must never
/// reach a player build (docs/design/09-information-and-ui.md § Debug-only information), and the
/// gate is compile-time so in a normal build the field is absent rather than merely empty.
/// </para>
/// </remarks>
public partial class PlaytestLog : Node
{
    private static readonly JsonSerializerOptions Json = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false,
    };

    private readonly List<string> _lines = [];

    private WaveController _controller = null!;
    private string _directory = string.Empty;
    private string? _path;

    private Entry? _open;
    private ulong _openedAtMsec;
    private int _placementsInState;

    /// <summary>Family-and-socket combinations inspected while the current state has been open.</summary>
    private readonly HashSet<string> _combinationsSeen = [];

    /// <summary>Sockets inspected under any family, while the current state has been open.</summary>
    private readonly HashSet<string> _socketsSeen = [];

    private string? _lastCombination;

    /// <summary>How the last combat was consumed. Set by the playback view, read on the next write.</summary>
    private string? _playbackDisposition;

    /// <summary>
    /// Attaches a log for this session, or returns null when logging is off.
    /// </summary>
    /// <remarks>
    /// One file per session, named for the arm and case, so an analysis pass can select on either
    /// without opening anything. Called from the composition root, after the controller exists and
    /// before the first wave, so the opening state is captured like any other.
    /// </remarks>
    public static PlaytestLog? Attach(
        Node parent,
        WaveController controller,
        Input.BoardInteraction interaction,
        string directory,
        bool enabled)
    {
        ArgumentNullException.ThrowIfNull(parent);
        ArgumentNullException.ThrowIfNull(controller);
        ArgumentNullException.ThrowIfNull(interaction);

        if (!enabled)
        {
            return null;
        }

        var log = new PlaytestLog
        {
            Name = "PlaytestLog",
            _controller = controller,
            _directory = directory,
        };

        parent.AddChild(log);

        controller.StateChanged += log.OnStateChanged;
        controller.MoveRejected += log.OnMoveRejected;
        interaction.CandidateInspected += log.OnCandidateInspected;

        return log;
    }

    /// <summary>Records how the player consumed combat playback.</summary>
    /// <remarks>
    /// The pre-committed reading is that skipping is information rather than failure - <i>"if it is
    /// always skipped, that is information, not failure"</i> - so this is logged flatly, with no
    /// judgement attached to any value.
    /// </remarks>
    public void RecordPlayback(string disposition) => _playbackDisposition = disposition;

    public override void _ExitTree()
    {
        // A session ends by closing the window, which is not a state change - so the state that was
        // on screen when it closed would otherwise never be written. That is the state a facilitator
        // most wants: the one the player was still looking at. Recorded with no choice against it,
        // which is itself the finding when a state is offered and never answered.
        if (_open is not null)
        {
            _open.DecisionMilliseconds = (long)(Time.GetTicksMsec() - _openedAtMsec);
            _open.Choice = null;
            _open.Abandoned = true;

            _lines.Add(JsonSerializer.Serialize(_open, Json));
            _open = null;
        }

        Flush();
    }

    private void OnStateChanged()
    {
        // The state that was on screen is now answered. Close it before opening the next.
        CloseOpenEntry();

        WaveSession session = _controller.Session;

        _open = new Entry
        {
            Sequence = _lines.Count,
            OfferedAtUtc = DateTime.UtcNow.ToString("O"),
            State = SessionSnapshot.Capture(session, _controller.Fixture?.Id),
            FixtureQuestion = _controller.Fixture?.Question,
            PlaybackDisposition = TakePlaybackDisposition(),
            Oracle = Oracle.For(session),
        };

        _openedAtMsec = Time.GetTicksMsec();
        _placementsInState = 0;

        _combinationsSeen.Clear();
        _socketsSeen.Clear();
        _lastCombination = null;
    }

    /// <summary>
    /// Records that one family-and-socket combination was looked at in the open state.
    /// </summary>
    /// <remarks>
    /// Three separate measurements come out of this, and they answer different questions
    /// (docs/prototype/VALIDATION.md § Improved-encounter instrumentation). <b>Distinct sockets</b>
    /// and <b>distinct combinations</b> say how much of the candidate space was swept - approaching
    /// the whole of it is the oracle signal. <b>Revisits</b> say something else: returning to a
    /// combination already seen is a player weighing two options against each other, which is the
    /// 2-3 plausible placements the design is aiming for rather than a failure.
    /// </remarks>
    private void OnCandidateInspected(Family family, SocketRef socket)
    {
        if (_open is null)
        {
            return;
        }

        string combination = $"{family}@{SessionSnapshot.Describe(socket)}";

        _socketsSeen.Add(SessionSnapshot.Describe(socket));

        // A revisit, not merely a repeat: crossing back onto the combination you just left is
        // pointer noise, whereas returning to one you left earlier is a comparison.
        if (!_combinationsSeen.Add(combination) && combination != _lastCombination)
        {
            _open.CandidateRevisits++;
        }

        _lastCombination = combination;
    }

    /// <summary>
    /// Attaches the decision that closed the open state, and writes the line.
    /// </summary>
    /// <remarks>
    /// The choice is derived by comparing the phase that was offered with the one that followed,
    /// rather than by having every control report itself: the session is the authority on what
    /// actually happened, and a control that forgot to report would leave a silent hole.
    /// </remarks>
    private void CloseOpenEntry()
    {
        if (_open is null)
        {
            return;
        }

        _open.DecisionMilliseconds = (long)(Time.GetTicksMsec() - _openedAtMsec);
        _open.Choice = ChoiceFrom(_open.State.Phase, _controller.Session.Phase);

        // "Whether placement changed before the choice": consecutive placements inside one offered
        // state mean the player rearranged the board before committing to hit or stand.
        _open.PlacementChangedBeforeChoice = _placementsInState > 1;

        _open.CandidateSocketsHovered = _socketsSeen.Count;
        _open.CandidateCombinationsHovered = _combinationsSeen.Count;

        _lines.Add(JsonSerializer.Serialize(_open, Json));
        _open = null;

        Flush();
    }

    private static string ChoiceFrom(string offered, WavePhase next) => (offered, next) switch
    {
        (nameof(WavePhase.AwaitingPlacement), _) => "place",
        (nameof(WavePhase.DrawDecision), WavePhase.AwaitingPlacement) => "hit",
        (nameof(WavePhase.DrawDecision), WavePhase.BustLocked) => "hit-bust",
        (nameof(WavePhase.DrawDecision), WavePhase.AdjustmentWindow) => "stand",
        (nameof(WavePhase.AdjustmentWindow), WavePhase.AdjustmentWindow) => "adjust",
        (nameof(WavePhase.AdjustmentWindow), WavePhase.Locked) => "lock",
        (_, WavePhase.AwaitingPlacement) => "next-wave",
        _ => "advance",
    };

    private void OnMoveRejected(string intent, string reason)
    {
        // The wanted move. Recorded against the state it was wanted in, which is the only place it
        // means anything.
        _open?.MovesWanted.Add(new WantedMove { Intent = intent, Reason = reason });
    }

    private string? TakePlaybackDisposition()
    {
        string? disposition = _playbackDisposition;
        _playbackDisposition = null;

        return disposition;
    }

    private void Flush()
    {
        if (_lines.Count == 0)
        {
            return;
        }

        // Named on first write, not at attach: the arm and the case are only known once the
        // controller has been configured, and that happens after this node exists.
        _path ??= PathFor(_directory, _controller);

        try
        {
            string globalPath = ProjectSettings.GlobalizePath(_path);
            Directory.CreateDirectory(Path.GetDirectoryName(globalPath)!);
            File.WriteAllLines(globalPath, _lines);
        }
        catch (IOException ex)
        {
            // A failed write must not take the session down: the playtest is the point and the log
            // is the record of it. Say so loudly and carry on.
            GD.PrintErr($"[21 Bastion] Could not write the session log to {_path}: {ex.Message}");
        }
    }

    /// <summary>One file per session, named for the arm and the case it ran.</summary>
    /// <remarks>
    /// Both are in the name so an analysis pass can select on either without opening anything -
    /// which matters most for the arm, since comparing arms is the point of the exercise.
    /// </remarks>
    private static string PathFor(string directory, WaveController controller)
    {
        string stamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");
        string arm = controller.Tuning?.March.ActivePreset ?? "unset";
        string fixtureId = controller.Fixture?.Id ?? "freeplay";

        return $"{directory.TrimEnd('/')}/{stamp}-arm{arm}-{fixtureId}.jsonl";
    }

    /// <summary>One offered state and the decision that closed it.</summary>
    private sealed class Entry
    {
        public required int Sequence { get; init; }
        public required string OfferedAtUtc { get; init; }
        public required StateRecord State { get; init; }

        /// <summary>What the battery case was asking, when this is a scripted state.</summary>
        public string? FixtureQuestion { get; init; }

        /// <summary>Milliseconds the state was on screen before the player acted.</summary>
        public long DecisionMilliseconds { get; set; }

        public string? Choice { get; set; }

        /// <summary>The session ended with this state still on screen and unanswered.</summary>
        public bool Abandoned { get; set; }

        public bool PlacementChangedBeforeChoice { get; set; }

        /// <summary>Distinct sockets whose candidate consequences were inspected in this state.</summary>
        public int CandidateSocketsHovered { get; set; }

        /// <summary>Distinct family-and-socket combinations inspected. The brute-force measurement.</summary>
        public int CandidateCombinationsHovered { get; set; }

        /// <summary>Returns to a combination inspected earlier: a comparison, not a sweep.</summary>
        public int CandidateRevisits { get; set; }

        /// <summary>Moves attempted and refused while this state was offered.</summary>
        public List<WantedMove> MovesWanted { get; } = [];

        /// <summary>Watched, fast-forwarded, or skipped, when combat ran into this state.</summary>
        public string? PlaybackDisposition { get; init; }

        /// <summary>Absent in a player build. See <see cref="Oracle"/>.</summary>
        public OracleRecord? Oracle { get; init; }
    }

    private sealed class WantedMove
    {
        public required string Intent { get; init; }
        public required string Reason { get; init; }
    }
}
