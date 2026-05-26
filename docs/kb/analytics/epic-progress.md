# Epic Progress

Dual tracking (ticket count + story points) with velocity and projected completion.

**Key file:** `Fokus.API/Features/Analytics/EpicProgressService.cs`
**Endpoint:** `GET /analytics/epic-progress?subTeam`
**Store:** `client/src/stores/epicsStore.ts`
**View:** `client/src/views/EpicsView.vue`

## Per-Epic Metrics

All SP sums use `GetEffectiveTicketSp(ticket, defaultSpPerBug)` (local helper on `EpicProgressService`): returns `StoryPoints` if non-null and > 0, else `defaultSpPerBug` if IssueType == "Bug" and default > 0, else null. Velocity uses `SprintMembership.GetEffectiveSp(defaultSpPerBug)`. Configured via `AppSettings.DefaultSpPerBug` (default 3).

```
doneTickets          = tickets where CurrentStatus IN completedStatuses  (boundary-driven, see cross-cutting.md)
ticketCompletionPct  = doneCount / totalCount * 100
doneSp               = sum(effectiveSP) for done tickets
remainingSp          = sum(effectiveSP) for remaining tickets
```

**Uses Ticket.CurrentStatus, NOT FinalStatus.** Epic progress reflects current reality, not historical sprint snapshots.

An "unestimated" ticket is one where `GetEffectiveTicketSp` returns null (no own SP, and either not a Bug or DefaultSpPerBug is 0).

**F35 additions:** `startedDate` (DateOnly?, nullable) and `lastWorkDate` (DateOnly?, nullable) are computed per-epic from transition data. See "## Activity Dates (F35)" for derivation. These are computed fields — not stored in the DB.

## Imputed Story Points

Unestimated tickets receive imputed SP = average effective SP of estimated tickets in the same epic. Only applied to REMAINING unestimated tickets.

```
avgSp           = average SP of estimated tickets in this epic
imputedSp       = avgSp * remainingUnestimatedCount
adjustedTotalSp = totalSp + imputedSp
spCompletionPct = doneSp / adjustedTotalSp * 100
```

Imputed SP are never used in velocity calculations. When an epic has zero estimated tickets, SP metrics are unavailable — ticket-count-only.

## Dual Mode: Progress vs Velocity (since TransitionBasedSprintScope)

**Progress tracking** (doneTickets, doneSp, spCompletionPct, isCompleted): Uses `CurrentStatus IN completedStatuses` (position-based, boundary-driven — see cross-cutting.md). Reflects current state of tickets, not sprint attribution.

**Velocity tracking**: Uses `TransitionAttributionChecker.IsCompletedInSprint` — transition-based. The sprint where the CycleTimeEndStage transition occurred gets velocity credit.

## Velocity (rolling 3-sprint, transition-based)

