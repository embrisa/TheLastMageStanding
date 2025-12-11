# Task: Collider-driven combat hits
- Status: done

## Summary
Replace ad-hoc radius checks with collider-driven hit detection for melee/contact attacks. Use the collision system + event bus to manage hurtboxes/hitboxes, filters, and damage application, aligning attacks with animations.

## Goals
- Define attack hitbox/hurtbox components with layer/faction filtering (no friendly fire).
- Drive player melee and enemy contact damage through collision events instead of manual distance math.
- Support attaching hitboxes to animation frames/offsets for readability and future VFX sync.
- Ensure projectile/ranged work (Task 021) can reuse the same collider query utilities.

## Non Goals
- Complex combo systems or animation event authoring tools.
- Network replication/lag compensation.
- Hit-stop/camera shake tuning beyond basic hooks.

## Acceptance criteria
- [x] Player melee attacks use collider hitboxes to damage valid targets via the event bus; old radius checks removed.
- [x] Enemy contact damage uses collision events with cooldowns and proper faction filtering.
- [x] Hitboxes can be toggled in debug view to verify alignment with animations.
- [x] Tests cover hit filtering (ally vs enemy), cooldown handling, and legacy CombatSystem removal.

## Definition of done
- [x] Builds pass (`dotnet build`)
- [x] Tests/play check done (43 tests passing, 13 new tests added)
- [x] Docs updated (design doc created at docs/design/026-collider-driven-combat-implementation.md)
- [x] Handoff notes added

## Plan
- Add hitbox/hurtbox components with faction masks and optional cooldown metadata.
- Wire collision events into combat systems to apply damage and cooldowns; delete direct distance checks.
- Add animation/frame offset hooks for spawning/enabling hitboxes during attacks.
- Build targeted tests for filtering and cooldowns; validate in a test scene with debug overlays.

## Notes / Risks / Blockers
- Needs coordination with Task 021 projectiles to avoid duplicated hit logic.
- Animation timing may require temporary hardcoded windows until full animation events exist.
- Must ensure collision events are deterministic enough for combat feedback.
- **Insight:** Melee hitboxes are often "transient" entities (alive for only a few frames). Ensure the ECS can handle rapid creation/destruction efficiently, or use a pooling strategy for hitbox entities.
- **Insight:** Use a `[Flags]` enum for Collision Layers (e.g., `Player | Projectile`) to allow efficient bitmask filtering in the broadphase.
- **Insight:** Store an `OwnerEntityId` on hitbox components so damage events can reference the attacker for XP/aggro attribution. This also prevents self-hits (player's hitbox vs player's hurtbox).
- **Insight:** For melee swings, use a "one-shot" hit flag: track which entities have already been hit by this hitbox instance (e.g., `HashSet<int> AlreadyHit`) to prevent multi-hitting the same target if they remain in the hitbox for multiple frames.
- **Insight:** Coordinate with invulnerability frames (iframes): if an entity has a "DamagedRecently" cooldown component, either filter them out in the collision mask temporarily or check the cooldown in the damage-application system before applying damage.

## Implementation Summary

Successfully replaced ad-hoc distance-based hit detection with collider-driven combat hits:

### Components Created
- `CombatHitboxComponents.cs`:
  - `AttackHitbox`: Transient hitbox component with owner tracking, faction filtering, lifetime, and one-shot hit tracking
  - `Hurtbox`: Marks entities as damageable with invulnerability support
  - `MeleeAttackConfig`: Configuration for spawning hitboxes with radius, offset, and duration

### Systems Created/Modified
- `MeleeHitSystem.cs`: NEW - Handles collision-based damage from attack hitboxes
  - Subscribes to CollisionEnterEvent
  - Validates hits (faction, self-damage, already-hit, invulnerability)
  - Applies brief invulnerability windows (50ms)
  - Manages hitbox lifetimes
- `CombatSystem.cs`: MODIFIED - Now spawns transient hitbox entities instead of distance checks
  - Removed `ApplyDamageInRange` and `HandleEnemyContact`
  - Added `SpawnAttackHitbox` to create trigger collider entities
  - Uses ProjectileLayer for player attacks vs Enemy mask
- `CollisionDebugRenderSystem.cs`: EXTENDED - Visualizes attack hitboxes
  - Magenta circles for player hitboxes
  - Red circles for enemy hitboxes
  - Toggle with F3 key

### Entity Factories Updated
- `PlayerEntityFactory`: Added Hurtbox and MeleeAttackConfig components
- `EnemyEntityFactory`: Added Hurtbox component (enemies use ContactDamageSystem for attacks)

### Testing
Created comprehensive test suite with 13 new tests:
- `MeleeHitSystemTests.cs`: 8 tests covering hit validation, faction filtering, invulnerability
- `CombatSystemHitboxTests.cs`: 5 tests covering hitbox spawning, configuration, cooldowns

All 43 tests passing (13 new + 30 existing).

### Key Features
- ✅ Faction filtering prevents friendly fire
- ✅ One-shot hit tracking prevents multi-hits
- ✅ Invulnerability windows (50ms) prevent rapid damage
- ✅ Transient hitbox entities with automatic cleanup
- ✅ Debug visualization for alignment verification
- ✅ Event-driven damage application
- ✅ Collision layer filtering for performance

### System Integration Order
```
CollisionSystem → CollisionEnterEvent → MeleeHitSystem → EntityDamagedEvent → HitReaction/Effects
```

### Performance Impact
- Negligible: 1-10 hitbox entities max simultaneously
- Collision filtering prevents unnecessary checks
- Automatic lifetime-based cleanup
- No pooling needed

### Documentation
- Design document created at `docs/design/026-collider-driven-combat-implementation.md`
- Covers architecture, components, systems, testing, configuration, and future enhancements

### Future Enhancements
- Directional hitbox offsets based on player facing
- Animation event system for frame-precise hitboxes
- Convert enemy melee to use same hitbox system
- Support for more hitbox shapes (AABB, cone, arc)






