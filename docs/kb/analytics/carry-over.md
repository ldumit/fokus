# Carry-Over Tracker

Measures incomplete work — what didn't finish, where it's stuck, and whether it keeps rolling.

**Key file:** `Fokus.API/Features/Analytics/CarryOverService.cs`
**Endpoint:** `GET /analytics/carry-over?sprintId&last&subTeam`
**Store:** `client/src/stores/sprintsStore.ts` (shared with scope-change)
**View:** `client/src/views/SprintsView.vue` (sections below scope-change)

## Core Formulas

A ticket is carried over if: `FinalStatus NOT IN doneStatuses AND RemovedAt == null`

```
carryOverSp         = sum(SP) for carried-over tickets NOT IN excludedStatuses
carryOverTicketCount = count of ALL carried-over tickets (including excluded)
totalScopeSp        = committedSpActive + addedSp
carryOverRate       = carryOverSp / totalScopeSp * 100  (0 if denominator == 0)
```

## Zombie Tickets

Tickets appearing in 3+ distinct sprints across ALL synced sprints (not just selected range). Sprint count = number of distinct sprint IDs in SprintMembership.

**Trajectory:** For each zombie, shows up to 10 most recent sprint appearances with the FinalStatus in each sprint. Reveals stall patterns (e.g., "Testing -> Testing -> Testing").

## Carry-Over Destination (single-sprint only)

Tracks what happened to the PRIOR sprint's carry-over tickets in the current sprint:

| Outcome | Condition |
|---------|-----------|
| **Completed** | In current sprint, FinalStatus IN doneStatuses |
| **Carried Again** | In current sprint, still not done, not removed |
| **Removed** | In current sprint, RemovedAt != null |
| **Dropped** | Not in current sprint at all |

Hidden when no prior sprint exists or prior sprint had zero carry-over.

## Status Distribution

Groups carry-over tickets by FinalStatus mapped to WorkflowStages. Unmatched statuses go to "Other" (appears last). Tickets with excluded statuses are invisible (not just SP-excluded).

## Effective SP

All SP sums use `GetEffectiveSp(defaultSpPerBug)`: returns `StoryPoints` if non-null and > 0, else `defaultSpPerBug` if IssueType == "Bug" and default > 0, else null. Configured via `AppSettings.DefaultSpPerBug` (default 3).

## Edge Cases

- ExcludedFromScopeStatuses: excluded from SP sums AND invisible in status distribution
- No workflow stages configured: all tickets grouped under "Other"
- Delta polarity: lower carry-over is better (green down, red up)
