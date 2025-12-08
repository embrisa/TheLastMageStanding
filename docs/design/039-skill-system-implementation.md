# Mage Skill System Implementation

**Implementation Date:** December 8, 2025  
**Task:** #039

## Overview

The skill system provides a flexible, data-driven framework for the Mage class abilities, covering fire/arcane/frost skill trees. The system integrates with the unified stat model (Task 029), event bus (Task 013), talent system (Task 031), and existing combat/projectile infrastructure to deliver a complete skill execution pipeline with cooldowns, modifiers, and deterministic scaling.

## Architecture

### Core Components

#### Skill Data (`SkillData.cs`)

**SkillId Enum**
Unique identifiers for all skills:
- Fire Skills: `Firebolt` (100), `Fireball` (101), `FlameWave` (102)
- Arcane Skills: `ArcaneMissile` (200), `ArcaneBurst` (201), `ArcaneBarrage` (202)
- Frost Skills: `FrostBolt` (300), `FrostNova` (301), `Blizzard` (302)

**SkillDefinition**
Immutable skill configuration:
- **Metadata**: Name, description, element (Fire/Arcane/Frost)
- **Delivery**: Projectile, AreaOfEffect, Melee, Beam
- **Targeting**: Direction (WASD), Nearest, GroundTarget, Self
- **Base Stats**: Cooldown, damage multiplier, range, AoE radius
- **Projectile Stats**: Count, speed, lifetime
- **Cast Time**: Windup duration (0 = instant)
- **Flags**: CanCrit for integration with stat system

**SkillModifiers**
Deterministic modifier stacking (additive → multiplicative):
- Cooldown reduction (additive/multiplicative)
- Damage scaling (additive/multiplicative)
- Range, AoE radius (additive/multiplicative)
- Resource cost (future mana system)
- Projectile count, speed, pierce, chain
- Cast time reduction

**ComputedSkillStats**
Final effective values after applying:
1. Base stats from definition
2. Skill-specific modifiers
3. Element-specific modifiers
4. Global modifiers (from perks/equipment)
5. Character CDR (from `ComputedStats`)
6. Clamping (min/max values)

#### Skill Registry (`SkillRegistry.cs`)

Central repository for skill definitions:
- Registers all default mage skills on initialization
- Provides lookup by `SkillId`
- Filters skills by element for UI/unlocks
- Extensible for custom/modded skills

#### Skill Components (`SkillComponents.cs`)

**EquippedSkills**
Tracks which skills are bound:
- `PrimarySkill`: Main attack (slot 0)
- `HotkeySkills`: Slots 1-4 for additional abilities

**SkillCooldowns**
Per-skill cooldown tracking:
- Dictionary mapping `SkillId` → remaining cooldown seconds
- Updated each frame by `SkillCastSystem`

**PlayerSkillModifiers**
Hierarchical modifier stacking:
- `GlobalModifiers`: Apply to all skills
- `ElementModifiers`: Element-specific (e.g., +20% Fire damage)
- `SkillSpecificModifiers`: Per-skill bonuses (e.g., Firebolt +1 projectile)
- `IsDirty`: Triggers recalculation when modifiers change

**SkillCasting**
Active cast state:
- Skill being cast
- Remaining cast time
- Progress (0-1)

### Events (`SkillEvents.cs`)

**SkillCastRequestEvent**
Published when player wants to cast (from `PlayerSkillInputSystem`):
- Caster entity
- Skill ID
- Target position and direction

**SkillCastStartedEvent**
Published when cast begins (has cast time):
- For UI feedback (cast bar)

**SkillCastCompletedEvent**
Published when cast finishes:
- Triggers `SkillExecutionSystem` to spawn effects

**SkillCastCancelledEvent**
Published on failure:
- Reason: "On cooldown", "Already casting", "Insufficient resources"

### Systems

#### PlayerSkillInputSystem (`PlayerSkillInputSystem.cs`)

Bridges existing input to skill system:
- Subscribes to `PlayerAttackIntentEvent`
- Reads `EquippedSkills` to get primary skill
- Determines targeting direction from movement input or last velocity
- Publishes `SkillCastRequestEvent`

**Integration Point**: Runs after `InputSystem`, before skill logic.

#### SkillCastSystem (`SkillCastSystem.cs`)

