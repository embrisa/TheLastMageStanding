using TheLastMageStanding.Game.Core.Campaign;
using TheLastMageStanding.Game.Core.Ecs.Components;
using TheLastMageStanding.Game.Core.Events;
using TheLastMageStanding.Game.Core.SceneState;

namespace TheLastMageStanding.Game.Core.Ecs.Systems;

/// <summary>
/// Keeps <see cref="StageRunState"/> in sync with the currently active stage.
/// </summary>
internal sealed class StageRunInitializationSystem : IUpdateSystem
{
    private readonly SceneStateService _sceneStateService;
    private readonly StageRegistry _stageRegistry;
    private Entity? _sessionEntity;
    private string? _lastStageId;

    public StageRunInitializationSystem(SceneStateService sceneStateService, StageRegistry stageRegistry)
    {
        _sceneStateService = sceneStateService;
        _stageRegistry = stageRegistry;
    }

    public void Initialize(EcsWorld world)
    {
    }

    public void Update(EcsWorld world, in EcsUpdateContext context)
    {
        if (!_sceneStateService.IsInStage())
        {
            return;
        }

        if (!TryGetSessionEntity(world, out var sessionEntity))
        {
            return;
        }

        var stageId = _sceneStateService.CurrentStageId ?? string.Empty;
        if (string.Equals(stageId, _lastStageId, StringComparison.Ordinal) &&
            world.TryGetComponent(sessionEntity, out StageRunState _))
        {
            return;
        }

        _lastStageId = stageId;

        var stage = _stageRegistry.GetStage(stageId);
        var state = new StageRunState
        {
            StageId = stage?.StageId ?? stageId,
            MaxWaves = stage?.MaxWaves ?? 10,
            IsBossStage = stage?.IsBossStage ?? false,
            BossWaveIndex = stage?.MaxWaves ?? 10,
            BossId = stage?.BossId,
            BiomeId = stage?.BiomeId ?? string.Empty,
            CompletionGold = stage?.Rewards.CompletionGold ?? 0,
            CompletionMetaXpBonus = stage?.Rewards.CompletionMetaXpBonus ?? 0,
            BossSpawned = false,
            BossEntity = Entity.None,
        };

        if (state.IsBossStage && !string.IsNullOrWhiteSpace(state.BossId))
        {
            var act = stage != null ? _stageRegistry.GetAct(stage.ActNumber) : null;
            state.BossArchetypeId = act?.Boss?.BossArchetypeId;
        }

        world.SetComponent(sessionEntity, state);
        world.EventBus.Publish(new StageRunStartedEvent(state.StageId));
    }

    private bool TryGetSessionEntity(EcsWorld world, out Entity sessionEntity)
    {
        if (_sessionEntity.HasValue && world.IsAlive(_sessionEntity.Value))
        {
            sessionEntity = _sessionEntity.Value;
            return true;
        }

        _sessionEntity = null;
        world.ForEach<GameSession>((Entity entity, ref GameSession _) =>
        {
            _sessionEntity = entity;
        });

        if (_sessionEntity.HasValue)
        {
            sessionEntity = _sessionEntity.Value;
            return true;
        }

        sessionEntity = default;
        return false;
    }
}

