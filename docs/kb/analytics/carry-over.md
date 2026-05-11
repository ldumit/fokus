# Carry-Over Tracker

Measures incomplete work — what didn't finish, where it's stuck, and whether it keeps rolling.

**Key file:** `Fokus.API/Features/Analytics/CarryOverService.cs`
**Endpoint:** `GET /analytics/carry-over?sprintId&last&subTeam`
**Store:** `client/src/stores/sprintsStore.ts` (shared with scope-change)
**View:** `client/src/views/SprintsView.vue` (sections below scope-change)

## Core Formulas (transition-based since TransitionBasedSprintScope)

A ticket is carried over if: `isStarted AND !isCompleted AND !Removed AND !IsBug AND !excluded` (transition-based, see cross-cutting.md). Feature-only.

```
activeSp             = sum(SP) where isStarted AND !Removed AND !IsBug AND !excluded
carryOverSp          = sum(SP) where isStarted AND !isCompleted AND !Removed AND !IsBug AND !excluded
carryOverTicketCount = count of ALL carried-over feature tickets
totalScopeSp         = activeSp  (replaces committedSpActive + addedSp)
carryOverRate        = carryOverSp / totalScopeSp * 100  (0 if denominator == 0)
```

## Zombie Tickets

Tickets appearing in 3+ distinct sprints across ALL synced sprints (not just selected range). Sprint count = number of distinct sprint IDs in SprintMembership.

**Trajectory:** For each zombie, shows up to 10 most recent sprint appearances with the FinalStatus in each sprint. Reveals stall patterns (e.g., "Testing -> Testing -> Testing").

## Carry-Over Destination (single-sprint only, transition-based since TransitionBasedSprintScope)

Tracks what happened to the PRIOR sprint's carry-over tickets in the current sprint:

| Outcome | Condition |
|---------|-----------|
| **Completed** | In current sprint, isCompletedInSprint (transition to endStage during current sprint) |
| **Carried Again** | In current sprint, isStartedInSprint AND !isCompletedInSprint |
| **Removed** | In current sprint, RemovedAt != null |
| **Dropped** | In current sprint but no qualifying start transition (fell back to backlog), OR not in current sprint at all |

Hidden when no prior sprint exists or prior sprint had zero carry-over.

## Status Distribution

Groups carry-over tickets by FinalStatus mapped to WorkflowStages. Unmatched statuses go to "Other" (appears last). Tickets with excluded statuses are invisible (not just SP-excluded).

## Effective SP

All SP sums use `GetEffectiveSp(defaultSpPerBug)`: returns `StoryPoints` if non-null and > 0, else `defaultSpPerBug` if IssueType == "Bug" and default > 0, else null. Configured via `AppSettings.DefaultSpPerBug` (default 3).

## Edge Cases

- ExcludedFromScopeStatuses: excluded from SP sums AND invisible in status distribution
- No workflow stages configured: all tickets grouped under "Other"
- Delta polarity: lower carry-over is better (green down, red up)
