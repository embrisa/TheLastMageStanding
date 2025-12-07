# Task: 015 - Migrate Player Actions to Event Bus
- Status: done

## Summary
Move discrete player actions (specifically "Attack") from the polled `InputIntent` component to the Event Bus. This allows for cleaner separation of input gathering and action handling, and supports future features like input buffering or combo systems.

## Goals
- Remove `Attack` boolean from `InputIntent` component.
- Create `PlayerAttackIntentEvent`.
- Update `InputSystem` to publish `PlayerAttackIntentEvent` when attack input is detected.
- Update `CombatSystem` (or relevant system) to subscribe to `PlayerAttackIntentEvent` to trigger attacks.

## Non Goals
- Changing continuous movement logic (WASD) - that stays in `InputIntent` for now.
- Implementing complex combo systems.

## Acceptance criteria
- [x] `Attack` property removed from `InputIntent`.
- [x] Player attacks trigger via `PlayerAttackIntentEvent`.
- [x] Attack behavior (cooldowns, range checks) remains functional.
- [x] `dotnet build` passes.

## Plan
- Step 1: Define `PlayerAttackIntentEvent` struct.
- Step 2: Update `InputSystem` to detect attack press and publish event.
- Step 3: Update `CombatSystem` to listen for the event and execute attack logic (checking cooldowns, etc.).
- Step 4: Remove `Attack` from `InputIntent`.
