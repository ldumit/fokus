# Daily Developer Progress

Real-time per-developer sprint progress for the active sprint only. No multi-sprint mode.

## Key Files

- `Fokus.API/Features/Analytics/DeveloperProgressService.cs` — computation
- `Fokus.API/Features/Analytics/GetDeveloperProgress/GetDeveloperProgressEndpoint.cs` — endpoint
- `Fokus.API/Hubs/SprintHub.cs` — SignalR hub + method name constants
- `client/src/stores/dailyProgressStore.ts` — SignalR lifecycle + fetch
- `client/src/components/developers/DailyProgressTab.vue` — tab layout
- `client/src/components/developers/DeveloperProgressCard.vue` — per-developer card with burnup chart

## Endpoint

`GET /api/analytics/developer-progress?subTeam=`

Returns `DeveloperProgressResponse`. Always targets the active sprint (state = "Active"). Returns `hasActiveSprint: false` if none found.

## Scope: All Ticket Types

Unlike DeveloperThroughputService (feature-only), daily progress includes ALL ticket types (features, bugs, tasks). Assigned SP = sum of SP for all non-removed memberships. Completed SP = sum for tickets with a qualifying done-status transition within the sprint.

## Pace Formula

```
dailyPace = assignedSp * (capacityPercent / 100) / totalDays
expectedAtCurrentDay = dailyPace * currentDay
```

`totalDays` = calendar days from sprintStart to sprintEnd inclusive (endDate − startDate + 1).
`currentDay` = calendar days from sprintStart to today inclusive, capped at totalDays.

## Grace Period

`isGracePeriod = currentDay <= 2`. During grace period: no behind-pace alerts, all `isBehindPace` flags suppressed on all developers.

## Behind-Pace Rule

`gap = expectedAtCurrentDay − completedSp`
`isBehindPace = gap > dailyPace` (gap exceeds one full day's pace).

Not evaluated during grace period.

## Stall Detection

A ticket is stalled when ALL of:
1. Has at least one transition to a non-done status within the sprint (started in sprint)
2. Is NOT completed (no done-status transition within sprint)
3. Last status transition is > 2 **business days** ago (Mon–Fri, excludes transition day itself)

Business day count: counts weekdays strictly between lastTransitionDate and today (exclusive on both ends, then +1 for today if today is a weekday).

## Alerts

`alerts` = developers with `isBehindPace = true` (suppressed during grace period).
Each alert: `accountId`, `displayName`, `avatarUrl`, `gapSp`, `gapDays` (gap / dailyPace).

## Capacity

Uses `DeveloperSprintCapacity` records for the active sprint. Default 100% if no record exists.

## Daily Breakdown

Per developer: array of `DayBreakdownEntry` (one per sprint calendar day up to today):
- `day` (1-based), `date`, `cumulativeSp`, `expectedCumulativeSp`, `completedTickets`

`completedTickets` = tickets whose first done-status transition falls on that calendar day.

## SignalR Auto-Refresh

`SprintHub` at `/hubs/sprint` broadcasts `SprintSynced` after `SyncSprintsEndpoint` completes. `dailyProgressStore` subscribes and re-fetches on receive. Broadcast is best-effort (try/catch in endpoint — failure does not break sync response).

## Excluded Developers

Endpoint loads `Developer.ExcludedFromAnalytics` flag; excluded developer accountIds are passed to `DeveloperProgressService` and filtered from results.
