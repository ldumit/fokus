# Cross-Cutting Rules

Shared business logic that applies across multiple analytics features.

## ExcludedFromScopeStatuses

Tickets whose FinalStatus matches an excluded status are removed from SP calculations. Behavior varies by service:

| Service | SP Excluded | Ticket Count Excluded | Visibility |
|---------|------------|----------------------|------------|
| ScopeChange | Yes (from committedSpActive, addedSp) | No | Visible in event table, marked "excluded" |
| CarryOver | Yes | No (carryOverTicketCount includes them) | Invisible in status distribution |
| BugRatio | Yes (excluded from completed filter entirely) | Yes | Not visible |
| Throughput | NOT APPLIED (pre-dates F10) | N/A | N/A |

Matching is case-insensitive. Configured in Settings, managed via `GET/PUT /settings/excluded-statuses`.

## Sub-Team Filter (C2)

All analytics endpoints accept optional `subTeam` parameter:
- Memberships filtered to tickets where `Ticket.Assignee.SubTeam == subTeam`
- Developer lists filtered to `Developer.SubTeam == subTeam`
- Epic progress filters tickets by assignee sub-team

Null/empty subTeam = no filter (all developers).

## Developer Exclusion

All analytics services accept `excludedDeveloperIds` (HashSet). Tickets with AssigneeId in this set are filtered out. Tickets with null AssigneeId are NOT filtered — they remain in team totals.

See [Developer & Capacity](domain/developer.md) for the capacity-based exclusion rule.

## Delta Pattern (C1)

Single-sprint mode: each metric card shows delta vs prior sprint (closed sprint with next-earlier start date). Multi-sprint mode: no deltas — trend charts serve this purpose.

Delta includes: value, direction (up/down/neutral), polarity (whether up is good or bad).

Polarity conventions:
- Higher is better: completion %, SP completed, non-bug SP, throughput -> green up, red down
- Lower is better: disruption %, carry-over %, bug ratio %, bug SP -> green down, red up
- Neutral: SP assigned -> no color

## Sprint Selection Convention

All multi-view endpoints: `sprintId` (single) vs `last` (range), mutually exclusive. Default: `last=5`. Frontend convention: `last=0` means "all sprints".

## DefaultSpPerBug (Effective SP Fallback)

`AppSettings.DefaultSpPerBug` (default 3, range 0–13) is a system-wide fallback for unestimated Bug tickets.

**Rule:** `GetEffectiveSp(defaultSpPerBug)` on `SprintMembership` returns:
1. `StoryPoints` if non-null and > 0
2. `(decimal)defaultSpPerBug` if `Ticket.IssueType == "Bug"` and `defaultSpPerBug > 0`
3. `null`

For `Ticket` entities (EpicProgress), a local `GetEffectiveTicketSp(ticket, defaultSpPerBug)` applies the same logic using `ticket.IssueType`.

Applied in: SprintSummary, ScopeChange, CarryOver, BugRatio, Throughput, EpicProgress.

Setting `DefaultSpPerBug = 0` disables the fallback entirely — unestimated bugs contribute no SP.

## Feature-Only Metrics (FeatureOnlyMetrics + TransitionBasedSprintScope)

All sprint scope surfaces are feature-only — bugs are excluded from the computation. The filtering rule is `!IsBug(m)` where `IsBug(m) => m.Ticket?.IssueType == "Bug"`.

### Feature-only surfaces (all scope metrics)

| Surface | Metrics | Where |
|---------|---------|-------|
| Dashboard Active SP card | activeSp (transition-based), delta, sparkline | SprintSummaryService |
| Dashboard Completion % card | completedSp / activeSp, delta, sparkline | SprintSummaryService |
| Dashboard Health Score — Completion sub-score | Uses feature-only Completion % | SprintSummaryService |
| Dashboard Carry-Over Rate | carryOverSp / activeSp | SprintSummaryService |
| Dashboard Scope Disruption Rate | addedSp / activeSp | SprintSummaryService |
| Burnup chart scope line (orange) | cumulative feature tickets transitioned to startStage | ScopeChangeService |
| Burnup chart completed line (green) | cumulative feature tickets transitioned to endStage | ScopeChangeService |
| Multi-sprint bar chart | activeSp, completedSp (feature-only) + BugSpCompleted (separate bug bars) | ScopeChangeService |
| Carry-Over tracker | carryOverSp, carryOverRate, totalScopeSp | CarryOverService |
| Developer Throughput table | spAssigned, spCompleted, completionPercent, ticketsDone, ticketsCarriedOver, rolling average | DeveloperThroughputService |

### Remain total-scope or separate (include bugs)

