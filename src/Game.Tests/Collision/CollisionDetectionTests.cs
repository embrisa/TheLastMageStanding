using Microsoft.Xna.Framework;
using TheLastMageStanding.Game.Core.Ecs.Components;
using TheLastMageStanding.Game.Core.Ecs.Systems.Collision;
using Xunit;

namespace TheLastMageStanding.Game.Tests.Collision;

public class CollisionDetectionTests
{
    [Fact]
    public void CircleCircle_NoOverlap_ReturnsNoCollision()
    {
        // Arrange
        var colliderA = Collider.CreateCircle(10f, CollisionLayer.Player, CollisionLayer.Enemy);
        var colliderB = Collider.CreateCircle(10f, CollisionLayer.Enemy, CollisionLayer.Player);
        var posA = new Vector2(0, 0);
        var posB = new Vector2(50, 0);

        // Act
        var result = CollisionDetection.TestCollision(colliderA, posA, colliderB, posB);

        // Assert
        Assert.False(result.IsColliding);
    }

    [Fact]
    public void CircleCircle_Overlapping_ReturnsCollision()
    {
        // Arrange
        var colliderA = Collider.CreateCircle(10f, CollisionLayer.Player, CollisionLayer.Enemy);
        var colliderB = Collider.CreateCircle(10f, CollisionLayer.Enemy, CollisionLayer.Player);
        var posA = new Vector2(0, 0);
        var posB = new Vector2(15, 0); // Overlapping by 5 units

        // Act
        var result = CollisionDetection.TestCollision(colliderA, posA, colliderB, posB);

        // Assert
        Assert.True(result.IsColliding);
        Assert.True(result.Penetration > 0f);
    }

    [Fact]
    public void CircleCircle_Touching_ReturnsCollision()
    {
        // Arrange
        var colliderA = Collider.CreateCircle(10f, CollisionLayer.Player, CollisionLayer.Enemy);
        var colliderB = Collider.CreateCircle(10f, CollisionLayer.Enemy, CollisionLayer.Player);
        var posA = new Vector2(0, 0);
        var posB = new Vector2(20, 0); // Exactly touching

        // Act
        var result = CollisionDetection.TestCollision(colliderA, posA, colliderB, posB);

        // Assert
        Assert.True(result.IsColliding);
    }

    [Fact]
    public void CircleAABB_NoOverlap_ReturnsNoCollision()
    {
        // Arrange
        var circle = Collider.CreateCircle(5f, CollisionLayer.Player, CollisionLayer.WorldStatic);
        var aabb = Collider.CreateAABB(10f, 10f, CollisionLayer.WorldStatic, CollisionLayer.Player);
        var circlePos = new Vector2(50, 0);
        var aabbPos = new Vector2(0, 0);

        // Act
        var result = CollisionDetection.TestCollision(circle, circlePos, aabb, aabbPos);

        // Assert
        Assert.False(result.IsColliding);
    }

    [Fact]
    public void CircleAABB_Overlapping_ReturnsCollision()
    {
        // Arrange
        var circle = Collider.CreateCircle(5f, CollisionLayer.Player, CollisionLayer.WorldStatic);
        var aabb = Collider.CreateAABB(10f, 10f, CollisionLayer.WorldStatic, CollisionLayer.Player);
        var circlePos = new Vector2(12, 0); // Circle overlaps right edge of AABB
        var aabbPos = new Vector2(0, 0);

        // Act
        var result = CollisionDetection.TestCollision(circle, circlePos, aabb, aabbPos);

        // Assert
        Assert.True(result.IsColliding);
    }

    [Fact]
    public void AABBAABB_NoOverlap_ReturnsNoCollision()
    {
        // Arrange
        var aabbA = Collider.CreateAABB(10f, 10f, CollisionLayer.Enemy, CollisionLayer.Player);
        var aabbB = Collider.CreateAABB(10f, 10f, CollisionLayer.Player, CollisionLayer.Enemy);
        var posA = new Vector2(0, 0);
        var posB = new Vector2(50, 0);

        // Act
        var result = CollisionDetection.TestCollision(aabbA, posA, aabbB, posB);

        // Assert
        Assert.False(result.IsColliding);
    }

