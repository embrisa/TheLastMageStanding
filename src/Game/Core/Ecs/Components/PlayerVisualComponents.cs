using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace TheLastMageStanding.Game.Core.Ecs.Components;

internal enum PlayerFacingDirection
{
    South = 0,
    SouthEast = 1,
    East = 2,
    NorthEast = 3,
    North = 4,
    NorthWest = 5,
    West = 6,
    SouthWest = 7,
}

internal enum PlayerAnimationClip
{
    Idle = 0,
    Run = 1,
    RunBackwards = 2,
    StrafeLeft = 3,
    StrafeRight = 4,
    Hit = 5,
}

internal readonly record struct SpriteAnimation(
    Texture2D Texture,
    int FrameWidth,
    int FrameHeight,
    int Columns,
    int Rows,
    float FrameDurationSeconds);

internal struct PlayerSpriteSet
{
    public SpriteAnimation Idle;
    public SpriteAnimation Run;
    public SpriteAnimation RunBackwards;
    public SpriteAnimation StrafeLeft;
    public SpriteAnimation StrafeRight;
    public SpriteAnimation Hit;
}

internal struct PlayerVisual
{
    public PlayerVisual(Vector2 origin, float scale, int frameSize)
    {
        Origin = origin;
        Scale = scale;
        FrameSize = frameSize;
    }

    public Vector2 Origin { get; set; }
    public float Scale { get; set; }
    public int FrameSize { get; set; }
}

internal struct PlayerAnimationState
{
    public PlayerFacingDirection Facing { get; set; }
    public PlayerAnimationClip ActiveClip { get; set; }
    public float Timer { get; set; }
    public int FrameIndex { get; set; }
    public bool IsMoving { get; set; }
}

internal struct PlayerHitState
{
    public PlayerHitState(float remainingSeconds, float totalSeconds)
    {
        RemainingSeconds = remainingSeconds;
        TotalSeconds = totalSeconds;
    }

    public float RemainingSeconds { get; set; }
    public float TotalSeconds { get; set; }
}


