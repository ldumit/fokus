# F37 — Code Quality Cleanup — Implementation

**Model:** claude-opus-5-5

## Step 1 — Settings: the Xray secret mask never round-trips; Re-detect keeps the guard
**Files:** `src/Services/Fokus/Fokus.API/Features/Settings/XraySecretMask.cs` — NEW: the single declaration of the mask (`Value`), `Apply` (mask for GET) and `IsKeepExisting` (empty or mask means keep the stored secret)
`src/Services/Fokus/Fokus.API/Features/Settings/GetSettings/GetSettingsEndpoint.cs` — private `MaskSecret` removed; response uses `XraySecretMask.Apply`
`src/Services/Fokus/Fokus.API/Features/Settings/SaveXraySettings/SaveXraySettingsEndpoint.cs` — secret overwritten only when `!XraySecretMask.IsKeepExisting(...)`, so the mask value is treated exactly like empty (A4-01)
`client/src/views/SettingsView.vue` — `syncFromStore` no longer copies `xrayClientSecret` into `form`; the field starts and stays empty
`client/src/components/settings/XrayTab.vue` — `secretPlaceholder` computed shows "Secret configured (leave blank to keep it)" when the store's masked value is non-empty; after a successful save the view clears `form.xrayClientSecret`; blank field still sends `null`
`client/src/stores/settingsStore.ts` — `saveXraySettingsAction` sets the store's `xrayClientSecret` to the mask after a save carrying a typed secret (keeps it otherwise); `runDetectWorkflowStages` clears `detectionResult` before each run (A4-02)
`client/src/components/settings/WorkflowTab.vue` — `reDetect` applies the result only when `store.detectionResult?.stages.length` (guard parity with `SettingsView.vue:84`)
**Skills:** vue-patterns, pinia-patterns
**Decisions:** After a typed-secret save the store holds `'****'` (module constant `MASKED_SECRET` in `settingsStore.ts`), the value GET returns; the view, not the store, clears the input (store/view boundary, `vue.md`). Placeholder copy chosen as above.
**Deviations:** None. Verification: fast step close green (12.8 s); plan-named `npm run build --prefix client` green; the three Step 1 accept greps pass (no `xrayClientSecret: s.xrayClientSecret`, no `if (store.detectionResult) {`, one `"****"` in the API — `XraySecretMask.cs`). Behaviour is the operator's (Step 12).

