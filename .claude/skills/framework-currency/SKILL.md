---
name: framework-currency
description: Keeps the .NET stack on latest stable versions — general currency rule plus FastEndpoints Send.* migration (two-stage detection, false-positive list). Use when upgrading framework versions or migrating legacy SendOkAsync/Send*Async calls.
user-invocable: true
---

# Framework Currency

Two rules in one skill: the **general currency rule** (stack-agnostic) and the **FastEndpoints `Send.*` variant** (FastEndpoints specialization).

## Assumes

- **Central package management** (`central-package-management`) — every version in one props file,
  `src/Directory.Packages.props`.
- The shared libraries (`src/BuildingBlocks/Blocks.*`) and the modules are in the same solution as the service, so
  one bump moves them all together.

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

## Governing Conventions

- **Currency** — always track the latest stable versions of the core stack; target the current .NET release the
  project standardizes on (.NET 10 here).
- **Send.\*** — FastEndpoints variant: use the modern `Send.*` API; never the legacy `Send*Async` instance methods.

---

## Rule 1 — General Currency (Stack-Agnostic)

Track the latest stable versions of the core stack declared centrally (see `central-package-management`). Target framework is **.NET 10** (`<TargetFramework>net10.0</TargetFramework>`).

- All package versions live in `src/Directory.Packages.props` — one bump in one file updates all projects.
- **One version of each library everywhere** — the endpoint framework included.
- **No preview and no floating version.** A package held on an older major is a pin with its one-line reason
  (`central-package-management` § The CPM Convention).
- Verify each version against the package's release notes / NuGet before bumping; do not rely on memory.
- After bumping, build immediately — breaking changes may appear as compile errors or runtime failures.

**Version-bump caution:** Bumping to latest can surface transitive breaking changes beyond the targeted package. Example: FastEndpoints 8 requires FluentValidation 12, so moving FastEndpoints moves the validators' library too. See `central-package-management` for the three-form verification grep after a bump.

---

## Rule 2 — FastEndpoints `Send.*` (FastEndpoints Variant)

> This rule is the FastEndpoints specialization of the currency convention. It applies only to projects using FastEndpoints as the endpoint framework — the Fokus service does.

Use the **modern `Send.*` API**. Never use the legacy instance `Send*Async` methods.

### Migration Map

Verify all names against the **installed FastEndpoints version** before migrating — method names shifted between major versions (6→7, 7→8) and may shift again. **Never trust memory; verify against the installed package** (its `FastEndpoints.xml` documentation file in the NuGet cache lists every member).

Read the installed `FastEndpoints` version from `src/Directory.Packages.props`. The table below is the **8.x worked example** — confirm the names against the version the project actually pins:

| Legacy (WRONG) | Modern (CORRECT) |
|----------------|-----------------|
| `await SendOkAsync(response, ct)` | `await Send.OkAsync(response, ct)` |
| `await SendAsync(body, statusCode, ct)` | `await Send.ResponseAsync(body, statusCode, ct)` |
| `await SendNoContentAsync(ct)` | `await Send.NoContentAsync(ct)` |
| `await SendNotFoundAsync(ct)` | removed — the lookup throws not-found (below) |
| `await SendErrorsAsync(cancellation: ct)` | removed — a validator reports input errors, a handler throws (below) |
| `await SendCreatedAtAsync(...)` | `await Send.CreatedAtAsync(...)` |
| `await SendForbiddenAsync(ct)` | removed — access is declared on the endpoint (below) |
| `await SendUnauthorizedAsync(ct)` | removed — sign-in code throws `UnauthorizedException` (below) |

**Bare overloads to include in migration:**
- Three-arg `SendAsync(body, statusCode, ct)` → `Send.ResponseAsync(body, statusCode, ct)`
- Two-arg `SendAsync(body, ct)` (no status code) → `Send.OkAsync(body, ct)`

`Send.NotFoundAsync`, `Send.ErrorsAsync`, `Send.ForbiddenAsync` and `Send.UnauthorizedAsync` exist in the framework,
but in this repo an endpoint never writes an error reply by hand: a missing entity is thrown by the lookup, a broken
rule is thrown, and access is declared (`error-handling`, `authorization-patterns`). The one central error middleware
writes every error reply. Migrate a legacy call of any of them by removing it in favour of those, not by renaming it.

