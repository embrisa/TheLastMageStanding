using System.Text.Json;

namespace TheLastMageStanding.Game.Core.MetaProgression;

/// <summary>
/// Manages persistence of player profile data with atomic writes and backup support.
/// </summary>
public sealed class PlayerProfileService
{
    private const string ProfileFileName = "player_profile.json";
    private const string BackupPrefix = "player_profile.backup";
    private const int MaxBackups = 3;

    private readonly IFileSystem _fileSystem;
    private readonly string _saveDirectory;
    private readonly JsonSerializerOptions _jsonOptions;

    public PlayerProfileService(IFileSystem fileSystem, string? saveDirectory = null)
    {
        _fileSystem = fileSystem;
        _saveDirectory = saveDirectory ?? GetDefaultSaveDirectory();
        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true
        };

        // Ensure save directory exists
        if (!_fileSystem.DirectoryExists(_saveDirectory))
        {
            _fileSystem.CreateDirectory(_saveDirectory);
        }
    }

    /// <summary>
    /// Gets the platform-specific default save directory.
    /// </summary>
    private static string GetDefaultSaveDirectory()
    {
        string baseDirectory;

        if (OperatingSystem.IsWindows())
        {
            baseDirectory = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        }
        else if (OperatingSystem.IsMacOS())
        {
            baseDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "Library",
                "Application Support"
            );
        }
        else // Linux
        {
            var xdgDataHome = Environment.GetEnvironmentVariable("XDG_DATA_HOME");
            baseDirectory = !string.IsNullOrEmpty(xdgDataHome)
                ? xdgDataHome
                : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".local", "share");
        }

        return Path.Combine(baseDirectory, "TheLastMageStanding");
    }

    /// <summary>
    /// Loads the player profile from disk. Creates a default profile if none exists or if corrupted.
    /// </summary>
    public PlayerProfile LoadProfile()
    {
        var profilePath = Path.Combine(_saveDirectory, ProfileFileName);

        if (!_fileSystem.FileExists(profilePath))
        {
            return PlayerProfile.CreateDefault();
        }

        try
        {
            var json = _fileSystem.ReadAllText(profilePath);
            var profile = JsonSerializer.Deserialize<PlayerProfile>(json, _jsonOptions);

            if (profile == null)
            {
                Console.WriteLine("Failed to deserialize profile, creating default.");
                return PlayerProfile.CreateDefault();
            }

            // Migrate profile if needed
            if (profile.SchemaVersion < 1)
            {
                profile = MigrateProfile(profile);
            }

            return profile;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading profile: {ex.Message}");
            Console.WriteLine("Attempting to restore from backup...");

            // Try to restore from backup
            var restoredProfile = TryRestoreFromBackup();
            if (restoredProfile != null)
            {
                Console.WriteLine("Successfully restored profile from backup.");
                return restoredProfile;
            }

            Console.WriteLine("No valid backup found, creating default profile.");
            return PlayerProfile.CreateDefault();
        }
    }

    /// <summary>
    /// Saves the player profile to disk using atomic write (temp file + rename).
    /// </summary>
    public void SaveProfile(PlayerProfile profile)
    {
        profile.LastPlayedAt = DateTime.UtcNow;

        var profilePath = Path.Combine(_saveDirectory, ProfileFileName);
        var tempPath = profilePath + ".tmp";

        try
        {
            // Create backup before saving
            if (_fileSystem.FileExists(profilePath))
            {
                BackupProfile();
            }

            // Serialize to temp file
            var json = JsonSerializer.Serialize(profile, _jsonOptions);
            _fileSystem.WriteAllText(tempPath, json);

            // Atomic rename
            if (_fileSystem.FileExists(profilePath))
            {
                _fileSystem.DeleteFile(profilePath);
            }
            _fileSystem.CopyFile(tempPath, profilePath, overwrite: true);
            _fileSystem.DeleteFile(tempPath);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error saving profile: {ex.Message}");
            // Clean up temp file if it exists
            if (_fileSystem.FileExists(tempPath))
            {
                try { _fileSystem.DeleteFile(tempPath); } catch { }
            }
            throw;
        }
    }

    /// <summary>
    /// Creates a backup of the current profile.
    /// Maintains a rolling window of the last N backups.
    /// </summary>
    public void BackupProfile()
    {
        var profilePath = Path.Combine(_saveDirectory, ProfileFileName);

        if (!_fileSystem.FileExists(profilePath))
        {
            return;
        }

        try
        {
            // Get existing backups and sort by timestamp
            var backups = _fileSystem.GetFiles(_saveDirectory, $"{BackupPrefix}*.json")
                .OrderByDescending(f => f)
                .ToList();

            // Remove oldest backups if we have too many
            while (backups.Count >= MaxBackups)
            {
                _fileSystem.DeleteFile(backups.Last());
                backups.RemoveAt(backups.Count - 1);
            }

            // Create new backup with timestamp
            var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss", System.Globalization.CultureInfo.InvariantCulture);
            var backupPath = Path.Combine(_saveDirectory, $"{BackupPrefix}_{timestamp}.json");
            _fileSystem.CopyFile(profilePath, backupPath, overwrite: false);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error creating backup: {ex.Message}");
            // Don't throw - backup failure shouldn't prevent saving
        }
    }

    /// <summary>
    /// Attempts to restore profile from the most recent valid backup.
    /// </summary>
    private PlayerProfile? TryRestoreFromBackup()
    {
        try
        {
            var backups = _fileSystem.GetFiles(_saveDirectory, $"{BackupPrefix}*.json")
                .OrderByDescending(f => f)
                .ToList();

            foreach (var backupPath in backups)
            {
                try
                {
                    var json = _fileSystem.ReadAllText(backupPath);
                    var profile = JsonSerializer.Deserialize<PlayerProfile>(json, _jsonOptions);
                    if (profile != null)
                    {
                        return profile;
                    }
                }
                catch
                {
                    // Try next backup
                    continue;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error restoring from backup: {ex.Message}");
        }

        return null;
    }

    /// <summary>
    /// Migrates profile from older schema versions.
    /// </summary>
    private static PlayerProfile MigrateProfile(PlayerProfile profile)
    {
        // Future migration logic goes here
        // For now, just update schema version
        profile.SchemaVersion = 1;
        return profile;
    }
}
