# C# Conventions

Programming conventions for C# / .NET specific to this codebase. Referenced by developer and reviewer agents.

## Namespace Imports

- C# does not auto-import parent namespaces. When a class in `Fokus.API.Features.Sync.SyncSprints` consumes a class in `Fokus.API.Features.Sync`, an explicit `using` is required.
- `GlobalUsings.cs` should exist in all three service projects (API, Domain, Persistence), not just API. Each project has its own frequently-used namespaces.

## Error Handling in Handlers

- Inside `HandleAsync`, throw domain exceptions (`NotFoundException`, `BadRequestException`, etc.). The `GlobalExceptionMiddleware` maps them to HTTP responses.
- `AddError`/`SendErrorsAsync` is only for FluentValidation pre-handler validation, not for handler business logic errors.

## Method Overrides

- Use `new` keyword when a derived repository overrides a base method signature (e.g., `UpsertAsync` with entity-specific field-copy logic). This is intentional hiding, not an accident — suppress CS0108.

## Type Conventions

- Use `sealed record` for DTOs, value objects, and domain events.
- Use `required init` properties for entity creation when no business rules or domain events are involved.

## Solution Format

- The solution file uses `.slnx` format (XML-based, .NET 10 default). Adding projects means adding `<Project Path="...">` entries under the appropriate `<Folder>` element.
