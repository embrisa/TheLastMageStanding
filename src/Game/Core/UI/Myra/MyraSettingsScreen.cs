using System;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using Microsoft.Xna.Framework.Input;
using Myra.Graphics2D.UI;
using TheLastMageStanding.Game.Core.Config;
using TheLastMageStanding.Game.Core.Ecs.Components;
using TheLastMageStanding.Game.Core.Events;

namespace TheLastMageStanding.Game.Core.UI.Myra;

/// <summary>
/// Tabbed settings UI for pause overlay. Handles audio, video, and control bindings.
/// </summary>
internal sealed class MyraSettingsScreen : MyraSettingsScreenBase
{
    private readonly IUiSoundPlayer? _uiSoundPlayer;
    private readonly Dictionary<string, Label> _bindingLabels = new(StringComparer.Ordinal);
    private InputBindingsConfig _bindings = InputBindingsConfig.Default;
    private string? _listeningActionId;
    private Label? _audioStatusLabel;
    private KeyboardState _previousKeyState;

    public MyraSettingsScreen(IUiSoundPlayer? uiSoundPlayer = null)
        : base(
            tabs: new[]
            {
                new SettingsTabDefinition("audio", "Audio"),
                new SettingsTabDefinition("video", "Video"),
                new SettingsTabDefinition("controls", "Controls"),
            },
            uiSoundPlayer: uiSoundPlayer)
    {
        _uiSoundPlayer = uiSoundPlayer;
        SetHeader("Settings");
        SetSubtitle("Adjust audio, video, and controls.");

        TabChanged += OnTabChanged;
    }

    public event Action<AudioSettingChangedEvent>? AudioSettingChanged;
    public event Action<VideoSettingChangedEvent>? VideoSettingChanged;
    public event Action<InputBindingChangedEvent>? InputBindingChanged;
    public event Action<string>? TabChangedEvent;

    public override void Update(Microsoft.Xna.Framework.GameTime gameTime)
    {
        base.Update(gameTime);

        var currentKeys = Keyboard.GetState();
        if (IsVisible && !string.IsNullOrEmpty(_listeningActionId))
        {
            foreach (var key in currentKeys.GetPressedKeys())
            {
                if (!_previousKeyState.IsKeyDown(key))
                {
                    CompleteRebind(key);
                    break;
                }
            }
        }

        _previousKeyState = currentKeys;
    }

    public void ApplyViewModel(SettingsMenuViewModel viewModel)
    {
        _bindings = viewModel.Bindings.Clone();
        if (viewModel.IsOpen)
        {
            Show();
            TabBar.Select(viewModel.ActiveTab);
            BuildAudioTab(viewModel.AudioState, viewModel.AudioMenu);
            BuildVideoTab(viewModel.VideoSettings);
            BuildControlsTab();
        }
        else
        {
            _listeningActionId = null;
            Hide();
        }
    }

