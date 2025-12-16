using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Myra;
using Myra.Graphics2D;
using Myra.Graphics2D.Brushes;
using Myra.Graphics2D.UI;
using TheLastMageStanding.Game.Core.Ecs.Components;

namespace TheLastMageStanding.Game.Core.UI.Myra;

internal sealed record InventoryRowViewModel(
    string Title,
    Color TitleColor,
    string Subtitle,
    string Footer);

internal sealed record InventoryOverlayViewModel(
    bool IsOpen,
    InventoryUiMode Mode,
    int SelectedIndex,
    IReadOnlyList<InventoryRowViewModel> Rows);

/// <summary>
/// Myra-based inventory/equipment overlay used during runs.
/// Input handling is driven by ECS; Myra is used for consistent layout and styling.
/// </summary>
internal sealed class MyraInventoryScreen : IDisposable
{
    private readonly Desktop _desktop;
    private readonly Panel _root;
    private readonly Label _titleLabel;
    private readonly Button _inventoryTab;
    private readonly Button _equipmentTab;
    private readonly VerticalStackPanel _rowsPanel;
    private readonly Label _emptyLabel;
    private readonly KeyboardHintBar _hintBar;

    public MyraInventoryScreen()
    {
        _desktop = new Desktop();

        _root = UiStyles.ScreenOverlay();
        _root.Visible = false;
        _root.Width = UiTheme.VirtualWidth;
        _root.Height = UiTheme.VirtualHeight;

        var layout = new MenuColumn(UiTheme.LargeSpacing)
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Center,
            Padding = new Thickness(UiTheme.LargePadding)
        };

        _titleLabel = UiStyles.Heading("Inventory", 1.4f, UiTheme.PrimaryText);
        layout.AddRow(_titleLabel);

        var tabsRow = new HorizontalStackPanel
        {
            Spacing = UiTheme.SettingsTabSpacing,
            HorizontalAlignment = HorizontalAlignment.Center
        };

        _inventoryTab = BuildTabButton("Inventory", InventoryUiMode.Inventory);
        _equipmentTab = BuildTabButton("Equipment", InventoryUiMode.Equipment);
        tabsRow.Widgets.Add(_inventoryTab);
        tabsRow.Widgets.Add(_equipmentTab);
        layout.AddRow(tabsRow);

