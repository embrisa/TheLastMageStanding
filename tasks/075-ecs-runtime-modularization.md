# Task: 075 - ECS runtime modularization and phase cleanup
- Status: in-progress (implementation complete, manual smoke test pending)

## Summary
Refactor the ECS runtime bootstrap so system registration is modular, explicit, and less dependent on one manually ordered list in `EcsWorldRunner`. The current setup makes feature work risky because combat, hub, progression, and UI systems are all wired together in one place and several systems blur update/draw responsibilities.

## Goals
- Split ECS registration into feature-level modules or registrars
- Define explicit runtime phases for update, draw, UI draw, and content loading
- Reduce manual wiring and hidden ordering dependencies in `EcsWorldRunner`
- Clarify which systems own update work versus draw work
- Preserve current gameplay behavior while improving maintainability

## Non Goals
- Rewriting gameplay systems from scratch
- Replacing the custom ECS implementation in this task
- Major balance, content, or UX changes
- Introducing a third-party ECS framework

## Acceptance criteria
- [x] `EcsWorldRunner` no longer hardcodes the full runtime composition in one constructor-sized block
- [x] System registration is organized by feature or module with clear phase boundaries
- [x] Update, draw, UI draw, and load-content phases are explicit in code
- [x] Render-oriented systems are no longer registered in update phases unless there is a clearly named update responsibility
- [ ] Existing stage and hub flows still initialize and run correctly
- [ ] `dotnet build` passes; manual smoke test confirms hub, stage, UI, and scene transitions still work

## Definition of done
- Builds pass (`dotnet build`)
- Tests/play check done
- Docs updated (if applicable)
- Handoff notes added

## Plan
- Step 1: Inventory all currently registered systems and group them by feature and phase
- Step 2: Introduce runtime module/registrar types for hub, stage, combat, progression, UI, and debug
- Step 3: Move system registration from `EcsWorldRunner` into those modules
- Step 4: Add explicit phase containers for update, draw, UI draw, and load-content work
- Step 5: Rename or split systems whose current names hide mixed update/draw responsibilities
- Step 6: Validate initialization, scene switching, and content loading behavior
- Step 7: Run build and smoke test hub and stage flows

## Notes / Risks / Blockers
- **Risk**: This work touches runtime composition and can create subtle ordering regressions if phases are not modeled carefully
- **Dependency**: Related cleanup in Task 076 and Task 077 should align with the new module boundaries
- **Testing**: Manual play checks should cover pause flow, stage start, stage completion, and hub return
- **Design**: Prefer incremental extraction over a one-shot rewrite

## Handoff Notes
- Current status: Runtime composition was extracted into explicit ECS runtime modules and scoped phase registries. `EcsWorldRunner` now orchestrates execution instead of constructing every system inline. Build passes. Manual hub/stage smoke coverage is still pending.
- Files changed:
  - `src/Game/Core/Ecs/EcsWorldRunner.cs`
  - `src/Game/Core/Ecs/Runtime/EcsRuntimeRegistration.cs`
  - `src/Game/Core/Ecs/Runtime/EcsRuntimeModuleContext.cs`
  - `src/Game/Core/Ecs/Runtime/EcsRuntimeComposer.cs`
  - `src/Game/Core/Ecs/Runtime/EcsCommonRuntimeModule.cs`
  - `src/Game/Core/Ecs/Runtime/EcsDebugRuntimeModule.cs`
  - `src/Game/Core/Ecs/Runtime/EcsHubRuntimeModule.cs`
  - `src/Game/Core/Ecs/Runtime/EcsStageRuntimeModule.cs`
  - `src/Game/Core/Ecs/Systems/PlayerAnimationSystem.cs`
  - `src/Game/Core/Ecs/Systems/EnemyAnimationSystem.cs`
  - `src/Game/Core/Ecs/Systems/DamageNumberLifecycleSystem.cs`
  - `src/Game/Core/Ecs/Systems/DamageNumberRenderSystem.cs`
  - `src/Game/Core/Ecs/Systems/PlayerRenderSystem.cs`
  - `src/Game/Core/Ecs/Systems/EnemyRenderSystem.cs`
  - removed `src/Game/Core/Ecs/Systems/DamageNumberSystem.cs`
- Tests/build/manual checks run:
  - `dotnet build` succeeded
  - Manual smoke test not run in this environment
- Next concrete step:
  - Run an interactive smoke test covering hub load, stage start, pause menu, level-up UI, stage completion/death, and return to hub
- Decisions made:
  - Added explicit scoped phase containers for common/hub/stage update, stage session/pre-gameplay/gameplay/hit-stop feedback, draw, UI draw, screen-space UI draw, and load-content
  - Moved system registration into runtime modules for common, debug, hub, and stage concerns
  - Split mixed render/update responsibilities into `PlayerAnimationSystem`, `EnemyAnimationSystem`, `DamageNumberLifecycleSystem`, and `DamageNumberRenderSystem`
- Risks/blockers:
  - Scene-transition and pause-flow regressions are still possible until manual smoke coverage confirms module ordering in live gameplay
