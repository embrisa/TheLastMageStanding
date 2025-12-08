using System.Collections.Generic;
using Xunit;
using TheLastMageStanding.Game.Core.Skills;

namespace TheLastMageStanding.Game.Tests.Skills;

public sealed class SkillDataTests
{
    [Fact]
    public void SkillModifiers_Zero_HasCorrectDefaults()
    {
        var modifiers = SkillModifiers.Zero;

        Assert.Equal(0f, modifiers.CooldownReductionAdditive);
        Assert.Equal(1f, modifiers.CooldownReductionMultiplicative);
        Assert.Equal(0f, modifiers.DamageAdditive);
        Assert.Equal(1f, modifiers.DamageMultiplicative);
        Assert.Equal(0, modifiers.ProjectileCountAdditive);
        Assert.Equal(0, modifiers.PierceCountAdditive);
    }

    [Fact]
    public void SkillModifiers_Combine_AddsAdditivesMultipliesMultiplicatives()
    {
        var modA = new SkillModifiers
        {
            DamageAdditive = 0.5f,
            DamageMultiplicative = 1.2f,
            CooldownReductionAdditive = 0.1f,
            ProjectileCountAdditive = 1
        };

        var modB = new SkillModifiers
        {
            DamageAdditive = 0.3f,
            DamageMultiplicative = 1.1f,
            CooldownReductionAdditive = 0.15f,
            ProjectileCountAdditive = 2
        };

        var combined = SkillModifiers.Combine(modA, modB);

        Assert.Equal(0.8f, combined.DamageAdditive); // 0.5 + 0.3
        Assert.Equal(1.32f, combined.DamageMultiplicative, 3); // 1.2 * 1.1
        Assert.Equal(0.25f, combined.CooldownReductionAdditive); // 0.1 + 0.15
        Assert.Equal(3, combined.ProjectileCountAdditive); // 1 + 2
    }

    [Fact]
    public void ComputedSkillStats_Calculate_AppliesStackingOrder()
    {
        var definition = new SkillDefinition(
            SkillId.Firebolt,
            "Firebolt",
            "Test",
            SkillElement.Fire,
            SkillDeliveryType.Projectile,
            SkillTargetType.Direction,
            baseCooldown: 1.0f,
            baseDamageMultiplier: 1.0f,
            range: 300f,
            projectileCount: 1
        );

        var modifiers = new SkillModifiers
        {
            DamageAdditive = 0.5f,  // +50% damage
            DamageMultiplicative = 1.2f,  // × 1.2
            CooldownReductionAdditive = 0.2f,  // -20% cooldown
            ProjectileCountAdditive = 2  // +2 projectiles
        };

        var stats = ComputedSkillStats.Calculate(definition, modifiers, globalCooldownReduction: 0f);

        // Damage: (1.0 + 0.5) * 1.2 = 1.8
        Assert.Equal(1.8f, stats.EffectiveDamageMultiplier, 3);

        // Cooldown: 1.0 * (1 - 0.2) = 0.8
        Assert.Equal(0.8f, stats.EffectiveCooldown, 3);

        // Projectiles: 1 + 2 = 3
        Assert.Equal(3, stats.EffectiveProjectileCount);
    }

    [Fact]
    public void ComputedSkillStats_Calculate_ClampsCooldownReduction()
    {
        var definition = new SkillDefinition(
            SkillId.Firebolt,
            "Firebolt",
            "Test",
            SkillElement.Fire,
            SkillDeliveryType.Projectile,
            SkillTargetType.Direction,
            baseCooldown: 1.0f,
            baseDamageMultiplier: 1.0f
        );

        var modifiers = new SkillModifiers
        {
            CooldownReductionAdditive = 0.9f  // 90% CDR should be clamped to 80%
        };

        var stats = ComputedSkillStats.Calculate(definition, modifiers, globalCooldownReduction: 0.1f);

        // Total CDR would be 100%, but should be clamped to 80%
        // 1.0 * (1 - 0.8) = 0.2
        Assert.Equal(0.2f, stats.EffectiveCooldown, 3);
    }

    [Fact]
    public void ComputedSkillStats_Calculate_ClampsMinimumCooldown()
    {
        var definition = new SkillDefinition(
            SkillId.Firebolt,
            "Firebolt",
            "Test",
            SkillElement.Fire,
            SkillDeliveryType.Projectile,
            SkillTargetType.Direction,
            baseCooldown: 0.05f,  // Very short base cooldown
            baseDamageMultiplier: 1.0f
        );

        var modifiers = new SkillModifiers
        {
            CooldownReductionAdditive = 0.8f  // Max CDR
        };

        var stats = ComputedSkillStats.Calculate(definition, modifiers, globalCooldownReduction: 0f);

        // Should be clamped to minimum 0.1s
        Assert.True(stats.EffectiveCooldown >= 0.1f);
    }

