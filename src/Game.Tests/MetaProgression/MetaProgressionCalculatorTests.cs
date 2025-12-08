using Xunit;
using TheLastMageStanding.Game.Core.MetaProgression;

namespace TheLastMageStanding.Game.Tests.MetaProgression;

public class MetaProgressionCalculatorTests
{
    [Fact]
    public void CalculateMetaXP_WithBasicRun_ReturnsPositiveXP()
    {
        // Arrange
        var run = new RunSession
        {
            StartTime = DateTime.UtcNow.AddMinutes(-10),
            EndTime = DateTime.UtcNow,
            WaveReached = 5,
            TotalKills = 50,
            GoldCollected = 100,
            TotalDamageDealt = 5000f
        };

        // Act
        var xp = MetaProgressionCalculator.CalculateMetaXP(run);

        // Assert
        Assert.True(xp > 0, "XP should be positive");
    }

    [Fact]
    public void CalculateMetaXP_HigherWave_GivesMoreXP()
    {
        // Arrange
        var runWave5 = new RunSession
        {
            StartTime = DateTime.UtcNow.AddMinutes(-10),
            EndTime = DateTime.UtcNow,
            WaveReached = 5,
            TotalKills = 50,
            GoldCollected = 100,
            TotalDamageDealt = 5000f
        };

        var runWave10 = new RunSession
        {
            StartTime = DateTime.UtcNow.AddMinutes(-10),
            EndTime = DateTime.UtcNow,
            WaveReached = 10,
            TotalKills = 50,
            GoldCollected = 100,
            TotalDamageDealt = 5000f
        };

        // Act
        var xpWave5 = MetaProgressionCalculator.CalculateMetaXP(runWave5);
        var xpWave10 = MetaProgressionCalculator.CalculateMetaXP(runWave10);

        // Assert
        Assert.True(xpWave10 > xpWave5, "Higher wave should give more XP");
    }

    [Fact]
    public void CalculateMetaXP_MoreKills_GivesMoreXP()
    {
        // Arrange
        var runLowKills = new RunSession
        {
            StartTime = DateTime.UtcNow.AddMinutes(-10),
            EndTime = DateTime.UtcNow,
            WaveReached = 5,
            TotalKills = 20,
            GoldCollected = 100,
            TotalDamageDealt = 5000f
        };

        var runHighKills = new RunSession
        {
            StartTime = DateTime.UtcNow.AddMinutes(-10),
            EndTime = DateTime.UtcNow,
            WaveReached = 5,
            TotalKills = 100,
            GoldCollected = 100,
            TotalDamageDealt = 5000f
        };

        // Act
        var xpLow = MetaProgressionCalculator.CalculateMetaXP(runLowKills);
        var xpHigh = MetaProgressionCalculator.CalculateMetaXP(runHighKills);

        // Assert
        Assert.True(xpHigh > xpLow, "More kills should give more XP");
    }

    [Fact]
    public void CalculateMetaXP_FasterRun_GivesBonusXP()
    {
        // Arrange
        var runFast = new RunSession
        {
            StartTime = DateTime.UtcNow.AddMinutes(-5),
            EndTime = DateTime.UtcNow,
            WaveReached = 5,
            TotalKills = 50,
            GoldCollected = 100,
            TotalDamageDealt = 5000f
        };

        var runSlow = new RunSession
        {
            StartTime = DateTime.UtcNow.AddMinutes(-30),
            EndTime = DateTime.UtcNow,
            WaveReached = 5,
            TotalKills = 50,
            GoldCollected = 100,
            TotalDamageDealt = 5000f
        };

        // Act
        var xpFast = MetaProgressionCalculator.CalculateMetaXP(runFast);
        var xpSlow = MetaProgressionCalculator.CalculateMetaXP(runSlow);

        // Assert
        Assert.True(xpFast > xpSlow, "Faster run should give more XP due to time bonus");
    }

    [Fact]
    public void CalculateMetaXP_MinimalRun_GivesAtLeast1XP()
    {
        // Arrange
        var run = new RunSession
        {
            StartTime = DateTime.UtcNow.AddMinutes(-1),
            EndTime = DateTime.UtcNow,
            WaveReached = 0,
            TotalKills = 0,
            GoldCollected = 0,
            TotalDamageDealt = 0f
        };

        // Act
        var xp = MetaProgressionCalculator.CalculateMetaXP(run);

        // Assert
        Assert.True(xp >= 1, "Minimum XP should be at least 1");
    }

