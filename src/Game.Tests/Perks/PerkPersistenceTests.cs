using Xunit;
using TheLastMageStanding.Game.Core.Player;
using TheLastMageStanding.Game.Core.MetaProgression;
using TheLastMageStanding.Game.Tests.MetaProgression;
using System.IO;
using System.Text.Json;

namespace TheLastMageStanding.Game.Tests.Perks;

public sealed class PerkPersistenceTests : IDisposable
{
    private readonly PerkPersistenceService _service;
    private readonly string _saveDirectory;

    public PerkPersistenceTests()
    {
        _saveDirectory = Path.Combine(Path.GetTempPath(), $"tlms-perk-tests-{Guid.NewGuid():N}");
        _service = new PerkPersistenceService(new DefaultFileSystem(), _saveDirectory);
        _service.ClearSave();
    }

    public void Dispose()
    {
        _service.ClearSave();
        if (Directory.Exists(_saveDirectory))
        {
            Directory.Delete(_saveDirectory, recursive: true);
        }
    }

    [Fact]
    public void SaveAndLoad_PreservesPerks()
    {
        // Create a snapshot with perks
        var snapshot = new PerkSnapshot
        {
            AvailablePoints = 5,
            TotalPointsEarned = 10,
            AllocatedRanks = new Dictionary<string, int>
            {
                ["core_power"] = 3,
                ["core_speed"] = 2,
                ["crit_mastery"] = 1
            }
        };

        // Save
        _service.SavePerks(snapshot);

        // Load
        var loaded = _service.LoadPerks();

        Assert.NotNull(loaded);
        Assert.Equal(5, loaded!.AvailablePoints);
        Assert.Equal(10, loaded.TotalPointsEarned);
        Assert.Equal(3, loaded.AllocatedRanks.Count);
        Assert.Equal(3, loaded.AllocatedRanks["core_power"]);
        Assert.Equal(2, loaded.AllocatedRanks["core_speed"]);
        Assert.Equal(1, loaded.AllocatedRanks["crit_mastery"]);
    }

    [Fact]
    public void LoadPerks_WithNoSave_ReturnsNull()
    {
        _service.ClearSave();

        var loaded = _service.LoadPerks();

        Assert.Null(loaded);
    }

    [Fact]
    public void LoadPerks_WithCorruptedFile_ThrowsJsonException()
    {
        File.WriteAllText(Path.Combine(_saveDirectory, "current_run_perks.json"), "{ invalid json }{");

        Assert.Throws<JsonException>(() => _service.LoadPerks());
    }

    [Fact]
    public void SavePerks_WhenWriteFails_ThrowsIOException()
    {
        var service = new PerkPersistenceService(
            new ControlledFileSystem
            {
                DirectoryExistsResult = true,
                WriteException = new IOException("write failed")
            },
            Path.Combine(Path.GetTempPath(), $"tlms-perk-write-fail-{Guid.NewGuid():N}"));

        Assert.Throws<IOException>(() => service.SavePerks(new PerkSnapshot()));
    }

    [Fact]
    public void HasSave_AfterSave_ReturnsTrue()
    {
        var snapshot = new PerkSnapshot();
        _service.SavePerks(snapshot);

        Assert.True(_service.HasSave());
    }

    [Fact]
    public void HasSave_AfterClear_ReturnsFalse()
    {
        var snapshot = new PerkSnapshot();
        _service.SavePerks(snapshot);
        _service.ClearSave();

        Assert.False(_service.HasSave());
    }

    [Fact]
    public void SaveAndLoad_WithEmptyPerks_PreservesData()
    {
        var snapshot = new PerkSnapshot
        {
            AvailablePoints = 0,
            TotalPointsEarned = 0,
            AllocatedRanks = new Dictionary<string, int>()
        };

        _service.SavePerks(snapshot);
        var loaded = _service.LoadPerks();

        Assert.NotNull(loaded);
        Assert.Equal(0, loaded!.AvailablePoints);
        Assert.Equal(0, loaded.TotalPointsEarned);
        Assert.Empty(loaded.AllocatedRanks);
    }
}
