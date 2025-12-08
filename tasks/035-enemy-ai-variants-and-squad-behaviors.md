# Task: 035 - Enemy AI variants & squad behaviors
- Status: backlog

## Summary
Add differentiated enemy roles (charger, protector, buffer) with simple squad behaviors that vary pressure and positioning. Reuse existing hitbox/projectile/telegraph systems to keep readability while increasing encounter variety.

## Goals
- Define AI role configs (behavior states, ranges, cooldowns) and a behavior selector that switches states based on distance/timers.
- Implement at least three behaviors:
  - Charger: closes distance fast, commits to a swing with telegraph and knockback.
  - Protector: shields nearby allies or blocks projectiles briefly.
  - Buffer: applies a timed buff (e.g., move speed or damage) to allies in radius.
- Integrate behaviors into wave spawning with per-role weights and spacing rules.
- Add debug tools to visualize current AI state, target, and active buffs/shields.
- Cover tests for behavior switching, cooldown enforcement, and wave config parsing for roles.

## Non Goals
- Pathfinding/navmesh work beyond current steering/separation.
- Complex group tactics (flanking, formations) or blackboard AI.
- Networking/rollback.
- New art; reuse current assets/telegraphs/VFX.

## Acceptance criteria
- [ ] Three role behaviors are implemented and spawn via wave config; behaviors telegraph clearly and do not deadlock pathing.
- [ ] Protectors can block or mitigate projectiles for a short window; buffers apply timed buffs to allies; chargers execute commit swings with knockback.
- [ ] Behavior switching respects cooldowns and distance thresholds; debug overlay can show current state and target.
- [ ] Wave pacing remains stable; no runaway performance or collision jitter with added behaviors.
- [ ] Tests cover state transitions, cooldowns, and wave role parsing; `dotnet build` passes.

## Definition of done
- Builds pass (`dotnet build`)
- Tests/play check done (if applicable)
- Docs updated (role configs, state machine, wave integration)
- Handoff notes added (if handing off)

## Plan
- Step 1: Add role definitions and a lightweight behavior selector/state machine.
- Step 2: Implement charger/protector/buffer behaviors with telegraphs and hooks into combat systems.
- Step 3: Integrate roles into wave config with weights and spacing; add debug overlays.
- Step 4: Add tests for state transitions and wave parsing; run build/play check.

## Notes / Risks / Blockers
- Context: Mage is the first class with fire/arcane/frost skill & talent trees; enemy roles should pressure that kit without requiring other classes.
- Ensure protector blocking integrates with collision layers without breaking player projectiles.
- Buff stacking must align with the unified stat model; avoid double-application.
- Chargers must respect separation/knockback systems to prevent jitter.
- Keep state machines deterministic; avoid random jitter per frame—seed any randomness.
- Visual clarity: telegraph colors should differentiate roles to reduce confusion in hordes.***

