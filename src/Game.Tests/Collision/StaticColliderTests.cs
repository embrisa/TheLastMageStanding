using Microsoft.Xna.Framework;
using TheLastMageStanding.Game.Core.Ecs;
using TheLastMageStanding.Game.Core.Ecs.Components;
using Xunit;

namespace TheLastMageStanding.Game.Tests.Collision;

public class StaticColliderTests
{
    [Fact]
    public void StaticCollider_CanBeCreatedInEcs()
    {
        // Arrange
        var world = new EcsWorld();
        var entity = world.CreateEntity();

        // Act
        world.SetComponent(entity, new Position(Vector2.Zero));
        world.SetComponent(entity, Collider.CreateAABB(50f, 50f, CollisionLayer.WorldStatic, CollisionLayer.Player));
        world.SetComponent(entity, new StaticCollider());

        // Assert
        Assert.True(world.TryGetComponent(entity, out StaticCollider _));
        Assert.True(world.TryGetComponent(entity, out Collider collider));
        Assert.Equal(ColliderShape.AABB, collider.Shape);
        Assert.Equal(50f, collider.Width);
        Assert.Equal(50f, collider.Height);
    }

    [Fact]
    public void StaticCollider_WithPosition_HasCorrectBounds()
    {
        // Arrange
        var world = new EcsWorld();
        var entity = world.CreateEntity();
        var position = new Vector2(100f, 200f);

        // Act
        world.SetComponent(entity, new Position(position));
        world.SetComponent(entity, Collider.CreateAABB(50f, 30f, CollisionLayer.WorldStatic, CollisionLayer.Player));
        world.SetComponent(entity, new StaticCollider());

        // Assert
        Assert.True(world.TryGetComponent(entity, out Position pos));
        Assert.True(world.TryGetComponent(entity, out Collider collider));
        
        var bounds = collider.GetWorldBounds(pos.Value);
        Assert.Equal(50, bounds.Left);  // 100 - 50
        Assert.Equal(150, bounds.Right);  // 100 + 50
        Assert.Equal(170, bounds.Top);  // 200 - 30
        Assert.Equal(230, bounds.Bottom);  // 200 + 30
    }

    [Fact]
    public void StaticCollider_HasWorldStaticLayer()
    {
        // Arrange
        var world = new EcsWorld();
        var entity = world.CreateEntity();

        // Act
        world.SetComponent(entity, new Position(Vector2.Zero));
        world.SetComponent(entity, Collider.CreateAABB(25f, 25f, CollisionLayer.WorldStatic, CollisionLayer.Player | CollisionLayer.Enemy));
        world.SetComponent(entity, new StaticCollider());

        // Assert
        Assert.True(world.TryGetComponent(entity, out Collider collider));
        Assert.True(collider.Layer.HasFlag(CollisionLayer.WorldStatic));
        Assert.True(collider.Mask.HasFlag(CollisionLayer.Player));
        Assert.True(collider.Mask.HasFlag(CollisionLayer.Enemy));
    }
}
