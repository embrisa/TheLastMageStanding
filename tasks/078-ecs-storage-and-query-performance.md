# Task: 078 - Improve ECS storage and query performance
- Status: in-progress (implementation complete, manual smoke test pending)

## Summary
Optimize the custom ECS world implementation before enemy density and combat complexity increase further. The current dictionary-backed component pools and snapshot-based `ForEach` iteration are simple and correct enough for now, but they create avoidable allocations and repeated copy/set work every frame.

## Goals
- Reduce per-frame allocations in ECS iteration
- Improve component query throughput for common hot paths
- Preserve current gameplay semantics and test coverage while improving internals
- Establish a clearer path for scaling enemy counts, collisions, and projectiles

## Non Goals
- Rewriting all gameplay systems
- Switching to a third-party ECS library
- Gameplay feature work or balance tuning
- Premature micro-optimization without measurement

## Acceptance criteria
- [x] Hot-path ECS iteration avoids snapshot allocation on every `ForEach`
- [x] Component access patterns are improved for common multi-component queries
- [x] The new storage/query approach is documented in code or notes for future contributors
- [ ] Existing ECS tests still pass and no gameplay regressions are observed in smoke testing
- [x] `dotnet build` passes; relevant tests and/or profiling notes are captured in the task handoff

## Definition of done
- Builds pass (`dotnet build`)
- Tests/play check done
- Docs updated (if applicable)
- Handoff notes added

## Plan
- Step 1: Measure or identify the hottest ECS iteration paths
- Step 2: Decide on a low-risk improvement path (buffer reuse, sparse-set style pools, cached queries, or phased query APIs)
- Step 3: Implement the chosen storage/query improvements incrementally
- Step 4: Validate entity destruction and component mutation semantics still hold
- Step 5: Run existing ECS-related tests and smoke test high-entity scenarios
- Step 6: Document the new constraints and expected usage patterns

## Notes / Risks / Blockers
- **Risk**: ECS internals are central infrastructure and mistakes here can create hard-to-find bugs
- **Dependency**: Coordinate with Task 075 so runtime composition changes do not conflict with storage changes
- **Testing**: Collision, combat, wave, and projectile tests are especially relevant

## Handoff Notes
- Current status: `EcsWorld` now uses dense component pools with entity-id-to-dense-index lookup instead of dictionary-backed storage, and `ForEach` snapshots entity ids through `ArrayPool<int>` rather than allocating a fresh list per query. Multi-component iteration now chooses the smallest source pool before intersecting the remaining components. Build and relevant automated tests pass. Manual gameplay smoke coverage is still pending.
- Files changed:
  - `src/Game/Core/Ecs/EcsWorld.cs`
  - `src/Game.Tests/Ecs/EcsWorldTests.cs`
- Tests/build/manual checks run:
  - `dotnet test src/Game.Tests/TheLastMageStanding.Game.Tests.csproj --filter EcsWorldTests`
  - `dotnet test src/Game.Tests/TheLastMageStanding.Game.Tests.csproj --filter "FullyQualifiedName~Collision|FullyQualifiedName~Combat|FullyQualifiedName~Ai|FullyQualifiedName~Wave"`
  - `dotnet build`
  - Manual smoke test not run in this environment
- Next concrete step:
  - Run an interactive smoke test with dense enemy waves, projectile-heavy combat, and stage restart flow to confirm there are no live ECS regressions
- Decisions made:
  - Replaced dictionary component pools with sparse-set-style dense storage backed by resizable arrays
  - Replaced per-query `SnapshotEntities()` list allocation with pooled integer buffers rented from `ArrayPool<int>`
  - Preserved mutation-safe `ForEach` semantics by iterating a pooled entity-id snapshot instead of live dense arrays
  - Reduced common multi-component query work by selecting the smallest component pool as the iteration source
  - Switched entity alive tracking from `HashSet<int>` to a boolean index array because entity ids are monotonic and not reused
- Risks/blockers:
  - Manual gameplay validation is still needed because automated coverage does not exercise long-running stage sessions, dense projectile bursts, or live scene transitions under player input
  - This task intentionally did not introduce cached query objects yet; if future profiling still shows ECS hotspots, the next step should be measured query caching or phase-specific query reuse on top of the new pools
