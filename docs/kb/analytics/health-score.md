# Health Score

Composite 0-100 score summarizing sprint health. Displayed as a RAG badge on the Dashboard.

**Key file:** `Fokus.API/Features/Analytics/SprintSummaryService.cs`
**Endpoint:** `GET /analytics/sprint-summary?sprintId&subTeam`
**Store:** `client/src/stores/dashboardStore.ts`
**View:** `client/src/views/DashboardView.vue`

## Input Metrics (transition-based since TransitionBasedSprintScope)

| Metric | Formula | Polarity |
|--------|---------|----------|
| Completion % | completedSp / activeSp * 100 | Higher is better |
| Disruption % | addedSp / activeSp * 100 | Lower is better |
| CarryOver % | carryOverSp / activeSp * 100 | Lower is better |

All metrics are **feature-only** and **transition-based** (see cross-cutting.md TransitionBasedSprintScope). All SP sums use `GetEffectiveSp(defaultSpPerBug)`. Filter: `RemovedAt == null AND !IsBug AND !excluded`.

- `activeSp` = sum(SP) where isStarted (transitioned to CycleTimeStartStage or beyond during sprint)
- `completedSp` = sum(SP) where isCompleted (transitioned to CycleTimeEndStage or beyond during sprint)
- `addedSp` = sum(SP) where isAdded (AddedAt > sprintStart AND isStarted)
- `carryOverSp` = sum(SP) where isStarted AND !isCompleted

Completion % can exceed 100% when carry-over from a prior sprint completes in the current sprint (no activeSp increment, but completedSp increments).

The dashboard displays disruption split into two metric cards: **Scope Disruption Rate** (feature additions / activeSp) and **Bug Disruption Rate** (bug additions / activeSp).

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
