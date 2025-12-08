# Task: 034 - Status effects & elemental interactions
- Status: backlog

## Summary
Introduce reusable status effects (Burn, Freeze/Slow, Shock, Poison) that stack, expire, and interact with elemental resistances. Integrate with the unified damage model and combat events, with clear VFX/SFX cues and debugability.

## Goals
- Define status effect components (type, potency, duration, stacks, tick cadence) with deterministic timing at fixed timestep.
- Hook status application into hit events (melee, projectile, elite mods), including resistance/immune flags on entities.
- Implement baseline effects: Burn (DoT), Freeze/Slow (movement/attack speed debuff), Shock (bonus damage or crit amp), Poison (stacking DoT with ramp).
- Add VFX/SFX/telegraph cues per effect and a debug overlay to visualize active statuses on entities.
- Cover tests for stacking rules, duration expiry, resistance/immune handling, and deterministic ticking.

## Non Goals
- Complex aura/area status propagation beyond simple AoE application.
- Advanced CC like stuns/roots (out of scope for this task).
- Network/rollback support.
- Long-form DoT/HoT balancing; keep values tunable placeholders.

## Acceptance criteria
- [ ] At least two status effects are applied by both player and enemies; they stack/refresh according to defined rules and expire correctly.
- [ ] Resistances/immunities prevent or reduce effects as configured; damage formulas use the unified model (Task 029).
- [ ] VFX/SFX cues indicate status presence; debug view can display active effects and remaining durations.
- [ ] Status ticks are deterministic and tied to fixed timestep; no double-tick or missed-tick bugs on variable frame rates.
- [ ] Tests cover stacking/refresh, resistance/immunity paths, and tick timing; `dotnet build` passes.

## Definition of done
- Builds pass (`dotnet build`)
- Tests/play check done (if applicable)
- Docs updated (status definitions, stacking rules, resist/immune flags)
- Handoff notes added (if handing off)

## Plan
- Step 1: Define status effect data/components and deterministic ticking mechanism.
- Step 2: Integrate application into hit events and tie into unified damage/resistance handling.
- Step 3: Add VFX/SFX/telegraph cues and debug overlays; tune initial values.
- Step 4: Add tests for stacking, immunity, and tick cadence; run build/play check.

## Notes / Risks / Blockers
- Ensure ticking is frame-rate independent and deterministic; store accumulated time.
- Stacking rules must be explicit (additive vs refresh vs max stacks) to avoid ambiguity.
- Avoid per-frame allocations when managing active status lists; consider pooled buffers.
- VFX spam risk in hordes; support LOD or throttling for effects on many entities.
- Coordinate with Task 033 dash i-frames: statuses should not apply during immune frames.***

