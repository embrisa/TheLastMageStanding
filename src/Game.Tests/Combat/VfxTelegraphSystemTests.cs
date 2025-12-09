using Microsoft.Xna.Framework;
using TheLastMageStanding.Game.Core.Ecs;
using TheLastMageStanding.Game.Core.Ecs.Components;
using TheLastMageStanding.Game.Core.Ecs.Systems;
using TheLastMageStanding.Game.Core.Events;
using Xunit;

namespace TheLastMageStanding.Game.Tests.Combat;

public class VfxSystemTests
{
    [Fact]
    public void VfxSystem_OnVfxSpawnEvent_CreatesVfxEntity()
    {
        // Arrange
        var world = new EcsWorld();
        var eventBus = new EventBus();
        world.EventBus = eventBus;
        var system = new VfxSystem();
        VfxSystem.EnableVfx = true;
        system.Initialize(world);

        // Act
        eventBus.Publish(new VfxSpawnEvent("test_vfx", new Vector2(100, 100), VfxType.Impact));
        eventBus.ProcessEvents();

        // Assert - VFX entity should exist (even though asset is missing, it logs and creates entity)
        var vfxCount = 0;
        world.ForEach<ActiveVfx>((Entity _, ref ActiveVfx _) => vfxCount++);
        Assert.Equal(1, vfxCount);
    }

    [Fact]
    public void VfxSystem_Update_DecrementsVfxLifetime()
    {
        // Arrange
        var world = new EcsWorld();
        var eventBus = new EventBus();
        world.EventBus = eventBus;
        var system = new VfxSystem();
        VfxSystem.EnableVfx = true;
        system.Initialize(world);

        eventBus.Publish(new VfxSpawnEvent("test_vfx", Vector2.Zero, VfxType.Impact));
        eventBus.ProcessEvents();

        // Act - update with large delta to expire VFX
        var context = new EcsUpdateContext(null!, 1.0f, null!, null!, Vector2.Zero);
        system.Update(world, context);

        // Assert - VFX should be removed
        var vfxCount = 0;
        world.ForEach<ActiveVfx>((Entity _, ref ActiveVfx _) => vfxCount++);
        Assert.Equal(0, vfxCount);
    }

    [Fact]
    public void VfxSystem_Disabled_DoesNotCreateVfx()
    {
        // Arrange
        var world = new EcsWorld();
        var eventBus = new EventBus();
        world.EventBus = eventBus;
        var system = new VfxSystem();
        VfxSystem.EnableVfx = false;
        system.Initialize(world);

        // Act
        eventBus.Publish(new VfxSpawnEvent("test_vfx", Vector2.Zero, VfxType.Impact));
        eventBus.ProcessEvents();

        // Assert
        var vfxCount = 0;
        world.ForEach<ActiveVfx>((Entity _, ref ActiveVfx _) => vfxCount++);
        Assert.Equal(0, vfxCount);
    }
}

public class TelegraphSystemTests
{
    [Fact]
    public void TelegraphSystem_SpawnTelegraph_CreatesEntity()
    {
        // Arrange
        var world = new EcsWorld();
        var system = new TelegraphSystem();
        TelegraphSystem.ShowTelegraphs = true;
        system.Initialize(world);

        var telegraphData = TelegraphData.Default();

        // Act
        TelegraphSystem.SpawnTelegraph(world, new Vector2(100, 100), telegraphData);

        // Assert
        var count = 0;
        world.ForEach<ActiveTelegraph>((Entity _, ref ActiveTelegraph _) => count++);
        Assert.Equal(1, count);
    }

    [Fact]
    public void TelegraphSystem_Update_DecrementsLifetime()
    {
        // Arrange
        var world = new EcsWorld();
        var system = new TelegraphSystem();
        TelegraphSystem.ShowTelegraphs = true;
        system.Initialize(world);

        var telegraphData = new TelegraphData(0.1f, Color.Red, 20f, Vector2.Zero);
        TelegraphSystem.SpawnTelegraph(world, Vector2.Zero, telegraphData);

        // Act - update to expire telegraph
        var context = new EcsUpdateContext(null!, 0.2f, null!, null!, Vector2.Zero);
        system.Update(world, context);

        // Assert - telegraph should be removed
        var count = 0;
        world.ForEach<ActiveTelegraph>((Entity _, ref ActiveTelegraph _) => count++);
        Assert.Equal(0, count);
    }

    [Fact]
    public void TelegraphSystem_Disabled_DoesNotCreateTelegraph()
    {
        // Arrange
        var world = new EcsWorld();
        TelegraphSystem.ShowTelegraphs = false;

        // Act
        TelegraphSystem.SpawnTelegraph(world, Vector2.Zero, TelegraphData.Default());

        // Assert
        var count = 0;
        world.ForEach<ActiveTelegraph>((Entity _, ref ActiveTelegraph _) => count++);
        Assert.Equal(0, count);
    }
}
