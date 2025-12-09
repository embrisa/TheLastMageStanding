# Task: 065 - Hub menu actions (settings and quit)
- Status: backlog

## Summary
Implement the actions for the hub menu (ESC menu) created in Task 051. Currently the menu displays but "Settings" and "Quit to Desktop" options don't do anything. Need to wire up settings UI navigation and proper game exit.

## Goals
- Implement "Settings" option to open AudioSettingsMenu UI
- Implement "Quit to Desktop" option to exit game gracefully
- Ensure ESC key properly toggles hub menu open/closed
- Add confirmation dialog for quit (optional but recommended)

## Non Goals
- New settings beyond audio (graphics, keybinds - future tasks)
- Hub menu reorganization or visual polish
- Save-on-quit prompts (auto-save already exists)

## Acceptance criteria
- [ ] Pressing ESC in hub opens hub menu
- [ ] Selecting "Settings" opens audio settings UI
- [ ] Audio settings allow volume adjustments (master, music, SFX)
- [ ] Changes to audio settings persist to config file
- [ ] Selecting "Quit to Desktop" exits game cleanly
- [ ] Optional: Quit shows confirmation dialog "Are you sure?"
- [ ] ESC closes hub menu and returns to hub exploration
- [ ] `dotnet build` passes; manual playtest confirms functionality

## Definition of done
- Builds pass (`dotnet build`)
- Tests/play check done
- Docs updated (if applicable)
- Handoff notes added

## Plan
- Step 1: Check if AudioSettingsMenu UI already exists (likely from Task 038)
- Step 2: Wire HubMenuSystem "Settings" to toggle AudioSettingsMenu visibility
- Step 3: Implement quit logic in HubMenuSystem.HandleMenuSelection
- Step 4: Add Game.Exit() call or equivalent for quit
- Step 5: Optional: Add confirmation dialog component for quit
- Step 6: Test ESC navigation flow (hub → menu → settings → back)
- Step 7: Verify audio settings persist on close
- Step 8: Verify quit doesn't leave orphaned processes

## Notes / Risks / Blockers
- **Dependency**: AudioSettingsMenu likely exists from Task 038 (extended sound settings)
- **Risk**: Need to ensure proper cleanup on exit (dispose resources, save profile)
- **UX**: Quit confirmation prevents accidental exits (recommended)
- **Tech**: Check if Game1.Exit() is the correct method for clean shutdown
