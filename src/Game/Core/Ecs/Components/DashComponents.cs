using Microsoft.Xna.Framework;

namespace TheLastMageStanding.Game.Core.Ecs.Components;

internal struct DashConfig
{
    public const float DefaultDistance = 150f;
    public const float DefaultDuration = 0.2f;
    public const float DefaultCooldown = 2.0f;
    public const float DefaultIFrameWindow = 0.15f;
    public const float DefaultInputBufferWindow = 0.05f;

    public float Distance { get; set; }
    public float Duration { get; set; }
    public float Cooldown { get; set; }
    public float IFrameWindow { get; set; }
    public float InputBufferWindow { get; set; }
}

internal struct DashState
{
    public bool IsActive { get; set; }
    public float Elapsed { get; set; }
    public Vector2 Direction { get; set; }
    public bool IFrameActive { get; set; }
}

internal struct DashCooldown
{
    public DashCooldown(float remainingSeconds)
    {
        RemainingSeconds = remainingSeconds;
    }

    public float RemainingSeconds { get; set; }
    public readonly bool IsReady => RemainingSeconds <= 0f;
}

internal struct DashInputBuffer
{
    public bool HasBufferedInput { get; set; }
    public float TimeRemaining { get; set; }

    public void Buffer(float windowSeconds)
    {
        HasBufferedInput = true;
        TimeRemaining = windowSeconds;
    }
}

/// <summary>
/// Marker used for invulnerability windows (dash i-frames, temporary shields, etc.).
/// </summary>
internal struct Invulnerable
{
}



