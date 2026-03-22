using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using TheLastMageStanding.Game.Core.Diagnostics;
using TheLastMageStanding.Game.Core.MetaProgression;

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
    private const string LogCategory = "Persistence.Perks";
    private readonly IFileSystem _fileSystem;
    private readonly string _savePath;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        IncludeFields = true
    };

    public PerkPersistenceService(IFileSystem fileSystem, string saveDirectory)
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
    /// Save current perk state to disk.
    /// </summary>
    public void SavePerks(PerkSnapshot snapshot)
    {
        try
        {
            var json = JsonSerializer.Serialize(snapshot, JsonOptions);
            _fileSystem.WriteAllText(_savePath, json);
        }
        catch (Exception ex)
        {
            RuntimeLog.Error(LogCategory, $"Failed to save perk snapshot to '{_savePath}'.", ex);
            throw;
        }
    }

    /// <summary>
    /// Load perk state from disk.
    /// Returns null if no save exists or loading fails.
    /// </summary>
    public PerkSnapshot? LoadPerks()
    {
        if (!_fileSystem.FileExists(_savePath))
        {
            return null;
        }

        try
        {
            var json = _fileSystem.ReadAllText(_savePath);
            return JsonSerializer.Deserialize<PerkSnapshot>(json, JsonOptions)
                ?? throw new InvalidDataException($"Perk snapshot at '{_savePath}' is empty or invalid.");
        }
        catch (Exception ex)
        {
            RuntimeLog.Error(LogCategory, $"Failed to load perk snapshot from '{_savePath}'.", ex);
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
            RuntimeLog.Error(LogCategory, $"Failed to clear perk snapshot at '{_savePath}'.", ex);
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
