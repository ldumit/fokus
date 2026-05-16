# Knowledge Base

Navigate here before reading source files. Find the relevant entry, get business rules and key files, then read only what you need.

## Domain Model

- [Sprint & Membership](domain/sprint.md) — sprint lifecycle, commitment logic, planning window
- [Ticket & Transitions](domain/ticket.md) — issue types, status transitions, epic linkage
- [Developer & Capacity](domain/developer.md) — sub-teams, capacity model, exclusion rules
- [Settings](domain/settings.md) — health thresholds, weights, workflow stages, done statuses

## Analytics

- [Health Score](analytics/health-score.md) — composite score, RAG thresholds, per-metric scoring
- [Scope Change](analytics/scope-change.md) — disruption rate, classification, burnup chart
- [Carry-Over](analytics/carry-over.md) — carry-over rate, zombie tickets, destination tracking
- [Bug Ratio](analytics/bug-ratio.md) — bug vs non-bug SP, alert system
- [Cycle Time](analytics/cycle-time.md) — stage durations, percentiles, outliers, rework
- [Throughput](analytics/throughput.md) — per-developer SP, rolling averages, capacity
- [Epic Progress](analytics/epic-progress.md) — dual tracking, imputed SP, velocity, projections
- [Leaderboard](analytics/leaderboard.md) — feature vs bug SP breakdown, delta polarity, dashboard toggle

## Cross-Cutting

- [Shared Rules](cross-cutting.md) — excluded statuses, sub-team filter, delta pattern, developer exclusion

## Integration

- [Jira Sync](jira-sync.md) — sync flow, changelog processing, membership construction
- [Xray](xray.md) — sync flow, link types, sprint attribution (BR7/BR8/BR9), coverage computation
- [Frontend Map](frontend-map.md) — view/store/API mapping, data flow
