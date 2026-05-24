# Developer Detail

Individual developer analytics page. Composes data from Leaderboard (bug/feature SP split), DailyProgress (burnup, pace, stalls), and Throughput (rolling average) patterns into a single per-developer view.

**Key file:** `Fokus.API/Features/Analytics/DeveloperDetailService.cs`
**Endpoint:** `GET /api/analytics/developer-detail/{accountId}?last={n}`

## Scope Model

Follows the Leaderboard scope model (not Throughput's feature-only model):
- **Completed filter:** `TransitionAttributionChecker.IsCompletedInSprint` + exclude removed memberships + exclude `ExcludedFromScopeStatuses` — same as `LeaderboardService.GetTransitionCompletedMemberships`
- **AssignedSp:** all non-removed memberships, all ticket types (features + bugs) — `m.RemovedAt == null`, `GetEffectiveSp(defaultSpPerBug)`
- **CompletionPercent:** all-types (diverges from Throughput's feature-only by design)
- **Bug classification:** `IssueType == "Bug"` (exact, case-sensitive)

## Sprint Inclusion Rule

A sprint is included in trends if:
1. Developer has at least one non-removed membership in that sprint
2. NOT (capacity == 0 AND totalSp == 0)

Sprint range: `last=N` returns last N qualifying sprints (chronological). `last=0` = all.

## Rolling Average

3-sprint rolling average of **all-types SP** (not feature-only).
- Null for first 2 sprints in the series
- Skips sprints with 0% capacity when looking back (capacity-aware skip, same pattern as `DeveloperThroughputService`)

## Work Allocation Summary

- `averageBugPercent`: mean of `bugPercent` across displayed sprints, excluding sprints with `totalSp == 0`
- `sprintsAboveTarget`: count where `bugPercent > bugRatioTarget` (from `AppSettings.BugRatioTarget`)
- `totalSprints`: count of displayed sprints

## Ticket State Derivation (Current Sprint)

| State | Condition |
|-------|-----------|
| done | `IsCompletedInSprint` returns true |
| stalled | `IsStartedInSprint` + not completed + business days in current status > 2 |
| in-progress | `IsStartedInSprint` + not completed + not stalled |
| not-started | `IsStartedInSprint` returns false |

**DaysInCurrentStatus:** business days (Mon-Fri) since last transition. For tickets with no transitions: count from sprint start date.

## Ticket Ordering

1. Stalled (most days desc)
2. In-progress
3. Not-started
4. Done

Within each group: story points descending (largest first).

## 404 Cases

- Developer not found or `isActive == false`
- Developer is excluded: 0% capacity AND 0 completed SP across all sprints

## Relationship to Other Analytics

| Analytics | Relationship |
|-----------|-------------|
| Leaderboard | Same completed filter, same bug/feature classification |
| DailyProgress | Same pace/burnup computation, same stall detection and business day counting |
| Throughput | Rolling average pattern (capacity-aware skip) but over all-types SP, not feature-only |

## Settings

- `AppSettings.BugRatioTarget` (decimal, default 30) — reference line for work allocation chart
- Saved via `PUT /api/settings/analytics-targets`
- Exposed in `GET /api/settings` response as `bugRatioTarget`
