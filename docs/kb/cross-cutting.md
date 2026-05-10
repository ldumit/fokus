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

## Feature-Only Metrics (FeatureOnlyMetrics)

Several analytics surfaces show feature-only values — bugs are excluded from the computation. The filtering rule is `!IsBug(m)` where `IsBug(m) => m.Ticket?.IssueType == "Bug"`.

### Feature-only surfaces

| Surface | Metrics | Where |
|---------|---------|-------|
| Dashboard SP Completed card | Value, delta, sparkline | SprintSummaryService |
| Dashboard Completion % card | Value (featureCompleted / featureCommitted), delta, sparkline | SprintSummaryService |
| Dashboard Health Score — Completion sub-score | Uses feature-only Completion % | SprintSummaryService |
| Burnup chart scope line (orange) | totalScopeSp, totalScopeTickets | ScopeChangeService |
| Burnup chart completed line (green) | completedSp, completedTickets | ScopeChangeService |
| Developer Throughput table | spAssigned, spCompleted, completionPercent, ticketsDone, ticketsCarriedOver, rolling average, all deltas | DeveloperThroughputService |

### Remain total-scope (include bugs)

- Scope Disruption Rate (denominator is total committed, not feature committed)
- Bug Disruption Rate (same denominator)
- Carry-Over Rate (all uncommitted + committed in numerator and denominator)
- Burnup chart bug SP area (red) — tracks remaining bug work
- Sprints page multi-sprint scope/completion fields
- Dashboard SP Completed annotation `(+X bug SP)` — a separate field `bugSpCompleted` on MetricsResult

### Two-committed-SP pattern

`ComputeMetrics` in SprintSummaryService maintains two committed aggregates:
- `committed` (total, including bugs) — used as denominator for disruption rates and carry-over
- `featureCommitted` (bugs excluded) — used as denominator for completion rate

`SprintMetrics` record carries both `FeatureCompleted` and `BugSpCompleted` for downstream use.

## Division by Zero

All analytics produce 0 (not null, not error) when the denominator is 0.
