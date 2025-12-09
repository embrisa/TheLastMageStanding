# Task: 062 - Skill selection UI in hub
- Status: backlog

## Summary
Implement the skill selection UI accessible via `npc_ability_loadout` NPC (blue square) in the hub. Players should be able to view all unlocked skills, equip up to 4 skills to hotbar slots (1-4 keys), and see skill descriptions/requirements. This UI is hub-only and changes are locked during stage runs.

## Goals
- Create SkillSelectionUISystem for browsing and equipping skills
- Display all skills unlocked by meta level
- Allow equipping skills to hotbar slots (1, 2, 3, 4)
- Show skill details (name, description, cooldown, damage, requirements)
- Persist equipped skills to PlayerProfile
- Integrate with existing InteractionInputSystem (E key on `npc_ability_loadout`)

## Non Goals
- Skill unlocking logic (already exists in meta progression)
- Skill balance or new skill creation
- In-run skill modification (already locked via scene gating)
- Skill upgrade/modification UI (future task)

## Acceptance criteria
- [ ] Pressing E near blue NPC (`npc_ability_loadout`) opens skill selection UI
- [ ] UI shows grid/list of all unlocked skills with icons and names
- [ ] Clicking a skill shows detailed info panel (description, stats, requirements)
- [ ] Can assign skills to slots 1-4 via drag-drop or click-to-assign
- [ ] Current hotbar shows equipped skills with visual indicators
- [ ] ESC or back button closes UI and returns to hub
- [ ] P key toggle still works (opens perk tree, not skill selection)
- [ ] Equipped skills save to profile and persist across sessions
- [ ] Skills show as locked/grayed if meta level requirement not met
- [ ] `dotnet build` passes; manual playtest confirms functionality

## Definition of done
- Builds pass (`dotnet build`)
- Tests/play check done
- Docs updated (if applicable)
- Handoff notes added

## Plan
- Step 1: Create SkillSelectionUIState component (IsOpen, SelectedSlot, HoveredSkillId, etc.)
- Step 2: Create SkillSelectionUISystem implementing IUpdateSystem, IUiDrawSystem, ILoadContentSystem
- Step 3: Add UI rendering: skill grid, hotbar slots, detail panel
- Step 4: Implement input handling: navigation, skill selection, slot assignment
- Step 5: Wire InteractionInputSystem to toggle SkillSelectionUIState on `npc_ability_loadout`
- Step 6: Load equipped skills from PlayerProfile on hub scene entry
- Step 7: Save equipped skills to PlayerProfile when changed
- Step 8: Add skill filtering/sorting (by type, unlock level, etc.)
- Step 9: Register system in EcsWorldRunner hub-only systems
- Step 10: Test equipping skills and verify they work in stage runs

## Notes / Risks / Blockers
- **Dependency**: SkillRegistry already exists with skill definitions
- **Dependency**: PlayerProfile needs EquippedSkills field (add if missing)
- **UX**: Should dragging be required or is click-to-assign sufficient? (Recommend click for simplicity)
- **Design**: How many skills are unlockable total? (Affects UI pagination)
- **Risk**: UI complexity - keep MVP simple, iterate later
