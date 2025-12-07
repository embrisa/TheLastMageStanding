# Task: 003 - ECS foundation bootstrap
- Status: done

## Summary
Introduce a lightweight ECS runtime to replace the current object-based loop, defining the core components, system scheduler, and wiring into `Game1` so future gameplay features can migrate cleanly.

## Goals
- Select or implement a minimal ECS suitable for MonoGame (entity ids, component storage, system iteration).
- Add a world/registry lifecycle (init/update/draw) hooked into `Game1`.
- Define baseline components (Position, Velocity, Health, Faction/Team, Hitbox, AttackStats, RenderDebug).
- Establish system ordering (input → movement → AI → combat → cleanup → render) and a simple debug entity to validate the pipeline.

## Non Goals
- Migrating player/enemies or waves (handled by follow-up tasks).
- Advanced optimizations (arch queries, job systems).

## Acceptance criteria
- ECS world/registry initialized and ticked from `Game1` update/draw.
- Baseline components and system scheduler exist and are callable.
- A debug entity path demonstrates component add/remove and system iteration (logs or on-screen placeholder).

## Definition of done
- Builds pass (`dotnet build`)
- Tests/play check done (if applicable)
- Docs updated (if applicable)
- Handoff notes added (if handing off)

## Plan
- Evaluate tiny ECS implementation vs. small in-house structs; add entity id and component storage.
- Create system scheduler and lifecycle hooks (init/update/draw).
- Define baseline components and stub systems; add a debug entity to prove the loop runs.
- Wire ECS world into `Game1` and remove unused legacy world bootstrapping.

## Notes / Risks / Blockers
- Keep storage simple first (sparse sets/dictionaries) to minimize churn during migration.
- Ensure API allows later batching and rendering separation.
- ECS scaffold is live in `src/Game/Core/Ecs` with debug agents proving add/update/remove and draw.

