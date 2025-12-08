# Implementation Order — Tasks 041-056

**Date:** 2025-01-19  
**Context:** After completing Task 040, we have tasks from two different contexts:
- **Tasks 041-049:** Created before the design pivot to 4-act story-driven ARPG
- **Tasks 050-056:** Created after design pivot to align codebase with new vision

This document clarifies the implementation order and which tasks need modification.

---

## TL;DR — Critical Path

1. **Task 053** (Remove mid-run config) — IMMEDIATE
2. **Task 051** (Hub scene) — HIGH PRIORITY (blocks most others)
3. **Task 041** (Skill hotkeys) — Can be done NOW (still valid)
4. **Task 050** (Level-up choices) — HIGH PRIORITY
5. Continue with tasks 054-056, then modified 044-049

---

## Recommended Implementation Order

### **IMMEDIATE PRIORITY**
**Do this first to prevent deepening design conflicts:**

#### 1. Task 053 — Remove Mid-Run Configuration Access
- **Why First:** Prevents continuing to build features around wrong design
- **Effort:** Small (2-3 hours)
- **Action:** Gate P key (talents), I key (inventory), Shift+R (run summary) behind hub scene
- **Status:** HIGH PRIORITY

---

### **HIGH PRIORITY (Foundation)**
**These are critical to the new game vision and block other work:**

#### 2. Task 051 — Hub Scene and Scene Management
- **Why Second:** Hub is the foundation for ALL meta activities (skills/talents/equipment/shop)
- **Effort:** Medium (5-7 hours)
- **Blocks:** Tasks 054, 055, 056, 045, 046, 047
- **Status:** HIGH PRIORITY

#### 3. Task 041 — Skill Hotkey Input (Keys 1-4)
- **Why Now:** Still valid under new design; skills exist, just need hotkey casting
- **Effort:** Small (1-2 hours)
- **Dependencies:** Task 040 (complete)
- **Status:** HIGH PRIORITY — **CAN START NOW**

#### 4. Task 050 — Level-Up Choice System
- **Why After 041:** Players need working skills before choices make sense
- **Effort:** Medium (4-6 hours)
- **Replaces:** Auto-stat-boosts with choice UI (stat boost OR skill modifier)
- **Status:** HIGH PRIORITY

---

### **MEDIUM PRIORITY (Meta Progression)**
**Build out hub features and meta systems:**

#### 5. Task 054 — Meta Progression System Updates
- **Dependencies:** Task 051 (hub scene)
- **Effort:** Medium (4-6 hours)
- **Action:** Enforce level cap 60, unlock tables for skills/talents/equipment
- **Status:** MEDIUM PRIORITY

#### 6. Task 055 — Skill Selection Hub UI
- **Dependencies:** Task 051 (hub), Task 054 (unlocks)
- **Effort:** Medium (4-5 hours)
- **Replaces:** Task 042 (superseded)
- **Status:** MEDIUM PRIORITY

#### 7. Task 056 — Equipment Management Hub UI
- **Dependencies:** Task 051 (hub), Task 054 (unlocks)
- **Effort:** Medium (4-6 hours)
- **Partially replaces:** Task 046 (equipment browsing/equipping)
- **Status:** MEDIUM PRIORITY

#### 8. Task 044 — Skill Balance & Feel Pass
- **Dependencies:** Tasks 041-043 complete (skills fully playable)
- **Effort:** Medium (2-3 hours playtesting)
- **Status:** MEDIUM PRIORITY — after skills are fully usable

---

### **MEDIUM PRIORITY (Hub Features — Modified)**
**These tasks need updates to fit new design:**

#### 9. Task 045 — Meta Hub Main Menu
- **Status:** BLOCKED — overlaps with Task 051
- **Decision:** Either merge into Task 051 or defer until 051 complete
- **Original Effort:** Medium (4-6 hours)

#### 10. Task 046 — Shop UI (Modified Scope)
- **Status:** NEEDS UPDATE — Task 056 covers equipment UI
- **New Scope:** Focus ONLY on shop purchasing UI (buy items with gold/currency)
- **Dependencies:** Task 051 (hub), Task 056 (equipment)
- **Reassess:** After Task 056 complete

