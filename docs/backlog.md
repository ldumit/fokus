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

| F19 | Leaderboard Breakdown | Done | [LeaderboardBreakdown](features/LeaderboardBreakdown/spec.md) | [plan](plans/LeaderboardBreakdown/plan.md) |

**F19:** Dashboard leaderboard Features/Bugs toggle with SP + ticket count columns. New Leaderboard tab on Developers page with stacked bar chart (feature SP blue, bug SP red) and detailed table. Aligns Dashboard excluded-from-scope filtering with Bug Ratio pattern.

| F20 | Normalized Capacity Indicator | Done | [NormalizedCapacityIndicator](features/NormalizedCapacityIndicator/spec.md) | [Plan](plans/NormalizedCapacityIndicator/plan.md) |

**F20:** Bracketed normalized SP value (~X) next to SP completed for developers with <100% capacity. Shows estimated output at full availability. Applies to Throughput tab, Leaderboard tab, and Dashboard leaderboard. Client-side computation using existing capacity data; leaderboard API adds capacity to response.

| F21 | Feature-Only Delivery Metrics | Done | [FeatureOnlyMetrics](features/FeatureOnlyMetrics/spec.md) | [plan](plans/FeatureOnlyMetrics/plan.md) |

**F21:** Separates delivery metrics from bug metrics across Dashboard, burnup chart, and throughput table. SP Completed and Completion % become feature-only (with bug SP annotation on the card). Health score Completion sub-score uses feature-only completion %. Burnup chart scope/completed lines exclude bugs (red bug area unchanged). Throughput tab excludes bugs. Principle: delivery surfaces show features, bug surfaces show bugs, leaderboard bridges both.

| F22 | Boundary-Driven Completion | Done | [BoundaryDrivenCompletion](features/BoundaryDrivenCompletion/spec.md) | [BoundaryDrivenCompletion](plans/BoundaryDrivenCompletion/plan.md) |

**F22:** Completion across all analytics derived from cycle time end boundary instead of static done statuses list. Changing the cycle time end stage redefines what "completed" means everywhere — dev throughput (end at Testing), end-to-end (end at Done), or any workflow boundary. Done statuses setting retained but no longer drives completion. Affects F8, F9, F10, F11, F12, F13, F14, F21.

| F23 | Transition-Based Sprint Scope | Done | [TransitionBasedSprintScope](features/TransitionBasedSprintScope/spec.md) | [TransitionBasedSprintScope](plans/TransitionBasedSprintScope/plan.md) |

**F23:** Sprint scope attribution via StatusTransition timestamps instead of snapshot-based WasCommitted/FinalStatus. Both cycle time boundaries drive all metrics: CycleTimeStartStage defines active/committed, CycleTimeEndStage defines completed. No carry-over double-counting. All scope surfaces feature-only with separate bug bars. Supersedes F22 completion mechanism. Affects F8, F9, F10, F11, F12, F13, F14.

| F24 | Planning-Gated Disruption | Done | [PlanningGatedDisruption](features/PlanningGatedDisruption/spec.md) | [PlanningGatedDisruption](plans/PlanningGatedDisruption/plan.md) |

**F24:** Aligns Committed SP cards and burnup chart into a coherent system. Total = membership at planning close (not activeSp + removedSp). Added/Removed SP gated by planning window + cycle entry — only post-planning, cycle-entered activity counts as disruption. Planning Overflow classification removed. Dashboard disruption rates and mid-sprint flag updated to use planningCutoff. Affects F8, F10.

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

## v2 — QA Analytics (Xray Integration)

**Spec:** [v2.md](product/v2.md)  
**Research:** [Xray QA Integration Research](proposals/xray-qa-integration-research.md)

Extends Fokus with Xray Cloud test management data. Enriches sprint tickets with test coverage and execution results. Gated behind a feature flag — fully optional.

### Sequencing

```
Tier 9: F25 (Xray Foundation + Ticket Enrichment)
          │
    ┌─────┼──────────┬──────────┐
Tier 10: F26  F28  F29  F30  F31
          │
Tier 11: F27
```

### Tier 9 — QA Foundation

| ID | Feature | Status | Spec | Plan |
|----|---------|--------|------|------|
| F25 | Xray Integration & Ticket Test Enrichment | Done | [XrayIntegration](specs/F25-XrayIntegration/definition/spec.md) | [plan](specs/F25-XrayIntegration/delivery/plan.md) |

