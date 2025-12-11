using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Myra.Graphics2D.UI;
using TheLastMageStanding.Game.Core.Config;
using TheLastMageStanding.Game.Core.Events;

namespace TheLastMageStanding.Game.Core.UI.Myra;

internal interface IUiSoundPlayer
{
    void PlayHover();
    void PlayClick();
}

/// <summary>
/// Helper to bind hover/click SFX to Myra widgets in a consistent way.
/// </summary>
internal static class UiSoundBinder
{
    private static WeakReference<Widget>? _lastKeyboardHover;

    public static void BindHoverAndClick(Widget widget, IUiSoundPlayer? player)
    {
        if (widget == null || player == null)
        {
            return;
        }

        widget.MouseEntered += (_, _) => player.PlayHover();

        switch (widget)
        {
            case Button button:
                button.Click += (_, _) => player.PlayClick();
                break;
            case CheckButton checkButton:
                checkButton.Click += (_, _) => player.PlayClick();
                break;
            case HorizontalSlider slider:
                slider.ValueChanged += (_, _) => player.PlayClick();
                break;
            case ListView listView:
                listView.SelectedIndexChanged += (_, _) => player.PlayClick();
                break;
            case ComboView comboView:
                comboView.SelectedIndexChanged += (_, _) => player.PlayClick();
                break;
        }
    }

    /// <summary>
    /// Play hover SFX for keyboard-driven focus changes. Avoids double-firing when the same widget stays selected.
    /// </summary>
    public static void PlayKeyboardHover(Widget? widget, IUiSoundPlayer? player)
    {
        if (player == null)
        {
            return;
        }

        if (widget != null &&
            _lastKeyboardHover != null &&
            _lastKeyboardHover.TryGetTarget(out var last) &&
            ReferenceEquals(last, widget))
        {
            return;
        }

        if (widget != null)
        {
            _lastKeyboardHover = new WeakReference<Widget>(widget);
        }

        player.PlayHover();
    }

    /// <summary>
    /// Play click SFX for keyboard activation (Enter/Space).
    /// </summary>
    public static void PlayKeyboardActivate(IUiSoundPlayer? player)
    {
        player?.PlayClick();
    }

    /// <summary>
    /// Play click SFX for keyboard cancel/back actions.
    /// </summary>
    public static void PlayKeyboardCancel(IUiSoundPlayer? player)
    {
        player?.PlayClick();
    }
}

/// <summary>
/// Plays UI SFX by publishing events to the ECS bus so SfxSystem handles volume/mute.
/// </summary>
internal sealed class EventBusUiSoundPlayer : IUiSoundPlayer
{
    private readonly IEventBus _eventBus;

    public EventBusUiSoundPlayer(IEventBus eventBus)
    {
        _eventBus = eventBus;
    }

    public void PlayHover() => Publish("UserInterfaceOnHover");

    public void PlayClick() => Publish("UserInterfaceOnClick");

    private void Publish(string soundName)
    {
        _eventBus.Publish(new SfxPlayEvent(soundName, SfxCategory.UI, Vector2.Zero));
    }
}

/// <summary>
/// Direct playback path for main-menu UI where ECS/SfxSystem is not active.
/// Respects AudioSettingsConfig UI volume/mutes.
/// </summary>
internal sealed class DirectUiSoundPlayer : IUiSoundPlayer
{
    private readonly AudioSettingsConfig _settings;
    private readonly SoundEffect? _hover;
    private readonly SoundEffect? _click;

    public DirectUiSoundPlayer(ContentManager content, AudioSettingsConfig settings)
    {
        _settings = settings;
        _hover = TryLoad(content, "Audio/UserInterfaceOnHover");
        _click = TryLoad(content, "Audio/UserInterfaceOnClick");
    }

    public void PlayHover() => Play(_hover);

    public void PlayClick() => Play(_click);

    private void Play(SoundEffect? effect)
    {
        if (effect == null)
        {
            return;
        }

        var volume = _settings.GetEffectiveUiVolume();
        if (volume <= 0f)
        {
            return;
        }

        effect.Play(volume, pitch: 0f, pan: 0f);
    }

    private static SoundEffect? TryLoad(ContentManager content, string assetName)
    {
        try
        {
            return content.Load<SoundEffect>(assetName);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[UI SFX] Failed to load {assetName}: {ex.Message}");
            return null;
        }
    }
}

