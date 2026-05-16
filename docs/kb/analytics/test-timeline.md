# Test Execution Timeline (F30)

## Purpose

F30 answers **when** testing happened relative to the sprint lifecycle. Distinct from F26 (coverage/pass rate — **what** was tested). Reads F25 entities: TestExecution, TestRun, TestExecutionLink.

## Key File

`src/Services/Fokus/Fokus.API/Features/Analytics/TestTimelineService.cs`

Endpoint: `GET /api/sprints/{sprintId}/test-timeline`

## Business Rules

### Effective Date Resolution
`FinishedAt ?? StartedAt`. Runs where both are null are excluded entirely.

### Terminal-Status Filter
Only PASS or FAIL runs are plotted or counted. Cancelled TEs excluded at repository level.

### Sprint Day Assignment
`dayNumber = (effectiveDate.Date - sprint.StartDate.Date).Days + 1`
Day 1 = sprint StartDate. Days after EndDate: `isWithinSprint = false` (post-sprint bucket).

### Burnup Accumulation
Cumulative PASS/FAIL/Total lines. Each day's value = prior day + today's terminal runs. Monotonically increasing.

### Testing Crunch Flag (>50% threshold)
- **Denominator:** terminal runs with effectiveDate within sprint boundary (StartDate..EndDate inclusive)
- **Numerator:** terminal runs in last 2 calendar days before EndDate (EndDate and EndDate-1), also within sprint
- **Fires when:** percentage > 50 (strictly greater). Zero denominator = null percentage, flag does not fire.
- Post-sprint runs excluded from both numerator and denominator.
- `ComputeCrunchFlag(...)` is a public static method — reused by `GetSprintSummaryEndpoint` for Dashboard flags without DI injection.

### Post-Sprint Testing
Runs with effectiveDate > EndDate. Percentage denominator = all terminal runs (within-sprint + post-sprint). Section hidden when zero post-sprint runs.

### Completed But Untested at Close
Ticket reached CycleTimeEndStage (via StatusTransition) before sprint EndDate AND has zero terminal-status runs with effectiveDate <= EndDate. Sorted by devDoneDate ascending.

### Dev-to-Test Gap
- **Dev done date:** earliest transition to CycleTimeEndStage
- **First test date:** earliest FinishedAt among PASS/FAIL runs linked via TestExecutionLinks (linkType=Tests)
- **Gap:** `(firstTestDate - devDoneDate).TotalDays` — negative gaps are valid
- **Median:** across all eligible tickets (both dates known)
- **Delta:** vs prior sprint's median. Direction: down = shrinking (good/green), up = growing (bad/red)

### Sub-Team Filter
When subTeam provided, filters SprintMembership to tickets whose Assignee.SubTeam matches. All computations scope to filtered memberships only.

### Scope Change Overlay
From SprintMembership AddedAt/RemovedAt, aggregates per-day SP added/removed/net. Only days with events included. AddedAt is non-nullable DateTime (not DateTime?).

## Prior Sprint Resolution
Most recent closed sprint before selected sprint by StartDate — consistent with SprintSummaryService pattern. Used for dev-to-test gap delta only.

## Relationship to F26
F26 = coverage/pass rate (what was tested). F30 = timing (when testing happened). Different questions, different metrics, separate endpoints.