        var card = new MenuCard(UiTheme.LargePadding)
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Width = 760,
            Height = 360,
            ClipToBounds = true
        };

        _rowsPanel = new VerticalStackPanel
        {
            Spacing = UiTheme.Spacing,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };

        _emptyLabel = UiStyles.BodyText("No items", UiTheme.MutedText, wrap: false);
        _emptyLabel.HorizontalAlignment = HorizontalAlignment.Center;
        _rowsPanel.Widgets.Add(_emptyLabel);

        var scrollViewer = new ScrollViewer
        {
            Content = _rowsPanel,
            VerticalAlignment = VerticalAlignment.Stretch,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
        card.Widgets.Add(scrollViewer);
        layout.AddRow(card);

        _hintBar = new KeyboardHintBar();
        _hintBar.SetHints(
            ("Tab", "Close"),
            ("Q/E", "Switch tab"),
            ("↑/↓", "Navigate"),
            ("Enter", "Equip/Unequip"),
            ("Mouse", "Click row"));
        layout.AddRow(_hintBar);

        _root.Widgets.Add(layout);
        _desktop.Root = _root;

        ApplyTabStyles(InventoryUiMode.Inventory);
    }

    public bool IsVisible => _root.Visible;

    public event Action<InventoryUiMode>? TabRequested;
    public event Action<int>? RowActivated;

    public void Dispose()
    {
        _desktop.Dispose();
    }

    public void ApplyViewModel(InventoryOverlayViewModel viewModel)
    {
        _root.Visible = viewModel.IsOpen;
        if (!viewModel.IsOpen)
        {
            return;
        }

        _titleLabel.Text = viewModel.Mode == InventoryUiMode.Inventory ? "Inventory" : "Equipment";
        ApplyTabStyles(viewModel.Mode);
        BuildRows(viewModel.Rows, viewModel.SelectedIndex);
    }

    public void Update(GameTime gameTime)
    {
        // Intentionally no scaling here; we render in the screen-space UI pass and let the caller manage
        // the active SpriteBatch state. We do map to the TLMS virtual resolution.
        if (MyraEnvironment.Game != null)
        {
            var bounds = MyraEnvironment.Game.Window.ClientBounds;
            _desktop.Scale = new Vector2(
                (float)bounds.Width / UiTheme.VirtualWidth,
                (float)bounds.Height / UiTheme.VirtualHeight);
        }
    }

    public void Render()
    {
        _desktop.Render();
    }

    private Button BuildTabButton(string text, InventoryUiMode tab)
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
            Width = 180,
            Padding = new Thickness(UiTheme.Padding, UiTheme.Padding / 2),
            HorizontalAlignment = HorizontalAlignment.Center
        };

        button.Click += (_, _) => TabRequested?.Invoke(tab);
        return button;
    }

    private void ApplyTabStyles(InventoryUiMode active)
    {
        UiStyles.StyleSettingsTabButton(_inventoryTab, active == InventoryUiMode.Inventory);
        UiStyles.StyleSettingsTabButton(_equipmentTab, active == InventoryUiMode.Equipment);
    }

    private void BuildRows(IReadOnlyList<InventoryRowViewModel> rows, int selectedIndex)
    {
        _rowsPanel.Widgets.Clear();

        if (rows.Count == 0)
        {
            _emptyLabel.Text = "No items";
            _rowsPanel.Widgets.Add(_emptyLabel);
            return;
        }

        for (var i = 0; i < rows.Count; i++)
        {
            var row = rows[i];
            var rowButton = BuildRowButton(row, i);
            UiStyles.HighlightSelection(rowButton, i == selectedIndex);
            _rowsPanel.Widgets.Add(rowButton);
        }
    }

    private Button BuildRowButton(InventoryRowViewModel row, int index)
    {
        var button = new Button
        {
            Height = 64,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Background = new SolidBrush(UiTheme.ButtonBackground),
            OverBackground = new SolidBrush(UiTheme.ButtonHover),
            PressedBackground = new SolidBrush(UiTheme.ButtonPressed),
            Border = new SolidBrush(UiTheme.ButtonBorder),
            BorderThickness = new Thickness(1),
            Padding = new Thickness(UiTheme.Padding, UiTheme.Padding / 2)
        };

        var content = new Grid
        {
            RowSpacing = 2,
            ColumnSpacing = UiTheme.Spacing,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Center
        };

        content.ColumnsProportions.Add(new Proportion(ProportionType.Fill));
        content.ColumnsProportions.Add(new Proportion(ProportionType.Auto));
        content.RowsProportions.Add(new Proportion(ProportionType.Auto));
        content.RowsProportions.Add(new Proportion(ProportionType.Auto));

        var titleLabel = UiStyles.BodyText(row.Title, row.TitleColor, wrap: false, scale: 1.0f);
        Grid.SetRow(titleLabel, 0);
        Grid.SetColumn(titleLabel, 0);
        content.Widgets.Add(titleLabel);

        var footerLabel = UiStyles.BodyText(row.Footer, UiTheme.MutedText, wrap: false, scale: 0.9f);
        footerLabel.HorizontalAlignment = HorizontalAlignment.Right;
        Grid.SetRow(footerLabel, 0);
        Grid.SetColumn(footerLabel, 1);
        content.Widgets.Add(footerLabel);

        var subtitleLabel = UiStyles.BodyText(row.Subtitle, UiTheme.MutedText, wrap: false, scale: 0.9f);
        Grid.SetRow(subtitleLabel, 1);
        Grid.SetColumnSpan(subtitleLabel, 2);
        content.Widgets.Add(subtitleLabel);

        button.Content = content;
        button.Click += (_, _) => RowActivated?.Invoke(index);
        return button;
    }
}
