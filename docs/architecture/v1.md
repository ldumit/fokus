# Fokus — Architecture (v1)

**Status:** Draft
**Date:** May 5, 2026

---

## 1. System Context

Fokus is a standalone web dashboard that syncs data from Jira's REST API and computes sprint analytics locally. It is part of the Agentic Rituals Suite but deploys independently.

```
┌─────────────┐         ┌─────────────────────┐         ┌──────────┐
│  Browser    │◄───────►│  Fokus (App Service) │────────►│ Jira API │
│  (Vue SPA)  │  HTTP   │  .NET 10 + SQLite   │  HTTPS  │  (Cloud) │
└─────────────┘         └─────────────────────┘         └──────────┘
                                  │
                                  ▼
                         ┌──────────────────┐
                         │  Azure Key Vault │
                         │  (secrets only)  │
                         └──────────────────┘
```

**Data flow:** User triggers sync → Fokus calls Jira API → raw data mapped to domain entities → persisted in SQLite. Dashboard screens query SQLite and compute metrics on the fly.

---

## 2. Project Structure

Single service, three-project split:

```
src/
  Services/
    Fokus/
      Fokus.API/                    # Host, endpoints, Jira client, DI
        Features/
          Sync/                     # SyncSprint, SyncAllSprints endpoints
          Dashboard/                # Sprint Summary Card endpoint
          Developers/               # Throughput, BugRatio, CycleTime endpoints
          Sprints/                  # ScopeChange, CarryOver endpoints
          Epics/                    # EpicProgress endpoint
          Settings/                 # CRUD for status mappings, thresholds, sub-teams
        Infrastructure/
          Jira/                     # JiraClient (typed HttpClient), DTOs, mapping
        wwwroot/                    # Vue SPA build output
        Program.cs
      Fokus.Domain/                 # Entities, value objects, enums
        Entities/
        ValueObjects/
        Enums/
      Fokus.Persistence/            # DbContext, entity configs, migrations, repositories
        Configurations/
        Migrations/
        Repositories/
        FokusDbContext.cs
  client/                           # Vue 3 SPA source
    src/
      views/                        # Dashboard, Developers, Sprints, Epics, Settings
      components/                   # Charts, cards, layout
      stores/                       # Pinia stores
      api/                          # HTTP client (typed fetch wrappers)
      types/                        # TypeScript interfaces matching API contracts
```

**Reference graph:**
```
API ──► Domain
 │
 └──► Persistence ──► Domain
```

---

## 3. Tech Stack

| Layer | Choice | Version |
|-------|--------|---------|
| Runtime | .NET 10 | Latest stable |
| Web framework | ASP.NET Core + FastEndpoints | — |
| Validation | FluentValidation (via FastEndpoints) | — |
| ORM | EF Core | 10.x |
| Database | SQLite (file-backed) | — |
| Object mapping | Mapster | — |
| API docs | Microsoft.AspNetCore.OpenApi + Scalar | — |
| Frontend | Vue 3 + TypeScript | 3.x |
| State | Pinia | — |
| Charts | ApexCharts (vue3-apexcharts) | — |
| CSS | Tailwind CSS | 4.x |
| Build | Vite → wwwroot/ | — |
| Deployment | Azure App Service (Linux) | — |
| Secrets | Azure Key Vault (prod) / User Secrets (dev) | — |

**Not used in v1:** SignalR, MassTransit, gRPC, scheduled jobs.

---

## 4. Domain Model

### Classification

| Entity | Role | Rationale |
|--------|------|-----------|
| Sprint | Aggregate root | Owns SprintMemberships; sync boundary |
| Ticket | Aggregate root | Owns StatusTransitions; independent lifecycle |
| Developer | Entity | Mostly Jira-sourced data; SubTeam is app-managed |
| SprintMembership | Child entity | No meaning outside a Sprint |
| StatusTransition | Child entity | No meaning outside a Ticket |

### Computed Value Objects (never stored)

| Value Object | Purpose |
|--------------|---------|
| HealthScore | Weighted composite 0-100 from completion rate, disruption rate, carry-over rate |
| StageDuration | Time spent in a single workflow stage for one ticket |
| MetricWithDelta | Any metric value paired with its delta vs. prior sprint |

**HealthScore formula:**
```
score = 100
score -= (1 - completionRate) * completionWeight
score -= disruptionRate * disruptionWeight
score -= carryOverRate * carryOverWeight
clamp(0, 100)
```

