# Design: 034 - Status effects & elemental interactions

## Overview
This design introduces reusable, deterministic status effects applied on hit via the unified damage model. Status effects:
- Apply after damage events are processed (health reduced first).
- Stack/refresh per-effect rules.
- Tick deterministically at fixed timestep using accumulated time.
- Respect immunities and resistances (stat-based + optional per-effect multipliers).
- Provide VFX/SFX hooks, debug overlay, and inspection tools.

Implementation lives primarily under `src/Game/Core/Ecs/Systems/StatusEffect*` and `src/Game/Core/Ecs/Components/StatusEffectComponents.cs`.

## Status effect definitions

### Burn
- Type: DoT
- Default: 5 dmg/sec, 3s duration, 0.5s tick
- Stacking: additive stacks up to max (default 3); refresh duration to max
- Damage: true damage per tick

### Poison
- Type: DoT
- Default: 3 dmg/sec, 4s duration, 0.5s tick
- Stacking: additive stacks up to max (default 5); refresh duration to max
- Ramp: +20% damage per extra stack (stack 1 = base, stack 2 = +20%, ...)
- Damage: true damage per tick

### Freeze
- Type: debuff (slow)
- Default: 70% move/attack slow, 2s duration
- Stacking: no stacks; strongest potency wins; refresh duration to max

### Slow
- Type: debuff (slow)
- Default: 50% move/attack slow, 1.5s duration
- Stacking: no stacks; strongest potency wins; refresh duration to max

### Shock
- Type: debuff (damage taken amplifier)
- Default: +25% damage taken, 2s duration
- Stacking: single stack; refresh duration to max
- Effect: multiplies incoming `DamageInfo.BaseDamage` while active (amplifies all damage types, including true)

## Stacking rules (summary)
- Burn: additive stacks, duration refresh
- Poison: additive stacks, duration refresh, ramped damage
- Freeze/Slow: no stacks, strongest potency wins, duration refresh
- Shock: single stack, duration refresh

## Resistance & immunity

### Stat-based resistance formula
Uses the same diminishing returns curve as damage resist:
- `reduction = stat / (stat + 100)`, clamped to 90%
- Applied as `(1 - reduction)` multiplier to both **duration** and (where relevant) **potency**

### Mapping
- Burn → `FireResist`
- Freeze/Slow → `FrostResist`
- Shock/Poison → `ArcaneResist`

### Immunity
`StatusEffectImmunities` can block specific effects completely.

### Extra per-effect resistance
`StatusEffectResistances` (0..1) can add an additional multiplier per effect (useful for enemy-specific immunities without changing stats).

## Application sources
- Skills: `SkillDefinition.OnHitStatusEffect` + `StatusEffectApplicationChance` (rolled deterministically via `CombatRng`).
- Combat hits: `AttackHitbox.StatusEffect` and `Projectile.StatusEffect` flow into `DamageInfo.StatusEffect`.
- Enemies (baseline examples):
  - `bone_scout`: poison-on-hit (chance-based).
  - `bone_mage`: shock-on-hit (chance-based, via ranged projectiles).

## Systems and ordering
- `DamageApplicationService` publishes `EntityDamagedEvent`.
- `HitReactionSystem` consumes `EntityDamagedEvent` to reduce shields/health (status ticks are excluded from knockback/slow reactions).
- `StatusEffectApplicationSystem` consumes `EntityDamagedEvent` and applies a status payload from `DamageInfo.StatusEffect`.
- `StatusEffectTickSystem` updates durations, applies DoT ticks (true damage), maintains `StatusEffectModifiers`, and updates `StatusEffectVisual` for tinting.

## Events & feedback
- Events: `StatusEffectAppliedEvent`, `StatusEffectTickEvent`, `StatusEffectExpiredEvent`
- VFX/SFX hooks: `StatusEffectVfxSystem` publishes `VfxSpawnEvent` and `SfxPlayEvent`
- Render feedback: `StatusEffectVisual` drives per-entity tinting in `PlayerRenderSystem` and `EnemyRenderSystem`

## Debugging
- F10 toggles status overlay (per-entity list of active effects).
- `StatusEffectInspector.InspectStatusEffects(...)` prints active effects and resist values.
- Console commands (during `dotnet run`):
  - `status apply <type> <durationSeconds>`
  - `status clear`
  - `status immune <type>`
  - `status resist <type> <amount>`