    [Fact]
    public void GetLevelFromXP_ZeroXP_ReturnsLevel1()
    {
        // Act
        var level = MetaProgressionCalculator.GetLevelFromXP(0);

        // Assert
        Assert.Equal(1, level);
    }

    [Fact]
    public void GetLevelFromXP_NegativeXP_ReturnsLevel1()
    {
        // Act
        var level = MetaProgressionCalculator.GetLevelFromXP(-100);

        // Assert
        Assert.Equal(1, level);
    }

    [Fact]
    public void GetLevelFromXP_IncreasesWithMoreXP()
    {
        // Act
        var level1 = MetaProgressionCalculator.GetLevelFromXP(0);
        var level2 = MetaProgressionCalculator.GetLevelFromXP(10000);
        var level3 = MetaProgressionCalculator.GetLevelFromXP(50000);

        // Assert
        Assert.True(level2 > level1, "More XP should give higher level");
        Assert.True(level3 > level2, "Even more XP should give even higher level");
    }

    [Fact]
    public void GetXPForLevel_Level1_Returns0()
    {
        // Act
        var xp = MetaProgressionCalculator.GetXPForLevel(1);

        // Assert
        Assert.Equal(0, xp);
    }

    [Fact]
    public void GetXPForLevel_HigherLevels_RequireMoreXP()
    {
        // Act
        var xpLevel2 = MetaProgressionCalculator.GetXPForLevel(2);
        var xpLevel3 = MetaProgressionCalculator.GetXPForLevel(3);
        var xpLevel4 = MetaProgressionCalculator.GetXPForLevel(4);

        // Assert
        Assert.True(xpLevel3 > xpLevel2, "Level 3 should require more XP than level 2");
        Assert.True(xpLevel4 > xpLevel3, "Level 4 should require more XP than level 3");
    }

    [Fact]
    public void GetXPToNextLevel_ReturnsCorrectRemaining()
    {
        // Arrange
        var currentXp = 500;

        // Act
        var xpToNext = MetaProgressionCalculator.GetXPToNextLevel(currentXp);
        var currentLevel = MetaProgressionCalculator.GetLevelFromXP(currentXp);
        var nextLevelXp = MetaProgressionCalculator.GetXPForLevel(currentLevel + 1);

        // Assert
        Assert.Equal(nextLevelXp - currentXp, xpToNext);
    }

    [Fact]
    public void GetLevelProgress_ReturnsBetween0And1()
    {
        // Arrange
        var xp = 1500;

        // Act
        var progress = MetaProgressionCalculator.GetLevelProgress(xp);

        // Assert
        Assert.InRange(progress, 0f, 1f);
    }

    [Fact]
    public void GetLevelProgress_StartOfLevel_Returns0()
    {
        // Arrange - Get XP exactly at level threshold
        var level = 5;
        var xp = MetaProgressionCalculator.GetXPForLevel(level);

        // Act
        var progress = MetaProgressionCalculator.GetLevelProgress(xp);

        // Assert
        Assert.True(progress >= 0f && progress < 0.1f, "Progress at level start should be near 0");
    }

    [Fact]
    public void CalculateGoldReward_ReturnsPositiveGold()
    {
        // Arrange
        var run = new RunSession
        {
            WaveReached = 5,
            TotalKills = 50
        };

        // Act
        var gold = MetaProgressionCalculator.CalculateGoldReward(run);

        // Assert
        Assert.True(gold > 0, "Gold reward should be positive");
    }

    [Fact]
    public void CalculateGoldReward_HigherWave_GivesMoreGold()
    {
        // Arrange
        var runWave5 = new RunSession { WaveReached = 5, TotalKills = 50 };
        var runWave15 = new RunSession { WaveReached = 15, TotalKills = 50 };

        // Act
        var goldWave5 = MetaProgressionCalculator.CalculateGoldReward(runWave5);
        var goldWave15 = MetaProgressionCalculator.CalculateGoldReward(runWave15);

        // Assert
        Assert.True(goldWave15 > goldWave5, "Higher wave should give more gold");
    }

    [Fact]
    public void CalculateGoldReward_MilestoneBonus_Applied()
    {
        // Arrange
        var runWave9 = new RunSession { WaveReached = 9, TotalKills = 100 };
        var runWave10 = new RunSession { WaveReached = 10, TotalKills = 100 };

        // Act
        var goldWave9 = MetaProgressionCalculator.CalculateGoldReward(runWave9);
        var goldWave10 = MetaProgressionCalculator.CalculateGoldReward(runWave10);

        // Assert
        var bonus = goldWave10 - goldWave9;
        Assert.True(bonus >= 50, "Wave 10 milestone should give bonus gold");
    }
}
