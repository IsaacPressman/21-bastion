using Bastion.Core.Board;
using Bastion.Core.Cards;
using Bastion.Core.Config;
using Bastion.Core.Wave;
using Bastion.Game.Presentation;
using Godot;

// Control also defines a FocusMode; alias the core one so the tower's target preference is unambiguous.
using CoreFocusMode = Bastion.Core.Board.FocusMode;

namespace Bastion.Game.Input;

/// <summary>
/// The action bar's decision half: the family toggle, the hit/stand choice, and the adjustment
/// window's fallback controls.
/// </summary>
/// <remarks>
/// <para>
/// Split in two. Contextual controls - one per socket, per legal relocation, per standing order -
/// wrap and scroll on the left; the <b>primary action wraps and scrolls nowhere</b>, sitting in a
/// fixed slot on the right. That is deliberate: the old single-column layout put "Lock and resolve"
/// last in a list that outgrew its viewport, so ending a turn meant scrolling past a dozen buttons
/// that had grown with the board.
/// </para>
/// <para>
/// The board is now the primary way to place and adjust (see <see cref="BoardInteraction"/>); the
/// socket and relocation buttons here remain as a keyboard-reachable, unambiguous fallback and as a
/// readable list of exactly what is legal.
/// </para>
/// <para>
/// It enforces nothing the core does not. Family locking, the single adjustment move, and the phase
/// boundaries all live in <see cref="WaveSession"/>. This only declines to <i>offer</i> a move the
/// session would reject, which is a usability choice, not a second copy of the rules.
/// </para>
/// </remarks>
public partial class PhaseControls : HBoxContainer
{
    private WaveController _controller = null!;
    private BoardInteraction _interaction = null!;

    private VBoxContainer _contextual = null!;
    private VBoxContainer _primary = null!;

    public void Bind(WaveController controller, BoardInteraction interaction)
    {
        _controller = controller;
        _interaction = interaction;

        _controller.StateChanged += Rebuild;
        _interaction.Changed += Rebuild;
    }

    public override void _Ready()
    {
        AddThemeConstantOverride("separation", 16);

        var scroll = new ScrollContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
        };
        AddChild(scroll);

        _contextual = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        _contextual.AddThemeConstantOverride("separation", 6);
        scroll.AddChild(_contextual);

