# Fokus — Backlog

**Owner:** Laurentiu
**Last updated:** May 5, 2026

---

## Sequencing

Features are ordered by dependency. Each tier must complete before the next tier's features can start, but features within a tier can parallelize.

```
Tier 0: F1 (Scaffolding)
         │
    ┌────┴─────┐
Tier 1: F2       F7
    (Domain)   (App Shell)
         │
Tier 2: F3 ─── F4
    (Settings) (Jira)
              │
Tier 3:      F5
          (Sync)
           │
    ┌──────┼──────────┐
Tier 4: F6  F8  F9  F10  F11  F13  F14
         │
Tier 5: F12
    (Cycle Time — needs F6 workflow stages)
```

---

## Features

### Tier 0 — Foundation

| ID | Feature | Status | Spec |
|----|---------|--------|------|
| F1 | Project Scaffolding | Done | — |

.NET solution (API/Domain/Persistence three-project split), Vue SPA with Vite + Tailwind + ApexCharts + Pinia, Vite build → wwwroot, dev proxy setup.

---

### Tier 1 — Core Structure

| ID | Feature | Status | Spec |
|----|---------|--------|------|
| F2 | Domain Model & Persistence | Done | — |
| F7 | App Shell & Navigation | Spec Ready | [AppShell](../features/AppShell.md) |

**F2:** All 5 entities (Sprint, Developer, Ticket, SprintMembership, StatusTransition), EF Core configs, composite keys, indexes, SQLite setup, initial migration.

**F7:** App shell (sidebar nav, theme toggle, design tokens, card-based layout convention), page-level toolbar with sprint selector and sub-team filter (visual only — data-wiring in F8), empty states, responsive layout (1440px primary, 1366px laptop floor, sidebar auto-collapse for split-screen).

---

### Tier 2 — Configuration & Jira Client

| ID | Feature | Status | Spec |
|----|---------|--------|------|
| F3 | Settings System | Done | — |
| F4 | Jira Integration | Done | [JiraSync](../features/JiraSync.md) |

**F3:** AppSettings entity (single-row, typed, JSON columns for complex values). CRUD endpoints. Settings UI: board selector, done statuses, health thresholds + weights, workflow stages. Developer sub-team tagging lives here.

**F4:** JiraClient (typed HttpClient), Basic Auth (User Secrets dev / Key Vault prod), Jira DTOs (sprint, issue, changelog, board), JiraMapper (DTOs → domain entities), 10 req/s throttle + 429 backoff.

---

### Tier 3 — Data Pipeline

| ID | Feature | Status | Spec |
|----|---------|--------|------|
| F5 | Sprint Sync | Done | [JiraSync](../features/JiraSync.md) |

SyncSprint endpoint (single sprint) + SyncAllSprints (all closed sprints for configured board). Full flow: fetch from Jira → map → upsert entities → compute WasCommitted. Idempotent (re-sync overwrites). Active sprints allowed but flagged. Returns sync summary with counts.

---

### Tier 4 — Analytics & Dashboard

| ID | Feature | Status | Spec |
|----|---------|--------|------|
| F6 | Workflow Auto-Detection | Done | [WorkflowAutoDetection](../features/WorkflowAutoDetection.md) |
| F8 | Sprint Summary Card | Done | [SprintSummaryCard](../features/SprintSummaryCard.md) |
| F9 | Developer Throughput | Done | [DeveloperThroughput](../features/DeveloperThroughput.md) |
| F10 | Scope Change & Disruption | Done | [ScopeChangeDisruption](../features/ScopeChangeDisruption.md) |
| F11 | Carry-Over Tracker | Spec Ready | [CarryOverTracker](../features/CarryOverTracker.md) |
| F13 | Bug Ratio | Not Started | — |
| F14 | Epic Progress | Not Started | — |

**F6:** Analyze StatusTransition data after sync, build directed transition graph weighted by frequency, topological sort → suggested workflow stage order. Present in Settings UI for confirmation. Triggered when no WorkflowStages configured.

**F8:** Landing page (Dashboard view). Composite health score (0-100, RAG color). 4 metric cards: SP completed/committed, completion %, disruption rate, carry-over rate. Each card: big number + delta vs prior sprint + 4-sprint sparkline. Top 3 epics progressed. Developer leaderboard. Flags (zombie tickets, high-disruption days, 0 SP devs).

**F9:** Per-developer table: SP assigned, SP completed, completion %, tickets done, tickets carried over. Multi-line chart across N sprints. 3-sprint rolling average. Sub-team filterable. Delta indicators on all metrics.

**F10:** Committed/added/removed SP per sprint. Disruption rate (added/committed × 100). Disruption classification: unplanned bug, scope injection, priority escalation, net-zero swap. Stacked bar chart, disruption timeline (which day items added), disruption rate trend line, breakdown table by category.

**F11:** Carry-over rate trend (line chart). Per-sprint pie chart of carry-over status distribution. Zombie tickets table (3+ sprints with full sprint history).

**F13:** Per-developer stacked bar: features (blue) vs bugs (red) per sprint. Team-level bug SP % trend. Alert flag when developer bug ratio > 50% for 2+ consecutive sprints.

**F14:** Per epic: total/done/remaining tickets + SP, completion % (by ticket count and SP), active sprints, epic velocity (SP/sprint, last 3), projected sprints to completion. Expandable rows with individual tickets.

---

### Tier 5 — Advanced Analytics

| ID | Feature | Status | Spec |
|----|---------|--------|------|
| F12 | Cycle Time | Not Started | — |

Multi-stage cycle time. Configurable workflow boundaries (depends on F3 settings + F6 auto-detection). Per-ticket duration, then aggregated: avg, median, p90 per sprint/developer/issue type. Stage funnel chart (stacked horizontal bar, widest = bottleneck). Box plot per sprint. Outlier flags (2x+ sprint median). Percentile toggle (p50/p75/p90).

---

## Cross-Cutting Concerns

These are not standalone features. Each analytics feature (F8–F14) must implement them.

| ID | Concern | Description |
|----|---------|-------------|
| C1 | Delta Pattern | Every metric endpoint returns `{ value, delta, direction }` vs. prior sprint. UI shows ↑/↓ with signed number, colored green/red based on metric polarity. |
| C2 | Sub-Team Filtering | All analytics endpoints accept `?subTeam=X`. Header filter drives it. Developer sub-team managed in Settings (F3). |
| C3 | Multi-Sprint Selection | Sprint selector: single sprint, "Last 3", "Last 5", "All". Endpoints accept `?sprintIds=1,2,3` or `?last=N`. |
