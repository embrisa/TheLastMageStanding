using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using TheLastMageStanding.Game.Core.Ecs.Components;
using TheLastMageStanding.Game.Core.Events;

namespace TheLastMageStanding.Game.Core.Ecs.Systems.Collision;

/// <summary>
/// Core collision system that manages broadphase, narrow-phase, and event emission.
/// Tracks collision pairs across frames to emit Enter/Stay/Exit events.
/// </summary>
internal sealed class CollisionSystem : IUpdateSystem
{
    private readonly SpatialGrid _dynamicGrid = new(128f);
    private readonly SpatialGrid _staticGrid = new(128f);
    
    // Track collision pairs from previous frame to detect Enter/Exit
    private HashSet<(int entityA, int entityB)> _previousCollisions = new();
    private HashSet<(int entityA, int entityB)> _currentCollisions = new();
    
    private bool _staticGridDirty = true;

    public void Initialize(EcsWorld world)
    {
        // Subscribe to relevant events if needed
        world.EventBus.Subscribe<SessionRestartedEvent>(_ => OnSessionRestarted());
    }

    public void Update(EcsWorld world, in EcsUpdateContext context)
    {
        // Rebuild static grid if needed (only once or when marked dirty)
        if (_staticGridDirty)
        {
            RebuildStaticGrid(world);
            _staticGridDirty = false;
        }

        // Rebuild dynamic grid every frame
        RebuildDynamicGrid(world);

        // Detect collisions and emit events
        DetectCollisions(world);
    }

    private void OnSessionRestarted()
    {
        _staticGridDirty = true;
        _previousCollisions.Clear();
        _currentCollisions.Clear();
    }

    private void RebuildStaticGrid(EcsWorld world)
    {
        _staticGrid.Clear();

        var positionPool = world.GetPool<Position>();
        var colliderPool = world.GetPool<Collider>();
        var staticPool = world.GetPool<StaticCollider>();

        // Index all static colliders
        world.ForEach<Position, Collider, StaticCollider>((Entity entity, ref Position pos, ref Collider col, ref StaticCollider staticTag) =>
        {
            var bounds = col.GetWorldBounds(pos.Value);
            _staticGrid.Insert(entity.Id, bounds);
        });
    }

    private void RebuildDynamicGrid(EcsWorld world)
    {
        _dynamicGrid.Clear();

        // Index all dynamic colliders (entities with Collider but without StaticCollider)
        var colliderPool = world.GetPool<Collider>();
        var positionPool = world.GetPool<Position>();
        var staticPool = world.GetPool<StaticCollider>();

        world.ForEach<Position, Collider>((Entity entity, ref Position pos, ref Collider col) =>
        {
            // Skip if this entity is static
            if (staticPool.TryGet(entity, out _))
                return;

            var bounds = col.GetWorldBounds(pos.Value);
            _dynamicGrid.Insert(entity.Id, bounds);
        });
    }

    private void DetectCollisions(EcsWorld world)
    {
        // Swap current/previous collision sets
        (_previousCollisions, _currentCollisions) = (_currentCollisions, _previousCollisions);
        _currentCollisions.Clear();

        var positionPool = world.GetPool<Position>();
        var colliderPool = world.GetPool<Collider>();

        // Get potential collision pairs from both grids
        var dynamicPairs = _dynamicGrid.QueryPotentialPairs();
        var staticPairs = QueryDynamicVsStaticPairs(world);

        // Combine all potential pairs
        var allPairs = new HashSet<(int entityA, int entityB)>(dynamicPairs);
        foreach (var pair in staticPairs)
        {
            allPairs.Add(pair);
        }

        // Test each potential pair with narrow-phase
        foreach (var (entityIdA, entityIdB) in allPairs)
        {
            var entityA = new Entity(entityIdA);
            var entityB = new Entity(entityIdB);

            // Skip if either entity is dead
            if (!world.IsAlive(entityA) || !world.IsAlive(entityB))
                continue;

            // Get components
            if (!positionPool.TryGet(entityA, out var posA) || !colliderPool.TryGet(entityA, out var colA))
                continue;
            if (!positionPool.TryGet(entityB, out var posB) || !colliderPool.TryGet(entityB, out var colB))
                continue;

            // Narrow-phase collision test
            var contact = CollisionDetection.TestCollision(colA, posA.Value, colB, posB.Value);

            if (contact.IsColliding)
            {
                var pair = (entityIdA, entityIdB);
                _currentCollisions.Add(pair);

                // Emit appropriate collision event
                if (_previousCollisions.Contains(pair))
                {
                    // Collision is ongoing (Stay)
                    world.EventBus.Publish(new CollisionStayEvent(entityA, entityB, contact.ContactPoint, contact.Normal));
                }
                else
                {
                    // New collision (Enter)
                    world.EventBus.Publish(new CollisionEnterEvent(entityA, entityB, contact.ContactPoint, contact.Normal));
                }
            }
        }

        // Emit Exit events for collisions that ended
        foreach (var pair in _previousCollisions)
        {
            if (!_currentCollisions.Contains(pair))
            {
                var entityA = new Entity(pair.entityA);
                var entityB = new Entity(pair.entityB);
                
                // Only emit if both entities are still alive
                if (world.IsAlive(entityA) && world.IsAlive(entityB))
                {
                    world.EventBus.Publish(new CollisionExitEvent(entityA, entityB));
                }
            }
        }
    }

    private HashSet<(int entityA, int entityB)> QueryDynamicVsStaticPairs(EcsWorld world)
    {
        var pairs = new HashSet<(int entityA, int entityB)>();
        var colliderPool = world.GetPool<Collider>();
        var positionPool = world.GetPool<Position>();
        var staticPool = world.GetPool<StaticCollider>();

        // For each dynamic collider, query nearby static colliders
        world.ForEach<Position, Collider>((Entity entity, ref Position pos, ref Collider col) =>
        {
            // Skip static colliders
            if (staticPool.TryGet(entity, out _))
                return;

            var bounds = col.GetWorldBounds(pos.Value);
            var nearbyStatic = _staticGrid.QueryNearby(bounds);

            foreach (var staticEntityId in nearbyStatic)
            {
                var dynamicId = entity.Id;
                var staticId = staticEntityId;

                // Ensure consistent ordering (lower ID first)
                if (dynamicId > staticId)
                    (dynamicId, staticId) = (staticId, dynamicId);

                pairs.Add((dynamicId, staticId));
            }
        });

        return pairs;
    }

    /// <summary>
    /// Marks the static grid as needing a rebuild.
    /// Call this when static colliders are added/removed/moved.
    /// </summary>
    public void MarkStaticGridDirty()
    {
        _staticGridDirty = true;
    }
}
