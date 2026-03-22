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
- Runtime: current gameplay path is ECS-driven; treat non-ECS prototype code as legacy unless a task explicitly says otherwise.
- Input: `Core/Input/InputState` normalizes WASD/arrow movement and escape-to-quit.
- Content: see `src/Game/Content/README.md` for pipeline commands and folder layout.

## Development policy
- This project is pre-release and under active refactor. Optimize for forward progress, clarity, and maintainability over compatibility with older local data or superseded code paths.
- Prefer breaking early and loudly during development rather than preserving silent fallbacks that hide problems.
- Backward compatibility is not required by default. Do not add migration layers, compatibility shims, or fallback behavior unless a task explicitly asks for them.
- When replacing a format, system, or workflow, update the codebase to the newest supported path and remove the old one instead of carrying both.
- If a stale file, save, config, or runtime path is incompatible with the current design, it is acceptable for it to stop working as part of development cleanup.

## Architecture guardrails
- Keep `Game1` thin: MonoGame lifecycle, top-level rendering, and composition/bootstrap only.
- Prefer extending existing systems/services over creating parallel flows that solve the same problem twice.
- ECS systems should have one clear responsibility and clear phase ownership (`Update`, `Draw`, `UI Draw`, content loading).
- Feature/module registration should be explicit; avoid growing a single global runtime wiring block without strong reason.
- Prefer pure logic in systems; keep MonoGame types at boundaries where practical.
- Persistence should be injected from a composition root; avoid ad hoc file access or `new DefaultFileSystem()` inside feature code unless that code is the composition root.
- Remove superseded code paths once replaced. If temporary duplication is unavoidable, mark it with a comment and task reference.

## Conventions
- C#: nullable enabled, implicit usings on, `AnalysisLevel=latest-recommended`.
- Keep namespaces under `TheLastMageStanding.Game.*`.
- Fixed timestep @ 60 FPS; leverage camera/view matrices instead of manual offsets.
- Screen-space UI goes through the UI pass: implement `IUiDrawSystem` and register in `EcsWorldRunner.DrawUI` lists (not world draw lists) so centering and scaling stay correct.
- Temporary debug logging is acceptable during development, but remove or consolidate it before handoff unless it is intentionally retained as runtime diagnostics.

## Persistence boundaries
- Global config data such as audio/video/input settings may live outside save slots.
- Player progression, hub state, run history, equipment/loadouts, and other player-owned gameplay data must be scoped to the active save slot unless a task explicitly says otherwise.
- New persistence code should document what is global, slot-scoped, or run-scoped.
- If a change introduces a new save file or modifies save structure, prefer the newest format only unless the task explicitly requires compatibility support.
- Do not keep legacy save migration code by default. If a data format changes during development, update the active path and remove the old compatibility path unless told otherwise.

## Branching / reviews / testing
- Branch per task; name `feature/<task-id>-short-desc` or `chore/<...>`.
- Include task link/id in PR title and description with acceptance criteria.
- Build + basic play check required before PR; add repro steps for bugs.
- Keep changelog in PR body; small, frequent merges preferred.
- Match verification depth to the change:
  - Systems/gameplay logic: run relevant tests if they exist; add tests when the change is logic-heavy and testable.
  - UI/rendering only: `dotnet build` plus a manual smoke check.
  - Persistence/config: `dotnet build` plus targeted persistence verification.
  - Scene/bootstrap/runtime composition: `dotnet build` plus hub/stage transition smoke test.

**End-of-work rule:** Always finish by running `dotnet build`, fix any errors, and then update the task status. This sequence must be the final step before handing off or stopping work.

## Task workflow (agents)
- Tasks live as individual files under `tasks/` created from `TASK_TEMPLATE.md`. Index and links are in `TASKS.md`.
- Every implementation task should update its task file before handoff.
- Update status and notes daily; mark blockers immediately with needs/asks.
- Keep acceptance criteria observable and testable. Do not use implementation steps as acceptance criteria.
- When handing off, leave at minimum:
  - current status
  - files changed
  - tests/build/manual checks run
  - next concrete step
  - decisions made
  - risks/blockers
  - links to WIP branches/PRs when available
- When changing or extending game design, update `docs/game-design-document.md` alongside the relevant task so the GDD stays current.
- Treat the GDD update as required when changing:
  - gameplay rules
  - progression rules
  - input or UI flow
  - save/persistence behavior
  - scene flow or hub/stage structure
- If work is architectural/refactor-heavy, note whether follow-up tasks depend on a specific implementation order.
