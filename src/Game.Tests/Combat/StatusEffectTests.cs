using Microsoft.Xna.Framework;
using TheLastMageStanding.Game.Core.Combat;
using TheLastMageStanding.Game.Core.Config;
using TheLastMageStanding.Game.Core.Ecs;
using TheLastMageStanding.Game.Core.Ecs.Components;
using TheLastMageStanding.Game.Core.Ecs.Systems;
using TheLastMageStanding.Game.Core.Events;
using Xunit;

namespace TheLastMageStanding.Game.Tests.Combat;

public class StatusEffectTests
{
    private static (EcsWorld world, EventBus bus, StatusEffectTickSystem tickSystem, StatRecalculationSystem statSystem) CreateWorldWithSystems()
    {
        var world = new EcsWorld();
        var bus = new EventBus();
        world.EventBus = bus;

        var application = new StatusEffectApplicationSystem();
        var tick = new StatusEffectTickSystem();
        var hitReaction = new HitReactionSystem();
        var statSystem = new StatRecalculationSystem();

        application.Initialize(world);
        tick.Initialize(world);
        hitReaction.Initialize(world);
        statSystem.Initialize(world);

        return (world, bus, tick, statSystem);
    }

    private static Entity CreateAttacker(EcsWorld world, Faction faction = Faction.Player)
    {
        var entity = world.CreateEntity();
        world.SetComponent(entity, faction);
        world.SetComponent(entity, new Position(Vector2.Zero));
        world.SetComponent(entity, CreateComputed());
        world.SetComponent(entity, OffensiveStats.Default);
        return entity;
    }

    private static Entity CreateTarget(EcsWorld world, float health = 100f, float fireResist = 0f, float frostResist = 0f)
    {
        var entity = world.CreateEntity();
        world.SetComponent(entity, Faction.Enemy);
        world.SetComponent(entity, new Position(Vector2.Zero));
        world.SetComponent(entity, new Health(health, health));
        world.SetComponent(entity, new Hurtbox { IsInvulnerable = false, InvulnerabilityEndsAt = 0f });
        world.SetComponent(entity, new MoveSpeed(200f));
        world.SetComponent(entity, new BaseMoveSpeed(200f));
        world.SetComponent(entity, OffensiveStats.Default);
        world.SetComponent(entity, new DefensiveStats
        {
            Armor = 0f,
            ArcaneResist = 0f,
            FireResist = fireResist,
            FrostResist = frostResist,
            NatureResist = 0f
        });
        world.SetComponent(entity, CreateComputed(fireResist: fireResist, frostResist: frostResist));
        return entity;
    }

    private static ComputedStats CreateComputed(
        float fireResist = 0f,
        float frostResist = 0f,
        float arcaneResist = 0f,
        float natureResist = 0f,
        float moveSpeed = 200f)
    {
        return new ComputedStats
        {
            EffectivePower = 1f,
            EffectiveAttackSpeed = 1f,
            EffectiveCritChance = 0f,
            EffectiveCritMultiplier = 1.5f,
            EffectiveCooldownReduction = 0f,
            EffectiveArmor = 0f,
            EffectiveArcaneResist = arcaneResist,
            EffectiveFireResist = fireResist,
            EffectiveFrostResist = frostResist,
            EffectiveNatureResist = natureResist,
            EffectiveMoveSpeed = moveSpeed,
            IsDirty = false
        };
    }

    [Fact]
    public void Burn_AppliesDamageOverTime()
    {
        var (world, bus, tickSystem, _) = CreateWorldWithSystems();
        var attacker = CreateAttacker(world);
        var target = CreateTarget(world);
        var damageService = new DamageApplicationService(world, new DamageCalculator(new CombatRng(123)));

        var burn = StatusEffectConfig.CreateBurn();
        var damageInfo = new DamageInfo(0f, DamageType.Physical, DamageFlags.None, DamageSource.Melee, burn);

        damageService.ApplyDamage(attacker, target, damageInfo, Vector2.Zero);
        bus.ProcessEvents();

        tickSystem.Update(world, new EcsUpdateContext(null!, 1.0f, null!, null!));
        bus.ProcessEvents();

        world.TryGetComponent(target, out Health health);
        Assert.InRange(health.Current, 94.5f, 95.5f);
    }

