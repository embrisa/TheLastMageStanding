using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace TheLastMageStanding.Game.Core.Config;

/// <summary>
/// Applies normalized video settings to the active MonoGame window and graphics device.
/// </summary>
internal sealed class VideoSettingsApplier
{
    private readonly GraphicsDeviceManager _graphics;
    private readonly GameWindow _window;

    public VideoSettingsApplier(GraphicsDeviceManager graphics, GameWindow window)
    {
        _graphics = graphics;
        _window = window;
    }

    public void Apply(VideoSettingsConfig settings, bool applyChanges)
    {
        settings.Normalize();
        _graphics.HardwareModeSwitch = false;
        _graphics.SynchronizeWithVerticalRetrace = settings.VSync;

        if (settings.Fullscreen)
        {
            var (displayWidth, displayHeight) = GetDisplayDimensions();
            _window.IsBorderless = true;
            _graphics.IsFullScreen = true;
            _graphics.PreferredBackBufferWidth = displayWidth;
            _graphics.PreferredBackBufferHeight = displayHeight;
        }
        else
        {
            _window.IsBorderless = false;
            _graphics.IsFullScreen = false;
            _graphics.PreferredBackBufferWidth = settings.BackBufferWidth;
            _graphics.PreferredBackBufferHeight = settings.BackBufferHeight;
        }

        if (applyChanges)
        {
            _graphics.ApplyChanges();
        }
    }

    private static (int Width, int Height) GetDisplayDimensions()
    {
        var mode = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode;
        return (mode.Width, mode.Height);
    }
}
