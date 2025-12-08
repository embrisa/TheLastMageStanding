# Loot and Equipment System

**Implementation Date:** December 8, 2024  
**Task:** #030

## Overview

The loot system provides item drops with rarity tiers, equipment slots, stat affixes, and inventory management. Items integrate with the unified stat model from Task 029 to provide meaningful character progression.

## Core Components

### Item Data Structures

#### `ItemRarity`
Five rarity tiers that determine visual presentation and affix counts:
- **Common**: 0-1 affixes, white color
- **Uncommon**: 1-2 affixes, green color
- **Rare**: 2-3 affixes, blue color
- **Epic**: 3-4 affixes, purple color
- **Legendary**: 4-5 affixes, orange color

#### `EquipSlot`
Equipment slots for items:
- **Weapon**: Main hand weapon
- **Armor**: Chest armor
- **Amulet**: Neck slot
- **Ring1/Ring2**: Ring slots

#### `ItemAffix`
Definition of a stat modifier that can roll on items:
- **Type**: Which stat is modified (e.g., PowerAdditive, CritChanceAdditive)
- **MinValue/MaxValue**: Roll range for the affix value

#### `RolledAffix`
An affix with a specific rolled value that applies to `StatModifiers`.

#### `ItemInstance`
A fully realized item with:
- Definition ID and display name
- Rarity, type, and equipment slot
- List of rolled affixes
- Unique instance ID for tracking

### Drop Tables

#### `DropTable<T>`
Generic weighted random selection:
```csharp
var table = new DropTable<ItemRarity>(entries);
var rarity = table.SelectRandom(rng);
```

#### `LootDropConfig`
Configuration for drop rates and item pools:
- **RarityTable**: Weights for each rarity tier
- **ItemTables**: Item pools per equipment slot
- **BaseDropChance**: 15% drop chance on normal enemies
- **EliteDropChance**: 50% drop chance on elites
- **BossDropChance**: 100% drop chance on bosses

### Item Registry

`ItemRegistry` provides default item definitions:
- **Weapons**: Arcane Staff, Mystic Wand
- **Armor**: Mage Robe, Shadow Cloak
- **Accessories**: Amulets and rings with various stat focuses

Each definition includes an affix pool that determines what stats can roll on that item type.

### Item Factory

`ItemFactory` generates item instances:
```csharp
var factory = new ItemFactory(definitions, config, rng);
var item = factory.GenerateItem(EquipSlot.Weapon);
```

- Selects item definition from slot table
- Rolls rarity from rarity table
- Rolls affixes based on rarity (no duplicates)
- Returns complete `ItemInstance`

## ECS Integration

### Components

#### `LootDropper`
Attached to enemies to enable loot drops:
```csharp
new LootDropper 
{ 
    DropChance = 0.15f,
    IsElite = false,
    IsBoss = false 
}
```

#### `DroppedLoot`
Represents a loot entity in the world:
- Item instance data
- Pickup cooldown (prevents instant pickup)
- Despawn timer (optional)

#### `Inventory`
Player inventory with max size limit (default: 50 items).

#### `Equipment`
Player equipped items by slot:
```csharp
equipment.EquipItem(item);
var previousItem = equipment.UnequipItem(EquipSlot.Weapon);
```

Tracks a `ModifiersDirty` flag to trigger stat recalculation.

#### `LootPickupRadius`
Pickup range for automatic collection (default: 32 units).

### Systems

#### `LootDropSystem`
Event-driven system that spawns loot on enemy death:
- Subscribes to death events
- Checks drop chance (base/elite/boss)
- Generates random item
- Spawns loot entity with collision

#### `LootPickupSystem`
Per-frame system that handles automatic pickup:
- Updates pickup cooldowns and despawn timers
- Checks distance to player
- Adds items to inventory
- Destroys loot entity
- Publishes `LootPickedUpEvent`

#### `EquipmentStatSystem`
Recalculates stats when equipment changes:
- Monitors `Equipment.ModifiersDirty` flag
- Combines stat modifiers from all equipped items
- Updates `StatModifiers` component
- Marks `ComputedStats` as dirty for recalculation
- Integrates with existing `StatRecalculationSystem`

#### `InventoryUiSystem`
Renders and handles input for inventory/equipment UI:
- Toggle with Tab or Y button
- Switch views with Q/E or LB/RB
- Navigate with arrow keys or D-pad
- Equip/unequip with Enter or A button
- Shows item name, rarity, slot, and affix count

## Stat Integration

Items modify stats via the unified stat model from Task 029:

### Affix Types
All affix types map to `StatModifiers` fields:
- **Offensive**: Power, AttackSpeed, CritChance, CritMultiplier, CooldownReduction
- **Defensive**: Armor, ArcaneResist
- **Movement**: MoveSpeed

### Modifier Stacking
Equipment modifiers combine with other sources (perks, buffs):
```csharp
effectiveStat = (base + additive) * multiplicative
```

### Example
```csharp
// Item with +0.3 Power and +0.1 Crit Chance
var affixes = new List<RolledAffix>
{
    new(AffixType.PowerAdditive, 0.3f),
    new(AffixType.CritChanceAdditive, 0.1f)
};

// Apply to player stats
var modifiers = item.CalculateStatModifiers();
world.SetComponent(player, modifiers);
// StatRecalculationSystem will compute effective stats
```

