using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Media;
using Microsoft.Xna.Framework.Input;
using Myra;
using TheLastMageStanding.Game.Core.Camera;
using TheLastMageStanding.Game.Core.Audio;
using TheLastMageStanding.Game.Core.Config;
using TheLastMageStanding.Game.Core.Ecs;
using TheLastMageStanding.Game.Core.Ecs.Components;
using TheLastMageStanding.Game.Core.Input;
using TheLastMageStanding.Game.Core.Campaign;
using TheLastMageStanding.Game.Core.World.Map;
using TheLastMageStanding.Game.Core.SceneState;
using TheLastMageStanding.Game.Core.Events;
using TheLastMageStanding.Game.Core.MetaProgression;
using TheLastMageStanding.Game.Core.UI;
using TheLastMageStanding.Game.Core.UI.Myra;

namespace TheLastMageStanding.Game;

public class Game1 : Microsoft.Xna.Framework.Game
{
    private const int VirtualWidth = 960;
    private const int VirtualHeight = 540;
    private const int WindowScale = 2;

    private readonly GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch = null!;
    private RenderTarget2D _renderTarget = null!;
    private Camera2D _camera = null!;
    private EventBus _eventBus = null!;
    private SceneStateService _sceneStateService = null!;
    private SceneManager _sceneManager = null!;
    private InputState _input = null!;
    private EcsWorldRunner? _ecs;
    private TiledMapService? _mapService;
    private AudioSettingsStore _audioSettingsStore = null!;
    private AudioSettingsConfig _audioSettings = null!;
    private VideoSettingsStore _videoSettingsStore = null!;
    private VideoSettingsConfig _videoSettings = null!;
    private InputBindingsStore _inputBindingsStore = null!;
    private InputBindingsConfig _inputBindings = null!;
    private MusicService _musicService = null!;
    private Song? _menuSong;
    private Song? _backgroundSong;
    private SaveSlotService _saveSlotService = null!;
    private StageRegistry _stageRegistry = null!;
    private StageContentResolver _stageContentResolver = null!;
    private MyraMainMenuScreen _myraMenu = null!;
    private MyraSettingsScreen _myraSettings = null!;
    private bool _mainMenuSettingsOpen;
    private string _mainMenuSettingsTab = "audio";
    private AudioSettingsMenu _mainMenuAudioMenu = new AudioSettingsMenu(false);
    private string? _activeSlotId;

    private const string HubMapAsset = "Tiles/Maps/HubMap";
    private const string FirstMapAsset = "Tiles/Maps/FirstMap";
    private const string MapEnvVar = "TLMS_MAP";

    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
        _graphics.HardwareModeSwitch = false; // Prefer borderless fullscreen to avoid macOS mode switches

        IsFixedTimeStep = true;
        TargetElapsedTime = TimeSpan.FromSeconds(1d / 60d);

