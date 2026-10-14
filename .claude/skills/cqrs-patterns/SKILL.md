---
name: cqrs-patterns
description: Commands, queries and handlers in a mediator-style service — the command and query markers, the load-call-save handler, where the pipeline and the validators fit, and how the FastEndpoints style differs. This repo's form (FastEndpoints with no mediator; the endpoint coordinates Application capability classes and Domain calculators) is stated first. Use when implementing a feature that dispatches through a mediator, or deciding where a feature's logic goes.
user-invocable: true
---

# CQRS Patterns

A write is a command, a read is a query; each has one handler. This skill is the mediator style's — a FastEndpoints
service has no handler class: its endpoint's `HandleAsync` carries the logic (`create-feature`).

## Assumes

- **A mediator** (MediatR in the samples) and **FluentValidation**.
- **The repository base with throwing lookups** (`persistence-patterns`) and **the save interceptor** that publishes
  domain events after a save.
- No shared library: the markers, behaviours and helpers are sampled in the skills that own them.

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

Fokus uses **FastEndpoints with no mediator**: there are no command or query markers, no handler classes and no
pipeline behaviours, and none are added. The endpoint is the coordinator:

- **The endpoint** (`src/Services/Fokus/Fokus.API/Features/{Area}/{Operation}/{Operation}Endpoint.cs`) reads the
  request, calls what it needs, and maps the result to its own reply.
- **Application capability classes** (`src/Services/Fokus/Fokus.Application/`) are injected classes, one folder per
  capability: a loader that gathers a fat endpoint's inputs from several repositories, and the sync classes around
  another system (`SprintIssueSyncService`, `XrayIssueSyncService`). They load, coordinate and save; they compute
  nothing a calculator should.
- **Domain calculators** (`src/Services/Fokus/Fokus.Domain/Analytics/`) compute from the loaded data and return
  results (`domain-service`).
- **`…Service`** names only a class around another system — the two sync classes. Logic several features share is
  a Domain calculator or an Application capability class, never an operation service.
- **Validators** are FastEndpoints `Validator<T>` classes in the operation's `{Operation}Command.cs` or
  `{Operation}Query.cs`, run by the framework before `HandleAsync`. The message helpers are
  `src/BuildingBlocks/Blocks.Core/FluentValidation/Extensions.cs` (`NotEmptyWithMessage`, `MaximumLengthWithMessage`)
  and the lengths `src/BuildingBlocks/Blocks.Core/MaxLength.cs`; the existing validators' per-rule literal messages
  predate `error-handling` § Validation Messages, and moving them is open work.
- **Domain events** go out through the save interceptor (`Blocks.EntityFrameworkCore/Interceptors/DispatchDomainEventsInterceptor.cs`)
  and the FastEndpoints event bus (`Blocks.FastEndpoints/DomainEventPublisher.cs`).

The rest of this skill describes the mediator style for a service that chooses it; Fokus does not.

## Command/Query Interfaces

```csharp file=ICommand.cs
public interface ICommand<out TResponse> : IRequest<TResponse>;
```

```csharp file=IQuery.cs
public interface IQuery<out TResponse> : IRequest<TResponse>
    where TResponse : notnull;
```

Each marker lives in its own file in the shared place. They state intent — a command changes state, a query reads
— and nothing branches on them: no behaviour checks whether a request is a command or a query. A behaviour that
applies to some requests constrains to the interface those requests really carry (`add-pipeline-behavior`).

## Handler Pattern (Load-Call-Save)

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

Load through a throwing lookup, call the aggregate's behaviour method with the command as its action object (who
acts and when), save, reply. The token is passed to every call that takes one. Domain events go out when the data is
saved. A handler does not log, does not catch to build a reply, and does not check access. In this repo the
throwing lookup is `FindByIdOrThrowAsync` (`Blocks.EntityFrameworkCore/Extensions/RepositoryExtensions.cs`).

## Pipeline Behaviors

The pipeline runs the identity stamp, then validation, then logging — `add-pipeline-behavior` carries the samples
and the order; `service-registration` § Registration Patterns registers them.

## Validator — Framework Variants

| Endpoint style | Validator base | Run by |
|---|---|---|
| FastEndpoints | `Validator<T>` | the framework, before `HandleAsync` |
| Mediator (Carter or minimal APIs) | `AbstractValidator<T>` | the validation behaviour, before the handler |

A base validator several commands share is named for its family (`OrderCommandValidator<T>`) and holds the rules
they share — a route id greater than zero. It adds nothing else: no logging and no null guard. Messages come from
the shared message helpers (`error-handling` § Validation Messages).

## Registration

The mediator finds its handlers and the validation library its validators; the behaviours are added in order
(`service-registration` § Registration Patterns).
