# Meta Progression Foundations - Design Document

## Overview
This document describes the foundational data models, persistence layer, and calculation systems for meta progression in The Last Mage Standing. Meta progression provides persistent player advancement across multiple runs through meta levels, gold currency, equipment collection, and talent unlocks.

## Architecture

### Core Components

#### Data Models (`Core/MetaProgression/`)

**PlayerProfile** - Persistent player data
- Meta level and total XP
- Gold currency balance
- Equipment inventory (permanent collection)
- Equipped loadout (weapon, armor, accessories)
- Unlocked talent nodes and skills
- Aggregate statistics (best wave, total kills, playtime, etc.)
- Schema versioning for migration support

**RunSession** - Tracks statistics during a single gameplay run
- Wave progression, kills, damage dealt/taken
- Gold collected and equipment found
- Start/end timestamps and duration
- Calculated meta XP earned
- Cause of death

**EquipmentItem** - Equipment data model
- Type (weapon, armor, accessory)
- Rarity tier (common, uncommon, rare, epic, legendary)
- Stats (damage, armor, health, speed, crit chance/damage)
- Icon reference and description

#### Services

**PlayerProfileService** - Profile persistence with robustness features
- JSON serialization with indented formatting
- Atomic writes (temp file + rename pattern)
- Automatic backup system (rolling window of 3 backups)
- Corruption handling with backup restoration
- Platform-specific save directory detection
  - Windows: `%AppData%\TheLastMageStanding`
  - macOS: `~/Library/Application Support/TheLastMageStanding`
  - Linux: `~/.local/share/TheLastMageStanding`

**RunHistoryService** - Run history persistence and querying
- Maintains last 50 runs in rolling window
- Query methods: recent runs, best runs, personal records
- JSON serialization

**MetaProgressionCalculator** - XP and level formulas (static utility)
- Calculates meta XP from run performance
- Determines level from total XP
- Provides XP threshold and progress queries
- Gold reward calculation

**MetaProgressionManager** - Integration coordinator
- Manages profile loading/saving
- Tracks current run session
- Subscribes to game events (run start/end, gold collected, etc.)
- Calculates and applies rewards at run end
- Publishes meta progression events

#### Events

**MetaProgressionEvents** - Event definitions
- `RunStartedEvent` - New run begins
- `RunEndedEvent` - Run completed
- `GoldCollectedEvent` - Gold picked up during run
- `MetaXpGainedEvent` - Meta XP awarded
- `MetaLevelUpEvent` - Meta level increased
- `EquipmentCollectedEvent` - Equipment added to inventory

### Integration Points

**EcsWorldRunner** - Game initialization and loop
- Creates `MetaProgressionManager` at startup
- Manager subscribes to event bus
- Publishes `RunStartedEvent` when session entity created
- Existing systems publish events (SessionRestartedEvent, PlayerDiedEvent, etc.)

**Event Flow**
1. Game starts → `RunStartedEvent` → `MetaProgressionManager` creates `RunSession`
2. During gameplay → Events update run statistics (kills, gold, damage)
3. Player dies or quits → `RunEndedEvent` → Finalize run, calculate rewards, save profile

## Formulas

### Meta XP Calculation

```
base_xp = wave_reached^1.5 * 100
kill_bonus = total_kills * 5
gold_bonus = gold_collected * 2
damage_bonus = damage_dealt / 1000
time_multiplier = max(0, 1 - (run_duration_minutes / 60))
meta_xp = (base_xp + kill_bonus + gold_bonus + damage_bonus) * (1 + time_multiplier * 0.5)
```

**Notes:**
- Base XP scales exponentially with wave progression (primary factor)
- Kill/gold/damage bonuses provide incremental rewards
- Time multiplier rewards efficient runs (up to 50% bonus)
- Minimum 1 XP guaranteed

### Level Calculation

```
xp_for_level_n = 1000 * (n^1.8)
```

**Examples:**
- Level 2: 1,000 XP
- Level 3: 3,482 XP
- Level 5: 9,549 XP
- Level 10: 39,811 XP

### Gold Rewards

```
base_gold = wave_reached * 10
kill_gold = total_kills * 2
milestone_bonus:
  - Wave 10+: +50 gold
  - Wave 20+: +100 gold (cumulative: 150)
  - Wave 30+: +200 gold (cumulative: 350)
```

## Persistence Format

### PlayerProfile JSON Schema (v1)

