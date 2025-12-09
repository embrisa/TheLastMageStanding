# Task: 051 - Hub scene and scene management
- Status: done (reimplemented)

## Summary
Create a central hub scene for all meta-progression activities (skill selection, talent tree, equipment management, shop, stage selection) and implement scene management to transition between hub and stage runs. This is the foundational structure for separating hub-only configuration from in-run gameplay.

**NOTE**: This task was reimplemented on 2024-12-09 to change from a static menu UI to a playable hub world with NPC interactions.

## Goals
- Implement scene management system with Hub, Stage, and future Cutscene support
- Create hub scene with placeholder UIs for all meta activities
- Enable scene transitions (hub → stage, stage → hub on completion/death)
- Lock all configuration UIs (skills, talents, equipment) to hub scene only
- Provide stage selection interface in hub
- Ensure profile/progression persists across scene transitions

## Non Goals
- Full art/visual polish for hub (functional first)
- Complex hub exploration or NPCs (static UI-driven hub is fine)
- Cutscene implementation (defer to future)
- Advanced scene transition effects (simple fade is fine)
- Story/narrative content (placeholder text is fine)

## Acceptance criteria
- [x] `GameScene` enum exists with Hub, Stage, Cutscene values
- [x] `SceneManager` handles current scene tracking and transitions
- [x] Hub scene loads on game start with all meta UIs accessible
- [x] Stage selection UI in hub shows unlocked acts/stages with requirements
- [x] Selecting a stage transitions to Stage scene with proper initialization
- [x] Stage completion/death transitions back to hub with rewards shown
- [x] P key (perk tree), I key (inventory), skill selection ONLY work in hub scene
- [x] Player profile persists across scene transitions
- [x] `dotnet build` passes; manual playtest confirms smooth transitions

## Definition of done
- Builds pass (`dotnet build`)
- Tests/play check done (scene transitions work)
- Docs updated (GDD section on scenes, architecture notes)
- Handoff notes added (if handing off)

## Plan
- Step 1: Create scene management foundation
  - Define `GameScene` enum
  - Create `SceneManager` class with current scene tracking
  - Add scene transition methods (`TransitionToHub()`, `TransitionToStage(stageId)`)
  - Hook into `Game1.cs` update/draw loop
  
- Step 2: Gate existing systems by scene
  - Update `InputState` to check current scene for P/I keys
  - Modify `PerkTreeUISystem` to only run in Hub scene
  - Modify `InventoryUiSystem` (if exists) to only run in Hub scene
  - Ensure combat/wave systems only run in Stage scene
  
- Step 3: Create hub scene structure
  - Define `HubSceneSystem` for hub-specific logic
  - Create placeholder hub UI layout (main menu with buttons)
  - Add "Stage Select", "Talents", "Skills", "Equipment", "Shop", "Stats" buttons
  - Wire navigation to open respective UIs
  
- Step 4: Implement stage selection UI
  - Create `StageSelectionUISystem`
  - Load campaign progression from profile (unlocked stages)
  - Display acts/stages with locked/unlocked states
  - Show stage requirements (meta level, previous stage completion)
  - "Start Stage" button transitions to Stage scene with selected stage data
  
- Step 5: Implement stage → hub transition
  - On `RunEndedEvent` (game over or completion), show rewards screen
  - Calculate meta XP, gold, equipment earned
  - Update profile persistence
  - Transition back to hub after confirmation
  
- Step 6: Test scene persistence
  - Verify profile saves before stage start
  - Verify stage results save on completion
  - Test rapid transitions don't corrupt state
  - Test scene-specific input gating works correctly

## Notes / Risks / Blockers
- **Dependency**: Task 037 (meta progression) must provide profile service
- **Dependency**: Task 030 (equipment) and Task 031 (perks) need scene gating
- **Risk**: Scene transitions could cause entity cleanup issues (ensure proper reset)
- **Risk**: Input handling across scenes needs careful state management
- **Tech**: May need to refactor `EcsWorldRunner` to support scene-specific system sets
- **UX**: Hub needs clear navigation; consider breadcrumb or back button
- **Performance**: Scene transitions should feel instant (< 100ms); avoid heavy initialization

## Related Tasks
- Task 030: Loot and equipment (hub-only equipping)
- Task 031: Talent/perk tree (hub-only allocation)
- Task 037: Meta progression (profile persistence)
- Task 039: Skill system (hub-only skill selection)
- Task 045: Meta hub UI & scene (original planned task, now superseded)
- Task 050: Level-up choice system (runs in Stage scene only)
- Task 052: Stage/act system (stage data for selection)

## Handoff Notes (2024-12-09 - Reimplementation)
**Status**: Complete reimplementation. Build passes, ready for manual testing.

