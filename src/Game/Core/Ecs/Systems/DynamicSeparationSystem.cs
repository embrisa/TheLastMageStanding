using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using TheLastMageStanding.Game.Core.Ecs.Components;
using TheLastMageStanding.Game.Core.Ecs.Systems.Collision;
using TheLastMageStanding.Game.Core.Events;

namespace TheLastMageStanding.Game.Core.Ecs.Systems;

/// <summary>
/// Dynamic actor separation system that prevents entities from stacking/overlapping.
/// Runs after collision detection but before movement application.
/// Uses soft separation forces to gradually push overlapping entities apart.
/// </summary>
internal sealed class DynamicSeparationSystem : IUpdateSystem
{
    private const int MaxSeparationIterations = 3;
    private const float MinSeparationVelocity = 0.1f;

    private readonly HashSet<(int, int)> _processedPairs = new();
    private readonly List<(Entity entityA, Entity entityB, ContactInfo contact)> _dynamicCollisions = new();

    public void Initialize(EcsWorld world)
    {
        // Subscribe to collision events to track dynamic vs dynamic collisions
        world.EventBus.Subscribe<CollisionEnterEvent>(OnCollisionEnter);
        world.EventBus.Subscribe<CollisionStayEvent>(OnCollisionStay);
    }

    public void Update(EcsWorld world, in EcsUpdateContext context)
    {
        var deltaSeconds = context.DeltaSeconds;

        // Process all dynamic collisions with iterative separation
        for (int iteration = 0; iteration < MaxSeparationIterations; iteration++)
        {
            if (_dynamicCollisions.Count == 0)
                break;

            _processedPairs.Clear();

            foreach (var (entityA, entityB, contact) in _dynamicCollisions)
            {
                // Skip if pair already processed this iteration
                var pairKey = entityA.Id < entityB.Id ? (entityA.Id, entityB.Id) : (entityB.Id, entityA.Id);
                if (_processedPairs.Contains(pairKey))
                    continue;

                _processedPairs.Add(pairKey);

                // Apply separation
                ApplySeparation(world, entityA, entityB, contact, deltaSeconds);
            }
        }

        // Clear collision list for next frame
        _dynamicCollisions.Clear();
    }

    private void OnCollisionEnter(CollisionEnterEvent evt)
    {
        TryAddDynamicCollision(evt.EntityA, evt.EntityB, 
            new ContactInfo(true, evt.ContactPoint, evt.Normal, 0f));
    }

    private void OnCollisionStay(CollisionStayEvent evt)
    {
        TryAddDynamicCollision(evt.EntityA, evt.EntityB,
            new ContactInfo(true, evt.ContactPoint, evt.Normal, 0f));
    }

    private void TryAddDynamicCollision(Entity entityA, Entity entityB, ContactInfo contact)
    {
        // Only track dynamic vs dynamic collisions (both have velocity and non-trigger colliders)
        if (!IsDynamicEntity(entityA) || !IsDynamicEntity(entityB))
            return;

        _dynamicCollisions.Add((entityA, entityB, contact));
    }

    private static bool IsDynamicEntity(Entity entity)
    {
        // Assume entity is dynamic if we received collision event
        // The collision system already filters for us based on layers and masks
        return true;
    }

    private static void ApplySeparation(EcsWorld world, Entity entityA, Entity entityB, ContactInfo contact, float deltaSeconds)
    {
        // Get required components
        if (!world.TryGetComponent(entityA, out Position posA) || !world.TryGetComponent(entityA, out Collider colA))
            return;
        if (!world.TryGetComponent(entityB, out Position posB) || !world.TryGetComponent(entityB, out Collider colB))
            return;

        // Skip triggers
        if (colA.IsTrigger || colB.IsTrigger)
            return;

        // Skip static entities
        var staticPool = world.GetPool<StaticCollider>();
        if (staticPool.TryGet(entityA, out _) || staticPool.TryGet(entityB, out _))
            return;

        // Get masses (default to 1.0 if not present)
        var massA = world.TryGetComponent(entityA, out Mass mA) ? mA.Value : 1.0f;
        var massB = world.TryGetComponent(entityB, out Mass mB) ? mB.Value : 1.0f;

        // Re-test collision to get accurate penetration depth
        var testContact = CollisionDetection.TestCollision(colA, posA.Value, colB, posB.Value);
        
        if (!testContact.IsColliding || testContact.Penetration < 0.001f)
            return;

        // Calculate separation based on mass ratio
        var totalMass = massA + massB;
        var massRatioA = massB / totalMass; // A gets pushed proportional to B's mass
        var massRatioB = massA / totalMass; // B gets pushed proportional to A's mass

        // Separation vector (push apart along normal)
        var separationVector = testContact.Normal * testContact.Penetration;

        // Apply soft separation as velocity impulse rather than hard position correction
        var separationStrength = 10.0f; // Tunable parameter
        var separationVelocityA = -separationVector * massRatioA * separationStrength;
        var separationVelocityB = separationVector * massRatioB * separationStrength;

        // Only apply if magnitude is significant
        if (separationVelocityA.LengthSquared() > MinSeparationVelocity * MinSeparationVelocity)
        {
            if (world.TryGetComponent(entityA, out Velocity velA))
            {
                velA.Value += separationVelocityA * deltaSeconds;
                world.SetComponent(entityA, velA);
            }
        }

        if (separationVelocityB.LengthSquared() > MinSeparationVelocity * MinSeparationVelocity)
        {
            if (world.TryGetComponent(entityB, out Velocity velB))
            {
                velB.Value += separationVelocityB * deltaSeconds;
                world.SetComponent(entityB, velB);
            }
        }
    }
}
