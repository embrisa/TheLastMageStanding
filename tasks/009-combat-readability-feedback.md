# Task: 009 - Combat readability & feedback
- Status: done

## Summary
Improve combat feedback so hits are clear and satisfying: add on-hit flashes, lightweight damage numbers, and a brief knockback/slow response without breaking core movement. Keep visuals performant and readable with existing scaling.

## Goals
- Add hit feedback (flash/tint or small effect) on player/enemy damage events.
- Show small, readable damage numbers that respect camera scale.
- Apply a short knockback or slow on melee hits to improve feel without desyncing movement.
- Keep performance stable and visuals consistent with 128 PPU/sprite sizing.

## Non Goals
- Full VFX system, screen shake, or audio pass.
- Rebalancing combat stats beyond minor tuning needed for feedback timing.

## Acceptance criteria
- [ ] When damage is dealt, the target shows a brief visual cue (flash/tint or tiny effect).
- [ ] Damage numbers appear, stay legible at virtual resolution, and vanish quickly without overlap issues.
- [ ] Melee contact applies a short knockback or slow that is visible and does not cause tunneling or movement jitter.
- [ ] Performance stays stable with current enemy counts (no new smearing or frame drops).
- [ ] `dotnet build` passes.

## Definition of done
- Builds pass (`dotnet build`)
- Tests/play check done (if applicable)
- Docs updated (if applicable)
- Handoff notes added (if handing off)

## Plan
- Add a damage feedback component/utility that triggers flash/tint on health change events.
- Implement lightweight floating numbers (screen-space or world-space scaled to 128 PPU) with lifetime and fade.
- Apply knockback/slow in combat resolution; clamp to avoid stacking and keep ECS state clean.
- Playtest to confirm readability and stability; tweak timings/magnitudes; run `dotnet build`.

## Notes / Risks / Blockers
- Character sprites are 128x128; pixel-per-unit is 128—ensure numbers/effects scale accordingly.
- Tiles are 128x256 with pivot X 0.5, Y 0.18 (unique tiles like cliffs may need Y ~0.43); useful for aligning hit effects near feet vs. center.
- Keep lifetimes short to avoid overdraw; ensure effects don’t reintroduce smearing (clear render targets each frame as now).

