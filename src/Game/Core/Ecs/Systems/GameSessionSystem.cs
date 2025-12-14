using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using TheLastMageStanding.Game.Core.Ecs.Components;
using TheLastMageStanding.Game.Core.Events;
using TheLastMageStanding.Game.Core.Config;
using TheLastMageStanding.Game.Core.Input;
using TheLastMageStanding.Game.Core.Audio;

namespace TheLastMageStanding.Game.Core.Ecs.Systems;

/// <summary>
/// Manages game session state, subscribes to wave and player death events,
/// handles restart input, and creates HUD notifications.
/// </summary>
internal sealed class GameSessionSystem : IUpdateSystem
{
    private readonly AudioSettingsConfig _audioSettings;
    private readonly AudioSettingsStore _audioSettingsStore;
    private readonly VideoSettingsConfig _videoSettings;
    private readonly VideoSettingsStore _videoSettingsStore;
    private readonly InputBindingsConfig _inputBindings;
    private readonly InputBindingsStore _inputBindingsStore;
    private readonly MusicService _musicService;
    private readonly SfxSystem _sfxSystem;
    private EcsWorld _world = null!;
    private Entity? _sessionEntity;
    private Entity? _notificationEntity;
    public bool ExitRequested { get; private set; }

    private enum PauseMenuOption
    {
        Resume = 0,
        Restart = 1,
        Settings = 2,
        Quit = 3,
    }

    private const float SliderStep = 0.05f;
    private const float SampleCooldownSeconds = 0.2f;

    public GameSessionSystem(
        AudioSettingsConfig audioSettings,
        AudioSettingsStore audioSettingsStore,
        VideoSettingsConfig videoSettings,
        VideoSettingsStore videoSettingsStore,
        InputBindingsConfig inputBindings,
        InputBindingsStore inputBindingsStore,
        MusicService musicService,
        SfxSystem sfxSystem)
    {
        _audioSettings = audioSettings;
        _audioSettingsStore = audioSettingsStore;
        _videoSettings = videoSettings;
        _videoSettingsStore = videoSettingsStore;
        _inputBindings = inputBindings;
        _inputBindingsStore = inputBindingsStore;
        _musicService = musicService;
        _sfxSystem = sfxSystem;
    }

    public void Initialize(EcsWorld world)
    {
        _world = world;
        world.EventBus.Subscribe<WaveStartedEvent>(OnWaveStarted);
        world.EventBus.Subscribe<WaveCompletedEvent>(OnWaveCompleted);
        world.EventBus.Subscribe<PlayerDiedEvent>(OnPlayerDied);
        world.EventBus.Subscribe<EnemyDiedEvent>(OnEnemyDied);
        world.EventBus.Subscribe<PauseMenuActionRequestedEvent>(OnPauseMenuActionRequested);
        world.EventBus.Subscribe<AudioSettingChangedEvent>(OnAudioSettingChanged);
        world.EventBus.Subscribe<VideoSettingChangedEvent>(OnVideoSettingChanged);
        world.EventBus.Subscribe<InputBindingChangedEvent>(OnInputBindingChanged);
        world.EventBus.Subscribe<SettingsTabChangedEvent>(OnSettingsTabChanged);
    }

