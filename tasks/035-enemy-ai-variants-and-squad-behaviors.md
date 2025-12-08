# Task: 035 - Enemy AI variants & squad behaviors
- Status: done

## Summary
Add differentiated enemy roles (charger, protector, buffer) with simple squad behaviors that vary pressure and positioning. Reuse existing hitbox/projectile/telegraph systems to keep readability while increasing encounter variety.

## Goals
- Define AI role configs (behavior states, ranges, cooldowns) and a behavior selector that switches states based on distance/timers.
- Implement at least three behaviors:
  - Charger: closes distance fast, commits to a swing with telegraph and knockback.
  - Protector: shields nearby allies or blocks projectiles briefly.
  - Buffer: applies a timed buff (e.g., move speed or damage) to allies in radius.
- Integrate behaviors into wave spawning with per-role weights and spacing rules.
- Add debug tools to visualize current AI state, target, and active buffs/shields.
- Cover tests for behavior switching, cooldown enforcement, and wave config parsing for roles.

## Non Goals
- Pathfinding/navmesh work beyond current steering/separation.
- Complex group tactics (flanking, formations) or blackboard AI.
- Networking/rollback.
- New art; reuse current assets/telegraphs/VFX.

## Acceptance criteria
- [x] Three role behaviors are implemented and spawn via wave config; behaviors telegraph clearly and do not deadlock pathing.
- [x] Protectors can block or mitigate projectiles for a short window; buffers apply timed buffs to allies; chargers execute commit swings with knockback.
- [x] Behavior switching respects cooldowns and distance thresholds; debug overlay can show current state and target.
- [x] Wave pacing remains stable; no runaway performance or collision jitter with added behaviors.
- [x] Tests cover state transitions, cooldowns, and wave role parsing; `dotnet build` passes.

## Definition of done
- Builds pass (`dotnet build`)
- Tests/play check done (if applicable)
- Docs updated (role configs, state machine, wave integration)
- Handoff notes added (if handing off)

## Plan

### Phase 1: Core AI Role Components & Behavior State Machine
**Goal:** Establish data structures for AI roles and simple state-based behaviors.

**Files to Create:**
- `src/Game/Core/Ecs/Components/AiRoleComponents.cs`
  - `EnemyRole` enum: Charger, Protector, Buffer, (base Melee, Ranged)
  - `AiBehaviorState` enum: Idle, Seeking, Committing, Buffing, Shielding, Cooldown
  - `AiRoleConfig` struct: role type, behavior ranges, cooldown durations, target priorities
  - `AiBehaviorStateMachine` component: current state, state timer, cooldown timer, target entity
  - `CommitAttackData` struct: windup duration, telegraph data, knockback force, commitment range

**Files to Modify:**
- `src/Game/Core/Ecs/Config/EnemyWaveConfig.cs`
  - Add `AiRoleConfig?` optional field to `EnemyArchetype`
  - Define charger/protector/buffer role configs as static methods
  - Example: `ChargerRole()` with commit ranges (50-150), cooldown (3.5s), knockback (400)

**Behavior State Transitions:**
```
┌──────────┐
│  Idle    │ ←─────── Spawned or no target
└────┬─────┘
     │ has target & state timer elapsed
     ▼
┌──────────┐
│ Seeking  │ ←─────── Moving toward target
└────┬─────┘
     │ distance in commit range (Charger)
     │ OR ally in buff range (Buffer)
     │ OR projectile detected (Protector)
     ▼
┌───────────┐
│Committing │ ───→ spawn telegraph, delay, spawn hitbox
│ Buffing   │ ───→ apply buff to nearby allies
│ Shielding │ ───→ enable projectile block flag
└────┬──────┘
     │ action complete
     ▼
┌──────────┐
│ Cooldown │ ───→ wait cooldownDuration
└────┬─────┘
     │ cooldown expired
     └──→ back to Seeking
```

