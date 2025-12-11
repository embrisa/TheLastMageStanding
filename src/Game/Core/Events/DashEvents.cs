using Microsoft.Xna.Framework;
using TheLastMageStanding.Game.Core.Ecs;

namespace TheLastMageStanding.Game.Core.Events;

internal readonly struct DashRequestEvent
{
    public Entity Actor { get; }
    public Vector2 Direction { get; }

    public DashRequestEvent(Entity actor, Vector2 direction)
    {
        Actor = actor;
        Direction = direction;
    }
}






