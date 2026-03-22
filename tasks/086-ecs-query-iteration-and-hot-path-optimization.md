# Task: 086 - ECS query, iteration, and hot-path optimization
- Status: backlog

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
- [ ] Hot update phases are profiled and the main ECS hotspots are documented
- [ ] Common query paths are improved through caching, query helpers, or other measured optimizations
- [ ] The ECS still allows safe mutation patterns needed by existing systems
- [ ] Performance-sensitive changes include verification notes or benchmarks demonstrating improvement

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
