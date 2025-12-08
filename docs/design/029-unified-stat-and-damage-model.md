# Unified Stat and Damage Model

**Implementation Date:** December 7, 2024  
**Task:** #029

## Overview

The game now uses a unified stat and damage calculation system that applies consistently across all combat interactions (melee, projectiles, contact damage, knockback). This replaces the previous ad-hoc damage calculations with a comprehensive model that supports:

- Power scaling, attack speed, and cooldown reduction
- Critical hits with configurable chance and multipliers
- Armor and arcane resistance with diminishing returns
- Damage types (Physical, Arcane, True)
- Stat modifiers with clear stacking rules (additive → multiplicative)
- Deterministic random number generation for reproducible combat

## Core Components

### Stat Components

#### `OffensiveStats`
Defines offensive capabilities:
- **Power**: Base damage multiplier (default: 1.0)
- **AttackSpeed**: Attack rate multiplier (default: 1.0)
- **CritChance**: Critical hit chance, 0-100% (default: 0.0)
- **CritMultiplier**: Damage multiplier on crit (default: 1.5)
- **CooldownReduction**: Ability cooldown reduction, clamped at 80% (default: 0.0)

#### `DefensiveStats`
Defines defensive capabilities:
- **Armor**: Physical damage reduction using diminishing returns formula
- **ArcaneResist**: Arcane damage reduction using diminishing returns formula

#### `StatModifiers`
Temporary or permanent stat changes with clear stacking:
- **Additive modifiers**: Added to base stats first
- **Multiplicative modifiers**: Applied after additive, stacked multiplicatively

#### `ComputedStats`
Cached effective stats to avoid per-frame recalculation:
- Marked dirty when equipment/perks change
- Recalculated by `StatRecalculationSystem` before combat systems run
- Includes effective values for all offensive, defensive, and movement stats

## Damage Calculation Pipeline

All damage goes through a unified pipeline via `DamageCalculator`:

1. **Base Damage** → Apply attacker's **Power** multiplier
2. **Crit Roll** → If CanCrit flag set, roll against CritChance
3. **Crit Multiplier** → If crit, multiply by CritMultiplier
4. **Resistance** → Calculate reduction based on damage type
   - Physical damage: `armor / (armor + 100)`, clamped at 90%
   - Arcane damage: `resist / (resist + 100)`, clamped at 90%
   - True damage: Bypasses all resistances
5. **Apply Reduction** → `finalDamage = damage * (1 - reduction)`
6. **Clamp** → Ensure non-negative damage

### Damage Types

```csharp
[Flags]
enum DamageType {
    Physical = 1 << 0,  // Reduced by Armor
    Arcane = 1 << 1,    // Reduced by ArcaneResist
    True = 1 << 2,      // Bypasses all resistances
}
```

### Damage Flags

```csharp
[Flags]
enum DamageFlags {
    CanCrit = 1 << 0,       // Can critically strike
    IgnoreArmor = 1 << 1,   // Bypass armor (not resist)
    IgnoreResist = 1 << 2,  // Bypass resist (not armor)
}
```

## Resistance Formula

Both Armor and Arcane Resist use the same diminishing returns formula:

```
reduction = stat / (stat + 100)
```

Clamped at 90% maximum reduction.

**Examples:**
- 50 armor/resist = 33% reduction
- 100 armor/resist = 50% reduction
- 200 armor/resist = 67% reduction
- 10000 armor/resist = 90% reduction (clamped)

This ensures that:
1. Each point of armor/resist always has value
2. No full immunity is possible
3. Scaling is predictable and tunable

## Stat Stacking

Modifiers stack according to clear rules:

```csharp
effectiveStat = (base + additive) * multiplicative
```

**Additive modifiers**: Sum together  
**Multiplicative modifiers**: Multiply together  

**Example:**
- Base Power: 1.0
- +0.5 Power (from perk)
- +0.3 Power (from equipment)
- ×1.2 Power (from buff)
- ×1.5 Power (from ultimate)

```
Effective Power = (1.0 + 0.5 + 0.3) * 1.2 * 1.5 = 3.24
```

## Attack Speed and Cooldown

