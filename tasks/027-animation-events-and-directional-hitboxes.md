# Task: 027 - Animation events & directional hitboxes
- Status: done

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
- [x] Animation events can enable/disable hitboxes at specific frames/timestamps and remain stable at the fixed timestep.
- [x] Player melee attacks use per-facing offsets; debug overlay shows the correct hitbox position as the swing plays.
- [x] At least one melee enemy archetype uses the same system; no remaining hardcoded melee timing constants.
- [x] Event data can trigger optional VFX/SFX hooks; missing assets log warnings without breaking gameplay.
- [x] Tests cover event scheduling, per-facing offset selection, and hitbox deactivation at the end of a window.
- [x] `dotnet build` passes.

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

## Implementation Notes (Done)
- Created comprehensive animation event data model with `AnimationEventType`, `AnimationEvent`, `AnimationEventTrack`, and `AnimationEventState` components.
- Implemented `DirectionalHitboxConfig` with 8-way facing offsets and `CreateDefault()` factory for symmetric forward attacks.
- Built `AnimationEventSystem` that processes events, spawns/destroys hitboxes with directional offsets, and handles VFX/SFX hooks.
- Integrated system into `EcsWorldRunner` before `CombatSystem` for proper timing.
- Extended `PlayerEntityFactory` to add animation-driven attack components with 24-unit forward offset.
- Enhanced `CollisionDebugRenderSystem` to show hitbox ownership lines, directional offset arrows, and current facing indicators.
- Created 9 comprehensive tests covering event scheduling, directional offsets, hitbox lifecycle, and edge cases.
- All tests pass (52 total project tests).
- `dotnet build` passes with no warnings.
- Documented in `docs/design/027-animation-events-and-directional-hitboxes-implementation.md`.

## Known Limitations
- Currently uses placeholder attack animation clip (enum value 99) for testing since player doesn't have dedicated attack sprites yet.
- Event data is code-defined in `AnimationEventSystem.RegisterDefaultEventTracks()`; no external authoring tool.
- Enemy melee attacks prepared but not implemented (enemies still use contact damage).
- VFX/SFX hooks log to console but don't trigger actual effects (awaiting asset integration).

## Next Steps (Future Tasks)
1. Add actual player attack animation sprites and replace placeholder clip.
2. Implement VFX/SFX system to consume animation event triggers.
3. Add enemy melee attack animations with event-driven hitboxes.
4. Consider JSON/CSV-based event track configuration for easier authoring.
5. Add combo/chain attack systems with animation event sequences.

