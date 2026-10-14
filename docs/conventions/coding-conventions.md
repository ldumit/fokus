# Coding Conventions

## Boy Scout Guardrails

Project-specific architectural boundaries that the boy-scout skill must respect:

- **Don't rename aggregate public methods** — they're domain contracts. Internal/private names are fair game.
- **Don't extract logic out of aggregates** — behavior belongs with state. If an aggregate method is complex, simplify it in place.
- **Don't move code across vertical slice boundaries** — a feature's endpoint, validator, and handler belong together. Don't extract shared services between features (that's a separate refactoring decision).
- **Don't create cross-feature helpers outside `Fokus.Domain/Analytics/Shared`** — the shared analytics kernel is the one home for logic several analytics features use (F39).
- **Don't touch domain events or integration event contracts** — those are public APIs between components.

@csharp.md
@vue.md
@ef-core.md
@testing.md
