using Xunit;
using TheLastMageStanding.Game.Core.Config;
using TheLastMageStanding.Game.Core.Ecs.Components;
using TheLastMageStanding.Game.Core.Perks;

namespace TheLastMageStanding.Game.Tests.Perks;

public class PerkServiceTests
{
    private readonly PerkTreeConfig _config;
    private readonly PerkService _service;

    public PerkServiceTests()
    {
        _config = PerkTreeConfig.Default;
        _service = new PerkService(_config);
    }

    [Fact]
    public void CanAllocate_WithNoPoints_ReturnsFalse()
    {
        var playerPerks = new PlayerPerks();
        var perkPoints = new PerkPoints(0, 0);

        var result = _service.CanAllocate("core_power", playerPerks, perkPoints);

        Assert.False(result.CanAllocate);
        Assert.Contains("Need", result.Message);
    }

    [Fact]
    public void CanAllocate_WithPoints_ReturnsTrue()
    {
        var playerPerks = new PlayerPerks();
        var perkPoints = new PerkPoints(5, 5);

        var result = _service.CanAllocate("core_power", playerPerks, perkPoints);

        Assert.True(result.CanAllocate);
    }

    [Fact]
    public void CanAllocate_WhenAtMaxRank_ReturnsFalse()
    {
        var playerPerks = new PlayerPerks();
        playerPerks.SetRank("core_power", 5); // Max rank
        var perkPoints = new PerkPoints(5, 5);

        var result = _service.CanAllocate("core_power", playerPerks, perkPoints);

        Assert.False(result.CanAllocate);
        Assert.Contains("max rank", result.Message);
    }

    [Fact]
    public void CanAllocate_WithUnmetPrerequisite_ReturnsFalse()
    {
        var playerPerks = new PlayerPerks();
        // crit_mastery requires core_power rank 2
        var perkPoints = new PerkPoints(5, 5);

        var result = _service.CanAllocate("crit_mastery", playerPerks, perkPoints);

        Assert.False(result.CanAllocate);
        Assert.Contains("Requires", result.Message);
    }

    [Fact]
    public void CanAllocate_WithMetPrerequisite_ReturnsTrue()
    {
        var playerPerks = new PlayerPerks();
        playerPerks.SetRank("core_power", 2);
        var perkPoints = new PerkPoints(5, 5);

        var result = _service.CanAllocate("crit_mastery", playerPerks, perkPoints);

        Assert.True(result.CanAllocate);
    }

    [Fact]
    public void Allocate_IncreasesRankAndSpendsPoints()
    {
        var playerPerks = new PlayerPerks();
        var perkPoints = new PerkPoints(5, 5);

        var success = _service.Allocate("core_power", ref playerPerks, ref perkPoints);

        Assert.True(success);
        Assert.Equal(1, playerPerks.GetRank("core_power"));
        Assert.Equal(4, perkPoints.AvailablePoints);
    }

    [Fact]
    public void Allocate_MultipleTimes_StacksRanks()
    {
        var playerPerks = new PlayerPerks();
        var perkPoints = new PerkPoints(5, 5);

        _service.Allocate("core_power", ref playerPerks, ref perkPoints);
        _service.Allocate("core_power", ref playerPerks, ref perkPoints);

        Assert.Equal(2, playerPerks.GetRank("core_power"));
        Assert.Equal(3, perkPoints.AvailablePoints);
    }

    [Fact]
    public void Deallocate_RemovesRankAndRefundsPoints()
    {
        var playerPerks = new PlayerPerks();
        playerPerks.SetRank("core_power", 2);
        var perkPoints = new PerkPoints(3, 5);

        var success = _service.Deallocate("core_power", ref playerPerks, ref perkPoints);

        Assert.True(success);
        Assert.Equal(1, playerPerks.GetRank("core_power"));
        Assert.Equal(4, perkPoints.AvailablePoints);
    }

    [Fact]
    public void Deallocate_WhenOtherPerkDepends_ReturnsFalse()
    {
        var playerPerks = new PlayerPerks();
        playerPerks.SetRank("core_power", 2);
        playerPerks.SetRank("crit_mastery", 1); // Depends on core_power rank 2
        var perkPoints = new PerkPoints(0, 5);

        var success = _service.Deallocate("core_power", ref playerPerks, ref perkPoints);

        Assert.False(success);
        Assert.Equal(2, playerPerks.GetRank("core_power")); // Unchanged
    }

    [Fact]
    public void RespecAll_ClearsPerksAndRefundsPoints()
    {
        var playerPerks = new PlayerPerks();
        playerPerks.SetRank("core_power", 3);
        playerPerks.SetRank("core_speed", 2);
        var perkPoints = new PerkPoints(0, 10);

        _service.RespecAll(ref playerPerks, ref perkPoints);

        Assert.Equal(0, playerPerks.GetRank("core_power"));
        Assert.Equal(0, playerPerks.GetRank("core_speed"));
        Assert.Equal(5, perkPoints.AvailablePoints); // 3 + 2 refunded
        Assert.Empty(playerPerks.AllocatedRanks);
    }

