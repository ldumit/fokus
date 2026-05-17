# Domain Glossary

Canonical vocabulary for the Fokus domain. Read this before naming new types, writing specs, or describing features. Each entry is the **authoritative name** — use it in code, specs, plans, and UI labels.

For business rules, computation logic, and edge cases, see the relevant [KB entry](index.md).

## How to Use

- **Bold term** = canonical name. Use it exactly.
- **Avoid** = synonyms that cause confusion. Don't use them.
- **Code** = the type/property name in the codebase. New code must match.
- Terms are grouped by domain area. Cross-cutting terms appear once in their primary area.

---

## Core Entities

**Sprint** — A Jira sprint synced into Fokus; the primary unit of work organization and analytics scope. Code: `Sprint` aggregate. Avoid: "iteration", "timebox."

**Sprint State** — Lifecycle phase: Active, Closed, or Future. Mapped from Jira lowercase strings. Code: enum on `Sprint`.

**Ticket** — A Jira issue tracked in Fokus, identified by its Jira issue key. Code: `Ticket` aggregate. Avoid: "issue" as the primary domain term (Jira uses "issue"; Fokus uses "Ticket").

**Sprint Membership** — The junction linking a Ticket to a Sprint; the central analytics data structure. Code: `SprintMembership`. Avoid: "sprint ticket", "sprint item."

**Developer** — A Jira user assigned to at least one ticket. Code: `Developer` aggregate. Avoid: "user", "assignee" (those are Jira terms).

**Epic** — A Jira parent grouping of tickets. No separate aggregate — derived from `Ticket.EpicKey` / `Ticket.EpicName`. Avoid: "initiative", "theme."

**Sub-Team** — Optional grouping label on Developer for filtering analytics. Manually managed, not from Jira. Code: `Developer.SubTeam`.

---

## Ticket Classification

**Bug** — A ticket where `IssueType == "Bug"` (case-sensitive exact match). Avoid: "defect."

**Feature** — Any ticket that is NOT a Bug. The default unit of planned work. Avoid: "story", "user story."

**Unlinked Work** — Tickets with no EpicKey. Counted separately in epic progress.

---

## Story Points & Estimation

**Story Points (SP)** — Effort estimate on a ticket. Decimal, nullable. Null = unestimated. Code: `SprintMembership.StoryPoints`, `Ticket.StoryPoints`.

**Effective SP** — Resolved SP value: actual StoryPoints if set, else DefaultSpPerBug for bugs, else null. Code: `GetEffectiveSp(defaultSpPerBug)`. Avoid: confusing with raw StoryPoints.

**Imputed Story Points** — Estimated SP for remaining unestimated epic tickets, calculated as the average SP of estimated tickets in the same epic.

**DefaultSpPerBug** — System-wide fallback SP for unestimated bugs (default 3; 0 disables). Code: `AppSettings.DefaultSpPerBug`.

---

## Sprint Scope & Timing

**WasCommitted** — Boolean on SprintMembership: was the ticket present at planning time? Code: `SprintMembership.WasCommitted`.

**AddedAt** — Timestamp when a ticket entered the sprint (from Jira changelog). Code: `SprintMembership.AddedAt`.

**RemovedAt** — Timestamp when a ticket was removed from the sprint. Null = still in sprint. Code: `SprintMembership.RemovedAt`.

**Planning Window Days** — Days after sprint start during which additions aren't disruption (default 2). Code: `AppSettings.PlanningWindowDays`.

**Planning Cutoff** — Computed deadline: `sprint.StartDate + PlanningWindowDays`. Additions after this = disruption.

---

## Status & Transitions

**Status Transition** — A single status-change event from Jira changelog. Code: `StatusTransition`. Avoid: confusing with FinalStatus (snapshot) or CurrentStatus (live).

**Current Status** — Live Jira status at last sync. Used in epic progress. Code: `Ticket.CurrentStatus`.

**Final Status** — Jira status snapshot on SprintMembership. Legacy — scope attribution now uses transitions. Code: `SprintMembership.FinalStatus`.

**Workflow Stages** — Ordered list of intermediate status names defining the team's pipeline. Code: `AppSettings.WorkflowStages`.

**Done Statuses** — Ordered list of completion status names, appended after WorkflowStages. Code: `AppSettings.DoneStatuses`.

**Ordered Stage Sequence** — Full ordered list: `WorkflowStages ++ DoneStatuses`. Used for all transition-based attribution.

**CycleTime Start Stage** — Configurable stage marking the start of cycle time measurement. Code: `AppSettings.CycleTimeStartStage`.

**CycleTime End Stage** — Configurable stage marking the end of cycle time measurement. Code: `AppSettings.CycleTimeEndStage`.

---

## Attribution Rules (Cross-Cutting)

**Transition-Based Sprint Scope** — All scope attribution uses transition timestamps, not snapshot fields. Code: `TransitionAttributionChecker`. The foundational rule for all analytics.

