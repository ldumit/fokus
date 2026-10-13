# F37 plan review — Codex (primary)

Round 1 · 2026-10-13 · job task-mv6lpo29-ye4d9j · persisted by the architect (main session) from the job's collected output; read-only run.

## Verdict: NO-GO

Findings refer to [plan.md](D:/src/fokus/docs/specs/F37-CodeQualityCleanup/delivery/plan.md).

1. **CRITICAL — plan.md:109, Step 7: removing tracking breaks existing calculations.** `GetCarryOverEndpoint.cs:46,66`, `GetBugRatioEndpoint.cs:51,70`, and `GetLeaderboardEndpoint.cs:58,77` pass the lightweight sprint from `GetAnalyticsSprintsAsync` into their services. Today, the subsequent membership query populates that same tracked instance. With `AsNoTracking`, the lightweight instance keeps empty memberships, producing empty/zero single-sprint results. The existing service tests bypass this loading sequence. **Suggested fix:** explicitly select the membership-loaded sprint before computing, audit other dependencies on relationship fix-up, and verify the actual repository loading sequence. [EF tracking documentation](https://learn.microsoft.com/en-us/ef/core/querying/tracking) confirms the underlying behavior.

2. **HIGH — plan.md:121, Step 8: the Settings load error can disappear before the proposed guard uses it.** `SettingsView.vue:80–85` continues into `syncFromStore` and auto-detection after `fetchSettings` catches its failure. `settingsStore.ts:173` then clears `store.error`. A successful detection therefore exposes defaults as savable settings despite adding the proposed error branch. **Suggested fix:** stop initialization after a failed settings fetch and gate editing on successful loading, independently of the shared save/detection error ref.

3. **HIGH — plan.md:77, Step 3: the projection discards required sprint counts.** Returning only zombie ticket IDs cannot populate the actual count in `ZombieTicket.SprintCount` (`SprintSummaryService.cs:74,837`) or preserve count-based ordering. `SprintFlags.vue:43` displays that count. The proposed tests check membership in a set, so an invented count of 3 could pass. **Suggested fix:** project ticket ID plus distinct sprint count, pass that mapping into the service, and assert counts above 3 and their ordering.

4. **HIGH — plan.md:75, Step 3: “all closed sprints plus selected” differs from the cited definition.** F11 BR7 and both KB entries say **all synced sprints**. Sync includes active sprints (`SyncSprintsEndpoint.cs:25`). When viewing a closed sprint, a ticket appearing in two closed sprints and a synced active sprint remains unflagged under this plan. `CarryOverService.GetZombieTicketKeys:534–542` itself has no closed-state filter; its endpoint supplies that restriction. **Suggested fix:** count all synced analytics sprint memberships, or explicitly resolve the narrower definition against the approved campaign before changing the KB.

5. **HIGH — plan.md:95, Step 5: the migration leaves instances of the approved performance defect.** `DeveloperProgressService.BuildStalledTickets:327–330` still scans the whole list per membership; A5-01 explicitly identifies this scan. The same pattern exists at `DeveloperDetailService.cs:347–350`, `ScopeChangeService.cs:552,565,581,770`, and the explicitly acknowledged `SprintSummaryService.cs:702–706`. The checker-only acceptance grep misses these. **Suggested fix:** enumerate direct scans alongside checker calls, replace the applicable scans with indexed lookups while preserving their predicates, and verify the complete migrated area.

6. **HIGH — plan.md:96, Step 5: replacing existing dictionaries with an ordinal index can change other results.** The two checker methods currently compare IDs ordinally, as claimed. However, `EpicProgressService` also uses its case-insensitive dictionary directly for activity dates (`:187–198`), without the checker’s additional ID comparison. Its permitted replacement with the ordinal index would change dates for case-differing IDs. Likewise, `ScopeChangeService.cs:320–328` passes case-insensitively grouped transitions to `IsRemovedPostPlanning`, which does not recheck IDs. **Suggested fix:** preserve those consumers’ existing comparison semantics and add service-level characterization cases before replacing their dictionaries.

7. **MEDIUM — plan.md:95, Step 5: index lifetime is underspecified.** “Once per public computation” permits rebuilding inside each `ExcludedDeveloperFilter.GetExcludedDeveloperIds` invocation. `GetDeveloperThroughputEndpoint.cs:88–91` calls that helper inside developer × target-sprint loops, retaining repeated whole-history work. **Suggested fix:** require the endpoint to construct and share the index across exclusion checks and the main computation; enumerate the helper’s transitive callers.

8. **MEDIUM — plan.md:68, Step 2: an exit can precede its inflow.** The rule chooses the first *in-sprint* completion, without relating it to membership entry. A bug completed on day 2 and added on day 4 would be subtracted before being added, recreating a negative area. **Suggested fix:** define completion eligibility relative to membership entry and the handling of already-completed additions; add a completion-before-addition case asserting both SP and ticket counts.

9. **MEDIUM — plan.md:66,70, Step 2: the affected region and invariant coverage are incomplete.** The bug-completion lookup is built at `ScopeChangeService.cs:537–588`, outside the cited `593–713` region. Its shared `RemovedAt` guard at `:542` suppresses completion timestamps for removed bugs. Changing that guard also touches feature eligibility, which the no-bug fixture does not adequately protect for the mixed scenarios being changed. **Suggested fix:** explicitly cover the lookup construction, preserve feature filtering, and assert every feature SP/ticket and committed-total series in a mixed fixture containing removed features and bugs.

10. **MEDIUM — plan.md:88, Step 4: the proposed tests can bypass the actual bug.** `ComputeRollingAverage` already walks the full supplied history correctly; the endpoint truncates that history. Service tests supplied with all sprints can therefore pass before the endpoint fix. **Suggested fix:** exercise the proposed window-selection helper and feed its selected history into `ComputeThroughput`, proving that the current two-extra-sprint selection fails the regression case.

11. **MEDIUM — plan.md:119, Step 8: Dashboard rendering conditions need explicit treatment.** The current QA component requires `qaMetrics !== null` (`DashboardView.vue:155`), precisely the condition that remains false when its initial fetch fails. The view also never renders the summary’s existing `store.error`, so summary failures still resemble “No sprint data yet.” **Suggested fix:** render QA errors independently of successful metric data, surface summary errors before the empty-state branch, and reset errors appropriately on selection/retry.

12. **MEDIUM — plan.md:126, Step 9: the environment plumbing spans three files.** `AddFokusAuth` is called by `DependencyInjection.AddFokusServices` (`DependencyInjection.cs:18–20`), which also accepts only configuration. Its caller is `Program.cs:12`. Updating only the immediate caller does not supply the environment. **Suggested fix:** name the complete `Program → AddFokusServices → AddFokusAuth` signature chain and pass `builder.Environment` through it.

13. **MEDIUM — plan.md:146, Step 12: operator acceptance omits the failure paths being fixed.** The check covers page loading but no failed settings initialization, failed QA fetch, failed Developers initialization/capacity save, or rejected settings-tab loads. It also omits the Development HTTP sign-in check cited by Must-NOT-Change. **Suggested fix:** add those failure-path checks, the failed Re-detect/stale-result case, and cookie-policy checks for Development and production.

14. **LOW — plan.md:5,93,128: caller baseline, sizing, and one acceptance command need correction.** The exact Step 5 grep returns **59 matching lines across 17 files**; there are **45 qualified checker call sites across 12 caller files**. Definitions, comments, and explicitly excluded private helpers inflate the broad grep. The slice explanation consequently rests on an inaccurate inventory, and Step 9’s literal `…` path exits **2** with “No such file,” making its acceptance command non-executable. **Suggested fix:** publish the qualified caller inventory plus required transitive callers, recalculate step/slice sizes, replace the ellipsis, and declare the acceptance shell.

The named owner-approved items are represented. No Step 7 getter has a live caller that mutates its returned entity and saves it; the tracking failure above concerns read-path fix-up.

The other stated grep baselines reproduced in Git Bash, and the existing test class contains 19 facts. Builds, tests, installs, and fixes were not run because they can write files. The saved npm report has six vulnerable packages with `fixAvailable: true`, supporting compatible remediation, consistent with [npm’s documentation](https://docs.npmjs.com/cli/v11/commands/npm-audit/). A fresh audit failed to reach the registry, so live fix availability remains unverified. No files were modified.

---

# Round 2 · 2026-10-13 · job task-mv6n3163-zjv9l2 · Read: full · Floor: MEDIUM

## Verdict: NO-GO

Plan lines refer to [plan.md](D:/src/fokus/docs/specs/F37-CodeQualityCleanup/delivery/plan.md). Closure assesses the revised instructions, not implementation.

| R1 id | Status | Plan line | Collateral |
|---|---|---|---|
| R1-01 | closed | 113–116 | Fix-up removal and endpoint audit specified. |
| R1-02 | closed | 124–125 | Separate tab-load overwrite remains: R2-02. |
| R1-03 | closed | 78–80 | True counts specified; candidate regression: R2-01. |
| R1-04 | closed | 78, 143, 193 | Closed + Active basis reconciled. |
| R1-05 | partial | 96–101 | Direct scans included, but Scope forbids part of their migration: R2-03. |
| R1-06 | closed | 23, 97–99 | Existing comparison semantics preserved. |
| R1-07 | closed | 98, 101, 198 | Index construction and loop restrictions explicit. |
| R1-08 | closed | 68, 71 | Exit clamped to entry; regression case added. |
| R1-09 | closed | 66, 69–71 | Lookup region and mixed feature fixture covered. |
| R1-10 | closed | 87–89 | Tests exercise window selection and the failing old selection. |
| R1-11 | closed | 122, 125 | Render sites fixed; error-erasure defect: R2-04. |
| R1-12 | closed | 129–131 | Complete plumbing chain and executable paths supplied. |
| R1-13 | partial | 149–153, 172–176 | Pages widened; required failure checks remain omitted: R2-05. |
| R1-14 | closed | 5, 96–97 | Live count is 45 calls across 12 files; unguarded direct scan preserved. |
| R1-15 | closed | 55, 60, 152 | Constant location, indicator update and input clearing specified. |
| R1-16 | closed | 70–72 | Fixture parameters and observable assertions specified. |

1. **HIGH · `fold` · R2-01 — [plan.md:79](D:/src/fokus/docs/specs/F37-CodeQualityCleanup/delivery/plan.md:79): selected-sprint candidates hide existing zombie flags.**  
   Live [SprintSummaryService.cs:823](D:/src/fokus/src/Services/Fokus/Fokus.API/Features/Analytics/SprintSummaryService.cs:823) selects candidates across the window. A ticket appearing in three preceding sprints, but absent from the selected sprint, currently appears; the revised instruction removes it regardless of its projected count. The approved A1-01 evidence also explicitly expects historical zombies outside the window ([verdicts-1.md:62](D:/src/fokus/docs/specs/F36-PublicReleaseCleanup/delivery/mining/verdicts-1.md:62)).  
   **Suggested fix:** project the eligible historical zombie rows, including display metadata and filtering information, rather than restricting candidates to selected memberships. Add cases absent from the selected sprint and outside the sparkline window.

2. **HIGH · `round-1 miss: Settings tab load/save` · R2-02 — [plan.md:124](D:/src/fokus/docs/specs/F37-CodeQualityCleanup/delivery/plan.md:124): failed excluded-status loading still permits configuration loss.**  
   The dedicated `loadError` protects `/api/settings`, while Jira’s excluded statuses load separately. After that request fails, the component retains `[]`; its save remains enabled and [saveExcluded:144](D:/src/fokus/client/src/components/settings/JiraTab.vue:144) clears `excludedError` before submitting it. [SaveExcludedStatusesEndpoint.cs:27](D:/src/fokus/src/Services/Fokus/Fokus.API/Features/Settings/SaveExcludedStatuses/SaveExcludedStatusesEndpoint.cs:27) replaces the stored list. Merely displaying the load error leaves this overwrite path open.  
   **Suggested fix:** gate excluded-status editing and saving on successful loading, independently of save errors. Verify that blocking this GET cannot erase existing exclusions.

3. **MEDIUM · `fold` · R2-03 — [plan.md:96](D:/src/fokus/docs/specs/F37-CodeQualityCleanup/delivery/plan.md:96): Step 5 contradicts the scope exclusion.**  
   Step 5 requires migrating `BuildBurnupData` scans at `551–557` and `564–570`. Both belong to the non-bug feature branch ([ScopeChangeService.cs:548](D:/src/fokus/src/Services/Fokus/Fokus.API/Features/Analytics/ScopeChangeService.cs:548)). Line 23 excludes *any* refactor of that method beyond the bug-area rule. These instructions cannot both be followed.  
   **Suggested fix:** explicitly permit Step 5’s mechanical lookup migration while keeping the broader A1-04 method cleanup excluded.

4. **MEDIUM · `fold` · R2-04 — [plan.md:122](D:/src/fokus/docs/specs/F37-CodeQualityCleanup/delivery/plan.md:122): sibling QA fetches erase unresolved errors.**  
   Untested and failing lists load independently through [QaMetricsSection.vue:23](D:/src/fokus/client/src/components/dashboard/QaMetricsSection.vue:23). Under the new rule, a failed untested fetch sets `qaError`, but opening the failing list clears it even if that request succeeds. The untested list remains empty and displays “No untested tickets” without its failure being resolved.  
   **Suggested fix:** maintain errors per QA resource; clear each only for its own retry or a selection reset. Verify one failed list followed by a successful sibling fetch.

5. **MEDIUM · `fold` · R2-05 — [plan.md:153](D:/src/fokus/docs/specs/F37-CodeQualityCleanup/delivery/plan.md:153): R1-13’s failure-path acceptance is incomplete.**  
   The revised operator check omits failed capacity saving, failed Re-detect after a previous successful detection, excluded-status loading, and the production cookie check. These were requested in round 1. A Developers load failure does not exercise the separate capacity-save path at [developersStore.ts:261](D:/src/fokus/client/src/stores/developersStore.ts:261).  
   **Suggested fix:** add explicit pass/fail items for these paths, including preserved workflow stages, capacity rollback with a visible error, blocked excluded-status saving, and production `Secure` cookie behavior.

6. **MEDIUM · `round-1 miss: Step 6 early returns` · R2-06 — [plan.md:105](D:/src/fokus/docs/specs/F37-CodeQualityCleanup/delivery/plan.md:105): replacing existing error blocks does not guarantee missing-sprint 404s.**  
   [GetSprintSummaryEndpoint.cs:21](D:/src/fokus/src/Services/Fokus/Fokus.API/Features/Analytics/GetSprintSummary/GetSprintSummaryEndpoint.cs:21) returns 200 before validating a supplied ID when no analytics sprints exist. [GetDeveloperThroughputEndpoint.cs:20](D:/src/fokus/src/Services/Fokus/Fokus.API/Features/Analytics/GetDeveloperThroughput/GetDeveloperThroughputEndpoint.cs:20) similarly returns 200 when no closed sprints exist. Step 6’s replacements leave these paths intact, contradicting the Step 12 guarantee.  
   **Suggested fix:** validate explicit sprint IDs before empty-data shortcuts; preserve empty 200 responses for aggregate/default requests. Check missing IDs with empty history as well as populated history.

No files were modified. Builds, tests and package operations were not run.
