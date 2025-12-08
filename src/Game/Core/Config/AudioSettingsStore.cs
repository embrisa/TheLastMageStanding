using System;
using System.IO;
using System.Text.Json;

namespace TheLastMageStanding.Game.Core.Config;

/// <summary>
/// Persists audio settings to a JSON file under the user's local app data.
/// Handles corrupt/missing files by falling back to defaults.
/// </summary>
internal sealed class AudioSettingsStore
{
    private const string FileName = "audio-settings.json";
    private readonly string _settingsPath;
    private readonly JsonSerializerOptions _serializerOptions = new()
    {
        WriteIndented = true
    };

    public AudioSettingsStore(string? customPath = null)
    {
        if (!string.IsNullOrWhiteSpace(customPath))
        {
            var directory = Path.GetDirectoryName(customPath);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            _settingsPath = customPath;
            return;
        }

        var root = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "TheLastMageStanding");
        Directory.CreateDirectory(root);
        _settingsPath = Path.Combine(root, FileName);
    }

    public AudioSettingsConfig LoadOrDefault()
    {
        try
        {
            if (!File.Exists(_settingsPath))
            {
                return AudioSettingsConfig.Default;
            }

            var json = File.ReadAllText(_settingsPath);
            var config = JsonSerializer.Deserialize<AudioSettingsConfig>(json, _serializerOptions)
                         ?? AudioSettingsConfig.Default;

            config.Normalize();
            return config;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[AudioSettingsStore] Failed to load settings, using defaults. Error: {ex.Message}");
            return AudioSettingsConfig.Default;
        }
    }

    public void Save(AudioSettingsConfig config)
    {
        try
        {
            config.Normalize();
            var json = JsonSerializer.Serialize(config, _serializerOptions);
            File.WriteAllText(_settingsPath, json);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[AudioSettingsStore] Failed to save settings: {ex.Message}");
        }
    }
}

