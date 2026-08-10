using System.Text;
using Bastion.Core.Board;
using Bastion.Core.Cards;
using Bastion.Core.Config;
using Bastion.Core.Hand;
using Bastion.Core.Wave;
using Godot;

namespace Bastion.Game.Presentation;

/// <summary>
/// The hand-consequences panel: the blackjack side of the decision.
/// </summary>
/// <remarks>
/// <para>
/// The second of the two separate surfaces. It shows the hand at full fidelity - total, hard/soft,
/// Formation Strength, summed power, runs, and the remaining pile with the busting ranks marked - and
/// stops there. <b>It never states a bust percentage, an edge, or a recommended action.</b> Marking
/// the busting ranks makes risk a reading skill instead of a lookup: the player sees how many safe
/// cards are left and feels it, which a percentage would flatten into a number
/// (docs/design/09-information-and-ui.md).
/// </para>
/// <para>
/// The pile is drawn in a monospaced face because it is a table. Padding a proportional font aligns
/// nothing, and a rank column that does not line up is exactly the kind of display that pushes a
/// player back toward wanting the percentage.
/// </para>
/// <para>
/// Nothing here reaches across to the battlefield panel. Weighing "this card busts me" against "that
/// lane is Open" is the player's job, by design.
/// </para>
/// </remarks>
public partial class HandPanel : PanelContainer
{
    private WaveController _controller = null!;
    private Label _total = null!;
    private Label _body = null!;
    private Label _notes = null!;
    private Label _composition = null!;

    public void Bind(WaveController controller)
    {
        _controller = controller;
        _controller.StateChanged += Refresh;
    }

    public override void _Ready()
    {
        var column = new VBoxContainer();
        column.AddThemeConstantOverride("separation", 6);
        AddChild(column);

        column.AddChild(new Label { Text = "HAND", ThemeTypeVariation = BastionTheme.PanelTitle });

        _total = new Label();
        _total.AddThemeFontOverride("font", BastionTheme.MonoFont);
        _total.AddThemeFontSizeOverride("font_size", 30);
        column.AddChild(_total);

        _body = new Label { ThemeTypeVariation = BastionTheme.Mono };
        column.AddChild(_body);

        _notes = new Label
        {
            ThemeTypeVariation = BastionTheme.Hint,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
        };
        column.AddChild(_notes);

        column.AddChild(new HSeparator());
        column.AddChild(new Label
        {
            Text = "REMAINING PILE   ★ BUSTS THIS HAND",
            ThemeTypeVariation = BastionTheme.PanelTitle,
        });

        _composition = new Label { ThemeTypeVariation = BastionTheme.Mono };
        column.AddChild(_composition);
    }

    private void Refresh()
    {
        WaveSession session = _controller.Session;
        HandState hand = session.Hand;
        IReadOnlyList<TowerState> towers = _controller.Board.Towers;

        _total.Text = hand.CardCount == 0
            ? "—"
            : hand.IsBust ? $"{hand.Total}  BUST" : $"{hand.Total}  {(hand.IsSoft ? "soft" : "hard")}";
        _total.AddThemeColorOverride("font_color", hand.IsBust ? Palette.Bust : Palette.Text);

        var sb = new StringBuilder();
        sb.AppendLine($"Formation Strength  ×{session.FormationMultiplier:0.00}");
        sb.AppendLine($"Base power          {towers.Sum(t => t.BasePower):0.0}");

        // Firing power is stated outright rather than left as base x multiplier for the player to
        // work out, because on a mixed board that multiplication is simply wrong: a carried-over
        // tower reverted to x1.00 at the wave boundary and does not take this hand's Formation
        // Strength. Summing each tower's own resolved damage is the only figure that is true.
        sb.AppendLine($"Firing power        {towers.Sum(t => t.ShotDamage):0.0}");

        int linked = towers.Count(t => t.RunBonus > 0.0);
        sb.Append(linked == 0
            ? "Active runs         none"
            : $"Active runs         {linked} linked, best +{towers.Max(t => t.RunBonus):P0}");

        _body.Text = sb.ToString();

        var notes = new StringBuilder();
        AppendAcePreview(notes, hand);
        AppendJunctionNote(notes, towers);
        AppendOverloadNote(notes, session);
        _notes.Text = notes.ToString().TrimEnd();
        _notes.Visible = _notes.Text.Length > 0;

        _composition.Text = CompositionLines(session);
    }

