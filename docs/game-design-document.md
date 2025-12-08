# The Last Mage Standing — Game Design Document

## 1. Vision & Pillars
- 2D isometric horde-survivor meets ARPG: survive waves, grow power, feel tactical mastery.
- Moment-to-moment readability: clear telegraphs, hit feedback, and camera stability.
- Deterministic, event-driven ECS so combat, VFX/SFX, and UI remain decoupled and testable.
- Scalable foundation: collision, hitboxes, projectiles, progression, and perks are additive.

## 2. Platform & Tech Stack
- .NET 9, MonoGame 3.8.4.1 (DesktopGL); fixed timestep 60 FPS.
- Virtual resolution 960x540, rendered to an off-screen target and scaled 2× (`Game1` render target path).
- Content pipeline via `Content.mgcb`; TMX maps built with MonoGame.Extended importer.
- Namespaces: `TheLastMageStanding.Game.*`; nullable on, latest C# features.

## 3. Core Loop
- Spawn-timed waves chase the player; player attacks with collider-driven melee and ranged projectiles (from enemies).
- Collisions drive combat: hitboxes/hurtboxes, contact damage, knockback, separation, and hit-stop.
- Defeat enemies → XP orbs → level-ups → stat growth; perks and loot hooks exist for expansion.
- Session flow: playing → death (game over overlay) → restart; pause/settings overlay available.

## 4. Camera, World, Maps
- **Camera**: `Camera2D` maintains view transform with optional shake offset; follows entity with `CameraTarget` component; shake applied as random offset during hit events.
- **Maps**: TMX format in `Content/Tiles/Maps`; loaded via MonoGame.Extended importer.
- **Map Selection**: Environment variable `TLMS_MAP=first` selects `FirstMap.tmx`, else defaults to `HubMap.tmx`.
- **Player Spawn**: Loaded from TMX object layer; looks for object named `player_start`; calculates position as object position + (size × 0.5) for center; fallback to Vector2.Zero if not found.
- **World Collision**: TMX collision/object layers parsed by `CollisionLoaderService`; filters objects with `type="collision"` or name containing "collision"/"wall"; creates static collider entities in ECS.
- **Render Order**: Map tiles (background) → world sprites (enemies, player, projectiles) → effects (VFX, telegraphs) → UI elements → render target → present scaled 2× to window.
- **Debug Overlay**: F3 toggles cyan collision shapes for static world geometry.

## 5. Input & Controls
### Core Gameplay (`Core/Input/InputState.cs`)
- **Movement**: WASD/Arrow keys normalized to unit vector.
- **Attack**: Left Mouse Button or **J** publishes `PlayerAttackIntentEvent`.
- **Dash/Evade**: Left Shift or Space triggers dash (0.2s, 150u, 2s cooldown) with 0.15s i-frames; 50ms input buffer during cooldown.
- **Pause**: Escape toggles pause overlay.
- **Restart**: R key resets run; Game Over screen also accepts Enter/Confirm to restart.

### UI Navigation
- **Menu Navigation**: Arrow keys/WASD + Enter/Space to confirm; Back/Escape to close submenus.
- **Pause Menu**: Resume, Restart, Settings (opens audio submenu), Quit.

### Progression & Debug
- **Inventory**: I key toggles inventory/equipment overlay.
- **Perk Tree**: P key toggles perk tree UI; arrow keys navigate perks; Enter allocates point.
- **Respec**: Shift+R performs full perk respec (refunds all points).

### Debug Toggles (Development)
- **F3**: Toggle collision/hitbox debug overlay (collider shapes, ownership lines, separation/knockback vectors).
- **F4**: Disable hit-stop (for testing without pauses).
- **F5**: Toggle camera shake on/off.
- **F6**: Toggle VFX/SFX rendering (for performance testing).
- **F7**: Spawn elite enemy at cursor (debug).
- **F8**: Spawn boss enemy at cursor (debug).
- **F9**: Toggle dash/i-frame overlay (trajectory line, invulnerable rings).
- **F11**: Toggle AI role/state overlay (state text + range circles).

