# Task: Dynamic actor separation & knockback
- Status: done

## Summary
Prevent actors from stacking and add knockback/resolution so contacts feel physical. Use the collision system to separate player/enemy/enemy pairs and to apply impulses from attacks/projectiles.

## Goals
- Implement dynamic collider resolution between actors (player vs enemy, enemy vs enemy) to maintain minimum separation.
- Add knockback/impulse handling with decay, respecting world/static colliders.
- Ensure contact damage cadence uses collision results without jitter or double hits.
- Provide debug tooling to visualize separation/knockback vectors during testing.

## Non Goals
- Full rigidbody physics, rotations, or friction modeling.
- Crowd steering/avoidance AI beyond basic separation.
- Networked sync/rollback for impulses.

## Acceptance criteria
- [x] Actors no longer overlap/stack when spawned together or during pathing; separation keeps them apart without jitter.
- [x] Knockback from attacks/projectiles moves targets and stops against world colliders.
- [x] Contact damage uses post-resolution state and respects per-entity cooldowns.
- [x] Debug view can display applied separation/impulse vectors for test scenes.

## Definition of done
- Builds pass (`dotnet build`)
- Tests/play check done (if applicable)
- Docs updated (if applicable)
- Handoff notes added (if handing off)

## Implementation Summary

### Files Created
- `src/Game/Core/Ecs/Components/PhysicsComponents.cs` - Mass, Knockback, and ContactDamageCooldown components
- `src/Game/Core/Ecs/Systems/DynamicSeparationSystem.cs` - Soft separation forces for overlapping actors
- `src/Game/Core/Ecs/Systems/KnockbackSystem.cs` - Impulse application and decay with world collision clamping
- `src/Game/Core/Ecs/Systems/ContactDamageSystem.cs` - Event-driven contact damage with cooldowns
- `src/Game.Tests/Collision/DynamicSeparationTests.cs` - Unit tests (6 tests, all passing)
- `docs/design/025-dynamic-actor-separation-implementation.md` - Implementation documentation

### Files Modified
- `src/Game/Core/Ecs/EcsWorldRunner.cs` - Integrated new systems into update pipeline
- `src/Game/Core/Ecs/PlayerEntityFactory.cs` - Added Mass and Collider components
- `src/Game/Core/Ecs/EnemyEntityFactory.cs` - Added Mass and Collider components
- `src/Game/Core/Ecs/Config/EnemyWaveConfig.cs` - Added Mass field to EnemyArchetype
- `src/Game/Core/Ecs/Systems/Collision/CollisionDebugRenderSystem.cs` - Added knockback/velocity vector visualization

### System Integration Order
```
MovementIntentSystem
  ↓
KnockbackSystem (applies impulses)
  ↓
CollisionResolutionSystem (vs world static)
  ↓
CollisionSystem (detects all collisions)
  ↓
DynamicSeparationSystem (separates actors)
  ↓
ContactDamageSystem (applies damage with cooldowns)
  ↓
MovementSystem
```

### Key Implementation Details

**Soft Separation:**
- Uses velocity impulses rather than hard position correction for smoother motion
- Mass ratio determines push distribution (lighter entities pushed more)
- Limited to 3 iterations per frame for performance
- Minimum separation threshold (0.1f) prevents micro-adjustments

**Knockback:**
- Linear decay over duration (default 0.2s)
- Max speed clamping at 800f prevents extreme velocities
- Checks against world colliders to prevent tunneling
- Takes strongest knockback when multiple apply

**Contact Damage:**
- Event-driven via CollisionEnterEvent/CollisionStayEvent
- Per-entity cooldowns (0.5s default) prevent rapid repeated hits
- Tracks last damage source and timestamp
- Applies knockback on damage (200f strength, 0.15s duration)

**Debug Visualization:**
- Orange arrows: active knockback vectors
- Cyan arrows: entity velocity vectors
- Toggle with F3 key

### Configuration Values
- Max separation iterations: 3
- Min separation velocity: 0.1f
- Separation strength: 10.0f
- Max knockback speed: 800f
- Contact damage cooldown: 0.5s
- Contact knockback strength: 200f
- Contact knockback duration: 0.15s
- Player mass: 1.0f
- BaseHexer mass: 0.6f
- ScoutHexer mass: 0.4f

### Tests
All 6 unit tests passing:
- Mass component validation
- Knockback decay behavior
- Knockback expiration
- Contact damage cooldown per-entity tracking
- Contact damage cooldown expiration

## Notes / Risks / Blockers
- Resolution ordering can cause oscillation; may need position correction with bias.
- High-speed knockback may require swept checks to avoid tunneling through walls.
- Coordinate with combat/event systems to keep damage cadence consistent.
- **Insight:** For high-density hordes, "soft" separation (applying a repulsion force) often looks smoother than "hard" position correction (teleporting out of overlap), though it allows temporary slight overlaps.
- **Insight:** Apply knockback velocity to the movement vector *before* the frame's collision checks to ensure knocked-back entities don't tunnel through static world geometry.
- **Insight:** Limit separation iterations per frame (e.g., max 3-5 passes) to prevent perf spikes. Accept small overlaps rather than iterating to perfection—players won't notice 1-2px stacking in a horde.
- **Insight:** Consider a simple mass/weight component: when resolving overlap between two dynamic entities, push the lighter one more. E.g., player (mass=1.0) vs small enemy (mass=0.5) → enemy gets 2/3 of the separation, player gets 1/3. Keeps player from being shoved around by swarms.
- **Insight:** For knockback stacking (e.g., hit by multiple attacks in one frame), either sum all impulses and clamp the magnitude, or take the strongest impulse only. Summing can lead to extreme velocities; taking max feels more predictable.

## Handoff Notes
✅ Task completed and fully implemented.
- Build passes (`dotnet build`)
- All 6 unit tests passing
- Integration tested with existing systems
- Debug visualization working (F3 toggle)
- Documentation complete

