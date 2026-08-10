using System.Globalization;
using System.Text;
using Bastion.Core.Board;
using Bastion.Core.Cards;
using Bastion.Core.Resolve;
using Bastion.Core.Wave;
using Godot;

namespace Bastion.Game.Presentation;

/// <summary>
/// The after-wave review: which lanes leaked, by how much, and why.
/// </summary>
/// <remarks>
/// <para>
/// Reads the <see cref="FinalForecast"/> that already resolved - it computes nothing new, it explains
/// what happened. The cause of each leak (undefended lane, never in range, or simply out-damaged) is
/// reported per the resolver's own <see cref="LeakCause"/>, which is the post-wave explanation the core
/// loop calls for (docs/design/01-core-loop.md).
/// </para>
/// <para>
/// The panel is the account; the button that moves on lives in the action bar with every other primary
/// action, so the way forward is always in the same place.
/// </para>
/// </remarks>
public partial class PostWaveView : PanelContainer
{
    private WaveController _controller = null!;
    private Label _body = null!;

    public void Bind(WaveController controller)
    {
        _controller = controller;
        _controller.StateChanged += OnStateChanged;
    }

    public override void _Ready()
    {
        var column = new VBoxContainer();
        column.AddThemeConstantOverride("separation", 6);
        AddChild(column);

        column.AddChild(new Label { Text = "AFTER THE WAVE", ThemeTypeVariation = BastionTheme.PanelTitle });

        _body = new Label
        {
            ThemeTypeVariation = BastionTheme.Mono,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
        };
        column.AddChild(_body);

        Visible = false;
    }

    /// <summary>Called by the playback node once combat reaches its recorded end.</summary>
    public void Show(FinalForecast forecast)
    {
        var sb = new StringBuilder();

        foreach (LaneOutcome lane in forecast.Lanes)
        {
            sb.AppendLine($"Lane {lane.LaneIndex}  {lane.Stake}");
            sb.AppendLine($"   took {lane.PredictedDamage} of {lane.EmptyLaneDamage}");

            if (lane.LeakedUnits.Count == 0)
            {
                sb.AppendLine("   held — nothing leaked");
            }
            else
            {
                foreach (IGrouping<LeakCause, LeakedUnit> byCause in lane.LeakedUnits.GroupBy(u => u.Cause))
                {
                    sb.AppendLine($"   {byCause.Count()}× {Explain(byCause.Key)}");
                    sb.AppendLine($"      {byCause.Sum(u => u.LeakDamage)} damage");
                }
            }

            AppendTowerActivity(sb, lane);
            sb.AppendLine();
        }

        _body.Text = sb.ToString().TrimEnd();
        Visible = true;

        // The three cards together are taller than the column, so as the last of them the review
        // opened below the fold - the one thing the player is meant to read at this moment, off
        // screen at the moment it arrives. Scrolling to it was not enough on a short viewport, where
        // the panel above it is itself taller than the remaining space. It takes the top of the
        // column instead, which needs no room to work and hides nothing: the two consequence panels
        // move down, they do not go away.
        GetParent()?.MoveChild(this, 0);
        CallDeferred(nameof(ScrollIntoView));
    }

    private void ScrollIntoView()
    {
        for (Node? node = GetParent(); node is not null; node = node.GetParent())
        {
            if (node is ScrollContainer scroll)
            {
                // To the top, not merely into view: the review is the first card now, and
                // EnsureControlVisible is satisfied by showing its last line, which for a long
                // review means opening on the end of the account rather than the start of it.
                scroll.ScrollVertical = 0;
                return;
            }
        }
    }

    private void OnStateChanged()
    {
        // A fresh wave clears the review until the next combat finishes, and puts it back at the
        // bottom of the column so the next wave is read in the order it is played.
        if (_controller.Session.Phase is not (WavePhase.Locked or WavePhase.BustLocked))
        {
            Visible = false;

            if (GetParent() is { } parent)
            {
                parent.MoveChild(this, parent.GetChildCount() - 1);
            }
        }
    }

    /// <summary>
    /// What each tower in this lane actually did.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The resolver has computed this every wave since Milestone 1 and nothing has ever shown it. It
    /// is the honest way to teach the power curve: the card values are sublinear and plateau at the
    /// top, so a player reading rank glyphs mis-estimates the spread, and no pre-commitment display
    /// corrects that as well as their own board's results do. It is also the only place armor becomes
    /// visible - a Club into heavy plate loses damage that a leak explanation never mentions.
    /// </para>
    /// <para>
    /// <b>A record of what happened, not advice about next time.</b> No tower is ranked, scored, or
    /// marked as a mistake; a silent tower is reported as silent and why it was silent is the
    /// player's to work out (docs/design/09-information-and-ui.md).
    /// </para>
    /// <para>
    /// A junction tower appears in both lanes' lists, each at its reduced contribution. That is the
    /// intended reading of "buys breadth and forfeits synergy" - the breadth is two entries.
    /// </para>
    /// </remarks>
    private static void AppendTowerActivity(StringBuilder sb, LaneOutcome lane)
    {
        if (lane.TowerActivity.Count == 0)
        {
            sb.AppendLine("   no tower fires into this lane");
            return;
        }

        foreach (TowerActivity tower in lane.TowerActivity)
        {
            sb.AppendLine($"   {Describe(tower.Socket)}  {RankGlyph(tower.Card)} {tower.Family}");

            if (tower.Shots == 0)
            {
                sb.AppendLine($"      never fired — idle all {tower.IdleTicks} ticks");
                continue;
            }

            string kills = tower.Kills == 1 ? "1 kill" : $"{tower.Kills} kills";
            sb.AppendLine($"      {tower.Shots} shots · {tower.DamageApplied:0.0} dealt · {kills}");

            // Only when armor actually ate something, so the line means "this matchup cost you"
            // rather than being a zero the eye learns to skip.
            if (tower.DamageLostToArmor > 0.05)
            {
                sb.AppendLine($"      {tower.DamageLostToArmor:0.0} lost to armor");
            }
        }
    }

    private static string Describe(SocketRef socket) =>
        socket.IsJunction ? "junction" : $"L{socket.LaneIndex}·S{socket.SocketIndex}";

    private static string RankGlyph(Card card) => card.Rank switch
    {
        Rank.Ace => card.AceHigh ? "A" : "1",
        Rank.Jack => "J",
        Rank.Queen => "Q",
        Rank.King => "K",
        _ => ((int)card.Rank).ToString(CultureInfo.InvariantCulture),
    };

    private static string Explain(LeakCause cause) => cause switch
    {
        LeakCause.LaneUndefended => "reached the end — no tower fires into this lane",
        LeakCause.NeverInRange => "reached the end — never entered a tower's window",
        LeakCause.OutDamaged => "reached the end — shot at, but survived",
        _ => cause.ToString(),
    };
}
