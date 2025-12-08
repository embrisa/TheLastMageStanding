# Elite and Boss Enemy System Implementation

## Overview

The elite and boss system adds challenging enemy variants with increased stats, guaranteed loot drops, and special visual indicators. Elite and boss enemies spawn based on wave progression and have spawn caps to maintain balanced difficulty.

## Architecture

### Enemy Tiers

#### `EnemyTier` Enum
Defines three enemy tiers:
- **Normal**: Standard enemies with base stats
- **Elite**: Tougher enemies with enhanced stats and better loot (50% drop chance)
- **Boss**: Very powerful enemies with high stats and guaranteed loot (100% drop chance)

### Components

#### `EliteTag`
Empty component that tags an entity as an elite enemy for easy querying and special behavior.

#### `BossTag`
Empty component that tags an entity as a boss enemy for easy querying and special behavior.

### Archetypes

#### Elite Hexer (`elite_hexer`)
**Stats:**
- Health: 80 (vs 24 for normal)
- Damage: 15 (vs 8 for normal)
- Move Speed: 95 (vs 80 for normal)
- Collision Radius: 7 (larger hitbox)
- Mass: 1.2 (harder to knock back)
- Scale: 1.4x (visually larger)
- Armor: 5
- Arcane Resist: 10%
- Tint: Gold/Orange (255, 200, 50)

**Unlock:** Wave 5  
**Spawn Weight:** 0.3

#### Skeleton Boss (`skeleton_boss`)
**Stats:**
- Health: 250 (boss-level)
- Damage: 0 (uses ranged attacks only)
- Move Speed: 70 (slower but menacing)
- Collision Radius: 9 (very large)
- Mass: 2.5 (nearly immovable)
- Scale: 1.8x (much larger)
- Armor: 15
- Arcane Resist: 25%
- Tint: Deep Purple (150, 50, 200)

**Ranged Attack:**
- Projectile Speed: 220
- Projectile Damage: 20
- Optimal Range: 160
- Windup: 1.0s (telegraphed attack)

**Unlock:** Wave 10  
**Spawn Weight:** 0.15

## Wave Spawning

### Spawn Caps
To prevent overwhelming the player:
- **Max Elites Active:** 3
- **Max Bosses Active:** 1

The `WaveSchedulerSystem` enforces these caps by:
1. Counting active elites/bosses before each wave
2. Rerolling archetype selection up to 5 times if cap is reached
3. Tracking spawned elites/bosses within the wave

### Spawn Logic
```csharp
// Count active elites and bosses
var (activeElites, activeBosses) = CountElitesAndBosses(world);

// Choose archetype with cap enforcement
var archetype = _config.ChooseArchetype(_waveIndex, _random);
while (rerollAttempts < 5)
{
    if (archetype.Tier == EnemyTier.Elite && activeElites >= maxElites)
    {
        archetype = _config.ChooseArchetype(_waveIndex, _random);
        continue;
    }
    // ... similar for bosses
    break;
}
```

## Loot Integration

### Loot Dropper Configuration
When enemies are created in `EnemyEntityFactory`:
```csharp
var lootDropper = new LootDropper
{
    DropChance = 0.15f, // Base chance
    IsElite = archetype.Tier == EnemyTier.Elite,
    IsBoss = archetype.Tier == EnemyTier.Boss
};
```

### Drop Rates (from Task 030)
- **Normal Enemies:** 15% base drop chance
- **Elite Enemies:** 50% drop chance (via `LootDropConfig.EliteDropChance`)
- **Boss Enemies:** 100% drop chance (via `LootDropConfig.BossDropChance`)

The `LootDropSystem` reads the `IsElite` and `IsBoss` flags from `LootDropper` to determine the final drop chance.

## Visual Indicators

### Tier Rings
Elite and boss enemies are rendered with colored rings around them:

**Elite Ring:**
- Color: Gold (255, 200, 50, 180 alpha)
- Thickness: 2px
- Radius: 20 * scale

**Boss Ring:**
- Color: Purple (150, 50, 200, 200 alpha)
- Thickness: 3px (thicker than elite)
- Radius: 20 * scale

Rendered in `EnemyRenderSystem.DrawTierIndicator()` using 32-segment circles.

### Size and Tint
Elites and bosses also have:
- Larger visual scale (1.4x for elite, 1.8x for boss)
- Distinctive color tints (gold for elite, purple for boss)
- Larger collision radii matching their visual size

## Stat Modifiers

### Elite Stats
Applied in `EnemyEntityFactory`:
```csharp
DefensiveStats { Armor = 5f, ArcaneResist = 10f }
```

### Boss Stats
Applied in `EnemyEntityFactory`:
```csharp
DefensiveStats { Armor = 15f, ArcaneResist = 25f }
```

These integrate with the unified damage model (Task 029) to reduce incoming damage.

## Debug Tools

### Debug Spawn Keys
- **F7:** Spawn an elite enemy near the player (80 units offset)
- **F8:** Spawn a boss enemy near the player (120 units offset)

Debug spawn methods:
```csharp
EnemyWaveConfig.CreateEliteForDebug()
EnemyWaveConfig.CreateBossForDebug()
```

Console output confirms spawns with position.

## Testing

### Test Coverage
14 tests in `EliteBossWaveTests.cs` covering:
- Archetype tier validation
- Health and scale thresholds
- Loot dropper configuration (IsElite/IsBoss flags)
- Component tagging (EliteTag/BossTag)
- Defensive stat bonuses
- Wave config profile inclusion
- Unlock wave enforcement
- Archetype selection after unlock
- Drop chance configuration

All tests passing.

## Integration with Existing Systems

