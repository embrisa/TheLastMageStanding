# Task: 051 - Hub scene and scene management
- Status: backlog

## Summary
Create a central hub scene for all meta-progression activities (skill selection, talent tree, equipment management, shop, stage selection) and implement scene management to transition between hub and stage runs. This is the foundational structure for separating hub-only configuration from in-run gameplay.

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
- [ ] `GameScene` enum exists with Hub, Stage, Cutscene values
- [ ] `SceneManager` handles current scene tracking and transitions
- [ ] Hub scene loads on game start with all meta UIs accessible
- [ ] Stage selection UI in hub shows unlocked acts/stages with requirements
- [ ] Selecting a stage transitions to Stage scene with proper initialization
- [ ] Stage completion/death transitions back to hub with rewards shown
- [ ] P key (perk tree), I key (inventory), skill selection ONLY work in hub scene
- [ ] Player profile persists across scene transitions
- [ ] `dotnet build` passes; manual playtest confirms smooth transitions

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
