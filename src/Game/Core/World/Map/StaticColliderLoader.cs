using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using MonoGame.Extended.Tiled;
using TheLastMageStanding.Game.Core.Ecs;
using TheLastMageStanding.Game.Core.Ecs.Components;

namespace TheLastMageStanding.Game.Core.World.Map;

/// <summary>
/// Loads TMX collision/object layers and creates static collider entities in the ECS.
/// </summary>
internal static class StaticColliderLoader
{
    /// <summary>
    /// Parses collision regions from TMX map object layers and creates static collider entities.
    /// Filters objects by type="collision_region" to avoid parsing decorative objects.
    /// </summary>
    public static void LoadCollisionRegions(TiledMap map, EcsWorld world)
    {
        if (map.ObjectLayers is null)
        {
            return;
        }

        var collisionCount = 0;
        
        foreach (var layer in map.ObjectLayers)
        {
            foreach (var obj in layer.Objects)
            {
                // Filter by collision type to avoid spawning colliders for NPCs, spawn points, etc.
                if (!IsCollisionObject(obj))
                {
                    continue;
                }

                // Convert TMX object to collider entity
                if (TryCreateColliderFromObject(obj, world))
                {
                    collisionCount++;
                }
            }
        }

        Console.WriteLine($"[StaticColliderLoader] Loaded {collisionCount} static collision regions");
    }

    private static bool IsCollisionObject(TiledMapObject obj)
    {
        // Check type property
        if (string.Equals(obj.Type, "collision_region", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        // Also check if the name contains "collision" or "wall" as fallback
        if (obj.Name != null && 
            (obj.Name.Contains("collision", StringComparison.OrdinalIgnoreCase) ||
             obj.Name.Contains("wall", StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        return false;
    }

    private static bool TryCreateColliderFromObject(TiledMapObject obj, EcsWorld world)
    {
        // TMX coordinates: top-left origin, Y-down
        // MonoGame/game logic: center-based positioning, Y-down in screen space
        
        if (obj.Size.Width <= 0 || obj.Size.Height <= 0)
        {
            // Skip zero-size or point objects
            Console.WriteLine($"[StaticColliderLoader] Skipping zero-size object '{obj.Name}' at ({obj.Position.X:F1}, {obj.Position.Y:F1})");
            return false;
        }

        // Only handle rectangular collision regions for now
        // If MonoGame.Extended exposes polygon data, we could add support later
        // For now, log non-rectangular objects and skip them
        if (obj.Type != null && obj.Type.Contains("ellipse", StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine($"[StaticColliderLoader] Skipping ellipse object '{obj.Name}' at ({obj.Position.X:F1}, {obj.Position.Y:F1}) - use AABB collision_region instead");
            return false;
        }

        // Handle rectangles (the common case)
        var position = TmxToWorld(obj.Position, obj.Size);
        var halfWidth = obj.Size.Width * 0.5f;
        var halfHeight = obj.Size.Height * 0.5f;

        // Create static collider entity
        var entity = world.CreateEntity();
        world.SetComponent(entity, new Position(position));
        world.SetComponent(entity, Collider.CreateAABB(
            halfWidth,
            halfHeight,
            CollisionLayer.WorldStatic,
            CollisionLayer.Player | CollisionLayer.Enemy | CollisionLayer.Projectile,
            isTrigger: false
        ));
        world.SetComponent(entity, new StaticCollider());

        return true;
    }

    /// <summary>
    /// Converts TMX position (top-left corner) to world center position.
    /// TMX uses top-left origin with Y-down, we need the center of the object.
    /// </summary>
    private static Vector2 TmxToWorld(Vector2 topLeft, MonoGame.Extended.SizeF size)
    {
        // TMX gives us the top-left corner, convert to center
        return new Vector2(
            topLeft.X + size.Width * 0.5f,
            topLeft.Y + size.Height * 0.5f
        );
    }
}
