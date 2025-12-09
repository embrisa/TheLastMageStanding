using Microsoft.Xna.Framework;
using TheLastMageStanding.Game.Core.Camera;
using TheLastMageStanding.Game.Core.Ecs;
using TheLastMageStanding.Game.Core.Ecs.Components;
using TheLastMageStanding.Game.Core.Ecs.Systems;
using TheLastMageStanding.Game.Core.Events;
using TheLastMageStanding.Game.Core.Input;
using Xunit;

namespace TheLastMageStanding.Game.Tests.AnimationEvents;

public class AnimationEventSystemTests
{
    private static EcsUpdateContext CreateTestContext()
    {
        var gameTime = new GameTime();
        var input = new InputState();
        var camera = new Camera2D(800, 600);
        return new EcsUpdateContext(gameTime, 0.016f, input, camera, Vector2.Zero);
    }
    [Fact]
    public void AnimationEvent_HitboxEnable_SpawnsHitbox()
    {
        // Arrange
        var world = new EcsWorld();
        var eventBus = new EventBus();
        world.EventBus = eventBus;
        var system = new AnimationEventSystem();
        system.Initialize(world);

        var player = CreateTestPlayer(world, new Vector2(100, 100));

        // Set animation state to trigger events (using a fake attack animation)
        world.SetComponent(player, new PlayerAnimationState
        {
            ActiveClip = (PlayerAnimationClip)99, // Non-standard clip to trigger attack processing
            Timer = 0.06f, // Just after hitbox enable event at 0.05s
            Facing = PlayerFacingDirection.East,
        });

        // Act
        system.Update(world, CreateTestContext());

        // Assert - hitbox entity should be created
        var hitboxCount = 0;
        world.ForEach<AttackHitbox>((Entity _, ref AttackHitbox _) => hitboxCount++);
        Assert.Equal(1, hitboxCount);
    }

    [Fact]
    public void AnimationEvent_HitboxDisable_RemovesHitbox()
    {
        // Arrange
        var world = new EcsWorld();
        var eventBus = new EventBus();
        world.EventBus = eventBus;
        var system = new AnimationEventSystem();
        system.Initialize(world);

        var player = CreateTestPlayer(world, new Vector2(100, 100));

        // Enable hitbox first
        world.SetComponent(player, new PlayerAnimationState
        {
            ActiveClip = (PlayerAnimationClip)99,
            Timer = 0.06f,
            Facing = PlayerFacingDirection.East,
        });
        system.Update(world, CreateTestContext());

        var hitboxCountAfterEnable = 0;
        world.ForEach<AttackHitbox>((Entity _, ref AttackHitbox _) => hitboxCountAfterEnable++);
        Assert.Equal(1, hitboxCountAfterEnable);

        // Act - advance past disable event
        world.SetComponent(player, new PlayerAnimationState
        {
            ActiveClip = (PlayerAnimationClip)99,
            Timer = 0.16f, // After disable at 0.15s
            Facing = PlayerFacingDirection.East,
        });
        system.Update(world, CreateTestContext());

        // Assert - hitbox should be destroyed
        var hitboxCountAfterDisable = 0;
        world.ForEach<AttackHitbox>((Entity _, ref AttackHitbox _) => hitboxCountAfterDisable++);
        Assert.Equal(0, hitboxCountAfterDisable);
    }

    [Fact]
    public void DirectionalHitboxConfig_EastFacing_AppliesCorrectOffset()
    {
        // Arrange
        var world = new EcsWorld();
        var eventBus = new EventBus();
        world.EventBus = eventBus;
        var system = new AnimationEventSystem();
        system.Initialize(world);

        var player = CreateTestPlayer(world, new Vector2(100, 100));

        // Set east facing
        world.SetComponent(player, new PlayerAnimationState
        {
            ActiveClip = (PlayerAnimationClip)99,
            Timer = 0.06f,
            Facing = PlayerFacingDirection.East,
        });

        // Act
        system.Update(world, CreateTestContext());

        // Assert - hitbox position should be offset to the east
        Entity? hitboxEntity = null;
        world.ForEach<AttackHitbox, Position>((Entity entity, ref AttackHitbox _, ref Position pos) =>
        {
            hitboxEntity = entity;
            // East offset should be positive X
            Assert.True(pos.Value.X > 100f, $"Expected X > 100, got {pos.Value.X}");
            Assert.Equal(100f, pos.Value.Y, 0.1f);
        });

        Assert.NotNull(hitboxEntity);
    }

