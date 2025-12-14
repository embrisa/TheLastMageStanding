using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using TheLastMageStanding.Game.Core.Camera;
using TheLastMageStanding.Game.Core.Ecs.Config;
using TheLastMageStanding.Game.Core.Ecs.Components;
using TheLastMageStanding.Game.Core.Ecs.Systems;
using TheLastMageStanding.Game.Core.Ecs.Systems.Collision;
using TheLastMageStanding.Game.Core.Input;
using TheLastMageStanding.Game.Core.Events;
using TheLastMageStanding.Game.Core.Config;
using TheLastMageStanding.Game.Core.Audio;
using TheLastMageStanding.Game.Core.Perks;
using TheLastMageStanding.Game.Core.Skills;
using TheLastMageStanding.Game.Core.Loot;
using TheLastMageStanding.Game.Core.MetaProgression;
using TheLastMageStanding.Game.Core.SceneState;
using TheLastMageStanding.Game.Core.Progression;
using TheLastMageStanding.Game.Core.Campaign;

namespace TheLastMageStanding.Game.Core.Ecs;

internal sealed class EcsWorldRunner
{
    private readonly EcsWorld _world = new();
    private readonly EventBus _eventBus;
    private readonly PlayerEntityFactory _playerFactory;
    private readonly EnemyEntityFactory _enemyFactory;
    private readonly EnemyWaveConfig _waveConfig;
    private readonly ProgressionConfig _progressionConfig;
    private readonly AudioSettingsConfig _audioSettings;
    private readonly AudioSettingsStore _audioSettingsStore;
    private readonly VideoSettingsConfig _videoSettings;
    private readonly VideoSettingsStore _videoSettingsStore;
    private readonly InputBindingsConfig _inputBindings;
    private readonly InputBindingsStore _inputBindingsStore;
    private readonly MusicService _musicService;
    private readonly SaveSlotService _saveSlotService;
    private readonly string _slotId;
    private readonly SfxSystem _sfxSystem;
    private readonly GameSessionSystem _gameSessionSystem;
    private readonly HitStopSystem _hitStopSystem;
    private readonly MetaProgressionManager _metaProgressionManager;
    private readonly SceneStateService _sceneStateService;
    private readonly StageRegistry _stageRegistry;
    private readonly List<IUpdateSystem> _updateSystems;
    private readonly List<IUpdateSystem> _hubOnlyUpdateSystems;
    private readonly List<IUpdateSystem> _stageOnlyUpdateSystems;
    private readonly List<IDrawSystem> _drawSystems;
    private readonly List<IDrawSystem> _hubOnlyDrawSystems;
    private readonly List<IDrawSystem> _stageOnlyDrawSystems;
    private readonly List<IUiDrawSystem> _hubOnlyUiDrawSystems;
    private readonly List<IUiDrawSystem> _uiDrawSystems;
    private readonly List<IUiDrawSystem> _screenSpaceUiDrawSystems;
    private readonly List<ILoadContentSystem> _loadSystems;
    private readonly Camera2D _camera;

