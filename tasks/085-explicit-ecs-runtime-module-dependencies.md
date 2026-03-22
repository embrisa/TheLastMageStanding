# Task: 085 - Explicit ECS runtime module dependencies
- Status: backlog

## Summary
ECS runtime modules and systems currently depend on registration order and side effects from other systems. Some systems require components to exist before they run, but those requirements are only enforced indirectly through module ordering.

## Goals
- Make ECS runtime dependencies explicit at composition time
- Fail early when required initialization/state is missing
- Reduce hidden ordering assumptions between modules and systems
- Improve maintainability of runtime registration as scene-specific systems grow

## Non Goals
- Replacing the custom ECS runtime architecture entirely
- Reordering systems solely for performance tuning
- Broad gameplay feature changes unrelated to composition safety

## Acceptance criteria
- [ ] Runtime/module composition encodes required dependencies explicitly rather than relying on array order alone
- [ ] Systems that require initialized session/settings/UI state fail clearly at composition/initialization time
- [ ] Runtime registration is easier to audit for scene scope and phase ownership
- [ ] Tests cover at least the critical composition invariants for stage and hub runtimes

## Definition of done
- Builds pass (`dotnet build`)
- Tests/play check done (if applicable)
- Docs updated (if applicable)
- Handoff notes added (if handing off)

## Plan
- Step 1: Identify runtime ordering assumptions and component prerequisites across modules/systems
- Step 2: Introduce dependency declarations or validated registration helpers
- Step 3: Add tests for invalid/missing runtime composition cases and update affected modules

## Notes / Risks / Blockers
- Strong overlap with existing runtime modularization work; sequence carefully with task 075/077-related changes
- Overcorrecting into a heavy framework would slow iteration; keep the solution lightweight