## 6. ECS Architecture
- World: simple component pools, `ForEach` snapshot per component type; event bus owned by world.
- System contracts: `IUpdateSystem`, `IDrawSystem`, `ILoadContentSystem`; contexts carry `GameTime`, delta, input, camera.
- **Component Patterns**:
  - **Component pools**: World maintains `Dictionary<Type, List<Component>>` per component type.
  - **ForEach snapshots**: Iteration uses snapshot of component list to allow mid-loop entity creation/destruction.
  - **Dirty flags**: Used in `EffectiveStats`, `StatModifiers` to trigger recalculations.
- **Factory Pattern**: `PlayerEntityFactory`, `EnemyEntityFactory`, `DebugEntityFactory` construct entities with full component sets.
- **Service Pattern**: `PerkService` (allocation/validation), `InventoryService` (inventory management), `MusicService` (playback), `DamageApplicationService` (damage events), `DamageCalculator` (damage math).
- **Persistence Pattern**: Store classes handle JSON save/load (`AudioSettingsStore`, `PerkPersistence`, `EquipmentPersistence`); auto-save systems run every 30s.
- Update ordering (key points from `EcsWorldRunner`):
  - Session/pause handling always first; hit-stop runs before gameplay and gates logic.
  - Input → stat recalcs → wave scheduling → spawn → AI (ranged, seek) → intents → projectile update.
  - Knockback → static collision resolution → collision detection → dynamic separation → contact damage.
  - Melee hitboxes/projectile hits → animation events → combat cooldowns → hit-stop/VFX/SFX/telegraphs → reactions/effects.
  - Movement integration → renders → camera follow → damage numbers → XP spawn/collect/level-up → perks → loot spawn/collect → equipment → cleanup → intent reset.
- Draw ordering: enemies, player, debug, projectiles, telegraphs/VFX, damage numbers, XP orbs, loot, collision debug.
- UI draw: HUD, perk tree UI, inventory UI.
- Hit-stop: during pause windows only VFX/SFX/telegraph/hit effects tick; movement/combat halted.

## 7. Event Bus
- Typed, queued events with per-type queues, processed up to 10 passes/frame to drain cascades.
- Core gameplay events: collision enter/stay/exit, damage, player attack intent, wave start/complete, player died, session restarted, XP/level, loot/perk events, VFX/SFX.
- Systems subscribe/unsubscribe directly; duplicate subscriber guard; frame-safe dispatch.

## 8. World Entities & Components
### Player (`PlayerEntityFactory`)
- Faction: Player; Position at spawn (map start); Velocity zeroed on spawn/reset.
- Base stats: speed 220; health 100; attack damage 20, cooldown 0.35s, range 42; hitbox radius 6; mass 1.0.
- Colliders: circle radius 6 on `Player` layer; masks `Enemy | Pickup | WorldStatic`; solid.
- Offensive stats: power 1.0, attack speed 1.0, crit 5%, crit mult 1.5; defensive: 0 armor/resist; modifiers cache dirty.
- Hurtbox + melee config: radius 42, offset 0, duration 0.15s; animation-driven attack track + directional hitbox config (24px forward).
- Progression: level 1, XP 0, XP to next via growth curve; perk points/perks/perk gameplay modifiers initialized.
- Inventory/equipment/loot pickup radius components seeded for future loot/perk work.

### Enemies (`EnemyEntityFactory`, `EnemyWaveConfig`)
- Faction Enemy; seek target Player; lifetime 20s safety; mass per archetype; colliders on `Enemy` layer.
- **Base Archetypes**:
  - **BaseHexer**: 80 move, 24 HP, 8 dmg, 1.2s CD, melee range 7, radius 5.5, mass 0.6; scale 1.0, white tint.
  - **ScoutHexer**: 120 move, 16 HP, 5 dmg, 0.9s CD, melee range 7, radius 5, mass 0.4 (lighter, faster); scale 0.92, light blue tint.
  - **BoneMage** (ranged): 65 move, 20 HP, projectile dmg 12, projectile speed 180, optimal range 140, windup 0.6s, radius 5.5, mass 0.5; CD 2.5s; scale 1.05, purple tint (200, 100, 255).
- **Elite Enemies**:
  - **EliteHexer**: 95 move, 80 HP, 15 dmg, 1.0s CD, radius 7, mass 1.2; **+5 armor, +10 arcane resist**; scale **1.4×**, gold tint (255, 200, 50); elite tag.
  - Wave unlock: **Wave 5**, spawn weight 0.3; loot drop chance boosted for higher rarity.
