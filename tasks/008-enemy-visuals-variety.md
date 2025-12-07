# Task: 008 - Enemy visuals & basic variety
- Status: done

## Summary
Swap enemy debug dots for actual enemy sprites and introduce a second enemy archetype to validate tuning and variety. Add minimal directional/facing animation so enemies read clearly while moving and attacking.

## Goals
- Render existing enemy (e.g., BoneHexer) with 128x128 sprite frames instead of debug dots.
- Add simple facing/animation (idle/run/attack or looped run) that reacts to movement direction.
- Introduce a second archetype with distinct speed/HP/damage and ensure it spawns via wave config.
- Keep health bars aligned and readable with camera scaling.

## Non Goals
- Complex AI behaviors, pathfinding, or advanced animation blending.
- Final art polish or VFX beyond basic readability.

## Acceptance criteria
- [ ] Primary enemy renders with sprite frames at correct scale/origin; no debug dot visible.
- [ ] Enemies face/move with a basic animation loop tied to velocity (or attack trigger).
- [ ] Health bars remain correctly anchored and scaled with virtual resolution.
- [ ] Second enemy archetype spawns in waves (configurable) with distinct stats and visuals.
- [ ] Uses 128x128 sprites and 128 PPU; origins set so positioning matches previous world coordinates.
- [ ] `dotnet build` passes.

## Definition of done
- Builds pass (`dotnet build`)
- Tests/play check done (if applicable)
- Docs updated (if applicable)
- Handoff notes added (if handing off)

## Plan
- Add sprite/animation components and draw path for enemies, replacing RenderDebug usage.
- Load enemy sprite sheets (15 frames/anim), apply PointClamp, set origins to match world positioning.
- Implement simple facing/looped run tied to velocity direction; optional attack pose if available.
- Add a second archetype (config + factory) and schedule spawns in wave config to validate variety.
- Verify in play: both archetypes spawn, animate, and health bars align; then run `dotnet build`.

## Notes / Risks / Blockers
- Character sprites are 128x128; pixel-per-unit is 128. Maintain consistent origin so collision/hitboxes remain aligned.
- Tiles are 128x256 with pivot X 0.5, Y 0.18 (unique tiles like cliffs may need Y ~0.43) — useful for visual alignment during testing.
- 15 frames per animation; choose frame timing that feels readable at 60 FPS without over-drawing.

