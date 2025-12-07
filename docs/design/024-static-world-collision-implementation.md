# Static World Collision Implementation

## Overview
This document describes the static world collision system that integrates TMX map collision regions into the ECS collision system, preventing players and enemies from passing through walls and solid geometry.

## Architecture

### Components
- **StaticColliderLoader** (`Core/World/Map/StaticColliderLoader.cs`)
  - Parses TMX object layers for collision regions
  - Filters objects by `type="collision_region"` property
  - Creates static collider entities in the ECS world
  - Converts TMX coordinates (top-left, Y-down) to game world coordinates (center-based)
  - Logs unsupported shapes (ellipses, polygons) for debugging

- **CollisionResolutionSystem** (`Core/Ecs/Systems/CollisionResolutionSystem.cs`)
  - Runs after MovementIntentSystem but before MovementSystem
  - Detects collisions between dynamic entities and static world geometry
  - Resolves penetration by sliding along collision normals
  - Maintains a spatial grid of static colliders for efficient queries
  - Only rebuilds static grid when marked dirty (on session restart or explicit call)

- **TiledMapService** (`Core/World/Map/TiledMapService.cs`)
  - Extended with `LoadCollisionRegions(EcsWorld)` method
  - Integrates collision loading into map initialization

### System Integration
The collision resolution system is integrated into the ECS update pipeline:

```
1. InputSystem
2. WaveSchedulerSystem
3. SpawnSystem
4. AiSeekSystem
5. MovementIntentSystem         ← Sets velocity based on intent
6. CollisionResolutionSystem    ← NEW: Resolves collisions, adjusts velocity
7. CollisionSystem              ← Detects collisions for events
8. CombatSystem
9. MovementSystem               ← Applies velocity to position
```

## TMX Collision Authoring

### Supported Formats
- **Rectangles**: Fully supported, converted to AABB colliders
- **Ellipses**: Not supported, logged with warning
- **Polygons/Polylines**: Not supported, logged with warning

### Tagging Collision Objects
Mark collision objects in Tiled with:
- `type="collision_region"` property (recommended)
- Or include "collision" or "wall" in the object name (fallback)

### Coordinate Conversion
TMX uses top-left origin with Y-down. The loader converts to world-space center:
```csharp
worldCenter.X = tmxX + width * 0.5
worldCenter.Y = tmxY + height * 0.5
```

### Example TMX Snippet
```xml
<objectgroup id="7" name="Object Layer 1">
  <object id="13" type="collision_region" x="680.667" y="337.998" width="266" height="232.67"/>
  <object id="16" type="collision_region" x="364.667" y="789.332" width="196" height="126.67"/>
  <object id="23" type="collision_region" x="683.667" y="304.332" width="231.333" height="33.3367"/>
</objectgroup>
```

## Debug Visualization
- **Toggle**: Press `F3` to toggle collision debug overlay
- **Color Coding**:
  - **Cyan** (semi-transparent): Static world colliders
  - **Lime**: Dynamic solid colliders
  - **Yellow**: Trigger colliders
  - **Blue center point**: Static collider center
  - **Red center point**: Dynamic collider center

## Configuration
Static colliders use these settings:
- **Layer**: `CollisionLayer.WorldStatic`
- **Mask**: `Player | Enemy | Projectile` (what can collide with walls)
- **IsTrigger**: `false` (solid, blocks movement)

## Performance
- Static colliders are indexed once at map load
- Spatial grid uses 128-unit cell size (same as dynamic grid)
- No per-frame updates for static geometry
- Efficient broad-phase queries reduce narrow-phase tests

## Usage

### Loading Collision Regions
In `Game1.LoadContent()`:
```csharp
_mapService = TiledMapService.Load(Content, GraphicsDevice, mapAsset);
_ecs.LoadContent(GraphicsDevice, Content);
_mapService.LoadCollisionRegions(_ecs.World);  // Load collisions after ECS init
```

### Marking Static Grid Dirty
If static colliders are added/removed dynamically:
```csharp
collisionResolutionSystem.MarkStaticGridDirty();
```

## Testing
Three new unit tests validate static collider creation:
1. `StaticCollider_CanBeCreatedInEcs` - Basic ECS integration
2. `StaticCollider_WithPosition_HasCorrectBounds` - Coordinate conversion
3. `StaticCollider_HasWorldStaticLayer` - Layer/mask configuration

All tests pass: **24 total** (21 existing + 3 new)

## Known Limitations
- Only rectangular collision regions supported (no polygons or circles)
- No support for one-way platforms or slopes
- No runtime destructible geometry
- Collision resolution uses simple sliding (no advanced friction/bounce)

## Future Enhancements
- Support for tileset-based collision layers with tile merging
- Polygon collision support via SAT or convex decomposition
- Collision region optimization (merge adjacent rects)
- Per-material collision properties (friction, bounciness)
