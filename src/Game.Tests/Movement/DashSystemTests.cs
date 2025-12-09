using Microsoft.Xna.Framework;
using TheLastMageStanding.Game.Core.Camera;
using TheLastMageStanding.Game.Core.Ecs;
using TheLastMageStanding.Game.Core.Ecs.Components;
using TheLastMageStanding.Game.Core.Ecs.Systems;
using TheLastMageStanding.Game.Core.Events;
using TheLastMageStanding.Game.Core.Input;
using Xunit;

namespace TheLastMageStanding.Game.Tests.Movement;

public class DashSystemTests
{
    [Fact]
    public void DashExecution_SetsStateAndInvulnerability()
    {
        var world = new EcsWorld();
        var eventBus = new EventBus();
        world.EventBus = eventBus;
        var hitStopSystem = new HitStopSystem();
        hitStopSystem.Initialize(world);
        var system = new DashExecutionSystem(hitStopSystem);
        system.Initialize(world);

        var player = world.CreateEntity();
        world.SetComponent(player, new Position(Vector2.Zero));
        world.SetComponent(player, new Velocity(Vector2.Zero));
        world.SetComponent(player, new DashConfig
        {
            Distance = DashConfig.DefaultDistance,
            Duration = DashConfig.DefaultDuration,
            Cooldown = DashConfig.DefaultCooldown,
            IFrameWindow = DashConfig.DefaultIFrameWindow,
            InputBufferWindow = DashConfig.DefaultInputBufferWindow
        });
        world.SetComponent(player, new DashCooldown(0f));

        eventBus.Publish(new DashRequestEvent(player, Vector2.UnitX));
        eventBus.ProcessEvents();

        Assert.True(world.TryGetComponent(player, out DashState dashState) && dashState.IsActive);
        Assert.True(world.TryGetComponent(player, out Velocity velocity));
        Assert.Equal(DashConfig.DefaultDistance / DashConfig.DefaultDuration, velocity.Value.Length(), 3);
        Assert.True(world.TryGetComponent(player, out Invulnerable _));
        Assert.True(world.TryGetComponent(player, out DashCooldown cooldown));
        Assert.Equal(DashConfig.DefaultCooldown, cooldown.RemainingSeconds, 3);
    }

    [Fact]
    public void DashExecution_RespectsCooldown()
    {
        var world = new EcsWorld();
        var eventBus = new EventBus();
        world.EventBus = eventBus;
        var hitStopSystem = new HitStopSystem();
        hitStopSystem.Initialize(world);
        var system = new DashExecutionSystem(hitStopSystem);
        system.Initialize(world);

        var player = world.CreateEntity();
        world.SetComponent(player, new Position(Vector2.Zero));
        world.SetComponent(player, new DashConfig
        {
            Distance = DashConfig.DefaultDistance,
            Duration = DashConfig.DefaultDuration,
            Cooldown = DashConfig.DefaultCooldown,
            IFrameWindow = DashConfig.DefaultIFrameWindow,
            InputBufferWindow = DashConfig.DefaultInputBufferWindow
        });
        world.SetComponent(player, new DashCooldown(1f));

        eventBus.Publish(new DashRequestEvent(player, Vector2.UnitX));
        eventBus.ProcessEvents();

        Assert.False(world.TryGetComponent(player, out DashState _));
    }

