using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TheLastMageStanding.Game.Core.Composition;
using TheLastMageStanding.Game.Core.SceneState;
using TheLastMageStanding.Game.Core.UI;
using TheLastMageStanding.Game.Core.UI.Myra;

namespace TheLastMageStanding.Game;

public class Game1 : Microsoft.Xna.Framework.Game
{
    private const int VirtualWidth = 960;
    private const int VirtualHeight = 540;
    private const int WindowScale = 2;

    private readonly GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch = null!;
    private RenderTarget2D _renderTarget = null!;
    private GameRuntime _runtime = null!;

    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
        _graphics.HardwareModeSwitch = false; // Prefer borderless fullscreen to avoid macOS mode switches

        IsFixedTimeStep = true;
        TargetElapsedTime = TimeSpan.FromSeconds(1d / 60d);

        _graphics.PreferredBackBufferWidth = VirtualWidth * WindowScale;
        _graphics.PreferredBackBufferHeight = VirtualHeight * WindowScale;
    }

    protected override void Initialize()
    {
        _runtime = GameRuntimeFactory.Create(this, _graphics, VirtualWidth, VirtualHeight);

        base.Initialize();
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);
        _renderTarget = new RenderTarget2D(GraphicsDevice, VirtualWidth, VirtualHeight);

        _runtime.LoadContent(this, Content);
    }

    protected override void Update(GameTime gameTime)
    {
        // Use Window.ClientBounds for mouse scaling because Mouse.GetState() returns window coordinates (points),
        // whereas GraphicsDevice.Viewport returns backbuffer coordinates (pixels).
        // On HiDPI (Retina), these differ.
        var clientBounds = Window.ClientBounds;
        _runtime.Input.Update(clientBounds.Width, clientBounds.Height);

        // Process pending scene transitions
        _runtime.SceneRuntimeService.ProcessPendingSceneTransition(Content, GraphicsDevice);

        if (_runtime.SceneStateService.IsInMainMenu())
        {
            if (_runtime.SettingsController.IsOpen)
            {
                _runtime.SettingsController.Update(gameTime, _runtime.Input);
            }
            else
            {
                var menuResult = _runtime.MainMenu.Update(gameTime, _runtime.Input);
                HandleMainMenuResult(menuResult);
            }

            base.Update(gameTime);
            return;
        }

        _runtime.SceneRuntimeService.EnsureSceneReady(Content, GraphicsDevice);
        _runtime.SceneRuntimeService.Update(gameTime, _runtime.Input);

        if (_runtime.SceneRuntimeService.ExitRequested)
        {
            Exit();
        }

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.SetRenderTarget(_renderTarget);
        GraphicsDevice.Clear(ClearOptions.Target, Color.CornflowerBlue, 1f, 0);

        if (!_runtime.SceneStateService.IsInMainMenu())
        {
            _runtime.SceneRuntimeService.MapService?.Draw(_runtime.Camera.Transform);

            _spriteBatch.Begin(transformMatrix: _runtime.Camera.Transform, samplerState: SamplerState.PointClamp);
            _runtime.SceneRuntimeService.WorldRunner?.Draw(_spriteBatch);
            _spriteBatch.End();

            // Draw UI to render target (screen space relative to virtual resolution)
            _spriteBatch.Begin(samplerState: SamplerState.PointClamp);
            _runtime.SceneRuntimeService.WorldRunner?.DrawUI(_spriteBatch);
            _spriteBatch.End();
        }

        GraphicsDevice.SetRenderTarget(null);
        GraphicsDevice.Clear(Color.Black);

        _spriteBatch.Begin(samplerState: SamplerState.PointClamp);
        _spriteBatch.Draw(
            _renderTarget,
            destinationRectangle: new Rectangle(0, 0, GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height),
            color: Color.White);
        _spriteBatch.End();

        if (!_runtime.SceneStateService.IsInMainMenu())
        {
            _spriteBatch.Begin(samplerState: SamplerState.PointClamp);
            _runtime.SceneRuntimeService.WorldRunner?.DrawScreenSpaceUI(_spriteBatch);
            _spriteBatch.End();
        }

        if (_runtime.SceneStateService.IsInMainMenu())
        {
            _runtime.MainMenu.Draw();
            _runtime.SettingsController.Draw();
        }

        base.Draw(gameTime);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _runtime?.Dispose();
        }

        base.Dispose(disposing);
    }

    private void HandleMainMenuResult(MainMenuResult result)
    {
        switch (result.Action)
        {
            case MainMenuAction.StartSlot when !string.IsNullOrEmpty(result.SlotId):
                _runtime.SceneRuntimeService.ActivateSlot(result.SlotId);
                _runtime.SettingsController.Close();
                _runtime.SceneManager.TransitionToHub();
                break;
            case MainMenuAction.CreateNewSlot:
                _runtime.SceneRuntimeService.CreateNextSlot();
                _runtime.SettingsController.Close();
                _runtime.SceneManager.TransitionToHub();
                break;
            case MainMenuAction.Settings:
                _runtime.SettingsController.Open("audio");
                break;
            case MainMenuAction.Quit:
                _runtime.SettingsController.Close();
                Exit();
                break;
        }
    }
}
