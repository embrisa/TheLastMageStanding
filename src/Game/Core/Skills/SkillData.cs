using System;
using System.Collections.Generic;
using TheLastMageStanding.Game.Core.Ecs.Components;

namespace TheLastMageStanding.Game.Core.Skills;

/// <summary>
/// Unique identifier for skills across all classes.
/// </summary>
public enum SkillId
{
    None = 0,
    
    // Fire Skills
    Firebolt = 100,
    Fireball = 101,
    FlameWave = 102,
    
    // Arcane Skills
    ArcaneMissile = 200,
    ArcaneBurst = 201,
    ArcaneBarrage = 202,
    
    // Frost Skills
    FrostBolt = 300,
    FrostNova = 301,
    Blizzard = 302
}

/// <summary>
/// Elemental type for skill theming and future interactions.
/// </summary>
public enum SkillElement
{
    None = 0,
    Fire = 1,
    Arcane = 2,
    Frost = 3
}

/// <summary>
/// Delivery mechanism for skill effects.
/// </summary>
public enum SkillDeliveryType
{
    Projectile = 0,     // Travels to target
    AreaOfEffect = 1,   // Instant radius around point
    Beam = 2,           // Continuous line effect
    Melee = 3           // Close-range hitbox
}

/// <summary>
/// Targeting mode for skill activation.
/// </summary>
public enum SkillTargetType
{
    Direction = 0,      // Fires in movement direction
    Nearest = 1,        // Auto-targets nearest enemy
    GroundTarget = 2,   // Click to target location
    Self = 3            // Centered on caster
}

/// <summary>
/// Base definition of a skill with metadata and scaling parameters.
/// Immutable after creation to ensure consistency.
/// </summary>
public sealed class SkillDefinition
{
    /// <summary>
    /// Unique identifier for this skill.
    /// </summary>
    public SkillId Id { get; }
    
    /// <summary>
    /// Display name for UI.
    /// </summary>
    public string Name { get; }
    
    /// <summary>
    /// Short description of what the skill does.
    /// </summary>
    public string Description { get; }
    
    /// <summary>
    /// Elemental type for theming and future elemental interactions.
    /// </summary>
    public SkillElement Element { get; }
    
    /// <summary>
    /// How the skill delivers its effect.
    /// </summary>
    public SkillDeliveryType DeliveryType { get; }
    
    /// <summary>
    /// Targeting mode for activation.
    /// </summary>
    public SkillTargetType TargetType { get; }
    
    /// <summary>
    /// Base cooldown in seconds (before modifiers).
    /// </summary>
    public float BaseCooldown { get; }
    
    /// <summary>
    /// Base damage multiplier (scales with Power stat).
    /// </summary>
    public float BaseDamageMultiplier { get; }
    
    /// <summary>
    /// Resource cost (mana/energy, future implementation).
    /// </summary>
    public float ResourceCost { get; }
    
    /// <summary>
    /// Range in world units (for projectiles or AoE).
    /// </summary>
    public float Range { get; }
    
    /// <summary>
    /// Area of effect radius (if applicable).
    /// </summary>
    public float AoeRadius { get; }
    
    /// <summary>
    /// Number of projectiles/hits (for multi-shot or pierce).
    /// </summary>
    public int ProjectileCount { get; }
    
    /// <summary>
    /// Projectile speed in units per second (if applicable).
    /// </summary>
    public float ProjectileSpeed { get; }
    
    /// <summary>
    /// Cast time/windup in seconds (0 = instant).
    /// </summary>
    public float CastTime { get; }
    
    /// <summary>
    /// Whether this skill can critically hit.
    /// </summary>
    public bool CanCrit { get; }

    /// <summary>
    /// Optional status effect applied on hit.
    /// </summary>
    public StatusEffectData? OnHitStatusEffect { get; }

    /// <summary>
    /// Chance to apply the status effect (0-1).
    /// </summary>
    public float StatusEffectApplicationChance { get; }

    public SkillDefinition(
        SkillId id,
        string name,
        string description,
        SkillElement element,
        SkillDeliveryType deliveryType,
        SkillTargetType targetType,
        float baseCooldown,
        float baseDamageMultiplier,
        float resourceCost = 0f,
        float range = 300f,
        float aoeRadius = 0f,
        int projectileCount = 1,
        float projectileSpeed = 400f,
        float castTime = 0f,
        bool canCrit = true,
        StatusEffectData? onHitStatusEffect = null,
        float statusEffectApplicationChance = 1f)
    {
        Id = id;
        Name = name;
        Description = description;
        Element = element;
        DeliveryType = deliveryType;
        TargetType = targetType;
        BaseCooldown = baseCooldown;
        BaseDamageMultiplier = baseDamageMultiplier;
        ResourceCost = resourceCost;
        Range = range;
        AoeRadius = aoeRadius;
        ProjectileCount = projectileCount;
        ProjectileSpeed = projectileSpeed;
        CastTime = castTime;
        CanCrit = canCrit;
        OnHitStatusEffect = onHitStatusEffect;
        StatusEffectApplicationChance = statusEffectApplicationChance;
    }
}

