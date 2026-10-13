# F37 — Code Quality Cleanup

**Feature Spec:** None — plan-only definition (owner, 2026-10-13)
**Model:** claude-opus-5-5
**Build slices:** 1–4 · 5 · 6–7 · 8–11; why: step 5 (45 checker calls in 12 files plus 7 direct scans) and step 7 (a cross-endpoint fix-up audit) are heavy, so step 5 runs alone and step 7 shares a slice only with the mechanical step 6; the Settings fix and the three calculation bugs stay together; the frontend error pass, config and docs close
**Lane:** full — owner's level
**Rule list:** not applicable — plan-only definition

## Context

F36 (public release) Phase 2 mined the codebase with `mine-verify-repo`: 68 findings, every one reproduced by a separate skeptic, merged to 61. The owner triaged them on 2026-10-13 into this lean campaign; everything else goes to the shipped known-open-work page (an F36 deliverable, not this feature).

- **Definition:** `docs/specs/F36-PublicReleaseCleanup/delivery/mining/consolidated.md` § Recommended lean campaign + the backlog rows TD-1 and TD-3. Each step cites its finding id; the finding's evidence and skeptic verdict are in `mining/findings-*.md` and `mining/verdicts-{1,2}.md`.
- **Service:** Fokus only (`src/Services/Fokus/*`) and the client (`client/src/*`). No module, contract or schema change.
- **Same-topic grep (plan time):** no earlier plan covers these items. `BUG-1`/`BUG-2`/`GAP-4` (burnup/bar-chart disagreements) touch the same burnup code but different series (feature scope/completed); they stay open and out of scope.
- **Why each fix:** five are wrong results or data loss a user sees; three are patterns the repo's own conventions forbid that students would copy as house style (`csharp.md:12-13`, `csharp.md:55`, `vue.md:20`); two are config/security debt.
- **Test harness:** `Fokus.Tests` (xunit, FluentAssertions, NSubstitute) holds service-level tests only (`DeveloperProgressServiceTests.cs`, 19 facts) with fixture builders in `_Fixtures/TestData.cs`. There is no endpoint test harness and no frontend test runner (`client/package.json` has no test script) — endpoint-only and frontend steps are accepted by grep, build and the operator page check (Step 12), which therefore visits every page these steps touch.

## Scope

**In scope:** the eleven developer steps below plus the operator page check.

**Out of scope:** every other mined finding (F36 known-open-work page), G-01 (Domain → Jira contracts; owner ruled open work), BUG-1, BUG-2, GAP-4, TD-2; the three private `IsStartedInSprint` copies that already take pre-filtered transitions (`DeveloperQualityService.cs:460`, `QaMetricsService.cs:194`, `QaTrendsService.cs:222`) and the inline copy in `TestExecutionRepository.cs:157` (A2-09, open work); the existing case-insensitive per-ticket dictionaries (`EpicProgressService.cs:112`, `ScopeChangeService.cs` `transitionsByTicket` feeding `IsRemovedPostPlanning` at :320-328, `SprintSummaryService.ComputeMetrics` grouping) — they keep their comparison; any refactor of `BuildBurnupData` beyond the bug-area rule (Step 2) and Step 5's mechanical swap of its whole-list scans for index lookups (A1-04, the method's wider cleanup, stays open work).

## Skill Mapping

| Step | Skill | Disposition | TDD | Feature-Specific Inputs | Gap? |
|------|-------|-------------|-----|------------------------|------|
| 1 | vue-patterns, pinia-patterns | Follow | no | The mask never enters the form; Re-detect guard parity; server ignores the mask value | — (no endpoint/front-end test harness; operator check covers it) |
| 2 | (none) | — | yes | Burnup bug-area entry/exit rule in `ScopeChangeService`; `TestData.Membership` gains optional parameters | — (edits an existing computation; tdd covers the process) |
| 3 | persistence-patterns | Follow | yes | Projection query: ticket id → distinct sprint count (≥3) over analytics sprints; `ComputeFlags` consumes it | — |
| 4 | analytics-computation-service | Follow | yes | Rolling-average window — the skill's "TakeLast after exclusion guards" rule | — |
| 5 | (none) | — | yes | Per-ticket transition index replaces whole-list scans: 45 checker calls in 12 files + 7 direct scans | gap: no skill for a behaviour-preserving performance refactor across a feature area |
| 6 | error-handling | Follow | no | `NotFoundException` in 9 analytics endpoints | — |
| 7 | persistence-patterns | Follow | no | Remove cross-query fix-up reliance, then `.AsNoTracking()` on the listed read-only methods | — |
| 8 | pinia-patterns, vue-patterns | Follow | no | Surface swallowed errors on Dashboard, Developers, Settings (dedicated settings `loadError`) | — |
| 9 | (none) | — | no | Cookie `SecurePolicy` by environment, plumbed Program → AddFokusServices → AddFokusAuth | — (one config branch) |
| 10 | (none) | — | no | `npm audit fix` within semver ranges | — |
| 11 | (none) | — | no | KB entries scope-change, health-score, throughput | — (kb-maintenance rule governs) |

