using System.Globalization;
using System.Text;
using Bastion.Core.Board;
using Bastion.Core.Cards;
using Bastion.Core.Resolve;
using Godot;

namespace Bastion.Game.Presentation;

/// <summary>
/// What the last committed card changed, kept on screen until the next one.
/// </summary>
/// <remarks>
/// <para>
/// <b>Counterfactual memory</b> (docs/design/14-encounter-timeline.md). It is step four of the
/// tactical loop - <i>observe delta</i> - made visible, and it is what lets a player answer "what
/// did that card buy you?" rather than only "what does the board look like now".
/// </para>
/// <para>
/// <b>Players learn causality from deltas, not from absolute levels.</b> So every line here is a
/// before and an after: a lane's leak going 2 → 0, a named unit going from surviving to killed, a
/// tower's attacks moving. Nothing is scored and nothing is judged - the panel says what happened,
/// and whether it was the right card is the player's to conclude.
/// </para>
/// <para>
/// It disappears when there is nothing to remember, rather than showing an empty frame. A wave's
/// first card has no predecessor and a settled wave's memory belongs to the wave that made it.
/// </para>
/// </remarks>
public partial class CounterfactualPanel : PanelContainer
{
    private WaveController _controller = null!;
    private Label _headline = null!;
    private Label _body = null!;

    public void Bind(WaveController controller)
    {
        _controller = controller;
        _controller.StateChanged += Refresh;
    }

    public override void _Ready()
    {
        var column = new VBoxContainer();
        column.AddThemeConstantOverride("separation", 4);
        AddChild(column);

        column.AddChild(new Label { Text = "LAST PLACEMENT", ThemeTypeVariation = BastionTheme.PanelTitle });

        _headline = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart };
        _headline.AddThemeFontSizeOverride("font_size", 13);
        column.AddChild(_headline);

        _body = new Label { ThemeTypeVariation = BastionTheme.Mono };
        column.AddChild(_body);
    }

    private void Refresh()
    {
        if (_controller.LastCommit is not { } commit)
        {
            Visible = false;
            return;
        }

        Visible = true;

        _headline.Text = $"{RankLabel(commit.Rank)} {commit.Family} → {Describe(commit.Socket)}"
            + (commit.Displaces is { } displaced
                ? $", replacing {RankLabel(displaced.Card.Rank)} ({displaced.ShotDamage:0.0}/shot)"
                : string.Empty);

        _headline.AddThemeColorOverride(
            "font_color", commit.Family == Family.Club ? Palette.Club : Palette.Spade);

        var sb = new StringBuilder();

        AppendLaneChanges(sb, commit);
        AppendFateChanges(sb, commit);
        AppendRunChange(sb, commit);
        AppendStepChanges(sb, commit);

        _body.Text = sb.Length == 0
            ? "Nothing the revealed force reaches. Its value is against what the Dealer has not shown yet."
            : sb.ToString().TrimEnd();
    }

    /// <summary>
    /// Per lane, and only where something moved.
    /// </summary>
    /// <remarks>
    /// Never summed across lanes: two lanes carry different stakes, and a single total would be the
    /// sortable score the design forbids (docs/design/14-encounter-timeline.md § Candidate placements
    /// show causal deltas). A lane that did not change simply says nothing.
    /// </remarks>
    private static void AppendLaneChanges(StringBuilder sb, CandidateDelta commit)
    {
        foreach (LaneDelta lane in commit.Lanes.Where(l => !l.IsUnchanged))
        {
            sb.AppendLine(CultureInfo.InvariantCulture,
                $"Lane {lane.LaneIndex} {lane.Stake,-8} takes {lane.PredictedDamageBefore} → {lane.PredictedDamageAfter}");

            if (lane.LeakCountBefore != lane.LeakCountAfter)
            {
                sb.AppendLine(CultureInfo.InvariantCulture,
                    $"   leaks {lane.LeakCountBefore} → {lane.LeakCountAfter}");
            }

            if (!Nullable.Equals(lane.FirstLeakTimeBefore, lane.FirstLeakTimeAfter))
            {
                sb.AppendLine(CultureInfo.InvariantCulture,
                    $"   first leak {Time(lane.FirstLeakTimeBefore)} → {Time(lane.FirstLeakTimeAfter)}");
            }
        }
    }

    /// <summary>The named units whose fate turned. The line a player repeats back afterwards.</summary>
    private static void AppendFateChanges(StringBuilder sb, CandidateDelta commit)
    {
        foreach (UnitFateChange change in commit.FateChanges)
        {
            string fate = change.LeaksBefore ? "got through → stopped" : "stopped → got through";

            sb.AppendLine(CultureInfo.InvariantCulture, $"{change.DisplayName} (lane {change.LaneIndex}): {fate}");
        }
    }

    private static void AppendRunChange(StringBuilder sb, CandidateDelta commit)
    {
        if (commit.Runs.IsUnchanged)
        {
            return;
        }

        sb.AppendLine(CultureInfo.InvariantCulture,
            $"Runs {commit.Runs.LinkedTowersBefore} → {commit.Runs.LinkedTowersAfter} linked, "
            + $"best +{commit.Runs.BestBonusBefore:P0} → +{commit.Runs.BestBonusAfter:P0}");
    }

    /// <summary>
    /// What another card would now cost the towers this one joined.
    /// </summary>
    /// <remarks>
    /// The step is priced against the board the card created, not the one before it - which is the
    /// only version of the number that answers what the player is about to be asked. Reported as a
    /// movement rather than a loss, because a step can push attacks backwards down a lane instead of
    /// only deleting them (measured, <c>tests/Wave/NextStepThreatTests.cs</c>).
    /// </remarks>
    private static void AppendStepChanges(StringBuilder sb, CandidateDelta commit)
    {
        TowerShotDelta[] moved =
        [
            .. commit.TowerShots.Where(t => t.AfterNextStep is { } next && next != t.After),
        ];

        if (moved.Length == 0)
        {
            return;
        }

        sb.AppendLine("Next march step:");

        foreach (TowerShotDelta tower in moved)
        {
            sb.AppendLine(CultureInfo.InvariantCulture,
                $"   {Describe(tower.Socket)} lane {tower.LaneIndex}  {tower.After} → {tower.AfterNextStep} attacks");
        }
    }

    private static string Time(double? seconds) =>
        seconds is { } value ? $"{value:0.0}s" : "never";

    private static string Describe(SocketRef socket) =>
        socket.IsJunction ? "junction" : $"L{socket.LaneIndex}·S{socket.SocketIndex}";

    private static string RankLabel(Rank rank) => rank switch
    {
        Rank.Ace => "A",
        Rank.Jack => "J",
        Rank.Queen => "Q",
        Rank.King => "K",
        _ => ((int)rank).ToString(CultureInfo.InvariantCulture),
    };
}