**Boundary-Driven Completion** — Position-based "is this status done?" check. Retained only for epic progress and ExcludedFromScopeStatuses. Code: `CompletionChecker`.

**Planning-Gated Disruption** — Disruption only counts additions after planningCutoff, not just after sprint start.

**Feature-Only Metrics** — All sprint scope surfaces exclude bugs from SP calculations. Bugs tracked separately.

---

## Analytics Metrics

**Health Score** — Composite 0–100 score: weighted combination of Completion, Disruption, CarryOver sub-scores. Displayed as RAG badge.

**RAG** — Red/Amber/Green classification based on configurable thresholds.

**Completion Rate** — `completedSp / activeSp * 100`. Higher is better.

**Active SP** — SP for tickets that entered CycleTimeStartStage during the sprint. The denominator for rate calculations.

**Completed SP** — SP for tickets that reached CycleTimeEndStage. Can exceed Active SP (prior carry-overs completing).

**Disruption Rate** — `addedSp / activeSp * 100`. Lower is better. Split into Scope Disruption (features) and Bug Disruption (bugs).

**Carry-Over Rate** — `carryOverSp / activeSp * 100`. Lower is better. Started but not completed.

**Bug Ratio** — `bugSp / completedSp * 100`. Lower is better.

**Cycle Time** — Time through workflow stages from start to end stage. Key flow efficiency metric.

**Throughput** — Per-developer SP and ticket count completed. Avoid: the term has two meanings — ticket count (cycle-time context) vs SP (throughput service context).

**Velocity** — Rolling 3-sprint average of completed SP at epic level. Same math as throughput rolling average, different scope.

---

## Scope Change Classifications

**Scope Change** — Any post-planning addition or removal. The overall category.

**Unplanned Bug** — Post-planning bug addition.

**Priority Escalation** — Post-planning addition of a pre-existing ticket (created before sprint start).

**Scope Injection** — Post-planning addition of brand new non-bug work. The residual category.

---

## Carry-Over

**Carry-Over Destination** — Outcome for prior-sprint carry-overs: Completed, Carried Again, Removed, or Dropped.

**Zombie Ticket** — A ticket appearing in 3+ distinct sprints. Indicates chronic incompletion.

**Zombie Trajectory** — Up to 10 most recent sprint appearances with FinalStatus per sprint.

---

## Developer & Capacity

**Default Capacity Percent** — Baseline developer availability (default 100%). Code: `Developer.DefaultCapacityPercent`.

**Developer Sprint Capacity** — Per-sprint capacity override. Code: `DeveloperSprintCapacity`.

**Excluded Developer** — Developer with 0% effective capacity AND 0 completed tickets. Removed from analytics.

**Normalized SP** — Capacity-adjusted projection: `spCompleted / (capacity / 100)`. Shows full-capacity equivalent.

---

## QA / Xray

**Test Execution (TE)** — Xray entity containing test runs. Attributed to sprints by highest SprintId of linked tickets. Code: `TestExecution`. Avoid: confusing with domain aggregate `TestExecution` — same concept.

**Test Run** — Single test result inside a TE. Status: Pass, Fail, Todo, Executing, Aborted. Code: `TestRun`.

**Test Set** — Jira issue representing a test collection. Code: `TestSet`.

**Tests Link** — Jira "Test" link type connecting tickets to TEs. The coverage link. Code: `TestExecutionLinkType.Tests`.

**Blocks Link** — Jira "Blocks" link type. Attribution only, NOT coverage. Code: `TestExecutionLinkType.Blocks`.

**Coverage Rate** — `covered feature tickets / total feature tickets * 100`. A ticket is "covered" if it has at least one non-cancelled TE via Tests link.

**Pass Rate** — `PASS runs / (PASS + FAIL) runs`. Todo/Executing/Aborted excluded from both sides.

**Testing Crunch** — Flag when >50% of terminal test runs happened in the last 2 days before sprint end.

**Dev-to-Test Gap** — Calendar days between dev done date and first terminal test run. Negative values valid.

---

## Dashboard & UI

**Delta** — Change vs prior sprint. Includes value, direction (up/down/neutral), and polarity.

**Delta Polarity** — Whether positive delta is good (green) or bad (red). Completion = positive-up. Disruption = positive-down.

**Sparkline** — Mini trend chart over up to 4 sprints on dashboard metric cards.

**Leaderboard** — Developer ranking by SP completed. Supports features-only / bugs-only toggle.

---

## Sync & Integration

**Jira Sync** — Fetching sprints and issues from Jira REST API; upserting to local DB. Code: `SprintIssueSyncService`.

**Changelog** — Jira's field-change history. Source for AddedAt, RemovedAt, WasCommitted, StatusTransitions.

**Backlog Sync** — Sync variant with `forcedNotCommitted = true`. All memberships WasCommitted=false.

**Epic Discovery** — Sync step fetching all issues under each distinct epic key.

**Xray Sync** — Fetching test executions from Xray Cloud GraphQL API. Code: `XrayIssueSyncService`.
