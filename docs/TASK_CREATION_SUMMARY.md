# Task Creation Summary

**Date**: December 8, 2025  
**Status**: Complete ✅

---

## Overview

Created 7 new task documents (Tasks 050-056) to replace the technical code changes summary with properly structured, actionable tasks following the project's task template standard.

---

## Tasks Created

All tasks follow `TASK_TEMPLATE.md` and include: Summary, Goals, Non Goals, Acceptance Criteria, Definition of Done, Plan, Notes/Risks/Blockers, and Related Tasks.

### Phase 1 - Remove Conflicts (HIGH PRIORITY)

**Task 053: Remove mid-run configuration access**
- Gate P key (perk tree), I key (inventory), Shift+R (respec) behind hub scene
- Stop granting perk points on in-run level-ups
- Lock skill hotbar when entering stages
- Enforce hub-only configuration model
- **Priority**: Immediate (prevents deepening design conflicts)

### Phase 2 - Core New Features (HIGH PRIORITY)

**Task 050: Level-up choice system**
- Replace automatic stat bonuses with choice UI
- Present 2 options: stat boost OR skill modifier
- Pause game on level-up until choice made
- Integrate with existing XP/skill systems
- **Priority**: High (core new feature)

**Task 051: Hub scene and scene management**
- Create Hub, Stage, Cutscene scene enum
- Implement scene manager with transitions
- Build hub scene with all meta UIs
- Stage selection interface
- **Priority**: High (foundational for all other tasks)

### Phase 3 - Meta Progression (MEDIUM PRIORITY)

**Task 054: Meta progression system updates**
- Enforce meta level cap at 60
- Define skill unlock table (which skills at which levels)
- Define talent point grant table
- Create MetaLevelUpEvent (separate from in-run)
- Update meta XP formula (stage-focused)
- **Priority**: Medium (extends Task 037)

**Task 055: Skill selection hub UI**
- Hub UI for browsing unlocked skills
- Show locked skills with requirements
- Equip skills to 5 hotbar slots (primary + 1-4)
- Keyboard navigation
- Persist to profile
- **Priority**: Medium (depends on Task 051, 054)

**Task 056: Equipment management hub UI**
- Hub UI for browsing equipment collection
- Filter by slot and rarity
- Stat comparison view
- Equip items to 5 slots
- Persist to profile
- **Priority**: Medium (depends on Task 051)

### Phase 4 - Campaign Structure (MEDIUM PRIORITY)

**Task 052: Stage/act campaign system**
- Define 4 acts with unique biomes
- Create stage definitions (3-6 stages per act)
- Implement boss encounters (multi-phase)
- Campaign progression tracking
- Act 1 fully implemented, Acts 2-4 framework
- **Priority**: Medium (major content work)

---

## Integration with Existing Work

### Updates to TASKS.md
- Added new "Design Alignment Arc" section
- Organized tasks by phase with priority levels
- Marked old skill UI tasks (042, 043) as superseded
- Kept task 041 (hotkey input) and 044 (balance) as still relevant

### Dependencies Between Tasks
- **Task 051** (hub scene) is foundational for 053, 055, 056
- **Task 054** (meta updates) enables 055 (skill selection)
- **Task 050** (level-up choices) can be done independently
- **Task 053** (remove conflicts) should be done ASAP
- **Task 052** (campaign) depends on 051 (hub scene)

### Recommended Implementation Order
1. **Task 053** (remove conflicts) — Prevent deepening design mismatch
2. **Task 051** (hub scene) — Foundation for everything else
3. **Task 050** (level-up choices) — Core new feature, can parallel with 051
4. **Task 054** (meta updates) — After hub scene exists
5. **Task 055** (skill UI) — After 051 + 054
6. **Task 056** (equipment UI) — After 051
7. **Task 052** (campaign) — Large content task, can be incremental

---

## Task Scope Summary

### Small/Quick (1-2 days)
- Task 053: Remove mid-run configuration (~1 day)

### Medium (3-5 days)
- Task 050: Level-up choice system (~3-4 days)
- Task 054: Meta progression updates (~2-3 days)
- Task 055: Skill selection UI (~3-4 days)
- Task 056: Equipment UI (~3-4 days)

