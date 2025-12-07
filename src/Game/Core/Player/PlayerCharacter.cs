using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TheLastMageStanding.Game.Core.Camera;
using TheLastMageStanding.Game.Core.Input;

namespace TheLastMageStanding.Game.Core.Player;

internal sealed class PlayerCharacter
{
    private readonly Camera2D _camera;
    private Texture2D? _debugDot;

    public Vector2 Position { get; private set; } = Vector2.Zero;
    public float MoveSpeed { get; set; } = 220f;

    public PlayerCharacter(Camera2D camera)
    {
        _camera = camera;
    }

    public void LoadContent(GraphicsDevice graphicsDevice)
    {
        // 1x1 white pixel for debug rendering and placeholder visuals.
        _debugDot ??= CreatePixel(graphicsDevice);
    }

    public void Update(GameTime gameTime, InputState input)
    {
        var delta = (float)gameTime.ElapsedGameTime.TotalSeconds;
        Position += input.Movement * MoveSpeed * delta;
        _camera.LookAt(Position);
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        if (_debugDot is null)
        {
            return;
        }

        // Simple placeholder: draw a small square centered on the player position.
        const float size = 8f;
        var origin = new Vector2(0.5f, 0.5f);
        spriteBatch.Draw(_debugDot, Position, null, Color.White, 0f, origin, size, SpriteEffects.None, 0f);
    }

    private static Texture2D CreatePixel(GraphicsDevice graphicsDevice)
    {
        var texture = new Texture2D(graphicsDevice, 1, 1);
        texture.SetData(new[] { Color.White });
        return texture;
    }
}

