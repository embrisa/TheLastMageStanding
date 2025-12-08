using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using TheLastMageStanding.Game.Core.Config;
using TheLastMageStanding.Game.Core.Events;

namespace TheLastMageStanding.Game.Core.Ecs.Systems;

/// <summary>
/// Manages SFX playback with per-category volume control and live re-application on changes.
/// </summary>
internal sealed class SfxSystem : IUpdateSystem, ILoadContentSystem
{
    private readonly record struct ActiveSound(SoundEffectInstance Instance, SfxCategory Category, float BaseVolume);

    private readonly Dictionary<SfxCategory, float> _categoryBaseVolumes = new()
    {
        { SfxCategory.Attack, 0.7f },
        { SfxCategory.Impact, 0.8f },
        { SfxCategory.Ability, 0.9f },
        { SfxCategory.UI, 1.0f },
        { SfxCategory.Voice, 1.0f },
    };

    private readonly List<ActiveSound> _activeInstances = new();
    private readonly HashSet<string> _missingAssets = new();
    private readonly Dictionary<string, SoundEffect> _loadedSounds = new();
    private readonly AudioSettingsConfig _settings;

    public SfxSystem(AudioSettingsConfig settings)
    {
        _settings = settings;
    }

    public void Initialize(EcsWorld world)
    {
        _settings.ApplyToSoundEffectMaster();
        world.EventBus.Subscribe<SfxPlayEvent>(OnSfxPlayEvent);
    }

    public void LoadContent(EcsWorld world, GraphicsDevice graphicsDevice, ContentManager content)
    {
        _ = world;
        _ = graphicsDevice;
        // Preload common/UI samples so settings feedback can play immediately.
        LoadSound(content, "Audio/GameplayOnPlayerDeath", "GameplayOnPlayerDeath");
        LoadSound(content, "Audio/UserInterfaceOnClick", "UserInterfaceOnClick");
        LoadSound(content, "Audio/UserInterfaceOnHover", "UserInterfaceOnHover");
    }

    public void Update(EcsWorld world, in EcsUpdateContext context)
    {
        // Prune finished instances to keep memory tidy
        for (var i = _activeInstances.Count - 1; i >= 0; i--)
        {
            var active = _activeInstances[i];
            if (active.Instance.State == SoundState.Stopped)
            {
                active.Instance.Dispose();
                _activeInstances.RemoveAt(i);
            }
        }
    }

    public void ApplySettings()
    {
        _settings.ApplyToSoundEffectMaster();

        for (var i = 0; i < _activeInstances.Count; i++)
        {
            var active = _activeInstances[i];
            if (active.Instance.State == SoundState.Playing)
            {
                active.Instance.Volume = CalculateVolume(active.Category, active.BaseVolume);
                _activeInstances[i] = active;
            }
        }
    }

    private void OnSfxPlayEvent(SfxPlayEvent evt)
    {
        if (_settings.MuteAll || _settings.MasterMuted)
        {
            return;
        }

        // Check if asset is missing
        if (_missingAssets.Contains(evt.SoundName))
        {
            return;
        }

        if (!_loadedSounds.TryGetValue(evt.SoundName, out var sound))
        {
            if (!_missingAssets.Contains(evt.SoundName))
            {
                Console.WriteLine($"[SFX] Missing asset: {evt.SoundName} category={evt.Category} (will not log again)");
                _missingAssets.Add(evt.SoundName);
            }
            return;
        }

        var finalVolume = CalculateVolume(evt.Category, evt.Volume);
        if (finalVolume <= 0f)
        {
            return;
        }

        var instance = sound.CreateInstance();
        instance.Volume = finalVolume;
        instance.Pan = 0f;
        instance.Pitch = 0f;
        instance.Play();

        _activeInstances.Add(new ActiveSound(instance, evt.Category, evt.Volume));
    }

    private void LoadSound(ContentManager content, string assetName, string key)
    {
        try
        {
            var sound = content.Load<SoundEffect>(assetName);
            _loadedSounds[key] = sound;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SFX] Failed to load {assetName}: {ex.Message}");
            _missingAssets.Add(key);
        }
    }

    private float CalculateVolume(SfxCategory category, float requestedVolume)
    {
        if (_settings.IsCategoryMuted(category))
        {
            return 0f;
        }

        var categoryBase = _categoryBaseVolumes.TryGetValue(category, out var baseVol) ? baseVol : 1f;
        var categoryVolume = _settings.GetCategoryVolume(category);
        var finalVolume = categoryVolume * categoryBase * requestedVolume;
        return Math.Clamp(finalVolume, 0f, 1f);
    }
}
