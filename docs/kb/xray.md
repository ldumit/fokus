# Xray Integration

Xray is an opt-in Jira plugin for test management. Fokus syncs test execution data from Xray Cloud to compute QA coverage metrics per sprint.

## Gate

Sync bails early (returns zeros, no error) if `AppSettings.XrayEnabled == false` OR either credential is empty. Authentication failures are non-fatal — added to `Warnings`, not thrown.

## Data Sources

| Entity | Source |
|--------|--------|
| TestExecution | Xray Cloud GraphQL API |
| TestRun | Xray Cloud GraphQL API (nested inside TE) |
| TestSet | Jira issue link DTO only — no Xray API call |

TestSets are read directly from the Jira issue link fields (`linkedIssue.Fields.*`). No Xray query is made for them.

## Sync Flow

1. Re-fetch fresh Jira issues for each sprint (issue links are not stored in the DB)
2. Walk `Issuelinks` on each issue — extract TE keys and TestSets
3. Authenticate with Xray Cloud (client credentials → bearer token)
4. Fetch TEs via GraphQL using TE issue keys
5. Upsert each TE, **replace** links, **replace** test runs (full replace, not merge)
6. Upsert each TestSet (status/summary from Jira DTO)

Key files:
- `Fokus.API/Features/Xray/XrayIssueSyncService.cs` — orchestration, link extraction, status mapping
- `Fokus.API/Features/Xray/SyncXray/SyncXrayEndpoint.cs` — `POST /api/xray/sync` (Admin only)
- `Fokus.API/Features/Settings/SaveXraySettings/SaveXraySettingsEndpoint.cs` — `PUT /api/settings/xray`
- `Fokus.Persistence/Repositories/TestExecutionRepository.cs` — sprint attribution and coverage queries

## Link Type Detection

Two Jira link types are recognized on sprint tickets:

| Jira link type name | Issue type detected | Result |
|---------------------|---------------------|--------|
| `"Test"` | Test Execution | → TE attributed to this ticket (`Tests` link) |
| `"Test"` | Test Set | → TestSet extracted from Jira DTO |
| `"Blocks"` | Test Execution | → TE attributed to this ticket (`Blocks` link) |

Both inward and outward sides are checked (`link.OutwardIssue ?? link.InwardIssue`).

`TestExecutionLinkType.Tests` is the coverage link. `TestExecutionLinkType.Blocks` creates attribution but is **not** used for coverage or failure counting.

## Business Rules

**BR7 — Cancelled TEs excluded from analytics.**
`TestExecution.IsCancelled` is a computed property (`Status == "Cancelled"`). EF Core cannot translate it — all queries filter with the raw string `te.Status != "Cancelled"`.

**BR8 — Sprint attribution: max-sprint-id tiebreaker.**
A TE belongs to the sprint with the *highest* SprintId across all its linked tickets' memberships. If a TE links tickets from multiple sprints, it's attributed to the latest sprint only.
Replicated identically in `GetTestExecutionsForSprintAsync` and `GetSprintIdsWithQaDataAsync` — keep in sync.

**BR9 — Sub-task coverage inheritance.**
Sub-tasks with no own TE `Tests` links inherit coverage from their parent ticket's TE links.

## Coverage Computation (`GetFeatureTicketsWithCoverageAsync`)

Active scope filter (all must be true):
- `RemovedAt == null` (not removed from sprint)
- `IssueType != "Bug"`
- Status not in `excludedStatuses`
- Has effective SP (`StoryPoints > 0`, or Bug with `defaultSpPerBug > 0`)
- `IsStartedInSprint` — at least one status transition within sprint window reaching a stage ≥ `startIndex`

Coverage = has ≥ 1 non-cancelled TE linked via `Tests` (not `Blocks`) links.

Run counts:
- `TotalRunCount` = runs with `Pass` or `Fail` status (Todo / Executing / Aborted excluded from denominator)
- `FailedRunCount` = runs with `Fail` status

## TestRun Status Mapping

Raw status strings from Xray GraphQL are normalized on ingest:

| Xray string | `TestRunStatus` |
|-------------|-----------------|
| PASS, PASSED | `Pass` |
| FAIL, FAILED | `Fail` |
| EXECUTING, IN PROGRESS | `Executing` |
| ABORTED, ABANDONED | `Aborted` |
| anything else | `Todo` |

## Developer Side-Effect

Any new `AssigneeId` or `ExecutedById` not already in the DB is auto-created as a `Developer` with `DisplayName = accountId` (placeholder — no separate Jira lookup). Existing developers are left unchanged.
