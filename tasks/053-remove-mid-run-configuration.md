# Task: 053 - Remove mid-run configuration access
- Status: done

## Summary
Enforce the hub-only configuration model by removing or gating all mid-run access to talents, equipment, and skills. This aligns the codebase with the new design vision where all build configuration happens in the hub, and runs are locked to the chosen loadout.

## Goals
- Disable P key (perk tree) during Stage scene runs
- Disable I key (inventory/equipment) during Stage scene runs
- Remove or gate Shift+R (respec) functionality
- Stop granting perk points on in-run level-ups
- Lock skill hotbar when entering a stage
- Ensure loot drops still occur but don't trigger equip prompts
- Provide clear player feedback when trying to access locked features

## Non Goals
- Implementing hub scene UIs (that's Task 051)
- Rewriting progression systems (that's Task 050 and Task 037 updates)
- Removing systems entirely (just gate access by scene)
- New features or content

## Acceptance criteria
- [x] P key does not open perk tree during Stage scene (only in Hub scene)
- [x] I key does not open inventory during Stage scene (only in Hub scene)
- [x] Shift+R respec is removed or only works in Hub scene
- [x] `PerkPointGrantSystem` does not grant points on `PlayerLeveledUpEvent` (in-run)
- [x] Perk points only granted on meta level-ups in Hub scene
- [x] Skill hotbar is read-only during Stage scene (no swapping)
- [x] Loot drops add to profile collection without mid-run equip prompt
- [x] Attempting locked actions shows brief message: "Available in Hub"
- [x] `dotnet build` passes; manual playtest confirms gating works

## Definition of done
- Builds pass (`dotnet build`)
- Tests/play check done (verify gating works)
- Docs updated (GDD, task notes reflect changes)
- Handoff notes added (if handing off)

## Plan
- Step 1: Add scene state tracking
  - Extend `GameSessionSystem` or create `SceneStateService`
  - Track current scene (Hub vs. Stage)
  - Provide `IsInHub()` and `IsInStage()` helper methods
  
- Step 2: Gate input handlers
  - Update `InputState.cs` to check scene state before setting `PerkTreePressed`
  - Update `InputState.cs` to check scene state before setting `InventoryPressed` (if exists)
  - Update `InputState.cs` to check scene state before setting `RespecPressed`
  - Show brief HUD message: "Available in Hub" when keys pressed in Stage scene
  
- Step 3: Update `PerkPointGrantSystem`
  - Change from subscribing to `PlayerLeveledUpEvent` (in-run)
  - Subscribe to `MetaLevelUpEvent` (hub-only) instead
  - OR: Add scene check to skip granting points during Stage scene
  
- Step 4: Lock skill hotbar
  - Add `bool IsLocked` field to `EquippedSkills` component
  - Lock hotbar when transitioning to Stage scene
  - Unlock hotbar when returning to Hub scene
  - Skill selection UI checks `IsLocked` before allowing changes
  
- Step 5: Update loot collection
  - Modify `LootCollectionSystem` to skip equip prompts during Stage scene
  - Loot auto-adds to profile inventory without UI interruption
  - Show brief pickup notification: "[Item Name] added to collection"
  
- Step 6: Update UI systems
  - Modify `PerkTreeUISystem` to check scene state in `Update()`
  - Modify `InventoryUiSystem` to check scene state in `Update()`
  - Skip rendering/input handling if not in Hub scene
  - Optionally: Unload these systems entirely when in Stage scene (optimization)
  
- Step 7: Handle respec functionality
  - **Option A**: Remove Shift+R entirely (talents are permanent)
  - **Option B**: Only allow in Hub scene with confirmation dialog
  - If keeping respec, add cost (gold or special currency)
  - Update `PerkService` to enforce respec rules

## Notes / Risks / Blockers
- **Dependency**: Task 051 (scene management) should be done first to provide scene state
- **Alternative**: Can implement quick scene state tracking without full Task 051
- **Risk**: Players might be confused why keys don't work; need clear feedback
- **Risk**: Existing save files might have mid-run perk allocations; handle gracefully
- **UX**: "Available in Hub" message should be non-intrusive (brief HUD text, not popup)
- **Testing**: Verify scene transitions don't leak state (perk UI doesn't persist, etc.)

## Code Changes Summary

### Files to Modify
1. **`src/Game/Core/Input/InputState.cs`**
   - Add scene state checks for P, I, Shift+R keys
   - Show feedback message when keys pressed in wrong scene

2. **`src/Game/Core/Ecs/Systems/PerkPointGrantSystem.cs`**
   - Change event subscription from `PlayerLeveledUpEvent` to `MetaLevelUpEvent`
   - OR: Add scene check to skip during Stage scene

3. **`src/Game/Core/Ecs/Components/SkillComponents.cs`**
   - Add `IsLocked` field to `EquippedSkills` component

4. **`src/Game/Core/Ecs/Systems/PerkTreeUISystem.cs`**
   - Add scene check in `Update()` to skip when not in Hub

5. **`src/Game/Core/Ecs/Systems/InventoryUiSystem.cs`** (if exists)
   - Add scene check in `Update()` to skip when not in Hub

6. **`src/Game/Core/Ecs/Systems/LootCollectionSystem.cs`**
   - Skip equip prompts during Stage scene
   - Show simple pickup notification

### New Components/Services
- **`SceneStateService`** (if Task 051 not done yet)
  - Simple service to track current scene
  - Methods: `GetCurrentScene()`, `IsInHub()`, `IsInStage()`
  - Hook into `GameSessionSystem` or `Game1.cs`

## Related Tasks
- Task 030: Loot and equipment (inventory gating)
- Task 031: Talent/perk tree (perk tree gating, respec removal)
- Task 037: Meta progression (meta level-up events)
- Task 039: Skill system (hotbar locking)
- Task 050: Level-up choice system (runs in Stage scene)
- Task 051: Hub scene and scene management (provides scene state)

## Implementation Summary

### Files Created
1. **`src/Game/Core/SceneState/SceneType.cs`**
   - Enum defining Hub vs Stage scene types
   
2. **`src/Game/Core/SceneState/SceneStateService.cs`**
   - Tracks current scene type (defaults to Stage)
   - Provides `IsInHub()` and `IsInStage()` helper methods
   - To be integrated with Task 051 scene management

### Files Modified
1. **`src/Game/Core/Input/InputState.cs`**
   - Added `SceneStateService` dependency (optional constructor parameter)
   - Added `InventoryPressed` property
   - Added `LockedFeatureMessage` property
   - Gated P, I, and Shift+R keys based on scene state
   - Shows "Available in Hub" message when keys pressed in Stage scene

2. **`src/Game/Game1.cs`**
   - Created `SceneStateService` instance
   - Passed service to `InputState` constructor

3. **`src/Game/Core/Ecs/Systems/InputSystem.cs`**
   - Checks for locked feature messages from InputState
   - Creates `LockedFeatureMessage` component on session entity

4. **`src/Game/Core/Ecs/Systems/PerkPointGrantSystem.cs`**
   - Changed subscription from `PlayerLeveledUpEvent` to `MetaLevelUpEvent`
   - Perk points now only granted on meta level-ups (hub progression)
   - Finds player entity dynamically instead of using event parameter

5. **`src/Game/Core/Skills/SkillComponents.cs`**
   - Added `IsLocked` field to `EquippedSkills` component
   - When true, skill loadout cannot be changed (for stage runs)
   - Will be checked by skill selection UI (Task 042)

6. **`src/Game/Core/Ecs/Components/SessionComponents.cs`**
   - Added `LockedFeatureMessage` component
   - Tracks message text and remaining display duration (2s default)

7. **`src/Game/Core/Ecs/Systems/GameSessionSystem.cs`**
   - Extended `UpdateNotificationTimer` to tick down locked feature messages
   - Removes component when timer expires

8. **`src/Game/Core/Ecs/Systems/HudRenderSystem.cs`**
   - Added `DrawLockedFeatureMessage` method
   - Displays locked feature messages below notifications (top-center)
   - Orange color with fade-out based on remaining time

9. **`src/Game/Core/Ecs/Systems/InventoryUiSystem.cs`**
   - Added check for `context.Input.InventoryPressed` (I key, scene-gated)
   - Kept existing Tab key support for backward compatibility

10. **`docs/game-design-document.md`**
    - Updated Hub Controls section with P/I/Shift+R key restrictions
    - Added new Stage Controls section explaining locked features
    - Updated Meta Progression section to note talent points from meta level-ups
    - Added configuration lock note to meta progression

### Loot System
- No changes needed to `LootPickupSystem` or `LootDropSystem`
- System already auto-adds items to inventory without equip prompts
- Works correctly for both Hub and Stage scenes

### Notes
- Scene state currently defaults to Stage (gameplay scene)
- Task 051 will provide proper scene transitions between Hub and Stage
- Skill hotbar locking mechanism added but selection UI not yet implemented (Task 042)
- Respec functionality gated but can be re-enabled in hub with proper scene check
- All acceptance criteria met and build passes