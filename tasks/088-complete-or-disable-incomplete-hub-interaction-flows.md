# Task: 088 - Complete or disable incomplete hub interaction flows
- Status: backlog

## Summary
The hub currently exposes interactions and menu actions that are still TODOs, including shop/settings/quit-related paths. Dead interaction points weaken the hub experience and make the game feel less trustworthy during active development.

## Goals
- Remove no-op hub interactions and menu actions from player-facing flows
- Either implement the intended behavior or gate the feature clearly until ready
- Keep hub UX internally consistent across keyboard/UI/NPC interactions
- Document any intentional temporary locks in code and design docs

## Non Goals
- Broad redesign of hub layout or NPC placement
- Full implementation of unrelated hub content polish
- Adding placeholder fallbacks that silently fail

## Acceptance criteria
- [ ] No hub interaction or menu entry exposed to players results in a no-op TODO path
- [ ] Shop/settings/quit-related flows are either implemented end-to-end or explicitly disabled with clear player feedback
- [ ] Keyboard, NPC proximity, and menu-driven access paths remain behaviorally consistent
- [ ] GDD/task notes are updated if the hub flow changes materially

## Definition of done
- Builds pass (`dotnet build`)
- Tests/play check done (if applicable)
- Docs updated (if applicable)
- Handoff notes added (if handing off)

## Plan
- Step 1: Audit current incomplete hub interactions and menu actions
- Step 2: Decide per flow whether to implement now or hide/lock it until later
- Step 3: Wire the final behavior consistently across ECS/UI entry points and update docs

## Notes / Risks / Blockers
- Some paths overlap with existing hub backlog tasks; align implementation order to avoid duplicate UI work
- If a feature is intentionally hidden, ensure the player cannot still reach it through alternate inputs