Validates and gates skill casts:
1. Check cooldown (`SkillCooldowns`)
2. Check if already casting (`SkillCasting`)
3. Check resources (future)
4. Calculate effective stats with modifiers
5. Apply cooldown
6. Start cast (if `CastTime > 0`) or execute immediately
7. Publish `SkillCastStartedEvent` or `SkillCastCompletedEvent`

**Updates per frame**:
- Tick all cooldowns
- Update active casts, publish completion when done

#### SkillExecutionSystem (`SkillExecutionSystem.cs`)

Spawns combat effects on cast completion:
- **Projectiles**: Creates projectile entities with collision
  - Multi-shot: Spreads projectiles at angles
  - Pierce support (requires `Projectile` extension)
  - AoE-on-impact: `ProjectileAoE` component for explosions
- **AoE**: Creates large trigger collider with `AttackHitbox`
  - Self-centered or ground-targeted
- **Melee**: Spawns directional hitbox (similar to existing melee)
- **VFX/SFX**: Publishes events for visual/audio feedback

**Damage Calculation**:
```csharp
baseDamage = casterPower * skillDamageMultiplier * 10.0f
```
Scales with `ComputedStats.EffectivePower` for consistency.

## System Integration

### Initialization

**PlayerEntityFactory**:
```csharp
_world.SetComponent(entity, new EquippedSkills()); // Default: Firebolt primary
_world.SetComponent(entity, new SkillCooldowns());
_world.SetComponent(entity, new PlayerSkillModifiers());
```

**EcsWorldRunner** system order:
```
InputSystem
  ↓
PlayerSkillInputSystem  ← Convert attack input to skill request
  ↓
SkillCastSystem  ← Validate, gate, cooldown
  ↓
SkillExecutionSystem  ← Spawn projectiles/AoE/hitboxes
  ↓
(existing collision/combat systems)
```

### Event Flow

```
PlayerAttackIntentEvent (from InputSystem)
  ↓
PlayerSkillInputSystem → SkillCastRequestEvent
  ↓
SkillCastSystem:
  - On cooldown? → SkillCastCancelledEvent
  - Already casting? → SkillCastCancelledEvent
  - Cast time > 0? → SkillCastStartedEvent + SkillCasting component
  - Instant? → SkillCastCompletedEvent
  ↓
(if cast time) Update loop ticks SkillCasting
  ↓
SkillCastCompletedEvent
  ↓
SkillExecutionSystem → Spawn projectiles/AoE/hitboxes
  ↓
CollisionSystem → Damage application (existing)
```

### Stat Integration

Skills leverage `ComputedStats` for:
- **Power**: Base damage scaling
- **CooldownReduction**: Global CDR applied to all skills
- **CritChance/CritMultiplier**: Projectiles and hitboxes inherit `CanCrit` flag

Modifier stacking order:
1. Character base stats (`OffensiveStats`)
2. Equipment modifiers (`EquipmentModifiers`)
3. Perk modifiers (`PerkModifiers`)
4. Skill-specific modifiers (`PlayerSkillModifiers`)

### Collision Integration

Skills reuse existing components:
- **Projectile**: Velocity, lifetime, faction filtering, pierce (future)
- **AttackHitbox**: Transient trigger colliders for melee/AoE
- **Collider**: Trigger mode for hit detection via `CollisionSystem`
- **ProjectileHitSystem** and **MeleeHitSystem**: Apply damage via unified `DamageApplicationService`

### Perk/Talent Integration

Perks/talents can modify skills by updating `PlayerSkillModifiers`:
```csharp
// Example: Fire specialization perk
var mods = entity.GetComponent<PlayerSkillModifiers>();
if (!mods.ElementModifiers.ContainsKey(SkillElement.Fire))
    mods.ElementModifiers[SkillElement.Fire] = SkillModifiers.Zero;

var fireMods = mods.ElementModifiers[SkillElement.Fire];
fireMods.DamageAdditive += 0.2f; // +20% Fire damage
mods.ElementModifiers[SkillElement.Fire] = fireMods;
mods.IsDirty = true;
```

This follows the same pattern as `PerkEffectApplicationSystem`.

## Default Mage Skills

### Fire Element

**Firebolt** (Primary)
- Fast projectile (500 speed, 0.5s cooldown)
- 1.0× damage multiplier
- Basic spam skill

**Fireball**
- Slower projectile (350 speed, 2s cooldown)
- 3.5× damage multiplier + 60 AoE radius
- 0.3s cast time (telegraph)
- High damage burst

