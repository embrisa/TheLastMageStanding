using Microsoft.Xna.Framework;
using TheLastMageStanding.Game.Core.Ecs;
using TheLastMageStanding.Game.Core.Ecs.Components;
using TheLastMageStanding.Game.Core.Combat;

namespace TheLastMageStanding.Game.Core.Events;

internal readonly struct EntityDamagedEvent
{
    public Entity Target { get; }
    public float Amount { get; }
    public Vector2 SourcePosition { get; }
    public Faction SourceFaction { get; }
    public bool IsCritical { get; }
    public DamageType DamageType { get; }

    public EntityDamagedEvent(Entity target, float amount, Vector2 sourcePosition, Faction sourceFaction, bool isCritical = false, DamageType damageType = DamageType.Physical)
    {
        Target = target;
        Amount = amount;
        SourcePosition = sourcePosition;
        SourceFaction = sourceFaction;
        IsCritical = isCritical;
        DamageType = damageType;
    }
}

internal readonly struct EnemyDiedEvent
{
    public Entity Enemy { get; }
    public EnemyDiedEvent(Entity enemy) => Enemy = enemy;
}
