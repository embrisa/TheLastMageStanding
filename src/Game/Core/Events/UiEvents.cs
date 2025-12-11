using Microsoft.Xna.Framework.Input;
using TheLastMageStanding.Game.Core.Config;
using TheLastMageStanding.Game.Core.Ecs.Components;

namespace TheLastMageStanding.Game.Core.Events;

internal enum PauseMenuAction
{
    Resume,
    Restart,
    OpenSettings,
    CloseSettings,
    Quit
}

internal readonly struct PauseMenuViewModel
{
    public bool IsOpen { get; init; }
    public bool IsAudioOpen { get; init; }
    public bool IsSettingsOpen { get; init; }
    public int SelectedIndex { get; init; }
    public AudioSettingsState AudioState { get; init; }
    public AudioSettingsMenu AudioMenu { get; init; }
    public bool LevelUpOpen { get; init; }
}

internal readonly struct PauseMenuViewModelEvent
{
    public PauseMenuViewModel ViewModel { get; init; }
}

internal readonly struct PauseMenuActionRequestedEvent
{
    public PauseMenuAction Action { get; init; }
}

internal enum VideoSettingField
{
    Fullscreen = 0,
    VSync = 1,
    Resolution = 2,
    WindowScale = 3
}

internal readonly struct VideoSettingChangedEvent
{
    public VideoSettingField Field { get; init; }
    public bool? ToggleValue { get; init; }
    public (int Width, int Height)? Resolution { get; init; }
    public int? WindowScale { get; init; }
    public bool Persist { get; init; }
}

internal readonly struct InputBindingChangedEvent
{
    public string ActionId { get; init; }
    public Keys NewPrimary { get; init; }
    public Keys? NewAlternate { get; init; }
    public bool Persist { get; init; }
}

internal readonly struct SettingsMenuViewModel
{
    public bool IsOpen { get; init; }
    public string ActiveTab { get; init; }
    public AudioSettingsState AudioState { get; init; }
    public AudioSettingsMenu AudioMenu { get; init; }
    public VideoSettingsConfig VideoSettings { get; init; }
    public InputBindingsConfig Bindings { get; init; }
}

internal readonly struct SettingsMenuViewModelEvent
{
    public SettingsMenuViewModel ViewModel { get; init; }
}

internal readonly struct SettingsTabChangedEvent
{
    public string TabId { get; init; }
}

internal enum AudioSettingField
{
    MasterVolume = 0,
    MusicVolume = 1,
    SfxVolume = 2,
    UiVolume = 3,
    VoiceVolume = 4,
    MuteAll = 5,
    MasterMute = 6,
    MusicMute = 7,
    SfxMute = 8,
    UiMute = 9,
    VoiceMute = 10
}

internal readonly struct AudioSettingChangedEvent
{
    public AudioSettingField Field { get; init; }
    public float? Value { get; init; }
    public bool? ToggleValue { get; init; }
    public bool Persist { get; init; }
}

