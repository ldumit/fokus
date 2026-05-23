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

There are two sync paths. They run independently.

### Path 1 — `POST /api/xray/sync` (dedicated TE sync)

1. Validate `JiraOptions.ProjectKey` is non-null (BadRequestException if missing)
2. Re-fetch fresh Jira issues for the target sprints (needed for TestSet discovery)
3. Authenticate with Xray Cloud (client credentials → bearer token)
4. Call `GetAllProjectTestExecutionsAsync(projectKey)` — JQL `project = {KEY}` fetches **all** project TEs with test cases and issuelinks in one paginated GraphQL query
5. Collect all candidate ticket keys from TE-level and test-case-level issuelinks (OutwardIssueKey ?? InwardIssueKey)
6. Call `TicketRepository.GetExistingKeysAsync(candidateKeys)` — resolves which keys exist in DB (FK safety, single query)
7. `BuildLinkMap` — derive TE→ticket link map from Xray response using known ticket keys only; two sources:
   - Test case issuelinks: `"Test"` link type → `TestExecutionLinkType.Tests`
   - TE-level issuelinks: `"Test"` → `Tests`, `"Blocks"` → `Blocks`
8. Upsert each TE, **replace** links, **replace** test runs (full replace, not merge)
9. `SyncTestSetsFromIssuesAsync` — walk Jira issue links, extract TestSet DTOs (`"Test"` link type + `"Test Set"` issue type), upsert each TestSet

### Path 2 — piggyback during `SyncSprintsFromJiraAsync`

Sprint sync piggbacks TestSet-only Xray sync: calls `SyncTestSetsFromIssuesAsync(issues)` for each sprint's Jira issues. TE discovery does **not** run here — it is project-wide and belongs in the dedicated endpoint.

Key files:
- `Fokus.API/Features/Xray/XrayIssueSyncService.cs` — `SyncTestExecutionsForProjectAsync`, `SyncTestSetsFromIssuesAsync`, `BuildLinkMap`
- `Fokus.API/Features/Xray/SyncXray/SyncXrayEndpoint.cs` — `POST /api/xray/sync` (Admin only)
- `Fokus.API/Features/Sync/SprintIssueSyncService.cs` — piggyback TestSet sync in `SyncAsync`
- `Fokus.API/Features/Settings/SaveXraySettings/SaveXraySettingsEndpoint.cs` — `PUT /api/settings/xray`
- `Fokus.Persistence/Repositories/TestExecutionRepository.cs` — sprint attribution and coverage queries

## Link Type Detection

Link types are now sourced from two places in the **Xray GraphQL response** (not from sprint ticket issuelinks on Jira):

| Source | Jira link type name | Result |
|--------|---------------------|--------|
| Test case `jira.issuelinks` | `"Test"` | → `TestExecutionLinkType.Tests` (test-case-mediated coverage) |
| TE `jira.issuelinks` | `"Test"` | → `TestExecutionLinkType.Tests` (direct story↔TE link) |
| TE `jira.issuelinks` | `"Blocks"` | → `TestExecutionLinkType.Blocks` |

TestSet links are still sourced from **Jira issue links** (`issue.Fields.Issuelinks`) — TestSets are not discoverable from the Xray GraphQL response.

Both inward and outward sides are checked (`OutwardIssueKey ?? InwardIssueKey`). Only links whose ticket key exists in the DB are included (FK safety).

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
