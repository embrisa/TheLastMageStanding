using Xunit;
using TheLastMageStanding.Game.Core.Player;
using TheLastMageStanding.Game.Core.Loot;
using System.Collections.Generic;
using System.IO;
using System;

namespace TheLastMageStanding.Game.Tests.Loot;

public sealed class EquipmentPersistenceTests : IDisposable
{
    private readonly EquipmentPersistenceService _service;

    public EquipmentPersistenceTests()
    {
        _service = new EquipmentPersistenceService();
        // Clear any existing save before tests
        _service.ClearSave();
    }

    public void Dispose()
    {
        // Clean up after tests
        _service.ClearSave();
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
