# F37-CodeQualityCleanup — Second Reader Review

## Step 2 — Code Review (second reader)

Second reader, round: first review. Codex is the primary reader this round; this file is the Sonnet second reader's copy of the Step-2 review. The primary's section lives in `review.md` and was not read or edited here.

## Reviewed By
reviewer (second reader, read-only, single-pass three-axis checklist: simplicity/DRY, bugs/correctness, conventions)
**Model:** claude-sonnet-5-5

## Verdict: APPROVED

No CRITICAL or HIGH finding. Two LOW findings and three Open Questions; no MEDIUM finding survived the evidence check.

## Pre-commitment Predictions
| Predicted | Found |
|-----------|-------|
| Step 2 span edge cases (bug removed after sprint end, completed before it was added) | Held. Half-open `[entry, exit)` makes a negative area impossible by construction; `m.RemovedAt <= StartDate` guard is functionally redundant (exit is floored at entry), harmless. Slice 5 test is non-vacuous (ignoring completion makes it red). |
| Step 5 removal of the case-insensitive dictionaries silently changes a result | Held. Old flow was dictionary (ignore case) then checker re-filter with ordinal `==`; the ordinal index selects the same transitions. The one plan-listed "keep" item the developer removed (`SprintSummaryService.ComputeMetrics` grouping) fed only the checker, so the removal is result-neutral (see Plan conformance notes). |
| Step 7 `AsNoTracking` on a method a write path or a later navigation read still depends on | Held. Every caller of the 22 methods only reads; the three fix-up users (CarryOver, BugRatio, Leaderboard) were fixed; other navigation reads (`m.Ticket`, `m.Ticket.Assignee`, `sm.Sprint`, `link.Ticket`, `te.Links`/`TestRuns`) are all covered by the owning method's own `Include`. No reference-identity reliance on `Ticket`/`Developer`/`Sprint` instances (grep for `ReferenceEquals`, entity `==`, entity-keyed `GroupBy`/`HashSet`: none). |
| Step 3 SQL grouping versus the ordinal window grouping | Held with a note: the dictionary is `OrdinalIgnoreCase` while window candidates group ordinally, so two window tickets differing only in case would both hit one dictionary entry. The developer recorded this as an approved analyze choice; ticket keys are Jira keys, so it is theoretical. |
| Step 8 error gating gaps | One cosmetic gap (Open Question 3). Gating of Jira excluded statuses, workflow boundaries, Users and Settings load verified against the plan. |

## Findings

### [LOW] `SelectRollingWindowSprintIds` returns `List<int>`, the convention asks for `IReadOnlyList<int>`
**File:** `src/Services/Fokus/Fokus.API/Features/Analytics/DeveloperThroughputService.cs:192`
**Origin:** implementation
**Issue:** `docs/conventions/csharp.md` § Return Types: public methods returning a materialized collection return `IReadOnlyList<T>`. The only consumer (`GetDeveloperThroughputEndpoint.cs`, `extraSprintIds.Concat(targetSprintIds).Distinct()`) and the three tests work unchanged with the read-only type.
**Fix:** Change the return type to `IReadOnlyList<int>`.
**Confidence:** 85/100

### [LOW] `ComputeSprintMetrics` resolves `ticketTransitions` for every membership though only the removed branch reads it
**File:** `src/Services/Fokus/Fokus.API/Features/Analytics/ScopeChangeService.cs:324`
**Origin:** implementation
**Issue:** Before Step 5 the per-ticket list also fed the two checker calls. Those now read `transitionIndex`, so `ticketTransitions` is consumed only inside the `m.RemovedAt != null` block (`IsRemovedPostPlanning`, lines 326-334) but is still looked up for every membership first. Result-neutral. This is the "leftover after a mid-step refactor" class in `docs/conventions/known-deviations.md`.
**Fix:** Move the `GetValueOrDefault` lookup into the removed-membership branch.
**Confidence:** 85/100

