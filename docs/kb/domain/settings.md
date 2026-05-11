# Settings

Singleton entity (Id=1). Configuration for all analytics features.

**Key file:** `Fokus.Domain/Settings/AppSettings.cs`

## Properties and Defaults

| Property | Default | Used By |
|----------|---------|---------|
| BoardId | null | Jira sync — which board to sync |
| DoneStatuses | ["Done", "Closed"] | Contributes tail of ordered stage sequence (WorkflowStages ++ DoneStatuses). No longer directly used for completion checks (see cross-cutting.md). |
| WorkflowStages | [] | CycleTime (stage funnel), CarryOver (status distribution) |
| ExcludedFromScopeStatuses | [] | ScopeChange, CarryOver, BugRatio — removes tickets from SP calculations |
| PlanningWindowDays | 2 | SprintMembership commitment logic |
| SyncBackSprintCount | 20 | Jira sync — how many past sprints to fetch |
| BugRatioAlertThreshold | 50 | BugRatio — % threshold for alert |
| BugRatioConsecutiveSprintCount | 2 | BugRatio — how many consecutive sprints triggers alert |
| DefaultSpPerBug | 3 | All analytics — fallback SP for unestimated Bug tickets |
| CycleTimeStartStage | null | CycleTime measurement start (null = auto: second workflow stage). Also drives **active/started scope attribution** across all analytics via TransitionAttributionChecker (null = auto: first stage, wider than cycle time). |
| CycleTimeEndStage | null | CycleTime measurement end (null = auto: first done status). Also drives **completion attribution** across all analytics via TransitionAttributionChecker (supersedes FinalStatus snapshot approach for sprint scope). EpicProgress progress tracking still uses boundary-driven CompletionChecker. |
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
