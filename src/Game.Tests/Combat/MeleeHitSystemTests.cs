using Microsoft.Xna.Framework;
using TheLastMageStanding.Game.Core.Camera;
using TheLastMageStanding.Game.Core.Ecs;
using TheLastMageStanding.Game.Core.Ecs.Components;
using TheLastMageStanding.Game.Core.Ecs.Systems;
using TheLastMageStanding.Game.Core.Events;
using TheLastMageStanding.Game.Core.Input;
using Xunit;

namespace TheLastMageStanding.Game.Tests.Combat;

public class MeleeHitSystemTests
{
    [Fact]
    public void AttackHitbox_HitsEnemy_DealsDamage()
    {
        // Arrange
        var world = new EcsWorld();
        var system = new MeleeHitSystem();
        var eventBus = new EventBus();
        world.EventBus = eventBus;
        system.Initialize(world);

        var player = world.CreateEntity();
        world.SetComponent(player, Faction.Player);
        world.SetComponent(player, new Position(new Vector2(100, 100)));

        var enemy = world.CreateEntity();
        world.SetComponent(enemy, Faction.Enemy);
        world.SetComponent(enemy, new Position(new Vector2(110, 100)));
        world.SetComponent(enemy, new Health(50f, 50f));
        world.SetComponent(enemy, new Hurtbox { IsInvulnerable = false });

        var hitbox = world.CreateEntity();
        world.SetComponent(hitbox, new Position(new Vector2(110, 100)));
        world.SetComponent(hitbox, new AttackHitbox(player, 25f, Faction.Player, 0.5f));
        world.SetComponent(hitbox, Collider.CreateCircle(10f, CollisionLayer.Projectile, CollisionLayer.Enemy, isTrigger: true));

        // Track damage events
        EntityDamagedEvent? damageEvent = null;
        eventBus.Subscribe<EntityDamagedEvent>(evt => damageEvent = evt);

        // Act - simulate collision
        var collisionEvent = new CollisionEnterEvent(hitbox, enemy, new Vector2(110, 100), Vector2.UnitX);
        eventBus.Publish(collisionEvent);
        eventBus.ProcessEvents();

        // Assert
        Assert.NotNull(damageEvent);
        Assert.Equal(enemy.Id, damageEvent.Value.Target.Id);
        Assert.Equal(25f, damageEvent.Value.Amount);
        Assert.Equal(Faction.Player, damageEvent.Value.SourceFaction);
    }

    [Fact]
    public void AttackHitbox_SameFaction_NoHit()
    {
        // Arrange
        var world = new EcsWorld();
        var system = new MeleeHitSystem();
        var eventBus = new EventBus();
        world.EventBus = eventBus;
        system.Initialize(world);

        var player = world.CreateEntity();
        world.SetComponent(player, Faction.Player);
        world.SetComponent(player, new Position(new Vector2(100, 100)));

        var ally = world.CreateEntity();
        world.SetComponent(ally, Faction.Player);
        world.SetComponent(ally, new Position(new Vector2(110, 100)));
        world.SetComponent(ally, new Health(50f, 50f));
        world.SetComponent(ally, new Hurtbox { IsInvulnerable = false });

        var hitbox = world.CreateEntity();
        world.SetComponent(hitbox, new Position(new Vector2(110, 100)));
        world.SetComponent(hitbox, new AttackHitbox(player, 25f, Faction.Player, 0.5f));
        world.SetComponent(hitbox, Collider.CreateCircle(10f, CollisionLayer.Projectile, CollisionLayer.Player, isTrigger: true));

        // Track damage events
        var damageCount = 0;
        eventBus.Subscribe<EntityDamagedEvent>(_ => damageCount++);

        // Act - simulate collision
        var collisionEvent = new CollisionEnterEvent(hitbox, ally, new Vector2(110, 100), Vector2.UnitX);
        eventBus.Publish(collisionEvent);
        eventBus.ProcessEvents();

        // Assert - no damage should be dealt
        Assert.Equal(0, damageCount);
    }

    [Fact]
    public void AttackHitbox_SelfHit_Prevented()
    {
        // Arrange
        var world = new EcsWorld();
        var system = new MeleeHitSystem();
        var eventBus = new EventBus();
        world.EventBus = eventBus;
        system.Initialize(world);

        var player = world.CreateEntity();
        world.SetComponent(player, Faction.Player);
        world.SetComponent(player, new Position(new Vector2(100, 100)));
        world.SetComponent(player, new Health(100f, 100f));
        world.SetComponent(player, new Hurtbox { IsInvulnerable = false });

        var hitbox = world.CreateEntity();
        world.SetComponent(hitbox, new Position(new Vector2(100, 100)));
        world.SetComponent(hitbox, new AttackHitbox(player, 25f, Faction.Player, 0.5f));

        // Track damage events
        var damageCount = 0;
        eventBus.Subscribe<EntityDamagedEvent>(_ => damageCount++);

        // Act - simulate collision with self
        var collisionEvent = new CollisionEnterEvent(hitbox, player, new Vector2(100, 100), Vector2.UnitX);
        eventBus.Publish(collisionEvent);
        eventBus.ProcessEvents();

        // Assert - no self-damage
        Assert.Equal(0, damageCount);
    }

