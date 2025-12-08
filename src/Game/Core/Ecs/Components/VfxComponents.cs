using Microsoft.Xna.Framework;

namespace TheLastMageStanding.Game.Core.Ecs.Components;

/// <summary>
/// Request to spawn a VFX effect.
/// </summary>
internal readonly struct VfxRequest
{
    public string EffectName { get; }
    public Vector2 Position { get; }
    public VfxType Type { get; }
    public Color? ColorTint { get; }

    public VfxRequest(string effectName, Vector2 position, VfxType type = VfxType.Impact, Color? colorTint = null)
    {
        EffectName = effectName;
        Position = position;
        Type = type;
        ColorTint = colorTint;
    }
}

/// <summary>
/// Type of VFX effect.
/// </summary>
internal enum VfxType
{
    Impact,
    WindupFlash,
    ProjectileTrail,
    MuzzleFlash,
    DashTrail,
    DashEnd
}

/// <summary>
/// Active VFX particle/sprite instance.
/// </summary>
internal struct ActiveVfx
{
    public string EffectName { get; }
    public VfxType Type { get; }
    public float Lifetime { get; }
    public float RemainingTime { get; set; }
    public Color Color { get; set; }
    public float Scale { get; set; }

    public ActiveVfx(string effectName, VfxType type, float lifetime, Color color, float scale = 1f)
    {
        EffectName = effectName;
        Type = type;
        Lifetime = lifetime;
        RemainingTime = lifetime;
        Color = color;
        Scale = scale;
    }
}
