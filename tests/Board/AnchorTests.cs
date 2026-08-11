using Bastion.Core.Board;
using Bastion.Core.Cards;
using Bastion.Core.Config;
using Bastion.Core.Wave;

namespace Bastion.Core.Tests.Board;

/// <summary>
/// The King as anchor: forced replacement cannot evict it.
/// </summary>
/// <remarks>
/// docs/design/04-cards-as-defenses.md § Face cards. One of the four properties face cards buy in
/// place of the runs they cannot form. It blocks eviction, not movement - the adjustment window is a
/// move the player chose and the tower survives it.
/// </remarks>
public sealed class AnchorTests
{
    private static readonly TuningData Tuning = TuningLoader.LoadFromRepositoryRoot();
    private static EncounterTuning Encounter => Tuning.Encounter("example_wave");

    private static SocketRef Bastion(int socket) => SocketRef.InLane(0, socket);

    /// <summary>A wave whose opening two are a King and a Two, the King placed at Bastion(1).</summary>
    private static WaveSession WithKingPlaced(params Rank[] then) =>
        WaveSession.Begin(
                Tuning, Encounter, Shoe.FromOrder([Rank.Ten, Rank.Seven, Rank.King, Rank.Two, .. then]))
            .Place(Family.Club, Bastion(1))
            .Place(Family.Club, Bastion(2));

    [Fact]
    public void A_placed_king_is_an_anchor_and_other_cards_are_not()
    {
        BoardState board = WithKingPlaced().Board();

        Assert.True(board.Towers.Single(t => t.Socket == Bastion(1)).IsAnchor);
        Assert.False(board.Towers.Single(t => t.Socket == Bastion(2)).IsAnchor);
    }

    [Fact]
    public void A_later_card_cannot_be_placed_onto_the_anchor()
    {
        WaveSession awaiting = WithKingPlaced(Rank.Four).Hit();

        Assert.True(awaiting.IsAnchored(Bastion(1)));
        Assert.Throws<InvalidOperationException>(() => awaiting.Place(Family.Club, Bastion(1)));
    }

    [Fact]
    public void The_same_card_may_replace_any_other_tower()
    {
        WaveSession placed = WithKingPlaced(Rank.Four).Hit().Place(Family.Club, Bastion(2));

        Assert.Equal(Rank.Four, placed.Board().Towers.Single(t => t.Socket == Bastion(2)).Card.Rank);
    }

    /// <summary>
    /// A bust cannot cost the anchor either. The busting card is destroyed rather than placed, so it
    /// displaces nothing and never reaches the placement rule at all - <c>Hit</c> locks the wave
    /// outright. The King is still standing on the board the wave resolves against.
    /// </summary>
    [Fact]
    public void A_bust_leaves_the_anchor_standing()
    {
        // King + 2 = 12, then 10 = 22.
        WaveSession busted = WithKingPlaced(Rank.Ten).Hit();

        Assert.Equal(WavePhase.BustLocked, busted.Phase);
        Assert.Equal(Rank.King, busted.Board().Towers.Single(t => t.Socket == Bastion(1)).Card.Rank);
        Assert.True(busted.IsAnchored(Bastion(1)));
    }

    /// <summary>
    /// The Ace Bastion is built King-class for its range and junction exemption, but it is not a
    /// placed card: it re-seats itself on every board derivation, so protecting its socket would
    /// block a placement the player is entitled to make.
    /// </summary>
    [Fact]
    public void The_ace_bastion_is_king_class_but_not_an_anchor()
    {
        // A natural: Ace and Ten as the opening two, earning the free anchor at the junction.
        WaveSession natural = WaveSession.Begin(
                Tuning, Encounter, Shoe.FromOrder([Rank.Ten, Rank.Seven, Rank.Ace, Rank.Ten]))
            .Place(Family.Club, Bastion(0))
            .Place(Family.Club, Bastion(1));

        TowerState bastion = natural.Board().Towers.Single(t => t.Socket == SocketRef.Junction);

        Assert.Equal(Rank.King, bastion.Card.Rank);
        Assert.True(bastion.IgnoresHalfArmor);
        Assert.False(bastion.IsAnchor);
        Assert.False(natural.IsAnchored(SocketRef.Junction));
    }

    /// <summary>The adjustment window still moves an anchor: it blocks eviction, not movement.</summary>
    [Fact]
    public void The_adjustment_window_may_still_relocate_an_anchor()
    {
        WaveSession adjusting = WithKingPlaced().Stand();

        WaveSession moved = adjusting.RelocateTower(Bastion(1), Bastion(0));

        Assert.Equal(Rank.King, moved.Board().Towers.Single(t => t.Socket == Bastion(0)).Card.Rank);
        Assert.DoesNotContain(moved.Board().Towers, t => t.Socket == Bastion(1));
    }
}
