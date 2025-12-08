# Task: 034 - Status effects & elemental interactions
- Status: done

## Summary
Introduce reusable status effects (Burn, Freeze/Slow, Shock, Poison) that stack, expire, and interact with elemental resistances. Integrate with the unified damage model and combat events, with clear VFX/SFX cues and debugability.

## Goals
- Define status effect components (type, potency, duration, stacks, tick cadence) with deterministic timing at fixed timestep.
- Hook status application into hit events (melee, projectile, elite mods), including resistance/immune flags on entities.
- Implement baseline effects: Burn (DoT), Freeze/Slow (movement/attack speed debuff), Shock (bonus damage or crit amp), Poison (stacking DoT with ramp).
- Add VFX/SFX/telegraph cues per effect and a debug overlay to visualize active statuses on entities.
- Cover tests for stacking rules, duration expiry, resistance/immune handling, and deterministic ticking.

## Non Goals
- Complex aura/area status propagation beyond simple AoE application.
- Advanced CC like stuns/roots (out of scope for this task).
- Network/rollback support.
- Long-form DoT/HoT balancing; keep values tunable placeholders.

## Acceptance criteria
- [ ] At least two status effects are applied by both player and enemies; they stack/refresh according to defined rules and expire correctly.
- [ ] Resistances/immunities prevent or reduce effects as configured; damage formulas use the unified model (Task 029).
- [ ] VFX/SFX cues indicate status presence; debug view can display active effects and remaining durations.
- [ ] Status ticks are deterministic and tied to fixed timestep; no double-tick or missed-tick bugs on variable frame rates.
- [ ] Tests cover stacking/refresh, resistance/immunity paths, and tick timing; `dotnet build` passes.

## Definition of done
- Builds pass (`dotnet build`)
- Tests/play check done (if applicable)
- Docs updated (status definitions, stacking rules, resist/immune flags)
- Handoff notes added (if handing off)

## Plan

### Phase 1: Core Status Effect Components & Data Model
**Goal:** Establish the data structures for status effects

**Files to Create:**
- `src/Game/Core/Ecs/Components/StatusEffectComponents.cs`
  - `StatusEffectType` enum: Burn, Freeze, Slow, Shock, Poison, (future: Stun, Root)
  - `StatusEffectData` struct: type, potency, duration, tickInterval, stacks, maxStacks
  - `ActiveStatusEffect` component: effect data, remainingDuration, accumulatedTickTime, currentStacks
  - `StatusEffectResistances` component: per-effect resistance multipliers (0-1, where 1.0 = immune)
  - `StatusEffectImmunities` component: bitflags for complete immunity to specific effects

**Files to Modify:**
- `src/Game/Core/Combat/DamageTypes.cs`
  - Add `DamageSource` enum: Melee, Projectile, ContactDamage, StatusEffect, Environmental
  - Extend `DamageInfo` with optional `StatusEffectData?` field for on-hit application
  - Add helper methods: `WithStatusEffect(StatusEffectData effect)`

**Configuration Constants:**
```csharp
// Status Effect Default Values
Burn: 5 dmg/sec, 3s duration, 0.5s tick, max 3 stacks (additive)
Freeze: 70% move/attack slow, 2s duration, no stacks
Slow: 50% move/attack slow, 1.5s duration, refreshable
Shock: +25% dmg taken amplifier, 2s duration, max 1 stack
Poison: 3 dmg/sec base, 4s duration, 0.5s tick, max 5 stacks (ramp: +20% per stack)
```

**Rationale:**
- Separate `StatusEffectData` (blueprint) from `ActiveStatusEffect` (runtime state) for clarity
- Accumulated tick time ensures deterministic ticking at fixed timestep
- Stacking rules per-effect (additive vs refresh vs max) allow diverse mechanics
- Resistances use multipliers (0.5 = 50% duration/potency reduction) vs binary immune flags

---

