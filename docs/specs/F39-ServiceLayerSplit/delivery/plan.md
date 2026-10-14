# F39 — Service Layer Split

**Feature Spec:** None — plan-only definition (owner, 2026-10-14)
**Model:** claude-opus-5-5
**Build slices:** 1–4 · 5–6 · 7 · 8–10 · 11–13; why:
- **1–4** lay the ground every move depends on: the skills, the comparison harness and its baseline, the Application project, and the shared kernel's independent parts.
- **5–6** move the calculators with their tests.
- **7** is heavy (about 25 methods over 80 lines), so it runs alone.
- **8–10** give each endpoint its own reply and move the loading and the syncs to Application.
- **11–13** close the tests, the docs and the final comparison.

**Lane:** full — owner's level
**Rule list:** not applicable — plan-only definition

## Context

The API project holds 18 `*Service` classes, 9,397 lines (plan-time `wc -l`): 15 in `Features/Analytics/`, plus `Features/Sync/SprintIssueSyncService.cs` (184), `Features/Xray/XrayIssueSyncService.cs` (337) and `Features/Settings/WorkflowDetectionService.cs` (194). The owner wants them split, as in his knowledge-gateway StoreAssistant service, into pure Domain logic and Application-layer orchestration, shrinking the large services and endpoints (owner, 2026-10-14).

- **What the code is today** (plan-time read, confirmed by both round-1 readers):
  - **16 classes are calculations already:** no constructor dependencies, no async. Two of them read the clock: `DeveloperDetailService.cs:211,329` and `DeveloperProgressService.cs:69,93` call `DateTime.UtcNow` (UTC dates).
    - Every endpoint loads data through repositories, calls one `Compute*` method with domain entities, and gets back records declared in the service file.
    - Many of those records are reply wrappers: an HTTP mode, plus nullable single-sprint and multi-sprint alternatives (`BugRatioResponse`, `CarryOverResponse`, `CycleTimeResponse`, `ScopeChangeResponse`, `LeaderboardResponse`, `TestTimelineResponse`).
  - **Two classes do I/O:** the sync services (repositories, `ILogger`, `IJiraClient`/`IXrayClient`). `SprintSyncFailure`, declared in `SprintIssueSyncService.cs`, is part of the `SyncSprints` reply (`SyncSprintsCommand.cs:25`). The broadcast `catch` is in `Sync/SyncSprints/SyncSprintsEndpoint.cs:51`, not in a service.
  - **Pure helper classes in the API project:** `TransitionAttributionChecker` (181), `StatusTransitionIndex` (37), `CompletionChecker` (61), `HealthScoreCalculator` (54), `ExcludedDeveloperFilter` (51). `TestExecutionRepository.cs:157-172` keeps an in-memory copy of `IsStartedInSprint`, and `:291` a private `GetStageIndex`.
  - **Repeated private helpers, in variants, not single copies:**
    - `FilterMemberships`: 9 copies in 4 variants.
    - `BuildMetricCard`: 5 copies with 3 return types (`MetricCard`, `ScopeMetricCard`, `BugRatioMetricCard`).
    - `DeltaDirection`/`DeltaPolarity`: 5 services, with overload sets differing per service.
    - `IsBug`, `FilterDevelopers`: 5 each. `GetCapacity`, `GetDeveloperMemberships`: 4 each.
    - `IsStartedInSprint`: 3 list-based copies plus the Persistence one. `GetStageIndex`/`ResolveStartIndex`: 3 each.
    - `ComputeRollingAverage`: 2, coupled to each service's own `ComputeMetrics`. `CountBusinessDays`: 2.
    - Eight byte-identical 4-field sprint-info records (`SprintSummaryService.cs:5` `SprintInfo` carries two more fields).
    - Four sparkline builders and two consecutive-sprint alert evaluators.
  - **Long methods:** 25 methods in these classes are over 80 lines (35 over 60, the `csharp.md` § Method Design limit). The largest:
    - `ComputeEpicProgress` 293, `ComputeProgress` 242, `Detect` 180;
    - `ComputeSummary` 150, `ComputeThroughput` 144, `BuildBurnupData` 143, `ComputeTicketCycleTime` 142, `ComputeQaMetrics` 140, `ComputeDeveloperQuality` 139.
  - **Fat endpoints** (lines): `GetQaWorkload` 333 (about 25 repository calls with per-sprint loops; mapping alone runs lines 202–333), `GetDeveloperQuality` 252, `GetQaTrends` 155, `GetQaMetrics` 152, `GetSprintSummary` 133, `GetDeveloperDetail` 117.
  - **Endpoints compose services:** `GetQaTrends` = `BugRatio` + `QaTrends`; `GetSprintSummary` = `QaMetrics` + `TestTimeline.ComputeCrunchFlag` + `SprintSummary`.
    - `GetQaWorkload`, `GetDeveloperQuality` and `GetQaMetrics` map to their own reply types, but `GetQaMetrics`' reply still embeds `MetricCard` (`GetQaMetricsQuery.cs:27-29`).
    - `Sprints/GetTestTimeline` returns the service's record directly.
  - **`TransitionEdge` and `TransitionEdgeData`** are declared in `Fokus.Persistence/Repositories/TicketRepository.cs:5-8`.
  - **Registrations:** all 18 classes are registered in `Fokus.API/DependencyInjection.cs:29-46`.
  - **Tests:** 41 facts in 5 files: `DeveloperProgress` 19, `TransitionAttributionChecker` 8, `ScopeChange` 6, `SprintSummary` 5, `DeveloperThroughput` 3.
    - The project references `Fokus.API` only.
    - `Fokus.Tests/GlobalUsings.cs:4` holds `global using Fokus.API.Features.Analytics`.