    public void Update(EcsWorld world, in EcsUpdateContext context)
    {
        if (!TryCacheSessionEntity(world))
        {
            return;
        }

        if (!world.TryGetComponent<GameSession>(_sessionEntity!.Value, out var session))
        {
            return;
        }

        var audioState = EnsureAudioSettingsState(world, _sessionEntity.Value);
        var audioMenu = EnsureAudioSettingsMenu(world, _sessionEntity.Value);
        var videoState = EnsureVideoSettingsState(world, _sessionEntity.Value);
        var settingsMenu = EnsureSettingsMenuState(world, _sessionEntity.Value);
        var pauseMenu = EnsurePauseMenu(world, _sessionEntity.Value);
        var levelUpOpen = IsLevelUpChoiceOpen(world);

        TickAudioMenu(ref audioMenu, context.DeltaSeconds);

        // Force paused while level-up choice UI is open
        if (levelUpOpen && session.State != GameState.GameOver && session.State != GameState.Paused)
        {
            session.State = GameState.Paused;
            world.SetComponent(_sessionEntity.Value, session);
        }

        // Pause/resume toggle (Escape)
        if (!levelUpOpen && session.State != GameState.GameOver && context.Input.PausePressed)
        {
            if (session.State == GameState.Playing)
            {
                session.State = GameState.Paused;
                world.SetComponent(_sessionEntity.Value, session);
                pauseMenu = EnsurePauseMenu(world, _sessionEntity.Value);
                pauseMenu.SelectedIndex = 0;
                world.SetComponent(_sessionEntity.Value, pauseMenu);
            }
            else if (session.State == GameState.Paused)
            {
                if (settingsMenu.IsOpen)
                {
                    settingsMenu.IsOpen = false;
                    world.SetComponent(_sessionEntity.Value, settingsMenu);
                }
                else if (audioMenu.IsOpen)
                {
                    audioMenu.IsOpen = false;
                    world.SetComponent(_sessionEntity.Value, audioMenu);
                }
                else
                {
                    session.State = GameState.Playing;
                    world.SetComponent(_sessionEntity.Value, session);
                }
            }
        }

        if (session.State == GameState.Paused)
        {
            if (!levelUpOpen && !settingsMenu.IsOpen)
            {
                HandlePauseMenu(world, context.Input, ref session, ref audioState, ref audioMenu, ref pauseMenu, ref settingsMenu);
            }
            PublishPauseMenuViewModel(world, session, pauseMenu, audioState, audioMenu, levelUpOpen, settingsMenu.IsOpen);
            PublishSettingsMenuViewModel(world, settingsMenu, audioState, audioMenu, videoState);
            world.SetComponent(_sessionEntity.Value, audioMenu);
            world.SetComponent(_sessionEntity.Value, settingsMenu);
            world.SetComponent(_sessionEntity.Value, videoState);
            return;
        }

        if (settingsMenu.IsOpen)
        {
            settingsMenu.IsOpen = false;
            world.SetComponent(_sessionEntity.Value, settingsMenu);
        }

        if (levelUpOpen)
        {
            return;
        }

        // Update wave timer if playing
        if (session.State == GameState.Playing)
        {
            session.WaveTimer += context.DeltaSeconds;
            session.TimeSurvived += context.DeltaSeconds;
            world.SetComponent(_sessionEntity.Value, session);
        }

        // Handle restart input if game over
        if (session.State == GameState.GameOver)
        {
            if (context.Input.RestartPressed || context.Input.MenuConfirmPressed)
            {
                RestartSession(world, ref session);
            }
        }

        UpdateNotificationTimer(world, context.DeltaSeconds);
        PublishPauseMenuViewModel(world, session, pauseMenu, audioState, audioMenu, levelUpOpen, settingsMenu.IsOpen);
        PublishSettingsMenuViewModel(world, settingsMenu, audioState, audioMenu, videoState);
        world.SetComponent(_sessionEntity.Value, audioMenu);
    }

    /// <summary>
    /// Resets stage run state when starting a new stage (or restarting the same stage via transition).
    /// </summary>
    public void ResetForNewStage(EcsWorld world)
    {
        if (!TryCacheSessionEntity(world) || _sessionEntity is not Entity sessionEntity)
        {
            return;
        }

        if (world.TryGetComponent(sessionEntity, out GameSession session))
        {
            RestartSession(world, ref session);
        }
    }

    private bool TryCacheSessionEntity(EcsWorld world)
    {
        if (_sessionEntity is not null && world.IsAlive(_sessionEntity.Value))
        {
            return true;
        }

        _sessionEntity = null;
        world.ForEach<GameSession>((Entity entity, ref GameSession _) =>
        {
            _sessionEntity = entity;
        });

        return _sessionEntity.HasValue;
    }

    private AudioSettingsState EnsureAudioSettingsState(EcsWorld world, Entity sessionEntity)
    {
        if (!world.TryGetComponent(sessionEntity, out AudioSettingsState audioState))
        {
            audioState = new AudioSettingsState(
                _audioSettings.MasterVolume,
                _audioSettings.MusicVolume,
                _audioSettings.SfxVolume,
                _audioSettings.UiVolume,
                _audioSettings.VoiceVolume,
                _audioSettings.MasterMuted,
                _audioSettings.MusicMuted,
                _audioSettings.SfxMuted,
                _audioSettings.UiMuted,
                _audioSettings.VoiceMuted,
                _audioSettings.MuteAll);
            world.SetComponent(sessionEntity, audioState);
        }

        SyncAudioSettings(ref audioState, persist: false);
        return audioState;
    }

    private VideoSettingsState EnsureVideoSettingsState(EcsWorld world, Entity sessionEntity)
    {
        if (!world.TryGetComponent(sessionEntity, out VideoSettingsState videoState))
        {
            videoState = new VideoSettingsState(
                _videoSettings.Fullscreen,
                _videoSettings.VSync,
                _videoSettings.ReduceStatusEffectFlashing,
                _videoSettings.BackBufferWidth,
                _videoSettings.BackBufferHeight,
                _videoSettings.WindowScale);
            world.SetComponent(sessionEntity, videoState);
        }

        return videoState;
    }

    private static SettingsMenuState EnsureSettingsMenuState(EcsWorld world, Entity sessionEntity)
    {
        if (!world.TryGetComponent(sessionEntity, out SettingsMenuState settingsMenu))
        {
            settingsMenu = new SettingsMenuState(false, "audio");
            world.SetComponent(sessionEntity, settingsMenu);
        }

        return settingsMenu;
    }