    [Fact]
    public void AABBAABB_Overlapping_ReturnsCollision()
    {
        // Arrange
        var aabbA = Collider.CreateAABB(10f, 10f, CollisionLayer.Enemy, CollisionLayer.Player);
        var aabbB = Collider.CreateAABB(10f, 10f, CollisionLayer.Player, CollisionLayer.Enemy);
        var posA = new Vector2(0, 0);
        var posB = new Vector2(15, 0); // Overlapping by 5 units on X axis

        // Act
        var result = CollisionDetection.TestCollision(aabbA, posA, aabbB, posB);

        // Assert
        Assert.True(result.IsColliding);
        Assert.True(result.Penetration > 0f);
    }

    [Fact]
    public void LayerMask_NoMatch_ReturnsNoCollision()
    {
        // Arrange - Player can only collide with Enemy, not with other Players
        var colliderA = Collider.CreateCircle(10f, CollisionLayer.Player, CollisionLayer.Enemy);
        var colliderB = Collider.CreateCircle(10f, CollisionLayer.Player, CollisionLayer.Enemy);
        var posA = new Vector2(0, 0);
        var posB = new Vector2(5, 0); // Overlapping

        // Act
        var result = CollisionDetection.TestCollision(colliderA, posA, colliderB, posB);

        // Assert - Should not collide because layer/mask don't match
        Assert.False(result.IsColliding);
    }

    [Fact]
    public void LayerMask_Match_ReturnsCollision()
    {
        // Arrange - Player can collide with Enemy
        var player = Collider.CreateCircle(10f, CollisionLayer.Player, CollisionLayer.Enemy);
        var enemy = Collider.CreateCircle(10f, CollisionLayer.Enemy, CollisionLayer.Player);
        var posA = new Vector2(0, 0);
        var posB = new Vector2(5, 0); // Overlapping

        // Act
        var result = CollisionDetection.TestCollision(player, posA, enemy, posB);

        // Assert
        Assert.True(result.IsColliding);
    }

    [Fact]
    public void CanCollide_BothDirectionsMatch_ReturnsTrue()
    {
        // A is Player layer with Enemy mask
        // B is Enemy layer with Player mask
        var result = CollisionDetection.CanCollide(
            CollisionLayer.Player, CollisionLayer.Enemy,
            CollisionLayer.Enemy, CollisionLayer.Player
        );

        Assert.True(result);
    }

    [Fact]
    public void CanCollide_OneDirectionMatches_ReturnsTrue()
    {
        // A is Player layer with Enemy mask
        // B is Enemy layer with no mask
        var result = CollisionDetection.CanCollide(
            CollisionLayer.Player, CollisionLayer.Enemy,
            CollisionLayer.Enemy, CollisionLayer.None
        );

        Assert.True(result);
    }

    [Fact]
    public void CanCollide_NoMatch_ReturnsFalse()
    {
        // A is Player layer with Enemy mask
        // B is WorldStatic layer with Player mask
        var result = CollisionDetection.CanCollide(
            CollisionLayer.Player, CollisionLayer.Enemy,
            CollisionLayer.WorldStatic, CollisionLayer.Enemy
        );

        Assert.False(result);
    }

    [Fact]
    public void CircleCollider_WithOffset_UsesCorrectWorldPosition()
    {
        // Arrange
        var offset = new Vector2(10, 5);
        var collider = Collider.CreateCircle(5f, CollisionLayer.Player, CollisionLayer.Enemy, offset: offset);
        var position = new Vector2(100, 100);

        // Act
        var worldCenter = collider.GetWorldCenter(position);

        // Assert
        Assert.Equal(110f, worldCenter.X);
        Assert.Equal(105f, worldCenter.Y);
    }
}
