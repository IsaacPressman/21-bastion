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
    /// The action bar along the bottom. Tall enough for two rows of wrapped contextual buttons beside
    /// the primary action, which is what keeps the primary action on screen at any board size.
    /// </summary>
    internal const float ActionBarHeight = 156f;

    /// <summary>The information column down the right: the two consequence panels and the review.</summary>
    internal const float RightColumnWidth = 396f;

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
        Mathf.Max(220f, viewport.Y - HeaderHeight - ActionBarHeight));
}