    [Fact]
    public void DashMovement_RemovesStateAfterDuration()
    {
        var world = new EcsWorld();
        var eventBus = new EventBus();
        world.EventBus = eventBus;
        var system = new DashMovementSystem();
        system.Initialize(world);

        var player = world.CreateEntity();
        world.SetComponent(player, new Position(Vector2.Zero));
        world.SetComponent(player, new DashConfig
        {
            Distance = 150f,
            Duration = 0.2f,
            Cooldown = 2f,
            IFrameWindow = 0.15f,
            InputBufferWindow = 0.05f
        });
        world.SetComponent(player, new DashState
        {
            IsActive = true,
            Direction = Vector2.UnitX,
            Elapsed = 0f,
            IFrameActive = true
        });
        world.SetComponent(player, new Velocity(Vector2.Zero));
        world.SetComponent(player, new Invulnerable());

        var input = new InputState();
        var context = new EcsUpdateContext(new GameTime(), 0.25f, input, new Camera2D(800, 600), Vector2.Zero);
        system.Update(world, context);

        Assert.False(world.TryGetComponent(player, out DashState _));
        Assert.True(world.TryGetComponent(player, out Velocity velocity));
        Assert.Equal(Vector2.Zero, velocity.Value);
        Assert.False(world.TryGetComponent(player, out Invulnerable _));
    }

    [Fact]
    public void DashMovement_DropsIFramesAfterWindow()
    {
        var world = new EcsWorld();
        var system = new DashMovementSystem();
        system.Initialize(world);

        var player = world.CreateEntity();
        world.SetComponent(player, new Position(Vector2.Zero));
        world.SetComponent(player, new DashConfig
        {
            Distance = 120f,
            Duration = 0.2f,
            Cooldown = 1f,
            IFrameWindow = 0.05f,
            InputBufferWindow = 0.05f
        });
        world.SetComponent(player, new DashState
        {
            IsActive = true,
            Direction = Vector2.UnitY,
            Elapsed = 0f,
            IFrameActive = true
        });
        world.SetComponent(player, new Velocity(Vector2.Zero));
        world.SetComponent(player, new Invulnerable());

        var input = new InputState();
        var context = new EcsUpdateContext(new GameTime(), 0.06f, input, new Camera2D(800, 600), Vector2.Zero);
        system.Update(world, context);

        Assert.True(world.TryGetComponent(player, out DashState dashState));
        Assert.False(dashState.IFrameActive);
        Assert.False(world.TryGetComponent(player, out Invulnerable _));
    }

    [Fact]
    public void DashInput_BuffersAndConsumes()
    {
        var world = new EcsWorld();
        var eventBus = new EventBus();
        world.EventBus = eventBus;

        var session = world.CreateEntity();
        world.SetComponent(session, new GameSession());

        var player = world.CreateEntity();
        world.SetComponent(player, new PlayerTag());
        world.SetComponent(player, new DashConfig
        {
            Distance = 150f,
            Duration = 0.2f,
            Cooldown = 0.2f,
            IFrameWindow = 0.15f,
            InputBufferWindow = 0.05f
        });
        world.SetComponent(player, new DashCooldown(0.1f));
        world.SetComponent(player, new DashInputBuffer());
        world.SetComponent(player, new InputIntent { Movement = new Vector2(1f, 0f) });

        var dashInputSystem = new DashInputSystem();
        dashInputSystem.Initialize(world);
        var dashExecutionSystem = new DashExecutionSystem(new HitStopSystem());
        dashExecutionSystem.Initialize(world);

        var dashRequests = 0;
        eventBus.Subscribe<DashRequestEvent>(_ => dashRequests++);

        var input = new InputState();
        input.SetTestState(Vector2.Zero, dashPressed: true);
        var context = new EcsUpdateContext(new GameTime(), 0.016f, input, new Camera2D(800, 600), Vector2.Zero);

        dashInputSystem.Update(world, context);
        eventBus.ProcessEvents();

        Assert.True(world.TryGetComponent(player, out DashInputBuffer buffer) && buffer.HasBufferedInput);
        Assert.Equal(0, dashRequests);

        world.SetComponent(player, new DashCooldown(0f));
        input.SetTestState(Vector2.Zero, dashPressed: false);

        dashInputSystem.Update(world, context);
        eventBus.ProcessEvents();

        Assert.Equal(1, dashRequests);
        Assert.True(world.TryGetComponent(player, out DashInputBuffer clearedBuffer));
        Assert.False(clearedBuffer.HasBufferedInput);
    }
}

