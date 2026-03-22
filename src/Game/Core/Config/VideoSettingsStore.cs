using System;
using System.IO;
using System.Text.Json;
using TheLastMageStanding.Game.Core.Diagnostics;

namespace TheLastMageStanding.Game.Core.Config;

/// <summary>
/// Persists video/backbuffer settings to disk. Falls back to defaults on errors.
/// </summary>
internal sealed class VideoSettingsStore
{
    private const string FileName = "video-settings.json";
    private const string LogCategory = "Config.Video";
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
            RuntimeLog.Warning(LogCategory, "Failed to load video settings. Falling back to defaults.");
            RuntimeLog.Error(LogCategory, $"Video settings load failed for '{_settingsPath}'.", ex);
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
            RuntimeLog.Error(LogCategory, $"Failed to save video settings to '{_settingsPath}'.", ex);
        }
    }
}
