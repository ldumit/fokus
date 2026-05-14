# Stack Rules

Architectural guardrails, repo structure, and conventions. Referenced by all agents that plan, write, or review code.

## Guardrails — DO NOT

- **No entity-wrapper service classes** (e.g. `ArticleService`, `SprintService`) — classes that accumulate business logic around a single entity. Focused operation services scoped to a feature area (e.g. `SprintIssueSyncService`) are allowed.
- **No repository interfaces** — single implementation, interfaces add zero value. Other components (modules, cross-cutting) do use interfaces where contracts are needed.
- **No god folders** (`Services/`, `Helpers/`, `Utils/`).
- **No bypassing domain rules** via EF configs or endpoints.
- **Domain events** = within service boundary. **Integration events** = cross-service.
- **No `.gitkeep` files** — don't track empty directories.

## Repo Structure

```
src/
  BuildingBlocks/              # Shared libraries
    (app-agnostic)             #   Reusable primitives (no project knowledge)
    (app-specific)             #   gRPC contracts, integration event contracts
  Services/                    # Microservices — one folder per service
    {Svc}/
      {Svc}.API/               #   FastEndpoints feature slices, SignalR hubs, DI, migration startup host
      {Svc}.Domain/            #   Aggregates, value objects, domain events
      {Svc}.Persistence/       #   EF Core DbContext, configs, migrations, repositories
  Modules/                     # Modular monolith (Contracts + Implementation per module)
  ApiGateway/                  # YARP reverse proxy
docker-compose                 # Local dev environment
```

Default to **module**; only use a microservice when deployment/scaling/ownership demands it.

## Project Reference Graph

```
API  ──▶  Domain
  │
  └─▶  Persistence  ──▶  Domain
```

- `API` references both `Domain` and `Persistence` directly. Endpoints consume concrete repositories — no abstraction layer.
- `Persistence` references `Domain`.
- `Domain` references nothing.

## Conventions

- **Domain events** carry aggregate reference, not individual properties.
- **Aggregate creation:** static factory when business rules or domain events are involved; `required init` properties when plain data.
- **FastEndpoints:** one endpoint class per feature, validator as sibling class in same folder.
- **Repositories** wrap `SaveChangesAsync` — endpoints never touch DbContext directly.
- **EF migrations:** `dotnet ef migrations add Name -p Services/{Svc}/{Svc}.Persistence -s Services/{Svc}/{Svc}.API`

## Build Verification

- `dotnet build` — run after every implementation step. Fresh output, not assumed.
- Debug artifact grep: `Console.WriteLine` (debugging), `TODO`, `HACK`, `FIXME`, commented-out code.
