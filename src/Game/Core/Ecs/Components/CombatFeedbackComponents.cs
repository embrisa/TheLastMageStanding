using Microsoft.Xna.Framework;

namespace TheLastMageStanding.Game.Core.Ecs.Components;



internal struct HitFlash
{
    public HitFlash(float remainingSeconds)
    {
        RemainingSeconds = remainingSeconds;
    }

    public float RemainingSeconds { get; set; }
}

internal struct HitSlow
{
    public HitSlow(float multiplier, float remainingSeconds)
    {
        Multiplier = multiplier;
        RemainingSeconds = remainingSeconds;
    }

    public float Multiplier { get; set; }
    public float RemainingSeconds { get; set; }
}

internal struct HitKnockback
{
    public HitKnockback(Vector2 velocity, float remainingSeconds)
    {
        Velocity = velocity;
        RemainingSeconds = remainingSeconds;
    }

    public Vector2 Velocity { get; set; }
    public float RemainingSeconds { get; set; }
}

internal struct DamageNumber
{
    public DamageNumber(float amount, float lifetimeSeconds, float floatSpeed, float horizontalJitter, float scale, Color color)
    {
        Amount = amount;
        LifetimeSeconds = lifetimeSeconds;
        FloatSpeed = floatSpeed;
        HorizontalJitter = horizontalJitter;
        Scale = scale;
        Color = color;
    }

    public float Amount { get; set; }
    public float LifetimeSeconds { get; set; }
    public float FloatSpeed { get; set; }
    public float HorizontalJitter { get; set; }
    public float Scale { get; set; }
    public Color Color { get; set; }
}



