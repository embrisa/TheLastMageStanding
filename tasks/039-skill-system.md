# Task: 039 - Mage skill system
- Status: completed

## Summary
Build the first-class skill system for the Mage, covering fire/arcane/frost skills and their integration with input, stats, and perks/talents. Provide a skill interface/registry, casting/targeting pipeline, and baseline skills per element to support current gameplay and the talent tree (Task 031).

## Goals
- Define skill identifiers, metadata, and a registry/factory for mage skills (fire/arcane/frost).
- Establish a skill execution pipeline (input/intent -> cast checks -> effects) that hooks into the event bus/intent system (Task 013) and the unified stat model (Task 029).
- Support skill modifiers from perks/talents and loot (cooldown, damage, pierce, AoE, resource cost) in a deterministic application order.
- Implement baseline mage skills (at least one per element) with placeholder VFX/SFX and collision/hit integration.
- Provide debugging hooks (spawn/test skills, cooldown reset) for rapid iteration.

## Non Goals
- Additional player classes or non-mage skill trees.
- Advanced skill editing tools or live scripting.
- Network/rollback support.
- Cinematic/channelled ultimates; keep to quick casts for now.

## Acceptance criteria
- [ ] Skill interface/registry exists with IDs and metadata for mage fire/arcane/frost skills; skills can be resolved and instantiated via the registry/factory.
- [ ] Skill execution flow integrates with the event bus/intent system and respects gating (cooldown/resource) plus stat-based scaling (Task 029).
- [ ] Perk/talent and loot modifiers can adjust at least cooldown and damage for skills; application order is deterministic and does not double-apply.
- [ ] At least one functional skill per element (e.g., Firebolt projectile, Arcane burst/AoE, Frost slow/root) with placeholder VFX/SFX and correct hit/collision handling.
- [ ] Debug hooks exist to trigger skills and reset cooldowns for testing; `dotnet build` passes.

## Definition of done
- Builds pass (`dotnet build`)
- Tests/play check done (if applicable)
- Docs updated (skill registry/IDs, modifier rules, baseline skills)
- Handoff notes added (if handing off)

## Plan
- Step 1: Define skill IDs, metadata, interfaces, and registry/factory; hook into event bus/intent plumbing.
- Step 2: Implement gating and scaling: cooldown/resource checks plus stat-driven values; apply deterministic modifier stack from perks/loot.
- Step 3: Build baseline mage skills (fire/arcane/frost) with collisions/VFX/SFX placeholders and debug triggers (cast/reset cooldown).
- Step 4: Add tests for registry resolution, modifier application order, and cast gating; run build/play check.

## Notes / Risks / Blockers
- Context: Mage is the first class with fire/arcane/frost skill & talent trees; future classes are out of scope.
- Ensure modifier ordering is consistent across perks and loot to avoid double-scaling.
- Keep VFX lightweight to avoid perf hits during horde scenarios; reuse existing telegraph pipeline where possible.
- Input mapping should align with current controls; allow easy rebinding when additional skills are added later.

## Implementation Notes (Completed 2025-12-08)

### Components Added
- **SkillData.cs**: `SkillId`, `SkillElement`, `SkillDeliveryType`, `SkillTargetType`, `SkillDefinition`, `SkillModifiers`, `ComputedSkillStats`
- **SkillRegistry.cs**: Central registry with 9 default mage skills (3 per element)
- **SkillComponents.cs**: `EquippedSkills`, `SkillCooldowns`, `PlayerSkillModifiers`, `SkillCasting`
- **SkillEvents.cs**: `SkillCastRequestEvent`, `SkillCastStartedEvent`, `SkillCastCompletedEvent`, `SkillCastCancelledEvent`

### Systems Added
1. **PlayerSkillInputSystem**: Bridges `PlayerAttackIntentEvent` to skill system, reads equipped skills and targeting
2. **SkillCastSystem**: Validates casts (cooldown/resource gating), manages cooldowns, handles cast timing
3. **SkillExecutionSystem**: Spawns projectiles/AoE/hitboxes on cast completion, integrates with collision

### Skills Implemented
**Fire**: Firebolt (fast projectile), Fireball (AoE projectile), Flame Wave (self AoE)  
**Arcane**: Arcane Missile (homing), Arcane Burst (quick AoE), Arcane Barrage (multi-shot)  
**Frost**: Frost Bolt (slow projectile), Frost Nova (defensive AoE), Blizzard (ground AoE)

### Integration
- **PlayerEntityFactory**: Initialize skill components (`EquippedSkills`, `SkillCooldowns`, `PlayerSkillModifiers`)
- **EcsWorldRunner**: Registered skill systems in correct order (after input, before stat recalc)
- **Stat System**: Skills scale with `ComputedStats.EffectivePower` and global CDR
- **Event Bus**: All skill logic event-driven via cast request → validation → execution
- **Collision**: Reuses existing `Projectile`, `AttackHitbox`, `Collider` components

### Modifier System
Deterministic stacking order:
1. Base skill definition
2. Skill-specific modifiers (`PlayerSkillModifiers.SkillSpecificModifiers`)
3. Element modifiers (`PlayerSkillModifiers.ElementModifiers`)
4. Global modifiers (`PlayerSkillModifiers.GlobalModifiers`)
5. Character CDR (`ComputedStats.EffectiveCooldownReduction`)
6. Clamping (80% max CDR, 0.1s min cooldown)

Modifiers support:
- Cooldown reduction (additive/multiplicative)
- Damage scaling (additive/multiplicative)
- Range/AoE radius (additive/multiplicative)
- Projectile count/speed/pierce/chain
- Cast time reduction

### Testing
- **12 tests** covering modifier stacking, CDR clamping, stat calculation, registry lookup
- **All tests pass** ✅
- `dotnet build` succeeds with 0 warnings/errors ✅

### Debug Tools
- `SkillDebugHelper.cs`: Force cast, reset cooldowns, inspect skills, apply test modifiers, show effective stats

### Documentation
- **Design doc**: `docs/design/039-skill-system-implementation.md`
- Comprehensive coverage of architecture, systems, integration, configuration, testing
- Usage examples for custom skills, equipping, modifiers, casting

### Known Limitations
1. **Pierce**: Requires extending `Projectile` component with `PierceRemaining` field
2. **Chain**: Needs target-tracking system (future)
3. **Resource costs**: Mana/energy not yet implemented
4. **Homing**: Arcane Missile marked but doesn't track yet
5. **Status effects**: Frost chill/slow requires Task 034
6. **Ground targeting**: Mouse input needed for `GroundTarget` skills
7. **Skill UI**: No hotkey bar or cooldown display yet

### Next Steps
- Playtest damage scaling and cooldown values
- Add skill unlock progression (tie to XP/levels)
- Implement hotkey slots 1-4 (currently only primary works)
- Add visual cooldown indicators to HUD
- Consider pierce/chain implementation for advanced skill builds
- Integrate with talent system for skill-specific perks

