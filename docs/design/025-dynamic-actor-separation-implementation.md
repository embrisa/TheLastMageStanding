# Dynamic Actor Separation & Knockback Implementation

## Overview
This document describes the dynamic actor separation and knockback system that prevents entities from stacking and adds physical response to attacks and projectiles.

## Architecture

### Components

#### PhysicsComponents.cs
- **Mass** - Determines separation priority during dynamic collisions
  - Lighter entities (0.4-0.6) pushed more than heavier ones (1.0+)
  - Standard values: player=1.0, small enemy=0.5, large enemy=2.0
  
- **Knockback** - Impulse force applied to entities
  - Velocity vector with duration (default 0.2s)
  - Decays linearly over time via `GetDecayedVelocity()`
  - Multiple knockbacks take the strongest (max magnitude)
  
- **ContactDamageCooldown** - Prevents rapid repeated hits
  - Per-entity cooldown tracking (default 0.5s)
  - Tracks last damage source and timestamp
  - Different entities can damage simultaneously

### Systems

#### DynamicSeparationSystem
**Purpose:** Prevents actors from overlapping using soft separation forces.

**Integration:**
- Subscribes to `CollisionEnterEvent` and `CollisionStayEvent`
- Runs after `CollisionSystem` but before `MovementSystem`
- Processes collisions iteratively (max 3 passes per frame)

**Algorithm:**
1. Collect dynamic vs dynamic collision pairs from events
2. For each overlapping pair:
   - Re-test collision for accurate penetration depth
   - Calculate mass ratio (lighter entity pushed more)
   - Apply soft separation as velocity impulse (not hard position correction)
   - Skip if separation magnitude < 0.1 (performance optimization)

**Configuration:**
- `MaxSeparationIterations = 3` - Balance between stability and performance
- `MinSeparationVelocity = 0.1f` - Threshold for applying separation
- `separationStrength = 10.0f` - Tunable force multiplier

**Key Insights:**
- Soft separation (velocity impulse) smoother than hard correction
- Iterative approach handles multi-entity stacking
- Mass ratio prevents player being shoved by swarms

#### KnockbackSystem
**Purpose:** Applies impulse forces from attacks/projectiles and handles decay.

**Integration:**
- Runs after `MovementIntentSystem` but before `CollisionResolutionSystem`
- Static grid rebuilt on session restart for wall clamping

**Features:**
- Linear decay over knockback duration
- Clamps to `MaxKnockbackSpeed = 800f` to prevent extreme velocities
- Checks against world colliders to prevent tunneling
- Removes knockback component when expired

**Usage:**
```csharp
KnockbackSystem.ApplyKnockback(world, entity, knockbackVelocity, duration: 0.15f);
```

**Key Insights:**
- Apply knockback velocity *before* collision checks (prevents tunneling)
- Takes strongest knockback when multiple apply in one frame
- Respects world collision normals for wall blocking

#### ContactDamageSystem
**Purpose:** Handles damage from entity collisions with cooldown tracking.

**Integration:**
- Subscribes to `CollisionEnterEvent` and `CollisionStayEvent`
- Runs after `CollisionSystem` and `DynamicSeparationSystem`
- Tracks game time for cooldown management

**Damage Flow:**
1. Collision event fires between two entities
2. Check faction compatibility (must be different)
3. Check target health (must be alive)
4. Check cooldown via `ContactDamageCooldown` component
5. Apply damage and publish `EntityDamagedEvent`
6. Apply knockback (200f base strength, 0.15s duration)
7. Record damage timestamp for cooldown

**Configuration:**
- Contact damage cooldown: 0.5s default
- Knockback strength: 200f
- Knockback duration: 0.15s

## System Order

The systems integrate into the ECS pipeline as follows:

```
1. InputSystem
2. WaveSchedulerSystem
3. SpawnSystem
4. AiSeekSystem
5. MovementIntentSystem         ← Sets velocity based on AI/input
6. KnockbackSystem              ← NEW: Applies knockback impulses
7. CollisionResolutionSystem    ← Resolves collisions vs world static
8. CollisionSystem              ← Detects all collisions, emits events
9. DynamicSeparationSystem      ← NEW: Separates overlapping actors
10. ContactDamageSystem         ← NEW: Handles contact damage with cooldowns
11. CombatSystem                ← Handles attack intents
12. MovementSystem              ← Applies velocity to position
```

## Debug Visualization

The `CollisionDebugRenderSystem` was extended to show:
- **Orange arrows** - Active knockback vectors (scaled 0.1x for visibility)
- **Cyan arrows** - Velocity vectors for moving dynamic entities (scaled 0.05x)
- **Lime outlines** - Dynamic solid colliders
- **Cyan outlines** - Static colliders
- **Yellow outlines** - Trigger colliders

Toggle with F3 key.

## Entity Factory Updates

### PlayerEntityFactory
- Added `Mass(1.0f)` - Standard player weight
- Added `Collider.CreateCircle(6f, ...)` - Circle collider matching hitbox

### EnemyEntityFactory
- Added `Mass(archetype.Mass)` - Per-archetype mass from config
- Added `Collider.CreateCircle(archetype.CollisionRadius, ...)`

### EnemyArchetype Config
- Added `Mass` field to `EnemyArchetype` record
- BaseHexer: mass=0.6f
- ScoutHexer: mass=0.4f (lighter, pushed more easily)

## Performance Considerations

### Separation System
- Limited to 3 iterations per frame (prevents perf spikes with large hordes)
- Minimum separation threshold prevents micro-adjustments
- Soft forces allow small overlaps (players won't notice 1-2px stacking)

### Knockback System
- Static grid cached until session restart
- Per-frame dynamic entity iteration only
- Max speed clamping prevents extreme calculations

### Contact Damage
- Event-driven (no per-frame iteration)
- Cooldown prevents redundant damage calculations
- O(1) cooldown check per collision event

## Testing

Created `DynamicSeparationTests.cs` with 6 unit tests:
- ✅ Mass default value validation
- ✅ Knockback decay over time
- ✅ Knockback expiration returns zero velocity
- ✅ Contact damage cooldown blocks same entity
- ✅ Contact damage cooldown allows different entities
- ✅ Contact damage cooldown expires after duration

All tests passing.

## Known Limitations

1. **High-Speed Collisions:** Very fast-moving entities may tunnel through thin walls if knockback speed exceeds collision resolution capacity. Mitigation: max speed clamping at 800f.

2. **Large Hordes:** With 100+ overlapping enemies, separation may take 2-3 frames to fully resolve. Acceptable trade-off for performance.

3. **Mass Symmetry:** Equal-mass entities push each other equally, which can cause symmetric oscillation. Rare in practice due to varied enemy masses.

4. **Knockback Stacking:** Currently takes strongest knockback only. Could sum impulses with magnitude clamping if more chaotic feel desired.

## Future Enhancements

- **Swept Collision:** For projectiles/dashes with very high speeds
- **Crowd Steering:** Soft avoidance before collision occurs
- **Damage Falloff:** Reduce contact damage over time in sustained contact
- **Knockback Resistance:** Component to reduce knockback for heavy/boss enemies