    private static bool IsLevelUpChoiceOpen(EcsWorld world)
    {
        var open = false;
        world.ForEach<LevelUpChoiceState>((Entity _, ref LevelUpChoiceState state) =>
        {
            if (state.IsOpen)
            {
                open = true;
            }
        });
        return open;
    }

    private static PauseMenu EnsurePauseMenu(EcsWorld world, Entity sessionEntity)
    {
        if (!world.TryGetComponent(sessionEntity, out PauseMenu pauseMenu))
        {
            pauseMenu = new PauseMenu(0);
            world.SetComponent(sessionEntity, pauseMenu);
        }

        return pauseMenu;
    }

    private static AudioSettingsMenu EnsureAudioSettingsMenu(EcsWorld world, Entity sessionEntity)
    {
        if (!world.TryGetComponent(sessionEntity, out AudioSettingsMenu audioMenu))
        {
            audioMenu = new AudioSettingsMenu(false);
            world.SetComponent(sessionEntity, audioMenu);
        }

        return audioMenu;
    }

    private static void TickAudioMenu(ref AudioSettingsMenu audioMenu, float deltaSeconds)
    {
        audioMenu.SampleCooldownSeconds = Math.Max(0f, audioMenu.SampleCooldownSeconds - deltaSeconds);
        audioMenu.ConfirmationTimerSeconds = Math.Max(0f, audioMenu.ConfirmationTimerSeconds - deltaSeconds);
        if (audioMenu.ConfirmationTimerSeconds <= 0f)
        {
            audioMenu.ConfirmationText = string.Empty;
        }
    }

    private void HandlePauseMenu(
        EcsWorld world,
        InputState input,
        ref GameSession session,
        ref AudioSettingsState audioState,
        ref AudioSettingsMenu audioMenu,
        ref PauseMenu pauseMenu,
        ref SettingsMenuState settingsMenu)
    {
        if (_sessionEntity is not Entity sessionEntity)
        {
            return;
        }

        var optionCount = Enum.GetValues<PauseMenuOption>().Length;
        var selectionChanged = false;

        if (input.MenuUpPressed)
        {
            pauseMenu.SelectedIndex = (pauseMenu.SelectedIndex - 1 + optionCount) % optionCount;
            selectionChanged = true;
        }

        if (input.MenuDownPressed)
        {
            pauseMenu.SelectedIndex = (pauseMenu.SelectedIndex + 1) % optionCount;
            selectionChanged = true;
        }

        if (selectionChanged)
        {
            PlayUiHover(world);
        }

        if (input.MenuConfirmPressed)
        {
            PlayUiClick(world);
            switch ((PauseMenuOption)pauseMenu.SelectedIndex)
            {
                case PauseMenuOption.Resume:
                    session.State = GameState.Playing;
                    world.SetComponent(sessionEntity, session);
                    break;
                case PauseMenuOption.Restart:
                    RestartSession(world, ref session);
                    pauseMenu.SelectedIndex = 0;
                    break;
                case PauseMenuOption.Settings:
                    settingsMenu.IsOpen = true;
                    settingsMenu.ActiveTab = "audio";
                    audioMenu.IsOpen = false;
                    world.SetComponent(sessionEntity, settingsMenu);
                    pauseMenu.SelectedIndex = 0;
                    world.SetComponent(sessionEntity, pauseMenu);
                    world.SetComponent(sessionEntity, audioMenu);
                    break;
                case PauseMenuOption.Quit:
                    ExitRequested = true;
                    break;
            }
        }

        world.SetComponent(sessionEntity, pauseMenu);
    }

