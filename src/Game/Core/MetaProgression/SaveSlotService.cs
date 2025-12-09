using System.Text.Json;

namespace TheLastMageStanding.Game.Core.MetaProgression;

public sealed record SaveSlotInfo(
    string SlotId,
    string SlotPath,
    bool HasProfileData,
    DateTime? CreatedAt,
    DateTime? LastPlayedAt);

/// <summary>
/// Manages save slot discovery, creation, and legacy migration.
/// Slots live under {SaveRoot}/Slots/{slotId}.
/// </summary>
public sealed class SaveSlotService
{
    private const string SlotPrefix = "slot";
    private const string SlotsFolderName = "Slots";
    private const string ProfileFileName = "player_profile.json";
    private const string RunHistoryFileName = "run_history.json";

    private readonly IFileSystem _fileSystem;
    private readonly string _saveRoot;
    private readonly JsonSerializerOptions _jsonOptions;

    public SaveSlotService(IFileSystem fileSystem, string? saveRoot = null)
    {
        _fileSystem = fileSystem;
        _saveRoot = saveRoot ?? PlayerProfileService.GetDefaultSaveDirectory();
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        EnsureDirectories();
        TryMigrateLegacySave();
    }

    public string SlotsRoot => Path.Combine(_saveRoot, SlotsFolderName);

    /// <summary>
    /// Returns all slots (with metadata if present), sorted by last played desc then slot id.
    /// </summary>
    public IReadOnlyList<SaveSlotInfo> GetSlots()
    {
        EnsureDirectories();
        var results = new List<SaveSlotInfo>();

        foreach (var directory in SafeGetDirectories(SlotsRoot))
        {
            var slotId = Path.GetFileName(directory);
            var info = BuildSlotInfo(slotId, directory);
            results.Add(info);
        }

        return results
            .OrderByDescending(s => s.LastPlayedAt ?? DateTime.MinValue)
            .ThenBy(s => s.SlotId, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    /// <summary>
    /// Returns the most recently played slot, or null if none.
    /// </summary>
    public SaveSlotInfo? GetMostRecentSlot()
    {
        return GetSlots().FirstOrDefault(s => s.HasProfileData);
    }

    /// <summary>
    /// Creates the next available slot (slot1, slot2, ...), writes a default profile, and returns metadata.
    /// </summary>
    public SaveSlotInfo CreateNextSlot()
    {
        EnsureDirectories();
        var slotId = GetNextSlotId();
        var slotPath = GetSlotPath(slotId);
        _fileSystem.CreateDirectory(slotPath);

        // Seed with a default profile so metadata is available immediately.
        var profileService = new PlayerProfileService(_fileSystem, slotPath);
        profileService.SaveProfile(PlayerProfile.CreateDefault());

        return BuildSlotInfo(slotId, slotPath);
    }

    /// <summary>
    /// Ensures a slot directory exists and returns its path.
    /// </summary>
    public string GetSlotPath(string slotId)
    {
        var slotPath = Path.Combine(SlotsRoot, slotId);
        if (!_fileSystem.DirectoryExists(slotPath))
        {
            _fileSystem.CreateDirectory(slotPath);
        }
        return slotPath;
    }

    /// <summary>
    /// Returns true if a slot directory exists.
    /// </summary>
    public bool SlotExists(string slotId)
    {
        var slotPath = Path.Combine(SlotsRoot, slotId);
        return _fileSystem.DirectoryExists(slotPath);
    }

    private SaveSlotInfo BuildSlotInfo(string slotId, string slotPath)
    {
        var profilePath = Path.Combine(slotPath, ProfileFileName);
        var hasProfile = _fileSystem.FileExists(profilePath);

        DateTime? createdAt = null;
        DateTime? lastPlayed = null;

        if (hasProfile)
        {
            var metadata = TryReadProfileMetadata(profilePath);
            createdAt = metadata?.CreatedAt;
            lastPlayed = metadata?.LastPlayedAt;
        }

        return new SaveSlotInfo(slotId, slotPath, hasProfile, createdAt, lastPlayed);
    }

    private (DateTime? CreatedAt, DateTime? LastPlayedAt)? TryReadProfileMetadata(string profilePath)
    {
        try
        {
            var json = _fileSystem.ReadAllText(profilePath);
            var profile = JsonSerializer.Deserialize<PlayerProfile>(json, _jsonOptions);
            return profile == null
                ? null
                : (profile.CreatedAt, profile.LastPlayedAt);
        }
        catch
        {
            return null;
        }
    }

    private void EnsureDirectories()
    {
        if (!_fileSystem.DirectoryExists(_saveRoot))
        {
            _fileSystem.CreateDirectory(_saveRoot);
        }

        if (!_fileSystem.DirectoryExists(SlotsRoot))
        {
            _fileSystem.CreateDirectory(SlotsRoot);
        }
    }

    private string GetNextSlotId()
    {
        var directories = SafeGetDirectories(SlotsRoot);
        var maxNumber = 0;

        foreach (var dir in directories)
        {
            var name = Path.GetFileName(dir);
            if (name.StartsWith(SlotPrefix, StringComparison.OrdinalIgnoreCase))
            {
                var suffix = name.Substring(SlotPrefix.Length);
                if (int.TryParse(suffix, out var number))
                {
                    maxNumber = Math.Max(maxNumber, number);
                }
            }
        }

        var nextNumber = maxNumber + 1;
        return $"{SlotPrefix}{nextNumber}";
    }

    private void TryMigrateLegacySave()
    {
        var legacyProfilePath = Path.Combine(_saveRoot, ProfileFileName);

        // Only migrate if legacy profile exists and there are no slots yet.
        if (!_fileSystem.FileExists(legacyProfilePath))
        {
            return;
        }

        if (SafeGetDirectories(SlotsRoot).Any())
        {
            return;
        }

        Console.WriteLine("[SaveSlotService] Migrating legacy single save to slot1");
        var slotPath = GetSlotPath($"{SlotPrefix}1");

        var targetProfilePath = Path.Combine(slotPath, ProfileFileName);
        _fileSystem.CopyFile(legacyProfilePath, targetProfilePath, overwrite: true);

        var legacyHistoryPath = Path.Combine(_saveRoot, RunHistoryFileName);
        if (_fileSystem.FileExists(legacyHistoryPath))
        {
            var targetHistoryPath = Path.Combine(slotPath, RunHistoryFileName);
            _fileSystem.CopyFile(legacyHistoryPath, targetHistoryPath, overwrite: true);
        }
    }

    private string[] SafeGetDirectories(string path)
    {
        try
        {
            return _fileSystem.GetDirectories(path);
        }
        catch
        {
            return Array.Empty<string>();
        }
    }
}