        _graphics.PreferredBackBufferWidth = VirtualWidth * WindowScale;
        _graphics.PreferredBackBufferHeight = VirtualHeight * WindowScale;
    }

    protected override void Initialize()
    {
        MyraEnvironment.Game = this;

        _camera = new Camera2D(VirtualWidth, VirtualHeight);
        _audioSettingsStore = new AudioSettingsStore();
        _audioSettings = _audioSettingsStore.LoadOrDefault();
        _videoSettingsStore = new VideoSettingsStore();
        _videoSettings = _videoSettingsStore.LoadOrDefault();
        _inputBindingsStore = new InputBindingsStore();
        _inputBindings = _inputBindingsStore.LoadOrDefault();
        ApplyVideoSettings(_videoSettings, applyChanges: false);
        _musicService = new MusicService(_audioSettings);
        _eventBus = new EventBus();
        _sceneStateService = new SceneStateService();
        _sceneManager = new SceneManager(_sceneStateService, _eventBus);
        _stageRegistry = new StageRegistry();
        _stageContentResolver = new StageContentResolver(_stageRegistry, HubMapAsset);
        _input = new InputState(_sceneStateService, VirtualWidth, VirtualHeight, _inputBindings);
        _saveSlotService = new SaveSlotService(new DefaultFileSystem());
        _myraMenu = new MyraMainMenuScreen(_saveSlotService);

        // Subscribe to scene transition events
        _eventBus.Subscribe<SceneEnterEvent>(OnSceneEnter);
        _eventBus.Subscribe<VideoSettingChangedEvent>(OnVideoSettingChanged);
        _eventBus.Subscribe<InputBindingChangedEvent>(OnInputBindingChanged);

        base.Initialize();
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);
        _renderTarget = new RenderTarget2D(GraphicsDevice, VirtualWidth, VirtualHeight);

        ApplyVideoSettings(_videoSettings, applyChanges: true);

        _menuSong = Content.Load<Song>("Audio/StartScreenMusic");
        _backgroundSong = Content.Load<Song>("Audio/Stage1Music");

        UiFonts.Load(Content);
        var uiSoundPlayer = new DirectUiSoundPlayer(Content, _audioSettings);
        _myraMenu.SetSoundPlayer(uiSoundPlayer);
        _myraMenu.Initialize(this);
        _myraSettings = new MyraSettingsScreen(uiSoundPlayer);
        _myraSettings.AudioSettingChanged += OnMainMenuAudioSettingChanged;
        _myraSettings.VideoSettingChanged += OnVideoSettingChanged;
        _myraSettings.InputBindingChanged += OnInputBindingChanged;
        _myraSettings.TabChangedEvent += tabId =>
        {
            _mainMenuSettingsTab = string.IsNullOrWhiteSpace(tabId) ? _mainMenuSettingsTab : tabId;
        };
        _myraSettings.ApplyViewModel(BuildMainMenuSettingsViewModel());

        PlayMenuMusic();
    }

    protected override void Update(GameTime gameTime)
    {
        // Use Window.ClientBounds for mouse scaling because Mouse.GetState() returns window coordinates (points),
        // whereas GraphicsDevice.Viewport returns backbuffer coordinates (pixels).
        // On HiDPI (Retina), these differ.
        var clientBounds = Window.ClientBounds;
        _input.Update(clientBounds.Width, clientBounds.Height);

        // Process pending scene transitions
        if (_sceneManager.ProcessPendingTransition())
        {
            // Scene transition happened, reload map if needed
            ReloadSceneContent();
        }

        if (_sceneStateService.IsInMainMenu())
        {
            if (_mainMenuSettingsOpen)
            {
                _myraSettings.Update(gameTime);
                if (_input.MenuBackPressed)
                {
                    CloseMainMenuSettings();
                }
            }
            else
            {
                var menuResult = _myraMenu.Update(gameTime, _input);
                HandleMainMenuResult(menuResult);
            }

            base.Update(gameTime);
            return;
        }

        if (_mapService == null || _ecs == null)
        {
            ReloadSceneContent();
        }

        _mapService?.Update(gameTime);
        _ecs?.Update(gameTime, _input);

        if (_ecs != null && _ecs.ExitRequested)
        {
            Exit();
        }

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.SetRenderTarget(_renderTarget);
        GraphicsDevice.Clear(ClearOptions.Target, Color.CornflowerBlue, 1f, 0);

        if (!_sceneStateService.IsInMainMenu())
        {
            _mapService?.Draw(_camera.Transform);

            _spriteBatch.Begin(transformMatrix: _camera.Transform, samplerState: SamplerState.PointClamp);
            _ecs?.Draw(_spriteBatch);
            _spriteBatch.End();

            // Draw UI to render target (screen space relative to virtual resolution)
            _spriteBatch.Begin(samplerState: SamplerState.PointClamp);
            _ecs?.DrawUI(_spriteBatch);
            _spriteBatch.End();
        }

        GraphicsDevice.SetRenderTarget(null);
        GraphicsDevice.Clear(Color.Black);

        _spriteBatch.Begin(samplerState: SamplerState.PointClamp);
        _spriteBatch.Draw(
            _renderTarget,
            destinationRectangle: new Rectangle(0, 0, GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height),
            color: Color.White);
        _spriteBatch.End();

        if (!_sceneStateService.IsInMainMenu())
        {
            _spriteBatch.Begin(samplerState: SamplerState.PointClamp);
            _ecs?.DrawScreenSpaceUI(_spriteBatch);
            _spriteBatch.End();
        }

        if (_sceneStateService.IsInMainMenu())
        {
            _myraMenu.Draw();
            if (_mainMenuSettingsOpen)
            {
                _myraSettings.Render();
            }
        }

        base.Draw(gameTime);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _mapService?.Dispose();
            _myraMenu?.Dispose();
            _myraSettings?.Dispose();
        }

        base.Dispose(disposing);
    }

    private void OnSceneEnter(SceneEnterEvent evt)
    {
        Console.WriteLine($"[Game1] Scene entered: {evt.Scene}, StageId: {evt.StageId}");
    }

    private void ReloadSceneContent()
    {
        var currentScene = _sceneManager.CurrentScene;
        var currentStageId = _sceneManager.CurrentStageId;
        Console.WriteLine($"[Game1] Reloading content for scene: {currentScene} (stageId: {currentStageId ?? "none"})");

        if (currentScene == SceneType.MainMenu)
        {
            _mapService?.Dispose();
            _mapService = null;
            PlayMenuMusic();
            return;
        }

        EnsureActiveSlot();
        EnsureWorldInitialized();
        var ecs = _ecs ?? throw new InvalidOperationException("ECS world failed to initialize.");

        if (currentScene == SceneType.Stage)
        {
            ecs.ResetStageStateForNewRun();
        }

        // Determine which map to load
        var mapAsset = currentScene == SceneType.Stage
            ? _stageContentResolver.ResolveMapAssetForStage(currentStageId)
            : HubMapAsset;

        if (currentScene == SceneType.Stage && mapAsset == HubMapAsset)
        {
            Console.WriteLine($"[Game1] Stage '{currentStageId ?? "unknown"}' missing map. Falling back to hub map.");
        }
        else
        {
            Console.WriteLine($"[Game1] Using map asset '{mapAsset}' for scene '{currentScene}'.");
        }

        // Dispose old map
        _mapService?.Dispose();

        // Load new map
        var mapService = TiledMapService.Load(Content, GraphicsDevice, mapAsset);
        _mapService = mapService;

        // Reset player position
        var playerSpawn = mapService.GetPlayerSpawnOrDefault(Vector2.Zero);
        ecs.SetPlayerPosition(playerSpawn);
        _camera.LookAt(playerSpawn);

        // Load collision regions
        mapService.LoadCollisionRegions(ecs.World);

        // Spawn NPCs if in hub
        if (currentScene == SceneType.Hub)
        {
            Console.WriteLine("[Game1] Calling SpawnHubNpcs for hub scene");
            ecs.SpawnHubNpcs(mapService.Map);
        }
        else
        {
            Console.WriteLine($"[Game1] Skipping NPC spawn, current scene is: {currentScene}");
        }

        PlayGameplayMusic();
    }

    private static string ResolveMapAsset()
    {
        var envValue = Environment.GetEnvironmentVariable(MapEnvVar);
        if (string.Equals(envValue, "first", StringComparison.OrdinalIgnoreCase))
        {
            return FirstMapAsset;
        }

        return HubMapAsset;
    }

    private void HandleMainMenuResult(MainMenuResult result)
    {
        switch (result.Action)
        {
            case MainMenuAction.StartSlot when !string.IsNullOrEmpty(result.SlotId):
                _activeSlotId = result.SlotId;
                CloseMainMenuSettings();
                _sceneManager.TransitionToHub();
                break;
            case MainMenuAction.CreateNewSlot:
                var newSlot = _saveSlotService.CreateNextSlot();
                _activeSlotId = newSlot.SlotId;
                CloseMainMenuSettings();
                _sceneManager.TransitionToHub();
                break;
            case MainMenuAction.Settings:
                OpenMainMenuSettings("audio");
                break;
            case MainMenuAction.Quit:
                CloseMainMenuSettings();
                Exit();
                break;
        }
    }

    private void OnMainMenuAudioSettingChanged(AudioSettingChangedEvent evt)
    {
        var audioState = BuildAudioStateFromConfig();
        var changed = ApplyAudioChange(evt, ref audioState);
        if (!changed)
        {
            return;
        }

        SyncAudioSettings(audioState, evt.Persist);
        var confirmation = BuildAudioConfirmation(evt.Field, audioState);
        _mainMenuAudioMenu.ConfirmationText = confirmation;
        _myraSettings.ApplyViewModel(BuildMainMenuSettingsViewModel(confirmation));
    }

    private void OpenMainMenuSettings(string tabId)
    {
        _mainMenuSettingsOpen = true;
        _mainMenuSettingsTab = string.IsNullOrWhiteSpace(tabId) ? "audio" : tabId;
        _mainMenuAudioMenu = new AudioSettingsMenu(false);
        _myraSettings.ApplyViewModel(BuildMainMenuSettingsViewModel());
    }

    private void CloseMainMenuSettings()
    {
        if (!_mainMenuSettingsOpen)
        {
            return;
        }

        _mainMenuSettingsOpen = false;
        _mainMenuAudioMenu = new AudioSettingsMenu(false);
        _myraSettings.ApplyViewModel(BuildMainMenuSettingsViewModel());
    }

    private SettingsMenuViewModel BuildMainMenuSettingsViewModel(string? confirmationText = null)
    {
        _mainMenuAudioMenu.ConfirmationText = confirmationText ?? _mainMenuAudioMenu.ConfirmationText;

        return new SettingsMenuViewModel
        {
            IsOpen = _mainMenuSettingsOpen,
            ActiveTab = _mainMenuSettingsTab,
            AudioState = BuildAudioStateFromConfig(),
            AudioMenu = _mainMenuAudioMenu,
            VideoSettings = _videoSettings.Clone(),
            Bindings = _inputBindings.Clone()
        };
    }

    private AudioSettingsState BuildAudioStateFromConfig()
    {
        return new AudioSettingsState(
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
    }

    private void SyncAudioSettings(AudioSettingsState audioState, bool persist)
    {
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

        if (persist)
        {
            _audioSettingsStore.Save(_audioSettings);
        }
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

    private static float ClampAndSnap(float value)
    {
        const float sliderStep = 0.05f;
        var snapped = (float)Math.Round(value / sliderStep) * sliderStep;
        return Math.Clamp(snapped, 0f, 1f);
    }

    private static string BuildAudioConfirmation(AudioSettingField field, AudioSettingsState audioState) => field switch
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

    private void EnsureActiveSlot()
    {
        if (!string.IsNullOrEmpty(_activeSlotId))
        {
            return;
        }

        var existing = _saveSlotService.GetMostRecentSlot();
        if (existing != null)
        {
            _activeSlotId = existing.SlotId;
            return;
        }

        var newSlot = _saveSlotService.CreateNextSlot();
        _activeSlotId = newSlot.SlotId;
    }

    private void EnsureWorldInitialized()
    {
        if (_ecs != null)
        {
            return;
        }

        if (string.IsNullOrEmpty(_activeSlotId))
        {
            EnsureActiveSlot();
        }

        _ecs = new EcsWorldRunner(_camera, _audioSettings, _audioSettingsStore, _videoSettings, _videoSettingsStore, _inputBindings, _inputBindingsStore, _musicService, _eventBus, _stageRegistry, _sceneStateService, _sceneManager, _saveSlotService, _activeSlotId!);

        _ecs.LoadContent(GraphicsDevice, Content);
    }

    private void PlayMenuMusic()
    {
        if (_menuSong != null)
        {
            _musicService.Play(_menuSong, isRepeating: true);
        }
    }

    private void PlayGameplayMusic()
    {
        if (_backgroundSong != null)
        {
            _musicService.Play(_backgroundSong, isRepeating: true);
        }
    }

    private void OnVideoSettingChanged(VideoSettingChangedEvent evt)
    {
        // #region agent log (hypothesis H)
        try
        {
            System.IO.File.AppendAllText(
                "/Users/philippetillheden/TheLastMageStanding/.cursor/debug.log",
                System.Text.Json.JsonSerializer.Serialize(new
                {
                    sessionId = "debug-session",
                    runId = "post-fix",
                    hypothesisId = "H",
                    location = "Game1.cs:OnVideoSettingChanged:entry",
                    message = "VideoSettingChanged received",
                    data = new
                    {
                        field = evt.Field.ToString(),
                        toggleValue = evt.ToggleValue,
                        windowScale = evt.WindowScale,
                        resolution = evt.Resolution.HasValue ? new { w = evt.Resolution.Value.Width, h = evt.Resolution.Value.Height } : null,
                        persist = evt.Persist
                    },
                    timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                }) + "\n");
        }
        catch
        {
            // ignored
        }
        // #endregion

        var changed = false;
        switch (evt.Field)
        {
            case VideoSettingField.Fullscreen when evt.ToggleValue.HasValue:
                _videoSettings.Fullscreen = evt.ToggleValue.Value;
                changed = true;
                break;
            case VideoSettingField.VSync when evt.ToggleValue.HasValue:
                _videoSettings.VSync = evt.ToggleValue.Value;
                changed = true;
                break;
            case VideoSettingField.Resolution when evt.Resolution.HasValue:
                var (width, height) = evt.Resolution.Value;
                _videoSettings.BackBufferWidth = Math.Max(640, width);
                _videoSettings.BackBufferHeight = Math.Max(360, height);
                changed = true;
                break;
            case VideoSettingField.WindowScale when evt.WindowScale.HasValue:
                var scale = Math.Clamp(evt.WindowScale.Value, 1, 4);
                _videoSettings.WindowScale = scale;
                _videoSettings.BackBufferWidth = VirtualWidth * scale;
                _videoSettings.BackBufferHeight = VirtualHeight * scale;
                changed = true;
                break;
        }

        if (!changed)
        {
            return;
        }

        ApplyVideoSettings(_videoSettings, applyChanges: true);

        // #region agent log (hypothesis H)
        try
        {
            System.IO.File.AppendAllText(
                "/Users/philippetillheden/TheLastMageStanding/.cursor/debug.log",
                System.Text.Json.JsonSerializer.Serialize(new
                {
                    sessionId = "debug-session",
                    runId = "post-fix",
                    hypothesisId = "H",
                    location = "Game1.cs:OnVideoSettingChanged:afterApply",
                    message = "ApplyVideoSettings called",
                    data = new
                    {
                        fullscreen = _videoSettings.Fullscreen,
                        vsync = _videoSettings.VSync,
                        backBufferWidth = _videoSettings.BackBufferWidth,
                        backBufferHeight = _videoSettings.BackBufferHeight,
                        windowScale = _videoSettings.WindowScale
                    },
                    timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                }) + "\n");
        }
        catch
        {
            // ignored
        }
        // #endregion

        if (evt.Persist)
        {
            _videoSettingsStore.Save(_videoSettings);
        }

        // If we're in the main-menu settings screen, refresh the view model so the UI
        // reflects any dynamic state (e.g. resolution locked when fullscreen is enabled).
        if (_mainMenuSettingsOpen)
        {
            // #region agent log (hypothesis I)
            try
            {
                System.IO.File.AppendAllText(
                    "/Users/philippetillheden/TheLastMageStanding/.cursor/debug.log",
                    System.Text.Json.JsonSerializer.Serialize(new
                    {
                        sessionId = "debug-session",
                        runId = "post-fix",
                        hypothesisId = "I",
                        location = "Game1.cs:OnVideoSettingChanged:refreshVm",
                        message = "Refreshing main menu settings view model",
                        data = new
                        {
                            activeTab = _mainMenuSettingsTab,
                            fullscreen = _videoSettings.Fullscreen,
                            backBufferWidth = _videoSettings.BackBufferWidth,
                            backBufferHeight = _videoSettings.BackBufferHeight,
                            windowScale = _videoSettings.WindowScale
                        },
                        timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                    }) + "\n");
            }
            catch
            {
                // ignored
            }
            // #endregion

            _myraSettings.ApplyViewModel(BuildMainMenuSettingsViewModel());
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
        _input.ApplyBindings(_inputBindings);

        if (evt.Persist)
        {
            _inputBindingsStore.Save(_inputBindings);
        }
    }

    private void ApplyVideoSettings(VideoSettingsConfig settings, bool applyChanges)
    {
        settings.Normalize();
        _graphics.HardwareModeSwitch = false; // Stay borderless to avoid swapchain churn on macOS
        _graphics.SynchronizeWithVerticalRetrace = settings.VSync;

        if (settings.Fullscreen)
        {
            var (displayWidth, displayHeight) = GetDisplayDimensions();
            Window.IsBorderless = true;
            _graphics.IsFullScreen = true;
            _graphics.PreferredBackBufferWidth = displayWidth;
            _graphics.PreferredBackBufferHeight = displayHeight;
        }
        else
        {
            Window.IsBorderless = false;
            _graphics.IsFullScreen = false;
            _graphics.PreferredBackBufferWidth = settings.BackBufferWidth;
            _graphics.PreferredBackBufferHeight = settings.BackBufferHeight;
        }

        if (applyChanges)
        {
            _graphics.ApplyChanges();
        }
    }

    private static (int Width, int Height) GetDisplayDimensions()
    {
        var mode = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode;
        return (mode.Width, mode.Height);
    }
}