- **Boss Enemies**:
  - **SkeletonBoss**: 70 move, 250 HP, projectile dmg 20, projectile speed 220, optimal range 160, windup 1.0s, radius 9, mass 2.5; **+15 armor, +25 arcane resist**; scale **1.8×**, deep purple tint (150, 50, 200); boss tag.
  - Wave unlock: **Wave 10**, spawn weight 0.15; loot drop chance heavily boosted for legendary gear.
- **Visuals**: 128px sprites, per-archetype origin (64, 96); scale/tint applied per tier; animation state (Idle/Run, facing) with 8-way directional support.
- **Loot**: `LootDropper` component; base 15% drop chance; elite/boss flags modify rarity table weights (elite favors rare/epic, boss favors epic/legendary).
- **Roles (AI Variants)**:
  - **Charger**: role config with commit range 60–120, windup 0.4s, cooldown 3.5s, knockback 400, move speed 110; telegraph red circle (radius ~46) before lunge; damage ×1.5 hitbox 30u forward.
  - **Protector**: shield range 80, detection 120, duration 1.5s, cooldown 5s; grants `ShieldActive` to allies (blocks one projectile) and telegraphs blue dome; mass 0.9.
  - **Buffer**: buff range 100, duration 4s, cooldown 6s; applies +30% move speed timed buff (non-stacking, refreshes) to nearby allies; green pulse telegraph, 0.5s animation lock.
  - **Wave unlocks/caps**: chargers wave 4 (weight 0.6), protectors + buffers wave 6 (weights 0.4/0.4). Caps per wave: max 2 chargers, 1 protector, 1 buffer. Spawn spacing: protectors 250–380, buffers 240–360, chargers default 260–420.

### Waves (`WaveSchedulerSystem`)
- Interval 5s; base 3 enemies, +1 per wave; spawn radius 260–420 from player; cap 40 active enemies total.
- **Weighted spawn profiles** unlock progressively:
  - Wave 1: BaseHexer (weight 1.0)
  - Wave 2: + ScoutHexer (weight 1.2)
  - Wave 3: + BoneMage ranged (weight 0.8)
  - Wave 5: + EliteHexer (weight 0.3)
  - Wave 10: + SkeletonBoss (weight 0.15)
- **Elite/Boss Caps**: Max 3 elites, 1 boss simultaneously; if cap hit during spawn roll, reroll until valid or fallback to base enemy.
- **Spawn Selection**: Weighted random from unlocked profiles; checks caps; creates spawn request consumed by `SpawnSystem`.
- Publishes `WaveStarted` (wave number) and `WaveCompleted` events.

### Session, HUD, Pause (`GameSessionSystem`, `HudRenderSystem`)
- Session state: Playing, Paused, GameOver; tracks current wave, timer, enemies killed, time survived.
- Game over on `PlayerDiedEvent`: halts waves/combat, shows persistent notification; restart clears enemies/orbs, resets player stats/XP, session counters, notifications, pause selection.
- Pause menu: Escape toggles; options Resume/Restart/Settings/Quit; audio settings submenu with sliders (master/music/sfx/ui/voice) and mutes (all/master/music/sfx/ui/voice); settings persist to store and apply to Music/SFX systems with sample sounds.
- HUD: wave toast, current wave, game over overlay, XP/level bar, kills/time.
- Run restart publishes `SessionRestartedEvent`.

### Input, Movement, AI
- InputSystem: ignores input if not Playing; writes movement intent; publishes attack events.
- Dash stack: `DashInputSystem` buffers/gates Shift/Space input → `DashExecutionSystem` starts dash/i-frames and cooldown → `DashMovementSystem` maintains dash velocity, drops invulnerability, and ends dash; `MovementIntentSystem` skips while dash active.
- MovementIntent: velocity = normalized intent × MoveSpeed; MovementSystem integrates per frame.
- AI: `AiSeekSystem` chases nearest target faction; `RangedAttackSystem` maintains optimal range with windup, backs off when too close, fires projectiles with faction filtering.

