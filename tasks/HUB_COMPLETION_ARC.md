# Hub Completion Arc — Task Summary

This document outlines the complete task arc to bring the hub to a fully implemented state following the Task 051 reimplementation (playable hub with NPC interactions).

## Overview

The hub is now a playable world where players walk to NPCs to access meta-progression features. This arc completes all hub functionality and adds polish.

## Task Breakdown

### Phase 1: Core Functionality (HIGH PRIORITY)
These tasks make the hub fully functional for all meta-progression activities.

**061 - Debug NPC Visibility** (IN PROGRESS)
- **Status**: Blocking all other hub tasks
- **Goal**: Fix NPCs not appearing; add debug logging
- **Estimate**: 1-2 hours
- **Blocker**: Cannot test other hub features until NPCs are visible

**062 - Skill Selection UI**
- **Goal**: Equip skills to hotbar via blue NPC (`npc_ability_loadout`)
- **Depends on**: Task 061 complete
- **Estimate**: 4-6 hours
- **Impact**: Enables skill customization in hub

**063 - Shop UI & Equipment Purchasing**
- **Goal**: Buy equipment with gold via gold NPC (`npc_vendor`)
- **Depends on**: Task 061 complete
- **Estimate**: 4-6 hours
- **Impact**: Enables equipment acquisition

**064 - Stats & Run History UI**
- **Goal**: View progression stats via green NPC (`npc_archivist`)
- **Depends on**: Task 061 complete
- **Estimate**: 3-4 hours
- **Impact**: Provides player feedback on progression

**065 - Hub Menu Actions**
- **Goal**: Wire up Settings and Quit from ESC menu
- **Depends on**: Task 061 complete (for testing)
- **Estimate**: 1-2 hours
- **Impact**: Completes hub menu functionality

### Phase 2: Visual Polish (MEDIUM PRIORITY)
These tasks improve hub aesthetics and player experience.

**066 - NPC Visual Improvements**
- **Goal**: Replace colored squares with sprites, animations, name plates
- **Depends on**: Task 061 complete, art assets created
- **Estimate**: 4-6 hours (code) + art time
- **Impact**: Professional appearance, clearer NPC identification

**067 - Hub Map Environmental Polish**
- **Goal**: Decorations, props, lighting, music, ambience
- **Depends on**: Art and audio assets created
- **Estimate**: 6-8 hours (map design + code) + asset time
- **Impact**: Immersive hub atmosphere

**068 - Hub Tutorial & First-Time UX**
- **Goal**: Guide new players through hub features
- **Depends on**: Tasks 062-065 complete (all hub features working)
- **Estimate**: 3-4 hours
- **Impact**: Better onboarding, reduced player confusion

## Estimated Timeline

- **Phase 1 (Core)**: 13-20 hours development time
- **Phase 2 (Polish)**: 13-18 hours development time + asset creation
- **Total**: ~26-38 hours development + art/audio assets

## Dependency Graph

```
061 (Debug NPCs) 
├── 062 (Skill Selection UI)
├── 063 (Shop UI)
├── 064 (Stats UI)
├── 065 (Hub Menu Actions)
└── 066 (NPC Visuals)
    └── 067 (Hub Polish)
        └── 068 (Tutorial)
```

## Current Blockers

1. **Task 061** is blocking all other tasks - NPCs must be visible first
2. **Art assets** needed for Tasks 066, 067 (can be done in parallel with Phase 1)
3. **Audio assets** (hub music) needed for Task 067

## Success Metrics

Upon completion of all tasks:
- ✅ All 5 NPCs visible and interactable
- ✅ All meta-progression activities accessible (skills, talents, equipment, shop)
- ✅ Player can view stats and progression
- ✅ Hub menu fully functional (settings, quit)
- ✅ Hub has professional visual appearance
- ✅ New players understand hub navigation
- ✅ Hub feels distinct from combat stages (safe haven atmosphere)

## Next Steps

1. **Immediate**: Complete Task 061 (debug NPC visibility)
2. **Parallel**: Begin art asset creation for Tasks 066-067
3. **Sequential**: Complete Tasks 062-065 in priority order
4. **Polish**: Complete Tasks 066-068 for release readiness

## Notes

- Phase 1 tasks can be done in any order after Task 061 is complete
- Phase 2 tasks have dependencies and should be done sequentially
- Consider creating placeholder art if final assets are delayed
- Tutorial (Task 068) should be last to ensure all features are stable
