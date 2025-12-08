# Design Clarification — New Vision vs. Old Implementation

**Date**: December 8, 2025  
**Status**: Active design pivot in progress

## Overview

This document clarifies the NEW game vision and identifies conflicts with existing implementations. The game has pivoted from an endless horde-survivor to a **story-driven 4-act ARPG** with distinct progression systems.

---

## New Core Vision

### Story Structure
- **4 Acts**: Each act tells part of the narrative.
- **Multiple Stages per Act**: Each stage is a distinct map/level with waves.
- **Act Bosses**: Final stage of each act features a unique boss fight.
- **Biomes**: Each act has a unique biome with themed enemies and visuals.
- **Linear Progression**: Must complete stages in order to unlock next stage/act.

### Progression Model

#### Meta Progression (Hub Only)
- **Meta Level**: Persistent account level, **cap at 60**.
- **Meta XP**: Earned from completing stages, bosses, milestones.
- **Unlocks**: New skills, talent points, equipment access at specific meta levels.
- **Talents**: Permanent stat/ability upgrades allocated in hub; **cannot respec** (permanent choices).
- **Skills**: Unlock at meta levels; equip to hotbar (primary + 1-4) in hub only.
- **Equipment**: All found/purchased items persist in collection; equip in hub only.
- **Gold**: Persistent currency for shop purchases.

#### In-Run Progression (Per Stage)
- **Run Level**: Starts at 1 each stage, **cap at 60**.
- **XP Orbs**: Drop from enemies; leveling formula same as before.
- **Level-Up Choice**: Each level presents TWO options (pick ONE):
  1. **Stat Boost**: +HP, +Damage, +Speed, +Armor, +Power, +Crit, etc.
  2. **Skill Modifier**: +Damage%, -Cooldown%, +AoE%, +Pierce, +Projectiles (for equipped skills only).
- **No Mid-Run Unlocks**: Cannot learn new skills, allocate talents, or change equipment during a stage.
- **Resets**: All in-run levels and choices reset when starting a new stage.

#### Key Rule: Hub vs. Run
- **Hub (Meta Scene)**: Configure skills, talents, equipment, purchase from shop.
- **Run (Stage Scene)**: Locked configuration; only level-up choices for temporary power.

---

## Conflicts with Existing Implementation

### ❌ Task 022: XP Orbs and Level-Ups
**Problem**: Grants fixed stat bonuses on level-up (+2 dmg, +5 speed, +10 HP).  
**New Vision**: Level-up should present a CHOICE between stat boost OR skill modifier.  
**Action**: Need choice UI; level-up flow must pause and show 2 options.

### ❌ Task 031: Talent/Perk Tree
**Problem**: Perks allocatable mid-run with P key; grants perk points on in-run level-up; supports respec with Shift+R.  
**New Vision**: Talents ONLY in hub; no perk points from in-run leveling; no respec (permanent).  
**Action**: 
- Remove P key input during runs.
- Perk points granted from meta level-ups only.
- Remove respec functionality (or make it a costly meta-hub action).
- Perk tree UI only accessible in hub scene.

### ❌ Task 030: Loot and Equipment
**Problem**: Loot drops during runs; I key toggles inventory to equip items mid-run.  
**New Vision**: Items drop but auto-add to profile collection; cannot equip during run; equipment only in hub.  
**Action**:
- Remove inventory UI (I key) during runs.
- Keep loot drops for collection/rewards.
- Equipment management UI only in hub.

### ❌ Task 039: Skill System
**Problem**: `PlayerSkillModifiers` component allows modifier changes, but unclear when/how modifiers apply.  
**New Vision**: Modifiers from level-up choices ONLY; no mid-run skill changes.  
**Action**:
- Skill selection UI only in hub.
- Level-up choices can add to `PlayerSkillModifiers`.
- Hotbar locked when entering stage.

### ❌ Task 037: Meta Progression
**Problem**: Meta progression foundation exists but doesn't distinguish hub-only vs. in-run clearly.  
**New Vision**: Meta level cap 60; unlocks are gated by meta level; clear separation.  
**Action**:
- Enforce meta level cap at 60.
- Define unlock table (which skills/talents at which meta levels).
- Create hub scene with all meta activities.

### ⚠️ Current Wave System
**Problem**: Endless waves with no stages/acts/bosses.  
**New Vision**: Stages with wave progression leading to boss; completing stage unlocks next.  
**Action**:
- Define stage structure (stage = map + wave config + boss).
- Act/stage data model and progression tracking.
- Boss mechanics per act.

