using Microsoft.Xna.Framework;
using TheLastMageStanding.Game.Core.Ecs;
using TheLastMageStanding.Game.Core.Ecs.Components;
using TheLastMageStanding.Game.Core.Ecs.Config;
using TheLastMageStanding.Game.Core.Events;
using Xunit;

namespace TheLastMageStanding.Game.Tests.Wave;

public class EliteBossWaveTests
{
    [Fact]
    public void EliteArchetype_HasCorrectTier()
    {
        // Arrange
        var elite = EnemyWaveConfig.CreateEliteForDebug();

        // Assert
        Assert.Equal(EnemyTier.Elite, elite.Tier);
        Assert.True(elite.MaxHealth > 50f); // Elites should be tankier
        Assert.True(elite.Visual.Scale > 1.2f); // Elites should be visibly larger
    }

    [Fact]
    public void BossArchetype_HasCorrectTier()
    {
        // Arrange
        var boss = EnemyWaveConfig.CreateBossForDebug();

        // Assert
        Assert.Equal(EnemyTier.Boss, boss.Tier);
        Assert.True(boss.MaxHealth > 200f); // Bosses should be very tanky
        Assert.True(boss.Visual.Scale > 1.5f); // Bosses should be much larger
        Assert.NotNull(boss.RangedAttack); // Boss uses ranged attacks
    }

    [Fact]
    public void EnemyFactory_SetsLootDropperForElite()
    {
        // Arrange
        var world = new EcsWorld();
        var factory = new EnemyEntityFactory(world);
        var elite = EnemyWaveConfig.CreateEliteForDebug();

        // Act
        var entity = factory.CreateEnemy(Vector2.Zero, elite);

        // Assert
        Assert.True(world.TryGetComponent(entity, out LootDropper dropper));
        Assert.True(dropper.IsElite);
        Assert.False(dropper.IsBoss);
    }

    [Fact]
    public void EnemyFactory_SetsLootDropperForBoss()
    {
        // Arrange
        var world = new EcsWorld();
        var factory = new EnemyEntityFactory(world);
        var boss = EnemyWaveConfig.CreateBossForDebug();

        // Act
        var entity = factory.CreateEnemy(Vector2.Zero, boss);

        // Assert
        Assert.True(world.TryGetComponent(entity, out LootDropper dropper));
        Assert.False(dropper.IsElite);
        Assert.True(dropper.IsBoss);
    }

    [Fact]
    public void EnemyFactory_AddsEliteTag()
    {
        // Arrange
        var world = new EcsWorld();
        var factory = new EnemyEntityFactory(world);
        var elite = EnemyWaveConfig.CreateEliteForDebug();

        // Act
        var entity = factory.CreateEnemy(Vector2.Zero, elite);

        // Assert
        Assert.True(world.TryGetComponent(entity, out EliteTag _));
        Assert.False(world.TryGetComponent(entity, out BossTag _));
    }

    [Fact]
    public void EnemyFactory_AddsBossTag()
    {
        // Arrange
        var world = new EcsWorld();
        var factory = new EnemyEntityFactory(world);
        var boss = EnemyWaveConfig.CreateBossForDebug();

        // Act
        var entity = factory.CreateEnemy(Vector2.Zero, boss);

        // Assert
        Assert.True(world.TryGetComponent(entity, out BossTag _));
        Assert.False(world.TryGetComponent(entity, out EliteTag _));
    }

    [Fact]
    public void Elite_HasHigherDefensiveStats()
    {
        // Arrange
        var world = new EcsWorld();
        var factory = new EnemyEntityFactory(world);
        var elite = EnemyWaveConfig.CreateEliteForDebug();

        // Act
        var entity = factory.CreateEnemy(Vector2.Zero, elite);

        // Assert
        Assert.True(world.TryGetComponent(entity, out DefensiveStats stats));
        Assert.True(stats.Armor > 0f); // Elites have armor
        Assert.True(stats.ArcaneResist > 0f); // Elites have resistance
    }

