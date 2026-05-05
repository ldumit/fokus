# Create Integration Event Publisher

A domain event handler that bridges to MassTransit `IPublishEndpoint.Publish()`.

## MediatR Variant (Submission, Review, Production)

**Reference:** `src/Services/Submission/Submission.Application/Features/ApproveArticle/PublishIntegrationEventOnArticleApprovedHandler.cs`

**Important:** Re-load the aggregate with full includes before mapping to DTO. The domain event's aggregate may have lazy-loaded collections that the DTO needs.

```csharp
public class PublishIntegrationEventOn{DomainEvent}Handler({AggregateRepository} _repository, IPublishEndpoint _publishEndpoint)
    : INotificationHandler<{DomainEvent}>
{
    public async Task Handle({DomainEvent} notification, CancellationToken ct)
    {
        // Re-load with full includes for complete DTO mapping
        var entity = await _repository.GetFull{Entity}ByIdAsync(notification.{Entity}.Id);

        var dto = entity.Adapt<{DtoType}>();
        await _publishEndpoint.Publish(new {EventName}Event(dto), ct);
    }
}
```

## FastEndpoints Variant (Auth, Journals)

**Reference:** `src/Services/Journals/Journals.API/Features/Journals/Create/PublishIntegrationEventOnJournalCreatedHandler.cs`

```csharp
public class PublishIntegrationEventOn{DomainEvent}Handler(Repository<{Entity}> _repository, IPublishEndpoint _publishEndpoint)
    : IEventHandler<{DomainEvent}>
{
    public async Task HandleAsync({DomainEvent} notification, CancellationToken ct)
    {
        var entity = await _repository.GetByIdAsync(notification.{Entity}.Id);
        var dto = entity.Adapt<{DtoType}>();
        await _publishEndpoint.Publish(new {EventName}Event(dto), ct);
    }
}
```

Note: `Handle` (MediatR) vs `HandleAsync` (FastEndpoints).

## Naming Convention

`PublishIntegrationEventOn{DomainEvent}Handler` — literal "IntegrationEvent" in the name, then the domain event that triggers it.

## Location

- MediatR: `{Service}.Application/Features/{Domain}/{Feature}/`
- FastEndpoints: `{Service}.API/Features/{Domain}/{Feature}/`
