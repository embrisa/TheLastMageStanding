using System;
using Microsoft.Xna.Framework;
using TheLastMageStanding.Game.Core.Camera;
using TheLastMageStanding.Game.Core.Ecs;
using TheLastMageStanding.Game.Core.Ecs.Components;
using TheLastMageStanding.Game.Core.Ecs.Config;
using TheLastMageStanding.Game.Core.Ecs.Systems;
using TheLastMageStanding.Game.Core.Input;
using TheLastMageStanding.Game.Core.Events;
using Xunit;

namespace TheLastMageStanding.Game.Tests.Ai;

public class AiRoleSystemTests
{
    private static EcsUpdateContext CreateContext(float deltaSeconds = 0.016f) =>
        new(
            new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(deltaSeconds)),
            deltaSeconds,
            new InputState(),
            new Camera2D(960, 540));

    [Fact]
    public void Charger_CommitsAndTelegraphsWithinRange()
    {
        var world = CreateWorld();
        var system = new AiChargerSystem();
        system.Initialize(world);

        var charger = CreateCharger(world, position: new Vector2(0, 0));
        CreatePlayer(world, new Vector2(80, 0)); // In commit range (60-120)

        TelegraphSystem.ShowTelegraphs = true;
        system.Update(world, CreateContext());

        Assert.True(world.TryGetComponent(charger, out AiBehaviorStateMachine state));
        Assert.Equal(AiBehaviorState.Committing, state.State);

        var telegraphCount = 0;
        world.ForEach<ActiveTelegraph>((Entity _, ref ActiveTelegraph _) => telegraphCount++);
        Assert.Equal(1, telegraphCount);
    }

    [Fact]
    public void Charger_TransitionsToCooldownAfterCommit()
    {
        var world = CreateWorld();
        var system = new AiChargerSystem();
        system.Initialize(world);

        var charger = CreateCharger(world, position: new Vector2(0, 0));
        CreatePlayer(world, new Vector2(70, 0));

        // First update enters committing
        system.Update(world, CreateContext());
        // Second update completes windup
        system.Update(world, CreateContext(0.5f));

        Assert.True(world.TryGetComponent(charger, out AiBehaviorStateMachine state));
        Assert.Equal(AiBehaviorState.Cooldown, state.State);
        Assert.True(state.CooldownTimer > 0f);

        var hitboxCount = 0;
        world.ForEach<AttackHitbox>((Entity _, ref AttackHitbox _) => hitboxCount++);
        Assert.Equal(1, hitboxCount);
    }

    [Fact]
    public void Protector_BlocksProjectileAndConsumesShield()
    {
        var world = CreateWorld();
        var eventBus = (EventBus)world.EventBus;
        var system = new ProjectileHitSystem();
        system.Initialize(world);

        var protector = world.CreateEntity();
        world.SetComponent(protector, Faction.Enemy);
        world.SetComponent(protector, new Health(20, 20));
        world.SetComponent(protector, new Hurtbox());
        world.SetComponent(protector, new ShieldActive(true, 1, 1.5f, protector));

        var playerSource = world.CreateEntity();
        world.SetComponent(playerSource, Faction.Player);

        var projectile = world.CreateEntity();
        world.SetComponent(projectile, new Projectile(playerSource, 10f, Faction.Player));
        world.SetComponent(projectile, new Position(Vector2.Zero));

        eventBus.Publish(new CollisionEnterEvent(projectile, protector, Vector2.Zero, Vector2.Zero));
        eventBus.ProcessEvents();
        system.Update(world, CreateContext());

        Assert.False(world.TryGetComponent(protector, out ShieldActive _));

        Assert.True(world.TryGetComponent(projectile, out Projectile proj));
        Assert.True(proj.HasHit);
    }

    [Fact]
    public void Buffer_AppliesBuffToAlliesInRange()
    {
        var world = CreateWorld();
        var bufferSystem = new AiBufferSystem();
        bufferSystem.Initialize(world);

        var modifiers = StatModifiers.Zero;
        modifiers.MoveSpeedMultiplicative = 1.3f;
        var role = new AiRoleConfig(
            Role: EnemyRole.Buffer,
            BuffRange: 100f,
            BuffDuration: 4f,
            CooldownDuration: 6f,
            BuffType: BuffType.MoveSpeedBuff,
            BuffModifiers: modifiers,
            BuffAnimationLock: 0.05f);

        var buffer = world.CreateEntity();
        world.SetComponent(buffer, Faction.Enemy);
        world.SetComponent(buffer, new Position(new Vector2(0, 0)));
        world.SetComponent(buffer, new Health(10, 10));
        world.SetComponent(buffer, role);
        world.SetComponent(buffer, new AiBehaviorStateMachine { State = AiBehaviorState.Seeking });

        var ally = world.CreateEntity();
        world.SetComponent(ally, Faction.Enemy);
        world.SetComponent(ally, new Position(new Vector2(10, 0)));
        world.SetComponent(ally, new Health(10, 10));
        world.SetComponent(ally, new MoveSpeed(80f));
        world.SetComponent(ally, new ComputedStats { IsDirty = true });
        world.SetComponent(ally, new OffensiveStats());
        world.SetComponent(ally, new DefensiveStats());

        bufferSystem.Update(world, CreateContext());

        Assert.True(world.TryGetComponent(ally, out ActiveBuffs buffs));
        Assert.NotNull(buffs.Buffs);
        Assert.Single(buffs.Buffs);
        Assert.Equal(BuffType.MoveSpeedBuff, buffs.Buffs[0].Type);
    }

    [Fact]
    public void BuffTickSystem_ExpiresBuffs()
    {
        var world = CreateWorld();
        var tickSystem = new BuffTickSystem();
        tickSystem.Initialize(world);

        var entity = world.CreateEntity();
        world.SetComponent(entity, Faction.Enemy);
        world.SetComponent(entity, new ActiveBuffs
        {
            Buffs =
            [
                new TimedBuff
                {
                    Type = BuffType.MoveSpeedBuff,
                    Duration = 1f,
                    RemainingDuration = 1f,
                    Modifiers = StatModifiers.Zero
                }
            ]
        });

        tickSystem.Update(world, CreateContext(1.1f));

        Assert.False(world.TryGetComponent(entity, out ActiveBuffs _));
    }

    [Fact]
    public void EnemyWaveConfig_IncludesRoleArchetypes()
    {
        var config = EnemyWaveConfig.Default;

        Assert.Contains(config.EnemyProfiles, p => p.Archetype.Id == "charger_hexer" && p.UnlockWave == 4);
        Assert.Contains(config.EnemyProfiles, p => p.Archetype.Id == "protector_hexer" && p.UnlockWave == 6);
        Assert.Contains(config.EnemyProfiles, p => p.Archetype.Id == "buffer_hexer" && p.UnlockWave == 6);
    }

    private static EcsWorld CreateWorld()
    {
        var world = new EcsWorld
        {
            EventBus = new EventBus()
        };

        return world;
    }

    private static Entity CreateCharger(EcsWorld world, Vector2 position)
    {
        var telegraph = new TelegraphData(0.4f, new Color(255, 50, 50, 180), 46f, Vector2.Zero);
        var roleConfig = new AiRoleConfig(
            Role: EnemyRole.Charger,
            CommitRangeMin: 60f,
            CommitRangeMax: 120f,
            WindupDuration: 0.4f,
            CooldownDuration: 3.5f,
            KnockbackForce: 400f,
            Telegraph: telegraph);

        var entity = world.CreateEntity();
        world.SetComponent(entity, Faction.Enemy);
        world.SetComponent(entity, new Position(position));
        world.SetComponent(entity, new MoveSpeed(110f));
        world.SetComponent(entity, new Health(30, 30));
        world.SetComponent(entity, new AttackStats(10f, 1.2f, 7f));
        world.SetComponent(entity, roleConfig);
        world.SetComponent(entity, new AiBehaviorStateMachine { State = AiBehaviorState.Seeking });
        return entity;
    }

    private static void CreatePlayer(EcsWorld world, Vector2 position)
    {
        var player = world.CreateEntity();
        world.SetComponent(player, Faction.Player);
        world.SetComponent(player, new Position(position));
        world.SetComponent(player, new Health(100, 100));
        world.SetComponent(player, new Hurtbox());
    }
}

