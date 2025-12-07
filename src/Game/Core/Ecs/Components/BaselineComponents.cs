using Microsoft.Xna.Framework;

namespace TheLastMageStanding.Game.Core.Ecs.Components;

internal enum Faction
{
    Neutral = 0,
    Player = 1,
    Enemy = 2,
}

internal struct Position
{
    public Position(Vector2 value) => Value = value;
    public Vector2 Value { get; set; }
}

internal struct Velocity
{
    public Velocity(Vector2 value) => Value = value;
    public Vector2 Value { get; set; }
}

internal struct Health
{
    public Health(float current, float max)
    {
        Current = current;
        Max = max;
    }

    public float Current { get; set; }
    public float Max { get; set; }
    public bool IsDead => Current <= 0f;
    public float Ratio => Max <= 0f ? 0f : Current / Max;
}

internal struct Hitbox
{
    public Hitbox(float radius) => Radius = radius;
    public float Radius { get; set; }
}

internal struct AttackStats
{
    public AttackStats(float damage, float cooldownSeconds, float range)
    {
        Damage = damage;
        CooldownSeconds = cooldownSeconds;
        Range = range;
        CooldownTimer = 0f;
    }

    public float Damage { get; set; }
    public float CooldownSeconds { get; set; }
    public float CooldownTimer { get; set; }
    public float Range { get; set; }
}

internal struct RenderDebug
{
    public RenderDebug(Color fill, float size, bool showHealthBar = true)
    {
        Fill = fill;
        Size = size;
        ShowHealthBar = showHealthBar;
    }

    public Color Fill { get; set; }
    public float Size { get; set; }
    public bool ShowHealthBar { get; set; }
}

internal struct Lifetime
{
    public Lifetime(float remainingSeconds) => RemainingSeconds = remainingSeconds;
    public float RemainingSeconds { get; set; }
}

internal struct DebugMover
{
    public DebugMover(float speed, float turnRate, float phase = 0f)
    {
        Speed = speed;
        TurnRate = turnRate;
        Phase = phase;
    }

    public float Speed { get; set; }
    public float TurnRate { get; set; }
    public float Phase { get; set; }
}

