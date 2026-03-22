using System;
using Microsoft.Xna.Framework;
using TheLastMageStanding.Game.Core.Ecs.Components;
using TheLastMageStanding.Game.Core.Events;
using TheLastMageStanding.Game.Core.Input;
using TheLastMageStanding.Game.Core.UI.Myra;

namespace TheLastMageStanding.Game.Core.Config;

/// <summary>
/// Coordinates shared settings application at the app boundary and owns the main-menu settings overlay.
/// </summary>
internal sealed class GameSettingsController : IDisposable
{
    private readonly EventBus _eventBus;
    private readonly RuntimeSettingsService _runtimeSettingsService;
    private readonly VideoSettingsConfig _videoSettings;
    private readonly InputBindingsConfig _inputBindings;
    private readonly InputState _inputState;
    private readonly VideoSettingsApplier _videoSettingsApplier;
    private readonly MyraSettingsScreen _settingsScreen;

    private bool _isOpen;
    private string _activeTab = "audio";
    private AudioSettingsMenu _audioMenu = new(false);

    public GameSettingsController(
        EventBus eventBus,
        RuntimeSettingsService runtimeSettingsService,
        VideoSettingsConfig videoSettings,
        InputBindingsConfig inputBindings,
        InputState inputState,
        VideoSettingsApplier videoSettingsApplier,
        IUiSoundPlayer? uiSoundPlayer = null)
    {
        _eventBus = eventBus;
        _runtimeSettingsService = runtimeSettingsService;
        _videoSettings = videoSettings;
        _inputBindings = inputBindings;
        _inputState = inputState;
        _videoSettingsApplier = videoSettingsApplier;
        _settingsScreen = new MyraSettingsScreen(uiSoundPlayer);

        _settingsScreen.AudioSettingChanged += OnScreenAudioSettingChanged;
        _settingsScreen.VideoSettingChanged += OnScreenVideoSettingChanged;
        _settingsScreen.InputBindingChanged += OnScreenInputBindingChanged;
        _settingsScreen.TabChangedEvent += OnTabChanged;

        _eventBus.Subscribe<VideoSettingChangedEvent>(OnRuntimeVideoSettingChanged);
        _eventBus.Subscribe<InputBindingChangedEvent>(OnRuntimeInputBindingChanged);

        RefreshViewModel();
    }

    public bool IsOpen => _isOpen;

    public void Open(string tabId)
    {
        _isOpen = true;
        _activeTab = string.IsNullOrWhiteSpace(tabId) ? "audio" : tabId;
        _audioMenu = new AudioSettingsMenu(false);
        RefreshViewModel();
    }

    public void Close()
    {
        if (!_isOpen)
        {
            return;
        }

        _isOpen = false;
        _audioMenu = new AudioSettingsMenu(false);
        RefreshViewModel();
    }

    public void Update(GameTime gameTime, InputState input)
    {
        if (!_isOpen)
        {
            return;
        }

        _settingsScreen.Update(gameTime);
        if (input.MenuBackPressed)
        {
            Close();
        }
    }

    public void Draw()
    {
        if (_isOpen)
        {
            _settingsScreen.Render();
        }
    }

    public void Dispose()
    {
        _eventBus.Unsubscribe<VideoSettingChangedEvent>(OnRuntimeVideoSettingChanged);
        _eventBus.Unsubscribe<InputBindingChangedEvent>(OnRuntimeInputBindingChanged);
        _settingsScreen.AudioSettingChanged -= OnScreenAudioSettingChanged;
        _settingsScreen.VideoSettingChanged -= OnScreenVideoSettingChanged;
        _settingsScreen.InputBindingChanged -= OnScreenInputBindingChanged;
        _settingsScreen.TabChangedEvent -= OnTabChanged;
        _settingsScreen.Dispose();
    }

    private void OnScreenAudioSettingChanged(AudioSettingChangedEvent evt)
    {
        var audioState = _runtimeSettingsService.BuildAudioState();
        if (!_runtimeSettingsService.TryApplyAudioChange(evt, ref audioState, out var confirmation))
        {
            return;
        }

        _audioMenu.ConfirmationText = confirmation;
        RefreshViewModel();
    }

    private void OnScreenVideoSettingChanged(VideoSettingChangedEvent evt)
    {
        var videoState = _runtimeSettingsService.BuildVideoState();
        if (!_runtimeSettingsService.TryApplyVideoChange(evt, ref videoState))
        {
            return;
        }

        ApplyVideoSettingsToGame();
    }

    private void OnRuntimeVideoSettingChanged(VideoSettingChangedEvent _)
    {
        ApplyVideoSettingsToGame();
    }

    private void OnScreenInputBindingChanged(InputBindingChangedEvent evt)
    {
        if (_runtimeSettingsService.ApplyInputBindingChange(evt))
        {
            ApplyInputBindingsToGame();
        }
    }

    private void OnRuntimeInputBindingChanged(InputBindingChangedEvent _)
    {
        ApplyInputBindingsToGame();
    }

    private void OnTabChanged(string tabId)
    {
        _activeTab = string.IsNullOrWhiteSpace(tabId) ? _activeTab : tabId;
    }

    private void RefreshViewModel()
    {
        _settingsScreen.ApplyViewModel(new SettingsMenuViewModel
        {
            IsOpen = _isOpen,
            ActiveTab = _activeTab,
            AudioState = _runtimeSettingsService.BuildAudioState(),
            AudioMenu = _audioMenu,
            VideoSettings = _runtimeSettingsService.CloneVideoSettings(),
            Bindings = _runtimeSettingsService.CloneInputBindings()
        });
    }

    private void ApplyVideoSettingsToGame()
    {
        _videoSettingsApplier.Apply(_videoSettings, applyChanges: true);
        RefreshViewModel();
    }

    private void ApplyInputBindingsToGame()
    {
        _inputState.ApplyBindings(_inputBindings);
        RefreshViewModel();
    }
}