## Domain Model Changes

None.

## Data Model Changes

None — no entity, column or migration. Step 3 adds one read-only projection query.

## Implementation Steps

### Step 1 — Settings: the Xray secret mask never round-trips; Re-detect keeps the guard (A4-01, A4-02)

Follow vue-patterns and pinia-patterns.

- **Secret (A4-01, data loss).** The GET response keeps masking the secret (`GetSettingsEndpoint.cs:27,34-35`). Declare the mask once in a new `src/Services/Fokus/Fokus.API/Features/Settings/XraySecretMask.cs` and use it in `GetSettingsEndpoint` and `SaveXraySettingsEndpoint`; `SaveXraySettingsEndpoint.cs:35-36` treats the mask value exactly like an empty value (keeps the stored secret). Client: `SettingsView.vue` `syncFromStore` (line 58) stops copying `xrayClientSecret` into `form` (the field starts empty); `XrayTab.vue` shows "secret configured" in the input's placeholder when the store's masked value is non-empty and sends `null` when the field is blank (line 36). After a successful save with a typed secret, `settingsStore.saveXraySettingsAction` (lines 184-195) sets the store's `xrayClientSecret` to a non-empty value (as GET would return) and `XrayTab` clears the input, so the indicator updates without a reload and the typed secret does not linger in the form.
- **Re-detect (A4-02).** `WorkflowTab.vue` `reDetect` (lines 87-92) applies the result only under the same guard as `SettingsView.vue:84` (`stages.length` > 0). `settingsStore.ts` `runDetectWorkflowStages` (lines 171-181) clears `detectionResult` before each run, so a failed run cannot re-apply a stale result.
- **Accept:**
  - `grep -n "xrayClientSecret: s.xrayClientSecret" client/src/views/SettingsView.vue` → no match.
  - `grep -n "if (store.detectionResult) {" client/src/components/settings/WorkflowTab.vue` → no match; the guard checks `stages.length`.
  - `grep -rn '"\*\*\*\*"' src/Services/Fokus/Fokus.API` → exactly one match, in `Features/Settings/XraySecretMask.cs`.
  - `npm run build --prefix client` passes.
  - Behaviour: Step 12 Settings checks.

### Step 2 — Burnup bug area: every bug enters before it leaves, and leaves once (A1-03)

`src/Services/Fokus/Fokus.API/Features/Analytics/ScopeChangeService.cs` `BuildBurnupData`, lines 537-713 — the bug-end lookup loop (537-591, whose `RemovedAt != null` skip at :542 also feeds the feature lookups) and the daily bug inflow/outflow (593-713). Today the area counts bugs in only when `RemovedAt == null` but counts removals out unconditionally (and without the `IsExcluded` filter), and a completion can be counted before the bug's entry day — so the area under-counts and can go negative.

- **Rule (KB `scope-change.md` § Bug area):** a bug is *eligible* when it is a bug, has effective SP and its final status is not excluded. An eligible bug's entry day is sprint day 1 when it was in the sprint at start (`AddedAt <= sprint start`), otherwise its `AddedAt` day; a bug removed on or before sprint start never enters. Its exit day is the later of its entry day and the earlier of {its first in-sprint end-stage transition, its removal day}; it exits at most once. The area on each day = SP of eligible bugs entered and not yet exited. Ticket counts follow the same rule.
- The feature lookups (start/end transitions for non-bugs) keep excluding removed memberships exactly as today; only the bug branch changes. Feature scope, completed and committed-total series stay byte-for-byte as today.
- **Fixtures:** add optional `addedAt`, `finalStatus` and `wasCommitted` parameters to `src/Services/Fokus/Fokus.Tests/_Fixtures/TestData.cs` `Membership(...)` (lines 72-88), defaulting to today's values so the 19 existing tests are untouched. Use fixed dates in the new tests.
- **TDD:** new `src/Services/Fokus/Fokus.Tests/Features/Analytics/ScopeChangeServiceTests.cs`, through the public `ComputeSingleSprint`, asserting `BurnupDataPoint.BugSp` and `BugTickets` per day. Cases: present at start, removed day 3; added day 2, removed day 4; completed day 3 then removed day 5 (exits once, day 3); completed day 2 but added day 4 (enters and exits day 4, never negative); excluded status removed mid-sprint (never enters); a mixed fixture with removed features and bugs where every feature series and committed-total value equals today's output.
- **Accept:** new tests pass; no `BugSp` or `BugTickets` value in any test is negative; `dotnet test` on `Fokus.Tests` passes.

