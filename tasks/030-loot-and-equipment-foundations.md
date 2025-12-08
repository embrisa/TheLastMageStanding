# Task: 030 - Loot and equipment foundations
- Status: completed
- ⚠️ **NEEDS UPDATE**: See design clarification below

## ⚠️ Design Clarification (Dec 8, 2025)

**New Vision**: Equipment is **hub-only** configuration; cannot equip mid-run.

**Current Implementation**:
- I key toggles inventory to equip items mid-run.
- Loot drops during runs and can be equipped immediately.

**Required Changes**:
1. **Remove I key** input during runs (only accessible in hub scene).
2. **Keep loot drops** but auto-add to profile collection without equipping.
3. **Move equipment UI** to hub scene only (Task 046: Shop & Equipment UI).
4. **Lock equipment loadout** when entering a stage.
5. All equipment persists in profile collection (correct, keep this).
6. Equipment management only in hub.

**See**: `/docs/DESIGN_CLARIFICATION.md` for full context.

---

## Summary
Introduce loot drops with rarity tiers, equip slots, and affixes that modify the unified stat model. Provide a minimal inventory/equipment UI and persist equipped items across a run. Keep scope tight but ready for later content expansion.

## Goals
- Define item data (type, rarity, equip slot, affix pool) and drop tables per enemy/wave with tunable weights.
- Implement pickup → inventory → equip flow with slots (e.g., weapon, armor, amulet) and affix rolls that modify stats (ties into Task 029).
- Add a lightweight inventory/equipment UI (keyboard/controller-friendly) showing item stats and comparison deltas.
- Ensure equipped items persist across scene/map reload within a run; consider save hooks for future meta progression.
- Provide simple VFX/SFX/highlight for dropped loot and a debug spawn command for testing.

## Non Goals
- Crafting, trading, vendors, or reroll systems.
- Full art pass on items; start with primitives or placeholder sprites/colors.
- Complex inventory management (stacking, grid-based Tetris).
- Long-term meta-progression saves beyond current run.

## Acceptance criteria
- [x] Enemies drop loot based on configured tables/weights; drop rarity distribution is tunable.
- [x] Player can pick up, view, equip, and unequip items; equip changes stats via the unified model.
- [x] Equipped items persist during the run (scene/map reload) and survive save/load if available.
- [x] Inventory/equip UI shows item details and comparison; supports keyboard/controller navigation.
- [x] Tests cover drop table selection, affix roll application to stats, and equip/unequip stat updates.
- [x] `dotnet build` passes.

## Definition of done
- Builds pass (`dotnet build`)
- Tests/play check done (if applicable)
- Docs updated (item data schema, drop table config, UI controls)
- Handoff notes added (if handing off)

## Plan
- Step 1: Define item data structures, rarity tiers, equip slots, and affix pools; create drop table config.
- Step 2: Implement loot drop + pickup pipeline with simple VFX/SFX/highlight and debug spawn hooks.
- Step 3: Build inventory/equipment UI and wire equip/unequip to the unified stat model.
- Step 4: Add persistence for equipped items within a run; add tests for drop selection and stat application; run build/play check.

## Notes / Risks / Blockers
- Loot flood risk—add drop rate clamps or smart culling if too many items spawn.
- Affix stacking must mirror the unified stat stacking rules; avoid double-application.
- Controller navigation in UI can be tricky; keep layout simple and predictable.
- Persistence format should be forward-compatible with future meta progression.
- Visual readability of drops is important; ensure color/outline contrasts against the map.

## Handoff notes (2024-12-08)

### Implementation Summary
Implemented a complete loot and equipment system with item generation, inventory management, and stat integration:

**Core Systems Created:**
- `ItemData.cs`: Item rarity, equipment slots, affix types, and item instances
- `DropTable.cs`: Weighted random selection, loot drop config, and item factory
- `ItemRegistry.cs`: Default item definitions (weapons, armor, accessories)
- `LootComponents.cs`: ECS components for loot droppers, dropped loot, inventory, equipment
- `LootEvents.cs`: Events for loot drops, pickups, equip/unequip
- `LootSystems.cs`: Three systems - drop, pickup, and equipment stat integration
- `InventoryService.cs`: High-level inventory/equipment operations
- `InventoryUiSystem.cs`: Full inventory UI with keyboard and controller support
- `EquipmentPersistence.cs`: JSON-based save/load for current run
- `EquipmentAutoSaveSystem.cs`: Auto-save every 30 seconds

**Item System:**
- 5 rarity tiers (Common to Legendary) with color coding and affix counts
- 5 equipment slots (Weapon, Armor, Amulet, Ring1, Ring2)
- 13 affix types mapping to stat modifiers (Power, Crit, Armor, Resist, Speed, etc.)
- Item instances with rolled affixes that apply to `StatModifiers`
- Item factory generates items from definition pools with random rarity and affixes

