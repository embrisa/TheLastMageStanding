# Task: 030 - Loot and equipment foundations
- Status: backlog

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
- [ ] Enemies drop loot based on configured tables/weights; drop rarity distribution is tunable.
- [ ] Player can pick up, view, equip, and unequip items; equip changes stats via the unified model.
- [ ] Equipped items persist during the run (scene/map reload) and survive save/load if available.
- [ ] Inventory/equip UI shows item details and comparison; supports keyboard/controller navigation.
- [ ] Tests cover drop table selection, affix roll application to stats, and equip/unequip stat updates.
- [ ] `dotnet build` passes.

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

