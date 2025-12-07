# Task: 006 - Enemy smearing investigation & fix
- Status: done

## Summary
Enemies leave smeared trails/healthbars across the screen and cause lag. We need to investigate the rendering artifacts and performance issue, then implement a fix so enemies render cleanly without trails and frame rate remains stable.

## Goals
- Reproduce the smearing/trailing artifact for enemies and health bars.
- Identify the root cause (render target clearing, SpriteBatch usage, entity rendering logic, or ECS state).
- Implement a fix so enemies/health bars render without trails at all times.
- Validate performance remains stable after the fix.

## Non Goals
- Final art/animation polish.
- Broad rendering overhaul beyond what’s needed to fix smearing.

## Acceptance criteria
- [x] Enemies and their health bars do not smear/ghost across frames during movement.
- [x] Frame rate remains stable after the fix (no regression vs current baseline).
- [x] Repro steps and fix are documented in the task notes.

## Definition of done
- Builds pass (`dotnet build`)
- Tests/play check done (if applicable)
- Docs updated (if applicable)
- Handoff notes added (if handing off)

## Plan
- Reproduce the issue and capture conditions (camera, count, settings).
- Inspect render pipeline (clear calls, SpriteBatch usage, render target settings) and ECS render logic for state leakage.
- Implement and verify the fix; retest to confirm no smearing and acceptable perf.
- Document findings, fix, and any tunables.

## Notes / Risks / Blockers
- Repro: run the game and let a wave spawn; the same spawn requests re-fire every frame, causing enemy counts to explode and leaving smeared enemy/health-bar trails plus heavy lag.
- Root cause: `EcsWorld.ForEach` wrote components back even after entities were destroyed, so `EnemySpawnRequest` entities stuck around and spawned a new enemy every frame.
- Fix: skip component writes for dead entities during `ForEach` and always purge component pools in `DestroyEntity` so destroyed entities and their components stay removed.
- Validation: `dotnet build` (pass); play/perf check verified in-game—no smearing, enemies follow, damage applies, and frame rate stays stable.

