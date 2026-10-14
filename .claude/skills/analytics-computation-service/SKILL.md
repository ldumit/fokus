---
name: analytics-computation-service
description: Creates an analytics computation — a pure Domain calculator beside its result records in Fokus.Domain/Analytics/{Area}, the Application loader when the loading spans many repositories, and the endpoint's own reply and hand-written mapping; plus the delta/direction/polarity triplet, multi-sprint iteration, sparkline builders and rolling average over the shared kernel. Use when adding a new analytics tab or metric endpoint, or extending an existing calculator with a new computation area.
user-invocable: true
---

# Analytics Computation

Creates the backend computation for a new analytics feature, split across three homes:

| Part | Home | Skill for its structure |
|------|------|-------------------------|
| Calculator + result records | `Fokus.Domain/Analytics/{Area}/` (`Sprints`, `Developers`, `Qa`, `Workflow`); logic several areas use goes in `Shared/` | `domain-service` |
| Loader (only when the loading spans many repositories or per-sprint loops) | `Fokus.Application/Analytics/{Area}/{Operation}Inputs.cs` | `service-registration` |
| Reply types + mapping | the endpoint's `{Operation}Query.cs` and `{Operation}Extensions.cs` in `Fokus.API/Features/Analytics/{Operation}/` | `create-feature` |

Area folder names are plural and never equal an aggregate type name (`…Analytics.Sprint` would hide the `Sprint` aggregate).

## When to use

When a plan step creates a new analytics calculator (e.g. `QaMetricsCalculator`, `QaWorkloadCalculator`, `TestTimelineCalculator`, `DeveloperDetailCalculator`) or extends an existing one with a new computation area.

## Reference files

These files demonstrate the established pattern:

| File | What it shows |
|------|---------------|
| `Fokus.Domain/Analytics/Sprints/BugRatioCalculator.cs` | Multi/single split, alert evaluation |
| `Fokus.Domain/Analytics/Qa/QaWorkloadCalculator.cs` | Delta + direction + polarity triplet per column, team sparkline |
| `Fokus.Domain/Analytics/Qa/QaMetricsCalculator.cs` | Sparkline builders, metric cards through `MetricCardFactory` |
| `Fokus.Domain/Analytics/Sprints/TestTimelineCalculator.cs` | Per-day computation, static helper extraction |
| `Fokus.Domain/Analytics/Developers/DeveloperProgressCalculator.cs` | Gap delta, stall detection, the clock as a `today` parameter |
| `Fokus.Domain/Analytics/Developers/DeveloperDetailCalculator.cs` | Rolling average, ticket state |
| `Fokus.Domain/Analytics/Developers/LeaderboardCalculator.cs` | Ranked entries, capacity-aware skip |
| `Fokus.Domain/Analytics/Shared/` | The shared kernel: `DeltaResolver`, `MetricCardFactory`, `SubTeamFilter`, `ExcludedDeveloperFilter`, `CapacityResolver`, `TransitionAttributionChecker`, `TestCoverageCalculator`, `SparklinePoint`, `SprintRef` |
| `Fokus.Application/Analytics/Qa/QaWorkloadInputs.cs` | A loader: multi-repository loading and window selection, returning a source record |
| `Fokus.API/Features/Analytics/GetBugRatio/` | Endpoint, its `…Query.cs` reply types and `…Extensions.cs` mapping |

## Calculator structure

1. **Result records** in `{Name}Result.cs` beside the calculator — computation results only, `sealed record` for new types. No HTTP mode, no single/multi reply wrapper, no `System.Text.Json` attributes: those belong to the endpoint's reply.
2. **Calculator class** `{Name}Calculator` — no constructor dependencies. No repository, `DbContext`, logger, options, clock or JSON: it receives pre-loaded domain entities and settings. Where it needs today's date it takes `today` as a parameter (the caller passes `timeProvider.GetUtcNow().UtcDateTime.Date`).
3. **Public method(s)** — typically `ComputeMultiSprint` and/or `ComputeSingleSprint`.
4. **Private helpers** — named steps of at most ~60 lines (`docs/conventions/csharp.md` § Method Design); several inputs travel as a `private sealed record …Context`. A helper another calculator already has is reused from `Shared/`, never copied.
5. **Registration** — `services.AddScoped<{Name}Calculator>()` in `Fokus.Application/DependencyInjection.cs` (`AddFokusApplication`). Static `Shared/` classes are not registered.

## Key patterns

### Delta / Direction / Polarity triplet

Each numeric column that shows change has three fields:

- `{Column}Delta` (`decimal?`) — difference from prior sprint (null if no prior)
- `{Column}Direction` (`string?`) — `"up"`, `"down"`, `"flat"`, or null
- `{Column}Polarity` (`string?`) — `"positive"`, `"negative"`, `"neutral"`, or null (depends on whether higher is better for that column)

Direction is derived from delta sign, polarity from direction + column semantics: `DeltaResolver.DeltaDirection` / `DeltaPolarity`, and `DeltaResolver.ResolveCardDelta` for a metric card.

### Sparkline builder

```
BuildSparkline(sprints, windowSize, anchorIndex, valueSelector) -> List<SparklinePoint>
```

Extracts a sliding window of values for a mini-chart. Each point (`Shared/SparklinePoint`) has the sprint identifier and the metric value. The four builders in the tree read different inputs, so each stays in its calculator.

### Rolling average

Iterates over prior sprints (capacity-aware: skip sprints where developer has 0 SP), computes average of a metric over a configurable look-back window. Uses `TakeLast` AFTER any exclusion guards — not before (ordering matters to avoid returning fewer entries than requested).

### Multi-sprint vs single-sprint

- **Multi-sprint:** Accepts a list of target sprints + full closed sprint history. Iterates targets, computes per-sprint metrics, returns a list.
- **Single-sprint:** Accepts one sprint + prior sprint (nullable). Computes current values + deltas against prior. Returns a single result.

### Xray-disabled fast path

Analytics routes that include QA data check `settings.XrayEnabled` early — in the endpoint, or in its loader. If disabled, return the non-QA portion of the reply with QA fields nulled out. Don't query QA data at all.

## Endpoint wiring

The endpoint coordinates; nothing else does:

1. reads the request;
2. loads — from repositories directly, or by calling its `Fokus.Application` loader where the loading spans many repositories or per-sprint loops (a loader is named for what it loads, `{Operation}Inputs`, never `…Service`, and returns a source record);
3. calls the injected Domain calculator with the loaded entities;
4. maps the result to its own reply with the `To…()` extensions in `{Operation}Extensions.cs`, and assembles any single/multi wrapper there.

The calculator never touches a repository. A query that only the computation needs is still a loader or repository call made before the calculator runs.

## What this skill does NOT cover

- Frontend types, API functions, store wiring — see `create-vue-feature` skill
- Endpoint class, reply types and mapping — see `create-feature` skill
- Calculator naming, purity and placement rules — see `domain-service` skill
- Repository query methods — describe in the plan step inline
- Domain model changes — see `domain-patterns` skill
