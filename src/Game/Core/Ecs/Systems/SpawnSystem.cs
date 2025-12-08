using TheLastMageStanding.Game.Core.Ecs.Components;

namespace TheLastMageStanding.Game.Core.Ecs.Systems;

internal sealed class SpawnSystem : IUpdateSystem
{
    private readonly EnemyEntityFactory _enemyFactory;

    public SpawnSystem(EnemyEntityFactory enemyFactory)
    {
        _enemyFactory = enemyFactory;
    }

    public void Initialize(EcsWorld world)
    {
    }

    public void Update(EcsWorld world, in EcsUpdateContext context)
    {
        world.ForEach<EnemySpawnRequest>(
            (Entity entity, ref EnemySpawnRequest request) =>
            {
                _enemyFactory.CreateEnemy(request.Position, request.Archetype, request.Modifiers);
                world.DestroyEntity(entity);
            });
    }
}

