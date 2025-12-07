# Task: Collider-driven combat hits
- Status: backlog

## Summary
Replace ad-hoc radius checks with collider-driven hit detection for melee/contact attacks. Use the collision system + event bus to manage hurtboxes/hitboxes, filters, and damage application, aligning attacks with animations.

## Goals
- Define attack hitbox/hurtbox components with layer/faction filtering (no friendly fire).
- Drive player melee and enemy contact damage through collision events instead of manual distance math.
- Support attaching hitboxes to animation frames/offsets for readability and future VFX sync.
- Ensure projectile/ranged work (Task 021) can reuse the same collider query utilities.

## Non Goals
- Complex combo systems or animation event authoring tools.
- Network replication/lag compensation.
- Hit-stop/camera shake tuning beyond basic hooks.

## Acceptance criteria
- [ ] Player melee attacks use collider hitboxes to damage valid targets via the event bus; old radius checks removed.
- [ ] Enemy contact damage uses collision events with cooldowns and proper faction filtering.
- [ ] Hitboxes can be toggled in debug view to verify alignment with animations.
- [ ] Tests cover hit filtering (ally vs enemy), cooldown handling, and legacy CombatSystem removal.

## Definition of done
- Builds pass (`dotnet build`)
- Tests/play check done (if applicable)
- Docs updated (if applicable)
- Handoff notes added (if handing off)

## Plan
- Add hitbox/hurtbox components with faction masks and optional cooldown metadata.
- Wire collision events into combat systems to apply damage and cooldowns; delete direct distance checks.
- Add animation/frame offset hooks for spawning/enabling hitboxes during attacks.
- Build targeted tests for filtering and cooldowns; validate in a test scene with debug overlays.

## Notes / Risks / Blockers
- Needs coordination with Task 021 projectiles to avoid duplicated hit logic.
- Animation timing may require temporary hardcoded windows until full animation events exist.
- Must ensure collision events are deterministic enough for combat feedback.
- **Insight:** Melee hitboxes are often "transient" entities (alive for only a few frames). Ensure the ECS can handle rapid creation/destruction efficiently, or use a pooling strategy for hitbox entities.
- **Insight:** Use a `[Flags]` enum for Collision Layers (e.g., `Player | Projectile`) to allow efficient bitmask filtering in the broadphase.
- **Insight:** Store an `OwnerEntityId` on hitbox components so damage events can reference the attacker for XP/aggro attribution. This also prevents self-hits (player's hitbox vs player's hurtbox).
- **Insight:** For melee swings, use a "one-shot" hit flag: track which entities have already been hit by this hitbox instance (e.g., `HashSet<int> AlreadyHit`) to prevent multi-hitting the same target if they remain in the hitbox for multiple frames.
- **Insight:** Coordinate with invulnerability frames (iframes): if an entity has a "DamagedRecently" cooldown component, either filter them out in the collision mask temporarily or check the cooldown in the damage-application system before applying damage.

