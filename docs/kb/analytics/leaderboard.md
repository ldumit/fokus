# Leaderboard Analytics

Covers both the Dashboard leaderboard widget (single-sprint, features/bugs toggle) and the Developers page Leaderboard tab (multi/single sprint, stacked bar chart, delta indicators).

## Business Rules

- **Bug classification:** `IssueType == "Bug"` (exact, case-sensitive). Everything else is a feature.
- **Completed filter:** `isCompletedInSprint AND RemovedAt == null AND FinalStatus NOT IN excludedFromScopeStatuses` (transition-based since TransitionBasedSprintScope — see cross-cutting.md). Excludes scope-rejection statuses — aligned with BugRatio.
- **SP computation:** `GetEffectiveSp(defaultSpPerBug)` — unestimated bugs use DefaultSpPerBug. Null-SP tickets are excluded from SP sums but included in ticket counts.
- **Active developers only:** Only active developers appear in results. Zero-value developers are shown (not hidden).
- **Developer exclusion:** Cross-cutting exclusion applies (0% capacity + 0 completed tickets). For multi-sprint: exclude developers excluded in ALL selected sprints.
- **Sort order:** Developers sorted by total SP descending, then displayName ascending.
- **Default last:** Multi-sprint defaults to last 5 sprints when neither sprintId nor last is provided.

## Delta Polarity

| Metric | Polarity |
|--------|----------|
| Feature SP | positive-up (more features = green) |
| Bug SP | positive-down (fewer bugs = green) |
| Total SP | neutral |
| Feature Tickets | positive-up |
| Bug Tickets | positive-down |

## Dashboard Leaderboard Toggle

- Default mode: `features` (resets to features on each page mount)
- Frontend-only re-sort: Features mode sorts by `featureSp` desc; Bugs mode by `bugSp` desc
- Both use secondary sort by `displayName` asc
- API returns full breakdown; no refetch on toggle

## API Endpoint

`GET /api/analytics/leaderboard?sprintId=&last=&subTeam=`

- `sprintId` and `last` are mutually exclusive (400 if both provided)
- `last < 1` → 400
- `sprintId` not a closed sprint → 400
- Returns `LeaderboardResponse` with `mode: 'multi' | 'single'`

## Normalized SP Indicator

Added by the NormalizedCapacityIndicator feature. Shown alongside SP values to give capacity-adjusted context.

### Normalization Formula

`normalizedSp = round(spCompleted / (effectiveCapacity / 100))`

This projects what SP the developer would have delivered at full capacity.

### Display Rules

- Only shown when `capacityPercent < 100` (and `capacityPercent > 0`).
- Rendered inline as `(~X)` immediately after the raw SP value.
- Column headers include an `InfoTooltip` explaining what the normalized value means.

### Capacity Resolution (effectiveCapacity)

Priority order:
1. Sprint-specific capacity from `SprintCapacities` lookup (keyed by `(developerId, sprintId)`).
2. Developer default capacity (`Developer.DefaultCapacityPercent`).
3. Fallback: `100` (full capacity — indicator suppressed since `< 100%` condition not met).

This matches the same resolution logic used in `DeveloperThroughputService`.

### Multi-Sprint Averaging (BR6)

Per-sprint normalize first, then average — **not** average-then-normalize.

For each sprint breakdown: compute `normalizedSp` using that sprint's `capacityPercent`. Average only the sprints where `capacityPercent < 100`. If no reduced-capacity sprints exist across the selection, the indicator is suppressed (returns `null`).

This prevents a developer with one low-capacity sprint from distorting the average.

## Key Files

- `src/Services/Fokus/Fokus.API/Features/Analytics/LeaderboardService.cs` — computation service
- `src/Services/Fokus/Fokus.API/Features/Analytics/GetLeaderboard/GetLeaderboardEndpoint.cs` — endpoint
- `src/Services/Fokus/Fokus.API/Features/Analytics/SprintSummaryService.cs` — `DeveloperSummary` record and `ComputeLeaderboard` (Dashboard)
- `client/src/components/developers/LeaderboardTab.vue` — Developers page container
- `client/src/components/developers/LeaderboardChart.vue` — stacked bar chart (CSS, no lib)
- `client/src/components/developers/LeaderboardTable.vue` — metrics table with delta indicators
- `client/src/views/DashboardView.vue` — Dashboard leaderboard toggle (features/bugs)
- `client/src/stores/developersStore.ts` — leaderboard state, fetchLeaderboard action
