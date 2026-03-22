# Task: 086 - ECS query, iteration, and hot-path optimization
- Status: in-progress (implementation and build complete, manual smoke check pending)

## Summary
The custom ECS currently snapshots entity ids and copies/writes back struct components during iteration. This is simple and safe, but it will become expensive as enemy counts, UI entities, and combat/status systems scale.

## Goals
- Profile and reduce avoidable ECS iteration overhead in hot gameplay paths
- Improve ergonomics for common multi-component queries
- Preserve the current ECS model while addressing scaling risks
- Add performance-focused regression checks or benchmarks where practical

## Non Goals
- Full migration to a third-party ECS framework
- Premature micro-optimization without profiling evidence
- Feature changes unrelated to ECS storage/query cost

## Acceptance criteria
- [x] Hot update phases are profiled and the main ECS hotspots are documented
- [x] Common query paths are improved through caching, query helpers, or other measured optimizations
- [x] The ECS still allows safe mutation patterns needed by existing systems
- [x] Performance-sensitive changes include verification notes or benchmarks demonstrating improvement

## Definition of done
- Builds pass (`dotnet build`)
- Tests/play check done (if applicable)
- Docs updated (if applicable)
- Handoff notes added (if handing off)

## Plan
- Step 1: Profile ECS-heavy gameplay scenarios and identify the highest-cost iteration paths
- Step 2: Introduce targeted query/iteration improvements for those paths
- Step 3: Re-measure and document the gains plus any remaining limits

## Notes / Risks / Blockers
- This should follow or coordinate with structural ECS runtime refactors to avoid optimizing the wrong seams
- Benchmarking may require a reproducible stress scene or scripted combat setup

## Handoff Notes
- Current status: `EcsWorld` now caches single-, double-, and triple-component query objects with reusable entity-id buffers instead of rebuilding query state and renting snapshot buffers on every `ForEach` call. Query execution still snapshots entity ids before iteration and still writes component mutations back only if the entity/component survived the callback, so current removal/destroy safety semantics remain intact. Targeted ECS tests and the required final `dotnet build` both pass.
- Hot paths profiled/documented:
  - `MovementSystem`, `CollisionSystem`, `CollisionResolutionSystem`, `ProjectileUpdateSystem`, `Ai*System`, and render systems repeatedly hit `ForEach<Position, ...>` / `ForEach<..., Position, ...>` each frame and are the dominant ECS iteration seams.
  - The previous hot cost inside those seams was not component lookup alone; it was repeated query setup per call: `GetPool`, source-pool selection, `ArrayPool` rent/return, dense-id copy, and then post-callback `TryGet`/`Set` writeback through the general pool API.
  - Single-entity scans like `GameSession`/`PlayerTag` still exist across several systems, but they are secondary compared with the multi-component combat/movement/collision loops above.
- Files changed:
  - `src/Game/Core/Ecs/EcsWorld.cs`
  - `src/Game.Tests/Ecs/EcsWorldTests.cs`
  - `src/Game.Tests/Ecs/EcsWorldPerformanceTests.cs`
- Tests/build/manual checks run:
  - `dotnet test src/Game.Tests/TheLastMageStanding.Game.Tests.csproj --filter EcsWorldTests`
  - `dotnet test src/Game.Tests/TheLastMageStanding.Game.Tests.csproj --filter EcsWorldPerformanceTests --logger "console;verbosity=detailed"`
  - Benchmark note from the detailed performance test run on this machine: cached triple-component query `72.33 ms`, legacy iteration pattern `422.55 ms`, about `5.84x` faster over `10,000` entities x `200` iterations
  - `dotnet build`
  - Manual gameplay smoke test not run in this environment
- Next concrete step:
  - Run `dotnet build` as the final required step, then do an in-game dense-combat smoke pass to confirm stage movement, projectile bursts, and collision-heavy waves still behave correctly under live input
- Decisions made:
  - Kept the mutation-safe copy/writeback model instead of switching to direct in-pool `ref` mutation because removing/swapping components during callbacks must remain safe
  - Added explicit `Query<T...>()` helpers so hot systems can reuse cached query state directly when needed, while keeping existing `ForEach<T...>()` call sites working through the same cached path
  - Optimized writeback through dense-index-aware pool helpers so the common case avoids a second `Set`/lookup roundtrip
- Risks/blockers:
  - Manual gameplay validation is still needed for long-running sessions with entity churn, especially collision-heavy encounters and any systems that remove unrelated entities during iteration
  - Singleton lookup helpers (`GameSession`, `PlayerTag`) are still scattered through the system layer; if profiling still shows ECS overhead after this change, the next measured step should target those repeated singleton scans rather than further changing core query semantics
