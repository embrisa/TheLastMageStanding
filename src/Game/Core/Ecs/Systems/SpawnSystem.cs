using TheLastMageStanding.Game.Core.Ecs.Components;
using TheLastMageStanding.Game.Core.Ecs.Config;

namespace TheLastMageStanding.Game.Core.Ecs.Systems;

internal sealed class SpawnSystem : IUpdateSystem
{
    private readonly EnemyEntityFactory _enemyFactory;
    private Entity? _sessionEntity;

    public SpawnSystem(EnemyEntityFactory enemyFactory)
    {
        _enemyFactory = enemyFactory;
    }

    public void Initialize(EcsWorld world)
    {
    }

    public void Update(EcsWorld world, in EcsUpdateContext context)
    {
        var hasStageRun = TryGetStageRunState(world, out var sessionEntity, out var stageState);

        world.ForEach<EnemySpawnRequest>(
            (Entity entity, ref EnemySpawnRequest request) =>
            {
                var spawned = _enemyFactory.CreateEnemy(request.Position, request.Archetype, request.Modifiers);

                if (hasStageRun &&
                    stageState.IsBossStage &&
                    request.Archetype.Tier == EnemyTier.Boss &&
                    !string.IsNullOrWhiteSpace(stageState.BossId) &&
                    string.Equals(stageState.BossArchetypeId, request.Archetype.Id, StringComparison.Ordinal))
                {
                    world.SetComponent(spawned, new BossEncounter(stageState.BossId!));
                    stageState.BossSpawned = true;
                    stageState.BossEntity = spawned;
                    world.SetComponent(sessionEntity, stageState);
                }

                world.DestroyEntity(entity);
            });
    }

    private bool TryGetStageRunState(EcsWorld world, out Entity sessionEntity, out StageRunState stageState)
    {
        if (_sessionEntity.HasValue && world.IsAlive(_sessionEntity.Value) && world.TryGetComponent(_sessionEntity.Value, out stageState))
        {
            sessionEntity = _sessionEntity.Value;
            return true;
        }

        _sessionEntity = null;
        var found = false;
        var capturedEntity = default(Entity);
        var capturedState = new StageRunState();
        world.ForEach<GameSession, StageRunState>((Entity entity, ref GameSession _, ref StageRunState state) =>
        {
            capturedEntity = entity;
            capturedState = state;
            _sessionEntity = entity;
            found = true;
        });

        sessionEntity = capturedEntity;
        stageState = capturedState;
        return found;
    }
}
