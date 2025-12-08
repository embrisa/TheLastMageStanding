using System.Text.Json;

namespace TheLastMageStanding.Game.Core.MetaProgression;

/// <summary>
/// Manages persistence and querying of run history data.
/// </summary>
public sealed class RunHistoryService
{
    private const string HistoryFileName = "run_history.json";
    private const int MaxHistorySize = 50;

    private readonly IFileSystem _fileSystem;
    private readonly string _saveDirectory;
    private readonly JsonSerializerOptions _jsonOptions;

    public RunHistoryService(IFileSystem fileSystem, string? saveDirectory = null)
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
    /// Saves a completed run to history.
    /// Maintains a rolling window of the last N runs.
    /// </summary>
    public void SaveRun(RunSession run)
    {
        try
        {
            var history = LoadHistory();
            
            // Add new run at the beginning
            history.Insert(0, run);

            // Trim to max size
            if (history.Count > MaxHistorySize)
            {
                history.RemoveRange(MaxHistorySize, history.Count - MaxHistorySize);
            }

            // Save to disk
            var historyPath = Path.Combine(_saveDirectory, HistoryFileName);
            var json = JsonSerializer.Serialize(history, _jsonOptions);
            _fileSystem.WriteAllText(historyPath, json);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error saving run history: {ex.Message}");
            // Don't throw - history save failure shouldn't crash the game
        }
    }

    /// <summary>
    /// Gets the most recent N runs.
    /// </summary>
    public List<RunSession> GetRecentRuns(int count)
    {
        var history = LoadHistory();
        return history.Take(count).ToList();
    }

    /// <summary>
    /// Gets all runs in history.
    /// </summary>
    public List<RunSession> GetAllRuns()
    {
        return LoadHistory();
    }

    /// <summary>
    /// Gets the best runs sorted by wave reached.
    /// </summary>
    public List<RunSession> GetBestRuns(int count = 10)
    {
        var history = LoadHistory();
        return history
            .OrderByDescending(r => r.WaveReached)
            .ThenByDescending(r => r.TotalKills)
            .Take(count)
            .ToList();
    }

    /// <summary>
    /// Gets runs that achieved personal records.
    /// </summary>
    public RunSession? GetBestRunByWave()
    {
        var history = LoadHistory();
        return history
            .OrderByDescending(r => r.WaveReached)
            .ThenByDescending(r => r.TotalKills)
            .FirstOrDefault();
    }

    /// <summary>
    /// Gets the run with the most kills.
    /// </summary>
    public RunSession? GetBestRunByKills()
    {
        var history = LoadHistory();
        return history
            .OrderByDescending(r => r.TotalKills)
            .FirstOrDefault();
    }

    /// <summary>
    /// Gets the run with the most gold collected.
    /// </summary>
    public RunSession? GetBestRunByGold()
    {
        var history = LoadHistory();
        return history
            .OrderByDescending(r => r.GoldCollected)
            .FirstOrDefault();
    }

    /// <summary>
    /// Loads run history from disk.
    /// </summary>
    private List<RunSession> LoadHistory()
    {
        var historyPath = Path.Combine(_saveDirectory, HistoryFileName);

        if (!_fileSystem.FileExists(historyPath))
        {
            return new List<RunSession>();
        }

        try
        {
            var json = _fileSystem.ReadAllText(historyPath);
            var history = JsonSerializer.Deserialize<List<RunSession>>(json, _jsonOptions);
            return history ?? new List<RunSession>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading run history: {ex.Message}");
            return new List<RunSession>();
        }
    }

    /// <summary>
    /// Clears all run history.
    /// </summary>
    public void ClearHistory()
    {
        var historyPath = Path.Combine(_saveDirectory, HistoryFileName);
        
        if (_fileSystem.FileExists(historyPath))
        {
            _fileSystem.DeleteFile(historyPath);
        }
    }
}
