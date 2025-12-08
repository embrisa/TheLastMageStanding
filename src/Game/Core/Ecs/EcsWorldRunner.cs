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

namespace TheLastMageStanding.Game.Core.Ecs;

internal sealed class EcsWorldRunner
{
    private readonly EcsWorld _world = new();
    private readonly EventBus _eventBus = new();
    private readonly PlayerEntityFactory _playerFactory;
    private readonly EnemyEntityFactory _enemyFactory;
    private readonly EnemyWaveConfig _waveConfig;
    private readonly ProgressionConfig _progressionConfig;
    private readonly AudioSettingsConfig _audioSettings;
    private readonly AudioSettingsStore _audioSettingsStore;
    private readonly MusicService _musicService;
    private readonly SfxSystem _sfxSystem;
    private readonly GameSessionSystem _gameSessionSystem;
    private readonly HitStopSystem _hitStopSystem;
    private readonly List<IUpdateSystem> _updateSystems;
    private readonly List<IDrawSystem> _drawSystems;
    private readonly List<IDrawSystem> _uiDrawSystems;
    private readonly List<ILoadContentSystem> _loadSystems;
    private readonly Camera2D _camera;

    public EcsWorldRunner(
        Camera2D camera,
        AudioSettingsConfig audioSettings,
        AudioSettingsStore audioSettingsStore,
        MusicService musicService)
    {
        _world.EventBus = _eventBus;
        _camera = camera;
        _audioSettings = audioSettings;
        _audioSettingsStore = audioSettingsStore;
        _musicService = musicService;
        _waveConfig = EnemyWaveConfig.Default;
        _progressionConfig = ProgressionConfig.Default;
        _playerFactory = new PlayerEntityFactory(_world, _progressionConfig);
        _enemyFactory = new EnemyEntityFactory(_world);
        _playerFactory.CreatePlayer(Vector2.Zero);

        _sfxSystem = new SfxSystem(_audioSettings);
        _gameSessionSystem = new GameSessionSystem(_audioSettings, _audioSettingsStore, _musicService, _sfxSystem);
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
        var debugInputSystem = new DebugInputSystem(collisionDebugRenderSystem);
        var animationEventSystem = new AnimationEventSystem();
        var hitStopSystem = new HitStopSystem();
        _hitStopSystem = hitStopSystem;  // Store reference for hit-stop checks
        var vfxSystem = new VfxSystem();
        var telegraphSystem = new TelegraphSystem();
        var telegraphRenderSystem = new TelegraphRenderSystem();

        _updateSystems =
        [
            _gameSessionSystem,
            debugInputSystem,  // Handle debug input early
            new InputSystem(),
            new StatRecalculationSystem(),  // Recalculate stats before combat systems
            new WaveSchedulerSystem(_waveConfig),
            new SpawnSystem(_enemyFactory),
            new RangedAttackSystem(),  // Handle ranged enemy AI
            new AiSeekSystem(),
            new MovementIntentSystem(),
            new ProjectileUpdateSystem(),  // Update projectile lifetimes
            knockbackSystem,  // Apply knockback before collision resolution
            collisionResolutionSystem,  // Resolve collisions before applying movement
            collisionSystem,  // Detect collisions after resolution
            dynamicSeparationSystem,  // Separate overlapping dynamic entities
            contactDamageSystem,  // Handle contact damage with cooldowns
            meleeHitSystem,  // Handle attack hitbox collisions
            new ProjectileHitSystem(),  // Handle projectile collisions
            animationEventSystem,  // Process animation events and spawn hitboxes
            new CombatSystem(),
            hitStopSystem,  // Handle hit-stop timing
            vfxSystem,  // Process VFX spawns
            _sfxSystem,  // Process SFX playback
            telegraphSystem,  // Update telegraph lifetimes
            hitReactionSystem,
            hitEffectSystem,
            new MovementSystem(),
            enemyRenderSystem,
            playerRenderSystem,
            new CameraFollowSystem(),
            damageNumberSystem,
            new XpOrbSpawnSystem(_progressionConfig),
            new XpCollectionSystem(_progressionConfig),
            new LevelUpSystem(_progressionConfig),
            new CleanupSystem(),
            new IntentResetSystem(),
        ];

        _drawSystems =
        [
            enemyRenderSystem,
            playerRenderSystem,
            new RenderDebugSystem(),
            projectileRenderSystem,
            telegraphRenderSystem,  // Draw telegraphs and VFX
            damageNumberSystem,
            xpOrbRenderSystem,
            collisionDebugRenderSystem,  // Draw collision debug last
        ];

        _uiDrawSystems =
        [
            new HudRenderSystem(),
        ];

        _loadSystems =
            _updateSystems.OfType<ILoadContentSystem>()
                .Concat(_drawSystems.OfType<ILoadContentSystem>())
                .Concat(_uiDrawSystems.OfType<ILoadContentSystem>())
                .ToList();

        foreach (var system in _updateSystems)
        {
            system.Initialize(_world);
        }

        foreach (var system in _drawSystems)
        {
            system.Initialize(_world);
        }

        foreach (var system in _uiDrawSystems)
        {
            system.Initialize(_world);
        }

        // Create session entity with initial state
        var sessionEntity = _world.CreateEntity();
        _world.SetComponent(sessionEntity, new GameSession(_waveConfig.WaveIntervalSeconds));
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

    public void Update(GameTime gameTime, InputState input)
    {
        var context = new EcsUpdateContext(
            gameTime,
            (float)gameTime.ElapsedGameTime.TotalSeconds,
            input,
            _camera);

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
                for (int i = 1; i < _updateSystems.Count; i++)
                {
                    var system = _updateSystems[i];
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
                // Normal update - skip hit-stop system as it's already run
                for (int i = 1; i < _updateSystems.Count; i++)
                {
                    if (_updateSystems[i] != _hitStopSystem)
                    {
                        _updateSystems[i].Update(_world, context);
                    }
                }
            }
        }

        _eventBus.ProcessEvents();
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        var context = new EcsDrawContext(spriteBatch, _camera);
        foreach (var system in _drawSystems)
        {
            system.Draw(_world, context);
        }
    }

    public void DrawUI(SpriteBatch spriteBatch)
    {
        var context = new EcsDrawContext(spriteBatch, _camera);
        foreach (var system in _uiDrawSystems)
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
}