### ⚠️ Missing: Hub Scene
**Problem**: No hub scene implemented yet.  
**New Vision**: Central hub for all meta activities (skill selection, talent tree, equipment, shop, stage select).  
**Action**:
- Task 045: Meta Hub UI & Scene (planned).
- Need scene switching (hub ↔ stage).

### ⚠️ Missing: Story/Biomes
**Problem**: No acts, biomes, or story content implemented.  
**New Vision**: 4 acts with unique biomes and narrative.  
**Action**:
- Define acts and biomes (themes, enemies, bosses).
- Create story beats and quest structure (if applicable).
- Design act-specific enemies and mechanics.

---

## Code Changes Required

### High Priority (Breaks Current Design)

1. **Level-Up Choice UI** (Task 022 update):
   - Replace auto-stat-boost with choice screen.
   - Show 2 cards: stat boost vs. skill modifier.
   - Pause game on level-up until choice made.

2. **Remove Mid-Run Perk Allocation** (Task 031 update):
   - Disable P key during runs.
   - Move perk UI to hub scene only.
   - Stop granting perk points on in-run levels.
   - Remove or gate respec functionality.

3. **Remove Mid-Run Equipment** (Task 030 update):
   - Disable I key during runs.
   - Keep loot drops for collection.
   - Move equipment UI to hub scene only.

4. **Lock Skill Hotbar** (Task 039 update):
   - Skills configured in hub.
   - Hotbar locked when entering stage.
   - Level-up modifiers apply to equipped skills only.

### Medium Priority (Missing Features)

5. **Meta Level Cap Enforcement** (Task 037 update):
   - Hard cap at 60.
   - Define skill/talent unlock table by meta level.

6. **Hub Scene Creation** (Task 045):
   - Central meta scene with all configuration UIs.
   - Scene switching system (hub ↔ stage).

7. **Stage/Act System**:
   - Stage data model (map, wave config, boss).
   - Act progression tracking.
   - Boss encounters per act.

8. **Biome & Enemy Variants**:
   - Define act-specific enemy sets.
   - Biome themes and visuals.

### Low Priority (Polish & Content)

9. **Story Integration**:
   - Narrative beats per act.
   - Quest system (if needed).

10. **Shop System** (Task 046):
    - Hub shop UI for equipment purchase.

11. **Run History UI** (Task 049):
    - Display past runs and stats.

---

## Testing & Validation Plan

### Phase 1: Remove Conflicts
1. Disable P key, I key during runs.
2. Verify perk/equipment UIs are inaccessible in play state.
3. Test that loot still drops and adds to profile.

### Phase 2: Level-Up Choice
1. Implement choice UI with 2 options.
2. Test stat boost application.
3. Test skill modifier application.
4. Verify choices persist through run but reset on restart.

### Phase 3: Meta Level Cap
1. Enforce level 60 cap.
2. Test XP accumulation stops at cap.
3. Define and test unlock table.

### Phase 4: Hub Scene
1. Create hub scene with placeholder UIs.
2. Implement scene switching.
3. Test skill/talent/equipment configuration in hub.
4. Test configuration persists into runs.

### Phase 5: Stage/Act System
1. Define stage data structure.
2. Implement stage selection from hub.
3. Test stage completion and unlocking.
4. Add boss encounters.

---

## Migration Notes for Agents

### When Working on Existing Tasks
- **Check this document first** to see if the task conflicts with new vision.
- **Do not implement features** that allow mid-run skill/talent/equipment changes.
- **Focus on hub-based configuration** for skills, talents, equipment.

### When Creating New Tasks
- **Separate hub vs. run logic** clearly in task descriptions.
- **Enforce level caps** (meta 60, run 60) in all progression code.
- **Design for choice** (level-up choices, not fixed bonuses).

### When Updating Documentation
- **Update task files** to reflect new vision where conflicts exist.
- **Reference this document** in task notes for clarity.
- **Update GDD** alongside task changes to keep in sync.

---

## Summary

**Old Vision**: Endless horde-survivor with in-run perk allocation and equipment swapping.  
**New Vision**: Story-driven 4-act ARPG with hub-based configuration and in-run level-up choices.

**Key Changes**:
- Meta level cap: 60
- In-run level cap: 60 per stage
- Skills, talents, equipment: hub only
- Level-ups: choice between stat boost OR skill modifier
- No mid-run respec or equipment swapping
- Story structure: 4 acts, multiple stages, act bosses, unique biomes

**Next Steps**:
1. Update conflicting task implementations (022, 030, 031, 039).
2. Create hub scene (Task 045).
3. Build stage/act system and progression tracking.
4. Define biomes and act-specific content.
