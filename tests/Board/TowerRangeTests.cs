using Bastion.Core.Board;
using Bastion.Core.Cards;
using Bastion.Core.Config;

namespace Bastion.Core.Tests.Board;

/// <summary>
/// Range varies by socket, and the face-card allowance is added to it rather than replacing it.
/// </summary>
/// <remarks>
/// <para>
/// The socket-geometry remedy for deep-placement dominance (docs/ROADMAP.md Open Decision 2,
/// docs/design/03-march-clock.md § Deep placement may be weakly dominant). A single flat range gives
/// every socket an identical window at entry 0, so advancement can only ever eat the forward one -
/// which taxes forward placement rather than drawing.
/// </para>
/// <para>
/// The shipped profile may be flat while the candidates are being swept, so these tests run against
/// a deliberately uneven profile. A flat profile cannot distinguish "reads the socket's range" from
/// "reads a constant", which is the whole thing worth pinning here.
/// </para>
/// </remarks>
public sealed class TowerRangeTests
{
    private static readonly TuningData Uneven = Shipped() with
    {
        Geometry = Shipped().Geometry with
        {
            RangeBySocket = [4.0, 3.0, 2.0],
            FaceCardRangeBonus = 1.0,
        },
    };

    private static TuningData Shipped() => TuningLoader.LoadFromRepositoryRoot();

    [Theory]
    [InlineData(0, 4.0)]
    [InlineData(1, 3.0)]
    [InlineData(2, 2.0)]
    public void A_lane_tower_takes_its_own_sockets_range(int socketIndex, double expected)
    {
        double range = TowerState.RangeFor(Uneven, SocketRef.InLane(0, socketIndex), faceCard: false);

        Assert.Equal(expected, range, precision: 6);
    }

    [Fact]
    public void The_junction_takes_the_range_of_the_socket_whose_ground_it_shares()
    {
        // towers.junctionPathPosition is pinned to the middle socket by the loader, so deriving the
        // junction's reach from the same socket keeps position and reach from drifting apart.
        double range = TowerState.RangeFor(Uneven, SocketRef.Junction, faceCard: false);

        Assert.Equal(Uneven.Geometry.RangeBySocket[Uneven.Geometry.MiddleSocketIndex], range, precision: 6);
    }

    [Fact]
    public void A_face_card_sees_further_than_a_number_card_at_the_same_socket()
    {
        // The rule is "face cards see further" (docs/design/04-cards-as-defenses.md). It has to hold
        // at every socket, which is exactly what an absolute face-card range stopped doing once
        // range started varying: an absolute 4.0 would SHORTEN a King at the 4.0-range forward
        // socket, and leave it unchanged rather than extended.
        foreach (int socketIndex in new[] { 0, 1, 2 })
        {
            SocketRef socket = SocketRef.InLane(0, socketIndex);

            double plain = TowerState.RangeFor(Uneven, socket, faceCard: false);
            double face = TowerState.RangeFor(Uneven, socket, faceCard: true);

            Assert.Equal(plain + Uneven.Geometry.FaceCardRangeBonus, face, precision: 6);
            Assert.True(face > plain, $"A face card at socket {socketIndex} must out-range a number card there.");
        }
    }

    [Fact]
    public void A_placed_tower_carries_its_sockets_range()
    {
        // The two construction sites must agree with the derivation, or the resolver fires at a
        // range the geometry never granted.
        TowerState forward = TowerState.Place(
            Uneven, new Card(Rank.Seven), Family.Club, SocketRef.InLane(0, 0), formationMultiplier: 1.0);

        TowerState rear = TowerState.Place(
            Uneven, new Card(Rank.Seven), Family.Club, SocketRef.InLane(0, 2), formationMultiplier: 1.0);

        Assert.Equal(4.0, forward.Range, precision: 6);
        Assert.Equal(2.0, rear.Range, precision: 6);
    }

    [Fact]
    public void A_placed_king_gets_the_allowance_on_top_of_its_socket()
    {
        TowerState king = TowerState.Place(
            Uneven, new Card(Rank.King), Family.Club, SocketRef.InLane(0, 0), formationMultiplier: 1.0);

        Assert.Equal(5.0, king.Range, precision: 6);
    }
}