- **The reference shape (knowledge-gateway StoreAssistant, read 2026-10-14):**
  - FastEndpoints with no mediator; "With FastEndpoints the coordination stays in the endpoint" (ADR-F192b).
  - `Application/` holds injected capability classes, one folder per capability.
  - `Domain/` holds pure rules: no database, logger or options.
  - `…Service` names only a class around another system.
- **Owner rulings (2026-10-14):**
  - a Domain + Application split, reversing the root `CLAUDE.md` "There is no separate Application project" (line 34) and `docs/architecture/v1.md`'s no-Application and thin-domain lines;
  - each endpoint owns its reply type (`domain-service` § Output Boundary);
  - name "Service Layer Split"; plan-only definition;
  - the comparison is the agents' own check: the owner supplies and needs nothing from it.
- **No new committed tests (standing owner ruling).** Behaviour is proven by a **golden comparison harness** kept outside the repo (Step 2). It replays a request manifest, including the sync POSTs with fake outside clients, against a seeded synthetic database, before and after each slice.
- **Same-topic grep:** no plan covers this. `docs/specs/F36-PublicReleaseCleanup/delivery/nexus-dotnet-skill-fit.md` rated `domain-service` 5 ("better as its own feature"); this is that feature. G-01 (Domain → `Jira.Contracts`) stays out of scope.
- **Order:** builds on F38's branch after F38 merges.
- **Shell:** every command in this plan is Git Bash (the Bash tool), not PowerShell.

## Scope

**In scope:** developer Steps 1–13 and the operator check (Step 14).

**Out of scope:**
- direct repository use in the Settings and Auth endpoints (Decision D2);
- G-01;
- role and status enums;
- any change to an output value;
- the front end;
- new committed tests;
- the existing non-analytics `DateTime.UtcNow` uses in `Fokus.Domain` entity behaviour (9 sites).

## Skill Mapping

| Step | Skill | Disposition | TDD | Feature-Specific Inputs | Gap? |
|------|-------|-------------|-----|------------------------|------|
| 1 | (none) | — | no | `domain-service` new; `service-registration`, `create-feature` and `cqrs-patterns` merged | gap: plugin skill → local copy (logged in F38 lessons) |
| 2 | (none) | — | no | the golden harness and the base capture | gap: no skill for a before/after output-comparison harness |
| 3 | service-registration | Follow | no | `Fokus.Application`, its registration, the reference graph | — |
| 4 | domain-service | Follow | no | the shared kernel's independent parts, `TransitionEdge*`, tests moved with them | — |
| 5 | domain-service | Follow | no | sprint-level calculators and their tests | — |
| 6 | domain-service | Follow | no | developer, QA and workflow calculators and their tests; the clock as input | — |
| 7 | domain-service | Follow | no | the long methods decomposed; coupled helpers consolidated | — |
| 8 | create-feature | Follow | no | endpoint-owned replies, hand-written mappings | — |
| 9 | service-registration | Follow | no | Application loaders for the fat endpoints | — |
| 10 | service-registration | Follow | no | the two sync services in Application | — |
| 11 | (none) | — | no | the test-project check; the Persistence copies | — |
| 12 | (none) | — | no | rules, the architecture doc, conventions, the service CLAUDE.md, local skills, KB | — (doc sweep under `kb-maintenance`) |
| 13 | (none) | — | no | the final comparison | gap: as Step 2 |
| 14 | (none) | — | no | operator check | — |

## Domain Model Changes

- New tree `Fokus.Domain/Analytics/`:
  - one folder per area (`Sprints/`, `Developers/`, `Qa/`, `Workflow/`, `Shared/`). An area name never equals an aggregate type name: a namespace `…Analytics.Sprint` hides the `Sprint` aggregate from every `Fokus.Domain.Analytics.*` file (CS0118; questions.md Q4, D14);
  - each calculator sits beside its own result record;
  - the shared kernel goes in `Shared/`.
- Names come from the skill's vocabulary (calculator, resolver, checker, provider), never `…Service`.
- **Only computation results enter Domain.** Reply wrappers (HTTP mode, single/multi alternatives, presentation fields) stay in the API as replies (Step 8).
- `TransitionEdge` and `TransitionEdgeData` move to Domain.
- No entity or aggregate changes.

## Data Model Changes

None. No migration; `Migrations/` and the model snapshot are unchanged (Must-NOT-Change).

