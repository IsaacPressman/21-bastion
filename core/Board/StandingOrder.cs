namespace Bastion.Core.Board;

/// <summary>
/// Which family a card was committed to. Chosen when the card is drawn, permanent for the wave.
/// </summary>
/// <remarks>
/// Hearts and Diamonds are cut from the prototype (docs/prototype/SCOPE.md). The shoe is neutral:
/// every card may become either of these at full effect.
/// </remarks>
public enum Family
{
    /// <summary>Artillery. Keyword: splash.</summary>
    Club,

    /// <summary>Traps and control. Keyword: slow, and ignores half of flat armor.</summary>
    Spade,
}

/// <summary>
/// The optional target preference half of a standing order.
/// </summary>
/// <remarks>
/// docs/design/05-battlefield.md offers Focus as "prefer armored targets, or prefer the leading
/// target" - two alternatives, not two toggles, which is why this is an enum.
/// </remarks>
public enum FocusMode
{
    /// <summary>No preference: fall through to the default nearest-target rule.</summary>
    None,

    /// <summary>Prefer targets with flat armor, when any are in range.</summary>
    PreferArmored,

    /// <summary>Prefer the target furthest along the path.</summary>
    PreferLeading,
}

/// <summary>
/// A tower's pre-committed conditionals, set or changed freely in the adjustment window.
/// </summary>
/// <remarks>
/// <para>
/// Combat takes no live input, so these are the whole of the player's tactical expression during a
/// wave. They are <b>modelled exactly by the resolver or they do not ship</b> - there is no
/// approximately-forecast tier (docs/design/05-battlefield.md).
/// </para>
/// <para>
/// Standing-order changes do not consume the adjustment window's single move
/// (<c>rules.standingOrderChangesConsumeMove</c>).
/// </para>
/// </remarks>
public sealed record StandingOrder
{
    /// <summary>
    /// Hold: fire only at enemies at or past this path position. Null means no Hold order.
    /// </summary>
    /// <remarks>
    /// The design says "past a chosen socket", so callers pass a socket's path position. Stored as
    /// a position rather than a socket index because the junction has no lane socket index and can
    /// still hold.
    /// </remarks>
    public double? HoldPastPosition { get; init; }

    public FocusMode Focus { get; init; } = FocusMode.None;

    /// <summary>
    /// Trigger on group: hold fire until enough enemies are clustered.
    /// </summary>
    /// <remarks>
    /// The threshold and radius are tuned (<c>standingOrders.*</c>) rather than per tower, because
    /// the design describes one behaviour rather than a dial.
    /// </remarks>
    public bool TriggerOnGroup { get; init; }

    /// <summary>No orders: the tower uses the default nearest-target rule and fires whenever ready.</summary>
    public static StandingOrder None { get; } = new();

    /// <summary>True when this order does nothing, which lets the resolver skip the filters entirely.</summary>
    public bool IsDefault => HoldPastPosition is null && Focus == FocusMode.None && !TriggerOnGroup;
}
