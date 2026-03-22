# Task: 079 - Unify persistence and save-slot boundaries
- Status: in-progress (implementation complete, manual two-slot smoke test pending)

## Summary
Standardize how runtime systems access persistence and ensure all player/run data respects save-slot boundaries. Persistence construction is currently scattered across bootstrap and feature code, and at least some run data uses a global file path instead of the active slot.

## Goals
- Centralize creation of persistence services behind a shared composition root
- Inject `IFileSystem` and slot-scoped paths instead of constructing file access ad hoc in feature code
- Ensure run, profile, equipment, settings, and history data all use intentional save boundaries
- Remove global-save behavior where slot-specific behavior is required

## Non Goals
- Redesigning the save slot UX
- Changing progression formulas or player-facing meta systems
- Migrating file formats unless necessary for slot correctness
- Cloud save support

## Acceptance criteria
- [x] Runtime feature code no longer calls `new DefaultFileSystem()` directly except in a clear composition root
- [x] Services that persist player or run data receive explicit slot-scoped paths/dependencies
- [x] In-run equipment persistence is either made slot-aware or intentionally retired/replaced
- [x] Persistence responsibilities are documented clearly enough for future tasks to extend safely
- [x] Existing save/profile/history behavior still works across multiple slots
- [ ] `dotnet build` passes; manual verification covers at least two save slots

## Definition of done
- Builds pass (`dotnet build`)
- Tests/play check done
- Docs updated (if applicable)
- Handoff notes added

## Plan
- Step 1: Inventory all persistence entry points and file paths
- Step 2: Define intended save boundaries for settings, profile, history, and run-scoped data
- Step 3: Introduce a shared persistence composition layer
- Step 4: Refactor affected services to receive dependencies instead of constructing them directly
- Step 5: Resolve the current run equipment save path mismatch
- Step 6: Verify multi-slot behavior manually and with tests where practical

## Notes / Risks / Blockers
- **Risk**: Data migration and backward compatibility may be needed for existing local saves
- **Dependency**: Task 077 should align with the new bootstrap/composition root
- **Testing**: Verify that slot switching does not leak equipment or history between profiles

## Handoff Notes
- Current status: Slot-scoped persistence is in place, and follow-up hardening now treats corrupt run-state saves as errors instead of silently resetting them. Missing `current_run_perks.json` / `current_run_equipment.json` still means “no saved run state”; malformed or unreadable files now log and throw during development. Build passes. Manual two-slot smoke coverage is still pending.
- Files changed:
  - `src/Game/Core/MetaProgression/PersistenceRoot.cs`
  - `src/Game/Core/MetaProgression/SaveSlotService.cs`
  - `src/Game/Core/MetaProgression/MetaProgressionManager.cs`
  - `src/Game/Core/SceneState/SceneRuntimeService.cs`
  - `src/Game/Core/Ecs/EcsWorldRunner.cs`
  - `src/Game/Core/Ecs/Runtime/EcsRuntimeModuleContext.cs`
  - `src/Game/Core/Ecs/Runtime/EcsCommonRuntimeModule.cs`
  - `src/Game/Core/Ecs/Systems/PerkAutoSaveSystem.cs`
  - `src/Game/Core/Player/PerkPersistence.cs`
  - `src/Game/Core/Player/EquipmentPersistence.cs`
  - `src/Game/Core/Player/EquipmentAutoSaveSystem.cs`
  - `src/Game/Game1.cs`
  - `src/Game.Tests/MetaProgression/PersistenceTests.cs`
  - `src/Game.Tests/Perks/PerkPersistenceTests.cs`
  - `src/Game.Tests/Loot/EquipmentPersistenceTests.cs`
  - `src/Game.Tests/MetaProgression/MetaProgressionManagerTests.cs`
  - `docs/game-design-document.md`
  - `src/Game/Core/Perks/README.md`
- Tests/build/manual checks run:
  - `dotnet test src/Game.Tests/TheLastMageStanding.Game.Tests.csproj --filter "FullyQualifiedName~EquipmentPersistenceTests|FullyQualifiedName~PerkPersistenceTests|FullyQualifiedName~PersistenceRootTests"`
  - `dotnet build`
  - Manual smoke check not run in this session
- Next concrete step:
  - Run a manual two-slot smoke test through main menu -> create/load two slots -> verify profile, history, and current-run perk/equipment state stay isolated.
- Decisions made:
  - Added `PersistenceRoot` as the shared composition layer for filesystem access, root paths, slot discovery, and slot-scoped service construction.
  - Kept audio/video/input settings global; moved profile/history/current-run perk and equipment persistence behind slot scopes.
  - Made in-run equipment persistence slot-aware but did not newly register its autosave system; it remains available for runtime wiring without leaking across slots.
  - Hardened run-state persistence so only an actually missing file returns `null`; corrupted data or filesystem failures now fail loudly and surface through tests.
- Risks/blockers:
  - No migration path was added for legacy global `current_run_perks.json` / `current_run_equipment.json`; old files are intentionally ignored.
  - Manual multi-slot verification is still required before marking the task complete.