#### 11. Task 047 — Talent Tree Hub Integration (Modified)
- **Status:** NEEDS UPDATE + BLOCKED by Task 051
- **Issue:** Assumes in-run talent access (P key in Goals)
- **Required Change:** Remove in-run access; hub-only integration
- **Dependencies:** Task 051 (hub), Task 054 (unlocks)

#### 12. Task 049 — Run History & Stats UI
- **Status:** Independent, no conflicts
- **Dependencies:** Task 051 (hub for navigation)
- **Effort:** Medium (3-4 hours)
- **Can be done:** Anytime after Task 051

---

### **MEDIUM PRIORITY (Campaign)**

#### 13. Task 052 — Stage/Act Campaign System
- **Status:** Independent of hub work
- **Effort:** Large (8-12 hours)
- **Action:** Implement 4 acts, stages, bosses, biomes, progression
- **Can be done:** In parallel with hub tasks or after

---

### **SUPERSEDED TASKS**
**These are fully replaced by new tasks:**

- ~~**Task 042** (Skill selection UI)~~ → **Replaced by Task 055**
- ~~**Task 043** (Skill unlock progression)~~ → **Replaced by Task 054**

---

### **CONFLICTING TASK**
**This needs complete reconsideration:**

#### Task 048 — In-Run Inventory & Equipment Swapping
- **Status:** CONFLICTS WITH NEW DESIGN — RECONSIDER
- **Issue:** New vision mandates hub-only equipment; no mid-run swapping
- **Options:**
  1. **Remove entirely** (simplest)
  2. **Rewrite as read-only stats display** (character sheet)
- **Decision needed:** What, if any, in-run equipment UI is needed?

---

## Summary of Changes to Tasks 041-049

| Task | Original Status | New Status | Action Required |
|------|----------------|------------|-----------------|
| 041 | Backlog | **HIGH PRIORITY** | ✅ No changes needed — still valid |
| 042 | Backlog | **SUPERSEDED** | ❌ Use Task 055 instead |
| 043 | Backlog | **SUPERSEDED** | ❌ Use Task 054 instead |
| 044 | Backlog | Medium Priority | ⚠️ Do after 041-043 complete |
| 045 | Backlog | **BLOCKED** | ⚠️ Overlaps with Task 051 — merge or defer |
| 046 | Backlog | **NEEDS UPDATE** | ⚠️ Narrow scope to shop only (not equipment UI) |
| 047 | Backlog | **NEEDS UPDATE** | ⚠️ Remove in-run access, hub-only |
| 048 | Backlog | **CONFLICTS** | ❌ Reconsider entirely |
| 049 | Backlog | Medium Priority | ✅ No changes needed — still valid |

---

## Decision Points

### 1. Task 045 vs Task 051
**Question:** Merge Task 045 into 051, or keep separate?  
**Recommendation:** Merge — they both create hub scene/UI; duplication is wasteful.

### 2. Task 048 — In-Run Inventory
**Question:** Remove entirely or convert to read-only stats display?  
**Recommendation:** TBD — depends on whether players need to see equipment stats mid-run.

### 3. Task 046 — Shop Scope
**Question:** Should shop also handle equipment browsing, or just purchasing?  
**Recommendation:** Just purchasing — Task 056 handles browsing/equipping.

---

## Phase Summary

### **Phase 1: Design Cleanup (IMMEDIATE)**
- Task 053 (remove conflicts)

### **Phase 2: Foundation (HIGH PRIORITY)**
- Task 051 (hub scene)
- Task 041 (hotkeys)
- Task 050 (level-up choices)

### **Phase 3: Meta Progression (MEDIUM)**
- Task 054 (meta progression updates)
- Task 055 (skill selection hub)
- Task 056 (equipment hub)
- Task 044 (balance pass)

### **Phase 4: Hub Features (MEDIUM)**
- Task 045 (decide: merge or defer)
- Task 046 (modified: shop only)
- Task 047 (modified: hub-only)
- Task 049 (run history)

### **Phase 5: Campaign (MEDIUM)**
- Task 052 (4-act campaign)

---

## Next Steps

1. ✅ Update `TASKS.md` with new priority order
2. ✅ Mark tasks 042/043 as superseded
3. ✅ Mark tasks 045/046/047/048 with status warnings
4. **Start with Task 053** (remove mid-run config conflicts)
5. Decide on Task 045 merge vs defer
6. Decide on Task 048 removal vs read-only rewrite
