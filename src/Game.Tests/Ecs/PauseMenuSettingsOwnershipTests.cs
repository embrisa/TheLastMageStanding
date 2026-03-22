using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using TheLastMageStanding.Game.Core.Audio;
using TheLastMageStanding.Game.Core.Camera;
using TheLastMageStanding.Game.Core.Config;
using TheLastMageStanding.Game.Core.Ecs;
using TheLastMageStanding.Game.Core.Ecs.Components;
using TheLastMageStanding.Game.Core.Ecs.Systems;
using TheLastMageStanding.Game.Core.Events;
using TheLastMageStanding.Game.Core.Input;
using Xunit;

namespace TheLastMageStanding.Game.Tests.Ecs;

public sealed class PauseMenuSettingsOwnershipTests
{
    [Fact]
    public void SettingsMenuSystem_Update_InitializesSessionSettingsState()
    {
        var world = CreateWorld(out var sessionEntity, out _);
        var audioSettings = AudioSettingsConfig.Default;
        audioSettings.MasterVolume = 0.42f;
        audioSettings.MusicVolume = 0.33f;
        audioSettings.MuteAll = true;

        var service = CreateRuntimeSettingsService(audioSettings);
        var settingsSystem = new SettingsMenuSystem(service);
        settingsSystem.Initialize(world);

        settingsSystem.Update(world, CreateContext());

        Assert.True(world.TryGetComponent(sessionEntity, out AudioSettingsState audioState));
        Assert.True(world.TryGetComponent(sessionEntity, out AudioSettingsMenu audioMenu));
        Assert.True(world.TryGetComponent(sessionEntity, out VideoSettingsState videoState));
        Assert.True(world.TryGetComponent(sessionEntity, out SettingsMenuState settingsMenu));

        Assert.Equal(0.4f, audioState.MasterVolume, 3);
        Assert.Equal(0.35f, audioState.MusicVolume, 3);
        Assert.True(audioState.MuteAll);
        Assert.False(audioMenu.IsOpen);
        Assert.False(settingsMenu.IsOpen);
        Assert.Equal("audio", settingsMenu.ActiveTab);
        Assert.Equal(VideoSettingsConfig.Default.WindowScale, videoState.WindowScale);
    }

    [Fact]
    public void PauseMenuSystem_Update_ConsumesOwnedSettingsState()
    {
        var world = CreateWorld(out var sessionEntity, out var eventBus);
        var audioSettings = AudioSettingsConfig.Default;
        audioSettings.MasterVolume = 0.25f;
        audioSettings.SfxVolume = 0.75f;

        var settingsSystem = new SettingsMenuSystem(CreateRuntimeSettingsService(audioSettings));
        settingsSystem.Initialize(world);
        settingsSystem.Update(world, CreateContext());

        var pauseSystem = new PauseMenuSystem();
        pauseSystem.Initialize(world);

        var viewModels = new List<PauseMenuViewModel>();
        eventBus.Subscribe<PauseMenuViewModelEvent>(evt => viewModels.Add(evt.ViewModel));

        pauseSystem.Update(world, CreateContext());
        eventBus.ProcessEvents();

        Assert.Single(viewModels);
        Assert.Equal(0.25f, viewModels[0].AudioState.MasterVolume, 3);
        Assert.Equal(0.75f, viewModels[0].AudioState.SfxVolume, 3);
        Assert.False(viewModels[0].AudioMenu.IsOpen);
        Assert.True(world.TryGetComponent(sessionEntity, out AudioSettingsState _));
        Assert.True(world.TryGetComponent(sessionEntity, out AudioSettingsMenu _));
        Assert.True(world.TryGetComponent(sessionEntity, out SettingsMenuState _));
    }

    [Fact]
    public void PauseMenuSystem_Update_WithoutOwnedSettingsState_FailsFast()
    {
        var world = CreateWorld(out _, out _);
        var pauseSystem = new PauseMenuSystem();
        pauseSystem.Initialize(world);

        var exception = Assert.Throws<InvalidOperationException>(() => pauseSystem.Update(world, CreateContext()));

        Assert.Contains("SettingsMenuSystem", exception.Message, StringComparison.Ordinal);
    }

    private static EcsWorld CreateWorld(out Entity sessionEntity, out EventBus eventBus)
    {
        eventBus = new EventBus();
        var world = new EcsWorld
        {
            EventBus = eventBus
        };

        sessionEntity = world.CreateEntity();
        world.SetComponent(sessionEntity, new GameSession());
        return world;
    }

    private static RuntimeSettingsService CreateRuntimeSettingsService(AudioSettingsConfig audioSettings)
    {
        return new RuntimeSettingsService(
            audioSettings,
            new AudioSettingsStore(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"), "audio-settings.json")),
            VideoSettingsConfig.Default,
            new VideoSettingsStore(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"), "video-settings.json")),
            InputBindingsConfig.Default.Clone(),
            new InputBindingsStore(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"), "input-bindings.json")),
            new MusicService(audioSettings));
    }

    private static EcsUpdateContext CreateContext() =>
        new(
            new GameTime(),
            0.016f,
            new InputState(),
            new Camera2D(960, 540),
            Vector2.Zero);
}