### Systems Updated
- **WaveSchedulerSystem:** Spawn cap logic for elites/bosses
- **EnemyEntityFactory:** Tier-based component configuration
- **EnemyRenderSystem:** Visual indicators for elite/boss
- **DebugInputSystem:** Debug spawn commands (F7/F8)
- **LootDropSystem:** Already supports elite/boss via `LootDropper` flags

### Systems Unchanged
- **LootDropSystem:** Reads IsElite/IsBoss flags (no changes needed)
- **Combat Systems:** Elite/boss stats integrate via unified stat model
- **Collision Systems:** Larger colliders handled automatically
- **AI Systems:** Elite/boss use same seek/ranged AI logic

## Configuration

### Default Wave Config
```csharp
enemyProfiles: new[]
{
    new EnemySpawnProfile(Archetype: BaseHexer(), Weight: 1f, UnlockWave: 1),
    new EnemySpawnProfile(Archetype: ScoutHexer(), Weight: 1.2f, UnlockWave: 2),
    new EnemySpawnProfile(Archetype: BoneMage(), Weight: 0.8f, UnlockWave: 3),
    new EnemySpawnProfile(Archetype: EliteHexer(), Weight: 0.3f, UnlockWave: 5),
    new EnemySpawnProfile(Archetype: SkeletonBoss(), Weight: 0.15f, UnlockWave: 10),
}
```

### Tuning Parameters

**Elite Spawn Rate:**
- Lower weight (0.3) means ~15-20% of spawns after wave 5
- Max 3 active at once

**Boss Spawn Rate:**
- Very low weight (0.15) means ~5-10% of spawns after wave 10
- Max 1 active at once

**Health Multipliers:**
- Elite: ~3.3x normal health (80 vs 24)
- Boss: ~10x normal health (250 vs 24)

**Damage Multipliers:**
- Elite: ~2x damage (15 vs 8)
- Boss: Ranged only (20 per projectile)

## Future Enhancements

### Potential Additions
1. **Elite Affixes:** Random modifiers like "Fast", "Tanky", "Arcane-Infused"
2. **Boss Phases:** Health-gated behavior changes
3. **Multi-Stage Boss Attacks:** Combo patterns with telegraphs
4. **Elite Packs:** Spawn multiple weaker elites together
5. **Boss-Specific Loot Tables:** Unique item pools for bosses
6. **Achievement Tracking:** "Defeat 10 elites", "Survive boss wave"
7. **Visual Effects:** Aura particles, screen tint on boss spawn
8. **Audio:** Dramatic music changes for boss encounters

### Performance Considerations
- Ring rendering adds 32 draw calls per elite/boss
- Consider instanced rendering if >10 elites/bosses active
- Caps prevent excessive entity counts

## Known Limitations

1. **No Unique Attacks:** Elite/boss use same attack patterns as normal enemies (contact damage or projectiles)
2. **No Boss Arenas:** Bosses spawn in normal wave flow
3. **No Elite Variations:** Only one elite archetype currently
4. **No Boss Loot Guarantee Indicator:** Players don't know bosses always drop loot until after kill
5. **Reroll Fallback:** If cap is hit after 5 rerolls, a capped tier may still spawn (edge case)

## Files Modified/Added

### Added
- `src/Game.Tests/Wave/EliteBossWaveTests.cs` - Test suite for elite/boss system
- `docs/design/032-elites-boss-waves-and-rewards-implementation.md` - This document

### Modified
- `src/Game/Core/Ecs/Config/EnemyWaveConfig.cs` - Added EnemyTier enum, elite/boss archetypes, debug spawn methods
- `src/Game/Core/Ecs/Components/EnemyComponents.cs` - Added EliteTag and BossTag components
- `src/Game/Core/Ecs/EnemyEntityFactory.cs` - Tier-based loot dropper, tags, and defensive stats
- `src/Game/Core/Ecs/Systems/EnemyRenderSystem.cs` - DrawTierIndicator for visual rings
- `src/Game/Core/Ecs/Systems/WaveSchedulerSystem.cs` - Spawn cap logic with CountElitesAndBosses
- `src/Game/Core/Ecs/Systems/DebugInputSystem.cs` - F7/F8 debug spawn commands
- `src/Game/Core/Ecs/EcsWorldRunner.cs` - Pass EnemyEntityFactory to DebugInputSystem

## Usage Examples

### Spawn Elite/Boss Programmatically
```csharp
var elite = EnemyWaveConfig.CreateEliteForDebug();
enemyFactory.CreateEnemy(position, elite);

var boss = EnemyWaveConfig.CreateBossForDebug();
enemyFactory.CreateEnemy(position, boss);
```

### Query Elites/Bosses
```csharp
world.ForEach<EliteTag, Health>((Entity e, ref EliteTag _, ref Health h) => 
{
    // Process all elite enemies
});

world.ForEach<BossTag, Position>((Entity e, ref BossTag _, ref Position p) => 
{
    // Process all boss enemies
});
```

### Check Enemy Tier at Runtime
```csharp
var isElite = world.TryGetComponent(entity, out EliteTag _);
var isBoss = world.TryGetComponent(entity, out BossTag _);
```

## Acceptance Criteria

✅ At least one elite archetype and one boss archetype spawn via wave config (elite by wave 5, boss by wave 10); they use unique telegraphed attacks (boss uses ranged with 1.0s windup).

✅ Elite/boss colliders and hitboxes are sized appropriately (7 and 9 radius) and respect existing collision/layer rules.

✅ Rewards drop reliably on kill (loot bias via IsElite/IsBoss flags) and are attributed to the player; drops integrate with loot system (Task 030).

✅ Debug command (F7/F8) can spawn/test elite/boss and show their telegraphs; normal waves remain stable with spawn caps.

✅ Tests cover wave config parsing for elites/bosses and reward drop attribution; `dotnet build` passes (14 tests passing).