**Configuration Values:**
```csharp
Charger:
  - CommitRange: 60-120 units (close enough to lunge)
  - WindupDuration: 0.4s (telegraph charge)
  - KnockbackForce: 400 (strong push)
  - Cooldown: 3.5s (can't spam charges)
  - MoveSpeed: 110 (faster than base)

Protector:
  - ShieldRange: 80 units (allies in radius)
  - ShieldDuration: 1.5s (brief block window)
  - Cooldown: 5.0s (long downtime for balance)
  - ShieldBlocksProjectiles: true (stops 1 projectile then expires)

Buffer:
  - BuffRange: 100 units (allies in radius)
  - BuffDuration: 4.0s (timed buff on allies)
  - Cooldown: 6.0s (infrequent but impactful)
  - BuffModifiers: +30% move speed OR +20% damage (pick one per config)
```

**Rationale:**
- State machine keeps behavior deterministic and testable
- Range-based triggers avoid complex pathfinding
- Cooldowns prevent spam and give player breathing room

---

### Phase 2: Charger Behavior Implementation
**Goal:** Implement commit-attack behavior with telegraph and knockback.

**Files to Create:**
- `src/Game/Core/Ecs/Systems/AiChargerSystem.cs`
  - Iterate entities with `AiRoleConfig(role=Charger)` and `AiBehaviorStateMachine`
  - **Seeking → Committing transition:**
    - Check if distance to target in commit range (60-120)
    - Stop velocity, set state=Committing, reset state timer
    - Spawn telegraph entity at predicted commit position (current + normalized direction × attackRange)
  - **Committing phase:**
    - Count down state timer (0.4s windup)
    - On timer expire: spawn hitbox at forward position, apply knockback to self (brief lunge visual)
    - Transition to Cooldown
  - **Cooldown phase:**
    - Set velocity=0, count down cooldown timer (3.5s)
    - On expire: transition back to Seeking

**Telegraph Integration:**
- Reuse `TelegraphData` from Task 028
- Telegraph color: Red (255, 50, 50, 180) to indicate danger
- Shape: Circle with radius matching attack hitbox (42-50 units)
- Spawn telegraph entity with `ActiveTelegraph` component and position at commit point
- Telegraph destroyed after windup completes

**Hitbox & Knockback:**
- Spawn `AttackHitbox` entity similar to player melee (Task 026)
- Position: charger position + forward direction × 30 (lunge reach)
- Damage: archetype.Damage × 1.5 (commit attacks hit harder)
- Apply knockback to target: magnitude 400, duration 0.2s
- Apply small knockback to charger (50 magnitude) for lunge "commitment" feel

**Key Behaviors:**
- Charger telegraphs before committing (0.4s warning)
- Commit attack has longer cooldown than normal attacks (3.5s vs 1.2s)
- Charger vulnerable during cooldown (easy to kite)
- Knockback respects existing `KnockbackSystem` and dynamic separation

---

### Phase 3: Protector Behavior Implementation
**Goal:** Implement projectile-blocking shield behavior.

**Files to Create:**
- `src/Game/Core/Ecs/Systems/AiProtectorSystem.cs`
  - Iterate entities with `AiRoleConfig(role=Protector)` and `AiBehaviorStateMachine`
  - **Seeking → Shielding transition:**
    - Check if any ally within shield range (80 units)
    - Check if any enemy projectiles within detection range (120 units) heading toward allies
    - If both true: stop velocity, set state=Shielding, reset state timer
    - Apply `ShieldActive` component to self
  - **Shielding phase:**
    - Count down state timer (1.5s duration)
    - `ShieldActive` flag checked by `ProjectileHitSystem` to block projectiles
    - On projectile blocked: decrement shield stack (max 1), remove `ShieldActive` if depleted
    - On timer expire: remove `ShieldActive`, transition to Cooldown
  - **Cooldown phase:**
    - Wait 5.0s, then back to Seeking

**Files to Modify:**
- `src/Game/Core/Ecs/Components/CombatHitboxComponents.cs`
  - Add `ShieldActive` component: `bool IsActive, int BlocksRemaining, float Duration`
