using TheLastMageStanding.Game.Core.Ecs.Components;

namespace TheLastMageStanding.Game.Core.Ecs.Systems;

internal sealed class IntentResetSystem : IUpdateSystem
{
    public void Initialize(EcsWorld world)
    {
    }

    public void Update(EcsWorld world, in EcsUpdateContext context)
    {
        world.ForEach<InputIntent>(
            (Entity _, ref InputIntent intent) =>
            {
                intent.Reset();
            });
    }
}

