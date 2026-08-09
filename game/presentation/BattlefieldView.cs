using Bastion.Core.Board;
using Bastion.Core.Cards;
using Bastion.Core.Config;
using Bastion.Core.March;
using Bastion.Core.Resolve;
using Bastion.Core.Wave;
using Bastion.Game.Input;
using Godot;

namespace Bastion.Game.Presentation;

/// <summary>
/// The spatial battlefield: lanes, sockets, the entry point, placed towers, the march cost drawn on
/// the lane, and - during combat - the units and effects of a recorded wave.
/// </summary>
/// <remarks>
/// <para>
/// A thin view. It reads the immutable <see cref="WaveSession"/> and, during playback, a
/// <see cref="PlaybackFrame"/> and event batches handed to it by the playback node - it computes no
/// combat outcome of its own. The march overlay is the one place it does arithmetic, and that
/// arithmetic is pure geometry (<see cref="Engagement.WindowForSocket"/>): it shades <b>which socket
/// windows the next march step would cut into</b>, on the lane, never a single summed engagement
/// number (docs/design/09-information-and-ui.md).
/// </para>
/// <para>
/// It is also the primary input surface. Placement and adjustment are spatial decisions, so they are
/// made by clicking a socket here rather than picking a name off a list; the board reports the click
/// to <see cref="BoardInteraction"/> and the session rules on it.
/// </para>
/// <para>
/// <b>Nothing here combines a hand consequence with a battlefield one, and nothing marks a socket as
/// a good choice.</b> Highlights answer "what am I pointing at" and "what would this gesture do" -
/// never "what should I do". Judging the board is the player's job.
/// </para>
/// </remarks>
public partial class BattlefieldView : Node2D
{
    /// <summary>
    /// Wide enough for the junction slot to sit clear of both lane bands rather than straddling them.
    /// </summary>
    private const float LaneGap = 66f;

    private const float MaxLaneHeight = 132f;
    private const float SocketHalf = 24f;
    private const float EffectSeconds = 0.22f;

    private WaveController _controller = null!;
    private BoardInteraction _interaction = null!;

    private PlaybackFrame? _frame;
    private double _cursor;
    private readonly List<Effect> _effects = [];
    private readonly Dictionary<int, Vector2> _lastSeen = [];

    /// <summary>Proportional face for prose on the board; the mono face for anything columnar.</summary>
    public Font LabelFont { get; set; } = null!;
    public Font MonoFont { get; set; } = null!;

    public void Bind(WaveController controller, BoardInteraction interaction)
    {
        _controller = controller;
        _interaction = interaction;

        _controller.StateChanged += OnStateChanged;
        _interaction.Changed += QueueRedraw;
        _interaction.PointerMoved += QueueRedraw;
    }

    /// <summary>The current playback frame to draw, or null when combat is not playing.</summary>
    public void SetFrame(PlaybackFrame? frame)
    {
        _frame = frame;

        if (frame is null)
        {
            _effects.Clear();
            _lastSeen.Clear();
        }
        else
        {
            _cursor = frame.Time;

            // Remember where everything was, so an effect naming a unit that has just died still has
            // somewhere to point.
            foreach (EnemyPlaybackState enemy in frame.LiveEnemies)
            {
                _lastSeen[enemy.SpawnIndex] = EnemyPosition(enemy);
            }

            _effects.RemoveAll(e => e.Expires <= _cursor);
        }

        QueueRedraw();
    }

    /// <summary>
    /// Flashes the events the cursor just crossed.
    /// </summary>
    /// <remarks>
    /// Every shot, slow, death, and burst drawn here is one the resolver recorded - the view invents
    /// none of them and skips none of them. That is what makes the visible wave a presentation of the
    /// run rather than a second, prettier account of it (docs/ARCHITECTURE.md).
    /// </remarks>
    public void PushEvents(IReadOnlyList<TimelineEvent> events)
    {
        foreach (TimelineEvent evt in events)
        {
            switch (evt)
            {
                case ShotEvent shot:
                    Add(shot.IsSplash ? EffectKind.Splash : EffectKind.Shot,
                        SocketCentre(shot.Socket, shot.LaneIndex), Seen(shot.TargetSpawnIndex));
                    break;

                case SlowEvent slow:
                    Add(EffectKind.Slow, Seen(slow.SpawnIndex), Seen(slow.SpawnIndex));
                    break;

                case DeathEvent death:
                    Add(EffectKind.Death, Seen(death.SpawnIndex), Seen(death.SpawnIndex));
                    break;

                case OverloadEvent overload:
                    foreach (OverloadHit hit in overload.Hits)
                    {
                        Add(EffectKind.Overload, Seen(hit.SpawnIndex), Seen(hit.SpawnIndex));
                    }

                    break;
            }
        }

        QueueRedraw();
    }

