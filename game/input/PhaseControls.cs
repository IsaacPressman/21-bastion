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

            // What a replacement costs, in the units it costs it in. "Replaces 2" and "replaces 9"
            // read as the same move and are not remotely the same move - what a card displaces is
            // one of the three clauses of the design's claim, so it is worth a number.
            //
            // An anchored socket is named and disabled rather than hidden: the King's protection is a
            // rule the player should be able to read off the board, not infer from a missing button.
            // The board itself still accepts the click and lets the session refuse it, so a player
            // who tries anyway is recorded as having wanted the move.
            bool anchored = held is { IsAnchor: true };

            var button = new Button
            {
                Disabled = anchored,
                Text = held is null
                    ? Describe(socket)
                    : anchored
                        ? $"{Describe(socket)} — {RankLabel(held.Card.Rank)}, anchored"
                        : $"{Describe(socket)} — replaces {RankLabel(held.Card.Rank)} ({held.ShotDamage:0.0})",
            };

            if (anchored)
            {
                button.TooltipText = "A King is an anchor: forced replacement cannot evict it.";
            }

            SocketRef captured = socket;
            button.Pressed += () => _interaction.Click(captured);
            sockets.AddChild(button);
        }

        BuildStandingOrders();

        _primary.AddChild(CardReadout(pending));
    }

    private void BuildDrawDecision()
    {
        _contextual.AddChild(Caption("THE COST OF ONE MORE CARD"));
        _contextual.AddChild(Paragraph(
            $"Hitting advances the entry by {_controller.Session.NextStepCost():0.0} before the card is revealed. "
            + "The board shades the firing windows that step would eat."));

        BuildStandingOrders();

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

        BuildStandingOrders();

        var lockButton = new Button { Text = "Lock and resolve", ThemeTypeVariation = BastionTheme.PrimaryButton };
        lockButton.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        lockButton.Pressed += () => _controller.Lock();
        _primary.AddChild(lockButton);
    }

    /// <summary>
    /// The standing-order cycle, offered in every phase before combat.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Orders are editable throughout and lock only when combat begins</b>
    /// (docs/design/05-battlefield.md § They are encounter skill, not a secondary menu). They used
    /// to appear in the adjustment window alone; being able to tell a Siege Club to hold for the
    /// armored target <i>at the moment it is placed</i>, rather than several decisions later, is the
    /// whole point of the widening.
    /// </para>
    /// <para>
    /// Setting one re-reads the Visible Threat and redraws the timeline, because an order that
    /// changes what a tower does changes the reading it is being judged against. An order whose
    /// consequence the player cannot see is a menu, which is what this is not.
    /// </para>
    /// </remarks>
    private void BuildStandingOrders()
    {
        BoardState board = _controller.Board;
        TuningData tuning = _controller.Tuning;

        if (board.Towers.Count == 0)
        {
            return;
        }

        HFlowContainer orders = Group("STANDING ORDERS  (free, now → next; locks at combat)");

        foreach (TowerState tower in board.Towers)
        {
            StandingOrder next = NextOrder(tower.Order, tower, tuning);

            // Both states on the face of the button, and what the next one means in the tooltip. A
            // cycle of five that names only where it currently is leaves the player clicking through
            // to find out - which, in a facilitated session, logs as a preference they never had.
            // The explanation is a tooltip rather than a paragraph because the action bar is a fixed
            // height and the primary action has to stay in it.
            var button = new Button
            {
                Text = $"{Describe(tower.Socket)}: {OrderLabel(tower.Order)} → {OrderLabel(next)}",
                TooltipText = Explain(next),
            };

            SocketRef socket = tower.Socket;
            button.Pressed += () => _controller.SetOrder(socket, next);
            orders.AddChild(button);
        }
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

        var power = new Label
        {
            Text = pending is null ? " " : $"{PendingBasePower(pending.Value):0.0} base power",
            HorizontalAlignment = HorizontalAlignment.Center,
            ThemeTypeVariation = BastionTheme.Hint,
        };
        column.AddChild(power);

        return box;
    }

    /// <summary>
    /// Base power of the card awaiting placement, at the value it will actually take.
    /// </summary>
    /// <remarks>
    /// An Ace is the only card whose power is not fixed by its rank, and blackjack - not the player -
    /// decides which way it lands: it counts high unless that would bust. Asking the hand what the
    /// card does to it is exact, and avoids quoting a power the tower will not have.
    /// </remarks>
    private double PendingBasePower(Rank rank)
    {
        int value = rank == Rank.Ace
            ? (_controller.Session.Hand.Hit(Rank.Ace).IsSoft ? 11 : 1)
            : rank.LowValue();

        return _controller.Tuning.CardPower.ForValue(value);
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

    /// <summary>What an order actually does, for the tooltip on the button that would set it.</summary>
    private static string Explain(StandingOrder order)
    {
        if (order.IsDefault)
        {
            return "Fire on the closest enemy in range.";
        }

        if (order.HoldPastPosition is double p)
        {
            return $"Hold fire until an enemy has passed path position {p:0.0}, then fire on the closest.";
        }

        if (order.Focus == CoreFocusMode.PreferArmored)
        {
            return "Prefer an armored enemy over a closer unarmored one, while both are in range.";
        }

        if (order.Focus == CoreFocusMode.PreferLeading)
        {
            return "Prefer the enemy furthest along the path, while both are in range.";
        }

        return order.TriggerOnGroup
            ? "Hold fire until two or more enemies are in range at once."
            : "A custom order.";
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
