# Task: 080 - Harden event bus behavior and runtime diagnostics
- Status: done

## Summary
Strengthen the event bus and runtime diagnostics so event-driven behavior is more predictable and easier to debug. The current bus silently caps cascading event processing after a fixed number of passes, and runtime logging is still mostly ad hoc `Console.WriteLine` output.

## Goals
- Make event processing limits explicit, observable, and safe
- Improve diagnostics when event chains exceed expected bounds
- Introduce a lightweight logging approach with categories or levels suitable for runtime systems
- Replace critical ad hoc `Console.WriteLine` usage in core runtime paths

## Non Goals
- Building a fully featured enterprise logging framework
- Replacing the event-driven architecture entirely
- Telemetry/analytics ingestion
- UI redesign for debug tooling

## Acceptance criteria
- [x] Event bus overflow or max-pass conditions are surfaced through clear diagnostics instead of failing silently
- [x] Event processing semantics are documented in code or adjacent notes
- [x] Core runtime systems use a shared logging abstraction or helper rather than scattered raw console writes
- [x] Debug logging can be gated or filtered by category/verbosity
- [x] Existing event-driven gameplay behavior remains intact
- [x] `dotnet build` passes; targeted tests or manual checks cover common event cascades

## Definition of done
- Builds pass (`dotnet build`)
- Tests/play check done
- Docs updated (if applicable)
- Handoff notes added

## Plan
- Step 1: Review current event publish/process usage and identify risky chains
- Step 2: Define desired event dispatch guarantees and overflow behavior
- Step 3: Update `EventBus` with diagnostics and safer processing semantics
- Step 4: Introduce a minimal logging abstraction/helper for runtime systems
- Step 5: Replace high-value `Console.WriteLine` sites in scene, persistence, audio, and ECS runtime code
- Step 6: Verify debug behavior and ordinary gameplay behavior still match expectations

## Notes / Risks / Blockers
- **Risk**: Changing dispatch semantics can surface hidden ordering assumptions in existing systems
- **Testing**: Scene transitions, run lifecycle, and collision/combat events should be exercised
- **Design**: Keep the logging layer lightweight and local to the project’s needs

## Handoff Notes
- Current status: completed. Follow-up hardening added `ScopedEventBus` so ECS/runtime subscriptions are owned per world and disposed cleanly when the runtime is replaced.
- Files changed:
  - `src/Game/Core/Diagnostics/*`
  - `src/Game/Core/Events/EventBus.cs`
  - `src/Game/Core/Events/EventBusOverflowException.cs`
  - `src/Game/Core/Events/IEventBus.cs`
  - `src/Game/Core/Events/ScopedEventBus.cs`
  - `src/Game/Game1.cs`
  - `src/Game/Core/SceneState/SceneManager.cs`
  - `src/Game/Core/SceneState/SceneRuntimeService.cs`
  - `src/Game/Core/Config/AudioSettingsStore.cs`
  - `src/Game/Core/Config/InputBindingsStore.cs`
  - `src/Game/Core/Config/VideoSettingsStore.cs`
  - `src/Game/Core/Player/EquipmentPersistence.cs`
  - `src/Game/Core/Player/PerkPersistence.cs`
  - `src/Game/Core/MetaProgression/PlayerProfileService.cs`
  - `src/Game/Core/MetaProgression/RunHistoryService.cs`
  - `src/Game/Core/MetaProgression/MetaProgressionManager.cs`
  - `src/Game/Core/Ecs/Systems/SfxSystem.cs`
  - `src/Game/Core/Ecs/Systems/VfxSystem.cs`
  - `src/Game/Core/Ecs/Systems/NpcSpawnSystem.cs`
  - `src/Game/Core/Ecs/Systems/DebugInputSystem.cs`
  - `src/Game/Core/Ecs/Systems/DebugCommandSystem.cs`
  - `src/Game/Core/World/Map/StaticColliderLoader.cs`
  - `src/Game/Core/Rendering/UI/SkillHotbarRenderer.cs`
  - `src/Game/Core/UI/Myra/UiSound.cs`
  - `src/Game.Tests/Events/EventBusTests.cs`
  - `src/Game.Tests/Events/ScopedEventBusTests.cs`
  - `src/Game.Tests/Diagnostics/RuntimeLogTests.cs`
- Tests/build/manual checks run:
  - `dotnet test src/Game.Tests/TheLastMageStanding.Game.Tests.csproj --filter "FullyQualifiedName~EventBusTests|FullyQualifiedName~ScopedEventBusTests|FullyQualifiedName~MetaProgressionManagerTests|FullyQualifiedName~SceneManagerTests"`
  - `dotnet build`
- Next concrete step:
  - Exercise scene transitions and a representative combat loop in a manual play check to confirm the new diagnostics stay quiet during normal gameplay and only fire on genuinely runaway cascades.
- Decisions made:
  - Event bus cascading work now fails loudly with `EventBusOverflowException` after the configured pass limit instead of truncating silently.
  - Runtime logging is centralized through a lightweight console-backed logger with category and minimum-level filtering driven by `TLS_RUNTIME_LOG_LEVEL` and `TLS_RUNTIME_LOG_CATEGORIES`.
  - High-value runtime, persistence, audio, scene, UI, and ECS debug paths now emit through the shared logger.
  - Added a scoped wrapper for the shared event bus so runtime systems can subscribe through the same queueing model without leaking handlers across disposed ECS worlds.
- Risks/blockers:
  - Runtime logging is currently console-backed only; if in-game diagnostics UI is added later, it should consume the same abstraction instead of introducing a parallel path.
