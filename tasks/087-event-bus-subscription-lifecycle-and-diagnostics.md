# Task: 087 - Event bus subscription lifecycle and diagnostics
- Status: done

## Summary
The event bus now has overflow protection, but subscription ownership is still manual and based on raw delegate storage. That leaves lifecycle management easy to get wrong during scene churn and makes runtime subscription state hard to inspect.

## Goals
- Make event subscription ownership explicit and easier to dispose safely
- Improve observability of event bus state during runtime
- Reduce the risk of leaked handlers during refactors and scene/world recreation
- Keep the event system lightweight and development-friendly

## Non Goals
- Replacing deferred event processing with synchronous dispatch
- Introducing multi-threaded event processing
- Reworking unrelated gameplay events for naming/style only

## Acceptance criteria
- [x] `Subscribe` returns a disposable/tokenized ownership model or equivalent lifecycle-safe mechanism
- [x] Systems can clean up subscriptions without relying on scattered manual unsubscribe calls
- [x] Runtime diagnostics expose useful subscriber/event queue information for debugging
- [x] Tests cover subscription cleanup and at least one leak-prone recreation path

## Definition of done
- Builds pass (`dotnet build`)
- Tests/play check done (if applicable)
- Docs updated (if applicable)
- Handoff notes added (if handing off)

## Plan
- Step 1: Audit current subscription/unsubscription patterns across long-lived and recreated systems
- Step 2: Introduce a safer subscription ownership API plus diagnostics hooks
- Step 3: Update key systems and add regression tests for cleanup behavior

## Notes / Risks / Blockers
- Coordinate with current event-bus hardening work to avoid duplicating API changes
- Diagnostics should stay lightweight enough for development builds

## Handoff Notes
- Current status: Completed.
- Files changed: `src/Game/Core/Events/IEventBus.cs`, `src/Game/Core/Events/EventBus.cs`, `src/Game/Core/Events/ScopedEventBus.cs`, `src/Game/Core/Events/EventSubscription.cs`, `src/Game/Core/Events/EventSubscriptionScope.cs`, `src/Game/Core/Events/EventBusDiagnostics.cs`, `src/Game/Core/UI/Myra/UiEventBridge.cs`, `src/Game/Core/Config/GameSettingsController.cs`, `src/Game/Core/MetaProgression/MetaProgressionManager.cs`, `src/Game.Tests/Events/EventBusTests.cs`, `src/Game.Tests/Events/ScopedEventBusTests.cs`
- Tests/build/manual checks run: `dotnet build`; `dotnet test src/Game.Tests --filter "FullyQualifiedName~EventBusTests|FullyQualifiedName~ScopedEventBusTests|FullyQualifiedName~MetaProgressionManagerTests"`
- Next concrete step: Manual runtime smoke-check if this task gets bundled with broader scene/runtime churn.
- Decisions made: Kept deferred dispatch unchanged; exposed diagnostics as a queryable snapshot instead of only logging.
- Risks/blockers: None currently identified.
