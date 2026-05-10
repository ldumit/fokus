# Knowledge Base Maintenance

The business knowledge base at `docs/kb/` captures domain rules, computation logic, and data flows that agents need to navigate the codebase efficiently.

## When to Update

After implementing a feature that modifies:
- Domain aggregate properties or invariants -> update `docs/kb/domain/{aggregate}.md`
- Analytics computation formulas or business rules -> update `docs/kb/analytics/{feature}.md`
- Cross-cutting rules (excluded statuses, sub-team filter, delta pattern) -> update `docs/kb/cross-cutting.md`
- Sync flow or Jira integration -> update `docs/kb/jira-sync.md`
- New views, stores, or API endpoints -> update `docs/kb/frontend-map.md`

## Who Updates

- **Developer:** Updates KB entries as part of implementation. Include in implementation.md under "Files Modified."
- **Reviewer:** Verifies KB entries match the implementation. Flag stale or missing entries as HIGH severity.

## What to Capture

- Business rules (the "why" that code doesn't explain)
- Computation formulas (exact logic, not just "calculates X")
- Edge cases (division by zero, null handling, boundary conditions)
- Key file paths (so agents know where to look)
- Relationships between concepts

## What NOT to Capture

- Full property lists (agents read the class file for that)
- Response DTO shapes (agents read the endpoint for that)
- UI layout details (agents read the Vue component for that)
- Anything derivable from reading the code itself