    [Fact]
    public void ComputedSkillStats_Calculate_AppliesGlobalCooldownReduction()
    {
        var definition = new SkillDefinition(
            SkillId.Firebolt,
            "Firebolt",
            "Test",
            SkillElement.Fire,
            SkillDeliveryType.Projectile,
            SkillTargetType.Direction,
            baseCooldown: 1.0f,
            baseDamageMultiplier: 1.0f
        );

        var modifiers = new SkillModifiers
        {
            CooldownReductionAdditive = 0.2f
        };

        var stats = ComputedSkillStats.Calculate(definition, modifiers, globalCooldownReduction: 0.3f);

        // 0.2 + 0.3 = 0.5 total CDR (50%)
        // 1.0 * (1 - 0.5) = 0.5
        Assert.Equal(0.5f, stats.EffectiveCooldown, 3);
    }

    [Fact]
    public void ComputedSkillStats_Calculate_MinimumProjectileCount()
    {
        var definition = new SkillDefinition(
            SkillId.Firebolt,
            "Firebolt",
            "Test",
            SkillElement.Fire,
            SkillDeliveryType.Projectile,
            SkillTargetType.Direction,
            baseCooldown: 1.0f,
            baseDamageMultiplier: 1.0f,
            projectileCount: 1
        );

        var modifiers = new SkillModifiers
        {
            ProjectileCountAdditive = -10  // Negative modifier shouldn't go below 1
        };

        var stats = ComputedSkillStats.Calculate(definition, modifiers, globalCooldownReduction: 0f);

        Assert.Equal(1, stats.EffectiveProjectileCount);
    }

    [Fact]
    public void ComputedSkillStats_Calculate_ScalesRange()
    {
        var definition = new SkillDefinition(
            SkillId.Firebolt,
            "Firebolt",
            "Test",
            SkillElement.Fire,
            SkillDeliveryType.Projectile,
            SkillTargetType.Direction,
            baseCooldown: 1.0f,
            baseDamageMultiplier: 1.0f,
            range: 300f
        );

        var modifiers = new SkillModifiers
        {
            RangeAdditive = 50f,  // +50 range
            RangeMultiplicative = 1.2f  // × 1.2
        };

        var stats = ComputedSkillStats.Calculate(definition, modifiers, globalCooldownReduction: 0f);

        // (300 + 50) * 1.2 = 420
        Assert.Equal(420f, stats.EffectiveRange, 3);
    }
}

public sealed class SkillRegistryTests
{
    [Fact]
    public void SkillRegistry_Constructor_RegistersDefaultSkills()
    {
        var registry = new SkillRegistry();

        var firebolt = registry.GetSkill(SkillId.Firebolt);
        Assert.NotNull(firebolt);
        Assert.Equal("Firebolt", firebolt.Name);
        Assert.Equal(SkillElement.Fire, firebolt.Element);
    }

    [Fact]
    public void SkillRegistry_GetSkillsByElement_FiltersCorrectly()
    {
        var registry = new SkillRegistry();

        var fireSkills = registry.GetSkillsByElement(SkillElement.Fire);
        var arcaneSkills = registry.GetSkillsByElement(SkillElement.Arcane);
        var frostSkills = registry.GetSkillsByElement(SkillElement.Frost);

        Assert.NotEmpty(fireSkills);
        Assert.NotEmpty(arcaneSkills);
        Assert.NotEmpty(frostSkills);

        foreach (var skill in fireSkills)
        {
            Assert.Equal(SkillElement.Fire, skill.Element);
        }
    }

    [Fact]
    public void SkillRegistry_GetSkill_ReturnsNullForInvalidId()
    {
        var registry = new SkillRegistry();

        var skill = registry.GetSkill(SkillId.None);
        Assert.Null(skill);
    }

    [Fact]
    public void SkillRegistry_RegisterSkill_CanAddCustomSkill()
    {
        var registry = new SkillRegistry();

        var customSkill = new SkillDefinition(
            (SkillId)9999,
            "Custom Skill",
            "Test custom skill",
            SkillElement.Arcane,
            SkillDeliveryType.Projectile,
            SkillTargetType.Direction,
            baseCooldown: 5f,
            baseDamageMultiplier: 2f
        );

        registry.RegisterSkill(customSkill);

        var retrieved = registry.GetSkill((SkillId)9999);
        Assert.NotNull(retrieved);
        Assert.Equal("Custom Skill", retrieved.Name);
    }
}
