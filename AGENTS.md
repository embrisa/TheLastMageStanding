# The Last Mage Standing — Agent Guide

## Project context
- Game: 2D isometric story-driven ARPG with 4-act campaign structure.
- Campaign: Each act has multiple stages leading to an act boss; distinct biomes per act.
- Progression: Two-tier system:
  - **Meta progression** (hub): Level cap 60, unlocks skills/talents/equipment, persistent across runs.
  - **In-run progression** (stages): Level cap 60 per stage, level-ups grant choice between stat boost OR skill modifier.
- Hub model: Skills, talents, and equipment are configured in the hub ONLY; cannot change mid-run.
- Tech: .NET 9, MonoGame 3.8.4.1 (DesktopGL), C# with nullable + latest language features.
- Entry point: `src/Game` (`Game1` bootstraps camera/input/world stubs); content pipeline via `Content.mgcb`.

## How to run
- Restore tools/deps: `dotnet tool restore` then `dotnet restore`.
- Build: `dotnet build`.
- Run the game: `dotnet run --project src/Game`.
- Edit content: `cd src/Game && dotnet mgcb-editor ./Content.mgcb`.

## Architecture notes
- Rendering: virtual resolution scaled via render target; camera stub in `Core/Camera/Camera2D`.
- World: placeholder `Core/World/GameWorld` with `PlayerCharacter` and input-driven movement.
- Input: `Core/Input/InputState` normalizes WASD/arrow movement and escape-to-quit.
- Content: see `src/Game/Content/README.md` for pipeline commands and folder layout.

## Conventions
- C#: nullable enabled, implicit usings on, `AnalysisLevel=latest-recommended`.
- Keep namespaces under `TheLastMageStanding.Game.*`.
- Prefer pure logic in systems; keep MonoGame types at boundaries where practical.
- Fixed timestep @ 60 FPS; leverage camera/view matrices instead of manual offsets.
- Screen-space UI goes through the UI pass: implement `IUiDrawSystem` and register in `EcsWorldRunner.DrawUI` lists (not world draw lists) so centering and scaling stay correct.

## Branching / reviews / testing
- Branch per task; name `feature/<task-id>-short-desc` or `chore/<...>`.
- Include task link/id in PR title and description with acceptance criteria.
- Build + basic play check required before PR; add repro steps for bugs.
- Keep changelog in PR body; small, frequent merges preferred.

**End-of-work rule:** Always finish by running `dotnet build`, fix any errors, and then update the task status. This sequence must be the final step before handing off or stopping work.

## Task workflow (agents)
- Tasks live as individual files under `tasks/` created from `TASK_TEMPLATE.md`. Index and links are in `TASKS.md`.
- Update status and notes daily; mark blockers immediately with needs/asks.
- When handing off, leave: current status, next steps, decisions, links to WIP branches/PRs.
- When changing or extending game design, update `docs/game-design-document.md` alongside the relevant task so the GDD stays current.

