# Create Integration Event Consumer

MassTransit consumer in the target service. Auto-discovered — no manual registration.

## Simple Consumer (Update/Upsert)

**Reference:** `src/Services/ArticleHub/ArticleHub.API/Articles/Consumers/ArticleAcceptedForProductionConsumer.cs`

```csharp
public sealed class {EventName}Consumer({DbContext} _dbContext)
    : IConsumer<{EventName}Event>
{
    public async Task Consume(ConsumeContext<{EventName}Event> context)
    {
        var dto = context.Message.{DtoName};

        // Idempotency: check if already processed
        var existing = await _dbContext.{Entities}
            .SingleOrDefaultAsync(e => e.Id == dto.Id);

        if (existing != null)
        {
            existing.{Property} = dto.{Property};
        }
        else
        {
            var entity = dto.Adapt<{Entity}>();
            await _dbContext.{Entities}.AddAsync(entity);
        }

        await _dbContext.SaveChangesAsync();
    }
}
```

## Rich Consumer (Factory Method + Supporting Entities)

**Reference:** `src/Services/Review/Review.Application/Features/Articles/InitializeFromSubmission/ArticleApprovedForReviewConsumer.cs`

When the consumer creates a full local aggregate with supporting entities:

```csharp
public sealed class {EventName}Consumer({Dependencies})
    : IConsumer<{EventName}Event>
{
    public async Task Consume(ConsumeContext<{EventName}Event> context)
    {
        var dto = context.Message.{DtoName};

        // Idempotency
        await _repository.EnsureNotExistsOrThrowAsync(dto.Id);

        // Factory method on aggregate
        var entity = {Entity}.From{Source}(dto);

        // Create supporting entities
        await CreateActors(dto);
        await GetOrCreateJournal(dto);

        await _repository.AddAsync(entity);
        await _repository.SaveChangesAsync();
    }
}
```

## Rules

1. **Consumers MUST be idempotent** — events may be delivered more than once
2. Check for existing records before inserting
3. Use `sealed class`
4. Inject DbContext or repository depending on the service's persistence pattern
5. Use Mapster for DTO → entity mapping
6. Factory methods (e.g., `Article.FromSubmission()`) keep creation logic in the domain

## Registration

Auto-discovered by MassTransit:
```csharp
config.AddConsumers(assembly);
```

Queue naming handled by `SnakeCaseWithServiceSuffixNameFormatter`.

## Naming Convention

`{EventName}Consumer` — matches the event name without "Event" suffix.
Example: `ArticleAcceptedForProductionEvent` → `ArticleAcceptedForProductionConsumer`

## Location

- ArticleHub: `ArticleHub.API/Articles/Consumers/`
- Other services: `{Service}.API/Features/{Domain}/Consumers/` or `{Service}.Application/Features/{Domain}/Consumers/`