## Positive Observations
- The Step 5 refactor is verifiably mechanical. Re-executed independently: 47 `TransitionAttributionChecker.Is(Started|Completed)InSprint` call sites under `src` = 45 production (the plan's inventory) + 2 test; `TicketId == ` in `Features/Analytics` has no match; index construction sites are all public-entry or endpoint-level, none inside a loop, lambda or per-sprint helper (checked by listing every `StatusTransitionIndex.Build`).
- Diff inspection of the moved scans (feature start/end lookups in `BuildBurnupData`, `BuildStalledTickets`, `ComputeTopEpics`, `ComputeBugTimeInProgress`, `CycleTimeService.ComputeTicketCycleTime`) shows predicates, ordering and the `endIndex >= 0` / absent `endIndex < 0` guards preserved exactly; `ComputeTopEpics` keeps its no-guard shape as the plan required.
- TDD discipline is evidenced and holds up under reading: the mixed-fixture burnup pin guards the feature and committed-total series; the throughput test pair documents the regression (old two-sprint window gives null); the checker test pins ordinal case sensitivity (slice 6), so an `OrdinalIgnoreCase` index would go red; zombie tests kill the three obvious mutants (window-based count, missing history filter, dropped ordering).
- Step 7a is a real audit, not a restatement: all 17 plan-time endpoints have a verdict, the three fix-up reliers are fixed, and I re-traced GetQaMetrics, GetEpicProgress, GetFeatureTicketsWithCoverageAsync (link query is a projection, no `link.Ticket` read) and TE link reads against the verdicts; all hold.
- The Q1 owner ruling is implemented precisely: a Closed target is taken from the membership-loaded list, an Active target keeps the lightweight entity, and `Sprint.Memberships` is a non-null empty list (`Sprint.cs:14`), so there is no new null path.
- Write paths are untouched (`AppSettingsRepository` 0 `AsNoTracking`, no `QueryTrackingBehavior` anywhere, `GetByIdAsync`/`Upsert*`/`Replace*`/`Update*` untouched); the sign-in handler's `GetByGoogleIdAsync` stays tracked, so `RecordLogin` still persists.
- KB entries are edited in place, grew rather than shrank, and `docs/kb/index.md` is unchanged.

## Plan conformance notes
- Step 5: the plan's Scope kept three case-insensitive dictionaries, one being the `SprintSummaryService.ComputeMetrics` grouping. The developer removed it because, after the signature change, it fed only the checker. Verdict: plan stale, code right. The old path was group (ignore case) then ordinal re-filter inside the checker; the new path is the ordinal index; results identical. `implementation.md` Step 5 says "Deviations: None" while the Decisions line records this departure, so the done check should score it as a recorded deviation.
- Step 6: `GetDeveloperQuality` and `GetQaWorkload` keep their Xray-disabled 200 ahead of the unknown-sprint check (recorded in Step 6 Decisions). The plan's accept (id check precedes the empty-data shortcut) is met. See Open Question 1.
- Step 7 accept grep `analyticsSprints.FirstOrDefault(s => s.Id == req.SprintId.Value)` still matches once each in CarryOver, BugRatio, Leaderboard: it is the Step 6 existence check and the Active-target entity; accepted by Q1.

## Gaps
- `SprintRepository.GetZombieSprintCountsAsync` (`SprintRepository.cs:50`) has no kept test: upper-casing group key, `COUNT(DISTINCT SprintId)`, HAVING filter and the Closed + Active predicate were verified once by a deleted throwaway test. `Fokus.Tests` has no database harness at all (no Sqlite or DbContext reference), and the plan accepts that (Testing Strategy, "no endpoint harness; adding one is out of scope"), so this is not filed as a finding. A translation failure would surface as a 500 on the Dashboard summary, the primary page, so it is worth the architect's follow-up decision (Open Question 2).
- Front-end steps 1 and 8, the `AsNoTracking` + `Include` behaviour on the field-backed `Sprint.Memberships`, the Step 7 single-sprint loads and the Step 9 Production `Secure` flag are verified only by build, grep and static reading. Step 12 (operator) carries them and is not yet recorded: `implementation.md` has no `§ Operator check`. The close may proceed; the merge waits (plan).
- No test exercises the endpoint wiring of `SelectRollingWindowSprintIds` (grep accept only). The helper itself is tested through `ComputeThroughput`.
- The backlog row recording the Active-sprint gap (Q1 consequence) is not yet in `docs/backlog.md`; the ruling places it at close.

## Open Questions
1. **Xray-disabled versus unknown sprint id (confidence 55).** `GetDeveloperQuality` (`GetDeveloperQualityEndpoint.cs:27`) and `GetQaWorkload` (`GetQaWorkloadEndpoint.cs:19`) return 200 for a non-existent `SprintId` when Xray is disabled, while `GetQaMetrics` (`GetQaMetricsEndpoint.cs:25`, outside the 9) checks the sprint first and returns 404 in that state. The plan sentence "an explicit SprintId that does not exist must return 404 whether or not any sprints exist" does not mention the feature-off shortcut. Developer reading (feature-off response, not the empty-data shortcut) is defensible; architect to confirm intended precedence.
2. **Kept SQLite test for the zombie projection (confidence 60).** Whether to open a follow-up to add a minimal in-memory SQLite fixture and one test for `GetZombieSprintCountsAsync` (the architect's own lesson in `lessons.md` argues for it). Not a finding because the plan scoped test infrastructure out.
3. **Users tab load-failure message can be cleared while its lock stays (confidence 60).** `UsersTab.vue` `changeUserRole` / `toggleUserStatus` reset `userMgmtError` at start. If only the invitations load failed, those actions stay enabled, clear the "failed to load invitations" message, and leave Revoke disabled with no visible reason. Cosmetic; the plan asked only that a rejected result be reported and the affected list's actions disabled.

## Carry-Over Findings (from implementation.md)
| Title | Disposition | Evidence |
|-------|-------------|----------|
| Active sprint as single target on Carry-Over / Bug Ratio / Leaderboard computes over no memberships | **Confirmed pre-existing and unchanged; ruling honoured.** At 3d1722a the target came from `GetAnalyticsSprintsAsync` and memberships were filled only for the closed ids passed to `GetSprintsWithMembershipsAsync`, so an Active target never had any. After F37 a Closed target takes the membership-loaded entity (`GetCarryOverEndpoint.cs:54`, `GetBugRatioEndpoint.cs:59`, `GetLeaderboardEndpoint.cs:66`), an Active one keeps the lightweight entity with an empty list, same output as before. Backlog row owed at close. | files above; `Sprint.cs:14` |
| `graphify update .` not run | **Confirmed outstanding, non-blocking.** CLAUDE.md requires it after code changes; `graphify-out/` is outside the developer's file set, so it falls to the main session before or at close. | CLAUDE.md § graphify |
| Step 12 operator page check outstanding | **Confirmed outstanding.** No `§ Operator check` in `implementation.md`; the front-end secret/error behaviour, the Step 7 single-sprint loads and the Production cookie are only operator-verified. Merge waits. | plan Step 12 |

## Evidence
| Check | Result | Command | Output |
|-------|--------|---------|--------|
| Run record | pass | brief's `Run record:` line (`dotnet build` + `dotnet test src/Fokus.slnx` + `npm run build --prefix client`, unfiltered, so all four new test classes and the 19 existing facts are included) | verify-run complete · pass · 2026-10-13T18:58Z · session 80d534fb-6ce3-4fdf-b940-67f3bc8a5ce1. Not re-run, per the brief. |
| Plan accept greps re-executed (no test run) | pass | `grep` over the tree | AppSettingsRepository `AsNoTracking` 0; `QueryTrackingBehavior` 0; `TakeLast(2)` in throughput endpoint 0; `hardcoded 2 days` in health-score.md 0; `catch {` in dashboardStore.ts 0; `silently ignore` in JiraTab.vue 0; `CookieSecurePolicy.Always` 1 and `SameAsRequest` 1 (Development arm); `AddError(r => r.SprintId, "Sprint not found.")` files 0; `throw new NotFoundException` files under Analytics 9; `allMembershipsFlat` 0; `"****"` in `Fokus.API` 1 (`XraySecretMask.cs`); `docs/kb/index.md` unchanged; `DeveloperProgressServiceTests.cs` unmodified |
| Platform-conditional grep | pass | grep for `process.platform`, `RuntimeInformation`, `OSVersion`, `DirectorySeparator`, `IsOSPlatform` over the added lines | no hit; no branch to force |
| Mutation battery | not run | not triggered: the record cites no mutation sweep as a sanction | the new tests were assessed by reading which mutants each kills (see Positive Observations) |
| Plan-mandated tests under a filter that excludes them | n/a | the complete record is unfiltered; no `Integration`-tagged test is mandated | no exclusion hazard |

*Status: COMPLETE — reviewer (second reader), 2026-10-13*
