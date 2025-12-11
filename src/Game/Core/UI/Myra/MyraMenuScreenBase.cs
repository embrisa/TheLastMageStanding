using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Myra;
using Myra.Graphics2D.UI;

namespace TheLastMageStanding.Game.Core.UI.Myra;

/// <summary>
/// Base class for Myra screens that handles desktop creation, scaling to the virtual resolution,
/// and optional render-target backed rendering to avoid HiDPI mouse offset issues.
/// </summary>
internal abstract class MyraMenuScreenBase : IDisposable
{
    private readonly bool _useRenderTarget;
    private RenderTarget2D? _renderTarget;
    private SpriteBatch? _spriteBatch;
    private Vector2 _lastScale = Vector2.Zero;
    private Point _lastBounds;

    protected MyraMenuScreenBase(int virtualWidth = UiTheme.VirtualWidth, int virtualHeight = UiTheme.VirtualHeight, bool useRenderTarget = false)
    {
        VirtualWidth = virtualWidth;
        VirtualHeight = virtualHeight;
        _useRenderTarget = useRenderTarget;

        Desktop = new Desktop();
    }

    protected Desktop Desktop { get; }
    protected int VirtualWidth { get; }
    protected int VirtualHeight { get; }

    public virtual void Dispose()
    {
        _renderTarget?.Dispose();
        _spriteBatch?.Dispose();
        Desktop.Dispose();
    }

    public virtual void Update(GameTime gameTime)
    {
        UpdateScale();
    }

    public void Render()
    {
        if (!_useRenderTarget)
        {
            Desktop.Render();
            return;
        }

        if (MyraEnvironment.Game == null)
        {
            return;
        }

        var device = MyraEnvironment.Game.GraphicsDevice;
        var bounds = MyraEnvironment.Game.Window.ClientBounds;
        EnsureRenderResources(device, bounds.Width, bounds.Height);

        device.SetRenderTarget(_renderTarget);
        device.Clear(Color.Transparent);
        Desktop.Render();
        device.SetRenderTarget(null);

        if (_spriteBatch == null || _renderTarget == null)
        {
            return;
        }

        _spriteBatch.Begin(samplerState: SamplerState.LinearClamp);
        _spriteBatch.Draw(_renderTarget, new Rectangle(0, 0, device.Viewport.Width, device.Viewport.Height), Color.White);
        _spriteBatch.End();
    }

    protected void UpdateScale()
    {
        if (MyraEnvironment.Game == null)
        {
            return;
        }

        var bounds = MyraEnvironment.Game.Window.ClientBounds;
        Desktop.Scale = new Vector2(
            (float)bounds.Width / VirtualWidth,
            (float)bounds.Height / VirtualHeight);

        var scaleChanged = _lastScale != Desktop.Scale || _lastBounds.X != bounds.Width || _lastBounds.Y != bounds.Height;
        if (scaleChanged)
        {
            _lastScale = Desktop.Scale;
            _lastBounds = new Point(bounds.Width, bounds.Height);
        }
    }

    private void EnsureRenderResources(GraphicsDevice device, int width, int height)
    {
        if (_renderTarget == null || _renderTarget.Width != width || _renderTarget.Height != height)
        {
            _renderTarget?.Dispose();
            _renderTarget = new RenderTarget2D(device, width, height);
        }

        _spriteBatch ??= new SpriteBatch(device);
    }
}

