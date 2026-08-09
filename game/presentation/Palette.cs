using Godot;

namespace Bastion.Game.Presentation;

/// <summary>
/// Every colour the game draws, in one place.
/// </summary>
/// <remarks>
/// <para>
/// The theme and the hand-drawn battlefield have to agree, and they cannot if each carries its own
/// literals - a panel border and a lane edge drifting apart is exactly how a built-in-code UI starts
/// looking assembled rather than designed.
/// </para>
/// <para>
/// <b>Nothing here encodes a judgement.</b> Colours separate <i>kinds</i> of thing - a lane from a
/// tower, a Club from a Spade, the revealed force from the complete army - and never rank an option
/// as better or worse. There is no green-for-good on a choice the player has yet to make; the two
/// accents below mark the two forecast types, which are different questions, not different verdicts
/// (docs/design/09-information-and-ui.md).
/// </para>
/// </remarks>
internal static class Palette
{
    // Surfaces.
    internal static readonly Color Background = new("0d1219");
    internal static readonly Color Surface = new("161d28");
    internal static readonly Color SurfaceRaised = new("1c2532");
    internal static readonly Color SurfaceEdge = new("273246");

    // The board.
    internal static readonly Color LaneFill = new("141b26");
    internal static readonly Color LaneEdge = new("2b3648");
    internal static readonly Color SocketEdge = new("445066");
    internal static readonly Color SocketHover = new("8fa4c4");
    internal static readonly Color SocketTarget = new("7fb7a8");
    internal static readonly Color SocketPicked = new("d8c26a");
    internal static readonly Color Entry = new("c95d5d");
    internal static readonly Color LostWindow = new(0.79f, 0.36f, 0.36f, 0.26f);
    internal static readonly Color Coverage = new(0.42f, 0.62f, 0.78f, 0.16f);

    // Families.
    internal static readonly Color Club = new("d08a3e");
    internal static readonly Color Spade = new("4aa3a2");

    // Enemies, by where they came from - not by how dangerous they are.
    internal static readonly Color BaseWaveEnemy = new("9e4350");
    internal static readonly Color DealerEnemy = new("cf4f5f");
    internal static readonly Color Vanguard = new("d0a24a");
    internal static readonly Color SlowTint = new("6fb7d6");
    internal static readonly Color AuraRing = new(0.82f, 0.64f, 0.29f, 0.35f);

    // Combat effects.
    internal static readonly Color Shot = new("ffe6a3");
    internal static readonly Color Splash = new("f0a868");
    internal static readonly Color SlowHit = new("8fd3ef");
    internal static readonly Color Death = new("ff8a7a");
    internal static readonly Color Overload = new("ffd166");

    // Health.
    internal static readonly Color Health = new("6fd08a");
    internal static readonly Color HealthBack = new("2a1620");

    // Text.
    internal static readonly Color Text = new("e6ecf5");
    internal static readonly Color TextDim = new("94a2b8");
    internal static readonly Color TextFaint = new("64738a");
    internal static readonly Color OnTower = new("11161d");

    // The two forecasts. Different questions, marked as such - never better and worse.
    internal static readonly Color VisibleThreat = new("d0a24a");
    internal static readonly Color FinalForecast = new("6fd08a");
    internal static readonly Color Bust = new("e2686a");
}