    private void HandleAudioSettingsMenu(
        EcsWorld world,
        InputState input,
        float deltaSeconds,
        ref AudioSettingsState audioState,
        ref AudioSettingsMenu audioMenu)
    {
        if (_sessionEntity is not Entity sessionEntity)
        {
            return;
        }

        const int controlCount = 12; // 5 sliders, 6 toggles, 1 back

        audioMenu.SampleCooldownSeconds = Math.Max(0f, audioMenu.SampleCooldownSeconds - deltaSeconds);
        audioMenu.ConfirmationTimerSeconds = Math.Max(0f, audioMenu.ConfirmationTimerSeconds - deltaSeconds);
        if (audioMenu.ConfirmationTimerSeconds <= 0f)
        {
            audioMenu.ConfirmationText = string.Empty;
        }

        if (audioMenu.SelectedIndex < 0)
        {
            audioMenu.SelectedIndex = 0;
        }
        else if (audioMenu.SelectedIndex >= controlCount)
        {
            audioMenu.SelectedIndex = controlCount - 1;
        }

        var selectionChanged = false;
        if (input.MenuUpPressed)
        {
            audioMenu.SelectedIndex = (audioMenu.SelectedIndex - 1 + controlCount) % controlCount;
            selectionChanged = true;
        }

        if (input.MenuDownPressed)
        {
            audioMenu.SelectedIndex = (audioMenu.SelectedIndex + 1) % controlCount;
            selectionChanged = true;
        }

        if (selectionChanged)
        {
            PlayUiHover(world);
        }

        var changed = false;
        var sampleCategory = SfxCategory.UI;

        if (IsSlider(audioMenu.SelectedIndex))
        {
            var delta = 0f;
            if (input.MenuLeftPressed) delta -= SliderStep;
            if (input.MenuRightPressed) delta += SliderStep;

            if (Math.Abs(delta) > 0f)
            {
                changed = AdjustSlider(ref audioState, audioMenu.SelectedIndex, delta);
                sampleCategory = GetSampleCategory(audioMenu.SelectedIndex);
            }
        }

        if (IsToggle(audioMenu.SelectedIndex) && input.MenuConfirmPressed)
        {
            ToggleSetting(ref audioState, audioMenu.SelectedIndex);
            changed = true;
            sampleCategory = GetSampleCategory(audioMenu.SelectedIndex);
            PlayUiClick(world);
        }

        var backSelected = audioMenu.SelectedIndex == 11;
        if ((backSelected && input.MenuConfirmPressed) || input.MenuBackPressed)
        {
            PlayUiCancel(world);
            audioMenu.IsOpen = false;
            audioMenu.SelectedIndex = 0;
            SyncAudioSettings(ref audioState, persist: true);
            audioMenu.ConfirmationText = "Audio settings saved";
            audioMenu.ConfirmationTimerSeconds = 1.0f;
            world.SetComponent(sessionEntity, audioState);
            world.SetComponent(sessionEntity, audioMenu);
            return;
        }

        if (changed)
        {
            SyncAudioSettings(ref audioState, persist: true);
            audioMenu.ConfirmationText = BuildConfirmationText(audioMenu.SelectedIndex, audioState);
            audioMenu.ConfirmationTimerSeconds = 1.0f;
            TryPlaySample(world, sampleCategory, ref audioMenu);
            world.SetComponent(sessionEntity, audioState);
        }

        world.SetComponent(sessionEntity, audioMenu);
    }

    private static bool IsSlider(int index) => index is >= 0 and <= 4;

    private static bool IsToggle(int index) => index is >= 5 and <= 10;

    private static float ClampAndSnap(float value)
    {
        var snapped = (float)Math.Round(value / SliderStep) * SliderStep;
        return Math.Clamp(snapped, 0f, 1f);
    }

    private static void PlayUiHover(EcsWorld world)
    {
        world.EventBus.Publish(new SfxPlayEvent("UserInterfaceOnHover", SfxCategory.UI, Vector2.Zero));
    }

    private static void PlayUiClick(EcsWorld world)
    {
        world.EventBus.Publish(new SfxPlayEvent("UserInterfaceOnClick", SfxCategory.UI, Vector2.Zero));
    }

    private static void PlayUiCancel(EcsWorld world) => PlayUiClick(world);

    private static bool AdjustSlider(ref AudioSettingsState audioState, int selectedIndex, float delta)
    {
        switch (selectedIndex)
        {
            case 0:
                audioState.MasterVolume = ClampAndSnap(audioState.MasterVolume + delta);
                return true;
            case 1:
                audioState.MusicVolume = ClampAndSnap(audioState.MusicVolume + delta);
                return true;
            case 2:
                audioState.SfxVolume = ClampAndSnap(audioState.SfxVolume + delta);
                return true;
            case 3:
                audioState.UiVolume = ClampAndSnap(audioState.UiVolume + delta);
                return true;
            case 4:
                audioState.VoiceVolume = ClampAndSnap(audioState.VoiceVolume + delta);
                return true;
            default:
                return false;
        }
    }

    private static void ToggleSetting(ref AudioSettingsState audioState, int selectedIndex)
    {
        switch (selectedIndex)
        {
            case 5:
                audioState.MuteAll = !audioState.MuteAll;
                break;
            case 6:
                audioState.MasterMuted = !audioState.MasterMuted;
                break;
            case 7:
                audioState.MusicMuted = !audioState.MusicMuted;
                break;
            case 8:
                audioState.SfxMuted = !audioState.SfxMuted;
                break;
            case 9:
                audioState.UiMuted = !audioState.UiMuted;
                break;
            case 10:
                audioState.VoiceMuted = !audioState.VoiceMuted;
                break;
        }
    }

