# Create Mediator Handler

Used by: services built on a mediator (Carter or minimal-API endpoints). A FastEndpoints service has no handler —
its logic lives in `HandleAsync`.

## Inputs
- Operation name and feature area
- Command (a write) or query (a read), and its reply
- The handler's dependencies

## Outputs
- `{Svc}.Application/Features/{Area}/{Operation}/{Operation}Command.cs` (or `…Query.cs`) — the request, its reply
  and its validator
- `{Svc}.Application/Features/{Area}/{Operation}/{Operation}CommandHandler.cs` (or `…QueryHandler.cs`)

## Gate
Proceed only after: the service's `CLAUDE.md` confirms a mediator.

## Command File

The command derives the area's command base, which carries the route id, the acting user and the time (the skill's
§ Requests, Replies and Names); the stamping hook fills the last two. Its validator lives in the same file:

```csharp file=ShipOrderCommand.cs
public record ShipOrderCommand : OrderCommand, ICommand<IdResponse>
{
    public required string TrackingNumber { get; init; }

    public override OrderActionType ActionType => OrderActionType.Ship;
}

public class ShipOrderCommandValidator : OrderCommandValidator<ShipOrderCommand>
{
    public ShipOrderCommandValidator()
    {
        RuleFor(command => command.TrackingNumber).RequiredWithMaxLength(MaxLength.Code);
    }
}
```

`ICommand<T>` marks a write and `IQuery<T>` a read; the markers state intent, and no behaviour branches on them
(`cqrs-patterns`).

## Handler File

The handler loads through the throwing lookup, calls the aggregate's behaviour with the command as its action
object, saves, and replies. Domain events go out when the data is saved; the handler never publishes them.

```csharp file=ShipOrderCommandHandler.cs
public class ShipOrderCommandHandler(OrderRepository _orderRepository)
    : IRequestHandler<ShipOrderCommand, IdResponse>
{
    public async Task<IdResponse> Handle(ShipOrderCommand command, CancellationToken ct)
    {
        var order = await _orderRepository.GetByIdOrThrowAsync(command.OrderId, ct);

        order.Ship(command.TrackingNumber, command);

        await _orderRepository.SaveChangesAsync(ct);

        return new IdResponse(order.Id);
    }
}
```

A query handler reads and maps by hand (`Mappings.md`):

```csharp file=GetOrderQueryHandler.cs
public class GetOrderQueryHandler(OrderRepository _orderRepository)
    : IRequestHandler<GetOrderQuery, GetOrderResponse>
{
    public async Task<GetOrderResponse> Handle(GetOrderQuery query, CancellationToken ct)
    {
        var order = await _orderRepository.GetByIdOrThrowAsync(query.OrderId, ct);

        return order.ToGetOrderResponse();
    }
}
```

## Location

```text
{Svc}.Application/Features/{Area}/{Operation}/
├── {Operation}Command.cs          the command, its reply and its validator
└── {Operation}CommandHandler.cs   the handler
```

## Rules

- Registering the handler follows `service-registration` § Registration Patterns.
- No logging in a handler — logging is central (the error middleware logs every mapped error).
- No `try/catch` that turns an error into a reply; no role check (`authorization-patterns`).
- Private steps of a handler end in `Async` when they are asynchronous; `Handle` keeps the framework's name.