### Step 3 — Dashboard zombie flag counts all synced sprints and shows the true count (A1-01)

`SprintSummaryService.ComputeFlags` (lines 823-841) counts distinct sprints per ticket over the ≤5-sprint sparkline window (`GetSprintSummaryEndpoint.cs:51-62`) and both flags (count ≥3) and displays/sorts by that count (`ZombieTicket.SprintCount`, :834-840; `client/src/components/dashboard/SprintFlags.vue:43`). F11 BR7 (`docs/specs/F11-CarryOverTracker/definition/spec.md:193`), KB `health-score.md:59` and `carry-over.md:24` say all synced sprints.

- Follow persistence-patterns: add a read-only projection query to `src/Services/Fokus/Fokus.Persistence/Repositories/SprintRepository.cs` returning ticket id → number of distinct sprints, for tickets in at least N distinct sprints, over sprints in state Closed or Active (the analytics sprints; Future sprints are not counted). Group ticket ids case-insensitively, as F8's `CarryOverService.GetZombieTicketKeys` (`CarryOverService.cs:534-542`) does; compute in SQL; return no entities.
- `GetSprintSummaryEndpoint` calls it with N = 3 and passes the dictionary into `SprintSummaryService.ComputeSummary`. `ComputeFlags` keeps today's candidate set — the (filtered) memberships of the window sprints it already receives — and changes only the counting basis: a candidate is a zombie when it is in the dictionary, and its `SprintCount` comes from the dictionary; the ordering stays by `SprintCount` descending. The window-based count is removed. Sparkline windows are untouched (A1-02 stays open work).
- **TDD:** new `src/Services/Fokus/Fokus.Tests/Features/Analytics/SprintSummaryServiceTests.cs` through the public `ComputeSummary`: a window ticket whose dictionary count is 6 is flagged with `SprintCount` 6 even when the window holds only 1 of its sprints; a window ticket absent from the selected sprint but in the dictionary is still flagged (today's candidate set kept); two zombies come back ordered 6 then 4; a window ticket absent from the dictionary is not flagged; the sub-team filter still hides a zombie owned by another sub-team.
- **Accept:** tests pass; `grep -n "allMembershipsFlat" src/Services/Fokus/Fokus.API/Features/Analytics/SprintSummaryService.cs` → no match.

### Step 4 — Throughput rolling average looks back past 0%-capacity sprints (A5-02)

Follow analytics-computation-service (its § Rolling average: "Uses TakeLast AFTER any exclusion guards — not before").

- `GetDeveloperThroughputEndpoint.cs:55-63` loads only 2 closed sprints before the earliest target; `DeveloperThroughputService.ComputeRollingAverage` (lines 276-312) walks back from the target (inclusive), skips 0%-capacity sprints and returns null below 3. The window must hold every closed sprint before the earliest target. Move the window selection into a small public static helper on `DeveloperThroughputService`, called by the endpoint; correct the false comment at line 56.
- `DeveloperDetailService` already passes its full qualifying list (`DeveloperDetailService.cs:95-135`) — no change (plan-time check: `TakeLast(2)` appears only at `GetDeveloperThroughputEndpoint.cs:61`).
- **TDD:** new `src/Services/Fokus/Fokus.Tests/Features/Analytics/DeveloperThroughputServiceTests.cs`. The test selects the window with the new helper and feeds it to the public `ComputeThroughput`. Positions: closed sprints P3, P2, P1, then target T (earliest target); the developer has capacity 0 in P1 and completed work in T, P2, P3 → rolling average = (T + P2 + P3) / 3. A second case asserts the old selection (two sprints before the target) would yield null for the same data, documenting the regression. A third: fewer than 3 qualifying sprints in the whole history → null.
- **Accept:** tests pass; `grep -c "TakeLast(2)" src/Services/Fokus/Fokus.API/Features/Analytics/GetDeveloperThroughput/GetDeveloperThroughputEndpoint.cs` → 0.

### Step 5 — Transition lookups read a per-ticket index, not the whole list (A5-01, A1-07) — heavy

`TransitionAttributionChecker.IsStartedInSprint` / `IsCompletedInSprint` (`TransitionAttributionChecker.cs:88-148`) filter the full transition list by ticket id on every call; throughput and progress default to all closed sprints, so the cost grows with the square of history.

- **Inventory (plan-time grep, Git Bash):** 45 calls to the two checker methods in 12 files — CarryOverService 14, SprintSummaryService 12, DeveloperDetailService 4, ScopeChangeService 4, DeveloperProgressService 3, DeveloperThroughputService 2, and one each in BugRatioService, CycleTimeService, EpicProgressService, ExcludedDeveloperFilter, LeaderboardService, `GetDeveloperDetail/GetDeveloperDetailEndpoint.cs`. Direct whole-list scans of the same shape: `DeveloperProgressService.cs:326-329`, `DeveloperDetailService.cs:347-350`, `ScopeChangeService.cs:551-557, 564-570, 580-586, 768-771`, `SprintSummaryService.cs:702-706`. Command: `grep -rn "TransitionAttributionChecker.Is\(Started\|Completed\)InSprint" src --include=*.cs | wc -l` → 45; `grep -rn "statusTransitions" src/Services/Fokus/Fokus.API/Features/Analytics --include=*.cs | grep "t.TicketId == m.TicketId"` plus the multi-line `.Where(t => t.TicketId ==` forms above.
- Introduce one per-ticket transition index type in `src/Services/Fokus/Fokus.API/Features/Analytics/` (built once from a transition list, looked up by ticket id, **ordinal case-sensitive key** — matching the `==` every one of these sites uses today). The two checker methods take the index instead of the list. The direct scans read the ticket's transitions from the index and keep their own predicates unchanged — including `SprintSummaryService.cs:702-706`, which has no `endIndex < 0` guard today and keeps none.
- **Index lifetime:** public service methods keep their `List<StatusTransition>` parameters (so `DeveloperProgressServiceTests` stay untouched) and build the index once at entry. Static helpers called from endpoint loops — `ExcludedDeveloperFilter.GetExcludedDeveloperIds` (called per developer × target sprint, `GetDeveloperThroughputEndpoint.cs:88-91`, and from the other analytics endpoints) and the check in `GetDeveloperDetailEndpoint.cs:73` — take the index; each endpoint builds it once per request and passes it to them. No index is built inside a loop, a per-membership lambda or a per-sprint helper.
- The existing case-insensitive dictionaries listed in Scope stay as they are.
- **TDD first (pins behaviour before the refactor):** new `src/Services/Fokus/Fokus.Tests/Features/Analytics/TransitionAttributionCheckerTests.cs` covering start/completion inside the sprint window, before it, after it, at the exact boundaries; the first qualifying transition wins and its timestamp is returned; a ticket id differing only in case does not match; end index < 0. Write and pass these against today's signature; after the refactor only their call lines change (building the index from the same fixture list).
- **Accept:** `rg -n -U "\.Where\(t => t\.TicketId == ticketId" src/Services/Fokus/Fokus.API/Features/Analytics/TransitionAttributionChecker.cs` → no match; each direct-scan site listed above reads from the index; `implementation.md` lists every index construction site with its enclosing method, and none is inside a loop or lambda; `DeveloperProgressServiceTests` (19) pass with no change to that file; all new tests pass.

### Step 6 — Missing sprint returns 404 via `NotFoundException` (A1-06)

Follow error-handling. In the 9 endpoints below, replace `AddError(r => r.SprintId, "Sprint not found."); await SendErrorsAsync(400, ct); return;` with `throw new NotFoundException(...)` (Blocks.Exceptions; `GlobalExceptionMiddleware.cs:57` maps it to 404; working precedent `SyncSprintsEndpoint.cs:29-31`). Files (all under `src/Services/Fokus/Fokus.API/Features/Analytics/`): `GetSprintSummary/GetSprintSummaryEndpoint.cs:34`, `GetCarryOver/GetCarryOverEndpoint.cs:49`, `GetBugRatio/GetBugRatioEndpoint.cs:54`, `GetDeveloperQuality/GetDeveloperQualityEndpoint.cs:46`, `GetDeveloperThroughput/GetDeveloperThroughputEndpoint.cs:35`, `GetCycleTime/GetCycleTimeEndpoint.cs:42`, `GetQaWorkload/GetQaWorkloadEndpoint.cs:117`, `GetLeaderboard/GetLeaderboardEndpoint.cs:61`, `GetScopeChange/GetScopeChangeEndpoint.cs:42`. The client does not depend on the 400 (`client/src/api/client.ts:19-21` throws on any non-OK status).

- **Order of checks:** all 9 endpoints return an empty 200 when there are no sprints *before* they validate a supplied `SprintId` (plan-time: the `Count == 0` shortcut sits at lines 20–30, above the not-found check, in every one). An explicit `SprintId` that does not exist must return 404 whether or not any sprints exist; requests without a `SprintId` keep their empty 200.
- **Accept:** `grep -rln 'AddError(r => r.SprintId, "Sprint not found.")' src/Services/Fokus/Fokus.API/Features` → 0 files (plan-time baseline: 9); `grep -rln "throw new NotFoundException" src/Services/Fokus/Fokus.API/Features/Analytics` lists the 9 files; in each of the 9, the explicit-id check precedes the empty-data shortcut (one line per endpoint in `implementation.md`); build passes.

### Step 7 — No cross-query fix-up, then read-only queries do not track (A1-09, A2-12) — heavy

Follow persistence-patterns. Two parts, in order.

- **7a — remove the hidden fix-up dependency.** Several analytics endpoints take an entity from one query and read navigation properties that a *later* tracked query filled in through EF relationship fix-up. Plan-time confirmed: `GetCarryOverEndpoint.cs:46`, `GetBugRatioEndpoint.cs:51` and `GetLeaderboardEndpoint.cs:58` take the single-sprint `target` from `GetAnalyticsSprintsAsync` (no `Include`) and pass it to services that read `target.Memberships`, filled only by the later `GetSprintsWithMembershipsAsync` (`SprintRepository.cs:30-36`). With `AsNoTracking` those memberships stay empty. Change each such endpoint to take the entity from the query that loads the navigation (e.g. the membership-loaded list). Audit every endpoint that calls both `GetAnalyticsSprintsAsync` and a membership-loading method (plan-time list: GetBugRatio, GetCarryOver, GetCycleTime, GetDeveloperDetail, GetDeveloperProgress, GetDeveloperQuality, GetDeveloperThroughput, GetEpicProgress, GetFailingTickets, GetLeaderboard, GetQaMetrics, GetQaTrends, GetQaWorkload, GetScopeChange, GetSprintSummary, GetUntestedTickets, `Sprints/GetTestTimeline`) and record a verdict per endpoint in `implementation.md` (relies on fix-up: fixed / does not: why). Apply the same audit to `Ticket.StatusTransitions` and `Ticket.Assignee` navigations read off entities from another query.
- **7b — `.AsNoTracking()`** on exactly these methods — classified READ-ONLY at plan time (every caller only reads; none mutates and saves): `SprintRepository` — `GetAnalyticsSprintsAsync`, `GetSprintsWithMembershipsAsync`, `GetSprintWithMembershipsAsync`, `GetAllAnalyticsSprintMembershipsAsync`, `GetAllAsync`; `TicketRepository` — `GetStatusTransitionsForSprintTicketsAsync`, `GetStatusTransitionsForTicketsAsync`, `GetTicketsWithEpicAsync`, `GetTicketsWithoutEpicInSprintsAsync`; `TestExecutionRepository` — `GetTestExecutionsForSprintAsync`, `GetFeatureTicketsWithCoverageAsync`, `GetTestExecutionDataForTicketsAsync`, `GetByIssueIdsAsync`; `DeveloperRepository` — `GetCapacitiesForSprintsAsync`, `GetCapacitiesForDeveloperAsync`, `GetActiveDevelopersAsync`, `GetAllAsync`, `GetCapacityAsync`; `AppUserRepository` — `GetByEmailAsync`, `GetAllAsync`; `InvitationRepository` — `GetAllAsync`, `GetPendingByEmailAsync`. Pure projections need nothing. Before each, confirm that every navigation its callers read is covered by that method's own `Include`/`ThenInclude` (one line per method in `implementation.md`).
- **Never** set a global `QueryTrackingBehavior`, and leave untouched every WRITE-PATH or MIXED method: `AppSettingsRepository.GetAsync`, `SprintRepository.GetByIdAsync`, `DeveloperRepository.GetByIdAsync`, `AppUserRepository.GetByGoogleIdAsync`/`GetByIdAsync`, `InvitationRepository.GetByTokenAsync`/`GetByIdAsync`, all `Upsert*`/`Replace*`/`Update*`.
- **Accept:** `grep -n "analyticsSprints.FirstOrDefault(s => s.Id == req.SprintId.Value)" src/Services/Fokus/Fokus.API/Features/Analytics/GetCarryOver/GetCarryOverEndpoint.cs src/Services/Fokus/Fokus.API/Features/Analytics/GetBugRatio/GetBugRatioEndpoint.cs src/Services/Fokus/Fokus.API/Features/Analytics/GetLeaderboard/GetLeaderboardEndpoint.cs` → no match feeding a service; `grep -c "AsNoTracking" src/Services/Fokus/Fokus.Persistence/Repositories/*.cs` → non-zero in the six files above; `grep -n "AsNoTracking" src/Services/Fokus/Fokus.Persistence/Repositories/AppSettingsRepository.cs` → no match; full suite green; Step 12 single-sprint checks on every analytics page show non-empty data.

### Step 8 — Errors are shown, not swallowed (A2-05, A3-01, A4-04)

Follow pinia-patterns and vue-patterns. Rule: `docs/conventions/vue.md:20`. Use the existing inline pattern (`<div class="text-sm text-status-danger">{{ … }}</div>`, as in `TeamView.vue:31-32`); no new component.

- **Dashboard:** `dashboardStore.ts` `fetchQaMetrics` / `fetchUntestedTickets` / `fetchFailingTickets` (lines 79-103) each set their own error ref (one per QA resource — metrics, untested, failing) instead of `catch {}`; each is cleared only by its own fetch or by a sprint selection reset, so a sibling's success never erases it. `DashboardView.vue` renders the metrics error in a standalone block outside `QaMetricsSection`'s `v-if` (line 155 requires `qaMetrics !== null`, which stays null on failure). The summary's `store.error` is rendered before the "No sprint data yet" empty state (today it is never rendered). Xray-disabled stays silent (server returns 200 + empty, `GetQaMetricsEndpoint.cs:30-32`).
- **Developers:** `DevelopersView.vue` renders `store.error` (set by `developersStore.ts` at lines 48, 159, 278, 285).
- **Settings:** add a dedicated `loadError` to `settingsStore.ts`, set only by `fetchSettings` (lines 55-65) and cleared only by a successful `fetchSettings`. When it is set, `SettingsView.vue` stops initialisation (no `syncFromStore`, no auto-detect — lines 79-90) and shows the error in place of the tabs, so the store's defaults cannot be saved over real settings. The shared `store.error` keeps its role for saves; tabs keep reporting save errors locally. Data a tab loads by its own request follows the same rule as `loadError`: when that load fails, the tab shows the error and disables editing and saving of that data until a reload succeeds — `JiraTab.vue:60-65` (excluded statuses: today a failed load leaves `[]`, and `saveExcluded` at :144 would replace the stored list via `SaveExcludedStatusesEndpoint.cs:27`) gets a load-failed flag separate from `excludedError`, which saves keep using; `WorkflowTab.vue:40-47` (cycle-time boundaries) catches into a load-failed state with the same gating, keeping `boundariesError` for saves; `UsersTab.vue:24-26` reports a rejected `allSettled` result through `userMgmtError` (line 15) and disables the affected list's actions.
- **Accept:** `grep -c "catch {" client/src/stores/dashboardStore.ts` → 0 (baseline 3); `grep -c "Error" client/src/views/DashboardView.vue` → ≥2 (summary and QA errors rendered); `grep -c "store.error" client/src/views/DashboardView.vue client/src/views/DevelopersView.vue` → ≥1 each (baseline 0); `grep -c "loadError" client/src/stores/settingsStore.ts client/src/views/SettingsView.vue` → ≥1 each; `grep -c "silently ignore" client/src/components/settings/JiraTab.vue` → 0 (baseline 1); `npm run build --prefix client` passes.

### Step 9 — Auth cookie is `Secure` outside Development (TD-1)

`src/Services/Fokus/Fokus.API/Auth/AuthConfiguration.cs:22` sets `CookieSecurePolicy.SameAsRequest` unconditionally. Use `Always` in every environment except Development, where it stays `SameAsRequest` so students on `http://localhost:5000` keep signing in. Plumb the environment through the chain: `src/Services/Fokus/Fokus.API/Program.cs:12` (`builder.Services.AddFokusServices(builder.Configuration)`) → `src/Services/Fokus/Fokus.API/DependencyInjection.cs:16-18` (`AddFokusServices` → `AddFokusAuth(configuration)`) → `AuthConfiguration.AddFokusAuth`.

- **Accept:** `grep -n "CookieSecurePolicy.Always" src/Services/Fokus/Fokus.API/Auth/AuthConfiguration.cs` → 1 match; `grep -n "CookieSecurePolicy.SameAsRequest" src/Services/Fokus/Fokus.API/Auth/AuthConfiguration.cs` → 1 match, inside the Development branch; `grep -n "builder.Environment" src/Services/Fokus/Fokus.API/Program.cs` → the `AddFokusServices` call; build passes.

### Step 10 — Front-end package advisories (TD-3)

In `client/`, run `npm audit fix` without `--force`. Plan-time evidence: the F36 audit (`docs/specs/F36-PublicReleaseCleanup/delivery/npm-audit.json`) lists 6 high advisories — `vue`, `@vue/server-renderer`, `vite`, `postcss`, `nanoid`, `source-map-js` — each `fixAvailable: true`; the plan reviewer's `npm audit fix --package-lock-only` on a scratch copy reached 0 vulnerabilities with no `--force` (vite → 8.3.x and rolldown rc → 1.2.x, inside the declared ranges). Commit `package-lock.json` (and `package.json` only if npm changed it).

- **Accept:** `npm audit --audit-level=high` in `client/` exits 0 (plan-time baseline: exit 1); `npm ci && npm run build` in `client/` passes. If any advisory needs `--force`, stop and record it in `questions.md` — do not force.

### Step 11 — Update the KB entries the fixes change

Edit with the Edit tool (never rewrite), per the KB maintenance rule:
- `docs/kb/analytics/scope-change.md` § Burnup Chart, Bug area — the entry/exit rule and eligibility filters of Step 2.
- `docs/kb/analytics/health-score.md` — zombie flag: the window's tickets are flagged when they appear in ≥3 distinct analytics sprints (Closed + Active) over the whole history, with that true count shown; note that the Carry-Over page counts closed sprints only (Step 3). Correct the stale mid-sprint line that says "hardcoded 2 days" (the code uses `PlanningWindowDays`).
- `docs/kb/analytics/throughput.md` § Rolling Average — the window holds every closed sprint before the target (Step 4).
- **Accept:** `grep -n "hardcoded 2 days" docs/kb/analytics/health-score.md` → no match; each entry states its new rule; `docs/kb/index.md` unchanged.

### Step 12 — Operator page check — Owner: operator

With a synced sprint, in Development over `http://localhost`:
- **Sign-in** still works over HTTP in Development (Step 9).
- **Every page touched by steps 3–8 loads with data, single-sprint and multi-sprint where the page has both:** Dashboard (summary, flags, QA section), Sprints (burnup), Carry-Over, Cycle Time, Epics, QA Trends, Team, Developers (Throughput, Daily Progress, Bug Ratio, Leaderboard, Quality, QA Workload), Developer Detail, Settings (every tab, Users included).
- **Fixed behaviours:** Xray save without retyping keeps the secret (Test Connection works) and the placeholder shows it is configured; a typed secret is stored and the field clears; Re-detect with an empty result keeps the stages; the burnup bug area never goes below zero; a zombie shows its real sprint count; a missing sprint id in an analytics URL returns 404.
- **Failure paths (stop the API or block one request in the browser dev tools):** Settings load failure shows the error instead of the tabs and nothing can be saved; a failed excluded-statuses load shows its error and its save is disabled (the stored list survives); a failed workflow-boundaries load shows its error and its save is disabled; a failed Re-detect after an earlier successful one keeps the current stages; a failed QA metrics fetch shows its error while the summary stays visible; a failed untested-tickets fetch keeps its error after the failing-tickets list loads; a failed summary shows its error instead of "No sprint data yet"; a failed Developers load shows `store.error`; a failed capacity save rolls the value back and shows an error; a rejected Users/Invitations load shows its message.
- **Production cookie:** run once with `ASPNETCORE_ENVIRONMENT=Production` behind HTTPS (or inspect the `Set-Cookie` header) and confirm the auth cookie carries `Secure`.
- Record pass/fail per item in `implementation.md` § Operator check. **OPERATOR ACTION REQUIRED** — the close may proceed before it; the merge waits for it.

## Cross-Service Changes

None.

## Migration Notes

None.

## Must-NOT-Change

| Invariant | Pinned by | Disposition |
|-----------|-----------|-------------|
| Checker results (started/completed + timestamp) for every input | `TransitionAttributionCheckerTests` written green before the refactor (Step 5) | new-test (step 5) |
| Ticket-id matching at the migrated sites stays ordinal, case-sensitive; the existing case-insensitive dictionaries keep theirs | Step 5 case-difference test; Scope out-of-scope list | new-test (step 5) |
| Daily progress outputs | `DeveloperProgressServiceTests` (19, existing, file untouched) | pinned |
| Throughput, sprint summary flags, burnup bug area | Steps 2–4 tests | new-test (steps 2–4) |
| Carry-over, leaderboard, bug-ratio, cycle-time, epic, developer-detail outputs after Steps 5 and 7 | compile-time signature change + Step 12 per-page single- and multi-sprint loads | accepted-gap (no endpoint harness; adding one is out of scope) |
| Burnup feature scope, completed and committed-total series | Step 2 mixed-fixture test | new-test (step 2) |
| Entities read through navigations are loaded by the query that includes them | Step 7a per-endpoint audit verdicts + Step 12 single-sprint loads | accepted-gap (audit + operator check) |
| Write paths still track their entities | Step 7 accept: no `AsNoTracking` in `AppSettingsRepository.cs`; untouched-method list; full suite | pinned |
| Students on `http://localhost` can still sign in | Step 9 accept + Step 12 sign-in item | pinned |
| Typed Xray secret is still saved | Step 12 | accepted-gap (no endpoint/front-end test harness) |
| Sparkline windows (A1-02 open work) | Step 3 leaves the window logic at `GetSprintSummaryEndpoint.cs:51-62` for sparklines; Step 3 tests | pinned |

## Testing Strategy

Backend steps 2–5 are test-first through public service methods with `TestData` fixtures (extended with optional parameters in Step 2); four new test classes under `Fokus.Tests/Features/Analytics/`, named per `docs/conventions/testing.md`. Steps 6, 7 and 9 change wiring and are covered by the build, the full suite, grep accepts and the Step 7a audit. Front-end steps 1 and 8 are accepted by grep, `npm run build` and the operator check (no test runner exists — adding one is out of scope). The close runs the declared full suite (`.claude/verify.json` `roles.full`: solution build, all tests, client build).

## KB Impact

`docs/kb/analytics/scope-change.md`, `docs/kb/analytics/health-score.md`, `docs/kb/analytics/throughput.md` — Step 11.

## Decisions

| Decision | Why | Rejected alternative | Status |
|---|---|---|---|
| Xray secret: the form never holds the mask, and the server also ignores the mask value | Fixes the cause on the client and closes the hole for any other client | Server-only guard (the client would still round-trip a sentinel) | decided |
| Zombie count over all analytics sprints (Closed + Active), not Future | F11 BR7 and the KB say "all synced sprints"; synced active sprints are real history; future sprints hold planned work only | Closed only plus the selected sprint (misses a ticket in 2 closed + 1 active sprint when viewing a closed one); window only (today's bug) | decided |
| Zombie data from one SQL projection (id → count) | The dashboard needs the true count for display and ordering without loading every sprint's memberships | Load all sprints with memberships (heavier, same answer) | decided |
| Zombie candidates stay the window's tickets; only the counting basis moves to the whole history | Fixes the cited defect (the count) without changing which tickets the dashboard lists; smallest reversible change | List every zombie in all history (could fill each sprint's dashboard with long-dead tickets); list only the selected sprint's tickets (hides window zombies shown today) | decided |
| Bug-area exit day is the later of entry and first completion/removal | Guarantees the area never goes negative and each bug leaves once; matches the KB | Subtract each event as it happens (double count, negative area) | decided |
| Throughput window loads every prior closed sprint | The walk-back needs history to skip 0%-capacity sprints; the dataset is bounded (ef-core.md) | Load incrementally until 3 qualify (more round-trips, more code) | decided |
| Transition index keyed ordinally; existing case-insensitive dictionaries left alone | Preserves today's results exactly at every site; this is a performance fix | One shared index everywhere (would change epic activity dates and removed-post-planning for case-differing ids) | decided |
| Public service signatures keep `List<StatusTransition>`; endpoints pass the index only to static helpers | The existing 19 tests stay untouched; loop-called helpers stop rebuilding | Index in every public signature (rewrites every test and caller) | decided |
| Remove fix-up reliance before turning tracking off | `AsNoTracking` silently empties memberships that were filled by fix-up | Skip tracking changes on sprint queries (leaves the convention breach on the hottest reads) | decided |
| `AsNoTracking` per method, never as a global default | Settings and user/invitation getters are loaded then saved | Global `QueryTrackingBehavior.NoTracking` | decided |
| Dashboard summary error rendered too (beyond the QA catches) | Same convention, same view; a failed summary today looks like "no data" | Leave it (the view would still swallow one error) | decided |
| Settings load failure uses a dedicated `loadError` | The shared `error` is reset by detect and set by saves | Reuse `store.error` (load error lost; failed save blanks the tabs) | decided |
| Cookie `SameAsRequest` stays in Development | Students run on `http://localhost:5000` | `Always` everywhere | decided |
| No front-end test runner added | Owner ruled out new test investment for the release; front-end fixes are small and operator-checked | Add Vitest for steps 1 and 8 | decided |

## Open Questions

None.

## Plan Review

Round 1 (2026-10-13), reader pair Codex + Sonnet: Codex NO-GO (1 CRITICAL, 5 HIGH, 7 MEDIUM, 1 LOW), Sonnet REVISE (2 HIGH, 2 MEDIUM, 6 LOW); merged to 16 findings (R1-01..R1-16), all folded as fixed — record in `review-critic.md` § Round 1, Codex verdict in `review-critic-codex.md`. The CRITICAL (EF relationship fix-up silently emptied by `AsNoTracking`) was found by Codex only and re-verified in source by the architect. Round 2 (2026-10-13, Codex alone, full read, floor MEDIUM): NO-GO — 14 of 16 round-1 findings closed, 2 partial; 6 new (2 HIGH, 4 MEDIUM: R2-01..R2-06), all folded as fixed — `review-critic.md` § Round 2. Round 2 held no CRITICAL, so the schedule stops here: no round 3. The two HIGH folds (zombie candidate set, excluded-status overwrite) are listed in the approval message for the owner.