    private static string BuildConfirmationText(int selectedIndex, AudioSettingsState audioState) => selectedIndex switch
    {
        0 => $"Master {(int)(audioState.MasterVolume * 100)}%",
        1 => $"Music {(int)(audioState.MusicVolume * 100)}%",
        2 => $"SFX {(int)(audioState.SfxVolume * 100)}%",
        3 => $"UI {(int)(audioState.UiVolume * 100)}%",
        4 => $"Voice {(int)(audioState.VoiceVolume * 100)}%",
        5 => audioState.MuteAll ? "Muted all" : "Unmuted all",
        6 => audioState.MasterMuted ? "Master muted" : "Master on",
        7 => audioState.MusicMuted ? "Music muted" : "Music on",
        8 => audioState.SfxMuted ? "SFX muted" : "SFX on",
        9 => audioState.UiMuted ? "UI muted" : "UI on",
        10 => audioState.VoiceMuted ? "Voice muted" : "Voice on",
        _ => "Audio updated",
    };

    private static SfxCategory GetSampleCategory(int selectedIndex) => selectedIndex switch
    {
        0 or 1 or 3 or 5 or 6 or 7 or 9 => SfxCategory.UI,
        4 or 10 => SfxCategory.Voice,
        _ => SfxCategory.Impact,
    };

    private static void TryPlaySample(EcsWorld world, SfxCategory category, ref AudioSettingsMenu audioMenu)
    {
        if (audioMenu.SampleCooldownSeconds > 0f)
        {
            return;
        }

        var soundName = category switch
        {
            SfxCategory.UI => "UserInterfaceOnClick",
            SfxCategory.Voice => "UserInterfaceOnHover",
            _ => "GameplayOnPlayerDeath",
        };

        audioMenu.SampleCooldownSeconds = SampleCooldownSeconds;
        world.EventBus.Publish(new SfxPlayEvent(soundName, category, Vector2.Zero));
    }

    private void SyncAudioSettings(ref AudioSettingsState audioState, bool persist)
    {
        audioState.MasterVolume = ClampAndSnap(audioState.MasterVolume);
        audioState.MusicVolume = ClampAndSnap(audioState.MusicVolume);
        audioState.SfxVolume = ClampAndSnap(audioState.SfxVolume);
        audioState.UiVolume = ClampAndSnap(audioState.UiVolume);
        audioState.VoiceVolume = ClampAndSnap(audioState.VoiceVolume);

        _audioSettings.MasterVolume = audioState.MasterVolume;
        _audioSettings.MusicVolume = audioState.MusicVolume;
        _audioSettings.SfxVolume = audioState.SfxVolume;
        _audioSettings.UiVolume = audioState.UiVolume;
        _audioSettings.VoiceVolume = audioState.VoiceVolume;
        _audioSettings.MasterMuted = audioState.MasterMuted;
        _audioSettings.MusicMuted = audioState.MusicMuted;
        _audioSettings.SfxMuted = audioState.SfxMuted;
        _audioSettings.UiMuted = audioState.UiMuted;
        _audioSettings.VoiceMuted = audioState.VoiceMuted;
        _audioSettings.MuteAll = audioState.MuteAll;

        _audioSettings.ApplyToMediaPlayer();
        _audioSettings.ApplyToSoundEffectMaster();
        _musicService.ApplySettings();
        _sfxSystem.ApplySettings();

        if (persist)
        {
            _audioSettingsStore.Save(_audioSettings);
        }
    }

    private void UpdateNotificationTimer(EcsWorld world, float deltaSeconds)
    {
        if (_notificationEntity.HasValue && world.IsAlive(_notificationEntity.Value))
        {
            if (world.TryGetComponent<WaveNotification>(_notificationEntity.Value, out var notification))
            {
                notification.RemainingSeconds -= deltaSeconds;
                if (notification.RemainingSeconds <= 0f)
                {
                    world.DestroyEntity(_notificationEntity.Value);
                    _notificationEntity = null;
                }
                else
                {
                    world.SetComponent(_notificationEntity.Value, notification);
                }
            }
        }

        // Update locked feature message timer
        if (_sessionEntity.HasValue && world.TryGetComponent(_sessionEntity.Value, out LockedFeatureMessage lockedMsg))
        {
            lockedMsg.RemainingSeconds -= deltaSeconds;
            if (lockedMsg.RemainingSeconds <= 0f)
            {
                world.RemoveComponent<LockedFeatureMessage>(_sessionEntity.Value);
            }
            else
            {
                world.SetComponent(_sessionEntity.Value, lockedMsg);
            }
        }
    }

    private static void PublishPauseMenuViewModel(
        EcsWorld world,
        GameSession session,
        PauseMenu pauseMenu,
        AudioSettingsState audioState,
        AudioSettingsMenu audioMenu,
        bool levelUpOpen,
        bool settingsOpen)
    {
        var isOpen = session.State == GameState.Paused && !levelUpOpen;
        world.EventBus.Publish(new PauseMenuViewModelEvent
        {
            ViewModel = new PauseMenuViewModel
            {
                IsOpen = isOpen,
                IsAudioOpen = audioMenu.IsOpen && isOpen,
                IsSettingsOpen = settingsOpen && isOpen,
                SelectedIndex = pauseMenu.SelectedIndex,
                AudioState = audioState,
                AudioMenu = audioMenu,
                LevelUpOpen = levelUpOpen
            }
        });
    }

