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
            FrostResist = frostResist
        });
        world.SetComponent(entity, CreateComputed(fireResist: fireResist, frostResist: frostResist));
        return entity;
    }

    private static ComputedStats CreateComputed(
        float fireResist = 0f,
        float frostResist = 0f,
        float arcaneResist = 0f,
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

        tickSystem.Update(world, new EcsUpdateContext(null!, 1.0f, null!, null!, Vector2.Zero));
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

        tickSystem.Update(world, new EcsUpdateContext(null!, 1.0f, null!, null!, Vector2.Zero));
        bus.ProcessEvents();

        world.TryGetComponent(target, out Health health);
        Assert.InRange(health.Current, 84.5f, 85.5f); // 3 stacks -> ~15 dmg
    }

    [Fact]
    public void Burn_RefreshesDuration()
    {
        var (world, bus, tickSystem, _) = CreateWorldWithSystems();
        var attacker = CreateAttacker(world);
        var target = CreateTarget(world);
        var damageService = new DamageApplicationService(world, new DamageCalculator(new CombatRng(77)));

        var burn = StatusEffectConfig.CreateBurn();
        var damageInfo = new DamageInfo(0f, DamageType.Physical, DamageFlags.None, DamageSource.Melee, burn);

        damageService.ApplyDamage(attacker, target, damageInfo, Vector2.Zero);
        bus.ProcessEvents();

        tickSystem.Update(world, new EcsUpdateContext(null!, 1.0f, null!, null!, Vector2.Zero));
        bus.ProcessEvents();

        damageService.ApplyDamage(attacker, target, damageInfo, Vector2.Zero);
        bus.ProcessEvents();

        Assert.True(world.TryGetComponent(target, out ActiveStatusEffects effects));
        Assert.InRange(effects.Effects[0].RemainingDuration, 2.9f, 3.1f);
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

        tickSystem.Update(world, new EcsUpdateContext(null!, 0.1f, null!, null!, Vector2.Zero));
        bus.ProcessEvents();
        statSystem.Update(world, new EcsUpdateContext(null!, 0.1f, null!, null!, Vector2.Zero));

        world.TryGetComponent(target, out MoveSpeed moveSpeed);
        Assert.InRange(moveSpeed.Value, 99f, 101f); // 50% slow on 200 base
    }

    [Fact]
    public void Freeze_DoesNotStack_StrongestWins()
    {
        var (world, bus, tickSystem, _) = CreateWorldWithSystems();
        var attacker = CreateAttacker(world);
        var target = CreateTarget(world);
        var damageService = new DamageApplicationService(world, new DamageCalculator(new CombatRng(33)));

        var weakFreeze = StatusEffectConfig.CreateFreeze(potency: 0.4f, duration: 2f);
        var strongFreeze = StatusEffectConfig.CreateFreeze(potency: 0.7f, duration: 2f);

        damageService.ApplyDamage(attacker, target, new DamageInfo(0f, DamageType.Physical, DamageFlags.None, DamageSource.Melee, weakFreeze), Vector2.Zero);
        bus.ProcessEvents();
        damageService.ApplyDamage(attacker, target, new DamageInfo(0f, DamageType.Physical, DamageFlags.None, DamageSource.Melee, strongFreeze), Vector2.Zero);
        bus.ProcessEvents();

        tickSystem.Update(world, new EcsUpdateContext(null!, 0.1f, null!, null!, Vector2.Zero));
        bus.ProcessEvents();

        Assert.True(world.TryGetComponent(target, out ActiveStatusEffects effects));
        Assert.Equal(1, effects.Effects[0].CurrentStacks);
        Assert.InRange(effects.Effects[0].Data.Potency, 0.69f, 0.71f);
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
    public void Invulnerable_BlocksStatusApplication()
    {
        var (world, bus, _, _) = CreateWorldWithSystems();
        var attacker = CreateAttacker(world);
        var target = CreateTarget(world);
        world.SetComponent(target, new Invulnerable());

        var damageService = new DamageApplicationService(world, new DamageCalculator(new CombatRng(123)));
        var burn = StatusEffectConfig.CreateBurn();
        var damageInfo = new DamageInfo(0f, DamageType.Physical, DamageFlags.None, DamageSource.Melee, burn);

        damageService.ApplyDamage(attacker, target, damageInfo, Vector2.Zero);
        bus.ProcessEvents();

        Assert.False(world.TryGetComponent(target, out ActiveStatusEffects _));
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
    public void Shock_AmplifiesIncomingDamage_AndRespectsResistance()
    {
        var (world, bus, _, _) = CreateWorldWithSystems();
        var attacker = CreateAttacker(world);
        var target = CreateTarget(world);
        world.SetComponent(target, new DefensiveStats { Armor = 0f, ArcaneResist = 50f, FireResist = 0f, FrostResist = 0f });
        world.SetComponent(target, CreateComputed(arcaneResist: 50f));

        var damageService = new DamageApplicationService(world, new DamageCalculator(new CombatRng(5)));

        // Apply shock with 25% base amp; arcane resist should reduce potency and duration.
        var shock = StatusEffectConfig.CreateShock(potency: 0.25f, duration: 2f);
        damageService.ApplyDamage(attacker, target, new DamageInfo(0f, DamageType.Physical, DamageFlags.None, DamageSource.Melee, shock), Vector2.Zero);
        bus.ProcessEvents();

        // Deal 10 base damage. With 50 resist, expected amp ~ (1 - 0.333) = 0.667 => ~16.7% amp.
        damageService.ApplyDamage(attacker, target, new DamageInfo(10f, DamageType.Physical, DamageFlags.None, DamageSource.Melee), Vector2.Zero);
        bus.ProcessEvents();

        world.TryGetComponent(target, out Health health);
        Assert.InRange(health.Current, 88.0f, 89.0f);
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

        tickSystem.Update(world, new EcsUpdateContext(null!, 1.0f, null!, null!, Vector2.Zero));
        bus.ProcessEvents();

        world.TryGetComponent(target, out Health health);
        Assert.InRange(health.Current, 95.5f, 96.5f); // ~4.2 damage from ramped poison
    }

    [Fact]
    public void Ticking_IsDeterministic_ForIrregularSteps()
    {
        static void Step(EcsWorld world, EventBus bus, StatusEffectTickSystem tick, params float[] deltas)
        {
            foreach (var delta in deltas)
            {
                tick.Update(world, new EcsUpdateContext(null!, delta, null!, null!, Vector2.Zero));
                bus.ProcessEvents();
            }
        }

        var (worldA, busA, tickA, _) = CreateWorldWithSystems();
        var (worldB, busB, tickB, _) = CreateWorldWithSystems();

        var attackerA = CreateAttacker(worldA);
        var targetA = CreateTarget(worldA);
        var attackerB = CreateAttacker(worldB);
        var targetB = CreateTarget(worldB);

        var damageServiceA = new DamageApplicationService(worldA, new DamageCalculator(new CombatRng(1)));
        var damageServiceB = new DamageApplicationService(worldB, new DamageCalculator(new CombatRng(1)));

        var burn = StatusEffectConfig.CreateBurn(potency: 5f, duration: 1.2f, tickInterval: 0.2f);
        var dmg = new DamageInfo(0f, DamageType.Physical, DamageFlags.None, DamageSource.Melee, burn);

        damageServiceA.ApplyDamage(attackerA, targetA, dmg, Vector2.Zero);
        busA.ProcessEvents();
        damageServiceB.ApplyDamage(attackerB, targetB, dmg, Vector2.Zero);
        busB.ProcessEvents();

        Step(worldA, busA, tickA, 0.17f, 0.33f, 0.11f, 0.39f);
        Step(worldB, busB, tickB, 0.25f, 0.25f, 0.25f, 0.25f);

        worldA.TryGetComponent(targetA, out Health healthA);
        worldB.TryGetComponent(targetB, out Health healthB);

        Assert.Equal(healthA.Current, healthB.Current, precision: 4);
    }

    [Fact]
    public void DoT_Damage_UsesTrueDamage()
    {
        var (world, bus, tickSystem, _) = CreateWorldWithSystems();
        var attacker = CreateAttacker(world);
        var target = CreateTarget(world);

        // Extremely high armor would heavily reduce physical damage, but should not reduce DoT ticks.
        var computed = CreateComputed(arcaneResist: 0f, fireResist: 0f, frostResist: 0f, moveSpeed: 200f);
        computed.EffectiveArmor = 1000f;
        world.SetComponent(target, computed);

        var damageService = new DamageApplicationService(world, new DamageCalculator(new CombatRng(2)));
        var burn = StatusEffectConfig.CreateBurn(potency: 5f, duration: 3f, tickInterval: 0.5f);
        var damageInfo = new DamageInfo(0f, DamageType.Physical, DamageFlags.None, DamageSource.Melee, burn);

        damageService.ApplyDamage(attacker, target, damageInfo, Vector2.Zero);
        bus.ProcessEvents();

        tickSystem.Update(world, new EcsUpdateContext(null!, 1.0f, null!, null!, Vector2.Zero));
        bus.ProcessEvents();

        world.TryGetComponent(target, out Health health);
        Assert.InRange(health.Current, 94.5f, 95.5f);
    }

    [Fact]
    public void Expires_AndRemovesModifiers()
    {
        var (world, bus, tickSystem, statSystem) = CreateWorldWithSystems();
        var attacker = CreateAttacker(world);
        var target = CreateTarget(world);
        var damageService = new DamageApplicationService(world, new DamageCalculator(new CombatRng(3)));

        var slow = StatusEffectConfig.CreateSlow(potency: 0.5f, duration: 0.2f);
        damageService.ApplyDamage(attacker, target, new DamageInfo(0f, DamageType.Physical, DamageFlags.None, DamageSource.Melee, slow), Vector2.Zero);
        bus.ProcessEvents();

        tickSystem.Update(world, new EcsUpdateContext(null!, 0.25f, null!, null!, Vector2.Zero));
        bus.ProcessEvents();
        statSystem.Update(world, new EcsUpdateContext(null!, 0.01f, null!, null!, Vector2.Zero));

        Assert.False(world.TryGetComponent(target, out ActiveStatusEffects _));
        Assert.False(world.TryGetComponent(target, out StatusEffectModifiers _));
    }
}
