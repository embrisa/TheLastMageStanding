# Task: 014 - Migrate Combat Events to Event Bus
- Status: completed

## Summary
Migrate the ad-hoc `DamageEvent` component pattern to the new `EventBus`. This will decouple damage sources from damage handlers and remove the need for component addition/removal for transient events.

## Goals
- Replace `DamageEvent` component with `EntityDamagedEvent` struct.
- Update `CombatSystem` to publish `EntityDamagedEvent`.
- Update `HitReactionSystem`, `HitEffectSystem`, and `DamageNumberSystem` to subscribe to `EntityDamagedEvent`.
- Remove `DamageEvent` component definition.

## Non Goals
- Changing the damage calculation logic itself.
- Migrating other event types (like death or spawn) yet.

## Acceptance criteria
- [x] `DamageEvent` component is removed.
- [x] `EntityDamagedEvent` is defined in `src/Game/Core/Events`.
- [x] Combat works exactly as before (damage numbers, flash, knockback).
- [x] No "DamageEvent" components are added to entities during runtime.
- [x] `dotnet build` passes.

## Plan
- Step 1: Define `EntityDamagedEvent` struct in `src/Game/Core/Events`.
- Step 2: Update `CombatSystem` to publish this event instead of adding `DamageEvent` component.
- Step 3: Update `HitReactionSystem` to subscribe to `EntityDamagedEvent` and apply health reduction/knockback.
- Step 4: Update `HitEffectSystem` to subscribe and apply visual effects.
- Step 5: Update `DamageNumberSystem` to subscribe and spawn damage numbers.
- Step 6: Delete `DamageEvent` component from `CombatFeedbackComponents.cs`.
