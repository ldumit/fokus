# Scope Change & Disruption

Measures what changed during a sprint — additions, removals, and disruption classification.

**Key file:** `Fokus.API/Features/Analytics/ScopeChangeService.cs`
**Endpoint:** `GET /analytics/scope-change?sprintId&last&subTeam`
**Store:** `client/src/stores/sprintsStore.ts`
**View:** `client/src/views/SprintsView.vue`

## Core Formulas

```
committedSpActive = sum(SP) where WasCommitted AND !Removed AND NOT IN excludedStatuses
committedSpTotal  = sum(SP) where WasCommitted AND !Removed (no exclusion)
addedSp           = sum(SP) where !WasCommitted AND !Removed AND NOT IN excludedStatuses
removedSp         = sum(SP) where Removed
completedSp       = sum(SP) where FinalStatus IN doneStatuses AND !Removed AND NOT IN excludedStatuses
netScopeChange    = addedSp - removedSp
disruptionRate    = addedSp / committedSpActive * 100  (0 if denominator == 0)
bugCount          = count where !WasCommitted AND !Removed AND IssueType == "Bug"
```

## Classification (priority order)

Each mid-sprint addition (non-committed, non-removed) gets exactly one category:

1. **Planning Overflow** — AddedAt <= planningCutoff (sprint start + 2 days). Wins over all others.
2. **Unplanned Bug** — IssueType == "Bug" AND added after planning window.
3. **Priority Escalation** — Ticket.CreatedDate < sprint.StartDate AND added after day 2. Pre-existing work pulled in.
4. **Scope Injection** — everything else after day 2. Brand new work.

## Burnup Chart (single-sprint)

- Day 1 = sprint start. Starting scope = committedSpActive (feature-only: excludes bugs).
- Each day: cumulative scope += added today - removed today (feature-only: additions and removals exclude bugs).
- Completed SP uses the FIRST done-status transition within the sprint window per ticket (feature-only: bug completions do NOT step up the green line).
- Phase shading: days 1-2 = "planning", day 3+ = "execution".
- **Feature-only (since FeatureOnlyMetrics):** `totalScopeSp` and `completedSp` series exclude bug tickets (`IssueType != "Bug"`). Ticket counts (`totalScopeTickets`, `completedTickets`) also exclude bugs. The `bugSp` red area is unchanged.
- **Multi-sprint fields unchanged:** `ComputePerSprintData` and `ComputeSprintMetrics` remain total-scope.

## Bug Time-in-Progress (single-sprint)

For mid-sprint bugs only. Active statuses = middle stages in WorkflowStages (skip first and last). Falls back to `{"In Progress"}` if no stages configured. Measures total calendar time spent in active statuses.

## Effective SP

All SP sums use `GetEffectiveSp(defaultSpPerBug)`: returns `StoryPoints` if non-null and > 0, else `defaultSpPerBug` if IssueType == "Bug" and default > 0, else null. Configured via `AppSettings.DefaultSpPerBug` (default 3).

## Edge Cases

- Division by zero -> 0
- committedSpTotal returned separately for "Active vs Total" display
- Bug count tracks ticket count regardless of story points
