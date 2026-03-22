using System;
using Microsoft.Xna.Framework;
using TheLastMageStanding.Game.Core.Config;
using TheLastMageStanding.Game.Core.Ecs.Components;
using TheLastMageStanding.Game.Core.Events;

namespace TheLastMageStanding.Game.Core.Ecs.Systems;

/// <summary>
/// Owns session-scoped settings menu state plus shared config synchronization.
/// </summary>
internal sealed class SettingsMenuSystem : IUpdateSystem
{
    private const float SampleCooldownSeconds = 0.2f;

    private readonly RuntimeSettingsService _settingsService;
    private EcsWorld _world = null!;
    private Entity? _sessionEntity;

    public SettingsMenuSystem(RuntimeSettingsService settingsService)
    {
        _settingsService = settingsService;
    }

    public void Initialize(EcsWorld world)
    {
        _world = world;
        world.EventBus.Subscribe<AudioSettingChangedEvent>(OnAudioSettingChanged);
        world.EventBus.Subscribe<VideoSettingChangedEvent>(OnVideoSettingChanged);
        world.EventBus.Subscribe<InputBindingChangedEvent>(OnInputBindingChanged);
        world.EventBus.Subscribe<SettingsTabChangedEvent>(OnSettingsTabChanged);
    }

    public void Update(EcsWorld world, in EcsUpdateContext context)
    {
        if (!TryGetSessionEntity(world, out var sessionEntity))
        {
            return;
        }

        EnsureSessionSettingsState(world, sessionEntity,
            out var audioState,
            out var audioMenu,
            out var videoState,
            out var settingsMenu);

        TickAudioMenu(ref audioMenu, context.DeltaSeconds);

        world.EventBus.Publish(new SettingsMenuViewModelEvent
        {
            ViewModel = new SettingsMenuViewModel
            {
                IsOpen = settingsMenu.IsOpen,
                ActiveTab = settingsMenu.ActiveTab,
                AudioState = audioState,
                AudioMenu = audioMenu,
                VideoSettings = _settingsService.BuildVideoConfig(videoState),
                Bindings = _settingsService.CloneInputBindings()
            }
        });

        world.SetComponent(sessionEntity, audioMenu);
        world.SetComponent(sessionEntity, audioState);
        world.SetComponent(sessionEntity, videoState);
        world.SetComponent(sessionEntity, settingsMenu);
    }

    private void OnAudioSettingChanged(AudioSettingChangedEvent evt)
    {
        if (!TryGetSessionEntity(_world, out var sessionEntity))
        {
            return;
        }

        EnsureSessionSettingsState(_world, sessionEntity,
            out var audioState,
            out var audioMenu,
            out _,
            out _);
        audioMenu.SelectedIndex = (int)evt.Field;

        if (!_settingsService.TryApplyAudioChange(evt, ref audioState, out var confirmation))
        {
            _world.SetComponent(sessionEntity, audioMenu);
            return;
        }

        audioMenu.ConfirmationText = confirmation;
        audioMenu.ConfirmationTimerSeconds = 1.0f;
        TryPlaySample(_world, GetSampleCategory((int)evt.Field), ref audioMenu);

        _world.SetComponent(sessionEntity, audioState);
        _world.SetComponent(sessionEntity, audioMenu);
    }

    private void OnVideoSettingChanged(VideoSettingChangedEvent evt)
    {
        if (!TryGetSessionEntity(_world, out var sessionEntity))
        {
            return;
        }

        EnsureSessionSettingsState(_world, sessionEntity,
            out _,
            out _,
            out var videoState,
            out _);
        if (!_settingsService.TryApplyVideoChange(evt, ref videoState))
        {
            return;
        }

        _world.SetComponent(sessionEntity, videoState);
    }

    private void OnInputBindingChanged(InputBindingChangedEvent evt)
    {
        _ = _settingsService.ApplyInputBindingChange(evt);
    }

    private void OnSettingsTabChanged(SettingsTabChangedEvent evt)
    {
        if (!TryGetSessionEntity(_world, out var sessionEntity))
        {
            return;
        }

        EnsureSessionSettingsState(_world, sessionEntity,
            out _,
            out _,
            out _,
            out var settingsMenu);
        settingsMenu.ActiveTab = string.IsNullOrWhiteSpace(evt.TabId) ? settingsMenu.ActiveTab : evt.TabId;
        _world.SetComponent(sessionEntity, settingsMenu);
    }

    private void EnsureSessionSettingsState(
        EcsWorld world,
        Entity sessionEntity,
        out AudioSettingsState audioState,
        out AudioSettingsMenu audioMenu,
        out VideoSettingsState videoState,
        out SettingsMenuState settingsMenu)
    {
        if (!world.TryGetComponent<AudioSettingsState>(sessionEntity, out var currentAudioState))
        {
            currentAudioState = _settingsService.BuildAudioState();
            world.SetComponent(sessionEntity, currentAudioState);
        }

        _settingsService.ApplyAudioState(ref currentAudioState, persist: false);
        if (!world.TryGetComponent<VideoSettingsState>(sessionEntity, out var currentVideoState))
        {
            currentVideoState = _settingsService.BuildVideoState();
            world.SetComponent(sessionEntity, currentVideoState);
        }

        if (!world.TryGetComponent<AudioSettingsMenu>(sessionEntity, out var currentAudioMenu))
        {
            currentAudioMenu = new AudioSettingsMenu(false);
            world.SetComponent(sessionEntity, currentAudioMenu);
        }

        if (!world.TryGetComponent<SettingsMenuState>(sessionEntity, out var currentSettingsMenu))
        {
            currentSettingsMenu = new SettingsMenuState(false, "audio");
            world.SetComponent(sessionEntity, currentSettingsMenu);
        }

        audioState = currentAudioState;
        audioMenu = currentAudioMenu;
        videoState = currentVideoState;
        settingsMenu = currentSettingsMenu;
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

    private static SfxCategory GetSampleCategory(int selectedIndex) => selectedIndex switch
    {
        0 or 1 or 3 or 5 or 6 or 7 or 9 => SfxCategory.UI,
        4 or 10 => SfxCategory.Voice,
        _ => SfxCategory.Impact,
    };

    private bool TryGetSessionEntity(EcsWorld world, out Entity sessionEntity)
    {
        if (_sessionEntity is not null && world.IsAlive(_sessionEntity.Value))
        {
            sessionEntity = _sessionEntity.Value;
            return true;
        }

        _sessionEntity = null;
        world.ForEach<GameSession>((Entity entity, ref GameSession _) =>
        {
            _sessionEntity = entity;
        });

        sessionEntity = _sessionEntity ?? default;
        return _sessionEntity.HasValue;
    }
}