Xray client (GraphQL + auth), domain model (TestExecution, TestExecutionLink, TestRun, TestSet), EF Core migrations, Settings UI (feature flag, credentials, connection test), dual-API sync (Jira issue links + Xray test runs), per-ticket test coverage status and pass/fail enrichment.

---

### Tier 10 — QA Analytics & Dashboard

| ID | Feature | Status | Spec | Plan |
|----|---------|--------|------|------|
| F26 | Sprint Test Coverage | Done | [SprintTestCoverage](specs/F26-SprintTestCoverage/definition/spec.md) | [plan](specs/F26-SprintTestCoverage/delivery/plan.md) |
| F28 | Per-Developer Story Quality | Done | [PerDeveloperStoryQuality](specs/F28-PerDeveloperStoryQuality/definition/spec.md) | [plan](specs/F28-PerDeveloperStoryQuality/delivery/plan.md) |
| F29 | QA Workload & Throughput | Done | [QaWorkloadThroughput](specs/F29-QaWorkloadThroughput/definition/spec.md) | [plan](specs/F29-QaWorkloadThroughput/delivery/plan.md) |
| F30 | Test Execution Timeline | Done | [TestExecutionTimeline](specs/F30-TestExecutionTimeline/definition/spec.md) | [plan](specs/F30-TestExecutionTimeline/delivery/plan.md) |
| F31 | Epic Test Health | Done | [EpicTestHealth](specs/F31-EpicTestHealth/definition/spec.md) | [plan](specs/F31-EpicTestHealth/delivery/plan.md) |

**F26:** Coverage %, Execution %, Pass Rate % cards on Dashboard. Quality sub-score in sprint health score. Untested/failing ticket lists. Configurable thresholds.

**F28:** Per-developer test coverage of their stories. Coverage %, pass rate, bugs found. Extension to Developers page.

**F29:** QA Workload tab on Developers page (4th tab). Per-person TE ownership, run throughput, pass/fail split, stories covered, bugs found. Workload distribution chart + throughput trend line. Balance flag when one person handles >50% of executions for 2+ consecutive sprints.

**F30:** Test execution burnup chart on Sprints page single-sprint detail. Testing crunch flag (>50% runs in last 2 days) on Dashboard + Sprints page. Post-sprint testing indicator. Completed-but-untested tickets list. Dev-done-to-tested gap metric (median days, delta). Scope disruption correlation overlay on burnup chart.

**F31:** Bottom-up epic test health from stories → TEs. Coverage %, pass rate, bugs found per epic. Extension to Epics page.

---

### Tier 11 — QA Trends

| ID | Feature | Status | Spec | Plan |
|----|---------|--------|------|------|
| F27 | Cross-Sprint QA Trends | Done | [CrossSprintQaTrends](specs/F27-CrossSprintQaTrends/definition/spec.md) | [plan](specs/F27-CrossSprintQaTrends/delivery/plan.md) |

**F27:** Dedicated QA sidebar page with cross-sprint trend charts. Quality Trends panel (coverage rate, pass rate, execution rate as percentage lines). Testing Volume panel (TE count, bugs found as grouped bars). Defect Correlation section with N+1 lag (coverage in sprint N vs bug ratio in sprint N+1) and Pearson r badge. Default last 10 sprints. Sub-team filterable.

---

### Tier 12 — Active Sprint Analytics

| ID | Feature | Status | Spec | Plan |
|----|---------|--------|------|------|
| F32 | Daily Developer Progress | Done | [DailyDeveloperProgress](specs/F32-DailyDeveloperProgress/definition/spec.md) | [plan](specs/F32-DailyDeveloperProgress/delivery/plan.md) |

**F32:** Per-developer burnup cards on a new "Daily Progress" tab on the Developers page. Active sprint only. Each developer card shows a mini burnup chart (actual vs expected pace line), SP completed/assigned (all ticket types), and stall detection (tickets with no status transition in 2+ business days). Alert banner flags developers behind pace. Pace-based alerting with 2-day grace period. Sub-team filterable. Auto-refresh via SignalR (requires building initial real-time infrastructure).

