# Task: 082 - Runtime composition and scene bootstrap decomposition
- Status: backlog

## Summary
`Game1`, `SceneRuntimeService`, and `EcsWorldRunner` currently share runtime composition, slot activation, scene bootstrap, and world construction responsibilities. This concentrates lifecycle logic into a few large classes and makes feature work depend on implicit boot order and growing constructor signatures.

## Goals
- Reduce constructor and service wiring sprawl across `Game1`, `SceneRuntimeService`, and `EcsWorldRunner`
- Move runtime/world creation into explicit factories or composition roots
- Keep scene bootstrap responsibilities clear between main menu, hub, and stage paths
- Make slot activation and scene reload behavior easier to reason about and test

## Non Goals
- Reworking gameplay systems unrelated to runtime composition
- Redesigning scene flow or campaign rules
- Adding compatibility layers for old bootstrap paths

## Acceptance criteria
- [ ] `Game1` is limited to MonoGame lifecycle, top-level rendering, and high-level routing
- [ ] Scene/world creation is delegated to explicit composition helpers instead of long constructor parameter lists
- [ ] `SceneRuntimeService` no longer owns unrelated concerns such as full runtime composition plus map/music/bootstrap orchestration in one class
- [ ] Slot activation, scene transition handling, and world lifetime rules are documented in code and covered by targeted tests where practical

## Definition of done
- Builds pass (`dotnet build`)
- Tests/play check done (if applicable)
- Docs updated (if applicable)
- Handoff notes added (if handing off)

## Plan
- Step 1: Map current responsibilities across `Game1`, `SceneRuntimeService`, and `EcsWorldRunner`
- Step 2: Introduce dedicated runtime factory/composition types and move wiring into them
- Step 3: Simplify the scene bootstrap flow and add regression coverage for slot/scene transitions

## Notes / Risks / Blockers
- Overlaps with existing refactor tasks around bootstrap and ECS runtime modularization; coordinate to avoid parallel rewrites of the same files
- Main risk is breaking scene transitions or slot-scoped persistence during extraction
