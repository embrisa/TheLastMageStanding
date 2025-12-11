using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Myra.Graphics2D;
using Myra.Graphics2D.UI;
using TheLastMageStanding.Game.Core.Ecs.Components;
using TheLastMageStanding.Game.Core.Events;

namespace TheLastMageStanding.Game.Core.UI.Myra;

/// <summary>
/// Myra-based pause and audio-settings overlay that mirrors the existing pause logic.
/// </summary>
internal sealed class MyraPauseMenuScreen : MyraMenuScreenBase
{
    private readonly List<MenuButton> _pauseButtons = new();
    private readonly Dictionary<AudioSettingField, HorizontalSlider> _sliders = new();
    private readonly Dictionary<AudioSettingField, CheckButton> _toggles = new();
    private readonly Label _audioConfirmationLabel;
    private readonly ModalDialog _audioDialog;
    private readonly Label _titleLabel;
    private readonly KeyboardHintBar _hintBar;
    private bool _applyingViewModel;
    private IUiSoundPlayer? _uiSoundPlayer;

    public MyraPauseMenuScreen(IUiSoundPlayer? uiSoundPlayer = null) : base(useRenderTarget: true)
    {
        _uiSoundPlayer = uiSoundPlayer;

        var root = UiStyles.ScreenOverlay();
        root.Visible = false;
        root.Width = VirtualWidth;
        root.Height = VirtualHeight;

        var layout = new MenuColumn(UiTheme.LargeSpacing)
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Center,
            Padding = new Thickness(UiTheme.LargePadding)
        };

        _titleLabel = UiStyles.Heading("Paused");
        layout.AddRow(_titleLabel);

