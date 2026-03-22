# Task: 088 - Complete or disable incomplete hub interaction flows
- Status: done

## Summary
The hub currently exposes interactions and menu actions that are still TODOs, including shop/settings/quit-related paths. Dead interaction points weaken the hub experience and make the game feel less trustworthy during active development.

## Goals
- Remove no-op hub interactions and menu actions from player-facing flows
- Either implement the intended behavior or gate the feature clearly until ready
- Keep hub UX internally consistent across keyboard/UI/NPC interactions
- Document any intentional temporary locks in code and design docs

## Non Goals
- Broad redesign of hub layout or NPC placement
- Full implementation of unrelated hub content polish
- Adding placeholder fallbacks that silently fail

## Acceptance criteria
- [x] No hub interaction or menu entry exposed to players results in a no-op TODO path
- [x] Shop/settings/quit-related flows are either implemented end-to-end or explicitly disabled with clear player feedback
- [x] Keyboard, NPC proximity, and menu-driven access paths remain behaviorally consistent
- [x] GDD/task notes are updated if the hub flow changes materially

## Definition of done
- Builds pass (`dotnet build`)
- Tests/play check done (if applicable)
- Docs updated (if applicable)
- Handoff notes added (if handing off)

## Plan
- Step 1: Audit current incomplete hub interactions and menu actions
- Step 2: Decide per flow whether to implement now or hide/lock it until later
- Step 3: Wire the final behavior consistently across ECS/UI entry points and update docs

## Notes / Risks / Blockers
- Some paths overlap with existing hub backlog tasks; align implementation order to avoid duplicate UI work
- If a feature is intentionally hidden, ensure the player cannot still reach it through alternate inputs

## Handoff Notes
- Current status: complete
- Files changed:
  - `src/Game/Core/Ecs/Runtime/EcsHubRuntimeModule.cs`
  - `src/Game/Core/Ecs/Systems/HubMenuSystem.cs`
  - `src/Game/Core/Ecs/Systems/InteractionInputSystem.cs`
  - `src/Game/Core/Ecs/Systems/InventoryUiSystem.cs`
  - `src/Game/Core/Ecs/Systems/PerkTreeUISystem.cs`
  - `src/Game/Core/Ecs/Systems/ProximityPromptRenderSystem.cs`
  - `src/Game/Core/Ecs/Systems/SkillSelectionUISystem.cs`
  - `src/Game/Core/Ecs/Systems/HubModalState.cs`
  - `src/Game/Core/Ecs/Systems/HubSettingsMyraSystem.cs`
  - `docs/game-design-document.md`
- Tests/build/manual checks run:
  - `dotnet run --project src/Game` reached the `MainMenu -> Hub` transition, loaded `Tiles/Maps/HubMap`, collision, and NPC spawn without the previous duplicate `SessionSettingsState` runtime exception
  - `dotnet build` succeeded
- Next concrete step:
  - Manual smoke-test the hub flows in-game: ESC menu -> settings open/close, return to main menu, vendor lock messaging, and hub modal blocking
- Decisions made:
  - Reused the shared tabbed settings overlay in hub instead of adding a hub-only settings implementation
  - Replaced the dead hub quit path with a working `Return to Main Menu` flow
  - Left the vendor visible but explicitly locked with `Shop coming soon` feedback until the real shop UI exists
- Risks/blockers:
  - The modal-blocking pass covers the touched hub entry points, but broader hub/manual UX validation is still needed in the running game
  - Follow-up runtime composition cleanup was required after wiring hub settings into the shared `SettingsMenuSystem`; `SessionSettingsState` ownership now lives in `EcsCommonRuntimeModule` so hub and stage both consume the same session-scoped settings state without duplicate registration.
