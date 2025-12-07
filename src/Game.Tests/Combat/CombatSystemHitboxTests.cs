using Microsoft.Xna.Framework;
using TheLastMageStanding.Game.Core.Ecs;
using TheLastMageStanding.Game.Core.Ecs.Components;
using TheLastMageStanding.Game.Core.Ecs.Systems;
using TheLastMageStanding.Game.Core.Events;
using Xunit;

namespace TheLastMageStanding.Game.Tests.Combat;

public class CombatSystemHitboxTests
{
    [Fact]
    public void PlayerAttack_SpawnsHitboxEntity()
    {
        // Arrange
        var world = new EcsWorld();
        var system = new CombatSystem();
        var eventBus = new EventBus();
        world.EventBus = eventBus;
        system.Initialize(world);

        var player = world.CreateEntity();
        world.SetComponent(player, Faction.Player);
        world.SetComponent(player, new Position(new Vector2(100, 100)));
        world.SetComponent(player, new AttackStats(20f, 0.5f, 42f));
        world.SetComponent(player, new MeleeAttackConfig(42f, Vector2.Zero, 0.15f));

        // Count entities before attack
        var entitiesBeforeAttack = 0;
        world.ForEach<AttackHitbox>((Entity _, ref AttackHitbox _) => entitiesBeforeAttack++);

        // Act - trigger player attack
        eventBus.Publish(new PlayerAttackIntentEvent(player));
        eventBus.ProcessEvents();

        // Assert - hitbox entity was created
        var hitboxCount = 0;
        world.ForEach<AttackHitbox>((Entity _, ref AttackHitbox _) => hitboxCount++);
        Assert.Equal(1, hitboxCount);
    }

    [Fact]
    public void PlayerAttack_HitboxHasCorrectProperties()
    {
        // Arrange
        var world = new EcsWorld();
        var system = new CombatSystem();
        var eventBus = new EventBus();
        world.EventBus = eventBus;
        system.Initialize(world);

        var player = world.CreateEntity();
        world.SetComponent(player, Faction.Player);
        world.SetComponent(player, new Position(new Vector2(100, 100)));
        world.SetComponent(player, new AttackStats(25f, 0.5f, 42f));
        world.SetComponent(player, new MeleeAttackConfig(42f, new Vector2(10, 0), 0.2f));

        // Act
        eventBus.Publish(new PlayerAttackIntentEvent(player));
        eventBus.ProcessEvents();

        // Assert
        Entity? hitboxEntity = null;
        world.ForEach<AttackHitbox>((Entity entity, ref AttackHitbox _) => hitboxEntity = entity);

        Assert.NotNull(hitboxEntity);
        
        var hitbox = world.TryGetComponent(hitboxEntity.Value, out AttackHitbox attackHitbox);
        Assert.True(hitbox);
        Assert.Equal(player.Id, attackHitbox.Owner.Id);
        Assert.Equal(25f, attackHitbox.Damage);
        Assert.Equal(Faction.Player, attackHitbox.OwnerFaction);
        Assert.Equal(0.2f, attackHitbox.LifetimeRemaining);

        // Check collider
        var hasCollider = world.TryGetComponent(hitboxEntity.Value, out Collider collider);
        Assert.True(hasCollider);
        Assert.Equal(ColliderShape.Circle, collider.Shape);
        Assert.Equal(42f, collider.Width);
        Assert.True(collider.IsTrigger);
        Assert.Equal(CollisionLayer.Projectile, collider.Layer);
        Assert.Equal(CollisionLayer.Enemy, collider.Mask);

        // Check position (should be at offset from player)
        var hasPosition = world.TryGetComponent(hitboxEntity.Value, out Position position);
        Assert.True(hasPosition);
        Assert.Equal(110f, position.Value.X);
        Assert.Equal(100f, position.Value.Y);
    }

    [Fact]
    public void PlayerAttack_OnCooldown_DoesNotSpawnHitbox()
    {
        // Arrange
        var world = new EcsWorld();
        var system = new CombatSystem();
        var eventBus = new EventBus();
        world.EventBus = eventBus;
        system.Initialize(world);

        var player = world.CreateEntity();
        world.SetComponent(player, Faction.Player);
        world.SetComponent(player, new Position(new Vector2(100, 100)));
        world.SetComponent(player, new AttackStats(20f, 0.5f, 42f) { CooldownTimer = 0.3f }); // On cooldown
        world.SetComponent(player, new MeleeAttackConfig(42f, Vector2.Zero, 0.15f));

        // Act
        eventBus.Publish(new PlayerAttackIntentEvent(player));
        eventBus.ProcessEvents();

        // Assert - no hitbox spawned
        var hitboxCount = 0;
        world.ForEach<AttackHitbox>((Entity _, ref AttackHitbox _) => hitboxCount++);
        Assert.Equal(0, hitboxCount);
    }

    [Fact]
    public void PlayerAttack_WithoutMeleeConfig_UsesDefaultValues()
    {
        // Arrange
        var world = new EcsWorld();
        var system = new CombatSystem();
        var eventBus = new EventBus();
        world.EventBus = eventBus;
        system.Initialize(world);

        var player = world.CreateEntity();
        world.SetComponent(player, Faction.Player);
        world.SetComponent(player, new Position(new Vector2(100, 100)));
        world.SetComponent(player, new AttackStats(20f, 0.5f, 42f));
        // No MeleeAttackConfig component

        // Act
        eventBus.Publish(new PlayerAttackIntentEvent(player));
        eventBus.ProcessEvents();

        // Assert - hitbox created with default config
        Entity? hitboxEntity = null;
        world.ForEach<AttackHitbox>((Entity entity, ref AttackHitbox _) => hitboxEntity = entity);

        Assert.NotNull(hitboxEntity);
        
        var hasCollider = world.TryGetComponent(hitboxEntity.Value, out Collider collider);
        Assert.True(hasCollider);
        Assert.Equal(42f, collider.Width); // Uses range from AttackStats

        var hasPosition = world.TryGetComponent(hitboxEntity.Value, out Position position);
        Assert.True(hasPosition);
        Assert.Equal(new Vector2(100, 100), position.Value); // No offset
    }

    [Fact]
    public void PlayerAttack_SetsCooldown()
    {
        // Arrange
        var world = new EcsWorld();
        var system = new CombatSystem();
        var eventBus = new EventBus();
        world.EventBus = eventBus;
        system.Initialize(world);

        var player = world.CreateEntity();
        world.SetComponent(player, Faction.Player);
        world.SetComponent(player, new Position(new Vector2(100, 100)));
        world.SetComponent(player, new AttackStats(20f, 0.5f, 42f));

        // Act
        eventBus.Publish(new PlayerAttackIntentEvent(player));
        eventBus.ProcessEvents();

        // Assert - cooldown is set
        var hasAttack = world.TryGetComponent(player, out AttackStats attack);
        Assert.True(hasAttack);
        Assert.Equal(0.5f, attack.CooldownTimer);
    }
}