**Flame Wave**
- Self-centered AoE (150 radius, 5s cooldown)
- 2.0× damage multiplier
- 0.2s cast time
- Crowd control

### Arcane Element

**Arcane Missile**
- Homing projectile (450 speed, 0.8s cooldown)
- 1.2× damage multiplier
- Targets nearest enemy automatically

**Arcane Burst**
- Self-centered AoE (100 radius, 3s cooldown)
- 2.5× damage multiplier
- 0.15s cast time
- Quick defensive burst

**Arcane Barrage**
- Multi-projectile (5 count, 500 speed, 4s cooldown)
- 0.8× damage per projectile (4.0× total)
- 0.5s cast time (channels all shots)
- Spread pattern

### Frost Element

**Frost Bolt**
- Chilling projectile (450 speed, 0.6s cooldown)
- 0.9× damage multiplier
- Slightly slower than Firebolt (future: apply slow debuff)

**Frost Nova**
- Self-centered AoE (120 radius, 8s cooldown)
- 1.5× damage multiplier
- 0.1s cast time
- Long cooldown defensive tool

**Blizzard**
- Ground-targeted AoE (150 radius, 10s cooldown)
- 4.0× damage multiplier
- 0.4s cast time
- Highest cooldown, highest damage

## Configuration

### Damage Scaling

Base damage formula:
```csharp
finalDamage = casterPower * skillDamageMultiplier * 10.0f
```

- `casterPower`: From `ComputedStats.EffectivePower` (default 1.0)
- `skillDamageMultiplier`: From `SkillDefinition.BaseDamageMultiplier`
- Scale factor: 10.0 for meaningful numbers

### Cooldown Reduction

Applied in order:
1. Skill-specific CDR (from modifiers)
2. Global character CDR (from `ComputedStats`)
3. Clamped at 80% max (0.8)
4. Minimum cooldown: 0.1s

```csharp
totalCDR = Math.Clamp(skillCDR + globalCDR, 0f, 0.8f);
effectiveCooldown = Math.Max(0.1f, baseCooldown * (1 - totalCDR));
```

### Projectile Spread

Multi-shot skills use a 30-degree spread:
```csharp
spreadAngle = 30 degrees
angleStep = spreadAngle / (count - 1)
for each projectile: direction = rotate(base, startAngle + i * angleStep)
```

### Element Colors

Visual identification:
- **Fire**: RGB(255, 100, 50) - Orange-red
- **Arcane**: RGB(150, 100, 255) - Purple
- **Frost**: RGB(100, 200, 255) - Cyan

## Debug Tools (`SkillDebugHelper.cs`)

**DebugCastSkill**: Force cast bypassing cooldowns  
**ResetAllCooldowns**: Clear all cooldowns for testing  
**InspectSkills**: Print equipped skills and cooldowns  
**EquipSkill**: Change equipped skills at runtime  
**ApplyTestModifiers**: Add test bonuses (+50% damage, +1 projectile, etc.)  
**ShowEffectiveStats**: Calculate and display final effective stats

## Testing

**SkillDataTests.cs** (12 tests, all passing):
- Modifier stacking (additive/multiplicative)
- Cooldown clamping (min/max)
- Global CDR integration
- Stat calculation order
- Projectile count clamping

**Coverage**:
- ✅ `SkillModifiers.Combine`
- ✅ `ComputedSkillStats.Calculate`
- ✅ `SkillRegistry` lookup and filtering
- ✅ CDR edge cases (min/max)
- ✅ Modifier application order

## Performance Considerations

- **Cooldown updates**: O(n) per entity with skills, only ticks active cooldowns
- **Modifier recalculation**: Only on `IsDirty` flag (event-driven)
- **Skill lookup**: Dictionary O(1) by `SkillId`
- **Projectile spawning**: Reuses existing pooled collider system
- **Cast state**: Minimal (1 component per actively casting entity)

## Known Limitations

1. **Pierce**: `Projectile` component needs `PierceRemaining` field (commented in code)
2. **Chain**: Requires target-tracking system (not yet implemented)
3. **Resource costs**: Mana/energy system not yet implemented
4. **Homing**: Arcane Missile marked as `Nearest` but doesn't actually home yet
5. **Status effects**: Frost chill/slow requires status effect system (Task 034)
6. **Ground targeting**: Mouse input integration needed for `GroundTarget` skills
7. **Skill UI**: No hotkey bar or cooldown display yet

