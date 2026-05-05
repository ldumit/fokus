# Handler Workflow

## Location Rule

The handler lives in `{Service}.API/Features/{Domain}/{Feature}/` — the feature folder it *serves*, not necessarily the one that raised the event.

- **Single handler for an event:** Place in the feature folder that triggers the event (cause and effect are the same feature).
- **Multiple handlers for the same event:** Each handler goes in the feature folder its *effect* belongs to.

## Naming Rule

Handler names describe their effect, not their trigger.

- **Effect matches event name (e.g., SignalR broadcast):** `Broadcast{EventName}Handler.cs` (short form).
  - Example: `CardCreatedEvent` -> `BroadcastCardCreatedHandler.cs`
- **Effect differs from event name:** `{EffectDescription}On{EventName}Handler.cs` (full form).
  - Example: `EvidenceLinkDetachedEvent` broadcasts "CardUpdated" -> `BroadcastCardUpdatedOnEvidenceLinkDetachedHandler.cs`
- **Multiple effects:** `{Effect1}And{Effect2}On{EventName}Handler.cs`.
  - Example: `EvidenceLinkAttachedEvent` broadcasts and enqueues a fetch -> `BroadcastAndFetchOnEvidenceLinkAttachedHandler.cs`

## Namespace

Matches the folder path exactly.

```
Reflekt.API.Features.Cards.CreateCard
```

## Pattern — FastEndpoints variant (Reflekt, Auth, Journals)

```csharp
public sealed class {HandlerName}({Dependencies})
    : IEventHandler<{EventName}>
{
    public async Task HandleAsync({EventName} ev, CancellationToken ct)
    {
        // side effect: SignalR broadcast, background job trigger, etc.
    }
}
```

## Pattern — MediatR variant (Submission, Review, Production)

```csharp
public sealed class {HandlerName}({Dependencies})
    : INotificationHandler<{EventName}>
{
    public async Task Handle({EventName} notification, CancellationToken ct)
    {
        // side effect
    }
}
```

## Reference

Canonical example:

```
src/Services/Reflekt/Reflekt.API/Features/Cards/CreateCard/BroadcastCardCreatedHandler.cs
```
