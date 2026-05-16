# Epic Test Health — Lessons

## Developer Lessons

- **`BlocksLinksByTicket` data structure inversion:** The repository returns `bugKey → [teIds]` (which bugs are blocked by which TEs). To compute "bugs found for an epic," collect all TE IDs used by the epic's feature tickets, then filter `BlocksLinksByTicket` for entries whose TE IDs intersect that set. This inversion pattern is not documented elsewhere — plan was vague on direction.

- **`Promise.all` with void-returning stores:** When `initialize()` already calls `fetchEpicProgress()` inside `Promise.all`, adding `settingsStore.fetchSettings()` works cleanly since both mutate store state rather than return values. The destructuring `const [teams] =` still works — extra promises beyond index 0 are just discarded from the destructure but still awaited.

- **Dual progress bar layout:** Stacking two bars vertically (`flex flex-col gap-1` on a wrapper div) works cleanly for dual bars. The SP bar stays `h-2` and the coverage bar uses `h-1` — the height difference alone creates visual hierarchy without needing color legend text.

- **`EpicProgressResponse` is a positional record:** Adding required positional parameters to the end of a C# `record` breaks all existing `new EpicProgressResponse(...)` callsites immediately. The endpoint's early-return empty-response path broke on the first build attempt. Always check for callsites when extending positional records.

- **`vue-tsc --noEmit` produces no output on success (exit 0):** Bash completion with no output = clean typecheck. This is the expected pattern for a passing vue-tsc run.

## Skill Gaps

- **Missing skill: `metric-query-patterns`** — Steps 1–3 had `Skill: None` because no skill covers computed-on-read analytics extension patterns (bulk-loading TE data for a ticket set, attaching QA metrics to existing response records, Xray-disabled fast-path in endpoints). Reference files used: `GetFeatureTicketsWithCoverageAsync` in `TestExecutionRepository.cs`, `GetQaMetricsEndpoint.cs` lines 29-33, `EpicProgressService.cs` existing pattern. A skill covering: (a) bulk TE data loading shape, (b) service QA computation structure, (c) Xray disabled guard pattern would cover 3 steps here and likely recurring analytics extensions.
