# Jira Team-Managed Project: Workarounds & Discoveries

## Problem

Fokus was built assuming company-managed (Scrum) Jira projects. Team-managed projects (formerly "next-gen") use a "simple" board type that behaves differently at the API level.

## Board Type: Simple vs Scrum

Team-managed projects create boards with `type: "simple"`. The Jira Agile REST API treats these differently:

- `GET /rest/agile/1.0/sprint/{sprintId}/issue` returns **no issues** for simple boards (empty or null `issues` array).
- `GET /rest/agile/1.0/board/{boardId}/backlog` same behavior — returns nothing useful.
- `GET /rest/agile/1.0/epic/{epicKey}/issue` same behavior — returns no issues.
- Sprint metadata (`GET /rest/agile/1.0/board/{boardId}/sprint`) **does work** — sprint names, dates, and states are returned correctly.

## Architecture: Base Class vs Subclass

Issue-fetching is handled by two classes:

- **`RestApiJiraClient`** — uses the Agile API (`/rest/agile/1.0/...`) for issue fetching. This is the default for company-managed (Scrum) boards. `GetBoardsAsync`, `GetStatusesAsync`, and `GetSprintsAsync` are shared by both board types and live only in the base class.

- **`TeamManagedJiraClient`** — subclass that overrides the three issue-fetching methods with JQL equivalents. Selected when `Jira:IsTeamManaged = true` in configuration.

DI registration in `AddRestApiJira()` reads `JiraOptions.IsTeamManaged` at startup and registers the correct implementation as `IJiraClient`.

```json
{
  "Jira": {
    "InstanceUrl": "https://your-org.atlassian.net",
    "Email": "you@example.com",
    "ApiToken": "...",
    "IsTeamManaged": true,
    "ProjectKey": "FOK"
  }
}
```

`ProjectKey` is only required when `IsTeamManaged = true` and backlog sync is used. It is used in the backlog JQL: `project = {key} AND sprint is EMPTY`.

## Solution: JQL Overrides (Team-Managed)

`TeamManagedJiraClient` overrides three methods with JQL queries via `/rest/api/3/search/jql`:

| Method | JQL |
|--------|-----|
| `GetSprintIssuesAsync` | `sprint = {sprintId}` |
| `GetEpicIssuesAsync` | `"Epic Link" = {epicKey}` (falls back to `parent = {epicKey}` on failure) |
| `GetBoardBacklogIssuesAsync` | `project = {projectKey} AND sprint is EMPTY` |

### Epic JQL Note

Team-managed projects may not support the `"Epic Link"` custom field in JQL. The implementation tries `"Epic Link"` first; if it fails (Jira returns an error), it retries with `parent = {epicKey}`. The field used is logged at DEBUG level.

**Deprecation notice:** The `"Epic Link"` field (`customfield_10014`) is deprecated in Jira Cloud as of May 2026 and will be removed in November 2026. After removal, `parent = {epicKey}` will be the only option.

## Changelog Truncation

### The Problem

The JQL search endpoint (`/rest/api/3/search/jql`) caps changelog at **100 entries per issue** on Jira Cloud (previously documented as 20 — that figure applies to Jira Server/Data Center). Issues with more than 100 changelog entries have truncated histories, which silently corrupts:

- `WasCommitted` / `AddedAt` / `RemovedAt` — derived from Sprint field changes
- `StatusTransition` records — derived from status changes

This affects F8 (Sprint Summary), F9 (Developer Throughput), and would make F10 (Scope Change) and F12 (Cycle Time) unreliable.

### Detection and Enrichment

`JiraChangelog` now carries `Total`, `StartAt`, and `MaxResults` fields alongside `Histories`. After fetching issues via JQL, `EnrichChangelogsAsync` checks each issue:

```
if (issue.Changelog.Total > issue.Changelog.Histories.Count)
    → replace Histories with full changelog from paginated endpoint
```