```json
{
  "SchemaVersion": 1,
  "MetaLevel": 5,
  "TotalMetaXp": 10000,
  "TotalGold": 500,
  "EquipmentInventory": [
    {
      "Id": "sword_01",
      "Name": "Rusty Sword",
      "Type": "Weapon",
      "Rarity": "Common",
      "Damage": 10.0,
      "Armor": 0.0,
      "Health": 0.0,
      "SpeedMultiplier": 1.0,
      "CritChance": 0.0,
      "CritDamage": 1.5,
      "IconPath": "Items/Weapons/sword_01",
      "Description": "A worn but functional blade",
      "GoldCost": 50
    }
  ],
  "EquippedWeaponId": "sword_01",
  "EquippedArmorId": null,
  "EquippedAccessoryIds": [],
  "UnlockedTalentNodes": ["node_01", "node_02"],
  "UnlockedSkillIds": ["fireball", "frost_nova"],
  "TotalRuns": 10,
  "BestWave": 15,
  "TotalKills": 500,
  "TotalDamageDealt": 50000.0,
  "TotalPlaytime": "02:30:00",
  "CreatedAt": "2025-12-08T10:00:00Z",
  "LastPlayedAt": "2025-12-08T12:30:00Z"
}
```

### RunHistory JSON Schema

```json
[
  {
    "StartTime": "2025-12-08T12:00:00Z",
    "EndTime": "2025-12-08T12:15:00Z",
    "WaveReached": 10,
    "TotalKills": 100,
    "TotalDamageDealt": 10000.0,
    "TotalDamageTaken": 500.0,
    "GoldCollected": 250,
    "MetaXpEarned": 1500,
    "EquipmentFound": [],
    "SkillsUsed": ["fireball", "frost_nova"],
    "CauseOfDeath": "Defeated",
    "FinalLevel": 5,
    "RunId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890"
  }
]
```

## File Operations

### Save Operations
- **Atomic writes:** Write to temp file, then rename (prevents partial writes)
- **Backup creation:** Before overwriting, copy old file to timestamped backup
- **Backup rotation:** Keep last 3 backups, delete oldest
- **Backup naming:** `player_profile.backup_YYYYMMDD_HHMMSS.json`

### Load Operations
- **Missing file:** Return default profile (new player)
- **Corrupted file:** Attempt backup restoration, fallback to default
- **Schema migration:** Check SchemaVersion, apply migrations if needed

## Testing

### Test Coverage
- **MetaProgressionCalculatorTests** (28 tests)
  - XP calculation scenarios (wave progression, kills, time bonus)
  - Level calculation and thresholds
  - Gold reward formulas
  - Edge cases (zero/negative values, minimum rewards)

- **PersistenceTests** (14 tests)
  - Profile save/load cycle
  - Equipment persistence
  - Backup creation and restoration
  - Corrupted file handling
  - Run history queries and ordering

### In-Memory File System
- Testable abstraction (`IFileSystem` interface)
- `InMemoryFileSystem` implementation for unit tests
- `DefaultFileSystem` implementation for production

## Future Extensions

### Deferred to Future Tasks
- **Task 045:** Meta Hub UI & Scene
- **Task 046:** Shop & Equipment Purchase UI
- **Task 047:** Talent Tree Integration & Application
- **Task 048:** In-Run Inventory & Equipment Swapping
- **Task 049:** Run History & Stats Display UI

### Extension Points
- **Equipment stats:** Currently defined, not yet applied in combat
- **Talent unlocks:** Stored in profile, not yet integrated with perk tree
- **Skill unlocks:** List maintained, not yet connected to skill system
- **Gold economy:** Conservative values, tune based on playtest data
- **XP formulas:** Data-driven constants, easy to adjust

## Design Decisions

### Why Two-Tier Progression?
- **In-run XP (Task 022):** Short-term rewards, run-specific power curve
- **Meta XP:** Long-term persistence, account-wide advancement
- Distinct progression loops avoid conflation and provide clear goals

### Why Permanent Equipment Collection?
- All dropped equipment automatically added to inventory
- Never lose equipment across runs
- Encourages experimentation and build diversity
- Supports future shop purchases

### Why Gold Persists?
- Meta currency for shop/unlocks (Tasks 046-047)
- Reward consistency (every run contributes to long-term goals)
- Sink for gameplay decisions (purchase vs. save)

### Why Conservative Initial Values?
- Start low, iterate up based on playtests
- Easier to buff than nerf (player perception)
- Avoid runaway inflation early

## References

- Task 022: XP Orbs and Level-Ups (in-run XP system)
- Task 029: Unified Stat and Damage Model
- Task 030: Loot and Equipment Foundations
- Task 031: Talent/Perk Tree (Task 031-SUMMARY.md)
- Task 013: Event Bus & Intent System
