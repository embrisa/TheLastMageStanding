using Microsoft.Xna.Framework;
using Myra.Graphics2D;
using Myra.Graphics2D.Brushes;
using Myra.Graphics2D.UI;

namespace TheLastMageStanding.Game.Core.UI.Myra;

/// <summary>
/// Common Myra widget styling helpers for TLMS menus.
/// </summary>
internal static class UiStyles
{
    public static Label Heading(string text, float scale = 1.0f, Color? color = null)
    {
        var label = new Label
        {
            Text = text,
            TextColor = color ?? UiTheme.AccentText,
            HorizontalAlignment = HorizontalAlignment.Center,
            Wrap = true
        };
        UiFonts.ApplyHeading(label, scale);
        return label;
    }

    public static Label BodyText(string text, Color? color = null, bool wrap = true, float scale = 1f)
    {
        var label = new Label
        {
            Text = text,
            TextColor = color ?? UiTheme.PrimaryText,
            Wrap = wrap
        };
        UiFonts.ApplyBody(label, scale);
        return label;
    }

    public static Label SectionTitle(string text, float scale = 1.1f)
    {
        var label = new Label
        {
            Text = text,
            TextColor = UiTheme.PrimaryText,
            Wrap = true,
            HorizontalAlignment = HorizontalAlignment.Left
        };
        UiFonts.ApplyHeading(label, scale);
        return label;
    }

    public static Label SectionDescription(string text, float scale = 0.95f)
    {
        var label = new Label
        {
            Text = text,
            TextColor = UiTheme.MutedText,
            Wrap = true,
            HorizontalAlignment = HorizontalAlignment.Left
        };
        UiFonts.ApplyBody(label, scale);
        return label;
    }

    public static Label SettingsLabel(string text)
    {
        var label = new Label
        {
            Text = text,
            TextColor = UiTheme.PrimaryText,
            Wrap = false,
            HorizontalAlignment = HorizontalAlignment.Left
        };
        UiFonts.ApplyBody(label);
        return label;
    }

    public static Label SettingsValue(string text, Color? color = null)
    {
        var label = new Label
        {
            Text = text,
            TextColor = color ?? UiTheme.AccentText,
            Wrap = false,
            HorizontalAlignment = HorizontalAlignment.Right
        };
        UiFonts.ApplyBody(label, 0.95f);
        return label;
    }

    public static Label SettingsHint(string text)
    {
        var label = new Label
        {
            Text = text,
            TextColor = UiTheme.MutedText,
            Wrap = true,
            HorizontalAlignment = HorizontalAlignment.Right
        };
        UiFonts.ApplyBody(label, 0.9f);
        return label;
    }

    public static Panel SettingsCard(int padding = UiTheme.SettingsSectionPadding) => Card(padding);

    public static Panel Card(int padding = UiTheme.Padding)
    {
        return new Panel
        {
            Background = new SolidBrush(UiTheme.CardBackground),
            Border = new SolidBrush(UiTheme.CardBorder),
            BorderThickness = new Thickness(1),
            Padding = new Thickness(padding)
        };
    }

    public static Panel ScreenOverlay()
    {
        return new Panel
        {
            Background = new SolidBrush(UiTheme.ScreenOverlay)
        };
    }

    public static Button MenuButton(string text, bool accent = false, int width = 420, int height = 52)
    {
        var label = new Label
        {
            Text = text,
            TextColor = UiTheme.PrimaryText,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        UiFonts.ApplyBody(label);

        var content = new Grid
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch
        };
        content.Widgets.Add(label);

        var button = new Button
        {
            Content = content,
            Width = width,
            Height = height,
            HorizontalAlignment = HorizontalAlignment.Center
        };

        StyleMenuButton(button, accent);
        return button;
    }

    public static void StyleMenuButton(Button button, bool accent = false)
    {
        var background = accent ? UiTheme.AccentBackground : UiTheme.ButtonBackground;
        var hover = accent ? UiTheme.AccentHover : UiTheme.ButtonHover;
        var pressed = accent ? UiTheme.AccentPressed : UiTheme.ButtonPressed;

        button.Background = new SolidBrush(background);
        button.OverBackground = new SolidBrush(hover);
        button.DisabledBackground = new SolidBrush(UiTheme.DisabledBackground);
        button.Border = new SolidBrush(UiTheme.ButtonBorder);
        button.BorderThickness = new Thickness(1);
        button.PressedBackground = new SolidBrush(pressed);
    }

