using Microsoft.Xna.Framework;
using System;

namespace TheLastMageStanding.Game.Core.Ecs.Components;

/// <summary>
/// Core offensive stats that determine damage output.
/// Replaces the legacy Damage field in AttackStats with a comprehensive stat model.
/// </summary>
internal struct OffensiveStats
{
    /// <summary>
    /// Base damage multiplier. Applies to all damage dealt.
    /// Default: 1.0 (no bonus)
    /// </summary>
    public float Power { get; set; }

    /// <summary>
    /// Attack speed multiplier. Affects attack cooldown (higher = faster).
    /// Default: 1.0 (normal speed)
    /// </summary>
    public float AttackSpeed { get; set; }

    /// <summary>
    /// Critical hit chance (0.0 to 1.0).
    /// Default: 0.0 (no crits)
    /// </summary>
    public float CritChance { get; set; }

    /// <summary>
    /// Critical hit damage multiplier.
    /// Default: 1.5 (150% damage on crit)
    /// </summary>
    public float CritMultiplier { get; set; }

    /// <summary>
    /// Cooldown reduction multiplier (0.0 to 1.0).
    /// Applied to ability cooldowns. 0.3 = 30% cooldown reduction.
    /// Default: 0.0 (no reduction)
    /// </summary>
    public float CooldownReduction { get; set; }

    public static OffensiveStats Default => new()
    {
        Power = 1.0f,
        AttackSpeed = 1.0f,
        CritChance = 0.0f,
        CritMultiplier = 1.5f,
        CooldownReduction = 0.0f
    };

    public OffensiveStats()
    {
        Power = 1.0f;
        AttackSpeed = 1.0f;
        CritChance = 0.0f;
        CritMultiplier = 1.5f;
        CooldownReduction = 0.0f;
    }
}

/// <summary>
/// Defensive stats that reduce incoming damage.
/// </summary>
internal struct DefensiveStats
{
    /// <summary>
    /// Physical damage reduction. Each point reduces physical damage by a diminishing amount.
    /// Formula: reduction = armor / (armor + 100)
    /// Default: 0.0
    /// </summary>
    public float Armor { get; set; }

    /// <summary>
    /// Arcane damage reduction. Each point reduces arcane damage by a diminishing amount.
    /// Formula: reduction = resist / (resist + 100)
    /// Default: 0.0
    /// </summary>
    public float ArcaneResist { get; set; }

    /// <summary>
    /// Fire damage/status resistance. Uses diminishing returns.
    /// </summary>
    public float FireResist { get; set; }

    /// <summary>
    /// Frost damage/status resistance. Uses diminishing returns.
    /// </summary>
    public float FrostResist { get; set; }

    /// <summary>
    /// Nature/poison resistance. Uses diminishing returns.
    /// </summary>
    public float NatureResist { get; set; }

    public static DefensiveStats Default => new()
    {
        Armor = 0.0f,
        ArcaneResist = 0.0f,
        FireResist = 0.0f,
        FrostResist = 0.0f,
        NatureResist = 0.0f
    };

    public DefensiveStats()
    {
        Armor = 0.0f;
        ArcaneResist = 0.0f;
        FireResist = 0.0f;
        FrostResist = 0.0f;
        NatureResist = 0.0f;
    }
}

/// <summary>
/// Base movement speed (unmodified). Used to ensure recalculations
/// can restore move speed after temporary modifiers.
/// </summary>
internal struct BaseMoveSpeed
{
    public BaseMoveSpeed(float value) => Value = value;
    public float Value { get; set; }
}

/// <summary>
/// Stat modifiers that can be applied temporarily or permanently.
/// Uses clear stacking order: base → additive → multiplicative
/// </summary>
internal struct StatModifiers
{
    // Offensive modifiers
    public float PowerAdditive { get; set; }
    public float PowerMultiplicative { get; set; }
    public float AttackSpeedAdditive { get; set; }
    public float AttackSpeedMultiplicative { get; set; }
    public float CritChanceAdditive { get; set; }
    public float CritMultiplierAdditive { get; set; }
    public float CooldownReductionAdditive { get; set; }

    // Defensive modifiers
    public float ArmorAdditive { get; set; }
    public float ArmorMultiplicative { get; set; }
    public float ArcaneResistAdditive { get; set; }
    public float ArcaneResistMultiplicative { get; set; }
    public float FireResistAdditive { get; set; }
    public float FireResistMultiplicative { get; set; }
    public float FrostResistAdditive { get; set; }
    public float FrostResistMultiplicative { get; set; }
    public float NatureResistAdditive { get; set; }
    public float NatureResistMultiplicative { get; set; }

    // Movement modifiers
    public float MoveSpeedAdditive { get; set; }
    public float MoveSpeedMultiplicative { get; set; }

    public static StatModifiers Zero => new()
    {
        PowerAdditive = 0f,
        PowerMultiplicative = 1f,
        AttackSpeedAdditive = 0f,
        AttackSpeedMultiplicative = 1f,
        CritChanceAdditive = 0f,
        CritMultiplierAdditive = 0f,
        CooldownReductionAdditive = 0f,
        ArmorAdditive = 0f,
        ArmorMultiplicative = 1f,
        ArcaneResistAdditive = 0f,
        ArcaneResistMultiplicative = 1f,
        FireResistAdditive = 0f,
        FireResistMultiplicative = 1f,
        FrostResistAdditive = 0f,
        FrostResistMultiplicative = 1f,
        NatureResistAdditive = 0f,
        NatureResistMultiplicative = 1f,
        MoveSpeedAdditive = 0f,
        MoveSpeedMultiplicative = 1f
    };

