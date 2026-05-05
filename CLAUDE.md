# CLAUDE.md

## Agents

When the user says "be {Agent}", read `.claude/agents/{Agent}.md` and follow it.

## Authority & Spec Files

Spec files (architecture.md, business.md, etc.) describe current state, not rules. I decide changes — flag conflicts, propose options, don't refuse.

## graphify

This project has a graphify knowledge graph at graphify-out/.

Rules:
- Before answering architecture or codebase questions, read graphify-out/GRAPH_REPORT.md for god nodes and community structure
- If graphify-out/wiki/index.md exists, navigate it instead of reading raw files
- After modifying code files in this session, run `graphify update .` to keep the graph current (AST-only, no API cost)

## Domain

### Domain events

Domain events carry a reference to the aggregate object, not individual properties. Since the `DomainEventPublishingInterceptor` dispatches after `SaveChangesAsync`, the aggregate's `Id` and all state are fully populated at dispatch time.

```csharp
// Event definition — holds the aggregate reference:
public sealed record TeamCreatedEvent(Team Team) : IEvent;

// Raised inside the aggregate factory/method:
AddDomainEvent(new TeamCreatedEvent(this));

// Handler reads populated state:
var teamId = ev.Team.Id;       // real auto-incremented value
var teamName = ev.Team.Name;
```

### Aggregate creation

Use a static factory method when creation involves business rules (guards) or raises a domain event. Use `required init` properties with direct construction when creation is plain data assignment with no invariants.

### Repositories

Concrete classes (no interfaces — see Guardrails). Each repository wraps `SaveChangesAsync` so endpoints never touch `DbContext` directly:

```csharp
public Task<int> SaveChangesAsync(CancellationToken ct) => _db.SaveChangesAsync(ct);
```

## Architecture

DDD, Vertical Slice, CQRS, Clean Architecture, Event Driven Design

## Repo Structure

```
src/
  BuildingBlocks/              # Shared libraries
    (app-agnostic)             #   Reusable primitives (no project knowledge)
    (app-specific)             #   gRPC contracts, integration event contracts
  Services/                    # Microservices — one folder per service
    {Svc}/
      {Svc}.API/               #   FastEndpoints feature slices (endpoint + request + response + validator + event handlers), SignalR hubs, DI, migration startup host
      {Svc}.Domain/            #   Aggregates, value objects, domain events
      {Svc}.Persistence/       #   EF Core DbContext, configs, migrations, repositories
  Modules/                     # Modular monolith (Contracts + Implementation per module)
  ApiGateway/                  # YARP reverse proxy
docker-compose                 # Local dev environment
```

Default to **module**; only use a microservice when deployment/scaling/ownership demands it.
New services use the three-project split above (API / Domain / Persistence). There is no separate Application project — FastEndpoints endpoint classes own feature slices directly (request + response + validator + handler logic + event handlers all live in `{Svc}.API/Features/`).

### Project reference graph

```
API  ──▶  Domain
  │
  └─▶  Persistence  ──▶  Domain
```

- `API` references both `Domain` and `Persistence` directly. Because we do not use repository interfaces (see Guardrails), endpoints consume concrete repositories from `Persistence` — no abstraction layer to bridge.
- `Persistence` references `Domain`.
- `Domain` references nothing.

## Tech Stack

- .NET 10 / ASP.NET Core
- **FastEndpoints** — endpoint classes per feature. One class owns request + response + handler logic; validator is a sibling class in the same folder. Built-in FluentValidation integration runs pre-handler; pre/post processors handle cross-cutting (logging, auth enrichment).
- **FluentValidation** — request validators, auto-discovered and executed by FastEndpoints before the handler runs.
- **FastEndpoints `IEvent` bus** — in-process publish/subscribe for domain events. Aggregates raise events onto a `DomainEvents` list; a `SaveChangesInterceptor` in Persistence publishes them after a successful save. `IEventHandler<T>` implementations (SignalR broadcasters, background-job triggers) live alongside their feature slices in `{Svc}.API`.
- Mapster — object mapping
- EF Core — SQLite (file-backed, local dev and single-node deploys)
- MassTransit + RabbitMQ — integration events
- gRPC code-first — service-to-service sync communication
- SignalR — real-time server → browser push (hub-per-service, groups for multi-tenant isolation)
- Google OAuth via ASP.NET Core `AddGoogle` — authentication (cookie auth; email + profile scope only)
- OpenAPI via `Microsoft.AspNetCore.OpenApi` (built into .NET 10) + **Scalar** (`Scalar.AspNetCore`) for the interactive UI. Do not use Swashbuckle. Scalar UI must be gated to `IsDevelopment`.

