# Developer & Capacity

## Developer

A Jira user who has been assigned to at least one ticket. ID is Jira accountId (never auto-generated).

**Key files:** `Fokus.Domain/Developer/Developer.cs`, `Fokus.Domain/Developer/Behaviors/Developer.cs`

**Properties:** Id (string, Jira accountId), DisplayName, AvatarUrl, SubTeam (string?), IsActive (bool, default true), Role (string, default "Developer"), DefaultCapacityPercent (int, default 100)

**Factory (Developer.FromJira):**
- Returns null if assignee is null (no developer record created)
- SubTeam, Role, DefaultCapacityPercent are NOT from Jira — manually managed in the app

**Invariants:**
- Only active developers appear in analytics results
- SubTeam is used for sub-team filtering across all analytics

## DeveloperSprintCapacity

Per-sprint capacity override for a developer.

**Key file:** `Fokus.Domain/Developer/DeveloperSprintCapacity.cs`

**Properties:** DeveloperAccountId + SprintId (composite key), CapacityPercent (int, default 100)

**Resolution:** If a DeveloperSprintCapacity record exists for developer+sprint, use its CapacityPercent. Otherwise, fall back to Developer.DefaultCapacityPercent.

**Exclusion rule (ExcludedDeveloperFilter):** A developer is excluded from sprint analytics if effective capacity == 0 AND they completed 0 tickets. A developer with 0% capacity who still completed a ticket is NOT excluded.

**Key file:** `Fokus.API/Features/Analytics/ExcludedDeveloperFilter.cs`
