# Animation Events & Directional Hitboxes Implementation

**Implemented:** December 7, 2025  
**Task:** 027 - Animation events & directional hitboxes

## Summary

Replaced hardcoded combat timing with animation-driven event windows and added directional hitbox offsets. Attacks now spawn hitboxes based on frame-accurate events and position them according to the entity's facing direction.

## Architecture

### Components (`AnimationEventComponents.cs`)

#### AnimationEventType
Enum defining event types:
- `HitboxEnable` - Enable hitbox at specific time
- `HitboxDisable` - Disable hitbox at specific time
- `VfxTrigger` - Optional VFX hook (future)
- `SfxTrigger` - Optional SFX hook (future)

#### AnimationEvent
Single event definition with:
- `Type` - Event type
- `TimeSeconds` - When to fire (seconds from animation start)
- `Data` - Optional payload (e.g., VFX/SFX asset name)

#### AnimationEventTrack
Collection of events for an animation clip:
- `AnimationName` - Clip identifier
- `Events` - Ordered list of events
- `GetEventsInRange()` - Returns events that should fire between two time points

#### AnimationEventState
Component tracking playback state:
- `PreviousTime` - For detecting event crossings
- `HitboxActive` - Whether hitbox is currently spawned

#### DirectionalHitboxConfig
Per-facing offsets for hitbox positioning:
- 8-way directional offsets (South, SouthEast, East, NorthEast, North, NorthWest, West, SouthWest)
- `GetOffsetForFacing()` - Returns offset for current facing
- `CreateDefault()` - Factory for symmetric forward offsets

#### AnimationDrivenAttack
Marks entity as using event-driven attacks:
- `AttackAnimationName` - Animation clip that triggers events

#### ActiveAnimationHitbox
Tracks currently active hitbox entity reference

### Systems

#### AnimationEventSystem (`AnimationEventSystem.cs`)
Processes animation events and spawns/destroys hitboxes:
- Registered as update system, runs before CombatSystem
- Processes player and enemy animation events separately
- Maintains event track registry with default tracks
- Spawns hitboxes with directional offsets on enable events
- Destroys hitboxes on disable events or animation exit
- Logs warnings for missing VFX/SFX assets without breaking gameplay

**Event Tracks:**
- `PlayerMelee` - Enable at 0.05s, disable at 0.15s
- `EnemyMelee` - Prepared for future enemy attacks

**System Order:**
```
...
13. MeleeHitSystem          ← Handles collision-based damage
14. AnimationEventSystem    ← NEW: Process events, spawn hitboxes
15. CombatSystem            ← Manages cooldowns
...
```

### Integration

#### PlayerEntityFactory
Added components:
- `AnimationDrivenAttack("PlayerMelee")`
- `DirectionalHitboxConfig.CreateDefault(24f)` - 24 units forward
- `AnimationEventState(0f, false)`

#### EcsWorldRunner
- Registered `AnimationEventSystem` before `CombatSystem`

#### CollisionDebugRenderSystem
Extended debug visualization:
- Lines from hitbox to owner entity (shows ownership)
- Hot pink arrows showing directional offset for current facing
- Small circles at offset positions
- Toggle with F3 key

## Implementation Details

### Event Processing Flow
1. Check if entity has animation-driven attack components
2. Determine if current animation clip is an attack (non-movement)
3. Query event track for events in time range `[previousTime, currentTime]`
4. Process each event:
   - Enable: Spawn hitbox with directional offset if not already active
   - Disable: Destroy hitbox entity if active
   - VFX/SFX: Log trigger (future implementation)
5. Update event state with current time

### Directional Offset Calculation
- Offsets configured per facing direction (8-way)
- Default factory creates symmetric forward offsets using trigonometry
- South = (0, +Y), North = (0, -Y), East = (+X, 0), West = (-X, 0)
- Diagonals = (±X * 0.707, ±Y * 0.707)
- Applied when spawning hitbox: `hitboxPosition = entityPosition + offset`

### Hitbox Lifecycle
1. Event fires at specified time during attack animation
2. System spawns new entity with:
   - Position (entity pos + directional offset)
   - AttackHitbox (owner, damage, faction, long lifetime)
   - Collider (trigger circle with layer filtering)
3. Reference stored in `ActiveAnimationHitbox` component
4. Destroyed when:
   - Disable event fires
   - Animation exits attack state
   - Entity destroyed

### Event Timing
- Fixed timestep ensures deterministic event firing
- `GetEventsInRange()` handles animation looping (wraparound)
- Previous time tracking prevents duplicate event processing
- Events fire once per animation cycle

## Testing

Created `AnimationEventSystemTests` with 9 tests covering:
- ✅ HitboxEnable spawns hitbox entity
- ✅ HitboxDisable removes hitbox entity
- ✅ East facing applies correct offset (positive X)
- ✅ South facing applies correct offset (positive Y)
- ✅ North facing applies correct offset (negative Y)
- ✅ Non-attack animations don't spawn hitboxes
- ✅ Event state prevents duplicate hitbox spawns
- ✅ EventTrack.GetEventsInRange returns correct events
- ✅ CreateDefault generates all 8 directions correctly

**Total Project Tests:** 52 passing (9 new, 43 existing)

## Configuration

### Player Defaults
- Forward distance: 24 units
- Hitbox enable: 0.05s into attack animation
- Hitbox disable: 0.15s into attack animation
- Hitbox active window: 0.10s
- Hitbox radius: 42 units (from AttackStats)

### Future Configuration Options
- Animation event data can be loaded from JSON/CSV
- Custom event tracks can be registered via `RegisterEventTrack()`
- Per-animation VFX/SFX asset names in event data
- Per-direction offset tuning for asymmetric attacks

## Compatibility

- Fully compatible with existing collision-driven combat (Task 026)
- Works with existing animation system (Task 007)
- Respects facing direction system (Task 011, 017)
- Debug overlay integrates with collision visualization (Task 024)
- Does not break existing enemy contact damage system

## Future Enhancements

**Immediate Opportunities:**
- Add attack animation to player sprite set (currently using fake clip)
- Implement VFX/SFX hooks when assets available
- Add enemy melee attack animations with events

**Later Additions:**
- Root motion integration
- Combo/chain attack event sequences
- Cancel windows with priority overrides
- Per-frame hitbox shape/size changes
- Animation event editor UI

## Known Limitations

- Currently uses non-standard clip enum value (99) to test attack events
- Player doesn't have actual attack animation yet (uses idle/run)
- Event data is code-defined (no external authoring tool)
- No animation blending or transition events
- VFX/SFX hooks log warnings but don't trigger effects

## Notes

- Animation event system is deterministic at fixed timestep (60 FPS)
- Directional offsets align with 8-way facing system
- Event tracks are cached to avoid per-frame allocations
- Hitbox entities are lightweight (3 components)
- System prepared for enemy melee attacks (future)
- Debug visualization helps validate hitbox positioning
