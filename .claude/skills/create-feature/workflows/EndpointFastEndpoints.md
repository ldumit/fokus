# Create Endpoint — FastEndpoints

Used by: services whose `CLAUDE.md` names FastEndpoints. No mediator: the endpoint's `HandleAsync` carries the
logic.

## Inputs
- Service name and feature area (from the plan step)
- Operation name — drives every file name
- Route and HTTP method
- Request and reply members
- Required roles, or anonymous access

## Outputs
- `{Svc}.API/Features/{Area}/{Operation}/{Operation}Endpoint.cs` — the endpoint, one type
- `{Svc}.API/Features/{Area}/{Operation}/{Operation}Command.cs` or `…Query.cs` — the request, its reply and its
  validator (the skill's § Requests, Replies and Names)
- `{Svc}.API/Features/{Area}/{Operation}/{Operation}Extensions.cs` — where the reply is mapped from a Domain result
  (`Mappings.md`)
- Optionally `{Operation}Summary.cs` — text for the API reference pages

## Gate
Proceed only after: the service's `CLAUDE.md` read, FastEndpoints confirmed, one existing feature of the service read.

## This Repo's Form

- **Access:** an admin-only endpoint carries `[Authorize(Policy = AccessPolicies.Admins)]`
  (`src/Services/Fokus/Fokus.API/Auth/AccessPolicies.cs`); every other endpoint takes authentication only, and the
  anonymous ones say so (`authorization-patterns` § This Repo's Form).
- **Routes are full paths on the attribute** (`[HttpGet("/api/analytics/bug-ratio")]`); there is no route prefix
  set in start-up, and the existing route parameters carry no type constraint (`{sprintId}`, `{accountId}`). The
  client (`client/src`) calls these addresses, so a route is not changed without moving the client's calls in the
  same change.
- **The request parameter** is `req` in most existing endpoints; new endpoints name it `command` or `query`.
- **The endpoint coordinates:** it reads the request, calls an Application capability class where the loading is
  large (a loader in `Fokus.Application/Analytics/{Area}/`), calls the Domain calculator (`domain-service`), and
  maps the result with its `{Operation}Extensions.cs`.
- **The one sanctioned `catch`:** `Sync/SyncSprints/SyncSprintsEndpoint.cs` catches the SignalR broadcast after a
  successful sync, logs it and keeps the sync reply — a best-effort side effect, not an error reply.

## Pattern

The endpoint derives `Endpoint<TRequest, TResponse>`. Route, access and tag are attributes, in the order
authorization, route, tags; the tag is the feature area. `HandleAsync` has a block body and passes the token on to
every call that takes one.

```csharp file=ShipOrderEndpoint.cs
[Authorize(Policy = Policy.Clerks)]
[HttpPost("orders/{orderId:int}:ship")]
[Tags("Orders")]
public class ShipOrderEndpoint(OrderRepository _orderRepository)
    : Endpoint<ShipOrderCommand, IdResponse>
{
    public override async Task HandleAsync(ShipOrderCommand command, CancellationToken ct)
    {
        var order = await _orderRepository.GetByIdOrThrowAsync(command.OrderId, ct);

        order.Ship(command.TrackingNumber, command);

        await _orderRepository.SaveChangesAsync(ct);

        await Send.OkAsync(new IdResponse(order.Id), ct);
    }
}
```

The route value `orderId` binds onto `ShipOrderCommand.OrderId`, and the body onto the rest of the command. The
role check and the resource check — may this caller ship this order — are the attribute's policy, not code here
(`authorization-patterns` § Phase 2 — Endpoint gate).

A read binds its query-string values onto the query record:

```csharp file=ListOrdersEndpoint.cs
[Authorize(Roles = Role.Clerk)]
[HttpGet("orders")]
[Tags("Orders")]
public class ListOrdersEndpoint(OrderReadStore _orderReadStore)
    : Endpoint<ListOrdersQuery, ListOrdersResponse>
{
    public override async Task HandleAsync(ListOrdersQuery query, CancellationToken ct)
    {
        var (orders, total) = await _orderReadStore.ListAsync(query.Status, query.Page, query.PageSize, ct);

        await Send.OkAsync(new ListOrdersResponse(orders.Select(order => order.ToOrderDto()).ToList(), total), ct);
    }
}
```

## API Reference Text

Where an endpoint needs text on the API reference pages, it goes in a summary class beside it — never a comment:

```csharp file=ShipOrderSummary.cs
public class ShipOrderSummary : Summary<ShipOrderEndpoint>
{
    public ShipOrderSummary()
    {
        Summary = "Ships an order";
        Response<IdResponse>(StatusCodes.Status200OK, "The order was shipped");
    }
}
```

## Shared Flow

Three or more sibling endpoints that share a flow derive a base endpoint in the area's `_Shared` folder (the skill's
§ Shared Base Endpoint). Below three, keep the lines in each endpoint.

## Registration

FastEndpoints finds its endpoints, validators and summary classes itself — no registration line.

## Errors

No `try/catch` in an endpoint, and no reply built for an error: a missing entity is thrown by the throwing lookup,
a broken rule by the domain, and the central mapper turns each into its status (`error-handling`). Never
`Send.NotFoundAsync`, `Send.ForbiddenAsync` or a hand-written error status. `AddError` and `ThrowIfAnyErrors`
belong to validation, never to business errors.

## Notes

- Replies go through the static `Send.…` API (`Send.OkAsync`, `Send.NoContentAsync`) — never the legacy
  `SendOkAsync` instance methods. `framework-currency` owns the migration.
- Domain events are published by the save interceptor when the data is saved (`create-domain-event-handler`); an
  endpoint never publishes them by hand.
- No response headers, and no settings read to branch on — except the headers a streaming reply needs.
