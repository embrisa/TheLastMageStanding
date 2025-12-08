using Microsoft.Xna.Framework;
using TheLastMageStanding.Game.Core.Ecs.Components;

namespace TheLastMageStanding.Game.Core.Events;

/// <summary>
/// Event requesting a VFX to be spawned.
/// </summary>
internal readonly struct VfxSpawnEvent
{
    public string EffectName { get; }
    public Vector2 Position { get; }
    public VfxType Type { get; }
    public Color? ColorTint { get; }

    public VfxSpawnEvent(string effectName, Vector2 position, VfxType type = VfxType.Impact, Color? colorTint = null)
    {
        EffectName = effectName;
        Position = position;
        Type = type;
        ColorTint = colorTint;
    }
}

/// <summary>
/// Event requesting an SFX to be played.
/// </summary>
internal readonly struct SfxPlayEvent
{
    public string SoundName { get; }
    public SfxCategory Category { get; }
    public Vector2 Position { get; }
    public float Volume { get; }

    public SfxPlayEvent(string soundName, SfxCategory category, Vector2 position, float volume = 1f)
    {
        SoundName = soundName;
        Category = category;
        Position = position;
        Volume = volume;
    }
}

/// <summary>
/// SFX category for volume control.
/// </summary>
internal enum SfxCategory
{
    Attack,
    Impact,
    Ability,
    UI,
    Voice
}
