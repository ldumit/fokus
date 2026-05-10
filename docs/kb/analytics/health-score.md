# Health Score

Composite 0-100 score summarizing sprint health. Displayed as a RAG badge on the Dashboard.

**Key file:** `Fokus.API/Features/Analytics/SprintSummaryService.cs`
**Endpoint:** `GET /analytics/sprint-summary?sprintId&subTeam`
**Store:** `client/src/stores/dashboardStore.ts`
**View:** `client/src/views/DashboardView.vue`

## Input Metrics

| Metric | Formula | Polarity |
|--------|---------|----------|
| Completion % | featureCompletedSP / featureCommittedSP * 100 | Higher is better |
| Disruption % | addedSP / committedSP * 100 | Lower is better |
| CarryOver % | carryOverSP / (committedSP + addedSP) * 100 | Lower is better |

All SP sums use `GetEffectiveSp(defaultSpPerBug)`: returns `StoryPoints` if non-null and > 0, else `defaultSpPerBug` if ticket is a Bug and default > 0, else null. Filter: `RemovedAt == null`. Committed = `WasCommitted == true`. Completed = `FinalStatus IN doneStatuses`. Added = `WasCommitted == false`. CarryOver = `FinalStatus NOT IN doneStatuses`.

**Completion % is feature-only** (since FeatureOnlyMetrics): uses `featureCommitted` (committed && !IsBug) and `featureCompleted` (done && !IsBug) as denominator and numerator. Returns 0 if featureCommitted == 0.

**Disruption rates and Carry-Over Rate remain total-scope** (include bugs in committed denominator and carry-over numerator). The single combined `DisruptionRate` (scope + bug additions combined) is used for the health score. The dashboard displays it split into two metric cards: **Scope Disruption Rate** (non-bug additions) and **Bug Disruption Rate** (bug additions).

## Per-Metric Scoring

**Higher-is-better (Completion):**
- value >= green (80) -> 100
- value in [amber (60), green) -> linear 50-99
- value < amber -> linear 0-49 (clamped to 0)

**Lower-is-better (Disruption, CarryOver):**
- value <= green (10) -> 100
- value in (green, amber (25)] -> linear 99-50
- value > amber -> linear 49-0 (hits 0 at 2x amber, clamped)

## Composite Score

`(completionScore * 40 + disruptionScore * 30 + carryOverScore * 30) / totalWeight`

Weights and thresholds are configurable in AppSettings. See [Settings](../domain/settings.md).

## RAG Classification

- Composite >= 75 -> green, >= 40 -> amber, else red
- Per-metric uses its own thresholds and polarity

## Other Dashboard Content

**Top epics:** Top 3 by SP completed in selected sprint. Completion % uses `Ticket.CurrentStatus` across ALL tickets with that epic key.

**Leaderboard:** Active developers ordered by SP completed desc, then name asc.

**Flags:**
- Zombie: tickets in 3+ distinct sprints (across all synced sprints)
- Mid-sprint disruption: tickets added >2 days after sprint start, not removed (hardcoded 2 days, not PlanningWindowDays)
- Zero-SP developers: active devs who completed 0 SP

**Sparklines:** Window of up to 4 sprints ending at the selected sprint.
