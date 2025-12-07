# Task: 017 - Directional TakeDamage animation
- Status: completed

## Summary
The player’s TakeDamage animation currently ignores facing and always plays the same direction. We need the hit reaction to respect the movement/facing direction so damage feedback matches how the player is moving when they get hit.

## Goals
- Make the TakeDamage animation pick the correct directional row based on the player’s current movement/facing input.
- Keep the hit reaction consistent if input is released mid-animation (e.g., lock to last facing used for the hit).
- Ensure the state machine returns to the correct idle/move direction after the hit animation completes.

## Non Goals
- Adding new art or expanding animation sets.
- Reworking overall movement/facing logic beyond what is required for TakeDamage.
- Broad combat balance or damage timing changes.

## Acceptance criteria
- [x] Taking damage while moving in any supported direction plays the TakeDamage animation facing that direction (matching the movement/idle facing system).
- [x] Releasing or changing input during the hit keeps a stable, predictable facing for the remainder of that TakeDamage animation.
- [x] After TakeDamage finishes, the player returns to the appropriate idle/move animation facing the latest valid direction; no forced front-facing lock.
- [x] `dotnet build` passes.

## Definition of done
- Builds pass (`dotnet build`)
- Tests/play check done (if applicable)
- Docs updated (if applicable)
- Handoff notes added (if handing off)

## Plan
- Step 1: Reproduce current behavior across movement directions; identify how facing is derived for damage state.
- Step 2: Bind TakeDamage row/clip selection to the player’s facing/movement input at damage time; decide lock vs. live update during the animation.
- Step 3: Verify transitions back to idle/move for the same direction; test rapid hits and direction changes.

## Notes / Risks / Blockers
- Implemented explicit facing update in `HitReactionSystem` using `InputIntent` (or `Velocity` fallback) to ensure the hit animation starts with the correct facing.
- `PlayerRenderSystem` correctly locks the facing during the hit animation and resumes updating it afterwards.

