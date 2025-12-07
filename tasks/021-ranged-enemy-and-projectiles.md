# Task: 021 - Ranged enemy and projectiles
- Status: complete

## Summary
Current enemies are melee-only. To increase variety and pressure, introduce a ranged enemy archetype plus a reusable projectile pipeline (components/systems/rendering) that fits the ECS/event bus architecture with clear telegraphing.

## Goals
- Add projectile components/systems for movement, collision, lifetime, and damage publication via the event bus.
- Create a ranged enemy archetype (e.g., BoneHexer caster) with windup/telegraph before firing projectiles.
- Render projectiles distinctly (color/scale) using existing assets or simple primitives; ensure they respect virtual resolution.
- Integrate the ranged archetype into wave config/spawning with tunable stats (speed, HP, fire rate, projectile speed/damage).

## Non Goals
- Advanced AI behaviors (kiting, pathfinding) beyond simple spacing + fire.
- Homing or complex projectile patterns.
- New audio/VFX passes beyond minimal readability.

## Acceptance criteria
- [x] Projectiles exist as ECS entities with movement, lifetime, and hit detection; on hit they publish damage events and clean up.
- [x] Ranged enemy spawns via wave config and alternates between moving/spacing and firing after a windup; telegraph is visible.
- [x] Projectile visuals are readable and scaled correctly; collisions do not hit allies.
- [x] Melee enemies remain functional; new systems do not regress existing combat.
- [x] `dotnet build` passes.

## Definition of done
- Builds pass (`dotnet build`)
- Tests/play check done (if applicable)
- Docs updated (if applicable)
- Handoff notes added (if handing off)

## Plan
- Step 1: Define projectile components (position, velocity, hitbox, lifetime, faction/source) and a projectile update/collision system that emits `EntityDamagedEvent`.
- Step 2: Add a ranged enemy archetype with fire-rate/windup config; implement simple AI to stop/space and spawn projectiles.
- Step 3: Render projectiles (primitive or sprite) and ensure cleanup on hit/expiry; tune damage/speed.
- Step 4: Wire archetype into wave config and playtest with melee enemies; run `dotnet build`.

## Notes / Risks / Blockers
- Prereq: complete collision tasks 023–026 before starting this; projectile hit handling depends on that system.
- Avoid double-damage per projectile; ensure it deactivates on first hit.
- Consider friendly-fire rules; default to no ally hits unless explicitly allowed.
- Keep telegraph duration readable to offset ranged pressure; balance alongside melee spawn counts.

## Handoff notes (2024-12-07)
### Implementation Summary
Successfully implemented complete projectile and ranged enemy system:

**New Components:**
- `Projectile` - tracks projectile state, damage, faction, lifetime, and hit status
- `ProjectileVisual` - rendering properties (color, radius)
- `RangedAttacker` - AI state for ranged enemies (windup timer, optimal range, projectile config)

**New Systems:**
- `ProjectileUpdateSystem` - manages projectile lifetimes and cleanup
- `ProjectileHitSystem` - handles collision events and damage application (no friendly fire)
- `RangedAttackSystem` - AI for ranged enemies (spacing, windup, firing)
- `ProjectileRenderSystem` - renders projectiles and windup telegraphs

**Configuration:**
- Extended `EnemyArchetype` with optional `RangedAttackDefinition` (projectile speed, damage, optimal range, windup)
- Added `BoneMage` archetype: purple-tinted enemy that fires projectiles at 140px range with 0.6s windup
- Unlocks at wave 3 with 0.8x spawn weight

**Integration:**
- Systems registered in `EcsWorldRunner` in proper order (ranged AI before melee AI, projectile hit after collision)
- Projectiles use `CollisionLayer.Projectile` vs `CollisionLayer.Player` (enemy projectiles) or vice versa
- Visual telegraph: pulsing orange circle during windup (scales with progress)
- Projectiles destroyed on hit or after 5s lifetime

**Balance:**
- BoneMage: 65 speed, 20 HP, 2.5s attack cooldown
- Projectiles: 180 speed, 12 damage, 4px collision radius
- Optimal range: 140px (enemy backs up if too close, approaches if too far)

### Testing
- `dotnet build` passes cleanly
- All existing melee systems remain functional (no regressions)
- Projectiles respect faction filtering (no friendly fire)

### Next Steps
- Playtest balance: projectile speed/damage vs player mobility
- Consider adding projectile collision with world static geometry (currently only hits players/enemies)
- Optional: add projectile trail VFX or sprite assets beyond primitive circles

