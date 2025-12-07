using TheLastMageStanding.Game.Core.Ecs;

namespace TheLastMageStanding.Game.Core.Events;

internal readonly struct PlayerAttackIntentEvent
{
    public Entity Player { get; }

    public PlayerAttackIntentEvent(Entity player)
    {
        Player = player;
    }
}
