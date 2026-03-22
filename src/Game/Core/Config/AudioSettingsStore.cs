using System;
using System.IO;
using System.Text.Json;
using TheLastMageStanding.Game.Core.Diagnostics;

namespace TheLastMageStanding.Game.Core.Config;

/// <summary>
/// Persists audio settings to a JSON file under the user's local app data.
/// Handles corrupt/missing files by falling back to defaults.
/// </summary>
internal sealed class AudioSettingsStore
{
    private const string FileName = "audio-settings.json";
    private const string LogCategory = "Config.Audio";
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
            RuntimeLog.Warning(LogCategory, "Failed to load audio settings. Falling back to defaults.");
            RuntimeLog.Error(LogCategory, $"Audio settings load failed for '{_settingsPath}'.", ex);
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
            RuntimeLog.Error(LogCategory, $"Failed to save audio settings to '{_settingsPath}'.", ex);
        }
    }
}
