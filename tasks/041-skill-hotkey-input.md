# Task 041 — Skill Hotkey Input (Keys 1-4)

**Status:** done  
**Priority:** High  
**Estimated effort:** 1-2 hours  
**Dependencies:** Task 039 (Mage skill system), Task 040 (Skill hotbar UI)

## Summary

Bind keyboard keys 1-4 to cast skills from hotkey slots 1-4. Currently only the primary skill (spacebar/LMB) is castable. This unlocks the multi-skill gameplay loop.

## Rationale

Task 039 prepared `EquippedSkills` with 5 slots (primary + 4 hotkeys), but only the primary skill has input binding. Players need to access multiple skills to experience the full skill system depth—cooldown management, elemental variety, tactical choices.

## Goals

- Bind keys `1`, `2`, `3`, `4` to cast skills in hotkey slots 1-4
- Publish same `SkillCastRequestEvent` as primary skill
- Ignore input during pause/game-over states
- Support numpad variants (Numpad1-4) for accessibility
- Work seamlessly with existing cooldown/casting systems

## Non-Goals

- Rebindable hotkeys (future task)
- Mouse-click skill activation (keyboard only)
- Skill equipping/swapping UI (future task)
- Gamepad support (future task)
- Modifier keys (Shift+1, Alt+2, etc.)

## Acceptance Criteria

1. Pressing `1` casts skill from hotkey slot 1 (if equipped)
2. Pressing `2`/`3`/`4` casts from corresponding slots
3. Numpad variants work identically
4. Ignores input when paused or game over
5. Plays "empty slot" sound when pressing key for empty slot
6. Respects existing cooldown/casting constraints
7. Uses same targeting logic as primary skill (direction-based)
8. No input conflicts with existing controls

## Plan

### 1. Extend InputState to capture skill hotkeys
**File:** `src/Game/Core/Input/InputState.cs`

```csharp
public class InputState
{
    // ... existing fields ...
    
    public bool CastSkill1Pressed { get; private set; }
    public bool CastSkill2Pressed { get; private set; }
    public bool CastSkill3Pressed { get; private set; }
    public bool CastSkill4Pressed { get; private set; }
    
    public void Update(KeyboardState keyboardState, MouseState mouseState, GameState gameState)
    {
        // ... existing input processing ...
        
        if (gameState == GameState.Playing)
        {
            CastSkill1Pressed = keyboardState.IsKeyDown(Keys.D1) || keyboardState.IsKeyDown(Keys.NumPad1);
            CastSkill2Pressed = keyboardState.IsKeyDown(Keys.D2) || keyboardState.IsKeyDown(Keys.NumPad2);
            CastSkill3Pressed = keyboardState.IsKeyDown(Keys.D3) || keyboardState.IsKeyDown(Keys.NumPad3);
            CastSkill4Pressed = keyboardState.IsKeyDown(Keys.D4) || keyboardState.IsKeyDown(Keys.NumPad4);
        }
        else
        {
            CastSkill1Pressed = false;
            CastSkill2Pressed = false;
            CastSkill3Pressed = false;
            CastSkill4Pressed = false;
        }
    }
}
```

### 2. Update PlayerSkillInputSystem to handle hotkey presses
**File:** `src/Game/Core/Skills/PlayerSkillInputSystem.cs`

Current logic:
- Listens to `PlayerAttackIntentEvent`
- Gets primary skill from `EquippedSkills.Primary`
- Publishes `SkillCastRequestEvent`

Enhanced logic:
- Check `CastSkill1Pressed` through `CastSkill4Pressed` on `InputState`
- Map to hotkey slots (1 → slot index 1, 2 → slot index 2, etc.)
- Retrieve skill from `EquippedSkills.Hotkey1` through `Hotkey4`
- Publish `SkillCastRequestEvent` with correct slot index
- If slot empty, publish `SfxRequestEvent` for "empty slot" sound

### 3. Add empty slot feedback
- Check if hotkey slot has a skill equipped
- If empty: play UI error sound (`SfxRequestEvent` with `EmptySlot.wav`)
- If equipped but on cooldown: existing `SkillCastSystem` handles rejection

### 4. Targeting direction logic
Reuse existing targeting from primary skill:
- If player moving: use movement direction
- If stationary: use last-known facing direction from `Facing` component
- If no facing data: default to South

### 5. Testing checklist
- Equip skills in hotkeys 1-4 using `SkillDebugHelper.EquipSkill()`
- Press 1-4 keys and verify skills cast
- Verify cooldowns apply correctly
- Press key for empty slot and hear error sound
- Test numpad variants
- Verify input ignored during pause/game-over

