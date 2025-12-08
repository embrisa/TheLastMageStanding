using TheLastMageStanding.Game.Core.Ecs.Components;
using TheLastMageStanding.Game.Core.Events;

namespace TheLastMageStanding.Game.Core.Ecs.Systems;

internal sealed class InputSystem : IUpdateSystem
{
    private Entity? _sessionEntity;

    public void Initialize(EcsWorld world)
    {
    }

    public void Update(EcsWorld world, in EcsUpdateContext context)
    {
        // Cache session entity
        if (_sessionEntity is null || !world.IsAlive(_sessionEntity.Value))
        {
            _sessionEntity = null;
            world.ForEach<GameSession>((Entity entity, ref GameSession _) =>
            {
                _sessionEntity = entity;
            });
        }

        // Check for locked feature message from input state
        if (!string.IsNullOrEmpty(context.Input.LockedFeatureMessage) && _sessionEntity.HasValue)
        {
            world.SetComponent(_sessionEntity.Value, new LockedFeatureMessage(context.Input.LockedFeatureMessage));
        }

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

