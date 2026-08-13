using Godot;

namespace Bastion.Game.Presentation;

/// <summary>
/// The screen's regions, shared by the Control tree and the hand-drawn board.
/// </summary>
/// <remarks>
/// The battlefield is a <see cref="Node2D"/> drawing in world space while everything around it is
/// anchored Controls, so the two cannot discover each other's bounds through the scene tree. They
/// agree here instead: the Controls anchor to these edges and the board derives its rect from the
/// same numbers, which is what keeps the board from drifting under a panel when the window resizes.
/// </remarks>
internal static class Layout
{
    /// <summary>The phase banner across the top.</summary>
    internal const float HeaderHeight = 46f;

    /// <summary>
    /// The action bar along the bottom. Tall enough for the adjustment window's three rows - moves,
    /// then standing orders over two - beside the primary action, which is what keeps the primary
    /// action on screen at any board size. The board gives up the height and has it to spare: two
    /// lanes at their capped height plus the junction gap need well under what is left.
    /// </summary>
    internal const float ActionBarHeight = 190f;

    /// <summary>The information column down the right: the two consequence panels and the review.</summary>
    internal const float RightColumnWidth = 396f;

    /// <summary>
    /// The encounter timeline, between the board and the action bar.
    /// </summary>
    /// <remarks>
    /// <b>Its own region, because it is its own axis.</b> The board is path space - where things are
    /// - and the timeline is time space - when they happen. Both are primary and neither can be
    /// folded into the other: a lane band already spends its width on path position, so a second
    /// x-axis inside it would be two different scales in one strip. The timeline is called the
    /// primary visual language of the encounter (docs/design/14-encounter-timeline.md), and it is
    /// sized to be readable rather than glanceable.
    /// </remarks>
    internal const float TimelineHeight = 168f;

    /// <summary>Breathing room between the board's outer edge and the lanes themselves.</summary>
    internal const float BoardPadLeft = 76f;
    internal const float BoardPadRight = 34f;
    internal const float BoardPadTop = 34f;
    internal const float BoardPadBottom = 26f;

    /// <summary>The rectangle the battlefield owns, for a viewport of the given size.</summary>
    internal static Rect2 BoardArea(Vector2 viewport) => new(
        0f,
        HeaderHeight,
        Mathf.Max(320f, viewport.X - RightColumnWidth),
        Mathf.Max(220f, viewport.Y - HeaderHeight - TimelineHeight - ActionBarHeight));

    /// <summary>The strip the encounter timeline owns, directly beneath the board.</summary>
    /// <remarks>
    /// Derived from the same numbers as <see cref="BoardArea"/> and stacked against it rather than
    /// anchored independently, so the two cannot drift apart or overlap when the window resizes.
    /// </remarks>
    internal static Rect2 TimelineArea(Vector2 viewport)
    {
        Rect2 board = BoardArea(viewport);

        return new Rect2(0f, board.Position.Y + board.Size.Y, board.Size.X, TimelineHeight);
    }
}
