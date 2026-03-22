using System;
using System.IO;
using System.Text.Json;
using TheLastMageStanding.Game.Core.Diagnostics;

namespace TheLastMageStanding.Game.Core.Config;

/// <summary>
/// Persists input binding configuration to disk. Falls back to defaults on load
/// failures.
/// </summary>
internal sealed class InputBindingsStore
{
    private const string FileName = "input-bindings.json";
    private const string LogCategory = "Config.Input";
    private readonly string _settingsPath;
    private readonly JsonSerializerOptions _serializerOptions = new()
    {
        WriteIndented = true
    };

    public InputBindingsStore(string? customPath = null)
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

    public InputBindingsConfig LoadOrDefault()
    {
        try
        {
            if (!File.Exists(_settingsPath))
            {
                return InputBindingsConfig.Default;
            }

            var json = File.ReadAllText(_settingsPath);
            var config = JsonSerializer.Deserialize<InputBindingsConfig>(json, _serializerOptions)
                         ?? InputBindingsConfig.Default;
            config.Normalize();
            return config;
        }
        catch (Exception ex)
        {
            RuntimeLog.Warning(LogCategory, "Failed to load input bindings. Falling back to defaults.");
            RuntimeLog.Error(LogCategory, $"Input bindings load failed for '{_settingsPath}'.", ex);
            return InputBindingsConfig.Default;
        }
    }

    public void Save(InputBindingsConfig config)
    {
        try
        {
            config.Normalize();
            var json = JsonSerializer.Serialize(config, _serializerOptions);
            File.WriteAllText(_settingsPath, json);
        }
        catch (Exception ex)
        {
            RuntimeLog.Error(LogCategory, $"Failed to save input bindings to '{_settingsPath}'.", ex);
        }
    }
}