    /// <summary>
    /// Turns a click on the board into a placement or an adjustment.
    /// </summary>
    /// <remarks>
    /// Positions come from the event via <see cref="CanvasItem.MakeInputLocal"/>, not from
    /// <c>GetLocalMousePosition</c>. The latter reads wherever the OS cursor happens to be at the
    /// moment the event is handled, which is not necessarily where the event says it happened - it
    /// disagrees under a canvas transform, and is simply wrong for any synthesised event.
    /// </remarks>
    public override void _UnhandledInput(InputEvent @event)
    {
        if (_controller?.Session is null || MakeInputLocal(@event) is not InputEventMouse local)
        {
            return;
        }

        switch (local)
        {
            case InputEventMouseMotion:
                _interaction.Hover(SocketAt(local.Position));
                break;

            case InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left }
                when SocketAt(local.Position) is { } socket:
                _interaction.Click(socket);
                GetViewport().SetInputAsHandled();
                break;

            // Right-click puts down a picked-up tower, so an adjustment gesture is always escapable.
            case InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Right }:
                _interaction.ClearPickedUp();
                break;
        }
    }

    public override void _Draw()
    {
        if (_controller?.Session is null)
        {
            return;
        }

        WaveSession session = _controller.Session;
        TuningData tuning = _controller.Tuning;

        DrawLanes(session, tuning);
        DrawMarchCost(session, tuning);
        DrawHoverAndPickup(tuning);
        DrawEntry(session);
        DrawTowers(tuning);

        if (_frame is not null)
        {
            DrawPlayback(tuning);
        }
        else
        {
            DrawVanguardPreview(session);
        }
    }

    // ---------------------------------------------------------------- geometry

    private Rect2 Area => Layout.BoardArea(GetViewportRect().Size);

    private float PathToX(double position)
    {
        Rect2 area = Area;
        float usable = area.Size.X - Layout.BoardPadLeft - Layout.BoardPadRight;
        double fraction = position / _controller.Tuning.Geometry.PathLength;

        return area.Position.X + Layout.BoardPadLeft + ((float)fraction * usable);
    }

    private float PixelsPerPathUnit()
    {
        Rect2 area = Area;
        return (area.Size.X - Layout.BoardPadLeft - Layout.BoardPadRight)
            / (float)_controller.Tuning.Geometry.PathLength;
    }

    /// <summary>
    /// Lane bands are divided out of the board rect rather than fixed, so the lane count stays a
    /// tuning value all the way to the screen instead of being pinned to two by the drawing code.
    /// </summary>
    private float LaneHeight()
    {
        Rect2 area = Area;
        int lanes = _controller.Tuning.Geometry.Lanes;
        float usable = area.Size.Y - Layout.BoardPadTop - Layout.BoardPadBottom;

        return Mathf.Min(MaxLaneHeight, (usable - (LaneGap * (lanes - 1))) / lanes);
    }

    private float LaneCentreY(int lane)
    {
        Rect2 area = Area;
        int lanes = _controller.Tuning.Geometry.Lanes;
        float height = LaneHeight();
        float block = (height * lanes) + (LaneGap * (lanes - 1));
        float top = area.Position.Y + ((area.Size.Y - block) / 2f);

        return top + (lane * (height + LaneGap)) + (height / 2f);
    }

    private float JunctionCentreY() =>
        (LaneCentreY(0) + LaneCentreY(_controller.Tuning.Geometry.Lanes - 1)) / 2f;

    private Vector2 SocketCentre(SocketRef socket, int laneIndex)
    {
        TuningData tuning = _controller.Tuning;

        return socket.IsJunction
            ? new Vector2(PathToX(tuning.Towers.JunctionPathPosition), JunctionCentreY())
            : new Vector2(PathToX(tuning.Geometry.SocketPositions[socket.SocketIndex]), LaneCentreY(laneIndex));
    }

    private Vector2 SocketCentre(SocketRef socket) =>
        SocketCentre(socket, socket.IsJunction ? 0 : socket.LaneIndex);

    /// <summary>
    /// Where a socket is drawn, for anything that needs to point at one.
    /// </summary>
    /// <remarks>
    /// Exposed so the capture run can click a socket without duplicating this file's geometry - a
    /// second copy would drift and then be testing itself rather than the board.
    /// </remarks>
    public Vector2 ScreenPositionOf(SocketRef socket) => SocketCentre(socket);

    private IEnumerable<SocketRef> AllSockets()
    {
        TuningData tuning = _controller.Tuning;

        for (int lane = 0; lane < tuning.Geometry.Lanes; lane++)
        {
            for (int s = 0; s < tuning.Geometry.SocketPositions.Count; s++)
            {
                yield return SocketRef.InLane(lane, s);
            }
        }

        yield return SocketRef.Junction;
    }

    private SocketRef? SocketAt(Vector2 point)
    {
        foreach (SocketRef socket in AllSockets())
        {
            Vector2 centre = SocketCentre(socket);
            if (new Rect2(centre - new Vector2(SocketHalf, SocketHalf), SocketHalf * 2f, SocketHalf * 2f).HasPoint(point))
            {
                return socket;
            }
        }

        return null;
    }

    // ---------------------------------------------------------------- board

    private void DrawLanes(WaveSession session, TuningData tuning)
    {
        float x0 = PathToX(0.0);
        float x1 = PathToX(tuning.Geometry.PathLength);
        float height = LaneHeight();

        for (int lane = 0; lane < tuning.Geometry.Lanes; lane++)
        {
            float cy = LaneCentreY(lane);
            var band = new Rect2(x0, cy - (height / 2f), x1 - x0, height);

            DrawRect(band, Palette.LaneFill);
            DrawRect(band, Palette.LaneEdge, filled: false, width: 1.5f);

            string stake = lane < session.Encounter.LaneStakes.Count ? session.Encounter.LaneStakes[lane] : "?";
            Text($"LANE {lane}", new Vector2(x0 - Layout.BoardPadLeft + 8f, cy - 6f), Palette.TextDim, 13);
            Text(stake.ToUpperInvariant(), new Vector2(x0 - Layout.BoardPadLeft + 8f, cy + 11f), Palette.TextFaint, 11);

            for (int s = 0; s < tuning.Geometry.SocketPositions.Count; s++)
            {
                DrawSocketSlot(SocketRef.InLane(lane, s));
            }
        }

        DrawSocketSlot(SocketRef.Junction);

        // Labelled at the left margin like the lanes, rather than under the slot where it would land
        // inside the next lane's band.
        Text("JUNCTION", new Vector2(x0 - Layout.BoardPadLeft + 8f, JunctionCentreY() + 4f), Palette.TextFaint, 11);
    }

    private void DrawSocketSlot(SocketRef socket)
    {
        Vector2 centre = SocketCentre(socket);
        var slot = new Rect2(centre - new Vector2(SocketHalf, SocketHalf), SocketHalf * 2f, SocketHalf * 2f);

        DrawRect(slot, Palette.SocketEdge, filled: false, width: 1f);
    }

    private void DrawEntry(WaveSession session)
    {
        float ex = PathToX(session.Entry);
        float top = LaneCentreY(0) - (LaneHeight() / 2f) - 16f;
        float bottom = LaneCentreY(_controller.Tuning.Geometry.Lanes - 1) + (LaneHeight() / 2f) + 16f;

        DrawLine(new Vector2(ex, top), new Vector2(ex, bottom), Palette.Entry, 2f);
        MonoText($"entry {session.Entry:0.0}", new Vector2(ex + 5f, top - 6f), Palette.Entry, 12);
    }

    /// <summary>
    /// Shades, per occupied socket, the slice of its firing window the next march step would remove.
    /// </summary>
    private void DrawMarchCost(WaveSession session, TuningData tuning)
    {
        if (session.Phase != WavePhase.DrawDecision)
        {
            return;
        }

        double step = session.NextStepCost();
        if (step <= 0.0)
        {
            return;   // past the clamp: further cards are free on the clock, so nothing is lost.
        }

        double entryNow = session.Entry;
        double entryNext = entryNow + step;
        double path = tuning.Geometry.PathLength;
        float height = LaneHeight();

        foreach (TowerState tower in _controller.Board.Towers)
        {
            double pos = tower.PositionOn(tuning);
            (double firstNow, double lastNow) = Engagement.WindowForSocket(pos, tower.Range, entryNow, path);
            (double firstNext, _) = Engagement.WindowForSocket(pos, tower.Range, entryNext, path);

            // The advancing entry eats the near end of the window; the lost slice is what it swept past.
            double lostTo = Math.Min(firstNext, lastNow);
            if (lostTo <= firstNow)
            {
                continue;
            }

            float lx = PathToX(firstNow);
            float rx = PathToX(lostTo);

            foreach (int lane in LanesOf(tower, tuning))
            {
                float cy = LaneCentreY(lane);
                DrawRect(new Rect2(lx, cy - (height / 2f) + 1f, rx - lx, height - 2f), Palette.LostWindow);
            }
        }
    }

    /// <summary>
    /// Marks what the pointer is on and, mid-adjustment, what the picked-up tower could do next.
    /// </summary>
    /// <remarks>
    /// Strictly a readback of the gesture in progress. A destination is marked because the player has
    /// already picked a tower up and is choosing where it goes - not to recommend the socket. Nothing
    /// is marked at all until they act.
    /// </remarks>
    private void DrawHoverAndPickup(TuningData tuning)
    {
        if (_frame is not null)
        {
            return;
        }

        if (_interaction.PickedUp is { } picked)
        {
            Outline(picked, Palette.SocketPicked, 2.5f);

            foreach (SocketRef socket in AllSockets())
            {
                if (socket != picked && IsOneMoveFrom(picked, socket, tuning))
                {
                    Outline(socket, Palette.SocketTarget, 1.5f);
                }
            }
        }

        if (_interaction.Hovered is { } hovered && _interaction.BoardAcceptsClicks)
        {
            Outline(hovered, Palette.SocketHover, 2f);
            DrawCoverage(hovered, tuning);
        }
    }

    /// <summary>The window a tower already on this socket covers, shown while the pointer rests on it.</summary>
    private void DrawCoverage(SocketRef socket, TuningData tuning)
    {
        TowerState? tower = _controller.Board.Towers.FirstOrDefault(t => t.Socket == socket);
        if (tower is null)
        {
            return;
        }

        (double first, double last) = Engagement.WindowForSocket(
            tower.PositionOn(tuning), tower.Range, _controller.Session.Entry, tuning.Geometry.PathLength);

        if (last <= first)
        {
            return;
        }

        float height = LaneHeight();

        foreach (int lane in LanesOf(tower, tuning))
        {
            float cy = LaneCentreY(lane);
            DrawRect(new Rect2(PathToX(first), cy - (height / 2f) + 1f, PathToX(last) - PathToX(first), height - 2f),
                Palette.Coverage);
        }
    }

    private void Outline(SocketRef socket, Color colour, float width)
    {
        Vector2 centre = SocketCentre(socket);
        float half = SocketHalf + 3f;

        DrawRect(new Rect2(centre - new Vector2(half, half), half * 2f, half * 2f), colour, filled: false, width: width);
    }

    /// <summary>
    /// Mirrors the session's adjacency rule to show reachable sockets. The session is still the
    /// authority - this only marks what a second click would attempt.
    /// </summary>
    private static bool IsOneMoveFrom(SocketRef a, SocketRef b, TuningData tuning)
    {
        if (a == b || (a.IsJunction && b.IsJunction))
        {
            return false;
        }

        if (a.IsJunction || b.IsJunction)
        {
            SocketRef lane = a.IsJunction ? b : a;
            return lane.SocketIndex == tuning.Geometry.SocketPositions.Count / 2;
        }

        return a.LaneIndex == b.LaneIndex && Math.Abs(a.SocketIndex - b.SocketIndex) == 1;
    }

    private void DrawTowers(TuningData tuning)
    {
        foreach (TowerState tower in _controller.Board.Towers)
        {
            Vector2 centre = SocketCentre(tower.Socket);
            Color fill = tower.Family == Family.Club ? Palette.Club : Palette.Spade;
            var body = new Rect2(centre - new Vector2(20f, 20f), 40f, 40f);

            DrawRect(body, fill);
            DrawRect(body, Palette.SurfaceEdge, filled: false, width: 1f);

            CentredText(RankGlyph(tower.Card), centre + new Vector2(0f, 6f), Palette.OnTower, 18, MonoFont);

            // A carried-over tower runs at x1.00 while this hand's run higher: worth seeing on the
            // board, because it is why an old tower quietly stops mattering.
            if (Math.Abs(tower.FormationMultiplier - 1.0) < 1e-9)
            {
                CentredText("carried", centre + new Vector2(0f, 34f), Palette.TextFaint, 10, LabelFont);
            }

            if (tower.RunBonus > 0.0)
            {
                CentredText($"run +{tower.RunBonus:P0}", centre + new Vector2(0f, -26f), new Color("cbe08a"), 11, LabelFont);
            }
        }
    }

    private void DrawVanguardPreview(WaveSession session)
    {
        int lane = session.Encounter.VanguardLane;
        float x = PathToX(session.Entry);
        float y = LaneCentreY(lane);

        DrawCircle(new Vector2(x, y), 13f, Palette.Vanguard);

        // Labelled above the band, clear of the lane border and the entry line's own label.
        CentredText("Vanguard", new Vector2(x, LaneCentreY(lane) - (LaneHeight() / 2f) - 8f), Palette.Vanguard, 11, LabelFont);
    }

    // ---------------------------------------------------------------- playback

    private void DrawPlayback(TuningData tuning)
    {
        foreach (Effect effect in _effects)
        {
            DrawEffect(effect);
        }

        foreach (EnemyPlaybackState enemy in _frame!.LiveEnemies)
        {
            DrawEnemy(enemy, tuning);
        }

        MonoText($"leaked {_frame.LeakedCount}   ({_frame.LeakDamageSoFar} damage)",
            new Vector2(PathToX(0.0), LaneCentreY(tuning.Geometry.Lanes - 1) + (LaneHeight() / 2f) + 22f),
            Palette.TextDim, 13);
    }

    /// <summary>
    /// One unit, drawn so its type is legible: armour changes its shape, health its size, an aura is
    /// a ring, and where it came from sets its colour.
    /// </summary>
    /// <remarks>
    /// All of it is read off <see cref="EnemyTuning"/> rather than keyed on enemy ids, so a new unit
    /// in <c>data/tuning.json</c> renders sensibly without a matching entry over here.
    /// </remarks>
    private void DrawEnemy(EnemyPlaybackState enemy, TuningData tuning)
    {
        EnemyTuning type = tuning.Enemy(enemy.EnemyId);
        Vector2 centre = EnemyPosition(enemy);
        float radius = RadiusFor(type, tuning);

        Color body = enemy.Source switch
        {
            SpawnSource.Vanguard => Palette.Vanguard,
            SpawnSource.DealerDraw => Palette.DealerEnemy,
            _ => Palette.BaseWaveEnemy,
        };

        if (enemy.IsSlowed)
        {
            body = body.Lerp(Palette.SlowTint, 0.55f);
        }

        if (type.Aura is { } aura)
        {
            DrawArc(centre, (float)aura.Radius * PixelsPerPathUnit(), 0f, Mathf.Tau, 48, Palette.AuraRing, 1.5f);
        }

        if (type.FlatArmor > 0.0)
        {
            // Armour reads as a plated hexagon; the outline thickens with how much of it there is.
            Vector2[] plate = Hexagon(centre, radius);
            DrawColoredPolygon(plate, body);
            DrawPolyline([.. plate, plate[0]], Palette.Text, 1f + (float)type.FlatArmor);
        }
        else
        {
            DrawCircle(centre, radius, body);
        }

        DrawHealthBar(centre, radius, enemy.HealthFraction);
    }

    private void DrawHealthBar(Vector2 centre, float radius, double fraction)
    {
        const float Width = 30f;
        var back = new Rect2(centre.X - (Width / 2f), centre.Y - radius - 10f, Width, 4f);

        DrawRect(back, Palette.HealthBack);
        DrawRect(new Rect2(back.Position, Width * (float)fraction, 4f), Palette.Health);
    }

    private Vector2 EnemyPosition(EnemyPlaybackState enemy) => new(
        PathToX(enemy.Position),
        LaneCentreY(enemy.LaneIndex) + LaneOffset(enemy.SpawnIndex));

    /// <summary>
    /// A fixed per-unit offset across the lane's width, so a bunched group reads as several units
    /// rather than one blob. Derived from the spawn index, so it never moves and never randomises.
    /// </summary>
    /// <remarks>
    /// Two tracks either side of the centreline, never on it: the towers sit on the centreline, and a
    /// unit sharing it would walk through a tower box rather than past it.
    /// </remarks>
    private float LaneOffset(int spawnIndex)
    {
        float track = (LaneHeight() / 2f) - 30f;
        return ((spawnIndex % 2) == 0 ? -1f : 1f) * track;
    }

    private static float RadiusFor(EnemyTuning type, TuningData tuning)
    {
        double min = double.MaxValue;
        double max = double.MinValue;

        foreach (EnemyTuning other in tuning.Enemies)
        {
            min = Math.Min(min, other.Health);
            max = Math.Max(max, other.Health);
        }

        float t = max > min ? (float)((type.Health - min) / (max - min)) : 0.5f;
        return Mathf.Lerp(7f, 17f, t);
    }

    private static Vector2[] Hexagon(Vector2 centre, float radius)
    {
        var points = new Vector2[6];

        for (int i = 0; i < 6; i++)
        {
            float angle = Mathf.Tau * i / 6f;
            points[i] = centre + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
        }

        return points;
    }

    private void DrawEffect(Effect effect)
    {
        // Effects fade over their lifetime rather than blinking out, so a fast wave still reads.
        float life = (float)Math.Clamp((effect.Expires - _cursor) / EffectSeconds, 0.0, 1.0);

        switch (effect.Kind)
        {
            case EffectKind.Shot:
                DrawLine(effect.From, effect.To, Palette.Shot with { A = life }, 2f);
                DrawCircle(effect.To, 4f + (4f * (1f - life)), Palette.Shot with { A = life * 0.8f });
                break;

            case EffectKind.Splash:
                DrawLine(effect.From, effect.To, Palette.Splash with { A = life * 0.6f }, 1f);
                DrawArc(effect.To, 6f + (10f * (1f - life)), 0f, Mathf.Tau, 20, Palette.Splash with { A = life * 0.7f }, 1.5f);
                break;

            case EffectKind.Slow:
                DrawArc(effect.To, 14f, 0f, Mathf.Tau, 24, Palette.SlowHit with { A = life }, 2f);
                break;

            case EffectKind.Death:
                DrawArc(effect.To, 6f + (14f * (1f - life)), 0f, Mathf.Tau, 24, Palette.Death with { A = life }, 2f);
                break;

            case EffectKind.Overload:
                DrawArc(effect.To, 8f + (26f * (1f - life)), 0f, Mathf.Tau, 32, Palette.Overload with { A = life }, 3f);
                break;
        }
    }

    private void Add(EffectKind kind, Vector2 from, Vector2 to) =>
        _effects.Add(new Effect(kind, from, to, _cursor + EffectSeconds));

    private Vector2 Seen(int spawnIndex) =>
        _lastSeen.TryGetValue(spawnIndex, out Vector2 at) ? at : new Vector2(PathToX(0.0), JunctionCentreY());

    // ---------------------------------------------------------------- helpers

    private static IEnumerable<int> LanesOf(TowerState tower, TuningData tuning) =>
        tower.Socket.IsJunction ? Enumerable.Range(0, tuning.Geometry.Lanes) : [tower.Socket.LaneIndex];

    // A low Ace reads as the 1 it is worth, not as an "A" indistinguishable from an 11-power tower.
    private static string RankGlyph(Card card) => card.Rank switch
    {
        Rank.Ace => card.AceHigh ? "A" : "1",
        Rank.Jack => "J",
        Rank.Queen => "Q",
        Rank.King => "K",
        _ => ((int)card.Rank).ToString(),
    };

    private void OnStateChanged()
    {
        _effects.Clear();
        QueueRedraw();
    }

    private void Text(string text, Vector2 position, Color colour, int size) =>
        Text(text, position, colour, size, LabelFont);

    private void MonoText(string text, Vector2 position, Color colour, int size) =>
        Text(text, position, colour, size, MonoFont);

    private void Text(string text, Vector2 position, Color colour, int size, Font? font)
    {
        if (font is not null)
        {
            DrawString(font, position, text, HorizontalAlignment.Left, -1f, size, colour);
        }
    }

    private void CentredText(string text, Vector2 centre, Color colour, int size, Font? font)
    {
        if (font is null)
        {
            return;
        }

        float width = font.GetStringSize(text, HorizontalAlignment.Left, -1f, size).X;
        DrawString(font, centre - new Vector2(width / 2f, 0f), text, HorizontalAlignment.Left, -1f, size, colour);
    }

    private enum EffectKind
    {
        Shot,
        Splash,
        Slow,
        Death,
        Overload,
    }

    /// <summary>A transient flash, positioned when it was pushed and expiring on the playback clock.</summary>
    private readonly record struct Effect(EffectKind Kind, Vector2 From, Vector2 To, double Expires);
}
