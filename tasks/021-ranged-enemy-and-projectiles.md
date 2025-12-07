# Task: 021 - Ranged enemy and projectiles
- Status: backlog

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
- [ ] Projectiles exist as ECS entities with movement, lifetime, and hit detection; on hit they publish damage events and clean up.
- [ ] Ranged enemy spawns via wave config and alternates between moving/spacing and firing after a windup; telegraph is visible.
- [ ] Projectile visuals are readable and scaled correctly; collisions do not hit allies.
- [ ] Melee enemies remain functional; new systems do not regress existing combat.
- [ ] `dotnet build` passes.

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
- Avoid double-damage per projectile; ensure it deactivates on first hit.
- Consider friendly-fire rules; default to no ally hits unless explicitly allowed.
- Keep telegraph duration readable to offset ranged pressure; balance alongside melee spawn counts.

