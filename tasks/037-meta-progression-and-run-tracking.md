# Task: 037 - Meta progression & run tracking (MVP)
- Status: backlog

## Summary
Add lightweight meta progression by persisting run stats (best wave, kills, time, damage dealt/taken) and unlocking small passive bonuses for future runs. Provide UI to view/apply unlocks, and include a reset option.

## Goals
- Capture end-of-run stats and store them between sessions (local persistence).
- Define an unlock tree of small passive bonuses (e.g., +HP, +MoveSpeed, +GoldFind) that tie into the unified stat model and loot/perk economy.
- Provide UI to view run history highlights and spend unlock tokens/points; include reset/wipe.
- Apply unlocks at run start and ensure they stack correctly with items/perks (Tasks 029–031).
- Add tests for persistence load/save, unlock application, and stat recomputation.

## Non Goals
- Cloud sync or account systems.
- Narrative meta content or quests; focus on numeric bonuses.
- Complex economy tuning; keep values conservative.
- Multiplayer considerations.

## Acceptance criteria
- [ ] Run stats persist across game restarts; best wave/time/kills/damage are recorded.
- [ ] Unlock tree exists with spendable points/tokens; applying unlocks updates player stats on the next run.
- [ ] UI displays run highlights and unlock options; includes a reset/wipe control with confirmation.
- [ ] Unlock effects stack deterministically with items/perks; no double-application or stale caches.
- [ ] Tests cover persistence load/save, unlock application, and integration with stat recompute; `dotnet build` passes.

## Definition of done
- Builds pass (`dotnet build`)
- Tests/play check done (if applicable)
- Docs updated (persistence format, unlock schema, UI flows)
- Handoff notes added (if handing off)

## Plan
- Step 1: Add run tracking and persistence (file/JSON) with summaries captured on game over.
- Step 2: Define unlock schema and apply-at-start logic hooked into player creation/stat model.
- Step 3: Build meta UI for viewing stats and spending points; add reset flow.
- Step 4: Add tests for persistence, unlock application, and stat recomputation; run build/play check.

## Notes / Risks / Blockers
- Persistence must be robust to schema changes; include versioning/defaults.
- Avoid applying unlocks twice when reloading scenes; centralize apply-on-start.
- Coordinate with perk/loot stacking to prevent runaway power; start with low values.
- Provide accessibility: allow skipping meta bonuses for challenge runs (toggle).***