    public static Grid ColumnLayout(int spacing = UiTheme.Spacing)
    {
        return new Grid
        {
            RowSpacing = spacing,
            ColumnSpacing = spacing,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch
        };
    }

    public static void HighlightSelection(Widget widget, bool isSelected)
    {
        if (widget is Button button)
        {
            if (isSelected)
            {
                button.Border = new SolidBrush(UiTheme.AccentText);
                button.BorderThickness = new Thickness(2);
            }
            else
            {
                button.Border = new SolidBrush(UiTheme.ButtonBorder);
                button.BorderThickness = new Thickness(1);
            }
        }
        else if (widget is Panel panel)
        {
            panel.Border = new SolidBrush(isSelected ? UiTheme.AccentText : UiTheme.CardBorder);
            panel.BorderThickness = new Thickness(isSelected ? 2 : 1);
        }
    }

    public static Button SettingsTabButton(string text, bool isActive)
    {
        var label = new Label
        {
            Text = text,
            TextColor = UiTheme.PrimaryText,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Wrap = false
        };
        UiFonts.ApplyBody(label);

        var button = new Button
        {
            Content = label,
            Height = UiTheme.SettingsTabHeight,
            Padding = new Thickness(UiTheme.Padding, UiTheme.Padding / 2),
            HorizontalAlignment = HorizontalAlignment.Left
        };

        StyleSettingsTabButton(button, isActive);
        return button;
    }

    public static void StyleSettingsTabButton(Button button, bool isActive)
    {
        button.Background = new SolidBrush(isActive ? UiTheme.TabActiveBackground : UiTheme.TabBackground);
        button.OverBackground = new SolidBrush(UiTheme.TabHover);
        button.Border = new SolidBrush(isActive ? UiTheme.TabActiveBorder : UiTheme.TabBorder);
        button.BorderThickness = new Thickness(isActive ? 2 : 1);
    }

    public static void StyleSlider(HorizontalSlider slider)
    {
        slider.Background = new SolidBrush(UiTheme.InputBackground);
        slider.Border = new SolidBrush(UiTheme.InputBorder);
        slider.BorderThickness = new Thickness(1);
        slider.Width = UiTheme.SettingsControlWidth;
        slider.Height = UiTheme.SettingsSliderHeight;
    }

    public static void StyleToggle(CheckButton toggle)
    {
        toggle.Background = new SolidBrush(UiTheme.InputBackground);
        toggle.OverBackground = new SolidBrush(UiTheme.InputHover);
        toggle.PressedBackground = new SolidBrush(UiTheme.InputPressed);
        toggle.Border = new SolidBrush(UiTheme.InputBorder);
        toggle.BorderThickness = new Thickness(1);
    }

#pragma warning disable CS0618 // ComboBox is obsolete in Myra; kept until ComboView adoption.
    public static void StyleDropdown(ComboView comboView)
    {
        comboView.Background = new SolidBrush(UiTheme.InputBackground);
        comboView.Border = new SolidBrush(UiTheme.InputBorder);
        comboView.BorderThickness = new Thickness(1);
        comboView.Height = UiTheme.SettingsRowHeight;
        comboView.ListView.Background = new SolidBrush(UiTheme.PanelBackground);
        comboView.ListView.Border = new SolidBrush(UiTheme.PanelBorder);
        comboView.ListView.BorderThickness = new Thickness(1);
        comboView.ListView.SelectionMode = SelectionMode.Single;
    }
#pragma warning restore CS0618

    public static Button SettingsKeybindButton(string text)
    {
        var label = new Label
        {
            Text = text,
            TextColor = UiTheme.PrimaryText,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        UiFonts.ApplyBody(label);

        var button = new Button
        {
            Content = label,
            Width = UiTheme.SettingsKeybindWidth,
            Height = UiTheme.SettingsRowHeight,
            HorizontalAlignment = HorizontalAlignment.Left
        };

        StyleInputButton(button);
        return button;
    }

    private static void StyleInputButton(Button button)
    {
        button.Background = new SolidBrush(UiTheme.InputBackground);
        button.OverBackground = new SolidBrush(UiTheme.InputHover);
        button.PressedBackground = new SolidBrush(UiTheme.InputPressed);
        button.Border = new SolidBrush(UiTheme.InputBorder);
        button.BorderThickness = new Thickness(1);
        button.DisabledBackground = new SolidBrush(UiTheme.DisabledBackground);
    }
}

