using Microsoft.Xna.Framework.Media;
using TheLastMageStanding.Game.Core.Config;

namespace TheLastMageStanding.Game.Core.Audio;

/// <summary>
/// Manages music playback and applies volume/mute settings.
/// </summary>
internal sealed class MusicService
{
    private readonly AudioSettingsConfig _settings;
    private Song? _currentSong;

    public MusicService(AudioSettingsConfig settings)
    {
        _settings = settings;
    }

    public void Play(Song song, bool isRepeating = true)
    {
        _currentSong = song;
        MediaPlayer.IsRepeating = isRepeating;
        ApplySettings();
        MediaPlayer.Play(song);
    }

    public void ApplySettings()
    {
        _settings.Normalize();
        var effectiveVolume = _settings.GetEffectiveMusicVolume();
        MediaPlayer.Volume = effectiveVolume;
        MediaPlayer.IsMuted = effectiveVolume <= 0.0001f;
    }

    public void Stop()
    {
        _currentSong = null;
        MediaPlayer.Stop();
    }

    public void EnsurePlaying()
    {
        if (_currentSong != null && MediaPlayer.State != MediaState.Playing)
        {
            ApplySettings();
            MediaPlayer.Play(_currentSong);
        }
    }
}

