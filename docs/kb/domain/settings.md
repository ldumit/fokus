# Settings

Singleton entity (Id=1). Configuration for all analytics features.

**Key file:** `Fokus.Domain/Settings/AppSettings.cs`

## Properties and Defaults

| Property | Default | Used By |
|----------|---------|---------|
| BoardId | null | Jira sync — which board to sync |
| DoneStatuses | ["Done", "Closed"] | All analytics — determines ticket completion |
| WorkflowStages | [] | CycleTime (stage funnel), CarryOver (status distribution) |
| ExcludedFromScopeStatuses | [] | ScopeChange, CarryOver, BugRatio — removes tickets from SP calculations |
| PlanningWindowDays | 2 | SprintMembership commitment logic |
| SyncBackSprintCount | 20 | Jira sync — how many past sprints to fetch |
| BugRatioAlertThreshold | 50 | BugRatio — % threshold for alert |
| BugRatioConsecutiveSprintCount | 2 | BugRatio — how many consecutive sprints triggers alert |
| DefaultSpPerBug | 3 | All analytics — fallback SP for unestimated Bug tickets |
| CycleTimeStartStage | null | CycleTime — null = auto (second workflow stage) |
| CycleTimeEndStage | null | CycleTime — null = auto (first done status) |
| HealthThresholds | (see below) | Health score RAG classification |
| HealthWeights | (see below) | Health score composite weighting |

## HealthWeightConfig (Value Object)

**Key file:** `Fokus.Domain/Settings/ValueObjects/HealthWeightConfig.cs`

Completion=40, Disruption=30, CarryOver=30. No enforced sum constraint — code divides by actual sum.

## HealthThresholdConfig (Value Object)

**Key file:** `Fokus.Domain/Settings/ValueObjects/HealthThresholdConfig.cs`

| Metric | Green (good) | Amber (warning) | Polarity |
|--------|-------------|-----------------|----------|
| Completion | >= 80% | >= 60% | Higher is better |
| Disruption | <= 10% | <= 25% | Lower is better |
| CarryOver | <= 10% | <= 25% | Lower is better |
