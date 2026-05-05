# Create Endpoint — Minimal APIs

Used by: Submission

Static class with `Map(IEndpointRouteBuilder)` extension method. Dispatches to MediatR handler.

## Pattern

**Reference:** `src/Services/Submission/Submission.API/Endpoints/CreateArticleEndpoint.cs`

Create `{FeatureName}Endpoint.cs` in `Endpoints/`:

```csharp
public static class {FeatureName}Endpoint
{
    public static void Map(this IEndpointRouteBuilder app)
    {
        app.Map{Method}("/{domain}/{route}", async ([{FromRoute}] int id, {CommandType} command, ISender sender) =>
        {
            var result = await sender.Send(command with { Id = id });
            return Results.Ok(result);
        })
        .WithTags("{Domain}")
        .WithName("{FeatureName}")
        .Produces<{ResponseType}>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .RequireRoleAuthorization(Role.{RequiredRole});
    }
}
```

## Registration — MANUAL

Add to `EndpointRegistration.cs`:

```csharp
public static class EndpointRegistration
{
    public static IEndpointRouteBuilder MapAllEndpoints(this IEndpointRouteBuilder app)
    {
        var api = app.MapGroup("/api");

        CreateArticleEndpoint.Map(api);
        {FeatureName}Endpoint.Map(api); // ADD THIS LINE

        return app;
    }
}
```

## Handler Location

The endpoint dispatches to a handler in the Application project:
- `{Service}.Application/Features/{Domain}/{FeatureName}/{FeatureName}CommandHandler.cs`

## File Upload Variant

For file uploads, use `[FromForm]` + `.DisableAntiforgery()`:

```csharp
app.MapPost("/articles/{articleId}/files",
    async ([FromRoute] int articleId, [FromForm] {UploadCommand} command, ISender sender) => { ... })
    .DisableAntiforgery();
```