### Phase 2: Status Effect Application System
**Goal:** Apply status effects from damage events and skill hits

**Files to Create:**
- `src/Game/Core/Ecs/Systems/StatusEffectApplicationSystem.cs`
  - Subscribe to `EntityDamagedEvent`
  - Check `DamageInfo` for attached status effect data
  - Validate target has no immunity/high resistance
  - Apply resistance multiplier to duration/potency
  - Stack or refresh existing effects based on type-specific rules
  - Publish `StatusEffectAppliedEvent` for VFX/SFX

**Files to Modify:**
- `src/Game/Core/Events/GameplayEvents.cs`
  - Add `StatusEffectAppliedEvent(Entity target, StatusEffectType type, float duration, int stacks)`
  - Add `StatusEffectExpiredEvent(Entity target, StatusEffectType type)`
  - Add `StatusEffectTickEvent(Entity target, StatusEffectType type, float damage)` (for DoT VFX)

**Stacking Logic by Type:**
```csharp
Burn: Additive damage, refresh duration to max
Freeze/Slow: Refresh duration, no stacking (strongest effect wins)
Shock: Single stack, refresh duration
Poison: Additive stacks (up to max), each stack +20% total dmg, refresh duration
```

**Application Flow:**
```
EntityDamagedEvent (with StatusEffectData)
  ↓
StatusEffectApplicationSystem.OnEntityDamaged
  ↓
Check immunity/resistance (skip or reduce)
  ↓
Apply stacking rules (add/refresh ActiveStatusEffect)
  ↓
Publish StatusEffectAppliedEvent
  ↓
VFX/SFX systems respond
```

**Integration Point:**
- Runs after `HitReactionSystem` (status effects applied after health reduction)
- Before movement systems (Slow/Freeze need to affect velocity this frame)

---

### Phase 3: Status Effect Tick & Update System
**Goal:** Process DoT/debuff effects over time with deterministic ticking

**Files to Create:**
- `src/Game/Core/Ecs/Systems/StatusEffectTickSystem.cs`
  - Iterate all entities with `ActiveStatusEffect` components
  - Update `remainingDuration` by deltaTime
  - Accumulate tick time; trigger damage/effects at tick intervals
  - For DoT (Burn/Poison): apply damage via `DamageApplicationService` with `DamageSource.StatusEffect`
  - For Slow/Freeze: update `StatModifiers` to reduce MoveSpeed/AttackSpeed
  - Remove expired effects and publish `StatusEffectExpiredEvent`

**Deterministic Tick Handling:**
```csharp
// In StatusEffectTickSystem.Update
foreach (var entity in entitiesWithStatusEffects) {
    var effect = world.GetComponent<ActiveStatusEffect>(entity);
    effect.RemainingDuration -= deltaTime;
    effect.AccumulatedTickTime += deltaTime;
    
    // Tick damage/effects at intervals
    while (effect.AccumulatedTickTime >= effect.Data.TickInterval) {
        effect.AccumulatedTickTime -= effect.Data.TickInterval;
        ApplyTickEffect(world, entity, effect);
    }
    
    // Expire if duration ended
    if (effect.RemainingDuration <= 0) {
        world.RemoveComponent<ActiveStatusEffect>(entity);
        world.EventBus.Publish(new StatusEffectExpiredEvent(entity, effect.Data.Type));
    }
}
```

**DoT Damage Calculation:**
```csharp
// Burn: base potency × stacks
float burnDamage = effect.Data.Potency * effect.CurrentStacks;

// Poison: base potency × (1 + 0.2 × stacks)
float poisonDamage = effect.Data.Potency * (1.0f + 0.2f * effect.CurrentStacks);

// Apply via unified damage system
damageService.ApplyDamage(
    target: entity,
    damageInfo: new DamageInfo(damage, DamageType.True, DamageFlags.None),
    sourcePosition: entityPosition,
    sourceFaction: Faction.Neutral);
```

