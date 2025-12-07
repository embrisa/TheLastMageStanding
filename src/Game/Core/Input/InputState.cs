using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace TheLastMageStanding.Game.Core.Input;

internal sealed class InputState
{
    public Vector2 Movement { get; private set; }
    public bool QuitRequested { get; private set; }

    public void Update()
    {
        var keyboard = Keyboard.GetState();
        var movement = Vector2.Zero;

        if (keyboard.IsKeyDown(Keys.W) || keyboard.IsKeyDown(Keys.Up))
        {
            movement.Y -= 1f;
        }

        if (keyboard.IsKeyDown(Keys.S) || keyboard.IsKeyDown(Keys.Down))
        {
            movement.Y += 1f;
        }

        if (keyboard.IsKeyDown(Keys.A) || keyboard.IsKeyDown(Keys.Left))
        {
            movement.X -= 1f;
        }

        if (keyboard.IsKeyDown(Keys.D) || keyboard.IsKeyDown(Keys.Right))
        {
            movement.X += 1f;
        }

        Movement = movement == Vector2.Zero ? Vector2.Zero : Vector2.Normalize(movement);
        QuitRequested = keyboard.IsKeyDown(Keys.Escape);
    }
}

