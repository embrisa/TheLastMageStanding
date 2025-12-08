using System.Collections.Generic;
using Microsoft.Xna.Framework;
using TheLastMageStanding.Game.Core.Camera;
using TheLastMageStanding.Game.Core.Combat;
using TheLastMageStanding.Game.Core.Ecs;
using TheLastMageStanding.Game.Core.Ecs.Components;
using TheLastMageStanding.Game.Core.Ecs.Config;
using TheLastMageStanding.Game.Core.Ecs.Systems;
using TheLastMageStanding.Game.Core.Events;
using TheLastMageStanding.Game.Core.Input;
using Xunit;

namespace TheLastMageStanding.Game.Tests.Modifiers;

public class EliteModifierTests
{
    [Fact]
    public void Factory_AttachesModifiersAndRewardMultiplier()
    {
        var world = new EcsWorld { EventBus = new EventBus() };
        var factory = new EnemyEntityFactory(world);
        var modifiers = new List<EliteModifierType>
        {
            EliteModifierType.ExtraProjectiles,
            EliteModifierType.Vampiric
        };

        var entity = factory.CreateEnemy(Vector2.Zero, EnemyWaveConfig.CreateEliteForDebug(), modifiers);

        Assert.True(world.TryGetComponent(entity, out EliteModifierData data));
        Assert.Equal(2, data.Count);
        Assert.True(world.TryGetComponent(entity, out LootDropper dropper));
        Assert.True(dropper.ModifierRewardMultiplier > 1f);
    }

    [Fact]
    public void Factory_DeduplicatesNonStackingModifiers()
    {
        var world = new EcsWorld { EventBus = new EventBus() };
        var factory = new EnemyEntityFactory(world);
        var modifiers = new List<EliteModifierType>
        {
            EliteModifierType.ExplosiveDeath,
            EliteModifierType.ExplosiveDeath
        };

        var entity = factory.CreateEnemy(Vector2.Zero, EnemyWaveConfig.CreateEliteForDebug(), modifiers);

        Assert.True(world.TryGetComponent(entity, out EliteModifierData data));
        Assert.Equal(1, data.Count);
    }

    [Fact]
    public void EliteModifierSystem_VampiricHealsAttacker()
    {
        var eventBus = new EventBus();
        var world = new EcsWorld { EventBus = eventBus };
        var system = new EliteModifierSystem();
        system.Initialize(world);

        var attacker = world.CreateEntity();
        world.SetComponent(attacker, new Health(50f, 100f));
        world.SetComponent(attacker, Faction.Enemy);
        world.SetComponent(attacker, new EliteModifierData(new List<EliteModifierType> { EliteModifierType.Vampiric }));

        var target = world.CreateEntity();
        world.SetComponent(target, new Health(50f, 50f));
        world.SetComponent(target, Faction.Player);

        var damageInfo = new DamageInfo(20f, DamageType.Physical, DamageFlags.CanCrit, DamageSource.Melee);
        world.EventBus.Publish(new EntityDamagedEvent(attacker, target, 20f, damageInfo, Vector2.Zero, Faction.Enemy));

        eventBus.ProcessEvents();

        Assert.True(world.TryGetComponent(attacker, out Health healed));
        Assert.True(healed.Current > 50f);
    }

    [Fact]
    public void EliteModifierSystem_SpawnsPendingExplosionOnDeath()
    {
        var eventBus = new EventBus();
        var world = new EcsWorld { EventBus = eventBus };
        var system = new EliteModifierSystem();
        system.Initialize(world);

        var modifiers = new List<EliteModifierType> { EliteModifierType.ExplosiveDeath };
        world.EventBus.Publish(new EnemyDiedEvent(new Entity(999), Vector2.Zero, modifiers));
        eventBus.ProcessEvents();

        var found = false;
        world.ForEach<PendingExplosion>((Entity _, ref PendingExplosion __) => { found = true; });

        Assert.True(found);
    }

    [Fact]
    public void EliteModifierSystem_ShieldRegeneratesAfterCooldown()
    {
        var world = new EcsWorld { EventBus = new EventBus() };
        var system = new EliteModifierSystem();
        system.Initialize(world);

        var entity = world.CreateEntity();
        world.SetComponent(entity, new Health(50f, 50f));
        world.SetComponent(
            entity,
            new EliteShield
            {
                Current = 10f,
                Max = 20f,
                RegenCooldown = 1f,
                RegenRate = 5f,
                CooldownTimer = 1f
            });

        var input = new InputState();
        input.SetTestState(Vector2.Zero);
        var camera = new Camera2D(800, 600);

        system.Update(world, new EcsUpdateContext(new GameTime(), 0.5f, input, camera));
        Assert.True(world.TryGetComponent(entity, out EliteShield shieldAfterHalfSecond));
        Assert.Equal(10f, shieldAfterHalfSecond.Current); // still on cooldown

        system.Update(world, new EcsUpdateContext(new GameTime(), 1.0f, input, camera));
        Assert.True(world.TryGetComponent(entity, out EliteShield shieldAfterRegen));
        Assert.True(shieldAfterRegen.Current > 10f);
    }
}

