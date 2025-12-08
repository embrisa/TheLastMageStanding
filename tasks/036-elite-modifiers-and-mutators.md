# Task: 036 - Elite modifiers & mutators
- Status: done

## Summary
Introduce stackable elite modifiers (mutators) that change enemy behavior and rewards (e.g., Extra Projectiles, Vampiric, Explosive Death, Shielded). Integrate with telegraphs/VFX and the unified stat/damage model to increase encounter variety and risk/reward.

## Goals
- Define elite modifier data (name, effects, telegraph cues, reward scaling) and attach them to elite archetypes at spawn.
- Implement at least four mods:
  - Extra Projectiles (fans or spreads)
  - Vampiric (lifesteal on hit)
  - Explosive Death (AoE on death with telegraph)
  - Shielded (temporary damage reduction or periodic shield)
- Ensure mods announce themselves (telegraph/VFX/SFX) and stack safely without duplicate effects.
- Scale rewards (loot bias, reroll tokens, or guaranteed drop) based on mod count/difficulty.
- Add debug hooks to spawn elites with specific mod sets for testing.

## Non Goals
- Full affix randomization for all enemies (limit to elites/bosses).
- Procedural boss phases; keep single-phase behaviors.
- Network/rollback support.
- New art pipelines; reuse existing FX/telegraph primitives.

## Acceptance criteria
- [ ] Elites can spawn with 1–N modifiers; each implemented mod functions and stacks without conflicts.
- [ ] Telegraph/VFX/SFX clearly indicate active modifiers (e.g., shield aura, explosive death warning circle).
- [ ] Rewards scale with modifier count; drops integrate with loot/perk systems (Tasks 030/031).
- [ ] Debug command/toggle can spawn elites with chosen mods; normal waves remain stable.
- [ ] Tests cover mod application, stacking, and reward scaling; `dotnet build` passes.

## Definition of done
- Builds pass (`dotnet build`)
- Tests/play check done (if applicable)
- Docs updated (modifier list, effects, telegraphs, reward rules)
- Handoff notes added (if handing off)

## Plan

### Phase 1: Data structures and modifier infrastructure (Foundation)

**1.1 Create modifier data model**
- Add `EliteModifierType` enum with: `ExtraProjectiles`, `Vampiric`, `ExplosiveDeath`, `Shielded`
- Create `EliteModifierDefinition` record struct:
  - `EliteModifierType Type`
  - `string DisplayName`
  - `TelegraphData? AuraOrIndicator` (visual cue, e.g., shield pulse, vampiric glow)
  - `Color TintOverlay` (subtle tint for affected enemy sprite)
  - `float RewardMultiplier` (1.0 = no bonus, 1.5 = 50% better drops)
  - Optional: `string SfxOnApply`, `string SfxOnTrigger`
- Create `EliteModifierData` component to attach active modifiers to entities:
  - `IReadOnlyList<EliteModifierType> ActiveModifiers`
  - Methods: `HasModifier(type)`, `GetModifierCount()`

**1.2 Create modifier config and registry**
- Add `EliteModifierRegistry` class with static definitions for the four mods:
  - Maps `EliteModifierType` → `EliteModifierDefinition`
  - Includes default telegraph/VFX/SFX and reward scaling per modifier
- Add modifier spawn rules to `EnemyWaveConfig` or new `EliteModifierConfig`:
  - Min/max modifiers per elite based on wave number (e.g., wave 5: 1 mod, wave 15: 2 mods)
  - Per-modifier unlock wave (e.g., `Vampiric` unlocks wave 7, `ExplosiveDeath` wave 10)
  - Weight/probability tables for random mod assignment

**1.3 Integrate with EnemyEntityFactory**
- Modify `EnemyEntityFactory.CreateEnemy` to accept optional `List<EliteModifierType>?`
- When creating elite/boss:
  - Roll modifiers based on wave index and config
  - Add `EliteModifierData` component with active modifiers
  - Apply visual telegraph/aura immediately (persistent `ActiveTelegraph` or aura VFX entity)
  - Apply sprite tint overlay if defined

**1.4 Extend LootDropper with modifier scaling**
- Add `float ModifierRewardMultiplier { get; set; }` to `LootDropper` component
- Calculate multiplier from active modifiers in factory: `prod(mod.RewardMultiplier)`
- Wire into `LootDropSystem` to scale drop chance or bias rarity roll

---

### Phase 2: Implement the four baseline modifiers