## Implementation Steps

### Step 1 — Bring the skills in

Same recipe and accept as F38 Step 1 (`docs/specs/F38-DotnetSkillAlignment/delivery/plan.md` § Step 1); the copies describe the target state.
- **Sources:** new `domain-service`, plus merges of the plugin text into the local `service-registration`, `create-feature` (whole folder, including `workflows/Mappings.md`) and `cqrs-patterns`, from `D:/src/claude-plugins/nexus/plugins/nexus-dotnet/skills/`.
- **This repo's form, stated in the copies:**
  - Application holds capability classes and no mediator; the endpoint coordinates.
  - Mapping is hand-written `To…()` extensions in `{Op}Extensions.cs` beside the feature. The local Mapster workflow goes: Fokus has no Mapster use (grep over `src/Services`: 0).
  - `…Service` names only a class around another system.
  - Registration method names keep this repo's `AddFokus{Layer}` form (Decision D10).
- **Accept:**
  - the F38 Step 1 self-containment greps → 0 on the 4 folders;
  - `grep -rni "Adapt<\|IRegister\|Mapster" .claude/skills/create-feature .claude/skills/cqrs-patterns .claude/skills/service-registration .claude/skills/domain-service` → 0 (the local `cqrs-patterns:108` and `service-registration:43` carry Mapster lines today; questions.md Q3);
  - `find .claude/skills -mindepth 2 -maxdepth 2 -name SKILL.md | wc -l` equals the `.claude/README.md` count (43).

### Step 2 — The golden harness and the base capture

The comparison is the agents' safety net (owner, 2026-10-14). It lives in `D:\temp\f39-golden\` and is never committed.

- **Base (no git writes — owner constraint; questions.md Q1, D12):**
  - record the base commit (`git rev-parse HEAD` before any F39 source edit) in `implementation.md`; confirm `git status --porcelain -- src Directory.Packages.props` is empty;
  - extract it twice, read-only: `git archive {base} | tar -xf - -C "/d/temp/f39-golden/base"`, and the same into `/d/temp/f39-golden/pristine` (the `/d/...` form: GNU tar can read a `D:` prefix as a remote host);
  - every "before" capture runs from the `base` extract, never from a moving branch; `pristine` is never built or edited.
