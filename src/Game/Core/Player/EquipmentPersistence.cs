using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using TheLastMageStanding.Game.Core.Loot;

namespace TheLastMageStanding.Game.Core.Player;

/// <summary>
/// Serializable snapshot of equipped items for persistence.
/// </summary>
[Serializable]
public sealed class EquipmentSnapshot
{
    [JsonInclude]
    public Dictionary<EquipSlot, ItemInstanceData> EquippedItems { get; set; } = new();
    
    [JsonInclude]
    public List<ItemInstanceData> InventoryItems { get; set; } = new();
}

/// <summary>
/// Serializable item instance data.
/// </summary>
[Serializable]
public sealed class ItemInstanceData
{
    [JsonInclude]
    public string DefinitionId { get; set; } = string.Empty;
    
    [JsonInclude]
    public string Name { get; set; } = string.Empty;
    
    [JsonInclude]
    public ItemType ItemType { get; set; }
    
    [JsonInclude]
    public EquipSlot EquipSlot { get; set; }
    
    [JsonInclude]
    public ItemRarity Rarity { get; set; }
    
    [JsonInclude]
    public List<AffixData> Affixes { get; set; } = new();
    
    [JsonInclude]
    public Guid InstanceId { get; set; }

    public ItemInstance ToItemInstance()
    {
        var affixes = new List<RolledAffix>();
        foreach (var affix in Affixes)
        {
            affixes.Add(new RolledAffix(affix.Type, affix.Value));
        }
        
        return new ItemInstance(
            DefinitionId,
            Name,
            ItemType,
            EquipSlot,
            Rarity,
            affixes);
    }

    public static ItemInstanceData FromItemInstance(ItemInstance item)
    {
        var affixes = new List<AffixData>();
        foreach (var affix in item.Affixes)
        {
            affixes.Add(new AffixData { Type = affix.Type, Value = affix.Value });
        }

        return new ItemInstanceData
        {
            DefinitionId = item.DefinitionId,
            Name = item.Name,
            ItemType = item.ItemType,
            EquipSlot = item.EquipSlot,
            Rarity = item.Rarity,
            Affixes = affixes,
            InstanceId = item.InstanceId
        };
    }
}

/// <summary>
/// Serializable affix data.
/// </summary>
[Serializable]
public sealed class AffixData
{
    [JsonInclude]
    public AffixType Type { get; set; }
    
    [JsonInclude]
    public float Value { get; set; }
}

/// <summary>
/// Service for persisting equipment and inventory within a run.
/// </summary>
internal sealed class EquipmentPersistenceService
{
    private const string SaveFileName = "current_run_equipment.json";
    private readonly string _savePath;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        IncludeFields = true
    };

    public EquipmentPersistenceService()
    {
        // Save to user's local app data
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var gameFolder = Path.Combine(appDataPath, "TheLastMageStanding");
        Directory.CreateDirectory(gameFolder);
        _savePath = Path.Combine(gameFolder, SaveFileName);
    }

    /// <summary>
    /// Save current equipment and inventory to disk.
    /// </summary>
    public void SaveEquipment(EquipmentSnapshot snapshot)
    {
        try
        {
            var json = JsonSerializer.Serialize(snapshot, JsonOptions);
            File.WriteAllText(_savePath, json);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to save equipment: {ex.Message}");
        }
    }

    /// <summary>
    /// Load equipment and inventory from disk.
    /// Returns null if no save exists or loading fails.
    /// </summary>
    public EquipmentSnapshot? LoadEquipment()
    {
        try
        {
            if (!File.Exists(_savePath))
                return null;

            var json = File.ReadAllText(_savePath);
            return JsonSerializer.Deserialize<EquipmentSnapshot>(json, JsonOptions);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to load equipment: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Clear the current run save file.
    /// </summary>
    public void ClearSave()
    {
        try
        {
            if (File.Exists(_savePath))
                File.Delete(_savePath);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to clear equipment save: {ex.Message}");
        }
    }

    /// <summary>
    /// Check if a save file exists.
    /// </summary>
    public bool HasSave()
    {
        return File.Exists(_savePath);
    }
}
