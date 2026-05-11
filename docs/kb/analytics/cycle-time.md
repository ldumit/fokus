# Cycle Time

Measures how long completed tickets spend in each workflow stage.

**Key file:** `Fokus.API/Features/Analytics/CycleTimeService.cs`
**Endpoint:** `GET /analytics/cycle-time?sprintId&last&subTeam`
**Store:** `client/src/stores/cycleTimeStore.ts`
**View:** `client/src/views/CycleTimeView.vue`

## Boundary Resolution

```
orderedStages = WorkflowStages + DoneStatuses
startStage    = CycleTimeStartStage ?? WorkflowStages[1]  (second stage, or first if only one)
endStage      = CycleTimeEndStage ?? DoneStatuses[0]
```

Both configurable in Settings. Null = auto-detect.

## Per-Ticket Computation

1. Only completed tickets: `RemovedAt == null AND isCompletedInSprint` (transition-based pre-filter since TransitionBasedSprintScope — ticket must have a qualifying transition to CycleTimeEndStage or beyond within [sprintStart, sprintEnd])
2. Find earliest qualifying completion transition within [sprintStart, sprintEnd] — tickets done before the sprint are excluded
3. Walk ALL transitions for the ticket. For each transition entering a stage within [startStage, endStage]:
   - Entry time clamped to sprintStart if transition predates it
   - Exit time = next transition timestamp (clamped to sprintEnd), or sprintEnd if no next transition
   - Accumulate duration per stage
4. Unrecognized stages accumulated under `"__Other__"`
5. Skip ticket if it never entered startStage or total cycle time <= 0

## Rework Counting

A stage entered again after having been visited before increments reworkCount per entry.

## Percentile Computation

Linear interpolation on sorted values. P50 (median), P75, P85, P90 all computed.

## Key Metrics

- **Median / P85 cycle time** with delta vs prior sprint
- **Throughput** = count of tickets that completed the full cycle
- **Outliers** = tickets with cycle time > median * 2 (relative threshold, not absolute)
- **Stage funnel** = average time per stage across all tickets. Widest segment = bottleneck.
- **Dominant stage** (per developer) = stage with highest total time across their completed tickets

## Edge Cases

- Sprint clamping: transitions before sprint start clamped to start; open stages at sprint end use end as exit
- Multi-sprint mode: metric cards show averages of per-sprint values; delta = last vs second-to-last sprint
- `hasWorkflowStages` in store: computed from `boundaries.workflowStageCount > 0`
