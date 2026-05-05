# Create MediatR Handler

Used by services with MediatR: Submission, Review

## Command + Handler Pattern

**Reference:** `src/Services/Submission/Submission.Application/Features/CreateArticle/CreateArticleCommandHandler.cs`

### Command file: `{FeatureName}Command.cs`

```csharp
public record {FeatureName}Command : ICommand<{ResponseType}>, IAuditableAction
{
    public int CreatedById { get; set; } // set by pipeline behavior
    public required int {EntityId} { get; init; }
    // other properties...
}
```

- Use `ICommand<T>` for mutations, `IQuery<T>` for reads
- Implement `IAuditableAction` if the command needs user ID assignment via pipeline behavior
- Validator class co-located in same file (see `workflows/Validator.md`)

### Handler file: `{FeatureName}CommandHandler.cs`

```csharp
public class {FeatureName}CommandHandler({Dependencies})
    : IRequestHandler<{FeatureName}Command, {ResponseType}>
{
    public async Task<{ResponseType}> Handle({FeatureName}Command command, CancellationToken ct)
    {
        // Load aggregate
        var entity = await _repository.FindByIdOrThrowAsync(command.{EntityId});

        // Execute domain logic
        entity.{DomainMethod}(command.{Params}, command);

        // Save (domain events dispatch via interceptor)
        await _repository.SaveChangesAsync();

        return new {ResponseType}(entity.Id);
    }
}
```

### Query Handler Pattern

```csharp
public class {FeatureName}QueryHandler({Dependencies})
    : IRequestHandler<{FeatureName}Query, {ResponseType}>
{
    public async Task<{ResponseType}> Handle({FeatureName}Query query, CancellationToken ct)
    {
        var entity = await _repository.FindByIdOrThrowAsync(query.Id);
        return entity.Adapt<{ResponseType}>();
    }
}
```

## Location

Handlers live in the Application project:
```
{Service}.Application/Features/{Domain}/{FeatureName}/
├── {FeatureName}Command.cs          (command record + validator)
└── {FeatureName}CommandHandler.cs   (handler)
```
