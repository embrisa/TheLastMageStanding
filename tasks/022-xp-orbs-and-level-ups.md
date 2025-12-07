# Task: 022 - XP orbs and level-ups
- Status: backlog

## Summary
Runs end without progression feedback. Add lightweight XP orbs on enemy death, collection mechanics, and a basic level-up flow that boosts player stats, giving players a sense of growth across a run.

## Goals
- Spawn XP orbs on enemy death events; make them collectible via proximity with optional magnet pull.
- Track XP/level data on the player; define XP curve and per-level stat bonuses (e.g., damage, move speed, max HP).
- Render an XP bar in the HUD that scales with virtual resolution.
- Show a simple level-up notification; apply bonuses immediately without a full choice UI.

## Non Goals
- Talent trees, reroll UI, or meta-progression persistence.
- New art/audio assets beyond simple orb sprites/primitive and UI bar.
- Complex pickup physics; keep movement simple and performant.

## Acceptance criteria
- [ ] Enemy death emits XP orbs that appear in the world, can be collected, and despawn on pickup.
- [ ] Player accrues XP and levels up according to a defined curve; levels grant immediate stat bonuses from config.
- [ ] HUD shows current XP progress and updates in real time; level-up notification is visible and non-blocking.
- [ ] XP and levels reset cleanly on run restart; no carry-over between runs.
- [ ] `dotnet build` passes.

## Definition of done
- Builds pass (`dotnet build`)
- Tests/play check done (if applicable)
- Docs updated (if applicable)
- Handoff notes added (if handing off)

## Plan
- Step 1: Define XP/level components/config; listen to enemy death events to spawn XP orbs with pickup/lifetime behavior.
- Step 2: Implement XP collection and leveling logic on the player; apply stat bonuses on level-up.
- Step 3: Add HUD XP bar and level-up notification; ensure scaling with virtual resolution.
- Step 4: Verify restart resets XP/level state and orbs; run `dotnet build`.

## Notes / Risks / Blockers
- Avoid spawning excessive orbs; consider clamping per-enemy XP or merging close orbs.
- Ensure pickups respect faction (player-only) and do not conflict with other interactions.
- Keep HUD bar performant; reuse existing fonts/atlas to avoid content pipeline churn.

