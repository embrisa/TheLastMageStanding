using TheLastMageStanding.Game.Core.Ecs;
using Microsoft.Xna.Framework;

namespace TheLastMageStanding.Game.Core.Skills;

/// <summary>
/// Event published when a player requests to cast a skill.
/// </summary>
internal readonly struct SkillCastRequestEvent
{
    public Entity Caster { get; }
    public SkillId SkillId { get; }
    public Vector2 TargetPosition { get; }
    public Vector2 Direction { get; }

    public SkillCastRequestEvent(Entity caster, SkillId skillId, Vector2 targetPosition, Vector2 direction)
    {
        Caster = caster;
        SkillId = skillId;
        TargetPosition = targetPosition;
        Direction = direction;
    }
}

/// <summary>
/// Event published when a skill cast begins (after validation).
/// </summary>
internal readonly struct SkillCastStartedEvent
{
    public Entity Caster { get; }
    public SkillId SkillId { get; }
    public float CastTime { get; }

    public SkillCastStartedEvent(Entity caster, SkillId skillId, float castTime)
    {
        Caster = caster;
        SkillId = skillId;
        CastTime = castTime;
    }
}

/// <summary>
/// Event published when a skill cast completes and should execute.
/// </summary>
internal readonly struct SkillCastCompletedEvent
{
    public Entity Caster { get; }
    public SkillId SkillId { get; }
    public Vector2 CasterPosition { get; }
    public Vector2 TargetPosition { get; }
    public Vector2 Direction { get; }

    public SkillCastCompletedEvent(
        Entity caster, 
        SkillId skillId, 
        Vector2 casterPosition,
        Vector2 targetPosition,
        Vector2 direction)
    {
        Caster = caster;
        SkillId = skillId;
        CasterPosition = casterPosition;
        TargetPosition = targetPosition;
        Direction = direction;
    }
}

/// <summary>
/// Event published when a skill cast is interrupted or cancelled.
/// </summary>
internal readonly struct SkillCastCancelledEvent
{
    public Entity Caster { get; }
    public SkillId SkillId { get; }
    public string Reason { get; }

    public SkillCastCancelledEvent(Entity caster, SkillId skillId, string reason)
    {
        Caster = caster;
        SkillId = skillId;
        Reason = reason;
    }
}
