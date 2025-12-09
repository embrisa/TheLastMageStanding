# Task: 067 - Hub map environmental polish
- Status: backlog

## Summary
Improve the hub map (HubMap.tmx) with environmental details, decorations, and visual polish to make it feel like a lived-in magical hub rather than an empty test arena. Add props, lighting effects, ambient particles, and background music.

## Goals
- Add decorative tiles and props to hub map
- Place furniture, plants, magical effects around NPCs
- Add lighting/shadow layers for depth
- Add ambient particles (dust motes, magic sparkles, etc.)
- Add hub background music track
- Improve map layout for better flow and visual interest
- Ensure all areas are accessible (no dead ends)

## Non Goals
- Multiple hub maps or areas
- Interactive environment objects (levers, doors, etc.)
- NPC pathfinding or roaming
- Dynamic time-of-day or weather

## Acceptance criteria
- [ ] Hub map has decorative tile layers (walls, floor details, ceiling)
- [ ] Props placed near NPCs (bookshelves near archivist, weapons near vendor, etc.)
- [ ] Ambient particles spawn in hub (subtle, not distracting)
- [ ] Background music plays on hub entry (loops seamlessly)
- [ ] Music stops or changes when entering stage
- [ ] Hub feels visually distinct from stage maps
- [ ] No collision issues with new decorations
- [ ] `dotnet build` passes; manual playtest confirms improved atmosphere

## Definition of done
- Builds pass (`dotnet build`)
- Tests/play check done
- Docs updated (if applicable)
- Handoff notes added

## Plan
- Step 1: Design hub layout improvements (sketch or mock-up)
- Step 2: Create or source tileset assets for decorations
- Step 3: Edit HubMap.tmx in Tiled with new layers and props
- Step 4: Add collision regions for new obstacles if needed
- Step 5: Create ambient particle system for hub (or configure existing VfxSystem)
- Step 6: Source or create hub music track
- Step 7: Add hub music to Content.mgcb
- Step 8: Update MusicService to play hub track on scene enter
- Step 9: Test map navigation and collision
- Step 10: Iterate based on playtest feedback

## Notes / Risks / Blockers
- **Dependency**: Tileset assets need to be created or sourced (art task)
- **Dependency**: Music track needs to be created or sourced (audio task)
- **Design**: Hub theme should feel "safe haven" vs stage "combat arena"
- **Tech**: Check if VfxSystem supports ambient effects or needs extension
- **Risk**: Overdecorating can clutter and confuse navigation - keep it clean
- **UX**: Consider adding signs or markers to guide new players to NPCs
