using TheLastMageStanding.Game.Core.Ecs.Components;

namespace TheLastMageStanding.Game.Core.Ecs.Systems;

internal sealed class MovementIntentSystem : IUpdateSystem
{
    public void Initialize(EcsWorld world)
    {
    }

    public void Update(EcsWorld world, in EcsUpdateContext context)
    {
        world.ForEach<InputIntent, MoveSpeed, Velocity>(
            (Entity entity, ref InputIntent intent, ref MoveSpeed moveSpeed, ref Velocity velocity) =>
            {
                if (world.TryGetComponent(entity, out DashState dashState) && dashState.IsActive)
                {
                    return;
                }

                velocity.Value = intent.Movement * moveSpeed.Value;
            });
    }
}