    private void OnVideoSettingChanged(VideoSettingChangedEvent evt)
    {
        if (_sessionEntity is null || !_world.IsAlive(_sessionEntity.Value))
        {
            return;
        }

        var videoState = EnsureVideoSettingsState(_world, _sessionEntity.Value);
        var changed = false;

        switch (evt.Field)
        {
            case VideoSettingField.Fullscreen when evt.ToggleValue.HasValue:
                videoState.Fullscreen = evt.ToggleValue.Value;
                _videoSettings.Fullscreen = evt.ToggleValue.Value;
                changed = true;
                break;
            case VideoSettingField.VSync when evt.ToggleValue.HasValue:
                videoState.VSync = evt.ToggleValue.Value;
                _videoSettings.VSync = evt.ToggleValue.Value;
                changed = true;
                break;
            case VideoSettingField.ReduceStatusEffectFlashing when evt.ToggleValue.HasValue:
                videoState.ReduceStatusEffectFlashing = evt.ToggleValue.Value;
                _videoSettings.ReduceStatusEffectFlashing = evt.ToggleValue.Value;
                changed = true;
                break;
            case VideoSettingField.Resolution when evt.Resolution.HasValue:
                var (width, height) = evt.Resolution.Value;
                videoState.BackBufferWidth = Math.Max(640, width);
                videoState.BackBufferHeight = Math.Max(360, height);
                _videoSettings.BackBufferWidth = videoState.BackBufferWidth;
                _videoSettings.BackBufferHeight = videoState.BackBufferHeight;
                changed = true;
                break;
            case VideoSettingField.WindowScale when evt.WindowScale.HasValue:
                var scale = Math.Clamp(evt.WindowScale.Value, 1, 4);
                videoState.WindowScale = scale;
                videoState.BackBufferWidth = 960 * scale;
                videoState.BackBufferHeight = 540 * scale;
                _videoSettings.WindowScale = scale;
                _videoSettings.BackBufferWidth = videoState.BackBufferWidth;
                _videoSettings.BackBufferHeight = videoState.BackBufferHeight;
                changed = true;
                break;
        }

        if (!changed)
        {
            return;
        }

        _world.SetComponent(_sessionEntity.Value, videoState);

        if (evt.Persist)
        {
            _videoSettingsStore.Save(_videoSettings);
        }
    }

    private void OnInputBindingChanged(InputBindingChangedEvent evt)
    {
        if (string.IsNullOrWhiteSpace(evt.ActionId))
        {
            return;
        }

        _inputBindings.Bindings[evt.ActionId] = new InputBinding(evt.NewPrimary, evt.NewAlternate);
        _inputBindings.Normalize();

        if (evt.Persist)
        {
            _inputBindingsStore.Save(_inputBindings);
        }
    }

    private void OnSettingsTabChanged(SettingsTabChangedEvent evt)
    {
        if (_sessionEntity is null || !_world.IsAlive(_sessionEntity.Value))
        {
            return;
        }

        var settingsMenu = EnsureSettingsMenuState(_world, _sessionEntity.Value);
        settingsMenu.ActiveTab = string.IsNullOrWhiteSpace(evt.TabId) ? settingsMenu.ActiveTab : evt.TabId;
        _world.SetComponent(_sessionEntity.Value, settingsMenu);
    }

    private void PublishSettingsMenuViewModel(
        EcsWorld world,
        SettingsMenuState settingsMenu,
        AudioSettingsState audioState,
        AudioSettingsMenu audioMenu,
        VideoSettingsState videoState)
    {
        var videoConfig = new VideoSettingsConfig
        {
            Version = _videoSettings.Version,
            Fullscreen = videoState.Fullscreen,
            VSync = videoState.VSync,
            ReduceStatusEffectFlashing = videoState.ReduceStatusEffectFlashing,
            BackBufferWidth = videoState.BackBufferWidth,
            BackBufferHeight = videoState.BackBufferHeight,
            WindowScale = videoState.WindowScale
        };

        world.EventBus.Publish(new SettingsMenuViewModelEvent
        {
            ViewModel = new SettingsMenuViewModel
            {
                IsOpen = settingsMenu.IsOpen,
                ActiveTab = settingsMenu.ActiveTab,
                AudioState = audioState,
                AudioMenu = audioMenu,
                VideoSettings = videoConfig,
                Bindings = _inputBindings.Clone()
            }
        });
    }

