---
name: extract-endpoint-types
description: A repair recipe for endpoint files written before the endpoint split was mandatory — moves the request, its reply, its DTOs and its validator out of the endpoint file into the operation's command or query file, leaving the endpoint file with the endpoint class alone. Use when an existing endpoint file holds more than the endpoint class, or a review flags the endpoint file layout. New endpoints are written split from the start with create-feature.
---

# Extract Endpoint Types

Every endpoint is written split from the start: the endpoint class alone in `{Operation}Endpoint.cs`, the request,
its reply and its validator in `{Operation}Command.cs` or `{Operation}Query.cs` (`create-feature`). This recipe
repairs a file written before that rule, in either endpoint style.

## Assumes

- **An existing feature folder** — `Features/{Area}/{Operation}/` — whose endpoint file holds more than the endpoint
  class.
- **The service's one endpoint framework** — FastEndpoints, or a mediator with minimal or Carter endpoints — as its
  `CLAUDE.md` names it. The repair is the same in both: only the file the types move into differs in project. The
  Fokus service uses FastEndpoints (`src/Services/Fokus/CLAUDE.md`), so the types move into the same feature folder
  under `src/Services/Fokus/Fokus.API/Features/`.
- The recipe moves types and renames them; it builds nothing.

## When to Use

- An existing `{Operation}Endpoint.cs` declares a request, a reply, a DTO or a validator beside the endpoint class.
- A review reports the endpoint file layout for a file.
- A validator sits in a file of its own instead of beside its request.

Never as a step of a new feature: `create-feature` writes the split form directly.

## Steps

1. **Classify the operation** — a write takes `{Operation}Command.cs`, a read `{Operation}Query.cs`. Rename a
   request that does not follow the names (`{Operation}Command`, `{Operation}Query`, `{Operation}Response`) in the
   same pass, with every reference. A rename changes type names only: the JSON property names on the wire stay as
   they are.
2. **Create the command or query file** in the same folder and namespace.
3. **Move into it:** the request record, its reply record, and the request's validator, renamed
   `{Request}Validator` where it is named otherwise.
4. **A DTO the reply uses** gets its own file — in the operation's folder when only this reply uses it, in the
   area's `_Shared` folder when another reply does too. This repo has no `_Shared` folder yet: a reply another
   operation reuses stays where it is (`UserResponse` lives in `Auth/GetUsers/GetUsersQuery.cs` and `Auth/UpdateRole`
   uses it); moving one is not part of this recipe.
5. **Keep in the endpoint file** only the endpoint class and the `using` lines it needs; move the rest of the
   `using` lines with the types that need them.
6. **Build** — `dotnet build src/Fokus.slnx` — and check the endpoint files: this repo has no convention check, so
   count the public types in each endpoint file instead (1 per file):

   ```bash
   for f in $(find src/Services/Fokus/Fokus.API/Features -name '*Endpoint.cs'); do
     echo "$(grep -cE '^\s*public (sealed )?(partial )?(class|record)' "$f") $f"
   done | grep -v '^1 '
   ```

   `Auth/Login/LoginEndpoint.cs` reads 0 and is expected: it is a `public static class` that maps a minimal-API route.

## File Layout After

```text
Features/{Area}/{Operation}/
├── {Operation}Endpoint.cs     the endpoint class only
├── {Operation}Command.cs      the request, its reply and its validator — or {Operation}Query.cs
└── {Thing}Dto.cs              a DTO only this reply uses, where there is one
```

## Reference Examples

Already split in this repo, under `src/Services/Fokus/Fokus.API/Features/`:

- **Query:** `Analytics/GetSprintSummary/` — `GetSprintSummaryEndpoint.cs` (the endpoint class) and
  `GetSprintSummaryQuery.cs` (the request and its validator).
- **Command:** `Auth/CreateInvitation/` — `CreateInvitationEndpoint.cs` and `CreateInvitationCommand.cs` (the
  request, its reply and its validator).
- **A validator folded in from a file of its own:** `Sprints/UpdateSprint/` — `UpdateSprintEndpoint.cs` and
  `UpdateSprintCommand.cs`.

The older split folders name the request `{Operation}Request` and its validator `{Operation}RequestValidator`;
they need no rename unless they are touched for another reason.

## What This Skill Does NOT Do

- Create an endpoint — use `create-feature`.
- Introduce a mediator, or move the endpoint's logic out of it.
- Move logic several endpoints share — that is a base endpoint
  (`create-feature/workflows/EndpointFastEndpoints.md` § Optional: Custom Base Endpoint) or a feature service
  (`extract-feature-service`).