Default weights: completion = 40, disruption = 30, carry-over = 30. Configurable in settings.

### Design Notes

- **Thin domain.** Entities are primarily data containers synced from Jira. Business logic lives in metric computation (query side), not in entity behavior.
- **No domain events in v1.** Sync is a simple write operation; no downstream reactions needed within the service.
- **Sprint.Id uses Jira's sprint ID** (not auto-increment). Same for Developer.AccountId and Ticket.Key. These are natural keys from the source system.
- **SprintMembership uses composite key:** (SprintId, TicketKey).
- **Metrics are never stored** — computed per-request from raw data. This keeps the sync idempotent (overwrite raw data, metrics recompute automatically).
- **Delta pattern:** All metric query responses include both the current value and the delta vs. the previous sprint (signed number + direction). The API computes the same metric for sprint N and sprint N-1 in a single query, returning both.

### Entity Definitions

```
Sprint
├── Id: int (Jira sprint ID, PK)
├── Name: string
├── StartDate: DateTime
├── EndDate: DateTime
├── BoardId: int
├── BoardName: string
├── State: SprintState (Active, Closed)
├── SyncedAt: DateTime
└── Memberships: List<SprintMembership>

Developer
├── AccountId: string (Jira account ID, PK)
├── DisplayName: string
├── AvatarUrl: string?
├── SubTeam: string?
├── IsActive: bool

Ticket
├── Key: string (e.g. "PROJ-1234", PK)
├── Summary: string
├── IssueType: string
├── StoryPoints: decimal?
├── EpicKey: string?
├── EpicName: string?
├── AssigneeId: string? (FK → Developer)
├── Priority: string
├── CurrentStatus: string
├── CreatedDate: DateTime
├── ResolvedDate: DateTime?
└── StatusTransitions: List<StatusTransition>

SprintMembership
├── SprintId: int (FK → Sprint, part of composite PK)
├── TicketKey: string (FK → Ticket, part of composite PK)
├── AddedAt: DateTime
├── RemovedAt: DateTime?
├── WasCommitted: bool
├── FinalStatus: string
├── StoryPoints: decimal? (snapshot at sync time)

StatusTransition
├── Id: int (auto PK)
├── TicketKey: string (FK → Ticket)
├── FromStatus: string
├── ToStatus: string
├── Timestamp: DateTime
├── AuthorId: string?
```

---

## 5. CQRS Breakdown

### Commands (write side — sync operations)

| Endpoint | Verb | Path | What it does |
|----------|------|------|--------------|
| SyncSprint | POST | /api/sync/sprint/{sprintId} | Fetch one sprint + its issues from Jira, upsert all entities |
| SyncAllSprints | POST | /api/sync/all | Fetch all closed sprints for the configured board, sync each |
| UpdateDeveloper | PUT | /api/developers/{accountId} | Update SubTeam, IsActive |
| SaveSettings | PUT | /api/settings | Persist status mappings, health thresholds |

### Queries (read side — metric computation)

| Endpoint | Verb | Path | Feature |
|----------|------|------|---------|
| SprintSummary | GET | /api/dashboard/{sprintId} | Sprint Summary Card (§5.7) |
| DeveloperThroughput | GET | /api/developers/throughput | Per-developer SP across sprints (§5.1) |
| ScopeChange | GET | /api/sprints/{sprintId}/scope | Scope change & disruption (§5.2) |
| CarryOver | GET | /api/sprints/{sprintId}/carry-over | Carry-over analysis (§5.3) |
| CycleTime | GET | /api/developers/cycle-time | Multi-stage cycle time with per-stage durations (§5.4) |
| BugRatio | GET | /api/developers/bug-ratio | Bug vs feature split (§5.5) |
| EpicProgress | GET | /api/epics | Epic completion & projections (§5.6) |
| SprintList | GET | /api/sprints | All synced sprints (for selectors) |
| DeveloperList | GET | /api/developers | All developers (for sub-team management) |
| GetSettings | GET | /api/settings | Current configuration |
| BoardList | GET | /api/boards | Proxy to Jira — list available boards |

**Query parameters pattern:**
- `?sprintIds=1,2,3` or `?last=5` — multi-sprint views
- `?subTeam=X` — sub-team filtering
- `?percentile=p50|p75|p90` — distribution metrics (cycle time)
- All metric endpoints return `{ value, delta, direction }` shape by default — delta is vs. previous sprint