    [Fact]
    public void Burn_StacksAdditivelyUpToMax()
    {
        var (world, bus, tickSystem, _) = CreateWorldWithSystems();
        var attacker = CreateAttacker(world);
        var target = CreateTarget(world);
        var damageService = new DamageApplicationService(world, new DamageCalculator(new CombatRng(42)));
        var burn = StatusEffectConfig.CreateBurn();
        var damageInfo = new DamageInfo(0f, DamageType.Physical, DamageFlags.None, DamageSource.Melee, burn);

        damageService.ApplyDamage(attacker, target, damageInfo, Vector2.Zero);
        bus.ProcessEvents();
        damageService.ApplyDamage(attacker, target, damageInfo, Vector2.Zero);
        bus.ProcessEvents();
        damageService.ApplyDamage(attacker, target, damageInfo, Vector2.Zero);
        bus.ProcessEvents();

        tickSystem.Update(world, new EcsUpdateContext(null!, 1.0f, null!, null!));
        bus.ProcessEvents();

        world.TryGetComponent(target, out Health health);
        Assert.InRange(health.Current, 84.5f, 85.5f); // 3 stacks -> ~15 dmg
    }

    [Fact]
    public void Slow_ReducesMoveSpeed()
    {
        var (world, bus, tickSystem, statSystem) = CreateWorldWithSystems();
        var attacker = CreateAttacker(world);
        var target = CreateTarget(world);
        var damageService = new DamageApplicationService(world, new DamageCalculator(new CombatRng(11)));
        var slow = StatusEffectConfig.CreateSlow(potency: 0.5f, duration: 2f);
        var damageInfo = new DamageInfo(0f, DamageType.Physical, DamageFlags.None, DamageSource.Melee, slow);

        damageService.ApplyDamage(attacker, target, damageInfo, Vector2.Zero);
        bus.ProcessEvents();

        tickSystem.Update(world, new EcsUpdateContext(null!, 0.1f, null!, null!));
        bus.ProcessEvents();
        statSystem.Update(world, new EcsUpdateContext(null!, 0.1f, null!, null!));

        world.TryGetComponent(target, out MoveSpeed moveSpeed);
        Assert.InRange(moveSpeed.Value, 99f, 101f); // 50% slow on 200 base
    }

    [Fact]
    public void Resistance_ReducesDurationAndPotency()
    {
        var (world, bus, tickSystem, _) = CreateWorldWithSystems();
        var attacker = CreateAttacker(world);
        var target = CreateTarget(world, fireResist: 50f);
        var damageService = new DamageApplicationService(world, new DamageCalculator(new CombatRng(7)));
        var burn = StatusEffectConfig.CreateBurn();
        var damageInfo = new DamageInfo(0f, DamageType.Physical, DamageFlags.None, DamageSource.Melee, burn);

        damageService.ApplyDamage(attacker, target, damageInfo, Vector2.Zero);
        bus.ProcessEvents();

        Assert.True(world.TryGetComponent(target, out ActiveStatusEffects effects));
        var effect = effects.Effects[0];
        Assert.InRange(effect.Data.Duration, 1.9f, 2.1f); // reduced from 3s
        Assert.InRange(effect.Data.Potency, 3.2f, 3.5f); // reduced from 5/sec
    }

    [Fact]
    public void Immunity_BlocksApplication()
    {
        var (world, bus, tickSystem, _) = CreateWorldWithSystems();
        var attacker = CreateAttacker(world);
        var target = CreateTarget(world);
        world.SetComponent(target, new StatusEffectImmunities { Flags = StatusEffectImmunity.Burn });

        var damageService = new DamageApplicationService(world, new DamageCalculator(new CombatRng(9)));
        var burn = StatusEffectConfig.CreateBurn();
        var damageInfo = new DamageInfo(0f, DamageType.Physical, DamageFlags.None, DamageSource.Melee, burn);

        damageService.ApplyDamage(attacker, target, damageInfo, Vector2.Zero);
        bus.ProcessEvents();

        Assert.False(world.TryGetComponent(target, out ActiveStatusEffects _));
    }

    [Fact]
    public void Poison_RampsWithStacks()
    {
        var (world, bus, tickSystem, _) = CreateWorldWithSystems();
        var attacker = CreateAttacker(world);
        var target = CreateTarget(world);
        var damageService = new DamageApplicationService(world, new DamageCalculator(new CombatRng(21)));
        var poison = StatusEffectConfig.CreatePoison();
        var damageInfo = new DamageInfo(0f, DamageType.Physical, DamageFlags.None, DamageSource.Melee, poison);

        // Apply three stacks
        for (int i = 0; i < 3; i++)
        {
            damageService.ApplyDamage(attacker, target, damageInfo, Vector2.Zero);
            bus.ProcessEvents();
        }

        tickSystem.Update(world, new EcsUpdateContext(null!, 1.0f, null!, null!));
        bus.ProcessEvents();

        world.TryGetComponent(target, out Health health);
        Assert.InRange(health.Current, 95.5f, 96.5f); // ~4.2 damage from ramped poison
    }
}

