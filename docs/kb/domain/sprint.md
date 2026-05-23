# Sprint & Membership

## Sprint

A Jira sprint synced into Fokus. ID comes from Jira (never auto-generated).

**Key files:** `Fokus.Domain/Sprint/Sprint.cs`, `Fokus.Domain/Sprint/Behaviors/Sprint.cs`

**Properties:** Id (int, Jira ID), Name, BoardId, BoardName, StartDate, EndDate, State (Active/Closed/Future), SyncedAt, Memberships (IReadOnlyList)

**Factory (Sprint.FromJira):**
- State mapping: "active"->Active, "closed"->Closed, "future"->Future, else Active
- Null StartDate defaults to UtcNow; null EndDate defaults to UtcNow+14 days
- SyncedAt always set to UtcNow

## SprintMembership

Junction between Sprint and Ticket. This is the central analytics data structure — most metrics are computed from memberships.

**Key files:** `Fokus.Domain/Sprint/SprintMembership.cs`, `Fokus.Domain/Sprint/Behaviors/SprintMembership.cs`

**Properties:**
- SprintId + TicketId (composite key)
- AddedAt (DateTime) — when ticket entered the sprint
- RemovedAt (DateTime?) — null = still in sprint; non-null = removed
- WasCommitted (bool) — was ticket in the sprint during planning
- FinalStatus (string) — current Jira status at sync time
- StoryPoints (decimal?) — null for unestimated tickets

**Commitment logic (SprintMembership.FromJira):**
- Walks Jira changelog looking for "Sprint"/"sprint" field changes
- addedAt = timestamp of first event where this sprint appears in `To` but not `From`
- removedAt = timestamp of first event where sprint appears in `From` but not `To`
- effectiveAddedAt = addedAt ?? sprint.StartDate
- planningCutoff = sprint.StartDate + PlanningWindowDays (default 2)
- WasCommitted = !forcedNotCommitted AND effectiveAddedAt <= planningCutoff

**Invariants:**
- RemovedAt != null means the ticket was actively taken out — removed tickets stay in the table for history
- Most SP calculations filter `RemovedAt == null`
- Null StoryPoints = excluded from SP sums, included in ticket counts

## Analytics Query Scope

**Key file:** `Fokus.Persistence/Repositories/SprintRepository.cs`

`GetAnalyticsSprintsAsync` returns sprints with `State == Active OR State == Closed` ordered by `StartDate` descending. Future sprints are excluded.

`GetAllAnalyticsSprintMembershipsAsync` applies the same Active/Closed filter.

**Closed-only guard rule:** All last-N averaging windows, sparklines, alert baselines, and streak counters operate exclusively on the closed-sprint subset. The active sprint is never included in trend/baseline calculations — only in single-sprint views and the selector dropdown.

Pattern used in every analytics endpoint:

```csharp
var allSprints = await sprintRepository.GetAnalyticsSprintsAsync(ct);
var ascending = allSprints.Where(s => s.State == SprintState.Closed).OrderBy(s => s.StartDate).ToList();
```

**Active-sprint prior-sprint resolution:** When the selected sprint is active (`closedIndex < 0`), the prior sprint is `ascending[^1]` (most recent closed sprint). Delta cards remain populated.

**Sparkline anchor for active sprint:** Use `ascending.Count - 1` as the anchor index so the full closed-sprint history feeds the sparkline window candidate set.

**Single-sprint lookup:** Always search `allSprints` (not `ascending`) when resolving the requested sprint ID. This allows active sprints to be viewed without a 400/404 error.