**Slow/Freeze Stat Modification:**
- **Option A (Preferred):** Add `StatusEffectModifiers` component, integrate with `StatRecalculationSystem`
- **Option B:** Directly modify `Velocity` component each frame (less clean, but immediate)
- **Recommendation:** Option A for consistency with perk/equipment modifiers

**System Order:**
```
StatusEffectApplicationSystem (apply new effects)
  ↓
StatusEffectTickSystem (tick existing effects, apply DoT)
  ↓
StatRecalculationSystem (compute effective stats with status debuffs)
  ↓
MovementSystem (uses slowed stats)
```

---

### Phase 4: Elemental Resistance Integration
**Goal:** Tie status effects into existing stat/resist system

**Files to Modify:**
- `src/Game/Core/Ecs/Components/StatsComponents.cs`
  - Add to `DefensiveStats`:
    - `float FireResist` (reduces Burn duration/potency)
    - `float FrostResist` (reduces Freeze/Slow duration)
    - `float NatureResist` (future: for Poison, currently uses Arcane)
  - Add to `StatModifiers`:
    - Additive/multiplicative arrays for new resist types
  - Update `ComputedStats` to cache effective resists

- `src/Game/Core/Combat/DamageCalculator.cs`
  - Extend resistance formula to handle status-specific resists
  - Add `CalculateStatusEffectResistance(StatusEffectType type, DefensiveStats defense)` method
  - Use same diminishing returns formula: `resist / (resist + 100)`, capped at 90%

**Resistance Application:**
```csharp
// In StatusEffectApplicationSystem
var resistance = CalculateStatusResistance(effect.Type, targetStats);
var adjustedDuration = effect.Duration * (1.0f - resistance);
var adjustedPotency = effect.Potency * (1.0f - resistance);

// Example: 50 FireResist = 33% reduction
// 3s Burn duration → 2.0s effective duration
// 5 dmg/sec → 3.35 dmg/sec effective potency
```

**Element-to-Resist Mapping:**
```csharp
Burn → FireResist
Freeze/Slow → FrostResist
Shock → ArcaneResist
Poison → ArcaneResist (future: NatureResist if added)
```

**Default Values:**
- Player: 0 resist (fully vulnerable)
- Basic Enemies: 0 resist
- Elite Enemies: 20-50 resist (meaningful but not immune)
- Boss Enemies: 50-80 resist (highly resistant, not immune)

---

### Phase 5: Skill & Attack Integration
**Goal:** Enable skills and attacks to apply status effects on hit

**Files to Modify:**
- `src/Game/Core/Skills/SkillData.cs`
  - Add `StatusEffectData? OnHitStatusEffect` property to `SkillDefinition`
  - Add `float StatusEffectApplicationChance` (default 1.0 = 100%)
  - Update default mage skills:
    - **Firebolt**: 30% chance to apply Burn (2s, 3 dmg/sec)
    - **Fireball**: 80% chance to apply Burn (4s, 5 dmg/sec)
    - **Flame Wave**: 100% chance to apply Burn (3s, 4 dmg/sec)
    - **Frost Bolt**: 50% chance to apply Slow (1.5s, 50% slow)
    - **Frost Nova**: 100% chance to apply Freeze (2s, 70% slow)
    - **Blizzard**: 100% chance to apply Slow per tick (1s, 60% slow)

- `src/Game/Core/Skills/SkillExecutionSystem.cs`
  - When spawning projectiles/hitboxes, attach status effect to `DamageInfo`
  - Roll application chance using `CombatRng` for determinism
  - Set `DamageInfo.StatusEffect = skill.OnHitStatusEffect` if roll succeeds

- `src/Game/Core/Ecs/Systems/MeleeHitSystem.cs`
- `src/Game/Core/Ecs/Systems/ProjectileHitSystem.cs`
  - Already pass `DamageInfo` to `DamageApplicationService`
  - No changes needed (status application handled in `StatusEffectApplicationSystem`)

