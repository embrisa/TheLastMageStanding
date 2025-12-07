# Task: 007 - Player visuals & animation hookup
- Status: done

## Summary
Replace the debug dot player rendering with real sprite-based visuals and basic animation states (idle/run/facing) that respect the game’s virtual resolution and camera scaling. Improves readability and grounds future VFX/UX work.

## Goals
- Render the player using the existing 128x128 character sprites instead of a debug dot.
- Drive idle/run (and basic facing) animation state from movement/input with consistent timing.
- Keep health bar/UI elements aligned to the player and camera scaling.
- Honor pixel-per-unit (128) and consistent origin/pivot handling.

## Non Goals
- Final VFX, SFX, or advanced animation blending.
- New abilities or combat changes beyond visuals/animation state hooks.

## Acceptance criteria
- [x] Player is drawn with sprite frames (128x128) at the correct scale/position; no debug dot remains.
- [x] Idle vs. run animation swaps based on movement input; facing/orientation reflected visually.
- [x] Health bar anchors cleanly to the player with the existing virtual resolution and scaling.
- [x] Uses 128 PPU conventions; origins/pivots are set so sprites align with world positions.
- [x] `dotnet build` passes.

## Definition of done
- Builds pass (`dotnet build`)
- Tests/play check done (if applicable)
- Docs updated (if applicable)
- Handoff notes added (if handing off)

## Plan
- Wire a simple sprite/animation component + system for the player (idle/run, facing).
- Load player sprite sheets (15 frames per animation) and set frame timing; apply PointClamp sampling.
- Replace debug draw with sprite draw using correct origin/scale; keep health bar offset intact.
- Validate in-game: idle, run in 8 directions (or 4), check alignment with camera and bars.
- Tidy docs/notes and run `dotnet build`.

## Notes / Risks / Blockers
- Player now rendered via `PlayerRenderSystem` animations (idle/run/facing) with correct origin/scale and health bar alignment.
- Character sprites are 128x128; pixel-per-unit is 128. Use consistent origins when converting from debug positions.
- Tiles are 128x256 with pivot X 0.5, Y 0.18 (unique tiles may need higher pivot e.g., Y 0.43) — keep this in mind if testing against terrain for alignment.
- 15 frames per animation; frame timing set (idle 6 fps, run 12 fps).
- Tests: `dotnet build` passing; manual playcheck covered idle/run/facing.

