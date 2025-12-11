using System;
using System.Collections.Generic;
using System.Linq;
using Myra.Graphics2D;
using Myra.Graphics2D.UI;
using Myra.Graphics2D.Brushes;

namespace TheLastMageStanding.Game.Core.UI.Myra;

internal readonly record struct SettingFieldChange(
    string TabId,
    string FieldId,
    float? SliderValue = null,
    bool? ToggleValue = null,
    string? OptionValue = null,
    bool Persist = true);

internal readonly record struct SettingsTabDefinition(string Id, string Label);

internal sealed class SettingsTabBar : HorizontalStackPanel
{
    private readonly List<(SettingsTabDefinition Definition, Button Button)> _tabs = new();
    private string _activeId = string.Empty;
    private IUiSoundPlayer? _uiSoundPlayer;

    public event Action<string>? TabSelected;

    public SettingsTabBar(IEnumerable<SettingsTabDefinition> tabs, string activeTabId, IUiSoundPlayer? uiSoundPlayer = null)
    {
        Spacing = UiTheme.SettingsTabSpacing;
        HorizontalAlignment = HorizontalAlignment.Left;
        VerticalAlignment = VerticalAlignment.Center;
        _uiSoundPlayer = uiSoundPlayer;

        SetTabs(tabs, activeTabId);
    }

    public string ActiveTabId => _activeId;

    public void SetTabs(IEnumerable<SettingsTabDefinition> tabs, string activeTabId)
    {
        Widgets.Clear();
        _tabs.Clear();
        _activeId = activeTabId;

        foreach (var tab in tabs)
        {
            var button = UiStyles.SettingsTabButton(tab.Label, tab.Id == activeTabId);
            UiSoundBinder.BindHoverAndClick(button, _uiSoundPlayer);
            button.Click += (_, _) => Select(tab.Id);
            _tabs.Add((tab, button));
            Widgets.Add(button);
        }
    }

    public void SetSoundPlayer(IUiSoundPlayer player)
    {
        _uiSoundPlayer = player;
        foreach (var (_, button) in _tabs)
        {
            UiSoundBinder.BindHoverAndClick(button, _uiSoundPlayer);
        }
    }

    public void Select(string tabId)
    {
        if (string.Equals(_activeId, tabId, StringComparison.Ordinal))
        {
            return;
        }

        _activeId = tabId;
        foreach (var (definition, button) in _tabs)
        {
            UiStyles.StyleSettingsTabButton(button, definition.Id == _activeId);
        }

        TabSelected?.Invoke(tabId);
    }
}

internal sealed class SettingsSection : Panel
{
    private readonly VerticalStackPanel _content;

    public SettingsSection(string title, string? description = null)
    {
        Background = new SolidBrush(UiTheme.CardBackground);
        Border = new SolidBrush(UiTheme.CardBorder);
        BorderThickness = new Thickness(1);
        Padding = new Thickness(UiTheme.SettingsSectionPadding);
        HorizontalAlignment = HorizontalAlignment.Stretch;
        VerticalAlignment = VerticalAlignment.Stretch;

        _content = new VerticalStackPanel
        {
            Spacing = UiTheme.SettingsSectionSpacing,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch
        };

        _content.Widgets.Add(UiStyles.SectionTitle(title));

        if (!string.IsNullOrWhiteSpace(description))
        {
            _content.Widgets.Add(UiStyles.SectionDescription(description));
        }

        Widgets.Add(_content);
    }

    public void AddRow(Widget widget)
    {
        widget.HorizontalAlignment = HorizontalAlignment.Stretch;
        _content.Widgets.Add(widget);
    }
}

internal sealed class SettingsFieldRow : Grid
{
    public SettingsFieldRow(string labelText, Widget control, Widget? valueOrHint = null)
    {
        ColumnSpacing = UiTheme.SettingsRowSpacing;
        RowSpacing = UiTheme.SettingsRowSpacing;
        ColumnsProportions.Add(new Proportion(ProportionType.Auto));
        ColumnsProportions.Add(new Proportion(ProportionType.Fill));
        ColumnsProportions.Add(new Proportion(ProportionType.Auto));

        Label = UiStyles.SettingsLabel(labelText);
        Label.Width = UiTheme.SettingsLabelWidth;
        Label.VerticalAlignment = VerticalAlignment.Center;
        Grid.SetColumn(Label, 0);
        Widgets.Add(Label);

        Control = control;
        Control.HorizontalAlignment = HorizontalAlignment.Stretch;
        Control.VerticalAlignment = VerticalAlignment.Center;
        Grid.SetColumn(Control, 1);
        Widgets.Add(Control);

        if (valueOrHint != null)
        {
            valueOrHint.HorizontalAlignment = HorizontalAlignment.Right;
            valueOrHint.VerticalAlignment = VerticalAlignment.Center;
            Grid.SetColumn(valueOrHint, 2);
            Widgets.Add(valueOrHint);
            ValueLabel = valueOrHint as Label;
        }
    }

    public Label Label { get; }
    public Widget Control { get; }
    public Label? ValueLabel { get; }

