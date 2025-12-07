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
        // Check session state - disable input if game over
        var sessionActive = false;
        world.ForEach<GameSession>((Entity _, ref GameSession session) =>
        {
            if (session.State == GameState.Playing)
            {
                sessionActive = true;
            }
        });
        if (!sessionActive)
        {
            return;
        }

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

