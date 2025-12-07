using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using TheLastMageStanding.Game.Core.Camera;
using TheLastMageStanding.Game.Core.Ecs.Config;
using TheLastMageStanding.Game.Core.Ecs.Components;
using TheLastMageStanding.Game.Core.Ecs.Systems;
using TheLastMageStanding.Game.Core.Input;
using TheLastMageStanding.Game.Core.Events;

namespace TheLastMageStanding.Game.Core.Ecs;

internal sealed class EcsWorldRunner
{
    private readonly EcsWorld _world = new();
    private readonly EventBus _eventBus = new();
    private readonly PlayerEntityFactory _playerFactory;
    private readonly EnemyEntityFactory _enemyFactory;
    private readonly EnemyWaveConfig _waveConfig;
    private readonly List<IUpdateSystem> _updateSystems;
    private readonly List<IDrawSystem> _drawSystems;
    private readonly List<ILoadContentSystem> _loadSystems;
    private readonly Camera2D _camera;

    public EcsWorldRunner(Camera2D camera)
    {
        _world.EventBus = _eventBus;
        _camera = camera;
        _waveConfig = EnemyWaveConfig.Default;
        _playerFactory = new PlayerEntityFactory(_world);
        _enemyFactory = new EnemyEntityFactory(_world);
        _playerFactory.CreatePlayer(Vector2.Zero);

        var enemyRenderSystem = new EnemyRenderSystem();
        var playerRenderSystem = new PlayerRenderSystem();
        var hitReactionSystem = new HitReactionSystem();
        var hitEffectSystem = new HitEffectSystem();
        var damageNumberSystem = new DamageNumberSystem();
        _updateSystems =
        [
            new InputSystem(),
            new WaveSchedulerSystem(_waveConfig),
            new SpawnSystem(_enemyFactory),
            new AiSeekSystem(),
            new MovementIntentSystem(),
            new CombatSystem(),
            hitReactionSystem,
            hitEffectSystem,
            new MovementSystem(),
            enemyRenderSystem,
            playerRenderSystem,
            new CameraFollowSystem(),
            damageNumberSystem,
            new CleanupSystem(),
            new IntentResetSystem(),
        ];

        _drawSystems =
        [
            enemyRenderSystem,
            playerRenderSystem,
            new RenderDebugSystem(),
            damageNumberSystem,
        ];

        _loadSystems =
            _updateSystems.OfType<ILoadContentSystem>()
                .Concat(_drawSystems.OfType<ILoadContentSystem>())
                .ToList();

        foreach (var system in _updateSystems)
        {
            system.Initialize(_world);
        }

        foreach (var system in _drawSystems)
        {
            system.Initialize(_world);
        }
    }

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

        foreach (var system in _updateSystems)
        {
            system.Update(_world, context);
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
}

