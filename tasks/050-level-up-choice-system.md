# Task: 050 - Level-up choice system
- Status: backlog

## Summary
Replace the current automatic stat bonus system on level-up with a choice-based system where players select between a stat boost OR a skill modifier. This is a core feature of the new game vision where in-run progression is choice-driven rather than fixed.

## Goals
- Replace automatic stat grants with a pause-and-choose UI on level-up
- Present two options: stat boost (e.g., +HP, +Damage, +Speed) or skill modifier (e.g., +15% damage, -10% cooldown)
- Ensure choices are temporary (reset on stage restart) but persist through the current run
- Integrate with existing XP/level system and skill modifier system
- Provide clear UI feedback showing what each choice does

## Non Goals
- Meta progression changes (that's Task 037 updates)
- Skill unlocking (handled in hub, Task 051)
- Visual polish or animations (functional UI first)
- Respec or undo of choices mid-run

## Acceptance criteria
- [ ] On level-up, game pauses and shows choice UI with 2 cards (stat boost vs. skill modifier)
- [ ] Player can navigate between choices with arrow keys/WASD and confirm with Enter
- [ ] Stat boost option offers multiple stat types (HP, Damage, Speed, Armor, Power, Crit)
- [ ] Skill modifier option shows relevant modifiers for equipped skills only
- [ ] Chosen effect applies immediately and is visible in stats/HUD
- [ ] Choices reset on stage restart (new run)
- [ ] Choice history can be viewed during run (optional but nice to have)
- [ ] `dotnet build` passes with no errors
- [ ] Manual playtest verifies choice flow feels good

## Definition of done
- Builds pass (`dotnet build`)
- Tests/play check done (if applicable)
- Docs updated (GDD, task notes)
- Handoff notes added (if handing off)

## Plan
- Step 1: Create level-up choice data model and config
  - Define `StatBoostChoice` and `SkillModifierChoice` structs
  - Create `LevelUpChoiceConfig` with pools of available choices
  - Add `LevelUpChoiceState` component to track pending choices
  
- Step 2: Refactor `LevelUpSystem` to use choice flow
  - On `PlayerLeveledUpEvent`, create choice state instead of applying bonuses
  - Pause game (similar to pause system)
  - Wait for player selection
  - Apply chosen effect
  
- Step 3: Create `LevelUpChoiceUISystem`
  - Render 2 cards side-by-side with descriptions
  - Handle navigation (left/right arrows, A/D keys)
  - Handle confirmation (Enter/Space)
  - Show preview of stat changes
  
- Step 4: Integrate choice application
  - Stat boost: Apply to `StatModifiers` or direct stat components
  - Skill modifier: Add to `PlayerSkillModifiers` component
  - Track applied choices for potential UI display
  
- Step 5: Test and tune
  - Verify choices persist through run
  - Verify choices reset on restart
  - Balance choice values for fair trade-offs
  - Test with multiple level-ups in quick succession

## Notes / Risks / Blockers
- **Dependency**: Task 022 must be understood (current XP/level system)
- **Dependency**: Task 039 skill system must support modifier stacking
- **Risk**: Choice generation needs to be smart (don't offer irrelevant skill modifiers)
- **Risk**: UI pause timing could conflict with other pause sources (game over, actual pause menu)
- **Balance**: Stat boost vs. skill modifier needs careful tuning to avoid dominant strategy
- **UX**: Need clear visual feedback on what each choice does (tooltips, stat deltas)
- **Tech**: May need to extend `StatModifiers` to track source of each modifier for future UI

## Related Tasks
- Task 022: XP orbs and level-ups (foundation)
- Task 029: Unified stat and damage model (stat application)
- Task 039: Skill system (modifier integration)
- Task 051: Hub scene and scene management (separation of concerns)
