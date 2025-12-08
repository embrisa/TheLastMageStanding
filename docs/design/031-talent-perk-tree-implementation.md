# Talent/Perk Tree System

**Implementation Date:** December 8, 2025  
**Task:** #031

## Overview

The perk/talent tree provides a branching progression system where players spend points earned from leveling up to unlock passive bonuses and gameplay modifiers. The system integrates with the unified stat model (Task 029) and supports prerequisites, rank caps, respec functionality, and within-run persistence.

## Core Components

### Data Model

#### `PerkDefinition`
Defines a single perk node with:
- **Id**: Unique identifier (e.g., "core_power")
- **Name**: Display name (e.g., "Arcane Mastery")
- **Description**: Effect description
- **MaxRank**: Maximum allocatable ranks (supports multi-rank perks)
- **PointsPerRank**: Cost per rank
- **Prerequisites**: List of required perks with minimum ranks
- **EffectsByRank**: Dictionary mapping rank → `PerkEffects`
- **GridPosition**: UI layout hint (row, column)

#### `PerkEffects`
Defines effects granted at a specific rank:
- **Stat Modifiers** (additive): Power, AttackSpeed, CritChance, CritMultiplier, CooldownReduction, Armor, ArcaneResist, MoveSpeed, Health
- **Stat Modifiers** (multiplicative): Power, AttackSpeed, MoveSpeed
- **Gameplay Modifiers**: ProjectilePierceBonus, ProjectileChainBonus, DashCooldownReduction

Effects combine via:
```csharp
effectiveStat = (base + additive) * multiplicative
```

#### `PlayerPerks`
Component storing allocated perks:
```csharp
Dictionary<string, int> AllocatedRanks  // perkId → current rank
```

#### `PerkPoints`
Component tracking available/spent points:
```csharp
int AvailablePoints
int TotalPointsEarned
```

#### `PerkGameplayModifiers`
Component storing gameplay-specific modifiers:
```csharp
int ProjectilePierceBonus
int ProjectileChainBonus
float DashCooldownReduction
```

### Services

#### `PerkService`
Core logic for perk allocation and validation:
- **CanAllocate**: Validates prerequisites, points, and rank caps
- **Allocate**: Allocates a rank and spends points
- **Deallocate**: Removes a rank with dependency checks (prevents orphaning dependent perks)
- **RespecAll**: Clears all perks and refunds points
- **CalculateTotalEffects**: Aggregates effects from all allocated perks

#### `PerkPersistenceService`
Saves/loads perk state to JSON:
- Save location: `%LocalAppData%/TheLastMageStanding/current_run_perks.json`
- Clears on run restart

## Systems

### `PerkPointGrantSystem`
- Subscribes to `PlayerLeveledUpEvent`
- Grants `PointsPerLevel` (default: 1) on each level gain
- Publishes `PerkPointsGrantedEvent` for UI notifications

### `PerkEffectApplicationSystem`
- Subscribes to `PerkAllocatedEvent` and `PerksRespecedEvent`
- Recalculates total perk effects via `PerkService.CalculateTotalEffects`
- Applies effects to `StatModifiers`, `Health.Max`, and `PerkGameplayModifiers`
- Marks `ComputedStats.IsDirty = true` to trigger stat recalculation

**Note:** Health bonus is applied as a delta to maintain health ratio. System tracks previous health bonus internally to avoid double-application on respec.

### `PerkAutoSaveSystem`
- Auto-saves every 30 seconds and on perk allocation/respec
- Loads saved perks on system initialization
- Clears save on `SessionRestartedEvent`

### `PerkTreeUISystem`
- Toggle with **P** key
- Navigation: **↑↓** to select perks
- Allocation: **Enter** to allocate selected perk
- Respec: **Shift+R** to reset all perks (free)
- Displays:
  - Perk name, current rank, max rank
  - Point cost
  - Prerequisites (color-coded)
  - Allocation status (green = can allocate, blue = allocated, gray = locked)
  - Selected perk description and prerequisites
  - Feedback messages (success/error)

## Default Perk Tree

The default mage tree includes:

### Foundation Tier (Row 0)
- **Vitality** (3 ranks): +20 max health per rank
- **Arcane Mastery** (5 ranks): +0.2 Power per rank
- **Swift Casting** (3 ranks): +0.1 AttackSpeed per rank

### Intermediate Tier (Row 1)
- **Arcane Armor** (3 ranks): +10 Armor and ArcaneResist per rank (requires Vitality 2)
- **Critical Focus** (3 ranks): +5% CritChance and +0.1 CritMultiplier per rank (requires Arcane Mastery 2)
- **Fleet Footed** (3 ranks): +10 MoveSpeed per rank (requires Swift Casting 1)

### Advanced Tier (Row 2)
- **Piercing Projectiles** (2 ranks, 2 pts/rank): +1 projectile pierce per rank (requires Critical Focus 2 and Swift Casting 2)
- **Temporal Flux** (2 ranks, 2 pts/rank): +10% CooldownReduction per rank (requires Swift Casting 3)

### Capstone Tier (Row 3)
- **Archmage's Might** (1 rank, 3 pts): +50% Power multiplier (requires Critical Focus 3 and Piercing Projectiles 1)

## Integration with Unified Stat Model

Perks modify stats via the `StatModifiers` component, which flows through `StatRecalculationSystem` (Task 029):

