# Task: 010 - TMX map loading integration
- Status: done

## Summary
Wire the newly imported Tiled maps (`HubMap.tmx`, `FirstMap.tmx`) into the MonoGame project so the game can load and render them using the PascalCase tilesets now in `Content/Tiles/Tilesets`. Establish a repeatable path for future maps without manual path hacks.

## Goals
- Load TMX from `Content/Tiles/Maps` using an engine-friendly importer/loader (e.g., MonoGame.Extended Tiled, custom TMX parser, or a lightweight runtime loader) that resolves the PascalCase tilesets in `Content/Tiles/Tilesets`.
- Render at least one map (start with `HubMap.tmx`) in-game with correct tile sizing (32px) and alignment to the existing camera/virtual resolution.
- Ensure tileset images are sourced from the content pipeline (no absolute file paths) and survive `dotnet build`/`mgcb` outputs.
- Provide a simple map selection path so switching between hub/test maps is straightforward for future tasks.

## Non Goals
- Full collision/navmesh authoring beyond basic parsing of collision/object layers.
- Lighting, z-order depth sorting polish, or animated tiles beyond what the loader provides out-of-the-box.
- Streaming/large map chunking.

## Acceptance criteria
- [ ] `HubMap.tmx` loads at runtime from built content without referencing absolute disk paths.
- [ ] Tiles render with correct scale/origin at 128 PPU equivalent (32px tiles, respecting current camera virtual resolution).
- [ ] Tilesets resolve from `Content/Tiles/Tilesets` assets produced by `Content.mgcb`.
- [ ] A simple switch exists to load `FirstMap.tmx` instead of `HubMap.tmx` for testing (config flag or small code toggle).
- [ ] `dotnet build` passes.

## Definition of done
- Builds pass (`dotnet build`).
- Content builds succeed for the map assets.
- Docs updated if load paths/config are added.
- Handoff notes recorded if handing off.

## Plan
- Pick a TMX loading approach compatible with MonoGame (MGCB importer or runtime parser) and add dependencies if needed.
- Hook map loading into `Game1`/world bootstrap so `HubMap.tmx` renders; align tiles with camera scaling.
- Validate tileset resolution via built content (`bin/Content`) and confirm animated tiles (if supported) behave or are skipped gracefully.
- Add a simple toggle/config to load `FirstMap.tmx` for testing.
- Update docs/config to describe map locations and loader usage; run `dotnet build`.

## Notes / Risks / Blockers
- Implemented with MonoGame.Extended 5.3.1 pipeline/runtime. TMX builds via Content.mgcb with map toggle `TLMS_MAP=first`. Animated tiles pass through loader; object layers load without consumption yet.

