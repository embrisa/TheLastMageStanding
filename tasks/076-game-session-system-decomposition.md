# Task: 076 - Decompose game session, pause, and settings orchestration
- Status: in-progress (implementation complete, manual smoke test pending)

## Summary
Break up `GameSessionSystem` into smaller units with clear responsibilities. The current class owns session state, pause flow, restart flow, notifications, menu state, and audio/video/input settings behavior in one place, which makes changes expensive and increases regression risk.

## Goals
- Split session state, pause menu flow, settings state, and notifications into focused systems/services
- Remove duplicated settings orchestration between in-run UI and other runtime entry points
- Make pause and settings behavior easier to test in isolation
- Keep current user-facing behavior stable during the refactor

## Non Goals
- Redesigning the pause/settings UI
- Adding new settings categories beyond the current scope
- Reworking scene management in this task
- Replacing Myra UI

## Acceptance criteria
- [x] `GameSessionSystem` is reduced substantially or removed in favor of smaller focused systems
- [x] Session state transitions are handled by a dedicated session-focused system/service
- [x] Pause menu input/state is handled independently from settings persistence logic
- [x] Notification/HUD message lifecycle is separated from pause/settings logic
- [x] Audio/video/input settings sync and persistence are shared through a dedicated service instead of embedded menu code
- [ ] `dotnet build` passes; manual playtest confirms pause, resume, restart, and settings still work

## Definition of done
- Builds pass (`dotnet build`)
- Tests/play check done
- Docs updated (if applicable)
- Handoff notes added

## Plan
- Step 1: Identify cohesive responsibility boundaries inside `GameSessionSystem`
- Step 2: Extract a shared settings service for applying and persisting config changes
- Step 3: Split pause menu state/input handling into a dedicated system
- Step 4: Split notification lifecycle into a dedicated system
- Step 5: Reduce session state handling to run state, restart, and game-over concerns
- Step 6: Add or update tests around session and pause flows where practical
- Step 7: Build and manually verify gameplay and menu behavior

## Notes / Risks / Blockers
- **Risk**: Pause and level-up interactions are tightly coupled today and need careful regression testing
- **Dependency**: Task 075 should absorb any runtime registration fallout from this split
- **Testing**: Cover game-over restart, ESC pause toggle, settings open/close, and level-up pause behavior

## Handoff Notes
- Current status: Monolithic `GameSessionSystem` was removed and replaced with focused session, pause, settings, and notification systems. Shared settings application/persistence now routes through `RuntimeSettingsService` and is reused by both stage runtime and main-menu settings. Build and automated tests pass. Manual smoke coverage is still pending.
- Files changed:
  - `src/Game/Game1.cs`
  - `src/Game/Core/Config/RuntimeSettingsService.cs`
  - `src/Game/Core/Ecs/EcsWorldRunner.cs`
  - `src/Game/Core/Ecs/Runtime/EcsRuntimeModuleContext.cs`
  - `src/Game/Core/Ecs/Runtime/EcsStageRuntimeModule.cs`
  - `src/Game/Core/Ecs/Systems/SessionStateSystem.cs`
  - `src/Game/Core/Ecs/Systems/PauseMenuSystem.cs`
  - `src/Game/Core/Ecs/Systems/SettingsMenuSystem.cs`
  - `src/Game/Core/Ecs/Systems/SessionNotificationSystem.cs`
  - `src/Game/Core/Events/SessionEvents.cs`
  - removed `src/Game/Core/Ecs/Systems/GameSessionSystem.cs`
  - `src/Game.Tests/Config/RuntimeSettingsServiceTests.cs`
- Tests/build/manual checks run:
  - `dotnet test --no-build`
  - `dotnet build`
  - Manual smoke test not run in this environment
- Next concrete step:
  - Run an interactive smoke test covering ESC pause toggle, pause resume, restart after death, open/close settings from pause, and main-menu settings changes
- Decisions made:
  - Session state transitions and restart/reset handling moved into `SessionStateSystem`
  - Pause menu state/input and exit intent moved into `PauseMenuSystem`
  - Settings menu synchronization/persistence moved into `SettingsMenuSystem` backed by `RuntimeSettingsService`
  - Wave/game-over notifications and locked-feature message timers moved into `SessionNotificationSystem`
  - Main-menu settings in `Game1` now reuse the same settings service instead of maintaining separate audio/video/input apply logic
- Risks/blockers:
  - Manual validation is still needed for pause/settings interaction timing, especially while level-up UI is open and when switching between pause and settings overlays
