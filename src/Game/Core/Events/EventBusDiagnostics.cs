using System;
using System.Collections.Generic;

namespace TheLastMageStanding.Game.Core.Events;

public sealed record EventBusDiagnostics(
    int MaxPasses,
    int PendingEventCount,
    int SubscriberCount,
    IReadOnlyList<EventBusEventTypeDiagnostics> EventTypes);

public sealed record EventBusEventTypeDiagnostics(
    Type EventType,
    int PendingCount,
    int SubscriberCount);
