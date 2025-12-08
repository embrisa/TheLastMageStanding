using System;
using System.Linq;
using Microsoft.Xna.Framework;
using TheLastMageStanding.Game.Core.Ecs;
using TheLastMageStanding.Game.Core.Ecs.Components;
using TheLastMageStanding.Game.Core.Skills;

namespace TheLastMageStanding.Game.Core.Debug;

/// <summary>
/// Debug utility for inspecting and testing skills.
/// </summary>
internal static class SkillDebugHelper
{
    /// <summary>
    /// Force cast a skill for testing (bypasses cooldown and resource checks).
    /// </summary>
    public static void DebugCastSkill(EcsWorld world, Entity caster, SkillId skillId, Vector2 direction)
    {
        if (!world.TryGetComponent(caster, out Position position))
        {
            return;
        }

        var targetPosition = position.Value + direction * 300f;
        
        world.EventBus.Publish(new SkillCastRequestEvent(
            caster,
            skillId,
            targetPosition,
            direction));
    }

    /// <summary>
    /// Reset all cooldowns for an entity.
    /// </summary>
    public static void ResetAllCooldowns(EcsWorld world, Entity entity)
    {
        if (world.TryGetComponent(entity, out SkillCooldowns cooldowns))
        {
            var keys = cooldowns.Cooldowns.Keys.ToList();
            foreach (var skillId in keys)
            {
                cooldowns.Cooldowns[skillId] = 0f;
            }
            world.SetComponent(entity, cooldowns);
        }
    }

    /// <summary>
    /// Get a formatted string showing skill info and cooldowns.
    /// </summary>
    public static string InspectSkills(EcsWorld world, Entity entity, SkillRegistry registry)
    {
        if (!world.TryGetComponent(entity, out EquippedSkills equipped) ||
            !world.TryGetComponent(entity, out SkillCooldowns cooldowns))
        {
            return "Entity has no skill components";
        }

        var result = "=== Equipped Skills ===\n";
        
        // Primary skill
        result += $"Primary: {equipped.PrimarySkill}\n";
        var primaryDef = registry.GetSkill(equipped.PrimarySkill);
        if (primaryDef != null)
        {
            var cooldown = cooldowns.GetCooldown(equipped.PrimarySkill);
            result += $"  Cooldown: {cooldown:F2}s / {primaryDef.BaseCooldown:F2}s\n";
            result += $"  Damage Mult: {primaryDef.BaseDamageMultiplier:F2}\n";
        }

        // Hotkey skills
        for (int i = 1; i <= 4; i++)
        {
            var skillId = equipped.GetSkill(i);
            if (skillId != SkillId.None)
            {
                result += $"Hotkey {i}: {skillId}\n";
                var def = registry.GetSkill(skillId);
                if (def != null)
                {
                    var cooldown = cooldowns.GetCooldown(skillId);
                    result += $"  Cooldown: {cooldown:F2}s / {def.BaseCooldown:F2}s\n";
                }
            }
        }

        // Modifiers
        if (world.TryGetComponent(entity, out PlayerSkillModifiers modifiers))
        {
            result += "\n=== Global Modifiers ===\n";
            result += $"Damage: +{modifiers.GlobalModifiers.DamageAdditive:F2} × {modifiers.GlobalModifiers.DamageMultiplicative:F2}\n";
            result += $"CDR: {modifiers.GlobalModifiers.CooldownReductionAdditive:P0}\n";
            result += $"Projectile Count: +{modifiers.GlobalModifiers.ProjectileCountAdditive}\n";
            result += $"Pierce: +{modifiers.GlobalModifiers.PierceCountAdditive}\n";
        }

        return result;
    }

    /// <summary>
    /// Equip a skill to a hotkey slot for testing.
    /// </summary>
    public static void EquipSkill(EcsWorld world, Entity entity, int slot, SkillId skillId)
    {
        if (!world.TryGetComponent(entity, out EquippedSkills equipped))
        {
            equipped = new EquippedSkills();
        }

        equipped.SetSkill(slot, skillId);
        world.SetComponent(entity, equipped);
    }

    /// <summary>
    /// Apply test modifiers to skills for debugging.
    /// </summary>
    public static void ApplyTestModifiers(EcsWorld world, Entity entity)
    {
        if (!world.TryGetComponent(entity, out PlayerSkillModifiers modifiers))
        {
            modifiers = new PlayerSkillModifiers();
        }

        // Add some test bonuses
        var global = modifiers.GlobalModifiers;
        global.DamageAdditive += 0.5f; // +50% damage
        global.CooldownReductionAdditive += 0.2f; // +20% CDR
        global.ProjectileCountAdditive += 1; // +1 projectile
        global.PierceCountAdditive += 1; // +1 pierce

        modifiers.GlobalModifiers = global;
        modifiers.IsDirty = true;
        world.SetComponent(entity, modifiers);
    }

    /// <summary>
    /// Calculate and display effective skill stats.
    /// </summary>
    public static string ShowEffectiveStats(
        SkillDefinition definition,
        in SkillModifiers skillModifiers,
        float globalCdr = 0f)
    {
        var stats = ComputedSkillStats.Calculate(definition, skillModifiers, globalCdr);
        
        var result = $"=== Effective Stats for {definition.Name} ===\n";
        result += $"Cooldown: {stats.EffectiveCooldown:F2}s (base: {definition.BaseCooldown:F2}s)\n";
        result += $"Damage Mult: {stats.EffectiveDamageMultiplier:F2} (base: {definition.BaseDamageMultiplier:F2})\n";
        result += $"Range: {stats.EffectiveRange:F1} (base: {definition.Range:F1})\n";
        result += $"Projectiles: {stats.EffectiveProjectileCount} (base: {definition.ProjectileCount})\n";
        result += $"Projectile Speed: {stats.EffectiveProjectileSpeed:F1}\n";
        
        if (stats.EffectiveAoeRadius > 0f)
        {
            result += $"AoE Radius: {stats.EffectiveAoeRadius:F1}\n";
        }
        
        if (stats.EffectivePierceCount > 0)
        {
            result += $"Pierce: {stats.EffectivePierceCount}\n";
        }
        
        if (stats.EffectiveChainCount > 0)
        {
            result += $"Chain: {stats.EffectiveChainCount}\n";
        }
        
        if (stats.EffectiveCastTime > 0f)
        {
            result += $"Cast Time: {stats.EffectiveCastTime:F2}s\n";
        }
        
        return result;
    }
}