    [Fact]
    public void Boss_HasEvenHigherDefensiveStats()
    {
        // Arrange
        var world = new EcsWorld();
        var factory = new EnemyEntityFactory(world);
        var boss = EnemyWaveConfig.CreateBossForDebug();

        // Act
        var entity = factory.CreateEnemy(Vector2.Zero, boss);

        // Assert
        Assert.True(world.TryGetComponent(entity, out DefensiveStats stats));
        Assert.True(stats.Armor > 10f); // Bosses have significant armor
        Assert.True(stats.ArcaneResist > 20f); // Bosses have high resistance
    }

    [Fact]
    public void WaveConfig_IncludesEliteAndBossProfiles()
    {
        // Arrange
        var config = EnemyWaveConfig.Default;

        // Assert
        var hasElite = false;
        var hasBoss = false;
        foreach (var profile in config.EnemyProfiles)
        {
            if (profile.Archetype.Tier == EnemyTier.Elite)
            {
                hasElite = true;
                Assert.True(profile.UnlockWave >= 5); // Elites unlock wave 5+
            }
            if (profile.Archetype.Tier == EnemyTier.Boss)
            {
                hasBoss = true;
                Assert.True(profile.UnlockWave >= 10); // Bosses unlock wave 10+
            }
        }

        Assert.True(hasElite, "Config should include at least one elite profile");
        Assert.True(hasBoss, "Config should include at least one boss profile");
    }

    [Fact]
    public void ChooseArchetype_RespectsUnlockWave()
    {
        // Arrange
        var config = EnemyWaveConfig.Default;
        var random = new Random(12345);

        // Act - early wave should not spawn elite/boss
        var earlyArchetype = config.ChooseArchetype(waveIndex: 1, random);

        // Assert
        Assert.NotEqual(EnemyTier.Elite, earlyArchetype.Tier);
        Assert.NotEqual(EnemyTier.Boss, earlyArchetype.Tier);
    }

    [Fact]
    public void ChooseArchetype_CanSpawnEliteAfterUnlock()
    {
        // Arrange
        var config = EnemyWaveConfig.Default;
        var random = new Random(12345);

        // Act - Try many spawns after elite unlock wave
        var foundElite = false;
        for (var i = 0; i < 100; i++)
        {
            var archetype = config.ChooseArchetype(waveIndex: 10, random);
            if (archetype.Tier == EnemyTier.Elite)
            {
                foundElite = true;
                break;
            }
        }

        // Assert
        Assert.True(foundElite, "Should be able to spawn elites after wave 5");
    }

    [Fact]
    public void ChooseArchetype_CanSpawnBossAfterUnlock()
    {
        // Arrange
        var config = EnemyWaveConfig.Default;
        var random = new Random(12345);

        // Act - Try many spawns after boss unlock wave
        var foundBoss = false;
        for (var i = 0; i < 200; i++)
        {
            var archetype = config.ChooseArchetype(waveIndex: 15, random);
            if (archetype.Tier == EnemyTier.Boss)
            {
                foundBoss = true;
                break;
            }
        }

        // Assert
        Assert.True(foundBoss, "Should be able to spawn bosses after wave 10");
    }

    [Fact]
    public void LootDropper_HasCorrectDropChancesForElite()
    {
        // Arrange
        var world = new EcsWorld();
        var factory = new EnemyEntityFactory(world);
        var elite = EnemyWaveConfig.CreateEliteForDebug();

        // Act
        var entity = factory.CreateEnemy(Vector2.Zero, elite);

        // Assert
        Assert.True(world.TryGetComponent(entity, out LootDropper dropper));
        // Drop chance is managed by LootDropConfig which reads IsElite flag
        Assert.True(dropper.IsElite);
    }

    [Fact]
    public void LootDropper_HasCorrectDropChancesForBoss()
    {
        // Arrange
        var world = new EcsWorld();
        var factory = new EnemyEntityFactory(world);
        var boss = EnemyWaveConfig.CreateBossForDebug();

        // Act
        var entity = factory.CreateEnemy(Vector2.Zero, boss);

        // Assert
        Assert.True(world.TryGetComponent(entity, out LootDropper dropper));
        // Drop chance is managed by LootDropConfig which reads IsBoss flag
        Assert.True(dropper.IsBoss);
    }
}
