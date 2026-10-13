# F37 plan review — record

## Round 1 — 2026-10-13 — plan — primary (codex)

Header note: reader pair `codex + sonnet` (picked, `reader-pair.md`). Primary: Codex, job `task-mv6lpo29-ye4d9j`, completed inside its limit — its verdict and findings are persisted verbatim in `review-critic-codex.md` (that file is this round's primary reader section). Codex returned no coverage ledger. The second reader's message arrived truncated once and was re-requested in full; the full text is below.

Primary verdict: **NO-GO** — 14 findings (1 CRITICAL, 5 HIGH, 7 MEDIUM, 1 LOW). See `review-critic-codex.md`.

## Second reader — round 1 (sonnet)

# F37 Code Quality Cleanup — Critic Review (second reader, Sonnet) — FULL TEXT

### Mode: Plan Review | Round 1 | Verdict: REVISE
Two HIGH (S3-1, S8-1), both code-verified. No CRITICAL, so no round-1 adversarial escalation. Steps 1, 2, 4, 6, 9, 10 and the unforced `npm audit fix` check out against live code. Step 3 and Step 8 are underspecified.

### Pre-commitment predictions vs found
- Shared-helper blast radius (Step 5): FOUND. Call-site count is wrong, most callers have no pinning test, acceptance cannot prove the performance goal (S5-1, S5-2, S5-3).
- AsNoTracking on mixed methods (Step 7): NOT FOUND. I read every caller of every listed method. All are read-only. GetByIssueIdsAsync and GetCapacityAsync have no callers.
- Zombie projection vs F8 (Step 3): FOUND. The projection returns ids only, so the displayed sprint count has no source (S3-1).
- Fixture feasibility for TDD cases: FOUND. TestData.Membership cannot set AddedAt or FinalStatus (S2-2).
- Frontend error surfacing: FOUND. store.error is shared and reset by other actions (S8-1, S8-2).

### Coverage ledger
Round: 1 · Depth: mechanism · Bar: mechanics · Ruler: seed only (critic-calibration/references/ruler.md; no docs/critic-calibration overlay in this repo) · Read: full

| Requirement / area | Surfaces opened | Status |
|---|---|---|
| A4-01 secret round-trip (Step 1) | GetSettingsEndpoint.cs; SaveXraySettingsEndpoint.cs; SettingsView.vue; XrayTab.vue; settingsStore.ts; api/settings.ts (grep) | PARTIAL (S1-1) |
| A4-02 Re-detect guard (Step 1) | WorkflowTab.vue 87-92; SettingsView.vue 84; settingsStore.ts 171-181 | COVERED |
| A1-03 burnup bug area (Step 2) | ScopeChangeService.cs 98-231 and 519-746; KB scope-change.md; TestData.cs | PARTIAL (S2-1, S2-2) |
| A1-01 zombie flag (Step 3) | SprintSummaryService.cs 99-246 and 680-886; GetSprintSummaryEndpoint.cs; CarryOverService.cs 150-180 and 528-542; SprintRepository.cs; SprintFlags.vue (grep); F11 spec:193; KB health-score.md, carry-over.md | PARTIAL (S3-1) |
| A5-02 rolling window (Step 4) | GetDeveloperThroughputEndpoint.cs; DeveloperThroughputService.cs 40-330; DeveloperDetailService.cs 92-136 and 413-445; skill § Rolling average | COVERED (S4-1) |
| A5-01 / A1-07 index (Step 5) | TransitionAttributionChecker.cs; all call sites by grep; BugRatio, CycleTime, DeveloperProgress, ExcludedDeveloperFilter, Leaderboard, EpicProgress callers; findings A5-01, A1-07 | PARTIAL (S5-1, S5-2, S5-3) |
| A1-06 not-found (Step 6) | all 9 cited lines grep-verified; NotFoundException; GlobalExceptionMiddleware.cs; client.ts; SyncSprintsEndpoint.cs | COVERED |
| A1-09 / A2-12 AsNoTracking (Step 7) | all 7 repositories; every caller of every listed method; AuthConfiguration.cs, SetCapacityEndpoint, UpdateTeamConfigEndpoint, CreateInvitationEndpoint, SyncXrayEndpoint | COVERED (net gap in S5-1) |
| A2-05 cluster (Step 8) | dashboardStore.ts; DashboardView.vue; QaMetricsSection.vue; developersStore.ts; DevelopersView.vue; JiraTab.vue; WorkflowTab.vue; UsersTab.vue; HealthTab.vue (grep). Baselines 3/1/0/0 confirmed | PARTIAL (S8-1, S8-2) |
| TD-1 cookie (Step 9) | AuthConfiguration.cs; DependencyInjection.cs; Program.cs; launchSettings.json; README | COVERED (S9-1) |
| TD-3 npm advisories (Step 10) | package.json; ran npm audit fix --dry-run, then --package-lock-only on a scratch copy | COVERED. Result: 0 vulnerabilities, no --force, platform bindings preserved |
| KB updates (Step 11) | scope-change.md; health-score.md lines 59-60 ("hardcoded 2 days" confirmed); throughput.md heading | COVERED |
| Operator check (Step 12) | client views/ list; DevelopersView tabs | PARTIAL (S5-1) |
| consolidated.md § Recommended lean campaign | read in full | COVERED: items 1-9 map to steps 1, 2, 3, 4, 5, 6, 8, 7, 9+10 |
| Finding text for A4-01, A4-02, A1-03, A1-01, A1-06, A1-09; verdicts-1.md and verdicts-2.md in full | — | NOT OPENED. I verified the claims against live code instead. |
| docs/conventions/vue.md, csharp.md | only the cited lines (vue.md 14-24; csharp.md 8-16, 52-57) | PARTIAL |
| SprintsView burnup consumer; CarryOverService 14 call-site bodies | — | NOT OPENED. Not needed at mechanism depth. |
| docs/critic-calibration/ruler.md overlay and corpus.jsonl | — | NOT OPENED. Neither exists in this repo. |

### Findings

#### [HIGH] S3-1 — Zombie projection returns ids only, but the flag shows and sorts by a per-ticket sprint count
Anchor: seed-0117 · above seed-0130 · below seed-0189
Shape: a changed basis leaves a dependent figure undefined
Consequence: zombie rows would show a window-based "N sprints" beside an all-sprints flag, wrong and unnoticed.
Evidence: quoted
Surfaces: SprintSummaryService.cs § ComputeFlags 828-841; client/src/components/dashboard/SprintFlags.vue:43; CarryOverService.cs:534-542; plan Step 3
Untouchable: silent-wrong-output
Source: Step 3 asks for a projection "returning the ticket ids that appear in at least N distinct sprints" and says ComputeFlags "flags a ticket … when it is in that set".
Issue:
- The live flag builds `new ZombieTicket(first.TicketId, …, g.Select(m => m.SprintId).Distinct().Count(), …)` (SprintSummaryService.cs:834-838), then `.OrderByDescending(z => z.SprintCount)` (:840).
- SprintFlags.vue:43 renders `{{ zombie.sprintCount }} sprints`.
- A set of ids carries no count. The developer's options are the windowed count (≤5 sprints, can show "2 sprints" on a flagged zombie) or a widened projection. The plan settles neither.
- F8's precedent, which the plan cites, returns Dictionary<string,int> (id to count).
Impact: flag membership is fixed, but the displayed figure and the sort order stay on the old, wrong basis.
Fix: specify an id-to-count projection (COUNT(DISTINCT SprintId) grouped by ticket). Pass that dictionary into ComputeSummary. Add a test assertion on SprintCount.

#### [HIGH] S8-1 — Settings load failure is routed through the shared store.error, which other actions reset or set
Anchor: seed-0116 · above seed-0130 · below seed-0189
Shape: a shared state flag used for a single-purpose signal
Consequence: a failed load can be wiped by the next call, leaving defaults savable over real config, while any failed save blanks every tab.
Evidence: quoted, with a scenario
Surfaces: settingsStore.ts 55-65 and 171-181; SettingsView.vue 79-90; HealthTab.vue:39; XrayTab.vue:37-41; plan Step 8 and its accept grep
Untouchable: data-loss
Source: Step 8 says SettingsView "renders store.error when the initial fetchSettings fails, and shows the error in place of the tabs". Accept: `grep -c "store.error" … SettingsView.vue → ≥1`.
Issue:
- fetchSettings catches its own failure (settingsStore.ts:60-61). Store defaults have `workflowStages: []`, so SettingsView.vue:82 immediately calls runDetectWorkflowStages().
- runDetectWorkflowStages starts with `error.value = null` (settingsStore.ts:173-174). If only /api/settings failed and detect succeeds, the load error is wiped and the tabs render over hard-coded defaults. That is the A4-04 bug the step exists to fix.
- Every tab does `await store.saveXAction(...); if (!store.error) {saved} else {localError = store.error}` (HealthTab.vue:39, XrayTab.vue:37-41, WorkflowTab.vue:56, JiraTab.vue:74, SyncTab.vue:51). A tab-replacing `v-if="store.error"` would blank the whole tab area on any failed save or detect. No action can then run to reset it, so only a reload recovers.
Impact: the intended fix does not reliably close the defaults-overwrite path, and it adds a new tab-blanking path.
Fix: add a dedicated loadError to the store, set only by fetchSettings and cleared only by a successful fetchSettings. Gate the tab area on it. Change the accept grep to name it.

#### [MEDIUM] S2-1 — "Never below zero" does not hold when a bug's first in-sprint completion precedes its entry day
Anchor: seed-0130 · above seed-0127 · below seed-0117
Shape: a stated guarantee that the stated rule does not enforce
Consequence: a bug closed on day 2 but added to the sprint on day 4 still dips the bug area below zero on days 2-3.
Evidence: scenario
Surfaces: ScopeChangeService.cs 540-591 and 664-713; plan Step 2 and Step 12
Untouchable: silent-wrong-output
Issue:
- endTransitionByBugTicket is the first end-stage transition anywhere in the sprint window (lines 580-588). It is not bounded by m.AddedAt. The inflow day is AddedAt.Date (line 670).
- Example: bug B (3 SP), end transition day 2, AddedAt day 4, no starting bugs. Day 2 gives −3, day 4 gives 0.
- The plan's test list does not include this case.
- Separately, the lookup loop at line 542 (`if (m.RemovedAt != null) continue;`) is outside the cited range 593-713. It must change for the "completed day 3, removed day 5, leaves once on day 3" case. The TDD case will force it.
Fix: state the rule for completion-before-entry, for example exit day = max(entry day, first completion or removal day), and add that test. Extend the cited range to lines 540-591.

#### [MEDIUM] S5-1 — Regression net is narrower than the blast radius of Steps 5-7, and one table row overclaims
Anchor: seed-0130 · above seed-0127 · below seed-0117
Shape: a safety claim the cited evidence does not support
Consequence: refactored carry-over, leaderboard, bug-ratio, cycle-time and epic code ships behind no test and no page visit.
Evidence: quoted
Surfaces: plan § Must-NOT-Change row 2 and Step 12; Fokus.Tests/Features/Analytics/ (one file); client views/ and DevelopersView.vue tabs
Issue:
- The row "Daily progress / throughput / carry-over outputs … pinned by DeveloperProgressServiceTests + Step 4 and Step 5 tests" is false for carry-over.
- CarryOverService holds 14 of the 45 checker call sites. The planned test files are Checker, ScopeChange, SprintSummary and Throughput. None touches CarryOverService, LeaderboardService, BugRatioService, CycleTimeService, EpicProgressService or DeveloperDetailService.
- Step 12 opens Dashboard, Sprints, Developers → Throughput and Daily Progress, Carry-Over, Settings → Xray and Workflow. It skips Developers → Bug Ratio, Leaderboard, Quality and QA Workload; Cycle Time; Developer Detail; Epics; QA Trends; Team; Settings → Users.
- Step 5 touches the first group's services. Step 6 touches nine endpoints. Step 7 touches Epics (GetAllAnalyticsSprintMembershipsAsync), Team and Users (GetAllAsync), and the QA pages (GetTestExecutionsForSprintAsync).
- With no endpoint harness, the operator check is the only net.
Fix: mark carry-over and the other unpinned services accepted-gap, with compile-time enforcement plus the operator check. Widen Step 12 to one load per view listed above.

#### [MEDIUM] S5-2 — Step 5 acceptance proves the shape of the change, not the performance goal, and has two internal contradictions
Anchor: seed-0130 · above seed-0127 · below seed-0117
Shape: acceptance criteria that cannot detect the failure the step exists to prevent
Consequence: a per-call index rebuild still passes every check, so the quadratic cost can survive or get worse.
Evidence: path
Surfaces: GetDeveloperThroughputEndpoint.cs 88-93; ExcludedDeveloperFilter.cs 20-45; plan Step 5 accept
Issue:
- ExcludedDeveloperFilter.GetExcludedDeveloperIds is static and is called once per developer per sprint from the endpoint's `activeDevelopers.Where(dev => targetSprintsLoaded.All(sprint => GetExcludedDeveloperIds(...)))`. If the index is built inside it ("each service builds the index once"), that is O(dev × sprints × T), worse than today.
- No accept item checks that the index is built once per request. The grep and the compiler confirm only the signature change.
- Accept says the 19 DeveloperProgressServiceTests and "every new test pass unchanged". That holds only if public service signatures keep taking List<StatusTransition>, but the step allows services to "receive" the index.
- The new TransitionAttributionCheckerTests call the checker, so they must change when its signature changes. They cannot pass unchanged.
Fix: require callers of static helpers (ExcludedDeveloperFilter, GetDeveloperDetailEndpoint.cs:73) to receive the index from the endpoint. Keep public service signatures list-based so the 19 tests stay untouched. Say only the checker tests' call line changes. Add a check that no index construction sits inside a loop or per-membership lambda.

#### [LOW] S5-3 — Call-site count and scope are off, and the ComputeTopEpics rewrite changes one edge
Anchor: seed-0127 · above none · below seed-0123
Shape: a figure that does not match the live count
Consequence: the developer touches out-of-scope files, or silently shifts one edge case.
Evidence: path
Surfaces: plan Step 5, Out of scope, header; SprintSummaryService.cs:702-707; TransitionAttributionChecker.cs:126
Issue:
- The plan says "50 call sites in 16 files". Live grep: 45 calls to TransitionAttributionChecker.Is{Started,Completed}InSprint in 12 files (CarryOver 14, SprintSummary 12, DeveloperDetail 4, ScopeChange 4, DeveloperProgress 3, DeveloperThroughput 2, one each in BugRatio, CycleTime, EpicProgress, ExcludedDeveloperFilter, GetDeveloperDetailEndpoint, Leaderboard).
- The raw grep returns 59 lines in 17 files. That includes comments, the checker itself, and four files the plan marks out of scope (the three private copies and TestExecutionRepository).
- "Update every caller found by the grep above" contradicts Out of scope.
- The inline check at SprintSummaryService.cs:702 has no endIndex < 0 guard, because `GetStageIndex(...) >= -1` is always true. IsCompletedInSprint returns false when endIndex < 0 (checker line 126). Swapping it in changes behaviour when no done statuses are configured, against "results unchanged for every input".
- ScopeChangeService.cs:564 and :580 carry the same per-membership inline scan and are not mentioned.
Fix: restate the count as 45 in 12 files. State whether the 702 check keeps its unguarded semantics or is deliberately aligned. Note lines 564 and 580 as left alone, or add them.

#### [LOW] S1-1 — Step 1 leaves the secret indicator stale and the typed secret in the form; the "exactly one match" accept depends on constant placement
Anchor: seed-0127 · above none · below seed-0123
Shape: unspecified post-action state
Consequence: after saving a first secret the placeholder still says none is configured, and the typed value stays in the field.
Evidence: quoted
Surfaces: settingsStore.ts:184-195; XrayTab.vue 36 and 129; plan Step 1
Issue:
- saveXraySettingsAction updates only xrayEnabled and xrayClientId in settings (settingsStore.ts:189). An indicator "derived from the masked value" stays stale until reload.
- The typed secret remains in form.xrayClientSecret (the v-model, XrayTab.vue:129). That contradicts the Skill Mapping line "Xray secret never held in the form".
- The accept grep `'"\*\*\*\*"' … Features/Settings → exactly one match` fails if the shared constant lives outside that folder (for example in the Domain). The plan names no location.
Fix: on a successful save with a non-null secret, set settings.xrayClientSecret to the mask and clear the input. Name the constant's file, inside Features/Settings.

#### [LOW] S2-2 — The TDD fixtures cannot express the Step 2 cases as written
Anchor: seed-0127 · above none · below seed-0123
Shape: a planned test whose fixture support is absent
Consequence: the developer extends shared fixtures the plan never mentions, and the accept names a local variable rather than the response field.
Evidence: path
Surfaces: Fokus.Tests/_Fixtures/TestData.cs:72-88; plan Step 2 accept; ScopeChangeService.cs:55-66
Issue:
- Membership(...) hard-codes AddedAt = DateTime.UtcNow.AddDays(-5), FinalStatus = "In Progress" and WasCommitted = true.
- With the default sprint start of UtcNow.Date.AddDays(-5), that AddedAt falls after the start. Every default membership is an "added day 1" bug. "Present at start", "added day 2" and "excluded status" cannot be built from TestData as it stands.
- The accept's cumulativeBugSp is a local inside BuildBurnupData. The observable fields are BurnupDataPoint.BugSp and BugTickets.
Fix: add optional addedAt, finalStatus and wasCommitted parameters to TestData.Membership. Use fixed dates. Rename the accept to BugSp and BugTickets.

#### [LOW] S4-1 — The Step 4 rolling-window test description is off by one
Anchor: seed-0127 · above none · below seed-0123
Shape: an ambiguous test specification
Consequence: the test may be written so it never reaches the history the fix adds.
Evidence: path
Surfaces: DeveloperThroughputService.cs:290-311; plan Step 4 TDD
Issue:
- The rolling average includes the current sprint (`i = currentIndex`, line 295).
- "Capacity 0 in one of the three sprints before the target and completed work in the fourth … mean of the three qualifying sprints" is ambiguous. With one zero-capacity sprint among P1-P3, the walk is T plus two qualifying priors and never reaches P4.
- The bug case is a zero-capacity sprint at P1 or P2 with the target as the earliest target. The current 2-extra window fails there; the full window does not.
Fix: restate the case with explicit positions, for example T, P1 zero capacity, P2 and P3 qualifying, expected mean (T + P2 + P3) / 3.

#### [LOW] S8-2 — Dashboard wording: the QA section isn't rendered when its fetch fails, and the summary error is never rendered
Anchor: seed-0127 · above none · below seed-0123
Shape: a placement the plan assumes but the template does not provide
Consequence: the QA error ref has no place to render, and the summary-failure path stays misleading.
Evidence: quoted
Surfaces: DashboardView.vue 108-121 and 154-162; dashboardStore.ts 14 and 114
Issue:
- QaMetricsSection carries `v-if="settingsStore.settings.xrayEnabled && store.qaMetrics !== null"` (DashboardView.vue:155). On a failed fetch qaMetrics stays null, so "renders it in the QA section" has nothing to attach to. A standalone block is needed.
- "The summary's error stays separate" implies the summary error is surfaced. DashboardView.vue and components/dashboard/* never reference `.error`. A failed summary shows "No sprint data yet – Go to Settings", the same vue.md:20 breach. This is outside the A2-05 text, so decide whether it is in scope.
Fix: add a standalone `v-if="store.qaError"` block. Decide whether summary-error rendering is in or out, and record it.

#### [LOW] S9-1 — The cookie change plumbs through two layers, not one
Anchor: seed-0127 · above none · below seed-0123
Shape: an understated edit surface
Consequence: none beyond a missed file in the step's file list.
Evidence: path
Surfaces: DependencyInjection.cs:18-20; Program.cs:12; AuthConfiguration.cs:12
Issue: "AddFokusAuth needs the environment … update its caller" omits that the caller AddFokusServices(IServiceCollection, IConfiguration) has no environment either. Program.cs must change too. The accept greps still hold.
Fix: list DependencyInjection.cs and Program.cs.

### Gap Analysis
- Step 7 fix-up reliance: I probed for navigation or identity-resolution reliance that AsNoTracking would break (the silent-null class ef-core.md warns about). Probes: link.Ticket.Assignee, Ticket.StatusTransitions, Sprint.Memberships aliasing in GetEpicProgressEndpoint, GetDeveloperProgressEndpoint and the QA services. I found none. Every downstream navigation is covered by an Include or ThenInclude, so there is no finding. The plan re-greps callers only for mutation. A one-line "navigation accessed downstream is covered by the method's own Include" check per method would close the gap.
- Step 3 zombie definitions: the dashboard counts closed sprints plus the selected sprint. F8 carry-over counts closed sprints only. The Decisions row covers this deliberately, so no finding. The Step 11 KB edits should state the active-sprint difference.
- Step 10: on a scratch copy of the lockfile, `npm audit fix --package-lock-only` reached "found 0 vulnerabilities" with all platform bindings still listed. The dry run moves vite to 8.3.4 and rolldown from an rc to 1.2.13, both inside the declared ranges. The build and operator check cover that jump.
- Steps 6 and 9: all nine cited line numbers match. Step 9 keeps SameAsRequest for Development, consistent with launchSettings.json and the README.

### Open Questions
- None carried as low-confidence findings. S2-1's guarantee ("never below zero") could instead be relaxed in the plan text if the owner accepts the residual edge.

Reviewed: D:\src\fokus\docs\specs\F37-CodeQualityCleanup\delivery\plan.md

### Merge — round 1

Coverage: verdicts-1.md, verdicts-2.md and the finding text files (Sonnet NOT OPENED; Codex returned no ledger); SprintsView burnup consumer; CarryOverService call-site bodies (Sonnet NOT OPENED; Codex cited none)

| id | readers | level | anchor | title | shape | consequence | evidence | untouchable | tag | surfaces | source ids |
|---|---|---|---|---|---|---|---|---|---|---|---|
| R1-01 | codex | CRITICAL | unranked — no anchor fits; Codex severity CRITICAL | Step 7 `AsNoTracking` breaks relationship fix-up: endpoints take the target sprint from `GetAnalyticsSprintsAsync` and rely on the later membership query filling it | a safety edit that removes an implicit dependency | single-sprint carry-over, bug-ratio, leaderboard return empty/zero | quoted (architect re-verified: `GetCarryOverEndpoint.cs:46`, `GetBugRatioEndpoint.cs:51`) | silent-wrong-output | step-7 tracking | GetCarryOver, GetBugRatio, GetLeaderboard endpoints; SprintRepository | codex-1 |
| R1-02 | codex, sonnet | HIGH | seed-0116 | Settings load error rides the shared `store.error`, reset by detect and set by every save | shared flag for a single-purpose signal | defaults savable over real config; any failed save blanks tabs | quoted | data-loss | step-8 settings | settingsStore.ts, SettingsView.vue, tabs | codex-2, S8-1 |
| R1-03 | codex, sonnet | HIGH | seed-0117 | Zombie projection returns ids only; flag shows and sorts by sprint count | changed basis leaves a dependent figure undefined | wrong "N sprints" beside the flag | quoted | silent-wrong-output | step-3 zombie | SprintSummaryService.cs:828-841, SprintFlags.vue:43 | codex-3, S3-1 |
| R1-04 | codex | HIGH | unranked — no anchor fits; Codex severity HIGH | "Closed sprints plus selected" differs from the cited "all synced sprints" | definition drift from the cited source | ticket in 2 closed + 1 active sprint unflagged when viewing a closed sprint | quoted | — | step-3 zombie | F11 spec:193, KB, SyncSprintsEndpoint.cs:25 | codex-4 |
| R1-05 | codex | HIGH | unranked — no anchor fits; Codex severity HIGH | Step 5 leaves direct whole-list scans of the same defect | partial fix of a problem class | quadratic cost survives in progress, detail, scope change, summary | path | — | step-5 scope | DeveloperProgressService:327-330, DeveloperDetailService:347-350, ScopeChangeService:552,565,581,770, SprintSummaryService:702-706 | codex-5, S5-3 (part) |
| R1-06 | codex | HIGH | unranked — no anchor fits; Codex severity HIGH | Replacing case-insensitive dictionaries with the ordinal index changes other results | behaviour change hidden in a perf refactor | epic activity dates, removed-post-planning change for case-differing ids | quoted | silent-wrong-output | step-5 semantics | EpicProgressService:187-198, ScopeChangeService:320-328 | codex-6 |
| R1-07 | codex, sonnet | MEDIUM | seed-0130 | Index lifetime underspecified; per-call rebuild inside endpoint loops passes every check | acceptance cannot detect the failure | quadratic cost survives or worsens | path | — | step-5 accept | GetDeveloperThroughputEndpoint:88-93, ExcludedDeveloperFilter | codex-7, S5-2 |
| R1-08 | codex, sonnet | MEDIUM | seed-0130 | Bug-area exit can precede its inflow (completion before entry) | stated guarantee the rule does not enforce | area dips below zero | scenario | silent-wrong-output | step-2 rule | ScopeChangeService:540-591, 664-713 | codex-8, S2-1 |
| R1-09 | codex | MEDIUM | unranked — no anchor fits; Codex severity MEDIUM | Step 2 region/invariant coverage incomplete (lookup at 537-588; mixed fixture) | understated edit surface | feature lines unprotected in mixed scenarios | path | — | step-2 scope | ScopeChangeService:537-588 | codex-9, S2-1 (part) |
| R1-10 | codex, sonnet | MEDIUM | seed-0127 | Step 4 tests can bypass the endpoint truncation; positions ambiguous | test that cannot fail on the bug | regression unproven | path | — | step-4 test | DeveloperThroughputService:290-311 | codex-10, S4-1 |
| R1-11 | codex, sonnet | MEDIUM | seed-0127 | Dashboard: QA error has no render site; summary error never rendered | placement the template does not provide | failures still look like "no data" | quoted | — | step-8 dashboard | DashboardView.vue:108-121,155 | codex-11, S8-2 |
| R1-12 | codex, sonnet | MEDIUM | seed-0127 | Cookie environment plumbing spans Program → AddFokusServices → AddFokusAuth; Step 9 accept has a literal `…` | understated edit surface | missed file; non-executable accept | path | — | step-9 | Program.cs:12, DependencyInjection.cs:18-20 | codex-12, codex-14 (part), S9-1 |
| R1-13 | codex, sonnet | MEDIUM | seed-0130 | Regression net narrower than Steps 5–7; operator check omits pages and failure paths; Must-NOT-Change overclaims carry-over | safety claim the evidence does not support | refactored services ship unchecked | quoted | — | step-12 net | plan Must-NOT-Change, Step 12 | codex-13, S5-1 |
| R1-14 | codex, sonnet | LOW | seed-0127 | Call-site inventory wrong (45 in 12 files); slice why rests on it; `:702` guard semantics | figure off the live count | wrong scope | path | — | step-5 inventory | plan header, Step 5 | codex-14, S5-3 |
| R1-15 | sonnet | LOW | seed-0127 | Secret indicator stale after save; typed secret stays in form; constant location unnamed | unspecified post-action state | misleading placeholder | quoted | — | step-1 | settingsStore.ts:184-195, XrayTab.vue | S1-1 |
| R1-16 | sonnet | LOW | seed-0127 | TestData.Membership cannot express Step 2 cases; accept names a local | planned test without fixture support | fixture edits unplanned | path | — | step-2 fixtures | TestData.cs:72-88 | S2-2 |

### Fold — round 1 (architect, claude-opus-5-5, 2026-10-13)

| id | disposition | level after | re-grade (level · anchor · why) | where |
|---|---|---|---|---|
| R1-01 | fixed-real | CRITICAL | — | Step 7 (new fix-up audit + endpoint fix, accept), Step 12, Must-NOT-Change |
| R1-02 | fixed-real | HIGH | — | Step 8 Settings bullet + accept |
| R1-03 | fixed-real | HIGH | — | Step 3 projection (id → count) + TDD |
| R1-04 | fixed-real | HIGH | — | Step 3 definition; Decisions row; Step 11 KB bullet |
| R1-05 | fixed-real | HIGH | — | Step 5 inventory (direct scans added, predicates preserved) + accept |
| R1-06 | fixed-real | HIGH | — | Step 5 semantics clause; Decisions row |
| R1-07 | fixed-real | MEDIUM | — | Step 5 index-lifetime clause + accept |
| R1-08 | fixed-real | MEDIUM | — | Step 2 rule + TDD case |
| R1-09 | fixed-real | MEDIUM | — | Step 2 cited range + mixed fixture |
| R1-10 | fixed-real | MEDIUM | — | Step 4 TDD |
| R1-11 | fixed-real | MEDIUM | — | Step 8 Dashboard bullet; Decisions row (summary error in scope) |
| R1-12 | fixed-real | MEDIUM | — | Step 9 files + accept |
| R1-13 | fixed-real | MEDIUM | — | Step 12 pages + failure paths; Must-NOT-Change rows |
| R1-14 | fixed-real | LOW | — | header Build slices why; Step 5 inventory; Out of scope |
| R1-15 | fixed-real | LOW | — | Step 1 secret bullet + accept |
| R1-16 | fixed-real | LOW | — | Step 2 fixtures + accept |

Reconciled: Step 3 ↔ Decisions (zombie basis) ↔ Step 11 (KB wording) ↔ Must-NOT-Change (sparkline row); Step 5 ↔ Out of scope ↔ header slice line ↔ lessons.md Skill Gap (count); Step 7 ↔ Step 12 ↔ Must-NOT-Change (tracking + fix-up rows); Step 8 ↔ Step 12 (failure paths); Step 9 ↔ Must-NOT-Change (localhost sign-in).

## Round 2 — 2026-10-13 — plan — primary (codex)

Header note: Codex alone (round ≥ 2 carries no second reader), job `task-mv6n3163-zjv9l2`, completed inside its limit. Read: full (round 1's `Coverage:` line was not `complete`). Floor: MEDIUM. Reader section: `review-critic-codex.md` § Round 2. Verdict NO-GO. Fold verification: R1-01..R1-04, R1-06..R1-12, R1-14..R1-16 closed; R1-05 partial (scope contradiction, → R2-03); R1-13 partial (failure paths, → R2-05).

### Merge — round 2

Coverage: complete

| id | readers | level | anchor | title | shape | consequence | evidence | untouchable | tag | surfaces | source ids |
|---|---|---|---|---|---|---|---|---|---|---|---|
| R2-01 | codex | HIGH | unranked — no anchor fits; Codex severity HIGH | Step 3 restricted zombie candidates to the selected sprint, hiding window zombies shown today | a fold that changes an unrelated behaviour | flags disappear from the dashboard | quoted (architect re-verified `SprintSummaryService.cs:823-840`) | silent-wrong-output | fold | plan Step 3 | codex-r2-1 |
| R2-02 | codex | HIGH | unranked — no anchor fits; Codex severity HIGH | A failed excluded-statuses load leaves `[]` savable over the stored list | load failure gated only for `/api/settings` | configuration loss | quoted (`JiraTab.vue:60-65,144`, `SaveExcludedStatusesEndpoint.cs:27`) | data-loss | round-1 miss: Settings tab load/save | plan Step 8 | codex-r2-2 |
| R2-03 | codex | MEDIUM | unranked — no anchor fits; Codex severity MEDIUM | Step 5 migrates `BuildBurnupData` scans that Scope excludes | contradiction between sections | developer cannot follow both | quoted | — | fold | plan Scope, Step 5 | codex-r2-3 |
| R2-04 | codex | MEDIUM | unranked — no anchor fits; Codex severity MEDIUM | One shared `qaError` is erased by a sibling QA fetch | shared flag for several resources | a failed list shows "No untested tickets" | quoted (`QaMetricsSection.vue:23`) | — | fold | plan Step 8 | codex-r2-4 |
| R2-05 | codex | MEDIUM | unranked — no anchor fits; Codex severity MEDIUM | Operator check still omits capacity save, failed Re-detect, excluded-status load, production cookie | incomplete acceptance | fixed paths unverified | path | — | fold | plan Step 12 | codex-r2-5 |
| R2-06 | codex | MEDIUM | unranked — no anchor fits; Codex severity MEDIUM | Empty-data shortcuts return 200 before the not-found check in all 9 endpoints | an early return bypassing the fix | missing sprint still 200 on empty history | quoted (architect: `Count == 0` at lines 20–30 above the check in all 9) | — | round-1 miss: Step 6 early returns | plan Step 6 | codex-r2-6 |

### Fold — round 2 (architect, claude-opus-5-5, 2026-10-13)

| id | disposition | level after | re-grade (level · anchor · why) | where |
|---|---|---|---|---|
| R2-01 | fixed-real | HIGH | — | Step 3 (candidates = window tickets, count from dictionary) + TDD case; Decisions row; Step 11 KB bullet |
| R2-02 | fixed-real | HIGH | — | Step 8 Settings bullet (per-tab load-failed gating: excluded statuses, boundaries, users) |
| R2-03 | fixed-real | MEDIUM | — | Scope out-of-scope sentence (mechanical swap permitted) |
| R2-04 | fixed-real | MEDIUM | — | Step 8 Dashboard bullet (one error per QA resource) + accept |
| R2-05 | fixed-real | MEDIUM | — | Step 12 failure paths + production cookie item |
| R2-06 | fixed-real | MEDIUM | — | Step 6 order-of-checks bullet + accept |

Reconciled: Step 3 ↔ Decisions (two zombie rows) ↔ Step 11 ↔ Step 12 (zombie item); Step 8 ↔ Step 12 (failure-path items) ↔ Must-NOT-Change (none changed); Step 6 ↔ Step 12 (404 item); Scope ↔ Step 5 (BuildBurnupData). No round 3: round 2's merge holds no CRITICAL.

