# Task: 045 - Meta Hub UI & Scene
- Status: SUPERSEDED (implemented via Task 051)

**NOTE (2025-12-14):** Task 051 delivered the hub + scene management (and switched the hub from a static menu to a playable hub world with NPC interactions). The remaining work described here was split into focused hub tasks; do not implement this as a separate `MetaHubState`.

**Superseded by**
- Task 051: Hub scene and scene management
- Hub follow-ups: `tasks/HUB_COMPLETION_ARC.md` (and Tasks 061–068)

## Summary
Create the meta hub game state and UI that serves as the central navigation point between runs. Display player meta level, XP, and gold. Provide navigation to shop, talent tree, run history, and the "Start Run" button.

## Goals
- Create `MetaHubState` game state (peer to `GameplayState`, `MainMenuState`).
- Implement meta hub UI layout with top bar (level/XP/gold) and main navigation area.
- Integrate with `PlayerProfileService` to display current profile data.
- Hook up navigation buttons to open shop, talent tree, run history screens.
- Implement "Start Run" button that transitions to gameplay.
- Handle transitions: game over → meta hub, main menu → meta hub.
- Add simple background/visuals for meta hub scene.

## Non Goals
- Shop/equipment UI implementation (Task 046).
- Talent tree UI implementation (Task 047).
- Run history UI implementation (Task 049).
- Complex hub customization or NPCs.
- Meta hub decorations or cosmetics.

## Acceptance criteria
- [ ] `MetaHubState` exists and can be entered after game over or from main menu.
- [ ] Top bar displays meta level, XP bar with progress, and gold amount.
- [ ] Navigation buttons exist for: Shop, Talents, Run History, Settings.
- [ ] "Start Run" button transitions to gameplay (loads profile and starts new run).
- [ ] Profile data (level/XP/gold) updates correctly when returning from runs.
- [ ] UI is responsive and matches game's visual style.
- [ ] `dotnet build` passes.

## Definition of done
- Builds pass (`dotnet build`)
- Play check done (can navigate to hub, view stats, start run)
- Docs updated (hub state, UI layout, navigation flow)
- Handoff notes added (if handing off)

## Plan

### Step 1: Meta Hub State (1 day)
- Create `GameStates/MetaHubState.cs`:
  - Implement `IGameState` interface (or equivalent)
  - Handle initialization: load player profile, set up UI
  - Handle update: UI input, navigation
  - Handle draw: render background, UI
  - Handle transitions: to gameplay, to settings
- Register `MetaHubState` in game state manager
- Add transition from `GameOverState` → `MetaHubState`
- Add transition from main menu → `MetaHubState` (optional "Continue" button)

### Step 2: Meta Hub UI Layout (2 days)
- Create `UI/MetaHub/MetaHubUI.cs`:
  - Top bar component:
    - Meta level display (e.g., "Meta Level 5")
    - XP progress bar with text (e.g., "1200 / 2000 XP")
    - Gold amount with icon (e.g., "500 Gold")
  - Main navigation area:
    - "Talents" button → opens talent tree (Task 047)
    - "Equipment" button → opens equipment/shop (Task 046)
    - "Run History" button → opens history (Task 049)
    - "Settings" button → opens settings overlay
  - Bottom area:
    - Large "Start Run" button → begins new gameplay session
    - "Quit to Menu" button → returns to main menu
- Layout design:
  - Center-aligned navigation buttons (2x2 grid or list)
  - Top bar anchored at top
  - Bottom buttons anchored at bottom
  - Use existing UI components/styles from other screens

### Step 3: Profile Integration (0.5 day)
- Load `PlayerProfile` on meta hub enter:
  - Use `PlayerProfileService.LoadProfile()`
  - Display meta level, XP, gold in top bar
  - Update display when returning from run (profile should already be updated by Task 037)
- Handle new player:
  - If no profile exists, create default (meta level 1, 0 XP, 0 gold)
  - Show brief welcome message or tutorial prompt (optional)

### Step 4: Navigation & Transitions (1 day)
- Implement button click handlers:
  - "Talents" → transition to talent tree state (placeholder/stub for now if Task 047 not done)
  - "Equipment" → transition to shop/equipment state (placeholder/stub for now if Task 046 not done)
  - "Run History" → transition to history state (placeholder/stub for now if Task 049 not done)
  - "Settings" → open settings overlay (reuse from Task 020)
  - "Start Run" → transition to gameplay:
    - Initialize new `RunSession` (Task 037)
    - Load player with profile's equipment loadout and talent bonuses (stub for now if Task 047/048 not done)
    - Transition to `GameplayState`
  - "Quit to Menu" → return to main menu state
- Ensure smooth state transitions with fade/transition effects (if desired)

### Step 5: Visuals & Polish (0.5 day)
- Add simple background for meta hub:
  - Reuse existing background asset or create placeholder
  - Consider ambient animation (particles, subtle movement)
- Add UI polish:
  - Hover effects on buttons
  - Click feedback (sound, visual)
  - XP bar fill animation when entering hub after run
  - Level-up notification if meta level increased (toast/popup)

### Step 6: Testing & Documentation (0.5 day)
- Test navigation:
  - Game over → meta hub → start run → game over → back to hub
  - Main menu → meta hub → quit to menu
  - All buttons clickable and functional (even if stubbed)
- Test profile display:
  - Correct level/XP/gold displayed
  - Updates after completing a run
- Document:
  - Meta hub state and UI layout
  - Navigation flow diagram
  - Integration with profile service
- Update `game-design-document.md` with meta hub section
- Run `dotnet build` and fix errors

## Estimated Timeline
- **Total: 3-4 days**

## Dependencies
- Task 037: Meta progression foundations (profile service, data models)
- Task 020: Pause and settings overlay (reuse settings UI)

## Notes / Risks / Blockers
- Navigation buttons may be stubbed if Tasks 046, 047, 049 are not yet complete. Ensure graceful handling (e.g., "Coming Soon" message).
- "Start Run" button should work even without full equipment/talent integration; start with default loadout.
- Consider adding profile selection/management if multiple profiles are desired (defer to future task).