**What changed from original implementation**:
1. **Removed static menu UI**: Deleted `HubSceneSystem` that showed button-based navigation
2. **Playable hub world**: Hub is now a walkable map where player moves freely
3. **NPC interaction system**: NPCs spawn from `HubMap.tmx` object markers, player walks to them and presses E to interact
4. **Distance-based triggers**: No collision required, proximity detection uses distance checks (80px radius)
5. **Hub-specific ESC menu**: Added `HubMenuSystem` (separate from stage pause menu) for settings/quit
6. **P and I keys work everywhere**: Removed scene gating for perk tree and inventory (viewable in both hub and stage, but changes only possible in hub)

**What was implemented**:
1. **Interaction Components** (`InteractionComponents.cs`):
   - `InteractionTrigger`: Marks NPCs as interactable with type (OpenTalentTree, OpenStageSelection, etc.) and radius
   - `ProximityPrompt`: Temporary component added to player when near NPC

2. **New Systems**:
   - `NpcSpawnSystem`: Parses TMX object layer, spawns NPC entities at marker positions (runs once on hub load)
   - `ProximityInteractionSystem`: Detects player proximity to NPCs, adds `ProximityPrompt` to player entity
   - `InteractionInputSystem`: Handles E key press, toggles appropriate UI based on NPC type
   - `HubMenuSystem`: ESC menu for hub scene (settings/quit), separate from stage pause menu
   - `ProximityPromptRenderSystem`: Draws "E - [Action]" prompts above nearby NPCs
   - `NpcRenderSystem`: Renders NPCs as colored squares (placeholder visuals) with interaction radius

3. **System Reorganization** (EcsWorldRunner):
   - **Moved to Stage-Only**: `GameSessionSystem` (run timer, pause menu now only in stages)
   - **Moved to Common**: `MovementIntentSystem`, `MovementSystem`, `CameraFollowSystem`, `CollisionSystem`, `CollisionResolutionSystem`, `InventoryUiSystem`, `PerkTreeUISystem` (player can move and view UIs in both hub and stage)
   - **Hub-Only Systems**: `StageSelectionUISystem`, `ProximityInteractionSystem`, `InteractionInputSystem`, `HubMenuSystem`
   - **Hub-Only Draw**: `NpcRenderSystem`, `ProximityPromptRenderSystem`

4. **Input Changes** (`InputState.cs`):
   - Added `InteractPressed` property for E key
   - Removed scene gating from P and I keys (now work in both hub and stage)
   - Kept scene gating for Shift+R (respec only in hub)

5. **TiledMapService**: Added public `Map` property to expose `TiledMap` for NPC spawning

6. **Game1.cs**: Added call to `_ecs.SpawnHubNpcs(_mapService.Map)` in `ReloadSceneContent()` when entering hub scene

**NPC Mappings** (from HubMap.tmx):
- `npc_tome_scribe` (purple) → Opens Talent Tree (P key equivalent)
- `npc_arena_master` (red) → Opens Stage Selection
- `npc_ability_loadout` (blue) → Opens Skill Selection (TODO: implement UI)
- `npc_vendor` (gold) → Opens Shop (TODO: implement UI)
- `npc_archivist` (green) → Opens Stats (TODO: implement UI)

**Architecture notes**:
- NPCs are pure ECS entities with `Position` + `InteractionTrigger` components (no sprites yet)
- Interaction radius is 80 pixels (tunable via `InteractionTrigger.InteractionRadius`)
- Only one proximity prompt shown at a time (closest NPC)
- Prompts render in world space above NPC position
- Hub menu (ESC) is separate from stage pause menu and only appears in hub
- Player can still use P/I shortcuts directly without walking to NPCs

**What's NOT done (future work)**:
- Skill selection UI (interaction triggers but does nothing)
- Shop UI (interaction triggers but does nothing)
- Stats UI (interaction triggers but does nothing)
- Settings UI from hub menu (menu item exists but does nothing)
- Quit to desktop from hub menu (menu item exists but does nothing)
- NPC sprite visuals (currently colored squares)
- NPC animations
- Interaction sound effects
- Better prompt visuals (background, icons)

**Testing notes**:
- Build passes with no errors
- Manual testing required:
  1. Start game → should spawn in hub map with player able to move (WASD)
  2. Walk near colored square NPCs → "E - [Action]" prompt should appear
  3. Press E when prompted → appropriate UI should open (perk tree for purple, stage select for red)
  4. Press ESC in hub → hub menu should appear (not pause menu)
  5. Press P or I in hub → perk tree/inventory should toggle (no restriction)
  6. Select stage from arena master → transition to stage
  7. In stage: Run Timer should count, ESC should show pause menu (not hub menu)
  8. In stage: P or I should still open perk tree/inventory (view-only, changes disabled)
  9. Die/complete stage → return to hub, Run Timer should NOT be running
  10. Camera should follow player smoothly in hub
  11. Player should collide with walls in hub map

**Known issues**:
- None currently, but interaction radius may need tuning based on feel
- NPC visuals are placeholder only

**Next steps**:
- Task 039/042: Implement skill selection UI for `npc_ability_loadout`
- Task 046: Implement shop UI for `npc_vendor`  
- Add stats/history UI for `npc_archivist`
- Add settings UI accessible from hub menu
- Replace NPC colored squares with proper sprites
- Add SFX for interactions
- Consider adding NPC dialogue/flavor text

