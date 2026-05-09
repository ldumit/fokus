# Developer Throughput

Per-developer SP completed with capacity-aware rolling averages.

**Key file:** `Fokus.API/Features/Analytics/DeveloperThroughputService.cs`
**Endpoint:** `GET /analytics/developer-throughput?sprintId&last&subTeam`
**Store:** `client/src/stores/developersStore.ts`
**View:** `client/src/views/DevelopersView.vue` (Throughput tab, default)

## Per-Developer, Per-Sprint Metrics

All SP sums use `GetEffectiveSp(defaultSpPerBug)`: returns `StoryPoints` if non-null and > 0, else `defaultSpPerBug` if IssueType == "Bug" and default > 0, else null. Configured via `AppSettings.DefaultSpPerBug` (default 3).

```
spAssigned        = sum(effectiveSP) where !Removed
spCompleted       = sum(effectiveSP) where !Removed AND FinalStatus IN doneStatuses
completionPercent = spCompleted / spAssigned * 100  (0 if spAssigned == 0)
ticketsDone       = count where !Removed AND FinalStatus IN doneStatuses
ticketsCarriedOver = count where !Removed AND FinalStatus NOT IN doneStatuses
```

## Rolling Average (3-sprint, capacity-aware)

- Walks backward from current sprint (inclusive)
- Skips sprints where developer has 0% effective capacity
- Collects up to 3 qualifying sprints
- Returns null if fewer than 3 qualifying sprints found
- Rolling average = average of SP completed in those 3 sprints

## Capacity Resolution

1. Check DeveloperSprintCapacity record for developer+sprint
2. If no record exists, fall back to Developer.DefaultCapacityPercent

## Delta Polarity

- spAssigned: neutral (no color)
- spCompleted, completionPercent, ticketsDone: higher is better (positive-up)
- ticketsCarriedOver: lower is better (positive-down)

## Important Notes

- Does NOT apply ExcludedFromScopeStatuses (implemented before F10 introduced them)
- Zero-ticket developers are visible with all-zero values (never hidden)
- Only active developers shown
- Capacity update is optimistic UI — immediate local update, rollback on API failure
