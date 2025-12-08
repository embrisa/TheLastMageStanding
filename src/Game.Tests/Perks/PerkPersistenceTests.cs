using Xunit;
using TheLastMageStanding.Game.Core.Player;

namespace TheLastMageStanding.Game.Tests.Perks;

public sealed class PerkPersistenceTests : IDisposable
{
    private readonly PerkPersistenceService _service;

    public PerkPersistenceTests()
    {
        _service = new PerkPersistenceService();
        // Clear any existing save before tests
        _service.ClearSave();
    }

    public void Dispose()
    {
        // Clean up after tests
        _service.ClearSave();
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
