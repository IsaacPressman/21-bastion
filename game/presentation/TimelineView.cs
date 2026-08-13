using Bastion.Core.Board;
using Bastion.Core.Config;
using Bastion.Core.Resolve;
using Bastion.Core.Wave;
using Godot;

namespace Bastion.Game.Presentation;

/// <summary>
/// The encounter timeline: one time-and-path strip per lane, and the primary visual language.
/// </summary>
/// <remarks>
/// <para>
/// The board answers <i>where</i>; this answers <i>when</i>. Enemy groups appear as scheduled
/// markers, tower firing windows as bands beneath them, and a march step is drawn as the attacks it
/// moves rather than as a change in the entry number (docs/design/14-encounter-timeline.md).
/// </para>
/// <para>
/// The intended read is never "entry moves to 4.0." It is <b>"if you draw again, this cannon loses
/// two shots"</b> - so the ghost row under each tower band is the same tower one step later, and the
/// caption states the difference in attacks.
/// </para>
/// <para>
/// <b>It computes nothing.</b> Every mark is a recorded event, arranged by
/// <see cref="TimelineStrip"/>. During the draw the strip comes from the revealed force's schedule
/// and is labelled as such; after the Dealer resolves it comes from the combat contract. The two are
/// separate types and are labelled differently on purpose - a player who reads a revealed-force
/// figure as a promise will feel the game break it when reinforcements land
/// (docs/design/09-information-and-ui.md § Two forecasts must never share a surface).
/// </para>
/// </remarks>
public partial class TimelineView : Node2D
{
    /// <summary>Row labels live here, left of the time axis, so no mark is drawn over a name.</summary>
    private const float GutterWidth = 152f;

    private const float TitleHeight = 16f;
    private const float AxisHeight = 14f;
    private const float LaneTitleHeight = 12f;
    private const float RightMargin = 14f;
    private const float LanePadding = 3f;
    private const float MinRowHeight = 9f;
    private const float MaxRowHeight = 16f;

    private static float HeaderHeight => TitleHeight + AxisHeight;

    private WaveController _controller = null!;
    private double? _cursor;

    /// <summary>Proportional face for prose; the mono face for anything columnar.</summary>
    public Font LabelFont { get; set; } = null!;
    public Font MonoFont { get; set; } = null!;

    public void Bind(WaveController controller)
    {
        _controller = controller;
        _controller.StateChanged += OnStateChanged;
    }

    /// <summary>Moves the playback cursor, or clears it when combat is not playing.</summary>
    public void SetCursor(double? time)
    {
        _cursor = time;
        QueueRedraw();
    }

    public override void _Draw()
    {
        if (_controller?.Session is null)
        {
            return;
        }

        Rect2 area = Area;

        DrawRect(area, Palette.Surface);
        DrawLine(area.Position, area.Position + new Vector2(area.Size.X, 0f), Palette.SurfaceEdge, 1f);

        TimelineStrip strip = _controller.Strip;

        DrawHeader(strip);
        DrawLanes(strip);
        DrawCursor(strip);
    }

    // ---------------------------------------------------------------- geometry

    private Rect2 Area => Layout.TimelineArea(GetViewportRect().Size);

    /// <summary>
    /// The seconds the axis spans.
    /// </summary>
    /// <remarks>
    /// Taken from the longer of the two readings when a next-step ghost exists, so the ghost and the
    /// live strip share one scale. Two axes would make "this shot moved later" indistinguishable
    /// from "the wave got shorter", which is exactly the comparison the row exists to support.
    /// </remarks>
    private double Span(TimelineStrip strip)
    {
        double span = strip.DurationSeconds;

        if (_controller.NextStepThreat is { } next)
        {
            span = Math.Max(span, next.Schedule.DurationSeconds);
        }

        return Math.Max(span, 1.0);
    }

    private float TimeToX(double time, double span)
    {
        Rect2 area = Area;
        float usable = area.Size.X - GutterWidth - RightMargin;

        return area.Position.X + GutterWidth + ((float)(time / span) * usable);
    }

    // ---------------------------------------------------------------- header

