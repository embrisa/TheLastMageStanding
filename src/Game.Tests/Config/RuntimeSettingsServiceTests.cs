using Microsoft.Xna.Framework.Input;
using TheLastMageStanding.Game.Core.Audio;
using TheLastMageStanding.Game.Core.Config;
using TheLastMageStanding.Game.Core.Ecs.Components;
using TheLastMageStanding.Game.Core.Events;
using Xunit;

namespace TheLastMageStanding.Game.Tests.Config;

public sealed class RuntimeSettingsServiceTests
{
    [Fact]
    public void TryApplyVideoChange_WindowScaleUpdatesBackBuffer()
    {
        var audioSettings = new AudioSettingsConfig();
        var videoSettings = new VideoSettingsConfig();
        var inputBindings = InputBindingsConfig.Default.Clone();
        var service = new RuntimeSettingsService(
            audioSettings,
            new AudioSettingsStore(),
            videoSettings,
            new VideoSettingsStore(),
            inputBindings,
            new InputBindingsStore(),
            new MusicService(audioSettings));
        var videoState = service.BuildVideoState();

        var changed = service.TryApplyVideoChange(
            new VideoSettingChangedEvent
            {
                Field = VideoSettingField.WindowScale,
                WindowScale = 3,
                Persist = false
            },
            ref videoState);

        Assert.True(changed);
        Assert.Equal(3, videoState.WindowScale);
        Assert.Equal(2880, videoState.BackBufferWidth);
        Assert.Equal(1620, videoState.BackBufferHeight);
    }

    [Fact]
    public void ApplyInputBindingChange_NormalizesUpdatedBinding()
    {
        var audioSettings = new AudioSettingsConfig();
        var videoSettings = new VideoSettingsConfig();
        var inputBindings = InputBindingsConfig.Default.Clone();
        var service = new RuntimeSettingsService(
            audioSettings,
            new AudioSettingsStore(),
            videoSettings,
            new VideoSettingsStore(),
            inputBindings,
            new InputBindingsStore(),
            new MusicService(audioSettings));

        var changed = service.ApplyInputBindingChange(
            new InputBindingChangedEvent
            {
                ActionId = InputActions.Attack,
                NewPrimary = Keys.F,
                NewAlternate = Keys.G,
                Persist = false
            });

        Assert.True(changed);
        var updatedBinding = inputBindings.GetBinding(InputActions.Attack);
        Assert.Equal(Keys.F, updatedBinding.Primary);
        Assert.Equal(Keys.G, updatedBinding.Alternate);
    }

    [Fact]
    public void BuildAudioState_MirrorsCurrentConfig()
    {
        var audioSettings = new AudioSettingsConfig
        {
            MasterVolume = 0.55f,
            MusicVolume = 0.4f,
            SfxMuted = true
        };
        var service = new RuntimeSettingsService(
            audioSettings,
            new AudioSettingsStore(),
            new VideoSettingsConfig(),
            new VideoSettingsStore(),
            InputBindingsConfig.Default.Clone(),
            new InputBindingsStore(),
            new MusicService(audioSettings));

        var state = service.BuildAudioState();

        Assert.Equal(0.55f, state.MasterVolume, 3);
        Assert.Equal(0.4f, state.MusicVolume, 3);
        Assert.True(state.SfxMuted);
    }
}
