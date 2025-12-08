# 034 — Status effects & elemental interactions

## Overview
- Adds reusable status effects that attach to `DamageInfo` and live on `ActiveStatusEffects` per-entity.
- Effects stack or refresh by type, tick deterministically, and hook into the unified damage model.
- Resistances (fire/frost/arcane/nature) and immunities gate potency/duration; shocks amplify incoming damage.

## Status definitions (defaults)
| Effect | Potency | Duration | Tick | Max stacks | Notes |
| --- | --- | --- | --- | --- | --- |
| Burn | 5 dmg/sec | 3s | 0.5s | 3 | Additive stacks, refresh duration |
| Freeze | 70% slow | 2s | — | 1 | No stacking; strongest wins vs. Slow |
| Slow | 50% slow | 1.5s | — | 1 | No stacking; strongest wins vs. Freeze |
| Shock | +25% damage taken | 2s | — | 1 | Refresh only |
| Poison | 3 dmg/sec | 4s | 0.5s | 5 | +20% damage per extra stack |

## Application & stacking
- Status payload rides on `DamageInfo.StatusEffect`.
- `StatusEffectApplicationSystem` subscribes to `EntityDamagedEvent`, checks immunities, calculates resistance, adjusts potency/duration, and merges into `ActiveStatusEffects`.
- Stacking rules:
  - Burn/Poison: additive stacks up to max; refresh duration.
  - Freeze/Slow: refresh; strongest potency wins.
  - Shock: single stack; refresh duration.
- Publishes `StatusEffectAppliedEvent` for VFX/SFX hooks.

## Tick & stat debuffs
- `StatusEffectTickSystem` runs every frame: updates durations, ticks DoTs at fixed intervals, removes expired effects, and drives stat debuffs.
- DoT ticks use `DamageSource.StatusEffect` and true damage; publishes `StatusEffectTickEvent`.
- Slow/Freeze feed `StatusEffectModifiers` → `StatRecalculationSystem` to lower move/attack speed.
- Shock is applied in `DamageApplicationService` as a damage multiplier on targets with Shock stacks.

## Resistances & immunities
- New defensive stats: FireResist, FrostResist, NatureResist (Poison uses Nature if present, else Arcane).
- Formula: `resist / (resist + 100)` (clamped to 90%), applied to both potency and duration.
- `StatusEffectResistances` component adds per-effect reduction (0-1, where 1 = immune); `StatusEffectImmunities` blocks specific effects.

## Skill mappings (Mage defaults)
- Firebolt: 30% Burn (3 dmg/sec, 2s)
- Fireball: 80% Burn (5 dmg/sec, 4s)
- Flame Wave: 100% Burn (4 dmg/sec, 3s)
- Frost Bolt: 50% Slow (50%, 1.5s)
- Frost Nova: 100% Freeze (70%, 2s)
- Blizzard: 100% Slow per hit (60%, 1s)

## Debug & visuals
- Status VFX/SFX react to applied/tick/expired events with color-coded cues.
- New debug overlay (`StatusEffectDebugSystem`, toggle F10) renders active effects above entities for quick inspection.
- `StatusEffectInspector` helper dumps active effect state for tooling/logs.

## Ordering
- Update order: Skill execution → Status apply → Status tick → Stat recalculation → AI/movement/combat.
- Status VFX runs alongside VFX/SFX systems; status damage uses unified damage events but skips hit-stop/flash spam.

## Testing
- New tests cover DoT ticking, stacking, slow-driven move-speed reduction, resistance/immune paths, and poison ramping.

