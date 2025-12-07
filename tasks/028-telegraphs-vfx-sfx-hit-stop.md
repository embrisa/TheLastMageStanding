# Task: 028 - Telegraphs, VFX/SFX, and hit-stop
- Status: backlog

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
- [ ] Melee and ranged attacks display a visible windup telegraph before the hit frame; duration is tunable per archetype.
- [ ] Base VFX (e.g., impact flash, projectile trail or muzzle) can be spawned via event hooks; missing assets log warnings but do not crash.
- [ ] SFX hooks fire on windup/impact with per-category volume; a global mute/testing toggle exists.
- [ ] Hit-stop triggers once per impact (per hitbox/projectile) with a capped duration and resumes smoothly; optional camera nudge is togglable.
- [ ] Debug toggle shows telegraph bounds/targets; automated test covers hit-stop timing/aggregation behavior.
- [ ] `dotnet build` passes.

## Definition of done
- Builds pass (`dotnet build`)
- Tests/play check done (if applicable)
- Docs updated (FX/telegraph/hit-stop usage and tuning knobs)
- Handoff notes added (if handing off)

## Plan
- Step 1: Define telegraph/VFX/SFX data structures and a small FX service with pooling and debug toggles.
- Step 2: Wire telegraph and FX hooks into animation events for player melee and ranged enemy attacks.
- Step 3: Implement hit-stop/camera nudge handler invoked by damage events; ensure clamping and global disable are supported.
- Step 4: Add tests for hit-stop aggregation/timing and smoke tests for telegraph/VFX hooks; run build/play check.

## Notes / Risks / Blockers
- Overlapping telegraphs can create visual noise; consider fade/alpha blending or priority rules.
- Hit-stop must not starve input or timers; ensure fixed timestep resumes cleanly.
- FX pooling should avoid per-frame allocations; watch for transient color structs.
- Audio asset gaps are likely; log once per missing asset to avoid spam.
- Camera nudge should respect current camera constraints to avoid motion sickness.

