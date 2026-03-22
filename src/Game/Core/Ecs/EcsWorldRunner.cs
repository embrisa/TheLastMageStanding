using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using TheLastMageStanding.Game.Core.Audio;
using TheLastMageStanding.Game.Core.Camera;
using TheLastMageStanding.Game.Core.Campaign;
using TheLastMageStanding.Game.Core.Config;
using TheLastMageStanding.Game.Core.Ecs.Components;
using TheLastMageStanding.Game.Core.Ecs.Config;
using TheLastMageStanding.Game.Core.Ecs.Runtime;
using TheLastMageStanding.Game.Core.Ecs.Systems;
using TheLastMageStanding.Game.Core.Events;
using TheLastMageStanding.Game.Core.Input;
using TheLastMageStanding.Game.Core.Loot;
using TheLastMageStanding.Game.Core.MetaProgression;
using TheLastMageStanding.Game.Core.Perks;
using TheLastMageStanding.Game.Core.Progression;
using TheLastMageStanding.Game.Core.SceneState;
using TheLastMageStanding.Game.Core.Skills;

namespace TheLastMageStanding.Game.Core.Ecs;

internal sealed class EcsWorldRunner : IDisposable
{
    private readonly EcsWorld _world = new();
    private readonly EventBus _eventBus;
    private readonly ScopedEventBus _worldEventBus;
    private readonly Camera2D _camera;
    private readonly SceneStateService _sceneStateService;
    private readonly SessionStateSystem _sessionStateSystem;
    private readonly PauseMenuSystem _pauseMenuSystem;
    private readonly HitStopSystem _hitStopSystem;
    private readonly EcsRuntimeRegistration _runtime;
    private readonly SlotPersistenceScope _slotPersistence;
    private readonly List<IDisposable> _disposables = new();
    private bool _disposed;

    public EcsWorldRunner(EcsWorldRunnerDependencies dependencies)
    {
        _eventBus = dependencies.EventBus;
        _worldEventBus = new ScopedEventBus(_eventBus);
        _world.EventBus = _worldEventBus;
        _camera = dependencies.Camera;
        _sceneStateService = dependencies.SceneStateService;
        _slotPersistence = dependencies.SlotPersistence;

        var waveConfig = EnemyWaveConfig.Default;
        var progressionConfig = ProgressionConfig.Default;
        var perkTreeConfig = PerkTreeConfig.Default;
        var perkService = new PerkService(perkTreeConfig);
        var lootConfig = LootDropConfig.CreateDefault();
        var itemRegistry = new ItemRegistry();
        var itemFactory = new ItemFactory(itemRegistry.GetAllDefinitions(), lootConfig);
        var skillRegistry = new SkillRegistry();
        var levelUpChoiceGenerator = new LevelUpChoiceGenerator(LevelUpChoiceConfig.Default, skillRegistry);

        var playerFactory = new PlayerEntityFactory(_world, progressionConfig);
        var enemyFactory = new EnemyEntityFactory(_world);
        var stageRunCleanupService = new StageRunEntityCleanupService();
        var stageRunResetService = new StageRunResetService(playerFactory);
        playerFactory.CreatePlayer(Vector2.Zero);

        var metaProgressionManager = new MetaProgressionManager(_worldEventBus, _slotPersistence);
        var profileService = _slotPersistence.PlayerProfile;
        var campaignProgressionService = new CampaignProgressionService(dependencies.StageRegistry, profileService);

        var sfxSystem = new SfxSystem(dependencies.AudioSettings);
        var settingsService = new RuntimeSettingsService(
            dependencies.AudioSettings,
            dependencies.AudioSettingsStore,
            dependencies.VideoSettings,
            dependencies.VideoSettingsStore,
            dependencies.InputBindings,
            dependencies.InputBindingsStore,
            dependencies.MusicService,
            sfxSystem);
        _sessionStateSystem = new SessionStateSystem(stageRunCleanupService, stageRunResetService);
        _pauseMenuSystem = new PauseMenuSystem();
        _hitStopSystem = new HitStopSystem();
        var settingsMenuSystem = new SettingsMenuSystem(settingsService);
        var sessionNotificationSystem = new SessionNotificationSystem();

        var context = new EcsRuntimeModuleContext(
            _world,
            dependencies.EventBus,
            _camera,
            dependencies.SceneStateService,
            dependencies.SceneManager,
            dependencies.StageRegistry,
            playerFactory,
            enemyFactory,
            waveConfig,
            progressionConfig,
            dependencies.AudioSettings,
            dependencies.AudioSettingsStore,
            dependencies.VideoSettings,
            dependencies.VideoSettingsStore,
            dependencies.InputBindings,
            dependencies.InputBindingsStore,
            dependencies.MusicService,
            _slotPersistence,
            metaProgressionManager,
            campaignProgressionService,
            perkTreeConfig,
            perkService,
            lootConfig,
            itemFactory,
            skillRegistry,
            levelUpChoiceGenerator,
            _sessionStateSystem,
            _pauseMenuSystem,
            settingsMenuSystem,
            sessionNotificationSystem,
            _hitStopSystem,
            sfxSystem);

        _runtime = EcsRuntimeComposer.Compose(context);

        foreach (var system in _runtime.EnumerateSystemsForInitialization())
        {
            system.Initialize(_world);
            if (system is IDisposable disposable)
            {
                _disposables.Add(disposable);
            }
        }

        _disposables.Add(metaProgressionManager);

        var sessionEntity = _world.CreateEntity();
        _world.SetComponent(sessionEntity, new GameSession(waveConfig.WaveIntervalSeconds));

        _eventBus.Publish(new RunStartedEvent());
    }

