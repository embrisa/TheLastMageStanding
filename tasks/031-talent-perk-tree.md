# Task: 031 - Talent/perk tree
- Status: backlog

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
- [ ] Level-ups grant perk points; points can be spent only when prerequisites are met and ranks are below cap.
- [ ] Perk effects apply immediately to stats and at least one behavior modifier (e.g., projectile pierce or dash cooldown).
- [ ] Respec is available with a defined cost/toggle and correctly rebuilds stats without leaks.
- [ ] UI shows prerequisites, ranks, costs, and effect summaries; supports keyboard/controller navigation.
- [ ] Persistence keeps perk allocations across scene/map reload in a run; tests cover prerequisite validation and stat recomputation.
- [ ] `dotnet build` passes.

## Definition of done
- Builds pass (`dotnet build`)
- Tests/play check done (if applicable)
- Docs updated (perk tree schema, effect binding, respec rules)
- Handoff notes added (if handing off)

## Plan
- Step 1: Define perk tree data structures, prerequisite evaluation, and effect bindings to the stat model.
- Step 2: Wire level-up point gains to the tree and implement respec logic with stat recomputation.
- Step 3: Build the perk tree UI (navigation, previews, affordability messaging) and integrate input.
- Step 4: Add persistence for perk allocations within a run; add tests for prereqs, rank caps, and stat recomputation; run build/play check.

## Notes / Risks / Blockers
- Must avoid stat double-application when respeccing; ensure clean rebuild on any change.
- Perk effects touching behaviors (e.g., pierce) need deterministic ordering with item/affix effects.
- UI readability in isometric view—keep text concise and ensure controller focus state is visible.
- Balance risk: perk point economy can invalidate loot power; coordinate values with Tasks 029/030.

