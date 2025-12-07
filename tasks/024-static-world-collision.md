# Task: Static world collision
- Status: backlog

## Summary
Integrate TMX collision/object layers into runtime colliders so players/enemies are blocked by walls/geometry. Tie map loading to the ECS collision system for solid world shapes.

## Goals
- Parse TMX collision regions into static collider entities (rects/poly simplified to AABBs where possible).
- Ensure movement/physics queries world colliders to prevent tunneling through walls.
- Provide a debug overlay that matches TMX collision layers for validation.
- Keep coordinate scaling consistent with our virtual resolution/camera.

## Non Goals
- Destructible/dynamic level geometry or runtime tile editing.
- Pathfinding/navmesh generation (out of scope here).
- Slopes/one-way platforms or precise polygon SAT.

## Acceptance criteria
- [ ] TMX collision/object layers load into ECS colliders on scene load with correct positioning/scale.
- [ ] Player and enemies cannot pass through collision regions when moving at current speeds.
- [ ] Debug view clearly shows loaded world colliders aligned with TMX authoring.
- [ ] Automated check or playtest script verifies representative collision shapes block movement.

## Definition of done
- Builds pass (`dotnet build`)
- Tests/play check done (if applicable)
- Docs updated (if applicable)
- Handoff notes added (if handing off)

## Plan
- Extend TMX loader to read collision/object layers and instantiate static collider entities.
- Bridge movement/physics to consult world colliders via the collision system when applying displacement.
- Add debug overlay toggle to render world colliders and validate alignment.
- Add focused tests or fixtures for TMX-to-world coordinate conversion and blocking.

## Notes / Risks / Blockers
- TMX polygons may need simplification; start with rects, log unsupported shapes.
- Must stay in sync with pixel-per-unit and camera scaling to avoid offsets.
- Coordinate handoff with map-loading timeline to avoid merge conflicts.
- **Insight:** If using Tile Layer collisions (vs Object Layer), consider merging adjacent solid tiles into larger AABB collider entities to drastically reduce the entity count and collision checks.
- **Insight:** Static colliders should be inserted into the spatial grid once (or lazily) and never updated. Ensure the system skips "Update" logic for entities marked Static.
- **Insight:** TMX coordinates are typically top-left origin with Y-down, while MonoGame/game logic often uses center-origin or Y-up. Add explicit conversion helpers (e.g., `TmxToWorld(x, y)`) and validate with debug rendering early to catch sign/offset bugs.
- **Insight:** When parsing TMX object layers, filter by object type or custom properties (e.g., `type="collision"`) to avoid accidentally creating colliders for decorative objects or spawn points.
- **Insight:** For rect merging, a simple horizontal scan-line approach works well: iterate rows, merge consecutive solid tiles in each row, then attempt vertical merges of identical-width rects. This can reduce collider count by 10-100x on typical maps.

