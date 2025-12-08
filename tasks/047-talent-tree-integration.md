# Task: 047 - Talent Tree Integration & Application
- Status: NEEDS UPDATE + BLOCKED by Task 051

**NOTE:** This task assumes in-run talent access (P key in Goals section). Under new design, talents are hub-only configuration. Remove all mentions of in-run access; focus purely on hub integration after Task 051 is complete.

## Summary
Integrate the talent-perk tree (Task 031) with the meta progression system. Apply talent unlock conditions (meta level, gold cost), allow players to unlock talents in the meta hub, and apply unlocked talent effects at run start.

## Goals
- Add unlock conditions to talent tree nodes (meta level requirement, gold cost, prerequisites).
- Track unlocked talents in `PlayerProfile`.
- Create UI in meta hub for viewing and unlocking talents.
- Implement `MetaProgressionApplicator` to apply unlocked talent effects at run start.
- Ensure talents stack correctly with equipment and in-game stats.
- Provide talent reset option (refund gold/levels with confirmation).

## Non Goals
- Redesigning the talent tree structure (use Task 031 as-is).
- In-run talent selection or dynamic unlocking.
- Complex talent synergies or combos (keep simple stat bonuses for MVP).
- Balance tuning (keep conservative values).

## Acceptance criteria
- [ ] Talent tree nodes have unlock conditions: meta level, gold cost (optional), prerequisites.
- [ ] `PlayerProfile` tracks unlocked talent node IDs.
- [ ] Talent tree UI shows locked/unlocked state and unlock requirements.
- [ ] Can spend gold or use meta level to unlock talents.
- [ ] `MetaProgressionApplicator` applies unlocked talents to player stats at run start.
- [ ] Talents stack correctly with equipment and don't double-apply.
- [ ] Talent reset button refunds gold and levels (with confirmation dialog).
- [ ] Tests cover talent unlock logic, application, and stat stacking.
- [ ] `dotnet build` passes.

## Definition of done
- Builds pass (`dotnet build`)
- Tests pass (`dotnet test`)
- Play check done (can unlock talents, verify stats applied in run)
- Docs updated (talent unlock system, application logic)
- Handoff notes added (if handing off)

## Plan

### Step 1: Talent Unlock Data Model (0.5 day)
- Extend talent tree data (from Task 031) with unlock conditions:
  - Each node has:
    - `MinMetaLevel` (int) — required meta level
    - `GoldCost` (int, optional) — gold cost to unlock
    - `PrerequisiteNodeIds` (list) — parent nodes that must be unlocked first
    - `IsUnlocked` (bool) — runtime state (not saved in tree, saved in profile)
- Update `PlayerProfile` with `List<string> UnlockedTalentNodeIds`
- Load talent tree data (JSON or hardcoded) with unlock conditions

### Step 2: Talent Unlock Service (1 day)
- Create `Core/MetaProgression/TalentUnlockService.cs`:
  - `CanUnlockTalent(string nodeId, PlayerProfile profile)` → check if level/gold sufficient and prerequisites met
  - `UnlockTalent(string nodeId, PlayerProfile profile)` → add to unlocked list, deduct gold (if applicable), save profile
  - `ResetTalents(PlayerProfile profile)` → refund gold spent on talents, clear unlocked list, save profile
  - `GetUnlockedTalents(PlayerProfile profile)` → return list of unlocked talent nodes
- Add validation:
  - Prevent unlocking if requirements not met
  - Prevent duplicate unlocks
  - Ensure prerequisites are satisfied (recursive check)
- Add unit tests for unlock logic

### Step 3: Talent Tree UI in Meta Hub (1.5 days)
- Create `UI/MetaHub/TalentTreeUI.cs` (or extend existing from Task 031):
  - Display talent tree nodes (visual graph or list)
  - Show locked vs. unlocked state:
    - Locked: grayed out, show unlock requirements (e.g., "Meta Lv 5, 200 Gold")
    - Unlocked: highlighted, show active effect
  - Click locked node:
    - Show unlock confirmation dialog with cost
    - Unlock button → call `TalentUnlockService.UnlockTalent()`
    - Update UI to show unlocked state
    - Show success feedback (sound, visual)
  - Click unlocked node:
    - Show talent details and active effects
  - Add "Reset Talents" button:
    - Confirmation dialog: "Are you sure? This will refund all gold and reset talents."
    - Call `TalentUnlockService.ResetTalents()`
    - Update UI to show all nodes locked
- Show total stat bonuses from unlocked talents (summary panel)
- Navigate to talent tree from meta hub "Talents" button
- Add "Back to Hub" button

### Step 4: Meta Progression Applicator (1 day)
- Create `Core/MetaProgression/MetaProgressionApplicator.cs`:
  - `ApplyMetaProgressionToPlayer(PlayerProfile profile, PlayerEntity player)`:
    - Load unlocked talents via `TalentUnlockService.GetUnlockedTalents()`
    - Apply each talent's stat bonuses to player (via Task 029 stat model)
    - Apply equipped equipment stats
    - Log applied bonuses for debugging
  - Ensure bonuses are applied as modifiers (not base values) to support stacking
  - Prevent double-application (clear modifiers before re-applying)
- Hook into run start:
  - When gameplay begins, call `MetaProgressionApplicator.ApplyMetaProgressionToPlayer()`
  - Player should start run with all meta bonuses active
- Add tests for applicator:
  - Apply multiple talents, verify stats sum correctly
  - Apply talents + equipment, verify stacking
  - Re-apply (e.g., scene reload), verify no double-application

### Step 5: Integration with Stat Model (0.5 day)
- Ensure talent effects integrate with Task 029 stat model:
  - Talents provide stat modifiers (e.g., +10% max HP, +5 damage)
  - Use `StatModifier` system (additive/multiplicative)
  - Tag modifiers as "Meta" source for debugging
- Test interactions:
  - Meta talents + equipment + in-game level-ups should all stack
  - Verify stat recalculation works correctly

### Step 6: Testing & Documentation (0.5 day)
- Test unlock flow:
  - Unlock talent with sufficient level/gold
  - Attempt unlock with insufficient level/gold (should fail)
  - Verify prerequisites enforced
- Test reset flow:
  - Unlock several talents
  - Reset → verify gold refunded, talents cleared
  - Verify profile persistence
- Test application:
  - Unlock talents → start run → verify stats applied
  - Check stat display in-game (player should have boosted stats)
- Document:
  - Talent unlock system design
  - Unlock conditions format
  - Application logic and stacking rules
- Update `game-design-document.md` with talent unlock section
- Run `dotnet build` and `dotnet test`, fix errors

## Estimated Timeline
- **Total: 2-3 days**

## Dependencies
- Task 037: Meta progression foundations (profile service, data models)
- Task 031: Talent-perk tree (tree structure, talent data)
- Task 029: Unified stat model (stat modifiers, application)
- Task 045: Meta hub UI (navigation integration)

## Notes / Risks / Blockers
- Talent tree data from Task 031 may need extension for unlock conditions. Coordinate with existing structure.
- Ensure talents don't provide runaway power. Start with small bonuses (e.g., +5% HP, +2% damage) and iterate.
- Talent reset should be generous (full refund) to encourage experimentation in MVP. Can add costs later.
- Visual representation of talent tree: can be simple node graph or vertical list for MVP. Polish later.
