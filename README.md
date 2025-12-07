# The Last Mage Standing

2D isometric horde-survivor meets ARPG/Diablo: classes, skills, talents, loot, story, and meta progression. Built with C#/.NET 9 and MonoGame 3.8.4.1 (DesktopGL).

## Prerequisites
- .NET 9 SDK (pinned via `global.json`)
- MonoGame templates installed: `dotnet new install MonoGame.Templates.CSharp::3.8.4.1`
- macOS/Linux/Windows with GPU drivers capable of OpenGL

## Setup
```bash
dotnet tool restore
dotnet restore
```

## Run
```bash
dotnet run --project src/Game
```

## Build content
From `src/Game`:
```bash
dotnet mgcb /@Content.mgcb /platform:DesktopGL /output:bin/Content
```
Open the pipeline editor (macOS): `dotnet mgcb-editor-mac ./Content.mgcb` (use `mgcb-editor` on Windows/Linux). See `src/Game/Content/README.md`.

## Placeholder assets
- Imported temporary sprites, audio, and fonts from the prior prototype at `/Users/philippetillheden/untitledGame/assets` into `src/Game/Content`; entries are registered in `Content.mgcb` for early builds and can be replaced with final art later.
- Newly available sprite sets from that prototype:
  - Enemies: `BoneLich`, `CryptScuttler`, `GraveMage`, `PlagueWarg`
  - NPCs: `AbilityLoadoutNpc`, `ArchivistNpc`, `ArenaMasterNpc`, `TomeScribeNpc`, `VendorNpc`

## Project layout
- `TheLastMageStanding.sln` — solution
- `src/Game/` — MonoGame DesktopGL project
  - `Game1.cs` — bootstrap with virtual resolution + camera/input/world stubs
  - `Core/` — camera, input, player, world scaffolding
  - `Content/` — `Content.mgcb` and asset folders (Sprites, Tiles, Fonts, Audio, Effects)

## Tasks & agent docs
- Task workflow: `TASKS.md` (use `TASK_TEMPLATE.md` to add work items)
- Agent onboarding/expectations: `AGENTS.md`

