---
name: error-handling
description: One family of errors, the guard helpers and throwing lookups, the validation-message helpers, and the one central error mapper that turns every error into its reply (400, 401, 403, 404, 409, 502, 500, and 499 on a cancelled request). Use when throwing or translating an error, adding an error type and its status, writing a guard or a not-found lookup, or choosing a validation message.
user-invocable: true
---

# Error Handling

Errors are thrown, never returned, and translated into a reply **once**, by one central mapper. Endpoints, handlers,
pre-processors and services neither catch to build a reply nor check for null by hand: a required lookup throws
not-found, a broken rule throws its error, and the mapper chooses the status.

## Assumes

- **ASP.NET Core** for the mapper (a middleware) and **FluentValidation** for the validators.
- Everything this skill uses already exists in this repo — nothing is built:
  - the error family: `src/BuildingBlocks/Blocks.Exceptions/` plus `DomainException` in
    `src/BuildingBlocks/Blocks.Domain/Exceptions/DomainException.cs`;
  - the guards: `src/BuildingBlocks/Blocks.Core/Guard.cs` and `GuardExtensions.cs`;
  - the throwing repository lookup: `src/BuildingBlocks/Blocks.EntityFrameworkCore/Extensions/RepositoryExtensions.cs`;
  - the mapper: `src/BuildingBlocks/Blocks.AspNetCore/Middlewares/GlobalExceptionMiddleware.cs`.

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

## Prohibitions (load-bearing)

- **No inline null or not-found check.** Never `if (sprint is null) return NotFound();` and never
  `Send.NotFoundAsync` — a required lookup throws through the guard helpers, and the mapper replies 404.
- **No hand-written error reply.** No endpoint, handler or pre-processor writes an error status or body:
  no `AddError` with `Send.ErrorsAsync` in a handler, no `Response.WriteAsync` of an error. Input errors are the
  validators'; everything else throws.
- **No `try/catch` that turns an error into a reply** — in an endpoint, a handler or a service. The mapper is the
  only place an error becomes a reply.
- **A `catch` stands only at an I/O boundary, and only to clean up and rethrow:** an outside call, a stream or file,
  a transaction to roll back. It never swallows and never branches on a domain error. The one other `catch` is a
  background job's loop, which has no mapper above it: it logs the failed pass and lets the next one run
  (`add-hosted-service`).
- **No `Result<T>` or `Either` type for errors.** Errors flow as exceptions.

**This repo's two other `catch` forms** — kept on purpose, because removing either changes what the caller gets:

- **The batch sync paths catch per item and report the failure in the reply.** `SprintIssueSyncService` catches per
  sprint and adds a failure to the batch result; `XrayIssueSyncService` catches per Xray call and per test execution
  and adds a warning to its result (both under `src/Services/Fokus/Fokus.API/Features/`). One failed sprint or ticket
  becomes a reported failure in a partial result — that is the sync contract.
- **The SignalR broadcast after a successful sync is best-effort.** `Sync/SyncSprints/SyncSprintsEndpoint.cs` wraps
  the `SprintSynced` broadcast in a `catch` that logs a warning, and the sync reply stands: a failed push must not
  fail a sync that succeeded.

A new `catch` is one of the forms above, or it is not written.

## Exception Hierarchy

**This repo's family** — not a `DomainException`-rooted tree. Each type carries a message and nothing else. The
`Blocks.Exceptions` types derive `HttpException(int statusCode, string message)`; `DomainException` derives
`Exception`:

| Error | Project | Thrown when | Reply |
|---|---|---|---|
| `BadRequestException` | `Blocks.Exceptions` | a request breaks a rule an endpoint checks (the input is valid but the action is not) | 400 |
| `NotFoundException` | `Blocks.Exceptions` | a required lookup finds nothing (through the guard helpers), or a rule decides the resource is absent | 404 |
| `UnauthorizedException` | `Blocks.Exceptions` | sign-in or the active-user check rejects the caller | 401 |
| `ForbiddenException` | `Blocks.Exceptions` | the caller is known but may not act — rare: access is normally declared on the endpoint (`authorization-patterns`) | 403 |
| `ConflictException` | `Blocks.Exceptions` | a create would duplicate what exists, or two writers collide | 409 |
| `BadGatewayException` | `Blocks.Exceptions` | an outside system (Jira, Xray) failed the call | 502 |
| `DomainException` | `Blocks.Domain` | a business rule is broken inside an aggregate's behaviour — its message written inline where the rule is | 400 |

```csharp
public abstract class HttpException(int statusCode, string message) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
}

public class NotFoundException(string message) : HttpException(404, message);
```

An optional lookup returns nothing and its caller decides; only a required one throws. Three kinds of lookup, three
forms:

- **A required lookup by key that returns null when missing** → `.OrThrowNotFound(message)` or
  `FindByIdOrThrowAsync`. Keep the endpoint's not-found message where it has one.
- **A 404 decided by a rule, not by a null** (a developer that exists but is inactive) → a direct
  `throw new NotFoundException(message)` at the rule. Not `Guard.ThrowIfFalse`, which answers 400.
- **An optional lookup whose miss is a normal answer** (an invitation token that is not valid, no active sprint) →
  an `if` that sends the 200 reply describing the miss.

Framework error types: `ArgumentException` reaches the mapper as a 400. `InvalidOperationException` is a start-up or
configuration fault and reaches it as a 500 — in this repo, `AppSettingsRepository.GetAsync` throws it when the
settings row is missing, because start-up seeds that row and a missing one is a fault, not a missing resource.
Never `NotSupportedException`.