## Step 2 — Burnup bug area: every bug enters before it leaves, and leaves once
**Files:** `src/Services/Fokus/Fokus.API/Features/Analytics/ScopeChangeService.cs` — `BuildBurnupData`: the bug branch of the lookup loop and the `startingBug*`/daily inflow-outflow-completion block are replaced by a per-bug span (`BugAreaSpan(Sp, EntryDay, ExitDay)`) built once by new `BuildBugAreaSpans` + `ResolveExitDay`; each day's `BugSp`/`BugTickets` = eligible bugs with `EntryDay <= day < ExitDay`. Eligible = bug, effective SP, final status not excluded, not removed at or before sprint start; entry = sprint day 1 when `AddedAt <= start`, else `AddedAt` day; exit = later of entry and the earlier of {first in-sprint end-stage transition, removal}. Removed bugs now get the completion lookup; the feature lookups keep the `RemovedAt != null` skip unchanged (the loop now skips bugs up front instead of branching).
`src/Services/Fokus/Fokus.Tests/_Fixtures/TestData.cs` — `Membership(...)` gains optional `addedAt`, `finalStatus`, `wasCommitted` (defaults = today's values)
`src/Services/Fokus/Fokus.Tests/Features/Analytics/ScopeChangeServiceTests.cs` — NEW, 6 facts through `ComputeSingleSprint` with fixed dates: mixed fixture pinning TotalScope/Completed/CommittedTotal SP and tickets; present-at-start removed day 3; added day 2 removed day 4; completed day 3 then removed day 5 (exits once); completed day 2 but added day 4 (never negative); excluded status removed mid-sprint (never enters)
**Skills:** tdd
**Decisions:** The mixed-fixture pin was written and run green against the unchanged code before the edit; the five bug-area cases were each confirmed red on the old code (5 failed, 1 passed), then all green after. "Removed on or before sprint start" compares timestamps (`RemovedAt <= StartDate`), matching the existing `AddedAt <= StartDate` entry test; exit/entry days compare dates, so a bug that enters and exits on the same day adds 0 that day.
**Deviations:** None. The bug-branch whole-list scan formerly at `ScopeChangeService.cs:580-586` now lives in `BuildBugAreaSpans` (same predicate) — Step 5 should migrate it there. Fast step close green.

## Step 3 — Dashboard zombie flag counts all synced sprints and shows the true count
**Files:** `src/Services/Fokus/Fokus.Persistence/Repositories/SprintRepository.cs` — NEW `GetZombieSprintCountsAsync(minSprintCount)`: read-only projection over `SprintMemberships` of Closed/Active sprints, grouped by `TicketId.ToUpper()`, `COUNT(DISTINCT SprintId)` with the ≥N filter in SQL; returns `IReadOnlyDictionary<string,int>` with an `OrdinalIgnoreCase` comparer; no entities, no tracking
`src/Services/Fokus/Fokus.API/Features/Analytics/GetSprintSummary/GetSprintSummaryEndpoint.cs` — `ZombieMinSprintCount = 3`; loads the counts and passes them to `ComputeSummary`; sparkline window logic untouched
`src/Services/Fokus/Fokus.API/Features/Analytics/SprintSummaryService.cs` — `ComputeSummary` gains required `IReadOnlyDictionary<string,int> zombieSprintCounts` (after `statusTransitions`); `ComputeFlags` keeps the window candidates (filtered window memberships grouped by `TicketId`), flags a candidate when it is in the dictionary, takes `SprintCount` from the dictionary, still orders by `SprintCount` desc; the window-based count and `allMembershipsFlat` are gone
`src/Services/Fokus/Fokus.Tests/Features/Analytics/SprintSummaryServiceTests.cs` — NEW, 5 facts through `ComputeSummary`: history count 6 with 1 window sprint; window ticket absent from selected sprint still flagged; order 6 then 4; window ticket absent from dictionary not flagged; sub-team filter hides another sub-team's zombie
**Skills:** persistence-patterns, tdd
**Decisions:** Repository method name and signature `GetZombieSprintCountsAsync(minSprintCount)`; ids grouped as `upper(TicketId)` in SQL, dictionary compares case-insensitively, window candidates keep today's ordinal `GroupBy` (approved analyze choice). Removed memberships count toward the history, as today's window count and F8's `GetZombieTicketKeys` do. All 5 tests were run red (assertion failures) before `ComputeFlags` changed, then green. SQL translation verified once with a throwaway in-memory SQLite test (case-differing ids merged, Future sprints excluded, below-threshold ticket dropped), deleted after the run — not part of the suite.
**Deviations:** None. Accept: tests pass; `grep -n "allMembershipsFlat" …SprintSummaryService.cs` → no match. Fast step close green.

## Step 4 — Throughput rolling average looks back past 0%-capacity sprints
**Files:** `src/Services/Fokus/Fokus.API/Features/Analytics/DeveloperThroughputService.cs` — NEW public static `SelectRollingWindowSprintIds(ascendingClosedSprints, targetSprintIds)`: every closed sprint before the earliest target (all closed sprints when the target is active/not in the closed list); `ComputeRollingAverage` unchanged
`src/Services/Fokus/Fokus.API/Features/Analytics/GetDeveloperThroughput/GetDeveloperThroughputEndpoint.cs` — window selection replaced by the helper call (`TakeLast(2)` gone); the false comment ("all closed sprints are available") replaced by one stating the window's constraint
`src/Services/Fokus/Fokus.Tests/Features/Analytics/DeveloperThroughputServiceTests.cs` — NEW, 3 facts: P3, P2, P1(capacity 0), T → rolling average (T + P2 + P3) / 3 = 4 via helper + `ComputeThroughput`; the old two-sprints-before selection yields null for the same data; fewer than 3 qualifying sprints in the whole history → null
**Skills:** analytics-computation-service, tdd
**Decisions:** Helper name/signature `SelectRollingWindowSprintIds(IReadOnlyList<Sprint>, IReadOnlyList<int>)` returning ids only (the endpoint keeps concatenating targets); test sprints made Closed by setting `State` after `TestData.ActiveSprint` — no new fixture parameter (approved analyze choices). TDD: the helper was first added carrying today's `TakeLast(2)` selection so the slice-1 test compiled and failed on the assertion (expected 4, got null); removing `TakeLast(2)` turned it green. `DeveloperDetailService` unchanged, per plan. Prior-sprint delta selection is unaffected (it was already the last closed sprint before the target, now still loaded).
**Deviations:** None. Accept: tests pass; `grep -c "TakeLast(2)" …GetDeveloperThroughputEndpoint.cs` → 0. Fast step close green; full unit run 33/33 (19 existing + 6 + 5 + 3), `DeveloperProgressServiceTests.cs` untouched. Boy-scout not applied (no adjacent change in scope worth the diff).

## Step 5 — Transition lookups read a per-ticket index, not the whole list
**Files:** `src/Services/Fokus/Fokus.API/Features/Analytics/StatusTransitionIndex.cs` — NEW sealed class: `Build(IEnumerable<StatusTransition>)` groups once into a `Dictionary` keyed `StringComparer.Ordinal`, each ticket's transitions in source-list order; `ForTicket(ticketId)` returns them (empty list when none)
`src/Services/Fokus/Fokus.API/Features/Analytics/TransitionAttributionChecker.cs` — `IsStartedInSprint` / `IsCompletedInSprint` take `StatusTransitionIndex` and filter `ForTicket(ticketId)` by the unchanged timestamp/stage predicate (the `.Where(t => t.TicketId == ticketId` scan is gone)
`src/Services/Fokus/Fokus.API/Features/Analytics/ExcludedDeveloperFilter.cs` — `GetExcludedDeveloperIds` takes the index instead of the list
`…/Analytics/BugRatioService.cs`, `LeaderboardService.cs`, `DeveloperDetailService.cs`, `CarryOverService.cs`, `SprintSummaryService.cs` — public methods keep `List<StatusTransition>` and build the index once at entry; private helpers take the index; the per-call case-insensitive `transitionsByTicket` dictionaries that only fed the checker are removed (5 in CarryOver, 6 in SprintSummary incl. `ComputeMetrics`, 1 each in BugRatio/Leaderboard/DeveloperDetail `GetTransitionCompletedMemberships`); the sparkline `valueSelector` delegate takes the index
`…/Analytics/CycleTimeService.cs` — index built in both public methods; `ComputeTicketCycleTime` takes it, and its per-sprint `ticketIds`/`transitionsByTicket` grouping is replaced by `ForTicket(ticketId).OrderBy(Timestamp)` per completed membership
`…/Analytics/DeveloperProgressService.cs`, `DeveloperThroughputService.cs` — index built once in `ComputeProgress` / `ComputeThroughput`; private helpers (`BuildStalledTickets`, `ComputeMetrics`, `ComputeRollingAverage`) take it
`…/Analytics/EpicProgressService.cs` — index built next to the kept case-insensitive `transitionsByTicket` (still feeds activity dates); the velocity completion check uses the index; the comment above the pair now states which comparison each use has
`…/Analytics/ScopeChangeService.cs` — index built in both public methods; `ComputeSprintMetrics` / `ComputePerSprintData` take the list **and** the index (the list still builds the kept case-insensitive dictionary for `IsRemovedPostPlanning`; checker calls use the index); `GetMidSprintAdditions`, `BuildEventTable` lose their checker-only dictionaries; `BuildBurnupData`, `BuildBugAreaSpans`, `ComputeBugTimeInProgress` take the index
`…/Analytics/Get{BugRatio,CarryOver,CycleTime,DeveloperDetail,DeveloperProgress,DeveloperThroughput,Leaderboard,ScopeChange,SprintSummary}/…Endpoint.cs` — one `StatusTransitionIndex.Build` right after the transitions load, passed to `GetExcludedDeveloperIds` (and to the inline check in `GetDeveloperDetailEndpoint`); services still receive the list
`src/Services/Fokus/Fokus.Tests/Features/Analytics/TransitionAttributionCheckerTests.cs` — NEW, 8 facts: inside window; before/after (±1 tick); inclusive boundaries; earliest qualifying wins regardless of list order; stage below the end boundary does not complete; case-differing id does not match (and matches its own casing); negative start/end index; ticket with no transitions
**Skills:** tdd (plan: Skill none, TDD yes)
**Decisions:** Type name `StatusTransitionIndex` (glossary term "Status Transition"); static `Build` + `ForTicket`. The 8 checker facts were written and run green against the old `List` signature before any production edit; after the refactor only the two call lines in the test helpers changed (`StatusTransitionIndex.Build(_transitions)`). Removing a case-insensitive dictionary that only fed the checker preserves results: the checker already re-filtered each sublist with ordinal `==`, so dictionary-then-checker and the ordinal index select the same transitions. The three Scope-listed dictionaries keep their comparison: EpicProgress (activity dates) and ScopeChange `ComputeSprintMetrics` (`IsRemovedPostPlanning`) are untouched; `SprintSummaryService.ComputeMetrics`' grouping fed only checker calls, so with the checker taking the index it had no remaining reader and was removed — its effective (ordinal) result is unchanged. Each direct scan keeps its own predicate minus the ticket compare, and its ordering: `DeveloperProgressService.BuildStalledTickets`, `DeveloperDetailService.BuildTicketDetails` (OrderByDescending), `ScopeChangeService.BuildBurnupData` ×2 (feature start/end, `endIndex >= 0` guard kept on the end scan), `BuildBugAreaSpans` (the Step-2 home of the former :580-586 bug completion lookup, per carry-over), `ComputeBugTimeInProgress` (OrderBy, `ToList()` for `ComputeTimeInActiveStatuses`), `SprintSummaryService.ComputeTopEpics` (`Any`, still no `endIndex < 0` guard).
**Index construction sites** (none inside a loop, lambda or per-sprint helper): `BugRatioService.ComputeMultiSprint`, `.ComputeSingleSprint`; `CarryOverService.ComputeMultiSprint`, `.ComputeSingleSprint`; `CycleTimeService.ComputeSingleSprint`, `.ComputeMultiSprint`; `DeveloperDetailService.ComputeDetail`; `DeveloperProgressService.ComputeProgress`; `DeveloperThroughputService.ComputeThroughput`; `EpicProgressService.ComputeEpicProgress`; `LeaderboardService.ComputeMultiSprint`, `.ComputeSingleSprint`; `ScopeChangeService.ComputeMultiSprint`, `.ComputeSingleSprint`; `SprintSummaryService.ComputeSummary`; `HandleAsync` of `GetBugRatioEndpoint`, `GetCarryOverEndpoint`, `GetCycleTimeEndpoint` (single branch, multi branch), `GetDeveloperDetailEndpoint` (built from the closed-sprint list before the active-sprint append — the same list the check read before), `GetDeveloperProgressEndpoint`, `GetDeveloperThroughputEndpoint`, `GetLeaderboardEndpoint`, `GetScopeChangeEndpoint` (single branch, multi branch), `GetSprintSummaryEndpoint`; test helpers in `TransitionAttributionCheckerTests` only.
**Deviations:** None. Accept: `TicketId == ticketId` in `TransitionAttributionChecker.cs` → 0; `grep -rn "TicketId == " …/Features/Analytics` → no match (every direct-scan site reads `ForTicket`); 45 production checker calls (+2 test); `DeveloperProgressServiceTests.cs` unchanged (`git status` clean for it); new tests green; fast step close green. Boy-scout not applied (mechanical refactor; scope kept to the swap). `graphify update .` not run — graphify-out is outside the developer's file set; owed by the main session.

## Step 6 — Missing sprint returns 404 via `NotFoundException`
**Files:** `src/Services/Fokus/Fokus.API/Features/Analytics/{GetSprintSummary,GetCarryOver,GetBugRatio,GetDeveloperQuality,GetDeveloperThroughput,GetCycleTime,GetLeaderboard,GetScopeChange}/…Endpoint.cs` — right after the analytics-sprints load, `requestedSprint = req.SprintId.HasValue ? list.FirstOrDefault(id) ?? throw new NotFoundException($"Sprint {id} was not found.") : null`; the single-sprint branch now reads `if (requestedSprint is Sprint target|match)` (Sprint Summary: `selectedSprintInfo = requestedSprint ?? sprints[0]`); the `AddError(…"Sprint not found.")` + `SendErrorsAsync(400)` blocks are gone
`src/Services/Fokus/Fokus.API/Features/Analytics/GetQaWorkload/GetQaWorkloadEndpoint.cs` — same lookup in `HandleAsync`; `HandleSingleSprintAsync` takes the resolved `Sprint targetSprintInfo` instead of `req` + `allSprints` (its own lookup, `req.SprintId!` and 400 block removed)
**Skills:** error-handling
**Decisions:** One inline lookup per endpoint, not a shared helper — the plan's accept greps `throw new NotFoundException` per file. Message follows the `SyncSprintsEndpoint` precedent ("… was not found."). An explicit id that exists keeps today's flow: with no closed sprints it still returns the empty 200 (only a non-existent id changes). `GetDeveloperQuality` and `GetQaWorkload` keep their Xray-disabled 200 first — that shortcut precedes the sprint load and is a feature-off response, not the empty-data shortcut; the id check precedes their `ascending.Count == 0` shortcut.
**Order of checks (explicit id before empty-data shortcut):**
- GetSprintSummary — `requestedSprint` lookup, then `sprints.Count == 0`
- GetCarryOver — lookup, then `ascending.Count == 0`
- GetBugRatio — lookup, then `ascending.Count == 0`
- GetDeveloperQuality — lookup, Xray-disabled 200, then `ascending.Count == 0` (order changed in Fix Round 1; was Xray-disabled 200 first)
- GetDeveloperThroughput — lookup, then `ascending.Count == 0`
- GetCycleTime — lookup, then `ascending.Count == 0`
- GetQaWorkload — lookup, Xray-disabled 200, then `ascending.Count == 0` (order changed in Fix Round 1; was Xray-disabled 200 first)
- GetLeaderboard — lookup, then `ascending.Count == 0`
- GetScopeChange — lookup, then `ascending.Count == 0`
**Deviations:** None. Accept: `grep -rln 'AddError(r => r.SprintId, "Sprint not found.")' …/Features` → 0 files; `grep -rln "throw new NotFoundException" …/Features/Analytics` → the 9 files; fast step close green (build + unit, 29.5 s). The lookup line in GetCarryOver/GetBugRatio/GetLeaderboard still feeds `target` to the services — Step 7a moves that.

## Step 7 — No cross-query fix-up, then read-only queries do not track
**Files:** `src/Services/Fokus/Fokus.API/Features/Analytics/GetCarryOver/GetCarryOverEndpoint.cs`, `GetBugRatio/GetBugRatioEndpoint.cs`, `GetLeaderboard/GetLeaderboardEndpoint.cs` — 7a: the single-sprint `target` is now `allSprints.First(id)` (the `GetSprintsWithMembershipsAsync` list) when the requested sprint is Closed; an Active target keeps the lightweight entity (it is not in the closed-only membership load — see Decisions)
`src/Services/Fokus/Fokus.Persistence/Repositories/SprintRepository.cs` — `.AsNoTracking()` on `GetAllAsync`, `GetAnalyticsSprintsAsync`, `GetSprintWithMembershipsAsync`, `GetSprintsWithMembershipsAsync`, `GetAllAnalyticsSprintMembershipsAsync`
`…/Repositories/TicketRepository.cs` — on `GetStatusTransitionsForTicketsAsync`, `GetStatusTransitionsForSprintTicketsAsync` (the entity query; the ticket-id query is a projection), `GetTicketsWithEpicAsync`, `GetTicketsWithoutEpicInSprintsAsync`
`…/Repositories/TestExecutionRepository.cs` — on `GetByIssueIdsAsync`, `GetTestExecutionsForSprintAsync` (final entity query), `GetFeatureTicketsWithCoverageAsync` (memberships, transitions and test-run entity queries), `GetTestExecutionDataForTicketsAsync` (test-run entity query); the other queries in them are projections
`…/Repositories/DeveloperRepository.cs` — on `GetAllAsync`, `GetActiveDevelopersAsync`, `GetCapacityAsync`, `GetCapacitiesForDeveloperAsync`, `GetCapacitiesForSprintsAsync`
`…/Repositories/AppUserRepository.cs` — on `GetByEmailAsync`, `GetAllAsync`; `…/Repositories/InvitationRepository.cs` — on `GetAllAsync`, `GetPendingByEmailAsync`
**Skills:** persistence-patterns
**7a audit — navigations read in `Fokus.API`:** `Sprint.Memberships`, `SprintMembership.Ticket`/`.Sprint`, `Ticket.Assignee`, `TestExecution.Links`/`.TestRuns`, `TestExecutionLink.Ticket`. Never read anywhere: `Ticket.StatusTransitions`, `StatusTransition.Ticket`, `TestRun.ExecutedBy`/`.TestExecution`, `TestExecution.Assignee`, `TestSet.Assignee`, `DeveloperSprintCapacity.Developer`/`.Sprint` — so no fix-up from the transition, capacity or developer loads is relied on. Every `.Assignee` read is off a ticket whose own query includes it (`m.Ticket.Assignee` via the sprint/membership includes; `t.Assignee` in Epic Progress/Sprint Summary via `GetTicketsWith(out)EpicAsync`).
**7a verdict per endpoint:**
- GetBugRatio — relied on fix-up (single-sprint `target` from `GetAnalyticsSprintsAsync`, memberships filled by the later closed-sprint load): fixed; multi does not (`allSprints` from the membership load)
- GetCarryOver — relied on fix-up (same shape): fixed; multi does not
- GetLeaderboard — relied on fix-up (same shape): fixed; multi does not
- GetCycleTime — does not: target/prior taken from `GetSprintsWithMembershipsAsync`; multi uses `loadedSprints`
- GetDeveloperDetail — does not: `closedSprints` from the membership load; active sprint from `GetSprintWithMembershipsAsync`; `analyticsSprints` read for scalars only
- GetDeveloperProgress — does not: services get `sprintWithMemberships`
- GetDeveloperQuality — does not: every sprint handed on (target, prior, sparkline, streak) comes from `GetSprintsWithMembershipsAsync`; deduped by `Id`
- GetDeveloperThroughput — does not: `loadedSprints` only
- GetEpicProgress — does not: `analyticsSprints` read for scalars only (the service reads no `.Memberships`); memberships from `GetAllAnalyticsSprintMembershipsAsync` (includes `Sprint`, `Ticket.Assignee`)
- GetFailingTickets — does not: `sprint` from `GetSprintWithMembershipsAsync`; the coverage query includes its own `Ticket.Assignee`
- GetQaMetrics — does not: selected/window/prior from `GetSprintsWithMembershipsAsync`; TE links include `Ticket`
- GetQaTrends — does not: `sortedLoaded`/`allSortedSprints` from the membership load
- GetQaWorkload — does not: every sprint handed on is loaded with memberships (Step 6 passes the lightweight entity for its `Id` only)
- GetScopeChange — does not: target/prior/multi from `GetSprintsWithMembershipsAsync`
- GetSprintSummary — does not: `selectedSprint` and window from `GetSprintsWithMembershipsAsync`
- GetUntestedTickets — does not: same as GetFailingTickets
- Sprints/GetTestTimeline — does not: `sprint`/`priorSprint` from `GetSprintsWithMembershipsAsync`; `BuildEmptyResponse` reads lightweight scalars only
**7b — navigations covered by each method's own Include:**
- SprintRepository.GetAnalyticsSprintsAsync — callers read scalars only (after 7a; the Active-target case below reads empty `Memberships`, as before); none needed
- SprintRepository.GetSprintsWithMembershipsAsync / GetSprintWithMembershipsAsync — `Memberships → Ticket → Assignee` covers every read (`m.Ticket`, `m.Ticket.Assignee`)
- SprintRepository.GetAllAnalyticsSprintMembershipsAsync — `Sprint`, `Ticket → Assignee` covers `sm.Sprint`, `sm.Ticket.Assignee` (Epic Progress)
- SprintRepository.GetAllAsync — only caller `SyncXrayEndpoint` reads `sprint.Id`; none needed
- TicketRepository.GetStatusTransitionsForSprintTicketsAsync / GetStatusTransitionsForTicketsAsync — no navigation read (the latter has no caller)
- TicketRepository.GetTicketsWithEpicAsync / GetTicketsWithoutEpicInSprintsAsync — `Assignee` covers `t.Assignee`
- TestExecutionRepository.GetTestExecutionsForSprintAsync — `Links → Ticket`, `TestRuns` cover `te.Links`, `link.Ticket.IssueType`, `te.TestRuns`
- TestExecutionRepository.GetFeatureTicketsWithCoverageAsync — memberships `Ticket → Assignee` covers `ticket.Assignee`; transitions/test runs read scalars; returns a projection record
- TestExecutionRepository.GetTestExecutionDataForTicketsAsync — test runs read scalars; returns dictionaries
- TestExecutionRepository.GetByIssueIdsAsync — `Links`, `TestRuns` via `Query()`; no caller
- DeveloperRepository.GetAllAsync / GetActiveDevelopersAsync — `Developer` has no navigation
- DeveloperRepository.GetCapacitiesForSprintsAsync / GetCapacitiesForDeveloperAsync / GetCapacityAsync — capacity navigations never read (`GetCapacityAsync` has no caller)
- AppUserRepository.GetByEmailAsync / GetAllAsync, InvitationRepository.GetAllAsync / GetPendingByEmailAsync — no navigations; `CreateInvitationEndpoint` only null-checks the two by-email results, then adds a new invitation
**Decisions:** The plan-time READ-ONLY classification was re-verified: every caller of the 22 methods only reads; no sync or upsert flow calls them. An **Active** single-sprint target on Carry-Over, Bug Ratio and Leaderboard was never in the membership load (closed sprints only), so today its `Memberships` are empty and those pages compute over nothing for it. Loading it would change outputs (its transitions and capacities are not loaded either), against the plan's Must-NOT-Change row, so it keeps the lightweight entity — results identical before and after, no fix-up involved. No global `QueryTrackingBehavior`; WRITE/MIXED methods untouched. The six repository files were edited by script, then restored to CRLF.
**Deviations:** Accept grep `analyticsSprints.FirstOrDefault(s => s.Id == req.SprintId.Value)` still matches once in each of the three files: it is the Step 6 existence check; a Closed target now reaches the service as the membership-loaded entity, an Active target as the lightweight one (above; Carry-Over row). Other accepts: `AsNoTracking` non-zero in the six repository files (Sprint 5, Ticket 4, TestExecution 6, Developer 5, AppUser 2, Invitation 2); `AppSettingsRepository.cs` → no match; no `QueryTrackingBehavior` in `src`; fast step close green (14.6 s). The full suite and the Step 12 single-sprint checks belong to the close gate and the operator.
**Carry-over (for the tail, architect):** Active sprint selected on Carry-Over / Bug Ratio / Leaderboard single-sprint computes over no memberships (pre-existing, unchanged by this step) — medium — `GetCarryOverEndpoint`/`GetBugRatioEndpoint`/`GetLeaderboardEndpoint` load memberships, transitions and capacities for closed sprints only, while `/api/sprints` lists the active sprint — Step 12 "single-sprint shows non-empty data" will fail there for the active sprint; fixing it is a behaviour change outside this plan.

## Step 8 — Errors are shown, not swallowed
**Files:** `client/src/stores/dashboardStore.ts` — three error refs `qaMetricsError`, `untestedError`, `failingError`; each QA fetch clears its own at start and sets it in its `catch (e)` (the three `catch {}` are gone); `clearQaState` (the sprint / sub-team selection reset) clears all three
`client/src/views/DashboardView.vue` — `store.error` rendered after the initializing skeleton and before the "No sprint data yet" empty state; the QA metrics error rendered in its own block (Xray enabled + `qaMetricsError`) above `QaMetricsSection`, outside that section's `qaMetrics !== null` `v-if`; passes `untested-error` / `failing-error` to the section
`client/src/components/dashboard/QaMetricsSection.vue` — props `untestedError`, `failingError`; each expanded list shows its own error before its empty message (collapse and re-expand retries the fetch, as before)
`client/src/views/DevelopersView.vue` — `store.error` replaces the "No developer data yet" empty state when initialization failed (no sprints), and shows above the tab bar otherwise (throughput load / capacity save failures)
`client/src/stores/settingsStore.ts` — new `loadError`, set only by `fetchSettings` on failure and cleared only by its success; `fetchSettings` no longer reads or writes the shared `error` (kept for saves and detect)
`client/src/views/SettingsView.vue` — after `fetchSettings`, initialization returns early when `store.loadError` (no `syncFromStore`, no auto-detect); the template shows `loadError` in place of the tabs
`client/src/components/settings/JiraTab.vue` — the "silently ignore" catch replaced by `excludedLoadError` (separate from `excludedError`, which saves keep); `excludedLocked` (read-only, still loading, or load failed) disables remove, the dropdown, Add and Save for excluded statuses; the load error is shown in the section
`client/src/components/settings/WorkflowTab.vue` — boundaries load gets a `catch` into `boundariesLoadError` (shown in the section); `boundariesLocked` disables both selects and Save Boundaries; `boundariesError` stays for saves
`client/src/components/settings/UsersTab.vue` — `loadUserManagement` reports each rejected `allSettled` result through `userMgmtError` (reason message or a fallback) and sets `usersLoadFailed` / `invitationsLoadFailed`; role select and Active toggle disabled when the users load failed, Revoke when the invitations load failed; a later fully successful load clears a load-failure message
**Skills:** pinia-patterns, vue-patterns
**Decisions:** Error markup is the inline `text-sm text-status-danger` div of `TeamView.vue`, no new component. Untested/failing errors render inside `QaMetricsSection` next to their own lists (the section is visible whenever those fetches can run). Excluded statuses are also locked while their load is in flight (`excludedLoading` starts `true`), so a save cannot send the empty pre-load list — same data-loss path as a failed load. A failed Developers init shows the error instead of the empty state; once sprints exist the error sits above the tabs, so a failed capacity save (value already rolled back by the store) is visible on any tab. `fetchSettings` is also called by Dashboard and Developers; they read only `settings`, so moving its failure to `loadError` changes nothing there.
**Deviations:** None. Accept: `grep -c "catch {" dashboardStore.ts` → 0; `grep -c "Error" DashboardView.vue` → 4; `grep -c "store.error"` → DashboardView 1, DevelopersView 2; `grep -c "loadError"` → settingsStore 4, SettingsView 2; `grep -c "silently ignore" JiraTab.vue` → 0; `npm run build --prefix client` green (vue-tsc + vite); fast step close green (8.8 s). Behaviour is the operator's (Step 12 failure paths). Boy-scout not applied (scope kept to the error paths).

## Step 9 — Auth cookie is `Secure` outside Development
**Files:** `src/Services/Fokus/Fokus.API/Auth/AuthConfiguration.cs` — `AddFokusAuth` gains `IHostEnvironment environment`; `Cookie.SecurePolicy` is `SameAsRequest` when `environment.IsDevelopment()`, otherwise `Always`
`src/Services/Fokus/Fokus.API/DependencyInjection.cs` — `AddFokusServices` gains `IHostEnvironment environment` and passes it to `AddFokusAuth`
`src/Services/Fokus/Fokus.API/Program.cs` — `AddFokusServices(builder.Configuration, builder.Environment)`
**Skills:** None (plan: Skill none, TDD no)
**Decisions:** `IHostEnvironment` passed through (not a bool), so the branch reads `IsDevelopment()` at the one place that decides; the policy is a ternary, its Development arm `SameAsRequest`. No other caller of `AddFokusServices` / `AddFokusAuth` exists (grep over `src`), so no test host needed updating.
**Deviations:** None. Accept: `CookieSecurePolicy.Always` → 1 match; `CookieSecurePolicy.SameAsRequest` → 1 match, the Development arm (line 23); `builder.Environment` in `Program.cs` → the `AddFokusServices` call; fast step close green (build + unit, 8.7 s). The Production `Secure` flag and HTTP sign-in in Development are Step 12's.

## Step 10 — Front-end package advisories
**Files:** `client/package-lock.json` — `npm audit fix` (no `--force`): 2 packages added, 24 changed; resolved now include vue / @vue/server-renderer 3.5.44, vite 8.3.4, rolldown 1.2.13, postcss 8.5.30, nanoid 3.3.20, source-map-js 1.2.2. `client/package.json` unchanged (every fix inside the declared ranges)
**Skills:** None (plan: Skill none, TDD no)
**Decisions:** None
**Deviations:** None. Accept: `npm audit --audit-level=high` exit 1 before, 0 after ("found 0 vulnerabilities"); no advisory needed `--force`; `npm ci` then `npm run build` in `client/` green; fast step close green (9.2 s). The lockfile is left uncommitted for the coordinator (no per-step commit strategy declared).

## Step 11 — Update the KB entries the fixes change
**Files:** `docs/kb/analytics/scope-change.md` — § Burnup Chart, Bug area: the "Unchanged" line replaced by the Step 2 rule (per-bug `[entry, exit)` span, eligibility filters incl. removed-at-or-before-start, entry day, exit day = later of entry and earlier of completion/removal; feature series unaffected)
`docs/kb/analytics/health-score.md` — § Flags: zombie line states the window candidates, the ≥3 distinct Closed + Active sprints over the whole history, the true count shown and sorted on, the `GetZombieSprintCountsAsync` source, and that the Carry-Over page counts closed sprints only; the stale "hardcoded 2 days" mid-sprint line now reads `sprint start + PlanningWindowDays` (default 2), matching `SprintSummaryService.cs:810-813`
`docs/kb/analytics/throughput.md` — § Rolling Average: first bullet states the window (every closed sprint before the earliest target, via `SelectRollingWindowSprintIds`) and why a fixed look-back fails
**Skills:** None (plan: Skill none, TDD no; kb-maintenance rule governs)
**Decisions:** Each change is an in-place Edit inside the existing section — no section removed or shortened. The Carry-Over closed-only count was re-checked in source (`CarryOverService.GetZombieTicketKeys` over the closed-sprint membership load); the default 2 in `AppSettings.cs:42`.
**Deviations:** None. Accept: `grep -n "hardcoded 2 days" health-score.md` → no match; each entry states its new rule; `docs/kb/index.md` unchanged (`git diff --quiet`); fast step close green (7.0 s).

## Fix Round 1
**Files:** `src/Services/Fokus/Fokus.API/Features/Analytics/GetDeveloperQuality/GetDeveloperQualityEndpoint.cs` — row 3: the analytics-sprints load and the explicit-id `NotFoundException` lookup now run right after the settings load, before the Xray-disabled 200; a nonexistent explicit id is 404 whether Xray is on or off; valid ids and no-id requests still get the empty 200 when Xray is off
`src/Services/Fokus/Fokus.API/Features/Analytics/GetQaWorkload/GetQaWorkloadEndpoint.cs` — row 3: same reorder (lookup before the Xray-disabled 200)
`client/src/components/settings/UsersTab.vue` — row 2: `usersLoadFailed`/`invitationsLoadFailed` flags replaced by their own messages `usersLoadError`/`invitationsLoadError`; each is set only by its own failed load and cleared only by its own successful reload in `loadUserManagement`; `userMgmtError` is now mutation-only (role, Active, Revoke), so clearing it at a mutation's start no longer hides a load failure; the three messages render separately; role select and Active toggle disable on `usersLoadError`, Revoke on `invitationsLoadError`
`docs/specs/F37-CodeQualityCleanup/delivery/implementation.md` — row 3: the Step 6 block's check-order lines for GetDeveloperQuality and GetQaWorkload updated to the new order, annotated as changed here
`src/Services/Fokus/Fokus.API/Features/Analytics/DeveloperThroughputService.cs` — row 4: `SelectRollingWindowSprintIds` returns `IReadOnlyList<int>` (`csharp.md` § Return Types); both callers (the endpoint's `Concat`, the tests' `LoadWindow(IReadOnlyList<int>)`) compile unchanged
`src/Services/Fokus/Fokus.API/Features/Analytics/ScopeChangeService.cs` — row 5: in `ComputeSprintMetrics` the `ticketTransitions` lookup moved inside the removed-membership branch, after its Bug and excluded-status skips — its only reader is `IsRemovedPostPlanning`; results unchanged
**Skills:** error-handling (row 3, Step 6's mapping), vue-patterns, pinia-patterns (row 2, Step 8's mapping), analytics-computation-service, tdd (row 4, Step 4's mapping), tdd (row 5, Step 5's mapping — refactor-while-green; `ScopeChangeServiceTests` / `DeveloperThroughputServiceTests` are the net, no new test since behaviour is unchanged)
**Decisions:** The sprint load moved above the Xray check unconditionally (one lightweight `GetAnalyticsSprintsAsync` query even when Xray is off) rather than a second conditional lookup — one lookup site per endpoint, as Step 6 decided. Class checked: of the Step 6 endpoints, only these two return before the lookup; JiraTab (`excludedLoadError`) and WorkflowTab (`boundariesLoadError`) already keep load and save errors apart. Row 1: every test this round ran through `verify-run.js --profile fast`; no raw test command was run.
**Deviations:** Row 3's Step 6 note edit touches a closed step block, which `implementation-format` otherwise forbids — done because the fix list names it; the edit is annotated so the original order stays readable. Verification: fast step close green (`verify-run`, pass, 7.0 s, 19:07Z) for rows 1–3; `npm run build --prefix client` green (vue-tsc + vite); after rows 4–5, fast step close green again (`verify-run`, pass, 4.3 s, 19:08Z). No endpoint test exists for these two endpoints (plan: Step 6 TDD no); the 404 path is the operator's (Step 12).

## Key Decisions
- Step 1: the form never holds the Xray secret mask; after a typed-secret save the store holds `'****'` (the value GET returns) and the view clears the input; the API treats the mask exactly like empty.
- Step 2: the bug area is a per-bug `[entry, exit)` span; removal compares timestamps against sprint start, entry/exit compare dates.
- Step 3: `GetZombieSprintCountsAsync(minSprintCount)` groups `upper(TicketId)` in SQL, case-insensitive dictionary; removed memberships count toward the history.
- Step 4: `SelectRollingWindowSprintIds` returns ids of every closed sprint before the earliest target; the endpoint appends the targets.
- Step 5: `StatusTransitionIndex` (ordinal key, source order); public service signatures keep `List<StatusTransition>`; case-insensitive dictionaries that only fed the checker were removed (results unchanged), the two scope-listed ones kept.
- Step 6: one inline `?? throw new NotFoundException(...)` per endpoint, before every empty-data shortcut.
- Step 7: Carry-Over / Bug Ratio / Leaderboard take a Closed single-sprint target from the membership load; an Active target keeps the lightweight entity (see Carry-Over, user ruling Q1).
- Step 8: inline `text-sm text-status-danger` errors; excluded statuses locked while loading as well as after a failed load; `fetchSettings` writes only `loadError`.
- Step 9: `IHostEnvironment` plumbed Program → `AddFokusServices` → `AddFokusAuth`.
- Step 11: KB edits in place, no index change.

## Skills Used
| Step | Skill(s) invoked | Notes |
|------|------------------|-------|
| 1 | vue-patterns, pinia-patterns | |
| 2 | tdd | plan: Skill none, TDD yes |
| 3 | persistence-patterns, tdd | |
| 4 | analytics-computation-service, tdd | |
| 5 | tdd | plan: Skill none, TDD yes |
| 6 | error-handling | |
| 7 | persistence-patterns | |
| 8 | pinia-patterns, vue-patterns | |
| 9 | None | plan: Skill none, TDD no |
| 10 | None | plan: Skill none, TDD no |
| 11 | None | plan: Skill none, TDD no (kb-maintenance rule) |
| 12 | — | Owner: operator; not a developer step |

## Carry-Over Findings
| Title | Severity | For | Evidence | Note |
|-------|----------|-----|----------|------|
| Active sprint as single target on Carry-Over / Bug Ratio / Leaderboard computes over no memberships | medium | architect | `GetCarryOverEndpoint`, `GetBugRatioEndpoint`, `GetLeaderboardEndpoint` load memberships, transitions and capacities for closed sprints only, while `/api/sprints` lists the active sprint | Pre-existing, unchanged by F37. User ruled "Leave for later" (questions.md Q1): F37 keeps today's behaviour; the remaining Step 7 accept-grep match in these three files is accepted; Step 12 skips the active sprint on these three pages; a backlog row records the gap at close |
| `graphify update .` not run | low | reviewer | Step 5 block — `graphify-out/` is outside the developer's file set | Owed by the main session after the code steps (CLAUDE.md graphify rule) |
| Step 12 operator page check outstanding | medium | reviewer | plan Step 12 — OPERATOR ACTION REQUIRED | Front-end steps 1 and 8, the Step 7 single-sprint loads and the Step 9 cookie are verified only there; the close may proceed, the merge waits |

## KB Changes
| Entry | Action | What changed |
|-------|--------|-------------|
| `docs/kb/analytics/scope-change.md` | UPDATE | Bug area entry/exit rule and eligibility filters (Step 2) |
| `docs/kb/analytics/health-score.md` | UPDATE | Zombie flag counts all Closed + Active sprints with the true count; Carry-Over page counts closed only; mid-sprint line uses `PlanningWindowDays` |
| `docs/kb/analytics/throughput.md` | UPDATE | Rolling-average window holds every closed sprint before the target (Step 4) |

## Deviations from Plan
- Step 7: the accept grep `analyticsSprints.FirstOrDefault(s => s.Id == req.SprintId.Value)` still matches once in each of `GetCarryOverEndpoint.cs`, `GetBugRatioEndpoint.cs`, `GetLeaderboardEndpoint.cs` — it is the Step 6 existence check, and it still feeds an Active target to the service. Accepted by the user's ruling (questions.md Q1).
- No other step deviated. Uncommitted for the coordinator (no per-step commit strategy declared), including `client/package-lock.json` from Step 10.

*Status: COMPLETE — developer, 2026-10-13*