1. **Perk allocated** → `PerkEffectApplicationSystem` recalculates total effects
2. **Effects applied to** `StatModifiers` component
3. **Mark** `ComputedStats.IsDirty = true`
4. **`StatRecalculationSystem`** computes effective stats from base + modifiers
5. **Combat systems** read effective stats from `ComputedStats`

**Stacking with Equipment:**
Currently, perk modifiers overwrite the `StatModifiers` component. For proper stacking with equipment/buffs, future integration should:
- Track modifier sources separately (perks, equipment, buffs)
- Combine modifiers from all sources before setting `StatModifiers`

## Persistence

### Within-Run Persistence
- Auto-saves every 30 seconds
- Saves on perk allocation/respec
- Loads on game start if save exists
- Clears on run restart

### Save Format
```json
{
  "AvailablePoints": 3,
  "TotalPointsEarned": 10,
  "AllocatedRanks": {
    "core_power": 3,
    "core_speed": 2,
    "crit_mastery": 1
  }
}
```

## Events

### `PerkPointsGrantedEvent`
Published when points are granted (e.g., level-up):
```csharp
Entity Player
int PointsGranted
int TotalAvailable
```

### `PerkAllocatedEvent`
Published when a perk is allocated or deallocated:
```csharp
Entity Player
string PerkId
int NewRank
```

### `PerksRespecedEvent`
Published when all perks are reset:
```csharp
Entity Player
```

## Input Bindings

- **P**: Toggle perk tree UI
- **↑↓**: Navigate perk list
- **Enter**: Allocate selected perk
- **Shift+R**: Respec all perks
- **Escape/P**: Close perk tree

## Testing

### `PerkServiceTests` (24 tests)
- Prerequisite validation
- Point spending and refunds
- Rank caps
- Allocation/deallocation
- Respec logic
- Effect combination (additive and multiplicative)
- Dependency checks (prevents orphaning)
- Complex prerequisite chains

### `PerkPersistenceTests` (5 tests)
- Save/load perk state
- Round-trip data integrity
- Empty state handling
- File cleanup

All tests pass; build succeeds cleanly.

## Known Limitations

1. **Modifier Stacking**: Perks currently overwrite `StatModifiers` instead of combining with equipment/buffs. Proper multi-source modifier tracking needed for full integration.
2. **UI Layout**: Perks displayed as a vertical list instead of tree visualization (grid layout deferred).
3. **Respec Cost**: Currently free (config supports cost but UI doesn't enforce it).
4. **Health Bonus Application**: Uses internal dictionary to track previous bonus; could be migrated to component-based tracking.
5. **Perk Points on Restart**: Currently resets; future meta-progression may persist some points across runs.

## Future Enhancements

- **Multiple Trees**: Support for class-specific trees (fire/arcane/frost specializations).
- **Visual Tree Layout**: Grid-based UI with node connections.
- **Perk Icons**: Replace text-only display with icons.
- **Respec Cost**: Enforce configurable respec cost.
- **Meta Progression**: Persist some perk points/unlocks across runs (Task 037).
- **Shared Modifier Pool**: Combine perk/equipment/buff modifiers properly.
- **Animation Effects**: Visual feedback on perk allocation.
- **Tooltips**: Rich tooltips with stat deltas and effect breakdowns.

## Usage Examples

### Defining a Custom Perk Tree
```csharp
var config = new PerkTreeConfig(
    new List<PerkDefinition>
    {
        new()
        {
            Id = "my_perk",
            Name = "My Perk",
            Description = "Does cool things",
            MaxRank = 3,
            PointsPerRank = 1,
            Prerequisites = new() { new("other_perk", 2) },
            EffectsByRank = new()
            {
                [1] = new() { PowerAdditive = 0.1f },
                [2] = new() { PowerAdditive = 0.2f },
                [3] = new() { PowerAdditive = 0.3f }
            }
        }
    },
    pointsPerLevel: 1,
    respecCost: 10
);
```

### Allocating Perks Programmatically
```csharp
var service = new PerkService(config);
var playerPerks = new PlayerPerks();
var perkPoints = new PerkPoints(5, 5);

if (service.Allocate("my_perk", ref playerPerks, ref perkPoints))
{
    // Success
    world.SetComponent(player, playerPerks);
    world.SetComponent(player, perkPoints);
    world.EventBus.Publish(new PerkAllocatedEvent(player, "my_perk", 1));
}
```

### Respeccing Perks
```csharp
service.RespecAll(ref playerPerks, ref perkPoints);
world.SetComponent(player, playerPerks);
world.SetComponent(player, perkPoints);
world.EventBus.Publish(new PerksRespecedEvent(player));
```

## Performance Considerations

- **Effect Calculation**: Only recalculated on perk changes (event-driven), not per-frame
- **Stat Recalculation**: Leverages existing dirty-flag system from Task 029
- **UI Rendering**: Only draws when perk tree is open
- **Persistence**: Auto-save throttled to 30-second intervals

## Migration Notes

Systems creating/modifying player stats should continue using the unified stat model. Perks integrate seamlessly via `StatModifiers` component.

When adding future systems that modify stats (buffs, equipment, debuffs):
1. Create a modifier tracking system that aggregates sources
2. Combine all sources into final `StatModifiers` component
3. Mark `ComputedStats.IsDirty = true`
4. Let `StatRecalculationSystem` compute effective stats
