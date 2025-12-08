using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TheLastMageStanding.Game.Core.Player;

/// <summary>
/// Serializable snapshot of player perks for persistence.
/// </summary>
[Serializable]
public sealed class PerkSnapshot
{
    [JsonInclude]
    public int AvailablePoints { get; set; }

    [JsonInclude]
    public int TotalPointsEarned { get; set; }

    [JsonInclude]
    public Dictionary<string, int> AllocatedRanks { get; set; } = new();
}

/// <summary>
/// Service for persisting perk allocations within a run.
/// </summary>
internal sealed class PerkPersistenceService
{
    private const string SaveFileName = "current_run_perks.json";
    private readonly string _savePath;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        IncludeFields = true
    };

    public PerkPersistenceService()
    {
        // Save to user's local app data
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var gameFolder = Path.Combine(appDataPath, "TheLastMageStanding");
        Directory.CreateDirectory(gameFolder);
        _savePath = Path.Combine(gameFolder, SaveFileName);
    }

    /// <summary>
    /// Save current perk state to disk.
    /// </summary>
    public void SavePerks(PerkSnapshot snapshot)
    {
        try
        {
            var json = JsonSerializer.Serialize(snapshot, JsonOptions);
            File.WriteAllText(_savePath, json);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to save perks: {ex.Message}");
        }
    }

    /// <summary>
    /// Load perk state from disk.
    /// Returns null if no save exists or loading fails.
    /// </summary>
    public PerkSnapshot? LoadPerks()
    {
        try
        {
            if (!File.Exists(_savePath))
                return null;

            var json = File.ReadAllText(_savePath);
            return JsonSerializer.Deserialize<PerkSnapshot>(json, JsonOptions);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to load perks: {ex.Message}");
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
            Console.WriteLine($"Failed to clear perk save: {ex.Message}");
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
