# 036 - Elite modifiers & mutators

## Overview
- Elites and bosses can roll stack-safe modifiers once unlocked (wave-based). Modifiers attach via `EliteModifierData` and are resolved in factory/system code — no runtime randomization outside wave spawning/debug shortcuts.
- Telegraphed auras are applied directly to the enemy (long-lived `ActiveTelegraph`) with tint overlays for readability.
- Runtime behaviors live in `EliteModifierSystem` (lifesteal, shield regen, explosive death); projectile fan handled inside `RangedAttackSystem`.

## Modifier definitions
- **Extra Projectiles**  
  - Effect: ranged enemies fire 3-shot fan (±20°) instead of single shots.  
  - Visuals: orange aura telegraph, muzzle flash VFX on fire.  
  - Reward: 1.25×.
- **Vampiric**  
  - Effect: heals attacker for 30% of final damage dealt.  
  - Visuals: red aura telegraph, green heal VFX on heal.  
  - Reward: 1.25×.
- **Explosive Death**  
  - Effect: on death, spawn 1.5s telegraph then detonate 72px AoE for 28 arcane damage (hits player + enemies).  
  - Visuals: red/orange windup telegraph + impact VFX/SFX on detonate.  
  - Reward: 1.3×.
- **Shielded**  
  - Effect: flat shield (45 HP) absorbs damage before health, 2.5s regen delay, 12 HP/s regen.  
  - Visuals: blue aura telegraph; shield breaks silently (no VFX asset yet).  
  - Reward: 1.25×.
- Reward multipliers stack multiplicatively and cap at 2.0×; stored on `LootDropper.ModifierRewardMultiplier`.

## Spawn rules
- Config: `EliteModifierConfig` (default used by `EnemyWaveConfig.Default` + `WaveSchedulerSystem`).
- Unlocks: ExtraProjectiles@5, Vampiric@7, Shielded@8, ExplosiveDeath@10.
- Counts by wave:  
  - `<5`: 0, `5–11`: 1, `12–19`: up to 2, `>=20`: up to 3 (min 2 after wave 20).  
- Weights: ExtraProjectiles 1.1, Vampiric 1.0, ExplosiveDeath 1.0, Shielded 0.9. Duplicate mods blocked unless `AllowStacking` (none enabled yet).

## Systems & data flow
- **Factory**: `EnemyEntityFactory.CreateEnemy` accepts optional modifier list; attaches `EliteModifierData`, computes reward multiplier, applies aura telegraph/tint, and adds `EliteShield` for Shielded.
- **Runtime**:  
  - `EliteModifierSystem.OnEntityDamaged` → Vampiric heal.  
  - `HitReactionSystem` handles shield absorption before health damage and pauses further processing if fully absorbed.  
  - `EliteModifierSystem.Update` → shield regen + pending explosion timers → AoE damage via `DamageApplicationService`.  
  - `RangedAttackSystem` checks modifier data to fan projectiles.
- **Events**: `EnemyDiedEvent` now carries death position and copied modifier list so drops/XP/explosions can use data even after entity destruction.

## Rewards
- `LootDropper` gains `ModifierRewardMultiplier`; `LootDropSystem` multiplies drop chance (capped 0.95 for elites, 1.0 for bosses).  
- XP orbs still spawn via `XpOrbSpawnSystem` using death position from event.

## Debug & testing
- Debug hotkeys: `Shift+1/2/3/4` spawn an elite at player with ExtraProjectiles/Vampiric/ExplosiveDeath/Shielded; `F7`/`F8` spawn base elite/boss.
- Tests: `EliteModifierTests` cover modifier attachment/deduplication, vampiric heal, explosion spawn, and shield regen.

