# Task: 018 - Game over & wave HUD
- Status: backlog

## Summary
`WaveStartedEvent`, `WaveCompletedEvent`, and `PlayerDiedEvent` exist but nothing consumes them. Waves keep spawning after the player dies, and there is no on-screen feedback for wave progression or failure. We need a simple session controller and HUD to surface these events, pause gameplay on death, and allow restarting a run.

## Goals
- Track session state (playing vs. game over) driven by game state events.
- React to wave start/complete events with visible HUD cues that show the current wave.
- On player death, halt input/combat spawning and show a game over overlay.
- Provide a restart control that resets the run (player health/position, wave timer/index, clears enemies) and resumes play.

## Non Goals
- Full menu system, save/load, or meta-progression.
- New art assets or audio design beyond text HUD/notifications.
- Changing combat balance or wave generation logic beyond pausing/resetting.

## Acceptance criteria
- [ ] Wave start/completion events produce visible HUD messaging tied to the current wave index and time out automatically.
- [ ] When the player dies, gameplay stops (no new spawns, enemies stop dealing damage, player input is disabled) and a game over overlay is shown.
- [ ] Pressing a restart input (e.g., Enter or R) from game over resets the session: player respawns, health restored, wave index resets, wave timer restarts, and existing enemies are cleared.
- [ ] HUD renders in screen space with existing fonts and scales correctly with the virtual resolution.
- [ ] `dotnet build` passes.

## Definition of done
- Builds pass (`dotnet build`)
- Tests/play check done (if applicable)
- Docs updated (if applicable)
- Handoff notes added (if handing off)

## Plan
- Step 1: Design a session/state component and system that subscribes to `WaveStartedEvent`, `WaveCompletedEvent`, and `PlayerDiedEvent`, gating wave scheduling/combat when game over.
- Step 2: Implement a HUD/overlay system (screen-space) that renders current wave and transient start/complete toasts; show game over messaging on death.
- Step 3: Add restart input handling in game-over state to reset world/session (player health/position, wave index/timers, clear entities) and resubscribe/cleanup event handlers as needed.
- Step 4: Playtest death and restart flows; verify build.

## Notes / Risks / Blockers
- Resetting the session may require reinitializing world state or carefully removing entities; avoid double subscriptions on the EventBus.
- Ensure wave timers pause during game over so the next run starts cleanly.
- Keep HUD text legible; reuse existing fonts to avoid new content pipeline steps.

