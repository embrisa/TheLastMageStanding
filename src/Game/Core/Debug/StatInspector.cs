using System;
using System.Globalization;
using System.Text;
using TheLastMageStanding.Game.Core.Ecs;
using TheLastMageStanding.Game.Core.Ecs.Components;
using TheLastMageStanding.Game.Core.Combat;

namespace TheLastMageStanding.Game.Core.Debug;

/// <summary>
/// Debug utility for inspecting entity stats and damage calculations.
/// Can be used in play tests to verify damage formulas and stat stacking.
/// </summary>
internal static class StatInspector
{
    /// <summary>
    /// Get a formatted string showing all stats for an entity.
    /// </summary>
    public static string InspectStats(EcsWorld world, Entity entity)
    {
        var sb = new StringBuilder();
        sb.AppendLine(CultureInfo.InvariantCulture, $"=== Stats for Entity {entity.Id} ===");

        // Basic info
        if (world.TryGetComponent(entity, out Faction faction))
            sb.AppendLine(CultureInfo.InvariantCulture, $"Faction: {faction}");

        if (world.TryGetComponent(entity, out Health health))
            sb.AppendLine(CultureInfo.InvariantCulture, $"Health: {health.Current:F1}/{health.Max:F1} ({health.Ratio:P0})");

        // Base stats
        if (world.TryGetComponent(entity, out OffensiveStats offense))
        {
            sb.AppendLine("\nBase Offensive Stats:");
            sb.AppendLine(CultureInfo.InvariantCulture, $"  Power: {offense.Power:F2}");
            sb.AppendLine(CultureInfo.InvariantCulture, $"  Attack Speed: {offense.AttackSpeed:F2}");
            sb.AppendLine(CultureInfo.InvariantCulture, $"  Crit Chance: {offense.CritChance:P1}");
            sb.AppendLine(CultureInfo.InvariantCulture, $"  Crit Multiplier: {offense.CritMultiplier:F2}x");
            sb.AppendLine(CultureInfo.InvariantCulture, $"  CDR: {offense.CooldownReduction:P1}");
        }

        if (world.TryGetComponent(entity, out DefensiveStats defense))
        {
            sb.AppendLine("\nBase Defensive Stats:");
            sb.AppendLine(CultureInfo.InvariantCulture, $"  Armor: {defense.Armor:F1}");
            sb.AppendLine(CultureInfo.InvariantCulture, $"  Arcane Resist: {defense.ArcaneResist:F1}");
            sb.AppendLine(CultureInfo.InvariantCulture, $"  Fire Resist: {defense.FireResist:F1}");
            sb.AppendLine(CultureInfo.InvariantCulture, $"  Frost Resist: {defense.FrostResist:F1}");
            sb.AppendLine(CultureInfo.InvariantCulture, $"  Nature Resist: {defense.NatureResist:F1}");
        }

        if (world.TryGetComponent(entity, out BaseMoveSpeed baseMoveSpeed))
        {
            sb.AppendLine(CultureInfo.InvariantCulture, $"\nBase Move Speed: {baseMoveSpeed.Value:F1}");
        }
        else if (world.TryGetComponent(entity, out MoveSpeed moveSpeed))
        {
            sb.AppendLine(CultureInfo.InvariantCulture, $"\nBase Move Speed: {moveSpeed.Value:F1}");
        }

        // Modifiers
        if (world.TryGetComponent(entity, out StatModifiers mods))
        {
            sb.AppendLine("\nModifiers:");
            if (mods.PowerAdditive != 0f || mods.PowerMultiplicative != 1f)
                sb.AppendLine(CultureInfo.InvariantCulture, $"  Power: +{mods.PowerAdditive:F2} × {mods.PowerMultiplicative:F2}");
            if (mods.AttackSpeedAdditive != 0f || mods.AttackSpeedMultiplicative != 1f)
                sb.AppendLine(CultureInfo.InvariantCulture, $"  Attack Speed: +{mods.AttackSpeedAdditive:F2} × {mods.AttackSpeedMultiplicative:F2}");
            if (mods.ArmorAdditive != 0f || mods.ArmorMultiplicative != 1f)
                sb.AppendLine(CultureInfo.InvariantCulture, $"  Armor: +{mods.ArmorAdditive:F1} × {mods.ArmorMultiplicative:F2}");
            if (mods.FireResistAdditive != 0f || mods.FireResistMultiplicative != 1f)
                sb.AppendLine(CultureInfo.InvariantCulture, $"  Fire Resist: +{mods.FireResistAdditive:F1} × {mods.FireResistMultiplicative:F2}");
            if (mods.FrostResistAdditive != 0f || mods.FrostResistMultiplicative != 1f)
                sb.AppendLine(CultureInfo.InvariantCulture, $"  Frost Resist: +{mods.FrostResistAdditive:F1} × {mods.FrostResistMultiplicative:F2}");
            if (mods.NatureResistAdditive != 0f || mods.NatureResistMultiplicative != 1f)
                sb.AppendLine(CultureInfo.InvariantCulture, $"  Nature Resist: +{mods.NatureResistAdditive:F1} × {mods.NatureResistMultiplicative:F2}");
            if (mods.MoveSpeedAdditive != 0f || mods.MoveSpeedMultiplicative != 1f)
                sb.AppendLine(CultureInfo.InvariantCulture, $"  Move Speed: +{mods.MoveSpeedAdditive:F1} × {mods.MoveSpeedMultiplicative:F2}");
        }

        // Computed stats
        if (world.TryGetComponent(entity, out ComputedStats computed))
        {
            sb.AppendLine("\nEffective Stats:");
            sb.AppendLine(CultureInfo.InvariantCulture, $"  Power: {computed.EffectivePower:F2}");
            sb.AppendLine(CultureInfo.InvariantCulture, $"  Attack Speed: {computed.EffectiveAttackSpeed:F2}");
            sb.AppendLine(CultureInfo.InvariantCulture, $"  Crit Chance: {computed.EffectiveCritChance:P1}");
            sb.AppendLine(CultureInfo.InvariantCulture, $"  Crit Multiplier: {computed.EffectiveCritMultiplier:F2}x");
            sb.AppendLine(CultureInfo.InvariantCulture, $"  CDR: {computed.EffectiveCooldownReduction:P1}");
            sb.AppendLine(CultureInfo.InvariantCulture, $"  Armor: {computed.EffectiveArmor:F1} ({CalculateReductionPercent(computed.EffectiveArmor):P1})");
            sb.AppendLine(CultureInfo.InvariantCulture, $"  Arcane Resist: {computed.EffectiveArcaneResist:F1} ({CalculateReductionPercent(computed.EffectiveArcaneResist):P1})");
            sb.AppendLine(CultureInfo.InvariantCulture, $"  Fire Resist: {computed.EffectiveFireResist:F1} ({CalculateReductionPercent(computed.EffectiveFireResist):P1})");
            sb.AppendLine(CultureInfo.InvariantCulture, $"  Frost Resist: {computed.EffectiveFrostResist:F1} ({CalculateReductionPercent(computed.EffectiveFrostResist):P1})");
            sb.AppendLine(CultureInfo.InvariantCulture, $"  Nature Resist: {computed.EffectiveNatureResist:F1} ({CalculateReductionPercent(computed.EffectiveNatureResist):P1})");
            sb.AppendLine(CultureInfo.InvariantCulture, $"  Move Speed: {computed.EffectiveMoveSpeed:F1}");
            sb.AppendLine(CultureInfo.InvariantCulture, $"  [Dirty: {computed.IsDirty}]");
        }

        // Attack info
        if (world.TryGetComponent(entity, out AttackStats attackStats))
        {
            sb.AppendLine("\nAttack Stats:");
            sb.AppendLine(CultureInfo.InvariantCulture, $"  Base Damage: {attackStats.Damage:F1}");
            sb.AppendLine(CultureInfo.InvariantCulture, $"  Cooldown: {attackStats.CooldownSeconds:F2}s");
            sb.AppendLine(CultureInfo.InvariantCulture, $"  Range: {attackStats.Range:F1}");

            if (world.TryGetComponent(entity, out ComputedStats comp))
            {
                var effectiveCd = StatCalculator.CalculateEffectiveCooldown(
                    attackStats.CooldownSeconds,
                    comp.EffectiveAttackSpeed,
                    comp.EffectiveCooldownReduction);
                sb.AppendLine(CultureInfo.InvariantCulture, $"  Effective Cooldown: {effectiveCd:F2}s");
                var avgDps = attackStats.Damage * comp.EffectivePower / effectiveCd;
                var critDps = avgDps * (1f + comp.EffectiveCritChance * (comp.EffectiveCritMultiplier - 1f));
                sb.AppendLine(CultureInfo.InvariantCulture, $"  Avg DPS: {avgDps:F1}");
                sb.AppendLine(CultureInfo.InvariantCulture, $"  DPS w/ Crits: {critDps:F1}");
            }
        }

        return sb.ToString();
    }

