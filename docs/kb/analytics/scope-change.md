# Scope Change & Disruption

Measures what changed during a sprint — additions, removals, and disruption classification.

**Key file:** `Fokus.API/Features/Analytics/ScopeChangeService.cs`
**Endpoint:** `GET /analytics/scope-change?sprintId&last&subTeam`
**Store:** `client/src/stores/sprintsStore.ts`
**View:** `client/src/views/SprintsView.vue`

## Core Formulas (planning-gated since PlanningGatedDisruption)

All scope attribution uses `TransitionAttributionChecker` (see cross-cutting.md). Feature-only throughout.
`planningCutoff = sprint.StartDate.AddDays(planningWindowDays)` (configured in Settings).

```
activeSp         = sum(SP) where isStarted AND !Removed AND !IsBug AND !excluded  (tickets that transitioned to startStage during sprint)
committedSpTotal = sum(SP) where !IsBug AND (WasCommitted OR AddedAt <= planningCutoff) AND (RemovedAt == null OR RemovedAt > planningCutoff)
                   (membership at planning cutoff — no excluded-status filter; matches dashed line behavior)
addedSp          = sum(SP) where AddedAt > planningCutoff AND isStarted AND !Removed AND !IsBug AND !excluded  (post-planning cycle-entered additions)
removedSp        = sum(SP) where RemovedAt > planningCutoff AND had entered cycle before removal AND !IsBug AND !excluded
                   (IsRemovedPostPlanning: RemovedAt > planningCutoff AND qualifying start transition in [sprintStart, RemovedAt])
completedSp      = sum(SP) where isCompleted AND !Removed AND !IsBug AND !excluded  (transition to endStage during sprint)
bugSpCompleted   = sum(SP) where isCompleted AND !Removed AND IsBug AND !excluded  (separate bug bar)
netScopeChange   = addedSp - removedSp
disruptionRate   = addedSp / activeSp * 100  (0 if denominator == 0)
bugCount         = count of bug tickets with isCompleted during sprint
```

## Classification (priority order)

Only tickets that qualify as Added SP (AddedAt > planningCutoff AND cycle-entered AND non-bug AND non-excluded AND non-removed) receive a classification category. 3 categories (Planning Overflow removed since PlanningGatedDisruption):

1. **Unplanned Bug** — IssueType == "Bug". (Note: bug tickets still get a category when they qualify as added.)
2. **Priority Escalation** — Ticket.CreatedDate < sprint.StartDate. Pre-existing work pulled in after planning.
3. **Scope Injection** — everything else. Brand new work added after planning.

## Burnup Chart (single-sprint, transition-based since TransitionBasedSprintScope)

- **Scope line (orange):** Cumulative feature SP that have transitioned to CycleTimeStartStage (or beyond) within [sprintStart, current day]. Starts near zero and grows as tickets enter the pipeline.
- **Completed line (green):** Cumulative feature SP that have transitioned to CycleTimeEndStage (or beyond) within [sprintStart, current day].
- **Bug area (red):** Unchanged — tracks remaining bug work (starts at bug SP present at sprint start, grows with bug additions, shrinks with bug completions and removals).
- Phase shading: days 1-2 = "planning", day 3+ = "execution".
- `TotalScopeSp` semantic changed from "plan-based scope" to "cumulative started tickets SP."

## Multi-Sprint Bar Chart (since TransitionBasedSprintScope)

`ScopeChangePerSprintData` includes `BugSpCompleted` field — separate bug SP completed bars alongside feature bars. Previously total-scope fields are now feature-only.

## Bug Time-in-Progress (single-sprint)

For mid-sprint bugs only. Active statuses = middle stages in WorkflowStages (skip first and last). Falls back to `{"In Progress"}` if no stages configured. Measures total calendar time spent in active statuses.

## Effective SP

All SP sums use `GetEffectiveSp(defaultSpPerBug)`: returns `StoryPoints` if non-null and > 0, else `defaultSpPerBug` if IssueType == "Bug" and default > 0, else null. Configured via `AppSettings.DefaultSpPerBug` (default 3).

## Edge Cases

- Division by zero -> 0
- committedSpTotal returned separately for "Active vs Total" display
- Bug count tracks ticket count regardless of story points