- Burnup chart bug SP area (red) — tracks remaining bug work separately
- `BugSpCompleted` field on `ScopeChangePerSprintData` — separate bug bars in multi-sprint bar chart
- Dashboard SP Completed annotation `(+X bug SP)` — a separate field `BugSpCompleted` on SprintMetrics

## Transition-Based Sprint Scope (TransitionBasedSprintScope)

All sprint scope attribution uses transition timestamps, not snapshot fields (`WasCommitted`, `FinalStatus`).

**Utility:** `TransitionAttributionChecker` (`Fokus.API/Features/Analytics/TransitionAttributionChecker.cs`)

**Attribution rules:**
- A ticket is **started** in the sprint where its first qualifying transition to CycleTimeStartStage (or beyond) occurred within [sprintStart, sprintEnd].
- A ticket is **completed** in the sprint where its first qualifying transition to CycleTimeEndStage (or beyond) occurred within [sprintStart, sprintEnd].
- Each transition timestamp falls in exactly one sprint — no double-counting.

**Ordered stage sequence:** `WorkflowStages ++ DoneStatuses` (same construction as CompletionChecker).

**Start stage fallback (spec BR19):** `CycleTimeStartStage ?? orderedStages[0]` (first stage — scope attribution captures all sprint engagement including queue entry; wider than cycle time measurement which defaults to second stage).

**End stage fallback:** `CycleTimeEndStage ?? DoneStatuses[0]`. Returns -1 if no done statuses configured (nothing completes).

**Key methods:**
- `ResolveStartIndex(settings)` → `(orderedStages, startIndex)`
- `ResolveEndIndex(settings, orderedStages)` → `endIndex` (−1 = nothing completes)
- `IsStartedInSprint(ticketId, transitions, sprintStart, sprintEnd, orderedStages, startIndex)` → `(bool, DateTime?)`
- `IsCompletedInSprint(ticketId, transitions, sprintStart, sprintEnd, orderedStages, endIndex)` → `(bool, DateTime?)`
- `IsAddedInSprint(membership, isStarted, planningCutoff)` → `bool` — AddedAt > planningCutoff AND isStarted
- `IsCarryOver(isStarted, isCompleted)` → `bool` — started AND NOT completed

**Transition loading:** Endpoints load transitions via `TicketRepository.GetStatusTransitionsForSprintTicketsAsync(sprintIds)` — single bulk query joining SprintMemberships to StatusTransitions.

**Used by:** All 8 analytics services, ExcludedDeveloperFilter.

**Carry-over completions:** A ticket started in a prior sprint and completed in the current sprint adds to `completedSp` but NOT `activeSp` for the current sprint. This is why completion % can exceed 100% (spec BR14).

## Boundary-Driven Completion (BoundaryDrivenCompletion)

**Retained for non-sprint-scope uses only.** For sprint scope attribution, use `TransitionAttributionChecker` instead.

**Utility:** `CompletionChecker` (`Fokus.API/Features/Analytics/CompletionChecker.cs`)

**Answers:** "Is this status a completed status?" (position-based, snapshot check on CurrentStatus or FinalStatus).

**Still used by:**
- EpicProgress progress tracking — `CurrentStatus IN completedStatuses` (current state, not sprint attribution)
- ExcludedFromScopeStatuses filtering — `FinalStatus` at sprint end (spec BR15)

**NOT used by:** Sprint scope attribution in any service. All services use `TransitionAttributionChecker` for started/completed checks.

## Planning-Gated Disruption (PlanningGatedDisruption)

All disruption metrics are now gated by `planningCutoff = sprint.StartDate.AddDays(planningWindowDays)`:

- **Dashboard Scope Disruption Rate** (`SprintSummaryService.ComputeScopeDisruptionRate`): uses `planningCutoff` as the addition threshold — only post-planning cycle-entered feature additions count.
- **Dashboard Bug Disruption Rate** (`SprintSummaryService.ComputeBugDisruptionRate`): same — uses `planningCutoff` for bug additions.
- **Dashboard `ComputeMetrics`**: `IsAddedInSprint` receives `planningCutoff` (not `sprintStart`).
- **Mid-sprint disruption flag** (`SprintSummaryService.ComputeFlags`): uses `planningWindowDays` instead of hardcoded 2 days for `disruptionCutoff`.
- **`IsAddedInSprint`** (`TransitionAttributionChecker`): parameter renamed from `sprintStart` to `planningCutoff` — callers pass `sprint.StartDate.AddDays(planningWindowDays)`.
- **`IsRemovedPostPlanning`** (`TransitionAttributionChecker`): new method — checks `RemovedAt > planningCutoff` AND qualifying start transition in `[sprintStart, RemovedAt]`.

`planningWindowDays` is configured in `AppSettings` (Settings page). Default is 2.

## Division by Zero

All analytics produce 0 (not null, not error) when the denominator is 0.
