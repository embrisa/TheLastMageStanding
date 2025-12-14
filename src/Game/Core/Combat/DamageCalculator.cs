using System;
using TheLastMageStanding.Game.Core.Ecs;
using TheLastMageStanding.Game.Core.Ecs.Components;

namespace TheLastMageStanding.Game.Core.Combat;

/// <summary>
/// Deterministic random number generator for combat calculations (crits, etc).
/// Uses a simple LCG for fast, deterministic rolls that work across platforms.
/// </summary>
internal sealed class CombatRng
{
    private uint _seed;

    public CombatRng(uint seed = 0)
    {
        _seed = seed == 0 ? (uint)Environment.TickCount : seed;
    }

    /// <summary>
    /// Get a random float in [0, 1) range.
    /// </summary>
    public float NextFloat()
    {
        // Linear Congruential Generator (LCG)
        // Using constants from Numerical Recipes
        _seed = (1664525u * _seed + 1013904223u);
        return (_seed >> 8) / 16777216f; // Use upper 24 bits for better distribution
    }

    /// <summary>
    /// Roll for a critical hit based on crit chance.
    /// </summary>
    public bool RollCrit(float critChance)
    {
        critChance = Math.Clamp(critChance, 0f, 1f);
        return NextFloat() < critChance;
    }

    /// <summary>
    /// Seed the RNG with a specific value (for testing).
    /// </summary>
    public void Seed(uint seed)
    {
        _seed = seed;
    }
}

/// <summary>
/// Unified damage calculator used by all combat systems.
/// Applies power, crit, resistances in a consistent order.
/// </summary>
internal sealed class DamageCalculator
{
    private readonly CombatRng _rng;

    public DamageCalculator(CombatRng rng)
    {
        _rng = rng;
    }

    /// <summary>
    /// Calculate final damage from attacker to defender.
    /// </summary>
    public DamageResult CalculateDamage(
        DamageInfo damageInfo,
        in OffensiveStats attackerOffense,
        in DefensiveStats defenderDefense)
    {
        // Step 1: Apply power multiplier
        var damageAfterPower = damageInfo.BaseDamage * attackerOffense.Power;

        // Step 2: Check for critical hit
        var isCrit = false;
        if (damageInfo.HasFlag(DamageFlags.CanCrit))
        {
            isCrit = _rng.RollCrit(attackerOffense.CritChance);
        }

        // Step 3: Apply crit multiplier
        var damageAfterCrit = isCrit
            ? damageAfterPower * attackerOffense.CritMultiplier
            : damageAfterPower;

        // Step 4: Calculate damage reduction from armor/resist
        var damageReduction = 0f;
        if (damageInfo.HasType(DamageType.True))
        {
            // True damage bypasses all resistances
            damageReduction = 0f;
        }
        else
        {
            // Calculate reduction based on damage type
            var physicalReduction = 0f;
            var arcaneReduction = 0f;

            if (damageInfo.HasType(DamageType.Physical) && !damageInfo.HasFlag(DamageFlags.IgnoreArmor))
            {
                physicalReduction = CalculateReduction(defenderDefense.Armor);
            }

            if (damageInfo.HasType(DamageType.Arcane) && !damageInfo.HasFlag(DamageFlags.IgnoreResist))
            {
                arcaneReduction = CalculateReduction(defenderDefense.ArcaneResist);
            }

            // For hybrid damage, take the average reduction
            if (damageInfo.HasType(DamageType.Physical) && damageInfo.HasType(DamageType.Arcane))
            {
                damageReduction = (physicalReduction + arcaneReduction) / 2f;
            }
            else if (damageInfo.HasType(DamageType.Physical))
            {
                damageReduction = physicalReduction;
            }
            else if (damageInfo.HasType(DamageType.Arcane))
            {
                damageReduction = arcaneReduction;
            }
        }

        // Step 5: Apply reduction
        var finalDamage = damageAfterCrit * (1f - damageReduction);

        // Step 6: Ensure damage is non-negative
        finalDamage = Math.Max(0f, finalDamage);

        return new DamageResult(
            finalDamage: finalDamage,
            baseBeforeMultipliers: damageInfo.BaseDamage,
            isCritical: isCrit,
            damageReduction: damageReduction,
            damageType: damageInfo.DamageType,
            source: damageInfo.Source);
    }

