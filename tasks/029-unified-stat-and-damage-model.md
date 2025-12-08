# Task: 029 - Unified stat and damage model
- Status: completed

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
- [x] One shared damage calculator is used by melee, projectile, and knockback systems; legacy ad-hoc math removed.
- [x] Core stats exist on player and enemies with sensible defaults and stacking behavior; movement and attack speed respect these stats.
- [x] Damage types and resist/armor reduction are applied consistently; crit rolls are deterministic and tested.
- [x] Debug output (console or overlay) can show current stats and recent damage calculations for validation.
- [x] Tests cover stat stacking, crit determination, resist/armor application, and integration through at least one attack path.
- [x] `dotnet build` passes.

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

## Handoff notes (2024-12-07)

### Implementation Summary
Implemented a comprehensive unified stat and damage model across the entire codebase:

**Core Components Created:**
- `StatsComponents.cs`: OffensiveStats, DefensiveStats, StatModifiers, ComputedStats
- `DamageTypes.cs`: DamageType enum (Physical, Arcane, True), DamageFlags, DamageInfo, DamageResult
- `DamageCalculator.cs`: CombatRng for deterministic rolls, DamageCalculator with full pipeline, StatCalculator with stacking rules
- `DamageApplicationService.cs`: Unified damage application used by all combat systems
- `StatRecalculationSystem.cs`: Automatically updates ComputedStats when stats change
- `StatInspector.cs`: Debug utility for viewing stats and simulating damage

**Systems Updated:**
- `MeleeHitSystem`: Now uses DamageApplicationService with Physical damage, can crit
- `ProjectileHitSystem`: Now uses DamageApplicationService with Arcane damage, can crit
- `ContactDamageSystem`: Now uses DamageApplicationService with Physical damage, can crit
- `PlayerEntityFactory`: Added default OffensiveStats, DefensiveStats, StatModifiers, ComputedStats
- `EnemyEntityFactory`: Added default stats (enemies don't crit by default)
- `EcsWorldRunner`: Registered StatRecalculationSystem to run before combat systems

**Events Updated:**
- `EntityDamagedEvent`: Now includes IsCritical and DamageType fields for future damage number styling

**Damage Formula:**
1. Base damage × Power
2. Roll for crit (if CanCrit flag set)
3. Apply crit multiplier if crit
4. Calculate resist/armor reduction: `stat / (stat + 100)`, clamped at 90%
5. Apply reduction: `damage * (1 - reduction)`
6. Result is final damage

**Default Stats:**
- Player: 1.0 power, 1.0 attack speed, 5% crit, 1.5x crit multi, 0 armor/resist
- Enemies: 1.0 power, 1.0 attack speed, 0% crit, 0 armor/resist

**Testing:**
- 29 tests covering damage calculation, stat stacking, RNG determinism
- All tests pass with deterministic seeds
- Coverage includes: power scaling, crits, armor/resist, damage types, stat modifiers, cooldown calc

**Documentation:**
- Full design doc at `docs/design/029-unified-stat-and-damage-model.md`
- Covers all formulas, stacking rules, integration points, and future extensibility

### Build Status
- `dotnet build` passes cleanly (42 warnings are CA1305 locale formatting in debug tool, non-blocking)
- All 29 new tests pass
- No regressions in existing systems

### Integration Points for Future Tasks
**Task 030 (Loot/Equipment):**
- Add items that modify `StatModifiers` component
- Mark `ComputedStats.IsDirty = true` when equipping/unequipping
- System will auto-recalculate next frame

**Task 031 (Talent/Perk Tree):**
- Perks modify `StatModifiers` component
- Use `StatModifiers.Combine()` to merge multiple perk effects
- Mark dirty flag when allocating/resetting perks

**Task 032 (Elites/Bosses):**
- Set higher base stats in OffensiveStats/DefensiveStats
- Can add unique modifiers (e.g., elite has 2.0x power, 50 armor)
- Damage calculation automatically applies

### Known Limitations
- `AttackStats.Damage` field still exists for backward compat but is now just base input to calculator
- Systems manually instantiate `DamageApplicationService` with lazy init (could be injected in future)
- Debug inspector uses culture-dependent formatting (CA1305 warnings, acceptable for debug tool)
- Move speed recalculation is opt-in (requires MoveSpeed component on entity)

### Next Steps
- Playtest to verify damage feels correct at default values
- May need to tune crit chance/multiplier and armor/resist scaling
- Consider adding stat soft caps if power stacking becomes too strong
- Integration with future loot/perk systems should be straightforward via StatModifiers