**Adding an error type and its status** is two edits in one change: the type, and its arm in the mapper's switch
below. The mapper matches each type by name — there is no `HttpException` arm — so a type with no arm falls through
to 500.

## Guard Utilities

The guards throw the family's errors and take a **message**, never an id:

```csharp
public static class Guard
{
    public static T NotFound<T>(T? value, string message = "The requested resource was not found.") where T : class;  // NotFoundException
    public static void ThrowIfNullOrWhiteSpace(string? value, string message = "Value cannot be null or whitespace."); // BadRequestException
    public static void ThrowIfFalse(bool condition, string message);                                                   // BadRequestException
}

public static class GuardExtensions
{
    public static T OrThrowNotFound<T>(this T? value, string message = "The requested resource was not found.") where T : class;
}
```

A value object's factory guards its value; an endpoint loads through a throwing lookup; nothing else checks
arguments. The throwing lookups (`…OrThrow…`) build on them — one per lookup a feature needs, never an inline check:

- `FindByIdOrThrowAsync(id, ct)` — an extension on the repository base `RepositoryBase<TContext, TEntity, TKey>`
  (`persistence-patterns`); its message is `"{Entity} with id '{id}' was not found."`;
- `GetBy{Key}Async(key, ct)` returning null, followed by `.OrThrowNotFound(message)` where that lookup is required.

```csharp
var sprint = await sprintRepository.FindByIdOrThrowAsync(req.SprintId, ct);
var user = (await appUserRepository.GetByIdAsync(req.Id, ct)).OrThrowNotFound("User not found.");
```

## Validation Messages

Validation messages come from shared message helpers or constants, never a literal per rule. The library's default
message is fine where it says enough. A small rule that no check can hold, so it is kept in review:

```csharp file=ValidationMessages.cs
public static class ValidationMessages
{
    public static readonly string Required = "'{PropertyName}' is required.";
    public static readonly string TooLong = "'{PropertyName}' must be at most {MaxLength} characters.";
    public static readonly string InvalidId = "'{PropertyName}' must be a valid id.";
}
```

```csharp file=ValidationExtensions.cs
public static class ValidationExtensions
{
    public static IRuleBuilderOptions<T, string> RequiredWithMaxLength<T>(this IRuleBuilder<T, string> rule, int maxLength) =>
        rule.NotEmpty().WithMessage(ValidationMessages.Required)
            .MaximumLength(maxLength).WithMessage(ValidationMessages.TooLong);

    public static IRuleBuilderOptions<T, int> ValidId<T>(this IRuleBuilder<T, int> rule) =>
        rule.GreaterThan(0).WithMessage(ValidationMessages.InvalidId);
}
```

FastEndpoints' `Validator<T>` derives FluentValidation's `AbstractValidator<T>`, so the same helpers serve it.

**This repo:** the validators predate this rule and still write a literal per rule (`.WithMessage("…")`). Two
helpers exist in `src/BuildingBlocks/Blocks.Core/FluentValidation/Extensions.cs` (`NotEmptyWithMessage`,
`MaximumLengthWithMessage`) and length constants in `src/BuildingBlocks/Blocks.Core/MaxLength.cs`; no message
class like the sample above exists yet. Moving the literals to shared helpers is open work. A new validator uses
the existing helpers or the library's default message, and does not add new literals.

## Global Exception Middleware

One central mapper turns every error into its reply: `GlobalExceptionMiddleware`
(`src/BuildingBlocks/Blocks.AspNetCore/Middlewares/GlobalExceptionMiddleware.cs`). It is registered first in the
pipeline — `app.UseMiddleware<GlobalExceptionMiddleware>()` in `src/Services/Fokus/Fokus.API/Program.cs`, before
static files, authentication and FastEndpoints (`service-registration` § Program.cs Structure (self-hosted service)).
It is the only place a status is chosen for an error — a new status is added here once, never written in an
endpoint.

**What it does, read from the live file:**

| Caught | Reply |
|---|---|
| `OperationCanceledException`, or any exception caused by one | 499, no body |
| FluentValidation `ValidationException` | 400, message `"One or more validation errors occurred."`, plus `errors` |
| `BadRequestException` | 400 |
| `ForbiddenException` | 403 |
| `NotFoundException` | 404 |
| `ConflictException` | 409 |
| `UnauthorizedException` | 401 |
| `BadGatewayException` | 502 |
| `DomainException` | 400 |
| `ArgumentException` | 400 |
| anything else | 500, message `"An unexpected error occurred."` |

- **Every mapped error is logged** (`LogError`), not only the 500s; the cancellation and validation arms do not log.
- **The body is camel-case JSON:** `{ "statusCode", "message", "traceId", "details" }`; the validation arm writes
  `{ "statusCode", "message", "traceId", "errors": [{ "propertyName", "errorMessage" }], "details" }`.
- **`details`** is the exception's full text in Development and `null` everywhere else. The message of a mapped error
  is its own message; a 500's is always the fixed text.

**FastEndpoints' own validation reply.** A FastEndpoints validator runs before the endpoint's handler, and a failure
is answered by FastEndpoints itself with its own 400 body — not by this middleware. The middleware's validation arm
only answers a `ValidationException` thrown from code.

## What this skill does NOT do

- **Validation itself** — the validators are `create-feature`'s (`../create-feature/workflows/Validator.md`).
- **The repository base** — `persistence-patterns` carries it.
- **Access declared on an endpoint** — `authorization-patterns`.
