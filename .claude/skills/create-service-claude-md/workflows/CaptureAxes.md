# Capture Service Axes

Walk through this checklist. For each axis, mark it as **filled** (from conversation context) or **unanswered**. At the end, ask the user about every unanswered axis in one batch.

## Checklist

### 1. Service name
- PascalCase, singular where possible (`Indexing`, not `Indexings`)
- Becomes the folder `src/Services/{Name}/` and the project prefix `{Name}.API`, `{Name}.Domain`, etc.

### 2. Purpose
- One or two sentences. Goes into the CLAUDE.md header as the service description.

### 3. Endpoint framework
Choose one:
- **FastEndpoints** — endpoint class IS the handler. No MediatR. Good for simple CRUD, low pipeline overhead.
- **Carter + MediatR** — endpoint dispatches to MediatR handler in `.Application`. Pipeline behaviors available.
- **Minimal APIs + MediatR** — same as Carter but with static method endpoints and explicit `MapAllEndpoints()`.
- **Carter (read-model only)** — no MediatR, no aggregate behavior, just queries and consumers.

### 4. Persistence
Choose one or combine:
- **EF Core + SQL Server** — default for transactional domain services.
- **EF Core + PostgreSQL** — for read-heavy projection / aggregate views.
- **EF Core + SQLite** — lightweight single-node deploys.
- **Redis** — only when the domain is shallow and read-heavy. Max 1-2 services per system.
- **Combined** — a service may add a file-storage secondary store (e.g., MongoGridFS, Azure Blob). Capture as "primary + secondary".

### 5. Cross-feature shared state? (Y/N)
Does the service need a `.Application` project?

Create `.Application` when the service has any of:
- State machines (aggregate transition rules)
- Cross-feature DTOs
- Shared access checkers

Skip `.Application` when the service has none of these — logic lives entirely in endpoints/handlers.

**Important:** this is NOT determined by the endpoint framework. A FastEndpoints service CAN have a `.Application` project if it holds shared state. Do not force-collapse.

### 6. Port assignment
- Check root CLAUDE.md for the current service index table. Next available index = last assigned + 1.
- Ports derive from the repo's port convention, as its root CLAUDE.md writes it — for example a prefix, a
  protocol slot and the service index.

### 7. Uses docker? (Y/N)
- Y (default) → the scaffold writes the service's `Dockerfile` and adds a service block to the repo's compose file.
- N → no docker files generated; the CLAUDE.md carries a `**Docker:** N` line, which the scaffold reads.

### 8. gRPC clients consumed
- List the interfaces from `{ProjectName}.Grpc.Contracts` that this service will call.
- e.g., `I{Entity}Service` in `{ProjectName}.Grpc.Contracts/{ServiceName}/`
- Empty list is fine.

### 9. gRPC servers exposed
- List any gRPC services this new service will expose. Usually empty for a new service.
- If non-empty, include method names.

### 10. Integration events published
- List the integration events this service will publish (from `{ProjectName}.Integration.Contracts` — existing events — or new events to add).
- Empty list is fine for a new service that only consumes.

### 11. Integration events consumed
- List the integration events this service will subscribe to.
- Each consumed event needs a MassTransit consumer in the service.

### 12. Modules used
- List the modules this service runs on, if the repo has any. Only those: a service references modules
  selectively, each through its contracts project where it has one.
- Never another service's projects — another service is reached by an event or a gRPC client.
- Empty list is fine.

## Ask-in-one-batch rule

When you reach the end of this checklist, collect every unanswered item and ask the user in one consolidated message:

> I need to confirm a few axes for the `{Name}` service before I write its CLAUDE.md:
> 1. [unanswered axis 1]
> 2. [unanswered axis 2]
> ...

Do NOT ask one question at a time. Do NOT write the file until all axes are filled AND the user has confirmed the summary (next step: `WriteClaudeMd.md`).
