# Telegraphs, VFX/SFX, and Hit-Stop Implementation

**Implemented:** December 7, 2025  
**Task:** 028 - Telegraphs, VFX/SFX, and hit-stop

## Summary

Added reusable telegraph system for attack windups, baseline VFX/SFX hooks integrated with animation events, and hit-stop micro-pauses with camera shake for impactful combat feedback. All systems include debug toggles and graceful degradation for missing assets.

## Architecture

### Components

#### TelegraphComponents.cs
- **`TelegraphData`** - Defines telegraph visual properties (duration, color, radius, offset, shape)
- **`TelegraphShape`** - Enum for telegraph shapes (Circle, Cone, Rectangle)
- **`ActiveTelegraph`** - Component marking entity as displaying telegraph warning

#### VfxComponents.cs
- **`VfxRequest`** - Request to spawn a VFX effect
- **`VfxType`** - Enum for VFX types (Impact, WindupFlash, ProjectileTrail, MuzzleFlash)
- **`ActiveVfx`** - Active VFX instance with lifetime, color, scale

### Events

#### VfxEvents.cs
- **`VfxSpawnEvent`** - Event requesting VFX spawn at position
- **`SfxPlayEvent`** - Event requesting SFX playback with category-based volume
- **`SfxCategory`** - Enum for SFX categories (Attack, Impact, Ability, UI)

### Systems

#### VfxSystem (`VfxSystem.cs`)
Manages VFX spawning, pooling, and lifecycle:
- Subscribes to `VfxSpawnEvent`
- Creates VFX entities with lifetime and fade-out
- Gracefully logs missing assets once without crashing
- Supports global enable/disable toggle (`VfxSystem.EnableVfx`)

#### SfxSystem (`SfxSystem.cs`)
Manages SFX playback with category-based volume control:
- Subscribes to `SfxPlayEvent`
- Calculates final volume from master × category × event volume
- Tracks missing assets to avoid log spam
- Supports global enable/disable toggle (`SfxSystem.EnableSfx`)
- Per-category volume control

#### TelegraphSystem (`TelegraphSystem.cs`)
Manages telegraph lifecycle:
- Updates telegraph lifetimes
- Static `SpawnTelegraph()` method for creating warnings
- Supports global visibility toggle (`TelegraphSystem.ShowTelegraphs`)

#### TelegraphRenderSystem (`TelegraphRenderSystem.cs`)
Renders telegraph warnings and VFX:
- Draws circular telegraphs (cone/rectangle shapes prepared for future)
- Renders simple flash VFX using pixel texture
- Respects global visibility toggles

#### HitStopSystem (`HitStopSystem.cs`)
Handles hit-stop and camera shake on damage:
- Subscribes to `EntityDamagedEvent`
- Calculates hit-stop duration based on damage (10 damage = 30ms, 50+ = 100ms max)
- Generates camera shake intensity scaled with damage
- Supports independent toggles for hit-stop and camera shake
- Provides `IsHitStopped()` check and `CameraShakeOffset` property

### Integration

#### AnimationEventSystem
Updated to fire VFX/SFX events:
- Added VFX/SFX triggers to default event tracks
- Player melee fires windup VFX at 0.03s, swing SFX at 0.04s
- Events published to event bus instead of console logging

#### MeleeHitSystem
Updated to trigger impact feedback:
- Publishes `VfxSpawnEvent` for melee impact with red color
- Publishes `SfxPlayEvent` for hit sound

#### EcsWorldRunner
Registered new systems in correct order:
- `HitStopSystem` runs early to track timing
- `VfxSystem`, `SfxSystem`, `TelegraphSystem` update in gameplay loop
- `TelegraphRenderSystem` draws after projectiles but before collision debug
- Hit-stop logic halts most systems but allows VFX/SFX/visual feedback to update
- Camera shake applied from `HitStopSystem.CameraShakeOffset`

#### Camera2D
Added shake offset support:
- New `ShakeOffset` property
- Applied to effective position in `UpdateTransform()`

#### DebugInputSystem
Added debug toggles:
- **F3** - Collision visualization (existing)
- **F4** - Toggle hit-stop enable/disable
- **F5** - Toggle camera shake enable/disable
- **F6** - Toggle VFX/SFX enable/disable

## Implementation Details

### VFX/SFX Asset Handling
- Systems gracefully degrade when assets are missing
- First access logs warning once per asset, then silent
- VFX entities still spawn (for future sprite/particle support)
- SFX checks loaded sounds dictionary before playing