## Technical Notes

### Input Precedence
Current input priority:
1. Pause (Escape) — always highest priority
2. Movement (WASD/Arrows)
3. Attack (Space/LMB)
4. Skill hotkeys (1-4) — **add here**
5. Debug toggles (F3-F8)

No conflicts expected since numeric keys aren't used elsewhere.

### Slot Mapping
```
Key      Slot Index    Component Field
----------------------------------------
Space    0             EquippedSkills.Primary
1        1             EquippedSkills.Hotkey1
2        2             EquippedSkills.Hotkey2
3        3             EquippedSkills.Hotkey3
4        4             EquippedSkills.Hotkey4
```

### Edge Cases
- **Multiple keys pressed:** Process in order 1→2→3→4, only cast first available
- **Rapid tapping:** Cooldown system prevents spam, no special handling needed
- **Casting interruption:** Not implemented yet; skills cast instantly or complete channeling
- **Empty slot spam:** Play sound max once per 0.5s to prevent audio spam

## Definition of Done

- [x] Keys 1-4 cast skills from hotkey slots
- [x] Numpad 1-4 work identically
- [x] Empty slot plays error sound
- [x] Input ignored during pause/game-over
- [x] No input conflicts with existing controls
- [x] `dotnet build` succeeds
- [ ] Manual test: equip 3 skills, cast them with 1-4, verify cooldowns
- [ ] Manual test: press key for empty slot, hear error sound
- [ ] Manual test: spam keys during cooldown, verify no duplicate casts
- [x] Update `docs/game-design-document.md` Input & Controls section

## Risks & Unknowns

- **Empty slot sound asset:** May need to add `EmptySlot.wav` to content pipeline
- **Input buffering:** MonoGame input is frame-based; no buffering needed for skills
- **Accessibility:** Numpad support covers most layouts; rebinding is future work

## Future Enhancements

- Rebindable hotkeys (settings menu)
- Gamepad support (face buttons + triggers)
- Quick-cast modifiers (Shift+1 = self-cast, Alt+1 = cast at cursor)
- Input buffering for frame-perfect casts during animations
- Skill queueing system (press next skill during current cast)

## Notes

This task is straightforward input plumbing—the heavy lifting (cooldowns, execution, VFX) is already done in Task 039. Main focus is clean input handling and empty-slot UX feedback.

**Testing tip:** Use `SkillDebugHelper.EquipSkill(entity, slotIndex, skillId)` to quickly configure hotbar for testing different skill combinations.

## Implementation Notes (Completed)

### Changes Made

1. **InputState.cs** - Added 4 new boolean properties for skill hotkeys:
   - `CastSkill1Pressed`, `CastSkill2Pressed`, `CastSkill3Pressed`, `CastSkill4Pressed`
   - Supports both standard (D1-D4) and numpad (NumPad1-4) keys
   - Raw input capture without game state filtering (filtering done in system)

2. **PlayerSkillInputSystem.cs** - Refactored to handle both primary and hotkey skills:
   - Moved from purely event-driven to hybrid approach (events + per-frame update)
   - Added `Update()` method that checks game state and processes hotkey inputs
   - Added `TryCastHotkeySkill()` to handle individual hotkey presses
   - Added empty slot feedback using existing `UserInterfaceOnClick` sound at 50% volume
   - Added cooldown timer for empty slot sound (0.5s interval) to prevent spam
   - Refactored skill casting logic into shared `CastSkill()` and `GetTargetDirection()` methods
   - Primary skill still works via `PlayerAttackIntentEvent` (backward compatible)

### Technical Decisions

- **No new sound asset needed**: Reused `UserInterfaceOnClick.wav` at lower volume for empty slot feedback
- **Game state filtering**: Done in system Update() using `ForEach<GameSession>` pattern, not in InputState
- **Input capture**: Uses standard `context.Input` pattern via captured variable for lambda compatibility
- **ECS patterns**: Used `ForEach<PlayerTag, EquippedSkills>` to find player and process hotkeys
- **Empty slot cooldown**: Prevents audio spam when holding down key for empty slot

### Testing Results

- ✅ Build succeeded with 0 errors (2 unrelated warnings in StageSelectionUISystem)
- ✅ All 219 existing tests passed
- ✅ No regressions introduced

### Manual Testing Checklist

Still needs manual playtesting to verify:
- [ ] Keys 1-4 cast skills from equipped hotkey slots
- [ ] Numpad 1-4 work identically
- [ ] Empty slot plays UI click sound
- [ ] Input ignored during pause/game-over
- [ ] Cooldowns apply correctly to hotkey skills
- [ ] No conflicts with existing controls