/// <summary>
/// Modifiers that can be applied to skills from talents, perks, or equipment.
/// Uses deterministic stacking: additive first, then multiplicative.
/// </summary>
public struct SkillModifiers
{
    // Cooldown modifiers
    public float CooldownReductionAdditive { get; set; }
    public float CooldownReductionMultiplicative { get; set; }
    
    // Damage modifiers
    public float DamageAdditive { get; set; }
    public float DamageMultiplicative { get; set; }
    
    // Range modifiers
    public float RangeAdditive { get; set; }
    public float RangeMultiplicative { get; set; }
    
    // AoE modifiers
    public float AoeRadiusAdditive { get; set; }
    public float AoeRadiusMultiplicative { get; set; }
    
    // Resource modifiers
    public float ResourceCostAdditive { get; set; }
    public float ResourceCostMultiplicative { get; set; }
    
    // Projectile modifiers
    public int ProjectileCountAdditive { get; set; }
    public float ProjectileSpeedAdditive { get; set; }
    public float ProjectileSpeedMultiplicative { get; set; }
    
    // Pierce and chain
    public int PierceCountAdditive { get; set; }
    public int ChainCountAdditive { get; set; }
    
    // Cast time modifiers
    public float CastTimeReductionAdditive { get; set; }
    public float CastTimeReductionMultiplicative { get; set; }

    public static SkillModifiers Zero => new()
    {
        CooldownReductionAdditive = 0f,
        CooldownReductionMultiplicative = 1f,
        DamageAdditive = 0f,
        DamageMultiplicative = 1f,
        RangeAdditive = 0f,
        RangeMultiplicative = 1f,
        AoeRadiusAdditive = 0f,
        AoeRadiusMultiplicative = 1f,
        ResourceCostAdditive = 0f,
        ResourceCostMultiplicative = 1f,
        ProjectileCountAdditive = 0,
        ProjectileSpeedAdditive = 0f,
        ProjectileSpeedMultiplicative = 1f,
        PierceCountAdditive = 0,
        ChainCountAdditive = 0,
        CastTimeReductionAdditive = 0f,
        CastTimeReductionMultiplicative = 1f
    };

    public SkillModifiers()
    {
        CooldownReductionAdditive = 0f;
        CooldownReductionMultiplicative = 1f;
        DamageAdditive = 0f;
        DamageMultiplicative = 1f;
        RangeAdditive = 0f;
        RangeMultiplicative = 1f;
        AoeRadiusAdditive = 0f;
        AoeRadiusMultiplicative = 1f;
        ResourceCostAdditive = 0f;
        ResourceCostMultiplicative = 1f;
        ProjectileCountAdditive = 0;
        ProjectileSpeedAdditive = 0f;
        ProjectileSpeedMultiplicative = 1f;
        PierceCountAdditive = 0;
        ChainCountAdditive = 0;
        CastTimeReductionAdditive = 0f;
        CastTimeReductionMultiplicative = 1f;
    }

    /// <summary>
    /// Combine two modifier sets using deterministic stacking.
    /// Additives stack additively, multiplicatives stack multiplicatively.
    /// </summary>
    public static SkillModifiers Combine(in SkillModifiers a, in SkillModifiers b)
    {
        return new SkillModifiers
        {
            CooldownReductionAdditive = a.CooldownReductionAdditive + b.CooldownReductionAdditive,
            CooldownReductionMultiplicative = a.CooldownReductionMultiplicative * b.CooldownReductionMultiplicative,
            DamageAdditive = a.DamageAdditive + b.DamageAdditive,
            DamageMultiplicative = a.DamageMultiplicative * b.DamageMultiplicative,
            RangeAdditive = a.RangeAdditive + b.RangeAdditive,
            RangeMultiplicative = a.RangeMultiplicative * b.RangeMultiplicative,
            AoeRadiusAdditive = a.AoeRadiusAdditive + b.AoeRadiusAdditive,
            AoeRadiusMultiplicative = a.AoeRadiusMultiplicative * b.AoeRadiusMultiplicative,
            ResourceCostAdditive = a.ResourceCostAdditive + b.ResourceCostAdditive,
            ResourceCostMultiplicative = a.ResourceCostMultiplicative * b.ResourceCostMultiplicative,
            ProjectileCountAdditive = a.ProjectileCountAdditive + b.ProjectileCountAdditive,
            ProjectileSpeedAdditive = a.ProjectileSpeedAdditive + b.ProjectileSpeedAdditive,
            ProjectileSpeedMultiplicative = a.ProjectileSpeedMultiplicative * b.ProjectileSpeedMultiplicative,
            PierceCountAdditive = a.PierceCountAdditive + b.PierceCountAdditive,
            ChainCountAdditive = a.ChainCountAdditive + b.ChainCountAdditive,
            CastTimeReductionAdditive = a.CastTimeReductionAdditive + b.CastTimeReductionAdditive,
            CastTimeReductionMultiplicative = a.CastTimeReductionMultiplicative * b.CastTimeReductionMultiplicative
        };
    }
}

