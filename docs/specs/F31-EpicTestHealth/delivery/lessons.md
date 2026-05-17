# Epic Test Health — Lessons

## Developer Lessons

- **`BlocksLinksByTicket` data structure inversion:** The repository returns `bugKey → [teIds]` (which bugs are blocked by which TEs). To compute "bugs found for an epic," collect all TE IDs used by the epic's feature tickets, then filter `BlocksLinksByTicket` for entries whose TE IDs intersect that set. This inversion pattern is not documented elsewhere — plan was vague on direction.

- **`Promise.all` with void-returning stores:** When `initialize()` already calls `fetchEpicProgress()` inside `Promise.all`, adding `settingsStore.fetchSettings()` works cleanly since both mutate store state rather than return values. The destructuring `const [teams] =` still works — extra promises beyond index 0 are just discarded from the destructure but still awaited.

- **Dual progress bar layout:** Stacking two bars vertically (`flex flex-col gap-1` on a wrapper div) works cleanly for dual bars. The SP bar stays `h-2` and the coverage bar uses `h-1` — the height difference alone creates visual hierarchy without needing color legend text.

- **`EpicProgressResponse` is a positional record:** Adding required positional parameters to the end of a C# `record` breaks all existing `new EpicProgressResponse(...)` callsites immediately. The endpoint's early-return empty-response path broke on the first build attempt. Always check for callsites when extending positional records.

- **`vue-tsc --noEmit` produces no output on success (exit 0):** Bash completion with no output = clean typecheck. This is the expected pattern for a passing vue-tsc run.

- **Dead-code cleanup in mid-implementation refactors:** When the BR4 bugs-found approach changed from a per-ticket loop to the post-loop `epicTeIds` set approach, the earlier `blocksTeIds` variable and empty `foreach` were left behind. Before finishing any computation helper, scan for unused variables and empty loop bodies — a dead `foreach` with only comments is a clear signal the approach changed mid-way.

- **`storeToRefs` for cross-store reads is a convention requirement, not just a runtime one:** Direct property access on a Pinia store inside `computed()` is reactive at runtime (Vue tracks the ref automatically). The `pinia-patterns` skill still requires `storeToRefs` for cross-store reads. Follow the skill convention even when the runtime behavior would be correct either way — consistency matters for code review and future readers.

- **All-aborted test run final `else` branch:** The `BuildTicketEntry` status derivation had four conditions (Failed, Passed, InProgress, else). The `else` branch is only reached when failCount=0, passCount=0, todoCount=0, executingCount=0 — meaning all runs are Aborted. BR5 defines Passed as "all test runs are PASS", so the `else` must be `"NoTests"` (TEs exist but no effective results), not `"Passed"`. When writing multi-condition status derivations, explicitly enumerate what the final `else` represents rather than leaving it implicit.

## Architect Lessons

- **Plan was clean for this feature class:** Extending an existing endpoint + existing UI with conditionally-rendered QA data is a well-understood pattern in this codebase. The 6-step plan mapped cleanly to implementation with only minor deviations (all justified). This confirms that "extend existing analytics endpoint" features can be planned at this granularity without over-specification.

- **`EpicQaData` named record was a good developer decision:** The plan said "record/tuple" — the developer chose a named record which is cleaner. Plans should prefer "parameter object" language over "tuple" when the data has 3+ grouped values, to signal intent without over-constraining.

- **Blocks link inversion pattern under-specified in plan:** The plan said "bugs found via Blocks links" but didn't specify the data direction (TE→bug vs bug→TE). The developer figured it out but flagged it as vague. Future plans involving Blocks links should specify: "Blocks links stored as bugKey→teIds; to find bugs for a ticket set, collect TE IDs first, then filter Blocks entries by intersection."

- **Positional record extension risk:** Adding parameters to positional C# records breaks all callsites. The plan didn't warn about this. Future plans that extend positional records should note: "Extend record — update all existing construction callsites (check early-return paths in endpoints)."

- **BR17 vs BR22 spec ambiguity resolution:** When two BRs contradict each other for the same state, the more specific rule (with an exact boolean condition) governs over the general empty-state description. BR17 gives a precise visibility condition for the summary card; BR22/Flow 5 1b describes the general empty-state concept. The specific rule wins. Future specs should avoid restating visibility rules inside empty-state descriptions when a dedicated BR already covers that element.

## Reviewer Lessons

- **Dead-code loops as review signal:** An empty `foreach` body with only comments inside is a reliable indicator of a refactor mid-implementation where the approach changed but the earlier attempt wasn't cleaned up. Always look for empty loop bodies in computation helpers — they're often harmless remnants but should be flagged (MEDIUM) to prevent future reader confusion.

- **Frontend TS build failures require git archaeology:** The `vue-tsc` error in SettingsView.vue looked like a new break but was pre-existing since at least commit `8c75050`. Before attributing a build failure to the feature under review, check the file in the commit immediately before the implementation commit via `git show <pre-commit>:<file>`.

- **`storeToRefs` convention vs runtime correctness:** Direct property access on a Pinia setup store inside a `computed()` callback IS reactive at runtime (Vue tracks the ref). However, the `pinia-patterns` skill explicitly requires `storeToRefs` for cross-store reads. Flag as MEDIUM for skill non-compliance even when runtime behavior is correct.

- **BR vs spec flow conflicts are open questions, not findings:** When two spec sections contradict each other (BR17 vs BR22 on summary card visibility), the correct call is to escalate as an open question rather than flag as a code bug. The implementation followed the more specific, named BR (BR17) over the prose flow example — a defensible default, but the architect should confirm.

- **All-aborted run edge case is LOW, not MEDIUM:** A ticket with exclusively Aborted test runs is extremely rare in practice (Xray typically doesn't produce all-aborted results without intervention). The test status "Passed" fallback is incorrect per spec wording but has minimal user impact. Downgraded to LOW appropriately.

## Skill Gaps

- **Missing skill: `metric-query-patterns`** — Steps 1–3 had `Skill: None` because no skill covers computed-on-read analytics extension patterns (bulk-loading TE data for a ticket set, attaching QA metrics to existing response records, Xray-disabled fast-path in endpoints). Reference files used: `GetFeatureTicketsWithCoverageAsync` in `TestExecutionRepository.cs`, `GetQaMetricsEndpoint.cs` lines 29-33, `EpicProgressService.cs` existing pattern. A skill covering: (a) bulk TE data loading shape, (b) service QA computation structure, (c) Xray disabled guard pattern would cover 3 steps here and likely recurring analytics extensions.
