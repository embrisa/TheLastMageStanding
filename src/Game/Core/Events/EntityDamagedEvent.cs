using Microsoft.Xna.Framework;
using TheLastMageStanding.Game.Core.Ecs;
using TheLastMageStanding.Game.Core.Ecs.Components;

namespace TheLastMageStanding.Game.Core.Events;

internal readonly struct EntityDamagedEvent
{
    public Entity Target { get; }
    public float Amount { get; }
    public Vector2 SourcePosition { get; }
    public Faction SourceFaction { get; }

    public EntityDamagedEvent(Entity target, float amount, Vector2 sourcePosition, Faction sourceFaction)
    {
        Target = target;
        Amount = amount;
        SourcePosition = sourcePosition;
        SourceFaction = sourceFaction;
    }
}

internal readonly struct EnemyDiedEvent
{
    public Entity Enemy { get; }
    public EnemyDiedEvent(Entity enemy) => Enemy = enemy;
}