### Combat & Damage
- Attack flow: `PlayerAttackIntentEvent` → `CombatSystem` cooldown gate → spawn transient `AttackHitbox` entity (trigger collider) with owner/faction/damage/lifetime.
- Collider-driven hits: `CollisionSystem` emits enter/stay/exit; `MeleeHitSystem` applies damage, invuln windows, one-shot per hitbox, faction filtering; `ContactDamageSystem` handles overlap damage with per-target cooldowns, adds knockback.
- Projectiles: componentized (Position, Velocity, Projectile, ProjectileVisual, Collider trigger). `ProjectileHitSystem` listens to collisions, prevents friendly fire and self-hits, applies damage, and destroys projectile after first hit; lifetimes enforced.
- Damage model: unified `DamageCalculator` applies power → crit → resist (armor/arcane) with diminishing returns (stat/(stat+100), capped 90%); flags for true/ignore resist/ignore armor; crit chance/multiplier per attacker. `DamageApplicationService` publishes `EntityDamagedEvent` with final damage and type.
- Status effects: `DamageInfo.StatusEffect` carries payload; `StatusEffectApplicationSystem` handles immunity/resist (fire/frost/arcane/nature), adjusts potency/duration, and stacks per type (Burn/Poison additive, Shock single, Freeze/Slow strongest). `StatusEffectTickSystem` runs deterministic ticks (DoTs are true damage, Shock amplifies incoming damage), feeds `StatusEffectModifiers` for slow/freeze debuffs, and publishes applied/tick/expired events. VFX/SFX hooks plus F10 debug overlay render active effects.

### Collision, Physics, Separation
- **CollisionSystem**: Spatial grid broadphase with separate dynamic and static grids; cell size 128; supports circle/AABB shapes, trigger vs solid, layer/mask filtering.
- **Spatial Grid Implementation**:
  - **Two grids**: Dynamic entities (rebuilt per frame), static entities (loaded once from TMX).
  - **Cell size**: 128 units for optimal performance with ~200 actors.
  - **Broadphase**: Per-cell entity lists; only checks entities in overlapping cells.
  - **Layer/mask filtering**: Applied during detection (e.g., Player layer masks Enemy | Pickup | WorldStatic).
- **Collision Detection**: Circle-circle, circle-AABB, AABB-AABB support; generates contact info (point, normal, depth); emits enter/stay/exit events; cached collision pairs for diffing.
- **Static World Collision**:
  - **TMX Loading** (`CollisionLoaderService`): Parses TMX object layers; filters objects with `type="collision"` or name contains "collision"/"wall".
  - **Coordinate Conversion**: TMX top-left origin → game center-based; helper `TmxToWorldCoordinates`.
  - **Static Collider Entities**: Created per TMX region; placed in static spatial grid; never move.
  - **Resolution** (`StaticCollisionResolutionSystem`): Runs **before** collision detection; projects velocity along contact normal (sliding); clamps velocity to prevent tunneling.
- **Dynamic Separation** (`DynamicSeparationSystem`):
  - **Mass-weighted push**: Lighter entities move more; formula uses mass ratio (`massB / (massA + massB)`) to distribute separation force.
  - **3 iterations** per frame for stable multi-body separation.
  - **Separation strength**: 10 (configurable); **min velocity threshold**: Prevents micro-jitter.
  - **Debug visualization**: Cyan arrows for separation vectors (F3).
- **Knockback**:
  - **Max speed**: 800 units/sec (clamped).
  - **Decay**: Exponential with configurable rate; applied in `KnockbackSystem`.
  - **Contact damage knockback**: 200 strength, 0.15s duration.
  - **Integration**: Applied before static collision resolution; combines with world collision sliding.
  - **Debug visualization**: Orange arrows for knockback vectors (F3).
- **Contact Damage**: `ContactDamageSystem` handles overlap damage; cooldown 0.5s default per attacker-target pair; adds knockback on hit.
- **Debug Overlays** (F3): Collider shapes (cyan static, green dynamic triggers, red solid), ownership lines (hitbox to owner), separation/knockback vectors.

