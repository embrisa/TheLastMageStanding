# Task: 020 - Pause and settings overlay
- Status: backlog

## Summary
The game currently runs continuously with no in-run pause or settings control. We need a pause state and overlay that cleanly halts gameplay (spawns, combat, timers) and provides basic session options like resume and restart, plus simple audio toggles using existing config.

## Goals
- Add a pause state driven by input (e.g., Escape) that freezes gameplay updates (waves, AI, combat, timers).
- Render a pause overlay in screen space with options to resume and restart the current run.
- Provide basic audio toggles (music/SFX mute) wired through the config system.
- Ensure pause respects the event bus loop and does not drop subscriptions or desync timers when resuming.

## Non Goals
- Full main menu or save/load flow.
- Detailed keybind remapping UI.
- Audio mixing beyond simple mute toggles.

## Acceptance criteria
- [ ] Pressing the pause input toggles a paused state; gameplay systems stop advancing (spawns/combat/movement/timers) until resumed.
- [ ] Pause overlay displays resume and restart actions; inputs work with keyboard (no mouse required).
- [ ] Audio mute toggles affect in-run playback via config/settings and persist for the session.
- [ ] Resuming restores timers (wave, stats) without double-processing events; restart resets the run state using the existing session reset flow.
- [ ] `dotnet build` passes.

## Definition of done
- Builds pass (`dotnet build`)
- Tests/play check done (if applicable)
- Docs updated (if applicable)
- Handoff notes added (if handing off)

## Plan
- Step 1: Introduce a pause flag/state in the world runner/session system; gate update systems and event processing when paused.
- Step 2: Implement a pause overlay UI (screen-space) with resume/restart; hook inputs to toggle and activate actions.
- Step 3: Add music/SFX mute toggles via config; ensure values propagate to existing audio hooks or stubs.
- Step 4: Playtest pause/resume/restart to verify timers, waves, and stats remain consistent; run `dotnet build`.

## Notes / Risks / Blockers
- Ensure event bus subscriptions are not cleared by pause; only gate processing.
- Restart should reuse the existing session reset path to avoid duplicated code.
- If audio routing is not yet centralized, add minimal plumbing to honor mute toggles without refactoring the entire audio stack.

