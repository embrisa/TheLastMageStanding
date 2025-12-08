using System;
using Microsoft.Xna.Framework;
using TheLastMageStanding.Game.Core.Config;
using TheLastMageStanding.Game.Core.Ecs;
using TheLastMageStanding.Game.Core.Ecs.Components;
using TheLastMageStanding.Game.Core.Events;

namespace TheLastMageStanding.Game.Core.Combat;

/// <summary>
/// Unified damage application service used by all combat systems.
/// Applies the full damage calculation pipeline: power → crit → resistance.
/// </summary>
internal sealed class DamageApplicationService
{
    private readonly EcsWorld _world;
    private readonly DamageCalculator _damageCalculator;

    public DamageApplicationService(EcsWorld world, DamageCalculator damageCalculator)
    {
        _world = world;
        _damageCalculator = damageCalculator;
    }

    /// <summary>
    /// Apply damage from attacker to target using unified calculation.
    /// Returns the final damage dealt (after all modifiers).
    /// </summary>
    public float ApplyDamage(
        Entity attacker,
        Entity target,
        DamageInfo damageInfo,
        Vector2 sourcePosition)
    {
        damageInfo = ApplyShockIfNeeded(target, damageInfo);

        // Get attacker's offensive stats
        var attackerOffense = _world.TryGetComponent(attacker, out ComputedStats attackerComputed)
            ? new OffensiveStats
            {
                Power = attackerComputed.EffectivePower,
                AttackSpeed = attackerComputed.EffectiveAttackSpeed,
                CritChance = attackerComputed.EffectiveCritChance,
                CritMultiplier = attackerComputed.EffectiveCritMultiplier,
                CooldownReduction = attackerComputed.EffectiveCooldownReduction
            }
            : OffensiveStats.Default;

        // Get target's defensive stats
        var targetDefense = _world.TryGetComponent(target, out ComputedStats targetComputed)
            ? new DefensiveStats
            {
                Armor = targetComputed.EffectiveArmor,
                ArcaneResist = targetComputed.EffectiveArcaneResist,
                FireResist = targetComputed.EffectiveFireResist,
                FrostResist = targetComputed.EffectiveFrostResist,
                NatureResist = targetComputed.EffectiveNatureResist
            }
            : DefensiveStats.Default;

        // Calculate damage
        var result = _damageCalculator.CalculateDamage(
            damageInfo,
            in attackerOffense,
            in targetDefense);

        // Get attacker faction for event
        var attackerFaction = _world.TryGetComponent(attacker, out Faction faction)
            ? faction
            : Faction.Neutral;

        // Publish damage event with full information
        _world.EventBus.Publish(new EntityDamagedEvent(
            attacker,
            target,
            result.FinalDamage,
            damageInfo,
            sourcePosition,
            attackerFaction,
            result.IsCritical));

        return result.FinalDamage;
    }

    /// <summary>
    /// Apply simple damage without an attacker entity (e.g., environmental damage).
    /// </summary>
    public float ApplyDamage(
        Entity target,
        DamageInfo damageInfo,
        Vector2 sourcePosition,
        Faction sourceFaction = Faction.Neutral)
    {
        damageInfo = ApplyShockIfNeeded(target, damageInfo);

        // No attacker stats, use defaults
        var attackerOffense = OffensiveStats.Default;

        // Get target's defensive stats
        var targetDefense = _world.TryGetComponent(target, out ComputedStats targetComputed)
            ? new DefensiveStats
            {
                Armor = targetComputed.EffectiveArmor,
                ArcaneResist = targetComputed.EffectiveArcaneResist,
                FireResist = targetComputed.EffectiveFireResist,
                FrostResist = targetComputed.EffectiveFrostResist,
                NatureResist = targetComputed.EffectiveNatureResist
            }
            : DefensiveStats.Default;

        // Calculate damage
        var result = _damageCalculator.CalculateDamage(
            damageInfo,
            in attackerOffense,
            in targetDefense);

        // Publish damage event
        _world.EventBus.Publish(new EntityDamagedEvent(
            Entity.None,
            target,
            result.FinalDamage,
            damageInfo,
            sourcePosition,
            sourceFaction,
            result.IsCritical));

        return result.FinalDamage;
    }

    private DamageInfo ApplyShockIfNeeded(Entity target, DamageInfo damageInfo)
    {
        if (!_world.TryGetComponent(target, out ActiveStatusEffects effects) || effects.Effects == null)
        {
            return damageInfo;
        }

        foreach (var effect in effects.Effects)
        {
            if (effect.Data.Type != StatusEffectType.Shock || effect.CurrentStacks <= 0)
            {
                continue;
            }

            var multiplier = 1f + (StatusEffectConfig.ShockDamageAmp * effect.CurrentStacks);
            return new DamageInfo(
                damageInfo.BaseDamage * multiplier,
                damageInfo.DamageType,
                damageInfo.Flags,
                damageInfo.Source,
                damageInfo.StatusEffect);
        }

        return damageInfo;
    }
}
