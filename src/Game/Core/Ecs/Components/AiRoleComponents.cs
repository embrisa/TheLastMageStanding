using Microsoft.Xna.Framework;

namespace TheLastMageStanding.Game.Core.Ecs.Components;

/// <summary>
/// Enemy role used to drive specialized AI behaviors.
/// </summary>
internal enum EnemyRole
{
    Melee = 0,
    Ranged = 1,
    Charger = 2,
    Protector = 3,
    Buffer = 4
}

/// <summary>
/// High level behavior states for AI-controlled enemies.
/// </summary>
internal enum AiBehaviorState
{
    Idle = 0,
    Seeking = 1,
    Committing = 2,
    Buffing = 3,
    Shielding = 4,
    Cooldown = 5
}

/// <summary>
/// Static configuration for an AI role. Values are tuned per-archetype.
/// </summary>
internal readonly record struct AiRoleConfig(
    EnemyRole Role,
    float CommitRangeMin = 0f,
    float CommitRangeMax = 0f,
    float WindupDuration = 0f,
    float CooldownDuration = 0f,
    float KnockbackForce = 0f,
    float ShieldRange = 0f,
    float ShieldDuration = 0f,
    float ShieldDetectionRange = 0f,
    bool ShieldBlocksProjectiles = false,
    float BuffRange = 0f,
    float BuffDuration = 0f,
    BuffType BuffType = BuffType.None,
    StatModifiers BuffModifiers = default,
    float BuffAnimationLock = 0.5f,
    TelegraphData? Telegraph = null)
{
    public bool HasTelegraph => Telegraph.HasValue;
}

/// <summary>
/// Runtime state machine data for AI roles.
/// </summary>
internal struct AiBehaviorStateMachine
{
    public AiBehaviorState State { get; set; }
    public float StateTimer { get; set; }
    public float CooldownTimer { get; set; }
    public float PerceptionTimer { get; set; }
    public Entity TargetEntity { get; set; }
    public bool HasTarget { get; set; }
    public Vector2 AimDirection { get; set; }
    public float PreviousMass { get; set; }
}

/// <summary>
/// Payload for commitment-style attacks (chargers).
/// </summary>
internal struct CommitAttackData
{
    public float WindupDuration { get; set; }
    public TelegraphData Telegraph { get; set; }
    public float KnockbackForce { get; set; }
    public float CommitRangeMin { get; set; }
    public float CommitRangeMax { get; set; }
    public float HitboxRadius { get; set; }
    public float AttackOffset { get; set; }
    public float CooldownDuration { get; set; }
}

