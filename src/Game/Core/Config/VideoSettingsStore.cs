using System;
using System.IO;
using System.Text.Json;

namespace TheLastMageStanding.Game.Core.Config;

/// <summary>
/// Persists video/backbuffer settings to disk. Falls back to defaults on errors.
/// </summary>
internal sealed class VideoSettingsStore
{
    private const string FileName = "video-settings.json";
    private readonly string _settingsPath;
    private readonly JsonSerializerOptions _serializerOptions = new()
    {
        WriteIndented = true
    };

    public VideoSettingsStore(string? customPath = null)
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

    public VideoSettingsConfig LoadOrDefault()
    {
        try
        {
            if (!File.Exists(_settingsPath))
            {
                return VideoSettingsConfig.Default;
            }

            var json = File.ReadAllText(_settingsPath);
            var config = JsonSerializer.Deserialize<VideoSettingsConfig>(json, _serializerOptions)
                         ?? VideoSettingsConfig.Default;
            config.Normalize();
            return config;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[VideoSettingsStore] Failed to load settings, using defaults. Error: {ex.Message}");
            return VideoSettingsConfig.Default;
        }
    }

    public void Save(VideoSettingsConfig config)
    {
        try
        {
            config.Normalize();
            var json = JsonSerializer.Serialize(config, _serializerOptions);
            File.WriteAllText(_settingsPath, json);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[VideoSettingsStore] Failed to save settings: {ex.Message}");
        }
    }
}