    public EcsWorldRunner(
        Camera2D camera,
        AudioSettingsConfig audioSettings,
        AudioSettingsStore audioSettingsStore,
        VideoSettingsConfig videoSettings,
        VideoSettingsStore videoSettingsStore,
        InputBindingsConfig inputBindings,
        InputBindingsStore inputBindingsStore,
        MusicService musicService,
        EventBus eventBus,
        StageRegistry stageRegistry,
        SceneStateService sceneStateService,
        SceneManager sceneManager,
        SaveSlotService saveSlotService,
        string slotId)
    {
        _eventBus = eventBus;
        _world.EventBus = _eventBus;
        _camera = camera;
        _sceneStateService = sceneStateService;
        _stageRegistry = stageRegistry;
        _audioSettings = audioSettings;
        _audioSettingsStore = audioSettingsStore;
        _videoSettings = videoSettings;
        _videoSettingsStore = videoSettingsStore;
        _inputBindings = inputBindings;
        _inputBindingsStore = inputBindingsStore;
        _musicService = musicService;
        _saveSlotService = saveSlotService;
        _slotId = slotId;
        _waveConfig = EnemyWaveConfig.Default;
        _progressionConfig = ProgressionConfig.Default;
        var perkTreeConfig = PerkTreeConfig.Default;
        var perkService = new PerkService(perkTreeConfig);
        _playerFactory = new PlayerEntityFactory(_world, _progressionConfig);
        _enemyFactory = new EnemyEntityFactory(_world);
        _playerFactory.CreatePlayer(Vector2.Zero);

        // Initialize meta progression system
        _metaProgressionManager = new MetaProgressionManager(_eventBus, _saveSlotService, _slotId);

        var lootConfig = LootDropConfig.CreateDefault();
        var itemRegistry = new ItemRegistry();
        var itemFactory = new ItemFactory(itemRegistry.GetAllDefinitions(), lootConfig);

        _sfxSystem = new SfxSystem(_audioSettings);
        _gameSessionSystem = new GameSessionSystem(_audioSettings, _audioSettingsStore, _videoSettings, _videoSettingsStore, _inputBindings, _inputBindingsStore, _musicService, _sfxSystem);
        var enemyRenderSystem = new EnemyRenderSystem();
        var playerRenderSystem = new PlayerRenderSystem();
        var hitReactionSystem = new HitReactionSystem();
        var hitEffectSystem = new HitEffectSystem();
        var damageNumberSystem = new DamageNumberSystem();
        var xpOrbRenderSystem = new XpOrbRenderSystem();
        var collisionSystem = new CollisionSystem();
        var collisionResolutionSystem = new CollisionResolutionSystem();
        var knockbackSystem = new KnockbackSystem();
        var dynamicSeparationSystem = new DynamicSeparationSystem();
        var contactDamageSystem = new ContactDamageSystem();
        var meleeHitSystem = new MeleeHitSystem();
        var projectileRenderSystem = new ProjectileRenderSystem();
        var collisionDebugRenderSystem = new CollisionDebugRenderSystem();
        var statusEffectDebugSystem = new StatusEffectDebugSystem();
        var aiDebugRenderSystem = new AiDebugRenderSystem();
        var debugInputSystem = new DebugInputSystem(collisionDebugRenderSystem, _enemyFactory, statusEffectDebugSystem, aiDebugRenderSystem);
        var animationEventSystem = new AnimationEventSystem();
        var hitStopSystem = new HitStopSystem();
        _hitStopSystem = hitStopSystem;  // Store reference for hit-stop checks
        var vfxSystem = new VfxSystem();
        var telegraphSystem = new TelegraphSystem();
        var telegraphRenderSystem = new TelegraphRenderSystem();
        var perkPointGrantSystem = new PerkPointGrantSystem(perkTreeConfig);
        var perkEffectApplicationSystem = new PerkEffectApplicationSystem(perkService);
        var perkAutoSaveSystem = new PerkAutoSaveSystem();
        var perkTreeUISystem = new PerkTreeUISystem(perkTreeConfig, perkService);
        var statusEffectApplicationSystem = new StatusEffectApplicationSystem();
        var statusEffectTickSystem = new StatusEffectTickSystem();
        var statusEffectVfxSystem = new StatusEffectVfxSystem();
        var eliteModifierSystem = new EliteModifierSystem();
        var statRecalculationSystem = new StatRecalculationSystem();
        var aiChargerSystem = new AiChargerSystem();
        var aiProtectorSystem = new AiProtectorSystem();
        var aiBufferSystem = new AiBufferSystem();
        var buffTickSystem = new BuffTickSystem();
        var lootDropSystem = new LootDropSystem(itemFactory, lootConfig);
        var lootPickupSystem = new LootPickupSystem();

        // Skill system
        var skillRegistry = new SkillRegistry();
        var playerSkillInputSystem = new PlayerSkillInputSystem();
        var skillCastSystem = new SkillCastSystem(skillRegistry);
        var skillExecutionSystem = new SkillExecutionSystem(skillRegistry);
        var skillHotbarRenderer = new Rendering.UI.SkillHotbarRenderer(skillRegistry);
        var levelUpChoiceGenerator = new LevelUpChoiceGenerator(LevelUpChoiceConfig.Default, skillRegistry);
        var levelUpChoiceSystem = new LevelUpChoiceSystem(levelUpChoiceGenerator);
        var dashInputSystem = new DashInputSystem();
        var dashExecutionSystem = new DashExecutionSystem(hitStopSystem);
        var dashMovementSystem = new DashMovementSystem();

        // Hub-specific systems
        var profileService = new PlayerProfileService(new DefaultFileSystem(), _saveSlotService.GetSlotPath(_slotId));
        var campaignProgressionService = new CampaignProgressionService(_stageRegistry, profileService);
        var stageSelectionUI = new StageSelectionUISystem(_stageRegistry, sceneManager, campaignProgressionService);
        var runHistoryUI = new RunHistoryUISystem(_metaProgressionManager.HistoryService, _sceneStateService);
        var pauseMenuUiSystem = new PauseMenuMyraSystem(_sceneStateService);
        var levelUpChoiceUiSystem = new LevelUpChoiceMyraSystem(_sceneStateService);
        var inventoryUiSystem = new InventoryUiSystem();
        var proximityInteractionSystem = new ProximityInteractionSystem();
        var interactionInputSystem = new InteractionInputSystem();
        var hubMenuSystem = new HubMenuSystem(_sceneStateService);
        var proximityPromptRenderSystem = new ProximityPromptRenderSystem();
        var npcRenderSystem = new NpcRenderSystem();

        // Stage completion system
        var stageRunInitializationSystem = new StageRunInitializationSystem(_sceneStateService, _stageRegistry);
        var stageCompletionSystem = new StageCompletionSystem(sceneManager, _sceneStateService, campaignProgressionService);
        var bossPhaseSystem = new BossPhaseSystem(_stageRegistry);

        // Stage-only systems (combat, waves, etc)
        _stageOnlyUpdateSystems =
        [
            _gameSessionSystem,  // Run timer, pause menu, wave tracking (stage-only)
            stageRunInitializationSystem, // Sync stage run parameters
            stageCompletionSystem,  // Handle stage completion and transitions
            dashInputSystem,
            dashExecutionSystem,
            dashMovementSystem,
            playerSkillInputSystem,  // Convert attack input to skill cast requests
            skillCastSystem,  // Validate and gate skill casts
            skillExecutionSystem,  // Execute completed skill casts
            statusEffectApplicationSystem,  // Apply statuses from hits
            statusEffectTickSystem,  // Tick DoTs/debuffs
            eliteModifierSystem, // Elite modifier runtime effects
            statRecalculationSystem,  // Recalculate stats before combat systems
            new WaveSchedulerSystem(_waveConfig),
            new SpawnSystem(_enemyFactory),
            bossPhaseSystem,
            new AiSeekSystem(),
            new RangedAttackSystem(),  // Handle ranged enemy AI
            aiChargerSystem,
            aiProtectorSystem,
            aiBufferSystem,
            buffTickSystem,
            new ProjectileUpdateSystem(),  // Update projectile lifetimes
            knockbackSystem,  // Apply knockback before collision resolution
            dynamicSeparationSystem,  // Separate overlapping dynamic entities
            contactDamageSystem,  // Handle contact damage with cooldowns
            meleeHitSystem,  // Handle attack hitbox collisions
            new ProjectileHitSystem(),  // Handle projectile collisions
            animationEventSystem,  // Process animation events and spawn hitboxes
            new CombatSystem(),
            hitStopSystem,  // Handle hit-stop timing
            vfxSystem,  // Process VFX spawns
            statusEffectVfxSystem,  // Status VFX/SFX hooks
            telegraphSystem,  // Update telegraph lifetimes
            hitReactionSystem,
            hitEffectSystem,
            new XpOrbSpawnSystem(_progressionConfig),
            new XpCollectionSystem(_progressionConfig),
            lootDropSystem,
            lootPickupSystem,
            new LevelUpSystem(levelUpChoiceGenerator),
            perkPointGrantSystem,
            perkEffectApplicationSystem,
        ];

        // Hub-only systems
        _hubOnlyUpdateSystems =
        [
            stageSelectionUI,
            runHistoryUI,
            proximityInteractionSystem,
            interactionInputSystem,
            hubMenuSystem,
        ];

        // Common systems (run in both hub and stage)
        _updateSystems =
        [
            debugInputSystem,  // Handle debug input early
            new InputSystem(),  // Read input (WASD, etc.)
            levelUpChoiceSystem,  // Level-up choice input should work while paused
            new MovementIntentSystem(),  // Convert input to velocity
            new MovementSystem(),  // Apply velocity to position
            new CameraFollowSystem(),  // Camera follows player
            collisionResolutionSystem,  // Resolve collisions before applying movement
            collisionSystem,  // Detect collisions after resolution
            _sfxSystem,  // Process SFX playback
            enemyRenderSystem,
            playerRenderSystem,
            damageNumberSystem,
            perkAutoSaveSystem,
            perkTreeUISystem,
            inventoryUiSystem,  // Allow inventory viewing in both hub and stage
            new CleanupSystem(),
            new IntentResetSystem(),
        ];

        _stageOnlyDrawSystems =
        [
            enemyRenderSystem,
            playerRenderSystem,
            new RenderDebugSystem(),
            statusEffectDebugSystem,
            aiDebugRenderSystem,
            projectileRenderSystem,
            telegraphRenderSystem,  // Draw telegraphs and VFX
            damageNumberSystem,
            xpOrbRenderSystem,
            collisionDebugRenderSystem,  // Draw collision debug last
        ];

        _hubOnlyDrawSystems =
        [
            npcRenderSystem,
            proximityPromptRenderSystem,
        ];

        // Common draw systems
        _drawSystems =
        [
            playerRenderSystem,
            damageNumberSystem,
        ];

        _hubOnlyUiDrawSystems =
        [
            hubMenuSystem,
        ];

        _screenSpaceUiDrawSystems =
        [
            stageSelectionUI,
            runHistoryUI,
            pauseMenuUiSystem,
            levelUpChoiceUiSystem,
        ];

        _uiDrawSystems =
        [
            new HudRenderSystem(),
            skillHotbarRenderer,
            perkTreeUISystem,
            inventoryUiSystem,
        ];

        _loadSystems =
            _updateSystems.OfType<ILoadContentSystem>()
                .Concat(_hubOnlyUpdateSystems.OfType<ILoadContentSystem>())
                .Concat(_stageOnlyUpdateSystems.OfType<ILoadContentSystem>())
                .Concat(_drawSystems.OfType<ILoadContentSystem>())
                .Concat(_hubOnlyDrawSystems.OfType<ILoadContentSystem>())
                .Concat(_stageOnlyDrawSystems.OfType<ILoadContentSystem>())
                .Concat(_hubOnlyUiDrawSystems.OfType<ILoadContentSystem>())
                .Concat(_screenSpaceUiDrawSystems.OfType<ILoadContentSystem>())
                .Concat(_uiDrawSystems.OfType<ILoadContentSystem>())
                .ToList();

        foreach (var system in _updateSystems)
        {
            system.Initialize(_world);
        }

        foreach (var system in _hubOnlyUpdateSystems)
        {
            system.Initialize(_world);
        }

        foreach (var system in _stageOnlyUpdateSystems)
        {
            system.Initialize(_world);
        }

        foreach (var system in _drawSystems)
        {
            system.Initialize(_world);
        }

        foreach (var system in _hubOnlyDrawSystems)
        {
            system.Initialize(_world);
        }

        foreach (var system in _stageOnlyDrawSystems)
        {
            system.Initialize(_world);
        }

        foreach (var system in _hubOnlyUiDrawSystems)
        {
            system.Initialize(_world);
        }

        foreach (var system in _uiDrawSystems)
        {
            system.Initialize(_world);
        }

        foreach (var system in _screenSpaceUiDrawSystems)
        {
            system.Initialize(_world);
        }

        // Create session entity with initial state
        var sessionEntity = _world.CreateEntity();
        _world.SetComponent(sessionEntity, new GameSession(_waveConfig.WaveIntervalSeconds));

        // Publish run started event for meta progression
        _eventBus.Publish(new RunStartedEvent());
    }