    /// <summary>
    /// Simulate a damage calculation for debugging.
    /// </summary>
    public static string SimulateDamage(
        DamageInfo damageInfo,
        in OffensiveStats attackerStats,
        in DefensiveStats defenderStats,
        uint seed = 12345)
    {
        var rng = new CombatRng(seed);
        var calculator = new DamageCalculator(rng);
        var result = calculator.CalculateDamage(damageInfo, in attackerStats, in defenderStats);

        var sb = new StringBuilder();
        sb.AppendLine("=== Damage Calculation ===");
        sb.AppendLine(CultureInfo.InvariantCulture, $"Base Damage: {damageInfo.BaseDamage:F1}");
        sb.AppendLine(CultureInfo.InvariantCulture, $"Damage Type: {damageInfo.DamageType}");
        sb.AppendLine(CultureInfo.InvariantCulture, $"Flags: {damageInfo.Flags}");
        sb.AppendLine(CultureInfo.InvariantCulture, $"\nAttacker Power: {attackerStats.Power:F2}");
        sb.AppendLine(CultureInfo.InvariantCulture, $"After Power: {damageInfo.BaseDamage * attackerStats.Power:F1}");
        
        if (result.IsCritical)
        {
            sb.AppendLine(CultureInfo.InvariantCulture, $"\n*** CRITICAL HIT! *** ({attackerStats.CritChance:P1} chance)");
            sb.AppendLine(CultureInfo.InvariantCulture, $"Crit Multiplier: {attackerStats.CritMultiplier:F2}x");
        }
        else
        {
            sb.AppendLine(CultureInfo.InvariantCulture, $"\nNo crit ({attackerStats.CritChance:P1} chance)");
        }

        sb.AppendLine(CultureInfo.InvariantCulture, $"\nDefender Armor: {defenderStats.Armor:F1}");
        sb.AppendLine(CultureInfo.InvariantCulture, $"Defender Resist: {defenderStats.ArcaneResist:F1}");
        sb.AppendLine(CultureInfo.InvariantCulture, $"Damage Reduction: {result.DamageReduction:P1}");
        sb.AppendLine(CultureInfo.InvariantCulture, $"\n=== FINAL DAMAGE: {result.FinalDamage:F1} ===");

        return sb.ToString();
    }

    private static float CalculateReductionPercent(float armorOrResist)
    {
        if (armorOrResist <= 0f)
            return 0f;
        return Math.Clamp(armorOrResist / (armorOrResist + 100f), 0f, 0.9f);
    }
}