        var card = new MenuCard(UiTheme.LargePadding);
        var buttonColumn = new MenuColumn(UiTheme.Spacing)
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Center
        };

        AddPauseButton(buttonColumn, "Resume", PauseMenuAction.Resume, accent: true);
        AddPauseButton(buttonColumn, "Restart Run", PauseMenuAction.Restart);
        AddPauseButton(buttonColumn, "Settings", PauseMenuAction.OpenSettings);
        AddPauseButton(buttonColumn, "Quit", PauseMenuAction.Quit);

        card.Widgets.Add(buttonColumn);
        layout.AddRow(card);

        _hintBar = new KeyboardHintBar();
        _hintBar.SetHints(
            ("Esc", "Close"),
            ("Enter", "Select"),
            ("↑/↓", "Navigate"));
        layout.AddRow(_hintBar);

        root.Widgets.Add(layout);

        _audioConfirmationLabel = new Label
        {
            Text = string.Empty,
            TextColor = UiTheme.AccentText,
            HorizontalAlignment = HorizontalAlignment.Center
        };
        UiFonts.ApplyBody(_audioConfirmationLabel, 0.9f);

        _audioDialog = BuildAudioDialog();
        root.Widgets.Add(_audioDialog);
        Desktop.Root = root;
    }

    public event Action<PauseMenuAction>? ActionRequested;
    public event Action<AudioSettingChangedEvent>? AudioSettingChanged;

    public bool IsVisible => Desktop.Root.Visible;

    public void ApplyViewModel(PauseMenuViewModel viewModel)
    {
        _applyingViewModel = true;
        Desktop.Root.Visible = viewModel.IsOpen && !viewModel.IsSettingsOpen;
        _audioDialog.Visible = false;
        HighlightPauseSelection(viewModel.SelectedIndex);
        _titleLabel.Text = "Paused";
        _hintBar.SetHints(
            ("Esc", "Close"),
            ("Enter", "Select"),
            ("↑/↓", "Navigate"));
        _applyingViewModel = false;
    }

    public void SetSoundPlayer(IUiSoundPlayer player)
    {
        _uiSoundPlayer = player;
    }

    private void AddPauseButton(MenuColumn column, string label, PauseMenuAction action, bool accent = false)
    {
        var button = new MenuButton(label, accent);
        button.Click += (_, _) => ActionRequested?.Invoke(action);
        UiSoundBinder.BindHoverAndClick(button, _uiSoundPlayer);
        _pauseButtons.Add(button);
        column.AddRow(button);
    }

    private void HighlightPauseSelection(int selectedIndex)
    {
        for (var i = 0; i < _pauseButtons.Count; i++)
        {
            UiStyles.HighlightSelection(_pauseButtons[i], i == selectedIndex);
        }
    }

    private ModalDialog BuildAudioDialog()
    {
        var dialog = new ModalDialog
        {
            Visible = false
        };

        var column = new MenuColumn(UiTheme.LargeSpacing)
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch
        };

        var header = new MenuColumn(UiTheme.Spacing);
        header.AddRow(UiStyles.Heading("Audio Settings", 1.4f, UiTheme.PrimaryText));
        column.AddRow(header);

        var grid = new Grid
        {
            RowSpacing = UiTheme.Spacing,
            ColumnSpacing = UiTheme.Spacing,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch
        };

        // Two columns: labels and sliders/toggles.
        grid.ColumnsProportions.Add(new Proportion(ProportionType.Auto));
        grid.ColumnsProportions.Add(new Proportion(ProportionType.Fill));
        grid.ColumnsProportions.Add(new Proportion(ProportionType.Auto));

        AddSliderRow(grid, 0, "Master Volume", AudioSettingField.MasterVolume);
        AddSliderRow(grid, 1, "Music Volume", AudioSettingField.MusicVolume);
        AddSliderRow(grid, 2, "SFX Volume", AudioSettingField.SfxVolume);
        AddSliderRow(grid, 3, "UI Volume", AudioSettingField.UiVolume);
        AddSliderRow(grid, 4, "Voice Volume", AudioSettingField.VoiceVolume);

        AddToggleRow(grid, 5, "Mute All", AudioSettingField.MuteAll);
        AddToggleRow(grid, 6, "Master Mute", AudioSettingField.MasterMute);
        AddToggleRow(grid, 7, "Music Mute", AudioSettingField.MusicMute);
        AddToggleRow(grid, 8, "SFX Mute", AudioSettingField.SfxMute);
        AddToggleRow(grid, 9, "UI Mute", AudioSettingField.UiMute);
        AddToggleRow(grid, 10, "Voice Mute", AudioSettingField.VoiceMute);

        var backButton = new MenuButton("Back", accent: false, width: 180, height: 44);
        backButton.Click += (_, _) => ActionRequested?.Invoke(PauseMenuAction.CloseSettings);
        grid.RowsProportions.Add(new Proportion(ProportionType.Auto));
        Grid.SetRow(backButton, 11);
        Grid.SetColumnSpan(backButton, 3);
        backButton.HorizontalAlignment = HorizontalAlignment.Center;
        UiSoundBinder.BindHoverAndClick(backButton, _uiSoundPlayer);
        grid.Widgets.Add(backButton);

        column.AddRow(grid, ProportionType.Fill);
        column.AddRow(_audioConfirmationLabel);

        dialog.ContentHost.Widgets.Add(column);
        return dialog;
    }

    private void AddSliderRow(Grid grid, int rowIndex, string labelText, AudioSettingField field)
    {
        grid.RowsProportions.Add(new Proportion(ProportionType.Auto));

        var label = UiStyles.BodyText(labelText, UiTheme.PrimaryText, wrap: false);
        Grid.SetRow(label, rowIndex);
        Grid.SetColumn(label, 0);
        grid.Widgets.Add(label);

        var slider = new HorizontalSlider
        {
            Minimum = 0,
            Maximum = 100,
            Value = 100,
            Width = 240,
            Height = 20
        };
        slider.ValueChanged += (_, __) =>
        {
            if (_applyingViewModel) return;
            AudioSettingChanged?.Invoke(new AudioSettingChangedEvent
            {
                Field = field,
                Value = (float)(slider.Value / 100f),
                Persist = true
            });
        };
        UiSoundBinder.BindHoverAndClick(slider, _uiSoundPlayer);

        Grid.SetRow(slider, rowIndex);
        Grid.SetColumn(slider, 1);
        grid.Widgets.Add(slider);
        _sliders[field] = slider;

        var percentLabel = UiStyles.BodyText("100%", UiTheme.MutedText, wrap: false);
        Grid.SetRow(percentLabel, rowIndex);
        Grid.SetColumn(percentLabel, 2);
        grid.Widgets.Add(percentLabel);

        slider.ValueChanged += (_, __) =>
        {
            percentLabel.Text = $"{(int)slider.Value}%";
        };
    }

    private void AddToggleRow(Grid grid, int rowIndex, string labelText, AudioSettingField field)
    {
        grid.RowsProportions.Add(new Proportion(ProportionType.Auto));

        var label = UiStyles.BodyText(labelText, UiTheme.PrimaryText, wrap: false);
        Grid.SetRow(label, rowIndex);
        Grid.SetColumn(label, 0);
        grid.Widgets.Add(label);

        var valueLabel = UiStyles.BodyText("Off", UiTheme.MutedText, wrap: false);
        valueLabel.VerticalAlignment = VerticalAlignment.Center;
        Grid.SetRow(valueLabel, rowIndex);
        Grid.SetColumn(valueLabel, 2);
        grid.Widgets.Add(valueLabel);

        var toggle = new CheckButton
        {
            VerticalAlignment = VerticalAlignment.Center
        };
        toggle.Click += (_, __) =>
        {
            var isOn = toggle.IsChecked;
            OnToggleChanged(field, isOn);
            valueLabel.Text = isOn ? "On" : "Off";
        };
        UiSoundBinder.BindHoverAndClick(toggle, _uiSoundPlayer);

        Grid.SetRow(toggle, rowIndex);
        Grid.SetColumn(toggle, 1);
        grid.Widgets.Add(toggle);
        _toggles[field] = toggle;
    }

    private void OnToggleChanged(AudioSettingField field, bool isChecked)
    {
        if (_applyingViewModel)
        {
            return;
        }

        AudioSettingChanged?.Invoke(new AudioSettingChangedEvent
        {
            Field = field,
            ToggleValue = isChecked,
            Persist = true
        });
    }

    private void SetAudioState(AudioSettingsState audioState, AudioSettingsMenu audioMenu)
    {
        if (!Desktop.Root.Visible)
        {
            return;
        }

        if (_sliders.TryGetValue(AudioSettingField.MasterVolume, out var master))
        {
            master.Value = Math.Clamp(audioState.MasterVolume * 100f, 0f, 100f);
        }
        if (_sliders.TryGetValue(AudioSettingField.MusicVolume, out var music))
        {
            music.Value = Math.Clamp(audioState.MusicVolume * 100f, 0f, 100f);
        }
        if (_sliders.TryGetValue(AudioSettingField.SfxVolume, out var sfx))
        {
            sfx.Value = Math.Clamp(audioState.SfxVolume * 100f, 0f, 100f);
        }
        if (_sliders.TryGetValue(AudioSettingField.UiVolume, out var ui))
        {
            ui.Value = Math.Clamp(audioState.UiVolume * 100f, 0f, 100f);
        }
        if (_sliders.TryGetValue(AudioSettingField.VoiceVolume, out var voice))
        {
            voice.Value = Math.Clamp(audioState.VoiceVolume * 100f, 0f, 100f);
        }

        SetToggle(AudioSettingField.MuteAll, audioState.MuteAll);
        SetToggle(AudioSettingField.MasterMute, audioState.MasterMuted);
        SetToggle(AudioSettingField.MusicMute, audioState.MusicMuted);
        SetToggle(AudioSettingField.SfxMute, audioState.SfxMuted);
        SetToggle(AudioSettingField.UiMute, audioState.UiMuted);
        SetToggle(AudioSettingField.VoiceMute, audioState.VoiceMuted);

        // Show confirmation text if present
        _audioConfirmationLabel.Text = string.IsNullOrEmpty(audioMenu.ConfirmationText)
            ? string.Empty
            : audioMenu.ConfirmationText;
    }

    private void SetToggle(AudioSettingField field, bool value)
    {
        if (_toggles.TryGetValue(field, out var toggle))
        {
            toggle.IsChecked = value;
        }
    }
}

