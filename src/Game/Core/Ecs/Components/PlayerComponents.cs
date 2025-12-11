using Microsoft.Xna.Framework;

namespace TheLastMageStanding.Game.Core.Ecs.Components;

internal struct PlayerTag
{
}

internal struct CameraTarget
{
}

internal struct MoveSpeed
{
    public MoveSpeed(float value) => Value = value;
    public float Value { get; set; }
}

internal struct InputIntent
{
    public Vector2 Movement { get; set; }

    public void Reset()
    {
        Movement = Vector2.Zero;
    }
}