    private void BuildAudioTab(AudioSettingsState audioState, AudioSettingsMenu audioMenu)
    {
        var section = new SettingsSection("Audio");
        section.AddRow(SettingsFieldRow.SliderRow(
            "audio",
            "master",
            "Master Volume",
            0,
            100,
            audioState.MasterVolume * 100f,
            onChanged: change => RaiseAudioChange(AudioSettingField.MasterVolume, change.SliderValue),
            uiSoundPlayer: _uiSoundPlayer));
        section.AddRow(SettingsFieldRow.SliderRow(
            "audio",
            "music",
            "Music Volume",
            0,
            100,
            audioState.MusicVolume * 100f,
            onChanged: change => RaiseAudioChange(AudioSettingField.MusicVolume, change.SliderValue),
            uiSoundPlayer: _uiSoundPlayer));
        section.AddRow(SettingsFieldRow.SliderRow(
            "audio",
            "sfx",
            "SFX Volume",
            0,
            100,
            audioState.SfxVolume * 100f,
            onChanged: change => RaiseAudioChange(AudioSettingField.SfxVolume, change.SliderValue),
            uiSoundPlayer: _uiSoundPlayer));
        section.AddRow(SettingsFieldRow.SliderRow(
            "audio",
            "ui",
            "UI Volume",
            0,
            100,
            audioState.UiVolume * 100f,
            onChanged: change => RaiseAudioChange(AudioSettingField.UiVolume, change.SliderValue),
            uiSoundPlayer: _uiSoundPlayer));
        section.AddRow(SettingsFieldRow.SliderRow(
            "audio",
            "voice",
            "Voice Volume",
            0,
            100,
            audioState.VoiceVolume * 100f,
            onChanged: change => RaiseAudioChange(AudioSettingField.VoiceVolume, change.SliderValue),
            uiSoundPlayer: _uiSoundPlayer));

        section.AddRow(SettingsFieldRow.ToggleRow(
            "audio",
            "mute_all",
            "Mute All",
            audioState.MuteAll,
            onChanged: change => RaiseAudioChange(AudioSettingField.MuteAll, null, change.ToggleValue),
            uiSoundPlayer: _uiSoundPlayer));
        section.AddRow(SettingsFieldRow.ToggleRow(
            "audio",
            "mute_master",
            "Master Mute",
            audioState.MasterMuted,
            onChanged: change => RaiseAudioChange(AudioSettingField.MasterMute, null, change.ToggleValue),
            uiSoundPlayer: _uiSoundPlayer));
        section.AddRow(SettingsFieldRow.ToggleRow(
            "audio",
            "mute_music",
            "Music Mute",
            audioState.MusicMuted,
            onChanged: change => RaiseAudioChange(AudioSettingField.MusicMute, null, change.ToggleValue),
            uiSoundPlayer: _uiSoundPlayer));
        section.AddRow(SettingsFieldRow.ToggleRow(
            "audio",
            "mute_sfx",
            "SFX Mute",
            audioState.SfxMuted,
            onChanged: change => RaiseAudioChange(AudioSettingField.SfxMute, null, change.ToggleValue),
            uiSoundPlayer: _uiSoundPlayer));
        section.AddRow(SettingsFieldRow.ToggleRow(
            "audio",
            "mute_ui",
            "UI Mute",
            audioState.UiMuted,
            onChanged: change => RaiseAudioChange(AudioSettingField.UiMute, null, change.ToggleValue),
            uiSoundPlayer: _uiSoundPlayer));
        section.AddRow(SettingsFieldRow.ToggleRow(
            "audio",
            "mute_voice",
            "Voice Mute",
            audioState.VoiceMuted,
            onChanged: change => RaiseAudioChange(AudioSettingField.VoiceMute, null, change.ToggleValue),
            uiSoundPlayer: _uiSoundPlayer));

        _audioStatusLabel = UiStyles.SettingsHint(audioMenu.ConfirmationText ?? string.Empty);
        section.AddRow(_audioStatusLabel);

        SetTabContent("audio", section);
    }

    private void BuildVideoTab(VideoSettingsConfig video)
    {
        var section = new SettingsSection("Video");
        section.AddRow(SettingsFieldRow.ToggleRow(
            "video",
            "fullscreen",
            "Fullscreen",
            video.Fullscreen,
            onChanged: change =>
            {
                if (change.ToggleValue.HasValue)
                {
                    VideoSettingChanged?.Invoke(new VideoSettingChangedEvent
                    {
                        Field = VideoSettingField.Fullscreen,
                        ToggleValue = change.ToggleValue,
                        Persist = change.Persist
                    });
                }
            },
            _uiSoundPlayer));

        section.AddRow(SettingsFieldRow.ToggleRow(
            "video",
            "vsync",
            "VSync",
            video.VSync,
            onChanged: change =>
            {
                if (change.ToggleValue.HasValue)
                {
                    VideoSettingChanged?.Invoke(new VideoSettingChangedEvent
                    {
                        Field = VideoSettingField.VSync,
                        ToggleValue = change.ToggleValue,
                        Persist = change.Persist
                    });
                }
            },
            _uiSoundPlayer));

        var scaleOptions = BuildScaleOptions();
        var selectedScale = video.WindowScale.ToString(CultureInfo.InvariantCulture);
        section.AddRow(SettingsFieldRow.DropdownRow(
            "video",
            "scale",
            "Window Scale",
            scaleOptions,
            selectedScale,
            onChanged: change =>
            {
                if (int.TryParse(change.OptionValue, out var newScale))
                {
                    VideoSettingChanged?.Invoke(new VideoSettingChangedEvent
                    {
                        Field = VideoSettingField.WindowScale,
                        WindowScale = newScale,
                        Persist = change.Persist
                    });
                }
            },
            _uiSoundPlayer));

        SetTabContent("video", section);
    }