---

## 6. Jira Integration

### JiraClient

A typed `HttpClient` registered in DI. Lives in `Fokus.API/Infrastructure/Jira/`.

```
Infrastructure/Jira/
├── JiraClient.cs            # HTTP methods matching Jira REST API
├── JiraOptions.cs           # Configuration POCO (instance URL, email)
├── Dtos/                    # Raw Jira response shapes
│   ├── JiraSprint.cs
│   ├── JiraIssue.cs
│   ├── JiraChangelog.cs
│   └── JiraBoard.cs
└── Mapping/
    └── JiraMapper.cs        # Jira DTOs → domain entities
```

**Authentication:** Basic Auth header built from email + API token. Token sourced from `IConfiguration` (Key Vault in prod, User Secrets in dev).

**Rate limiting:** Client-side throttle at 10 req/s with exponential backoff on 429 responses.

### Sync Flow

```
SyncSprint endpoint
  1. Fetch sprint metadata from Jira
  2. Fetch all issues in sprint (paginated, with changelog expanded)
  3. Map to domain entities: Sprint, Tickets, Developers (upsert), SprintMemberships, StatusTransitions
  4. Determine WasCommitted for each membership (AddedAt vs Sprint.StartDate)
  5. Persist via repository (upsert semantics — idempotent)
  6. If no WorkflowStages configured: run auto-detection from StatusTransition data, save as suggested (not confirmed)
  7. Return sync summary (counts + workflow suggestion if first sync)
```

---

## 7. Configuration & Settings

### Secrets (Azure Key Vault / User Secrets)

| Key | Description |
|-----|-------------|
| `Jira:InstanceUrl` | e.g. `https://acme.atlassian.net` |
| `Jira:Email` | Account email for API auth |
| `Jira:ApiToken` | Atlassian API token |

### Runtime Settings (SQLite — user-configurable via UI)

| Setting | Type | Purpose |
|---------|------|---------|
| BoardId | int | Which Jira board to sync |
| DoneStatuses | string[] | Status names that count as "done" (e.g. ["Done", "Closed"]) |
| WorkflowStages | string[] | Ordered workflow stages for multi-stage cycle time (e.g. ["To Do", "In Progress", "Code Review", "Testing", "Done"]) |
| HealthThresholds | object | Green/amber/red boundaries for completion, disruption, carry-over rates |
| HealthWeights | object | Weights for composite score: `{ completion: 40, disruption: 30, carryOver: 30 }` |

Stored as a typed `AppSettings` entity (single row, JSON columns for complex values) rather than generic key-value. This gives compile-time access patterns.

### Workflow Stage Auto-Detection

On first sync (or when no `WorkflowStages` are configured):

1. Analyze all `StatusTransition` records from synced data
2. Build a directed graph of status transitions (from → to), weighted by frequency
3. Derive the most common linear path (topological sort, preferring high-frequency edges)
4. Present the suggested order in Settings UI for user confirmation

This eliminates manual configuration for most teams — they sync, confirm the detected workflow, done.

---

## 8. Frontend Architecture

### SPA Structure

```
client/src/
├── App.vue
├── main.ts
├── router.ts                    # 5 routes matching nav
├── views/
│   ├── DashboardView.vue        # Sprint Summary Card
│   ├── DevelopersView.vue       # Throughput + CycleTime + BugRatio
│   ├── SprintsView.vue          # ScopeChange + CarryOver
│   ├── EpicsView.vue            # Epic Progress
│   └── SettingsView.vue         # Jira config, sub-teams, mappings
├── components/
│   ├── layout/
│   │   ├── AppSidebar.vue
│   │   ├── AppHeader.vue        # Sprint selector, sub-team filter
│   │   └── AppLayout.vue
│   ├── charts/                  # ApexCharts wrappers per chart type
│   └── cards/                   # Metric card, health badge components
├── stores/
│   ├── sprintStore.ts           # Sprint list, selected sprint(s)
│   ├── developerStore.ts
│   ├── dashboardStore.ts        # Aggregated dashboard data
│   └── settingsStore.ts
├── api/
│   ├── client.ts                # Base fetch config (base URL, error handling)
│   ├── sync.ts
│   ├── dashboard.ts
│   ├── developers.ts
│   ├── sprints.ts
│   ├── epics.ts
│   └── settings.ts
└── types/
    └── index.ts                 # API response interfaces
```

