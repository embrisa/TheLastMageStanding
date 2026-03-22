using TheLastMageStanding.Game.Core.Ecs.Components;
using TheLastMageStanding.Game.Core.Events;

namespace TheLastMageStanding.Game.Core.Ecs.Systems;

/// <summary>
/// Owns session state transitions, run timers, death state, and restart/reset flow.
/// </summary>
internal sealed class SessionStateSystem : IUpdateSystem
{
    private readonly StageRunEntityCleanupService _cleanupService;
    private readonly StageRunResetService _resetService;
    private EcsWorld _world = null!;
    private Entity? _sessionEntity;

    public SessionStateSystem(
        StageRunEntityCleanupService cleanupService,
        StageRunResetService resetService)
    {
        _cleanupService = cleanupService;
        _resetService = resetService;
    }

    public void Initialize(EcsWorld world)
    {
        _world = world;
        world.EventBus.Subscribe<WaveStartedEvent>(OnWaveStarted);
        world.EventBus.Subscribe<EnemyDiedEvent>(OnEnemyDied);
        world.EventBus.Subscribe<PlayerDiedEvent>(OnPlayerDied);
        world.EventBus.Subscribe<SessionStateRequestEvent>(OnSessionStateRequested);
    }

    public void Update(EcsWorld world, in EcsUpdateContext context)
    {
        if (!TryGetSessionEntity(world, out var sessionEntity) ||
            !world.TryGetComponent(sessionEntity, out GameSession session))
        {
            return;
        }

        if (IsLevelUpChoiceOpen(world) && session.State == GameState.Playing)
        {
            SetSessionState(world, sessionEntity, ref session, GameState.Paused);
        }

        if (session.State == GameState.Playing)
        {
            session.WaveTimer += context.DeltaSeconds;
            session.TimeSurvived += context.DeltaSeconds;
            world.SetComponent(sessionEntity, session);
            return;
        }

        if (session.State == GameState.GameOver &&
            (context.Input.RestartPressed || context.Input.MenuConfirmPressed))
        {
            RestartSession(world, ref session);
        }
    }

    /// <summary>
    /// Resets stage run state when starting a new stage (or restarting the same stage via transition).
    /// </summary>
    public void ResetForNewStage(EcsWorld world)
    {
        if (!TryGetSessionEntity(world, out var sessionEntity) ||
            !world.TryGetComponent(sessionEntity, out GameSession session))
        {
            return;
        }

        RestartSession(world, ref session);
    }

    private void OnSessionStateRequested(SessionStateRequestEvent evt)
    {
        if (!TryGetSessionEntity(_world, out var sessionEntity) ||
            !_world.TryGetComponent(sessionEntity, out GameSession session))
        {
            return;
        }

        switch (evt.Command)
        {
            case SessionStateCommand.Pause when session.State == GameState.Playing:
                SetSessionState(_world, sessionEntity, ref session, GameState.Paused);
                break;
            case SessionStateCommand.Resume when session.State == GameState.Paused:
                SetSessionState(_world, sessionEntity, ref session, GameState.Playing);
                break;
            case SessionStateCommand.Restart:
                RestartSession(_world, ref session);
                break;
        }
    }

    private void OnWaveStarted(WaveStartedEvent evt)
    {
        if (!TryGetSessionEntity(_world, out var sessionEntity) ||
            !_world.TryGetComponent(sessionEntity, out GameSession session))
        {
            return;
        }

        session.CurrentWave = evt.WaveIndex;
        session.WaveTimer = 0f;
        _world.SetComponent(sessionEntity, session);
    }

    private void OnEnemyDied(EnemyDiedEvent evt)
    {
        if (!TryGetSessionEntity(_world, out var sessionEntity) ||
            !_world.TryGetComponent(sessionEntity, out GameSession session))
        {
            return;
        }

        session.EnemiesKilled++;
        _world.SetComponent(sessionEntity, session);
    }

    private void OnPlayerDied(PlayerDiedEvent evt)
    {
        if (!TryGetSessionEntity(_world, out var sessionEntity) ||
            !_world.TryGetComponent(sessionEntity, out GameSession session))
        {
            return;
        }

        SetSessionState(_world, sessionEntity, ref session, GameState.GameOver);
    }

    private static void SetSessionState(EcsWorld world, Entity sessionEntity, ref GameSession session, GameState state)
    {
        session.State = state;
        world.SetComponent(sessionEntity, session);
    }

    private void RestartSession(EcsWorld world, ref GameSession session)
    {
        if (!TryGetSessionEntity(world, out var sessionEntity))
        {
            return;
        }

        _cleanupService.RemoveTransientStageEntities(world);
        _resetService.RestoreDefaults(world, sessionEntity, ref session);
        world.EventBus.Publish(new SessionRestartedEvent());
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

    private static bool IsLevelUpChoiceOpen(EcsWorld world)
    {
        var open = false;
        world.ForEach<LevelUpChoiceState>((Entity _, ref LevelUpChoiceState state) =>
        {
            if (state.IsOpen)
            {
                open = true;
            }
        });

        return open;
    }
}
