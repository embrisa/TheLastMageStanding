# Task: 083 - Stage run reset service and default state restoration
- Status: done

## Summary
`SessionStateSystem` currently hardcodes run reset behavior using literal baseline values for player movement, attack, health, XP, and cleanup rules. That approach will drift from profile/config-derived defaults as progression systems grow and makes restarts fragile.

## Goals
- Replace hardcoded reset literals with a single source of truth for run-scoped defaults
- Separate entity cleanup from player-state restoration
- Ensure stage starts and restarts rebuild valid run state from configuration/profile data
- Make reset behavior observable through targeted tests

## Non Goals
- Rebalancing progression numbers
- Redesigning death/game-over UX
- Introducing backward compatibility for obsolete run-state formats

## Acceptance criteria
- [x] Session restart and new-stage reset paths do not hardcode player baseline values inside `SessionStateSystem`
- [x] A dedicated reset service or equivalent rebuild path restores run-scoped player/session state from config/profile sources
- [x] Enemy/orb cleanup and player stat reset responsibilities are separated clearly
- [x] Tests cover restart/new-stage reset behavior for player stats, XP, status effects, and session counters

## Definition of done
- [x] Builds pass (`dotnet build`)
- [x] Tests/play check done (if applicable)
- [x] Docs updated (if applicable)
- [x] Handoff notes added (if handing off)

## Plan
- Step 1: Identify all run-scoped components reset during restart/new-stage flow
- Step 2: Introduce a reset/restoration service based on progression/profile/config inputs
- Step 3: Update session/state systems to call the new path and add regression tests

## Notes / Risks / Blockers
- Must preserve the distinction between hub-scoped state, slot-scoped state, and run-scoped state
- Hidden dependencies may exist in systems that currently assume restart side effects from `SessionStateSystem`

## Handoff notes
- Completed: introduced `StageRunResetService` and `StageRunEntityCleanupService`, with `SessionStateSystem` delegating restart/new-stage flows to those services instead of hardcoding player baselines.
- Source of truth: `PlayerEntityFactory.CreateRunScopedDefaults()` now defines the player run baseline used both for initial player creation and later run restoration, including XP progression values from `ProgressionConfig`.
- Reset scope: player run state now restores position/velocity, attack/health/xp, dash state, skill cooldowns, hurtbox/animation transient state, session counters, and clears transient status/buff/casting state while preserving hub/slot-scoped loadout and progression data.
- Files changed:
  - `src/Game/Core/Ecs/PlayerEntityFactory.cs`
  - `src/Game/Core/Ecs/StageRunResetService.cs`
  - `src/Game/Core/Ecs/StageRunEntityCleanupService.cs`
  - `src/Game/Core/Ecs/Systems/SessionStateSystem.cs`
  - `src/Game/Core/Ecs/EcsWorldRunner.cs`
  - `src/Game.Tests/Ecs/StageRunResetServiceTests.cs`
  - `docs/game-design-document.md`
- Verification:
  - `dotnet build`
  - `dotnet test src/Game.Tests/TheLastMageStanding.Game.Tests.csproj --no-build --filter StageRunResetServiceTests`
- Decisions made:
  - Enemy and XP orb cleanup remain a separate concern from player/session restoration.
  - Run defaults are pulled from the same factory/config path that seeds the initial player entity to avoid drift.
  - Level-up run modifiers/UI continue to clear via the existing `SessionRestartedEvent` subscribers and are covered by the new reset-path test.
- Risks/blockers:
  - Restart-in-place still resets player position to `Vector2.Zero`; stage scene loads immediately reposition to map spawn, but in-stage restart UX may warrant a follow-up task if restart-to-spawn becomes a requirement.
- Next concrete step:
  - Manual stage smoke check for restart/game-over flow to confirm the runtime experience matches the current intended behavior.