- `src/Game/Core/Ecs/Systems/ProjectileHitSystem.cs`
  - Before applying damage, check if target has `ShieldActive` with `BlocksRemaining > 0`
  - If true: destroy projectile, decrement blocks, publish `ProjectileBlockedEvent` (for VFX), skip damage
  - If blocks depleted: remove `ShieldActive` component

**Visual Feedback:**
- Spawn VFX circle around protector when shield activates (blue glow, radius 80)
- VFX destroyed when shield expires
- Optional: spawn impact VFX at block location when projectile blocked

**Key Behaviors:**
- Protector shields allies, not self (role is support)
- Shield blocks 1 projectile then expires immediately (no multi-block tank)
- Long cooldown (5s) prevents constant blocking
- Does not block melee attacks (only projectiles)

---

### Phase 4: Buffer Behavior Implementation
**Goal:** Implement ally buff aura behavior.

**Files to Create:**
- `src/Game/Core/Ecs/Systems/AiBufferSystem.cs`
  - Iterate entities with `AiRoleConfig(role=Buffer)` and `AiBehaviorStateMachine`
  - **Seeking → Buffing transition:**
    - Check if any ally within buff range (100 units)
    - If true: stop velocity, set state=Buffing, reset state timer
  - **Buffing phase:**
    - On enter: iterate all allies in range, apply `TimedBuff` component with duration (4.0s)
    - VFX: spawn buff indicator above each buffed ally (green upward arrow or glow)
    - Count down state timer (instant cast, but 0.5s animation lock)
    - On timer expire: transition to Cooldown
  - **Cooldown phase:**
    - Wait 6.0s, then back to Seeking

**Files to Create:**
- `src/Game/Core/Ecs/Components/BuffComponents.cs`
  - `BuffType` enum: MoveSpeedBuff, DamageBuff, AttackSpeedBuff, (future: Regeneration, Shield)
  - `TimedBuff` component: `BuffType Type, float Duration, StatModifiers Modifiers`
  
**Files to Create:**
- `src/Game/Core/Ecs/Systems/BuffTickSystem.cs`
  - Iterate entities with `TimedBuff`
  - Count down duration by deltaTime
  - Remove component when duration <= 0
  - Publish `BuffExpiredEvent` for VFX cleanup

**Files to Modify:**
- `src/Game/Core/Ecs/Systems/StatRecalculationSystem.cs`
  - When checking for dirty stats, also iterate `TimedBuff` components
  - Aggregate buff modifiers into `StatModifiers` before recalculating
  - Buffs stack additively with equipment/perks (use `StatModifiers.Combine()`)

**Buff Configurations:**
```csharp
MoveSpeedBuff:
  - Modifiers: MoveSpeedMultiplicative = 1.3f (+30% speed)
  - Duration: 4.0s
  - Visual: Green glow + speed lines

DamageBuff:
  - Modifiers: PowerAdditive = 0.2f (+20% damage)
  - Duration: 4.0s
  - Visual: Red glow + power icon
```

**Key Behaviors:**
- Buff applies to all allies in range (including other buffers)
- Buff does NOT stack with itself (refresh duration instead)
- Buffs integrate with existing stat system (Task 029)
- Buffer is vulnerable during cast and cooldown

---

### Phase 5: Wave Config Integration & Role Weighting
**Goal:** Enable role-based enemy spawning in wave config.

**Files to Modify:**
- `src/Game/Core/Ecs/Config/EnemyWaveConfig.cs`
  - Add `ChargerHexer()` archetype with Charger role config
  - Add `ProtectorHexer()` archetype with Protector role config
  - Add `BufferHexer()` archetype with Buffer role config
  - Update `Default` config to include new archetypes with appropriate weights:
    - Charger: weight 0.6, unlock wave 4
    - Protector: weight 0.4, unlock wave 6
    - Buffer: weight 0.4, unlock wave 6
  - Lower base enemy weight slightly to compensate (1.0 → 0.8)

