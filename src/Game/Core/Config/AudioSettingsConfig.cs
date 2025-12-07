using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Media;

namespace TheLastMageStanding.Game.Core.Config;

/// <summary>
/// Session-scoped audio settings with simple mute toggles for music and SFX.
/// </summary>
internal sealed class AudioSettingsConfig
{
    public AudioSettingsConfig(bool musicMuted = false, bool sfxMuted = false)
    {
        MusicMuted = musicMuted;
        SfxMuted = sfxMuted;
    }

    public bool MusicMuted { get; set; }
    public bool SfxMuted { get; set; }

    public static AudioSettingsConfig Default => new();

    public void Apply()
    {
        MediaPlayer.IsMuted = MusicMuted;
        SoundEffect.MasterVolume = SfxMuted ? 0f : 1f;
    }
}


