# Task: 016 - Game State Events
- Status: completed

## Summary
Introduce high-level game state events to allow systems to react to major changes (Wave Start, Wave End, Player Death) without tight coupling.

## Goals
- Define `WaveStartedEvent`, `WaveCompletedEvent`, `PlayerDiedEvent`.
- Update `WaveSchedulerSystem` to publish wave events.
- Update health/death logic to publish `PlayerDiedEvent`.
- Ensure these events are available for UI or other listeners.

## Non Goals
- Implementing a full UI system.
- Changing the game loop state machine (just the events).

## Acceptance criteria
- [x] Events defined in `src/Game/Core/Events`.
- [x] `WaveSchedulerSystem` publishes wave events.
- [x] Player death triggers `PlayerDiedEvent`.
- [x] `dotnet build` passes.

## Plan
- Step 1: Define the event structs.
- Step 2: Instrument `WaveSchedulerSystem` to publish events at state transitions.
- Step 3: Instrument the system handling player health (likely `HitReactionSystem` or similar) to publish death event.
