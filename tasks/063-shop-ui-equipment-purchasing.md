# Task: 063 - Shop UI and equipment purchasing
- Status: backlog

## Summary
Implement the shop UI accessible via `npc_vendor` NPC (gold square) in the hub. Players can browse equipment (weapons, armor, accessories), purchase items with gold, and see item stats/comparisons. Purchased items go to inventory and can be equipped via inventory UI (Task 030 already implemented).

## Goals
- Create ShopUISystem for browsing and purchasing equipment
- Display shop inventory with item rarities, stats, and prices
- Allow purchasing items with gold currency
- Show player's current gold balance
- Compare shop items with equipped items (stat differences)
- Handle insufficient gold gracefully
- Persist purchases to PlayerProfile inventory

## Non Goals
- Dynamic shop inventory (static pool is fine for MVP)
- Item crafting or upgrade systems
- Selling items back to shop (inventory-only for now)
- Special shop NPCs or faction vendors

## Acceptance criteria
- [ ] Pressing E near gold NPC (`npc_vendor`) opens shop UI
- [ ] UI shows grid/list of purchasable items with icons, names, prices
- [ ] Player's current gold displayed prominently
- [ ] Clicking an item shows detail panel with stats and "Buy" button
- [ ] Buying an item deducts gold, adds to inventory, updates UI
- [ ] Items already owned show "Owned" or similar indicator
- [ ] Insufficient gold shows error message, prevents purchase
- [ ] ESC closes shop and returns to hub
- [ ] Purchased items persist to PlayerProfile inventory
- [ ] `dotnet build` passes; manual playtest confirms purchases work

## Definition of done
- Builds pass (`dotnet build`)
- Tests/play check done
- Docs updated (if applicable)
- Handoff notes added

## Plan
- Step 1: Create ShopUIState component (IsOpen, SelectedItemId, etc.)
- Step 2: Define shop inventory (hardcoded or config-driven item list)
- Step 3: Create ShopUISystem implementing IUpdateSystem, IUiDrawSystem, ILoadContentSystem
- Step 4: Render shop UI: item grid, detail panel, gold display, buy button
- Step 5: Implement input handling: item browsing, purchase confirmation
- Step 6: Wire InteractionInputSystem to toggle ShopUIState on `npc_vendor`
- Step 7: Integrate with PlayerProfile gold and inventory systems
- Step 8: Add purchase transaction logic (deduct gold, add item)
- Step 9: Show comparison tooltips (current vs shop item stats)
- Step 10: Register system in EcsWorldRunner hub-only systems

## Notes / Risks / Blockers
- **Dependency**: ItemRegistry and ItemFactory already exist (Task 030)
- **Dependency**: PlayerProfile needs Gold field (check if exists, add if missing)
- **Design**: Should shop restock or be one-time purchases? (Recommend one-time for MVP)
- **Design**: How is gold earned? (Stage rewards, boss kills - already implemented?)
- **Risk**: Need to define initial shop inventory pool (weapons, armor types, rarity distribution)
