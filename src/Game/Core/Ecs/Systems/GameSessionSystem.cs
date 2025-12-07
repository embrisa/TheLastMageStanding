using Microsoft.Xna.Framework.Input;
using TheLastMageStanding.Game.Core.Ecs.Components;
using TheLastMageStanding.Game.Core.Events;

namespace TheLastMageStanding.Game.Core.Ecs.Systems;

/// <summary>
/// Manages game session state, subscribes to wave and player death events,
/// handles restart input, and creates HUD notifications.
/// </summary>
internal sealed class GameSessionSystem : IUpdateSystem
{
    private EcsWorld _world = null!;
    private Entity? _sessionEntity;
    private Entity? _notificationEntity;

    public void Initialize(EcsWorld world)
    {
        _world = world;
        world.EventBus.Subscribe<WaveStartedEvent>(OnWaveStarted);
        world.EventBus.Subscribe<WaveCompletedEvent>(OnWaveCompleted);
        world.EventBus.Subscribe<PlayerDiedEvent>(OnPlayerDied);
        world.EventBus.Subscribe<EnemyDiedEvent>(OnEnemyDied);
    }

    public void Update(EcsWorld world, in EcsUpdateContext context)
    {
        // Find or cache session entity
        if (_sessionEntity is null || !world.IsAlive(_sessionEntity.Value))
        {
            world.ForEach<GameSession>((Entity entity, ref GameSession _) =>
            {
                _sessionEntity = entity;
            });
            if (_sessionEntity is null)
                return;
        }

        // Get session component
        if (!world.TryGetComponent<GameSession>(_sessionEntity.Value, out var session))
            return;

        // Update wave timer if playing
        if (session.State == GameState.Playing)
        {
            session.WaveTimer += context.DeltaSeconds;
            session.TimeSurvived += context.DeltaSeconds;
            world.SetComponent(_sessionEntity.Value, session);
        }

        // Handle restart input if game over
        if (session.State == GameState.GameOver)
        {
            var keyboard = Keyboard.GetState();
            if (keyboard.IsKeyDown(Keys.R) || keyboard.IsKeyDown(Keys.Enter))
            {
                RestartSession(world);
            }
        }

        // Update notification timer
        if (_notificationEntity.HasValue && world.IsAlive(_notificationEntity.Value))
        {
            if (world.TryGetComponent<WaveNotification>(_notificationEntity.Value, out var notification))
            {
                notification.RemainingSeconds -= context.DeltaSeconds;
                if (notification.RemainingSeconds <= 0f)
                {
                    world.DestroyEntity(_notificationEntity.Value);
                    _notificationEntity = null;
                }
                else
                {
                    world.SetComponent(_notificationEntity.Value, notification);
                }
            }
        }
    }

    private void OnWaveStarted(WaveStartedEvent evt)
    {
        if (_sessionEntity is null)
            return;

        if (_world.TryGetComponent<GameSession>(_sessionEntity.Value, out var session))
        {
            session.CurrentWave = evt.WaveIndex;
            session.WaveTimer = 0f;
            _world.SetComponent(_sessionEntity.Value, session);

            // Create notification
            CreateNotification($"Wave {evt.WaveIndex} Started!", 2.5f);
        }
    }

    private void OnWaveCompleted(WaveCompletedEvent evt)
    {
        CreateNotification($"Wave {evt.WaveIndex} Complete!", 2.5f);
    }

    private void OnEnemyDied(EnemyDiedEvent evt)
    {
        if (_sessionEntity is null)
            return;

        if (_world.TryGetComponent<GameSession>(_sessionEntity.Value, out var session))
        {
            session.EnemiesKilled++;
            _world.SetComponent(_sessionEntity.Value, session);
        }
    }

    private void OnPlayerDied(PlayerDiedEvent evt)
    {
        if (_sessionEntity is null)
            return;

        if (_world.TryGetComponent<GameSession>(_sessionEntity.Value, out var session))
        {
            session.State = GameState.GameOver;
            _world.SetComponent(_sessionEntity.Value, session);

            // Create persistent game over notification
            CreateNotification("GAME OVER", float.MaxValue);
        }
    }

    private void CreateNotification(string message, float duration)
    {
        // Remove existing notification if any
        if (_notificationEntity.HasValue && _world.IsAlive(_notificationEntity.Value))
        {
            _world.DestroyEntity(_notificationEntity.Value);
        }

        // Create new notification entity
        _notificationEntity = _world.CreateEntity();
        _world.SetComponent(_notificationEntity.Value, new WaveNotification(message, duration));
    }

    private void RestartSession(EcsWorld world)
    {
        // Get session
        if (_sessionEntity is null || !world.TryGetComponent<GameSession>(_sessionEntity.Value, out var session))
            return;

        // Clear all enemies
        var enemiesToRemove = new List<Entity>();
        world.ForEach<Faction>((Entity entity, ref Faction faction) =>
        {
            if (faction == Faction.Enemy)
            {
                enemiesToRemove.Add(entity);
            }
        });
        foreach (var entity in enemiesToRemove)
        {
            world.DestroyEntity(entity);
        }

        // Restore player health
        world.ForEach<PlayerTag, Health>((Entity entity, ref PlayerTag _, ref Health health) =>
        {
            health.Current = health.Max;
        });

        // Reset player position and velocity
        world.ForEach<PlayerTag, Position, Velocity>((Entity entity, ref PlayerTag _, ref Position pos, ref Velocity vel) =>
        {
            pos.Value = System.Numerics.Vector2.Zero;
            vel.Value = System.Numerics.Vector2.Zero;
        });

        // Reset session state
        session.State = GameState.Playing;
        session.CurrentWave = 0;
        session.WaveTimer = 0f;
        session.EnemiesKilled = 0;
        session.TimeSurvived = 0f;
        world.SetComponent(_sessionEntity.Value, session);

        // Clear notification
        if (_notificationEntity.HasValue && world.IsAlive(_notificationEntity.Value))
        {
            world.DestroyEntity(_notificationEntity.Value);
            _notificationEntity = null;
        }
    }
}
