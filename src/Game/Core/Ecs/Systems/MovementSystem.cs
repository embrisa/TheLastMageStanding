using TheLastMageStanding.Game.Core.Ecs.Components;

namespace TheLastMageStanding.Game.Core.Ecs.Systems;

internal sealed class MovementSystem : IUpdateSystem
{
    public void Initialize(EcsWorld world)
    {
    }

    public void Update(EcsWorld world, in EcsUpdateContext context)
    {
        var deltaSeconds = context.DeltaSeconds;
        world.ForEach<Position, Velocity>(
            (Entity entity, ref Position position, ref Velocity velocity) =>
            {
                position.Value += velocity.Value * deltaSeconds;
            });
    }
}

