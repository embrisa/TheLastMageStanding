# Task: 068 - Hub tutorial and first-time UX
- Status: backlog

## Summary
Create a tutorial experience for first-time players entering the hub. Guide them to interact with NPCs, understand the hub layout, and prepare for their first stage run. Can be as simple as on-screen prompts or a brief dialogue sequence.

## Goals
- Detect first-time player (new profile)
- Show tutorial prompts explaining hub navigation
- Guide player to interact with each NPC type
- Explain skill selection, talent tree, equipment basics
- Encourage player to start first stage run
- Make tutorial skippable for returning players

## Non Goals
- Full narrative tutorial or story cutscenes
- Combat tutorial (belongs in first stage, not hub)
- Forced hand-holding (let players explore freely)
- Complex tutorial quest system

## Acceptance criteria
- [ ] First-time players see "Welcome to the Hub" prompt on spawn
- [ ] Tutorial prompts appear near NPCs explaining their purpose
- [ ] Interacting with an NPC dismisses its tutorial prompt
- [ ] After visiting all NPCs, final prompt encourages stage selection
- [ ] Tutorial state persists (doesn't repeat on next hub visit)
- [ ] Returning players see no tutorial prompts
- [ ] Optional: Settings menu has "Reset Tutorial" option
- [ ] `dotnet build` passes; manual playtest confirms tutorial clarity

## Definition of done
- Builds pass (`dotnet build`)
- Tests/play check done
- Docs updated (if applicable)
- Handoff notes added

## Plan
- Step 1: Add TutorialState component/field to PlayerProfile (tracking completion)
- Step 2: Create TutorialUISystem for displaying prompts
- Step 3: Define tutorial steps (hub intro, each NPC, stage selection)
- Step 4: Render tutorial prompts at appropriate positions
- Step 5: Detect tutorial triggers (first spawn, NPC proximity, interactions)
- Step 6: Mark tutorial steps complete on interaction
- Step 7: Save tutorial state to profile
- Step 8: Add tutorial skip option (button or key)
- Step 9: Test with fresh profile and returning profile
- Step 10: Iterate based on clarity feedback

## Notes / Risks / Blockers
- **Dependency**: PlayerProfile persistence (already exists)
- **Design**: Keep tutorial brief and unobtrusive (3-5 prompts max)
- **UX**: Use clear, concise language (avoid jargon)
- **Tech**: Tutorial prompts should layer above proximity prompts
- **Risk**: Too much text can overwhelm - prefer "show don't tell"
- **Future**: Could expand to stage-specific tutorials later
