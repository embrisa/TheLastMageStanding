using Xunit;
using TheLastMageStanding.Game.Core.MetaProgression;

namespace TheLastMageStanding.Game.Tests.MetaProgression;

/// <summary>
/// In-memory file system for testing without disk I/O.
/// </summary>
public class InMemoryFileSystem : IFileSystem
{
    private readonly Dictionary<string, string> _files = new();
    private readonly HashSet<string> _directories = new();

    public bool FileExists(string path) => _files.ContainsKey(path);

    public string ReadAllText(string path)
    {
        if (!_files.TryGetValue(path, out var content))
            throw new FileNotFoundException($"File not found: {path}");
        return content;
    }

    public void WriteAllText(string path, string content)
    {
        _files[path] = content;
    }

    public void DeleteFile(string path)
    {
        _files.Remove(path);
    }

    public void CopyFile(string sourcePath, string destPath, bool overwrite)
    {
        if (!_files.TryGetValue(sourcePath, out var content))
            throw new FileNotFoundException($"Source file not found: {sourcePath}");
        
        if (_files.ContainsKey(destPath) && !overwrite)
            throw new IOException($"Destination file already exists: {destPath}");

        _files[destPath] = content;
    }

    public void CreateDirectory(string path)
    {
        _directories.Add(path);
    }

    public string[] GetFiles(string directory, string searchPattern)
    {
        // Simple pattern matching (just prefix matching for now)
        var prefix = searchPattern.Replace("*", "");
        return _files.Keys
            .Where(k => k.Contains(directory) && k.Contains(prefix))
            .ToArray();
    }

    public bool DirectoryExists(string path) => _directories.Contains(path);

    public string[] GetDirectories(string path)
    {
        return _directories
            .Where(d => d.StartsWith(path, StringComparison.OrdinalIgnoreCase))
            .ToArray();
    }
}

public class PlayerProfileServiceTests
{
    [Fact]
    public void LoadProfile_NoExistingFile_ReturnsDefaultProfile()
    {
        // Arrange
        var fileSystem = new InMemoryFileSystem();
        var service = new PlayerProfileService(fileSystem, "/test/save");

        // Act
        var profile = service.LoadProfile();

        // Assert
        Assert.NotNull(profile);
        Assert.Equal(1, profile.MetaLevel);
        Assert.Equal(0, profile.TotalMetaXp);
        Assert.True(profile.TotalGold >= 0);
    }

    [Fact]
    public void SaveProfile_ThenLoad_RestoresProfile()
    {
        // Arrange
        var fileSystem = new InMemoryFileSystem();
        var service = new PlayerProfileService(fileSystem, "/test/save");
        
        var profile = new PlayerProfile
        {
            MetaLevel = 5,
            TotalMetaXp = 10000,
            TotalGold = 500,
            TotalRuns = 10,
            BestWave = 15
        };

        // Act
        service.SaveProfile(profile);
        var loadedProfile = service.LoadProfile();

        // Assert
        Assert.Equal(profile.MetaLevel, loadedProfile.MetaLevel);
        Assert.Equal(profile.TotalMetaXp, loadedProfile.TotalMetaXp);
        Assert.Equal(profile.TotalGold, loadedProfile.TotalGold);
        Assert.Equal(profile.TotalRuns, loadedProfile.TotalRuns);
        Assert.Equal(profile.BestWave, loadedProfile.BestWave);
    }

    [Fact]
    public void SaveProfile_WithEquipment_PersistsEquipment()
    {
        // Arrange
        var fileSystem = new InMemoryFileSystem();
        var service = new PlayerProfileService(fileSystem, "/test/save");
        
        var equipment = new EquipmentItem
        {
            Id = "sword_01",
            Name = "Rusty Sword",
            Type = EquipmentType.Weapon,
            Rarity = EquipmentRarity.Common,
            Damage = 10f
        };

        var profile = PlayerProfile.CreateDefault();
        profile.EquipmentInventory.Add(equipment);
        profile.EquippedWeaponId = equipment.Id;

        // Act
        service.SaveProfile(profile);
        var loadedProfile = service.LoadProfile();

        // Assert
        Assert.Single(loadedProfile.EquipmentInventory);
        Assert.Equal(equipment.Id, loadedProfile.EquipmentInventory[0].Id);
        Assert.Equal(equipment.Name, loadedProfile.EquipmentInventory[0].Name);
        Assert.Equal(equipment.Damage, loadedProfile.EquipmentInventory[0].Damage);
        Assert.Equal(equipment.Id, loadedProfile.EquippedWeaponId);
    }

    [Fact]
    public void SaveProfile_CreatesBackup()
    {
        // Arrange
        var fileSystem = new InMemoryFileSystem();
        var service = new PlayerProfileService(fileSystem, "/test/save");
        
        var profile = PlayerProfile.CreateDefault();
        
        // Act - Save twice to trigger backup
        service.SaveProfile(profile);
        profile.TotalGold = 999;
        service.SaveProfile(profile);

        // Assert - Check backup exists
        var backups = fileSystem.GetFiles("/test/save", "player_profile.backup");
        Assert.True(backups.Length > 0, "Backup should be created");
    }

