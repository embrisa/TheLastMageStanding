using Microsoft.Xna.Framework;

namespace TheLastMageStanding.Game.Core.Ecs.Components;

/// <summary>
/// Marks an entity as a projectile that travels in a straight line and deals damage on impact.
/// </summary>
internal struct Projectile
{
    public Projectile(
        Entity source,
        float damage,
        Faction sourceFaction,
        float lifetimeSeconds = 5f,
        StatusEffectData? statusEffect = null)
    {
        Source = source;
        Damage = damage;
        SourceFaction = sourceFaction;
        MaxLifetime = lifetimeSeconds;
        LifetimeRemaining = lifetimeSeconds;
        HasHit = false;
        StatusEffect = statusEffect;
    }

    /// <summary>
    /// The entity that fired this projectile (for attribution).
    /// </summary>
    public Entity Source { get; set; }

    /// <summary>
    /// How much damage this projectile deals on hit.
    /// </summary>
    public float Damage { get; set; }

    /// <summary>
    /// Faction of the source entity (for filtering targets).
    /// </summary>
    public Faction SourceFaction { get; set; }

    /// <summary>
    /// Maximum lifetime in seconds.
    /// </summary>
    public float MaxLifetime { get; set; }

    /// <summary>
    /// How long this projectile remains active (in seconds).
    /// Projectile is destroyed when this reaches zero.
    /// </summary>
    public float LifetimeRemaining { get; set; }

    /// <summary>
    /// Whether this projectile has already hit a target.
    /// Once true, the projectile should be destroyed.
    /// </summary>
    public bool HasHit { get; set; }

    /// <summary>
    /// Optional status effect applied when the projectile hits.
    /// </summary>
    public StatusEffectData? StatusEffect { get; set; }
}

/// <summary>
/// Visual representation settings for projectiles.
/// </summary>
internal struct ProjectileVisual
{
    public ProjectileVisual(Color color, float radius = 4f)
    {
        Color = color;
        Radius = radius;
    }

    /// <summary>
    /// Color of the projectile.
    /// </summary>
    public Color Color { get; set; }

    /// <summary>
    /// Radius for rendering the projectile.
    /// </summary>
    public float Radius { get; set; }
}