    private void AppendAcePreview(StringBuilder sb, HandState hand)
    {
        if (!hand.Cards.Contains(Rank.Ace))
        {
            return;
        }

        // § Shown asks for "Ace transformations and their power consequence, before commitment", and
        // the consequence is the number: 5.4 to 1.0 is an 81% cut to that tower, which "a high-power
        // tower" does not convey. The one place a blackjack event has an instant physical result
        // (docs/design/04-cards-as-defenses.md § Aces).
        CardPowerTuning power = _controller.Tuning.CardPower;

        sb.AppendLine(hand.IsSoft
            ? $"Ace counts as 11 — {power.ForValue(11):0.0} base power. A hit that forces it to 1 "
              + $"drops that tower to {power.ForValue(1):0.0}."
            : $"Ace counts as 1 — {power.ForValue(1):0.0} base power. A further hit no longer flips it.");
    }

    /// <summary>
    /// Says so when firing power overstates what actually lands, rather than quietly overstating it.
    /// </summary>
    /// <remarks>
    /// A junction tower fires into both lanes at a reduced contribution each unless it is a face card,
    /// so its full shot damage is never delivered to one lane. The figure above is per shot before
    /// that reduction, which is the honest thing to sum but not the whole story.
    /// </remarks>
    private void AppendJunctionNote(StringBuilder sb, IReadOnlyList<TowerState> towers)
    {
        if (towers.FirstOrDefault(t => t.Socket.IsJunction) is not { } junction)
        {
            return;
        }

        sb.AppendLine(junction.ExemptFromJunctionPenalty
            ? "The junction tower is a face card — it fires into both lanes at full contribution."
            : $"Firing power is before the junction tower's split: it contributes "
              + $"{_controller.Tuning.Towers.JunctionContributionFraction:P0} into each lane.");
    }

    private void AppendOverloadNote(StringBuilder sb, WaveSession session)
    {
        if (session.Phase != WavePhase.DrawDecision)
        {
            return;
        }

        // The pre-hit reading: were the next card to bust, this is the lane Overload would strike. Shown
        // here, on the hand side, because it is a consequence of choosing to hit (docs/design/07).
        int lane = _controller.VisibleThreat
            .HighestThreatLane(_controller.Tuning.Rules.OverloadTieBreakStake).LaneIndex;

        sb.AppendLine($"Were this hand to bust, Overload would strike Lane {lane}.");
    }

    /// <summary>
    /// The pile as a fixed-width grid: rank, count, and a star on any rank that would bust this hand.
    /// </summary>
    private string CompositionLines(WaveSession session)
    {
        IReadOnlyDictionary<Rank, int> composition = session.Shoe.RemainingComposition();
        HandState hand = session.Hand;

        var sb = new StringBuilder();
        int column = 0;

        foreach (Rank rank in Enum.GetValues<Rank>())
        {
            bool busts = hand.CardCount > 0 && hand.Hit(rank).IsBust;
            sb.Append($"{RankLabel(rank),2} {composition[rank]}{(busts ? "★" : " ")}  ");

            if (++column % 5 == 0)
            {
                sb.AppendLine();
            }
        }

        return sb.ToString().TrimEnd();
    }

    private static string RankLabel(Rank rank) => rank switch
    {
        Rank.Ace => "A",
        Rank.Jack => "J",
        Rank.Queen => "Q",
        Rank.King => "K",
        _ => ((int)rank).ToString(),
    };
}