    [Fact]
    public void DirectionalHitboxConfig_SouthFacing_AppliesCorrectOffset()
    {
        // Arrange
        var world = new EcsWorld();
        var eventBus = new EventBus();
        world.EventBus = eventBus;
        var system = new AnimationEventSystem();
        system.Initialize(world);

        var player = CreateTestPlayer(world, new Vector2(100, 100));

        // Set south facing
        world.SetComponent(player, new PlayerAnimationState
        {
            ActiveClip = (PlayerAnimationClip)99,
            Timer = 0.06f,
            Facing = PlayerFacingDirection.South,
        });

        // Act
        system.Update(world, CreateTestContext());

        // Assert - hitbox position should be offset to the south
        Entity? hitboxEntity = null;
        world.ForEach<AttackHitbox, Position>((Entity entity, ref AttackHitbox _, ref Position pos) =>
        {
            hitboxEntity = entity;
            Assert.Equal(100f, pos.Value.X, 0.1f);
            // South offset should be positive Y (down in screen coords)
            Assert.True(pos.Value.Y > 100f, $"Expected Y > 100, got {pos.Value.Y}");
        });

        Assert.NotNull(hitboxEntity);
    }

    [Fact]
    public void DirectionalHitboxConfig_NorthFacing_AppliesCorrectOffset()
    {
        // Arrange
        var world = new EcsWorld();
        var eventBus = new EventBus();
        world.EventBus = eventBus;
        var system = new AnimationEventSystem();
        system.Initialize(world);

        var player = CreateTestPlayer(world, new Vector2(100, 100));

        // Set north facing
        world.SetComponent(player, new PlayerAnimationState
        {
            ActiveClip = (PlayerAnimationClip)99,
            Timer = 0.06f,
            Facing = PlayerFacingDirection.North,
        });

        // Act
        system.Update(world, CreateTestContext());

        // Assert - hitbox position should be offset to the north
        Entity? hitboxEntity = null;
        world.ForEach<AttackHitbox, Position>((Entity entity, ref AttackHitbox _, ref Position pos) =>
        {
            hitboxEntity = entity;
            Assert.Equal(100f, pos.Value.X, 0.1f);
            // North offset should be negative Y (up in screen coords)
            Assert.True(pos.Value.Y < 100f, $"Expected Y < 100, got {pos.Value.Y}");
        });

        Assert.NotNull(hitboxEntity);
    }

    [Fact]
    public void AnimationEvent_NonAttackAnimation_DoesNotSpawnHitbox()
    {
        // Arrange
        var world = new EcsWorld();
        var eventBus = new EventBus();
        world.EventBus = eventBus;
        var system = new AnimationEventSystem();
        system.Initialize(world);

        var player = CreateTestPlayer(world, new Vector2(100, 100));

        // Set to idle animation
        world.SetComponent(player, new PlayerAnimationState
        {
            ActiveClip = PlayerAnimationClip.Idle,
            Timer = 0.06f,
            Facing = PlayerFacingDirection.East,
        });

        // Act
        system.Update(world, CreateTestContext());

        // Assert - no hitbox should be created
        var hitboxCount = 0;
        world.ForEach<AttackHitbox>((Entity _, ref AttackHitbox _) => hitboxCount++);
        Assert.Equal(0, hitboxCount);
    }

    [Fact]
    public void AnimationEvent_EventStateTracking_PreventsDuplicateEvents()
    {
        // Arrange
        var world = new EcsWorld();
        var eventBus = new EventBus();
        world.EventBus = eventBus;
        var system = new AnimationEventSystem();
        system.Initialize(world);

        var player = CreateTestPlayer(world, new Vector2(100, 100));

        // Enable hitbox
        world.SetComponent(player, new PlayerAnimationState
        {
            ActiveClip = (PlayerAnimationClip)99,
            Timer = 0.06f,
            Facing = PlayerFacingDirection.East,
        });
        system.Update(world, CreateTestContext());

        var hitboxCountFirst = 0;
        world.ForEach<AttackHitbox>((Entity _, ref AttackHitbox _) => hitboxCountFirst++);

        // Act - update again without crossing disable threshold
        world.SetComponent(player, new PlayerAnimationState
        {
            ActiveClip = (PlayerAnimationClip)99,
            Timer = 0.08f, // Still in active window
            Facing = PlayerFacingDirection.East,
        });
        system.Update(world, CreateTestContext());

        // Assert - should still have exactly one hitbox (no duplicates)
        var hitboxCountSecond = 0;
        world.ForEach<AttackHitbox>((Entity _, ref AttackHitbox _) => hitboxCountSecond++);
        Assert.Equal(hitboxCountFirst, hitboxCountSecond);
        Assert.Equal(1, hitboxCountSecond);
    }