/// <summary>
/// Computed skill properties after applying all modifiers.
/// </summary>
public struct ComputedSkillStats
{
    public float EffectiveCooldown { get; set; }
    public float EffectiveDamageMultiplier { get; set; }
    public float EffectiveRange { get; set; }
    public float EffectiveAoeRadius { get; set; }
    public float EffectiveResourceCost { get; set; }
    public int EffectiveProjectileCount { get; set; }
    public float EffectiveProjectileSpeed { get; set; }
    public int EffectivePierceCount { get; set; }
    public int EffectiveChainCount { get; set; }
    public float EffectiveCastTime { get; set; }
    
    /// <summary>
    /// Calculate effective skill stats from base definition and modifiers.
    /// Applies stacking order: base → additive → multiplicative → clamps.
    /// </summary>
    public static ComputedSkillStats Calculate(
        in SkillDefinition definition,
        in SkillModifiers modifiers,
        float globalCooldownReduction = 0f)
    {
        // Cooldown: affected by both skill-specific and global CDR
        var totalCdr = modifiers.CooldownReductionAdditive + globalCooldownReduction;
        totalCdr = Math.Clamp(totalCdr, 0f, 0.8f); // Max 80% CDR
        var cooldown = definition.BaseCooldown * (1f - totalCdr) * modifiers.CooldownReductionMultiplicative;
        cooldown = Math.Max(0.1f, cooldown); // Minimum 0.1s cooldown
        
        // Damage
        var damage = (definition.BaseDamageMultiplier + modifiers.DamageAdditive) * modifiers.DamageMultiplicative;
        damage = Math.Max(0.1f, damage); // Minimum damage multiplier
        
        // Range
        var range = (definition.Range + modifiers.RangeAdditive) * modifiers.RangeMultiplicative;
        range = Math.Max(10f, range); // Minimum range
        
        // AoE
        var aoeRadius = (definition.AoeRadius + modifiers.AoeRadiusAdditive) * modifiers.AoeRadiusMultiplicative;
        aoeRadius = Math.Max(0f, aoeRadius);
        
        // Resource cost
        var resourceCost = (definition.ResourceCost + modifiers.ResourceCostAdditive) * modifiers.ResourceCostMultiplicative;
        resourceCost = Math.Max(0f, resourceCost);
        
        // Projectile count
        var projectileCount = Math.Max(1, definition.ProjectileCount + modifiers.ProjectileCountAdditive);
        
        // Projectile speed
        var projectileSpeed = (definition.ProjectileSpeed + modifiers.ProjectileSpeedAdditive) * modifiers.ProjectileSpeedMultiplicative;
        projectileSpeed = Math.Max(50f, projectileSpeed);
        
        // Pierce and chain
        var pierceCount = Math.Max(0, modifiers.PierceCountAdditive);
        var chainCount = Math.Max(0, modifiers.ChainCountAdditive);
        
        // Cast time
        var castTimeCdr = Math.Clamp(modifiers.CastTimeReductionAdditive, 0f, 0.9f); // Max 90% cast time reduction
        var castTime = definition.CastTime * (1f - castTimeCdr) * modifiers.CastTimeReductionMultiplicative;
        castTime = Math.Max(0f, castTime);

        return new ComputedSkillStats
        {
            EffectiveCooldown = cooldown,
            EffectiveDamageMultiplier = damage,
            EffectiveRange = range,
            EffectiveAoeRadius = aoeRadius,
            EffectiveResourceCost = resourceCost,
            EffectiveProjectileCount = projectileCount,
            EffectiveProjectileSpeed = projectileSpeed,
            EffectivePierceCount = pierceCount,
            EffectiveChainCount = chainCount,
            EffectiveCastTime = castTime
        };
    }
}
