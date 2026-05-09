# Jira Sync

Data pipeline from Jira REST API into the local SQLite database.

**Key file:** `Fokus.API/Features/Sync/SprintIssueSyncService.cs`
**Jira client:** `Modules/Jira/Jira.RestApi/RestApiJiraClient.cs`
**Endpoints:** `GET /sync/boards`, `GET /sync/jira-sprints`, `POST /sync/sprints`, `POST /sync/backlog-sprints`

## Sync Flow (per sprint)

1. Fetch all issues for sprint from Jira (with changelog expand)
2. Upsert Sprint entity via `Sprint.FromJira`
3. For each issue:
   - Upsert Ticket via `Ticket.FromJira`
   - Upsert Developer if assignee present via `Developer.FromJira` (returns null if no assignee)
   - REPLACE all StatusTransitions for that ticket (full replace, not incremental)
   - Build SprintMembership via `SprintMembership.FromJira` with PlanningWindowDays from Settings
4. Upsert all memberships for the sprint atomically

## Epic Discovery Flow

- Finds all distinct epic keys from existing tickets
- Fetches all issues under each epic from Jira
- Only INSERTS tickets that don't already exist (skips existing)
- Also upserts developers for any assignees found

## Backlog Sync

Uses `forcedNotCommitted = true` — all memberships created with `WasCommitted = false` regardless of timing. Used for syncing historical/backlog sprints where commitment data isn't meaningful.

## Changelog Processing

SprintMembership.FromJira walks Jira changelog to determine:
- **AddedAt:** First event where sprint ID appears in `To` but not `From` (comma-separated sprint IDs)
- **RemovedAt:** First event where sprint ID appears in `From` but not `To`
- **WasCommitted:** effectiveAddedAt <= planningCutoff

StatusTransition.ListFromJira extracts status changes:
- Only processes items where `Field == "status"` (exact match)
- Ordered by changelog Created ascending

## Data Freshness

- SyncedAt timestamp on Sprint tracks when last synced
- No real-time updates — user manually triggers sync
- SyncBackSprintCount setting controls how many past sprints to fetch (default 20)
