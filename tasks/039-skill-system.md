# Task: 039 - Mage skill system
- Status: backlog

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

