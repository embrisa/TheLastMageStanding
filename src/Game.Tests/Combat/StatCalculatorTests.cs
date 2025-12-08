using Xunit;
using TheLastMageStanding.Game.Core.Ecs.Components;
using TheLastMageStanding.Game.Core.Combat;

namespace TheLastMageStanding.Game.Tests.Combat;

public class StatCalculatorTests
{
    [Fact]
    public void RecalculateStats_NoModifiers_ReturnsBaseValues()
    {
        var baseOffense = new OffensiveStats
        {
            Power = 1.5f,
            AttackSpeed = 1.2f,
            CritChance = 0.1f,
            CritMultiplier = 2.0f,
            CooldownReduction = 0.2f
        };
        var baseDefense = new DefensiveStats
        {
            Armor = 50f,
            ArcaneResist = 30f
        };
        var baseMoveSpeed = 200f;
        var modifiers = StatModifiers.Zero;

        var computed = StatCalculator.RecalculateStats(
            in baseOffense,
            in baseDefense,
            baseMoveSpeed,
            in modifiers);

        Assert.Equal(1.5f, computed.EffectivePower);
        Assert.Equal(1.2f, computed.EffectiveAttackSpeed);
        Assert.Equal(0.1f, computed.EffectiveCritChance);
        Assert.Equal(2.0f, computed.EffectiveCritMultiplier);
        Assert.Equal(0.2f, computed.EffectiveCooldownReduction);
        Assert.Equal(50f, computed.EffectiveArmor);
        Assert.Equal(30f, computed.EffectiveArcaneResist);
        Assert.Equal(200f, computed.EffectiveMoveSpeed);
        Assert.False(computed.IsDirty);
    }

    [Fact]
    public void RecalculateStats_AdditiveModifiers_AddedToBase()
    {
        var baseOffense = new OffensiveStats { Power = 1.0f };
        var baseDefense = DefensiveStats.Default;
        var baseMoveSpeed = 100f;
        var modifiers = new StatModifiers
        {
            PowerAdditive = 0.5f,
            PowerMultiplicative = 1f
        };

        var computed = StatCalculator.RecalculateStats(
            in baseOffense,
            in baseDefense,
            baseMoveSpeed,
            in modifiers);

        Assert.Equal(1.5f, computed.EffectivePower);
    }

    [Fact]
    public void RecalculateStats_MultiplicativeModifiers_MultipliedAfterAdditive()
    {
        var baseOffense = new OffensiveStats { Power = 1.0f };
        var baseDefense = DefensiveStats.Default;
        var baseMoveSpeed = 100f;
        var modifiers = new StatModifiers
        {
            PowerAdditive = 0.5f,
            PowerMultiplicative = 2.0f
        };

        var computed = StatCalculator.RecalculateStats(
            in baseOffense,
            in baseDefense,
            baseMoveSpeed,
            in modifiers);

        // (1.0 + 0.5) * 2.0 = 3.0
        Assert.Equal(3.0f, computed.EffectivePower);
    }

    [Fact]
    public void RecalculateStats_CritChance_ClampedBetween0And1()
    {
        var baseOffense = new OffensiveStats { CritChance = 0.8f };
        var baseDefense = DefensiveStats.Default;
        var baseMoveSpeed = 100f;
        var modifiers = new StatModifiers
        {
            CritChanceAdditive = 0.5f // Would exceed 1.0
        };

        var computed = StatCalculator.RecalculateStats(
            in baseOffense,
            in baseDefense,
            baseMoveSpeed,
            in modifiers);

        Assert.Equal(1.0f, computed.EffectiveCritChance); // Clamped
    }

    [Fact]
    public void RecalculateStats_CooldownReduction_ClampedAt80Percent()
    {
        var baseOffense = new OffensiveStats { CooldownReduction = 0.5f };
        var baseDefense = DefensiveStats.Default;
        var baseMoveSpeed = 100f;
        var modifiers = new StatModifiers
        {
            CooldownReductionAdditive = 0.5f // Would exceed 0.8
        };

        var computed = StatCalculator.RecalculateStats(
            in baseOffense,
            in baseDefense,
            baseMoveSpeed,
            in modifiers);

        Assert.Equal(0.8f, computed.EffectiveCooldownReduction); // Clamped at 80%
    }

    [Fact]
    public void RecalculateStats_NegativeArmor_ClampedAtZero()
    {
        var baseOffense = OffensiveStats.Default;
        var baseDefense = new DefensiveStats { Armor = 10f };
        var baseMoveSpeed = 100f;
        var modifiers = new StatModifiers
        {
            ArmorAdditive = -20f // Would go negative
        };

        var computed = StatCalculator.RecalculateStats(
            in baseOffense,
            in baseDefense,
            baseMoveSpeed,
            in modifiers);

        Assert.Equal(0f, computed.EffectiveArmor); // Clamped at 0
    }

    [Fact]
    public void CalculateEffectiveCooldown_BaseCase()
    {
        var baseCooldown = 1.0f;
        var attackSpeed = 1.0f;
        var cdr = 0.0f;

        var effective = StatCalculator.CalculateEffectiveCooldown(baseCooldown, attackSpeed, cdr);

        Assert.Equal(1.0f, effective);
    }

    [Fact]
    public void CalculateEffectiveCooldown_CDR_ReducesCooldown()
    {
        var baseCooldown = 1.0f;
        var attackSpeed = 1.0f;
        var cdr = 0.5f; // 50% CDR

        var effective = StatCalculator.CalculateEffectiveCooldown(baseCooldown, attackSpeed, cdr);

        Assert.Equal(0.5f, effective);
    }

    [Fact]
    public void CalculateEffectiveCooldown_AttackSpeed_ReducesCooldown()
    {
        var baseCooldown = 1.0f;
        var attackSpeed = 2.0f;
        var cdr = 0.0f;

        var effective = StatCalculator.CalculateEffectiveCooldown(baseCooldown, attackSpeed, cdr);

        Assert.Equal(0.5f, effective);
    }

    [Fact]
    public void CalculateEffectiveCooldown_Combined_CDRThenAttackSpeed()
    {
        var baseCooldown = 1.0f;
        var attackSpeed = 2.0f;
        var cdr = 0.5f;

        var effective = StatCalculator.CalculateEffectiveCooldown(baseCooldown, attackSpeed, cdr);

        // 1.0 * (1 - 0.5) / 2.0 = 0.25
        Assert.Equal(0.25f, effective);
    }

    [Fact]
    public void CalculateEffectiveCooldown_MinimumClamped()
    {
        var baseCooldown = 0.1f;
        var attackSpeed = 100.0f;
        var cdr = 0.9f;

        var effective = StatCalculator.CalculateEffectiveCooldown(baseCooldown, attackSpeed, cdr);

        Assert.Equal(0.05f, effective); // Minimum 0.05s
    }

    [Fact]
    public void StatModifiers_Combine_AddsAdditivesMultipliesMultiplicatives()
    {
        var mod1 = new StatModifiers
        {
            PowerAdditive = 0.2f,
            PowerMultiplicative = 1.5f,
            ArmorAdditive = 10f
        };
        var mod2 = new StatModifiers
        {
            PowerAdditive = 0.3f,
            PowerMultiplicative = 2.0f,
            ArmorAdditive = 20f
        };

        var combined = StatModifiers.Combine(mod1, mod2);

        Assert.Equal(0.5f, combined.PowerAdditive); // 0.2 + 0.3
        Assert.Equal(3.0f, combined.PowerMultiplicative); // 1.5 * 2.0
        Assert.Equal(30f, combined.ArmorAdditive); // 10 + 20
    }
}
