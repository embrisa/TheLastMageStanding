# Task: 027 - Animation events & directional hitboxes
- Status: backlog

## Summary
Drive combat hitboxes from animation timelines instead of hardcoded windows. Add per-facing offsets so swings align to sprite direction, enabling future animation-driven VFX/SFX and reuse across player and enemy melee. Keep fully compatible with the collider-driven combat work from Task 026.

## Goals
- Define a lightweight animation event data model (frame/time-based) that can enable/disable hitboxes and emit optional hooks (VFX/SFX).
- Add directional hitbox offsets per facing (4-way or 8-way) and wire them into melee hitbox spawning.
- Migrate existing melee attacks (player + at least one melee enemy archetype) to use animation-event-driven windows; remove legacy hardcoded timings.
- Extend debug overlay to show active hitbox windows, facing offsets, and owning entity for validation.
- Keep event data authorable in code/config (no new editor) and cache per animation to avoid per-frame allocations.

## Non Goals
- Building a full animation editor or content-authoring UI.
- Combo/chain systems, cancel windows, or advanced animation blending.
- Root-motion changes or physics-driven animation.
- Networking/rollback synchronization of animation events.

## Acceptance criteria
- [ ] Animation events can enable/disable hitboxes at specific frames/timestamps and remain stable at the fixed timestep.
- [ ] Player melee attacks use per-facing offsets; debug overlay shows the correct hitbox position as the swing plays.
- [ ] At least one melee enemy archetype uses the same system; no remaining hardcoded melee timing constants.
- [ ] Event data can trigger optional VFX/SFX hooks; missing assets log warnings without breaking gameplay.
- [ ] Tests cover event scheduling, per-facing offset selection, and hitbox deactivation at the end of a window.
- [ ] `dotnet build` passes.

## Definition of done
- Builds pass (`dotnet build`)
- Tests/play check done (if applicable)
- Docs updated (design/tech notes on event data and facing offsets)
- Handoff notes added (if handing off)

## Plan
- Step 1: Add animation event data model (frame/time keyed) and resolver mapping current animation state + facing to event windows.
- Step 2: Extend melee hitbox spawner to consume event windows, apply directional offsets, and retire hardcoded timings.
- Step 3: Update player and at least one melee enemy animation configs with event data; add debug overlay for active windows and owner labels.
- Step 4: Add tests for event scheduling, offset selection, and hitbox lifecycle; run build/play check.

## Notes / Risks / Blockers
- Need exact alignment between frame indices, spritesheet packing, and playback speed—validate with current animation system.
- Must stay deterministic at fixed timestep; avoid floating drift for looping animations.
- Direction mapping (4-way vs 8-way) must match existing facing logic to prevent mirrored offsets on diagonals.
- Keep event hooks optional so missing VFX/SFX do not break the game.
- Asset authorship is still manual; consider CSV/JSON export later if iteration slows.

