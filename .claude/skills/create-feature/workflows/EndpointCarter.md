# Create Endpoint — Carter

Used by: services whose `CLAUDE.md` names Carter with a mediator. The endpoint sends the command to its handler in
the Application project.

## Inputs
- Operation name, route and HTTP method
- The command or query and its reply (written first — `Handler.md`)
- Required roles

## Outputs
- `{Svc}.API/Endpoints/{Operation}Endpoint.cs`

## Gate
Proceed only after: the service's `CLAUDE.md` confirms Carter with a mediator, and `Handler.md` is done.

## Pattern

One `ICarterModule` per operation. Route values are set onto the command with a `with` expression — the command's
route members are `init` — and the token is passed to `Send`.

```csharp file=ShipOrderEndpoint.cs
public class ShipOrderEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
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

A read binds its query-string values into the query record with `[AsParameters]`:

```csharp file=ListOrdersEndpoint.cs
public class ListOrdersEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("orders", async ([AsParameters] ListOrdersQuery query, ISender sender, CancellationToken ct) =>
        {
            var response = await sender.Send(query, ct);
            return Results.Ok(response);
        })
        .RequireRoleAuthorization(Role.Clerk)
        .WithName("ListOrders")
        .WithTags("Orders")
        .Produces<ListOrdersResponse>(StatusCodes.Status200OK);
    }
}
```

## Handler Location

`{Svc}.Application/Features/{Area}/{Operation}/{Operation}CommandHandler.cs` — see `Handler.md`.

## Registration

Carter finds its modules through `AddCarter()` and `app.MapCarter()` — no line per endpoint.

## Errors

No `try/catch` in an endpoint and no `Results.Problem(…)` built for an error: the lookup throws, the domain throws,
and the central mapper turns each into its status (`error-handling`).

## Access

Writes take the role gate and the resource gate together, through the one role-authorization extension
(`authorization-patterns` § Phase 2 — Endpoint gate). A bare `.RequireAuthorization()` is the read-model case only.
