---
name: extract-feature-service
description: Extracts shared or complex endpoint logic into a pure Domain calculator (computation over loaded entities) or an injected Application capability class (loading across repositories, or a class around another system). Named for its job, single-purpose, returning a result record. Use when two endpoints share logic or one endpoint's HandleAsync has outgrown itself.
---

# Extract Shared Endpoint Logic

Extracts shared or complex logic out of one or more endpoints. The endpoint stays the coordinator; what moves goes to one of two homes, decided by what the logic does:

| The logic… | Goes to | Named | Structure skill |
|------------|---------|-------|-----------------|
| computes over entities already loaded (rules, figures, checks) | a pure Domain class in `Fokus.Domain/{Area}` — for analytics `Fokus.Domain/Analytics/{Area}/`, or `Shared/` when several areas use it | for its job: `…Calculator`, `…Resolver`, `…Checker`, `…Factory` | `domain-service` |
| loads across several repositories, or selects the sprint window before the computation | an injected class in `Fokus.Application/{Area}/` | for what it loads: `{Operation}Inputs` | `service-registration` |
| wraps another system (Jira, Xray) | an injected class in `Fokus.Application/{Area}/` | `…Service` — the only use of that suffix (`SprintIssueSyncService`) | `service-registration` |

Logic that both loads and computes splits: the loading to Application, the computation to Domain.

## When to Use

- Two or more endpoints share handler logic
- A single endpoint's `HandleAsync` has grown too complex, or its loading spans many repositories
- A plan step specifies extraction of shared logic

## Steps

1. **Identify the logic** — the code block(s) that will move out of the endpoint(s). Note the exact line ranges, and split them into loading and computation.

2. **Choose the home and the name** from the table above. Bad: `SprintService` (an entity wrapper — violates the guardrails), or `…Service` on a class that does not wrap another system.

3. **Define result record(s)** — each public method returns a result record (`sealed record` for new types). A Domain result carries computation results only — no HTTP mode, reply wrapper or JSON attribute; the endpoint maps it to its own reply.

4. **Create the file:**
   ```
   Fokus.Domain/Analytics/{Area}/{Name}Calculator.cs    ← computation
   Fokus.Domain/Analytics/{Area}/{Name}Result.cs        ← its result records
   Fokus.Application/Analytics/{Area}/{Operation}Inputs.cs ← loading, returns a source record
   ```

5. **Dependencies:**
   - Domain class: none. No repository, `DbContext`, logger, options, clock or JSON; today's date is a `today` parameter.
   - Application class: primary-constructor injection of concrete repositories, clients and loggers.

6. **Move the logic** — each method one cohesive operation, at most ~60 lines (`docs/conventions/csharp.md` § Method Design).

7. **Update the endpoint(s)** — inject the class, call it in place of the extracted code, map the result to the endpoint's reply in `{Operation}Extensions.cs` (`create-feature`).

8. **Register** — `services.AddScoped<{Name}>()` in `Fokus.Application/DependencyInjection.cs` (`AddFokusApplication`). Static Domain helpers are not registered.

9. **Verify the build** — `dotnet build` must pass.

## Guardrails

- **Single-purpose** — if the class grows beyond one cohesive job, split it.
- **Not an entity wrapper** — a class that accumulates all logic about one entity is a guardrail violation.
- **No god folders** — the class lives in its area folder, never in `Services/`, `Helpers/` or `Utils/`.
- **No repository interfaces** — inject concrete repositories directly.
- **No mediator** — the endpoint calls the class itself.

## Reference Examples

- `Fokus.Domain/Analytics/Sprints/` and `Fokus.Domain/Analytics/Shared/` — calculators and the shared kernel
- `Fokus.Application/Analytics/Qa/QaWorkloadInputs.cs` — a loader
- `Fokus.Application/Sync/SprintIssueSyncService.cs` — a class around another system

## What This Skill Does NOT Do

- Create entity-wrapper classes — those violate guardrails.
- Introduce abstractions — no interface for the class unless it's a module contract.
- Move the coordination out of the endpoint — reading the request, calling the classes and mapping the reply stay there.
