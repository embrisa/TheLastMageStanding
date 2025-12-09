# Task: Port main menu to Myra UI
- Status: backlog

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
- [ ] Myra package is referenced and initialized; menu renders through Myra while respecting the virtual render target scaling.
- [ ] Main menu shows title and buttons for Continue/Start, New Game, Load Game, Settings, Quit using Myra widgets.
- [ ] Continue/Start picks the most recent save if one exists; otherwise starts a newly created slot (same as today).
- [ ] New Game creates the next available slot and enters the hub scene.
- [ ] Load Game lists existing slots with created/last-played metadata; selecting a slot starts that slot; back returns to the main menu.
- [ ] Settings button shows the existing placeholder text; no additional settings implemented.
- [ ] Quit exits the application.
- [ ] Mouse and keyboard navigation behave the same as the current menu (including escape/back in load view).
- [ ] `dotnet build` succeeds and a smoke run reaches the Myra main menu without regressions.

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

