# EF Core Gotchas

Known pitfalls that affect multiple agents. Violations are silent — builds pass, behavior breaks at runtime.

- **`HasData` + `ToJson()` are incompatible.** EF Core cannot seed entities via `HasData` when the entity (or an owned type on it) uses `ToJson()` column mapping. Use runtime seeding (`SeedTestData`) instead.
- **Collection expressions (`[]`) fail in expression tree lambdas.** EF Core translates lambdas to SQL via expression trees. C# 12 collection expressions are not representable as expression trees — use `new List<T>()` or `Array.Empty<T>()` explicitly.
