# Task: 012 - Player hit animation not playing
- Status: completed

## Summary
Player damage feedback still fails: the TakeDamage animation does not play and the player sprite can freeze on hit. Need to investigate animation state handling, asset loading, and hit-state transitions so hits visibly play the damage clip and recover smoothly.

## Goals
- Ensure the player plays the TakeDamage animation when hit and returns to movement/idle afterward.
- Keep hit tint/flash temporary with no long-lived color lock.
- Avoid freezes or unintended movement/attack lock while the hit clip runs.
- Verify behavior under repeated hits and while moving.

## Non Goals
- Reworking all player animation sets or adding new art.
- Broad combat balance changes beyond animation timing fixes.
- Audio/VFX passes beyond what is needed to confirm hit readability.

## Acceptance criteria
- [x] TakeDamage animation reliably triggers on player damage events and completes.
- [x] Player resumes appropriate movement/idle clip after the hit finishes without freezing.
- [x] Flash/tint effect clears after the hit window; no persistent color tint.
- [x] Repeated hits in quick succession queue/reset cleanly without desync or stuck frames.
- [x] `dotnet build` passes.

## Definition of done
- Builds pass (`dotnet build`)
- Tests/play check done (if applicable)
- Docs updated (if applicable)
- Handoff notes added (if handing off)

## Plan
- Reproduce: hit the player and capture when/if the TakeDamage clip triggers or freezes.
- Inspect animation assets (frame count/fps) and sprite set wiring for TakeDamage.
- Trace state transitions (hit state, timers, facing, movement) and ensure the hit clip overrides/relinquishes correctly.
- Adjust update order/timers to prevent freezes and guarantee playback; retest under rapid hits and movement.
- Verify visuals: tint clears, clip returns to movement/idle; run `dotnet build`.

## Notes / Risks / Blockers
- Hit state interacts with slow/knockback; changes must avoid reintroducing movement drift.
- Asset frame counts and fps must match expected playback speed; incorrect rows/columns could stall frames.
- Need to ensure damage events and hit state timing align with the fixed timestep update loop.

## Current Findings (2025-12-07)
- TakeDamage sheet: 1920x1024 (15 columns, 8 rows of 128px); all rows/columns have alpha content.
- Tint flash triggers and the hit clip switches to frame 0, but the animation freezes on that first frame and never advances, even under repeated hits or movement.
- Code changes attempted:
  - Extended hit duration to ~1.6s, added total/remaining tracking, reset hit state on each damage, and drove hit frame by progress.
  - Switched to per-frame progress indexing and ensured hit state is seeded via hit flash fallback.
  - Tried timer-based advancement and progress-based advancement; both still stuck visually on frame 0 in-game.
- Build remains clean (`dotnet build`), so the issue appears to be runtime/state/render interaction rather than compilation.
- Suspect remaining causes: row selection/mapping for hit clip, state not exiting hit due to lingering component, or render state not refreshed on row/column change. Needs deeper runtime tracing.
- Latest update (FIXED): The issue was identified in `HitReactionSystem.cs`. The system was unconditionally resetting the `PlayerAnimationState` (frame index and timer) on every damage event. This caused the animation to freeze on frame 0 if damage events occurred frequently or if the state was being reapplied. The fix involves checking if the player is already in the `Hit` animation state before resetting the frame index. This allows the animation to continue playing even if the hit duration is extended by subsequent hits.

