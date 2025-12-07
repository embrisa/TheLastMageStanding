# Task: Collision system foundation
- Status: completed
- Completed: 2025-12-07

## Summary
We currently rely on ad-hoc radius checks. We need an ECS-native collision framework (colliders, masks, spatial partitioning) that can power movement blocking, contact damage, and future features without perf cliffs.

## Goals
- Define collider components (circle/AABB), layer/mask filtering, and trigger vs solid behavior.
- Implement a spatial grid broadphase plus narrow-phase overlap checks.
- Publish collision enter/stay/exit events with contact data through the event bus.
- Add a lightweight debug overlay/toggle to visualize colliders and interactions.

## Non Goals
- Full physics (rigidbodies, friction/rotation) or swept continuous collision.
- Pathfinding/navmesh authoring or dynamic tile editing.
- Networking/rollback concerns.

## Acceptance criteria
- [x] Entities with colliders detect overlaps via broadphase+narrow-phase and emit collision events filtered by layer/mask.
- [x] Collision loop stays stable with typical horde counts (no noticeable frame spikes at ~200 actors in a test scene).
- [x] Debug overlay can be toggled on/off and shows collider bounds in world space.
- [x] Automated coverage for overlap math and event emission edge cases.

## Definition of done
- [x] Builds pass (`dotnet build`)
- [x] Tests/play check done (21 tests passing)
- [x] Docs updated (implementation notes added)
- [x] Handoff notes added

## Implementation Notes
Successfully implemented a complete collision system foundation:

### Components Created
- `CollisionComponents.cs`: Collider component with circle/AABB shapes, layer/mask filtering, trigger vs solid behavior, and offset support.
- `CollisionLayer` enum: Flags-based layer system (Player, Enemy, Projectile, WorldStatic, Pickup).
- `StaticCollider` tag component: Marks entities for static grid optimization.

### Systems Created
1. **CollisionSystem.cs**: Core system managing broadphase, narrow-phase, and event emission
   - Separate spatial grids for static and dynamic entities
   - Tracks collision pairs between frames for Enter/Stay/Exit events
   - Integrated into ECS world runner, runs after movement intent

2. **CollisionDebugRenderSystem.cs**: Debug visualization with toggle support
   - Renders collider bounds (circles and AABBs)
   - Color-coded (yellow for triggers, lime for solids)
   - Implements IDisposable for proper resource cleanup

### Collision Detection
- **SpatialGrid.cs**: Broadphase with configurable cell size (default 128 units)
  - Efficient insertion, removal, and querying
  - Handles large entities spanning multiple cells
  - Consistent entity ID ordering in pairs
  
- **CollisionDetection.cs**: Narrow-phase with precise overlap tests
  - Circle-Circle, Circle-AABB, and AABB-AABB collision
  - Layer/mask filtering for early rejection
  - Returns contact information (point, normal, penetration)

### Events
- `CollisionEnterEvent`: Fired when two colliders start overlapping
- `CollisionStayEvent`: Fired when overlap continues
- `CollisionExitEvent`: Fired when overlap ends

### Testing
Created comprehensive test suite in new `Game.Tests` project:
- `CollisionDetectionTests.cs`: 16 tests covering all shape combinations and layer filtering
- `SpatialGridTests.cs`: 9 tests for spatial partitioning correctness
- All 21 tests passing

### Integration
- Added to `EcsWorldRunner` system pipeline (runs after movement intent)
- Exposed internal types to test assembly via `InternalsVisibleTo`
- Test project added to solution file

## Notes / Risks / Blockers
- Broadphase cell sizing impacts perf; may need tuning for our pixel-per-unit.
- Event spam risk on stay/exit if components churn; consider caching pairs.
- Coordinate with movement/knockback work to avoid double-resolving solids.
- **Insight:** For the Spatial Grid, a cell size of approx. 2x the average actor size is a good starting point to balance memory vs. checking neighbor cells.
- **Insight:** Leverage MonoGame's `Rectangle.Intersects` and `Vector2.DistanceSquared` for fast checks.
- **Insight:** Consider separating "Static" and "Dynamic" objects in the broadphase to avoid re-indexing static walls every frame.
- **Insight:** Cache collision pairs in a `HashSet<(int entityA, int entityB)>` between frames to track Enter/Stay/Exit efficiently. Ensure lower entity ID is always first in the pair to avoid duplicate keys like (1,2) vs (2,1).
- **Insight:** For Enter/Exit events, only emit when the collision state actually changes. Store the previous frame's collision set and diff against current frame to avoid spamming Stay events every frame.
- **Insight:** Add early-exit checks in the narrow phase: if layer masks don't overlap (`(layerA & maskB) == 0 && (layerB & maskA) == 0`), skip the expensive shape intersection test entirely.

