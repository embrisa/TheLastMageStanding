using System;
using System.Collections.Generic;
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
        var reduceFlashing = GetReduceStatusEffectFlashing(world);

        _damageService ??= new DamageApplicationService(
            world,
            new DamageCalculator(new CombatRng()));

        world.ForEach<ActiveStatusEffects>(
            (Entity entity, ref ActiveStatusEffects active) =>
            {
                if (active.Effects == null || active.Effects.Count == 0)
                {
                    world.RemoveComponent<ActiveStatusEffects>(entity);
                    world.RemoveComponent<StatusEffectVisual>(entity);
                    return;
                }

                var statusMods = StatModifiers.Zero;
                var dirtyStats = false;
                var slowPotency = 0f;

                for (int i = active.Effects.Count - 1; i >= 0; i--)
                {
                    var effect = active.Effects[i];
                    var remainingBefore = effect.RemainingDuration;
                    effect.RemainingDuration -= delta;

                    // Tick DoT effects
                    if (effect.Data.TickInterval > 0f && remainingBefore > 0f)
                    {
                        var activeDelta = MathF.Max(0f, MathF.Min(delta, remainingBefore));
                        effect.AccumulatedTickTime += activeDelta;

                        var interval = effect.Data.TickInterval;
                        var ticks = (int)MathF.Floor((effect.AccumulatedTickTime + 0.0001f) / interval);
                        if (ticks > 0)
                        {
                            effect.AccumulatedTickTime -= ticks * interval;
                            for (var t = 0; t < ticks; t++)
                            {
                                ApplyTick(entity, ref effect);
                            }
                        }
                    }

                    if (effect.RemainingDuration <= 0f)
                    {
                        active.Effects.RemoveAt(i);
                        world.EventBus.Publish(new StatusEffectExpiredEvent(entity, effect.Data.Type));
                        dirtyStats = true;
                        continue;
                    }

                    // Build stat debuffs for slow/freeze
                    if (effect.Data.Type == StatusEffectType.Slow || effect.Data.Type == StatusEffectType.Freeze)
                    {
                        slowPotency = Math.Max(slowPotency, effect.Data.Potency);
                        dirtyStats = true;
                    }

                    active.Effects[i] = effect;
                }

                if (active.Effects.Count == 0)
                {
                    world.RemoveComponent<ActiveStatusEffects>(entity);
                    world.RemoveComponent<StatusEffectModifiers>(entity);
                    world.RemoveComponent<StatusEffectVisual>(entity);
                }
                else
                {
                    world.SetComponent(entity, active);
                }

                UpdateStatusVisual(world, entity, active, delta, reduceFlashing);

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

    private static bool GetReduceStatusEffectFlashing(EcsWorld world)
    {
        var reduce = false;
        world.ForEach<VideoSettingsState>((Entity _, ref VideoSettingsState video) =>
        {
            reduce = video.ReduceStatusEffectFlashing;
        });
        return reduce;
    }

    private static void UpdateStatusVisual(
        EcsWorld world,
        Entity entity,
        in ActiveStatusEffects active,
        float deltaSeconds,
        bool reduceFlashing)
    {
        if (active.Effects == null || active.Effects.Count == 0)
        {
            world.RemoveComponent<StatusEffectVisual>(entity);
            return;
        }

        var dominant = GetDominantEffectType(active.Effects);
        if (dominant == StatusEffectType.None)
        {
            world.RemoveComponent<StatusEffectVisual>(entity);
            return;
        }

        var (color, baseStrength, pulse) = dominant switch
        {
            StatusEffectType.Burn => (new Color(255, 120, 50), 0.30f, true),
            StatusEffectType.Freeze => (new Color(100, 200, 255), 0.40f, false),
            StatusEffectType.Slow => (new Color(150, 180, 255), 0.25f, false),
            StatusEffectType.Shock => (new Color(150, 100, 255), 0.33f, true),
            StatusEffectType.Poison => (new Color(50, 200, 80), 0.30f, true),
            _ => (Color.White, 0f, false)
        };

        var visual = world.TryGetComponent(entity, out StatusEffectVisual existing)
            ? existing
            : new StatusEffectVisual();

        if (visual.DominantType != dominant)
        {
            visual.PulseTime = 0f;
        }

        visual.DominantType = dominant;
        visual.Color = color;
        visual.PulseTime += deltaSeconds;

        var strength = baseStrength;
        if (pulse && !reduceFlashing)
        {
            var pulseFactor = 0.78f + (0.22f * MathF.Sin(visual.PulseTime * 10f));
            strength = baseStrength * pulseFactor;
        }

        visual.Strength = Math.Clamp(strength, 0f, 0.85f);
        world.SetComponent(entity, visual);
    }

    private static StatusEffectType GetDominantEffectType(List<ActiveStatusEffect> effects)
    {
        StatusEffectType dominant = StatusEffectType.None;
        var bestPriority = int.MinValue;

        foreach (var effect in effects)
        {
            if (effect.RemainingDuration <= 0f)
            {
                continue;
            }

            var priority = effect.Data.Type switch
            {
                StatusEffectType.Freeze => 50,
                StatusEffectType.Shock => 40,
                StatusEffectType.Poison => 30,
                StatusEffectType.Burn => 20,
                StatusEffectType.Slow => 10,
                _ => 0
            };

            if (priority > bestPriority)
            {
                bestPriority = priority;
                dominant = effect.Data.Type;
            }
        }

        return dominant;
    }
}
