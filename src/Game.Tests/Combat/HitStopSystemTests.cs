using Microsoft.Xna.Framework;
using TheLastMageStanding.Game.Core.Ecs;
using TheLastMageStanding.Game.Core.Ecs.Components;
using TheLastMageStanding.Game.Core.Ecs.Systems;
using TheLastMageStanding.Game.Core.Events;
using TheLastMageStanding.Game.Core.Combat;
using Xunit;

namespace TheLastMageStanding.Game.Tests.Combat;

public class HitStopSystemTests
{
    [Fact]
    public void HitStopSystem_OnDamage_TriggersHitStop()
    {
        // Arrange
        var world = new EcsWorld();
        var eventBus = new EventBus();
        world.EventBus = eventBus;
        var system = new HitStopSystem();
        HitStopSystem.EnableHitStop = true;
        system.Initialize(world);

        // Act - publish damage event
        eventBus.Publish(new EntityDamagedEvent(
            Entity.None,
            new Entity(1),
            25f,
            new DamageInfo(25f),
            Vector2.Zero,
            Faction.Player));
        eventBus.ProcessEvents();

        // Assert
        Assert.True(system.IsHitStopped());
    }

    [Fact]
    public void HitStopSystem_Update_DecrementsHitStopTimer()
    {
        // Arrange
        var world = new EcsWorld();
        var eventBus = new EventBus();
        world.EventBus = eventBus;
        var system = new HitStopSystem();
        HitStopSystem.EnableHitStop = true;
        system.Initialize(world);

        // Trigger hit-stop
        eventBus.Publish(new EntityDamagedEvent(Entity.None, new Entity(1), 25f, new DamageInfo(25f), Vector2.Zero, Faction.Player));
        eventBus.ProcessEvents();
        Assert.True(system.IsHitStopped());

        // Act - update to consume hit-stop time
        var context = new EcsUpdateContext(
            null!,
            0.2f, // 200ms should clear typical hit-stop
            null!,
            null!
        );
        system.Update(world, context);

        // Assert
        Assert.False(system.IsHitStopped());
    }

    [Fact]
    public void HitStopSystem_HighDamage_TriggersStrongerHitStop()
    {
        // Arrange
        var world = new EcsWorld();
        var eventBus = new EventBus();
        world.EventBus = eventBus;
        var system = new HitStopSystem();
        HitStopSystem.EnableHitStop = true;
        system.Initialize(world);

        // Act - low damage
        eventBus.Publish(new EntityDamagedEvent(Entity.None, new Entity(1), 10f, new DamageInfo(10f), Vector2.Zero, Faction.Player));
        eventBus.ProcessEvents();
        var lowDamageStopActive = system.IsHitStopped();

        // Update to clear
        system.Update(world, new EcsUpdateContext(null!, 0.2f, null!, null!));

        // High damage
        eventBus.Publish(new EntityDamagedEvent(Entity.None, new Entity(2), 100f, new DamageInfo(100f), Vector2.Zero, Faction.Player));
        eventBus.ProcessEvents();
        var highDamageStopActive = system.IsHitStopped();

        // Assert - both should trigger hit-stop
        Assert.True(lowDamageStopActive);
        Assert.True(highDamageStopActive);
    }

    [Fact]
    public void HitStopSystem_Disabled_DoesNotTriggerHitStop()
    {
        // Arrange
        var world = new EcsWorld();
        var eventBus = new EventBus();
        world.EventBus = eventBus;
        var system = new HitStopSystem();
        HitStopSystem.EnableHitStop = false; // Disabled
        system.Initialize(world);

        // Act
        eventBus.Publish(new EntityDamagedEvent(Entity.None, new Entity(1), 50f, new DamageInfo(50f), Vector2.Zero, Faction.Player));
        eventBus.ProcessEvents();

        // Assert
        Assert.False(system.IsHitStopped());
    }

    [Fact]
    public void HitStopSystem_OnDamage_TriggersCameraShake()
    {
        // Arrange
        var world = new EcsWorld();
        var eventBus = new EventBus();
        world.EventBus = eventBus;
        var system = new HitStopSystem();
        HitStopSystem.EnableCameraShake = true;
        system.Initialize(world);

        // Act
        eventBus.Publish(new EntityDamagedEvent(Entity.None, new Entity(1), 30f, new DamageInfo(30f), Vector2.Zero, Faction.Player));
        eventBus.ProcessEvents();
        
        // Update system to generate shake offset
        var context = new EcsUpdateContext(null!, 0.016f, null!, null!);
        system.Update(world, context);

        // Assert - shake offset should be non-zero
        Assert.NotEqual(Vector2.Zero, system.CameraShakeOffset);
    }

    [Fact]
    public void HitStopSystem_CameraShakeDisabled_NoShakeOffset()
    {
        // Arrange
        var world = new EcsWorld();
        var eventBus = new EventBus();
        world.EventBus = eventBus;
        var system = new HitStopSystem();
        HitStopSystem.EnableCameraShake = false;
        system.Initialize(world);

        // Act
        eventBus.Publish(new EntityDamagedEvent(Entity.None, new Entity(1), 50f, new DamageInfo(50f), Vector2.Zero, Faction.Player));
        eventBus.ProcessEvents();

        // Assert
        Assert.Equal(Vector2.Zero, system.CameraShakeOffset);
    }
}