- **Harness project** `D:\temp\f39-golden\harness\` (.NET 10):
  - It references `Fokus.API.csproj` by absolute path in the tree under test. For the base it targets the `base` extract's copy; the after runs target the main tree.
  - It references `Microsoft.AspNetCore.Mvc.Testing`, whose build targets write the content-root manifest.
  - Its entry type is a public type of `Fokus.API`, for example `SprintHub`, so `Program.cs` is not changed.
- **Host settings:**
  - **Environment `Production`,** so error bodies carry no stack trace.
  - **Configuration overrides:**
    - `ConnectionStrings:Database` = `Data Source=D:\temp\f39-golden\run-{n}.db` (a connection string; F38 lesson);
    - dummy non-empty Google `ClientId`/`ClientSecret`;
    - an empty `Xray` section, so no machine credentials are copied.
  - **A start-up filter that sets `HttpContext.TraceIdentifier` to a fixed value,** so error envelopes are byte-stable.
  - **A test authentication scheme as the default.** Its `NameIdentifier` is the id of a seeded, active Admin `AppUser`; `ActiveUserPreProcessor` checks the database.
  - **`IJiraClient` and `IXrayClient` replaced by deterministic fakes,** one of which fails for one chosen sprint or test execution.
  - **A database command interceptor** that counts SQL commands per request.
- **Seeding, after the host has started** (migrations and the default settings row have run): the harness writes the synthetic data through the host's own `FokusDbContext`, deterministically (fixed ids, no random).
  - **Dates relative to the run's UTC date** `D` (`DateTime.UtcNow.Date`, recorded):
    - 6 closed sprints ending before `D`;
    - one active sprint from `D-1` to `D+9` (current day 2, so the grace-period branch runs, `DeveloperProgressService.cs:72`);
    - one future sprint.
  - **Tickets** of every issue type the analytics read: stories, bugs, and epics with children.
  - **Status transitions** covering carry-over, scope added and removed after planning, zombies, done, reopened, a sidelined status, and a cycle.
  - **Developers:**
    - active, inactive, and excluded in one sprint only;
    - one developer active in some closed sprints but not all;
    - capacities, including 0;
    - two sub-teams.
  - **Test executions, runs and sets** with passes and failures.
  - **Settings:** workflow stages, done statuses, excluded statuses, cycle-time boundaries, health thresholds, a `BoardId`, `XrayEnabled = true`, and dummy Xray client id and secret in the settings row. The sync endpoints return 400 without a board, and the Xray sync stops early without credentials; the fakes ignore the values.
  - **Fixture variant B:** `XrayEnabled = false`, and an active sprint from `D-5` to `D+5` (current day 6, outside the grace period), run for every GET.
  - **Fixture variant C:** no active sprint (every sprint closed or future), run for every GET, so the no-active-sprint empty replies are captured.
- **Request manifest** (written to `harness/manifest.json`, with the expected status per entry):
  - **Every GET** under `/api/analytics/*`, `/api/sprints*`, `/api/developers*`, `/api/team` and `/api/settings/workflow-stages/detect`:
    - each once with no parameters;
    - once per sprint id, the active one included;
    - with `subTeam` = each seeded sub-team wherever the request takes it;
    - with `last=2` wherever it takes `last`;
    - path ids per developer: `developer-detail` for active, inactive, excluded and missing; `capacity` per developer.
  - **Sync POSTs:** `/api/sync/sprints`, `/api/sync/backlog` and `/api/xray/sync`, each once with the failing fake item.
  - **One write** each for the PUTs under `/api/developers/{id}/*` and `/api/sprints/{id}`, followed by the GET that reads it back.
- **Output, in three separate places:**
  - **`{capture}/replies/`:** status and body bytes per request.
    - One normalization, applied by the harness and listed in `implementation.md`: a sync reply's failure text has its stack-trace lines replaced by `<stack>`. `SprintIssueSyncService.cs:75` writes `ex.StackTrace` into the reply, and frame paths differ between trees.
    - Nothing else is normalized.
  - **`{capture}/sql-counts.json`:** the SQL command count per request.
  - **`{capture}/api.json`:** `/swagger/v1/swagger.json` from a separate Development-environment host.
- **The comparison method** (used at every slice end and at Step 13):
  - **replies:** `diff -r "{base}/replies" "{after}/replies"` exits 0;
  - **SQL counts:** for every request, the after count ≤ the base count;
  - **API document:** the F38 method (per path, verb and resolved property location; schema type names may differ) finds no difference.
- **Accept, all in `implementation.md`:**
  - **Determinism:** two base captures from the extract, and one from the main tree before any F39 source edit (main = base then), are identical by the comparison method — the third proves the extract behaves as the checkout.
  - **Fixture check:** for each route, at least one entry returns 200 with data: QA routes `hasQaData: true`, non-empty lists, the workflow detect reply listing stages, and each sync POST reaching its failing fake item.
  - **Sabotage check:** change one calculator constant and one sub-team filter in the `base` extract; the replies diff fails; revert both, proven by `diff -r --exclude=bin --exclude=obj "/d/temp/f39-golden/base" "/d/temp/f39-golden/pristine"` exiting 0 (any further build-output exclusion is named in `implementation.md`).
  - **Coverage:** the manifest size and route count.

### Step 3 — The Application project

Follow service-registration (local copy).
- **New** `src/Services/Fokus/Fokus.Application/Fokus.Application.csproj`:
  - references `Fokus.Domain`, `Fokus.Persistence`, `Jira.Contracts`, `Xray.Contracts` and the `Blocks.*` it needs;
  - added to `src/Fokus.slnx`;
  - `DependencyInjection.AddFokusApplication(services, configuration)`, called from the composition between the API and Persistence registrations (D10).
- **Registrations:**
  - `TimeProvider.System` registers once, in `AddFokusServices`.
  - Each class moved by Steps 4–10 has its registration moved in the same step: calculators and loaders to `AddFokusApplication`, and the shared kernel's injected classes with them.
  - No registration is left for a moved type in `Fokus.API/DependencyInjection.cs`.
- **References:** `Fokus.API` references `Fokus.Application` and keeps `Persistence` (D2). `Fokus.Tests` references `Fokus.Domain` and `Fokus.Application` directly.
- **Accept:**
  - `grep -c "Fokus.Application" src/Fokus.slnx src/Services/Fokus/Fokus.API/Fokus.API.csproj src/Services/Fokus/Fokus.Tests/Fokus.Tests.csproj` → 1 each;
  - `Fokus.Domain.csproj` unchanged;
  - full suite green.

### Step 4 — The shared kernel's independent parts

Follow domain-service (local copy). Move into `Fokus.Domain/Analytics/` only what has no dependency still in the API:
- `TransitionAttributionChecker`, `StatusTransitionIndex`, `CompletionChecker`, `HealthScoreCalculator` and `ExcludedDeveloperFilter`;
- `TransitionEdge` and `TransitionEdgeData` together, with `TicketRepository` and every caller updated;
- the shared records with no API dependency (`SparklinePoint`, `TestingCrunchFlag`, and the eight 4-field `{Area}SprintInfo` records merged into one `SprintRef` — questions.md Q2, D13). The 6-field `SprintInfo` moves with `SprintSummary` in Step 5. Add the glossary line to `docs/kb/glossary.md` beside **Sprint**: "**Sprint Reference** — A sprint's id, name, start and end dates, carried inside an analytics result. Code: `SprintRef`. Not the `Sprint` aggregate.";
- the helpers with no coupling: delta direction and polarity, the bug test, business days, the stage index.

**Merge rule (D6):** variants are merged only where byte-identical or provably equal on every input. Each surviving variant is named in `implementation.md` with its difference. Coupled helpers (the rolling average with its `ComputeMetrics`, the metric-card builders that return feature records) wait for Step 7.

**Tests move in the same step:** `Fokus.Tests/GlobalUsings.cs:4`, `TransitionAttributionCheckerTests.cs` and any other reference get the new namespaces and type names. No assertion changes.

**Accept:**
- `grep -rlE "class (TransitionAttributionChecker|StatusTransitionIndex|CompletionChecker|HealthScoreCalculator|ExcludedDeveloperFilter)\b" --include=*.cs src/Services/Fokus/Fokus.API` → 0;
- `grep -rn "TransitionEdge" --include=*.cs src/Services/Fokus/Fokus.Persistence | grep -c "record "` → 0;
- `grep -rnE "record [A-Za-z]+SprintInfo\b" --include=*.cs src/Services/Fokus` → 0, and `grep -c "SprintRef" docs/kb/glossary.md` → 1;
- full suite green;
- the slice-end comparison runs after Step 4 (Step 2's comparison method).

### Step 5 — Sprint-level calculators

Follow domain-service (local copy).
- **Move** `SprintSummary`, `ScopeChange`, `CarryOver`, `BugRatio`, `CycleTime`, `EpicProgress` and `TestTimeline` to `Fokus.Domain/Analytics/Sprints/` (namespace `Fokus.Domain.Analytics.Sprints`; D14), renamed by the skill's vocabulary.
- **Skill text:** the local `domain-service` § This Repo's Form **Home** bullet names `Sprints/` and `Developers/` and states the no-aggregate-name rule (exact before/after in questions.md Q4).
- **Only computation results move.** Each reply wrapper stays in the API until Step 8 as the endpoint's reply; until then the endpoint keeps the wrapper and fills it from the result.
- **Tests and registrations move in the same step:** `SprintSummaryServiceTests`, `ScopeChangeServiceTests` and the registration lines.
- **Accept:**
  - `for n in SprintSummary ScopeChange CarryOver BugRatio CycleTime EpicProgress TestTimeline; do test -e "src/Services/Fokus/Fokus.API/Features/Analytics/${n}Service.cs" && echo "$n"; done` → no output;
  - `grep -rnE "namespace Fokus\.(Domain|Application)\.Analytics\.(Sprint|Developer|Ticket|TestExecution|TestRun|TestSet)\b" --include=*.cs src/Services/Fokus` → 0, and `grep -c "Sprints/" .claude/skills/domain-service/SKILL.md` ≥ 1;
  - full suite green.

### Step 6 — Developer, QA and workflow calculators; the clock as input

Follow domain-service (local copy). Same as Step 5, into `Fokus.Domain/Analytics/{Developers,Qa,Workflow}/` (D14), for:
- `DeveloperProgress`, `DeveloperDetail`, `DeveloperQuality`, `DeveloperThroughput` and `Leaderboard`;
- `QaMetrics`, `QaTrends` and `QaWorkload`;
- `WorkflowDetection`.

**The clock:** the two clock readers take `today` as a parameter. The caller passes `timeProvider.GetUtcNow().UtcDateTime.Date`, the same UTC date as today. `DeveloperProgressServiceTests` and `DeveloperThroughputServiceTests` calls pass an explicit date.

**Accept:**
- `find src/Services/Fokus/Fokus.API/Features -name "*Service.cs" | grep -E "/Analytics/|WorkflowDetectionService"` → no output;
- `grep -rnE "DateTime\.(UtcNow|Now|Today)|DateTimeOffset\.UtcNow" --include=*.cs src/Services/Fokus/Fokus.Domain/Analytics` → 0;
- full suite green;
- the slice-end comparison runs after Step 6.

### Step 7 — Decompose the long methods; consolidate the coupled helpers

Follow domain-service (local copy); the method rules are `docs/conventions/csharp.md` § Method Design.
- **Long methods:** every method over 80 lines in `Fokus.Domain/Analytics` (25 at plan time) becomes named private steps of at most ~60 lines.
- **Coupled helpers:** the rolling averages, metric-card builders, sparkline builders, alert evaluators and membership and developer filters are consolidated where D6 allows. A variant that stays is named.
- **Accept:**
  - a method-length count over `Fokus.Domain/Analytics/**/*.cs` (the script in `implementation.md`) → no method over 80 lines, with the five longest listed;
  - for each helper family in § Context (`FilterMemberships`, `FilterDevelopers`, `GetDeveloperMemberships`, `GetCapacity`, `IsBug`, `IsStartedInSprint`, `GetStageIndex`, `ResolveStartIndex`, `DeltaDirection`, `DeltaPolarity`, `BuildMetricCard`, `ComputeRollingAverage`, `CountBusinessDays`), `grep -rnE "static [^=]*\b{Name}\(" --include=*.cs src/Services/Fokus/Fokus.Domain src/Services/Fokus/Fokus.API` lists the declarations. Each family has one, or each extra is a variant named in `implementation.md` with its difference;
  - full suite green;
  - the slice-end comparison runs after Step 7.

### Step 8 — Each endpoint owns its reply

Follow create-feature (local copy, hand-written mappings).
- **Every endpoint whose reply is or embeds a Domain record** gets reply types in its `{Op}Query.cs`/`{Op}Command.cs` and maps by hand in `{Op}Extensions.cs`. This covers:
  - every analytics GET;
  - `Sprints/GetTestTimeline`;
  - `Analytics/GetQaMetrics`, whose `MetricCard` properties and nested sparklines are included;
  - the empty-reply branches.
- **The wire stays byte-identical:** property names, nesting and member order.
- **Accept:**
  - `grep -rn "System.Text.Json" --include=*.cs src/Services/Fokus/Fokus.Domain/Analytics` → 0;
  - the API-document diff (F38 method: per path, verb and property location) between the base `api.json` and the current one is empty;
  - an inventory of the final Domain records in `implementation.md`, none of them a reply wrapper;
  - full suite green.

### Step 9 — Loading moves out of the fat endpoints

Follow service-registration (local copy) for registrations; the shape is the knowledge-gateway one (§ Context).
- **Endpoints:** for `GetQaWorkload`, `GetDeveloperQuality`, `GetQaTrends`, `GetQaMetrics`, `GetSprintSummary` and `GetDeveloperDetail`, the multi-repository loading and window selection move into one `Fokus.Application/Analytics/{Area}/` class per endpoint, with the Domain area names (`Sprints/`, `Developers/`, `Qa/` — D14; a `…Analytics.Sprint` namespace hides the aggregate here too). Each is named for what it loads (for example `QaWorkloadInputs`), never `…Service`.
- **The endpoint keeps** reading the request, calling the loader and the calculator, and the call to its mapping extension.
- **`GetDeveloperDetail`'s inline activity check stays inline** (D11).
- **No more queries:** the per-sprint loops keep the SQL command count per request at or below the base.
- **Accept:**
  - each of the six endpoint files under 120 lines, counts listed;
  - per manifest entry, the after command count ≤ the base count (harness report);
  - full suite green.

### Step 10 — The sync services move to Application

Follow service-registration (local copy).
- **Move** `SprintIssueSyncService` (with `SprintSyncFailure`) to `Fokus.Application/Sync/` and `XrayIssueSyncService` to `Fokus.Application/Xray/`. They keep `…SyncService` (D3). Callers to update: `SyncSprints`, `SyncBacklogSprints`, `SyncXray` and `SyncSprintsCommand.cs`.
- **`SyncSprints` owns its reply:** `SyncSprintsCommand.cs` declares its own failure reply type, mapped by hand from the Application `SprintSyncFailure`, with the wire unchanged. The same applies to any other reply that would embed an Application type.
- **Every `catch` block in the two classes is unchanged, and so is `SyncSprintsEndpoint.cs`'s broadcast `catch`** (F38 D11).
- **Accept:**
  - `find src/Services/Fokus/Fokus.API -name "*Service.cs"` → none;
  - the diff of every `catch` block is empty, whitespace aside;
  - full suite green;
  - the slice-end comparison runs after Step 10.

### Step 11 — The test project and the Persistence copies

- **Test classes:** renamed to `{ClassUnderTest}Tests`. Remove their stale keys in `.claude/test-times.json`.
- **Test diff:** the test files' diff against the base contains only `using` lines, type and class names, and the explicit date arguments. Paste the diff summary into `implementation.md`.
- **Persistence copies:** `TestExecutionRepository.cs:157-172` (in-memory) uses the Domain started-in-sprint helper, building the index the helper takes. Its `:291` `GetStageIndex` uses the Domain stage index. Where the shapes cannot meet, the copy stays with a comment naming its Domain twin.
- **Accept:** fact count 41 before and after (runner output); full suite green.

### Step 12 — Rules, the architecture doc, conventions, skills and KB

- **Root `CLAUDE.md`:**
  - **line 34** — before: "There is no separate Application project — FastEndpoints endpoint classes own feature slices directly (…)." After: "A service adds an `{Svc}.Application` project when its logic outgrows the endpoints (Fokus does, since F39). Application holds injected capability classes with no mediator; the endpoint stays the coordinator; pure rules and calculators live in `{Svc}.Domain`."
  - **Repo Structure** gains the `{Svc}.Application/` line.
  - **Tech Stack line 44** — before: "- Mapster — object mapping". After: "- Hand-written `To…()` mapping extensions in `{Op}Extensions.cs` — object mapping; no mapping library." (Fokus has no Mapster package or use — Step 1; questions.md Q3.)
- **`docs/architecture/v1.md`:**
  - the project reference graph and the "no separate Application project" text (lines 20–37);
  - the "No service layer classes" constraint;
  - the "Thin domain in v1" line (171);
  - all three get the new layering.
- **`docs/conventions/project-rules.md`:**
  - **Repo Structure** gains Application.
  - **Reference graph:** API → Application → Domain; Application → Persistence → Domain; API → Persistence (D2).
  - **Line 22:** "Focused operation services scoped to a feature area (e.g. `SprintIssueSyncService`) are allowed" → "Logic shared by a feature area is a Domain calculator or an Application capability class; `…Service` names only a class around another system."
- **`docs/conventions/coding-conventions.md`** — before: "Don't create cross-feature helpers — if two features need the same logic, that's an architect decision, not a boy scout fix." After: "Don't create cross-feature helpers outside `Fokus.Domain/Analytics/Shared` — the shared analytics kernel is the one home for logic several analytics features use (F39)."
- **`src/Services/Fokus/CLAUDE.md`:**
  - Application layer: `Fokus.Application`;
  - the Endpoint pattern and "computed on-the-fly in endpoints" lines say who computes, who loads and who maps;
  - the domain line names `Domain/Analytics`;
  - "Focused operation services" reworded.
- **Local skills:**
  - **`analytics-computation-service`** — frontmatter, When to use, Reference files, Service structure and Endpoint wiring rewritten to the new homes: the calculator in Domain, loading in Application, the reply and mapping in the endpoint. The repository-query exception for calculators is removed. The patterns (delta triplet, sparkline, rolling average, Xray fast path) are kept, pointing at the shared kernel.
  - **`extract-feature-service`** — rewritten as "extract shared endpoint logic into a Domain calculator or an Application capability class", or retired with a pointer.
  - **`create-service`** workflows: their Application-eligibility and layout text matches the root rule.
- **KB:** every key-file path and prose type mention of a moved class (19 references in 15 files at plan time) is updated. KB lint: no stale key file, no orphan.
- **Accept:**
  - `grep -rnE "(Features/(Analytics|Sync|Xray|Settings)/[A-Za-z]+(Service|Checker|Filter|Calculator|Index)\.cs)|\b[A-Z][A-Za-z]+(Progress|Summary|Change|Over|Ratio|Time|Timeline|Detail|Quality|Throughput|Metrics|Trends|Workload|Detection)Service\b" docs/kb .claude/skills docs/conventions docs/architecture CLAUDE.md src/Services/Fokus/CLAUDE.md` → 0, apart from the two sync services;
  - `grep -rn "There is no separate Application project\|No separate Application project\|Thin domain" CLAUDE.md docs/architecture docs/conventions src/Services/Fokus/CLAUDE.md` → 0;
  - `grep -ci "mapster" CLAUDE.md` → 0.

### Step 13 — The final comparison

- **Run** the harness on the finished tree, and a fresh base capture from the `base` extract, on the same UTC date (recorded).
- **Accept:** the comparison method of Step 2 holds on all three parts (replies, SQL counts, API document), for fixture variants A, B and C. Paste the results into `implementation.md`.
- **Any difference is a defect to fix, never to accept.** The same method runs at every slice end: after Steps 4, 6, 7 and 10.
- **Cleanup:** remove both extracts after (`rm -rf "/d/temp/f39-golden/base" "/d/temp/f39-golden/pristine"`).

### Step 14 — Operator check — Owner: operator

1. Start the API on the real database; it starts.
2. Open every analytics tab, the Dashboard, Developers (one detail page), Sprints, Team and Settings → Workflow → Re-detect. The values look as before.
3. Run one Jira sync and, if configured, one Xray sync. Both complete.

## Cross-Service Changes

None.

## Migration Notes

None.

## Must-NOT-Change

| Invariant | Pinned by | Disposition |
|-----------|-----------|-------------|
| Every manifest reply (GETs incl. workflow detect, the sync POSTs with a failing item, the PUT round-trips) in fixture variants A, B and C: status and bytes, with the one stack-trace normalization | Step 2 harness, diffed at slice ends and Step 13; fixture and sabotage checks prove it can fail | pinned |
| Request and reply property names, paths and verbs | API-document diff at Steps 8 and 13 | pinned |
| SQL command count per request does not rise | harness interceptor, Steps 9 and 13 | pinned |
| Sync `catch` blocks, broadcast included | Step 10 diff check; harness failing-item POSTs | pinned |
| Tests unchanged in meaning | Step 11 test-diff review and fact count 41 | pinned |
| No schema change | `git diff --stat {base} -- src/Services/Fokus/Fokus.Persistence/Migrations` empty | pinned |
| `Fokus.Domain/Analytics` is pure: no DbContext, repository, logger, options, clock or JSON | Steps 6 and 8 greps; `grep -rnE "ILogger|IOptions|DbContext|Repository" --include=*.cs src/Services/Fokus/Fokus.Domain/Analytics` → 0 | pinned |

## Testing Strategy

No new committed tests (owner ruling). The proof is the external golden harness (Steps 2 and 13, plus the slice ends). It covers every read route, the workflow detect, the syncs with a failing item and the PUT round-trips, on a seeded fixture with an active sprint relative to the run date. Its fixture check and sabotage check prove it can fail. The 41 existing facts move with their classes. The operator check covers real data and real outside systems.

## KB Impact

Key-file paths and type names only (Step 12): 19 references in 15 files at plan time. No business rule or formula changes.

## Decisions

| # | Decision | Why | Rejected alternative | Status |
|---|---|---|---|---|
| D1 | Application holds injected capability classes and no mediator; the endpoint stays the coordinator | the owner's knowledge-gateway shape (ADR-F192b); the plugin `cqrs-patterns` FastEndpoints form, merged locally in Step 1 | MediatR commands/queries | decided |
| D2 | `Fokus.API` keeps its `Persistence` reference; only the fat endpoints and the syncs get Application classes | the Settings/Auth endpoints load one row, and a pass-through class adds files, not clarity | route every load through Application | decided |
| D3 | Calculators take the skill's vocabulary names; the two sync classes keep `…SyncService` | `domain-service` naming; knowledge-gateway keeps `…Service` for a class around another system | rename the sync classes | decided |
| D4 | The clock is a calculator parameter; callers read UTC dates from `TimeProvider` | keeps Domain pure; the original code used UTC dates | inject `TimeProvider` into Domain | decided |
| D5 | Behaviour is proven by an external golden harness | no new committed tests (owner); 4 of 18 classes plus 1 helper have tests | characterization tests in the repo | decided |
| D6 | A helper variant merges only where byte-identical or provably equal; differing variants stay, named | a value-changing merge would hide in a large move | normalise every variant | decided |
| D7 | All long methods (25 over 80 lines) are decomposed here, in their own slice | the owner's aim is to shrink the big handlers; the harness pins the output | move now, decompose later | decided |
| D8 | `TransitionEdge` and `TransitionEdgeData` move to Domain | a calculator input must be a domain type | keep them in Persistence and map them | decided |
| D9 | The harness uses a synthetic, seeded database | the comparison is the agents' check; the owner supplies nothing (owner, 2026-10-14) | a copy of real data | decided |
| D10 | Registration methods keep this repo's `AddFokus{Layer}` names | the existing `AddFokusServices`/`AddFokusPersistence` form; the skill's `Add{Layer}Services` would rename both for no gain | rename to `AddApplicationServices` etc. | decided |
| D11 | `GetDeveloperDetail`'s activity check stays inline | it is an any-sprint check, not the per-sprint `ExcludedDeveloperFilter`, and not provably equal | replace it with the filter | decided |
| D12 | The base snapshot is a pair of `git archive` extracts (`base`, `pristine`), not a git worktree | the owner's "no git writes" constraint; the extract is byte-identical to the checkout (verified), and the determinism check adds a main-tree capture to prove it behaves the same | `git worktree add`/`remove` (a git write) | decided |
| D13 | The eight byte-identical 4-field sprint-info records merge into one Domain `SprintRef` (glossary: Sprint Reference) | D6-legal, wire bytes unchanged; `SprintInfo` is taken by the 6-field record until Step 5 | reuse `SprintInfo`; defer to Step 7 | decided |
| D14 | Analytics area folders and namespaces are `Sprints/` and `Developers/` (plural), in Domain and Application; no area name equals an aggregate type name | `…Analytics.Sprint` hides the `Sprint` aggregate (CS0118, reproduced by the developer); plural matches `Fokus.API/Features/Sprints` and `…/Developers`, and keeps namespaces equal to folders | a per-file alias (a trap for every new file); a namespace that differs from its folder | decided |

## Open Questions

None.

## Plan Review

- **Round 1 (2026-10-14):** Codex primary (NO-GO: 11 HIGH, 9 MEDIUM, 1 LOW) + Sonnet second reader (REVISE: 2 HIGH, 12 MEDIUM, 9 LOW). Merged in `review-critic.md` § Round 1; folded by this rewrite.
- **Round 2 (2026-10-14):** Codex, a check of the fold only, MEDIUM and up. NO-GO: 22 closed, 4 partial, 1 HIGH + 6 MEDIUM (sync stack traces in replies; sync fixture prerequisites; the SyncSprints reply owning its failure type; helper-family declaration check; three-part comparison method; fixture variants B/C and the grace day; a check after every slice; Bash-safe commands). All folded as `fixed-real`. No CRITICAL, so there is no round 3. Record: `review-critic.md` § Round 2.
- **Amendment (2026-10-14, developer Phase 1):** questions.md Q1 (Steps 2 and 13 use `git archive` extracts — D12), Q2 (`SprintRef` — D13, Step 4; § Context count corrected to eight) and Q3 (Step 12 rewrites the root `CLAUDE.md` Tech Stack Mapster line; Step 1's Mapster grep covers all four merged skill folders). Out of scope, not added: the Mapster lines in `add-integration-event/workflows/Consumer.md:71` and `create-grpc-contract/workflows/Server.md:39` (skills F39 does not touch) — a separate small fix.
- **Amendment (2026-10-14, slice 2, Step 5):** questions.md Q4 — the `Sprint`/`Developer` area names clash with the aggregates (CS0118); areas become `Sprints/` and `Developers/` in Domain and Application (D14). Changed: § Domain Model Changes, Steps 5, 6 and 9; the developer applies the local `domain-service` Home-bullet edit in Step 5.
