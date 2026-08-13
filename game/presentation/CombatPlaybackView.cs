using Bastion.Core.Resolve;
using Bastion.Core.Wave;
using Godot;

namespace Bastion.Game.Presentation;

/// <summary>
/// Drives combat playback: it replays the recorded wave, it never re-simulates it.
/// </summary>
/// <remarks>
/// <para>
/// Once placement locks (on a stand <i>or</i> a bust), this builds a <see cref="TimelinePlayer"/> over
/// the <see cref="FinalForecast"/>'s timeline and steps a cursor through it, feeding each frame to the
/// battlefield to draw. Because the player only reads recorded events, what the screen shows can never
/// contradict the forecast the player was promised - there is one simulation path, and this is not on
/// it (docs/ARCHITECTURE.md).
/// </para>
/// <para>
/// It feeds the board two things per step: the frame, which is where everything <i>is</i>, and the
/// events crossed since the last step, which is what just <i>happened</i>. The second is why towers
/// visibly fire - <see cref="TimelinePlayer.EventsBetween"/> is half-open, so advancing the cursor
/// neither replays nor drops an event at a boundary, and every flash on screen corresponds to exactly
/// one recorded event.
/// </para>
/// <para>
/// Watchable, fast-forwardable, skippable, as the core loop requires: play/pause, a speed multiplier,
/// and a skip that jumps the cursor to the recorded end. It shares the action bar with
/// <c>PhaseControls</c> and shows only during the combat phases.
/// </para>
/// </remarks>
public partial class CombatPlaybackView : HBoxContainer
{
    private static readonly float[] Speeds = [1f, 2f, 4f];

    private WaveController _controller = null!;
    private BattlefieldView _battlefield = null!;
    private PostWaveView _postWave = null!;
    private TimelineView _timeline = null!;

    private TimelinePlayer? _player;
    private double _cursor;
    private double _previousCursor;
    private int _speedIndex;
    private int _fastestSpeedUsed;
    private bool _skipped;
    private bool _playing;
    private bool _finished;

    private Label _status = null!;
    private ProgressBar _progress = null!;
    private Button _playButton = null!;
    private Button _speedButton = null!;
    private Button _skipButton = null!;
    private Button _nextWaveButton = null!;

    /// <summary>
    /// Raised once a wave's playback ends, saying how the player consumed it.
    /// </summary>
    /// <remarks>
    /// One of the four instrumentation items only the interface can see. The transport is the only
    /// thing that knows whether a wave was watched through, hurried, or dismissed, and
    /// <c>game/telemetry/PlaytestLog.cs</c> is the only listener.
    /// </remarks>
    [Signal]
    public delegate void PlaybackFinishedEventHandler(string disposition);

    public void Bind(
        WaveController controller, BattlefieldView battlefield, PostWaveView postWave, TimelineView timeline)
    {
        _controller = controller;
        _battlefield = battlefield;
        _postWave = postWave;
        _timeline = timeline;
        _controller.StateChanged += OnStateChanged;
    }

    public override void _Ready()
    {
        AddThemeConstantOverride("separation", 16);

        var left = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        left.AddThemeConstantOverride("separation", 8);
        AddChild(left);

        _status = new Label { ThemeTypeVariation = BastionTheme.Mono };
        left.AddChild(_status);

        _progress = new ProgressBar
        {
            ShowPercentage = false,
            CustomMinimumSize = new Vector2(0f, 8f),
            MaxValue = 1.0,
        };
        left.AddChild(_progress);

        var transport = new HBoxContainer();
        transport.AddThemeConstantOverride("separation", 8);
        left.AddChild(transport);

        _playButton = new Button { Text = "Play" };
        _playButton.Pressed += TogglePlay;
        transport.AddChild(_playButton);

        _speedButton = new Button { Text = "×1" };
        _speedButton.Pressed += CycleSpeed;
        transport.AddChild(_speedButton);

        _skipButton = new Button { Text = "Skip to end" };
        _skipButton.Pressed += Skip;
        transport.AddChild(_skipButton);

        var primary = new VBoxContainer
        {
            CustomMinimumSize = new Vector2(230f, 0f),
            Alignment = BoxContainer.AlignmentMode.Center,
        };
        AddChild(primary);

        _nextWaveButton = new Button
        {
            Text = "Settle and begin next wave",
            ThemeTypeVariation = BastionTheme.PrimaryButton,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            Visible = false,
        };
        _nextWaveButton.Pressed += () => _controller.SettleAndBeginNextWave();
        primary.AddChild(_nextWaveButton);

        Visible = false;
    }