    public static SettingsFieldRow SliderRow(
        string tabId,
        string fieldId,
        string label,
        double minimum,
        double maximum,
        double value,
        Action<SettingFieldChange>? onChanged = null,
        Func<double, string>? format = null,
        IUiSoundPlayer? uiSoundPlayer = null)
    {
        format ??= v => $"{Math.Round(v)}";
        var clamped = Math.Clamp(value, minimum, maximum);

        var slider = new HorizontalSlider
        {
            Minimum = (float)minimum,
            Maximum = (float)maximum,
            Value = (float)clamped,
            Width = UiTheme.SettingsControlWidth,
            Height = UiTheme.SettingsSliderHeight
        };

        UiStyles.StyleSlider(slider);
        UiSoundBinder.BindHoverAndClick(slider, uiSoundPlayer);

        var valueLabel = UiStyles.SettingsValue(format(clamped));
        slider.ValueChanged += (_, __) =>
        {
            valueLabel.Text = format(slider.Value);
            onChanged?.Invoke(new SettingFieldChange(
                tabId,
                fieldId,
                SliderValue: (float)slider.Value,
                Persist: true));
        };

        return new SettingsFieldRow(label, slider, valueLabel);
    }

    public static SettingsFieldRow ToggleRow(
        string tabId,
        string fieldId,
        string label,
        bool initialValue,
        Action<SettingFieldChange>? onChanged = null,
        IUiSoundPlayer? uiSoundPlayer = null)
    {
        var toggle = new CheckButton
        {
            IsChecked = initialValue,
            Width = UiTheme.SettingsToggleSize,
            Height = UiTheme.SettingsToggleSize
        };

        UiStyles.StyleToggle(toggle);
        UiSoundBinder.BindHoverAndClick(toggle, uiSoundPlayer);

        var valueLabel = UiStyles.SettingsValue(initialValue ? "On" : "Off");

        toggle.Click += (_, __) =>
        {
            var isOn = toggle.IsChecked;
            valueLabel.Text = isOn ? "On" : "Off";
            onChanged?.Invoke(new SettingFieldChange(
                tabId,
                fieldId,
                ToggleValue: isOn,
                Persist: true));
        };

        return new SettingsFieldRow(label, toggle, valueLabel);
    }

#pragma warning disable CS0618 // ComboBox/ListItem obsolete in Myra; retained until ComboView migration.
    public static SettingsFieldRow DropdownRow(
        string tabId,
        string fieldId,
        string label,
        IEnumerable<(string Id, string Label)> options,
        string? selectedId,
        Action<SettingFieldChange>? onChanged = null,
        IUiSoundPlayer? uiSoundPlayer = null)
    {
        var comboView = new ComboView
        {
            Width = UiTheme.SettingsControlWidth,
            Height = UiTheme.SettingsRowHeight,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            SelectionMode = SelectionMode.Single
        };

        UiStyles.StyleDropdown(comboView);
        UiSoundBinder.BindHoverAndClick(comboView, uiSoundPlayer);

        var optionList = options.ToList();
        var widgets = new List<Widget>();
        var selectedIndex = 0;

        for (var i = 0; i < optionList.Count; i++)
        {
            var option = optionList[i];
            var itemLabel = UiStyles.SettingsLabel(option.Label);
            itemLabel.Tag = option.Id;
            widgets.Add(itemLabel);
            if (!string.IsNullOrEmpty(selectedId) &&
                string.Equals(option.Id, selectedId, StringComparison.OrdinalIgnoreCase))
            {
                selectedIndex = i;
            }
        }

        foreach (var widget in widgets)
        {
            comboView.ListView.Widgets.Add(widget);
        }

        if (comboView.ListView.Widgets.Count > 0)
        {
            comboView.SelectedIndex = selectedIndex;
        }

        var currentSelection = comboView.SelectedItem as Label;
        var valueLabel = UiStyles.SettingsValue(currentSelection?.Text ?? string.Empty);

        comboView.SelectedIndexChanged += (_, __) =>
        {
            var selected = comboView.SelectedItem as Label;
            valueLabel.Text = selected?.Text ?? string.Empty;
            var selectedOption = selected?.Tag as string ?? string.Empty;
            onChanged?.Invoke(new SettingFieldChange(
                tabId,
                fieldId,
                OptionValue: selectedOption,
                Persist: true));
        };

        return new SettingsFieldRow(label, comboView, valueLabel);
    }
#pragma warning restore CS0618

    public static SettingsFieldRow KeybindRow(
        string tabId,
        string fieldId,
        string label,
        string bindingLabel,
        Action<string, string>? onRequestRebind = null,
        string? hint = null,
        IUiSoundPlayer? uiSoundPlayer = null)
    {
        var keyButton = UiStyles.SettingsKeybindButton(bindingLabel);
        UiSoundBinder.BindHoverAndClick(keyButton, uiSoundPlayer);
        keyButton.Click += (_, _) => onRequestRebind?.Invoke(tabId, fieldId);

        Label? hintLabel = null;
        if (!string.IsNullOrWhiteSpace(hint))
        {
            hintLabel = UiStyles.SettingsHint(hint);
        }

        return new SettingsFieldRow(label, keyButton, hintLabel);
    }
}