    [Fact]
    public void CalculateTotalEffects_CombinesEffects()
    {
        var playerPerks = new PlayerPerks();
        playerPerks.SetRank("core_power", 2); // +0.4 power
        playerPerks.SetRank("core_speed", 1); // +0.1 attack speed

        var totalEffects = _service.CalculateTotalEffects(playerPerks);

        Assert.Equal(0.4f, totalEffects.PowerAdditive);
        Assert.Equal(0.1f, totalEffects.AttackSpeedAdditive);
    }

    [Fact]
    public void CalculateTotalEffects_WithNoPerks_ReturnsNone()
    {
        var playerPerks = new PlayerPerks();

        var totalEffects = _service.CalculateTotalEffects(playerPerks);

        Assert.Equal(0f, totalEffects.PowerAdditive);
        Assert.Equal(0f, totalEffects.AttackSpeedAdditive);
    }

    [Fact]
    public void PerkEffects_Combine_AddsAdditiveModifiers()
    {
        var effect1 = new PerkEffects { PowerAdditive = 0.5f, ArmorAdditive = 10f };
        var effect2 = new PerkEffects { PowerAdditive = 0.3f, MoveSpeedAdditive = 15f };

        var combined = PerkEffects.Combine(effect1, effect2);

        Assert.Equal(0.8f, combined.PowerAdditive);
        Assert.Equal(10f, combined.ArmorAdditive);
        Assert.Equal(15f, combined.MoveSpeedAdditive);
    }

    [Fact]
    public void PerkEffects_Combine_MultipliesMultiplicativeModifiers()
    {
        var effect1 = new PerkEffects { PowerMultiplicative = 1.2f };
        var effect2 = new PerkEffects { PowerMultiplicative = 1.5f };

        var combined = PerkEffects.Combine(effect1, effect2);

        Assert.Equal(1.8f, combined.PowerMultiplicative, precision: 4);
    }

    [Fact]
    public void PerkEffects_Combine_GameplayModifiers()
    {
        var effect1 = new PerkEffects { ProjectilePierceBonus = 1 };
        var effect2 = new PerkEffects { ProjectilePierceBonus = 1, ProjectileChainBonus = 2 };

        var combined = PerkEffects.Combine(effect1, effect2);

        Assert.Equal(2, combined.ProjectilePierceBonus);
        Assert.Equal(2, combined.ProjectileChainBonus);
    }

    [Fact]
    public void PlayerPerks_GetRank_ReturnsZeroForUnallocated()
    {
        var playerPerks = new PlayerPerks();

        Assert.Equal(0, playerPerks.GetRank("nonexistent"));
    }

    [Fact]
    public void PlayerPerks_SetRank_SetsValue()
    {
        var playerPerks = new PlayerPerks();

        playerPerks.SetRank("test_perk", 3);

        Assert.Equal(3, playerPerks.GetRank("test_perk"));
    }

    [Fact]
    public void PlayerPerks_SetRankToZero_RemovesPerk()
    {
        var playerPerks = new PlayerPerks();
        playerPerks.SetRank("test_perk", 3);

        playerPerks.SetRank("test_perk", 0);

        Assert.Equal(0, playerPerks.GetRank("test_perk"));
        Assert.DoesNotContain("test_perk", playerPerks.AllocatedRanks.Keys);
    }

    [Fact]
    public void ComplexPrerequisiteChain_WorksCorrectly()
    {
        var playerPerks = new PlayerPerks();
        var perkPoints = new PerkPoints(20, 20);

        // Build up prerequisite chain
        Assert.True(_service.Allocate("core_power", ref playerPerks, ref perkPoints));
        Assert.True(_service.Allocate("core_power", ref playerPerks, ref perkPoints));
        Assert.True(_service.Allocate("core_speed", ref playerPerks, ref perkPoints));
        Assert.True(_service.Allocate("core_speed", ref playerPerks, ref perkPoints));

        // Now can allocate crit_mastery (needs core_power 2)
        Assert.True(_service.Allocate("crit_mastery", ref playerPerks, ref perkPoints));
        Assert.True(_service.Allocate("crit_mastery", ref playerPerks, ref perkPoints));

        // Advance core_speed to max
        Assert.True(_service.Allocate("core_speed", ref playerPerks, ref perkPoints));

        // Now can allocate projectile_pierce (needs crit_mastery 2 and core_speed 2)
        var result = _service.CanAllocate("projectile_pierce", playerPerks, perkPoints);
        Assert.True(result.CanAllocate);
    }
}
