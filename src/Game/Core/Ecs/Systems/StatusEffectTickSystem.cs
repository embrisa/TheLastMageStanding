using System;
using Microsoft.Xna.Framework;
using TheLastMageStanding.Game.Core.Combat;
using TheLastMageStanding.Game.Core.Config;
using TheLastMageStanding.Game.Core.Ecs.Components;
using TheLastMageStanding.Game.Core.Events;

namespace TheLastMageStanding.Game.Core.Ecs.Systems;

/// <summary>
/// Updates active status effects, applies DoT ticks, and maintains stat debuffs.
/// </summary>
internal sealed class StatusEffectTickSystem : IUpdateSystem
{
    private EcsWorld _world = null!;
    private DamageApplicationService? _damageService;

    public void Initialize(EcsWorld world)
    {
        _world = world;
    }

    public void Update(EcsWorld world, in EcsUpdateContext context)
    {
        var delta = context.DeltaSeconds;

        _damageService ??= new DamageApplicationService(
            world,
            new DamageCalculator(new CombatRng()));

        world.ForEach<ActiveStatusEffects>(
            (Entity entity, ref ActiveStatusEffects active) =>
            {
                if (active.Effects == null || active.Effects.Count == 0)
                {
                    world.RemoveComponent<ActiveStatusEffects>(entity);
                    return;
                }

                var statusMods = StatModifiers.Zero;
                var dirtyStats = false;
                var slowPotency = 0f;

                for (int i = active.Effects.Count - 1; i >= 0; i--)
                {
                    var effect = active.Effects[i];
                    effect.RemainingDuration -= delta;

                    // Tick DoT effects
                    if (effect.Data.TickInterval > 0f && effect.RemainingDuration > 0f)
                    {
                        effect.AccumulatedTickTime += delta;
                        while (effect.AccumulatedTickTime >= effect.Data.TickInterval)
                        {
                            effect.AccumulatedTickTime -= effect.Data.TickInterval;
                            ApplyTick(entity, ref effect);
                        }
                    }

                    // Build stat debuffs for slow/freeze
                    if (effect.Data.Type == StatusEffectType.Slow || effect.Data.Type == StatusEffectType.Freeze)
                    {
                        slowPotency = Math.Max(slowPotency, effect.Data.Potency);
                        dirtyStats = true;
                    }

                    if (effect.RemainingDuration <= 0f)
                    {
                        active.Effects.RemoveAt(i);
                        world.EventBus.Publish(new StatusEffectExpiredEvent(entity, effect.Data.Type));
                        dirtyStats = true;
                        continue;
                    }

                    active.Effects[i] = effect;
                }

                if (active.Effects.Count == 0)
                {
                    world.RemoveComponent<ActiveStatusEffects>(entity);
                    world.RemoveComponent<StatusEffectModifiers>(entity);
                }
                else
                {
                    world.SetComponent(entity, active);
                }

                if (dirtyStats)
                {
                    if (slowPotency > 0f)
                    {
                        var multiplier = Math.Clamp(1f - slowPotency, 0.1f, 1f);
                        statusMods.MoveSpeedMultiplicative = multiplier;
                        statusMods.AttackSpeedMultiplicative = multiplier;
                    }

                    if (slowPotency <= 0f)
                    {
                        world.RemoveComponent<StatusEffectModifiers>(entity);
                    }
                    else
                    {
                        world.SetComponent(entity, new StatusEffectModifiers { Value = statusMods });
                    }
                }

                if (dirtyStats && world.TryGetComponent(entity, out ComputedStats computed))
                {
                    ComputedStats.MarkDirty(ref computed);
                    world.SetComponent(entity, computed);
                }
            });
    }

    private void ApplyTick(Entity target, ref ActiveStatusEffect effect)
    {
        if (_damageService == null)
        {
            return;
        }

        var position = _world.TryGetComponent(target, out Position pos)
            ? pos.Value
            : Vector2.Zero;

        float damage = effect.Data.Type switch
        {
            StatusEffectType.Burn => effect.Data.Potency * effect.CurrentStacks * effect.Data.TickInterval,
            StatusEffectType.Poison => CalculatePoisonDamage(effect) * effect.Data.TickInterval,
            _ => 0f
        };

        if (damage <= 0f)
        {
            return;
        }

        var info = new DamageInfo(
            baseDamage: damage,
            damageType: DamageType.True,
            flags: DamageFlags.None,
            source: DamageSource.StatusEffect);

        _damageService.ApplyDamage(
            target,
            info,
            position,
            Faction.Neutral);

        _world.EventBus.Publish(new StatusEffectTickEvent(target, effect.Data.Type, damage));
    }

    private static float CalculatePoisonDamage(in ActiveStatusEffect effect)
    {
        var rampStacks = Math.Max(0, effect.CurrentStacks - 1);
        var ramp = 1f + (StatusEffectConfig.PoisonRampPerStack * rampStacks);
        return effect.Data.Potency * ramp;
    }
}

