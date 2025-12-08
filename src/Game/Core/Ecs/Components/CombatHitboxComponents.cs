using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace TheLastMageStanding.Game.Core.Ecs.Components;

/// <summary>
/// Marks an entity as a transient attack hitbox that deals damage on collision.
/// Typically created for a few frames during attack animations.
/// </summary>
internal struct AttackHitbox
{
    public AttackHitbox(
        Entity owner,
        float damage,
        Faction ownerFaction,
        float lifetimeSeconds = 0.1f,
        StatusEffectData? statusEffect = null)
    {
        Owner = owner;
        Damage = damage;
        OwnerFaction = ownerFaction;
        LifetimeRemaining = lifetimeSeconds;
        AlreadyHit = new HashSet<int>();
        StatusEffect = statusEffect;
    }

    /// <summary>
    /// The entity that created this hitbox (for attribution and self-hit prevention).
    /// </summary>
    public Entity Owner { get; set; }

    /// <summary>
    /// How much damage this hitbox deals.
    /// </summary>
    public float Damage { get; set; }

    /// <summary>
    /// Faction of the owner (for filtering targets).
    /// </summary>
    public Faction OwnerFaction { get; set; }

    /// <summary>
    /// How long this hitbox remains active (in seconds).
    /// </summary>
    public float LifetimeRemaining { get; set; }

    /// <summary>
    /// Tracks entities that have already been hit by this hitbox instance
    /// to prevent multi-hitting the same target.
    /// </summary>
    public HashSet<int> AlreadyHit { get; set; }

    /// <summary>
    /// Optional status effect to apply when this hitbox deals damage.
    /// </summary>
    public StatusEffectData? StatusEffect { get; set; }
}

/// <summary>
/// Marks an entity as able to take damage from attack hitboxes.
/// This is the "hurtbox" - what can be hit.
/// </summary>
internal struct Hurtbox
{
    /// <summary>
    /// Invulnerability frames - if true, this entity cannot take damage temporarily.
    /// </summary>
    public bool IsInvulnerable { get; set; }

    /// <summary>
    /// When invulnerability expires (game time).
    /// </summary>
    public float InvulnerabilityEndsAt { get; set; }
}

/// <summary>
/// Configuration for spawning attack hitboxes, typically attached to the attacker.
/// </summary>
internal struct MeleeAttackConfig
{
    public MeleeAttackConfig(float hitboxRadius, Vector2 hitboxOffset, float duration = 0.15f)
    {
        HitboxRadius = hitboxRadius;
        HitboxOffset = hitboxOffset;
        Duration = duration;
    }

    /// <summary>
    /// Radius of the hitbox circle.
    /// </summary>
    public float HitboxRadius { get; set; }

    /// <summary>
    /// Offset from attacker position (for directional attacks).
    /// </summary>
    public Vector2 HitboxOffset { get; set; }

    /// <summary>
    /// How long the hitbox stays active.
    /// </summary>
    public float Duration { get; set; }
}

/// <summary>
/// Temporary projectile shield applied by protector enemies.
/// </summary>
internal struct ShieldActive
{
    public ShieldActive(bool isActive, int blocksRemaining, float durationSeconds, Entity source)
    {
        IsActive = isActive;
        BlocksRemaining = blocksRemaining;
        RemainingDuration = durationSeconds;
        Source = source;
    }

    public bool IsActive { get; set; }
    public int BlocksRemaining { get; set; }
    public float RemainingDuration { get; set; }
    public Entity Source { get; set; }
}
