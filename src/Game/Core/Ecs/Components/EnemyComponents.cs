using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TheLastMageStanding.Game.Core.Ecs.Config;

namespace TheLastMageStanding.Game.Core.Ecs.Components;

internal struct AiSeekTarget
{
    public AiSeekTarget(Faction targetFaction)
    {
        TargetFaction = targetFaction;
    }

    public Faction TargetFaction { get; set; }
}

internal enum EnemyAnimationClip
{
    Idle = 0,
    Run = 1,
}

internal readonly record struct EnemySpriteAssets(string IdleAsset, string RunAsset);

internal readonly record struct EnemyVisual(Vector2 Origin, float Scale, int FrameSize, Color Tint);

internal struct EnemySpriteSet
{
    public SpriteAnimation Idle;
    public SpriteAnimation Run;
}

internal struct EnemyAnimationState
{
    public PlayerFacingDirection Facing { get; set; }
    public EnemyAnimationClip ActiveClip { get; set; }
    public float Timer { get; set; }
    public int FrameIndex { get; set; }
    public bool IsMoving { get; set; }
}

internal readonly record struct EnemySpawnRequest(Vector2 Position, EnemyArchetype Archetype);

/// <summary>
/// Marks an enemy as a ranged attacker that fires projectiles.
/// Enemy will stop at a certain distance and fire periodically.
/// </summary>
internal struct RangedAttacker
{
    public RangedAttacker(float projectileSpeed, float projectileDamage, float optimalRange, float windupSeconds = 0.5f)
    {
        ProjectileSpeed = projectileSpeed;
        ProjectileDamage = projectileDamage;
        OptimalRange = optimalRange;
        WindupSeconds = windupSeconds;
        WindupTimer = 0f;
        IsWindingUp = false;
    }

    /// <summary>
    /// Speed of projectiles fired by this enemy.
    /// </summary>
    public float ProjectileSpeed { get; set; }

    /// <summary>
    /// Damage dealt by each projectile.
    /// </summary>
    public float ProjectileDamage { get; set; }

    /// <summary>
    /// Preferred distance to maintain from target before firing.
    /// </summary>
    public float OptimalRange { get; set; }

    /// <summary>
    /// Duration of windup/telegraph before firing.
    /// </summary>
    public float WindupSeconds { get; set; }

    /// <summary>
    /// Current windup timer.
    /// </summary>
    public float WindupTimer { get; set; }

    /// <summary>
    /// Whether the enemy is currently winding up to fire.
    /// </summary>
    public bool IsWindingUp { get; set; }
}