    private void BuildControlsTab()
    {
        _bindingLabels.Clear();
        var section = new SettingsSection("Controls");

        foreach (var (actionId, label) in GetControlActions())
        {
            var binding = _bindings.GetBinding(actionId);
            var button = SettingsFieldRow.KeybindRow(
                "controls",
                actionId,
                label,
                FormatBinding(binding),
                onRequestRebind: (_, fieldId) => BeginRebind(fieldId),
                hint: _listeningActionId == actionId ? "Press a key..." : null,
                uiSoundPlayer: _uiSoundPlayer);

            if (button.Control is Button keyButton && keyButton.Content is Label buttonLabel)
            {
                _bindingLabels[actionId] = buttonLabel;
            }
            section.AddRow(button);
        }

        SetTabContent("controls", section);
    }

    private void BeginRebind(string actionId)
    {
        _listeningActionId = actionId;
        if (_bindingLabels.TryGetValue(actionId, out var label))
        {
            label.Text = "Press a key...";
        }
    }

    private void CompleteRebind(Keys key)
    {
        if (string.IsNullOrEmpty(_listeningActionId))
        {
            return;
        }

        var actionId = _listeningActionId;
        _listeningActionId = null;

        _bindings.Bindings[actionId] = new InputBinding(key, null);
        if (_bindingLabels.TryGetValue(actionId, out var label))
        {
            label.Text = FormatBinding(_bindings.GetBinding(actionId));
        }

        InputBindingChanged?.Invoke(new InputBindingChangedEvent
        {
            ActionId = actionId,
            NewPrimary = key,
            NewAlternate = null,
            Persist = true
        });
    }

    private void OnTabChanged(string tabId)
    {
        TabChangedEvent?.Invoke(tabId);
    }

    private static List<(string Id, string Label)> BuildScaleOptions()
    {
        var options = new List<(string, string)>();
        for (var scale = 1; scale <= 3; scale++)
        {
            var width = 960 * scale;
            var height = 540 * scale;
            options.Add((scale.ToString(CultureInfo.InvariantCulture), $"{scale}x ({width}x{height})"));
        }

        return options;
    }

    private static IEnumerable<(string ActionId, string Label)> GetControlActions()
    {
        yield return (InputActions.Pause, "Pause");
        yield return (InputActions.MenuConfirm, "Confirm");
        yield return (InputActions.MenuBack, "Back");
        yield return (InputActions.MenuUp, "Menu Up");
        yield return (InputActions.MenuDown, "Menu Down");
        yield return (InputActions.MenuLeft, "Menu Left");
        yield return (InputActions.MenuRight, "Menu Right");
        yield return (InputActions.MoveUp, "Move Up");
        yield return (InputActions.MoveDown, "Move Down");
        yield return (InputActions.MoveLeft, "Move Left");
        yield return (InputActions.MoveRight, "Move Right");
        yield return (InputActions.Attack, "Attack");
        yield return (InputActions.Dash, "Dash");
        yield return (InputActions.Interact, "Interact");
        yield return (InputActions.PerkTree, "Perk Tree");
        yield return (InputActions.Inventory, "Inventory");
        yield return (InputActions.Respec, "Respec");
        yield return (InputActions.Restart, "Restart");
        yield return (InputActions.Skill1, "Skill 1");
        yield return (InputActions.Skill2, "Skill 2");
        yield return (InputActions.Skill3, "Skill 3");
        yield return (InputActions.Skill4, "Skill 4");
    }

    private void RaiseAudioChange(AudioSettingField field, float? value, bool? toggle = null)
    {
        AudioSettingChanged?.Invoke(new AudioSettingChangedEvent
        {
            Field = field,
            Value = value.HasValue ? value.Value / 100f : null,
            ToggleValue = toggle,
            Persist = true
        });
    }

    private static string FormatBinding(InputBinding binding)
    {
        if (binding.Alternate.HasValue && binding.Alternate.Value != Keys.None)
        {
            return $"{binding.Primary} / {binding.Alternate.Value}";
        }

        return binding.Primary.ToString();
    }
}