The paginated changelog endpoint (`GET /rest/api/3/issue/{key}/changelog`) returns all entries without a cap, using offset-based pagination (`startAt` + `values`). `GetFullChangelogAsync` loops until all pages are consumed.

Both helpers live in `RestApiJiraClient` as `protected` methods — reusable infrastructure, not subclass-specific policy.

## API Migration: `/rest/api/3/search` to `/rest/api/3/search/jql`

Atlassian deprecated `/rest/api/3/search` (CHANGE-2046). The old endpoint returns **HTTP 410 Gone**.

### Breaking Changes

| Area | Old (`/search`) | New (`/search/jql`) |
|------|-----------------|---------------------|
| Pagination | `startAt` (offset-based) | `nextPageToken` (cursor-based) |
| Total count | `total` in response | **Removed** |
| Default fields | All navigable fields | `id` only — must specify `fields` explicitly |
| Empty JQL | Worked | Returns **400 Bad Request** |
| Changelog expand | Unlimited | Capped at 100 items (Cloud) |

### Our Adaptation

- `IJiraApi.SearchIssuesAsync` points to `/rest/api/3/search/jql`
- `nextPageToken` replaces `startAt` parameter
- `SearchAllIssuesAsync` in `RestApiJiraClient` implements cursor-based pagination (loop until `NextPageToken` is null/empty)
- `fields` parameter is always sent explicitly (`IssueFields` constant)
- Offset-based `GetAllIssuesAsync` is kept for Agile endpoints which still use `startAt`/`total`

### Affected Files

- `Jira.Contracts/JiraPagedResult.cs` — added `NextPageToken` to `JiraIssuePagedResult`
- `Jira.Contracts/JiraChangelog.cs` — added `Total`, `StartAt`, `MaxResults` for truncation detection
- `Jira.RestApi/IJiraApi.cs` — updated endpoint URL, swapped `startAt` for `nextPageToken`; added `GetIssueChangelogPageAsync`
- `Jira.RestApi/RestApiJiraClient.cs` — added `SearchAllIssuesAsync`, `GetFullChangelogAsync`, `EnrichChangelogsAsync`; made issue-fetching methods `virtual`
- `Jira.RestApi/TeamManagedJiraClient.cs` — new subclass with JQL overrides and changelog enrichment
- `Jira.RestApi/DependencyInjection.cs` — config-driven factory for `IJiraClient`

## Agile API Deprecation (Future Migration)

Atlassian has deprecated `/rest/agile/1.0/` endpoints with planned removal in **November 2026**. The replacement is `/rest/software/1.0/`. This migration is out of scope for the current work — it is a separate concern that affects company-managed boards and is tracked for the November 2026 deadline.

## Null Safety: Changelog

Team-managed projects may return **null changelog** for some issues (especially if the issue has no history). Null guards exist in:

- `SprintMembership.FromJira` — `dto.Changelog?.Histories` with null check before iteration
- `StatusTransition.ListFromJira` — early return if `histories is null || histories.Count == 0`

## Content-Type on GET Requests

FastEndpoints attempts to parse request body when `Content-Type: application/json` is set, even on GET requests. The frontend `apiFetch` client was setting this header unconditionally. Fixed to only set `Content-Type` when `options.body` exists.

## Reference

- [Atlassian CHANGE-2046](https://developer.atlassian.com/changelog/#CHANGE-2046) — search endpoint migration
- [Jira Cloud REST API - Issue Search](https://developer.atlassian.com/cloud/jira/platform/rest/v3/api-group-issue-search/)
- [Jira Cloud REST API - Issue Changelog](https://developer.atlassian.com/cloud/jira/platform/rest/v3/api-group-issues/#api-rest-api-3-issue-issueidorkey-changelog-get)
- [Jira Software API deprecation (CHANGE-XXXX)](https://developer.atlassian.com/changelog/) — `/rest/agile/1.0/` removal November 2026