**Elite/Boss Status Application:**
- Elite modifiers (Task 032/036) can grant "Applies Poison on hit" or "Applies Shock on hit"
- Store effect data in elite modifier component
- Apply via `ContactDamageSystem` or melee attacks

---

### Phase 6: VFX/SFX & Visual Feedback
**Goal:** Add clear visual/audio indicators for status effects

**Files to Create:**
- `src/Game/Core/Ecs/Systems/StatusEffectVfxSystem.cs`
  - Subscribe to `StatusEffectAppliedEvent`, `StatusEffectTickEvent`, `StatusEffectExpiredEvent`
  - Spawn persistent VFX attached to entity (fire particles for Burn, ice crystals for Freeze)
  - Publish `VfxSpawnEvent` with appropriate color/type
  - Play SFX on application/expiry

**VFX Types & Colors:**
```csharp
Burn: Orange particle emitter, flicker effect (Color(255, 120, 50))
Freeze: Light blue ice crystals, frozen overlay (Color(100, 200, 255))
Slow: Blue/white swirl around feet (Color(150, 180, 255))
Shock: Purple lightning arc, pulsing (Color(150, 100, 255))
Poison: Green dripping effect, bubbles (Color(50, 200, 80))
```

**SFX Hooks:**
```csharp
// On application
SfxPlayEvent("BurnApply", SfxCategory.Impact, position)
SfxPlayEvent("FreezeApply", SfxCategory.Impact, position)

// On tick (DoT only, throttle to avoid spam)
SfxPlayEvent("BurnTick", SfxCategory.Impact, position, volume: 0.3f)

// On expiry
SfxPlayEvent("BurnExpire", SfxCategory.Impact, position, volume: 0.5f)
```

**Tint/Flash Integration:**
- Reuse existing `HitFlash` component system
- Add status-specific tint colors that override/blend with hit flash
- Burn: constant orange tint (alpha 0.3)
- Freeze: constant blue tint (alpha 0.4)
- Priority: Status tints > hit flash > normal render

**LOD/Throttling:**
- If >20 entities have same status, reduce VFX fidelity
- Skip particle effects beyond screen distance threshold
- Always show status on player/elites/bosses regardless of count

---

### Phase 7: Debug Overlay & Inspection
**Goal:** Provide tools to visualize and debug status effects

**Files to Create:**
- `src/Game/Core/Debug/StatusEffectInspector.cs`
  - Static utility class similar to `StatInspector`
  - `InspectStatusEffects(EcsWorld world, Entity entity)` → formatted string
  - Shows active effects, remaining duration, stacks, current tick time
  - `SimulateStatusEffect(effect, targetStats)` → projected damage/duration

**Files to Modify:**
- `src/Game/Core/Ecs/Systems/DebugRenderSystem.cs` (or create if missing)
  - Add toggle for status effect overlay (F4 or similar)
  - Render floating text above entities with active status
  - Format: `"🔥 Burn (2.3s) x3"` or icons + duration bar

**Debug Overlay Format:**
```
Entity 42 (Enemy)
━━━━━━━━━━━━━━━━━━━━
Active Status Effects:
  🔥 Burn
    Duration: 2.34s / 3.00s
    Stacks: 3
    Potency: 5.0 dmg/sec
    Next Tick: 0.12s
    
  ❄️ Slow
    Duration: 0.85s / 1.50s
    Modifier: -50% move/attack speed
    
Resistances:
  Fire: 0 (0%)
  Frost: 25 (20%)
  Arcane: 0 (0%)
```

**Console Commands:**
```csharp
// In debug command handler
"status apply <type> <duration>" → Apply status to player
"status clear" → Remove all status effects from player
"status immune <type>" → Toggle immunity to specific effect
"status resist <type> <amount>" → Set resistance value
```

---