    public override void _Process(double delta)
    {
        if (!_playing || _player is null)
        {
            return;
        }

        _previousCursor = _cursor;
        _cursor += delta * Speeds[_speedIndex];

        bool reachedEnd = _cursor >= _player.Duration;
        if (reachedEnd)
        {
            _cursor = _player.Duration;
            _playing = false;
        }

        // Frame first, then the events: the board resolves an effect's position from where the units
        // are now, so it needs the frame in hand before it is told what just happened to them.
        _battlefield.SetFrame(_player.FrameAt(_cursor));
        _battlefield.PushEvents(_player.EventsBetween(_previousCursor, _cursor));

        // The same cursor on the timeline, so the wave can be watched against the schedule that
        // promised it rather than only in the abstract.
        _timeline.SetCursor(_cursor);

        UpdateStatus();

        if (reachedEnd)
        {
            Finish();
        }
    }

    private void OnStateChanged()
    {
        bool inCombat = _controller.Session.Phase is WavePhase.Locked or WavePhase.BustLocked;

        if (inCombat && _player is null)
        {
            EnterCombat();
        }
        else if (!inCombat)
        {
            ExitCombat();
        }
    }

    private void EnterCombat()
    {
        _player = new TimelinePlayer(_controller.Forecast.Timeline, _controller.Tuning);
        _cursor = 0.0;
        _previousCursor = 0.0;
        _speedIndex = 0;
        _fastestSpeedUsed = 0;
        _skipped = false;
        _playing = false;
        _finished = false;

        _playButton.Text = "Play";
        _playButton.Disabled = false;   // Finish() disables it; a new wave gets a working transport.
        _speedButton.Text = "×1";
        _nextWaveButton.Visible = false;
        Visible = true;

        _battlefield.SetFrame(_player.FrameAt(0.0));
        _timeline.SetCursor(0.0);
        UpdateStatus();
    }

    private void ExitCombat()
    {
        _player = null;
        _playing = false;
        _finished = false;
        Visible = false;
        _nextWaveButton.Visible = false;
        _battlefield.SetFrame(null);
        _timeline.SetCursor(null);
    }

    private void TogglePlay()
    {
        if (_player is null || _finished)
        {
            return;
        }

        _playing = !_playing;
        _playButton.Text = _playing ? "Pause" : "Play";
        UpdateStatus();
    }

    private void CycleSpeed()
    {
        _speedIndex = (_speedIndex + 1) % Speeds.Length;
        _fastestSpeedUsed = System.Math.Max(_fastestSpeedUsed, _speedIndex);
        _speedButton.Text = $"×{Speeds[_speedIndex]:0}";
    }

    private void Skip()
    {
        if (_player is null)
        {
            return;
        }

        _skipped = true;
        _cursor = _player.Duration;
        _previousCursor = _cursor;
        _playing = false;

        // No event flush on a skip: a wave's worth of flashes in one frame would be noise, and the
        // review panel is the account of what happened.
        _battlefield.SetFrame(_player.FrameAt(_cursor));
        _timeline.SetCursor(_cursor);
        UpdateStatus();
        Finish();
    }

    private void Finish()
    {
        _playButton.Text = "Play";
        _playButton.Disabled = true;

        if (_finished)
        {
            return;
        }

        _finished = true;
        _nextWaveButton.Visible = true;

        // The boundary is worth naming on the button that crosses it: the towers the player spent the
        // encounter building do not come back, and finding that out afterwards is the wrong order.
        _nextWaveButton.Text = _controller.Session.IsFinalWaveOfEncounter
            ? "Encounter over — begin the next (board resets)"
            : "Settle and begin next wave";
        UpdateStatus();
        _postWave.Show(_controller.Forecast);

        // "Combat watched / fast-forwarded / skipped" carries a pre-committed reading: if it is
        // always skipped, that is information, not failure (docs/prototype/VALIDATION.md).
        EmitSignal(SignalName.PlaybackFinished, Disposition());
    }

    /// <summary>
    /// How this wave's combat was consumed: skipped, fast-forwarded, or watched.
    /// </summary>
    /// <remarks>
    /// Skipping wins over speed because a player who raised the speed and then gave up did give up.
    /// Speed is read from the highest setting reached rather than the current one, since the
    /// transport cycles back round to x1.
    /// </remarks>
    private string Disposition() =>
        _skipped ? "skipped"
        : _fastestSpeedUsed > 0 ? "fast-forwarded"
        : "watched";

    private void UpdateStatus()
    {
        if (_player is null)
        {
            return;
        }

        string state = _finished ? "complete" : _playing ? "playing" : "paused";
        _status.Text = $"{_cursor,5:0.0}s / {_player.Duration:0.0}s   {state}";
        _progress.Value = _player.Duration > 0.0 ? _cursor / _player.Duration : 1.0;
    }
}