| F33 | Daily Progress Enhancements | Done | [DailyProgressEnhancements](specs/F33-DailyProgressEnhancements/definition/spec.md) | [Plan](specs/F33-DailyProgressEnhancements/delivery/plan.md) |
| F34 | Developer Detail Page | Spec Ready | [DeveloperDetailPage](specs/F34-DeveloperDetailPage/definition/spec.md) | — |

**F33:** Bug/feature SP breakdown on progress cards, delta-based pace change alerting (replacing static "behind pace" banner with worsening/improving signals), chart rendering fixes (Y-axis orientation, increased height, zoom disabled). Extends F32.

**F34:** Individual developer drill-down page accessible from Daily Progress cards. Cross-sprint velocity trends (stacked feature/bug bar chart, completion % trend, rolling average), work type allocation (bug % per sprint with configurable target threshold), current sprint detail (larger burnup chart, ticket table grouped by state). Configurable bug ratio target in Settings.

---

### v2 Cross-Cutting Concerns

| ID | Concern | Description |
|----|---------|-------------|
| C4 | Xray Feature Flag | Boolean setting gates all QA UI and Xray API calls. When off, Fokus behaves exactly as v1. |
| C5 | Dual-API Sync | Sprint sync reads Jira issue links (coverage graph + bug links), then Xray GraphQL API (test run results). Graceful degradation if Xray is unreachable. |
| C6 | QA Delta Pattern | Same as C1 — QA metric endpoints return `{ value, delta, direction }` vs. prior sprint. |

---

## Bugs & Gaps

New issues use `docs/specs/{slug}/definition/` (bug.md for bugs, spec.md for gaps — see agents-workflow.md § Slug and Path Resolution). Existing issues remain at legacy `docs/issues/` paths until migrated.

| ID | Issue | Type | Severity | Status | Feature | File |
|----|-------|------|----------|--------|---------|------|
| GAP-1 | Excluded from Scope Statuses UI | Gap | High | Open | F10 | [GAP-1](issues/GAP-1-excluded-statuses-ui.md) |
| GAP-2 | Sprint Range Picker for Sync | Gap | Medium | Open | F5 | [GAP-2](issues/GAP-2-sprint-range-picker.md) |
| GAP-3 | Sub-Team Management UI | Gap | Medium | Open | F3 | [GAP-3](issues/GAP-3-sub-team-management-ui.md) |
| GAP-4 | Sprint Cards Total-Scope vs Burnup Feature-Only | Gap | Medium | Open | F21 | [GAP-4](issues/GAP-4-sprint-cards-scope-mismatch.md) |
| BUG-1 | Bug Count = 0 on Sprint 26 | Bug | High | Open | F10 | [BUG-1](issues/BUG-1-bug-count-zero.md) |
| BUG-2 | Bar Chart vs Burnup Completed SP Disagree (63 SP gap) | Bug | High | Open | F10/F21 | [BUG-2](issues/BUG-2-barchart-burnup-completed-sp-mismatch.md) |

---

## Tech Debt

| ID | Item | Priority | Notes |
|----|------|----------|-------|
| TD-1 | Cookie `SecurePolicy.Always` for production | High | Currently `SameAsRequest` — cookies not marked Secure over HTTP. Must be `Always` before Azure deployment behind HTTPS. |
| TD-2 | Google OAuth options pattern | Medium | `AuthConfiguration` reads `Google:ClientId`/`ClientSecret` via `configuration[]` + inline `Bind`. Should use a typed `GoogleAuthOptions` class with `ValidateDataAnnotations().ValidateOnStart()` like `JiraOptions`. |

---

## Cross-Cutting Concerns

These are not standalone features. Each analytics feature (F8–F14) must implement them.

| ID | Concern | Description |
|----|---------|-------------|
| C1 | Delta Pattern | Every metric endpoint returns `{ value, delta, direction }` vs. prior sprint. UI shows ↑/↓ with signed number, colored green/red based on metric polarity. |
| C2 | Sub-Team Filtering | All analytics endpoints accept `?subTeam=X`. Header filter drives it. Developer sub-team managed in Settings (F3). |
| C3 | Multi-Sprint Selection | Sprint selector: single sprint, "Last 3", "Last 5", "All". Endpoints accept `?sprintIds=1,2,3` or `?last=N`. |