### UI Patterns (informed by Hatica, LinearB, Harness SEI)

**Page hierarchy (top → bottom):**
1. **Health signals row** — compact metric cards above the fold (composite score + per-metric RAG badges with 4-sprint trend sparkline)
2. **Data table** — one row per developer/sprint/epic with sortable columns. Each metric cell shows value + delta indicator (↑↓ with signed number, colored green/red)
3. **Section-level trend panels** — collapsible ApexCharts below tables showing last N sprints as stacked bars or lines. Not inline sparklines per cell.
4. **Drill-down** — click a row → slide panel with contributing tickets

**Specific patterns:**
- Composite health score (0-100) as the hero number on Dashboard, with RAG color
- Per-metric cards show: big number + delta vs prior sprint + 4-sprint mini trend
- Cycle time rendered as a **stage funnel** — stacked horizontal bar where each segment = one workflow stage, widest = bottleneck
- Percentile toggle (p50/p75/p90) on cycle time and throughput distribution views
- Developer table: Assignee | SP Completed | Completion % | Cycle Time Avg | Bug Ratio | Delta columns for each

### Design System

- Dark theme default, light theme toggle (Tailwind `dark:` classes, `class` strategy)
- Card-based layout with consistent spacing
- Health indicators: colored badges (green/amber/red) derived from threshold config
- Composite health score: 0-100 with RAG color based on thresholds (≥80 green, 60-79 amber, <60 red)
- Delta indicators: green ↑ for improvements, red ↓ for regressions (direction depends on metric polarity)
- ApexCharts with dark theme config, smooth animations
- Responsive: CSS grid, works on 1440px+ (primary) and 1024px (laptop)

### Data Flow

Views call Pinia store actions → stores call API module → API module does fetch → response typed and stored in state → components reactively render.

No optimistic UI needed (read-heavy dashboard). Sync operations show a loading state and refresh data on completion.

---

## 9. Deployment

### Azure App Service (Linux)

- Single App Service instance (B1 tier sufficient for single-user/small-team)
- SQLite file stored in App Service persistent storage (`/home/data/fokus.db`)
- Vue SPA served as static files from `wwwroot/`
- Azure Key Vault for secrets, accessed via Managed Identity (no connection strings in config)
- No separate database server — SQLite file deploys with the app

### Build & Deploy

```
dotnet publish → single self-contained binary + wwwroot/
```

CI/CD (future): GitHub Actions → build .NET + Vite → deploy to App Service.

### Local Development

```
# Backend
dotnet run --project src/Services/Fokus/Fokus.API

# Frontend (dev server with HMR, proxied to backend)
cd client && npm run dev
```

Vite dev server proxies `/api/*` to the .NET backend. Production build outputs to `wwwroot/`.

---

## 10. Cross-Cutting Concerns

### Error Handling

- Jira API errors surface as typed error responses to the frontend (connection failed, auth invalid, rate limited)
- Global exception handler via FastEndpoints for unhandled errors
- No retry logic beyond the rate-limit backoff in JiraClient

### Logging

- Structured logging via `ILogger<T>`
- Log sync operations (start, item counts, duration, errors)
- No external log sink in v1 (console + App Service logs)

### Security

- No user authentication in v1 — single-user tool deployed on a private App Service
- Jira credentials never exposed via API responses
- CORS restricted to same-origin (SPA served from same host)
- Scalar UI gated to Development environment only

### Performance

- Metric queries run against SQLite with appropriate indexes (SprintMembership composite key, StatusTransition by TicketKey+Timestamp)
- No caching layer in v1 — dataset is small (hundreds of tickets, not thousands)
- Sync is the only write path; reads don't contend

---

## 11. Key Indexes

| Table | Index | Purpose |
|-------|-------|---------|
| SprintMembership | PK (SprintId, TicketKey) | Composite key, all sprint queries |
| SprintMembership | IX_TicketKey | Find all sprints a ticket appeared in (carry-over) |
| StatusTransition | IX_TicketKey_Timestamp | Cycle time computation |
| Ticket | IX_EpicKey | Epic progress queries |
| Ticket | IX_AssigneeId | Developer throughput queries |

---

## 12. What's Explicitly Out of Scope (v1)

- Scheduled/automatic sync
- Multi-board support
- GitHub integration
- Export (PDF/PNG)
- User authentication (single-user deployment)
- SignalR / real-time updates
- MassTransit / integration events
- gRPC (no service-to-service calls)