### Large (5-10 days)
- Task 051: Hub scene and scene management (~5-7 days)
- Task 052: Stage/act campaign system (~7-10 days, Act 1 only)

**Total Estimated Effort**: 25-35 days (5-7 weeks)

---

## Documentation Structure

```
docs/
  ├── game-design-document.md (UPDATED - new vision)
  ├── DESIGN_CLARIFICATION.md (NEW - conflict analysis)
  └── ALIGNMENT_SUMMARY.md (UPDATED - task references)

tasks/
  ├── 050-level-up-choice-system.md (NEW)
  ├── 051-hub-scene-and-scene-management.md (NEW)
  ├── 052-stage-act-campaign-system.md (NEW)
  ├── 053-remove-mid-run-configuration.md (NEW)
  ├── 054-meta-progression-updates.md (NEW)
  ├── 055-skill-selection-hub-ui.md (NEW)
  └── 056-equipment-management-hub-ui.md (NEW)

TASKS.md (UPDATED - new Design Alignment Arc section)
AGENTS.md (UPDATED - new vision context)
```

---

## Key Features of Task Documents

Each task includes:

✅ **Clear Summary**: What and why  
✅ **Explicit Goals**: What's in scope  
✅ **Non-Goals**: What's explicitly out of scope  
✅ **Observable Acceptance Criteria**: Checkboxes for validation  
✅ **Definition of Done**: Build/test/docs requirements  
✅ **Step-by-Step Plan**: Concrete implementation steps  
✅ **Risk Assessment**: Known risks, blockers, dependencies  
✅ **Related Tasks**: Cross-references for context  
✅ **Notes Section**: Design decisions, open questions, UX considerations

---

## Open Questions (From Tasks)

These questions appear across multiple tasks and should be answered before implementation:

1. **In-run level cap**: 60 to match meta level, or lower per stage?
2. **Talent respec**: Remove entirely or allow in hub with cost?
3. **Loot drops**: Keep during runs for collection or remove?
4. **XP formulas**: Same for meta and in-run, or different?
5. **Hub entrance**: When does player first access hub? (Game start vs. tutorial)
6. **Stage difficulty**: How does difficulty scale across acts/stages?
7. **Boss phases**: How many phases per boss, what mechanics?
8. **Act themes**: Need clarity on biome themes and narrative for Acts 2-4

---

## Testing Strategy

Each task includes testing requirements:

- **Unit Tests**: Where applicable (services, calculations)
- **Integration Tests**: Scene transitions, progression flow
- **Manual Playtest**: Required for all UI and gameplay tasks
- **Build Verification**: `dotnet build` must pass
- **Regression Check**: Ensure existing features still work

---

## Handoff Notes

For agents picking up these tasks:

1. **Read DESIGN_CLARIFICATION.md first** — Understand the design pivot
2. **Check task dependencies** — Some tasks block others
3. **Follow the task template** — Update status, plan, notes daily
4. **Reference GDD** — Keep game-design-document.md in sync with changes
5. **Update TASKS.md** — Move tasks between sections as status changes
6. **Build frequently** — Catch issues early
7. **Document decisions** — Add notes to task files when making design choices

---

## Success Metrics

This alignment effort will be successful when:

- [ ] All 7 tasks have been implemented and tested
- [ ] Build passes with no errors or warnings
- [ ] Game runs with new progression flow (hub → stage → hub)
- [ ] No mid-run configuration access (skills/talents/equipment hub-only)
- [ ] Level-up choices work and feel good
- [ ] Meta level cap enforced at 60
- [ ] Act 1 fully playable with 3 stages and boss
- [ ] Profile persistence works across all scenes
- [ ] Documentation stays in sync with implementation

---

## Conclusion

The code changes summary has been successfully transformed into 7 properly structured task documents that can be picked up by any agent. All tasks are tracked in TASKS.md under the "Design Alignment Arc" section, organized by phase and priority.

**Next Step**: Review tasks and begin implementation with Task 053 (remove conflicts) to prevent deepening the design mismatch.

Build Status: ✅ **PASSING**  
Tasks Created: ✅ **7 TASKS (050-056)**  
Documentation: ✅ **COMPLETE**
