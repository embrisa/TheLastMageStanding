using Microsoft.Xna.Framework;
using Myra.Graphics2D;
using Myra.Graphics2D.Brushes;
using Myra.Graphics2D.UI;

namespace TheLastMageStanding.Game.Core.UI.Myra;

internal sealed class MenuButton : Button
{
    public MenuButton(string text, bool accent = false, int width = 420, int height = 52)
    {
        var content = new Grid
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch
        };
        var label = new Label
        {
            Text = text,
            TextColor = UiTheme.PrimaryText,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        UiFonts.ApplyBody(label);
        content.Widgets.Add(label);

        Content = content;
        Width = width;
        Height = height;
        HorizontalAlignment = HorizontalAlignment.Center;
        UiStyles.StyleMenuButton(this, accent);
    }
}

internal sealed class MenuCard : Panel
{
    public MenuCard(int padding = UiTheme.Padding)
    {
        Background = new SolidBrush(UiTheme.CardBackground);
        Border = new SolidBrush(UiTheme.CardBorder);
        BorderThickness = new Thickness(1);
        Padding = new Thickness(padding);
    }
}

internal sealed class MenuColumn : Grid
{
    private int _rowIndex;

    public MenuColumn(int spacing = UiTheme.Spacing)
    {
        RowSpacing = spacing;
        ColumnSpacing = spacing;
        HorizontalAlignment = HorizontalAlignment.Stretch;
        VerticalAlignment = VerticalAlignment.Stretch;
    }

    public void AddRow(Widget widget, ProportionType proportionType = ProportionType.Auto)
    {
        RowsProportions.Add(new Proportion(proportionType));
        Grid.SetRow(widget, _rowIndex++);
        Widgets.Add(widget);
    }
}

internal sealed class ModalDialog : Grid
{
    public Panel ContentHost { get; }

    public ModalDialog()
    {
        RowSpacing = 0;
        ColumnSpacing = 0;
        HorizontalAlignment = HorizontalAlignment.Stretch;
        VerticalAlignment = VerticalAlignment.Stretch;

        var overlay = UiStyles.ScreenOverlay();
        overlay.HorizontalAlignment = HorizontalAlignment.Stretch;
        overlay.VerticalAlignment = VerticalAlignment.Stretch;
        Widgets.Add(overlay);

        ContentHost = UiStyles.Card(UiTheme.LargePadding);
        ContentHost.HorizontalAlignment = HorizontalAlignment.Center;
        ContentHost.VerticalAlignment = VerticalAlignment.Center;
        Widgets.Add(ContentHost);
    }
}

internal sealed class KeyboardHintBar : HorizontalStackPanel
{
    public KeyboardHintBar()
    {
        Spacing = UiTheme.Spacing;
        HorizontalAlignment = HorizontalAlignment.Center;
        VerticalAlignment = VerticalAlignment.Center;
    }

    public void SetHints(params (string Key, string Description)[] hints)
    {
        Widgets.Clear();
        foreach (var hint in hints)
        {
            Widgets.Add(BuildHint(hint.Key, hint.Description));
        }
    }

    private static HorizontalStackPanel BuildHint(string key, string description)
    {
        var container = new HorizontalStackPanel { Spacing = UiTheme.Spacing };

        var keyBadge = new Panel
        {
            Background = new SolidBrush(UiTheme.ButtonBackground),
            Border = new SolidBrush(UiTheme.ButtonBorder),
            BorderThickness = new Thickness(1),
            Padding = new Thickness(6, 4),
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Center
        };
        var keyLabel = new Label
        {
            Text = key,
            TextColor = UiTheme.AccentText,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        UiFonts.ApplyBody(keyLabel, 0.9f);
        keyBadge.Widgets.Add(keyLabel);

        container.Widgets.Add(keyBadge);
        var descriptionLabel = new Label
        {
            Text = description,
            TextColor = UiTheme.MutedText,
            VerticalAlignment = VerticalAlignment.Center
        };
        UiFonts.ApplyBody(descriptionLabel);
        container.Widgets.Add(descriptionLabel);

        return container;
    }
}

