# Ticket & Status Transitions

## Ticket

A Jira issue. ID is the Jira issue key (e.g. "FOK-123"), never auto-generated.

**Key files:** `Fokus.Domain/Ticket/Ticket.cs`, `Fokus.Domain/Ticket/Behaviors/Ticket.cs`

**Properties:** Id (string, Jira key), Summary, IssueType, Priority, CurrentStatus, StoryPoints (decimal?), EpicKey (string?), EpicName (string?), AssigneeId (string?, FK to Developer), CreatedDate, ResolvedDate

**Factory (Ticket.FromJira):**
- IssueType defaults to "Unknown" if null
- EpicKey: Fields.EpicKey first; fallback to Parent.Key if parent is Epic
- EpicName: Fields.EpicName first; fallback to Parent.Fields.Summary if parent is Epic
- Priority defaults to "Medium"; CurrentStatus defaults to "Unknown"

**Bug classification:** `IssueType == "Bug"` (exact match, case-sensitive). Used by BugRatioService and ScopeChangeService classification.

**Epic linkage:** Tickets with the same EpicKey belong to the same epic. Epic progress uses `Ticket.CurrentStatus` (current reality), NOT sprint membership FinalStatus (sprint snapshot).

## StatusTransition

A status change event on a ticket, extracted from Jira changelog.

**Key files:** `Fokus.Domain/Ticket/StatusTransition.cs`, `Fokus.Domain/Ticket/Behaviors/StatusTransition.cs`

**Properties:** Id, TicketId (FK), FromStatus, ToStatus, Timestamp, AuthorId (string?)

**Factory (StatusTransition.ListFromJira):**
- Iterates changelog histories ordered by Created ascending
- Only processes items where `Field == "status"` (exact match)
- Returns empty list if no changelog

**Used by:** CycleTimeService (stage duration calculation), ScopeChangeService (bug time-in-progress, burnup completion dates)
