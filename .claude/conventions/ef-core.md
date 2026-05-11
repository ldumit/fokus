# EF Core Conventions

Programming conventions for EF Core specific to this codebase. Referenced by developer and reviewer agents.

## Navigation Loading

- `ThenInclude` chains continue from the last included navigation. Pattern: `Include(s => s.Memberships).ThenInclude(m => m.Ticket).ThenInclude(t => t.Assignee)`.
- `Contains` on a fetched `List<string>` translates to SQL `IN (...)` correctly. For very large sets this would need batching, but Fokus dataset is bounded.

## Cross-Aggregate Queries

- For analytic queries that join across aggregates, add the query method to the repository that owns the primary data (e.g., `TicketRepository` for status transitions). Don't create a separate read-model query class until the pattern recurs.

## Migrations

- `dotnet ef migrations remove --force` works cleanly when the migration has not been applied to a database. No need to delete files manually.
- When running `dotnet ef migrations add` with FastEndpoints and no endpoints are registered yet, the host startup fails. Add `IDesignTimeDbContextFactory` in the Persistence project to bypass this.

## Additional Gotchas

- `First` vs `FirstOrDefault` at service boundaries: even in single-user SQLite apps, prefer `FirstOrDefault` with null handling over `First` which throws an unhandled 500.
- `required int` on composite key members is more defensively correct than plain `int` — int defaults to 0 which can silently produce invalid FK records.
- `HasData` + `ToJson()` are incompatible. EF Core cannot seed entities via `HasData` when the entity (or an owned type on it) uses `ToJson()` column mapping. Use runtime seeding (`SeedTestData`) instead.
- Collection expressions (`[]`) fail in expression tree lambdas. EF Core translates lambdas to SQL via expression trees. C# 12 collection expressions are not representable as expression trees — use `new List<T>()` or `Array.Empty<T>()` explicitly.
