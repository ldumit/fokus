# Developer Throughput

Per-developer SP completed with capacity-aware rolling averages.

**Key file:** `Fokus.API/Features/Analytics/DeveloperThroughputService.cs`
**Endpoint:** `GET /analytics/developer-throughput?sprintId&last&subTeam`
**Store:** `client/src/stores/developersStore.ts`
**View:** `client/src/views/DevelopersView.vue` (Throughput tab, default)

## Per-Developer, Per-Sprint Metrics

All SP sums use `GetEffectiveSp(defaultSpPerBug)`: returns `StoryPoints` if non-null and > 0, else `defaultSpPerBug` if IssueType == "Bug" and default > 0, else null. Configured via `AppSettings.DefaultSpPerBug` (default 3).

**All metrics are feature-only (since FeatureOnlyMetrics):** bug tickets (`IssueType == "Bug"`) are excluded from all SP and ticket count calculations.

All metrics use `TransitionAttributionChecker` (see cross-cutting.md TransitionBasedSprintScope). Feature-only.

```
spAssigned         = sum(effectiveSP) where isStarted AND !Removed AND !IsBug  (transitioned to startStage during sprint)
spCompleted        = sum(effectiveSP) where isCompleted AND !Removed AND !IsBug  (transitioned to endStage during sprint)
completionPercent  = spCompleted / spAssigned * 100  (0 if spAssigned == 0)
ticketsDone        = count where isCompleted AND !Removed AND !IsBug
ticketsCarriedOver = count where isStarted AND !isCompleted AND !Removed AND !IsBug
```

## Rolling Average (3-sprint, capacity-aware, transition-based)

- Walks backward from current sprint (inclusive)
- Skips sprints where developer has 0% effective capacity
- Collects up to 3 qualifying sprints
- Returns null if fewer than 3 qualifying sprints found
- Rolling average = average of feature-only transition-based spCompleted in those 3 sprints

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