    private void DrawHeader(TimelineStrip strip)
    {
        Rect2 area = Area;
        double span = Span(strip);
        bool contract = _controller.ShowsCombatContract;

        // The reading is named before anything is drawn from it. Same discipline as the battlefield
        // panel's header: the number changing when reinforcements land is fine, a number silently
        // changing meaning while keeping its name is not.
        Text(contract ? "TIMELINE — COMBAT CONTRACT" : "TIMELINE — REVEALED FORCE, SCHEDULE ONLY",
            new Vector2(area.Position.X + 8f, area.Position.Y + 12f),
            contract ? Palette.FinalForecast : Palette.VisibleThreat, 11, LabelFont);

        // The tower rows carry "22→18×" in the gutter, which needs saying once rather than per row.
        // Right-aligned so it cannot run into the title, whose width changes with the reading.
        if (_controller.NextStepThreat is not null)
        {
            RightAlignedText("tower rows: shots now → shots after one more card",
                new Vector2(area.Position.X + area.Size.X - RightMargin, area.Position.Y + 12f),
                Palette.TextFaint, 9);
        }

        float axisY = area.Position.Y + HeaderHeight - 3f;
        DrawLine(new Vector2(TimeToX(0.0, span), axisY), new Vector2(TimeToX(span, span), axisY),
            Palette.SurfaceEdge, 1f);

        // A tick every whole second the strip can fit, so a marker's position is readable as a time
        // rather than only as a place.
        double step = span <= 8.0 ? 1.0 : span <= 20.0 ? 2.0 : 5.0;

        for (double t = 0.0; t <= span + 1e-9; t += step)
        {
            float x = TimeToX(t, span);
            DrawLine(new Vector2(x, axisY - 3f), new Vector2(x, axisY), Palette.TextFaint, 1f);
            Text($"{t:0}s", new Vector2(x + 2f, axisY - 5f), Palette.TextFaint, 9, MonoFont);
        }
    }

    // ---------------------------------------------------------------- lanes

    private void DrawLanes(TimelineStrip strip)
    {
        Rect2 area = Area;
        WaveSession session = _controller.Session;
        double span = Span(strip);

        float top = area.Position.Y + HeaderHeight + 2f;
        float available = area.Size.Y - HeaderHeight - 6f;
        float perLane = available / Math.Max(1, strip.Lanes.Count);

        foreach (LaneStrip lane in strip.Lanes)
        {
            float laneTop = top + (lane.LaneIndex * perLane);

            if (lane.LaneIndex > 0)
            {
                DrawLine(new Vector2(area.Position.X + 6f, laneTop - 1f),
                    new Vector2(area.Position.X + area.Size.X - 8f, laneTop - 1f), Palette.SurfaceEdge, 1f);
            }

            // The lane's title owns a row of its own. Sharing one with the first group put a stake
            // and an enemy count on top of each other in the gutter, which is the sort of thing only
            // a screenshot at a real viewport size shows.
            DrawLaneLabel(lane, session, new Vector2(area.Position.X + 8f, laneTop + 10f));
            DrawLaneRows(lane, span, laneTop + LaneTitleHeight, perLane - LaneTitleHeight);
        }
    }

    /// <summary>The lane's name, stake, and - when it is the hidden card's - the located unknown.</summary>
    /// <remarks>
    /// The hidden card's destination lane is baseline information and its rank is not
    /// (docs/design/06-dealer-and-enemies.md). Marked on the lane it will arrive in, because
    /// uncertainty the player can locate is uncertainty they can hedge - which is the whole
    /// difference between "something is coming" and a plan.
    /// </remarks>
    private void DrawLaneLabel(LaneStrip lane, WaveSession session, Vector2 at)
    {
        string stake = lane.LaneIndex < session.Encounter.LaneStakes.Count
            ? session.Encounter.LaneStakes[lane.LaneIndex]
            : "?";

        Text($"LANE {lane.LaneIndex}  {stake}", at, Palette.TextDim, 10, LabelFont);

        // Beside the title, not beneath it: the row below belongs to the first enemy group.
        if (!_controller.ShowsCombatContract && session.HiddenCardLane == lane.LaneIndex)
        {
            Text("← hidden card arrives here", at + new Vector2(GutterWidth - 60f, 0f),
                Palette.DealerEnemy, 9, LabelFont);
        }
    }

