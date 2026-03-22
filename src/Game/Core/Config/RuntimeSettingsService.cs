using System;
using TheLastMageStanding.Game.Core.Audio;
using TheLastMageStanding.Game.Core.Ecs.Components;
using TheLastMageStanding.Game.Core.Ecs.Systems;
using TheLastMageStanding.Game.Core.Events;

namespace TheLastMageStanding.Game.Core.Config;

/// <summary>
/// Centralizes runtime settings state creation, application, and persistence.
/// </summary>
internal sealed class RuntimeSettingsService
{
    private const float SliderStep = 0.05f;
    private const int VirtualWidth = 960;
    private const int VirtualHeight = 540;

    private readonly AudioSettingsConfig _audioSettings;
    private readonly AudioSettingsStore _audioSettingsStore;
    private readonly VideoSettingsConfig _videoSettings;
    private readonly VideoSettingsStore _videoSettingsStore;
    private readonly InputBindingsConfig _inputBindings;
    private readonly InputBindingsStore _inputBindingsStore;
    private readonly MusicService _musicService;
    private readonly SfxSystem? _sfxSystem;

    public RuntimeSettingsService(
        AudioSettingsConfig audioSettings,
        AudioSettingsStore audioSettingsStore,
        VideoSettingsConfig videoSettings,
        VideoSettingsStore videoSettingsStore,
        InputBindingsConfig inputBindings,
        InputBindingsStore inputBindingsStore,
        MusicService musicService,
        SfxSystem? sfxSystem = null)
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

    public AudioSettingsState BuildAudioState() => new(
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

    public VideoSettingsState BuildVideoState() => new(
        _videoSettings.Fullscreen,
        _videoSettings.VSync,
        _videoSettings.ReduceStatusEffectFlashing,
        _videoSettings.BackBufferWidth,
        _videoSettings.BackBufferHeight,
        _videoSettings.WindowScale);

    public VideoSettingsConfig BuildVideoConfig(VideoSettingsState state) => new()
    {
        Version = _videoSettings.Version,
        Fullscreen = state.Fullscreen,
        VSync = state.VSync,
        ReduceStatusEffectFlashing = state.ReduceStatusEffectFlashing,
        BackBufferWidth = state.BackBufferWidth,
        BackBufferHeight = state.BackBufferHeight,
        WindowScale = state.WindowScale
    };

    public VideoSettingsConfig CloneVideoSettings() => _videoSettings.Clone();

    public InputBindingsConfig CloneInputBindings() => _inputBindings.Clone();

    public bool TryApplyAudioChange(AudioSettingChangedEvent evt, ref AudioSettingsState audioState, out string confirmationText)
    {
        if (!ApplyAudioChange(evt, ref audioState))
        {
            confirmationText = string.Empty;
            return false;
        }

        ApplyAudioState(ref audioState, evt.Persist);
        confirmationText = BuildAudioConfirmation(evt.Field, audioState);
        return true;
    }

    public void ApplyAudioState(ref AudioSettingsState audioState, bool persist)
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
        _sfxSystem?.ApplySettings();

        if (persist)
        {
            _audioSettingsStore.Save(_audioSettings);
        }
    }

    public bool TryApplyVideoChange(VideoSettingChangedEvent evt, ref VideoSettingsState videoState)
    {
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
                videoState.BackBufferWidth = VirtualWidth * scale;
                videoState.BackBufferHeight = VirtualHeight * scale;
                _videoSettings.WindowScale = scale;
                _videoSettings.BackBufferWidth = videoState.BackBufferWidth;
                _videoSettings.BackBufferHeight = videoState.BackBufferHeight;
                changed = true;
                break;
        }

        if (changed && evt.Persist)
        {
            _videoSettingsStore.Save(_videoSettings);
        }

        return changed;
    }

    public bool ApplyInputBindingChange(InputBindingChangedEvent evt)
    {
        if (string.IsNullOrWhiteSpace(evt.ActionId))
        {
            return false;
        }

        _inputBindings.Bindings[evt.ActionId] = new InputBinding(evt.NewPrimary, evt.NewAlternate);
        _inputBindings.Normalize();

        if (evt.Persist)
        {
            _inputBindingsStore.Save(_inputBindings);
        }

        return true;
    }

    public static string BuildAudioConfirmation(AudioSettingField field, AudioSettingsState audioState) => field switch
    {
        AudioSettingField.MasterVolume => $"Master {(int)(audioState.MasterVolume * 100)}%",
        AudioSettingField.MusicVolume => $"Music {(int)(audioState.MusicVolume * 100)}%",
        AudioSettingField.SfxVolume => $"SFX {(int)(audioState.SfxVolume * 100)}%",
        AudioSettingField.UiVolume => $"UI {(int)(audioState.UiVolume * 100)}%",
        AudioSettingField.VoiceVolume => $"Voice {(int)(audioState.VoiceVolume * 100)}%",
        AudioSettingField.MuteAll => audioState.MuteAll ? "Muted all" : "Unmuted all",
        AudioSettingField.MasterMute => audioState.MasterMuted ? "Master muted" : "Master on",
        AudioSettingField.MusicMute => audioState.MusicMuted ? "Music muted" : "Music on",
        AudioSettingField.SfxMute => audioState.SfxMuted ? "SFX muted" : "SFX on",
        AudioSettingField.UiMute => audioState.UiMuted ? "UI muted" : "UI on",
        AudioSettingField.VoiceMute => audioState.VoiceMuted ? "Voice muted" : "Voice on",
        _ => "Audio updated"
    };

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

    private static float ClampAndSnap(float value)
    {
        var snapped = (float)Math.Round(value / SliderStep) * SliderStep;
        return Math.Clamp(snapped, 0f, 1f);
    }
}