**2.1 Extra Projectiles modifier**
- **Where**: Hook into `EnemyRangedAttackSystem` (Task 021)
- **Logic**:
  - Check entity for `EliteModifierData.HasModifier(ExtraProjectiles)`
  - If true, spawn 2 additional projectiles at ±20° angles (fan pattern)
  - Use same projectile definition (speed, damage, lifetime)
- **Telegraph/VFX**:
  - Orange pulsing aura around ranged enemy with radius = attack range
  - Flash VFX on projectile spawn (`MuzzleFlash` + orange tint)
- **Testing**: Verify 3 projectiles spawn in fan; confirm no duplicate spawns per attack cycle

**2.2 Vampiric modifier**
- **Where**: Hook into damage application (likely `CombatSystem` or wherever `EntityDamagedEvent` is raised)
- **Logic**:
  - Listen for `EntityDamagedEvent` where attacker has `Vampiric` modifier
  - Heal attacker for % of damage dealt (e.g., 30%)
  - Clamp healing to `Health.Max` (no over-healing)
  - Skip if attacker is dead
- **Telegraph/VFX**:
  - Red pulsing aura around enemy (smaller radius, darker red)
  - Green "lifesteal" particle effect from victim to attacker on heal
  - Optional SFX: soft "drain" sound
- **Stat integration**: Consider adding `LifeStealPercent` to `OffensiveStats` and applying via `StatModifiers` for consistency
- **Testing**: Verify healing math; confirm clamp to max HP; test multi-hit scenarios (no double-heal)

**2.3 Explosive Death modifier**
- **Where**: Hook into `EnemyDeathSystem` or wherever `EnemyDiedEvent` is handled
- **Logic**:
  - On death, spawn telegraph entity at enemy position (1.5s duration, red circle, radius 60–80px)
  - After telegraph expires, deal AoE damage to all entities within radius (player + other enemies)
  - Use unified damage model: create damage event with `Power = explosionDamage`, respect armor/resists
  - Ensure collision layers prevent chain explosions (mark explosion source as environment/neutral)
- **Telegraph/VFX**:
  - Red expanding telegraph circle (1.5s windup)
  - Orange explosion VFX on detonation (reuse impact VFX scaled up)
  - Screen shake / camera nudge on explosion
  - SFX: windup hiss → explosion boom
- **Testing**: Verify telegraph timing is fair; confirm AoE hits player and enemies; test chain explosion prevention

**2.4 Shielded modifier**
- **Where**: Hook into damage calculation (before applying to `Health`, likely in `CombatSystem`)
- **Logic**:
  - Add `ShieldComponent` struct: `float ShieldValue`, `float RegenCooldown`, `float RegenRate`
  - Apply damage to shield first, then overflow to health
  - Shield recharges after `RegenCooldown` seconds of not taking damage
  - Alt: Fixed damage reduction % (e.g., 25% DR) via `StatModifiers.DamageReduction`
- **Telegraph/VFX**:
  - Blue pulsing shield aura (circular outline)
  - Shield "crack" VFX when broken
  - Subtle glow when shield is active vs. recharging (alpha fade)
- **Stat integration**: Use `DefensiveStats.Armor` or add `DamageReduction` field; apply via `StatModifiers` + `StatRecalculationSystem`
- **Testing**: Verify shield absorbs damage correctly; test shield regen timing; confirm overflow to health

---

### Phase 3: Stacking, spawn logic, and debug tools

**3.1 Ensure safe modifier stacking**
- Prevent duplicate modifiers on same entity (use `HashSet` or check in `EliteModifierData`)
- Define interaction rules:
  - `Vampiric` + `Shielded`: both apply independently (lifesteal heals HP, shield absorbs damage)
  - `ExtraProjectiles`: no stacking (only one instance)
  - `ExplosiveDeath`: no stacking (only one explosion)
- Document non-stacking mods in `EliteModifierRegistry` with `AllowStacking` flag

**3.2 Integrate with wave spawning**
- Update `WaveSchedulerSystem` or `EnemyWaveConfig.ChooseArchetype`:
  - When spawning elite, roll modifiers based on wave index
  - Pass modifier list to `EnemyEntityFactory.CreateEnemy`
- Add wave config parameters:
  - `MinModifiersPerElite`, `MaxModifiersPerElite` (scale with wave)
  - `ModifierUnlockWaves` dictionary
  - `ModifierWeights` for random selection

**3.3 Debug spawn commands**
- Extend `DebugInputSystem` with new keybinds (e.g., `Shift+1/2/3/4` to spawn elite with specific mod)
- Add command: `SpawnEliteWithModifiers(position, archetypeId, modifierTypes[])`
- Add debug overlay to display active modifiers on enemies (text labels or icons above sprite)

