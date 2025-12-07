# Task: 019 - Session stats and run summary
- Status: done

## Summary
With wave/game-state events in place and a game over overlay planned, we still lack run-level feedback. Players cannot see how long they survived, which wave they reached, or how many enemies they defeated. We need to track session stats and surface them both during play and on game over, driven by proper death events.

## Goals
- Emit a death event for enemies (and optionally generic entities) when health reaches zero so stats can subscribe cleanly.
- Track session metrics: elapsed run time, current wave, waves cleared, and enemies killed.
- Render a lightweight HUD showing timer, wave index, and kill count in screen space using existing fonts.
- Show a run summary on game over (time survived, wave reached, kills) that clears and resets on restart.

## Non Goals
- Leaderboards, persistence, or meta-progression.
- New art/audio assets; keep text-based HUD/summary.
- Rebalancing combat or wave pacing beyond what’s needed for accurate stats.

## Acceptance criteria
- [x] Enemy deaths emit a dedicated event (e.g., `EnemyDiedEvent` or `EntityDiedEvent` with faction) and are not double-fired.
- [x] Live HUD displays elapsed time and kill count; wave indicator matches wave events and stays in sync after restarts.
- [x] On player death, a summary overlay shows time survived, wave reached, and enemies killed; values reset when restarting a run.
- [x] Stats pause during game over and resume correctly after restart; no accumulation across runs.
- [x] `dotnet build` passes.

## Definition of done
- Builds pass (`dotnet build`)
- Tests/play check done (if applicable)
- Docs updated (if applicable)
- Handoff notes added (if handing off)

## Plan
- Step 1: Add an enemy (or generic) death event in the health/damage flow and ensure it fires once per death.
- Step 2: Introduce a session stats component/system that subscribes to wave/death events and tracks time elapsed with pause on game over.
- Step 3: Implement HUD/overlay rendering for timer, wave, and kill count using existing fonts; add run summary in game over state.
- Step 4: Integrate with restart flow to reset stats and verify counts/timers across multiple runs; run `dotnet build`.

## Notes / Risks / Blockers
- Avoid double-counting deaths from cleanup or repeated damage after zero HP.
- Pause timers during game over to keep summaries accurate.
- Ensure event subscriptions are not duplicated on restart; consider lifecycle hooks or system resets.

