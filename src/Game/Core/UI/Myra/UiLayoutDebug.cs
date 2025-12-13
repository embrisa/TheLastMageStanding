using Microsoft.Xna.Framework;

namespace TheLastMageStanding.Game.Core.UI.Myra;

/// <summary>
/// Dev-only toggle for rendering Myra widget bounds.
/// Enabled via F3 in DEBUG builds.
/// </summary>
internal static class UiLayoutDebug
{
    public static bool Enabled { get; private set; }

    public static Color BoundsColor { get; set; } = new(255, 0, 255, 180);

    public static void Toggle() => Enabled = !Enabled;

    public static void SetEnabled(bool enabled) => Enabled = enabled;
}