    private void OnPauseMenuActionRequested(PauseMenuActionRequestedEvent evt)
    {
        if (_sessionEntity is null || !_world.IsAlive(_sessionEntity.Value))
        {
            return;
        }

        if (!_world.TryGetComponent(_sessionEntity.Value, out GameSession session))
        {
            return;
        }

        var audioMenu = EnsureAudioSettingsMenu(_world, _sessionEntity.Value);
        var pauseMenu = EnsurePauseMenu(_world, _sessionEntity.Value);
        var settingsMenu = EnsureSettingsMenuState(_world, _sessionEntity.Value);

        switch (evt.Action)
        {
            case PauseMenuAction.Resume:
                session.State = GameState.Playing;
                _world.SetComponent(_sessionEntity.Value, session);
                break;
            case PauseMenuAction.Restart:
                RestartSession(_world, ref session);
                pauseMenu.SelectedIndex = 0;
                _world.SetComponent(_sessionEntity.Value, pauseMenu);
                break;
            case PauseMenuAction.OpenSettings:
                settingsMenu.IsOpen = true;
                settingsMenu.ActiveTab = "audio";
                audioMenu.IsOpen = false;
                pauseMenu.SelectedIndex = (int)PauseMenuOption.Settings;
                _world.SetComponent(_sessionEntity.Value, pauseMenu);
                _world.SetComponent(_sessionEntity.Value, settingsMenu);
                _world.SetComponent(_sessionEntity.Value, audioMenu);
                break;
            case PauseMenuAction.CloseSettings:
                settingsMenu.IsOpen = false;
                _world.SetComponent(_sessionEntity.Value, settingsMenu);
                break;
            case PauseMenuAction.Quit:
                ExitRequested = true;
                break;
        }
    }

    private void OnAudioSettingChanged(AudioSettingChangedEvent evt)
    {
        if (_sessionEntity is null || !_world.IsAlive(_sessionEntity.Value))
        {
            return;
        }

        if (!_world.TryGetComponent(_sessionEntity.Value, out AudioSettingsState audioState))
        {
            return;
        }

        var audioMenu = EnsureAudioSettingsMenu(_world, _sessionEntity.Value);
        audioMenu.SelectedIndex = (int)evt.Field;

        var changed = ApplyAudioChange(evt, ref audioState);
        if (!changed)
        {
            _world.SetComponent(_sessionEntity.Value, audioMenu);
            return;
        }

        SyncAudioSettings(ref audioState, evt.Persist);
        audioMenu.ConfirmationText = BuildConfirmationText((int)evt.Field, audioState);
        audioMenu.ConfirmationTimerSeconds = 1.0f;
        TryPlaySample(_world, GetSampleCategory((int)evt.Field), ref audioMenu);

        _world.SetComponent(_sessionEntity.Value, audioState);
        _world.SetComponent(_sessionEntity.Value, audioMenu);
    }

    private static bool ApplyAudioChange(AudioSettingChangedEvent evt, ref AudioSettingsState audioState)
    {
        switch (evt.Field)
        {
            case AudioSettingField.MasterVolume when evt.Value.HasValue:
                audioState.MasterVolume = ClampAndSnap(evt.Value.Value);
                return true;
            case AudioSettingField.MusicVolume when evt.Value.HasValue:
                audioState.MusicVolume = ClampAndSnap(evt.Value.Value);
                return true;
            case AudioSettingField.SfxVolume when evt.Value.HasValue:
                audioState.SfxVolume = ClampAndSnap(evt.Value.Value);
                return true;
            case AudioSettingField.UiVolume when evt.Value.HasValue:
                audioState.UiVolume = ClampAndSnap(evt.Value.Value);
                return true;
            case AudioSettingField.VoiceVolume when evt.Value.HasValue:
                audioState.VoiceVolume = ClampAndSnap(evt.Value.Value);
                return true;
            case AudioSettingField.MuteAll when evt.ToggleValue.HasValue:
                audioState.MuteAll = evt.ToggleValue.Value;
                return true;
            case AudioSettingField.MasterMute when evt.ToggleValue.HasValue:
                audioState.MasterMuted = evt.ToggleValue.Value;
                return true;
            case AudioSettingField.MusicMute when evt.ToggleValue.HasValue:
                audioState.MusicMuted = evt.ToggleValue.Value;
                return true;
            case AudioSettingField.SfxMute when evt.ToggleValue.HasValue:
                audioState.SfxMuted = evt.ToggleValue.Value;
                return true;
            case AudioSettingField.UiMute when evt.ToggleValue.HasValue:
                audioState.UiMuted = evt.ToggleValue.Value;
                return true;
            case AudioSettingField.VoiceMute when evt.ToggleValue.HasValue:
                audioState.VoiceMuted = evt.ToggleValue.Value;
                return true;
            default:
                return false;
        }
    }

    private void OnWaveStarted(WaveStartedEvent evt)
    {
        if (_sessionEntity is null)
            return;

        if (_world.TryGetComponent<GameSession>(_sessionEntity.Value, out var session))
        {
            session.CurrentWave = evt.WaveIndex;
            session.WaveTimer = 0f;
            _world.SetComponent(_sessionEntity.Value, session);

            // Create notification
            CreateNotification($"Wave {evt.WaveIndex} Started!", 2.5f);
        }
    }

