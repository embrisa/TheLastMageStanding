using TheLastMageStanding.Game.Core.Ecs;

namespace TheLastMageStanding.Game.Core.Events;

internal readonly struct PlayerDiedEvent
{
    public Entity Player { get; }

    public PlayerDiedEvent(Entity player)
    {
        Player = player;
    }
}
