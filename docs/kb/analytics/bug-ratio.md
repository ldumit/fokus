# Bug Ratio

Measures how much developer capacity goes to bug fixing vs. planned work.

**Key file:** `Fokus.API/Features/Analytics/BugRatioService.cs`
**Endpoint:** `GET /analytics/bug-ratio?sprintId&last&subTeam`
**Store:** `client/src/stores/developersStore.ts` (lazy-loaded on tab switch)
**View:** `client/src/views/DevelopersView.vue` (Bug Ratio tab)

## Core Formulas

Bug = `IssueType == "Bug"` (exact match, case-sensitive). Completed = `isCompletedInSprint AND RemovedAt == null AND FinalStatus NOT IN excludedStatuses` (transition-based since TransitionBasedSprintScope — see cross-cutting.md).

All SP sums use `GetEffectiveSp(defaultSpPerBug)`: returns `StoryPoints` if non-null and > 0, else `defaultSpPerBug` if Bug and default > 0, else null. Configured via `AppSettings.DefaultSpPerBug` (default 3).

```
bugSp           = sum(effectiveSP) for completed Bug tickets
nonBugSp        = sum(effectiveSP) for completed non-Bug tickets
completedSp     = bugSp + nonBugSp
bugRatioPercent = bugSp / completedSp * 100  (0 if completedSp == 0)
```

**Multi-sprint team ratio:** Uses ratio of totals (total bugSp / total completedSp), NOT average of per-sprint ratios. Prevents high-SP sprints from skewing results.

## Alert System

Configurable in Settings: threshold (default 50%), consecutive sprint count (default 2).

**Evaluation:** Walks closed sprints from most recent backward. Counts consecutive sprints where developer's bug ratio >= threshold (transition-based completion per sprint). Alert is active if count >= configured consecutive sprint count.

**Streak breakers:**
- A sprint with 0 completed SP = 0% bug ratio -> breaks the streak
- Alert evaluates against ALL sprint history, not just the current view range

## Delta Polarity

- Bug SP, Bug Ratio %, Bug Count: lower is better (positive-down)
- Non-Bug SP, Non-Bug Count: higher is better (positive-up)

## Issue Type Breakdown

Completed tickets grouped by raw Jira issue type (case-preserved as-is). Shows composition beyond the binary bug/non-bug split.
