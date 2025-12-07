using System;
using Microsoft.Xna.Framework;
using TheLastMageStanding.Game.Core.Ecs.Components;
using TheLastMageStanding.Game.Core.Ecs.Systems.Collision;

namespace TheLastMageStanding.Game.Core.Ecs.Systems;

/// <summary>
/// Knockback system that applies impulse forces to entities and handles decay over time.
/// Knockback velocities are clamped against world colliders to prevent tunneling.
/// Runs after movement intent but before collision resolution.
/// </summary>
internal sealed class KnockbackSystem : IUpdateSystem
{
    private const float MaxKnockbackSpeed = 800f; // Clamp extreme velocities
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
        var knockbackPool = world.GetPool<Knockback>();

        // Process all entities with knockback
        world.ForEach<Knockback, Velocity>((Entity entity, ref Knockback kb, ref Velocity vel) =>
        {
            // Update time remaining
            kb.TimeRemaining -= deltaSeconds;

            if (kb.TimeRemaining <= 0f)
            {
                // Knockback expired, remove component
                knockbackPool.Remove(entity);
                return;
            }

            // Get decayed knockback velocity
            var knockbackVel = kb.GetDecayedVelocity();

            // Clamp to max speed
            var speed = knockbackVel.Length();
            if (speed > MaxKnockbackSpeed)
            {
                knockbackVel = knockbackVel / speed * MaxKnockbackSpeed;
            }

            // Apply knockback to velocity (will be processed by collision resolution)
            vel.Value += knockbackVel * deltaSeconds;

            // Clamp against world colliders if entity has position and collider
            if (positionPool.TryGet(entity, out var pos) && colliderPool.TryGet(entity, out var col))
            {
                ClampKnockbackAgainstWorld(world, entity, ref pos, ref vel, col, deltaSeconds);
            }

            // Update component
            knockbackPool.Set(entity, kb);
        });
    }

    private void ClampKnockbackAgainstWorld(EcsWorld world, Entity entity, ref Position pos, ref Velocity vel, in Collider col, float deltaSeconds)
    {
        // Skip if no velocity
        if (vel.Value.LengthSquared() < 0.0001f)
            return;

        // Calculate intended position after knockback
        var intendedPosition = pos.Value + vel.Value * deltaSeconds;
        var intendedBounds = col.GetWorldBounds(intendedPosition);

        // Query nearby static colliders
        var nearbyStatic = _staticGrid.QueryNearby(intendedBounds);

        var positionPool = world.GetPool<Position>();
        var colliderPool = world.GetPool<Collider>();

        // Check for collisions with static world geometry
        foreach (var staticEntityId in nearbyStatic)
        {
            var staticEntity = new Entity(staticEntityId);

            if (!positionPool.TryGet(staticEntity, out var staticPos) ||
                !colliderPool.TryGet(staticEntity, out var staticCol))
                continue;

            // Only check against solid colliders (not triggers)
            if (staticCol.IsTrigger)
                continue;

            // Test collision at intended position
            var contact = CollisionDetection.TestCollision(col, intendedPosition, staticCol, staticPos.Value);

            if (contact.IsColliding)
            {
                // Stop knockback in the direction of the wall
                var dot = Vector2.Dot(vel.Value, contact.Normal);
                if (dot < 0) // Moving into the surface
                {
                    // Remove velocity component in normal direction
                    vel.Value -= contact.Normal * dot;
                }
            }
        }
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

        // Add all static colliders to the grid
        world.ForEach<Position, Collider, StaticCollider>((Entity entity, ref Position pos, ref Collider col, ref StaticCollider _) =>
        {
            var bounds = col.GetWorldBounds(pos.Value);
            _staticGrid.Insert(entity.Id, bounds);
        });
    }

    /// <summary>
    /// Adds a knockback impulse to an entity. If multiple knockbacks occur in one frame,
    /// takes the strongest one (max magnitude).
    /// </summary>
    public static void ApplyKnockback(EcsWorld world, Entity entity, Vector2 impulseVelocity, float duration = 0.2f)
    {
        // Check if entity already has knockback
        if (world.TryGetComponent(entity, out Knockback existingKb))
        {
            // Take the stronger knockback (max magnitude)
            var existingSpeed = existingKb.Velocity.LengthSquared();
            var newSpeed = impulseVelocity.LengthSquared();

            if (newSpeed > existingSpeed)
            {
                world.SetComponent(entity, new Knockback(impulseVelocity, duration));
            }
        }
        else
        {
            // Add new knockback
            world.SetComponent(entity, new Knockback(impulseVelocity, duration));
        }
    }
}
