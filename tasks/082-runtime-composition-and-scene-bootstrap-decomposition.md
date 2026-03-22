# Task: 082 - Runtime composition and scene bootstrap decomposition
- Status: in_progress (implementation complete, manual smoke test pending)

## Summary
`Game1`, `SceneRuntimeService`, and `EcsWorldRunner` currently share runtime composition, slot activation, scene bootstrap, and world construction responsibilities. This concentrates lifecycle logic into a few large classes and makes feature work depend on implicit boot order and growing constructor signatures.

## Goals
- Reduce constructor and service wiring sprawl across `Game1`, `SceneRuntimeService`, and `EcsWorldRunner`
- Move runtime/world creation into explicit factories or composition roots
- Keep scene bootstrap responsibilities clear between main menu, hub, and stage paths
- Make slot activation and scene reload behavior easier to reason about and test

## Non Goals
- Reworking gameplay systems unrelated to runtime composition
- Redesigning scene flow or campaign rules
- Adding compatibility layers for old bootstrap paths

## Acceptance criteria
- [x] `Game1` is limited to MonoGame lifecycle, top-level rendering, and high-level routing
- [x] Scene/world creation is delegated to explicit composition helpers instead of long constructor parameter lists
- [x] `SceneRuntimeService` no longer owns unrelated concerns such as full runtime composition plus map/music/bootstrap orchestration in one class
- [x] Slot activation, scene transition handling, and world lifetime rules are documented in code and covered by targeted tests where practical

## Definition of done
- Builds pass (`dotnet build`)
- Tests/play check done (if applicable)
- Docs updated (if applicable)
- Handoff notes added (if handing off)

## Plan
- Step 1: Map current responsibilities across `Game1`, `SceneRuntimeService`, and `EcsWorldRunner`
- Step 2: Introduce dedicated runtime factory/composition types and move wiring into them
- Step 3: Simplify the scene bootstrap flow and add regression coverage for slot/scene transitions

## Notes / Risks / Blockers
- Overlaps with existing refactor tasks around bootstrap and ECS runtime modularization; coordinate to avoid parallel rewrites of the same files
- Main risk is breaking scene transitions or slot-scoped persistence during extraction

## Handoff
- Current status: implementation complete. `Game1` now delegates runtime construction to a dedicated composition root, ECS world creation is slot-scoped behind `EcsWorldFactory`, and active-slot tracking is isolated in `ActiveSaveSlotController`. Manual smoke coverage for menu → hub → stage transitions is still pending.
- Files changed: `src/Game/Game1.cs`, `src/Game/Core/Composition/GameRuntime.cs`, `src/Game/Core/Composition/GameRuntimeFactory.cs`, `src/Game/Core/Ecs/EcsWorldFactory.cs`, `src/Game/Core/Ecs/EcsWorldRunner.cs`, `src/Game/Core/SceneState/ActiveSaveSlotController.cs`, `src/Game/Core/SceneState/SceneRuntimeService.cs`, `src/Game.Tests/SceneState/ActiveSaveSlotControllerTests.cs`, `docs/game-design-document.md`, `tasks/082-runtime-composition-and-scene-bootstrap-decomposition.md`, `TASKS.md`.
- Tests/build/manual checks run: `dotnet test src/Game.Tests/TheLastMageStanding.Game.Tests.csproj --no-restore`, `dotnet build`.
- Next concrete step: run an interactive smoke test covering slot selection from the main menu, new-slot creation, hub entry, stage entry, return to hub, and return to main menu.
- Decisions made: kept `Game1` as the MonoGame/render boundary only; moved runtime wiring into `GameRuntimeFactory`; replaced `EcsWorldRunner`'s long constructor with a dependency record created by `EcsWorldFactory`; separated active-slot ownership from scene bootstrap so slot changes explicitly invalidate the current world.
- Risks/blockers: manual verification is still needed for scene transition behavior because this refactor changed bootstrap ownership and world replacement timing.
