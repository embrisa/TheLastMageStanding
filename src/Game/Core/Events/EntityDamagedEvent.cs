using Microsoft.Xna.Framework;
using TheLastMageStanding.Game.Core.Ecs;
using TheLastMageStanding.Game.Core.Ecs.Components;
using TheLastMageStanding.Game.Core.Combat;
using System.Collections.Generic;

namespace TheLastMageStanding.Game.Core.Events;

internal readonly struct EntityDamagedEvent
{
    public Entity Source { get; }
    public Entity Target { get; }
    public float Amount { get; }
    public DamageInfo DamageInfo { get; }
    public Vector2 SourcePosition { get; }
    public Faction SourceFaction { get; }
    public bool IsCritical { get; }
    public DamageType DamageType { get; }

    public EntityDamagedEvent(
        Entity source,
        Entity target,
        float amount,
        DamageInfo damageInfo,
        Vector2 sourcePosition,
        Faction sourceFaction,
        bool isCritical = false)
    {
        Source = source;
        Target = target;
        Amount = amount;
        DamageInfo = damageInfo;
        SourcePosition = sourcePosition;
        SourceFaction = sourceFaction;
        IsCritical = isCritical;
        DamageType = damageInfo.DamageType;
    }
}

internal readonly struct EnemyDiedEvent
{
    public Entity Enemy { get; }
    public Vector2 Position { get; }
    public IReadOnlyList<EliteModifierType>? Modifiers { get; }

    public EnemyDiedEvent(Entity enemy, Vector2 position, IReadOnlyList<EliteModifierType>? modifiers = null)
    {
        Enemy = enemy;
        Position = position;
        Modifiers = modifiers;
    }
}