    private void DrawLaneRows(LaneStrip lane, double span, float laneTop, float laneHeight)
    {
        List<Action<float, float>> rows = [];

        foreach (IGrouping<(string EnemyId, SpawnSource Source), UnitTrack> group in GroupUnits(lane))
        {
            (string enemyId, SpawnSource source) = group.Key;
            UnitTrack[] units = [.. group];

            rows.Add((y, height) => DrawGroupRow(enemyId, source, units, span, y, height));
        }

        foreach (TowerBand band in lane.Towers)
        {
            TowerBand captured = band;
            rows.Add((y, height) => DrawTowerRow(captured, lane.LaneIndex, span, y, height));
        }

        if (rows.Count == 0)
        {
            Text("nothing scheduled", new Vector2(TimeToX(0.0, span), laneTop + 14f), Palette.TextFaint, 9, LabelFont);
            return;
        }

        float usable = laneHeight - (LanePadding * 2f);
        float rowHeight = Mathf.Clamp(usable / rows.Count, MinRowHeight, MaxRowHeight);
        float y = laneTop + LanePadding;

        foreach (Action<float, float> row in rows)
        {
            row(y, rowHeight);
            y += rowHeight;
        }
    }

    /// <summary>
    /// Units of one type and origin, in spawn order.
    /// </summary>
    /// <remarks>
    /// Grouped rather than given a row each, because the design describes enemy <i>groups</i> as
    /// scheduled markers - and because a lane carrying a dozen units would otherwise need a dozen
    /// rows that say the same thing. Origin is part of the key so a drawn reinforcement never hides
    /// inside the base wave's row.
    /// </remarks>
    private static IEnumerable<IGrouping<(string, SpawnSource), UnitTrack>> GroupUnits(LaneStrip lane) =>
        lane.Units
            .OrderBy(u => u.SpawnIndex)
            .GroupBy(u => (u.EnemyId, u.Source));

    private void DrawGroupRow(
        string enemyId, SpawnSource source, UnitTrack[] units, double span, float y, float height)
    {
        TuningData tuning = _controller.Tuning;
        EnemyTuning type = tuning.Enemy(enemyId);
        Color colour = SourceColour(source);
        float mid = y + (height / 2f);

        Text($"{units.Length}× {type.DisplayName}", new Vector2(Area.Position.X + 16f, mid + 3f),
            colour, 9, LabelFont);

        foreach (UnitTrack unit in units)
        {
            float from = TimeToX(unit.SpawnTime, span);
            float to = TimeToX(unit.ExitTime, span);
            float thickness = Math.Max(2f, height * 0.28f);

            // The unit's life as a bar: it starts where it spawns and stops where the recording says
            // it stopped. A leaker is drawn to the wall in full colour; a kill fades out, so a lane
            // that is being handled reads as bars that end early.
            DrawRect(new Rect2(from, mid - (thickness / 2f), Math.Max(2f, to - from), thickness),
                unit.Exit == UnitExit.Leaked ? colour : colour with { A = 0.45f });

            foreach ((double start, double until) in unit.Slows)
            {
                // Slow is a spatial event and this is where it becomes visible as one: the bar keeps
                // running while the column behind it closes up.
                DrawRect(
                    new Rect2(TimeToX(start, span), mid - (thickness / 2f) - 2f,
                        Math.Max(1f, TimeToX(until, span) - TimeToX(start, span)), thickness + 4f),
                    Palette.SlowTint with { A = 0.28f });
            }

            DrawExitGlyph(unit, new Vector2(to, mid));
        }
    }

    /// <summary>Killed, leaked, or still standing - the three ways a bar can end.</summary>
    private void DrawExitGlyph(UnitTrack unit, Vector2 at)
    {
        switch (unit.Exit)
        {
            case UnitExit.Died:
                DrawLine(at + new Vector2(-3f, -3f), at + new Vector2(3f, 3f), Palette.Death, 1.5f);
                DrawLine(at + new Vector2(-3f, 3f), at + new Vector2(3f, -3f), Palette.Death, 1.5f);
                break;

            case UnitExit.Leaked:
                // A filled wedge at the wall. A leak is the one outcome the player is being asked to
                // prevent, so it is the one that is drawn solid.
                DrawColoredPolygon(
                    [at + new Vector2(-4f, -4f), at + new Vector2(4f, 0f), at + new Vector2(-4f, 4f)],
                    Palette.Bust);
                break;

            default:
                DrawCircle(at, 2f, Palette.TextFaint);
                break;
        }
    }

