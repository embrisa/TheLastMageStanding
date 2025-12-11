using System;
using System.IO;
using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Myra.Graphics2D.UI;

namespace TheLastMageStanding.Game.Core.UI.Myra;

/// <summary>
/// Centralized Myra font loading and helpers so all menus share the same typography.
/// </summary>
internal static class UiFonts
{
    private static FontSystem? _bodySystem;
    private static FontSystem? _headingSystem;
    private static DynamicSpriteFont? _body;
    private static DynamicSpriteFont? _heading;

    /// <summary>
    /// Base scale factors tuned so the larger spritefont sizes render at UI-friendly sizes.
    /// </summary>
    public const float BodyBaseScale = 0.5f;    // 32px font → ~16px effective
    public const float HeadingBaseScale = 0.6f; // 40px font → ~24px effective

    public static SpriteFontBase Body => _body ?? throw new InvalidOperationException("UiFonts not loaded. Call UiFonts.Load first.");
    public static SpriteFontBase Heading => _heading ?? throw new InvalidOperationException("UiFonts not loaded. Call UiFonts.Load first.");

    public static void Load(ContentManager content)
    {
        _body ??= LoadFont(content, ref _bodySystem, "FontRegularTitle.otf", 32);
        _heading ??= LoadFont(content, ref _headingSystem, "FontStylizedTitle.otf", 40);
    }

    public static void ApplyBody(Label label, float scaleMultiplier = 1f)
    {
        label.Font = Body;
        var scale = BodyBaseScale * scaleMultiplier;
        label.Scale = new Vector2(scale, scale);
    }

    public static void ApplyHeading(Label label, float scaleMultiplier = 1f)
    {
        label.Font = Heading;
        var scale = HeadingBaseScale * scaleMultiplier;
        label.Scale = new Vector2(scale, scale);
    }

    private static DynamicSpriteFont LoadFont(
        ContentManager content,
        ref FontSystem? fontSystem,
        string fontFileName,
        int size)
    {
        fontSystem ??= new FontSystem(new FontSystemSettings
        {
            TextureWidth = 1024,
            TextureHeight = 1024
        });

        var fontPath = Path.Combine(content.RootDirectory, "Fonts", fontFileName);
        using var stream = TitleContainer.OpenStream(fontPath);
        using var memory = new MemoryStream();
        stream.CopyTo(memory);
        fontSystem.AddFont(memory.ToArray());

        return fontSystem.GetFont(size);
    }
}

