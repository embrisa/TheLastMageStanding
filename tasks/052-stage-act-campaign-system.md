# Task: 052 - Stage/act campaign system
- Status: done

## Summary
Implement the 4-act campaign structure with multiple stages per act, act bosses, biome definitions, and linear progression tracking. This replaces the endless wave system with a story-driven stage progression where completing a stage unlocks the next.

## Goals
- Define 4 acts with unique biomes (themes, enemies, visuals)
- Create stage definitions (map, wave config, boss flag) for each act
- Implement act boss encounters (unique multi-phase bosses per act)
- Track campaign progression (which stages/acts are unlocked/completed)
- Persist progression in player profile
- Integrate stage selection with hub scene (Task 051)
- Provide meaningful rewards for stage completion and boss kills

## Non Goals
- Full story/narrative scripting (placeholder text is fine)
- Cutscenes or dialogue systems (defer to future)
- Complex boss AI (functional multi-phase is enough)
- All 4 acts fully designed (Act 1 + framework is acceptable)
- Quest system or side objectives
- Difficulty scaling beyond basic tuning

## Acceptance criteria
- [x] `ActDefinition` and `StageDefinition` data models exist
- [x] Campaign config defines 4 acts with at least 3 stages each (Act 1 fully fleshed out)
- [x] Each act has unique biome theme (at minimum: name, color palette, enemy set)
- [x] Act bosses defined with multi-phase mechanics (at least Act 1 boss implemented)
- [x] `CampaignProgressionService` tracks unlocked/completed stages
- [x] Completing a stage unlocks the next stage/act
- [x] Boss stages award significant rewards (meta XP, gold, guaranteed loot)
- [x] Stage selection UI shows progression and locked/unlocked states
- [x] Profile persistence includes campaign progress
- [x] `dotnet build` passes; Act 1 fully playable end-to-end

## Definition of done
- Builds pass (`dotnet build`)
- Tests/play check done (Act 1 fully playable)
- Docs updated (GDD with act descriptions, boss mechanics)
- Handoff notes added (if handing off)

## Implementation notes (2025-12-13)
- Campaign models and config live in `src/Game/Core/Campaign/CampaignData.cs` and `src/Game/Core/Campaign/StageRegistry.cs`.
- Stage runs are now capped by `StageDefinition.MaxWaves`; non-boss stages complete after the final wave.
- Boss stages spawn a deterministic act boss at the boss wave and complete on boss death; bosses have a simple multi-phase system (stat scaling + summon bursts).
- Campaign progression persists via `PlayerProfile.CompletedStages` and is saved as part of the meta progression run finalization.
- Stage selection UI now shows act names and clearer lock requirements.
- Maps are still placeholders (`Tiles/Maps/FirstMap`) for all stages until unique stage maps/arenas are authored.

## Plan
- Step 1: Define campaign data model
  - Create `Core/Campaign/CampaignData.cs`
  - Define `ActDefinition`, `StageDefinition`, `BossDefinition`, `BiomeDefinition`
  - Create `CampaignConfig` with 4 acts
  
- Step 2: Design Act 1 (Tutorial Biome)
  - **Act 1: "The Fallen Academy"**
    - Biome: Ruined magical academy
    - Enemies: Basic undead (Hexers, Bone Mages)
    - Stage 1: Courtyard (tutorial, waves 1-5)
    - Stage 2: Library (waves 1-8, introduces elites)
    - Stage 3: Headmaster's Hall (waves 1-10, boss: Corrupted Headmaster)
  - Define boss phases, attacks, rewards
  
- Step 3: Stub out Acts 2-4 (basic framework)
  - **Act 2**: (Name TBD, mid-game biome, waves 1-12, elite-heavy)
  - **Act 3**: (Name TBD, late-game biome, waves 1-15, advanced mechanics)
  - **Act 4**: (Name TBD, endgame biome, waves 1-20, final boss)
  - Placeholder stage/boss definitions
  
