# Task: Dynamic actor separation & knockback
- Status: backlog

## Summary
Prevent actors from stacking and add knockback/resolution so contacts feel physical. Use the collision system to separate player/enemy/enemy pairs and to apply impulses from attacks/projectiles.

## Goals
- Implement dynamic collider resolution between actors (player vs enemy, enemy vs enemy) to maintain minimum separation.
- Add knockback/impulse handling with decay, respecting world/static colliders.
- Ensure contact damage cadence uses collision results without jitter or double hits.
- Provide debug tooling to visualize separation/knockback vectors during testing.

## Non Goals
- Full rigidbody physics, rotations, or friction modeling.
- Crowd steering/avoidance AI beyond basic separation.
- Networked sync/rollback for impulses.

## Acceptance criteria
- [ ] Actors no longer overlap/stack when spawned together or during pathing; separation keeps them apart without jitter.
- [ ] Knockback from attacks/projectiles moves targets and stops against world colliders.
- [ ] Contact damage uses post-resolution state and respects per-entity cooldowns.
- [ ] Debug view can display applied separation/impulse vectors for test scenes.

## Definition of done
- Builds pass (`dotnet build`)
- Tests/play check done (if applicable)
- Docs updated (if applicable)
- Handoff notes added (if handing off)

## Plan
- Add resolution step that pushes intersecting actors apart using collision contacts.
- Implement a knockback/impulse component+system with decay over time and clamping vs world solids.
- Integrate contact damage timing with collision/contact data to avoid duplicate hits.
- Add test harness or deterministic scenarios to validate separation stability and knockback clamping.

## Notes / Risks / Blockers
- Resolution ordering can cause oscillation; may need position correction with bias.
- High-speed knockback may require swept checks to avoid tunneling through walls.
- Coordinate with combat/event systems to keep damage cadence consistent.
- **Insight:** For high-density hordes, "soft" separation (applying a repulsion force) often looks smoother than "hard" position correction (teleporting out of overlap), though it allows temporary slight overlaps.
- **Insight:** Apply knockback velocity to the movement vector *before* the frame's collision checks to ensure knocked-back entities don't tunnel through static world geometry.
- **Insight:** Limit separation iterations per frame (e.g., max 3-5 passes) to prevent perf spikes. Accept small overlaps rather than iterating to perfection—players won't notice 1-2px stacking in a horde.
- **Insight:** Consider a simple mass/weight component: when resolving overlap between two dynamic entities, push the lighter one more. E.g., player (mass=1.0) vs small enemy (mass=0.5) → enemy gets 2/3 of the separation, player gets 1/3. Keeps player from being shoved around by swarms.
- **Insight:** For knockback stacking (e.g., hit by multiple attacks in one frame), either sum all impulses and clamp the magnitude, or take the strongest impulse only. Summing can lead to extreme velocities; taking max feels more predictable.