    /// <summary>
    /// One tower's firing band, with the same tower one march step later drawn beneath it.
    /// </summary>
    /// <remarks>
    /// <b>The ghost is a change, not a loss.</b> A march step always takes engagement window away,
    /// but per-tower attacks are not monotonic: a forward tower that kills less leaves the rear one
    /// more to shoot at, so a step can move attacks backwards down the lane rather than only
    /// deleting them (measured - <c>tests/Wave/NextStepThreatTests.cs</c>). The caption says which
    /// happened rather than assuming, because "loses 2" printed over a tower that gained 2 would be
    /// a plain falsehood on the surface the design leans on hardest.
    /// </remarks>
    private void DrawTowerRow(TowerBand band, int laneIndex, double span, float y, float height)
    {
        TowerState? tower = _controller.Board.Towers.FirstOrDefault(t => t.Socket == band.Socket);
        Color colour = tower?.Family == Family.Spade ? Palette.Spade : Palette.Club;
        float mid = y + (height / 2f);

        string name = tower is null
            ? Describe(band.Socket)
            : $"{Describe(band.Socket)} {RankGlyph(tower)}";

        Text($"{name}  {StepLabel(band, laneIndex)}{OrderTag(tower)}",
            new Vector2(Area.Position.X + 16f, mid + 3f), colour, 9, LabelFont);

        float from = TimeToX(band.FirstFire, span);
        float to = TimeToX(band.LastFire, span);

        DrawRect(new Rect2(from, mid - 3f, Math.Max(2f, to - from), 6f), colour with { A = 0.22f });

        foreach (double shot in band.ShotTimes)
        {
            float x = TimeToX(shot, span);
            DrawLine(new Vector2(x, mid - 3f), new Vector2(x, mid + 3f), colour, 1f);
        }

        DrawHoldMarker(tower, band, span, mid, colour);
        DrawStepGhost(band, laneIndex, span, mid);
    }

    /// <summary>
    /// The standing order in force, named on the row it changes.
    /// </summary>
    /// <remarks>
    /// <b>An order must alter this display rather than open a separate abstract menu</b>
    /// (docs/design/05-battlefield.md § They are encounter skill). The band above already <i>is</i>
    /// the consequence - setting Hold moves where it starts, Focus and Trigger on Group change which
    /// shots land and when - so what the tag adds is the cause: the row moved because you told it to,
    /// not because the wave behaved differently. Abbreviated because the gutter is fixed width and
    /// the shot counts lead.
    /// </remarks>
    private static string OrderTag(TowerState? tower)
    {
        if (tower is null || tower.Order.IsDefault)
        {
            return string.Empty;
        }

        if (tower.Order.HoldPastPosition is not null)
        {
            return "  hold";
        }

        if (tower.Order.TriggerOnGroup)
        {
            return "  group";
        }

        return tower.Order.Focus switch
        {
            Bastion.Core.Board.FocusMode.PreferArmored => "  armored",
            Bastion.Core.Board.FocusMode.PreferLeading => "  leading",
            _ => string.Empty,
        };
    }

    /// <summary>
    /// Where a Hold order starts this tower's band.
    /// </summary>
    /// <remarks>
    /// docs/design/05-battlefield.md § They are encounter skill: an order must alter this display
    /// rather than open a separate abstract menu, and <b>Hold shifts a firing window</b>. The band
    /// already begins at the first recorded shot, so the order's effect is visible as the band
    /// itself moving; this marks the commitment that caused it, so the shift reads as a choice
    /// rather than as the wave behaving differently.
    /// </remarks>
    private void DrawHoldMarker(TowerState? tower, TowerBand band, double span, float mid, Color colour)
    {
        if (tower?.Order.HoldPastPosition is null || band.ShotTimes.Count == 0)
        {
            return;
        }

        float x = TimeToX(band.FirstFire, span);

        DrawLine(new Vector2(x, mid - 6f), new Vector2(x, mid + 6f), Palette.SocketPicked, 1.5f);
        Text("hold", new Vector2(x - 22f, mid - 6f), Palette.SocketPicked, 8, LabelFont);
    }

