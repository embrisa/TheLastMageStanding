# Task: 013 - Event bus & intent system
- Status: done

## Summary
Design a robust, explicit event/intent system so gameplay, UI, and ECS systems can communicate via typed, ordered events without ad-hoc component hacks. Provide a plan for publish/subscribe APIs, lifecycle, ordering, and integration points that fit the current single-threaded MonoGame/ECS loop.

## Goals
- Define requirements and constraints for a central event/intent bus.
- Specify event categories (gameplay, VFX/SFX, UI, telemetry) and payload patterns.
- Establish ordering, delivery, lifetimes (one-shot vs. sticky), and priorities.
- Define API surface (publish, subscribe, filters) and ECS bridging patterns.
- Outline testing strategy and migration steps for existing damage/hit flows.

## Non Goals
- Implementing the full system now.
- Introducing multithreading or networking/event streaming.
- Refactoring all current systems to the new bus in one go.

## Acceptance criteria
- [x] Documented design/plan for an event/intent bus with clear API and lifecycle.
- [x] Delivery semantics: ordering, once/exactly-once per frame, priorities defined.
- [x] Strategy for ECS integration (component-to-event and event-to-component bridges).
- [x] Migration outline for at least one path (e.g., damage/hit feedback) using the bus.
- [x] Testing approach for determinism and perf (allocs/frame) described.
- [x] `dotnet build` passes.

## Definition of done
- Builds pass (`dotnet build`)
- Tests/play check done (if applicable)
- Docs updated (if applicable)
- Handoff notes added (if handing off)

## Plan
- Step 1: Capture use-cases and constraints (single-threaded, fixed update, ECS-centric).
- Step 2: Define event taxonomy and payload conventions (structs, pooled vs. transient).
- Step 3: Specify bus API (Publish/Subscribe), filtering (by type/topic/entity), and lifetime rules (frame-scoped queues, sticky intents).
- Step 4: Decide ordering and priority handling (per-queue FIFO, per-phase dispatch points in update loop).
- Step 5: Design ECS bridge patterns (event components -> bus; bus -> components/intent) and where to dispatch in `EcsWorldRunner`.
- Step 6: Outline instrumentation/testing (allocation budget, determinism checks, debug taps) and phased migration (start with damage/hit feedback).

## Notes / Risks / Blockers
- Implemented core `EventBus` in `src/Game/Core/Events`.
- Integrated into `EcsWorld` and `EcsWorldRunner`.
- Design doc at `docs/design/013-event-bus.md`.
- Must remain single-threaded and deterministic; avoid allocations each frame (consider struct events + pooling).
- Clear dispatch points in the frame are required to avoid reentrancy surprises.
- Need to ensure bus does not bypass ECS ownership rules; bridge patterns must be well-defined.

