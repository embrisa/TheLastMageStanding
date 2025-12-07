using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using TheLastMageStanding.Game.Core.Camera;
using TheLastMageStanding.Game.Core.Input;
using TheLastMageStanding.Game.Core.World;

namespace TheLastMageStanding.Game;

public class Game1 : Microsoft.Xna.Framework.Game
{
    private const int VirtualWidth = 960;
    private const int VirtualHeight = 540;
    private const int WindowScale = 2;

    private readonly GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch = null!;
    private RenderTarget2D _renderTarget = null!;
    private Camera2D _camera = null!;
    private InputState _input = null!;
    private GameWorld _world = null!;

    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;

        IsFixedTimeStep = true;
        TargetElapsedTime = TimeSpan.FromSeconds(1d / 60d);

        _graphics.PreferredBackBufferWidth = VirtualWidth * WindowScale;
        _graphics.PreferredBackBufferHeight = VirtualHeight * WindowScale;
    }

    protected override void Initialize()
    {
        _camera = new Camera2D(VirtualWidth, VirtualHeight);
        _input = new InputState();
        _world = new GameWorld(_camera);

        base.Initialize();
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);
        _renderTarget = new RenderTarget2D(GraphicsDevice, VirtualWidth, VirtualHeight);

        _world.LoadContent(GraphicsDevice);
    }

    protected override void Update(GameTime gameTime)
    {
        _input.Update();

        if (_input.QuitRequested)
        {
            Exit();
        }

        _world.Update(gameTime, _input);

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.SetRenderTarget(_renderTarget);
        GraphicsDevice.Clear(Color.CornflowerBlue);

        _spriteBatch.Begin(transformMatrix: _camera.Transform, samplerState: SamplerState.PointClamp);
        _world.Draw(_spriteBatch);
        _spriteBatch.End();

        GraphicsDevice.SetRenderTarget(null);
        GraphicsDevice.Clear(Color.Black);

        _spriteBatch.Begin(samplerState: SamplerState.PointClamp);
        _spriteBatch.Draw(
            _renderTarget,
            destinationRectangle: new Rectangle(0, 0, _graphics.PreferredBackBufferWidth, _graphics.PreferredBackBufferHeight),
            color: Color.White);
        _spriteBatch.End();

        base.Draw(gameTime);
    }
}