    /// <summary>
    /// This tower's attack count now and one march step later, for the gutter label.
    /// </summary>
    /// <remarks>
    /// It goes in the gutter beside the tower's name rather than at the end of its band. The band
    /// runs to the right edge of the strip, so a caption after it had nowhere to go and clipped -
    /// which is the kind of fault only a screenshot at a real viewport size reveals, and the reason
    /// the capture harness exists.
    /// </remarks>
    private string StepLabel(TowerBand band, int laneIndex)
    {
        if (_controller.GhostStrip is not { } stepped)
        {
            return $"{band.Shots}×";
        }

        int after = stepped.Lanes[laneIndex].Towers
            .FirstOrDefault(b => b.Socket == band.Socket)?.Shots ?? 0;

        // Stated as a movement, never as a verdict: the player is owed what changes, and whether
        // that is worth a card is theirs to decide.
        return after == band.Shots ? $"{band.Shots}× (unchanged)" : $"{band.Shots}→{after}×";
    }

    /// <summary>The same tower one march step later, drawn as a second row of ticks under its band.</summary>
    private void DrawStepGhost(TowerBand band, int laneIndex, double span, float mid)
    {
        if (_controller.GhostStrip is not { } stepped)
        {
            return;
        }

        if (stepped.Lanes[laneIndex].Towers.FirstOrDefault(b => b.Socket == band.Socket) is not { } ghost)
        {
            return;
        }

        foreach (double shot in ghost.ShotTimes)
        {
            float x = TimeToX(shot, span);
            DrawLine(new Vector2(x, mid + 4f), new Vector2(x, mid + 7f), Palette.TextFaint, 1f);
        }
    }

    private void DrawCursor(TimelineStrip strip)
    {
        if (_cursor is not { } time)
        {
            return;
        }

        Rect2 area = Area;
        float x = TimeToX(time, Span(strip));

        DrawLine(new Vector2(x, area.Position.Y + HeaderHeight - 6f),
            new Vector2(x, area.Position.Y + area.Size.Y - 2f), Palette.Entry, 1.5f);
    }

    // ---------------------------------------------------------------- helpers

    private static Color SourceColour(SpawnSource source) => source switch
    {
        SpawnSource.Vanguard => Palette.Vanguard,
        SpawnSource.DealerDraw => Palette.DealerEnemy,
        _ => Palette.BaseWaveEnemy,
    };

    private static string Describe(SocketRef socket) =>
        socket.IsJunction ? "junction" : $"L{socket.LaneIndex}·S{socket.SocketIndex}";

    private static string RankGlyph(TowerState tower) => tower.Card.Rank switch
    {
        Bastion.Core.Cards.Rank.Ace => tower.Card.AceHigh ? "A" : "1",
        Bastion.Core.Cards.Rank.Jack => "J",
        Bastion.Core.Cards.Rank.Queen => "Q",
        Bastion.Core.Cards.Rank.King => "K",
        _ => ((int)tower.Card.Rank).ToString(),
    };

    private void OnStateChanged()
    {
        _cursor = null;
        QueueRedraw();
    }

    private void Text(string text, Vector2 position, Color colour, int size, Font? font)
    {
        if (font is not null)
        {
            DrawString(font, position, text, HorizontalAlignment.Left, -1f, size, colour);
        }
    }

    /// <summary>Draws text ending at <paramref name="rightEdge"/>, for anything that must not run into a neighbour.</summary>
    private void RightAlignedText(string text, Vector2 rightEdge, Color colour, int size)
    {
        if (LabelFont is null)
        {
            return;
        }

        float width = LabelFont.GetStringSize(text, HorizontalAlignment.Left, -1f, size).X;
        Text(text, rightEdge - new Vector2(width, 0f), colour, size, LabelFont);
    }
}
