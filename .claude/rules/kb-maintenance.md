# Knowledge Base Maintenance

The business knowledge base at `docs/kb/` captures domain rules, computation logic, and data flows that agents need to navigate the codebase efficiently.

## When to Update

`docs/kb/index.md` is the topic map — it lists which KB entries exist and what each one covers. After implementing a feature that modifies any concept tracked there, update the corresponding KB entry. If the feature introduces a new tracked concept, add a new entry and register it in the topic map.

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
- UI layout details (agents read the UI component source for that)
- Anything derivable from reading the code itself

## On-Demand Update

When the user asks to update the KB for a concept (e.g., "update KB for Xray"):

1. Read the relevant source files (entities, services, repositories for that concept)
2. Extract: business rules, computation formulas, edge cases, key file paths, relationships
3. Write or update `docs/kb/{area}/{concept}.md` following the structure above
4. Update `docs/kb/index.md` if it's a new entry (add a link under the appropriate section)
5. Apply the "What NOT to Capture" rules — keep it to ~60 lines of what's non-obvious

Any agent can perform this (solo, developer, or the main session directly). No pipeline or plan required.