    [Fact]
    public void AttackHitbox_HitsSameTargetOnce()
    {
        // Arrange
        var world = new EcsWorld();
        var system = new MeleeHitSystem();
        var eventBus = new EventBus();
        world.EventBus = eventBus;
        system.Initialize(world);

        var player = world.CreateEntity();
        world.SetComponent(player, Faction.Player);

        var enemy = world.CreateEntity();
        world.SetComponent(enemy, Faction.Enemy);
        world.SetComponent(enemy, new Position(new Vector2(110, 100)));
        world.SetComponent(enemy, new Health(50f, 50f));
        world.SetComponent(enemy, new Hurtbox { IsInvulnerable = false });

        var hitbox = world.CreateEntity();
        world.SetComponent(hitbox, new Position(new Vector2(110, 100)));
        world.SetComponent(hitbox, new AttackHitbox(player, 25f, Faction.Player, 0.5f));

        // Track damage events
        var damageCount = 0;
        eventBus.Subscribe<EntityDamagedEvent>(_ => damageCount++);

        // Act - simulate multiple collisions with same target
        var collisionEvent = new CollisionEnterEvent(hitbox, enemy, new Vector2(110, 100), Vector2.UnitX);
        eventBus.Publish(collisionEvent);
        eventBus.Publish(collisionEvent); // Second hit
        eventBus.Publish(collisionEvent); // Third hit
        eventBus.ProcessEvents();

        // Assert - should only hit once
        Assert.Equal(1, damageCount);
    }

    [Fact]
    public void AttackHitbox_Invulnerable_NoHit()
    {
        // Arrange
        var world = new EcsWorld();
        var system = new MeleeHitSystem();
        var eventBus = new EventBus();
        world.EventBus = eventBus;
        system.Initialize(world);

        var player = world.CreateEntity();
        world.SetComponent(player, Faction.Player);

        var enemy = world.CreateEntity();
        world.SetComponent(enemy, Faction.Enemy);
        world.SetComponent(enemy, new Position(new Vector2(110, 100)));
        world.SetComponent(enemy, new Health(50f, 50f));
        world.SetComponent(enemy, new Hurtbox { IsInvulnerable = true, InvulnerabilityEndsAt = 999999f });

        var hitbox = world.CreateEntity();
        world.SetComponent(hitbox, new Position(new Vector2(110, 100)));
        world.SetComponent(hitbox, new AttackHitbox(player, 25f, Faction.Player, 0.5f));

        // Track damage events
        var damageCount = 0;
        eventBus.Subscribe<EntityDamagedEvent>(_ => damageCount++);

        // Act
        var collisionEvent = new CollisionEnterEvent(hitbox, enemy, new Vector2(110, 100), Vector2.UnitX);
        eventBus.Publish(collisionEvent);
        eventBus.ProcessEvents();

        // Assert - no damage due to invulnerability
        Assert.Equal(0, damageCount);
    }

    [Fact]
    public void AttackHitbox_DeadTarget_NoHit()
    {
        // Arrange
        var world = new EcsWorld();
        var system = new MeleeHitSystem();
        var eventBus = new EventBus();
        world.EventBus = eventBus;
        system.Initialize(world);

        var player = world.CreateEntity();
        world.SetComponent(player, Faction.Player);

        var enemy = world.CreateEntity();
        world.SetComponent(enemy, Faction.Enemy);
        world.SetComponent(enemy, new Position(new Vector2(110, 100)));
        world.SetComponent(enemy, new Health(0f, 50f)); // Dead
        world.SetComponent(enemy, new Hurtbox { IsInvulnerable = false });

        var hitbox = world.CreateEntity();
        world.SetComponent(hitbox, new Position(new Vector2(110, 100)));
        world.SetComponent(hitbox, new AttackHitbox(player, 25f, Faction.Player, 0.5f));

        // Track damage events
        var damageCount = 0;
        eventBus.Subscribe<EntityDamagedEvent>(_ => damageCount++);

        // Act
        var collisionEvent = new CollisionEnterEvent(hitbox, enemy, new Vector2(110, 100), Vector2.UnitX);
        eventBus.Publish(collisionEvent);
        eventBus.ProcessEvents();

        // Assert - no damage to dead target
        Assert.Equal(0, damageCount);
    }

    [Fact]
    public void AttackHitbox_Lifetime_ExpiresAndRemoved()
    {
        // Arrange
        var world = new EcsWorld();
        var system = new MeleeHitSystem();
        var eventBus = new EventBus();
        world.EventBus = eventBus;
        system.Initialize(world);

        var player = world.CreateEntity();
        var hitbox = world.CreateEntity();
        world.SetComponent(hitbox, new AttackHitbox(player, 25f, Faction.Player, 0.1f));

        // Act - update past lifetime
        var gameTime = new GameTime();
        var input = new InputState();
        var camera = new Camera2D(800, 600);
        system.Update(world, new EcsUpdateContext(gameTime, 0.15f, input, camera));

        // Assert - hitbox should be destroyed
        Assert.False(world.TryGetComponent(hitbox, out AttackHitbox _));
    }

    [Fact]
    public void Hurtbox_Invulnerability_ExpiresOverTime()
    {
        // Arrange
        var world = new EcsWorld();
        var system = new MeleeHitSystem();
        var eventBus = new EventBus();
        world.EventBus = eventBus;
        system.Initialize(world);

        var entity = world.CreateEntity();
        world.SetComponent(entity, new Hurtbox { IsInvulnerable = true, InvulnerabilityEndsAt = 0.05f });

        // Act - update past invulnerability time
        var gameTime = new GameTime();
        var input = new InputState();
        var camera = new Camera2D(800, 600);
        system.Update(world, new EcsUpdateContext(gameTime, 0.1f, input, camera));

        // Assert
        world.TryGetComponent(entity, out Hurtbox hurtbox);
        Assert.False(hurtbox.IsInvulnerable);
    }
}