**3.4 Ensure normal waves remain stable**
- Guard all modifier logic behind `if (HasModifier(X))` checks
- No modifier application to non-elite enemies (check `EnemyTier` or `EliteModifierData` presence)
- Test that baseline enemies (no modifiers) still spawn and function correctly

---

### Phase 4: Reward scaling and loot integration

**4.1 Wire modifier count into loot system**
- In `EnemyEntityFactory`, calculate `ModifierRewardMultiplier`:
  - Example: `1.0 + (modifierCount * 0.25)` → 2 mods = 1.5x rewards
  - Cap at reasonable max (e.g., 2.0x for 4 mods)
- Store in `LootDropper.ModifierRewardMultiplier`

**4.2 Apply multiplier in LootDropSystem**
- Option A: Increase drop chance: `finalDropChance = baseChance * multiplier`
- Option B: Bias rarity roll: add weight to higher rarities (e.g., shift `Rare` weight up)
- Option C: Guaranteed drop + rarity bias for 3+ modifiers
- Preferred: Combo of A+B for smooth scaling

**4.3 Consider alternative rewards**
- Extra XP orbs on death (if Task 022 XP system exists)
- Reroll tokens (if perk system Task 031 uses them)
- Guaranteed legendary for bosses with 3+ modifiers

**4.4 Balance testing**
- Track drop rates in automated tests (spawn 100 elites, verify average drops)
- Play test: ensure rewards feel worth the risk (modifiers should be challenging but fair)
- Document reward curves in `docs/game-design-document.md` or new `036-elite-modifiers-rewards.md`

---

### Phase 5: Testing, docs, and polish

**5.1 Automated tests**
- `EliteModifierSystemTests`:
  - Verify modifier application (entity has correct `EliteModifierData`)
  - Test each modifier's core behavior (extra projectiles, lifesteal math, explosion AoE, shield absorption)
  - Test stacking prevention (no duplicate mods)
  - Test reward scaling calculation
- `LootDropRewardScalingTests`:
  - Verify modifier multiplier affects drop chance/rarity
  - Test edge cases (no modifiers, max modifiers)

**5.2 Integration / play tests**
- Spawn elites with 1–4 modifiers; verify each is visually clear and mechanically correct
- Test telegraph readability in crowded scenarios (multiple elites + normal enemies)
- Verify SFX doesn't spam (e.g., vampiric lifesteal sound every hit)

**5.3 Documentation**
- Create `docs/design/036-elite-modifiers-and-mutators.md` with:
  - Modifier definitions (effects, telegraphs, reward scaling)
  - System architecture (components, systems, flow diagrams)
  - Balance parameters (unlock waves, spawn weights, reward multipliers)
  - Debug commands and testing strategies
- Update `docs/game-design-document.md`:
  - Add "Elite Modifiers" section under Combat or Enemies
  - Link to detailed design doc

**5.4 Final build and handoff**
- Run `dotnet build` — fix all errors/warnings
- Run full test suite: `dotnet test`
- Play test: spawn waves 5–20, verify elites appear with modifiers, rewards feel appropriate
- Update task status and add handoff notes (next steps, open questions, tuning suggestions)

## Notes / Risks / Blockers

### Context
- Mage is the first class with fire/arcane/frost skill & talent trees; mod design and rewards should align with mage gameplay pacing.
- Existing systems to leverage:
  - **Telegraph system** (Task 028): `TelegraphData`, `ActiveTelegraph`, `TelegraphSystem.SpawnTelegraph`
  - **VFX/SFX** (Task 028): `VfxRequest`, `SfxRequest`, event-driven playback
  - **Loot system** (Task 030): `LootDropper` component, `LootDropSystem`, `LootDropConfig` with elite/boss drop chances
  - **Unified stats** (Task 029): `StatModifiers`, `ComputedStats`, `StatRecalculationSystem` for armor/resists
  - **Combat events**: `EntityDamagedEvent`, `EnemyDiedEvent` for hooking modifier triggers
  - **ECS world**: `EcsWorld`, component query patterns, system update order

### Implementation risks and mitigations
- **Stacking must be idempotent**: Guard against duplicate application on respawn or refresh.
  - *Mitigation*: Use `HashSet<EliteModifierType>` in `EliteModifierData`; check before applying effects.