- Step 4: Implement campaign progression service
  - Create `CampaignProgressionService`
  - Load/save campaign state from profile
  - Methods: `IsStageUnlocked()`, `CompleteStage()`, `GetUnlockedStages()`, `GetCurrentAct()`
  - Unlock logic: Stage N requires Stage N-1 completed
  
- Step 5: Integrate stage selection with hub
  - Update `StageSelectionUISystem` (from Task 051) to use campaign data
  - Display acts as sections, stages as selectable items
  - Show locked stages with requirements (e.g., "Complete Act 1 Stage 2")
  - "Start Stage" button loads selected stage
  
- Step 6: Implement boss encounters
  - Extend `EnemyEntityFactory` to support boss entities
  - Create `BossPhaseSystem` to handle phase transitions
  - Define Act 1 boss: Corrupted Headmaster
    - Phase 1: Ranged attacks + summons (0-100% HP)
    - Phase 2: Melee + telegraphed AoE (below 50% HP)
    - Phase 3: Enrage + rapid attacks (below 25% HP)
  - Boss death triggers stage completion
  
- Step 7: Implement stage completion flow
  - On `WaveCompletedEvent` (final wave), check if boss stage
  - If boss stage, boss death triggers `StageCompletedEvent`
  - Show rewards screen (meta XP, gold, loot, unlocks)
  - Update campaign progress via `CampaignProgressionService`
  - Transition to hub
  
- Step 8: Define biome-specific content
  - Placeholder: Act-specific enemy tints/scales (reuse existing sprites)
  - Placeholder: Act-specific music tracks (or silence)
  - Future: Unique tilesets, enemy sprites, VFX

## Notes / Risks / Blockers
- **Dependency**: Task 051 (hub scene) for stage selection integration
- **Dependency**: Task 037 (meta progression) for campaign persistence
- **Dependency**: Task 032 (boss waves) provides baseline boss mechanics
- **Risk**: Boss difficulty tuning requires extensive playtesting
- **Risk**: Content creation for 4 acts is significant scope; prioritize Act 1
- **Design**: Act themes and narrative need clarity (TBD: story outline?)
- **Tech**: Boss phase system needs to be robust (health thresholds, state machine)
- **Balance**: Stage rewards need tuning to feel meaningful but not game-breaking

## Act Structure (Preliminary)

### Act 1: The Fallen Academy (Tutorial)
- **Biome**: Ruined magical academy, grey stone, arcane corruption
- **Enemies**: Hexers, Bone Mages, Elite Hexers
- **Stages**: 3 (Courtyard, Library, Headmaster's Hall)
- **Boss**: Corrupted Headmaster (ranged caster with summons)
- **Unlock**: Available from start (meta level 1)

### Act 2: [TBD] (Mid-game)
- **Biome**: [Define biome theme]
- **Enemies**: [New enemy types]
- **Stages**: 4
- **Boss**: [Define boss]
- **Unlock**: Complete Act 1 (meta level 10+)

### Act 3: [TBD] (Late-game)
- **Biome**: [Define biome theme]
- **Enemies**: [Advanced enemies]
- **Stages**: 4-5
- **Boss**: [Define boss]
- **Unlock**: Complete Act 2 (meta level 25+)

### Act 4: [TBD] (Endgame)
- **Biome**: [Final biome theme]
- **Enemies**: [Elite variants, unique enemies]
- **Stages**: 5-6
- **Boss**: [Final boss with complex mechanics]
- **Unlock**: Complete Act 3 (meta level 40+)

## Related Tasks
- Task 032: Elites/boss waves & rewards (baseline boss mechanics)
- Task 037: Meta progression (campaign persistence)
- Task 051: Hub scene (stage selection UI)
- Task 002: Basic enemy wave prototype (foundation)
- Task 005: ECS enemy waves combat (wave system)
