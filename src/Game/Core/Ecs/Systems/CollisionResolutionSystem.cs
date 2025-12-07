using System.Collections.Generic;
using Microsoft.Xna.Framework;
using TheLastMageStanding.Game.Core.Ecs.Components;
using TheLastMageStanding.Game.Core.Ecs.Systems.Collision;

namespace TheLastMageStanding.Game.Core.Ecs.Systems;

/// <summary>
/// Resolves movement to prevent entities from passing through solid colliders.
/// This system runs AFTER MovementIntentSystem (which sets Velocity) but BEFORE MovementSystem (which applies Position).
/// </summary>
internal sealed class CollisionResolutionSystem : IUpdateSystem
{
    private readonly SpatialGrid _staticGrid = new(128f);
    private bool _staticGridDirty = true;

    public void Initialize(EcsWorld world)
    {
        world.EventBus.Subscribe<Events.SessionRestartedEvent>(_ => OnSessionRestarted());
    }

    public void Update(EcsWorld world, in EcsUpdateContext context)
    {
        // Rebuild static grid if needed
        if (_staticGridDirty)
        {
            RebuildStaticGrid(world);
            _staticGridDirty = false;
        }

        var deltaSeconds = context.DeltaSeconds;
        var positionPool = world.GetPool<Position>();
        var velocityPool = world.GetPool<Velocity>();
        var colliderPool = world.GetPool<Collider>();
        var staticPool = world.GetPool<StaticCollider>();

        // For each dynamic entity with velocity and collider
        world.ForEach<Position, Velocity, Collider>((Entity entity, ref Position pos, ref Velocity vel, ref Collider col) =>
        {
            // Skip static entities
            if (staticPool.TryGet(entity, out _))
                return;

            // Skip if not moving
            if (vel.Value.LengthSquared() < 0.0001f)
                return;

            // Calculate intended new position
            var intendedPosition = pos.Value + vel.Value * deltaSeconds;
            var intendedBounds = col.GetWorldBounds(intendedPosition);

            // Query nearby static colliders
            var nearbyStatic = _staticGrid.QueryNearby(intendedBounds);

            // Check for collisions with static world geometry
            var hasCollision = false;
            var resolvedPosition = intendedPosition;

            foreach (var staticEntityId in nearbyStatic)
            {
                var staticEntity = new Entity(staticEntityId);
                
                if (!positionPool.TryGet(staticEntity, out var staticPos) || 
                    !colliderPool.TryGet(staticEntity, out var staticCol))
                    continue;

                // Only resolve against solid colliders (not triggers)
                if (staticCol.IsTrigger)
                    continue;

                // Test collision at intended position
                var contact = CollisionDetection.TestCollision(col, intendedPosition, staticCol, staticPos.Value);

                if (contact.IsColliding)
                {
                    hasCollision = true;

                    // Resolve by sliding along the collision normal
                    // Simple approach: project velocity onto tangent plane
                    var penetration = contact.Normal * contact.Penetration;
                    resolvedPosition = intendedPosition + penetration;

                    // Also adjust velocity to slide along the surface
                    var dot = Vector2.Dot(vel.Value, contact.Normal);
                    if (dot < 0) // Moving into the surface
                    {
                        vel.Value -= contact.Normal * dot;
                    }
                }
            }

            // Update velocity if we had a collision (for slide behavior)
            if (hasCollision)
            {
                velocityPool.Set(entity, vel);
            }
        });
    }

    private void OnSessionRestarted()
    {
        _staticGridDirty = true;
    }

    private void RebuildStaticGrid(EcsWorld world)
    {
        _staticGrid.Clear();

        var positionPool = world.GetPool<Position>();
        var colliderPool = world.GetPool<Collider>();
        var staticPool = world.GetPool<StaticCollider>();

        // Index all static colliders
        world.ForEach<Position, Collider, StaticCollider>((Entity entity, ref Position pos, ref Collider col, ref StaticCollider _) =>
        {
            var bounds = col.GetWorldBounds(pos.Value);
            _staticGrid.Insert(entity.Id, bounds);
        });
    }

    /// <summary>
    /// Marks the static grid as needing a rebuild.
    /// Call this when static colliders are added/removed.
    /// </summary>
    public void MarkStaticGridDirty()
    {
        _staticGridDirty = true;
    }
}
