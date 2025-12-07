using Microsoft.Xna.Framework;
using TheLastMageStanding.Game.Core.Ecs;
using TheLastMageStanding.Game.Core.Ecs.Components;
using TheLastMageStanding.Game.Core.Events;
using Xunit;

namespace TheLastMageStanding.Game.Tests.Collision;

public class DynamicSeparationTests
{
    [Fact]
    public void Mass_DefaultValue_IsOne()
    {
        // Arrange & Act
        var mass = new Mass(1.0f);

        // Assert
        Assert.Equal(1.0f, mass.Value);
    }

    [Fact]
    public void Knockback_GetDecayedVelocity_ReturnsZeroWhenExpired()
    {
        // Arrange
        var knockback = new Knockback(new Vector2(100, 0), 0.2f);
        knockback.TimeRemaining = 0f;

        // Act
        var velocity = knockback.GetDecayedVelocity();

        // Assert
        Assert.Equal(Vector2.Zero, velocity);
    }

    [Fact]
    public void Knockback_GetDecayedVelocity_DecaysOverTime()
    {
        // Arrange
        var initialVelocity = new Vector2(100, 0);
        var knockback = new Knockback(initialVelocity, 1.0f);
        knockback.TimeRemaining = 0.5f; // Half time remaining

        // Act
        var velocity = knockback.GetDecayedVelocity();

        // Assert
        Assert.Equal(50f, velocity.X, 2); // 50% decay
        Assert.Equal(0f, velocity.Y, 2);
    }

    [Fact]
    public void ContactDamageCooldown_CanTakeDamageFrom_AllowsDifferentEntities()
    {
        // Arrange
        var cooldown = new ContactDamageCooldown(0.5f);
        cooldown.RecordDamage(1, 0f);

        // Act & Assert
        Assert.False(cooldown.CanTakeDamageFrom(1, 0.1f)); // Same entity, too soon
        Assert.True(cooldown.CanTakeDamageFrom(2, 0.1f));  // Different entity
    }

    [Fact]
    public void ContactDamageCooldown_CanTakeDamageFrom_AllowsAfterCooldown()
    {
        // Arrange
        var cooldown = new ContactDamageCooldown(0.5f);
        cooldown.RecordDamage(1, 0f);

        // Act & Assert
        Assert.False(cooldown.CanTakeDamageFrom(1, 0.4f)); // Too soon
        Assert.True(cooldown.CanTakeDamageFrom(1, 0.6f));  // After cooldown
    }

    [Fact]
    public void Collider_WithMass_CanBeAddedToEntity()
    {
        // Arrange
        var world = new EcsWorld();
        var entity = world.CreateEntity();

        // Act
        world.SetComponent(entity, new Position(Vector2.Zero));
        world.SetComponent(entity, new Mass(0.5f));
        world.SetComponent(entity, Collider.CreateCircle(10f, CollisionLayer.Enemy, CollisionLayer.Player));

        // Assert
        Assert.True(world.TryGetComponent(entity, out Mass mass));
        Assert.Equal(0.5f, mass.Value);
    }
}