    /// <summary>
    /// Calculate damage reduction from armor/resist using diminishing returns formula.
    /// Formula: reduction = stat / (stat + 100)
    /// - 50 armor/resist = 33% reduction
    /// - 100 armor/resist = 50% reduction
    /// - 200 armor/resist = 67% reduction
    /// Clamped to [0, 0.9] to prevent full immunity.
    /// </summary>
    private static float CalculateReduction(float armorOrResist)
    {
        if (armorOrResist <= 0f)
            return 0f;

        var reduction = armorOrResist / (armorOrResist + 100f);
        return Math.Clamp(reduction, 0f, 0.9f); // Max 90% reduction
    }

    /// <summary>
    /// Calculate resistance multiplier for status effects based on defensive stats.
    /// </summary>
    public static float CalculateStatusEffectResistance(StatusEffectType type, in DefensiveStats defense)
    {
        float rawResist = type switch
        {
            StatusEffectType.Burn => defense.FireResist,
            StatusEffectType.Freeze => defense.FrostResist,
            StatusEffectType.Slow => defense.FrostResist,
            StatusEffectType.Shock => defense.ArcaneResist,
            StatusEffectType.Poison => defense.ArcaneResist,
            _ => 0f
        };

        return CalculateReduction(rawResist);
    }
}

/// <summary>
/// Static helper for stat recalculation.
/// </summary>
internal static class StatCalculator
{
    /// <summary>
    /// Recalculate effective stats from base stats and modifiers.
    /// Uses stacking order: base → additive → multiplicative
    /// </summary>
    public static ComputedStats RecalculateStats(
        in OffensiveStats baseOffense,
        in DefensiveStats baseDefense,
        float baseMoveSpeed,
        in StatModifiers modifiers)
    {
        var computed = new ComputedStats();

        // Offensive stats
        computed.EffectivePower = (baseOffense.Power + modifiers.PowerAdditive) * modifiers.PowerMultiplicative;
        computed.EffectiveAttackSpeed = (baseOffense.AttackSpeed + modifiers.AttackSpeedAdditive) * modifiers.AttackSpeedMultiplicative;
        computed.EffectiveCritChance = Math.Clamp(baseOffense.CritChance + modifiers.CritChanceAdditive, 0f, 1f);
        computed.EffectiveCritMultiplier = baseOffense.CritMultiplier + modifiers.CritMultiplierAdditive;
        computed.EffectiveCooldownReduction = Math.Clamp(baseOffense.CooldownReduction + modifiers.CooldownReductionAdditive, 0f, 0.8f); // Max 80% CDR

        // Defensive stats
        computed.EffectiveArmor = Math.Max(0f, (baseDefense.Armor + modifiers.ArmorAdditive) * modifiers.ArmorMultiplicative);
        computed.EffectiveArcaneResist = Math.Max(0f, (baseDefense.ArcaneResist + modifiers.ArcaneResistAdditive) * modifiers.ArcaneResistMultiplicative);
        computed.EffectiveFireResist = Math.Max(0f, (baseDefense.FireResist + modifiers.FireResistAdditive) * modifiers.FireResistMultiplicative);
        computed.EffectiveFrostResist = Math.Max(0f, (baseDefense.FrostResist + modifiers.FrostResistAdditive) * modifiers.FrostResistMultiplicative);

        // Movement speed
        computed.EffectiveMoveSpeed = Math.Max(0f, (baseMoveSpeed + modifiers.MoveSpeedAdditive) * modifiers.MoveSpeedMultiplicative);

        computed.IsDirty = false;
        return computed;
    }

    /// <summary>
    /// Calculate effective attack cooldown from base cooldown and attack speed.
    /// </summary>
    public static float CalculateEffectiveCooldown(float baseCooldown, float effectiveAttackSpeed, float cooldownReduction)
    {
        // Apply cooldown reduction first
        var cooldownAfterCdr = baseCooldown * (1f - cooldownReduction);
        
        // Then apply attack speed (inverse relationship)
        var effectiveCooldown = cooldownAfterCdr / effectiveAttackSpeed;
        
        return Math.Max(0.05f, effectiveCooldown); // Minimum 0.05s cooldown
    }
}