### Hit-Stop Behavior
- Duration scaled with damage: `duration = damage * 0.002f` clamped to [0.02s, 0.1s]
- Overlapping hits extend timer rather than resetting
- Camera shake intensity: `intensity = damage * 0.15f` clamped to [1px, 8px]
- Shake fades out over 150ms duration
- Hit-stop halts movement, combat, AI but allows visual/audio feedback systems

### Telegraph Lifecycle
- Created via `TelegraphSystem.SpawnTelegraph(world, position, data)`
- Timer decrements each frame
- Removed when lifetime expires
- Currently renders as filled circles; cone/rectangle shapes prepared

### System Update Order
```
1. GameSessionSystem
2. DebugInputSystem
3. InputSystem
...
13. MeleeHitSystem (publishes VFX/SFX events)
14. ProjectileHitSystem
15. AnimationEventSystem (publishes VFX/SFX events)
16. CombatSystem
17. HitStopSystem ← Tracks timing
18. VfxSystem ← Processes spawns
19. SfxSystem ← Processes playback
20. TelegraphSystem ← Updates lifetimes
21. HitReactionSystem
...
```

### Debug Toggles
All systems respect their enable flags:
- `VfxSystem.EnableVfx` (default: true)
- `SfxSystem.EnableSfx` (default: true)
- `TelegraphSystem.ShowTelegraphs` (default: true)
- `HitStopSystem.EnableHitStop` (default: true)
- `HitStopSystem.EnableCameraShake` (default: true)
- `HitStopSystem.MaxHitStopDuration` (default: 0.1s)

## Testing

### HitStopSystemTests.cs
- `HitStopSystem_OnDamage_TriggersHitStop` - Verifies hit-stop triggers on damage
- `HitStopSystem_Update_DecrementsHitStopTimer` - Verifies timer decrements and clears
- `HitStopSystem_HighDamage_TriggersStrongerHitStop` - Verifies damage scaling
- `HitStopSystem_Disabled_DoesNotTriggerHitStop` - Verifies toggle behavior
- `HitStopSystem_OnDamage_TriggersCameraShake` - Verifies shake generation
- `HitStopSystem_CameraShakeDisabled_NoShakeOffset` - Verifies shake toggle

### VfxTelegraphSystemTests.cs
- `VfxSystem_OnVfxSpawnEvent_CreatesVfxEntity` - Verifies VFX entity creation
- `VfxSystem_Update_DecrementsVfxLifetime` - Verifies VFX lifetime management
- `VfxSystem_Disabled_DoesNotCreateVfx` - Verifies toggle behavior
- `TelegraphSystem_SpawnTelegraph_CreatesEntity` - Verifies telegraph creation
- `TelegraphSystem_Update_DecrementsLifetime` - Verifies telegraph lifetime
- `TelegraphSystem_Disabled_DoesNotCreateTelegraph` - Verifies toggle behavior

All tests pass (64 total).

## Future Enhancements

**Immediate Opportunities:**
- Add actual VFX sprite/particle assets
- Load SFX assets into `SfxSystem._loadedSounds`
- Implement cone and rectangle telegraph shapes
- Add telegraph color pulsing/animation

**Later Additions:**
- Advanced VFX pooling with object reuse
- Audio mixing bus with ducking/reverb
- Post-processing shader effects for screen shake
- Slow-motion time dilation (currently only brief hit-stop)
- Positional audio with 2D panning
- VFX authoring tool/editor

## Usage Examples

### Spawning a Telegraph
```csharp
var telegraphData = new TelegraphData(
    duration: 0.3f,
    color: new Color(255, 0, 0, 128),
    radius: 50f,
    offset: Vector2.Zero,
    shape: TelegraphShape.Circle
);
TelegraphSystem.SpawnTelegraph(world, enemyPosition, telegraphData);
```

### Triggering VFX
```csharp
world.EventBus.Publish(new VfxSpawnEvent(
    "explosion",
    position,
    VfxType.Impact,
    Color.Orange
));
```

### Playing SFX
```csharp
world.EventBus.Publish(new SfxPlayEvent(
    "sword_swing",
    SfxCategory.Attack,
    attackerPosition,
    volume: 0.8f
));
```

### Adjusting Category Volume
```csharp
sfxSystem.SetCategoryVolume(SfxCategory.UI, 0.5f);
```

## Known Limitations

- VFX currently renders as simple colored pixels (awaiting sprite assets)
- SFX assets not yet loaded (graceful degradation with console warnings)
- Telegraph shapes limited to circles (cone/rectangle prepared but not implemented)
- Hit-stop is global (doesn't support per-entity time dilation)
- Camera shake is simple random offset (no decay curves or directional bias)
- No audio mixing bus or advanced audio features
