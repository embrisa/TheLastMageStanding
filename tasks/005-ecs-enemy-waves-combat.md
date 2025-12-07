# Task: 005 - ECS enemy waves & combat
- Status: done

## Summary
Port enemies, wave spawning, and combat/collision logic into ECS so chasing mobs, spawning cadence, and damage exchange run through components and systems.

## Goals
- Enemy factory with Position, Velocity, Health, Hitbox, Faction, AISeekTarget, AttackStats, RenderDebug components.
- Wave system using config (interval, count growth, spawn radii, enemy stats) to enqueue spawns; spawn system instantiates entities.
- AI seek/chase system targeting the player faction; collision/overlap system applying contact damage and attack cooldowns.
- Death/cleanup system to remove dead entities; render health bars/placeholders via ECS.

## Non Goals
- Advanced pathfinding/steering, loot drops, XP/meta, or VFX/SFX.
- Projectile/ranged attacks (unless trivially reused from melee hitbox overlap).

## Acceptance criteria
- Waves spawn automatically on a timer; enemy counts grow per wave per config.
- Enemies chase the player and apply contact damage on overlap; player attacks damage enemies via ECS components.
- Dead entities are removed/hidden; simple health bars or color shifts show HP.
- Wave and enemy tunables live in config and can be adjusted without code changes.

## Definition of done
- Builds pass (`dotnet build`)
- Tests/play check done (if applicable)
- Docs updated (if applicable)
- Handoff notes added (if handing off)

## Plan
- Add enemy archetype/factory and wave config definitions.
- Implement wave timer → spawn queue system; spawn processor to create entities at offsets.
- Add AI seek/chase system and collision/combat system using Hitbox/Faction/AttackStats.
- Implement death/cleanup and ECS rendering for enemies with debug bars; verify loop end-to-end.

## Notes / Risks / Blockers
- Keep collision broad-phase simple initially; optimize if entity counts grow.
- Ensure attack cooldowns are per-entity to avoid burst damage stacking.

## Testing
- dotnet build