A call that is `return`ed instead of awaited (`return SendOkAsync(response, ct);`) migrates the same way
(`return Send.OkAsync(response, ct);`).

**Target state:** a fully migrated service uses `Send.OkAsync(...)` (and the other success `Send.*` forms) throughout — zero legacy `Send*Async` instance calls remain.

### Two-Stage Detection

A naive `Send*Async(` grep in a mostly-migrated repo hits both legacy FastEndpoints calls **and** unrelated `SendAsync` calls from other frameworks (SignalR, HttpClient). The grep cannot distinguish them — that is Stage 2's job. Use this approach:

**Stage 1 — Find candidate `Send*Async` calls:**

Search `*.cs` files for any `Send*Async(` pattern. The regex will also match receiver-qualified false positives — Stage 2 filters those out:

```
grep -rn --include="*.cs" "\bSend\w*Async(" src/
```

Or scoped to endpoint files first (reduces noise):
```
grep -rn --include="*Endpoint.cs" "\bSend\w*Async(" src/
```

To list only receiver-less calls (the ones to migrate):
```
grep -rnE --include="*.cs" "(^|[^.A-Za-z])Send[A-Za-z]*Async\(" src/
```

Candidate hits include:
- `await SendOkAsync(...)` — legacy FastEndpoints, migrate
- `await SendAsync(body, 200, ct)` — legacy 3-arg bare overload, migrate
- `await SendAsync(body, ct)` — legacy 2-arg bare overload, migrate to `Send.OkAsync`
- `await SendNoContentAsync()` — legacy FastEndpoints, migrate
- `await httpClient.SendAsync(request, ct)` — HttpClient false positive, **skip**
- `await hubContext.Clients.All.SendAsync(...)` — SignalR false positive, **skip**

**Stage 2 — Manual receiver inspection for each hit:**

For every hit from Stage 1, check whether the call has a receiver (an object before the dot). **Receiver-qualified calls are NOT FastEndpoints calls and must be left alone:**

| Hit | Receiver | Action |
|-----|----------|--------|
| `await SendOkAsync(response)` | none | MIGRATE — FastEndpoints legacy |
| `await SendAsync(body, ct)` | none | MIGRATE — 2-arg form → `Send.OkAsync` |
| `await httpClient.SendAsync(request, ct)` | `httpClient` | SKIP — `HttpClient` method |
| `await hubContext.Clients.All.SendAsync("event", data, ct)` | `hubContext` | SKIP — SignalR hub context |

### False Positives — Do NOT Migrate

**Inspect the receiver before migrating.** The two recurring false-positive classes, both present in this repo:

1. **`HttpClient.SendAsync`** — used in GraphQL/REST clients.
   - Example: `await httpClient.SendAsync(request, ct)` (`src/Modules/Xray/Xray.GraphQL/GraphQLXrayClient.cs`)
   - Receiver: `httpClient` (type `HttpClient`)

2. **SignalR `hubContext.Clients.*.SendAsync`** — used to push messages to browser clients.
   - Example: `await hubContext.Clients.All.SendAsync(SprintHubMethods.SprintSynced, ...)` (`src/Services/Fokus/Fokus.API/Features/Sync/SyncSprints/SyncSprintsEndpoint.cs`)
   - Receiver: `hubContext` (type `IHubContext<T>`)

The safe rule: **any dot-qualified receiver before `SendAsync` means it is not a FastEndpoints call**.

### Version Verification Reminder

Before applying any migration map:
1. Open `src/Directory.Packages.props` and note the `FastEndpoints` version.
2. Check the FastEndpoints changelog for that version to confirm the method names.
3. If the version differs from the 8.x example above, re-verify every method name.

---

## Relationship to Other Skills

| Skill | Relationship |
|-------|-------------|
| `central-package-management` | Where versions are declared (CPM props file); cross-reference for the three-form verification grep and transitive-break caution |
| `error-handling` | Why the error-reply calls are removed rather than renamed |

## What This Skill Does NOT Cover

- Central Package Management setup — see `central-package-management`
- Endpoint class structure — see `create-feature`
- Repository or EF Core version changes — follow the same general currency rule; no special guidance needed beyond the migration map above
