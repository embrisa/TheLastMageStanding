using System.Collections.Generic;

namespace TheLastMageStanding.Game.Core.Ecs.Components;

internal enum BuffType
{
    None = 0,
    MoveSpeedBuff = 1,
    DamageBuff = 2,
    AttackSpeedBuff = 3
}

/// <summary>
/// Represents a single timed buff instance applied to an entity.
/// </summary>
internal struct TimedBuff
{
    public BuffType Type { get; set; }
    public float Duration { get; set; }
    public float RemainingDuration { get; set; }
    public StatModifiers Modifiers { get; set; }
    public Entity Source { get; set; }

    public bool IsExpired => RemainingDuration <= 0f;
}

/// <summary>
/// Collection of active timed buffs on an entity.
/// </summary>
internal struct ActiveBuffs
{
    public List<TimedBuff> Buffs { get; set; }
}