### Phase 8: Testing & Validation
**Goal:** Comprehensive test coverage for status effect mechanics

**Files to Create:**
- `src/Game.Tests/Combat/StatusEffectTests.cs`
  - **Test: StatusEffect_Burn_AppliesDamageOverTime**
  - **Test: StatusEffect_Burn_StacksAdditively**
  - **Test: StatusEffect_Burn_RefreshesDuration**
  - **Test: StatusEffect_Freeze_DoesNotStack**
  - **Test: StatusEffect_Poison_RampsWithStacks**
  - **Test: StatusEffect_Shock_SingleStackRefresh**
  - **Test: StatusEffect_TickingIsDeterministic** (fixed timestep test)
  - **Test: StatusEffect_ResistanceReducesDuration**
  - **Test: StatusEffect_ImmunityPreventsApplication**
  - **Test: StatusEffect_ExpiresCorrectly**
  - **Test: StatusEffect_DoTDamageUsesTrueDamageType**
  - **Test: StatusEffect_SlowReducesMovementSpeed**

**Test Patterns:**
```csharp
[Fact]
public void StatusEffect_Burn_StacksAdditively() {
    var world = CreateTestWorld();
    var entity = CreateTestEnemy(world);
    
    // Apply 3 stacks of Burn (5 dmg/sec each)
    ApplyStatusEffect(entity, StatusEffectType.Burn, 3.0f, 5.0f, stacks: 3);
    
    // Tick 1 second
    AdvanceTime(world, 1.0f);
    
    // Verify 15 damage dealt (3 × 5)
    var health = world.GetComponent<Health>(entity);
    Assert.Equal(85f, health.Current); // 100 - 15
}

[Fact]
public void StatusEffect_TickingIsDeterministic() {
    var rng = new CombatRng(12345);
    var world1 = CreateTestWorld(rng);
    var world2 = CreateTestWorld(rng);
    
    // Apply same effect to both worlds
    ApplyStatusEffect(world1Entity, StatusEffectType.Burn, 2.0f, 5.0f);
    ApplyStatusEffect(world2Entity, StatusEffectType.Burn, 2.0f, 5.0f);
    
    // Tick irregular intervals (simulate variable frame rate)
    AdvanceTime(world1, 0.017f); // ~60 FPS
    AdvanceTime(world1, 0.033f); // ~30 FPS
    AdvanceTime(world2, 0.025f); // ~40 FPS
    AdvanceTime(world2, 0.025f); // ~40 FPS
    
    // Both should deal same total damage
    Assert.Equal(GetHealthDamage(world1Entity), GetHealthDamage(world2Entity));
}
```

**Integration Test:**
```csharp
[Fact]
public void StatusEffect_IntegrationWithSkills() {
    var world = CreateTestWorld();
    var player = CreateTestPlayer(world);
    var enemy = CreateTestEnemy(world);
    
    // Equip Firebolt (30% burn chance)
    EquipSkill(player, SkillId.Firebolt);
    
    // Cast 10 times, verify burn applied ~3 times (with deterministic seed)
    int burnApplications = 0;
    for (int i = 0; i < 10; i++) {
        CastSkill(player, SkillId.Firebolt, targetPosition: enemy.Position);
        if (HasStatusEffect(enemy, StatusEffectType.Burn)) burnApplications++;
    }
    
    Assert.InRange(burnApplications, 2, 4); // Allow variance
}
```

---

### Phase 9: Documentation & Configuration
**Goal:** Document status effect mechanics and provide tuning knobs

**Files to Create:**
- `docs/design/034-status-effects-and-elemental-interactions.md`
  - Full specification of each status effect type
  - Stacking rules reference table
  - Resistance formula documentation
  - Application chance formulas
  - Skill-to-status mappings
  - Elite modifier examples

