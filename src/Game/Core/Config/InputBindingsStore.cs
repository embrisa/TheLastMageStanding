using System;
using System.IO;
using System.Text.Json;

namespace TheLastMageStanding.Game.Core.Config;

/// <summary>
/// Persists input binding configuration to disk. Falls back to defaults on load
/// failures.
/// </summary>
internal sealed class InputBindingsStore
{
    private const string FileName = "input-bindings.json";
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
            Console.WriteLine($"[InputBindingsStore] Failed to load settings, using defaults. Error: {ex.Message}");
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
            Console.WriteLine($"[InputBindingsStore] Failed to save settings: {ex.Message}");
        }
    }
}

