using Microsoft.Xna.Framework;

namespace TheLastMageStanding.Game.Core.Ecs.Components;

/// <summary>
/// Collision layer flags. Each object has one or more layers assigned.
/// Use bitwise operations to combine layers.
/// </summary>
[Flags]
internal enum CollisionLayer : uint
{
    None = 0,
    Player = 1 << 0,        // 0x00000001
    Enemy = 1 << 1,         // 0x00000002
    Projectile = 1 << 2,    // 0x00000004
    WorldStatic = 1 << 3,   // 0x00000008
    Pickup = 1 << 4,        // 0x00000010
    // Add more layers as needed, up to 32 layers
}

/// <summary>
/// Shape type for collision detection.
/// </summary>
internal enum ColliderShape
{
    Circle,
    AABB,  // Axis-Aligned Bounding Box
}

/// <summary>
/// Collider component defines the collision shape and behavior for an entity.
/// </summary>
internal struct Collider
{
    public ColliderShape Shape { get; set; }
    
    /// <summary>
    /// For Circle: radius. For AABB: half-width.
    /// </summary>
    public float Width { get; set; }
    
    /// <summary>
    /// For AABB: half-height. Ignored for Circle.
    /// </summary>
    public float Height { get; set; }
    
    /// <summary>
    /// The layer(s) this collider belongs to.
    /// </summary>
    public CollisionLayer Layer { get; set; }
    
    /// <summary>
    /// Which layers this collider can interact with (layer mask filter).
    /// </summary>
    public CollisionLayer Mask { get; set; }
    
    /// <summary>
    /// If true, this is a trigger (no physical blocking, only events).
    /// If false, this is a solid collider (can block movement).
    /// </summary>
    public bool IsTrigger { get; set; }
    
    /// <summary>
    /// Offset from entity position.
    /// </summary>
    public Vector2 Offset { get; set; }

    public static Collider CreateCircle(float radius, CollisionLayer layer, CollisionLayer mask, bool isTrigger = false, Vector2 offset = default)
    {
        return new Collider
        {
            Shape = ColliderShape.Circle,
            Width = radius,
            Height = 0f,
            Layer = layer,
            Mask = mask,
            IsTrigger = isTrigger,
            Offset = offset
        };
    }

    public static Collider CreateAABB(float halfWidth, float halfHeight, CollisionLayer layer, CollisionLayer mask, bool isTrigger = false, Vector2 offset = default)
    {
        return new Collider
        {
            Shape = ColliderShape.AABB,
            Width = halfWidth,
            Height = halfHeight,
            Layer = layer,
            Mask = mask,
            IsTrigger = isTrigger,
            Offset = offset
        };
    }
    
    /// <summary>
    /// Gets the world-space bounds for this collider at the given position.
    /// </summary>
    public Rectangle GetWorldBounds(Vector2 position)
    {
        var center = position + Offset;
        
        if (Shape == ColliderShape.Circle)
        {
            var radius = Width;
            return new Rectangle(
                (int)(center.X - radius),
                (int)(center.Y - radius),
                (int)(radius * 2),
                (int)(radius * 2)
            );
        }
        else // AABB
        {
            return new Rectangle(
                (int)(center.X - Width),
                (int)(center.Y - Height),
                (int)(Width * 2),
                (int)(Height * 2)
            );
        }
    }
    
    /// <summary>
    /// Gets the world-space center for this collider.
    /// </summary>
    public Vector2 GetWorldCenter(Vector2 position) => position + Offset;
}

/// <summary>
/// Marks an entity as static (doesn't move) for collision optimization.
/// Static colliders are indexed once and don't need to be updated every frame.
/// </summary>
internal struct StaticCollider
{
}
