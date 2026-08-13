using Bastion.Core.Board;
using Bastion.Core.Cards;
using Bastion.Core.Wave;

namespace Bastion.Game.Input;

/// <summary>
/// What the player is currently pointing at and has half-chosen on the board.
/// </summary>
/// <remarks>
/// <para>
/// Placement and adjustment are spatial decisions - "where it goes, and what it displaces" - so they
/// are made by clicking the board. That needs one piece of state two nodes share: the battlefield
/// draws the hover and the picked-up tower, the action bar drives the family toggle, and both have to
/// see the same thing. This is that state, plus the dispatch of a completed gesture.
/// </para>
/// <para>
/// <b>It adds no rules.</b> A click is forwarded to <see cref="WaveController"/> and the session
/// decides; an illegal gesture is refused there and reported, not filtered out here. What it does do
/// is model the two-step nature of an adjustment - pick a tower up, then choose where it goes - which
/// has no representation in the session because the session only ever sees the finished move.
/// </para>
/// <para>
/// Plain C# events rather than Godot signals: <see cref="SocketRef"/> is a core struct and not
/// Variant-compatible, and marshalling it into lane/index pairs to cross a signal would lose the type
/// the rest of the layer is built on.
/// </para>
/// </remarks>
public sealed class BoardInteraction
{
    private readonly WaveController _controller;

    public BoardInteraction(WaveController controller)
    {
        _controller = controller;
        _controller.StateChanged += OnStateChanged;
    }

    /// <summary>
    /// Raised when the chosen family or the picked-up tower changes - a change of intent.
    /// </summary>
    internal event Action? Changed;

    /// <summary>
    /// Raised when the pointer moves to a different socket.
    /// </summary>
    /// <remarks>
    /// Kept apart from <see cref="Changed"/> because the two have very different costs. The action bar
    /// rebuilds its buttons on a change of intent, which must not happen every time the pointer crosses
    /// a socket - that would discard and recreate every control mid-hover. Only the board listens here.
    /// </remarks>
    internal event Action? PointerMoved;

    /// <summary>
    /// The player has looked at what one family-and-socket combination would do.
    /// </summary>
    /// <remarks>
    /// <b>The hover-brute-force measurement</b> (docs/prototype/VALIDATION.md § Improved-encounter
    /// instrumentation). The candidate preview is the design's most dangerous surface: full
    /// information plus exhaustive preview turns placement into search, and the guardrail against
    /// it is that the failure is <i>detected</i> rather than assumed absent
    /// (docs/design/14-encounter-timeline.md § The solvable-puzzle risk).
    /// <para>
    /// So this fires whenever a combination is inspected - a new socket under the pointer, or the
    /// same socket with the family switched - and <c>game/telemetry/PlaytestLog.cs</c> counts them.
    /// If players routinely inspect nearly every combination before committing, the preview is
    /// working as an oracle and the response is to reduce sortable outputs, not to hide information.
    /// </para>
    /// </remarks>
    internal event Action<Family, SocketRef>? CandidateInspected;

    /// <summary>The family the next placement will lock in. Chosen before the socket.</summary>
    internal Family SelectedFamily { get; private set; } = Family.Club;

    /// <summary>The socket under the pointer, or null when the pointer is off the board.</summary>
    internal SocketRef? Hovered { get; private set; }

    /// <summary>
    /// In the adjustment window, the tower awaiting a destination. Null when nothing is picked up.
    /// </summary>
    internal SocketRef? PickedUp { get; private set; }

    /// <summary>Whether clicking the board does anything in the current phase.</summary>
    internal bool BoardAcceptsClicks => _controller.Session.Phase
        is WavePhase.AwaitingPlacement or WavePhase.AdjustmentWindow;

    internal void SelectFamily(Family family)
    {
        if (SelectedFamily == family)
        {
            return;
        }

        SelectedFamily = family;
        Changed?.Invoke();
        ReportInspection();
    }

    internal void Hover(SocketRef? socket)
    {
        if (Nullable.Equals(Hovered, socket))
        {
            return;
        }

        Hovered = socket;
        PointerMoved?.Invoke();
        ReportInspection();
    }

    /// <summary>
    /// Announces the combination now under consideration, when there is a card to place.
    /// </summary>
    /// <remarks>
    /// Only during placement: the pointer crossing the board in the adjustment window is not a
    /// candidate being weighed, and counting it would inflate the very measurement that is supposed
    /// to detect searching.
    /// </remarks>
    private void ReportInspection()
    {
        if (Hovered is { } socket && _controller.Session.Phase == WavePhase.AwaitingPlacement)
        {
            CandidateInspected?.Invoke(SelectedFamily, socket);
        }
    }

    /// <summary>
    /// Acts on a click at <paramref name="socket"/>, interpreted for the current phase.
    /// </summary>
    /// <remarks>
    /// Placement is one click - the family is already chosen. An adjustment is two: the first picks a
    /// tower up, the second drops it on an empty socket (a relocation) or onto another tower (a swap).
    /// Clicking the picked-up tower again puts it back down, so the gesture is always escapable.
    /// </remarks>
    internal void Click(SocketRef socket)
    {
        switch (_controller.Session.Phase)
        {
            case WavePhase.AwaitingPlacement:
                _controller.Place(SelectedFamily, socket);
                break;

            case WavePhase.AdjustmentWindow:
                ClickInAdjustment(socket);
                break;
        }
    }

    /// <summary>Puts down whatever is picked up, without moving it.</summary>
    internal void ClearPickedUp()
    {
        if (PickedUp is null)
        {
            return;
        }

        PickedUp = null;
        Changed?.Invoke();
    }

    private void ClickInAdjustment(SocketRef socket)
    {
        if (PickedUp is not { } from)
        {
            // Only a socket with something on it can be picked up; an empty one is not a gesture.
            if (_controller.Board.Towers.Any(t => t.Socket == socket))
            {
                PickedUp = socket;
                Changed?.Invoke();
            }

            return;
        }

        if (from == socket)
        {
            ClearPickedUp();
            return;
        }

        bool occupied = _controller.Board.Towers.Any(t => t.Socket == socket);

        // The session rules on legality - adjacency, the single move, whether a swap is allowed at
        // all. Either way the tower goes back down, so a refused move does not leave a stuck gesture.
        if (occupied)
        {
            _controller.Swap(from, socket);
        }
        else
        {
            _controller.Relocate(from, socket);
        }

        PickedUp = null;
        Changed?.Invoke();
    }

    private void OnStateChanged()
    {
        // A new phase, a placed card, or a spent move all end whatever gesture was in progress.
        if (PickedUp is not null)
        {
            PickedUp = null;
            Changed?.Invoke();
        }
    }
}