    [Fact]
    public void LoadProfile_CorruptedFile_ReturnsDefaultProfile()
    {
        // Arrange
        var fileSystem = new InMemoryFileSystem();
        fileSystem.CreateDirectory("/test/save");
        fileSystem.WriteAllText("/test/save/player_profile.json", "{ invalid json }{");
        
        var service = new PlayerProfileService(fileSystem, "/test/save");

        // Act
        var profile = service.LoadProfile();

        // Assert - Should return default profile instead of throwing
        Assert.NotNull(profile);
        Assert.Equal(1, profile.MetaLevel);
    }
}

public class RunHistoryServiceTests
{
    [Fact]
    public void SaveRun_ThenGetAllRuns_ReturnsRun()
    {
        // Arrange
        var fileSystem = new InMemoryFileSystem();
        var service = new RunHistoryService(fileSystem, "/test/save");
        
        var run = new RunSession
        {
            StartTime = DateTime.UtcNow,
            EndTime = DateTime.UtcNow.AddMinutes(10),
            WaveReached = 10,
            TotalKills = 100,
            MetaXpEarned = 500
        };

        // Act
        service.SaveRun(run);
        var runs = service.GetAllRuns();

        // Assert
        Assert.Single(runs);
        Assert.Equal(run.WaveReached, runs[0].WaveReached);
        Assert.Equal(run.TotalKills, runs[0].TotalKills);
    }

    [Fact]
    public void SaveRun_MultipleTimes_MaintainsOrder()
    {
        // Arrange
        var fileSystem = new InMemoryFileSystem();
        var service = new RunHistoryService(fileSystem, "/test/save");
        
        var run1 = new RunSession { WaveReached = 5, TotalKills = 50 };
        var run2 = new RunSession { WaveReached = 10, TotalKills = 100 };
        var run3 = new RunSession { WaveReached = 8, TotalKills = 75 };

        // Act
        service.SaveRun(run1);
        service.SaveRun(run2);
        service.SaveRun(run3);
        var runs = service.GetAllRuns();

        // Assert
        Assert.Equal(3, runs.Count);
        // Most recent should be first
        Assert.Equal(8, runs[0].WaveReached);
        Assert.Equal(10, runs[1].WaveReached);
        Assert.Equal(5, runs[2].WaveReached);
    }

    [Fact]
    public void GetRecentRuns_LimitsCount()
    {
        // Arrange
        var fileSystem = new InMemoryFileSystem();
        var service = new RunHistoryService(fileSystem, "/test/save");
        
        for (int i = 0; i < 10; i++)
        {
            service.SaveRun(new RunSession { WaveReached = i });
        }

        // Act
        var recentRuns = service.GetRecentRuns(5);

        // Assert
        Assert.Equal(5, recentRuns.Count);
    }

    [Fact]
    public void GetBestRuns_SortsByWave()
    {
        // Arrange
        var fileSystem = new InMemoryFileSystem();
        var service = new RunHistoryService(fileSystem, "/test/save");
        
        service.SaveRun(new RunSession { WaveReached = 5, TotalKills = 50 });
        service.SaveRun(new RunSession { WaveReached = 15, TotalKills = 100 });
        service.SaveRun(new RunSession { WaveReached = 10, TotalKills = 75 });

        // Act
        var bestRuns = service.GetBestRuns(3);

        // Assert
        Assert.Equal(15, bestRuns[0].WaveReached);
        Assert.Equal(10, bestRuns[1].WaveReached);
        Assert.Equal(5, bestRuns[2].WaveReached);
    }

    [Fact]
    public void GetBestRunByWave_ReturnsHighestWave()
    {
        // Arrange
        var fileSystem = new InMemoryFileSystem();
        var service = new RunHistoryService(fileSystem, "/test/save");
        
        service.SaveRun(new RunSession { WaveReached = 5 });
        service.SaveRun(new RunSession { WaveReached = 15 });
        service.SaveRun(new RunSession { WaveReached = 10 });

        // Act
        var bestRun = service.GetBestRunByWave();

        // Assert
        Assert.NotNull(bestRun);
        Assert.Equal(15, bestRun.WaveReached);
    }

    [Fact]
    public void GetBestRunByKills_ReturnsHighestKills()
    {
        // Arrange
        var fileSystem = new InMemoryFileSystem();
        var service = new RunHistoryService(fileSystem, "/test/save");
        
        service.SaveRun(new RunSession { TotalKills = 50 });
        service.SaveRun(new RunSession { TotalKills = 150 });
        service.SaveRun(new RunSession { TotalKills = 100 });

        // Act
        var bestRun = service.GetBestRunByKills();

        // Assert
        Assert.NotNull(bestRun);
        Assert.Equal(150, bestRun.TotalKills);
    }

    [Fact]
    public void ClearHistory_RemovesAllRuns()
    {
        // Arrange
        var fileSystem = new InMemoryFileSystem();
        var service = new RunHistoryService(fileSystem, "/test/save");
        
        service.SaveRun(new RunSession { WaveReached = 5 });
        service.SaveRun(new RunSession { WaveReached = 10 });

        // Act
        service.ClearHistory();
        var runs = service.GetAllRuns();

        // Assert
        Assert.Empty(runs);
    }
}
