# Task: 032 - Elites/boss waves & rewards
- Status: backlog

## Summary
Add elite and boss archetypes with unique, telegraphed attacks that stress-test the combat stack. Integrate them into wave pacing and grant meaningful rewards (guaranteed loot or perk reroll tokens) on kill.

## Goals
- Define elite/boss archetype data (HP, scale, resists, move speed, unique attacks/telegraphs) and ensure collider/hitbox sizes match visuals.
- Implement at least one elite and one boss attack pattern using existing systems (projectiles, directional hitboxes, hit-stop/telegraphs).
- Integrate spawn rules into wave configuration (spawn timing, caps, spacing) with tunable difficulty ramps.
- Add guaranteed reward drops (loot table bias, perk reroll token, or stat bonus pickup) on elite/boss death.
- Provide debug tooling to force-spawn elites/bosses and visualize their telegraphs/colliders.

## Non Goals
- Multi-phase cinematic bosses or cutscenes.
- Procedural boss generation or complex AI state machines.
- New asset pipelines beyond reuse of existing sprites/primitive FX.
- Networked co-op behavior.

## Acceptance criteria
- [ ] At least one elite archetype and one boss archetype spawn via wave config (e.g., elite by wave 5, boss by wave 10); they use unique telegraphed attacks.
- [ ] Elite/boss colliders and hitboxes are sized appropriately and respect existing collision/layer rules.
- [ ] Rewards drop reliably on kill (loot bias or reroll token) and are attributed to the player; drops integrate with loot system (Task 030).
- [ ] Debug command or toggle can spawn/test elite/boss and show their telegraphs; normal waves remain stable.
- [ ] Tests cover wave config parsing for elites/bosses and reward drop attribution; `dotnet build` passes.

## Definition of done
- Builds pass (`dotnet build`)
- Tests/play check done (if applicable)
- Docs updated (elite/boss archetypes, wave config, rewards)
- Handoff notes added (if handing off)

## Plan
- Step 1: Define elite/boss archetype data (stats, colliders, attacks) and implement at least one of each using existing systems.
- Step 2: Integrate elite/boss spawning into wave configuration with pacing rules and debug spawn hooks.
- Step 3: Hook rewards into death handling (loot bias or reroll token) and ensure attribution to player.
- Step 4: Add tests for config parsing and reward drops; run build/play check and a focused playtest.

## Notes / Risks / Blockers
- Larger colliders/hitboxes may pressure collision performance; watch broadphase cell sizes.
- Telegraphed attacks must remain readable amid hordes; lean on Task 028 FX/telegraphs.
- Reward tuning can destabilize economy; start conservative and iterate.
- Spawn positioning must avoid overlaps with static colliders or player spawn area.