### Animation, FX, Feedback
- **Animation System**: Player/enemy animation with 8-way facing octants; animation states (Idle/Run/Hit); frame timing from sprite sets (run 12 fps, idle 6 fps).
- **Animation Events** (`AnimationEventSystem`):
  - **AnimationEventTrack**: Cached per animation, contains time-keyed events.
  - **Event Types**: HitboxEnable, HitboxDisable, VfxTrigger, SfxTrigger.
  - **AnimationEventState**: Tracks playback time and active hitbox flag per entity.
  - **Hitbox Lifecycle**: Events enable/disable `AttackHitbox` entities based on animation timing.
- **Directional Hitboxes**:
  - **8-way offsets**: Per-facing offset vectors (South, SE, East, NE, North, NW, West, SW).
  - **DirectionalHitboxConfig**: Stores per-facing offsets for attacks.
  - **Default Player Melee**: 24px forward; Events: 0.03s Windup VFX, 0.04s Swing SFX, 0.05s Hitbox Enable, 0.15s Hitbox Disable.
  - **AnimationEventCombat**: Marks entity as using event-driven combat flow.
- **Telegraphs**:
  - **TelegraphData**: Duration, color, radius, shape (Circle/Cone/Line).
  - **Telegraph Entity**: Runtime entity with lifetime, rendered with debug-style visuals.
  - **Ranged Windups**: BoneMage 0.6s, SkeletonBoss 1.0s; telegraphs show target area before projectile fires.
  - **Color Conventions**: Red for enemy attacks, blue for player (if used).
- **VFX System**:
  - **VfxRequestEvent**: Position, type (HitImpact, ProjectileTrail, MuzzleFlash, LevelUp, etc.), data payload, faction.
  - **VfxSystem**: Subscribes to events, spawns pooled VFX entities, plays particle effects.
  - **Graceful Degradation**: Missing VFX assets log once, don't crash.
- **SFX System**:
  - **SfxRequestEvent**: Asset name, category (UI, Impact, Voice), volume, position (for 3D if needed).
  - **SfxSystem**: Loads and plays sound effects; routes category to volume/mute settings from `AudioSettingsConfig`.
  - **Sample Playback**: Settings menu plays sample sounds on volume adjustments.
