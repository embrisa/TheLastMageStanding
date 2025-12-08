# Perk Tree System - Quick Start Guide

## For Players

### Opening the Perk Tree
- Press **P** to open/close the perk tree

### Navigating
- **↑ / ↓** - Move between perks
- **Enter** - Allocate a point in the selected perk
- **Shift + R** - Reset all perks (refunds all points)
- **Escape / P** - Close the perk tree

### How It Works
1. **Earn Points**: Gain 1 perk point per level
2. **Allocate**: Spend points on perks (some require prerequisites)
3. **Effects**: Changes apply immediately to your stats
4. **Respec**: Reset all perks anytime for free

### Perk Tiers

#### Foundation (Always Available)
- **Vitality** - Increases max health
- **Arcane Mastery** - Increases damage
- **Swift Casting** - Increases attack speed

#### Intermediate (Requires Foundation Perks)
- **Arcane Armor** - Increases armor and magic resist
- **Critical Focus** - Increases crit chance and damage
- **Fleet Footed** - Increases movement speed

#### Advanced (Requires Intermediate Perks)
- **Piercing Projectiles** - Projectiles hit multiple enemies
- **Temporal Flux** - Reduces cooldowns

#### Capstone (Requires Advanced Perks)
- **Archmage's Might** - Massive power boost

## For Developers

### Adding a New Perk

```csharp
// In PerkTreeConfig.cs or custom config
new PerkDefinition
{
    Id = "my_custom_perk",
    Name = "My Custom Perk",
    Description = "Does something cool",
    MaxRank = 3,
    PointsPerRank = 1,
    Prerequisites = new()
    {
        new("core_power", 2)  // Requires "core_power" at rank 2
    },
    GridPosition = (2, 1),  // Row 2, Column 1
    EffectsByRank = new()
    {
        [1] = new() { PowerAdditive = 0.2f, ArmorAdditive = 10f },
        [2] = new() { PowerAdditive = 0.4f, ArmorAdditive = 20f },
        [3] = new() { PowerAdditive = 0.6f, ArmorAdditive = 30f }
    }
}
```

### Available Effect Properties

#### Stat Modifiers (Additive)
- `PowerAdditive` - Base damage multiplier
- `AttackSpeedAdditive` - Attack rate
- `CritChanceAdditive` - Crit chance (0.05 = 5%)
- `CritMultiplierAdditive` - Crit damage bonus
- `CooldownReductionAdditive` - CDR (0.1 = 10%)
- `ArmorAdditive` - Physical damage reduction
- `ArcaneResistAdditive` - Magic damage reduction
- `MoveSpeedAdditive` - Movement speed
- `HealthAdditive` - Max health

#### Stat Modifiers (Multiplicative)
- `PowerMultiplicative` - Power multiplier (1.5 = +50%)
- `AttackSpeedMultiplicative` - Attack speed multiplier
- `MoveSpeedMultiplicative` - Movement speed multiplier

#### Gameplay Modifiers
- `ProjectilePierceBonus` - Number of additional enemies hit
- `ProjectileChainBonus` - Chain lightning bounces
- `DashCooldownReduction` - Dash cooldown reduction

### Using Perk Effects in Your Systems

```csharp
// Check if player has gameplay modifiers
if (world.TryGetComponent<PerkGameplayModifiers>(player, out var modifiers))
{
    var totalPierce = basePierce + modifiers.ProjectilePierceBonus;
    // Use totalPierce in projectile logic
}
```

### Creating a Custom Perk Tree

```csharp
var config = new PerkTreeConfig(
    perks: myPerkList,
    pointsPerLevel: 2,    // Give 2 points per level
    respecCost: 100       // Cost 100 gold to respec
);

var service = new PerkService(config);
```

### Listening to Perk Events

```csharp
world.EventBus.Subscribe<PerkAllocatedEvent>(evt =>
{
    Console.WriteLine($"Perk {evt.PerkId} allocated to rank {evt.NewRank}");
});

world.EventBus.Subscribe<PerksRespecedEvent>(evt =>
{
    Console.WriteLine("All perks reset!");
});
```

### Testing Perks

```csharp
[Fact]
public void MyPerk_Works()
{
    var config = PerkTreeConfig.Default;
    var service = new PerkService(config);
    var playerPerks = new PlayerPerks();
    var perkPoints = new PerkPoints(10, 10);

    // Allocate perk
    service.Allocate("my_perk", ref playerPerks, ref perkPoints);

    // Check effects
    var effects = service.CalculateTotalEffects(playerPerks);
    Assert.Equal(expectedValue, effects.PowerAdditive);
}
```

## Architecture

```
Player Levels Up
    ↓
PerkPointGrantSystem (grants points)
    ↓
Player Opens Perk UI (press P)
    ↓
PerkTreeUISystem (navigation, allocation)
    ↓
PerkService.Allocate (validation, spending)
    ↓
PerkAllocatedEvent published
    ↓
PerkEffectApplicationSystem (applies effects)
    ↓
StatModifiers updated
    ↓
ComputedStats.IsDirty = true
    ↓
StatRecalculationSystem (recalculates stats)
    ↓
Player stats updated!
```

## Persistence

Perks are auto-saved every 30 seconds and when changed. Save location:
```
%LocalAppData%/TheLastMageStanding/current_run_perks.json
```

Save is cleared on run restart to keep progression run-specific.

## Performance Notes

- Effect calculation only happens on perk changes (event-driven)
- Stat recalculation uses dirty flag system (not per-frame)
- UI only renders when perk tree is open
- Persistence is throttled to 30-second intervals

## Common Issues

### "Can't allocate perk"
- Check you have enough perk points
- Check prerequisites are met (hover to see requirements)
- Check perk isn't at max rank already

### "Stats not updating"
- Perks apply immediately via event system
- If stats seem wrong, check `ComputedStats` in debug

### "Perks reset on restart"
- This is intentional! Perks are per-run progression
- For cross-run persistence, see Task 037 (Meta Progression)

## See Also

- **Design Doc**: `docs/design/031-talent-perk-tree-implementation.md`
- **Task Notes**: `tasks/031-talent-perk-tree.md`
- **Tests**: `src/Game.Tests/Perks/`
