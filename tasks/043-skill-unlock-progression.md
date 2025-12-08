# Task 043 — Skill Unlock Progression

**Status:** backlog  
**Priority:** Medium  
**Estimated effort:** 2-3 hours  
**Dependencies:** Task 042 (Skill selection UI), Task 022 (XP/leveling)

## Summary

Gate skill availability behind player level progression. Players start with 1-2 basic skills and unlock more as they level up, creating a sense of growth and discovery.

## Rationale

Giving players all 9 skills immediately is overwhelming and removes progression hooks. Gradual unlocks teach mechanics incrementally, reward leveling, and create "power spike" moments when new skills become available.

## Goals

- Define unlock levels for all 9 mage skills
- Track unlocked skills per player (component + persistence)
- Gray out locked skills in skill selection UI with "Unlocks at level X" text
- Show unlock notification when player levels up and gains new skill
- Load/save unlocked skills across sessions
- Default unlock progression balances power curve with variety

## Non-Goals

- Alternative unlock conditions (achievements, quests, crafting)
- Skill upgrade/evolution system (future)
- Unlock via currency/gold (future meta progression)
- Class/element-specific unlock trees (future)
- Skill prerequisites (future)

## Acceptance Criteria

1. Player starts run with only Firebolt and 1 element-specific skill unlocked
2. Skills unlock automatically at specific levels (defined in design)
3. Skill selection UI grays out locked skills with level requirement label
4. Level-up notification shows "New skill unlocked: [Skill Name]!" when applicable
5. Unlocked skills persist across game restarts (but reset on new run)
6. First-time unlocks trigger celebratory VFX/SFX
7. Unlock progression documented in game design doc

## Plan

### 1. Define unlock progression
**Proposed unlock levels:**

| Level | Skill(s) Unlocked | Rationale |
|-------|-------------------|-----------|
| 1 (Start) | Firebolt | Tutorial skill, always available |
| 1 (Start) | Frost Bolt | Second starter option for variety |
| 3 | Arcane Missile | First alternative element unlock |
| 5 | Fireball | First AoE skill, power spike |
| 7 | Arcane Burst | Defensive option mid-game |
| 9 | Frost Nova | Crowd control utility |
| 12 | Flame Wave | Strong melee-range AoE |
| 15 | Arcane Barrage | Multi-target DPS |
| 18 | Blizzard | Ultimate-tier ground AoE |

**Design philosophy:**
- Start with 2 skills (Fire + Frost) for immediate choice
- Unlock 1 skill every 2-3 levels
- AoE skills unlock after basic projectiles
- Strongest skills (Barrage, Blizzard) unlock late-game
- Arcane unlocks spread across levels (not front-loaded)

### 2. Create UnlockedSkills component
**File:** `src/Game/Core/Skills/SkillComponents.cs` (add to existing)

```csharp
public class UnlockedSkills
{
    public HashSet<SkillId> UnlockedSkillIds { get; set; } = new();
    
    public UnlockedSkills()
    {
        // Default starter skills
        UnlockedSkillIds.Add(SkillId.Firebolt);
        UnlockedSkillIds.Add(SkillId.FrostBolt);
    }
    
    public bool IsUnlocked(SkillId skillId) => UnlockedSkillIds.Contains(skillId);
    public void Unlock(SkillId skillId) => UnlockedSkillIds.Add(skillId);
}
```

### 3. Add unlock levels to SkillDefinition
**File:** `src/Game/Core/Skills/SkillData.cs`

```csharp
public class SkillDefinition
{
    // ... existing fields ...
    
    public int UnlockLevel { get; init; }
}
```

Update `SkillRegistry.RegisterDefaultMageSkills()` to set unlock levels per table above.

### 4. Create SkillUnlockSystem
**File:** `src/Game/Core/Skills/SkillUnlockSystem.cs`

```csharp
public class SkillUnlockSystem : IUpdateSystem
{
    public void Update(UpdateContext context)
    {
        // Subscribe to PlayerLeveledUpEvent
        // Check all skills for unlockLevel == newLevel
        // Add to UnlockedSkills component
        // Publish SkillUnlockedEvent (for notification/VFX)
    }
}
```

Event: `SkillUnlockedEvent { SkillId SkillId, int Level }`

### 5. Update skill selection UI
**File:** `src/Game/Core/UI/SkillSelectionRenderSystem.cs`

Modifications:
- Query `UnlockedSkills` component from player
- For each skill in grid:
  - If unlocked: render normally (clickable)
  - If locked: render grayed out, show lock icon, display "Unlocks at level X"
- Locked skills not clickable
- Tooltip shows unlock level prominently for locked skills

Visual states:
- **Locked:** 40% opacity, grayscale, lock icon overlay, red "Level X" badge
- **Newly unlocked:** Pulsing gold glow for 5 seconds after unlock
- **Unlocked:** Normal appearance

### 6. Add unlock notification
**File:** `src/Game/Core/UI/HudRenderSystem.cs` (extend)