        _primary = new VBoxContainer
        {
            CustomMinimumSize = new Vector2(230f, 0f),
            Alignment = BoxContainer.AlignmentMode.Center,
        };
        _primary.AddThemeConstantOverride("separation", 8);
        AddChild(_primary);
    }

    private void Rebuild()
    {
        Clear(_contextual);
        Clear(_primary);

        WaveSession session = _controller.Session;
        Visible = session.Phase
            is WavePhase.AwaitingPlacement or WavePhase.DrawDecision or WavePhase.AdjustmentWindow;

        switch (session.Phase)
        {
            case WavePhase.AwaitingPlacement:
                BuildPlacement();
                break;
            case WavePhase.DrawDecision:
                BuildDrawDecision();
                break;
            case WavePhase.AdjustmentWindow:
                BuildAdjustment(session);
                break;
        }
    }

    // ---------------------------------------------------------------- phases

    private void BuildPlacement()
    {
        Rank? pending = _controller.PendingRank;

        HFlowContainer families = Group("FAMILY  (locked at placement)");
        families.AddChild(FamilyButton("Club — artillery, splash", Family.Club));
        families.AddChild(FamilyButton("Spade — traps, slow", Family.Spade));

        HFlowContainer sockets = Group("SOCKET  (or click the board)");
        BoardState board = _controller.Board;

        foreach (SocketRef socket in AllSockets(_controller.Tuning))
        {
            TowerState? held = board.Towers.FirstOrDefault(t => t.Socket == socket);
            var button = new Button
            {
                Text = held is null
                    ? Describe(socket)
                    : $"{Describe(socket)} — replaces {RankLabel(held.Card.Rank)}",
            };

            SocketRef captured = socket;
            button.Pressed += () => _interaction.Click(captured);
            sockets.AddChild(button);
        }

        _primary.AddChild(CardReadout(pending));
    }

    private void BuildDrawDecision()
    {
        _contextual.AddChild(Caption("THE COST OF ONE MORE CARD"));
        _contextual.AddChild(Paragraph(
            $"Hitting advances the entry by {_controller.Session.NextStepCost():0.0} before the card is revealed. "
            + "The board shades the firing windows that step would eat."));

        var hit = new Button { Text = "Hit", ThemeTypeVariation = BastionTheme.PrimaryButton };
        hit.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        hit.Pressed += () => _controller.Hit();
        _primary.AddChild(hit);

        var stand = new Button { Text = "Stand", ThemeTypeVariation = BastionTheme.PrimaryButton };
        stand.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        stand.Pressed += () => _controller.Stand();
        _primary.AddChild(stand);
    }

    private void BuildAdjustment(WaveSession session)
    {
        BoardState board = _controller.Board;
        TuningData tuning = _controller.Tuning;
        var occupied = board.Towers.Select(t => t.Socket).ToHashSet();

        if (!session.MoveSpent)
        {
            HFlowContainer moves = Group(_interaction.PickedUp is { } picked
                ? $"MOVING {Describe(picked)} — choose a destination, or right-click to cancel"
                : "ONE MOVE  (click a tower on the board, or use a button)");

            foreach (SocketRef from in occupied)
            {
                foreach (SocketRef to in AllSockets(tuning))
                {
                    if (!occupied.Contains(to) && AreAdjacent(from, to, tuning))
                    {
                        var button = new Button { Text = $"{Describe(from)} → {Describe(to)}" };
                        (SocketRef f, SocketRef t) = (from, to);
                        button.Pressed += () => _controller.Relocate(f, t);
                        moves.AddChild(button);
                    }
                }
            }

            var sockets = occupied.ToList();
            for (int i = 0; i < sockets.Count; i++)
            {
                for (int j = i + 1; j < sockets.Count; j++)
                {
                    if (AreAdjacent(sockets[i], sockets[j], tuning))
                    {
                        var button = new Button { Text = $"{Describe(sockets[i])} ↔ {Describe(sockets[j])}" };
                        (SocketRef a, SocketRef b) = (sockets[i], sockets[j]);
                        button.Pressed += () => _controller.Swap(a, b);
                        moves.AddChild(button);
                    }
                }
            }
        }
        else
        {
            _contextual.AddChild(Caption("MOVE SPENT — standing orders are still free"));
        }

        HFlowContainer orders = Group("STANDING ORDERS  (free)");

        foreach (TowerState tower in board.Towers)
        {
            var button = new Button { Text = $"{Describe(tower.Socket)}: {OrderLabel(tower.Order)}" };
            TowerState captured = tower;
            button.Pressed += () =>
                _controller.SetOrder(captured.Socket, NextOrder(captured.Order, captured, tuning));
            orders.AddChild(button);
        }

        var lockButton = new Button { Text = "Lock and resolve", ThemeTypeVariation = BastionTheme.PrimaryButton };
        lockButton.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        lockButton.Pressed += () => _controller.Lock();
        _primary.AddChild(lockButton);
    }

    // ---------------------------------------------------------------- widgets

    private Control CardReadout(Rank? pending)
    {
        var box = new PanelContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        var column = new VBoxContainer();
        box.AddChild(column);

        var caption = new Label
        {
            Text = "PLACING",
            HorizontalAlignment = HorizontalAlignment.Center,
            ThemeTypeVariation = BastionTheme.PanelTitle,
        };
        column.AddChild(caption);

        var rank = new Label
        {
            Text = pending is null ? "—" : RankLabel(pending.Value),
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        rank.AddThemeFontOverride("font", BastionTheme.MonoFont);
        rank.AddThemeFontSizeOverride("font_size", 34);
        rank.AddThemeColorOverride("font_color",
            _interaction.SelectedFamily == Family.Club ? Palette.Club : Palette.Spade);
        column.AddChild(rank);

        var family = new Label
        {
            Text = _interaction.SelectedFamily == Family.Club ? "Club" : "Spade",
            HorizontalAlignment = HorizontalAlignment.Center,
            ThemeTypeVariation = BastionTheme.Hint,
        };
        column.AddChild(family);

        return box;
    }

    private Button FamilyButton(string text, Family family)
    {
        bool selected = _interaction.SelectedFamily == family;
        var button = new Button { Text = text, ToggleMode = true, ButtonPressed = selected };

        button.AddThemeColorOverride("font_color",
            selected ? (family == Family.Club ? Palette.Club : Palette.Spade) : Palette.TextDim);

        button.Pressed += () => _interaction.SelectFamily(family);
        return button;
    }

    /// <summary>
    /// A captioned row of controls that wraps within itself.
    /// </summary>
    /// <remarks>
    /// Each group owns its own flow container rather than everything sharing one. In a single flow the
    /// captions are just more items and drift into the middle of the preceding row, which reads as
    /// though the buttons after them belong to the group before.
    /// </remarks>
    private HFlowContainer Group(string caption)
    {
        _contextual.AddChild(Caption(caption));

        var row = new HFlowContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        row.AddThemeConstantOverride("h_separation", 8);
        row.AddThemeConstantOverride("v_separation", 6);
        _contextual.AddChild(row);

        return row;
    }

    private static Label Caption(string text) => new()
    {
        Text = text,
        ThemeTypeVariation = BastionTheme.PanelTitle,
    };

    private static Label Paragraph(string text) => new()
    {
        Text = text,
        ThemeTypeVariation = BastionTheme.Hint,
        AutowrapMode = TextServer.AutowrapMode.WordSmart,
        SizeFlagsHorizontal = SizeFlags.ExpandFill,
    };

    /// <summary>
    /// Removes children immediately rather than only queueing them.
    /// </summary>
    /// <remarks>
    /// <c>QueueFree</c> alone defers deletion to the end of the frame, so the outgoing controls stay
    /// children - and stay laid out by the container - while the replacements are added. Detaching
    /// first is what keeps a rebuild from briefly showing both sets.
    /// </remarks>
    private static void Clear(Node parent)
    {
        foreach (Node child in parent.GetChildren())
        {
            parent.RemoveChild(child);
            child.QueueFree();
        }
    }

    // ---------------------------------------------------------------- labels

    private static IEnumerable<SocketRef> AllSockets(TuningData tuning)
    {
        for (int lane = 0; lane < tuning.Geometry.Lanes; lane++)
        {
            for (int s = 0; s < tuning.Geometry.SocketPositions.Count; s++)
            {
                yield return SocketRef.InLane(lane, s);
            }
        }

        yield return SocketRef.Junction;
    }

    /// <summary>
    /// Mirrors <see cref="WaveSession"/>'s adjacency rule for button enablement only. The session is
    /// still the authority; this just avoids offering a move it would refuse.
    /// </summary>
    private static bool AreAdjacent(SocketRef a, SocketRef b, TuningData tuning)
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

    private static StandingOrder NextOrder(StandingOrder current, TowerState tower, TuningData tuning)
    {
        double holdPosition = tower.PositionOn(tuning);

        if (current.IsDefault)
        {
            return new StandingOrder { HoldPastPosition = holdPosition };
        }

        if (current.HoldPastPosition is not null)
        {
            return new StandingOrder { Focus = CoreFocusMode.PreferArmored };
        }

        if (current.Focus == CoreFocusMode.PreferArmored)
        {
            return new StandingOrder { Focus = CoreFocusMode.PreferLeading };
        }

        if (current.Focus == CoreFocusMode.PreferLeading)
        {
            return new StandingOrder { TriggerOnGroup = true };
        }

        return StandingOrder.None;
    }

    private static string OrderLabel(StandingOrder order)
    {
        if (order.IsDefault)
        {
            return "nearest";
        }

        if (order.HoldPastPosition is double p)
        {
            return $"hold past {p:0.0}";
        }

        if (order.Focus == CoreFocusMode.PreferArmored)
        {
            return "focus armored";
        }

        if (order.Focus == CoreFocusMode.PreferLeading)
        {
            return "focus leading";
        }

        return order.TriggerOnGroup ? "trigger on group" : "custom";
    }

    private static string Describe(SocketRef socket) =>
        socket.IsJunction ? "junction" : $"L{socket.LaneIndex}·S{socket.SocketIndex}";

    private static string RankLabel(Rank rank) => rank switch
    {
        Rank.Ace => "A",
        Rank.Jack => "J",
        Rank.Queen => "Q",
        Rank.King => "K",
        _ => ((int)rank).ToString(),
    };
}