**Spawn Cap Enforcement:**
- Reuse existing elite/boss cap logic from Task 032
- Add role caps: max 2 chargers, 1 protector, 1 buffer per wave
- If cap hit, reroll archetype (similar to elite cap handling)

**Spacing Rules:**
- Chargers spawn at normal range (260-420 from player)
- Protectors spawn slightly closer to allies (250-380)
- Buffers spawn with allies (240-360)
- No special formation logic; rely on AI spacing naturally

**Wave Progression:**
```
Wave 1-3: Base enemies only
Wave 4+: Chargers start appearing (fast pressure)
Wave 6+: Protectors + Buffers unlock (support tactics)
Wave 10+: All roles + elites + boss (full chaos)
```

---

### Phase 6: Debug Visualization & Inspection
**Goal:** Provide tools to visualize AI state and active behaviors.

**Files to Create:**
- `src/Game/Core/Debug/AiRoleInspector.cs`
  - Static utility similar to `StatInspector`
  - `InspectAiBehavior(EcsWorld world, Entity entity)` → formatted string
  - Shows: current role, state, state timer, cooldown timer, target entity

**Files to Modify:**
- `src/Game/Core/Ecs/Systems/CollisionDebugRenderSystem.cs` (or create `AiDebugRenderSystem.cs`)
  - Add toggle for AI state overlay (F5 key or similar)
  - Render text above enemies with AI state info:
    - Format: `"Charger | Committing (0.2s) | CD: 1.3s"`
    - Color-code states: Seeking=White, Committing=Yellow, Shielding=Blue, Buffing=Green, Cooldown=Gray
  - Draw debug ranges:
    - Charger commit range: yellow circle
    - Protector shield range: blue circle
    - Buffer buff range: green circle

**Console Commands (optional):**
```csharp
"ai spawn charger" → Spawn charger near player
"ai spawn protector" → Spawn protector near player
"ai spawn buffer" → Spawn buffer near player
"ai toggle-states" → Toggle AI state overlay
```

**Key Insights:**
- Debug overlay shows active buffs on entities (green icon above buffed allies)
- Telegraph colors differentiate roles: red=charger, blue=protector (shield VFX)
- AI state visible to diagnose stuck behaviors or cooldown issues

---

### Phase 7: Testing & Validation
**Goal:** Comprehensive test coverage for AI behaviors and wave integration.

**Files to Create:**
- `src/Game.Tests/Ai/AiRoleSystemTests.cs`
  - **Test: Charger_CommitsAttack_WithinRange**
  - **Test: Charger_TelegraphsBeforeCommit**
  - **Test: Charger_KnockbackAppliedToTarget**
  - **Test: Charger_CooldownPreventsSpam**
  - **Test: Protector_ActivatesShield_WhenProjectileDetected**
  - **Test: Protector_BlocksOneProjectile_ThenExpires**
  - **Test: Protector_ShieldDoesNotBlockMelee**
  - **Test: Buffer_AppliesBuff_ToAlliesInRange**
  - **Test: Buffer_BuffDoesNotStack_RefreshesDuration**
  - **Test: Buffer_IntegratesWithStatSystem**
  - **Test: AiBehaviorStateMachine_TransitionsCorrectly**
  - **Test: RoleBasedSpawning_RespectsWaveUnlocks**
  - **Test: RoleBasedSpawning_EnforcesSpawnCaps**

