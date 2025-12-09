# Dash / Defense Move Implementation

## Overview
Implements a snappy dash/evade with i-frames, cooldown gating, buffered input, UI feedback, and debug visualization. Dash direction prioritizes current movement, then velocity, then facing.

## Components
- `DashConfig` (`DashComponents.cs`): distance 150, duration 0.2s, cooldown 2s, i-frame window 0.15s, input buffer 0.05s.
- `DashState`: active flag, elapsed time, direction, i-frame active toggle.
- `DashCooldown`: remaining cooldown seconds.
- `DashInputBuffer`: buffered dash input + time remaining.
- `Invulnerable`: tag for i-frame immunity (damage systems skip when present).

## Systems
- `DashInputSystem`: reads `InputState.DashPressed` (Shift/Space), ticks cooldown, buffers attempts when on cooldown/active, and publishes `DashRequestEvent` when ready. Uses `InputIntent` → `Velocity` → `PlayerAnimationState` to lock direction.
- `DashExecutionSystem`: consumes `DashRequestEvent`, starts dash state, sets velocity override, applies cooldown, adds `Invulnerable`, and fires start VFX/SFX + camera nudge via `HitStopSystem`.
- `DashMovementSystem`: advances dash over time, keeps velocity at dash speed, drops invulnerability at i-frame end, clears state/velocity when dash finishes, and emits end VFX/SFX.

## Event/Order
Update pipeline excerpt:
```
InputSystem
DashInputSystem
DashExecutionSystem
DashMovementSystem
... (skill/calc/AI)
MovementIntentSystem   (skips when dash active)
KnockbackSystem
CollisionResolutionSystem
CollisionSystem
ContactDamageSystem / MeleeHitSystem / ProjectileHitSystem (skip Invulnerable)
```

## Collision & Damage Integration
- `Invulnerable` tag added/removed by dash systems; `ContactDamageSystem`, `MeleeHitSystem`, and `ProjectileHitSystem` skip targets with the tag.
- Dash velocity still runs through static collision resolution for wall clamping/sliding.

## UI / Debug / Audio-Visuals
- HUD adds bottom-right dash indicator (cooldown fill, buffered flash).
- F9 toggles dash/i-frame overlay inside `CollisionDebugRenderSystem` (cyan invuln rings, yellow dash trajectory).
- VFX types: `DashTrail` (start) and `DashEnd`; SFX hooks `DashStart` / `DashEnd` (logs missing until assets exist).
- HitStopSystem gains `TriggerCameraNudge` reuse of camera shake for subtle dash kick.

## Tests
`DashSystemTests` cover:
- Execution respects cooldown and sets state/velocity/invulnerability.
- Dash movement ends after duration and drops velocity/invulnerability.
- I-frames end before dash completion.
- Input buffering holds during cooldown and consumes when ready.




