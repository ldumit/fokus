# Create Endpoint — Minimal APIs

Used by: services whose `CLAUDE.md` names minimal APIs with a mediator. The endpoint sends the command to its
handler in the Application project.

## Inputs
- Operation name, route and HTTP method
- The command or query and its reply (written first — `Handler.md`)
- Required roles

## Outputs
- `{Svc}.API/Endpoints/{Operation}Endpoint.cs`
- One line in the API project's endpoint registration

## Gate
Proceed only after: the service's `CLAUDE.md` confirms minimal APIs with a mediator, and `Handler.md` is done.

## Pattern

A static class with one `Map` extension method. Route values are set onto the command with a `with` expression,
and the token is passed to `Send`.

```csharp file=ShipOrderEndpoint.cs
public static class ShipOrderEndpoint
{
    public static void Map(this IEndpointRouteBuilder app)
    {
        app.MapPost("orders/{orderId:int}:ship", async (int orderId, ShipOrderCommand command, ISender sender, CancellationToken ct) =>
        {
            var response = await sender.Send(command with { OrderId = orderId }, ct);
            return Results.Ok(response);
        })
        .RequireRoleAuthorization(Role.Clerk, Role.Manager)
        .WithName("ShipOrder")
        .WithTags("Orders")
        .Produces<IdResponse>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status404NotFound);
    }
}
```

## Registration

Each endpoint is mapped by one explicit line in the API project's endpoint extensions, under the route group the
host creates once (the prefix is set there, never on an endpoint). A `Map…` method lives in an `Extensions` class,
never in a registration class (`service-registration` § Naming Conventions):

```csharp file=EndpointExtensions.cs
public static class EndpointExtensions
{
    public static IEndpointRouteBuilder MapAllEndpoints(this IEndpointRouteBuilder app)
    {
        var api = app.MapGroup("api");

        ShipOrderEndpoint.Map(api);

        return app;
    }
}
```

## Handler Location

`{Svc}.Application/Features/{Area}/{Operation}/{Operation}CommandHandler.cs` — see `Handler.md`.

## Errors

No `try/catch` in an endpoint and no `Results.Problem(…)` built for an error: the lookup throws, the domain throws,
and the central mapper turns each into its status (`error-handling`).

## File Upload Variant

A file upload binds the form into the command with `[FromForm]` and turns off the antiforgery check:

```csharp
app.MapPost("orders/{orderId:int}/files:upload", async (int orderId, [FromForm] UploadOrderFileCommand command, ISender sender, CancellationToken ct) =>
    Results.Ok(await sender.Send(command with { OrderId = orderId }, ct)))
    .DisableAntiforgery();
```
