using System;
using Microsoft.Xna.Framework;
using TheLastMageStanding.Game.Core.Ecs.Components;

namespace TheLastMageStanding.Game.Core.Ecs.Systems.Collision;

/// <summary>
/// Contact information from a collision detection test.
/// </summary>
internal readonly struct ContactInfo
{
    public ContactInfo(bool isColliding, Vector2 contactPoint, Vector2 normal, float penetration)
    {
        IsColliding = isColliding;
        ContactPoint = contactPoint;
        Normal = normal;
        Penetration = penetration;
    }

    public bool IsColliding { get; }
    public Vector2 ContactPoint { get; }
    public Vector2 Normal { get; }
    public float Penetration { get; }

    public static ContactInfo NoCollision => new(false, Vector2.Zero, Vector2.Zero, 0f);
}

/// <summary>
/// Narrow-phase collision detection for precise overlap tests between colliders.
/// </summary>
internal static class CollisionDetection
{
    /// <summary>
    /// Tests if two colliders overlap and returns contact information.
    /// </summary>
    public static ContactInfo TestCollision(
        in Collider colliderA, Vector2 positionA,
        in Collider colliderB, Vector2 positionB)
    {
        var centerA = colliderA.GetWorldCenter(positionA);
        var centerB = colliderB.GetWorldCenter(positionB);

        // Early exit: check layer mask filtering
        if (!CanCollide(colliderA.Layer, colliderA.Mask, colliderB.Layer, colliderB.Mask))
        {
            return ContactInfo.NoCollision;
        }

        // Dispatch to appropriate collision test based on shape types
        return (colliderA.Shape, colliderB.Shape) switch
        {
            (ColliderShape.Circle, ColliderShape.Circle) => TestCircleCircle(centerA, colliderA.Width, centerB, colliderB.Width),
            (ColliderShape.Circle, ColliderShape.AABB) => TestCircleAABB(centerA, colliderA.Width, centerB, colliderB.Width, colliderB.Height),
            (ColliderShape.AABB, ColliderShape.Circle) => TestAABBCircle(centerA, colliderA.Width, colliderA.Height, centerB, colliderB.Width),
            (ColliderShape.AABB, ColliderShape.AABB) => TestAABBAABB(centerA, colliderA.Width, colliderA.Height, centerB, colliderB.Width, colliderB.Height),
            _ => ContactInfo.NoCollision
        };
    }

    /// <summary>
    /// Checks if two colliders can interact based on layer and mask settings.
    /// </summary>
    public static bool CanCollide(CollisionLayer layerA, CollisionLayer maskA, CollisionLayer layerB, CollisionLayer maskB)
    {
        // Check if A's layer matches B's mask OR B's layer matches A's mask
        return ((layerA & maskB) != 0) || ((layerB & maskA) != 0);
    }

    /// <summary>
    /// Circle vs Circle collision test.
    /// </summary>
    private static ContactInfo TestCircleCircle(Vector2 centerA, float radiusA, Vector2 centerB, float radiusB)
    {
        var delta = centerB - centerA;
        var distanceSquared = delta.LengthSquared();
        var radiusSum = radiusA + radiusB;
        var radiusSumSquared = radiusSum * radiusSum;

        if (distanceSquared > radiusSumSquared)
        {
            return ContactInfo.NoCollision;
        }

        var distance = MathF.Sqrt(distanceSquared);
        
        // Handle exact overlap (both circles at same position)
        if (distance < 0.0001f)
        {
            return new ContactInfo(
                isColliding: true,
                contactPoint: centerA,
                normal: Vector2.UnitX,
                penetration: radiusSum
            );
        }

        var normal = delta / distance;
        var contactPoint = centerA + normal * radiusA;
        var penetration = radiusSum - distance;

        return new ContactInfo(
            isColliding: true,
            contactPoint: contactPoint,
            normal: normal,
            penetration: penetration
        );
    }

