---
name: create-feature
description: Creates a complete feature slice — the endpoint file, the command or query file holding the request, its reply and its validator, the handler where the service uses a mediator, hand-written mapping, and registration — in either endpoint style (FastEndpoints, or a mediator with minimal or Carter endpoints). Use when adding an endpoint or a feature slice to a service; the service's CLAUDE.md names its one endpoint framework. This repo's form (FastEndpoints, no mediator, endpoint-owned replies mapped in {Operation}Extensions.cs) is stated first.
user-invocable: true
---

# Create Feature

Creates one feature slice: a folder per operation holding the endpoint and its command or query. Two endpoint
styles exist, and a rule that belongs to one of them says so:

- **FastEndpoints** — the endpoint class carries the logic in `HandleAsync`; no mediator, no handler class.
- **A mediator with minimal or Carter endpoints** — the endpoint sends the command to a handler in the
  Application project.

## Assumes

- **The service's `CLAUDE.md` names its one endpoint framework** — FastEndpoints, Carter or minimal APIs — and the
  service uses only that one.
- **A validation library** — FastEndpoints' `Validator<T>` or FluentValidation's `AbstractValidator<T>` — so every
  input has a validator that runs before the logic.
- **One central error mapper** turns errors into replies, so an endpoint never catches to build a reply. Where the
  repo has none yet, `error-handling` carries its sample; build it once under Look First below.
- **A repository with throwing lookups** (`GetByIdOrThrowAsync`) — `persistence-patterns` carries the base
  class's sample.
- No shared library, building block or module layout is assumed: every helper a sample here calls is named by its
  job and has a sample in the skill that owns it.

#### Look First

Before building anything a skill relies on — a guard helper, the central error mapper, a settings binder, an
aggregate or repository base, a save interceptor, an event publisher — look at what the repo already has: the shared
libraries under `src/BuildingBlocks/Blocks.*` (guards in `Blocks.Core`, the error family in `Blocks.Exceptions`, the
entity and aggregate bases in `Blocks.Domain`, the repository base and save interceptor in `Blocks.EntityFrameworkCore`,
the error middleware in `Blocks.AspNetCore`, the event publisher in `Blocks.FastEndpoints`), the module contracts
(`src/Modules/Jira/Jira.Contracts`, `src/Modules/Xray/Xray.Contracts`), and the packages in
`src/Directory.Packages.props`. Use what you find. Only otherwise build it once, in the shared place: the lowest
project every user of it already references; where the users share no project, a new `Blocks.*` project each of them
references (`create-building-blocks-package`), with no reference cycle. Never a copy beside each user.

## This Repo's Form

Fokus is one service, `src/Services/Fokus`, on **FastEndpoints with no mediator** (`src/Services/Fokus/CLAUDE.md`).
What applies here, and which rules below it departs from:

- **Layers:** the endpoint in `Fokus.API/Features/{Area}/{Operation}/` coordinates. Large loading goes to an
  Application capability class in `Fokus.Application` (a loader named for what it loads, never `…Service`);
  computation goes to a Domain calculator in `Fokus.Domain/Analytics` (`domain-service`). `…Service` names only a
  class around another system (the two sync classes).
- **The endpoint owns its reply:** reply types live in `{Operation}Query.cs` or `{Operation}Command.cs`, and the
  map from a Domain result is hand-written in `{Operation}Extensions.cs` (`workflows/Mappings.md`). No mapping
  library is used.
- **Existing shapes are kept, not converted:** most requests are classes named `{Operation}Request` (reads, in
  `{Operation}Query.cs`) or `{Operation}Command` (writes), with settable properties; replies are a mix of classes and
  records. New types follow § Requests, Replies and Names; an existing type is renamed or reshaped only when a plan
  step says so, and never in a way that changes the JSON the client reads.
