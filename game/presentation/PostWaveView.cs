using System.Text;
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
                continue;
            }

            foreach (IGrouping<LeakCause, LeakedUnit> byCause in lane.LeakedUnits.GroupBy(u => u.Cause))
            {
                sb.AppendLine($"   {byCause.Count()}× {Explain(byCause.Key)}");
                sb.AppendLine($"      {byCause.Sum(u => u.LeakDamage)} damage");
            }
        }

        _body.Text = sb.ToString().TrimEnd();
        Visible = true;

        // The review is the last card in a column that overflows, so it opens below the fold. Scroll
        // it into view: it is the one thing the player is meant to read at this moment.
        CallDeferred(nameof(ScrollIntoView));
    }

    private void ScrollIntoView()
    {
        for (Node? node = GetParent(); node is not null; node = node.GetParent())
        {
            if (node is ScrollContainer scroll)
            {
                scroll.EnsureControlVisible(this);
                return;
            }
        }
    }

    private void OnStateChanged()
    {
        // A fresh wave clears the review until the next combat finishes.
        if (_controller.Session.Phase is not (WavePhase.Locked or WavePhase.BustLocked))
        {
            Visible = false;
        }
    }

    private static string Explain(LeakCause cause) => cause switch
    {
        LeakCause.LaneUndefended => "reached the end — no tower fires into this lane",
        LeakCause.NeverInRange => "reached the end — never entered a tower's window",
        LeakCause.OutDamaged => "reached the end — shot at, but survived",
        _ => cause.ToString(),
    };
}