    [Fact]
    public void AnimationEventTrack_GetEventsInRange_ReturnsCorrectEvents()
    {
        // Arrange
        var events = new System.Collections.Generic.List<AnimationEvent>
        {
            new AnimationEvent(AnimationEventType.HitboxEnable, 0.05f),
            new AnimationEvent(AnimationEventType.HitboxDisable, 0.15f),
            new AnimationEvent(AnimationEventType.VfxTrigger, 0.10f, "swing_vfx"),
        };
        var track = new AnimationEventTrack("TestAnimation", events);

        // Act - query events between 0.04 and 0.12
        var foundEvents = new System.Collections.Generic.List<AnimationEvent>();
        foreach (var evt in track.GetEventsInRange(0.04f, 0.12f))
        {
            foundEvents.Add(evt);
        }

        // Assert - should find enable and vfx events, but not disable
        Assert.Equal(2, foundEvents.Count);
        Assert.Contains(foundEvents, e => e.Type == AnimationEventType.HitboxEnable);
        Assert.Contains(foundEvents, e => e.Type == AnimationEventType.VfxTrigger);
        Assert.DoesNotContain(foundEvents, e => e.Type == AnimationEventType.HitboxDisable);
    }

    [Fact]
    public void DirectionalHitboxConfig_CreateDefault_GeneratesAllDirections()
    {
        // Arrange & Act
        var config = DirectionalHitboxConfig.CreateDefault(forwardDistance: 30f);

        // Assert - verify all directions have reasonable offsets
        Assert.NotEqual(Vector2.Zero, config.GetOffsetForFacing(PlayerFacingDirection.North));
        Assert.NotEqual(Vector2.Zero, config.GetOffsetForFacing(PlayerFacingDirection.South));
        Assert.NotEqual(Vector2.Zero, config.GetOffsetForFacing(PlayerFacingDirection.East));
        Assert.NotEqual(Vector2.Zero, config.GetOffsetForFacing(PlayerFacingDirection.West));
        Assert.NotEqual(Vector2.Zero, config.GetOffsetForFacing(PlayerFacingDirection.NorthEast));
        Assert.NotEqual(Vector2.Zero, config.GetOffsetForFacing(PlayerFacingDirection.NorthWest));
        Assert.NotEqual(Vector2.Zero, config.GetOffsetForFacing(PlayerFacingDirection.SouthEast));
        Assert.NotEqual(Vector2.Zero, config.GetOffsetForFacing(PlayerFacingDirection.SouthWest));

        // Verify south points down (positive Y)
        var southOffset = config.GetOffsetForFacing(PlayerFacingDirection.South);
        Assert.True(southOffset.Y > 0, "South should point down (positive Y)");

        // Verify north points up (negative Y)
        var northOffset = config.GetOffsetForFacing(PlayerFacingDirection.North);
        Assert.True(northOffset.Y < 0, "North should point up (negative Y)");

        // Verify east points right (positive X)
        var eastOffset = config.GetOffsetForFacing(PlayerFacingDirection.East);
        Assert.True(eastOffset.X > 0, "East should point right (positive X)");

        // Verify west points left (negative X)
        var westOffset = config.GetOffsetForFacing(PlayerFacingDirection.West);
        Assert.True(westOffset.X < 0, "West should point left (negative X)");
    }

    private static Entity CreateTestPlayer(EcsWorld world, Vector2 position)
    {
        var player = world.CreateEntity();
        world.SetComponent(player, Faction.Player);
        world.SetComponent(player, new Position(position));
        world.SetComponent(player, new AttackStats(25f, 0.5f, 42f));
        world.SetComponent(player, new MeleeAttackConfig(42f, Vector2.Zero, 0.15f));
        world.SetComponent(player, new AnimationDrivenAttack("PlayerMelee"));
        world.SetComponent(player, DirectionalHitboxConfig.CreateDefault(24f));
        world.SetComponent(player, new AnimationEventState(0f, false));
        world.SetComponent(player, new PlayerAnimationState
        {
            Facing = PlayerFacingDirection.East,
            ActiveClip = PlayerAnimationClip.Idle,
            Timer = 0f,
        });
        return player;
    }
}
