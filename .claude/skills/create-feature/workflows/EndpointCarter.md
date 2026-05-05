# Create Endpoint — Carter

Used by: Review, ArticleHub

Class implementing `ICarterModule` with fluent route building. Dispatches to MediatR handler.

## Pattern

**Reference:** `src/Services/Review/Review.API/Endpoints/Articles/AcceptArticleEndpoint.cs`

Create `{FeatureName}Endpoint.cs` in `Endpoints/{Domain}/`:

```csharp
public class {FeatureName}Endpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
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

## Handler Location

The endpoint dispatches to a handler in the Application project:
- `{Service}.Application/Features/{Domain}/{FeatureName}/{FeatureName}CommandHandler.cs`

## Registration

Carter auto-discovers modules via `AddCarter()` + `app.MapCarter()` — no manual registration.

## Notes

- Route parameters bind via `[FromRoute]`, body via `[FromBody]` or parameter name matching
- Multiple routes can be defined in a single `ICarterModule`
- Group-level authorization: apply `.RequireAuthorization()` on the route
