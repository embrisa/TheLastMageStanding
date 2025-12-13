using TheLastMageStanding.Game.Core.Ecs.Components;

namespace TheLastMageStanding.Game.Core.Ecs.Systems;

internal sealed class MovementSystem : IUpdateSystem
{
    private Entity? _sessionEntity;

    public void Initialize(EcsWorld world)
    {
    }

    public void Update(EcsWorld world, in EcsUpdateContext context)
    {
        if (!IsPlaying(world))
        {
            return;
        }

        var deltaSeconds = context.DeltaSeconds;
        world.ForEach<Position, Velocity>(
            (Entity entity, ref Position position, ref Velocity velocity) =>
            {
                position.Value += velocity.Value * deltaSeconds;
            });
    }

    private bool IsPlaying(EcsWorld world)
    {
        if (_sessionEntity is null || !world.IsAlive(_sessionEntity.Value))
        {
            _sessionEntity = null;
            world.ForEach<GameSession>((Entity entity, ref GameSession _) =>
            {
                _sessionEntity = entity;
            });
        }

        if (!_sessionEntity.HasValue)
        {
            return true;
        }

        return world.TryGetComponent(_sessionEntity.Value, out GameSession session) &&
               session.State == GameState.Playing;
    }
}