    public bool ExitRequested => _gameSessionSystem.ExitRequested;

    /// <summary>
    /// Exposes the ECS world for map collision loading and other external integrations.
    /// </summary>
    public EcsWorld World => _world;

    public void LoadContent(GraphicsDevice graphicsDevice, ContentManager content)
    {
        foreach (var loadSystem in _loadSystems)
        {
            loadSystem.LoadContent(_world, graphicsDevice, content);
        }
    }

    public void SetPlayerPosition(Vector2 position)
    {
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

    /// <summary>
    /// Clears stage-only state when starting a fresh run (stage reload/restart).
    /// </summary>
    public void ResetStageStateForNewRun()
    {
        _gameSessionSystem.ResetForNewStage(_world);
    }

    public void Update(GameTime gameTime, InputState input)
    {
        // Transform mouse screen position to world space
        var mouseWorldPosition = _camera.ScreenToWorld(input.MouseScreenPosition);

        var context = new EcsUpdateContext(
            gameTime,
            (float)gameTime.ElapsedGameTime.TotalSeconds,
            input,
            _camera,
            mouseWorldPosition);

        var isInHub = _sceneStateService.IsInHub();
        var isInStage = _sceneStateService.IsInStage();

        // Always run common systems
        foreach (var system in _updateSystems)
        {
            system.Update(_world, context);
        }

        // Run scene-specific systems
        if (isInHub)
        {
            foreach (var system in _hubOnlyUpdateSystems)
            {
                system.Update(_world, context);
            }
        }
        else if (isInStage)
        {
            // Always run session system to handle pause/resume/restart
            _gameSessionSystem.Update(_world, context);

            var sessionState = GetSessionState();
            if (sessionState == GameState.Playing)
            {
                // Run hit-stop system first to track timing
                _hitStopSystem.Update(_world, context);

                // Apply camera shake from hit-stop system
                _camera.ShakeOffset = _hitStopSystem.CameraShakeOffset;

                // If hit-stopped, only update certain systems (VFX, SFX, visual feedback)
                if (_hitStopSystem.IsHitStopped())
                {
                    // During hit-stop, only update visual/audio feedback systems
                    // Skip movement, combat logic, etc.
                    foreach (var system in _stageOnlyUpdateSystems)
                    {
                        // Allow VFX, SFX, visual effects to update during hit-stop
                        if (system is VfxSystem || system is SfxSystem ||
                            system is HitEffectSystem || system is TelegraphSystem)
                        {
                            system.Update(_world, context);
                        }
                    }
                }
                else
                {
                    // Normal update - run all stage systems except hit-stop (already run)
                    foreach (var system in _stageOnlyUpdateSystems)
                    {
                        if (system != _hitStopSystem)
                        {
                            system.Update(_world, context);
                        }
                    }
                }
            }
        }

        _eventBus.ProcessEvents();
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        var context = new EcsDrawContext(spriteBatch, _camera);
        var isInHub = _sceneStateService.IsInHub();
        var isInStage = _sceneStateService.IsInStage();

        // Draw common systems
        foreach (var system in _drawSystems)
        {
            system.Draw(_world, context);
        }

        // Draw scene-specific systems
        if (isInHub)
        {
            foreach (var system in _hubOnlyDrawSystems)
            {
                system.Draw(_world, context);
            }
        }
        else if (isInStage)
        {
            foreach (var system in _stageOnlyDrawSystems)
            {
                system.Draw(_world, context);
            }
        }
    }

    public void DrawUI(SpriteBatch spriteBatch)
    {
        var context = new EcsDrawContext(spriteBatch, _camera);
        var isInHub = _sceneStateService.IsInHub();

        if (isInHub)
        {
            foreach (var system in _hubOnlyUiDrawSystems)
            {
                system.Draw(_world, context);
            }
        }

        foreach (var system in _uiDrawSystems)
        {
            system.Draw(_world, context);
        }
    }

    public void DrawScreenSpaceUI(SpriteBatch spriteBatch)
    {
        var context = new EcsDrawContext(spriteBatch, _camera);
        foreach (var system in _screenSpaceUiDrawSystems)
        {
            system.Draw(_world, context);
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

    /// <summary>
    /// Spawns NPC entities from the hub map after map loading.
    /// Should be called when entering the hub scene.
    /// </summary>
    public void SpawnHubNpcs(MonoGame.Extended.Tiled.TiledMap hubMap)
    {
        var npcSpawnSystem = new NpcSpawnSystem(hubMap);
        npcSpawnSystem.Initialize(_world);

        // Run once to spawn NPCs
        var dummyContext = new EcsUpdateContext(
            new Microsoft.Xna.Framework.GameTime(),
            0f,
            new Input.InputState(),
            _camera,
            Microsoft.Xna.Framework.Vector2.Zero
        );
        npcSpawnSystem.Update(_world, dummyContext);
    }
}
