using System;
using System.Collections.Generic;
using System.Linq;
using TheLastMageStanding.Game.Core.Config;
using TheLastMageStanding.Game.Core.Ecs.Components;

namespace TheLastMageStanding.Game.Core.Skills;

/// <summary>
/// Central registry for all skill definitions.
/// Provides lookup by ID and element filtering.
/// </summary>
public sealed class SkillRegistry
{
    private readonly Dictionary<SkillId, SkillDefinition> _skills;

    public SkillRegistry()
    {
        _skills = new Dictionary<SkillId, SkillDefinition>();
        RegisterDefaultMageSkills();
    }

    /// <summary>
    /// Get a skill definition by ID.
    /// </summary>
    public SkillDefinition? GetSkill(SkillId id)
    {
        return _skills.TryGetValue(id, out var skill) ? skill : null;
    }

    /// <summary>
    /// Get all skills for a specific element.
    /// </summary>
    public IEnumerable<SkillDefinition> GetSkillsByElement(SkillElement element)
    {
        return _skills.Values.Where(s => s.Element == element);
    }

    /// <summary>
    /// Get all registered skills.
    /// </summary>
    public IEnumerable<SkillDefinition> GetAllSkills()
    {
        return _skills.Values;
    }

    /// <summary>
    /// Register a skill definition.
    /// </summary>
    public void RegisterSkill(SkillDefinition skill)
    {
        _skills[skill.Id] = skill;
    }

    private void RegisterDefaultMageSkills()
    {
        // ===== FIRE SKILLS =====
        
        RegisterSkill(new SkillDefinition(
            id: SkillId.Firebolt,
            name: "Firebolt",
            description: "Launch a bolt of fire that burns enemies",
            element: SkillElement.Fire,
            deliveryType: SkillDeliveryType.Projectile,
            targetType: SkillTargetType.Direction,
            baseCooldown: 0.4f,
            baseDamageMultiplier: 1.0f,
            range: 350f,
            projectileSpeed: 600f,
            canCrit: true,
            onHitStatusEffect: StatusEffectConfig.CreateBurn(potency: 3f, duration: 2f),
            statusEffectApplicationChance: 0.3f
        ));

        RegisterSkill(new SkillDefinition(
            id: SkillId.Fireball,
            name: "Fireball",
            description: "Hurl a massive fireball that explodes on impact",
            element: SkillElement.Fire,
            deliveryType: SkillDeliveryType.Projectile,
            targetType: SkillTargetType.Direction,
            baseCooldown: 2.0f,
            baseDamageMultiplier: 3.5f,
            range: 400f,
            aoeRadius: 80f,
            projectileSpeed: 350f,
            castTime: 0.2f,
            canCrit: true,
            onHitStatusEffect: StatusEffectConfig.CreateBurn(potency: 5f, duration: 4f),
            statusEffectApplicationChance: 0.8f
        ));

        RegisterSkill(new SkillDefinition(
            id: SkillId.FlameWave,
            name: "Flame Wave",
            description: "Unleash a wave of fire in all directions",
            element: SkillElement.Fire,
            deliveryType: SkillDeliveryType.AreaOfEffect,
            targetType: SkillTargetType.Self,
            baseCooldown: 5.0f,
            baseDamageMultiplier: 2.0f,
            range: 0f,
            aoeRadius: 180f,
            castTime: 0.15f,
            canCrit: true,
            onHitStatusEffect: StatusEffectConfig.CreateBurn(potency: 4f, duration: 3f),
            statusEffectApplicationChance: 1.0f
        ));

        // ===== ARCANE SKILLS =====
        
        RegisterSkill(new SkillDefinition(
            id: SkillId.ArcaneMissile,
            name: "Arcane Missile",
            description: "Fire a homing arcane projectile",
            element: SkillElement.Arcane,
            deliveryType: SkillDeliveryType.Projectile,
            targetType: SkillTargetType.Nearest,
            baseCooldown: 0.7f,
            baseDamageMultiplier: 1.2f,
            range: 400f,
            projectileSpeed: 550f,
            canCrit: true
        ));

        RegisterSkill(new SkillDefinition(
            id: SkillId.ArcaneBurst,
            name: "Arcane Burst",
            description: "Create an explosion of arcane energy",
            element: SkillElement.Arcane,
            deliveryType: SkillDeliveryType.AreaOfEffect,
            targetType: SkillTargetType.Self,
            baseCooldown: 3.0f,
            baseDamageMultiplier: 2.5f,
            range: 0f,
            aoeRadius: 120f,
            castTime: 0.1f,
            canCrit: true
        ));

        RegisterSkill(new SkillDefinition(
            id: SkillId.ArcaneBarrage,
            name: "Arcane Barrage",
            description: "Launch multiple arcane missiles in rapid succession",
            element: SkillElement.Arcane,
            deliveryType: SkillDeliveryType.Projectile,
            targetType: SkillTargetType.Direction,
            baseCooldown: 4.0f,
            baseDamageMultiplier: 0.8f,
            range: 350f,
            projectileCount: 5,
            projectileSpeed: 600f,
            castTime: 0.4f,
            canCrit: true
        ));

        // ===== FROST SKILLS =====
        
        RegisterSkill(new SkillDefinition(
            id: SkillId.FrostBolt,
            name: "Frost Bolt",
            description: "Launch a bolt of frost that chills enemies",
            element: SkillElement.Frost,
            deliveryType: SkillDeliveryType.Projectile,
            targetType: SkillTargetType.Direction,
            baseCooldown: 0.6f,
            baseDamageMultiplier: 0.9f,
            range: 350f,
            projectileSpeed: 500f,
            canCrit: true,
            onHitStatusEffect: StatusEffectConfig.CreateSlow(potency: 0.5f, duration: 2.0f),
            statusEffectApplicationChance: 0.5f
        ));

        RegisterSkill(new SkillDefinition(
            id: SkillId.FrostNova,
            name: "Frost Nova",
            description: "Freeze all nearby enemies in place",
            element: SkillElement.Frost,
            deliveryType: SkillDeliveryType.AreaOfEffect,
            targetType: SkillTargetType.Self,
            baseCooldown: 7.0f,
            baseDamageMultiplier: 1.5f,
            range: 0f,
            aoeRadius: 140f,
            castTime: 0.1f,
            canCrit: true,
            onHitStatusEffect: StatusEffectConfig.CreateFreeze(potency: 0.7f, duration: 2f),
            statusEffectApplicationChance: 1.0f
        ));

        RegisterSkill(new SkillDefinition(
            id: SkillId.Blizzard,
            name: "Blizzard",
            description: "Summon a blizzard that continuously damages enemies",
            element: SkillElement.Frost,
            deliveryType: SkillDeliveryType.AreaOfEffect,
            targetType: SkillTargetType.GroundTarget,
            baseCooldown: 10.0f,
            baseDamageMultiplier: 4.0f,
            range: 500f,
            aoeRadius: 180f,
            castTime: 0.3f,
            canCrit: true,
            onHitStatusEffect: StatusEffectConfig.CreateSlow(potency: 0.6f, duration: 1.0f),
            statusEffectApplicationChance: 1.0f
        ));
    }
}
