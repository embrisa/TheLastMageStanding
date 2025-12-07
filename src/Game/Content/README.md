## Content pipeline

- Edit assets with the MonoGame Pipeline Tool: `dotnet mgcb-editor ./Content.mgcb`
- Build content: `dotnet mgcb /@Content.mgcb /platform:DesktopGL /output:bin/Content`
- The project already references `Content.mgcb`; building the game will invoke the content builder.

### Suggested folder layout

- `Sprites/` — characters, projectiles, UI atlases
- `Tiles/` — isometric tiles, terrain, props
- `Fonts/` — sprite fonts
- `Audio/` — music, SFX, VO
- `Effects/` — shaders and sprite effects

