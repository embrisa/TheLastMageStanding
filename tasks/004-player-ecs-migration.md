# Task: 004 - Player ECS migration
- Status: done

## Summary
Move the player into the new ECS so input, movement, camera follow, health, and attack intent are component-driven rather than hardcoded classes.

## Goals
- Add player-facing components (PlayerTag, InputIntent, MoveSpeed, AttackStats, Health, Hitbox, Sprite/DebugRender).
- Build input system to write movement/attack intents; movement system consumes velocity/move speed.
- Add camera follow system keyed on the player entity.
- Render player via ECS (placeholder sprite + health bar) and remove legacy `PlayerCharacter` usage.

## Non Goals
- Enemy AI, waves, or combat resolution (handled later).
- Final art/animations; placeholders are fine.

## Acceptance criteria
- Player is spawned through an ECS factory with the required components.
- WASD/arrow + space/left-click drives movement and attack intent through ECS systems.
- Camera follows the player via a system, not direct references.
- Legacy player class is no longer used in the main loop.

## Definition of done
- Builds pass (`dotnet build`)
- Tests/play check done (if applicable)
- Docs updated (if applicable)
- Handoff notes added (if handing off)

## Plan
- Define player components and factory.
- Implement input → intent system, movement system, and camera follow system.
- Hook ECS rendering for the player with debug visuals/health bar.
- Remove `PlayerCharacter` usage from world/game loop and verify control still works.

## Notes / Risks / Blockers
- Ensure camera utilities remain compatible with ECS world coordinates.
- Keep intent stateless per frame to avoid sticky inputs.
- Completed via ECS: player factory, input/movement + attack intents, camera follow, ECS render; debug spawn removed.

