# Task: 084 - UI feature state, controller, and view split
- Status: backlog

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
- [ ] At least the largest current UI flows (`SkillSelection`, `StageSelection`, and HUD/pause overlays as applicable) are decomposed into smaller classes with clear responsibilities
- [ ] Persistence/profile updates are handled outside raw view/layout classes
- [ ] Input/navigation logic is separated from rendering/layout code
- [ ] Regression coverage exists for the extracted state/controller logic where practical

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
- The highest-risk areas are modal hub flows that currently pause gameplay/session state directly
- Coordinate with any parallel work in hub UI tasks to avoid overlapping edits