- **Explosive death must respect collision layers and static geometry**: Ensure telegraph timing is fair.
  - *Mitigation*: Use existing collision query patterns; test telegraph duration (1.5s recommended); add safe zone indicator.
- **Vampiric should clamp healing to max HP**: Avoid multi-hit over-healing per frame.
  - *Mitigation*: Clamp `Health.Current = Math.Min(Current + heal, Max)` in heal logic; log if over-healing attempted.
- **Shielded/DR effects must use unified stat model hooks**: Avoid bypassing mitigation logic.
  - *Mitigation*: Integrate via `StatModifiers.DamageReduction` or `DefensiveStats.Armor`; run through `StatRecalculationSystem`.
- **Reward inflation risk**: Start conservative; consider soft caps per wave.
  - *Mitigation*: Use multiplicative scaling with cap (e.g., `min(2.0, 1.0 + count * 0.25)`); track drop rates in tests; adjust after play testing.

### Files likely to be created/modified

**New files:**
- `src/Game/Core/Ecs/Components/EliteModifierComponents.cs` — `EliteModifierData`, `ShieldComponent` (if needed)
- `src/Game/Core/Ecs/Config/EliteModifierConfig.cs` — Modifier definitions, spawn rules, unlock waves
- `src/Game/Core/Ecs/Systems/EliteModifierSystem.cs` — Apply modifier effects (vampiric heal, shield regen, etc.)
- `src/Game.Tests/Modifiers/EliteModifierTests.cs` — Unit tests for modifier application, stacking, rewards
- `docs/design/036-elite-modifiers-and-mutators.md` — Detailed design doc

**Modified files:**
- `src/Game/Core/Ecs/EnemyEntityFactory.cs` — Roll and attach modifiers; apply visual cues
- `src/Game/Core/Ecs/Systems/EnemyRangedAttackSystem.cs` — Hook extra projectiles modifier
- `src/Game/Core/Ecs/Systems/CombatSystem.cs` (or equivalent) — Hook vampiric lifesteal, shielded DR
- `src/Game/Core/Ecs/Systems/EnemyDeathSystem.cs` (or death handler) — Hook explosive death
- `src/Game/Core/Ecs/Systems/LootSystems.cs` (`LootDropSystem`) — Apply modifier reward multiplier
- `src/Game/Core/Ecs/Systems/DebugInputSystem.cs` — Add debug spawn commands
- `src/Game/Core/Ecs/Systems/WaveSchedulerSystem.cs` — Integrate modifier rolling into wave spawns
- `src/Game/Core/Ecs/Config/EnemyWaveConfig.cs` — Add modifier config parameters
- `docs/game-design-document.md` — Add "Elite Modifiers" section

### Performance considerations
- Modifier checks should be cheap: use component presence (`HasComponent<EliteModifierData>`) before iterating modifiers.
- Telegraph/VFX pooling: reuse existing `VfxSystem` pooling; avoid spawning hundreds of aura entities.
- Explosive death AoE: use spatial queries (existing collision system); don't iterate all entities.
- Vampiric lifesteal: trigger only on `EntityDamagedEvent`; don't poll every frame.

### Design notes and tuning suggestions
- **Modifier unlock pacing**: Start with 1 modifier at wave 5, add second modifier at wave 12, third at wave 20.
- **Reward scaling**: Conservative baseline of +25% per modifier (1 mod = 1.25x, 2 mods = 1.5x, 3 mods = 1.75x, cap at 2.0x).
- **Visual clarity**: Each modifier should have distinct color/VFX (orange=projectiles, red=vampiric, yellow=explosive, blue=shielded).
- **Sound design**: Keep SFX subtle for vampiric (could trigger 10+ times per second); use one-shot sounds for explosive death.
- **Balance**: Modifiers should increase difficulty by ~30% per mod; rewards should feel proportional to challenge increase.

### Open questions and future extensions
- **Modifier combinations**: Should certain combos be banned (e.g., Vampiric + Explosive Death = too punishing)? Current plan: allow all combos, test extensively.
- **Boss-only modifiers**: Consider adding unique mods for bosses in future (e.g., Enrage, Split, Summon Adds). Defer to Task 032 follow-up.
- **UI indicator**: Should player see mod icons above elite heads? Current plan: yes, use simple text labels in debug; add icon sprites in polish pass.
- **Modifier persistence**: If enemies despawn (out of range), should modifiers persist on respawn? Current plan: modifiers are set at spawn, don't change during lifetime.
- **Meta progression**: Should player unlock "bane" perks that counter specific modifiers (e.g., anti-shield skill)? Defer to Task 031/039 integration.

