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

| ID | Feature | Status | Spec | Plan |
|----|---------|--------|------|------|
| F1 | Project Scaffolding | Done | — | [Scaffolding](plans/Scaffolding/plan.md) |

.NET solution (API/Domain/Persistence three-project split), Vue SPA with Vite + Tailwind + ApexCharts + Pinia, Vite build → wwwroot, dev proxy setup.

---

### Tier 1 — Core Structure

| ID | Feature | Status | Spec | Plan |
|----|---------|--------|------|------|
| F2 | Domain Model & Persistence | Done | — | — |
| F7 | App Shell & Navigation | Done | [AppShell](features/AppShell/spec.md) | [AppShell](plans/AppShell/plan.md) |

**F2:** All 5 entities (Sprint, Developer, Ticket, SprintMembership, StatusTransition), EF Core configs, composite keys, indexes, SQLite setup, initial migration.

**F7:** App shell (sidebar nav, theme toggle, design tokens, card-based layout convention), page-level toolbar with sprint selector and sub-team filter (visual only — data-wiring in F8), empty states, responsive layout (1440px primary, 1366px laptop floor, sidebar auto-collapse for split-screen).

---

### Tier 2 — Configuration & Jira Client

| ID | Feature | Status | Spec | Plan |
|----|---------|--------|------|------|
| F3 | Settings System | Done | — | — |
| F4 | Jira Integration | Done | [JiraSync](features/JiraSync/spec.md) | [JiraSync](plans/JiraSync/plan.md) |

**F3:** AppSettings entity (single-row, typed, JSON columns for complex values). CRUD endpoints. Settings UI: board selector, done statuses, health thresholds + weights, workflow stages. Developer sub-team tagging lives here.

**F4:** JiraClient (typed HttpClient), Basic Auth (User Secrets dev / Key Vault prod), Jira DTOs (sprint, issue, changelog, board), JiraMapper (DTOs → domain entities), 10 req/s throttle + 429 backoff.

---

### Tier 3 — Data Pipeline

| ID | Feature | Status | Spec | Plan |
|----|---------|--------|------|------|
| F5 | Sprint Sync | Done | [JiraSync](features/JiraSync/spec.md) | [JiraSync](plans/JiraSync/plan.md) |

SyncSprint endpoint (single sprint) + SyncAllSprints (all closed sprints for configured board). Full flow: fetch from Jira → map → upsert entities → compute WasCommitted. Idempotent (re-sync overwrites). Active sprints allowed but flagged. Returns sync summary with counts.

---

### Tier 4 — Analytics & Dashboard

| ID | Feature | Status | Spec | Plan |
|----|---------|--------|------|------|
| F6 | Workflow Auto-Detection | Done | [WorkflowAutoDetection](features/WorkflowAutoDetection/spec.md) | [WorkflowAutoDetection](plans/WorkflowAutoDetection/plan.md) |
| F8 | Sprint Summary Card | Done | [SprintSummaryCard](features/SprintSummaryCard/spec.md) | [SprintSummaryCard](plans/SprintSummaryCard/plan.md) |
| F9 | Developer Throughput | Done | [DeveloperThroughput](features/DeveloperThroughput/spec.md) | [DeveloperThroughput](plans/DeveloperThroughput/plan.md) |
| F10 | Scope Change & Disruption | Done | [ScopeChangeDisruption](features/ScopeChangeDisruption/spec.md) | [ScopeChangeDisruption](plans/ScopeChangeDisruption/plan.md) |
| F11 | Carry-Over Tracker | Done | [CarryOverTracker](features/CarryOverTracker/spec.md) | [CarryOverTracker](plans/CarryOverTracker/plan.md) |
| F13 | Bug Ratio | Done | [BugRatio](features/BugRatio/spec.md) | [BugRatio](plans/BugRatio/plan.md) |
| F14 | Epic Progress | Done | [EpicProgress](features/EpicProgress/spec.md) | [EpicProgress](plans/EpicProgress/plan.md) |

**F6:** Analyze StatusTransition data after sync, build directed transition graph weighted by frequency, topological sort → suggested workflow stage order. Present in Settings UI for confirmation. Triggered when no WorkflowStages configured.

**F8:** Landing page (Dashboard view). Composite health score (0-100, RAG color). 4 metric cards: SP completed/committed, completion %, disruption rate, carry-over rate. Each card: big number + delta vs prior sprint + 4-sprint sparkline. Top 3 epics progressed. Developer leaderboard. Flags (zombie tickets, high-disruption days, 0 SP devs).

**F9:** Per-developer table: SP assigned, SP completed, completion %, tickets done, tickets carried over. Multi-line chart across N sprints. 3-sprint rolling average. Sub-team filterable. Delta indicators on all metrics.

**F10:** Committed/added/removed SP per sprint. Disruption rate (added/committed × 100). Disruption classification: unplanned bug, scope injection, priority escalation, net-zero swap. Stacked bar chart, disruption timeline (which day items added), disruption rate trend line, breakdown table by category.

**F11:** Carry-over rate trend (line chart). Per-sprint pie chart of carry-over status distribution. Zombie tickets table (3+ sprints with full sprint history).

**F13:** Per-developer stacked bar: features (blue) vs bugs (red) per sprint. Team-level bug SP % trend. Alert flag when developer bug ratio > 50% for 2+ consecutive sprints.

**F14:** Per epic: total/done/remaining tickets + SP, completion % (by ticket count and SP), active sprints, epic velocity (SP/sprint, last 3), projected sprints to completion. Expandable rows with individual tickets.

---

### Tier 5 — Advanced Analytics

| ID | Feature | Status | Spec | Plan |
|----|---------|--------|------|------|
| F12 | Cycle Time | Done | [CycleTime](features/CycleTime/spec.md) | [CycleTime](plans/CycleTime/plan.md) |

