using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using TheLastMageStanding.Game.Core.Diagnostics;
using TheLastMageStanding.Game.Core.Loot;
using TheLastMageStanding.Game.Core.MetaProgression;

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
    private const string LogCategory = "Persistence.Equipment";
    private readonly IFileSystem _fileSystem;
    private readonly string _savePath;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        IncludeFields = true
    };

    public EquipmentPersistenceService(IFileSystem fileSystem, string saveDirectory)
    {
        ArgumentNullException.ThrowIfNull(fileSystem);
        _fileSystem = fileSystem;
        if (string.IsNullOrWhiteSpace(saveDirectory))
        {
            throw new ArgumentException("Save directory is required.", nameof(saveDirectory));
        }

        if (!_fileSystem.DirectoryExists(saveDirectory))
        {
            _fileSystem.CreateDirectory(saveDirectory);
        }

        _savePath = Path.Combine(saveDirectory, SaveFileName);
    }

    /// <summary>
    /// Save current equipment and inventory to disk.
    /// </summary>
    public void SaveEquipment(EquipmentSnapshot snapshot)
    {
        try
        {
            var json = JsonSerializer.Serialize(snapshot, JsonOptions);
            _fileSystem.WriteAllText(_savePath, json);
        }
        catch (Exception ex)
        {
            RuntimeLog.Error(LogCategory, $"Failed to save equipment snapshot to '{_savePath}'.", ex);
            throw;
        }
    }

    /// <summary>
    /// Load equipment and inventory from disk.
    /// Returns null if no save exists or loading fails.
    /// </summary>
    public EquipmentSnapshot? LoadEquipment()
    {
        if (!_fileSystem.FileExists(_savePath))
        {
            return null;
        }

        try
        {
            var json = _fileSystem.ReadAllText(_savePath);
            return JsonSerializer.Deserialize<EquipmentSnapshot>(json, JsonOptions)
                ?? throw new InvalidDataException($"Equipment snapshot at '{_savePath}' is empty or invalid.");
        }
        catch (Exception ex)
        {
            RuntimeLog.Error(LogCategory, $"Failed to load equipment snapshot from '{_savePath}'.", ex);
            throw;
        }
    }

    /// <summary>
    /// Clear the current run save file.
    /// </summary>
    public void ClearSave()
    {
        try
        {
            if (_fileSystem.FileExists(_savePath))
            {
                _fileSystem.DeleteFile(_savePath);
            }
        }
        catch (Exception ex)
        {
            RuntimeLog.Error(LogCategory, $"Failed to clear equipment snapshot at '{_savePath}'.", ex);
            throw;
        }
    }

    /// <summary>
    /// Check if a save file exists.
    /// </summary>
    public bool HasSave()
    {
        return _fileSystem.FileExists(_savePath);
    }
}