## Persistence

### Within-Run Persistence
`EquipmentAutoSaveSystem` auto-saves every 30 seconds:
- Saves equipped items and inventory to JSON
- Loads on game start if save exists
- Clears save on run end

### Save Location
```
%LocalAppData%/TheLastMageStanding/current_run_equipment.json
```

### Data Format
```json
{
  "EquippedItems": {
    "Weapon": { "DefinitionId": "weapon_staff", ... }
  },
  "InventoryItems": [ { ... }, { ... } ]
}
```

## Services

### `InventoryService`
High-level operations for inventory/equipment management:
```csharp
var service = new InventoryService(world);

// Equip item from inventory
var previousItem = service.EquipItem(player, item);

// Unequip item back to inventory
bool success = service.UnequipItem(player, EquipSlot.Weapon);

// Query inventory
var items = service.GetInventoryItems(player);
var equipped = service.GetEquippedItems(player);
```

### `EquipmentPersistenceService`
Save/load operations:
```csharp
var persistence = new EquipmentPersistenceService();
persistence.SaveEquipment(snapshot);
var loaded = persistence.LoadEquipment();
persistence.ClearSave();
```

## Events

### `LootDroppedEvent`
Published when loot spawns in the world:
- Loot entity
- Item instance
- Position

### `LootPickedUpEvent`
Published when player picks up loot:
- Player entity
- Item instance

### `ItemEquippedEvent`
Published when an item is equipped:
- Player entity
- New item
- Previous item (if any)

### `ItemUnequippedEvent`
Published when an item is unequipped:
- Player entity
- Item
- Equipment slot

## Configuration

### Drop Rates
Tunable in `LootDropConfig.CreateDefault()`:
- Normal enemies: 15%
- Elite enemies: 50%
- Bosses: 100%

### Rarity Weights
Default distribution:
- Common: 50%
- Uncommon: 30%
- Rare: 15%
- Epic: 4%
- Legendary: 1%

### Inventory Size
Default: 50 items  
Configure in `Inventory` component initialization.

## Visual Presentation

### Dropped Loot
- Collider for pickup detection (8-unit radius)
- Colored highlight based on rarity
- Collision layer: `Pickup`

### Inventory UI
- Semi-transparent overlay
- Item list with rarity colors
- Selected item highlighted
- Navigation hints at bottom
- Separate inventory and equipment views

## Testing

### Coverage
20 tests covering:
- Drop table weighted selection
- Affix rolling and application
- Item generation with rarity constraints
- Stat modifier calculation
- Persistence round-trip
- Default configuration validity

### Test Structure
```
Game.Tests/Loot/
  LootSystemTests.cs         - Drop tables, affixes, item generation
  EquipmentPersistenceTests.cs - Save/load functionality
```

## Integration Points

### Task 029 (Unified Stats)
Items modify `StatModifiers` → `StatRecalculationSystem` → `ComputedStats`

### Task 031 (Talents/Perks) - Future
Perks can add modifiers alongside equipment modifiers.

### Task 032 (Elites/Bosses) - Future
Set `IsElite` or `IsBoss` flags for higher drop rates.

## Known Limitations

- **No crafting or reroll**: Items are generated as-is
- **No item sets or unique effects**: Only stat modifiers
- **No visual item sprites**: Uses colored squares
- **No loot filters**: All loot is visible
- **Inventory is list-based**: Not grid-based like Tetris
- **Persistence is per-run only**: No cross-run meta progression yet

## Performance Considerations

- `ItemFactory` uses a single RNG instance
- Stat recalculation only triggers on `ModifiersDirty` flag
- Pickup system uses distance checks (O(n*m) players × loot)
- Auto-save runs every 30 seconds, not per-frame
- JSON serialization is cached with static options

## Future Extensions

1. **Visual polish**: Item sprites, pickup animations, better VFX
2. **Item tooltips**: Show stat comparisons when hovering
3. **Loot filters**: Hide low-rarity items
4. **Item sets**: Bonuses for wearing multiple pieces
5. **Unique items**: Special effects beyond stat modifiers
6. **Crafting system**: Reroll affixes, upgrade rarity
7. **Meta progression**: Persistent unlockable item bases
8. **Trading/vendors**: Buy/sell/trade items

## Usage Examples

### Add loot drops to enemies
```csharp
// In EnemyEntityFactory
world.SetComponent(enemy, new LootDropper 
{ 
    DropChance = 0.15f,
    IsElite = false 
});
```

### Register systems
```csharp
// In EcsWorldRunner
var itemFactory = new ItemFactory(
    new ItemRegistry().GetAllDefinitions(),
    LootDropConfig.CreateDefault());

_updateSystems.Add(new LootDropSystem(itemFactory, config));
_updateSystems.Add(new LootPickupSystem());
_updateSystems.Add(new EquipmentStatSystem());
_updateSystems.Add(new EquipmentAutoSaveSystem());
_updateSystems.Add(new InventoryUiSystem());
```

### Debug spawn items
```csharp
// Spawn a random item for testing
var factory = new ItemFactory(definitions, config);
var item = factory.GenerateItem(EquipSlot.Weapon);
SpawnLootEntity(item, playerPosition);
```