Multi-stage cycle time. Configurable workflow boundaries (depends on F3 settings + F6 auto-detection). Per-ticket duration, then aggregated: avg, median, p90 per sprint/developer/issue type. Stage funnel chart (stacked horizontal bar, widest = bottleneck). Box plot per sprint. Outlier flags (2x+ sprint median). Percentile toggle (p50/p75/p90).

---

### Tier 6 — Team Configuration

| ID | Feature | Status | Spec | Plan |
|----|---------|--------|------|------|
| F15 | Team Management | Done | [TeamManagement](features/TeamManagement/spec.md) | [TeamManagement](plans/TeamManagement/plan.md) |

Dedicated "Team" page in sidebar. Configure developer roles, default capacity %, sub-team assignment, active toggle. Lazy capacity fallback (default used when no sprint override exists). Cross-cutting exclusion rule: 0% capacity + 0 completed tickets = hidden from all analytics. Resolves GAP-3 (sub-team management UI).

---

### Tier 7 — Cross-Cutting Enhancements

| ID | Feature | Status | Spec | Plan |
|----|---------|--------|------|------|
| F16 | Bug Cost & Disruption Split | Done | [BugCostDisruption](features/BugCostDisruption/spec.md) | [BugCostDisruption](plans/BugCostDisruption/plan.md) |
| F17 | Burnup Bug Overlay | Spec Ready | [BurnupBugOverlay](features/BurnupBugOverlay/spec.md) | — |

**F16:** Configurable default SP per bug (fallback for unestimated bugs, applied system-wide). Dashboard disruption rate split into two cards: Scope Disruption Rate + Bug Disruption Rate. Health score unchanged (uses combined total).

**F17:** Semi-transparent red shaded area on the single-sprint Scope Burnup chart showing cumulative bug SP per day. Visualizes when bugs appeared and how much sprint capacity they consumed. Depends on F16 for default SP per bug.

---

### Tier 8 — Security & Access

| ID | Feature | Status | Spec | Plan |
|----|---------|--------|------|------|
| F18 | Authentication & Access Control | Done | [Auth](features/Auth/spec.md) | [Auth](plans/Auth/plan.md) |

**F18:** Google OAuth with invitation-based access. Company domain restriction. Two roles: Admin (full control) and Manager (view-only). First login bootstraps Admin. Invite link flow (no automated email). Settings read-only for Managers. All existing endpoints require auth.

---

## Internal Improvements

Refactors, infrastructure work, and UX tweaks that don't have a feature spec.

| ID | Improvement | Status | Plan |
|----|------------|--------|------|
| I1 | Skill Cleanup | Planned | [SkillCleanup](plans/SkillCleanup/plan.md) |
| I2 | Skill Enforcement | Planned | [SkillEnforcement](plans/SkillEnforcement/plan.md) |
| I3 | Skill Alignment (BuildingBlocks & Entities) | Done | [SkillAlignment](plans/SkillAlignment/plan.md) |
| I4 | Skill Alignment Phase 2 (Endpoints) | Planned | [SkillAlignmentEndpoints](plans/SkillAlignmentEndpoints/plan.md) |
| I5 | Extract Jira Module | Done | [ExtractJiraModule](plans/ExtractJiraModule/plan.md) |
| I6 | Jira Module Refactor | Done | [JiraModuleRefactor](plans/JiraModuleRefactor/plan.md) |
| I7 | Sync Refactoring | Done | [SyncRefactoring](plans/SyncRefactoring/plan.md) |
| I8 | Settings Dropdowns | Planned | [SettingsDropdowns](plans/SettingsDropdowns/plan.md) |
| I9 | Team-Managed Sync Fix | Done | [TeamManagedSync](plans/TeamManagedSync/plan.md) |
| I10 | Tooltip Wiring | Done | [TooltipWiring](plans/TooltipWiring/plan.md) |
| I11 | Settings Gap Fill (GAP-1 + GAP-2 + GAP-3) | Done | [SettingsGapFill](plans/SettingsGapFill/plan.md) |

---

## Bugs & Gaps

Tracked in `docs/issues/`. One file per issue. Gaps are specified features with missing UI or functionality. Bugs are incorrect behavior.

| ID | Issue | Type | Severity | Status | Feature | File |
|----|-------|------|----------|--------|---------|------|
| GAP-1 | Excluded from Scope Statuses UI | Gap | High | Open | F10 | [GAP-1](issues/GAP-1-excluded-statuses-ui.md) |
| GAP-2 | Sprint Range Picker for Sync | Gap | Medium | Open | F5 | [GAP-2](issues/GAP-2-sprint-range-picker.md) |
| GAP-3 | Sub-Team Management UI | Gap | Medium | Open | F3 | [GAP-3](issues/GAP-3-sub-team-management-ui.md) |
| BUG-1 | Bug Count = 0 on Sprint 26 | Bug | High | Open | F10 | [BUG-1](issues/BUG-1-bug-count-zero.md) |

---

## Cross-Cutting Concerns

These are not standalone features. Each analytics feature (F8–F14) must implement them.

| ID | Concern | Description |
|----|---------|-------------|
| C1 | Delta Pattern | Every metric endpoint returns `{ value, delta, direction }` vs. prior sprint. UI shows ↑/↓ with signed number, colored green/red based on metric polarity. |
| C2 | Sub-Team Filtering | All analytics endpoints accept `?subTeam=X`. Header filter drives it. Developer sub-team managed in Settings (F3). |
| C3 | Multi-Sprint Selection | Sprint selector: single sprint, "Last 3", "Last 5", "All". Endpoints accept `?sprintIds=1,2,3` or `?last=N`. |
