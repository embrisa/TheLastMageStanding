using System;
using System.Collections.Generic;
using TheLastMageStanding.Game.Core.Combat;
using TheLastMageStanding.Game.Core.Ecs.Components;
using TheLastMageStanding.Game.Core.Events;

namespace TheLastMageStanding.Game.Core.Ecs.Systems;

/// <summary>
/// Applies incoming status effects from damage events to targets, respecting resistance/immune rules.
/// </summary>
internal sealed class StatusEffectApplicationSystem : IUpdateSystem
{
    private EcsWorld _world = null!;

    public void Initialize(EcsWorld world)
    {
        _world = world;
        world.EventBus.Subscribe<EntityDamagedEvent>(OnEntityDamaged);
    }

    public void Update(EcsWorld world, in EcsUpdateContext context)
    {
        // Event-driven
    }

    private void OnEntityDamaged(EntityDamagedEvent evt)
    {
        var statusEffect = evt.DamageInfo.StatusEffect;
        if (statusEffect is null)
        {
            return;
        }

        var data = statusEffect.Value;

        if (!_world.IsAlive(evt.Target))
        {
            return;
        }

        if (_world.TryGetComponent(evt.Target, out Health health) && health.IsDead)
        {
            return;
        }

        if (_world.TryGetComponent(evt.Target, out StatusEffectImmunities immunities) &&
            immunities.IsImmune(data.Type))
        {
            return;
        }

        var defense = GetDefensiveStats(evt.Target);
        var resistance = DamageCalculator.CalculateStatusEffectResistance(data.Type, in defense);

        if (_world.TryGetComponent(evt.Target, out StatusEffectResistances extraResist))
        {
            var extra = Math.Clamp(extraResist.Get(data.Type), 0f, 1f);
            resistance = 1f - ((1f - resistance) * (1f - extra));
        }

        var adjustedDuration = data.Duration * (1f - resistance);
        var adjustedPotency = data.Potency * (1f - resistance);

        if (adjustedDuration <= 0f)
        {
            return;
        }

        if (data.TickInterval > 0f && adjustedPotency <= 0f)
        {
            return;
        }

        var appliedData = new StatusEffectData(
            data.Type,
            adjustedPotency,
            adjustedDuration,
            data.TickInterval,
            data.MaxStacks,
            data.InitialStacks);

        ApplyOrRefresh(evt.Target, appliedData);
    }

    private DefensiveStats GetDefensiveStats(Entity target)
    {
        if (_world.TryGetComponent(target, out ComputedStats computed))
        {
            return new DefensiveStats
            {
                Armor = computed.EffectiveArmor,
                ArcaneResist = computed.EffectiveArcaneResist,
                FireResist = computed.EffectiveFireResist,
                FrostResist = computed.EffectiveFrostResist,
                NatureResist = computed.EffectiveNatureResist
            };
        }

        if (_world.TryGetComponent(target, out DefensiveStats defense))
        {
            return defense;
        }

        return DefensiveStats.Default;
    }

    private void ApplyOrRefresh(Entity target, StatusEffectData data)
    {
        var active = _world.TryGetComponent(target, out ActiveStatusEffects effects)
            ? effects
            : new ActiveStatusEffects { Effects = new List<ActiveStatusEffect>() };

        active.Effects ??= new List<ActiveStatusEffect>();

        var index = active.Effects.FindIndex(e => e.Data.Type == data.Type);
        int stacksApplied;

        if (index >= 0)
        {
            var effect = active.Effects[index];

            switch (data.Type)
            {
                case StatusEffectType.Burn:
                    effect.CurrentStacks = Math.Clamp(effect.CurrentStacks + data.InitialStacks, 1, data.MaxStacks);
                    effect.RemainingDuration = Math.Max(effect.RemainingDuration, data.Duration);
                    effect.Data = data;
                    break;

                case StatusEffectType.Freeze:
                case StatusEffectType.Slow:
                    var strongest = data.Potency > effect.Data.Potency ? data : effect.Data;
                    effect.Data = strongest;
                    effect.RemainingDuration = Math.Max(effect.RemainingDuration, data.Duration);
                    effect.CurrentStacks = 1;
                    break;

                case StatusEffectType.Shock:
                    effect.CurrentStacks = 1;
                    effect.RemainingDuration = Math.Max(effect.RemainingDuration, data.Duration);
                    effect.Data = data;
                    break;

                case StatusEffectType.Poison:
                    effect.CurrentStacks = Math.Clamp(effect.CurrentStacks + data.InitialStacks, 1, data.MaxStacks);
                    effect.RemainingDuration = Math.Max(effect.RemainingDuration, data.Duration);
                    effect.Data = data;
                    break;

                default:
                    effect.RemainingDuration = Math.Max(effect.RemainingDuration, data.Duration);
                    effect.Data = data;
                    break;
            }

            stacksApplied = effect.CurrentStacks;
            active.Effects[index] = effect;
        }
        else
        {
            var newEffect = new ActiveStatusEffect
            {
                Data = data,
                RemainingDuration = data.Duration,
                AccumulatedTickTime = 0f,
                CurrentStacks = Math.Clamp(data.InitialStacks, 1, data.MaxStacks)
            };

            active.Effects.Add(newEffect);
            stacksApplied = newEffect.CurrentStacks;
        }

        _world.SetComponent(target, active);

        if (_world.TryGetComponent(target, out ComputedStats computed))
        {
            ComputedStats.MarkDirty(ref computed);
            _world.SetComponent(target, computed);
        }

        _world.EventBus.Publish(new StatusEffectAppliedEvent(target, data.Type, data.Duration, stacksApplied));
    }
}