- Groups closed sprint memberships for epic by sprint
- Sums completed SP per sprint using transition-based completion (isCompletedInSprint per sprint's date range)
- Excludes sprints with zero completed SP
- Takes up to 3 most recent sprints by StartDate desc
- Velocity = average completed SP across those sprints

## Projection

```
remainingWork             = remainingSp + imputedSp
projectedSprintsRemaining = remainingWork / velocity
```

Confidence = "low" if fewer than 3 data points; null otherwise.

## Activity Dates (F35)

Two computed date fields added to `EpicProgressEntry`: `startedDate` and `lastWorkDate`.

**Derivation rule:** For each epic, iterate all status transitions across its tickets (from `transitionsByTicket` dictionary in `EpicProgressService`). A transition qualifies if `TransitionAttributionChecker.GetStageIndex(transition.ToStatus, orderedStages) >= startIndex` — same cycle time start boundary used by velocity.

- `startedDate` = earliest qualifying `transition.Timestamp` as `DateOnly`. Null if no qualifying transitions exist.
- `lastWorkDate` = most recent qualifying `transition.Timestamp` as `DateOnly`. Null if no qualifying transitions exist.

**Transition data scope:** Uses only transitions for tickets in the current epic group (after sub-team filtering). Epic tickets that have never appeared in any analytics sprint have no transitions in the collection — their contributions are excluded.

**Sub-team filter:** Date derivation operates on `filteredEpicTickets` — same scope as all other per-epic metrics. Only transitions belonging to filtered tickets contribute.

**Null conditions:** Both fields are null when no ticket in the epic has a qualifying transition past the cycle time start boundary.

**API:** Both fields are serialized as ISO 8601 date strings (`startedDate`, `lastWorkDate`) in `GET /analytics/epic-progress`. The deprecated `activeSprintCount` field is retained in the response for backward compatibility but is no longer displayed in the UI.

## Sort and Display (updated F35)

- **Default sort (F35):** `lastWorkDate` descending — most recently active epics first. Epics with null `lastWorkDate` sort last.
- Active epics: server returns sorted by SP completion % ascending (service-side); client re-sorts by the active `sortColumn` / `sortDirection` from the store.
- Completed epics: server returns sorted by name ascending; client re-sorts per store sort state.
- Epic is complete when ALL tickets have done status
- Summary average completion: weighted by adjustedTotalSp
- **Sortable columns (BR9):** Epic name (alpha), Progress (SP completion % or ticket %), Tickets (done count), Velocity, Projected sprints, Started date, Last Work date, Coverage %, Pass Rate %, Bugs Found. "SP Done / Total" is NOT sortable.
- **Null-last sort (BR8):** All nullable columns sort nulls last regardless of ascending/descending direction.

## Special

- No sprint selector — epic progress is always cross-sprint
- Unlinked work: tickets with no EpicKey counted separately
- Client-side filtering: active vs completed toggle uses `isCompleted` flag

## QA Metrics (F31 — requires Xray enabled)

**Endpoint behavior:** When `settings.XrayEnabled`, the endpoint loads all TE links and runs for epic ticket keys via `TestExecutionRepository.GetTestExecutionDataForTicketsAsync`, passes as `EpicQaData` to service. When disabled, `qaData=null` — no DB round-trip, all QA fields null/0, `hasQaData=false`.

**Feature ticket scope (BR1):** Denominator = tickets where `IssueType != "Bug"`. Bug tickets are never test targets; QA fields are null for bug tickets in ticket detail.

**Coverage Rate (BR2):** `covered feature tickets / total feature tickets * 100`. Covered = has at least one non-cancelled TE via Tests link. Sub-tasks inherit parent ticket's TE links when they have none of their own (BR12).

**Pass Rate (BR3):** `PASS runs / (PASS + FAIL) runs` across all non-cancelled TEs linked via Tests to the epic's feature tickets. TODO/EXECUTING/ABORTED excluded from both numerator and denominator. Returns 0% when no executed runs.

**Bugs Found (BR4):** Count of unique bug ticket keys in `BlocksLinksByTicket` where at least one blocking TE ID is in the epic's feature tickets' TE set.

**Per-ticket Test Status (BR5/BR8):** Failed (any FAIL run) > Passed (all PASS, no TODO/EXECUTING) > InProgress (TODO or EXECUTING, no FAIL) > NoTests (no TEs).

**Summary card (BR16):** Arithmetic mean of non-null `CoverageRate` values across visible (filtered) epics. Null-coverage epics (zero feature tickets) excluded from mean.

**RAG coloring:** Uses `HealthScoreCalculator.MetricRag` with `settings.QaHealthThresholds.CoverageGreen/Amber` and `PassRateGreen/Amber`.

**Key files:**
- Repository method: `Fokus.Persistence/Repositories/TestExecutionRepository.cs` — `GetTestExecutionDataForTicketsAsync`
- Service QA helpers: `Fokus.API/Features/Analytics/EpicProgressService.cs` — `ComputeEpicQaMetrics`, `BuildTicketEntry`
- Store: `client/src/stores/epicsStore.ts` — `isXrayEnabled`, `averageTestCoverage` computed

## Client-Side Interactions (F35)

**Search:** Client-side substring match against `epicName` and `epicKey` (case-insensitive). Implemented in `searchFilteredEpics` computed in `epicsStore`. Operates on `filteredEpics` (already filtered by active/completed toggle). No API changes — search never triggers a server re-fetch.

**Summary card recalculation on search (BR5):** When search narrows the visible set, `searchSummaryMetrics` computed recalculates:
- Active Epics count — count of search-filtered epics matching the active/completed tab
- Average Completion — weighted by `adjustedTotalSp` (falls back to ticket % average when no SP data)
- Average Test Coverage — arithmetic mean of non-null `coverageRate` values in the filtered set
- Unlinked Work — does NOT recalculate on search (no epic name/key to match against; stays from full API response)

**Sort persistence (localStorage key: `fokus-epics-sort`):** JSON `{column, direction}`. Default: `lastWorkDate` desc. Hydrated at store init. BR11: if stored sort column is in `hiddenColumns` on load, reset to default.

**Column visibility persistence (localStorage key: `fokus-epics-columns`):** JSON array of hidden column IDs. Default: all columns visible. Epic name column is always visible (not in the toggle list). QA columns gated by Xray: not shown in toggle dropdown when `hasQaData=false`, hidden regardless of preference.

**Sticky column (BR15/BR16):** Epic name `<th>`/`<td>` and the leading chevron cell use `position: sticky; left: 0` with explicit background color. A right-side box-shadow on the epic name cell indicates the scroll boundary.

**Column IDs used in store and toggle:**
`progress`, `spDoneTotal`, `tickets`, `velocity`, `projected`, `startedDate`, `lastWorkDate`, `coverageRate`, `passRate`, `bugsFound`
