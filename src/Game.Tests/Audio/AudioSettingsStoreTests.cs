using System;
using System.IO;
using TheLastMageStanding.Game.Core.Config;
using Xunit;

namespace TheLastMageStanding.Game.Tests.Audio;

public class AudioSettingsStoreTests : IDisposable
{
    private readonly string _tempDirectory;
    private readonly string _tempFile;

    public AudioSettingsStoreTests()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        _tempFile = Path.Combine(_tempDirectory, "audio-settings.json");
    }

    [Fact]
    public void LoadOrDefault_WhenMissing_ReturnsDefaults()
    {
        var store = new AudioSettingsStore(_tempFile);

        var settings = store.LoadOrDefault();

        Assert.Equal(1f, settings.MasterVolume, 3);
        Assert.Equal(0.85f, settings.MusicVolume, 3);
    }

    [Fact]
    public void Save_AndReload_PersistsValues()
    {
        var store = new AudioSettingsStore(_tempFile);
        var settings = AudioSettingsConfig.Default;
        settings.MasterVolume = 0.55f;
        settings.MusicVolume = 0.42f;
        settings.MuteAll = true;

        store.Save(settings);
        var reloaded = store.LoadOrDefault();

        Assert.Equal(0.55f, reloaded.MasterVolume, 3);
        Assert.Equal(0.42f, reloaded.MusicVolume, 3);
        Assert.True(reloaded.MuteAll);
    }

    [Fact]
    public void LoadOrDefault_WithCorruptFile_FallsBackToDefault()
    {
        Directory.CreateDirectory(_tempDirectory);
        File.WriteAllText(_tempFile, "{ not valid json }");
        var store = new AudioSettingsStore(_tempFile);

        var settings = store.LoadOrDefault();

        Assert.Equal(AudioSettingsConfig.Default.MasterVolume, settings.MasterVolume, 3);
        Assert.False(settings.MuteAll);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }

        GC.SuppressFinalize(this);
    }
}

