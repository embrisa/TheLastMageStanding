using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TheLastMageStanding.Game.Core.Input;
using TheLastMageStanding.Game.Core.Player;
using TheLastMageStanding.Game.Core.Camera;

namespace TheLastMageStanding.Game.Core.World;

internal sealed class GameWorld
{
    private readonly PlayerCharacter _player;

    public GameWorld(Camera2D camera)
    {
        _player = new PlayerCharacter(camera);
    }

    public void LoadContent(GraphicsDevice graphicsDevice)
    {
        _player.LoadContent(graphicsDevice);
    }

    public void Update(GameTime gameTime, InputState input)
    {
        _player.Update(gameTime, input);
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        _player.Draw(spriteBatch);
    }
}