**Test Patterns:**
```csharp
[Fact]
public void Charger_CommitsAttack_WithinRange() {
    var world = CreateTestWorld();
    var charger = CreateChargerEnemy(world, position: new Vector2(100, 100));
    var target = CreatePlayer(world, position: new Vector2(140, 100)); // 40 units away (in range)
    
    // Initially seeking
    var stateMachine = world.GetComponent<AiBehaviorStateMachine>(charger);
    Assert.Equal(AiBehaviorState.Seeking, stateMachine.State);
    
    // Update system - should transition to Committing
    RunSystem<AiChargerSystem>(world, deltaTime: 0.016f);
    
    stateMachine = world.GetComponent<AiBehaviorStateMachine>(charger);
    Assert.Equal(AiBehaviorState.Committing, stateMachine.State);
    
    // Verify telegraph spawned
    var telegraphCount = CountEntitiesWithComponent<ActiveTelegraph>(world);
    Assert.Equal(1, telegraphCount);
}

[Fact]
public void Protector_BlocksOneProjectile_ThenExpires() {
    var world = CreateTestWorld();
    var protector = CreateProtectorEnemy(world);
    
    // Activate shield
    world.SetComponent(protector, new ShieldActive(isActive: true, blocksRemaining: 1, duration: 1.5f));
    
    // Spawn enemy projectile heading toward player
    var projectile = CreateProjectile(world, targetFaction: Faction.Player);
    
    // Simulate hit
    var hitSystem = new ProjectileHitSystem();
    hitSystem.Initialize(world);
    PublishCollisionEvent(world, projectile, protector);
    hitSystem.Update(world, CreateContext(0.016f));
    
    // Projectile destroyed, shield consumed
    Assert.False(world.IsEntityAlive(projectile));
    var shield = world.GetComponent<ShieldActive>(protector);
    Assert.Equal(0, shield.BlocksRemaining); // Shield depleted
}
```

**Integration Test:**
- Spawn mixed wave (charger + protector + buffer + base enemies)
- Run simulation for 30 seconds
- Verify:
  - Chargers commit attacks and don't deadlock
  - Protectors block at least one projectile
  - Buffers apply buffs to nearby enemies
  - No collision jitter or pathfinding freeze
  - Wave pacing remains stable (no runaway spawns)

---

### Phase 8: Documentation & Configuration
**Goal:** Document AI role mechanics and provide tuning knobs.

**Files to Create:**
- `docs/design/035-enemy-ai-variants-and-squad-behaviors.md`
  - Full specification of each AI role
  - State machine diagram and transition rules
  - Behavior ranges and cooldown values
  - Integration with existing systems (collision, combat, buffs)
  - Examples of role combinations in waves

**GDD Update:**
- Add "Enemy AI Roles" section to `docs/game-design-document.md`
- Include role descriptions, behavior states, and spawn progression
- Document role caps and wave unlock thresholds

**Configuration Tuning:**
All role configs should be in `EnemyWaveConfig.cs` as static methods for easy iteration:
```csharp
private static AiRoleConfig ChargerRole() => new(
    role: EnemyRole.Charger,
    commitRangeMin: 60f,
    commitRangeMax: 120f,
    windupDuration: 0.4f,
    cooldownDuration: 3.5f,
    knockbackForce: 400f
);
```

---

### Phase 9: Performance & Polish
**Goal:** Optimize for horde scenarios and add final polish.

