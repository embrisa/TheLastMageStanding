using System;

namespace TheLastMageStanding.Game.Core.Config;

/// <summary>
/// Window/backbuffer and presentation settings. Defaults match current startup.
/// </summary>
internal sealed class VideoSettingsConfig
{
    public int Version { get; set; } = 1;

    // Uses borderless fullscreen (no hardware mode switch) for stability.
    public bool Fullscreen { get; set; }
    public bool VSync { get; set; } = true;

    public int BackBufferWidth { get; set; } = 960 * 2;  // Virtual * default scale
    public int BackBufferHeight { get; set; } = 540 * 2;

    public int WindowScale { get; set; } = 2; // Used for resolution presets (virtual * scale)

    public static VideoSettingsConfig Default => new();

    public VideoSettingsConfig Clone() => new()
    {
        Version = Version,
        Fullscreen = Fullscreen,
        VSync = VSync,
        BackBufferWidth = BackBufferWidth,
        BackBufferHeight = BackBufferHeight,
        WindowScale = WindowScale
    };

    public void Normalize()
    {
        BackBufferWidth = Math.Clamp(BackBufferWidth, 640, 3840);
        BackBufferHeight = Math.Clamp(BackBufferHeight, 360, 2160);
        WindowScale = Math.Clamp(WindowScale, 1, 4);
    }
}

