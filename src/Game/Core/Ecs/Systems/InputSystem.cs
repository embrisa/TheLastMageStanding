using TheLastMageStanding.Game.Core.Ecs.Components;
using TheLastMageStanding.Game.Core.Events;

namespace TheLastMageStanding.Game.Core.Ecs.Systems;

internal sealed class InputSystem : IUpdateSystem
{
    public void Initialize(EcsWorld world)
    {
    }

    public void Update(EcsWorld world, in EcsUpdateContext context)
    {
        var input = context.Input;
        world.ForEach<PlayerTag, InputIntent>(
            (Entity entity, ref PlayerTag player, ref InputIntent intent) =>
            {
                intent.Movement = input.Movement;
                if (input.AttackPressed)
                {
                    world.EventBus.Publish(new PlayerAttackIntentEvent(entity));
                }
            });
    }
}

