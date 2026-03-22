# Task: 084 - UI feature state, controller, and view split
- Status: done

## Summary
Large UI classes and UI-facing ECS systems currently mix input handling, modal state, rendering, layout, and persistence concerns. This is especially visible in skill selection, stage selection, and HUD-related code, which makes UI changes slower and increases regression risk.

## Goals
- Split large UI features into clearer state, controller, and view responsibilities
- Keep ECS systems focused on game-state coordination instead of direct UI orchestration
- Reduce the size and responsibility count of large Myra and HUD classes
- Make UI features easier to test without a full rendering path

## Non Goals
- Visual redesign of existing hub/stage UI
- Replacing Myra as the UI framework
- Adding new feature scope to existing UI flows

## Acceptance criteria
- [x] At least the largest current UI flows (`SkillSelection`, `StageSelection`, and HUD/pause overlays as applicable) are decomposed into smaller classes with clear responsibilities
- [x] Persistence/profile updates are handled outside raw view/layout classes
- [x] Input/navigation logic is separated from rendering/layout code
- [x] Regression coverage exists for the extracted state/controller logic where practical

## Definition of done
- Builds pass (`dotnet build`)
- Tests/play check done (if applicable)
- Docs updated (if applicable)
- Handoff notes added (if handing off)

## Plan
- Step 1: Inventory the largest UI classes and decide extraction boundaries per feature
- Step 2: Move state transitions and command handling into feature controllers/view models
- Step 3: Shrink view classes to layout/render concerns and add targeted tests for controller logic

## Notes / Risks / Blockers
- Completed:
  - Extracted `SkillSelectionController` and `StageSelectionController` so ECS systems now coordinate world state and side effects instead of owning UI navigation logic.
  - Converted `MyraStageSelectionScreen` into a view-only surface that applies prepared view state and emits intents; removed campaign/profile reads from the screen.
  - Kept pause/HUD on the existing split path; `PauseMenuMyraSystem` already matched the controller/view separation expected by this task.
- Files changed:
  - `src/Game/Core/UI/SkillSelectionController.cs`
  - `src/Game/Core/UI/StageSelectionController.cs`
  - `src/Game/Core/UI/MyraStageSelectionScreen.cs`
  - `src/Game/Core/UI/Myra/MyraSkillSelectionScreen.cs`
  - `src/Game/Core/Ecs/Systems/SkillSelectionUISystem.cs`
  - `src/Game/Core/Ecs/Systems/StageSelectionUISystem.cs`
  - `src/Game.Tests/UI/UiFeatureControllerTests.cs`
- Verification:
  - `dotnet test src/Game.Tests/TheLastMageStanding.Game.Tests.csproj --filter UiFeatureControllerTests`
  - `dotnet test src/Game.Tests/TheLastMageStanding.Game.Tests.csproj --filter PauseMenuSettingsOwnershipTests`
  - `dotnet build`
- Next concrete step:
  - Apply the same controller/view extraction pattern to remaining larger Myra hub flows if they continue to accumulate behavior.
- Decisions made:
  - Kept persistence and scene transitions inside ECS systems/services, not inside Myra view classes.
  - Limited scope to the largest mixed-responsibility flows instead of refactoring every UI system in one pass.
- Risks/blockers:
  - No manual smoke check was run in this task; stage selection and skill selection should still get a quick in-game validation for click and keyboard parity.
