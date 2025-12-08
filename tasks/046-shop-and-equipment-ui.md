# Task: 046 - Shop & Equipment Purchase UI
- Status: NEEDS UPDATE (Task 056 covers equipment UI)

**NOTE:** Task 056 (Equipment management hub UI) now covers the equipment browsing/equipping aspect. This task should focus ONLY on shop purchasing UI (buy items with gold/currency). Reassess scope after Task 056 is complete.

## Summary
Create shop UI for browsing and purchasing equipment with gold, and equipment UI for viewing inventory and managing loadout. Integrate with `PlayerProfileService` to persist purchases and equipped items.

## Goals
- Create shop UI for browsing available equipment (weapons, armor, accessories).
- Display equipment stats, cost, and "Owned" status.
- Implement purchase flow with gold transaction handling.
- Create equipment inventory UI for viewing all owned equipment.
- Implement loadout management: equip/unequip items for next run.
- Show stat comparisons and previews when hovering over equipment.
- Persist purchases and loadout to player profile.

## Non Goals
- In-run equipment swapping (Task 048).
- Equipment drops from enemies (handled in Task 030).
- Complex shop features (daily deals, sales, bundles).
- Cosmetic items or non-equipment purchases.
- Equipment crafting or upgrading systems.

## Acceptance criteria
- [ ] Shop UI displays available equipment with name, icon, stats, cost.
- [ ] "Owned" badge shows for already-purchased equipment; purchase button disabled.
- [ ] Purchase button deducts gold and adds equipment to profile inventory.
- [ ] "Insufficient funds" feedback when gold < cost.
- [ ] Equipment UI displays all owned equipment in grid/list.
- [ ] Can equip/unequip items to loadout (weapon, armor, accessories).
- [ ] Equipped items highlighted; stat changes shown when hovering.
- [ ] Loadout persists to profile; applied at run start.
- [ ] Filter/sort options work (by type, rarity, cost).
- [ ] `dotnet build` passes.

## Definition of done
- Builds pass (`dotnet build`)
- Play check done (can browse shop, purchase items, equip loadout)
- Docs updated (shop inventory format, equipment data, UI flows)
- Handoff notes added (if handing off)

## Plan

### Step 1: Shop Data & Service (1 day)
- Create `Core/MetaProgression/MetaShopService.cs`:
  - Define shop inventory (static list or JSON file):
    - Equipment items with: ID, name, type, rarity, stats, cost, icon
  - Methods:
    - `GetShopInventory()` → list of available equipment
    - `PurchaseItem(string itemId, PlayerProfile profile)` → deduct gold, add to inventory, return success/failure
    - `IsItemOwned(string itemId, PlayerProfile profile)` → check if already purchased
- Create example shop inventory:
  - Weapons: Wooden Sword (100g), Iron Sword (300g), Steel Sword (800g)
  - Armor: Cloth Armor (150g), Leather Armor (400g), Chain Mail (900g)
  - Accessories: Simple Ring (200g), Magic Amulet (500g)
- Extend `PlayerProfile.EquipmentInventory` to include `List<EquipmentItem>`
- Add unit tests for shop service (purchase, insufficient funds, duplicate purchase)

### Step 2: Shop UI (1.5 days)
- Create `UI/MetaHub/ShopUI.cs`:
  - Display shop inventory in grid or scrollable list
  - Each item shows:
    - Icon, name, rarity (color-coded)
    - Stats preview (damage, armor, etc.)
    - Cost in gold
    - "Purchase" button OR "Owned" badge
  - Implement purchase flow:
    - Click "Purchase" → confirmation dialog (optional)
    - Deduct gold via `MetaShopService.PurchaseItem()`
    - Update UI to show "Owned"
    - Show success feedback (sound, visual effect)
    - Show "Insufficient Funds" message if gold < cost
  - Filter/sort controls:
    - Filter by type (weapon/armor/accessory)
    - Sort by cost, rarity, name
  - Hover tooltip: show detailed stats and comparison with equipped item
- Navigate to shop from meta hub "Equipment" button
- Add "Back to Hub" button

### Step 3: Equipment Inventory UI (1.5 days)
- Create `UI/MetaHub/EquipmentInventoryUI.cs` (or reuse/extend `ShopUI`):
  - Display all owned equipment in grid
  - Show currently equipped loadout:
    - Weapon slot
    - Armor slot
    - Accessory slots (1-2)
  - Implement equip/unequip:
    - Click item → equip to appropriate slot (auto-detect type)
    - Click equipped item → unequip (return to inventory)
    - Drag-and-drop alternative (optional)
  - Show stat changes:
    - Display total stats from equipped items
    - Show delta when hovering over unequipped item (e.g., "+10 damage, -5 armor")
  - Filter/sort controls (same as shop)
  - Visual feedback:
    - Highlight equipped items
    - Show empty slot indicators
- Persist loadout to profile on change
- Add "Back to Hub" button

### Step 4: Integration & Persistence (0.5 day)
- Integrate shop and equipment UI with `PlayerProfileService`:
  - Load profile on shop/equipment open
  - Save profile on purchase or loadout change
  - Ensure atomic writes (no partial updates)
- Hook into meta hub navigation:
  - "Equipment" button opens combined shop/equipment screen OR separate tabs
- Ensure gold updates in meta hub top bar after purchases

### Step 5: Visuals & Polish (0.5 day)
- Add UI polish:
  - Hover effects on items
  - Click feedback (sound, visual)
  - Purchase success animation (gold spent, item added)
  - Equip/unequip animations (item slides into slot)
- Rarity color-coding:
  - Common (gray), Uncommon (green), Rare (blue), Epic (purple), Legendary (orange)
- Add placeholder icons if real assets not ready

### Step 6: Testing & Documentation (0.5 day)
- Test purchase flow:
  - Purchase items with sufficient gold
  - Attempt purchase with insufficient gold
  - Verify "Owned" badge appears after purchase
- Test equipment management:
  - Equip items to loadout
  - Unequip items
  - Verify stats update correctly
  - Verify loadout persists across sessions
- Test filter/sort controls
- Document:
  - Shop inventory format (JSON schema)
  - Equipment item data model
  - Purchase and loadout flows
- Update `game-design-document.md` with shop and equipment sections
- Run `dotnet build` and fix errors

## Estimated Timeline
- **Total: 3-4 days**

## Dependencies
- Task 037: Meta progression foundations (profile service, equipment data model)
- Task 045: Meta hub UI (navigation integration)
- Task 029: Unified stat model (equipment stats structure)

## Notes / Risks / Blockers
- Shop inventory can start with hardcoded items; move to JSON config later for easier tuning.
- Equipment stats must align with Task 029 stat model.
- Decide: combined shop/equipment screen vs. separate screens? (Suggest separate tabs for clarity.)
- Asset dependency: need equipment icons/sprites. Use placeholders if not ready.
- Gold economy balance: start conservative, iterate based on playtest feedback.
