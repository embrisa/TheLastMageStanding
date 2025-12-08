# Task: 028 - Telegraphs, VFX/SFX, and hit-stop
- Status: completed
- Completed: 2025-12-07

## Summary
Add readable windup telegraphs, baseline combat VFX/SFX hooks, and lightweight hit-stop/camera nudge so attacks feel impactful. Leverage animation/event hooks from Task 027 and the collider-driven combat stack.

## Goals
- Introduce reusable telegraph data (duration, color/scale style) that melee and ranged attacks can trigger via animation events.
- Add a small VFX service (primitive shapes or sprites) and attach it to attack windup/impact hooks with pooling to avoid allocations.
- Add SFX hooks with basic category routing and volume controls; tolerate missing assets gracefully.
- Implement hit-stop micro-pauses and optional camera nudge/shake triggered from damage events with clamped durations.
- Provide debug toggles to visualize telegraph bounds and to disable hit-stop/FX for automated tests.

## Non Goals
- Full audio pipeline (mixing bus, ducking, reverb) or localization.
- Complex shader/post-processing passes or cinematic camera work.
- Advanced VFX authoring tooling; start with simple primitives/sprites.
- Slow-motion time dilation outside of brief hit-stop.

## Acceptance criteria
- [x] Melee and ranged attacks display a visible windup telegraph before the hit frame; duration is tunable per archetype.
- [x] Base VFX (e.g., impact flash, projectile trail or muzzle) can be spawned via event hooks; missing assets log warnings but do not crash.
- [x] SFX hooks fire on windup/impact with per-category volume; a global mute/testing toggle exists.
- [x] Hit-stop triggers once per impact (per hitbox/projectile) with a capped duration and resumes smoothly; optional camera nudge is togglable.
- [x] Debug toggle shows telegraph bounds/targets; automated test covers hit-stop timing/aggregation behavior.
- [x] `dotnet build` passes.

## Definition of done
- [x] Builds pass (`dotnet build`)
- [x] Tests/play check done (64 tests passing, 10 new tests added)
- [x] Docs updated (design doc created at docs/design/028-telegraphs-vfx-sfx-hit-stop-implementation.md)
- [x] Handoff notes added

## Plan
- Step 1: Define telegraph/VFX/SFX data structures and a small FX service with pooling and debug toggles.
- Step 2: Wire telegraph and FX hooks into animation events for player melee and ranged enemy attacks.
- Step 3: Implement hit-stop/camera nudge handler invoked by damage events; ensure clamping and global disable are supported.
- Step 4: Add tests for hit-stop aggregation/timing and smoke tests for telegraph/VFX hooks; run build/play check.

## Notes / Risks / Blockers
- ~~Overlapping telegraphs can create visual noise; consider fade/alpha blending or priority rules.~~
  - Implemented with tunable duration and global toggle for testing.
- ~~Hit-stop must not starve input or timers; ensure fixed timestep resumes cleanly.~~
  - Hit-stop only pauses gameplay logic, VFX/SFX systems continue updating.
- ~~FX pooling should avoid per-frame allocations; watch for transient color structs.~~
  - VFX entities are pooled via ECS; color updates handled efficiently.
- ~~Audio asset gaps are likely; log once per missing asset to avoid spam.~~
  - Graceful degradation implemented with one-time logging per missing asset.
- ~~Camera nudge should respect current camera constraints to avoid motion sickness.~~
  - Shake intensity clamped to 8px max; independent toggle for camera shake (F5).

## Handoff Notes
- **Implementation Complete**: All acceptance criteria met, 64 tests passing (10 new tests).
- **Key Components Created**:
  - `TelegraphComponents.cs` - Telegraph data structures
  - `VfxComponents.cs` - VFX component definitions
  - `VfxEvents.cs` - VFX/SFX event definitions
  - `VfxSystem.cs` - VFX lifecycle management
  - `SfxSystem.cs` - SFX playback with category volumes
  - `TelegraphSystem.cs` - Telegraph lifecycle management
  - `TelegraphRenderSystem.cs` - Rendering for telegraphs and VFX
  - `HitStopSystem.cs` - Hit-stop and camera shake logic
- **Systems Integrated**:
  - Hit-stop halts gameplay but allows visual/audio feedback during pause
  - Camera shake applied via `Camera2D.ShakeOffset` property
  - VFX/SFX hooks wired into `AnimationEventSystem` and `MeleeHitSystem`
  - Debug toggles: F3 (collision), F4 (hit-stop), F5 (camera shake), F6 (VFX/SFX)
- **Testing**: Comprehensive tests for hit-stop timing, VFX lifecycle, and telegraph behavior
- **Documentation**: Full implementation guide at `docs/design/028-telegraphs-vfx-sfx-hit-stop-implementation.md`
- **Next Steps**: 
  - Add actual VFX sprite/particle assets when available
  - Load SFX assets into `SfxSystem._loadedSounds` dictionary
  - Consider implementing cone/rectangle telegraph shapes
  - Tune hit-stop durations and camera shake intensity based on playtesting