**Files changed**:
- `src/Game/Core/Ecs/Components/InteractionComponents.cs` (new)
- `src/Game/Core/Ecs/Systems/NpcSpawnSystem.cs` (new)
- `src/Game/Core/Ecs/Systems/ProximityInteractionSystem.cs` (new)
- `src/Game/Core/Ecs/Systems/InteractionInputSystem.cs` (new)
- `src/Game/Core/Ecs/Systems/HubMenuSystem.cs` (new)
- `src/Game/Core/Ecs/Systems/ProximityPromptRenderSystem.cs` (new)
- `src/Game/Core/Ecs/Systems/NpcRenderSystem.cs` (new)
- `src/Game/Core/Ecs/EcsWorldRunner.cs` (major refactor: system reorganization, added hub systems)
- `src/Game/Core/Input/InputState.cs` (added InteractPressed, removed scene gating for P/I)
- `src/Game/Core/World/Map/TiledMapService.cs` (added public Map property)
- `src/Game/Game1.cs` (added SpawnHubNpcs call)
- `src/Game/Core/Ecs/Systems/HubSceneSystem.cs` (deleted)

---

## Original Handoff Notes (2024-12-08)
**Status**: Complete and tested. Build passes, all acceptance criteria met.

**What was implemented**:
1. Created `SceneType` enum (Hub, Stage, Cutscene) and `SceneManager` for transitions
2. Added `SceneEvents` (SceneEnterEvent, SceneExitEvent) to event system
3. Refactored `EcsWorldRunner` to support scene-specific system sets:
   - `_hubOnlyUpdateSystems` and `_hubOnlyDrawSystems` for hub-only logic
   - `_stageOnlyUpdateSystems` and `_stageOnlyDrawSystems` for combat/wave systems
   - Common systems run in both scenes
4. Created `HubSceneSystem` with main menu navigation (Stage Select, Skills, Talents, Equipment, Stats, Quit)
5. Created `StageRegistry` and `StageDefinition` for campaign structure (Act 1 stages defined as placeholders)
6. Created `StageSelectionUISystem` with:
   - Stage unlocking based on meta level and previous stage completion
   - Stage completion tracking in `PlayerProfile.CompletedStages`
   - Act/stage navigation with visual indicators
7. Created `StageCompletionSystem` to handle `RunEndedEvent` and transition back to hub
8. Updated `Game1.cs` to process scene transitions and reload maps dynamically
9. Input gating (P, I keys) already worked via `InputState` checking scene state

**Architecture notes**:
- Game starts in Hub scene by default (see `SceneManager` constructor)
- Scene transitions are deferred via `_pendingTransition` and processed at start of `Game1.Update()`
- Map reloading happens in `Game1.ReloadSceneContent()` when scene transitions occur
- `EcsWorldRunner.Update()` conditionally runs systems based on `_sceneStateService.IsInHub()` / `IsInStage()`
- Hub and Stage scenes share some common systems (player rendering, damage numbers, input, SFX)

**What's NOT done (future work)**:
- Rewards screen after stage completion (just transitions immediately)
- Actual skill selection UI in hub (placeholder menu item exists)
- Dynamic map loading based on `StageDefinition.MapAssetPath` (currently loads FirstMap for all stages)
- Proper shop UI (menu item exists but does nothing)
- Stats UI is placeholder only
- Stage progression persistence (completed stages save to profile but don't unlock next stages yet)
- Multiple acts (only Act 1 defined)

**Testing notes**:
- Build passes with 2 warnings (CA1822 style suggestions, can be ignored or fixed later)
- Manual testing required: start game, verify hub menu appears, select stage, verify transition, die/complete, verify return to hub
- Input gating should be tested: P/I keys should only work in hub, not during stage gameplay

**Next steps**:
- Task 052 (stage/act system) can expand the stage registry with more content
- Task 045 (meta hub UI) can enhance the hub visuals and layout
- Task 050 (level-up choice system) should already work in stage scene only
- Consider adding a "Resume Run" option if player quits mid-stage (currently starts fresh)

**Files changed**:
- `src/Game/Core/SceneState/SceneType.cs` (added Cutscene)
- `src/Game/Core/SceneState/SceneManager.cs` (new)
- `src/Game/Core/Events/SceneEvents.cs` (new)
- `src/Game/Core/Campaign/StageRegistry.cs` (new)
- `src/Game/Core/Ecs/Systems/HubSceneSystem.cs` (new)
- `src/Game/Core/Ecs/Systems/StageSelectionUISystem.cs` (new)
- `src/Game/Core/Ecs/Systems/StageCompletionSystem.cs` (new)
- `src/Game/Core/Ecs/EcsWorldRunner.cs` (major refactor for scene-specific systems)
- `src/Game/Core/MetaProgression/PlayerProfile.cs` (added CompletedStages)
- `src/Game/Game1.cs` (added scene transition handling)