## Frontend

- **Vue 3** + **TypeScript** — SPA framework.
- **Pinia** — state management.
- **Tailwind CSS** — styling. No full UI kit (no Vuetify, no PrimeVue). `shadcn-vue` acceptable if it stays lightweight.
- Built with **Vite** into the API's `wwwroot/` and served as static files — single-solution monorepo.
- Optimistic UI for user actions; reconcile or revert on API response.

## Port Convention

All services publish host ports using an `{AA}XY` scheme:

- `{AA}` — 2-digit app prefix (TBD per project).
- `X` — protocol slot (tens digit).
- `Y` — service index (units digit).

Host port is derived mechanically: `<app prefix> + <protocol slot> + <service index>`.

### Protocol slots

| Slot | Protocol   | Container port | Mandatory? |
|------|------------|----------------|------------|
| 0    | HTTP       | 8080           | Yes        |
| 1–3  | _reserved_ | —              | — (future: metrics, health, debug) |
| 4    | HTTPS      | 8081           | No — opt-in per service |
| 5    | gRPC (h2c) | 8082           | No — only if service exposes a gRPC surface |

### Service indices

| Index | Service |
|-------|---------|
| TBD   | TBD     |

Example — a service at index `1` with app prefix `44`:

- HTTP:  `4401:8080`
- HTTPS: `4441:8081` _(opt-in)_
- gRPC:  `4451:8082` _(opt-in)_

### Rules

- **HTTP is mandatory** on every service compose entry.
- **HTTPS is opt-in.** Do not activate inside dev containers without a concrete reason — dev cert mounting is friction with no payoff for local loopback work. Reserve the slot; don't wire it.
- **gRPC is opt-in** AND only when the service actually exposes a gRPC surface. Write-side-only services declare no gRPC port.
- **gRPC runs h2c** (cleartext HTTP/2) on container port `8082` inside the compose network. This requires explicit Kestrel endpoint configuration in `Program.cs` — it is not a default of the aspnet base image.
- **Slots 1–3 are reserved.** Do not use them for anything else without amending this convention.
- **Services do not duplicate port numbers in their own `CLAUDE.md`.** Each service records only its service index; the ports derive from this table.

## Guardrails — DO NOT

- **No service layer classes** (e.g. `ArticleService`). Use: domain methods, handlers, repositories, gRPC clients, infra helpers.
- **No repository interfaces** — single implementation, interfaces add zero value. Other components (modules, cross-cutting) do use interfaces where contracts are needed.
- **No god folders** (`Services/`, `Helpers/`, `Utils/`).
- **No bypassing domain rules** via EF configs or endpoints.
- **Domain events** = within service boundary. **Integration events** = cross-service.
- **No `.gitkeep` files** — don't track empty directories. Directories are created at runtime or when files are added.

## Codebase Graph

This project has a graphify knowledge graph at `graphify-out/`.

- Before answering architecture or codebase questions, read `graphify-out/GRAPH_REPORT.md` for god nodes and community structure
- If `graphify-out/wiki/index.md` exists, navigate it instead of reading raw files
- After modifying code files in this session, run `graphify update .` to keep the graph current (AST-only, no API cost)

## EF Core Migrations

```bash
# Add migration
dotnet ef migrations add Name -p Services/{Svc}/{Svc}.Persistence -s Services/{Svc}/{Svc}.API

# Apply migration
dotnet ef database update -p Services/{Svc}/{Svc}.Persistence -s Services/{Svc}/{Svc}.API
```
