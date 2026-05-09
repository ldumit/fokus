# Epic Progress

Dual tracking (ticket count + story points) with velocity and projected completion.

**Key file:** `Fokus.API/Features/Analytics/EpicProgressService.cs`
**Endpoint:** `GET /analytics/epic-progress?subTeam`
**Store:** `client/src/stores/epicsStore.ts`
**View:** `client/src/views/EpicsView.vue`

## Per-Epic Metrics

All SP sums use `GetEffectiveTicketSp(ticket, defaultSpPerBug)` (local helper on `EpicProgressService`): returns `StoryPoints` if non-null and > 0, else `defaultSpPerBug` if IssueType == "Bug" and default > 0, else null. Velocity uses `SprintMembership.GetEffectiveSp(defaultSpPerBug)`. Configured via `AppSettings.DefaultSpPerBug` (default 3).

```
doneTickets          = tickets where CurrentStatus IN doneStatuses
ticketCompletionPct  = doneCount / totalCount * 100
doneSp               = sum(effectiveSP) for done tickets
remainingSp          = sum(effectiveSP) for remaining tickets
```

**Uses Ticket.CurrentStatus, NOT FinalStatus.** Epic progress reflects current reality, not historical sprint snapshots.

An "unestimated" ticket is one where `GetEffectiveTicketSp` returns null (no own SP, and either not a Bug or DefaultSpPerBug is 0).

## Imputed Story Points

Unestimated tickets receive imputed SP = average effective SP of estimated tickets in the same epic. Only applied to REMAINING unestimated tickets.

```
avgSp           = average SP of estimated tickets in this epic
imputedSp       = avgSp * remainingUnestimatedCount
adjustedTotalSp = totalSp + imputedSp
spCompletionPct = doneSp / adjustedTotalSp * 100
```

Imputed SP are never used in velocity calculations. When an epic has zero estimated tickets, SP metrics are unavailable — ticket-count-only.

## Velocity (rolling 3-sprint)

- Groups closed sprint memberships for epic by sprint
- Sums completed SP per sprint
- Excludes sprints with zero completed SP
- Takes up to 3 most recent sprints by StartDate desc
- Velocity = average completed SP across those sprints

## Projection

```
remainingWork             = remainingSp + imputedSp
projectedSprintsRemaining = remainingWork / velocity
```

Confidence = "low" if fewer than 3 data points; null otherwise.

## Sort and Display

- Active epics: sorted by SP completion % ascending (least done first, nulls last)
- Completed epics: sorted by name ascending
- Epic is complete when ALL tickets have done status
- Summary average completion: weighted by adjustedTotalSp

## Special

- No sprint selector — epic progress is always cross-sprint
- Unlinked work: tickets with no EpicKey counted separately
- Client-side filtering: active vs completed toggle uses `isCompleted` flag