    public StatModifiers()
    {
        PowerAdditive = 0f;
        PowerMultiplicative = 1f;
        AttackSpeedAdditive = 0f;
        AttackSpeedMultiplicative = 1f;
        CritChanceAdditive = 0f;
        CritMultiplierAdditive = 0f;
        CooldownReductionAdditive = 0f;
        ArmorAdditive = 0f;
        ArmorMultiplicative = 1f;
        ArcaneResistAdditive = 0f;
        ArcaneResistMultiplicative = 1f;
        FireResistAdditive = 0f;
        FireResistMultiplicative = 1f;
        FrostResistAdditive = 0f;
        FrostResistMultiplicative = 1f;
        NatureResistAdditive = 0f;
        NatureResistMultiplicative = 1f;
        MoveSpeedAdditive = 0f;
        MoveSpeedMultiplicative = 1f;
    }

    /// <summary>
    /// Combines multiple modifier sets using standard stacking rules.
    /// </summary>
    public static StatModifiers Combine(params StatModifiers[] modifiers)
    {
        var result = Zero;
        foreach (var mod in modifiers)
        {
            // Additive bonuses stack additively
            result.PowerAdditive += mod.PowerAdditive;
            result.AttackSpeedAdditive += mod.AttackSpeedAdditive;
            result.CritChanceAdditive += mod.CritChanceAdditive;
            result.CritMultiplierAdditive += mod.CritMultiplierAdditive;
            result.CooldownReductionAdditive += mod.CooldownReductionAdditive;
            result.ArmorAdditive += mod.ArmorAdditive;
            result.ArcaneResistAdditive += mod.ArcaneResistAdditive;
            result.FireResistAdditive += mod.FireResistAdditive;
            result.FrostResistAdditive += mod.FrostResistAdditive;
            result.NatureResistAdditive += mod.NatureResistAdditive;
            result.MoveSpeedAdditive += mod.MoveSpeedAdditive;

            // Multiplicative bonuses stack multiplicatively
            result.PowerMultiplicative *= mod.PowerMultiplicative;
            result.AttackSpeedMultiplicative *= mod.AttackSpeedMultiplicative;
            result.ArmorMultiplicative *= mod.ArmorMultiplicative;
            result.ArcaneResistMultiplicative *= mod.ArcaneResistMultiplicative;
            result.FireResistMultiplicative *= mod.FireResistMultiplicative;
            result.FrostResistMultiplicative *= mod.FrostResistMultiplicative;
            result.NatureResistMultiplicative *= mod.NatureResistMultiplicative;
            result.MoveSpeedMultiplicative *= mod.MoveSpeedMultiplicative;
        }
        return result;
    }
}

/// <summary>
/// Cached computed stats to avoid recalculating every frame.
/// Invalidated when equipment/perks change.
/// </summary>
internal struct ComputedStats
{
    public float EffectivePower { get; set; }
    public float EffectiveAttackSpeed { get; set; }
    public float EffectiveCritChance { get; set; }
    public float EffectiveCritMultiplier { get; set; }
    public float EffectiveCooldownReduction { get; set; }
    public float EffectiveArmor { get; set; }
    public float EffectiveArcaneResist { get; set; }
    public float EffectiveFireResist { get; set; }
    public float EffectiveFrostResist { get; set; }
    public float EffectiveNatureResist { get; set; }
    public float EffectiveMoveSpeed { get; set; }

    /// <summary>
    /// Marks this cache as dirty and needing recalculation.
    /// </summary>
    public bool IsDirty { get; set; }

    public ComputedStats()
    {
        EffectivePower = 1.0f;
        EffectiveAttackSpeed = 1.0f;
        EffectiveCritChance = 0.0f;
        EffectiveCritMultiplier = 1.5f;
        EffectiveCooldownReduction = 0.0f;
        EffectiveArmor = 0.0f;
        EffectiveArcaneResist = 0.0f;
        EffectiveFireResist = 0.0f;
        EffectiveFrostResist = 0.0f;
        EffectiveNatureResist = 0.0f;
        EffectiveMoveSpeed = 0.0f;
        IsDirty = true;
    }

    public static void MarkDirty(ref ComputedStats stats)
    {
        stats.IsDirty = true;
    }
}

/// <summary>
/// Component holding stat modifiers from perks.
/// </summary>
internal struct PerkModifiers
{
    public StatModifiers Value { get; set; }
}

/// <summary>
/// Component holding stat modifiers from equipment.
/// </summary>
internal struct EquipmentModifiers
{
    public StatModifiers Value { get; set; }
}

/// <summary>
/// Component storing modifiers derived from status effects (burn/slow/etc).
/// </summary>
internal struct StatusEffectModifiers
{
    public StatModifiers Value { get; set; }
}

/// <summary>
/// Aggregate stat modifiers granted by in-run level-up choices.
/// </summary>
internal struct LevelUpStatModifiers
{
    public StatModifiers Value { get; set; }
}
