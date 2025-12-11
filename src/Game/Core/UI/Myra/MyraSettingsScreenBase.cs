using System;
using System.Collections.Generic;
using System.Linq;
using Myra.Graphics2D;
using Myra.Graphics2D.UI;

namespace TheLastMageStanding.Game.Core.UI.Myra;

/// <summary>
/// Shared scaffold for the tabbed settings menu. Provides tabs, content host, and
/// a unified event surface for tab changes and field changes.
/// </summary>
internal abstract class MyraSettingsScreenBase : MyraMenuScreenBase
{
    private readonly Dictionary<string, Widget> _tabContent = new();
    private readonly VerticalStackPanel _contentStack;
    private readonly Label _titleLabel;
    private readonly Label _subtitleLabel;

    protected readonly SettingsTabBar TabBar;
    protected readonly KeyboardHintBar HintBar;

    private string _activeTabId;

    public event Action<string>? TabChanged;
    public event Action<SettingFieldChange>? FieldChanged;

    protected MyraSettingsScreenBase(IEnumerable<SettingsTabDefinition>? tabs = null, IUiSoundPlayer? uiSoundPlayer = null)
        : base(useRenderTarget: true)
    {
        var tabDefinitions = (tabs ?? DefaultTabs()).ToList();
        _activeTabId = tabDefinitions.FirstOrDefault().Id ?? "audio";

        var root = UiStyles.ScreenOverlay();
        root.Width = VirtualWidth;
        root.Height = VirtualHeight;
        root.Visible = false;

        var layout = new Grid
        {
            RowSpacing = UiTheme.LargeSpacing,
            ColumnSpacing = UiTheme.LargeSpacing,
            Padding = new Thickness(UiTheme.LargePadding),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch
        };
        layout.RowsProportions.Add(new Proportion(ProportionType.Auto));
        layout.RowsProportions.Add(new Proportion(ProportionType.Auto));
        layout.RowsProportions.Add(new Proportion(ProportionType.Fill));
        layout.RowsProportions.Add(new Proportion(ProportionType.Auto));

        var header = new MenuColumn(UiTheme.Spacing)
        {
            HorizontalAlignment = HorizontalAlignment.Left
        };
        _titleLabel = UiStyles.Heading("Settings", 1.6f, UiTheme.AccentText);
        _subtitleLabel = UiStyles.BodyText("Adjust audio, video, controls, and gameplay.", UiTheme.MutedText);
        header.AddRow(_titleLabel);
        header.AddRow(_subtitleLabel);
        Grid.SetRow(header, 0);
        layout.Widgets.Add(header);

        TabBar = new SettingsTabBar(tabDefinitions, _activeTabId, uiSoundPlayer);
        TabBar.TabSelected += OnTabSelected;
        Grid.SetRow(TabBar, 1);
        layout.Widgets.Add(TabBar);

        _contentStack = new VerticalStackPanel
        {
            Spacing = UiTheme.SettingsSectionSpacing,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch
        };

        var scrollViewer = new ScrollViewer
        {
            Content = _contentStack,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch
        };

        var contentCard = UiStyles.SettingsCard(UiTheme.LargePadding);
        contentCard.HorizontalAlignment = HorizontalAlignment.Stretch;
        contentCard.VerticalAlignment = VerticalAlignment.Stretch;
        contentCard.Widgets.Add(scrollViewer);

        Grid.SetRow(contentCard, 2);
        layout.Widgets.Add(contentCard);

        HintBar = new KeyboardHintBar();
        HintBar.SetHints(("Esc", "Back"), ("Tab", "Switch"), ("Enter", "Activate"));
        Grid.SetRow(HintBar, 3);
        layout.Widgets.Add(HintBar);

        root.Widgets.Add(layout);
        Desktop.Root = root;

        ApplyContent(BuildPlaceholderContent(_activeTabId));
    }

    public bool IsVisible => Desktop.Root.Visible;
    protected string ActiveTabId => _activeTabId;

    protected void Show() => Desktop.Root.Visible = true;
    protected void Hide() => Desktop.Root.Visible = false;

    protected void SetHeader(string text) => _titleLabel.Text = text;
    protected void SetSubtitle(string text) => _subtitleLabel.Text = text;

    protected void SetTabContent(string tabId, Widget content)
    {
        _tabContent[tabId] = content;
        if (string.Equals(tabId, _activeTabId, StringComparison.Ordinal))
        {
            ApplyContent(content);
        }
    }

    protected void ClearTabContent(string tabId)
    {
        if (!_tabContent.Remove(tabId))
        {
            return;
        }

        if (string.Equals(tabId, _activeTabId, StringComparison.Ordinal))
        {
            ApplyContent(BuildPlaceholderContent(tabId));
        }
    }

    protected void RaiseFieldChanged(SettingFieldChange change) => FieldChanged?.Invoke(change);

    private void OnTabSelected(string tabId)
    {
        _activeTabId = tabId;
        if (_tabContent.TryGetValue(tabId, out var content))
        {
            ApplyContent(content);
        }
        else
        {
            ApplyContent(BuildPlaceholderContent(tabId));
        }

        TabChanged?.Invoke(tabId);
    }

    private void ApplyContent(Widget content)
    {
        _contentStack.Widgets.Clear();
        _contentStack.Widgets.Add(content);
    }

    private static IEnumerable<SettingsTabDefinition> DefaultTabs()
    {
        yield return new SettingsTabDefinition("audio", "Audio");
        yield return new SettingsTabDefinition("video", "Video");
        yield return new SettingsTabDefinition("controls", "Controls");
        yield return new SettingsTabDefinition("gameplay", "Gameplay");
    }

    private static SettingsSection BuildPlaceholderContent(string tabId)
    {
        var section = new SettingsSection("Coming soon", $"Settings for '{tabId}' are not wired yet.");
        section.AddRow(UiStyles.BodyText("Use SettingsComponents to add sliders, toggles, dropdowns, or keybind rows."));
        return section;
    }
}