- **No action base and no shared id reply yet:** the repo has no `IAction`/`ActionCommandBase` and no `IdResponse`;
  the acting user is read from the claims where needed (`authorization-patterns` § This Repo's Form). Adding them is
  open work.
- **Routes** are full paths on the attribute, without type constraints and without `:verb` actions; the client calls
  them, so they are not moved here (`workflows/EndpointFastEndpoints.md` § This Repo's Form).
- **One `_Shared` folder, for reply shapes:** `Fokus.API/Features/Analytics/_Shared/` (namespace
  `Fokus.API.Features.Analytics.Shared`) holds the reply types several analytics operations use — `MetricCardDto`
  with `SparklinePointDto`, `ScopeMetricCardDto`, `SprintRefDto`, `DayBreakdownEntryDto` — each beside its
  `…Extensions.cs` map. An operation uses these rather than declaring a same-shaped copy. Shared analytics logic
  lives in `Fokus.Domain/Analytics/Shared/`, never in `_Shared`.

## Steps

1. **Read the service's `CLAUDE.md` first** — the framework decides the workflow:
   - FastEndpoints → `workflows/EndpointFastEndpoints.md`
   - Carter with a mediator → `workflows/EndpointCarter.md`
   - Minimal APIs with a mediator → `workflows/EndpointMinimalApi.md`
2. **Read one existing feature of the same service** and copy its folder layout and naming.
3. **Write the command or query file** — the request, its reply and its validator (§ Requests, Replies and Names,
   § Validation and Binding; `workflows/Validator.md`).
4. **Write the endpoint file** with the style's workflow.
5. **Mediator style only:** write the handler — `workflows/Handler.md`.
6. **Map by hand** where a reply is built from an entity — `workflows/Mappings.md`.
7. **Register** — FastEndpoints and Carter find their endpoints themselves; a minimal-API endpoint gets one line in
   the API project's endpoint registration (the minimal-API workflow).
8. **Build** — `dotnet build`.

## Feature Folder Structure

A feature is always a folder: one folder per operation, `Features/{Area}/{Operation}/`, holding 1 to 5 files. The
endpoint lives in `{Operation}Endpoint.cs` and holds that one type. The request, its reply and its validator live
together in `{Operation}Command.cs` (a write) or `{Operation}Query.cs` (a read) — always, never inline in the
endpoint file. Pieces several operations of an area share live in that area's `_Shared` folder, which sorts first;
its namespace is `.Shared`. A feature area never references a sibling area, and there is no shared catch-all
folder of features.

FastEndpoints — everything in the API project:

```text
{Svc}.API/Features/{Area}/{Operation}/
├── {Operation}Endpoint.cs      the endpoint and its logic
├── {Operation}Command.cs       the request, its reply and its validator (or {Operation}Query.cs)
└── {Operation}Summary.cs       optional: text for the API reference pages
```

A mediator with minimal or Carter endpoints — the endpoint in the API project, the rest in the Application project:

```text
{Svc}.API/Endpoints/
└── {Operation}Endpoint.cs      sends the command to its handler

{Svc}.Application/Features/{Area}/{Operation}/
├── {Operation}Command.cs       the request, its reply and its validator (or {Operation}Query.cs)
└── {Operation}CommandHandler.cs
```

A type another operation also needs — a `{Thing}Dto`, a base command, a base endpoint — moves to `_Shared` when the
second user appears. A DTO only one reply uses gets its own file in that operation's folder.

## Requests, Replies and Names

- **Names.** Inputs are `{Operation}Command` (a write) or `{Operation}Query` (a read); outputs are
  `{Operation}Response`; a shape several replies share is `{Thing}Dto`. The endpoint class is named for its
  operation, verb first, and equals its folder and its command: `ShipOrder/ShipOrderEndpoint.cs` handles
  `ShipOrderCommand`.
- **Parameter names.** The request parameter is `command` or `query`; no abbreviations (`req`, `cmd`, `res`). The
  one short name is `ct` for the cancellation token.
- **Records.** Requests and replies are records. A request has `init` members, filled by binding; a reply is a
  positional record, built with positional arguments — named arguments only where two neighbouring members have the
  same type and a swap would compile (a choice: positional building is shorter, named arguments are safer there).
- **Create and update reply with the shared id reply.** It is built once, in the shared place:

```csharp file=IdResponse.cs
public record IdResponse(int Id);
```

- **A reply that carries a list is a named record holding the list** — never a bare list and never `object`, so
  the reply can grow (paging, totals) without breaking its readers:

```csharp file=ListOrdersQuery.cs
public record ListOrdersQuery
{
    public OrderStatus? Status { get; init; }

    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = 20;
}

public record ListOrdersResponse(List<OrderDto> Orders, int Total);

public class ListOrdersQueryValidator : Validator<ListOrdersQuery>
{
    public ListOrdersQueryValidator()
    {
        RuleFor(query => query.Status).IsInEnum();
        RuleFor(query => query.Page).GreaterThan(0);
        RuleFor(query => query.PageSize).InclusiveBetween(1, 100);
    }
}
```

- **The command base carries who acts and when.** Every write command derives one base record that names its
  action and holds the acting user and the time. The stamping hook (`authorization-patterns` § Phase 4 — Identity
  stamping — pick by framework) fills those two members from the token and the injected clock; the wire never sets
  them, so they are `[JsonIgnore]` — the one place a request record has a `set`. The domain's behaviour methods take
  the action object (`order.Ship(command.TrackingNumber, command)`), so the interface lives in the Domain project and the base record in
  the features' `_Shared` folder:

```csharp file=IAction.cs
public interface IAction
{
    int ActingUserId { get; set; }

    DateTimeOffset ActedOn { get; set; }

    string ActionName { get; }
}
```

```csharp file=ActionCommandBase.cs
using System.Text.Json.Serialization;

public abstract record ActionCommandBase<TActionType> : IAction
    where TActionType : struct, Enum
{
    [JsonIgnore]
    public abstract TActionType ActionType { get; }

    [JsonIgnore]
    public string ActionName => ActionType.ToString();

    [JsonIgnore]
    public int ActingUserId { get; set; }

    [JsonIgnore]
    public DateTimeOffset ActedOn { get; set; }
}
```

  An area's commands share the route id and its validation through one more base, in the area's `_Shared` folder;
  its file holds the family validator beside it. The samples show the FastEndpoints validator base (`Validator<T>`);
  the mediator style writes `AbstractValidator<T>`:

```csharp file=OrderCommand.cs
using System.Text.Json.Serialization;

public abstract record OrderCommand : ActionCommandBase<OrderActionType>, IOrderAction
{
    [JsonIgnore]
    public int OrderId { get; init; }
}

public abstract class OrderCommandValidator<TCommand> : Validator<TCommand>
    where TCommand : OrderCommand
{
    protected OrderCommandValidator()
    {
        RuleFor(command => command.OrderId).ValidId();
    }
}
```

  The area's action interface lives beside `IAction` in the Domain project, so a behaviour method or a state
  machine guard can read which action it is given:

```csharp file=IOrderAction.cs
public interface IOrderAction : IAction
{
    OrderActionType ActionType { get; }
}
```

```csharp file=ShipOrderCommand.cs
public record ShipOrderCommand : OrderCommand
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

  The length is the field-length constant the table configuration uses for the same column
  (`src/BuildingBlocks/Blocks.Core/MaxLength.cs` here), so a value the validator accepts always fits the table. A shared base validator is named for its family (`OrderCommandValidator<T>`); each command's own validator is
  `{Command}Validator` (`workflows/Validator.md`).

## Endpoint Styles

- **One endpoint framework per service,** named in the service's `CLAUDE.md`. Two frameworks in one service double
  what every reader must know.
- **FastEndpoints:** route, access and tag are attributes on the class, in the order authorization, route, tags;
  the tag is the feature area. `HandleAsync` has a block body. Text for the API reference pages, where an endpoint
  needs any, goes in a `{Operation}Summary` class beside it — never in a comment.
- **Mediator style:** the endpoint builds nothing itself — it sets the route values onto the command with a `with`
  expression, sends it with the token, and returns the reply. Route, access, name and tag are fluent calls on the
  route.
- **No error handling in an endpoint.** It has no `try/catch` that builds a reply and writes no error status by
  hand; a missing entity is thrown by the lookup and turned into a 404 by the central mapper
  (`error-handling` § Prohibitions (load-bearing)).
- **No access check inside an endpoint.** Access is declared on the endpoint (`authorization-patterns`).
- **Endpoints set no response headers and read no settings to branch on.** The one exception: an endpoint that
  streams its reply sets the headers streaming needs.
- **The route prefix and the JSON settings are set once,** in the host's start-up
  (`service-registration` § Program.cs Structure (self-hosted service)); an endpoint declares only the rest of its
  route.
- **Status codes by named constant** (`StatusCodes.Status404NotFound`), never a bare number.

## Validation and Binding

- **Every input has a validator,** route ids included: an id is validated greater than zero.
- **Required members are non-nullable and validated;** a member is nullable only when it is truly optional. Never
  `!.Value` on a request member.
- **Query-string inputs bind into the query record,** with defaults written on its members (`Page = 1`).
- **Every route parameter carries a type constraint** — `{orderId:int}`, `{code:guid}`.
- **Route and query values end up on the command or query record,** never passed on as separate arguments.
  FastEndpoints binds them there; the mediator style copies each route value onto the command once, with `with`,
  before it sends it. An endpoint without a request only where there is truly no input.
- **Validation messages** come from the shared message helpers or constants, never a literal per rule
  (`error-handling` § Validation Messages).

## Route Conventions

- **Resource first, `:verb` for an action.** Writes on a resource use its route (`POST orders`,
  `PUT orders/{orderId:int}`); an action that is not plain create, update or delete appends its verb after a colon
  (`POST orders/{orderId:int}:ship`, `POST orders/{orderId:int}/items/{itemId:int}:return`).
- **A route parameter names its entity and its type** — `{orderId:int}`, never `{id}` beside a second id.
- **A read binds from the query string** (`GET orders?status=Open&page=2`); it has no body. A delete takes its id
  from the route, has no body and replies 204.
- **A route is written as text on its endpoint.** A named constant only when a second place must read the same
  address.
- **Existing routes follow the form too.** Moving a route moves its callers in the same change: the repo's own
  pages and tests, and — where another system holds the address (a webhook, a sign-in return address, a link
  another app opens) — that system's registration. The cost is that one change spans every caller; with no outside
  caller it is small.

## Shared Base Endpoint

Sibling endpoints that share a flow — load the same aggregate, check the same state, reply the same way — inherit
it from a base endpoint in the area's `_Shared` folder, once three siblings share it. Below three, a base class
costs more than it saves: leave the lines in each endpoint. In the mediator style the same rule applies to a base
handler.

```csharp file=OrderEndpoint.cs
public abstract class OrderEndpoint<TCommand>(OrderRepository _orderRepository)
    : Endpoint<TCommand, IdResponse>
    where TCommand : OrderCommand
{
    public override async Task HandleAsync(TCommand command, CancellationToken ct)
    {
        var order = await _orderRepository.GetByIdOrThrowAsync(command.OrderId, ct);

        Apply(order, command);

        await _orderRepository.SaveChangesAsync(ct);

        await Send.OkAsync(new IdResponse(order.Id), ct);
    }

    protected abstract void Apply(Order order, TCommand command);
}
```

Logic two endpoints share is not moved into an operation class named `…Service`: the base endpoint (or base
handler) is the place, and a computation over several aggregates is a domain calculator (`domain-service`).

## Anti-patterns

- **Writing code before reading the service's `CLAUDE.md`.** The framework decides the workflow; finding it out
  mid-way means rewriting the endpoint.
- **A layout derived from this skill alone.** Copy the folder layout of an existing feature in the same service.
- **Registering a FastEndpoints endpoint by hand.** It finds its endpoints itself; a manual line routes twice.
- **The request or reply declared in the endpoint file.** The split is mandatory; `extract-endpoint-types` repairs
  a file written before it.
