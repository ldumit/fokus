# Capture Service Axes

Walk through this checklist. For each axis, mark it as **filled** (from conversation context) or **unanswered**. At the end, ask the user about every unanswered axis in one batch.

## Checklist

### 1. Service name
- PascalCase, singular where possible (`Indexing`, not `Indexings`)
- Becomes the folder `src/Services/{Name}/` and the project prefix `{Name}.API`, `{Name}.Domain`, etc.

### 2. Purpose
- One or two sentences. Goes into the CLAUDE.md header as the service description.
- Example: "Indexing service — receives `ArticlePublished` integration events and maintains a full-text search index exposed via a query API."

### 3. Endpoint framework
Choose one:
- **FastEndpoints** — endpoint class IS the handler. No MediatR. Good for simple CRUD, low pipeline overhead. Examples: Auth, Journals, Production.
- **Carter + MediatR** — endpoint dispatches to MediatR handler in `.Application`. Pipeline behaviors available. Example: Review.
- **Minimal APIs + MediatR** — same as Carter but with static method endpoints and explicit `MapAllEndpoints()`. Example: Submission.
- **Carter (read-model only)** — no MediatR, no aggregate behavior, just queries and consumers. Example: ArticleHub.

### 4. Persistence
Choose one or combine:
- **EF Core + SQL Server** — default for transactional domain services.
- **EF Core + PostgreSQL** — for read-heavy projection / aggregate views.
- **Redis** — only when the domain is shallow and read-heavy. Max 1-2 services per system.
- **Combined** — a service may add a file-storage secondary store (e.g., MongoGridFS, Azure Blob). Capture as "primary + secondary".

### 5. Cross-feature shared state? (Y/N)
Does the service need a `.Application` project?

Create `.Application` when the service has any of:
- State machines (aggregate transition rules)
- Cross-feature DTOs
- Shared access checkers (e.g., `ArticleAccessChecker`)
- Seed helpers
- Shared mapping utilities

Skip `.Application` when the service has none of these — logic lives entirely in endpoints/handlers. (Auth and Journals are the examples.)

**Important:** this is NOT determined by the endpoint framework. Production uses FastEndpoints AND has a `.Application` project because it holds shared state. Do not force-collapse.

### 6. Port assignment (HTTP / HTTPS pair)
- Range: 4400-4499
- Pairs in use today: 4401/4451 (Auth), 4402/4452 (Journals), 4403/4453 (ArticleHub), 4404/4454 (Submission), 4405/4455 (Review), 4406/4456 (Production)
- Pick the next free pair for the new service (e.g., 4407/4457)

### 7. Uses docker? (Y/N)
- Y (default) → Developer's scaffold generates `Dockerfile`, adds a service block to `src/docker-compose.yml`, adds a `<ProjectReference>` to `src/docker-compose.dcproj`, and sets `<DockerComposeProjectPath>` in the service's csproj.
- N → no docker files generated.

### 8. gRPC clients consumed
- List the interfaces from `Articles.Grpc.Contracts` that this service will call.
- Examples: `IPersonService` (Auth), `IJournalService` (Journals).
- Empty list is fine.

### 9. gRPC servers exposed
- List any gRPC services this new service will expose. Usually empty for a new service.
- If non-empty, include method names.

### 10. Integration events published
- List the integration events this service will publish (from `Articles.Integration.Contracts` — existing events — or new events to add).
- Empty list is fine for a new service that only consumes.

### 11. Integration events consumed
- List the integration events this service will subscribe to.
- Each consumed event needs a MassTransit consumer in the service.

## Ask-in-one-batch rule

When you reach the end of this checklist, collect every unanswered item and ask the user in one consolidated message:

> I need to confirm a few axes for the `{Name}` service before I write its CLAUDE.md:
> 1. [unanswered axis 1]
> 2. [unanswered axis 2]
> ...

Do NOT ask one question at a time. Do NOT write the file until all axes are filled AND the user has confirmed the summary (next step: `WriteClaudeMd.md`).
