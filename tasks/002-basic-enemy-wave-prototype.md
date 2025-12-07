# Task: 002 - Basic enemy wave prototype
- Status: done

## Summary
Create a minimal enemy wave loop to validate combat pacing: spawn basic mobs, move toward player, and handle simple damage/death with placeholder visuals.

## Goals
- Spawn recurring waves with adjustable rate and count
- Enemies path toward player and can be kited
- Basic hit/damage flow between player and enemies
- Temporary visuals (shapes/dots) acceptable to prove loop

## Non Goals
- Final art, VFX, or SFX
- Loot drops, experience, or meta progression
- Advanced AI behaviors beyond seek/chase

## Acceptance criteria
- [x] Spawns trigger automatically over time without input
- [x] Enemies move toward player and collide/overlap is handled simply
- [x] Player can damage/kill enemies; enemies can damage player
- [x] Basic health values visible via debug text/log or simple color change
- [x] Wave parameters configurable in code (spawn interval, count, enemy speed/HP/damage)

## Definition of done
- Builds pass (`dotnet build`)
- Tests/play check done (if applicable)
- Docs updated (if applicable)
- Handoff notes added (if handing off)

## Plan
- Add simple enemy entity with movement toward player and health/damage fields
- Implement spawn manager with timer-driven waves and tunable parameters
- Add collision/damage exchange between player and enemies with debug feedback
- Expose tuning constants for easy iteration

## Notes / Risks / Blockers
- Without art, rely on debug shapes/colors for clarity

## Testing
- `dotnet build`