On `SkillUnlockedEvent`:
- Show toast notification: "New Skill Unlocked: [Skill Name]!"
- Display skill icon with shiny/sparkle effect
- Play unlock SFX (`SkillUnlock.wav`)
- Auto-dismiss after 4 seconds or manual dismiss

### 7. Persistence integration
**File:** `src/Game/Core/Skills/SkillProgressionPersistence.cs`

```csharp
public class SkillProgressionPersistence
{
    private const string SavePath = "Data/UnlockedSkills.json";
    
    public void Save(UnlockedSkills unlocks) { /* ... */ }
    public UnlockedSkills? Load() { /* ... */ }
}
```

**Important:** Unlocked skills persist across sessions but **reset on run restart**. This is session progression, not meta progression. Future Task 037 handles permanent unlocks.

Load/save triggers:
- Load on player spawn
- Save on skill unlock
- Save on game exit
- **Clear on session restart** (new run = fresh unlocks)

### 8. Testing checklist
- Start new run: only Firebolt and Frost Bolt unlocked
- Use debug command to set player to level 5: verify Fireball unlocks
- Level up normally from 4→5: verify unlock notification appears
- Locked skills in UI show correct level requirements
- Click locked skill: no effect (not equippable)
- Restart game: verify unlocks persist
- Restart run: verify unlocks reset to starters

## Technical Notes

### Unlock Level Balance
Current XP formula: `XP = 10 × (1.5 ^ (level - 1))`

| Level | XP Required | Cumulative XP |
|-------|-------------|---------------|
| 1→2   | 10          | 10            |
| 1→3   | 25          | 25            |
| 1→5   | 51          | 51            |
| 1→9   | 256         | 256           |
| 1→18  | 13,318      | 13,318        |

With 1 XP per enemy:
- Level 5 (Fireball) = ~50 kills
- Level 9 (Frost Nova) = ~250 kills
- Level 18 (Blizzard) = ~13,000 kills (end-game grind)

**Balance notes:** Level 18 may be too high for normal runs. Consider adjusting unlock level for Blizzard to 15 if playtesting shows it's unreachable.

### First-Time vs Repeat Unlocks
Current design: Unlocks persist **per session** but reset on new run.

Future meta progression (Task 037) could add:
- **Permanent unlocks:** Skills unlocked once stay unlocked across all runs
- **Unlock currency:** Spend essence/tokens to permanently unlock skills
- **Achievement unlocks:** "Kill 1000 enemies with Fire skills → unlock Meteor"

### Unlock Order Philosophy
- **Horizontal progression:** More options, not just more power
- **Element diversity:** Don't unlock all Fire skills first; interleave elements
- **Power curve:** Basic → AoE → Ultimate
- **Skill floor:** Level 1 should feel capable with 2 skills; not handicapped

## Definition of Done

- [ ] Unlock levels defined for all 9 mage skills
- [ ] `UnlockedSkills` component tracks unlocked skills
- [ ] `SkillUnlockSystem` auto-unlocks skills on level-up
- [ ] Skill selection UI grays out locked skills with level requirement
- [ ] Unlock notification appears on level-up with new skill
- [ ] Unlocked skills persist across sessions
- [ ] Unlocked skills reset on new run
- [ ] `dotnet build` succeeds
- [ ] Unit test: verify unlock levels match registry definitions
- [ ] Manual test: reach level 5, verify Fireball unlocks
- [ ] Manual test: restart session, verify unlocks persist
- [ ] Manual test: restart run, verify unlocks reset to starters
- [ ] Update `docs/game-design-document.md` with unlock progression table

## Risks & Unknowns

- **Balance:** Unlock levels are estimates; playtesting will reveal if pacing is too fast/slow
- **Session vs meta persistence:** Clear distinction needed to avoid confusion
- **Late-game content:** Level 18 unlock may be unreachable in normal runs
- **Unlock notification spam:** Multiple unlocks at once (e.g., debug level set) needs queueing

## Future Enhancements

- Alternative unlock paths (e.g., kill boss → unlock boss-themed skill)
- Skill evolution (Firebolt → Greater Firebolt at level 10)
- Unlock via currency (spend gold/essence to unlock early)
- Meta progression permanent unlocks (Task 037)
- Unlock achievements (unlock all Fire skills → bonus)
- Skill mastery system (use skill X times → upgrade)
- Class-specific unlock trees (Pyromancer vs Cryomancer paths)

## Notes

This task bridges progression (XP/leveling) with skill system (unlocks). Focus on clear communication of unlock requirements and satisfying "new toy" moments when skills unlock.

**Playtesting priority:** Verify unlock pacing feels good. Too slow = frustration, too fast = no sense of progression.

**Design reference:** Vampire Survivors unlocks every 5-10 levels; Hades unlocks via currency/story. Hybrid approach: auto-unlock via level + optional early unlock via meta currency (future).
