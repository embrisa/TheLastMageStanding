using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Media;
using TheLastMageStanding.Game.Core.Camera;
using TheLastMageStanding.Game.Core.Audio;
using TheLastMageStanding.Game.Core.Config;
using TheLastMageStanding.Game.Core.Ecs;
using TheLastMageStanding.Game.Core.Input;
using TheLastMageStanding.Game.Core.World.Map;
using TheLastMageStanding.Game.Core.SceneState;
using TheLastMageStanding.Game.Core.Events;
using TheLastMageStanding.Game.Core.MetaProgression;
using TheLastMageStanding.Game.Core.UI;

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
    private MusicService _musicService = null!;
    private Song? _menuSong;
    private Song? _backgroundSong;
    private SaveSlotService _saveSlotService = null!;
    private MainMenuScreen _mainMenu = null!;
    private string? _activeSlotId;

    private const string HubMapAsset = "Tiles/Maps/HubMap";
    private const string FirstMapAsset = "Tiles/Maps/FirstMap";
    private const string MapEnvVar = "TLMS_MAP";

    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;

        IsFixedTimeStep = true;
        TargetElapsedTime = TimeSpan.FromSeconds(1d / 60d);

        _graphics.PreferredBackBufferWidth = VirtualWidth * WindowScale;
        _graphics.PreferredBackBufferHeight = VirtualHeight * WindowScale;
    }

    protected override void Initialize()
    {
        _camera = new Camera2D(VirtualWidth, VirtualHeight);
        _audioSettingsStore = new AudioSettingsStore();
        _audioSettings = _audioSettingsStore.LoadOrDefault();
        _musicService = new MusicService(_audioSettings);
        _eventBus = new EventBus();
        _sceneStateService = new SceneStateService();
        _sceneManager = new SceneManager(_sceneStateService, _eventBus);
        _input = new InputState(_sceneStateService);
        _saveSlotService = new SaveSlotService(new DefaultFileSystem());
        _mainMenu = new MainMenuScreen(_saveSlotService);

        // Subscribe to scene transition events
        _eventBus.Subscribe<SceneEnterEvent>(OnSceneEnter);

        base.Initialize();
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);
        _renderTarget = new RenderTarget2D(GraphicsDevice, VirtualWidth, VirtualHeight);

        _menuSong = Content.Load<Song>("Audio/StartScreenMusic");
        _backgroundSong = Content.Load<Song>("Audio/Stage1Music");
        _mainMenu.LoadContent(GraphicsDevice, Content);

        PlayMenuMusic();
    }

    protected override void Update(GameTime gameTime)
    {
        _input.Update();

        // Process pending scene transitions
        if (_sceneManager.ProcessPendingTransition())
        {
            // Scene transition happened, reload map if needed
            ReloadSceneContent();
        }

        if (_sceneStateService.IsInMainMenu())
        {
            var menuResult = _mainMenu.Update(gameTime, _input);
            HandleMainMenuResult(menuResult);
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

        if (_sceneStateService.IsInMainMenu())
        {
            _spriteBatch.Begin(samplerState: SamplerState.PointClamp);
            _mainMenu.Draw(_spriteBatch);
            _spriteBatch.End();
        }
        else
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
            destinationRectangle: new Rectangle(0, 0, _graphics.PreferredBackBufferWidth, _graphics.PreferredBackBufferHeight),
            color: Color.White);
        _spriteBatch.End();

        base.Draw(gameTime);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _mapService?.Dispose();
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
        Console.WriteLine($"[Game1] Reloading content for scene: {currentScene}");

        if (currentScene == SceneType.MainMenu)
        {
            _mapService?.Dispose();
            _mapService = null;
            PlayMenuMusic();
            return;
        }

        EnsureActiveSlot();
        EnsureWorldInitialized();

        // Determine which map to load
        string mapAsset = currentScene switch
        {
            SceneType.Hub => HubMapAsset,
            SceneType.Stage => FirstMapAsset, // TODO: Load based on selected stage
            _ => HubMapAsset
        };

        // Dispose old map
        _mapService?.Dispose();

        // Load new map
        _mapService = TiledMapService.Load(Content, GraphicsDevice, mapAsset);

        // Reset player position
        var playerSpawn = _mapService.GetPlayerSpawnOrDefault(Vector2.Zero);
        _ecs.SetPlayerPosition(playerSpawn);
        _camera.LookAt(playerSpawn);

        // Load collision regions
        _mapService.LoadCollisionRegions(_ecs.World);

        // Spawn NPCs if in hub
        if (currentScene == SceneType.Hub)
        {
            Console.WriteLine("[Game1] Calling SpawnHubNpcs for hub scene");
            _ecs.SpawnHubNpcs(_mapService.Map);
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
                _sceneManager.TransitionToHub();
                break;
            case MainMenuAction.CreateNewSlot:
                var newSlot = _saveSlotService.CreateNextSlot();
                _activeSlotId = newSlot.SlotId;
                _sceneManager.TransitionToHub();
                break;
            case MainMenuAction.Quit:
                Exit();
                break;
        }
    }

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

        _ecs = new EcsWorldRunner(_camera, _audioSettings, _audioSettingsStore, _musicService, _eventBus, _sceneStateService, _sceneManager, _saveSlotService, _activeSlotId!);

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
}
