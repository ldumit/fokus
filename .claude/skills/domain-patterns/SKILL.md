---
name: domain-patterns
description: DDD patterns — aggregates, entities, value objects, domain events, partial class split, state machines, event dispatch. Loaded when designing or implementing domain models.
user-invocable: false
---

# Domain Patterns

## AggregateRoot

**File:** `src/BuildingBlocks/Blocks.Domain/Entities/AggregateRoot.cs`

- Extends `Entity<TPrimaryKey>`, adds audit fields (`CreatedById`, `CreatedOn`, `LastModifiedById`, `LastModifiedOn`)
- Private `_domainEvents` list exposed as `IReadOnlyList<IDomainEvent>`
- Methods: `AddDomainEvent()`, `ClearDomainEvents()`
- Convenience form: `AggregateRoot` (non-generic, PK = int)
- **Variant:** Auth `User` extends `IdentityUser<int>` and implements `IAggregateRoot` manually (ASP.NET Identity constraint)

## Entity

**File:** `src/BuildingBlocks/Blocks.Domain/Entities/Entity.cs`

- `Entity<TPrimaryKey>` with `Id` property and equality by ID
- `IsNew` returns true when ID is default value
- Transient entities (both `IsNew`) are never considered equal

## Value Objects

**File:** `src/BuildingBlocks/Blocks.Domain/ValueObjects/`

Three base types:
- `ValueObject` — multi-property, implements `GetEqualityComponents()`
- `StringValueObject` — single string `Value`, full equality (`Equals`, `==`/`!=`, `GetHashCode`)
- `SingleValueObject<T>` — single struct `Value`, handles boxed `T` in `Equals(object?)`

Pattern: static `Create()` factory with validation, internal constructor:
```csharp
public class EmailAddress : StringValueObject
{
    internal EmailAddress(string value) { Value = value; }
    public static EmailAddress Create(string value)
    {
        Guard.ThrowIfNullOrWhiteSpace(value);
        return new EmailAddress(value);
    }
}
```

Real examples: `Auth.Domain/Persons/ValueObjects/EmailAddress.cs`, `Production.Domain/Assets/ValueObjects/AssetName.cs`

**Per-bounded-context duplication is intentional.** Each service defines its own VOs even if the shape is identical (e.g., `EmailAddress` in Auth, Review, Production). This follows DDD — each BC owns its types. Only share VOs through BuildingBlocks when they are truly cross-cutting (e.g., `SingleValueObject<T>` base).

## Partial Class Behavior Split

All services split aggregate state from behavior:
- `{Aggregate}.cs` — properties, backing collections
- `Behaviors/{Aggregate}.cs` — domain methods that mutate state and raise events

Backing collection pattern:
```csharp
private readonly List<Asset> _assets = new();
public IReadOnlyList<Asset> Assets => _assets.AsReadOnly();
```

Behavior method convention — **action parameter is typically last, but may precede factory parameters.** Review and Submission vary in ordering (e.g., Submission places action before stateMachineFactory in some methods, Review places it after). Prefer action last when writing new code:
```csharp
// Production — action last (preferred convention)
public void AssignTypesetter(Typesetter typesetter, ArticleStateMachineFactory stateMachineFactory, IArticleAction action)
{
    // validate via stateMachineFactory
    // mutate state
    AddDomainEvent(new TypesetterAssigned(typesetter.Id, typesetter.UserId!.Value, action));
}
```

## Domain Events

**Interface:** `IDomainEvent : INotification, IEvent` (dual MediatR + FastEndpoints)

**Base record:** `DomainEvent<TAction>(TAction Action)` where `TAction : IArticleAction`

Events are simple records:
```csharp
public record ArticleApproved(Article Article, IArticleAction Action) : DomainEvent(Action);
```

Journals uses `IDomainEvent` directly (no action parameter):
```csharp
public record JournalCreated(Journal Journal) : IDomainEvent;
```

## Event Dispatch

Two interceptor variants in `Blocks.EntityFrameworkCore/Interceptors/`:

| Interceptor | Used by | Behavior |
|-------------|---------|----------|
| `DispatchDomainEventsInterceptor` | Submission, Review | Dispatches after SaveChanges completes, no transaction |
| `TransactionalDispatchDomainEventsInterceptor` | Production | Wraps save + dispatch in single transaction, rolls back on failure |

Use **standard** by default. Use **transactional** when domain event handlers write to the same DB in the same request (e.g., Production + ArticleTimeline sharing a transaction).

Both scan `ChangeTracker` for aggregates with pending events, clear them, then publish via `IDomainEventPublisher`.

## IDomainEventPublisher — Framework Variants

| Implementation | Used by | Mechanism |
|---------------|---------|-----------|
| `Blocks.MediatR/DomainEventPublisher` | Submission, Review, Production | `IMediator.Publish()` → `INotificationHandler<T>` |
| `Blocks.FastEndpoints/DomainEventPublisher` | Auth, Journals | `IEvent.PublishAsync(Mode.WaitForAll)` → `IEventHandler<T>` |

The variant follows the CQRS/dispatch axis. Production uses FastEndpoints for endpoints but MediatR for event dispatch (it needs `INotificationHandler`).

## State Machine Pattern

**File:** `src/Services/Submission/Submission.Application/StateMachines/`

- `ArticleStateMachineFactory` registered as delegate factory — avoids injecting into aggregates
- Validates stage transitions via transition table stored in DB (`ArticleStageTransition`)
- Called in aggregate behavior methods before state mutation
- Per-service `ArticleActionType` enum defines allowed actions

Generalizable: replace `ArticleStage`/`ArticleActionType` with any entity states/transitions. The pattern is: DB-stored transition rules + factory-provided validator + aggregate-enforced invariants.
