# Task: 056 - Equipment management hub UI
- Status: backlog

## Summary
Create an equipment management UI in the hub scene where players can browse their equipment collection, view item stats, compare gear, and equip items to their 5 slots (Weapon, Armor, Amulet, Ring1, Ring2). Equipment can only be changed in the hub, not during runs.

## Goals
- Provide hub-only UI for browsing and equipping items from collection
- Display all collected items with filtering (by slot, rarity)
- Show item details (affixes, stats) and compare with equipped items
- Allow equipping items to 5 equipment slots
- Show stat totals and deltas when hovering/selecting items
- Persist equipped items to profile for use in runs
- Visual feedback for equipped items, new items, rarity tiers

## Non Goals
- Item acquisition during runs (that's Task 030, already done)
- Shop UI (that's Task 046, separate)
- Crafting or rerolling items
- Item sorting or advanced filters (basic filters are enough)
- Visual polish or 3D item preview

## Acceptance criteria
- [ ] Equipment UI accessible from hub main menu
- [ ] Shows all items in profile collection with rarity color-coding
- [ ] Can filter items by slot (All, Weapon, Armor, Amulet, Ring)
- [ ] Can filter items by rarity (All, Common, Uncommon, Rare, Epic, Legendary)
- [ ] Clicking item shows detailed stats (affixes, values)
- [ ] Comparison view shows stat deltas vs. currently equipped item
- [ ] Can click or press Enter to equip selected item
- [ ] Equipped items persist to profile and apply in runs
- [ ] Stat summary panel shows total stats from all equipped items
- [ ] Keyboard navigation works (arrow keys, Enter, Escape)
- [ ] `dotnet build` passes; manual test confirms equipping works

## Definition of done
- Builds pass (`dotnet build`)
- Tests/play check done (verify item equipping and stat application)
- Docs updated (UI flow, equipment system)
- Handoff notes added (if handing off)

## Plan
- Step 1: Create equipment UI system
  - Create `EquipmentManagementUISystem` in `Core/Ecs/Systems/`
  - Render in hub scene only (check scene state)
  - Load equipment collection from profile
  
- Step 2: Display equipment collection
  - Query `PlayerProfile.EquipmentCollection` for all owned items
  - Render item list with name, slot, rarity (color-coded)
  - Show item icon (placeholder or sprite)
  - Display "NEW" badge for recently acquired items
  
- Step 3: Implement filtering
  - Add filter dropdown or buttons: All, Weapon, Armor, Amulet, Ring
  - Add rarity filter: All, Common, Uncommon, Rare, Epic, Legendary
  - Update item list based on active filters
  
- Step 4: Implement item details panel
  - When item selected, show detail view
  - Display base stats and all affixes with values
  - Show item level (if applicable) and rarity
  - Show flavor text or description (if any)
  
- Step 5: Implement comparison view
  - When hovering/selecting item, compare with equipped item in same slot
  - Show stat deltas with color coding (green +, red -, white =)
  - Example: "+15 Attack Damage, +10% Crit Chance, -5 Armor"
  
- Step 6: Implement equipment slots UI
  - Render 5 equipment slots: Weapon, Armor, Amulet, Ring1, Ring2
  - Show currently equipped items with name and icon
  - Highlight selected slot when browsing compatible items
  
- Step 7: Implement item equipping
  - Click or press Enter on item to equip to appropriate slot
  - If slot occupied, confirm replacement (show comparison)
  - Update `PlayerProfile.EquippedItems` dictionary
  - Recalculate stat totals and update display
  
- Step 8: Implement stat summary panel
  - Show total stats from all equipped items
  - Example: "Attack Damage: +45, Armor: +30, Crit Chance: +15%"
  - Update in real-time as items are equipped/unequipped
  
- Step 9: Add keyboard navigation
  - Arrow keys: Navigate item list
  - Tab: Switch between item list and equipment slots
  - Enter: Equip selected item
  - Delete/Backspace: Unequip item from selected slot
  - Escape: Exit UI
  
- Step 10: Persist to profile
  - Save equipped items to `PlayerProfile.EquippedItems`
  - Load equipped items from profile on hub entry
  - Apply stat modifiers from equipped items in runs

## UI Layout (Preliminary)

```
┌──────────────────────────────────────────────────────┐
│  EQUIPMENT MANAGEMENT                                │
├──────────────────────────────────────────────────────┤
│  ┌──────────────┐  ┌──────────────────────────────┐ │
│  │ EQUIPPED     │  │ COLLECTION                   │ │
│  │              │  │ Filter: [All▼] [All Rarity▼]│ │
│  │ Weapon:      │  │ ────────────────────────────│ │
│  │ [Staff of    │  │ □ Worn Sword (Common)       │ │
│  │  Flames]     │  │ □ Iron Chestplate (Uncommon)│ │
│  │              │  │ ☑ Staff of Flames (Rare)    │ │
│  │ Armor:       │  │ □ Amulet of Power (Epic) NEW│ │
│  │ [Empty]      │  │ □ Ring of Haste (Rare)      │ │
│  │              │  │ □ Crystal Ring (Legendary)  │ │
│  │ Amulet:      │  │ ...                         │ │
│  │ [Empty]      │  └──────────────────────────────┘ │
│  │              │                                    │
│  │ Ring 1:      │  ┌──────────────────────────────┐ │
│  │ [Empty]      │  │ ITEM DETAILS                 │ │
│  │              │  │ Staff of Flames (Rare)       │ │
│  │ Ring 2:      │  │ ────────────────────────────│ │
│  │ [Empty]      │  │ + 25 Attack Damage (25%)     │ │
│  │              │  │ + 15 Power (30%)             │ │
│  │ ──────────── │  │ + 10% Crit Chance            │ │
│  │ TOTAL STATS  │  │                              │ │
│  │ Attack: +25  │  │ vs. Equipped: [None]         │ │
│  │ Power: +15   │  │ ▲ +25 Attack Damage          │ │
│  │ Crit: +10%   │  │ ▲ +15 Power                  │ │
│  └──────────────┘  └──────────────────────────────┘ │
│  [Equip] [Unequip] [Compare]              [Close]   │
└──────────────────────────────────────────────────────┘
```

## Notes / Risks / Blockers
- **Dependency**: Task 030 provides item data model and collection system
- **Dependency**: Task 051 provides hub scene context
- **Dependency**: Task 053 ensures equipment can't be changed mid-run
- **Risk**: Large equipment collections could make UI slow; need pagination or virtual scrolling
- **UX**: Comparison math could be complex (additive vs. multiplicative affixes)
- **UX**: Need clear feedback when equipping (confirmation sound, visual effect)
- **Balance**: Starting equipment or "naked" start? (Design decision needed)
- **Tech**: Profile schema needs `EquippedItems` field (may already exist from Task 030)

## Related Tasks
- Task 030: Loot and equipment foundations (item system)
- Task 029: Unified stat and damage model (stat calculations)
- Task 051: Hub scene (provides scene context)
- Task 053: Remove mid-run configuration (hub-only enforcement)
- Task 046: Shop and equipment UI (shop is separate but related)
