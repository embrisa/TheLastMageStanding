using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using TheLastMageStanding.Game.Core.Combat;
using TheLastMageStanding.Game.Core.Ecs.Components;
using TheLastMageStanding.Game.Core.Events;

namespace TheLastMageStanding.Game.Core.Ecs.Systems;

/// <summary>
/// Handles elite modifier runtime effects (vampiric heals, shield regen, explosive death).
/// </summary>
internal sealed class EliteModifierSystem : IUpdateSystem
{
    private const float VampiricLifestealPercent = 0.3f;
    private const float ExplosionTelegraphSeconds = 1.5f;
    private const float ExplosionRadius = 72f;
    private const float ExplosionDamage = 28f;

    private EcsWorld _world = null!;
    private DamageApplicationService? _damageService;

    public void Initialize(EcsWorld world)
    {
        _world = world;
        world.EventBus.Subscribe<EntityDamagedEvent>(OnEntityDamaged);
        world.EventBus.Subscribe<EnemyDiedEvent>(OnEnemyDied);
    }

    public void Update(EcsWorld world, in EcsUpdateContext context)
    {
        _damageService ??= new DamageApplicationService(
            world,
            new DamageCalculator(new CombatRng()));

        RegenerateShields(world, context.DeltaSeconds);
        TickExplosions(world, context.DeltaSeconds);
    }

    private void OnEntityDamaged(EntityDamagedEvent evt)
    {
        if (evt.Source == Entity.None || _world == null)
        {
            return;
        }

        if (!_world.TryGetComponent(evt.Source, out EliteModifierData modifierData) ||
            !modifierData.HasModifier(EliteModifierType.Vampiric))
        {
            return;
        }

        if (!_world.TryGetComponent(evt.Source, out Health attackerHealth) || attackerHealth.IsDead)
        {
            return;
        }

        var healAmount = evt.Amount * VampiricLifestealPercent;
        attackerHealth.Current = MathF.Min(attackerHealth.Max, attackerHealth.Current + healAmount);
        _world.SetComponent(evt.Source, attackerHealth);

        if (_world.TryGetComponent(evt.Source, out Position attackerPosition))
        {
            _world.EventBus.Publish(new VfxSpawnEvent("elite_vampiric_heal", attackerPosition.Value, VfxType.Impact, new Color(140, 255, 140)));
        }
    }

    private void OnEnemyDied(EnemyDiedEvent evt)
    {
        if (_world == null)
        {
            return;
        }

        if (!HasModifier(evt.Modifiers, EliteModifierType.ExplosiveDeath))
        {
            return;
        }

        var explosionEntity = _world.CreateEntity();
        _world.SetComponent(explosionEntity, new Position(evt.Position));
        _world.SetComponent(
            explosionEntity,
            new PendingExplosion
            {
                RemainingTime = ExplosionTelegraphSeconds,
                Radius = ExplosionRadius,
                Damage = ExplosionDamage,
                SourceFaction = Faction.Enemy
            });
        _world.SetComponent(
            explosionEntity,
            new ActiveTelegraph(
                ExplosionTelegraphSeconds,
                new TelegraphData(
                    ExplosionTelegraphSeconds,
                    new Color(255, 80, 60, 110),
                    ExplosionRadius,
                    Vector2.Zero)));
        _world.SetComponent(explosionEntity, new Lifetime(ExplosionTelegraphSeconds + 0.5f));
    }

    private static void RegenerateShields(EcsWorld world, float deltaSeconds)
    {
        world.ForEach<EliteShield, Health>(
            (Entity entity, ref EliteShield shield, ref Health health) =>
            {
                if (health.IsDead)
                {
                    return;
                }

                shield.CooldownTimer = MathF.Max(0f, shield.CooldownTimer - deltaSeconds);
                if (shield.CooldownTimer <= 0f && shield.Current < shield.Max)
                {
                    shield.Current = MathF.Min(shield.Max, shield.Current + (shield.RegenRate * deltaSeconds));
                }

                world.SetComponent(entity, shield);
            });
    }

    private void TickExplosions(EcsWorld world, float deltaSeconds)
    {
        if (_damageService == null)
        {
            return;
        }

        var toDetonate = new List<(Entity entity, PendingExplosion explosion, Vector2 position)>();
        world.ForEach<PendingExplosion, Position>((Entity entity, ref PendingExplosion explosion, ref Position position) =>
        {
            explosion.RemainingTime -= deltaSeconds;
            if (explosion.RemainingTime <= 0f)
            {
                toDetonate.Add((entity, explosion, position.Value));
            }
            else
            {
                world.SetComponent(entity, explosion);
            }
        });

        foreach (var (entity, explosion, position) in toDetonate)
        {
            Detonate(position, explosion);
            world.DestroyEntity(entity);
        }
    }

    private void Detonate(Vector2 center, PendingExplosion explosion)
    {
        if (_damageService == null)
        {
            return;
        }

        _world.EventBus.Publish(new VfxSpawnEvent("elite_explosive_death", center, VfxType.Impact, new Color(255, 180, 80)));
        _world.EventBus.Publish(new SfxPlayEvent("elite_explosive_death", SfxCategory.Impact, center));

        _world.ForEach<Position, Health, Faction>((Entity target, ref Position pos, ref Health health, ref Faction faction) =>
        {
            if (health.IsDead)
            {
                return;
            }

            var distanceSq = Vector2.DistanceSquared(center, pos.Value);
            if (distanceSq > explosion.Radius * explosion.Radius)
            {
                return;
            }

            if (_world.TryGetComponent(target, out Hurtbox hurtbox) && hurtbox.IsInvulnerable)
            {
                return;
            }

            var damageInfo = new DamageInfo(
                explosion.Damage,
                DamageType.Arcane,
                DamageFlags.CanCrit,
                DamageSource.Environmental);

            _damageService.ApplyDamage(
                target,
                damageInfo,
                center,
                explosion.SourceFaction);
        });
    }

    private static bool HasModifier(IReadOnlyList<EliteModifierType>? modifiers, EliteModifierType type)
    {
        if (modifiers == null)
        {
            return false;
        }

        for (var i = 0; i < modifiers.Count; i++)
        {
            if (modifiers[i] == type)
            {
                return true;
            }
        }

        return false;
    }
}