    public bool ExitRequested => _pauseMenuSystem.ExitRequested;

    public EcsWorld World => _world;

    public void LoadContent(GraphicsDevice graphicsDevice, ContentManager content)
    {
        ThrowIfDisposed();

        foreach (var system in _runtime.LoadContent.Systems)
        {
            system.LoadContent(_world, graphicsDevice, content);
        }
    }

    public void SetPlayerPosition(Vector2 position)
    {
        ThrowIfDisposed();

        _world.ForEach<PlayerTag, Position>(
            (Entity entity, ref PlayerTag _, ref Position playerPosition) =>
            {
                playerPosition = new Position(position);
                _world.SetComponent(entity, playerPosition);

                if (_world.TryGetComponent(entity, out Velocity velocity))
                {
                    velocity.Value = Vector2.Zero;
                    _world.SetComponent(entity, velocity);
                }
            });
    }

    public void ResetStageStateForNewRun()
    {
        ThrowIfDisposed();

        _sessionStateSystem.ResetForNewStage(_world);
    }

    public void Update(GameTime gameTime, InputState input)
    {
        ThrowIfDisposed();

        var mouseWorldPosition = _camera.ScreenToWorld(input.MouseScreenPosition);
        var context = new EcsUpdateContext(
            gameTime,
            (float)gameTime.ElapsedGameTime.TotalSeconds,
            input,
            _camera,
            mouseWorldPosition);

        RunUpdatePhase(_runtime.Update.Common, context);

        if (_sceneStateService.IsInHub())
        {
            RunUpdatePhase(_runtime.Update.Hub, context);
        }
        else if (_sceneStateService.IsInStage())
        {
            RunUpdatePhase(_runtime.StageSessionUpdate.Stage, context);

            if (GetSessionState() == GameState.Playing)
            {
                RunUpdatePhase(_runtime.StagePreGameplayUpdate.Stage, context);
                _camera.ShakeOffset = _hitStopSystem.CameraShakeOffset;

                if (_hitStopSystem.IsHitStopped())
                {
                    RunUpdatePhase(_runtime.StageHitStopFeedbackUpdate.Stage, context);
                }
                else
                {
                    RunUpdatePhase(_runtime.StageGameplayUpdate.Stage, context);
                }
            }
            else
            {
                _camera.ShakeOffset = Vector2.Zero;
            }
        }

        _eventBus.ProcessEvents();
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        ThrowIfDisposed();

        var context = new EcsDrawContext(spriteBatch, _camera);

        RunDrawPhase(_runtime.Draw.Common, context);

        if (_sceneStateService.IsInHub())
        {
            RunDrawPhase(_runtime.Draw.Hub, context);
        }
        else if (_sceneStateService.IsInStage())
        {
            RunDrawPhase(_runtime.Draw.Stage, context);
        }
    }