- **Hit-Stop & Camera Shake**:
  - **Hit-Stop Duration**: `damage × 0.002`, clamped 0.02s–0.1s (20–100ms).
  - **Aggregation**: Multiple hits extend timer (doesn't reset), allowing smooth multi-hit combos.
  - **Pause Behavior**: Halts gameplay systems (movement, combat, AI); VFX/SFX/telegraph/hit systems continue.
  - **Camera Shake Intensity**: `damage × 0.15`, clamped 1–8 pixels.
  - **Shake Duration**: 0.15s per shake; linear intensity decay.
  - **Random Offset**: ±intensity pixels in X/Y applied to `Camera2D.ShakeOffset`.
  - **Debug Toggles**: F4 disables hit-stop, F5 toggles camera shake.
- **Damage Feedback**: Hit reactions (slows, knockback), hit effects, floating damage numbers; invulnerability windows (0.05s) on melee hits (debounce); contact damage cooldown 0.5s per target; tint/flash applied and cleared after hit.
- **Hit Reactions**: Player slowed to 85% speed (0.18s) on hit; Enemies slowed to 60% speed (0.28s). Player plays hit animation for 1.5s (visual override).

### Unified Stats & Damage Model
- **Stat Modifiers**: `StatModifiers` component with additive and multiplicative arrays per stat; applied in order `base → additive → multiplicative`.
- **Effective Stats**: `EffectiveStats` component caches calculated values with dirty flag; `StatRecalculationSystem` recomputes on modifier changes.
- **Damage Types**: Physical, Arcane, True (bypasses all resistances); `DamageInstance` carries base damage, type, and flags (CanCrit, IgnoreArmor, IgnoreResist).
- **Damage Calculation Pipeline** (`DamageCalculator`):
  1. Apply power multiplier: `baseDamage × attacker.Power`
  2. Roll crit: deterministic LCG RNG; if crit → `damage × attacker.CritMultiplier`
  3. Apply resistance: Physical checks `target.Armor`, Arcane checks `target.ArcaneResist`; True bypasses.
  4. Resistance formula: `reduction = stat / (stat + 100)`, capped at 90% (stat 900).
  5. Final damage: `rawDamage × (1 - reduction)`
- **Resistance Breakpoints**: 50 stat = 33% reduction, 100 stat = 50%, 200 stat = 67%, 900 stat = 90% cap.
- **Crit Defaults**: Player 5% chance, 1.5× multiplier; enemies don't crit by default.
- **Cooldown Reduction**: Effective cooldown `baseCooldown / attackSpeed`; CDR capped at 80% (attackSpeed ≤ 5.0).
- **Stat Helper**: `StatHelper.CalculateEffectiveCooldown` for display/logic; crit DPS formula `avgDPS × (1 + critChance × (critMultiplier - 1))`.

### Progression & Perks
- **XP System**: Orbs spawn on `EnemyDiedEvent` (1 XP each); magnet radius 120px, collection radius 40px, lifetime 10s, lerp pull strength 3.0.
- **Leveling Formula**: Base XP 10, growth 1.5×; requirement = `10 × (1.5 ^ (level - 1))`.
- **Level-Up Bonuses**: +2 attack damage, +5 move speed, +10 max health (preserves health ratio); publishes `PlayerLeveledUpEvent`.
- **Perk Points**: Granted on level-up (1 per level, configurable); tracked in `PerkPoints` component.
- **Perk Allocations**: `PerkAllocations` component stores `Dictionary<perkId, currentRank>`; `PerkService` validates prerequisites and max ranks.
- **Default Mage Tree** (10 perks, 4 tiers):
  - **Foundation (Row 0)**: Vitality (+20 HP/rank, 3 ranks), Arcane Mastery (+0.2 power/rank, 5 ranks), Swift Casting (+0.1 attack speed/rank, 3 ranks).
  - **Intermediate (Row 1)**: Critical Focus (+5% crit chance, +0.1 crit mult/rank, 3 ranks; req: Arcane 2), Arcane Armor (+10 armor, +10 arcane resist/rank, 3 ranks; req: Vitality 2), Fleet Footed (+10 move speed/rank, 3 ranks; req: Swift 1).
  - **Advanced (Row 2)**: Piercing Projectiles (+1 pierce/rank, 2 ranks; 2 pts/rank; req: Critical Focus 2, Swift 2), Temporal Flux (+10% CDR/rank, 2 ranks; 2 pts/rank; req: Swift 3).
  - **Capstone (Row 3)**: Archmage's Might (+50% power multiplier, 1 rank; 3 pts/rank; req: Critical Focus 3, Piercing Projectiles 1).
- **Prerequisites**: Perk nodes require specific rank in parent perks; `PerkService` enforces graph validation.
- **Respec**: Full respec (Shift+R) resets all allocations, refunds points, clears stat modifiers; single-perk deallocation checks dependents.
- **Perk UI**: Toggled with P key; grid navigation with arrows/WASD; Enter allocates; displays requirements, current rank, effects.
- **Gameplay Modifiers**: `PerkGameplayModifiers` component stores non-stat effects (projectile pierce, chain lightning, dash CDR) for system consumption.
- **Persistence**: Auto-saves perk allocations every 30s to JSON; cleared on run restart (not meta-progression).

### Skill System (Mage)
- **Skill Structure**: `SkillDefinition` (immutable metadata), `SkillModifiers` (stat scaling), `ComputedSkillStats` (effective values after modifiers).
- **Skill Categories**: Fire (Firebolt, Fireball, Flame Wave), Arcane (Arcane Missile, Arcane Burst, Arcane Barrage), Frost (Frost Bolt, Frost Nova, Blizzard).
- **Delivery Types**: Projectile (travels to target), AreaOfEffect (radius burst), Melee (close-range hitbox), Beam (planned).
- **Targeting Modes**: Direction (WASD), Nearest (auto-target), GroundTarget (mouse click, future), Self (centered on caster).
- **Skill Components**:
  - `EquippedSkills`: Primary (slot 0) + hotkeys 1–4; default primary is Firebolt.
  - `SkillCooldowns`: Per-skill cooldown tracking (seconds remaining).
  - `PlayerSkillModifiers`: Hierarchical modifiers (global → element → skill-specific).
  - `SkillCasting`: Active cast state with progress (0–1).
- **Cast Pipeline**:
  1. `PlayerSkillInputSystem`: Convert `PlayerAttackIntentEvent` to `SkillCastRequestEvent`.
  2. `SkillCastSystem`: Validate cooldown/resources → apply cooldown → start cast or execute immediately.
  3. `SkillExecutionSystem`: Spawn projectiles/AoE/hitboxes on `SkillCastCompletedEvent`.
- **Modifier Stacking**: Base → skill-specific → element → global → character CDR → clamps (80% CDR max, 0.1s min cooldown).
- **Damage Scaling**: `finalDamage = casterPower × skillDamageMultiplier × 10.0`; integrates with unified stat model (`ComputedStats.EffectivePower`).
- **Skill Events**: `SkillCastRequestEvent`, `SkillCastStartedEvent`, `SkillCastCompletedEvent`, `SkillCastCancelledEvent` (all event-bus driven).
- **Default Skills**:
  - Fire: Firebolt (fast, 0.5s CD, 1.0× dmg), Fireball (slow AoE, 2s CD, 3.5× dmg + 60 radius), Flame Wave (self AoE, 5s CD, 2.0× dmg).
  - Arcane: Arcane Missile (homing, 0.8s CD, 1.2× dmg), Arcane Burst (quick AoE, 3s CD, 2.5× dmg), Arcane Barrage (5 projectiles, 4s CD, 0.8× dmg each).
  - Frost: Frost Bolt (chill, 0.6s CD, 0.9× dmg), Frost Nova (freeze AoE, 8s CD, 1.5× dmg), Blizzard (ground AoE, 10s CD, 4.0× dmg).
- **Modifiers**: Support damage, cooldown, range, AoE radius, projectile count/speed/pierce/chain, cast time reduction (additive/multiplicative).
- **Integration**: Skills reuse `Projectile`, `AttackHitbox`, `Collider` components; damage flows through `DamageApplicationService` for crit/resist handling.
- **Debug Tools**: `SkillDebugHelper` for force cast, cooldown reset, inspect skills, apply test modifiers.

### Loot & Equipment
- **Rarity System**: 5 tiers with color-coding and affix counts:
  - Common (white, 0–1 affixes)
  - Uncommon (green, 1–2 affixes)
  - Rare (blue, 2–3 affixes)
  - Epic (purple, 3–4 affixes)
  - Legendary (orange, 4–5 affixes)
- **Equipment Slots**: Weapon, Armor, Amulet, Ring1, Ring2 (5 total); `Equipment` component maps slots to `ItemInstance`.
- **Item Structure**:
  - `ItemDefinition`: Base template with name, slot, affix pool, icon.
  - `ItemInstance`: Rolled item with GUID, definition reference, rolled affixes (type + value).
  - `ItemRegistry`: Singleton registry of all item definitions.
- **Affixes**: 13 types mapping to stat modifiers (Health, AttackDamage, Power, AttackSpeed, Armor, ArcaneResist, MoveSpeed, CritChance, CritMultiplier, CDR, LifeSteal, Thorns, PickupRadius); both additive and multiplicative variants.
- **Drop System**:
  - `LootDropper` component: base 15% drop chance per enemy; elite/boss flags boost rarity.
  - `LootSpawnSystem`: Subscribes to `EnemyDiedEvent`, rolls drop chance, selects rarity from weighted table, creates `DroppedLoot` entity.
  - Weighted tables: Common 50%, Uncommon 30%, Rare 15%, Epic 4%, Legendary 1% (adjustable per enemy tier).
- **Loot Pickup**: `LootPickupRadius` component (default 60px); `LootCollectionSystem` auto-collects on overlap, adds to `Inventory`.
- **Inventory**: `Inventory` component with 50-item capacity; `InventoryService` manages add/remove/equip operations.
- **Equipment System**: `EquipmentSystem` recalculates stat modifiers when equipment changes; publishes `EquipmentChangedEvent`.
- **Visuals**: `LootVisuals` component with rarity-based sprite tint; world-space loot entities with lifetime (30s before despawn).
- **UI**: Inventory overlay (I key) shows grid, equipment slots, stat comparisons; keyboard/controller navigation; drag-to-equip (planned).
- **Persistence**: Auto-saves equipment every 30s to JSON; cleared on run restart (not meta-progression).

### Elite Modifiers
- **Core Mods**: Extra Projectiles (3-shot fan), Vampiric (30% lifesteal), Explosive Death (1.5s telegraph → 72px AoE), Shielded (45 HP shield, 2.5s cooldown, 12/s regen). Aura telegraphs + sprite tints differentiate mods.
- **Scaling**: Unlocks from wave 5+; max 1/2/3 modifiers at waves 5/12/20; reward multiplier stacks multiplicatively per mod (cap 2.0×) and feeds `LootDropper.ModifierRewardMultiplier`.
- **Systems**: `EliteModifierConfig` drives rolls in `WaveSchedulerSystem`; `EliteModifierSystem` handles lifesteal, shield regen, explosive death; `RangedAttackSystem` adds projectile fans.
- **Debug**: `Shift+1/2/3/4` spawn elite with specific mod; `F7/F8` spawn base elite/boss.

### Audio
- **Music Service**: `MusicService` wraps MediaPlayer; plays `Stage1Music` on loop during Playing state; volume controlled by `AudioSettingsConfig`; `MediaPlayer.IsMuted` set when effective volume ≤ 0.0001.
- **SFX System**: `SfxSystem` loads and plays sound effects; subscribes to `SfxRequestEvent`; routes categories (UI, Impact, Voice) to per-category volume/mute settings; gracefully handles missing assets (logs once, continues).
- **Audio Categories**: 5 categories with independent controls:
  - **Master**: Global volume multiplier (default 1.0).
  - **Music**: Background music (default 0.85).
  - **SFX**: Sound effects (default 0.9).
  - **UI**: Interface sounds (default 1.0).
  - **Voice**: Character voices (default 1.0).
- **Mute Toggles**: Independent mutes per category + "Mute All" global override.
- **Audio Settings Persistence**: `AudioSettingsStore` handles JSON save/load; versioned format (v1); settings applied on load to MediaPlayer and SoundEffect instances; auto-saves on changes.
- **Sample Playback**: Settings menu plays sample sounds on slider/toggle adjustments for immediate feedback.

### UI & HUD
- World-space: player/enemy sprites, health bars, projectiles, telegraphs, VFX, damage numbers, XP orbs.
- Screen-space: HUD (wave, time, kills, XP/level bar), wave toasts, game-over overlay, pause/settings menu, perk tree UI.
- Rendering uses `SpriteBatch` with PointClamp to preserve pixel art.

### Content & Assets
- Folder conventions: PascalCase assets; sprites under `Sprites/`, tiles under `Tiles/`, fonts under `Fonts/`, audio under `Audio/`, effects under `Effects/`.
- Placeholder assets imported from prior project; includes player/enemy/NPC sets, icons, UI panels, ability sprites, XP shard, fonts, music/SFX; registered in `Content.mgcb`.
- Tiles: isometric 128x256 pivots (X 0.5, Y ~0.18) for alignment; collision merging suggested for performance.
- Pipeline commands: `dotnet mgcb /@Content.mgcb /platform:DesktopGL /output:bin/Content`; editor via `dotnet mgcb-editor ./Content.mgcb`.

### Systems & Debug Toggles (key)
- **F3**: Collision/hitbox debug overlay with vector visualization (separation, knockback, ownership lines).
- **F4**: Disable hit-stop for testing.
- **F5**: Toggle camera shake.
- **F6**: Toggle VFX/SFX rendering.
- **F7/F8**: Debug spawn elite/boss at cursor.
- **P**: Perk tree UI.
- **I**: Inventory/equipment UI.
- **Shift+R**: Full perk respec.
- **Audio settings** in pause menu: volume sliders (master/music/sfx/ui/voice), mute toggles, sample playback on adjustment.

## 9. Balancing & Tuning Anchors
- Player: 220 move, 20 dmg, 0.35s CD, 42 hitbox radius, 6 body radius, 100 HP.
- Enemy counts: wave interval 5s, base 3 growing by 1; cap 40 active; boss unlock at wave 10.
- Knockback max speed 800; contact cooldown 0.5s; separation strength 10, iterations 3.
- XP: base 10, growth 1.5×; magnet 120px; orb lifetime 10s.
- Projectile defaults: speed 180 (BoneMage), radius 4, lifetime 5s.
- Hit-stop durations clamped per-impact (see task 028), camera shake ≤8px.