    /// <summary>
    /// Circle vs AABB collision test.
    /// </summary>
    private static ContactInfo TestCircleAABB(Vector2 circleCenter, float radius, Vector2 aabbCenter, float halfWidth, float halfHeight)
    {
        // Find the closest point on the AABB to the circle center
        var closestX = Math.Clamp(circleCenter.X, aabbCenter.X - halfWidth, aabbCenter.X + halfWidth);
        var closestY = Math.Clamp(circleCenter.Y, aabbCenter.Y - halfHeight, aabbCenter.Y + halfHeight);
        var closest = new Vector2(closestX, closestY);

        var delta = circleCenter - closest;
        var distanceSquared = delta.LengthSquared();
        var radiusSquared = radius * radius;

        if (distanceSquared > radiusSquared)
        {
            return ContactInfo.NoCollision;
        }

        var distance = MathF.Sqrt(distanceSquared);
        
        // Circle center is inside AABB
        if (distance < 0.0001f)
        {
            // Find closest edge
            var dx = MathF.Abs(circleCenter.X - aabbCenter.X) - halfWidth;
            var dy = MathF.Abs(circleCenter.Y - aabbCenter.Y) - halfHeight;

            Vector2 normalInside;
            float penetrationInside;

            if (dx > dy)
            {
                normalInside = circleCenter.X < aabbCenter.X ? new Vector2(-1, 0) : new Vector2(1, 0);
                penetrationInside = radius + MathF.Abs(dx);
            }
            else
            {
                normalInside = circleCenter.Y < aabbCenter.Y ? new Vector2(0, -1) : new Vector2(0, 1);
                penetrationInside = radius + MathF.Abs(dy);
            }

            return new ContactInfo(
                isColliding: true,
                contactPoint: circleCenter,
                normal: normalInside,
                penetration: penetrationInside
            );
        }

        var normal = delta / distance;
        var penetration = radius - distance;

        return new ContactInfo(
            isColliding: true,
            contactPoint: closest,
            normal: normal,
            penetration: penetration
        );
    }

    /// <summary>
    /// AABB vs Circle collision test (just swaps parameters and inverts normal).
    /// </summary>
    private static ContactInfo TestAABBCircle(Vector2 aabbCenter, float halfWidth, float halfHeight, Vector2 circleCenter, float radius)
    {
        var result = TestCircleAABB(circleCenter, radius, aabbCenter, halfWidth, halfHeight);
        
        if (!result.IsColliding)
            return result;

        // Invert the normal since we swapped the parameters
        return new ContactInfo(
            isColliding: true,
            contactPoint: result.ContactPoint,
            normal: -result.Normal,
            penetration: result.Penetration
        );
    }

    /// <summary>
    /// AABB vs AABB collision test.
    /// </summary>
    private static ContactInfo TestAABBAABB(Vector2 centerA, float halfWidthA, float halfHeightA, Vector2 centerB, float halfWidthB, float halfHeightB)
    {
        // Calculate overlap on each axis
        var deltaX = centerB.X - centerA.X;
        var deltaY = centerB.Y - centerA.Y;

        var overlapX = (halfWidthA + halfWidthB) - MathF.Abs(deltaX);
        var overlapY = (halfHeightA + halfHeightB) - MathF.Abs(deltaY);

        if (overlapX <= 0f || overlapY <= 0f)
        {
            return ContactInfo.NoCollision;
        }

        // Find the axis with minimum overlap (separation axis)
        Vector2 normal;
        float penetration;
        Vector2 contactPoint;

        if (overlapX < overlapY)
        {
            // Separate along X axis
            normal = deltaX < 0 ? new Vector2(-1, 0) : new Vector2(1, 0);
            penetration = overlapX;
            contactPoint = new Vector2(
                centerA.X + (deltaX < 0 ? -halfWidthA : halfWidthA),
                centerA.Y
            );
        }
        else
        {
            // Separate along Y axis
            normal = deltaY < 0 ? new Vector2(0, -1) : new Vector2(0, 1);
            penetration = overlapY;
            contactPoint = new Vector2(
                centerA.X,
                centerA.Y + (deltaY < 0 ? -halfHeightA : halfHeightA)
            );
        }

        return new ContactInfo(
            isColliding: true,
            contactPoint: contactPoint,
            normal: normal,
            penetration: penetration
        );
    }
}
