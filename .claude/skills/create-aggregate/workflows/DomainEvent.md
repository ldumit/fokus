# Create Domain Event

## Pattern

**Reference:** `src/Services/Submission/Submission.Domain/Events/`

### With Action (services using IArticleAction)

```csharp
public record {EventName}({AggregateName} {AggregateName}, IArticleAction Action) : DomainEvent(Action);
```

### Without Action (simple events)

```csharp
public record {EventName}({AggregateName} {AggregateName}) : IDomainEvent;
```

Journals uses this simpler form — no action parameter.

## Raising Events

In the aggregate behavior method (in `Behaviors/{Aggregate}.cs`):

```csharp
public void {Method}({Params}, IArticleAction action)
{
    // validate + mutate
    AddDomainEvent(new {EventName}(this, action));
}
```

## Handling Events

Domain event handlers bridge to side effects (integration events, emails, timeline entries).

### MediatR variant (Submission, Review, Production):
```csharp
public class {HandlerName}({Dependencies})
    : INotificationHandler<{EventName}>
{
    public async Task Handle({EventName} notification, CancellationToken ct)
    {
        // publish integration event, send email, etc.
    }
}
```

### FastEndpoints variant (Auth, Journals):
```csharp
public class {HandlerName}({Dependencies})
    : IEventHandler<{EventName}>
{
    public async Task HandleAsync({EventName} notification, CancellationToken ct)
    {
        // publish integration event, send email, etc.
    }
}
```

Note the method name difference: `Handle` (MediatR) vs `HandleAsync` (FastEndpoints).

## Location

- Events: `{Service}.Domain/{AggregateName}/Events/{EventName}.cs`
- Handlers (MediatR):
  - Review: `{Service}.Application/Features/{Domain}/{Feature}/{HandlerName}.cs`
  - Submission: `{Service}.Application/Features/{Feature}/{HandlerName}.cs` (flat — no {Domain} grouping level)
  - Production: `{Service}.API/Features/{Domain}/{Feature}/{HandlerName}.cs` (uses MediatR but handlers live in API project, no separate Application layer)
- Handlers (FastEndpoints): `{Service}.API/Features/{Domain}/{Feature}/{HandlerName}.cs`
