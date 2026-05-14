# Knowledge Base Maintenance

The business knowledge base at `docs/kb/` captures domain rules, computation logic, and data flows that agents need to navigate the codebase efficiently.

## When to Update

`docs/kb/kb-topics.md` is the topic map — it lists which KB entries exist and what each one covers. After implementing a feature that modifies any concept tracked there, update the corresponding KB entry. If the feature introduces a new tracked concept, add a new entry and register it in the topic map.

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