Attack speed and cooldown reduction are applied in order:

```csharp
effectiveCooldown = baseCooldown * (1 - CDR) / attackSpeed
```

Minimum cooldown is clamped at 0.05 seconds.

**Example:**
- Base cooldown: 1.0s
- 50% CDR → 0.5s
- 2.0 attack speed → 0.25s final cooldown

## Combat Systems Integration

All combat systems now use `DamageApplicationService`:

### MeleeHitSystem
- Physical damage, can crit
- Uses attacker's offensive stats
- Target's defensive stats reduce damage

### ProjectileHitSystem
- Arcane damage, can crit
- Fired by ranged enemies
- Same stat calculation as melee

### ContactDamageSystem
- Physical damage, can crit
- Applied on collision with cooldown
- Follows same damage pipeline

## Deterministic RNG

`CombatRng` provides deterministic random rolls:

```csharp
var rng = new CombatRng(seed);
var isCrit = rng.RollCrit(critChance);
```

- Uses Linear Congruential Generator (LCG)
- Same seed produces same sequence
- Platform-independent
- Fast and suitable for fixed timestep simulation

## Default Values

### Player Starting Stats
```csharp
OffensiveStats:
  Power: 1.0
  AttackSpeed: 1.0
  CritChance: 0.05 (5%)
  CritMultiplier: 1.5
  CooldownReduction: 0.0

DefensiveStats:
  Armor: 0
  ArcaneResist: 0
```

### Enemy Starting Stats
```csharp
OffensiveStats:
  Power: 1.0
  AttackSpeed: 1.0
  CritChance: 0.0 (enemies don't crit by default)
  CritMultiplier: 1.5
  CooldownReduction: 0.0

DefensiveStats:
  Armor: 0
  ArcaneResist: 0
```

## Debug/Inspection

`StatInspector` provides debug output for playtesting:

```csharp
// Inspect entity stats
var statsText = StatInspector.InspectStats(world, entity);
Console.WriteLine(statsText);

// Simulate damage calculation
var damageText = StatInspector.SimulateDamage(
    damageInfo,
    attackerOffense,
    defenderDefense,
    seed: 12345);
Console.WriteLine(damageText);
```

Output includes:
- Base and effective stats
- Modifier breakdown
- Damage calculations with step-by-step breakdown
- Effective DPS estimates

## Future Integration

This system is designed to support future features:

- **Task 030 (Loot/Equipment)**: Items can add stat modifiers directly
- **Task 031 (Talents/Perks)**: Perks modify StatModifiers component
- **Task 032 (Elites/Bosses)**: Special enemies can have higher stats or unique modifiers

When adding equipment or perks:
1. Update entity's `StatModifiers` component
2. Mark `ComputedStats` as dirty
3. `StatRecalculationSystem` will refresh effective stats next frame

## Testing

Comprehensive test coverage includes:

### DamageCalculatorTests
- Power scaling
- Critical hits
- Armor/resist reduction
- Damage type interactions
- True damage bypassing resistances
- Full pipeline integration

### StatCalculatorTests
- Modifier stacking (additive + multiplicative)
- Stat clamping (crit, CDR)
- Cooldown calculations
- Stat recalculation

### CombatRngTests
- Deterministic behavior
- Distribution validation
- Crit roll accuracy

All tests use deterministic seeds for reproducibility.

## Performance Considerations

- `ComputedStats` caching prevents per-frame recalculation
- `StatRecalculationSystem` only updates when `IsDirty = true`
- Damage application uses struct-based data flow (no allocations)
- `CombatRng` is fast (single multiplication + addition per roll)

## Migration Notes

Legacy `AttackStats.Damage` field remains for backward compatibility but is now used as base damage input to the unified system. All actual damage calculations go through `DamageCalculator`.

Systems creating damage should use:

```csharp
var damageInfo = new DamageInfo(
    baseDamage: attackStats.Damage,
    damageType: DamageType.Physical,
    flags: DamageFlags.CanCrit);

damageService.ApplyDamage(attacker, target, damageInfo, sourcePosition);
```

The service handles all stat lookups, calculations, and event publishing.
