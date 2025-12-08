using System;
using Xunit;
using TheLastMageStanding.Game.Core.Combat;
using TheLastMageStanding.Game.Core.Ecs.Components;

namespace TheLastMageStanding.Game.Tests.Combat;

public class DamageCalculatorTests
{
    [Fact]
    public void CalculateDamage_BaseDamage_NoModifiers()
    {
        var rng = new CombatRng(12345);
        var calculator = new DamageCalculator(rng);

        var damageInfo = new DamageInfo(100f, DamageType.Physical, DamageFlags.None);
        var attackerStats = OffensiveStats.Default;
        var defenderStats = DefensiveStats.Default;

        var result = calculator.CalculateDamage(damageInfo, in attackerStats, in defenderStats);

        Assert.Equal(100f, result.FinalDamage);
        Assert.False(result.IsCritical);
        Assert.Equal(0f, result.DamageReduction);
    }

    [Fact]
    public void CalculateDamage_PowerMultiplier_Applied()
    {
        var rng = new CombatRng(12345);
        var calculator = new DamageCalculator(rng);

        var damageInfo = new DamageInfo(100f, DamageType.Physical, DamageFlags.None);
        var attackerStats = new OffensiveStats { Power = 1.5f };
        var defenderStats = DefensiveStats.Default;

        var result = calculator.CalculateDamage(damageInfo, in attackerStats, in defenderStats);

        Assert.Equal(150f, result.FinalDamage);
    }

    [Fact]
    public void CalculateDamage_CriticalHit_AppliesMultiplier()
    {
        var rng = new CombatRng(12345);
        var calculator = new DamageCalculator(rng);

        var damageInfo = new DamageInfo(100f, DamageType.Physical, DamageFlags.CanCrit);
        var attackerStats = new OffensiveStats
        {
            Power = 1.0f,
            CritChance = 1.0f, // 100% crit chance
            CritMultiplier = 2.0f
        };
        var defenderStats = DefensiveStats.Default;

        var result = calculator.CalculateDamage(damageInfo, in attackerStats, in defenderStats);

        Assert.Equal(200f, result.FinalDamage);
        Assert.True(result.IsCritical);
    }

    [Fact]
    public void CalculateDamage_NoCrit_WhenFlagNotSet()
    {
        var rng = new CombatRng(12345);
        var calculator = new DamageCalculator(rng);

        var damageInfo = new DamageInfo(100f, DamageType.Physical, DamageFlags.None);
        var attackerStats = new OffensiveStats
        {
            Power = 1.0f,
            CritChance = 1.0f, // Would crit, but flag not set
            CritMultiplier = 2.0f
        };
        var defenderStats = DefensiveStats.Default;

        var result = calculator.CalculateDamage(damageInfo, in attackerStats, in defenderStats);

        Assert.Equal(100f, result.FinalDamage);
        Assert.False(result.IsCritical);
    }

    [Fact]
    public void CalculateDamage_Armor_ReducesPhysicalDamage()
    {
        var rng = new CombatRng(12345);
        var calculator = new DamageCalculator(rng);

        var damageInfo = new DamageInfo(100f, DamageType.Physical, DamageFlags.None);
        var attackerStats = OffensiveStats.Default;
        var defenderStats = new DefensiveStats { Armor = 100f }; // 50% reduction

        var result = calculator.CalculateDamage(damageInfo, in attackerStats, in defenderStats);

        Assert.Equal(50f, result.FinalDamage);
        Assert.Equal(0.5f, result.DamageReduction);
    }

    [Fact]
    public void CalculateDamage_ArcaneResist_ReducesArcaneDamage()
    {
        var rng = new CombatRng(12345);
        var calculator = new DamageCalculator(rng);

        var damageInfo = new DamageInfo(100f, DamageType.Arcane, DamageFlags.None);
        var attackerStats = OffensiveStats.Default;
        var defenderStats = new DefensiveStats { ArcaneResist = 50f }; // 33% reduction

        var result = calculator.CalculateDamage(damageInfo, in attackerStats, in defenderStats);

        Assert.InRange(result.FinalDamage, 66f, 67f); // ~66.67
        Assert.InRange(result.DamageReduction, 0.33f, 0.34f);
    }

    [Fact]
    public void CalculateDamage_Armor_DoesNotAffectArcaneDamage()
    {
        var rng = new CombatRng(12345);
        var calculator = new DamageCalculator(rng);

        var damageInfo = new DamageInfo(100f, DamageType.Arcane, DamageFlags.None);
        var attackerStats = OffensiveStats.Default;
        var defenderStats = new DefensiveStats { Armor = 100f };

        var result = calculator.CalculateDamage(damageInfo, in attackerStats, in defenderStats);

        Assert.Equal(100f, result.FinalDamage); // No reduction
    }

    [Fact]
    public void CalculateDamage_TrueDamage_BypassesAllResistances()
    {
        var rng = new CombatRng(12345);
        var calculator = new DamageCalculator(rng);

        var damageInfo = new DamageInfo(100f, DamageType.True, DamageFlags.None);
        var attackerStats = OffensiveStats.Default;
        var defenderStats = new DefensiveStats
        {
            Armor = 200f,
            ArcaneResist = 200f
        };

        var result = calculator.CalculateDamage(damageInfo, in attackerStats, in defenderStats);

        Assert.Equal(100f, result.FinalDamage);
        Assert.Equal(0f, result.DamageReduction);
    }

    [Fact]
    public void CalculateDamage_FullPipeline_PowerCritArmor()
    {
        var rng = new CombatRng(12345);
        var calculator = new DamageCalculator(rng);

        var damageInfo = new DamageInfo(100f, DamageType.Physical, DamageFlags.CanCrit);
        var attackerStats = new OffensiveStats
        {
            Power = 1.5f,
            CritChance = 1.0f,
            CritMultiplier = 2.0f
        };
        var defenderStats = new DefensiveStats { Armor = 100f }; // 50% reduction

        var result = calculator.CalculateDamage(damageInfo, in attackerStats, in defenderStats);

        // 100 * 1.5 (power) * 2.0 (crit) * 0.5 (armor) = 150
        Assert.Equal(150f, result.FinalDamage);
        Assert.True(result.IsCritical);
        Assert.Equal(0.5f, result.DamageReduction);
    }

    [Fact]
    public void CalculateDamage_HighArmor_ClampedAt90Percent()
    {
        var rng = new CombatRng(12345);
        var calculator = new DamageCalculator(rng);

        var damageInfo = new DamageInfo(100f, DamageType.Physical, DamageFlags.None);
        var attackerStats = OffensiveStats.Default;
        var defenderStats = new DefensiveStats { Armor = 10000f }; // Would be > 90% without clamp

        var result = calculator.CalculateDamage(damageInfo, in attackerStats, in defenderStats);

        Assert.InRange(result.FinalDamage, 9.99f, 10.01f); // Max 90% reduction (allow floating point imprecision)
        Assert.Equal(0.9f, result.DamageReduction);
    }
}
