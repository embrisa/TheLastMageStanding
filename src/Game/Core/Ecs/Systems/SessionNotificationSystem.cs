using TheLastMageStanding.Game.Core.Ecs.Components;
using TheLastMageStanding.Game.Core.Events;

namespace TheLastMageStanding.Game.Core.Ecs.Systems;

/// <summary>
/// Owns stage session notifications and locked-feature HUD message lifetimes.
/// </summary>
internal sealed class SessionNotificationSystem : IUpdateSystem
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
        world.EventBus.Subscribe<SessionRestartedEvent>(OnSessionRestarted);
    }

    public void Update(EcsWorld world, in EcsUpdateContext context)
    {
        UpdateNotificationTimer(world, context.DeltaSeconds);
        UpdateLockedFeatureTimer(world, context.DeltaSeconds);
    }

    private void OnWaveStarted(WaveStartedEvent evt) => CreateNotification($"Wave {evt.WaveIndex} Started!", 2.5f);

    private void OnWaveCompleted(WaveCompletedEvent evt) => CreateNotification($"Wave {evt.WaveIndex} Complete!", 2.5f);

    private void OnPlayerDied(PlayerDiedEvent evt) => CreateNotification("GAME OVER", float.MaxValue);

    private void OnSessionRestarted(SessionRestartedEvent evt)
    {
        if (_notificationEntity.HasValue && _world.IsAlive(_notificationEntity.Value))
        {
            _world.DestroyEntity(_notificationEntity.Value);
        }

        _notificationEntity = null;
    }

    private void CreateNotification(string message, float duration)
    {
        if (_notificationEntity.HasValue && _world.IsAlive(_notificationEntity.Value))
        {
            _world.DestroyEntity(_notificationEntity.Value);
        }

        _notificationEntity = _world.CreateEntity();
        _world.SetComponent(_notificationEntity.Value, new WaveNotification(message, duration));
    }

    private void UpdateNotificationTimer(EcsWorld world, float deltaSeconds)
    {
        if (!_notificationEntity.HasValue || !world.IsAlive(_notificationEntity.Value))
        {
            return;
        }

        if (!world.TryGetComponent<WaveNotification>(_notificationEntity.Value, out var notification))
        {
            return;
        }

        notification.RemainingSeconds -= deltaSeconds;
        if (notification.RemainingSeconds <= 0f)
        {
            world.DestroyEntity(_notificationEntity.Value);
            _notificationEntity = null;
            return;
        }

        world.SetComponent(_notificationEntity.Value, notification);
    }

    private void UpdateLockedFeatureTimer(EcsWorld world, float deltaSeconds)
    {
        if (!TryGetSessionEntity(world, out var sessionEntity) ||
            !world.TryGetComponent(sessionEntity, out LockedFeatureMessage lockedMessage))
        {
            return;
        }

        lockedMessage.RemainingSeconds -= deltaSeconds;
        if (lockedMessage.RemainingSeconds <= 0f)
        {
            world.RemoveComponent<LockedFeatureMessage>(sessionEntity);
            return;
        }

        world.SetComponent(sessionEntity, lockedMessage);
    }

    private bool TryGetSessionEntity(EcsWorld world, out Entity sessionEntity)
    {
        if (_sessionEntity is not null && world.IsAlive(_sessionEntity.Value))
        {
            sessionEntity = _sessionEntity.Value;
            return true;
        }

        _sessionEntity = null;
        world.ForEach<GameSession>((Entity entity, ref GameSession _) =>
        {
            _sessionEntity = entity;
        });

        sessionEntity = _sessionEntity ?? default;
        return _sessionEntity.HasValue;
    }
}
