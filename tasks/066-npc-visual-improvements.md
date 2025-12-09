# Task: 066 - NPC visual improvements
- Status: backlog

## Summary
Replace placeholder colored square NPCs with proper sprites, animations, and visual polish. Add idle animations, interaction feedback, and name plates to make NPCs feel more alive and the hub more immersive.

## Goals
- Replace colored squares with NPC sprite assets
- Add idle animations for each NPC type
- Add name plates above NPCs (visible names)
- Add interaction animation/effect when player presses E
- Improve proximity prompt visuals (background, better positioning)
- Add SFX for interaction and proximity detection

## Non Goals
- NPC dialogue system or story text
- Multiple NPC states (walking, talking, etc.)
- Unique NPCs beyond the 5 hub NPCs
- NPC quests or missions

## Acceptance criteria
- [ ] Each NPC has unique sprite (or at minimum, distinct visual from colored square)
- [ ] NPCs play idle animation loop
- [ ] NPC name plates visible above sprites
- [ ] Proximity prompt has semi-transparent background box
- [ ] Interaction plays SFX and visual feedback (flash, particle, etc.)
- [ ] NPCs stand out visually from environment
- [ ] `dotnet build` passes; manual playtest confirms improved visuals

## Definition of done
- Builds pass (`dotnet build`)
- Tests/play check done
- Docs updated (if applicable)
- Handoff notes added

## Plan
- Step 1: Design or source NPC sprites (5 unique designs or variants)
- Step 2: Create sprite sheets with idle animations (4-8 frames each)
- Step 3: Add sprites to Content.mgcb, set up build pipeline
- Step 4: Update NpcRenderSystem to use SpriteSheet instead of colored squares
- Step 5: Add animation playback logic (frame timing, looping)
- Step 6: Create name plate rendering in NpcRenderSystem
- Step 7: Improve ProximityPromptRenderSystem with background box
- Step 8: Add NpcNameComponent with display names
- Step 9: Add interaction SFX to InteractionInputSystem
- Step 10: Optional: Add particle effect on interaction

## Notes / Risks / Blockers
- **Dependency**: Sprite assets need to be created or sourced (art task)
- **Design**: NPC visual themes should match hub biome aesthetic
- **Tech**: Leverage existing SpriteSheet/Animation systems if available
- **Risk**: Art pipeline may take time; consider placeholder "better squares" as interim
- **UX**: Name plates should be readable but not obtrusive
