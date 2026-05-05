# Create Endpoint — FastEndpoints

Used by: Auth, Journals, Production

Class extending `Endpoint<TRequest, TResponse>`. Handler logic lives directly in `HandleAsync` — no MediatR dispatch.

## Pattern

**Reference:** `src/Services/Auth/Auth.API/Features/Users/CreateAccount/CreateUserEndpoint.cs`

Create `{FeatureName}Endpoint.cs` in `Features/{Domain}/{FeatureName}/`:

```csharp
[Authorize(Roles = Role.{RequiredRole})]
[Http{Method}("{route}")]
[Tags("{Domain}")]
public class {FeatureName}Endpoint({Dependencies})
    : Endpoint<{CommandType}, {ResponseType}>
{
    public override async Task HandleAsync({CommandType} command, CancellationToken ct)
    {
        // Load aggregate/data
        // Execute domain logic
        // Save changes

        await Send.OkAsync(new {ResponseType}(...), ct);
    }
}
```

## Production Variant: BaseEndpoint

Production adds an abstract `BaseEndpoint<TCommand, TResponse>` that wraps common patterns:

**Reference:** `src/Services/Production/Production.API/Features/_Shared/BaseEndpoint.cs`

```csharp
public class {FeatureName}Endpoint({Dependencies})
    : BaseEndpoint<{CommandType}, {ResponseType}>
{
    public override async Task HandleAsync({CommandType} command, CancellationToken ct)
    {
        // ...
    }
}
```

## Registration

FastEndpoints auto-discovers endpoints — no manual registration needed.

## Alternative: Configure() partial class split

For complex configuration, split into two partial files:

**`{FeatureName}Endpoint.Configure.cs`:**
```csharp
public partial class {FeatureName}Endpoint
{
    public override void Configure()
    {
        AllowAnonymous(); // or Roles(...)
        Post("{route}");
        Description(x => x.WithSummary("...").WithTags("{Domain}"));
    }
}
```

## Notes

- Journals: handler class name sometimes ends with `QueryHandler` or `CommandHandler` even though it's an endpoint (legacy naming — avoid in new code)
- Production validators extend `BaseValidator<T>` (custom, wraps `Validator<T>`)
- Use `Send.OkAsync()` for all FastEndpoints responses — the pattern used across all services
- Domain events: dispatched automatically via interceptor on SaveChanges, or via `await PublishAsync(new {Event}(...))` for FastEndpoints events
