# Task: 077 - Refactor Game1 bootstrap, scene loading, and shared settings flow
- Status: in_progress (implementation complete, manual smoke test pending)

## Summary
Reduce `Game1`'s responsibilities by extracting scene/bootstrap logic, world initialization, and shared settings orchestration. `Game1` currently coordinates rendering, scene reloads, slot activation, map loading, audio switching, and a second settings flow for the main menu.

## Goals
- Extract scene bootstrap/reload responsibilities out of `Game1`
- Centralize shared settings application used by both main menu and in-run flows
- Reduce duplicate audio/input/video handling between `Game1` and runtime systems
- Keep the MonoGame boundary thin and focused on lifecycle and rendering

## Non Goals
- Replacing MonoGame lifecycle ownership
- Redesigning the main menu UI
- Deep ECS runtime restructuring beyond what is necessary for bootstrap extraction
- Reworking campaign/stage data

## Acceptance criteria
- [x] `Game1` no longer owns most scene reload/bootstrap details directly
- [x] Map loading, player spawn placement, collision loading, and scene-specific initialization are handled by extracted services/controllers
- [x] Main-menu settings and in-run settings share the same config application path where appropriate
- [x] Save-slot activation and world initialization logic are easier to follow and test
- [x] `Game1` is materially smaller and focused on app lifecycle and top-level rendering
- [ ] `dotnet build` passes; manual smoke test confirms main menu, hub entry, stage entry, and settings still work

## Definition of done
- Builds pass (`dotnet build`)
- Tests/play check done
- Docs updated (if applicable)
- Handoff notes added

## Plan
- Step 1: Identify bootstrap concerns currently embedded in `Game1`
- Step 2: Extract scene reload/world bootstrap service(s)
- Step 3: Move shared settings application into reusable service(s)
- Step 4: Update `Game1` to delegate bootstrap and settings work
- Step 5: Verify slot selection, menu transitions, hub load, and stage load flows
- Step 6: Run build and manual smoke test

## Notes / Risks / Blockers
- **Risk**: Scene loading touches map services, NPC spawning, collision setup, and music transitions
- **Dependency**: Task 075 and Task 076 should inform where new services live
- **Design**: Keep MonoGame types at the boundary where practical, per repo conventions

## Handoff Notes
- Current status: Bootstrap, scene loading, and shared settings orchestration are extracted out of `Game1`. Follow-up hardening also tightened runtime teardown: `SceneRuntimeService` now disposes replaced/disposed `EcsWorldRunner` instances, and returning to the main menu tears down the active world so stale ECS subscriptions do not survive behind the menu. Build passes. Manual smoke coverage for main menu, hub, stage entry, and settings is still pending.
- Files changed:
  - `src/Game/Game1.cs`
  - `src/Game/Core/Config/GameSettingsController.cs`
  - `src/Game/Core/Config/VideoSettingsApplier.cs`
  - `src/Game/Core/SceneState/SceneRuntimeService.cs`
  - `src/Game/Core/Ecs/EcsWorldRunner.cs`
  - `src/Game/Core/Events/ScopedEventBus.cs`
  - `src/Game.Tests/Events/ScopedEventBusTests.cs`
  - `src/Game.Tests/MetaProgression/MetaProgressionManagerTests.cs`
- Tests/build/manual checks run:
  - `dotnet test src/Game.Tests/TheLastMageStanding.Game.Tests.csproj --filter "FullyQualifiedName~ScopedEventBusTests|FullyQualifiedName~MetaProgressionManagerTests|FullyQualifiedName~StageContentResolverTests|FullyQualifiedName~PauseMenuSettingsOwnershipTests|FullyQualifiedName~EquipmentPersistenceTests|FullyQualifiedName~PerkPersistenceTests|FullyQualifiedName~PersistenceRootTests"`
  - `dotnet build`
  - Manual smoke test not run in this environment
- Next concrete step:
  - Run an interactive smoke test covering main-menu slot selection, new-slot start, hub load, stage start, return to hub/main menu, and settings changes from both main menu and in-run UI
- Decisions made:
  - Extracted app-boundary video/input application into `GameSettingsController` + `VideoSettingsApplier`
  - Extracted save-slot activation, ECS world creation, map loading, collision loading, spawn placement, and hub NPC spawn into `SceneRuntimeService`
  - Kept `Game1` focused on MonoGame lifecycle, render passes, and top-level menu delegation
  - Reused the shared `RuntimeSettingsService` path for main-menu and in-run settings, while avoiding duplicate runtime mutation for event-bus-driven video/input changes
  - Scoped ECS subscriptions through `ScopedEventBus` so world replacement and shutdown dispose cleanly without leaking handlers into later runtimes
- Risks/blockers:
  - Manual verification is still needed for scene transitions and settings interactions because this refactor moved several previously inlined runtime responsibilities behind new services
