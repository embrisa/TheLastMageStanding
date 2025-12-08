# Task: 033 - Dash/defense moves & i-frames
- Status: backlog

## Summary
Add a responsive defensive move (dash/evade/block) with brief invulnerability frames, stamina/cooldown gating, and telegraphed timing. Integrate with animation events, camera nudge, and the collision/hit-stop stack so players can avoid burst pressure from elites and projectiles.

## Goals
- Implement a dash/evade action with configurable distance, speed curve, and i-frame window, driven by animation events.
- Add gating via stamina bar or cooldown (configurable), with UI feedback and input buffering.
- Integrate with collision: allow passing through enemies during i-frames, but respect world-static colliders; clamp end position.
- Hook in readability: telegraph cue on start/end, camera nudge, optional afterimage VFX, and SFX.
- Cover tests for cooldown/stamina, i-frame correctness, collision immunity, and end-position clamping.

## Non Goals
- Complex parry/counter mechanics or shield directions.
- Perfect block timing bonuses beyond basic i-frame avoidance.
- Netcode/rollback considerations.
- New animation authoring tools (reuse current animation event system).

## Acceptance criteria
- [ ] Player can dash/evade with a defined i-frame window; collisions with enemies/projectiles do not deal damage during i-frames.
- [ ] Dash respects world geometry: no tunneling through static colliders; end position is clamped or slides along walls.
- [ ] Gating works: dash is limited by cooldown or stamina; UI shows availability; input buffering works within a short window.
- [ ] Telegraph/FX: visible start/end cue; optional camera nudge; SFX plays; debug overlay can show i-frame window when enabled.
- [ ] Tests cover cooldown/stamina gating, i-frame immunity, and collision clamping; `dotnet build` passes.

## Definition of done
- Builds pass (`dotnet build`)
- Tests/play check done (if applicable)
- Docs updated (dash config, i-frame rules, UI controls)
- Handoff notes added (if handing off)

## Plan
- Step 1: Add dash components/config (distance, speed, i-frame window, cooldown/stamina) and input buffering.
- Step 2: Wire animation events to drive dash start/end, apply camera nudge, and toggle i-frame state.
- Step 3: Integrate with collision/hit systems to allow enemy/projectile passthrough during i-frames while blocking world geometry.
- Step 4: Add UI feedback and tests (gating, immunity, clamping); run build/play check.

## Notes / Risks / Blockers
- Ensure deterministic i-frame timing at fixed timestep; avoid drift between animation speed and logic.
- Prevent dash chaining exploits by enforcing cooldown/stamina and buffering rules.
- Sliding along walls must not overshoot; reuse collision resolution to clamp displacement.
- Camera nudge should be subtle; respect existing shake limits (Task 028).
- Consider accessibility: allow disabling camera nudge while keeping hit-stop.***