    private void OnWaveCompleted(WaveCompletedEvent evt)
    {
        CreateNotification($"Wave {evt.WaveIndex} Complete!", 2.5f);
    }

    private void OnEnemyDied(EnemyDiedEvent evt)
    {
        if (_sessionEntity is null)
            return;

        if (_world.TryGetComponent<GameSession>(_sessionEntity.Value, out var session))
        {
            session.EnemiesKilled++;
            _world.SetComponent(_sessionEntity.Value, session);
        }
    }

    private void OnPlayerDied(PlayerDiedEvent evt)
    {
        if (_sessionEntity is null)
            return;

        if (_world.TryGetComponent<GameSession>(_sessionEntity.Value, out var session))
        {
            session.State = GameState.GameOver;
            _world.SetComponent(_sessionEntity.Value, session);

            // Create persistent game over notification
            CreateNotification("GAME OVER", float.MaxValue);
        }
    }

    private void CreateNotification(string message, float duration)
    {
        // Remove existing notification if any
        if (_notificationEntity.HasValue && _world.IsAlive(_notificationEntity.Value))
        {
            _world.DestroyEntity(_notificationEntity.Value);
        }

        // Create new notification entity
        _notificationEntity = _world.CreateEntity();
        _world.SetComponent(_notificationEntity.Value, new WaveNotification(message, duration));
    }

    private void RestartSession(EcsWorld world, ref GameSession session)
    {
        if (_sessionEntity is null)
        {
            return;
        }

        var sessionEntity = _sessionEntity.Value;

        // Clear all enemies
        var enemiesToRemove = new List<Entity>();
        world.ForEach<Faction>((Entity entity, ref Faction faction) =>
        {
            if (faction == Faction.Enemy)
            {
                enemiesToRemove.Add(entity);
            }
        });
        foreach (var entity in enemiesToRemove)
        {
            world.DestroyEntity(entity);
        }

        // Clear all XP orbs
        var orbsToRemove = new List<Entity>();
        world.ForEach<XpOrb>((Entity entity, ref XpOrb _) =>
        {
            orbsToRemove.Add(entity);
        });
        foreach (var entity in orbsToRemove)
        {
            world.DestroyEntity(entity);
        }

        // Reset player position and velocity
        world.ForEach<PlayerTag, Position, Velocity>((Entity entity, ref PlayerTag _, ref Position pos, ref Velocity vel) =>
        {
            pos.Value = System.Numerics.Vector2.Zero;
            vel.Value = System.Numerics.Vector2.Zero;
            world.SetComponent(entity, pos);
            world.SetComponent(entity, vel);
        });

        // Reset movement and attack stats to base values
        world.ForEach<PlayerTag, MoveSpeed, AttackStats>(
            (Entity entity, ref PlayerTag _, ref MoveSpeed moveSpeed, ref AttackStats attackStats) =>
            {
                moveSpeed.Value = 220f;
                if (world.TryGetComponent(entity, out BaseMoveSpeed baseMove))
                {
                    baseMove.Value = 220f;
                    world.SetComponent(entity, baseMove);
                }
                attackStats.Damage = 20f;
                attackStats.CooldownTimer = 0f;

                world.SetComponent(entity, moveSpeed);
                world.SetComponent(entity, attackStats);
                world.RemoveComponent<ActiveStatusEffects>(entity);
                world.RemoveComponent<StatusEffectModifiers>(entity);

                if (world.TryGetComponent(entity, out ComputedStats computed))
                {
                    computed.IsDirty = true;
                    world.SetComponent(entity, computed);
                }
            });

        // Reset player health and XP to base values
        world.ForEach<PlayerTag, Health, PlayerXp>(
            (Entity entity, ref PlayerTag _, ref Health health, ref PlayerXp playerXp) =>
            {
                health.Max = 100f;
                health.Current = 100f;

                playerXp.Level = 1;
                playerXp.CurrentXp = 0;
                playerXp.XpToNextLevel = 10;

                world.SetComponent(entity, health);
                world.SetComponent(entity, playerXp);
            });

        // Reset session state
        session.State = GameState.Playing;
        session.CurrentWave = 0;
        session.WaveTimer = 0f;
        session.EnemiesKilled = 0;
        session.TimeSurvived = 0f;
        world.SetComponent(sessionEntity, session);

        // Reset pause menu selection
        if (world.TryGetComponent(sessionEntity, out PauseMenu pauseMenu))
        {
            pauseMenu.SelectedIndex = 0;
            world.SetComponent(sessionEntity, pauseMenu);
        }

        // Clear notification
        if (_notificationEntity.HasValue && world.IsAlive(_notificationEntity.Value))
        {
            world.DestroyEntity(_notificationEntity.Value);
            _notificationEntity = null;
        }

        world.EventBus.Publish(new SessionRestartedEvent());
    }
}
