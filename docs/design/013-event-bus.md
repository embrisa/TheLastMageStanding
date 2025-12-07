# Event Bus & Intent System Design

## Overview
The Event Bus provides a decoupled communication mechanism for systems to exchange messages without direct dependencies or ad-hoc component flags. It supports a "fire-and-forget" pattern for events and intents, ensuring determinism and thread safety within the single-threaded ECS loop.

## Architecture

### Core Components
- **`IEventBus`**: The public interface for publishing and subscribing to events.
- **`EventBus`**: The implementation managing event queues and dispatching.
- **`GameEvent` (Structs)**: Events are defined as `struct`s to minimize GC pressure.

### Lifecycle & Ordering
1.  **Publishing**: Systems publish events via `bus.Publish(new MyEvent(...))`. Events are **queued** immediately, not dispatched.
2.  **Processing**: `EcsWorldRunner` calls `bus.ProcessEvents()` at the end of the `Update` loop (after all systems have run).
3.  **Dispatch**:
    -   The bus iterates through queued events.
    -   Handlers are invoked.
    -   If handlers publish new events, they are added to the queue and processed in the *same frame* (up to a max pass limit to prevent infinite loops).
    -   This ensures that an event chain (Input -> Intent -> Action -> Result) can resolve in a single frame if desired, or span multiple frames if designed so.

### ECS Integration
-   **Access**: The `EventBus` instance is attached to `EcsWorld.EventBus`.
-   **Initialization**: Systems can access `world.EventBus` during `Initialize` to subscribe.
-   **Runtime**: Systems can access `world.EventBus` during `Update` to publish.

### Memory Management
-   **Struct Events**: Events are structs to avoid allocation per event.
-   **Type-Safe Queues**: The bus uses `Queue<T>` internally to avoid boxing structs.
-   **Subscriber Lists**: Subscriber lists are managed to minimize allocations during dispatch.

## Usage Patterns

### 1. Defining an Event
```csharp
public readonly struct EntityDamagedEvent
{
    public readonly Entity Target;
    public readonly int Amount;
    public EntityDamagedEvent(Entity target, int amount) 
    { 
        Target = target;
        Amount = amount;
    }
}
```

### 2. Subscribing (in `Initialize`)
```csharp
public void Initialize(EcsWorld world)
{
    world.EventBus.Subscribe<EntityDamagedEvent>(OnEntityDamaged);
}

private void OnEntityDamaged(EntityDamagedEvent evt)
{
    // Handle damage
}
```

### 3. Publishing (in `Update`)
```csharp
public void Update(EcsWorld world, in EcsUpdateContext context)
{
    // ...
    world.EventBus.Publish(new EntityDamagedEvent(target, 10));
}
```

## Migration Strategy
1.  **Identify Ad-Hoc Components**: Look for components used solely as transient messages (e.g., `DamageEventComponent`).
2.  **Create Event Struct**: Define a corresponding struct event.
3.  **Update Producer**: Change `world.SetComponent(entity, new DamageEventComponent(...))` to `world.EventBus.Publish(new DamageEvent(...))`.
4.  **Update Consumer**:
    -   Remove iteration over `DamageEventComponent`.
    -   Subscribe to `DamageEvent`.
    -   Move logic to the handler.

## Future Considerations
-   **Global vs. Local**: Currently global. We might need entity-local buses for complex entities, but usually global with `EntityId` filter is enough.
-   **Immediate Dispatch**: We might add `PublishImmediate` for critical low-latency needs, but it risks reentrancy issues.
