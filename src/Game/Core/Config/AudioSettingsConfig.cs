using System;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Media;
using TheLastMageStanding.Game.Core.Events;

namespace TheLastMageStanding.Game.Core.Config;

/// <summary>
/// Player audio settings with per-category volumes, mute toggles, and a master mute.
/// </summary>
internal sealed class AudioSettingsConfig
{
    public int Version { get; set; } = 1;

    public float MasterVolume { get; set; } = 1.0f;
    public float MusicVolume { get; set; } = 0.85f;
    public float SfxVolume { get; set; } = 0.9f;
    public float UiVolume { get; set; } = 1.0f;
    public float VoiceVolume { get; set; } = 1.0f;

    public bool MasterMuted { get; set; }
    public bool MusicMuted { get; set; }
    public bool SfxMuted { get; set; }
    public bool UiMuted { get; set; }
    public bool VoiceMuted { get; set; }
    public bool MuteAll { get; set; }

    public static AudioSettingsConfig Default => new();

    public AudioSettingsConfig Clone() => new()
    {
        Version = Version,
        MasterVolume = MasterVolume,
        MusicVolume = MusicVolume,
        SfxVolume = SfxVolume,
        UiVolume = UiVolume,
        VoiceVolume = VoiceVolume,
        MasterMuted = MasterMuted,
        MusicMuted = MusicMuted,
        SfxMuted = SfxMuted,
        UiMuted = UiMuted,
        VoiceMuted = VoiceMuted,
        MuteAll = MuteAll
    };

    public void Normalize()
    {
        MasterVolume = Clamp01(MasterVolume);
        MusicVolume = Clamp01(MusicVolume);
        SfxVolume = Clamp01(SfxVolume);
        UiVolume = Clamp01(UiVolume);
        VoiceVolume = Clamp01(VoiceVolume);
    }

    public float GetEffectiveMusicVolume() => CalculateEffective(MusicVolume, MusicMuted);
    public float GetEffectiveSfxVolume() => CalculateEffective(SfxVolume, SfxMuted);
    public float GetEffectiveUiVolume() => CalculateEffective(UiVolume, UiMuted);
    public float GetEffectiveVoiceVolume() => CalculateEffective(VoiceVolume, VoiceMuted);

    public float GetCategoryVolume(SfxCategory category) => category switch
    {
        SfxCategory.UI => GetEffectiveUiVolume(),
        SfxCategory.Voice => GetEffectiveVoiceVolume(),
        _ => GetEffectiveSfxVolume(),
    };

    public bool IsCategoryMuted(SfxCategory category) => category switch
    {
        SfxCategory.UI => MuteAll || MasterMuted || UiMuted,
        SfxCategory.Voice => MuteAll || MasterMuted || VoiceMuted,
        _ => MuteAll || MasterMuted || SfxMuted,
    };

    public void ApplyToMediaPlayer()
    {
        Normalize();
        var effectiveVolume = GetEffectiveMusicVolume();
        MediaPlayer.Volume = effectiveVolume;
        MediaPlayer.IsMuted = effectiveVolume <= 0.0001f;
    }

    public void ApplyToSoundEffectMaster()
    {
        Normalize();
        // SoundEffect.MasterVolume scales every instance; keep it at 1 and
        // rely on per-instance volumes to avoid double-scaling.
        SoundEffect.MasterVolume = 1f;
    }

    private float CalculateEffective(float categoryVolume, bool categoryMuted)
    {
        if (MuteAll || MasterMuted || categoryMuted)
        {
            return 0f;
        }

        var volume = MasterVolume * categoryVolume;
        return Math.Clamp(volume, 0f, 1f);
    }

    private static float Clamp01(float value)
    {
        if (float.IsNaN(value) || float.IsInfinity(value))
        {
            return 0f;
        }

        return Math.Clamp(value, 0f, 1f);
    }
}