    public void DrawUI(SpriteBatch spriteBatch)
    {
        ThrowIfDisposed();

        var context = new EcsDrawContext(spriteBatch, _camera);

        if (_sceneStateService.IsInHub())
        {
            RunUiDrawPhase(_runtime.UiDraw.Hub, context);
        }

        RunUiDrawPhase(_runtime.UiDraw.Common, context);

        if (_sceneStateService.IsInStage())
        {
            RunUiDrawPhase(_runtime.UiDraw.Stage, context);
        }
    }

    public void DrawScreenSpaceUI(SpriteBatch spriteBatch)
    {
        ThrowIfDisposed();

        var context = new EcsDrawContext(spriteBatch, _camera);

        RunUiDrawPhase(_runtime.ScreenSpaceUiDraw.Common, context);

        if (_sceneStateService.IsInHub())
        {
            RunUiDrawPhase(_runtime.ScreenSpaceUiDraw.Hub, context);
        }
        else if (_sceneStateService.IsInStage())
        {
            RunUiDrawPhase(_runtime.ScreenSpaceUiDraw.Stage, context);
        }
    }

    private GameState GetSessionState()
    {
        var state = GameState.Playing;
        _world.ForEach<GameSession>((Entity _, ref GameSession session) =>
        {
            state = session.State;
        });

        return state;
    }

    public void SpawnHubNpcs(MonoGame.Extended.Tiled.TiledMap hubMap)
    {
        ThrowIfDisposed();

        var npcSpawnSystem = new NpcSpawnSystem(hubMap);
        npcSpawnSystem.Initialize(_world);

        var dummyContext = new EcsUpdateContext(
            new GameTime(),
            0f,
            new InputState(),
            _camera,
            Vector2.Zero);
        npcSpawnSystem.Update(_world, dummyContext);
    }

    private void RunUpdatePhase(System.Collections.Generic.IEnumerable<IUpdateSystem> systems, in EcsUpdateContext context)
    {
        foreach (var system in systems)
        {
            system.Update(_world, context);
        }
    }

    private void RunDrawPhase(System.Collections.Generic.IEnumerable<IDrawSystem> systems, in EcsDrawContext context)
    {
        foreach (var system in systems)
        {
            system.Draw(_world, context);
        }
    }

    private void RunUiDrawPhase(System.Collections.Generic.IEnumerable<IUiDrawSystem> systems, in EcsDrawContext context)
    {
        foreach (var system in systems)
        {
            system.Draw(_world, context);
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        _worldEventBus.Dispose();

        Exception? disposeError = null;
        for (var i = _disposables.Count - 1; i >= 0; i--)
        {
            try
            {
                _disposables[i].Dispose();
            }
            catch (Exception ex)
            {
                disposeError ??= ex;
            }
        }

        _disposables.Clear();

        if (disposeError != null)
        {
            throw disposeError;
        }
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }
}

internal sealed record EcsWorldRunnerDependencies(
    Camera2D Camera,
    AudioSettingsConfig AudioSettings,
    AudioSettingsStore AudioSettingsStore,
    VideoSettingsConfig VideoSettings,
    VideoSettingsStore VideoSettingsStore,
    InputBindingsConfig InputBindings,
    InputBindingsStore InputBindingsStore,
    MusicService MusicService,
    EventBus EventBus,
    StageRegistry StageRegistry,
    SceneStateService SceneStateService,
    SceneManager SceneManager,
    SlotPersistenceScope SlotPersistence);
