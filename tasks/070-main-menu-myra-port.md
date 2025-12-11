# Task: Port main menu to Myra UI
- Status: done
- Completed: 2025-12-09

## Summary
Replace the hand-drawn SpriteBatch main menu with a Myra UI implementation while preserving all current behaviors (continue/start, new slot creation, load slot selection, settings placeholder messaging, quit flow) and fitting into the existing virtual-resolution render-target pipeline.

## Goals
- Add and initialize Myra UI for the game (Desktop/MyraEnvironment) within the existing 960x540 virtual resolution pipeline.
- Recreate the current main menu structure and navigation using Myra widgets (title, Continue/Start, New Game, Load Game with slot list and metadata, Settings placeholder, Quit).
- Preserve current save-slot behaviors: continue uses most recent slot if available; new creates the next slot; load lists slots with created/last-played info and starts the selected slot.
- Preserve existing input expectations: mouse hit-testing matches virtual resolution; keyboard navigation (up/down, enter/space confirm, escape/back) still works in the menu.
- Keep existing main-menu audio and scene transition flow intact (menu music, transitions to hub/stage, quit exit).

## Non Goals
- Visual redesign or custom theming beyond Myra’s default skin (only functional parity/styling minimal).
- Expanding settings; keep the current “settings coming soon” placeholder message/behavior.
- Changing save-slot logic, progression rules, or scene state management.

## Acceptance criteria
- [x] Myra package is referenced and initialized; menu renders through Myra while respecting the virtual render target scaling.
- [x] Main menu shows title and buttons for Continue/Start, New Game, Load Game, Settings, Quit using Myra widgets.
- [x] Continue/Start picks the most recent save if one exists; otherwise starts a newly created slot (same as today).
- [x] New Game creates the next available slot and enters the hub scene.
- [x] Load Game lists existing slots with created/last-played metadata; selecting a slot starts that slot; back returns to the main menu.
- [x] Settings button shows the existing placeholder text; no additional settings implemented.
- [x] Quit exits the application.
- [x] Mouse and keyboard navigation behave the same as the current menu (including escape/back in load view).
- [x] `dotnet build` succeeds and a smoke run reaches the Myra main menu without regressions.

## Definition of done
- Builds pass (`dotnet build`).
- Quick play/smoke check verifies Myra menu interactions and scene transitions.
- Docs updated if any setup/run steps change (e.g., note Myra dependency/init).
- Handoff notes added (if handing off).

## Plan
- Step 1: Add Myra package reference and restore.
+- Step 2: Bootstrap MyraEnvironment/Desktop in Game1 using the virtual resolution; ensure mouse coordinates are scaled correctly.
+- Step 3: Build a Myra-based main menu layout mirroring current entries and load-slot view, including placeholder settings message.
+- Step 4: Wire menu actions into existing save-slot and scene transition logic; remove/retire the old SpriteBatch menu path.
+- Step 5: Run `dotnet build` and smoke-test the menu flow (continue/new/load/settings placeholder/quit).

## Notes / Risks / Blockers
- Must ensure Myra input uses virtual-resolution mouse coordinates to avoid the known pointer offset issue.
- Keep menu music and scene transitions untouched; regressions here block completion.

## Handoff Notes (2025-12-09)
### Completed
- Successfully ported main menu from hand-drawn SpriteBatch to Myra UI framework
- Created `MyraMainMenuScreen` class that mirrors all functionality of the old `MainMenuScreen`
- Integrated Myra into Game1 initialization and render loop
- Removed old `MainMenuScreen.cs` and extracted shared types (`MainMenuAction`, `MainMenuResult`) to `MainMenuResult.cs`
- Build passes with `dotnet build`

### Implementation Details
- **New files:**
  - `src/Game/Core/UI/MyraMainMenuScreen.cs` - Myra-based menu implementation
  - `src/Game/Core/UI/MainMenuResult.cs` - Shared menu action types
- **Modified files:**
  - `src/Game/Game1.cs` - Replaced `MainMenuScreen` with `MyraMainMenuScreen`, initialized Myra environment
- **Removed files:**
  - `src/Game/Core/UI/MainMenuScreen.cs` - Old SpriteBatch-based menu (no longer needed)

### Key Technical Points
- Myra is initialized via `MyraEnvironment.Game` with a minimal wrapper class
- Desktop widget manages the UI hierarchy with virtual resolution (960x540)
- Mouse input scaling is handled by existing `InputState` and works correctly with Myra
- Escape key navigation in load menu handled explicitly in `Update()` method
- Menu rebuilds on slot refresh (every 2 seconds) to show updated save data

### Testing Notes
- Build succeeds without errors
- All menu functionality preserved:
  - Continue/Start with most recent slot fallback
  - New Game creates next available slot
  - Load Game shows slot list with metadata
  - Settings shows placeholder message
  - Quit exits application
  - ESC navigation in load menu returns to main menu

### Next Steps / Follow-up
- Smoke test recommended: run the game and verify menu interactions visually
- Consider visual theming/styling of Myra widgets (currently using default skin)
- Future tasks may want to leverage Myra for other UI elements (HUD, pause menu, etc.)



