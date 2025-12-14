namespace TheLastMageStanding.Game.Core.Ecs.Components;

/// <summary>
/// Game session state: Playing allows normal gameplay, GameOver halts spawning/combat/input.
/// </summary>
internal enum GameState
{
    Playing,
    Paused,
    GameOver
}

/// <summary>
/// Singleton component tracking the current game session state, wave progression, and wave timer.
/// </summary>
internal struct GameSession
{
    public GameState State { get; set; }
    public int CurrentWave { get; set; }
    public float WaveTimer { get; set; }
    public float WaveInterval { get; set; }
    public int EnemiesKilled { get; set; }
    public float TimeSurvived { get; set; }

    public GameSession(float waveInterval = 5.0f)
    {
        State = GameState.Playing;
        CurrentWave = 0;
        WaveTimer = 0f;
        WaveInterval = waveInterval;
        EnemiesKilled = 0;
        TimeSurvived = 0f;
    }
}

/// <summary>
/// Tracks the current pause menu selection.
/// </summary>
internal struct PauseMenu
{
    public PauseMenu(int selectedIndex)
    {
        SelectedIndex = selectedIndex;
    }

    public int SelectedIndex { get; set; }
}

/// <summary>
/// Session-level audio settings that mirror the applied audio configuration.
/// </summary>
internal struct AudioSettingsState
{
    public AudioSettingsState(
        float masterVolume,
        float musicVolume,
        float sfxVolume,
        float uiVolume,
        float voiceVolume,
        bool masterMuted,
        bool musicMuted,
        bool sfxMuted,
        bool uiMuted,
        bool voiceMuted,
        bool muteAll)
    {
        MasterVolume = masterVolume;
        MusicVolume = musicVolume;
        SfxVolume = sfxVolume;
        UiVolume = uiVolume;
        VoiceVolume = voiceVolume;
        MasterMuted = masterMuted;
        MusicMuted = musicMuted;
        SfxMuted = sfxMuted;
        UiMuted = uiMuted;
        VoiceMuted = voiceMuted;
        MuteAll = muteAll;
    }

    public float MasterVolume { get; set; } = 1f;
    public float MusicVolume { get; set; } = 1f;
    public float SfxVolume { get; set; } = 1f;
    public float UiVolume { get; set; } = 1f;
    public float VoiceVolume { get; set; } = 1f;
    public bool MasterMuted { get; set; }
    public bool MusicMuted { get; set; }
    public bool SfxMuted { get; set; }
    public bool UiMuted { get; set; }
    public bool VoiceMuted { get; set; }
    public bool MuteAll { get; set; }
}

/// <summary>
/// Session-level video settings mirroring the applied graphics config.
/// </summary>
internal struct VideoSettingsState
{
    public bool Fullscreen { get; set; }
    public bool VSync { get; set; }
    public bool ReduceStatusEffectFlashing { get; set; }
    public int BackBufferWidth { get; set; }
    public int BackBufferHeight { get; set; }
    public int WindowScale { get; set; }

    public VideoSettingsState(
        bool fullscreen,
        bool vSync,
        bool reduceStatusEffectFlashing,
        int backBufferWidth,
        int backBufferHeight,
        int windowScale)
    {
        Fullscreen = fullscreen;
        VSync = vSync;
        ReduceStatusEffectFlashing = reduceStatusEffectFlashing;
        BackBufferWidth = backBufferWidth;
        BackBufferHeight = backBufferHeight;
        WindowScale = windowScale;
    }
}

/// <summary>
/// Tracks the tabbed settings menu state when opened from pause/hub.
/// </summary>
internal struct SettingsMenuState
{
    public bool IsOpen { get; set; }
    public string ActiveTab { get; set; }

    public SettingsMenuState(bool isOpen, string activeTab)
    {
        IsOpen = isOpen;
        ActiveTab = activeTab;
    }
}

/// <summary>
/// Transient notification message displayed on the HUD with an auto-dismiss timer.
/// </summary>
internal struct WaveNotification
{
    public string Message { get; set; }
    public float RemainingSeconds { get; set; }

    public WaveNotification(string message, float duration = 2.5f)
    {
        Message = message;
        RemainingSeconds = duration;
    }
}

/// <summary>
/// Tracks the audio settings UI state within the pause menu.
/// </summary>
internal struct AudioSettingsMenu
{
    public bool IsOpen { get; set; }
    public int SelectedIndex { get; set; }
    public float SampleCooldownSeconds { get; set; }
    public float ConfirmationTimerSeconds { get; set; }
    public string ConfirmationText { get; set; }

    public AudioSettingsMenu(bool isOpen)
    {
        IsOpen = isOpen;
        SelectedIndex = 0;
        SampleCooldownSeconds = 0f;
        ConfirmationTimerSeconds = 0f;
        ConfirmationText = string.Empty;
    }
}

/// <summary>
/// Transient message shown when a player tries to access a hub-only feature during a stage run.
/// </summary>
internal struct LockedFeatureMessage
{
    public string Message { get; set; }
    public float RemainingSeconds { get; set; }

    public LockedFeatureMessage(string message, float duration = 2.0f)
    {
        Message = message;
        RemainingSeconds = duration;
    }
}
