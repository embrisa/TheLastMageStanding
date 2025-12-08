# Task: 054 - Meta progression system updates
- Status: backlog

## Summary
Update the meta progression system to enforce the level 60 cap, define skill/talent unlock tables, separate meta level-ups from in-run level-ups, and integrate with hub-based configuration. This builds on Task 037's foundation to fully realize the hub-centric progression model.

## Goals
- Enforce meta level cap at 60 (hard cap, no further XP/levels)
- Define skill unlock table (which skills unlock at which meta levels)
- Define talent point grant table (which meta levels grant talent points)
- Create separate `MetaLevelUpEvent` distinct from in-run `PlayerLeveledUpEvent`
- Integrate meta unlocks with hub scene UIs (skill selection, talent tree)
- Update meta XP formula to emphasize stage completion over waves

## Non Goals
- Changing core meta XP calculation logic (Task 037 foundation is fine)
- Profile persistence changes (already handled in Task 037)
- Hub UI implementation (that's Task 051)
- Rebalancing all meta XP values (initial tuning is enough)

## Acceptance criteria
- [ ] Meta level stops at 60; XP accumulation stops or continues without leveling
- [ ] `MetaUnlockConfig` defines skill unlocks per meta level (e.g., Fireball at level 3)
- [ ] `MetaUnlockConfig` defines talent point grants per meta level (e.g., 1 point at levels 2, 4, 6...)
- [ ] `MetaLevelUpEvent` publishes separately from `PlayerLeveledUpEvent`
- [ ] Skill selection UI (Task 051) shows locked skills with unlock requirements
- [ ] Talent tree UI (Task 031) uses meta-granted points, not in-run points
- [ ] Meta XP formula updated to reward stage completion heavily
- [ ] Profile tracks unlocked skills based on meta level
- [ ] `dotnet build` passes; manual test confirms unlock gating works

## Definition of done
- Builds pass (`dotnet build`)
- Tests/play check done (verify unlock progression)
- Docs updated (GDD with unlock tables, meta level cap)
- Handoff notes added (if handing off)

## Plan
- Step 1: Define meta unlock configuration
  - Create `Core/Config/MetaUnlockConfig.cs`
  - Define default skill unlock table (9 skills across levels 1-30)
  - Define default talent point grant table (1 point every 2-3 levels, total ~20 points at level 60)
  - Load config at startup
  
- Step 2: Enforce level 60 cap
  - Update `MetaProgressionCalculator.CalculateLevelFromXp()` to cap at 60
  - Add check in meta XP application: if level 60, don't increment level
  - Optionally: Continue tracking XP past cap for future prestige system
  
- Step 3: Create `MetaLevelUpEvent`
  - Define new event in `Core/Events/MetaProgressionEvents.cs`
  - Publish when meta level increases (distinct from in-run level-up)
  - Include `OldLevel`, `NewLevel`, `UnlockedSkills`, `TalentPointsGranted` fields
  
- Step 4: Update meta level-up flow
  - Modify `MetaProgressionManager` (or equivalent) to detect level-ups
  - When meta level increases, check unlock tables
  - Publish `MetaLevelUpEvent` with unlock data
  - Update profile with newly unlocked skills
  
- Step 5: Integrate with perk point system
  - Update `PerkPointGrantSystem` (Task 053) to subscribe to `MetaLevelUpEvent`
  - Grant talent points based on `MetaUnlockConfig.TalentPointGrants`
  - Store talent points in profile (not per-run component)
  
- Step 6: Update meta XP formula
  - Modify formula to heavily reward stage completion
  - Formula: `base_xp = (stage_number^1.8) * 200 + (is_boss_stage ? 1000 : 0)`
  - Wave/kill bonuses become minor (10-20% of total)
  - Boss kills give massive bonus (1000+ base XP)
  
- Step 7: Add unlock validation
  - Create `MetaUnlockService` to check if content is unlocked
  - Methods: `IsSkillUnlocked(skillId)`, `GetUnlockedSkills()`, `GetNextUnlock()`
  - Skill selection UI queries this service to show/hide skills

## Meta Unlock Tables (Preliminary)

### Skill Unlocks
```
Level 1:  Firebolt (starting skill)
Level 3:  Fireball, Arcane Missile
Level 5:  Flame Wave, Frost Bolt
Level 8:  Frost Nova, Arcane Burst
Level 12: Arcane Barrage, Blizzard
(Plus any future skills at levels 15-30)
```

### Talent Point Grants
```
Levels 2, 4, 6, 8, 10:     1 point each (5 total by level 10)
Levels 12, 14, 16, 18, 20: 1 point each (10 total by level 20)
Levels 25, 30, 35, 40:     2 points each (18 total by level 40)
Levels 45, 50, 55, 60:     2 points each (26 total by level 60)
```
Total: ~26 talent points at level 60

### Meta Level Requirements
- **Act 1**: Level 1+ (tutorial, always unlocked)
- **Act 2**: Level 10+ (mid-game content)
- **Act 3**: Level 25+ (late-game content)
- **Act 4**: Level 40+ (endgame content)

## Notes / Risks / Blockers
- **Dependency**: Task 037 provides meta progression foundation
- **Dependency**: Task 051 for hub UI integration
- **Dependency**: Task 053 to separate in-run from meta progression
- **Risk**: Unlock pacing needs careful tuning (playtesting required)
- **Risk**: Players might feel progression is too slow; balance carefully
- **Design**: Should XP continue accumulating past level 60? (For future prestige/paragon?)
- **Balance**: Talent point count needs to match perk tree depth (Task 031)
- **UX**: Clear feedback when unlocking new skills/talents (notifications, hub UI highlights)

## Meta XP Formula Update

### Current (Task 037)
```
base_xp = wave_reached^1.5 * 100
kill_bonus = total_kills * 5
gold_bonus = gold_collected * 2
damage_bonus = damage_dealt / 1000
time_multiplier = max(0, 1 - (run_duration_minutes / 60))
meta_xp = (base_xp + kill_bonus + gold_bonus + damage_bonus) * (1 + time_multiplier * 0.5)
```

### Proposed (Stage-Focused)
```
# Base XP heavily rewards stage progression
stage_base_xp = (stage_number^1.8) * 200
boss_bonus = is_boss_stage ? 1000 : 0
act_multiplier = act_number * 1.5  # Later acts worth more

# Performance bonuses are minor
wave_bonus = waves_reached * 10
kill_bonus = total_kills * 2
efficiency_bonus = time_multiplier * 100

meta_xp = (stage_base_xp + boss_bonus) * act_multiplier + wave_bonus + kill_bonus + efficiency_bonus
```

Example:
- Act 1 Stage 1: ~200 XP
- Act 1 Stage 3 (boss): ~1400 XP
- Act 2 Stage 1: ~600 XP
- Act 4 Boss: ~4000+ XP

## Related Tasks
- Task 037: Meta progression foundations (foundation)
- Task 031: Talent/perk tree (talent points)
- Task 039: Skill system (skill unlocks)
- Task 051: Hub scene (unlock UI integration)
- Task 052: Stage/act system (stage completion events)
- Task 053: Remove mid-run config (event separation)
