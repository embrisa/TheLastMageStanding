using Microsoft.Xna.Framework;
using TheLastMageStanding.Game.Core.Ecs;

namespace TheLastMageStanding.Game.Core.Events;

/// <summary>
/// Event fired when two colliders start overlapping.
/// </summary>
internal readonly struct CollisionEnterEvent
{
    public CollisionEnterEvent(Entity entityA, Entity entityB, Vector2 contactPoint, Vector2 normal)
    {
        EntityA = entityA;
        EntityB = entityB;
        ContactPoint = contactPoint;
        Normal = normal;
    }

    /// <summary>
    /// First entity in collision (lower entity ID).
    /// </summary>
    public Entity EntityA { get; }
    
    /// <summary>
    /// Second entity in collision (higher entity ID).
    /// </summary>
    public Entity EntityB { get; }
    
    /// <summary>
    /// Approximate point of contact in world space.
    /// </summary>
    public Vector2 ContactPoint { get; }
    
    /// <summary>
    /// Normal vector pointing from EntityA to EntityB.
    /// </summary>
    public Vector2 Normal { get; }
}

/// <summary>
/// Event fired when two colliders continue overlapping.
/// </summary>
internal readonly struct CollisionStayEvent
{
    public CollisionStayEvent(Entity entityA, Entity entityB, Vector2 contactPoint, Vector2 normal)
    {
        EntityA = entityA;
        EntityB = entityB;
        ContactPoint = contactPoint;
        Normal = normal;
    }

    public Entity EntityA { get; }
    public Entity EntityB { get; }
    public Vector2 ContactPoint { get; }
    public Vector2 Normal { get; }
}

/// <summary>
/// Event fired when two colliders stop overlapping.
/// </summary>
internal readonly struct CollisionExitEvent
{
    public CollisionExitEvent(Entity entityA, Entity entityB)
    {
        EntityA = entityA;
        EntityB = entityB;
    }

    public Entity EntityA { get; }
    public Entity EntityB { get; }
}
