using TheLastMageStanding.Game.Core.Ecs;
using TheLastMageStanding.Game.Core.Ecs.Components;

namespace TheLastMageStanding.Game.Core.Events;

internal readonly struct StatusEffectAppliedEvent
{
    public Entity Target { get; }
    public StatusEffectType Type { get; }
    public float Duration { get; }
    public int Stacks { get; }

    public StatusEffectAppliedEvent(Entity target, StatusEffectType type, float duration, int stacks)
    {
        Target = target;
        Type = type;
        Duration = duration;
        Stacks = stacks;
    }
}

internal readonly struct StatusEffectExpiredEvent
{
    public Entity Target { get; }
    public StatusEffectType Type { get; }

    public StatusEffectExpiredEvent(Entity target, StatusEffectType type)
    {
        Target = target;
        Type = type;
    }
}

internal readonly struct StatusEffectTickEvent
{
    public Entity Target { get; }
    public StatusEffectType Type { get; }
    public float Damage { get; }

    public StatusEffectTickEvent(Entity target, StatusEffectType type, float damage)
    {
        Target = target;
        Type = type;
        Damage = damage;
    }
}

