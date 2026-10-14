# Fokus Service

**Endpoint framework:** FastEndpoints
**Database:** SQLite (FokusDb)
**Port:** `http://localhost:5000` (development profile, `Fokus.API/Properties/launchSettings.json`); no compose service index — the service is not containerised
**Docker:** N

## Purpose

Fokus is a sprint analytics service that syncs data from Jira (sprints, tickets, developers, status transitions) and computes health metrics (completion rate, disruption rate, carry-over rate) for sprint retrospectives. It is a single-service system with no cross-service dependencies in v1.

## Domain model

- **Aggregates:** Sprint (int PK, owns SprintMemberships), Ticket (string PK, owns StatusTransitions)
- **Key entities:** Developer (Entity<string>), StatusTransition (Entity<int>), SprintMembership (composite key child, plain class)
- **Value objects:** HealthThresholdConfig, HealthWeightConfig (owned by AppSettings, stored as JSON columns)
- **Analytics calculators:** pure Domain classes in `Fokus.Domain/Analytics/{Sprints,Developers,Qa,Workflow}/`, each beside its result record, with the shared kernel (sub-team and excluded-developer filters, transition attribution, delta, metric cards, test coverage) in `Shared/`. No database, logger, options, clock or JSON: the clock arrives as a `today` parameter read from `TimeProvider` (`domain-service`).
- **Application layer:** `Fokus.Application`, registered by `AddFokusApplication` (which also registers the instance Domain calculators; the `Shared/` classes are static) — injected capability classes, no mediator: the loaders of the fat analytics endpoints (`Analytics/{Sprints,Developers,Qa}/{Operation}Inputs`, named for what they load) and the two classes around another system, `Sync/SprintIssueSyncService` and `Xray/XrayIssueSyncService`.

## Endpoint pattern

Class extending `Endpoint<TRequest, TResponse>` or `EndpointWithoutRequest<TResponse>`. `HandleAsync` coordinates: it reads the request, loads (repositories directly, or the endpoint's `Fokus.Application` loader where the loading spans many repositories), calls the Domain calculator, and maps the result to its own reply with the `To…()` extensions in `{Operation}Extensions.cs` (`create-feature`). The endpoint file holds the endpoint class only; its request, reply and validator (`Validator<T>`) live in `{Operation}Command.cs` or `{Operation}Query.cs` in the same feature folder (`extract-endpoint-types`). Endpoints are auto-discovered by `AddFastEndpoints()`.

## MediatR pipeline behaviors

N/A — FastEndpoints native, no MediatR.

## Domain event infrastructure

- **Publisher:** `Blocks.FastEndpoints/DomainEventPublisher` — dispatches via `IEvent.PublishAsync(Mode.WaitForAll)`
- **Interceptor:** `DispatchDomainEventsInterceptor` (standard, non-transactional) — registered as `ISaveChangesInterceptor`
- **v1 status:** Interceptor is registered and harmless. No domain events are raised in v1. Ready for future use.
- **Handlers:** `IEventHandler<T>` implementations live alongside their feature slices in `Fokus.API/Features/`

## Key patterns

- **Sync-from-external:** All data originates from Jira API. `JiraMapper` maps Jira DTOs → domain entities.
- **Natural keys:** Ticket.Id = Jira issue key (e.g. "FOK-123"), Developer.Id = Jira accountId. Sprint.Id = Jira sprint ID. These are never auto-generated (`ValueGeneratedNever`).
- **Computed metrics:** Health scores and every analytics metric are computed per request by the Domain calculators the endpoint calls, from data the endpoint or its loader reads; the endpoint maps the result to its reply — never stored in the DB.
- **Domain split:** each aggregate is a partial class — state in `{Aggregate}.cs`, behaviour in `Behaviors/{Aggregate}.cs`. Most behaviour is factory mapping from Jira/Xray DTOs (`FromJira`, `FromXray`); the Auth aggregates carry real behaviour (`AppUser.CreateBootstrapAdmin`/`CreateFromInvitation`/`RecordLogin`, `Invitation.Create`/`Accept`/`Revoke`).
- **AppSettings singleton:** Single row (Id=1). Added by the start-up line `SeedDefaultAppSettingsAsync()` (`Fokus.Persistence/SeedExtensions.cs`, called from `Program.cs` after `MigrateFokusDatabase()`) when the table is empty, in every environment. `AppSettingsRepository.GetAsync` only reads; a missing row is a start-up fault (500). Holds board config, done-status list, workflow stages, health thresholds, health weights.
- **Errors:** a required lookup throws (`OrThrowNotFound(message)`, or `throw new NotFoundException(message)` where a rule decides the 404); a handler throws `BadRequestException` for a rule it checks. No endpoint or pre-processor writes an error reply — `GlobalExceptionMiddleware` writes every one (`error-handling`).
- **Access roles:** `AccessRole` / `AccessRoles` (`Admin`, `Manager`) in `Fokus.Domain/Auth/`, never a literal. Admin-only endpoints carry `[Authorize(Policy = AccessPolicies.Admins)]` (`Fokus.API/Auth/AccessPolicies.cs`, registered in `AuthConfiguration.cs`); every other endpoint takes authentication only (`authorization-patterns`), except the two sign-in endpoints that allow anonymous calls: `Auth/Login` (`/auth/login`) and `Auth/ValidateInvitation` (`/api/invitations/{token}/validate`).
- **Replies:** FastEndpoints 8's `Send.*` API (`Send.OkAsync`, `Send.NoContentAsync`, `Send.ResponseAsync`) — never the removed `Send…Async` instance methods (`framework-currency`).
- **Shared logic:** logic shared across endpoints is a pure Domain calculator (in `Fokus.Domain/Analytics/{Area}`, or `Shared/` when several areas use it) or an injected `Fokus.Application` capability class, named for its job (calculator, resolver, checker, `…Inputs` loader). `…Service` names only a class around another system (`SprintIssueSyncService`, `XrayIssueSyncService`). Each stays single-purpose — if it grows beyond one job, split it.

## Existing features

Feature areas under `Fokus.API/Features/`, one folder per operation inside each:

- `Analytics` — sprint summary, carry-over, cycle time, bug ratio, scope change, leaderboard, developer detail/progress/quality/throughput, epic progress, QA metrics/trends/workload, failing and untested tickets
- `Auth` — sign-in, sign-out, current user, users (role and status), invitations (create, list, revoke, validate)
- `Developers` — developer list, sub-teams, capacity
- `Settings` — the settings row, workflow-stage detection, and each Settings tab's read and save
- `Sprints` — sprint list, sprint update, test timeline
- `Sync` — Jira boards, sprints and statuses; sprint and backlog sync
- `Team` — team roster, a developer's team config
- `Xray` — Xray sync, connection test

## Modules

- **Jira** — `Jira.Contracts` + `Jira.RestApi` (`src/Modules/Jira/`)
- **Xray** — `Xray.Contracts` + `Xray.GraphQL` (`src/Modules/Xray/`)

## gRPC clients

None

## gRPC server

None

## Integration events

**Published:** None
**Consumed:** None

## Jira integration

- `JiraClient` — HTTP client, reads from `appsettings.json` `Jira` section (`InstanceUrl`, `Email`, `ApiToken`, `DefaultProjectKey`). Throws `UnauthorizedException` for 401 and `BadGatewayException` for other Jira errors — handled by `GlobalExceptionMiddleware`, not by endpoints.
- `JiraOptions` — bound from `appsettings.json`, validated on start