## Future Enhancements

1. **Multiple skill slots**: Extend `EquippedSkills` for hotkeys 1-4
2. **Skill unlocks**: Integration with progression/talent system
3. **Skill morphs**: Talent-driven skill transformations
4. **Combo system**: Chain skills for bonuses
5. **Channeled skills**: Beam-type delivery
6. **Skill synergies**: Element interaction bonuses
7. **Skill trees**: Fire/Arcane/Frost specialization branches
8. **Ultimate abilities**: High-cooldown, high-impact skills
9. **Skill UI**: Visual cooldown timers, hotkey indicators
10. **Skill tooltips**: In-game stat display with modifier breakdowns

## Integration with Other Systems

### Task 031 (Talents)
Talents can add skill modifiers via `PlayerSkillModifiers`:
- **Piercing Projectiles** perk: `PierceCountAdditive++`
- **Fire Mastery**: `ElementModifiers[Fire].DamageAdditive += 0.2f`
- **Swift Casting**: `GlobalModifiers.CooldownReductionAdditive += 0.1f`

### Task 030 (Equipment)
Equipment affixes could modify skills:
- "+20% Fire skill damage" → Element modifiers
- "+1 to all projectile skills" → Global modifiers
- "Firebolt chains 2 times" → Skill-specific modifiers

### Task 034 (Status Effects)
Skills can trigger status effects:
- Frost skills: Apply chill/freeze
- Fire skills: Apply burn DoT
- Arcane skills: Apply vulnerability debuff

### Task 037 (Meta Progression)
Persistent skill unlocks:
- Start runs with additional skills unlocked
- Permanent skill modifiers from meta currency

## Usage Examples

### Defining a Custom Skill

```csharp
var lightningBolt = new SkillDefinition(
    id: (SkillId)1000,
    name: "Lightning Bolt",
    description: "Strike enemies with lightning",
    element: SkillElement.Arcane,
    deliveryType: SkillDeliveryType.Projectile,
    targetType: SkillTargetType.Direction,
    baseCooldown: 1.5f,
    baseDamageMultiplier: 2.0f,
    range: 500f,
    projectileSpeed: 800f,
    canCrit: true
);

skillRegistry.RegisterSkill(lightningBolt);
```

### Equipping a Skill

```csharp
var equipped = entity.GetComponent<EquippedSkills>();
equipped.SetSkill(0, SkillId.Fireball); // Primary
equipped.SetSkill(1, SkillId.FrostNova); // Hotkey 1
world.SetComponent(entity, equipped);
```

### Adding Skill Modifiers

```csharp
var mods = entity.GetComponent<PlayerSkillModifiers>();

// Global: +30% skill damage
mods.GlobalModifiers.DamageAdditive += 0.3f;

// Fire: +50% damage, +1 projectile
var fireMods = SkillModifiers.Zero;
fireMods.DamageAdditive = 0.5f;
fireMods.ProjectileCountAdditive = 1;
mods.ElementModifiers[SkillElement.Fire] = fireMods;

// Firebolt specifically: +1 pierce
var firebolts = SkillModifiers.Zero;
fireboltMods.PierceCountAdditive = 1;
mods.SkillSpecificModifiers[SkillId.Firebolt] = fireboltMods;

mods.IsDirty = true;
world.SetComponent(entity, mods);
```

### Manually Casting a Skill

```csharp
var direction = new Vector2(1f, 0f); // Right
var targetPos = casterPosition + direction * 300f;

world.EventBus.Publish(new SkillCastRequestEvent(
    caster: playerEntity,
    skillId: SkillId.Fireball,
    targetPosition: targetPos,
    direction: direction
));
```

## Build Status

- ✅ `dotnet build` passes with 0 warnings/errors
- ✅ All 12 skill system tests pass
- ✅ No regressions in existing systems
- ✅ Integrated with player factory and world runner
- ✅ Event bus integration complete
- ✅ Stat system integration complete

## Documentation

- **Design doc**: `docs/design/039-skill-system-implementation.md`
- **Task notes**: Updated in `tasks/039-skill-system.md`
- **Test coverage**: `src/Game.Tests/Skills/SkillDataTests.cs`
- **Debug utilities**: `src/Game/Core/Debug/SkillDebugHelper.cs`
