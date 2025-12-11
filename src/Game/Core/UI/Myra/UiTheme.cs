using Microsoft.Xna.Framework;

namespace TheLastMageStanding.Game.Core.UI.Myra;

/// <summary>
/// Shared colors, spacing, and sizing for Myra-based UI in TLMS.
/// Keeps menus visually consistent and easy to theme in one place.
/// </summary>
internal static class UiTheme
{
    public const int VirtualWidth = 960;
    public const int VirtualHeight = 540;

    public const int Spacing = 8;
    public const int LargeSpacing = 12;
    public const int Padding = 16;
    public const int LargePadding = 24;
public const int SettingsRowSpacing = 8;
public const int SettingsSectionSpacing = 12;
public const int SettingsSectionPadding = 20;
public const int SettingsLabelWidth = 220;
public const int SettingsControlWidth = 280;
public const int SettingsValueWidth = 120;
public const int SettingsRowHeight = 36;
public const int SettingsSliderHeight = 20;
public const int SettingsToggleSize = 28;
public const int SettingsKeybindWidth = 200;
public const int SettingsTabHeight = 42;
public const int SettingsTabSpacing = 8;

    public static readonly Color ScreenOverlay = new(10, 10, 16, 200);
    public static readonly Color PanelBackground = new(24, 26, 34, 240);
    public static readonly Color PanelBorder = new(70, 78, 96, 255);

    public static readonly Color CardBackground = new(32, 35, 45, 230);
    public static readonly Color CardBorder = new(88, 98, 118, 255);

    public static readonly Color PrimaryText = Color.White;
    public static readonly Color MutedText = new(180, 185, 195, 255);
    public static readonly Color AccentText = Color.Gold;
    public static readonly Color ErrorText = Color.OrangeRed;
    public static readonly Color SuccessText = Color.LightGreen;
    public static readonly Color InfoText = Color.Cyan;

    public static readonly Color ButtonBackground = new(38, 42, 55, 230);
    public static readonly Color ButtonHover = new(60, 68, 88, 240);
    public static readonly Color ButtonPressed = new(46, 52, 68, 255);
    public static readonly Color ButtonBorder = new(96, 106, 130, 255);
public static readonly Color InputBackground = new(38, 42, 55, 230);
public static readonly Color InputHover = new(58, 64, 80, 240);
public static readonly Color InputPressed = new(48, 54, 70, 255);
public static readonly Color InputBorder = new(96, 106, 130, 255);
public static readonly Color InputFocusBorder = new(0, 145, 190, 255);

    public static readonly Color AccentBackground = new(0, 115, 155, 255);
    public static readonly Color AccentHover = new(0, 145, 190, 255);
    public static readonly Color AccentPressed = new(0, 90, 130, 255);
public static readonly Color TabBackground = new(32, 36, 48, 230);
public static readonly Color TabHover = new(44, 50, 64, 240);
public static readonly Color TabActiveBackground = new(48, 60, 78, 240);
public static readonly Color TabBorder = new(88, 98, 118, 255);
public static readonly Color TabActiveBorder = new(0, 145, 190, 255);

    public static readonly Color DisabledBackground = new(50, 50, 50, 200);
    public static readonly Color DisabledText = new(140, 140, 140, 255);
}

