# Frontend Map

Vue 3 + Pinia + Tailwind. Built with Vite into API's wwwroot/.

## View -> Store -> API Mapping

| View | Store | API Endpoints | Notes |
|------|-------|--------------|-------|
| DashboardView | dashboardStore | sprint-summary, closed sprints, sub-teams | Single sprint only, no multi-sprint mode |
| SprintsView | sprintsStore | scope-change, carry-over | Scope change + carry-over on same page, shared selector |
| DevelopersView | developersStore | developer-throughput, bug-ratio | Two tabs: Throughput (default) + Bug Ratio (lazy-loaded) |
| CycleTimeView | cycleTimeStore | cycle-time, cycle-time boundaries | Separate boundaries config endpoint |
| EpicsView | epicsStore | epic-progress | No sprint selector, cross-sprint always |
| TeamView | teamStore | team roster, team config | Team management, not analytics |
| SettingsView | settingsStore | settings, excluded-statuses, detect-workflow | Configuration UI |

## API Base

All analytics: `/analytics/{feature}?sprintId&last&subTeam`
Convention: `last=0` means "all sprints" (frontend sends 0 when user picks "All")

## Store Patterns

**Initialization:** Stores load closed sprints and sub-teams on init, default to most recent sprint or last 5.

**Selection modes:**
- `'single'` = specific sprintId
- `'multi'` = last N sprints (3, 5, or 0 for all)

**Optimistic UI:** developersStore.updateCapacity applies change immediately, rolls back on API failure, then re-fetches.

**Lazy loading:** Bug ratio data only fetched when Bug Ratio tab is active.

## Component Locations

- Dashboard metric cards: SP Completed, Completion %, Scope Disruption Rate, Bug Disruption Rate, Carry-Over Rate (5 cards, `lg:grid-cols-5`)
- `client/src/components/dashboard/` — HealthScoreBadge, MetricCard, SprintFlags
- `client/src/components/sprints/` — BurnupChart, ScopeChangeChart, CarryOver*, Classification*, Event*, Zombie*
- `client/src/components/developers/` — BugRatio*, throughput table
- `client/src/components/` — shared: AppSidebar, PageToolbar, BaseCard, BaseSelect, EmptyState, PageLayout