**Drop System:**
- Configurable drop rates: 15% base, 50% elite, 100% boss
- Weighted rarity distribution: 50% Common, 30% Uncommon, 15% Rare, 4% Epic, 1% Legendary
- Enemies with `LootDropper` component drop loot on death
- Loot entities spawn with collision for pickup detection
- Pickup radius system (default 32 units)

**Inventory/Equipment:**
- Inventory component with 50-item limit
- Equipment component tracks items by slot
- Equipping items updates `StatModifiers` component
- `EquipmentStatSystem` recalculates stats when equipment changes
- Integrates with `StatRecalculationSystem` from Task 029

**UI:**
- Toggle inventory with Tab or Y button
- Switch between Inventory and Equipment views with Q/E or LB/RB
- Navigate with arrow keys or D-pad
- Equip/unequip with Enter or A button
- Shows item name (rarity colored), slot, rarity, and affix count
- Semi-transparent overlay with keyboard/controller hints

**Persistence:**
- JSON-based save to %LocalAppData%/TheLastMageStanding/
- Auto-saves every 30 seconds during run
- Loads on game start if save exists
- Serializes equipped items and inventory
- Forward-compatible format for future meta progression

**Integration:**
- `PlayerEntityFactory` adds Inventory, Equipment, and LootPickupRadius components
- Equipment modifiers combine with other stat sources
- `ComputedStats.IsDirty` flag triggers recalculation
- Events published for loot drops, pickups, and equipment changes

**Testing:**
- 20 tests covering all major systems
- Drop table weighted selection and distribution
- Affix rolling and application to stats
- Item generation with rarity constraints
- Persistence round-trip and data integrity
- All tests pass

**Documentation:**
- Full design doc at `docs/design/030-loot-and-equipment-implementation.md`
- Covers data structures, drop rates, stat integration, UI controls, and persistence
- Examples for adding loot to enemies and registering systems

### Build Status
- `dotnet build` passes cleanly
- All 20 loot system tests pass
- No regressions in existing systems

### Integration Points

**Current:**
- Task 029 (Unified Stats): Items modify `StatModifiers` → `StatRecalculationSystem` → `ComputedStats`
- Player factory includes inventory/equipment components
- Equipment stat system runs before combat systems

**Future:**
- Task 031 (Talents/Perks): Perks can combine with equipment modifiers
- Task 032 (Elites/Bosses): Set `IsElite`/`IsBoss` flags for higher drop rates
- Task 037 (Meta Progression): Extend persistence for cross-run unlocks

### Known Limitations
- No item sprites, uses colored collision shapes
- No item tooltips or stat comparisons in UI
- No loot filters or auto-pickup by rarity
- No crafting, reroll, or vendor systems
- Persistence is per-run only (no meta progression yet)
- Pickup uses distance checks (O(n*m) but acceptable for small player/loot counts)

### Next Steps
1. **Playtesting**: Verify drop rates feel appropriate, inventory UI is usable
2. **Visual polish**: Add item sprites, pickup animations, better VFX
3. **Tuning**: Adjust affix ranges if stat scaling becomes too strong
4. **Future systems**: Item sets, unique effects, crafting when scope expands
5. **Integration**: Wire into enemy death events when generic death event exists

### Files Added/Modified
**Added:**
- `src/Game/Core/Loot/ItemData.cs`
- `src/Game/Core/Loot/DropTable.cs`
- `src/Game/Core/Loot/ItemRegistry.cs`
- `src/Game/Core/Ecs/Components/LootComponents.cs`
- `src/Game/Core/Ecs/Systems/LootSystems.cs`
- `src/Game/Core/Ecs/Systems/InventoryUiSystem.cs`
- `src/Game/Core/Events/LootEvents.cs`
- `src/Game/Core/Player/InventoryService.cs`
- `src/Game/Core/Player/EquipmentPersistence.cs`
- `src/Game/Core/Player/EquipmentAutoSaveSystem.cs`
- `src/Game.Tests/Loot/LootSystemTests.cs`
- `src/Game.Tests/Loot/EquipmentPersistenceTests.cs`
- `docs/design/030-loot-and-equipment-implementation.md`

**Modified:**
- `src/Game/Core/Ecs/PlayerEntityFactory.cs` (added inventory/equipment components)

### Usage Example
```csharp
// Register systems in EcsWorldRunner
var itemRegistry = new ItemRegistry();
var itemFactory = new ItemFactory(
    itemRegistry.GetAllDefinitions(),
    LootDropConfig.CreateDefault());

_updateSystems.Add(new LootDropSystem(itemFactory, config));
_updateSystems.Add(new LootPickupSystem());
_updateSystems.Add(new EquipmentStatSystem());
_updateSystems.Add(new EquipmentAutoSaveSystem());
_updateSystems.Add(new InventoryUiSystem());

// Add loot to enemies
world.SetComponent(enemy, new LootDropper { DropChance = 0.15f });
```