**Performance Considerations:**
1. **State Machine Caching:**
   - Cache target entity lookups (don't re-query every frame)
   - Use bounding volume tests before expensive range checks
   
2. **Behavior Throttling:**
   - Protectors check for projectiles every 0.2s, not every frame
   - Buffers scan for allies every 0.5s, not every frame
   
3. **VFX Culling:**
   - Skip buff VFX for off-screen entities (>2 screens away)
   - Reuse telegraph entities from object pool
   
4. **Collision Optimization:**
   - Charger commit hitboxes are short-lived (0.15s)
   - Shield collider checks cached during shield duration

**Polish Items:**
- Add distinct audio cues for each role (charger grunt, protector shield clang, buffer chime)
- Telegraph color variations per role (red=charger, blue=protector, green=buffer)
- Buff icons above entities (placeholder: colored circles with symbols)
- Charger "charge" animation (speed up run sprite or add dust VFX)
- Protector shield VFX (blue dome or particles)
- Buffer cast VFX (green pulse from caster to allies)

**Balance Tuning:**
- Chargers should be scary but avoidable (0.4s telegraph is enough warning)
- Protectors should feel impactful but not frustrating (1 block per shield prevents infinite stalling)
- Buffers should encourage target prioritization (kill buffer first to avoid buffed swarm)

---

### System Order Summary
```
1.  InputSystem
2.  SkillInputSystem
...
10. AiSeekSystem (base behavior)
11. RangedAttackSystem (ranged enemies)
12. AiChargerSystem ← NEW: Charger commit logic
13. AiProtectorSystem ← NEW: Protector shield activation
14. AiBufferSystem ← NEW: Buffer aura casting
15. BuffTickSystem ← NEW: Tick buff durations
16. MovementIntentSystem
17. KnockbackSystem (handles charger knockback)
18. MovementSystem
19. CollisionSystem
20. DynamicSeparationSystem
21. ContactDamageSystem
22. MeleeHitSystem
23. ProjectileHitSystem (checks ShieldActive)
24. HitReactionSystem
25. StatRecalculationSystem (integrates buff modifiers)
...
```

---

### Rollout Strategy
1. **Phase 1-2 (Charger):** Implement and test charger behavior alone; verify telegraph and knockback work correctly
2. **Phase 3 (Protector):** Add protector after charger is stable; test projectile blocking in isolation
3. **Phase 4-5 (Buffer + Integration):** Add buffer last, integrate all roles into wave config, test mixed waves
4. **Phase 6-9 (Debug/Polish):** Add debug tools, run performance tests, tune balance, finalize documentation

---

### Integration Points with Other Tasks
**Task 033 (Dash/Defense):**
- Charger commit attacks should be dodgeable with i-frames
- Protector shield does not grant i-frames (only blocks projectiles)

**Task 034 (Status Effects):**
- Buffers can apply status immunity buffs (future extension)
- Chargers can apply Stun on commit hit (future extension)

**Task 036 (Elite Modifiers):**
- Elite chargers have faster commit speed (0.3s windup vs 0.4s)
- Elite protectors block 2 projectiles instead of 1
- Elite buffers apply stronger buffs (+40% vs +30%)

**Task 037 (Meta Progression):**
- Track "chargers dodged" and "protector shields broken" as session stats
- Achievements: "Block 10 projectiles with protectors" or "Kill 5 chargers mid-commit"

## Notes / Risks / Blockers
- Context: Mage is the first class with fire/arcane/frost skill & talent trees; enemy roles should pressure that kit without requiring other classes.
- Ensure protector blocking integrates with collision layers without breaking player projectiles.
- Buff stacking must align with the unified stat model; avoid double-application.
- Chargers must respect separation/knockback systems to prevent jitter.
- Keep state machines deterministic; avoid random jitter per frame—seed any randomness.
- Visual clarity: telegraph colors should differentiate roles to reduce confusion in hordes.

### Key Technical Risks

**1. Charger Pathfinding Deadlock**
- **Risk:** Charger commits toward target, but target moves; charger stuck in commit animation while out of range
- **Mitigation:** Add "commit cancellation" if target moves >200 units away during windup; transition back to Seeking
- **Fallback:** Cap commit duration at 1.0s; force transition to Cooldown even if hitbox never spawns

**2. Protector Shield Spam**
- **Risk:** Multiple protectors chain shields, creating invulnerable ally blob
- **Mitigation:** Shield does not stack; strongest shield overwrites weaker ones
- **Cap Enforcement:** Max 1 protector per wave (enforced in spawn logic)

**3. Buffer Buff Stacking Exploit**
- **Risk:** Multiple buffers spam buffs, creating super-powered enemies
- **Mitigation:** `TimedBuff` component refreshes duration instead of stacking; same buff type cannot stack
- **Integration:** Buffs use `StatModifiers.Combine()` which handles multiplicative stacking correctly

**4. Performance with Large Hordes**
- **Risk:** 40 enemies each running AI state machines and range checks every frame
- **Mitigation:** Throttle expensive checks (ally scans, projectile detection) to every 0.2-0.5s
- **Optimization:** Cache target lookups; use spatial partitioning if needed (future)

**5. Collision Jitter with Chargers**
- **Risk:** Charger applies knockback, gets separated by `DynamicSeparationSystem`, creates feedback loop
- **Mitigation:** Charger is briefly marked "immovable" (high mass) during commit; separation system skips immovable entities
- **Integration:** Reuse existing `Mass` component; temporarily set to 10.0 during commit, restore to 0.8 after

**6. Visual Confusion in Mixed Waves**
- **Risk:** Player cannot distinguish role behaviors in chaotic horde scenarios
- **Mitigation:** Strong telegraph differentiation: charger=red circle, protector=blue shield dome, buffer=green pulse
- **Audio Cues:** Each role has distinct SFX (charger grunt, protector clang, buffer chime)
- **Debug Overlay:** F5 toggle shows AI state above all enemies for testing

### Design Decisions & Rationale

**Why State Machine vs Complex AI?**
- State machines are deterministic, testable, and performant
- Avoid expensive pathfinding or blackboard systems for 40+ entities
- Clear behavior transitions make debugging easier

**Why Cooldowns Instead of Resource Systems?**
- Cooldowns are simple to tune and understand
- Resource systems (energy, mana) add complexity without clear benefit for enemy AI
- Cooldowns prevent spam and create natural pacing

**Why Soft Separation Instead of Hard Position Correction?**
- Velocity-based separation integrates smoothly with existing physics
- Hard position snapping causes visual pops and can break collision layers
- Iterative separation handles multi-entity stacking gracefully

**Why Single-Block Shield Instead of Multi-Block?**
- Multi-block shields frustrate players (feels like invulnerability)
- Single-block creates meaningful decision: "Do I bait the shield or focus other targets?"
- Keeps protector role impactful without being overpowered

**Why Buffs Don't Stack?**
- Stacking buffs exponentially broken (+30% × 3 = ~119% with multiplicative stacking)
- Refresh-on-reapply keeps buffs relevant without power creep
- Aligns with equipment/perk stacking rules (Task 029/031)

### Testing Strategy

**Unit Tests (Phase 7):**
- Test each role's state transitions in isolation
- Verify cooldowns prevent spam
- Test buff/shield/knockback integration with existing systems
- Validate wave spawn caps and role weighting

**Integration Tests:**
- Spawn mixed wave (all roles + base + elite + boss)
- Run 60-second simulation with AI input
- Verify no deadlocks, jitter, or runaway spawns
- Check frame time stays <16ms with 40 entities

**Playtest Focus:**
- Does charger commit feel fair? (0.4s telegraph enough warning?)
- Are protectors annoying or interesting? (shield block frequency)
- Do buffers encourage target prioritization? (kill buffer first)
- Visual clarity: Can player distinguish roles in horde?

### Future Extensions (Out of Scope)

**Not in this task, but possible future work:**
- **Healer Role:** Buffers that restore ally health (requires healing system)
- **Summoner Role:** Spawns weak minions (requires spawning during wave)
- **Controller Role:** Roots/stuns player (requires CC system from Task 034)
- **Formation AI:** Chargers flank while protectors front-line (requires group coordination)
- **Advanced Pathfinding:** A* or navmesh for complex level geometry (current: direct seek)
- **Blackboard AI:** Shared enemy knowledge (overkill for current scope)

---

## Summary

This task adds three distinct enemy AI roles—**Charger** (commit attacks with telegraph and knockback), **Protector** (projectile-blocking shields), and **Buffer** (ally buff auras)—to increase encounter variety and pressure different playstyles. The implementation uses simple state machines, reuses existing systems (collision, combat, telegraphs, buffs), and integrates with wave spawning via role-weighted configs. Debug tools and comprehensive testing ensure stable behavior in horde scenarios, while cooldowns and spawn caps prevent overwhelming the player. The design prioritizes readability (clear telegraphs), determinism (fixed-timestep state updates), and extensibility (future roles can follow the same pattern).

