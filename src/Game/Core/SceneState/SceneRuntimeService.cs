using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Media;
using TheLastMageStanding.Game.Core.Audio;
using TheLastMageStanding.Game.Core.Camera;
using TheLastMageStanding.Game.Core.Campaign;
using TheLastMageStanding.Game.Core.Diagnostics;
using TheLastMageStanding.Game.Core.Ecs;
using TheLastMageStanding.Game.Core.Input;
using TheLastMageStanding.Game.Core.World.Map;

namespace TheLastMageStanding.Game.Core.SceneState;

/// <summary>
/// Owns save-slot activation, ECS runtime creation, and scene-specific map bootstrap outside of Game1.
/// </summary>
internal sealed class SceneRuntimeService : IDisposable
{
    private const string HubMapAsset = "Tiles/Maps/HubMap";
    private const string LogCategory = "SceneRuntime";

    private readonly Camera2D _camera;
    private readonly MusicService _musicService;
    private readonly SceneStateService _sceneStateService;
    private readonly SceneManager _sceneManager;
    private readonly ActiveSaveSlotController _slotController;
    private readonly IEcsWorldFactory _worldFactory;
    private readonly StageContentResolver _stageContentResolver;

    private EcsWorldRunner? _ecs;
    private TiledMapService? _mapService;
    private Song? _menuSong;
    private Song? _gameplaySong;
    private string? _worldSlotId;

    public SceneRuntimeService(
        Camera2D camera,
        MusicService musicService,
        StageRegistry stageRegistry,
        SceneStateService sceneStateService,
        SceneManager sceneManager,
        ActiveSaveSlotController slotController,
        IEcsWorldFactory worldFactory)
    {
        _camera = camera;
        _musicService = musicService;
        _sceneStateService = sceneStateService;
        _sceneManager = sceneManager;
        _slotController = slotController;
        _worldFactory = worldFactory;
        _stageContentResolver = new StageContentResolver(stageRegistry);
    }

    public TiledMapService? MapService => _mapService;
    public EcsWorldRunner? WorldRunner => _ecs;
    public bool ExitRequested => _ecs?.ExitRequested == true;

    public void LoadContent(ContentManager content)
    {
        _menuSong = content.Load<Song>("Audio/StartScreenMusic");
        _gameplaySong = content.Load<Song>("Audio/Stage1Music");
        PlayMenuMusic();
    }

    public void ActivateSlot(string slotId)
    {
        if (_slotController.ActivateSlot(slotId))
        {
            DisposeWorldRunner();
        }
    }

    public string CreateNextSlot()
    {
        var newSlot = _slotController.CreateNextSlot();
        DisposeWorldRunner();
        return newSlot;
    }

    public bool ProcessPendingSceneTransition(ContentManager content, GraphicsDevice graphicsDevice)
    {
        if (!_sceneManager.ProcessPendingTransition())
        {
            return false;
        }

        ReloadSceneContent(content, graphicsDevice);
        return true;
    }

    public void EnsureSceneReady(ContentManager content, GraphicsDevice graphicsDevice)
    {
        if (_sceneStateService.IsInMainMenu())
        {
            return;
        }

        if (_mapService == null || _ecs == null)
        {
            ReloadSceneContent(content, graphicsDevice);
        }
    }

    public void Update(GameTime gameTime, InputState input)
    {
        _mapService?.Update(gameTime);
        _ecs?.Update(gameTime, input);
    }

    public void Dispose()
    {
        Exception? disposeError = null;

        try
        {
            _mapService?.Dispose();
        }
        catch (Exception ex)
        {
            disposeError ??= ex;
        }
        finally
        {
            _mapService = null;
        }

        try
        {
            _ecs?.Dispose();
        }
        catch (Exception ex)
        {
            disposeError ??= ex;
        }
        finally
        {
            _ecs = null;
        }

        _worldSlotId = null;

        if (disposeError != null)
        {
            throw disposeError;
        }
    }

    private void ReloadSceneContent(ContentManager content, GraphicsDevice graphicsDevice)
    {
        var currentScene = _sceneManager.CurrentScene;
        var currentStageId = _sceneManager.CurrentStageId;
        RuntimeLog.Info(LogCategory, $"Reloading content for scene {currentScene} (stageId: {currentStageId ?? "none"}).");

        if (currentScene == SceneType.MainMenu)
        {
            TransitionToMainMenu();
            return;
        }

        var ecs = EnsureWorldInitialized(content, graphicsDevice);
        LoadGameplayScene(content, graphicsDevice, currentScene, currentStageId, ecs);
    }

    private void TransitionToMainMenu()
    {
        _mapService?.Dispose();
        _mapService = null;
        DisposeWorldRunner();
        PlayMenuMusic();
    }

    private void LoadGameplayScene(
        ContentManager content,
        GraphicsDevice graphicsDevice,
        SceneType currentScene,
        string? currentStageId,
        EcsWorldRunner ecs)
    {
        var mapAsset = currentScene == SceneType.Stage
            ? _stageContentResolver.ResolveMapAssetForStage(currentStageId)
            : HubMapAsset;

        if (currentScene == SceneType.Stage)
        {
            ecs.ResetStageStateForNewRun();
        }

        RuntimeLog.Info(LogCategory, $"Using map asset '{mapAsset}' for scene '{currentScene}'.");

        _mapService?.Dispose();
        _mapService = TiledMapService.Load(content, graphicsDevice, mapAsset);

        var playerSpawn = _mapService.GetPlayerSpawnOrDefault(Vector2.Zero);
        ecs.SetPlayerPosition(playerSpawn);
        _camera.LookAt(playerSpawn);
        _mapService.LoadCollisionRegions(ecs.World);

        if (currentScene == SceneType.Hub)
        {
            ecs.SpawnHubNpcs(_mapService.Map);
        }

        PlayGameplayMusic();
    }

    private EcsWorldRunner EnsureWorldInitialized(ContentManager content, GraphicsDevice graphicsDevice)
    {
        var activeSlotId = _slotController.EnsureActiveSlot();

        if (_ecs != null && string.Equals(_worldSlotId, activeSlotId, StringComparison.Ordinal))
        {
            return _ecs;
        }

        var nextEcs = _worldFactory.Create(activeSlotId);

        try
        {
            nextEcs.LoadContent(graphicsDevice, content);
        }
        catch
        {
            nextEcs.Dispose();
            throw;
        }

        var previousEcs = _ecs;
        _ecs = nextEcs;
        _worldSlotId = activeSlotId;
        previousEcs?.Dispose();
        return _ecs;
    }

    private void DisposeWorldRunner()
    {
        _ecs?.Dispose();
        _ecs = null;
        _worldSlotId = null;
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
        if (_gameplaySong != null)
        {
            _musicService.Play(_gameplaySong, isRepeating: true);
        }
    }
}
