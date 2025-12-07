## Content pipeline

- Edit assets with the MonoGame Pipeline Tool: `dotnet mgcb-editor ./Content.mgcb`
- Build content: `dotnet mgcb /@Content.mgcb /platform:DesktopGL /output:bin/Content`
- The project already references `Content.mgcb`; building the game will invoke the content builder.

### Maps

- Maps live under `Tiles/Maps` with shared tilesets in `Tiles/Tilesets` (PascalCase).
- TMX files are built through the MonoGame.Extended tiled importer configured in `Content.mgcb`.
- Runtime toggle: set `TLMS_MAP=first` to load `FirstMap.tmx` (default is `HubMap.tmx`).

### Suggested folder layout

- `Sprites/` — characters, projectiles, UI atlases
- `Tiles/` — isometric tiles, terrain, props
- `Fonts/` — sprite fonts
- `Audio/` — music, SFX, VO
- `Effects/` — shaders and sprite effects
- `Tiles/Maps` — Tiled `.tmx` maps (`HubMap.tmx`, `FirstMap.tmx`)
- `Tiles/Tilesets` — shared tileset images/defs (`AncientRuinsTerrain`, `AnimatedTerrains8Frames`, `AncientRuinsWall8`, `PrototypeTileset`)

### Naming convention

- Files use PascalCase with no underscores (e.g., `CursorDefault.png`, `Stage1Music.mp3`, `FontRegularText.otf`).
- Use folder hierarchy for categories (e.g., `Sprites/ui`, `Sprites/icons/abilities`, `Sprites/abilities/Frost/GlacialSpike/Animation.png`).
- Asset-specific folders are also PascalCase to match the asset name (`BoneHexer`, `ArcanePulse`, `GlacialSpike`).

