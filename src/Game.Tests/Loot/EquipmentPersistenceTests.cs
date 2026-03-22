using Xunit;
using TheLastMageStanding.Game.Core.Player;
using TheLastMageStanding.Game.Core.Loot;
using TheLastMageStanding.Game.Core.MetaProgression;
using TheLastMageStanding.Game.Tests.MetaProgression;
using System.Collections.Generic;
using System.IO;
using System;
using System.Text.Json;

namespace TheLastMageStanding.Game.Tests.Loot;

public sealed class EquipmentPersistenceTests : IDisposable
{
    private readonly EquipmentPersistenceService _service;
    private readonly string _saveDirectory;

    public EquipmentPersistenceTests()
    {
        _saveDirectory = Path.Combine(Path.GetTempPath(), $"tlms-equipment-tests-{Guid.NewGuid():N}");
        _service = new EquipmentPersistenceService(new DefaultFileSystem(), _saveDirectory);
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
    public void SaveAndLoad_PreservesEquipment()
    {
        // Create a snapshot with items
        var snapshot = new EquipmentSnapshot();
        
        var item = CreateTestItem();
        snapshot.EquippedItems[EquipSlot.Weapon] = ItemInstanceData.FromItemInstance(item);
        snapshot.InventoryItems.Add(ItemInstanceData.FromItemInstance(item));

        // Save
        _service.SaveEquipment(snapshot);

        // Load
        var loaded = _service.LoadEquipment();

        Assert.NotNull(loaded);
        Assert.Single(loaded!.EquippedItems);
        Assert.Single(loaded.InventoryItems);
        Assert.True(loaded.EquippedItems.ContainsKey(EquipSlot.Weapon));
    }

    [Fact]
    public void LoadEquipment_WithNoSave_ReturnsNull()
    {
        _service.ClearSave();

        var loaded = _service.LoadEquipment();

        Assert.Null(loaded);
    }

    [Fact]
    public void LoadEquipment_WithCorruptedFile_ThrowsJsonException()
    {
        File.WriteAllText(Path.Combine(_saveDirectory, "current_run_equipment.json"), "{ invalid json }{");

        Assert.Throws<JsonException>(() => _service.LoadEquipment());
    }

    [Fact]
    public void SaveEquipment_WhenWriteFails_ThrowsIOException()
    {
        var service = new EquipmentPersistenceService(
            new ControlledFileSystem
            {
                DirectoryExistsResult = true,
                WriteException = new IOException("write failed")
            },
            Path.Combine(Path.GetTempPath(), $"tlms-equipment-write-fail-{Guid.NewGuid():N}"));

        Assert.Throws<IOException>(() => service.SaveEquipment(new EquipmentSnapshot()));
    }

    [Fact]
    public void HasSave_AfterSave_ReturnsTrue()
    {
        var snapshot = new EquipmentSnapshot();
        _service.SaveEquipment(snapshot);

        Assert.True(_service.HasSave());
    }

    [Fact]
    public void HasSave_AfterClear_ReturnsFalse()
    {
        var snapshot = new EquipmentSnapshot();
        _service.SaveEquipment(snapshot);
        _service.ClearSave();

        Assert.False(_service.HasSave());
    }

    [Fact]
    public void ItemInstanceData_RoundTrip_PreservesData()
    {
        var original = CreateTestItem();
        var data = ItemInstanceData.FromItemInstance(original);
        var restored = data.ToItemInstance();

        Assert.Equal(original.Name, restored.Name);
        Assert.Equal(original.DefinitionId, restored.DefinitionId);
        Assert.Equal(original.Rarity, restored.Rarity);
        Assert.Equal(original.EquipSlot, restored.EquipSlot);
        Assert.Equal(original.Affixes.Count, restored.Affixes.Count);
        
        for (int i = 0; i < original.Affixes.Count; i++)
        {
            Assert.Equal(original.Affixes[i].Type, restored.Affixes[i].Type);
            Assert.Equal(original.Affixes[i].Value, restored.Affixes[i].Value, precision: 4);
        }
    }

    private static ItemInstance CreateTestItem()
    {
        var affixes = new List<RolledAffix>
        {
            new(AffixType.PowerAdditive, 0.3f),
            new(AffixType.CritChanceAdditive, 0.1f)
        };

        return new ItemInstance(
            "test_item",
            "Test Item",
            ItemType.Weapon,
            EquipSlot.Weapon,
            ItemRarity.Rare,
            affixes);
    }
}
