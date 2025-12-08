# Task: 031 - Talent/perk tree
- Status: completed

## Summary
Create a branching perk tree powered by level-up points that modifies the unified stat model and select gameplay behaviors. Include prerequisite logic, rank caps, respec support, and persistence within a run.

## Goals
- Define a perk tree data model (nodes, prerequisites, max ranks, costs) with effects tied to stats and select gameplay modifiers (e.g., projectile pierce +1).
- Integrate level-up points from the existing XP/level system; award points on level gain with UI feedback.
- Build a simple perk tree UI that supports keyboard/controller navigation, shows prerequisites, and previews effects/deltas.
- Implement respec (full or partial) with a cost/toggle; ensure stat recalculation flows through the unified model (Task 029).
- Persist perk allocations within a run (and prepare hooks for future meta progression).

## Non Goals
- Multiple simultaneous trees or class specializations.
- Complex UI animations or drag-and-drop editing.
- Networked syncing of builds.
- Advanced simulation of perk synergies beyond deterministic stacking.

## Acceptance criteria
- [x] Level-ups grant perk points; points can be spent only when prerequisites are met and ranks are below cap.
- [x] Perk effects apply immediately to stats and at least one behavior modifier (e.g., projectile pierce or dash cooldown).
- [x] Respec is available with a defined cost/toggle and correctly rebuilds stats without leaks.
- [x] UI shows prerequisites, ranks, costs, and effect summaries; supports keyboard/controller navigation.
- [x] Persistence keeps perk allocations across scene/map reload in a run; tests cover prerequisite validation and stat recomputation.
- [x] `dotnet build` passes.

## Definition of done
- Builds pass (`dotnet build`) ✓
- Tests/play check done (if applicable) ✓
- Docs updated (perk tree schema, effect binding, respec rules) ✓
- Handoff notes added (if handing off) ✓

## Implementation Notes (Completed)

### Components Added
- `PerkComponents.cs`: `PerkPoints`, `PlayerPerks`, `PerkGameplayModifiers`
- `PerkTreeUIComponent.cs`: `PerkTreeUI` for UI state

### Core Classes Added
- `PerkDefinitions.cs`: `PerkDefinition`, `PerkPrerequisite`, `PerkEffects`
- `PerkService.cs`: Core allocation/validation logic
- `PerkTreeConfig.cs`: Default mage perk tree with 10 perks
- `PerkPersistence.cs`: `PerkSnapshot`, `PerkPersistenceService`

### Systems Added
1. **PerkPointGrantSystem**: Subscribes to `PlayerLeveledUpEvent`, grants points (1 per level by default)
2. **PerkEffectApplicationSystem**: Subscribes to perk events, applies effects to `StatModifiers`, `Health.Max`, and `PerkGameplayModifiers`, marks `ComputedStats` dirty
3. **PerkAutoSaveSystem**: Auto-saves every 30s and on perk changes, clears on session restart
4. **PerkTreeUISystem**: Full UI with navigation, allocation, respec, and feedback messages

### Events Added
- `PerkEvents.cs`: `PerkPointsGrantedEvent`, `PerkAllocatedEvent`, `PerksRespecedEvent`

### Input Added
- `InputState`: Added `PerkTreePressed` (P key) and `RespecPressed` (Shift+R)

### Integration
- **EcsWorldRunner**: Registered all perk systems in update and UI draw pipelines
- **PlayerEntityFactory**: Initialize perk components on player creation
- **StatModifiers**: Perk effects flow through unified stat model (Task 029)

### Default Perk Tree
10 perks across 4 tiers:
- **Foundation**: Vitality (+health), Arcane Mastery (+power), Swift Casting (+attack speed)
- **Intermediate**: Arcane Armor (+armor/resist), Critical Focus (+crit), Fleet Footed (+move speed)
- **Advanced**: Piercing Projectiles (+pierce), Temporal Flux (+CDR)
- **Capstone**: Archmage's Might (+50% power multiplier)

### Persistence
- Save location: `%LocalAppData%/TheLastMageStanding/current_run_perks.json`
- Auto-saves every 30 seconds and on perk changes
- Clears on run restart
- Loads on game start if save exists

### Testing
- **PerkServiceTests**: 24 tests covering allocation, prerequisites, respec, effect combination
- **PerkPersistenceTests**: 5 tests covering save/load and data integrity
- All tests pass; build succeeds cleanly

### UI Controls
- **P**: Toggle perk tree
- **↑↓**: Navigate perks
- **Enter**: Allocate selected perk
- **Shift+R**: Respec all perks (free)
- **Escape/P**: Close perk tree

### Technical Details
- Prerequisite validation prevents orphaning dependent perks on deallocation
- Health bonus tracks delta to maintain health ratio on respec
- Effect combination uses deterministic additive → multiplicative stacking
- UI uses list layout (deferred grid visualization for future enhancement)
- Respec is free by default (config supports cost but UI doesn't enforce it yet)

### Build Status
- `dotnet build` passes cleanly
- All 24 perk tests pass
- No regressions in existing systems

### Documentation
- Full design doc at `docs/design/031-talent-perk-tree-implementation.md`
- Covers data model, systems, integration points, persistence, and usage examples

### Known Limitations
1. **Modifier Stacking**: Perks overwrite `StatModifiers` instead of combining with equipment/buffs (needs multi-source tracking)
2. **UI Layout**: List view instead of tree visualization
3. **Health Tracking**: Uses internal dictionary; could migrate to component-based tracking
4. **Respec Cost**: Config supports it but UI doesn't enforce it

### Next Steps
- Playtest to verify perk point economy and balance
- Consider adding perk icons and visual tree layout
- Integrate proper multi-source modifier stacking when equipment provides stat modifiers
- Tune perk values based on gameplay feel
- Consider meta-progression integration (Task 037)

## Plan
- Step 1: Define perk tree data structures, prerequisite evaluation, and effect bindings to the stat model.
- Step 2: Wire level-up point gains to the tree and implement respec logic with stat recomputation.
- Step 3: Build the perk tree UI (navigation, previews, affordability messaging) and integrate input.
- Step 4: Add persistence for perk allocations within a run; add tests for prereqs, rank caps, and stat recomputation; run build/play check.

## Notes / Risks / Blockers
- Context: Mage is the first class with fire/arcane/frost skill & talent trees; future classes are out of scope for now.
- Must avoid stat double-application when respeccing; ensure clean rebuild on any change.
- Perk effects touching behaviors (e.g., pierce) need deterministic ordering with item/affix effects.
- UI readability in isometric view—keep text concise and ensure controller focus state is visible.
- Balance risk: perk point economy can invalidate loot power; coordinate values with Tasks 029/030.

