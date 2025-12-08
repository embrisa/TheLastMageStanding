# Task: 036 - Elite modifiers & mutators
- Status: backlog

## Summary
Introduce stackable elite modifiers (mutators) that change enemy behavior and rewards (e.g., Extra Projectiles, Vampiric, Explosive Death, Shielded). Integrate with telegraphs/VFX and the unified stat/damage model to increase encounter variety and risk/reward.

## Goals
- Define elite modifier data (name, effects, telegraph cues, reward scaling) and attach them to elite archetypes at spawn.
- Implement at least four mods:
  - Extra Projectiles (fans or spreads)
  - Vampiric (lifesteal on hit)
  - Explosive Death (AoE on death with telegraph)
  - Shielded (temporary damage reduction or periodic shield)
- Ensure mods announce themselves (telegraph/VFX/SFX) and stack safely without duplicate effects.
- Scale rewards (loot bias, reroll tokens, or guaranteed drop) based on mod count/difficulty.
- Add debug hooks to spawn elites with specific mod sets for testing.

## Non Goals
- Full affix randomization for all enemies (limit to elites/bosses).
- Procedural boss phases; keep single-phase behaviors.
- Network/rollback support.
- New art pipelines; reuse existing FX/telegraph primitives.

## Acceptance criteria
- [ ] Elites can spawn with 1–N modifiers; each implemented mod functions and stacks without conflicts.
- [ ] Telegraph/VFX/SFX clearly indicate active modifiers (e.g., shield aura, explosive death warning circle).
- [ ] Rewards scale with modifier count; drops integrate with loot/perk systems (Tasks 030/031).
- [ ] Debug command/toggle can spawn elites with chosen mods; normal waves remain stable.
- [ ] Tests cover mod application, stacking, and reward scaling; `dotnet build` passes.

## Definition of done
- Builds pass (`dotnet build`)
- Tests/play check done (if applicable)
- Docs updated (modifier list, effects, telegraphs, reward rules)
- Handoff notes added (if handing off)

## Plan
- Step 1: Add modifier data structures and attach flow during elite spawn; include reward scaling hooks.
- Step 2: Implement the four baseline modifiers with telegraphs/VFX/SFX and integrate with combat systems.
- Step 3: Add debug spawn options and wave config support; ensure stacking safety.
- Step 4: Add tests for modifier application and reward scaling; run build/play check.

## Notes / Risks / Blockers
- Context: Mage is the first class with fire/arcane/frost skill & talent trees; mod design and rewards should align with mage gameplay pacing.
- Stacking must be idempotent; guard against duplicate application on respawn or refresh.
- Explosive death must respect collision layers and static geometry; ensure telegraph timing is fair.
- Vampiric should clamp healing to max HP and avoid multi-hit over-healing per frame.
- Shielded/DR effects must use unified stat model hooks to avoid bypassing mitigation logic.
- Reward inflation risk—start conservative; consider soft caps per wave.***

