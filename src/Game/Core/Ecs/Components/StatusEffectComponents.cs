using System;
using System.Collections.Generic;

namespace TheLastMageStanding.Game.Core.Ecs.Components;

public enum StatusEffectType
{
    None = 0,
    Burn = 1,
    Freeze = 2,
    Slow = 3,
    Shock = 4,
    Poison = 5
}

/// <summary>
/// Immutable blueprint describing how a status effect should behave when applied.
/// Potency meaning depends on the effect (e.g., damage/sec for DoTs, slow amount for debuffs).
/// </summary>
public readonly struct StatusEffectData
{
    public StatusEffectType Type { get; init; }
    public float Potency { get; init; }
    public float Duration { get; init; }
    public float TickInterval { get; init; }
    public int MaxStacks { get; init; }
    public int InitialStacks { get; init; }

    public StatusEffectData(
        StatusEffectType type,
        float potency,
        float duration,
        float tickInterval = 0f,
        int maxStacks = 1,
        int initialStacks = 1)
    {
        Type = type;
        Potency = potency;
        Duration = duration;
        TickInterval = tickInterval;
        MaxStacks = Math.Max(1, maxStacks);
        InitialStacks = Math.Max(1, initialStacks);
    }
}

/// <summary>
/// Runtime instance of an applied status effect.
/// </summary>
internal struct ActiveStatusEffect
{
    public StatusEffectData Data { get; set; }
    public float RemainingDuration { get; set; }
    public float AccumulatedTickTime { get; set; }
    public int CurrentStacks { get; set; }
}

/// <summary>
/// Container component holding all active status effects on an entity.
/// </summary>
internal struct ActiveStatusEffects
{
    public List<ActiveStatusEffect> Effects { get; set; }
}

/// <summary>
/// Per-effect resistance multipliers (0 = none, 1 = immune) applied in addition to stat-based resist.
/// </summary>
internal struct StatusEffectResistances
{
    public float Burn { get; set; }
    public float Freeze { get; set; }
    public float Slow { get; set; }
    public float Shock { get; set; }
    public float Poison { get; set; }

    public float Get(StatusEffectType type) => type switch
    {
        StatusEffectType.Burn => Burn,
        StatusEffectType.Freeze => Freeze,
        StatusEffectType.Slow => Slow,
        StatusEffectType.Shock => Shock,
        StatusEffectType.Poison => Poison,
        _ => 0f
    };
}

[Flags]
internal enum StatusEffectImmunity
{
    None = 0,
    Burn = 1 << 0,
    Freeze = 1 << 1,
    Slow = 1 << 2,
    Shock = 1 << 3,
    Poison = 1 << 4
}

internal struct StatusEffectImmunities
{
    public StatusEffectImmunity Flags { get; set; }

    public bool IsImmune(StatusEffectType type)
    {
        var flag = type switch
        {
            StatusEffectType.Burn => StatusEffectImmunity.Burn,
            StatusEffectType.Freeze => StatusEffectImmunity.Freeze,
            StatusEffectType.Slow => StatusEffectImmunity.Slow,
            StatusEffectType.Shock => StatusEffectImmunity.Shock,
            StatusEffectType.Poison => StatusEffectImmunity.Poison,
            _ => StatusEffectImmunity.None
        };

        return (Flags & flag) != 0;
    }
}