**Configuration File (Optional):**
- `src/Game/Core/Config/StatusEffectConfig.cs`
  - Static class with tunable constants
  - Allows designers to adjust values without recompiling
  - Example:
    ```csharp
    public static class StatusEffectConfig {
        public const float BurnBaseDamage = 5.0f;
        public const float BurnDuration = 3.0f;
        public const int BurnMaxStacks = 3;
        // ...
    }
    ```

**GDD Update:**
- Add "Status Effects & Elemental Interactions" section to `docs/game-design-document.md`
- Include status effect table, resist formulas, skill interactions
- Note interaction with Task 033 dash i-frames (statuses don't apply during invuln)

---

### Phase 10: Performance & Polish
**Goal:** Optimize for horde scenarios and add final polish

**Performance Optimizations:**
1. **Component Pooling:**
   - Reuse `ActiveStatusEffect` components instead of allocating new
   - Pool status effect VFX entities
   
2. **Batch Processing:**
   - Process all Burn ticks together, then all Poison ticks
   - Reduces cache misses from jumping between entity types
   
3. **Culling:**
   - Skip status VFX for off-screen enemies (>2 screens away)
   - Throttle DoT damage numbers (show every 3rd tick instead of every tick)
   
4. **ECS Query Optimization:**
   - Use filtered queries: `world.Query<ActiveStatusEffect, Health>()` instead of iterating all entities
   - Cache query results if iterating multiple times per frame

**Polish Items:**
- Add status effect icon sprites (placeholder: colored circles with symbols)
- Implement status effect priority rendering (most severe effect shown prominently)
- Add haptic feedback on status application (controller rumble)
- Tune VFX particle counts for performance (start conservative)
- Add accessibility option to disable flashing status effects

---

### System Order Summary
```
1.  InputSystem
2.  DashInputSystem (Task 033 - checks invuln)
3.  SkillInputSystem
4.  ...
10. CollisionSystem
11. DynamicSeparationSystem
12. ContactDamageSystem
13. MeleeHitSystem
14. ProjectileHitSystem
15. HitReactionSystem (health reduction)
16. StatusEffectApplicationSystem ← NEW: Apply status from hit events
17. StatusEffectTickSystem ← NEW: Tick DoT, update debuffs
18. StatRecalculationSystem (recompute stats with status modifiers)
19. HitEffectSystem (visual flash)
20. StatusEffectVfxSystem ← NEW: Spawn/update status VFX
21. MovementIntentSystem (uses slowed stats)
22. ...
```

---

### Rollout Strategy
**Incremental Implementation:**
1. **Week 1:** Phases 1-2 (components, application)
2. **Week 2:** Phases 3-4 (ticking, resistance)
3. **Week 3:** Phases 5-6 (skill integration, VFX)
4. **Week 4:** Phases 7-10 (debug, testing, polish)

**Testing Milestones:**
- After Phase 2: Verify status application from manual events
- After Phase 3: Verify deterministic ticking and DoT damage
- After Phase 5: Verify Fire skills apply Burn, Frost skills apply Slow/Freeze
- After Phase 8: All automated tests pass

**Coordination with Other Tasks:**
- **Task 033 (Dash i-frames):** Status effects respect `Invulnerable` tag during application
- **Task 036 (Elite Modifiers):** Elites can have "Applies Poison on hit" modifier
- **Task 039 (Mage Skills):** Fire/Frost skills retroactively gain status effect data
- **Task 044 (Skill Balance):** Tune status application chances and durations based on playtests

## Notes / Risks / Blockers
- Context: Mage is the first class with fire/arcane/frost skill & talent trees; element interactions should prioritize those schools.
- Ensure ticking is frame-rate independent and deterministic; store accumulated time.
- Stacking rules must be explicit (additive vs refresh vs max stacks) to avoid ambiguity.
- Avoid per-frame allocations when managing active status lists; consider pooled buffers.
- VFX spam risk in hordes; support LOD or throttling for effects on many entities.
- Coordinate with Task 033 dash i-frames: statuses should not apply during immune frames.***

