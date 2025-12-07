# Task: 029 - Unified stat and damage model
- Status: backlog

## Summary
Establish a single stat model and damage formula used by melee, projectiles, and knockback effects. Define core stats (Power, AttackSpeed, CritChance/CritMulti, MoveSpeed, Armor/Resist, CooldownReduction) and stacking rules so future loot/perks can modify them consistently.

## Goals
- Create stat components/modifiers with clear stacking order (base → additive → multiplicative) and caching to minimize per-frame work.
- Define damage types/flags (e.g., Physical, Arcane) and a shared damage calculation that all combat systems consume.
- Expose derived values for attack speed, crit, and movement that plug into existing systems without breaking input/physics.
- Add deterministic random handling for crit rolls to keep behavior stable at fixed timestep and in tests.
- Provide debug/inspection tools to view current stats and effective DPS inputs during playtests.

## Non Goals
- Advanced status effects (DoT, aura, lifesteal) beyond simple resist/armor hooks.
- Full economy/progression tuning; focus on correctness and integration.
- Networking/rollback concerns.
- Healing/regen systems beyond minimal placeholders if needed for tests.

## Acceptance criteria
- [ ] One shared damage calculator is used by melee, projectile, and knockback systems; legacy ad-hoc math removed.
- [ ] Core stats exist on player and enemies with sensible defaults and stacking behavior; movement and attack speed respect these stats.
- [ ] Damage types and resist/armor reduction are applied consistently; crit rolls are deterministic and tested.
- [ ] Debug output (console or overlay) can show current stats and recent damage calculations for validation.
- [ ] Tests cover stat stacking, crit determination, resist/armor application, and integration through at least one attack path.
- [ ] `dotnet build` passes.

## Definition of done
- Builds pass (`dotnet build`)
- Tests/play check done (if applicable)
- Docs updated (stat definitions, stacking rules, damage formula)
- Handoff notes added (if handing off)

## Plan
- Step 1: Define stat components, modifier data structures, and deterministic RNG hooks for crits.
- Step 2: Implement unified damage calculation (including resist/armor) and wire it into melee/projectile/knockback systems.
- Step 3: Update player/enemy defaults and any configs that reference damage/attack speed; add debug/inspection helpers.
- Step 4: Add tests for stacking, crit, and resist behavior, plus an integration test through a combat path; run build/play check.

## Notes / Risks / Blockers
- Must avoid per-frame allocations when combining modifiers; prefer pooled or struct-based aggregations.
- Attack speed changes could desync animation timing; coordinate with Task 027 event timing.
- Crit randomness must be deterministic across platforms; seed strategy should be explicit.
- Armor/resist curves need clamping to avoid negative or runaway mitigation; document the formula.
- Ensure systems that cache stat values refresh on equipment/perk changes (see Tasks 030/031).

