# Task: 048 - In-Run Inventory & Equipment Swapping
- Status: CONFLICTS WITH NEW DESIGN - RECONSIDER

**NOTE:** New game vision mandates hub-only equipment configuration; no mid-run swapping allowed. This task needs complete rewrite or removal. If inventory UI is needed in-run, it should be READ-ONLY stats display only.

**Implementation note (current codebase):** There is an existing in-run inventory/equipment overlay in `src/Game/Core/Ecs/Systems/InventoryUiSystem.cs` primarily for debugging. UI rendering has been ported to standardized Myra components (`src/Game/Core/UI/Myra/MyraInventoryScreen.cs`), but the underlying equip/unequip behavior still reflects the older mid-run swapping model and should be revisited per the new design.

## Summary
Create in-game inventory UI accessible during runs that allows players to swap between owned equipment. Display all equipment from profile plus new drops from current run, and apply stat changes immediately when equipment is changed.

## Goals
- Create in-game inventory UI accessible via hotkey (e.g., 'I' or 'Tab').
- Display all owned equipment from profile + new drops from current run.
- Allow equipping/unequipping items during gameplay.
- Apply stat changes immediately via Task 029 stat model.
- Highlight newly dropped equipment with "NEW" badge.
- Track equipment swaps for analytics.
- Pause or slow-time when inventory is open (design choice).

## Non Goals
- Shop functionality during runs (shop is meta hub only).
- Equipment crafting or upgrading during runs.
- Complex inventory management (sorting, filtering — keep simple).
- Item comparison with enemies or environmental items.

## Acceptance criteria
- [ ] Inventory UI opens via hotkey during gameplay.
- [ ] Displays all owned equipment + new drops from current run.
- [ ] Can equip/unequip items; stats recalculate immediately.
- [ ] Newly dropped items highlighted with "NEW" badge or glow.
- [ ] Game pauses or slows when inventory is open.
- [ ] Inventory closes via hotkey or close button; gameplay resumes.
- [ ] Equipment swaps tracked via events for analytics.
- [ ] Stat tooltips show comparison (current vs. hover).
- [ ] `dotnet build` passes.

## Definition of done
- Builds pass (`dotnet build`)
- Play check done (can open inventory during run, swap equipment, see stat changes)
- Docs updated (inventory UI flow, equipment swapping logic)
- Handoff notes added (if handing off)

## Plan

### Step 1: Inventory UI Layout (1 day)
- Create `UI/InGame/InGameInventoryUI.cs`:
  - Layout:
    - Left panel: All owned equipment (grid or list)
    - Right panel: Currently equipped items (weapon, armor, accessories)
    - Bottom: Total stats display (HP, damage, armor, etc.)
  - Equipment item display:
    - Icon, name, rarity (color-coded)
    - "NEW" badge for items dropped this run
    - Hover tooltip with detailed stats
  - Equipped slot display:
    - Show equipped item or "Empty" placeholder
    - Click to unequip
  - Close button and/or ESC to close
- Visual style: consistent with other in-game UI (Task 018, 020)

### Step 2: Inventory Open/Close Logic (0.5 day)
- Hook up hotkey (e.g., 'I' or 'Tab') in input system (Task 013):
  - Press key → open inventory UI, pause/slow game
  - Press again or ESC → close inventory UI, resume game
- Implement pause vs. slow-time:
  - Option A: Full pause (time scale = 0)
  - Option B: Slow-time (time scale = 0.1)
  - Make configurable or decide based on game feel
- Ensure inventory can't be opened during certain states (e.g., game over, already paused)

### Step 3: Equipment Data Loading (0.5 day)
- Load equipment inventory at run start:
  - Get all owned equipment from `PlayerProfile` via `EquipmentInventoryService`
  - Store in run-time inventory manager
- Track new drops during run:
  - When equipment drops (from Task 030), add to inventory immediately
  - Mark as "NEW" in inventory UI
  - Persist new drops to profile on run end (already handled in Task 037)
- Display combined list (owned + new drops) in inventory UI

### Step 4: Equipment Swapping Logic (1 day)
- Implement equip/unequip actions:
  - Click item in inventory → equip to appropriate slot (auto-detect type: weapon/armor/accessory)
  - Click equipped item → unequip (return to inventory, apply stat changes)
  - If slot already occupied, swap (unequip old, equip new)
- Stat recalculation:
  - On equip/unequip, call stat model (Task 029) to recalculate player stats
  - Update stat display immediately
  - Ensure modifiers are added/removed correctly (no double-apply)
- Validation:
  - Can't equip multiple weapons (only one weapon slot)
  - Can equip multiple accessories (2-3 accessory slots)
  - Ensure equipment type matches slot
- Emit events:
  - `EquipmentEquippedEvent` when item equipped
  - `EquipmentUnequippedEvent` when item unequipped
- Add tests for swap logic

### Step 5: Stat Comparison Tooltips (0.5 day)
- Implement hover tooltips:
  - When hovering over unequipped item, show:
    - Item stats
    - Delta compared to currently equipped item (e.g., "+10 damage, -5 armor")
    - Color-code deltas: green for positive, red for negative
  - When hovering over equipped item, show:
    - Item stats
    - "Currently equipped" label
- Use existing tooltip system (if available) or create simple one

### Step 6: Visuals & Polish (0.5 day)
- Add UI polish:
  - Hover effects on items (highlight border)
  - Click feedback (sound, visual)
  - Equip/unequip animations (item slides into slot, particles)
  - "NEW" badge glow or pulse effect
- Add equipment slot icons/labels (weapon icon, armor icon, etc.)
- Add placeholder icons if real assets not ready

### Step 7: Testing & Documentation (0.5 day)
- Test inventory flow:
  - Open/close inventory during gameplay
  - Equip/unequip items, verify stats update
  - Verify game pauses/slows correctly
  - Pick up new drop, verify it appears in inventory with "NEW" badge
- Test edge cases:
  - Swap weapon multiple times
  - Unequip all items
  - Equip item, die, check profile persistence
- Test with stat model integration:
  - Equip high-damage weapon, verify damage increases
  - Equip high-armor item, verify survivability improves
- Document:
  - Inventory UI flow and controls
  - Equipment swapping logic
  - Stat recalculation integration
- Update `game-design-document.md` with in-run inventory section
- Run `dotnet build` and fix errors

## Estimated Timeline
- **Total: 2-3 days**

## Dependencies
- Task 037: Meta progression foundations (equipment data, profile service)
- Task 030: Loot & equipment drops (equipment drop events)
- Task 029: Unified stat model (stat recalculation on equip/unequip)
- Task 046: Shop & equipment UI (equipment data model, inventory service)
- Task 013: Event bus (input handling, equipment events)

## Notes / Risks / Blockers
- Pause vs. slow-time: Pause is safer for MVP (no timing issues), but slow-time feels more immersive. Test both and decide.
- Stat recalculation performance: If stat calculation is expensive, consider caching or optimizing. Should be fine for MVP.
- UI clarity: Ensure players understand which item is equipped and what stats will change. Clear visual feedback critical.
- Inventory accessibility: Consider adding tutorial/tooltip on first open ("Press I to swap equipment anytime").
- Equipment drops: Ensure integration with Task 030 loot system is smooth. Equipment should auto-add to inventory on pickup.
