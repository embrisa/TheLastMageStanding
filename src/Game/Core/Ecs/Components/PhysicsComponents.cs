using Microsoft.Xna.Framework;

namespace TheLastMageStanding.Game.Core.Ecs.Components;

/// <summary>
/// Mass/weight component for determining separation priority during dynamic collisions.
/// Lighter entities are pushed more than heavier ones.
/// </summary>
internal struct Mass
{
    public Mass(float value)
    {
        Value = value;
    }

    /// <summary>
    /// Mass value. Standard values: player=1.0, small enemy=0.5, large enemy=2.0.
    /// </summary>
    public float Value { get; set; }
}

/// <summary>
/// Knockback impulse applied to an entity, typically from attacks or projectile impacts.
/// Decays over time and respects world collisions.
/// </summary>
internal struct Knockback
{
    public Knockback(Vector2 velocity, float duration = 0.2f)
    {
        Velocity = velocity;
        Duration = duration;
        TimeRemaining = duration;
    }

    /// <summary>
    /// Current knockback velocity vector.
    /// </summary>
    public Vector2 Velocity { get; set; }

    /// <summary>
    /// Total duration of the knockback effect.
    /// </summary>
    public float Duration { get; set; }

    /// <summary>
    /// Time remaining for this knockback effect.
    /// </summary>
    public float TimeRemaining { get; set; }

    /// <summary>
    /// Returns the current knockback velocity with decay applied.
    /// </summary>
    public readonly Vector2 GetDecayedVelocity()
    {
        if (TimeRemaining <= 0f || Duration <= 0f)
            return Vector2.Zero;

        var decay = TimeRemaining / Duration;
        return Velocity * decay;
    }
}

/// <summary>
/// Tracks when an entity was last damaged by another entity to prevent rapid repeated hits.
/// </summary>
internal struct ContactDamageCooldown
{
    public ContactDamageCooldown(float cooldownSeconds = 0.5f)
    {
        CooldownSeconds = cooldownSeconds;
        LastDamageTime = 0f;
        LastDamageFrom = -1;
    }

    /// <summary>
    /// Cooldown duration between taking damage from the same entity.
    /// </summary>
    public float CooldownSeconds { get; set; }

    /// <summary>
    /// Game time when last damage was taken.
    /// </summary>
    public float LastDamageTime { get; set; }

    /// <summary>
    /// Entity ID that last dealt damage.
    /// </summary>
    public int LastDamageFrom { get; set; }

    /// <summary>
    /// Checks if enough time has passed to take damage again from the specified entity.
    /// </summary>
    public readonly bool CanTakeDamageFrom(int entityId, float currentTime)
    {
        if (entityId != LastDamageFrom)
            return true;

        return (currentTime - LastDamageTime) >= CooldownSeconds;
    }

    /// <summary>
    /// Records that damage was taken from the specified entity.
    /// </summary>
    public void RecordDamage(int entityId, float currentTime)
    {
        LastDamageFrom = entityId;
        LastDamageTime = currentTime;
    }
}
