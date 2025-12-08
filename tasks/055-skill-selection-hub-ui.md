# Task: 055 - Skill selection hub UI
- Status: backlog

## Summary
Create a skill selection UI in the hub scene where players can browse unlocked skills, view skill details, and equip skills to their hotbar (primary + slots 1-4). Skills are locked by meta level and can only be changed in the hub, not during runs.

## Goals
- Provide hub-only UI for browsing and equipping skills
- Show locked/unlocked skills with unlock requirements (meta level)
- Display skill details (damage, cooldown, element, targeting, description)
- Allow equipping skills to 5 hotbar slots (primary + 1-4)
- Validate skill selection (can't equip same skill twice)
- Persist equipped skills to profile for use in runs
- Provide visual feedback for equipped skills and unlocks

## Non Goals
- Skill unlocking logic (that's Task 054)
- In-run skill usage (already implemented in Task 039)
- Skill balance or new skill creation
- Advanced UI polish or animations (functional first)
- Skill modifiers or customization (that's in-run level-up choices)

## Acceptance criteria
- [ ] Skill selection UI accessible from hub main menu
- [ ] Shows all 9 mage skills (Fire/Arcane/Frost) with locked/unlocked states
- [ ] Clicking a skill shows detailed stats (damage mult, cooldown, range, description)
- [ ] Can drag-and-drop or click-to-equip skills to 5 hotbar slots
- [ ] Locked skills show unlock requirement (e.g., "Unlock at Meta Level 8")
- [ ] Equipped skills persist to profile and load in runs
- [ ] Visual distinction for equipped skills (highlight, icon, border)
- [ ] Can unequip skills to empty slots
- [ ] Keyboard navigation works (arrow keys, Enter to equip, Escape to exit)
- [ ] `dotnet build` passes; manual test confirms equipping works

## Definition of done
- Builds pass (`dotnet build`)
- Tests/play check done (verify skill equipping)
- Docs updated (UI flow, skill unlock requirements)
- Handoff notes added (if handing off)

## Plan
- Step 1: Create skill selection UI system
  - Create `SkillSelectionUISystem` in `Core/Ecs/Systems/`
  - Render in hub scene only (check scene state)
  - Show grid or list of all skills
  
- Step 2: Display skill data
  - Query `SkillRegistry` for all skill definitions
  - Render skill cards with icon (placeholder), name, element
  - Show locked/unlocked state based on `MetaUnlockService.IsSkillUnlocked()`
  - Display unlock requirement text for locked skills
  
- Step 3: Implement skill details panel
  - When skill selected, show detail view
  - Display: damage multiplier, cooldown, cast time, range, AoE radius (if applicable)
  - Show description text explaining skill behavior
  - Show element and targeting mode
  
- Step 4: Implement hotbar slot UI
  - Render 5 hotbar slots at bottom or side of UI
  - Slot 0: "Primary" (default attack)
  - Slots 1-4: "Hotkey 1-4" (number keys)
  - Show currently equipped skills in each slot
  
- Step 5: Implement skill equipping
  - Click or press Enter on skill to enter "equip mode"
  - Highlight hotbar slots; click slot to equip skill
  - Validate: Can't equip locked skills, can't equip same skill twice
  - Update `EquippedSkills` component in profile
  
- Step 6: Implement skill unequipping
  - Right-click or press Delete on hotbar slot to clear
  - Show confirmation if removing primary skill (slot 0 should always have something)
  
- Step 7: Add keyboard navigation
  - Arrow keys: Navigate skill grid
  - Enter: Select skill for equipping
  - Number keys 0-4: Directly assign selected skill to that slot
  - Escape: Exit UI or cancel equip mode
  
- Step 8: Persist to profile
  - Save equipped skills to `PlayerProfile.EquippedSkills`
  - Load equipped skills from profile on hub entry
  - Validate equipped skills are still unlocked (handle meta level changes)

## UI Layout (Preliminary)

```
┌─────────────────────────────────────────────┐
│  SKILL SELECTION                            │
├─────────────────────────────────────────────┤
│  [Filter: All | Fire | Arcane | Frost]     │
├─────────────────────────────────────────────┤
│  ┌────────┐ ┌────────┐ ┌────────┐          │
│  │Firebolt│ │Fireball│ │Flame   │  (Fire) │
│  │ [icon] │ │ [icon] │ │ Wave   │          │
│  │EQUIPPED│ │UNLOCKED│ │UNLOCKED│          │
│  └────────┘ └────────┘ └────────┘          │
│  ┌────────┐ ┌────────┐ ┌────────┐          │
│  │Arcane  │ │Arcane  │ │Arcane  │ (Arcane)│
│  │Missile │ │ Burst  │ Barrage │          │
│  │LOCKED  │ │UNLOCKED│ │LOCKED  │          │
│  │Lvl 3   │ │        │ │Lvl 12  │          │
│  └────────┘ └────────┘ └────────┘          │
│  ┌────────┐ ┌────────┐ ┌────────┐          │
│  │Frost   │ │Frost   │ │Blizzard│  (Frost)│
│  │ Bolt   │ │ Nova   │ │ [icon] │          │
│  │UNLOCKED│ │LOCKED  │ │LOCKED  │          │
│  │        │ │Lvl 8   │ │Lvl 12  │          │
│  └────────┘ └────────┘ └────────┘          │
├─────────────────────────────────────────────┤
│  SKILL DETAILS: Firebolt                   │
│  Element: Fire | Targeting: Direction       │
│  Damage: 1.0x | Cooldown: 0.5s              │
│  "A fast fire projectile. Your basic        │
│   attack and most reliable spell."          │
├─────────────────────────────────────────────┤
│  HOTBAR                                     │
│  [0: Firebolt] [1: Empty] [2: Empty]        │
│  [3: Empty] [4: Empty]                      │
│  Press number key or click to assign        │
└─────────────────────────────────────────────┘
```

## Notes / Risks / Blockers
- **Dependency**: Task 039 provides skill definitions and registry
- **Dependency**: Task 054 provides unlock service and meta level gating
- **Dependency**: Task 051 provides hub scene context
- **Risk**: UI complexity could balloon; keep it simple and functional
- **UX**: Drag-and-drop vs. click-to-equip? Start with click for simplicity
- **UX**: Need clear feedback when trying to equip locked skills
- **Balance**: Starting skill (Firebolt) should be compelling but not mandatory
- **Tech**: Profile schema needs `EquippedSkills` field (may need migration)

## Related Tasks
- Task 039: Skill system (skill definitions and registry)
- Task 051: Hub scene (provides scene context)
- Task 054: Meta progression updates (unlock service)
- Task 042: Skill selection UI (original planned task, now superseded by this)
- Task 040: Skill hotbar UI (in-run display, already implemented)
