# F39 — Service Layer Split — Implementation

**Model:** claude-opus-5-5

## Step 1 — Bring the skills in
**Files:** `.claude/skills/domain-service/SKILL.md` — NEW, plugin text + `This Repo's Form` (the `Fokus.Domain/Analytics/{Area}` homes, the shared kernel in `Shared/`, static helpers versus registered calculators, the clock as a parameter, results not replies, loaders in `Fokus.Application`, the `Jira.Contracts`-in-Domain departure). Rewrote Look First. Reworded the contrast section: no handler in Fokus, so "an endpoint, or an Application capability class". Added `analytics-computation-service` to the relationships table.
`.claude/skills/cqrs-patterns/SKILL.md` — MERGE: plugin text as the base, plus `This Repo's Form` (FastEndpoints with no mediator; the endpoint, Application capability classes and Domain calculators; `…Service` only for the sync classes; validators and message helpers; domain-event path). Dropped the old local copy's false facts: `Blocks.MediatR` (no such project), `BaseValidator<T>`, `AssignUserIdPreProcessor`/`IClaimsProvider` (none in the tree), and the Mapster registration line.
`.claude/skills/service-registration/SKILL.md` — MERGE: plugin text as the base, plus `This Repo's Form` (the `AddFokus{Layer}` names (D10); the Application layer's contents; the clock line; the module registrations; no `ConfigureApiOptions`; Persistence's publisher and interceptor; the forwarding lambdas and stateful singletons in the modules as known departures; the `Program.cs` order; no host, no bus, no background service). The old local copy had no frontmatter; the merged copy has it. Dropped false facts: the open-generic `Repository<>` + `AddDerivedTypesOf` (Fokus registers each repository explicitly), the shared-connection SQL Server pattern, FastEndpoints multi-assembly scanning, and the Mapster line.
`.claude/skills/create-feature/SKILL.md` — MERGE: plugin text as the base, plus `This Repo's Form` (the layers, the endpoint-owned reply and `{Operation}Extensions.cs`, existing request/reply shapes kept, no action base or `IdResponse`, routes unchanged, shared analytics logic in `Fokus.Domain/Analytics/Shared`). Added `user-invocable: true`.
`.claude/skills/create-feature/workflows/Mappings.md` — REPLACED: was the Mapster workflow; now the plugin's hand-mapping text plus `This Repo's Form` (`{Operation}Extensions.cs` per operation, wire unchanged, wrappers assembled in the endpoint). Line 58's library-name list reworded so the accept grep finds no mapping-library name.
`.claude/skills/create-feature/workflows/EndpointFastEndpoints.md` — MERGE: plugin text plus `This Repo's Form` (the admin policy, full-path routes with no constraints, `req` in existing endpoints, the coordinating endpoint, the one sanctioned broadcast `catch`).
`.claude/skills/create-feature/workflows/Validator.md` — MERGE: plugin text plus `This Repo's Form` (`Validator<T>` only, the `{Operation}Request`/`Command` names, the `Blocks.Core` helpers, the samples' `ValidId`/`RequiredWithMaxLength` not present here).
`.claude/skills/create-feature/workflows/Handler.md`, `EndpointCarter.md`, `EndpointMinimalApi.md` — REPLACED with the plugin text: mediator-style templates Fokus does not use and which carry no Fokus facts. `Handler.md`'s "the conventions' § Comments and Logging" pointer reworded (no such heading in `docs/conventions/csharp.md`).
`.claude/skills/create-feature/CHANGELOG.md` — F39 entry.
`.claude/README.md` — Information Layers row `(42 skills)` → `(43 skills)`.
**Skills:** None — plan Skill Mapping `(none)`, TDD no
**Decisions:**
- Each merged copy carries a `## This Repo's Form` section, as F38's `authorization-patterns` copy does, instead of edits scattered through the plugin text, so the plugin text stays recognisable and the repo's departures are listed in one place.
- Existing request/reply names and shapes are stated as kept. No plan step renames them, and Step 8 keeps the wire byte-identical.
- References to skills absent from `.claude/skills/`, and what was done with each:
  - `service-infra-conventions` (§ 1, § 3, § 6) in `service-registration` — reworded plainly (settings fail at start-up; the request context where the service has one; JSON settings once; keep the framework's default casing).
  - `create-host` (3×) in `service-registration` — dropped or reworded ("registered by the host"; "a separate decision; Fokus has no host").
  - `add-typed-client` in `service-registration` — reworded to "typed HTTP clients".
  - `add-state-machine` in `service-registration` (2×) and `create-feature` (1×) — reworded to "a state machine factory" or dropped.
  - `file-storage-patterns` in `service-registration` — reworded to "a second file store".
  - `project-lists.md` (§ Helpers, § Shared-state caches) — Look First rewritten in F38's form; "listed cache" reworded to "named cache".
  - `create-aggregate` § Field-Length Constants (no such heading locally) in `create-feature` and `Validator.md` — re-pointed at `src/BuildingBlocks/Blocks.Core/MaxLength.cs`.
- Repo facts kept in the copies, with the live file that confirms each:
  - FastEndpoints with no mediator → `src/Services/Fokus/CLAUDE.md`; no `MediatR` in `src/Directory.Packages.props`.
  - `AddFokusServices(configuration, environment)` / `AddFokusPersistence(configuration)` → `Fokus.API/DependencyInjection.cs:17`, `Fokus.Persistence/DependencyInjection.cs:13`, `Program.cs:8-9`.
  - `AddFokusApplication`, `TimeProvider.System` in `AddFokusServices` → target — lands in Step 3.
  - Domain calculators in `Fokus.Domain/Analytics/{Area}`, loaders in `Fokus.Application/Analytics/{Area}` → target — lands in Steps 4–9.
  - `SprintRef`, `SparklinePoint` and `TestingCrunchFlag` in `Shared/` → target — lands in Step 4.
  - `{Operation}Extensions.cs` maps → target — lands in Step 8 (no `*Extensions.cs` under `Features/` today apart from `XrayCredentialsStartupExtensions.cs`).
  - Sync classes in `Fokus.Application` → target — lands in Step 10.
  - `JiraOptions` validated at start-up, `XrayOptions` bound without validation → `Jira.RestApi/DependencyInjection.cs:17-20`, `Xray.GraphQL/DependencyInjection.cs:11-12`.
  - `IXrayClient` forwarding lambda; singleton `XrayBearerTokenManager`/`XrayRateLimiter` → `Xray.GraphQL/DependencyInjection.cs:22-24`. `IJiraClient` built per call → `Jira.RestApi/DependencyInjection.cs:40-51`.
  - Publisher and interceptor scoped in Persistence; SQLite `ConnectionStrings:Database`; explicit repositories → `Fokus.Persistence/DependencyInjection.cs:15-29`.
  - `Program.cs` start-up order; `UseFokusAuth`/`UseFokusMiddleware` extend `WebApplication` → `Program.cs`, `Auth/AuthMiddleware.cs:5`, `DependencyInjection.cs:50`.
  - `AccessPolicies.Admins` → `Fokus.API/Auth/AccessPolicies.cs`.
  - Full-path routes with no constraints → `grep '\[Http' Features` (e.g. `/api/analytics/developer-detail/{accountId}`).
  - `req` in 37 endpoints, `command`/`query` in 2 → grep over `HandleAsync(` parameters.
  - 24 `{Op}Request`, 14 `{Op}Command`, 1 `{Op}Query` request types → grep under `Features`.
  - No `IdResponse`, `IAction` or `ActionCommandBase` → grep under `src/Services`: 0.
  - Broadcast `catch` → `Sync/SyncSprints/SyncSprintsEndpoint.cs:51`.
  - Validation helpers `NotEmptyWithMessage`/`MaximumLengthWithMessage` → `Blocks.Core/FluentValidation/Extensions.cs:7,16`. `MaxLength.C0…C2048` → `Blocks.Core/MaxLength.cs`.
  - `FindByIdOrThrowAsync` → `Blocks.EntityFrameworkCore/Extensions/RepositoryExtensions.cs`.
  - `DispatchDomainEventsInterceptor`, `DomainEventPublisher` → `Blocks.EntityFrameworkCore/Interceptors/`, `Blocks.FastEndpoints/`.
  - Hand-written client types → `client/src/types/index.ts`.
  - `Fokus.Domain` → `Jira.Contracts` → `Fokus.Domain.csproj:11` (G-01, out of scope).
- Accept results:
  - self-containment grep (`nexus|\{plugin\}|claude-plugins|project-lists\.md|nexus-gen`) over the 4 folders → 0;
  - every backticked kebab-case name that is a skill exists (18 names, all present);
  - `conventions/{file}.md §` pointers in the 4 folders → none;
  - `grep -rni "Adapt<\|IRegister\|Mapster"` over the 4 folders → 0;
  - `find .claude/skills -mindepth 2 -maxdepth 2 -name SKILL.md | wc -l` → 43, and `.claude/README.md` says 43.
**Deviations:** None. No `src/` file touched.
**Unfinished:** whole unit run — cut at 30 s (the step close ran `verify-run.js --profile fast` once: `reason: "timeout"` at 34.0 s, during the build; the MSB4166 child-node lines in its output come from the kill. Step 1 changes no code.)

## Step 2 — The golden harness and the base capture
**Files:** none in the repository (owner constraint: the harness lives only in `D:\temp\f39-golden\`, never committed). Outside it:
- `D:\temp\f39-golden\base\` and `D:\temp\f39-golden\pristine\` — two read-only extracts of the base commit.
- `D:\temp\f39-golden\harness\src\{Program,Infra,Fixture,Fakes,Manifest}.cs` — the harness code, shared by two project files.
- `D:\temp\f39-golden\harness\base\Harness.csproj` and `harness\main\Harness.csproj` — identical apart from the absolute `ProjectReference` (the `base` extract's `Fokus.API.csproj`, or `D:\src\fokus`'s).
- `D:\temp\f39-golden\harness\manifest.json` — the request manifest with the expected status per entry.
- `D:\temp\f39-golden\compare.sh` — the three-part comparison method in one command: `compare.sh <baseCapture> <afterCapture>`.
- `D:\temp\f39-golden\captures\{name}\` — `replies/{A,B,C}/NNNN.txt`, `sql-counts.json`, `api.json`, `manifest.json`, `run-info.json`.
**Skills:** None — plan Skill Mapping `(none)`, TDD no (gap: no skill for a before/after output-comparison harness)
**Decisions:**
- **Base:** commit `5fe66f81689324367474d047fa79a0278a0b546e` (`git rev-parse HEAD` before any F39 source edit). `git status --porcelain -- src Directory.Packages.props` was empty. Extracted twice with `git archive 5fe66f8 | tar -xf - -C "/d/temp/f39-golden/{base,pristine}"`, and `diff -r` of the two extracts was empty. No git write (questions.md Q1, D12).
  - Line endings: the extract is CRLF (`core.autocrlf=true`), while some checkout files are LF (e.g. `Fokus.API/Program.cs`). The main-tree capture below shows this changes no reply byte.
- **Harness shape:** two project folders (`harness/base`, `harness/main`) compile the same `../src/**/*.cs`. Each tree under test keeps its own harness `obj/bin`, and the reference stays a plain absolute path. Entry type `Fokus.API.Hubs.SprintHub`; `Program.cs` unchanged.
- **Content root:** with `Microsoft.AspNetCore.Mvc.Testing` in a console app, `WebApplicationFactory` cannot find the content-root attribute, so it throws "Solution root could not be located". The harness reads the `Fokus.API` path from the generated `MvcTestingAppManifest.json` and sets `ASPNETCORE_TEST_CONTENTROOT_FOKUS_API` before the host starts.
- **Host settings** (`Infra.cs` `GoldenFactory`):
  - Environment `Production` for the replies; a separate `Development` host serves `/swagger/v1/swagger.json`.
  - Configuration overrides (in-memory, last source): `ConnectionStrings:Database = Data Source=D:\temp\f39-golden\run-{capture}-{variant}.db`; dummy Google `ClientId`/`ClientSecret`; `Xray:ClientId`/`ClientSecret` empty.
  - Also dummy `Jira:*` values (`InstanceUrl`, `Email`, `ApiToken`, `ProjectKey = FOK`). This addition to the plan's list stops the Development host's user secrets from bringing machine credentials, and gives `SyncXray` the project key it requires.
  - Logging off.
  - After start-up, the harness checks that the `FokusDbContext` connection string is the golden one, and aborts otherwise.
  - An `IStartupFilter` sets `TraceIdentifier = "golden-trace"` before the error middleware.
  - Default authentication scheme `Golden`: claims `NameIdentifier = 1` and `Role = Admin`. App user 1 is seeded active Admin, so `ActiveUserPreProcessor` passes.
  - `IJiraClient`/`IXrayClient` replaced by `FakeJiraClient`/`FakeXrayClient` singletons.
  - A `DbCommandInterceptor` counts reader, non-query and scalar commands; it is reset before each request.
- **Fakes, each with one failing item:**
  - `FakeJiraClient.GetSprintIssuesAsync` throws for sprint 106 (inside `/api/sync/sprints`' 105–107 range) and for future sprint 109 (`/api/sync/backlog`).
  - `FakeXrayClient` returns two test executions; `FOK-XTE2` carries two runs with the same id, so its per-item `catch` reports a warning.
  - All fake data is fixed or relative to `D`.
- **Seeding** (`Fixture.cs`), after start-up (migrations and the default settings row), through the host's `FokusDbContext`, fixed ids, `D = DateTime.UtcNow.Date` (recorded per capture in `run-info.json`; this run 2026-10-14):
  - **Sprints:** 6 closed, 14 days apart; sprint 107 active from `D-1` 08:00 to `D+9` 17:00 (current day 2: `isGracePeriod: true` in variant A); future sprint 108.
  - **Tickets:** 10 per sprint — stories and bugs, epic children of `FOK-900`/`FOK-901`, and two backlog epic children.
  - **Status transitions** covering:
    - done;
    - carry-over (each sprint's `-02` ticket is carried into the next and finished there; sprint 106's stays open in 107);
    - reopened (Done → In Progress → Done);
    - sidelined (`Parked`, the excluded-from-scope status);
    - a cycle (In Progress → To Do → In Progress);
    - added after planning (`WasCommitted = false`, added at start + 3 steps);
    - removed after planning (`RemovedAt`);
    - a zombie (`FOK-050`, in sprints 101–104).
  - **Developers:**
    - active: Alice, Bob, Carol, Erin, and the testers Quinn and Rae;
    - inactive: Dan;
    - excluded in one sprint: Bob (capacity 0 in 104, no tickets there). Carol has capacity 0 in 105 but completes tickets, so she is not excluded;
    - Erin works only in sprints 101, 102 and 105;
    - capacities include 0;
    - two sub-teams, Alpha and Beta.
  - **Test data:** 2 test executions per sprint, linked with `Tests` and `Blocks` links; runs Pass, Fail and Todo; 2 test sets.
  - **Settings:** workflow stages To Do → Done, done status `Done`, excluded status `Parked`, cycle time In Progress → Done, default health thresholds, `BoardId = 42`, `XrayEnabled` per variant, dummy Xray client id and secret.
  - **Variant B:** `XrayEnabled = false`; active sprint from `D-5` (current day 6, `isGracePeriod: false`, alerts present).
  - **Variant C:** sprint 107 closed (`D-12` to `D-2`), future sprint from `D+2`; no active sprint, so `hasActiveSprint: false` replies are captured.
- **Manifest** (`Manifest.cs`): 609 entries (A 209, B 200, C 200), 29 route templates (23 GET).
  - Every GET under `/api/analytics/*`, `/api/sprints*`, `/api/developers*`, `/api/team` and `/api/settings/workflow-stages/detect`:
    - once with no parameters;
    - once per sprint id 101–108, plus missing id 999, wherever the request takes a sprint;
    - with `subTeam` Alpha and Beta wherever it takes one (for the four sprint-path routes, on sprints 106 and 107);
    - with `last=2` wherever it takes one;
    - `developer-detail` per developer plus missing, and `dev-alice?last=2`;
    - `capacity` per developer, plus `dev-alice?sprintId=107`.
  - Variant A then runs the three PUT round-trips — capacity, team-config, sprint 108 — each followed by the GET that reads it back.
  - The three sync POSTs run last, because a sync re-stamps `Sprint.SyncedAt`, which the sprint-summary reply carries. B and C run every GET.
  - Expected statuses: 200; 404 for sprint 999, future sprint 108 (not an analytics sprint), the missing developer, and inactive Dan's `developer-detail`. The first run showed 108 and Dan as 404; both are the live rules, recorded as expected. The first run also showed `Role = "QA"` rejected by the team-config validator, so the testers' role and the PUT use the valid `Tester`/`Tech Lead`. Every capture since: 0 status mismatches.
- **The one normalization:** in `POST /api/sync/sprints` replies, the text after the first ` | ` in each `"error"` string is replaced by `<stack>` (`SprintIssueSyncService.cs:75` writes `ex.StackTrace` there). Nothing else is normalized.
- **Results:**
  - **Determinism:** `compare.sh base-1 base-2` (two extract captures) and `compare.sh base-1 main-0` (main tree before any F39 source edit) both print `COMPARISON: PASS`:
    - replies `diff -r` exit 0, 0 files differ;
    - SQL counts 609/609 entries, 0 risen, total 6536 both;
    - API document 56 operations, 1366 locations, 0 differences.
  - **Fixture check** (`F39Golden report`): every route has 200 replies with data. QA routes return `hasQaData: true` (developer-quality 22, qa-trends 8, qa-workload 22, qa-metrics 22, test-timeline 22, epic-progress 6). The workflow detect reply lists `["To Do","In Progress","Code Review","QA","Done"]` with `sidelined: ["Parked"]`. Each sync POST reached its failing fake item:
    - sprints: `failures: [{sprintId: 106, error: "Golden fake: Jira sprint 106 is unavailable. | <stack>"}]`;
    - backlog: `sprintFailures: 1`;
    - xray: warning "Failed to sync Test Execution FOK-XTE2: …".
  - **Sabotage check** (in the `base` extract), and the comparator itself:
    - (1) `HealthScoreCalculator.ScoreLowerIsBetter`'s `49 -` → `48 -`: replies differ in 7 files, `COMPARISON: FAIL`;
    - (2) `DeveloperProgressService.cs:89` `d.SubTeam == subTeam` → `!=`: 4 files differ, `COMPARISON: FAIL`;
    - both reverted from byte copies; `diff -r --exclude=bin --exclude=obj "/d/temp/f39-golden/base" "/d/temp/f39-golden/pristine"` exit 0, before and after the clean rebuild. No further exclusion was needed;
    - comparator self-test: one renamed reply property in `api.json` and one raised SQL count are both reported (`lost`/`gained`, `rose 9 -> 999`), exit 1.
  - **Coverage:** 609 manifest entries, 29 route templates; one capture takes about 11 s.
- **The slice-end method from here on:** rebuild `harness/main`, run `dotnet run --no-build -- capture 'D:\temp\f39-golden\captures\{name}'`, then `compare.sh base-1 {name}`. Where the UTC date has changed since `base-1`, capture a fresh base from `harness/base` on the same date first, and compare against that.
**Deviations:** Two additions to the plan's list, both inside Step 2's intent: the dummy `Jira:*` overrides, and the comparator self-test. The step close ran `verify-run.js --profile fast` once: pass, 10.5 s (no `src/` change in this step).

## Step 3 — The Application project
**Files:** `src/Services/Fokus/Fokus.Application/Fokus.Application.csproj` — NEW; references `Fokus.Domain`, `Fokus.Persistence`, `Jira.Contracts` and `Xray.Contracts`. No `Blocks.*` reference yet: nothing in the project uses one, and Step 4's moves go to Domain.
`src/Services/Fokus/Fokus.Application/DependencyInjection.cs` — NEW; `AddFokusApplication(this IServiceCollection services, IConfiguration configuration)`, empty until Step 5 brings the first registered calculator (Step 4's kernel classes are static, so they are not registered).
`src/Services/Fokus/Fokus.Application/GlobalUsings.cs` — NEW (`docs/conventions/csharp.md` § Namespace Imports: every service project has one).
`src/Fokus.slnx` — the project added under `/Services/Fokus/`.
`src/Services/Fokus/Fokus.API/Fokus.API.csproj` — references `Fokus.Application`; keeps `Persistence` (D2).
`src/Services/Fokus/Fokus.Tests/Fokus.Tests.csproj` — references `Fokus.Application` and `Fokus.Domain` directly.
`src/Services/Fokus/Fokus.API/Program.cs` — `AddFokusApplication(builder.Configuration)` called between `AddFokusServices` and `AddFokusPersistence` (D10).
`src/Services/Fokus/Fokus.API/DependencyInjection.cs` — `services.TryAddSingleton(TimeProvider.System)` in `AddFokusServices`, once (the skill's clock line; `TryAdd` keeps a clock a test registered first).
**Skills:** service-registration
**Decisions:** `IConfiguration` and `IServiceCollection` reach `Fokus.Application` transitively through `Fokus.Persistence`, as they reach Persistence itself, so no package line was added to `src/Directory.Packages.props`.
**Deviations:** None. Accept:
- `grep -c "Fokus.Application"` → `src/Fokus.slnx:1`, `Fokus.API.csproj:1`, `Fokus.Tests.csproj:1`;
- `git diff --stat -- src/Services/Fokus/Fokus.Domain/Fokus.Domain.csproj` → empty;
- step close `verify-run.js --profile fast`, one call: pass, 10.9 s (limit now 60 s). The full suite is the close gate's.

## Step 4 — The shared kernel's independent parts
**Files:**
- **Moved** to `src/Services/Fokus/Fokus.Domain/Analytics/Shared/` (namespace `Fokus.Domain.Analytics.Shared`, bodies unchanged):
  - `TransitionAttributionChecker.cs`, `StatusTransitionIndex.cs`, `CompletionChecker.cs`, `HealthScoreCalculator.cs` and `ExcludedDeveloperFilter.cs`, from `Fokus.API/Features/Analytics/`.
  - Every input they take is already a Domain type (`AppSettings`, `Sprint`, `SprintMembership`, `StatusTransition`, `Developer`, `DeveloperSprintCapacity`).
- **New** in `Fokus.Domain/Analytics/Shared/`:
  - `SprintRef.cs` — `record SprintRef(int Id, string Name, DateTime StartDate, DateTime EndDate)`, replacing the eight identical `{Area}SprintInfo` records (Q2, D13).
  - `SparklinePoint.cs` — moved from `SprintSummaryService.cs:34`.
  - `TestingCrunchFlag.cs` — moved from `TestTimelineService.cs:89`.
  - `DeltaResolver.cs` — `DeltaDirection(decimal|int)` and `DeltaPolarity(decimal|int, bool)`.
  - `IssueTypeChecker.cs` — `IsBug(SprintMembership)`.
  - `BusinessDayCalculator.cs` — `CountBusinessDays(DateTime, DateTime)`; keeps the one constraint comment its source carried ("exclude the transition day itself").
- `src/Services/Fokus/Fokus.Domain/Analytics/Workflow/TransitionEdge.cs` — NEW. `TransitionEdge` and `TransitionEdgeData`, moved together from `Fokus.Persistence/Repositories/TicketRepository.cs:5-11` (D8).
- `src/Services/Fokus/Fokus.Persistence/Repositories/TicketRepository.cs` — the two records removed; `using Fokus.Domain.Analytics.Workflow;` added. Its callers (`DetectWorkflowStagesEndpoint`, `WorkflowDetectionService`) reach the type through the API global using.
- `src/Services/Fokus/Fokus.API/Features/Analytics/`:
  - `BugRatio`, `CarryOver`, `CycleTime`, `DeveloperProgress`, `DeveloperQuality`, `Leaderboard`, `QaTrends`, `ScopeChange` `…Service.cs` — the `{Area}SprintInfo` record removed, and every use renamed to `SprintRef`. `DeveloperDetailService.cs` uses `SprintRef` where it used `DeveloperProgressSprintInfo`.
  - The private helper copies removed, each replaced by a `using static Fokus.Domain.Analytics.Shared.{Class};` line, so every call site is unchanged. Orphaned section headers (`// --- Delta helpers ---` and the like) were removed with them:
    - `BugRatioService` (IsBug, DeltaDirection ×2, DeltaPolarity ×2);
    - `DeveloperDetailService` (IsBug, CountBusinessDays);
    - `DeveloperProgressService` (CountBusinessDays);
    - `DeveloperQualityService` (DeltaDirection ×2, DeltaPolarity, GetStageIndex, ResolveStartIndex);
    - `DeveloperThroughputService` (IsBug, DeltaDirection ×2, DeltaPolarity ×2);
    - `LeaderboardService` (IsBug, DeltaDirection ×2, DeltaPolarity ×2);
    - `QaMetricsService` (GetStageIndex, ResolveStartIndex);
    - `QaTrendsService` (GetStageIndex, ResolveStartIndex);
    - `QaWorkloadService` (DeltaDirection, DeltaPolarity);
    - `SprintSummaryService` (IsBug).
  - `SprintSummaryService.cs` and `TestTimelineService.cs` also lose the `SparklinePoint` and `TestingCrunchFlag` declarations.
- `src/Services/Fokus/Fokus.API/GlobalUsings.cs` — `global using Fokus.Domain.Analytics.Shared;` and `…Analytics.Workflow;`.
- `src/Services/Fokus/Fokus.Tests/GlobalUsings.cs` — `global using Fokus.Domain.Analytics.Shared;` beside line 4. The services still live in `Fokus.API.Features.Analytics` until Steps 5–6, so line 4 stays. `TransitionAttributionCheckerTests.cs` compiles unchanged through it; no assertion changed.
- `docs/kb/glossary.md` — the **Sprint Reference** line beside **Sprint** (Q2).
**Skills:** domain-service
**Decisions:**
- **D6 merges** — every merged variant is provably equal on every input:
  - `IsBug`: 5 copies, byte-identical bodies (`m.Ticket?.IssueType == "Bug"`).
  - `DeltaDirection(decimal)` 5 copies and `(int)` 4 copies, byte-identical. `DeltaPolarity(decimal, bool)` 5 copies and `(int, bool)` 3 copies, byte-identical. The shared class carries the union of the overloads, so each caller binds the same overload it bound before.
  - `CountBusinessDays`: 2 copies, identical apart from one trailing comment.
  - `GetStageIndex`: 3 private copies, byte-identical to `TransitionAttributionChecker.GetStageIndex`.
  - `ResolveStartIndex`: 3 private copies, identical to `TransitionAttributionChecker.ResolveStartIndex` apart from comments and the `if (startIndex < 0) startIndex = 0;` line break. The plan names it a Step 7 family; it merges here because it is provably equal and uncoupled.
  - `{Area}SprintInfo`: 8 records with identical members in the same order, so the JSON bytes are unchanged.
- **Surviving variant:** `TestExecutionRepository.cs:291` keeps its private `GetStageIndex` (Persistence; Step 11's).
- **`using static` instead of qualifying each call:** call sites stay byte-identical, which keeps the move reviewable. A class's own private member still wins name lookup over a static import, so files that keep a same-named private method (`DeveloperQualityService.IsStartedInSprint`) are unaffected. The build proves no ambiguity.
- **Names** from the skill's vocabulary: `DeltaResolver`, `IssueTypeChecker`, `BusinessDayCalculator`. The method names stay as before, so Step 7's declaration grep still finds each family.
- **`TransitionEdge*` home:** `Analytics/Workflow/`, not `Shared/` — workflow detection is its only reader, and Step 6 moves `WorkflowDetection` to `Workflow/`.
- **Line endings:** `sed -i` rewrote some edited files from CRLF to LF. With `core.autocrlf=true` the index stores LF either way, so `git diff` shows only the content lines.
- **Test file location:** the test file stays in `Fokus.Tests/Features/Analytics/`. Step 11 owns test renames and stale `test-times.json` keys.
**Deviations:**
- `ResolveStartIndex` merged one step early (above).
- No `graphify update .` (the root `CLAUDE.md` asks for one after code edits): `graphify-out/` is not a developer-owned file. Flagged to the team lead.
- Accept:
  - class grep in `Fokus.API` → 0;
  - `TransitionEdge` records in Persistence → 0;
  - `record …SprintInfo` → 0;
  - `SprintRef` in the glossary → 1;
  - step close `verify-run.js --profile fast`, one call: pass, 3.8 s.
- **Slice-end comparison** (Step 2 method): `harness/main` rebuilt, `capture after-step4`, `compare.sh base-1 after-step4` → `COMPARISON: PASS`:
  - replies `diff -r` exit 0, 0 files differ, all three variants;
  - SQL counts 609/609, 0 risen, total 6536 both;
  - API document 56 operations, 1366 locations, 0 differences;
  - same UTC date as `base-1` (2026-10-14).

## Step 5 — Sprint-level calculators
**Files:**
- **Moved** to `src/Services/Fokus/Fokus.Domain/Analytics/Sprints/` (namespace `Fokus.Domain.Analytics.Sprints`, D14). Each `Fokus.API/Features/Analytics/{N}Service.cs` is split in two: `{N}Calculator.cs` holds the class, and `{N}Result.cs` holds its records. Method bodies are unchanged. `{N}` = `SprintSummary`, `ScopeChange`, `CarryOver`, `BugRatio`, `CycleTime`, `EpicProgress`, `TestTimeline`. The seven `…Service.cs` files are deleted.
  - Classes renamed `{N}Service` → `{N}Calculator`; method names kept (`ComputeSummary`, `ComputeSingleSprint`, …).
  - Result records renamed from `…Response` to `…Result`: `{ScopeChange,CarryOver,BugRatio,CycleTime}{MultiSprint,SingleSprint}Result`, `SprintSummaryResult`, `EpicProgressResult`. Same members, same order.
  - `TestTimelineResult` — NEW. It holds the six computed parts (burnup, scope overlay, crunch, post-sprint, untested, gap). `ComputeTimeline` now returns it in place of the reply.
  - `EpicQaData` (calculator input) moves with `EpicProgressCalculator` into `EpicProgressResult.cs`.
- `src/Services/Fokus/Fokus.Domain/Analytics/Shared/MetricCard.cs` — NEW. `MetricCard` moved from `SprintSummaryService.cs`. It is shared because `QaMetricsService`, `QaWorkloadService` and `GetQaMetricsQuery` also use it.
- **Reply wrappers kept in the API**, each moved into its endpoint's query file unchanged:
  - `BugRatioResponse` → `GetBugRatio/GetBugRatioQuery.cs`;
  - `CarryOverResponse` → `GetCarryOver/GetCarryOverQuery.cs`;
  - `CycleTimeResponse` → `GetCycleTime/GetCycleTimeQuery.cs`;
  - `ScopeChangeResponse` → `GetScopeChange/GetScopeChangeQuery.cs`;
  - `TestTimelineResponse` (all 11 members, `sealed`) → `Sprints/GetTestTimeline/GetTestTimelineQuery.cs`.
- `src/Services/Fokus/Fokus.API/Features/Sprints/GetTestTimeline/GetTestTimelineEndpoint.cs` — fills `TestTimelineResponse` from the calculator's result, plus the five values the old method set itself: `HasQaData: true`, `IsXrayEnabled: true`, `sprint.StartDate`, `sprint.EndDate` and `settings.PlanningWindowDays`. These are the same `sprint` and `settings` it passed to the calculator. The `using Fokus.API.Features.Analytics;` line is dropped (no longer needed).
- Endpoints `GetBugRatio`, `GetCarryOver`, `GetCycleTime`, `GetEpicProgress`, `GetScopeChange`, `GetSprintSummary`, `GetQaTrends` and `GetTestTimeline`: they inject `{N}Calculator`, and the constructor parameter is renamed `{n}Calculator`. Result type names are updated (`EpicProgressResult`/`SprintSummaryResult` empty replies; `TestTimelineCalculator.ComputeCrunchFlag`).
- `src/Services/Fokus/Fokus.API/DependencyInjection.cs` — the seven registrations removed.
- `src/Services/Fokus/Fokus.Application/DependencyInjection.cs` — `AddScoped<{N}Calculator>()` ×7 in `AddFokusApplication`, plus `using Fokus.Domain.Analytics.Sprints;`.
- `src/Services/Fokus/Fokus.API/GlobalUsings.cs`, `src/Services/Fokus/Fokus.Tests/GlobalUsings.cs` — `global using Fokus.Domain.Analytics.Sprints;`.
- `src/Services/Fokus/Fokus.Tests/Features/Analytics/SprintSummaryServiceTests.cs`, `ScopeChangeServiceTests.cs` — `_sut` type is now `{N}Calculator`. No other line changed. File and class renames are Step 11's.
- `.claude/skills/domain-service/SKILL.md` — § This Repo's Form **Home** bullet: `Sprints/`, `Developers/` and the no-aggregate-name rule, exactly as given in the questions.md Q4 answer.
**Skills:** domain-service
**Decisions:**
- **Which records are wrappers:**
  - The four `{N}Response(Mode, MultiSprint?, SingleSprint?)` records and `TestTimelineResponse` are reply wrappers (the plan § Context list). They stay in the API in the endpoint's query file until Step 8.
  - `SprintSummaryResponse` and `EpicProgressResponse` are the calculators' own return values, so they move as `…Result`. Their endpoints still send them directly until Step 8.
- **`MetricCard` to `Shared/`, not `Sprints/`:** the QA area uses it too (the skill's shared-kernel rule).
- **File shape:** one `{N}Calculator.cs` and one `{N}Result.cs` per calculator (the skill's Location). Each result file holds all of that calculator's public records, as the service file did.
- **Comments:** the `// --- Service ---`, `// --- Response record hierarchy ---`/`TestTimeline response records` and `// --- Response wrapper ---`/`Wrapper` section headers were removed with what they introduced. CycleTime's `Single-sprint response`/`Multi-sprint response` headers now read `Single-sprint`/`Multi-sprint`.
- **Body check:** each new `{N}Result.cs` + `{N}Calculator.cs` was diffed (`diff -w`) against `git show HEAD:…/{N}Service.cs`, with the renames applied. The only differences are the namespace and using lines, the removed wrapper and section headers, Step 4's removals and `SprintRef`, and the `TestTimelineResult` split.
**Deviations:**
- Plan amended mid-step by Q4 (D14): the folder is `Sprints/`, not `Sprint/`.
- The move script's first run stopped half-way (a regex group index). `SprintSummaryService.cs` was already deleted by then, and its `MetricCard` text was lost in that run. The text was recovered read-only with `git show HEAD:…/SprintSummaryService.cs` (Step 4 did not change it), and the body diff above covers the result.
- One unplanned check: a golden capture after this step (`capture after-step5`, about 11 s; the plan asks for one after Step 6). `compare.sh base-1 after-step5` → `COMPARISON: PASS`: replies 0 files differ; SQL counts 609/609, 0 risen, total 6536 both; API document 56 operations, 1366 locations, 0 differences; same UTC date 2026-10-14.
- Accept:
  - the `{N}Service.cs` existence loop → no output;
  - the no-aggregate-namespace grep → 0;
  - `grep -c "Sprints/"` in the skill → 1;
  - purity grep (`ILogger|IOptions|DbContext|Repository|System.Text.Json|DateTime.UtcNow…` over `Fokus.Domain/Analytics`) → 0;
  - step close `verify-run.js --profile fast`, one call: pass, 3.4 s.

## Step 6 — Developer, QA and workflow calculators; the clock as input
**Files:**
- **Moved** with the Step 5 shape (`{N}Calculator.cs` holds the class, `{N}Result.cs` its records; bodies unchanged apart from the clock, below). The nine `…Service.cs` files are deleted.
  - `src/Services/Fokus/Fokus.Domain/Analytics/Developers/` (namespace `…Analytics.Developers`, D14): `DeveloperProgress`, `DeveloperDetail`, `DeveloperQuality`, `DeveloperThroughput`, `Leaderboard`.
  - `src/Services/Fokus/Fokus.Domain/Analytics/Qa/` (namespace `…Analytics.Qa`): `QaMetrics`, `QaTrends`, `QaWorkload`. `QaWorkloadCalculator.cs` keeps its `internal NullableStringComparer`.
  - `src/Services/Fokus/Fokus.Domain/Analytics/Workflow/WorkflowStageResolver.cs` and `WorkflowDetectionResult.cs`, from `Fokus.API/Features/Settings/WorkflowDetectionService.cs`. The `using Fokus.Persistence.Repositories;` line is dropped; `TransitionEdgeData` has been in Domain since Step 4.
  - Classes renamed `{N}Service` → `{N}Calculator`, and `WorkflowDetectionService` → `WorkflowStageResolver`.
  - Result records renamed `…Response` → `…Result`: `DeveloperProgressResult`, `DeveloperDetailResult`, `DeveloperThroughputResult`, `QaTrendsResult`, `Leaderboard{MultiSprint,SingleSprint}Result`, `QaWorkload{MultiSprint,SingleSprint}Result`. `DeveloperQualityResult`, `QaMetricsResult` and `WorkflowDetectionResult` keep their names.
- **Wrappers:**
  - `LeaderboardResponse` → `Analytics/GetLeaderboard/GetLeaderboardQuery.cs`, unchanged.
  - `QaWorkloadResponse` deleted: no file but its own declared it. `GetQaWorkload` sends its own reply types, so it never reached the API document.
- **`SprintSummaryItem` merged into `SprintRef`:** same 4 members in the same order. Its users (`DeveloperThroughput`, `QaWorkload`, `GetQaWorkloadEndpoint`) now name `SprintRef`.
- **The clock (D4):**
  - `DeveloperProgressCalculator.ComputeProgress` gains a last parameter `DateTime today`, which replaces both `DateTime.UtcNow.Date` reads.
  - `DeveloperDetailCalculator.ComputeDetail` gains the same parameter. It is passed on to `ComputeCurrentSprintDetail` and `BuildTicketDetails`, replacing their two reads.
- `src/Services/Fokus/Fokus.API/Features/Analytics/GetDeveloperProgress/GetDeveloperProgressEndpoint.cs`, `GetDeveloperDetail/GetDeveloperDetailEndpoint.cs` — inject `TimeProvider timeProvider` (registered once in `AddFokusServices`, Step 3) and pass `timeProvider.GetUtcNow().UtcDateTime.Date`.
- Endpoints `GetDeveloperDetail`, `GetDeveloperProgress`, `GetDeveloperQuality`, `GetDeveloperThroughput`, `GetLeaderboard`, `GetQaMetrics`, `GetQaTrends`, `GetQaWorkload`, `GetSprintSummary`, `DetectWorkflowStages` — the renamed types and constructor parameters (`{n}Calculator`, `workflowStageResolver`), and `DeveloperThroughputCalculator.SelectRollingWindowSprintIds`.
- `src/Services/Fokus/Fokus.API/DependencyInjection.cs` — the nine registrations removed, along with the now-unused `using Fokus.API.Features.Analytics;` and `using Fokus.API.Features.Settings;`. Left there: the two sync services (Step 10).
- `src/Services/Fokus/Fokus.Application/DependencyInjection.cs` — nine `AddScoped` lines, plus the `Developers`, `Qa` and `Workflow` usings.
- `src/Services/Fokus/Fokus.API/GlobalUsings.cs` — `…Analytics.Developers`, `…Analytics.Qa`.
- `src/Services/Fokus/Fokus.Tests/GlobalUsings.cs` — the same two added. `global using Fokus.API.Features.Analytics;` (line 4) is removed: that namespace is now empty.
- `src/Services/Fokus/Fokus.Tests/Features/Analytics/DeveloperProgressServiceTests.cs` — the `_sut` type, and `today: DateTime.UtcNow.Date` as the last argument of all 19 `ComputeProgress` calls. That is the same date the calculator read before, so no assertion changed.
- `src/Services/Fokus/Fokus.Tests/Features/Analytics/DeveloperThroughputServiceTests.cs` — the `_sut` type and `DeveloperThroughputCalculator.SelectRollingWindowSprintIds` only.
- `src/Services/Fokus/Fokus.Domain/Analytics/Shared/HealthScoreCalculator.cs` — removed the history line "Extracted from SprintSummaryService …" from the class summary: comment discipline, and Step 12's grep would match it.
**Skills:** domain-service
**Decisions:**
- **`WorkflowStageResolver`, not `WorkflowDetectionCalculator`:** the class resolves the stage order and the sidelined statuses from transition edges. "Resolver" is the skill-vocabulary word for that; it computes no figure. Its result keeps the existing name `WorkflowDetectionResult`.
- **`SprintSummaryItem` → `SprintRef`** under D6/D13: byte-identical members, and the wire bytes are unchanged (the comparison below). It was the ninth 4-field sprint record, missed by the Step 4 count because of its name. It is used across two areas (Developers, Qa), so it needed a shared home anyway.
- **`QaWorkloadResponse` deleted rather than moved:** it was dead code. Moving it into `GetQaWorkloadQuery.cs` would have planted an unused type beside the real reply.
- **Clock parameter last, required, no default:** a forgotten caller fails to compile instead of silently reading `default(DateTime)`.
- **`var todayDate = today;`** kept in `DeveloperProgressCalculator`, so the body diff is the read alone. Step 7 decomposes this method.
**Deviations:**
- The plan says the `DeveloperThroughputServiceTests` calls pass an explicit date, but `ComputeThroughput` never read the clock. Only its type names changed.
- `SprintSummaryItem` merge and `QaWorkloadResponse` removal: not named in Step 6. Both fall under D6 and dead-code rules; reasons above.
- **Body check:** each moved file pair was diffed (`diff -w`) against its pre-step copy, with the renames applied. Differences: the namespace and using lines, removed section headers, the wrappers, `SprintSummaryItem`, and the clock lines only.
- **Accept:**
  - `find … -name "*Service.cs" | grep -E "/Analytics/|WorkflowDetectionService"` → no output;
  - the clock grep over `Fokus.Domain/Analytics` → 0;
  - the purity grep (`ILogger|IOptions|DbContext|Repository|System.Text.Json`) → 0;
  - the no-aggregate-namespace grep → 0;
  - step close `verify-run.js --profile fast`, one call: pass, 3.4 s. The `HealthScoreCalculator` comment edit came after it (comment only); the harness build below compiled it.
- **Slice-end comparison** (Step 2 method): `harness/main` rebuilt, `capture after-step6`, `compare.sh base-1 after-step6` → `COMPARISON: PASS`:
  - replies `diff -r` exit 0, 0 files differ, all three variants;
  - SQL counts 609/609, 0 risen, total 6536 both;
  - API document 56 operations, 1366 locations, 0 differences;
  - same UTC date as `base-1` (2026-10-14).

## Step 7 — Decompose the long methods; consolidate the coupled helpers
**Files:**
- **New** in `src/Services/Fokus/Fokus.Domain/Analytics/Shared/`:
  - `SubTeamFilter.cs` — `FilterMemberships(IReadOnlyList<SprintMembership>, string?, HashSet<string>? = null)`, `FilterDevelopers`, `GetDeveloperMemberships(Sprint, string, string?)`.
  - `CapacityResolver.cs` — `GetCapacity` (the per-sprint nested lookup).
  - `MetricCardFactory.cs` — `BuildMetricCard` returning `MetricCard` (the type's home is `Shared/`).
  - `TestCoverageCalculator.cs` — the QA coverage helpers: `GetActiveFeatureTicketKeys`, `BuildTestsTesByTicket`, `BuildRunsByTe`, `ComputeCoverageRate`, `ComputeExecutionRate`, `ComputePassRate`, `ComputeBugsFound`, and the private `GetEffectiveSp` they use.
- **New** `src/Services/Fokus/Fokus.Domain/Analytics/Sprints/ScopeMetricCardFactory.cs` — `BuildMetricCard` returning `ScopeMetricCard` (that record lives in `ScopeChangeResult.cs`, so a `Shared/` home would point Shared at an area).
- `Shared/DeltaResolver.cs` — `ResolveCardDelta(decimal?, string polarity)`: the direction and polarity block every metric-card builder repeated, written over `DeltaDirection`/`DeltaPolarity`.
- `Shared/TransitionAttributionChecker.cs` — two additions: the list-based `IsStartedInSprint(List<StatusTransition>, …) : bool` overload, and `GetTransitionCompletedMemberships`.
- **Decomposed**, helpers replaced by `using static` imports of the shared classes; no public signature changed:
  - `Sprints/`: `EpicProgressCalculator`, `SprintSummaryCalculator`, `ScopeChangeCalculator`, `CycleTimeCalculator`, `BugRatioCalculator`, `CarryOverCalculator`. In `TestTimelineCalculator`, only its `FilterMemberships` copy goes.
  - `Developers/`: `DeveloperProgressCalculator`, `DeveloperThroughputCalculator`, `DeveloperQualityCalculator`, `DeveloperDetailCalculator`, `LeaderboardCalculator`.
  - `Qa/`: `QaMetricsCalculator`, `QaTrendsCalculator`, `QaWorkloadCalculator`.
  - `Workflow/WorkflowStageResolver.cs`.
  - How: a long method became named private steps. Where a step needs many of the method's inputs, they travel as a `private sealed record …Context`, the shape `CycleTimeCalculator.TicketCycleResult` already used for a private record. Results of three or more fields are private records (`docs/conventions/csharp.md` § Decomposition Judgment).
- Outside the repo (owner constraint): `D:\temp\f39-golden\method-lengths.js` (the method-length count), `extract.js` and `step7\*.py` (the edit and compare scripts).

**Skills:** domain-service
**Decisions:**
- **Method-length count** — `node D:\temp\f39-golden\method-lengths.js <dir> [threshold=80] [top=5]`. It sits beside the harness and is never committed.
  - What it does: blanks strings and comments, finds each method from its modifier line (joining the next line when the name is on it, which covers tuple returns), then counts from the declaration to the matching `}`, or to the `;` that ends an expression body, inclusive.
  - Calibration: run on the base extract it reproduces the plan's figures (293 `ComputeEpicProgress`, 242 `ComputeProgress`, 180 `Detect`, …).
  - At base: 26 methods over 80 (25 in `Features/Analytics` services and helpers, plus `WorkflowDetectionService.Detect`).
  - Now, over `Fokus.Domain/Analytics`: 236 methods, 0 over 80. The five longest: 80 `QaMetricsCalculator.ComputeQaMetrics`, 77 `DeveloperDetailCalculator.ComputeDetail`, 76 `SprintSummaryCalculator.ComputeFlags`, 75 `DeveloperDetailCalculator.ComputeCurrentSprintDetail`, 75 `ScopeChangeCalculator.ComputeSingleSprint`.
  - The 75–80 methods are public entry points whose signatures run 8–17 lines, so their bodies are about 60. `ComputeFlags` (76) was under 80 at plan time and is untouched.
- **Helper families after the step** (accept grep over `Fokus.Domain` and `Fokus.API`); each family has one declaration, or each extra is named here:
  - `FilterMemberships` — 1 (`SubTeamFilter`). The 9 copies came in two variants:
    - (a) `List` in, returns the same list when there is no sub-team — 4 copies: QaMetrics, QaTrends, TestTimeline, EpicProgress;
    - (b) `IReadOnlyList` in, always a new list, optional excluded developers — 5 copies.
    - Variant (b) with no exclusion set is provably equal for every (a) caller: each (a) caller only reads the result and the source (grep for `Add/Remove/Clear/Sort/Insert/Reverse` on those lists: none). The only difference is a copy instead of the same instance, with the same elements in the same order.
  - `FilterDevelopers` — 1 (5 identical copies).
  - `GetDeveloperMemberships` — 1. 3 copies were identical. `DeveloperDetail`'s had no sub-team, so it now passes `subTeam: null`: with a null sub-team the `IsNullOrWhiteSpace` clause is always true, so the filter is the same.
  - `GetCapacity` — 2:
    - `CapacityResolver` — 3 identical copies, plus the same expression inline in `SprintSummaryCalculator.ComputeLeaderboard`;
    - surviving variant: `DeveloperProgressCalculator.GetCapacity` takes a flat `Dictionary<string,int>` lookup already narrowed to the active sprint, with no sprint id. The input is different.
  - `IsBug`, `GetStageIndex`, `ResolveStartIndex`, `CountBusinessDays` — 1 each (Step 4). `DeltaDirection`/`DeltaPolarity` — one class, with the `decimal` and `int` overloads (Step 4).
  - `IsStartedInSprint` — 2 overloads in `TransitionAttributionChecker`:
    - the index-based one returns `(IsStarted, StartedAt)`;
    - the new list-based one returns `bool`. It replaces 3 byte-identical private copies (`DeveloperQuality`, `QaMetrics`, `QaTrends`).
    - Different inputs, and the overloads differ in arity, so no call rebinds.
  - `BuildMetricCard` — 3, one per return type. All three take their direction and polarity from `DeltaResolver.ResolveCardDelta`:
    - `MetricCardFactory` → `MetricCard` (merged from `QaWorkload` and `SprintSummary`);
    - `ScopeMetricCardFactory` → `ScopeMetricCard` (merged from `CarryOver` and `ScopeChange`);
    - `BugRatioCalculator` (private) → `BugRatioMetricCard`, its only user.
    - Two more builders, under other names, use the same resolver: `CycleTimeCalculator.BuildCard` (→ `CycleTimeMetricCard`), and `QaMetricsCalculator.BuildQaMetricCard`, which is now `MetricCardFactory.BuildMetricCard(…, "positive-up", …) with { Rag = rag }`. Its old hand-written polarity was the `positive-up` branch.
    - Why `ResolveCardDelta` is equal: `delta > 0 ? "up" : delta < 0 ? "down" : "flat"` is `DeltaDirection`, and the two switch arms are `DeltaPolarity(d, true/false)`; any other polarity string is still `"neutral"`.
  - `ComputeRollingAverage` — 2, not provably equal:
    - `DeveloperDetail`: all-types SP of completed memberships, with the excluded-status filter and no sub-team; it is given the index;
    - `DeveloperThroughput`: feature-only SP completed from its own `ComputeMetrics`, with the sub-team; it finds the index by sprint id.
  - Sparkline builders — 4 stay, each over different inputs:
    - `SprintSummary.BuildSparkline`: memberships per sprint;
    - `QaMetrics.BuildQaSparkline`: TEs plus active keys;
    - `QaWorkload.BuildTeamSparkline`: TEs only;
    - `DeveloperQuality.BuildDevSparkline`: per-developer metrics.
  - Alert evaluators — 2 stay, with different rules:
    - `BugRatio.EvaluateAlert`: bug-ratio threshold streak, from settings;
    - `QaWorkload.EvaluateWorkloadAlert`: run share over 50% for 2 sprints.
  - The Persistence copies (`TestExecutionRepository` `GetStageIndex`, `IsStartedInSprint`) are Step 11's.
- **Consolidated beyond the plan's family list** (same rule: byte-identical, or provably equal), because the local `domain-service` § This Repo's Form says logic several analytics features use goes in `Shared/`, not a private copy per calculator:
  - the QA coverage helpers → `TestCoverageCalculator`:
    - `GetActiveFeatureTicketKeys` — 2 copies, plus `DeveloperQuality`'s `…ForDev`, identical;
    - `BuildTestsTesByTicket` — 3 copies;
    - `BuildRunsByTe`, `ComputeCoverageRate`, `ComputeExecutionRate` — 2 each;
    - `ComputePassRate` — 3 copies. `DeveloperQuality`'s returned `0m` where the others returned `0`; both are a decimal zero with scale 0;
    - `GetEffectiveSp` — 3 copies;
    - `ComputeBugsFound`, which also replaces the same inline loop in `QaTrends` and `DeveloperQuality`;
  - `GetTransitionCompletedMemberships` → `TransitionAttributionChecker`: 3 identical copies (`Leaderboard`, `DeveloperDetail`, `BugRatio`).
  - In-file: `QaWorkload.GetAttributedAccountIdStatic` was byte-identical to `GetAttributedAccountId` and is merged into it. `EpicProgress` repeated the effective-TE-id lookup 3 times and `CarryOver` its carry-over predicate 4 times; each is now one private method.
- **Dead code removed:**
  - in `WorkflowStageResolver.Detect`, the unused `totalWeight` sum (and its comment) and the `predecessors` map, written but never read;
  - the unused `defaultSpPerBug` parameter of `DeveloperDetailCalculator.ComputeCurrentSprintDetail`; its body read `settings.DefaultSpPerBug`, which is the same value.
- **Comments:** each moved method kept its rule comments (BR tags), placed above the method they now introduce. Dropped: the narration and history in the old `ComputeBugsFound` ("The plan says…", "check if it's loaded"), and section headers whose methods left the file.
- **Equivalence points a reader may check:**
  - **Evaluation order.** Pure computations were reordered only where nothing depends on the order:
    - `SprintSummary` builds each sparkline inside its card;
    - in `ScopeChange.ComputeSprintMetrics` the removed SP comes from its own pass over the same memberships in the same order, so it adds the same elements in the same sequence;
    - the `CarryOver` destination buckets sum the same elements in the same order.
  - **Insertion order.** `QaWorkload`'s person map is filled by the same three passes in the same order, so its insertion order (the tie order under the stable sorts) is unchanged.
  - **Merged branch.** In the bugs pass, the old "exists, else `GetOrCreate`" branch is the same as `GetOrCreate` alone.
**Deviations:**
- The added consolidations listed above go beyond the plan's family list. Reason: the skill's shared-kernel rule.
- No `graphify update .` (the root `CLAUDE.md` asks for one after code edits): `graphify-out/` is not a developer-owned file. Flagged to the team lead, as at Step 4.
- `boy-scout` was not invoked. The step is itself a whole-file cleanup of every file it touched.
- Accept:
  - method-length count → 0 over 80 (above);
  - family grep → the declarations above;
  - purity grep (`ILogger|IOptions|DbContext|Repository|System.Text.Json` over `Fokus.Domain/Analytics`) → 0;
  - clock grep → 0;
  - no-aggregate-namespace grep → 0;
  - step close `verify-run.js --profile fast`, one call: pass, 7.6 s.
- **Slice-end comparison** (Step 2 method): `harness/main` rebuilt, `capture after-step7`, `compare.sh base-1 after-step7` → `COMPARISON: PASS`:
  - replies `diff -r` exit 0, 0 files differ, all three variants;
  - SQL counts 609/609, 0 risen, total 6536 both;
  - API document 56 operations, 1366 locations, 0 differences;
  - same UTC date as `base-1` (2026-10-14).

## Step 8 — Each endpoint owns its reply
**Files:**
- **New** `src/Services/Fokus/Fokus.API/Features/Analytics/_Shared/` (namespace `Fokus.API.Features.Analytics.Shared`) — the reply shapes two or more analytics operations share, each with its map:
  - `SprintRefDto.cs` + `SprintRefExtensions.cs` (`ToSprintRefDto`) — 9 users: BugRatio, CarryOver, CycleTime, ScopeChange, Leaderboard, DeveloperThroughput, DeveloperProgress, DeveloperDetail, QaTrends;
  - `MetricCardDto.cs` (`MetricCardDto` with its `Rag` init member, `SparklinePointDto`) + `MetricCardExtensions.cs` — SprintSummary, QaMetrics;
  - `ScopeMetricCardDto.cs` + `ScopeMetricCardExtensions.cs` — CarryOver, ScopeChange;
  - `DayBreakdownEntryDto.cs` (`DayBreakdownEntryDto`, `CompletedTicketEntryDto`) + `DayBreakdownEntryExtensions.cs` — DeveloperProgress, DeveloperDetail.
- **Reply types appended to each `{Op}Query.cs`**, and a **new `{Op}Extensions.cs`** (`static class {Op}Extensions`, one `To{Dto}()` per reply type), for `Analytics/` `GetBugRatio`, `GetCarryOver`, `GetCycleTime`, `GetScopeChange`, `GetLeaderboard`, `GetDeveloperThroughput`, `GetDeveloperProgress`, `GetDeveloperDetail`, `GetEpicProgress`, `GetSprintSummary`, `GetQaTrends`, and `Sprints/GetTestTimeline`. Each reply type mirrors its Domain record member for member, in the same order, as a `sealed record` (`docs/conventions/csharp.md` § Type Conventions), with `IReadOnlyList<T>` for lists (§ Records) and the same nullability.
- **Mode wrappers** (`BugRatioResponse`, `CarryOverResponse`, `CycleTimeResponse`, `ScopeChangeResponse`, `LeaderboardResponse`) — their `MultiSprint`/`SingleSprint` members now take the operation's `…MultiSprintDto`/`…SingleSprintDto`; the endpoints assemble them from `result.To…Dto()`.
- **New top-level replies** for the six endpoints that sent a Domain result directly: `GetDeveloperThroughputResponse`, `GetDeveloperProgressResponse`, `GetDeveloperDetailResponse`, `GetEpicProgressResponse`, `GetSprintSummaryResponse`, `GetQaTrendsResponse`. In those endpoints the `Endpoint<,>` reply argument, the empty-reply branches (`new Get…Response(…)`, with `FlagsDto`, `EpicProgressSummaryMetricsDto`, `EpicProgressUnlinkedWorkDto`) and the send line changed.
- `Sprints/GetTestTimeline/GetTestTimelineQuery.cs` — `TestTimelineResponse` members typed with the new DTOs. `GetTestTimelineExtensions.ToTestTimelineResponse(this TestTimelineResult, Sprint, int planningWindowDays)` builds the reply; the five values the endpoint set by hand since Step 5 move with it. `GetTestTimelineEndpoint.cs` — one send line; the empty branch builds `TestingCrunchDto`/`PostSprintTestingDto`/`UntestedAtCloseDto`/`DevToTestGapDto`.
- `Analytics/GetQaMetrics/` — `QaMetricsResponse`'s three `MetricCard?` members are `MetricCardDto?`; the endpoint's private `MapResponse` moved to `GetQaMetricsExtensions.ToQaMetricsResponse`, mapping the cards with `ToMetricCardDto()`. `BuildEmptyResponse` stays in the endpoint.
- `Analytics/GetDeveloperQuality/` and `Analytics/GetQaWorkload/` — the replies were already the endpoint's own; their private map methods moved unchanged (bar names, `this` and the parameter name) to `GetDeveloperQualityExtensions` (`ToDeveloperQualityResponse`) and `GetQaWorkloadExtensions` (`ToQaWorkloadResponseDto` ×2 overloads, `ToQaWorkloadSprintInfoDto`, `ToWorkloadAlertDto`, `ToQaWorkloadEntryDto`, `ToQaWorkloadSingleEntryDto`, `ToQaWorkloadMetricCardDto`). Endpoint files: 252 → 201 and 333 → 199 lines.
- `src/Services/Fokus/Fokus.Domain/Analytics/Sprints/EpicProgressResult.cs`, `EpicProgressCalculator.cs` — Domain record `TestRunSummaryDto` renamed `TestRunSummary` (5 sites): a Domain result is not a DTO, and the reply type now carries the name `TestRunSummaryDto`.
- Outside the repo: `D:\temp\f39-golden\step8\gen.py` (reads the Domain records, writes the reply types and maps), `assemble.py`, `edits.py`, `edits2.py`.
**Skills:** create-feature (with `workflows/Mappings.md`)
**Decisions:**
- **Generated, then reviewed:** the reply types and their maps were written by `gen.py` from the Domain records, so every member is copied in order and none is missed; nested records are found by walking each root's members. Argument style per the skill: positional, with named arguments where a neighbouring member has the same type (nullable or not), so a swap cannot compile.
- **Names:** a reply type is the Domain name with `Result` dropped, plus `Dto` (`BugRatioMultiSprintResult` → `BugRatioMultiSprintDto`, `HealthScoreResult` → `HealthScoreDto`). A new top-level reply is `{Operation}Response` (skill § Requests, Replies and Names); existing reply names (`BugRatioResponse`, `QaMetricsResponse`, `QaWorkloadResponseDto`, …) are kept (skill § This Repo's Form: existing shapes kept). Map parameter names are the type name with the operation prefix dropped (`multiSprint`, `teamTrendEntry`), falling back to the full name where that gives a keyword or a name `csharp.md` § Naming by Intent bans (`info` → `developerDetailInfo`).
- **`_Shared/` for the four shapes several operations use:** the skill moves a shared `{Thing}Dto` to the area's `_Shared` folder when a second user appears; a copy per operation would undo Step 4's `SprintRef` merge on the reply side. Imported per file (`using Fokus.API.Features.Analytics.Shared;`, csharp.md § Namespace Imports), not globally: `GetQaWorkload` keeps its own `SparklinePointDto` class, and a global import would put two same-named types in its scope.
- **Not touched:** `Settings/DetectWorkflowStages` (its reply is already its own and embeds no Domain record; its inline map stays), `GetFailingTickets`, `GetUntestedTickets` (own replies, no Domain record).
- **Domain record inventory after the step** (`Fokus.Domain/Analytics`): 121 public records. None is a reply wrapper (the mode and single/multi wrappers are the five API `…Response` records), and none carries a JSON attribute.
  - 118 computation results: every record in the `*Result.cs` files of `Sprints/`, `Developers/`, `Qa/` and `Workflow/`, plus `Shared/` `MetricCard`, `SparklinePoint`, `SprintRef` and `TestingCrunchFlag`;
  - 3 calculator inputs: `EpicQaData`, `TransitionEdge`, `TransitionEdgeData`;
  - besides them, 32 private records inside calculators (Step 7's step records).
- **No Domain type reaches the wire:** the after-step `api.json` has 213 schemas; none bears the name of a type declared in `Fokus.Domain`.
**Deviations:**
- `GetDeveloperQuality`, `GetQaWorkload` and `GetQaMetrics` had their maps moved into `{Op}Extensions.cs`, although only `GetQaMetrics`' reply embeds a Domain record. Reason: the skill puts every hand map in `{Operation}Extensions.cs`, and Step 9 needs those endpoint files under 120 lines.
- `TestRunSummaryDto` → `TestRunSummary` in Domain: not named in the plan; reason above.
- `boy-scout` not invoked: every touched file was generated or moved whole in this step.
- No `graphify update .`: as at Steps 4 and 7.
- **Accept:**
  - `grep -rn "System.Text.Json" --include=*.cs src/Services/Fokus/Fokus.Domain/Analytics` → 0;
  - API document (F38 method, `compare.sh`): 56 operations, 1366 locations, 0 differences;
  - inventory above;
  - step close `verify-run.js --profile fast`, one call: pass, 4.0 s.
- **Unplanned check:** a golden capture (`capture after-step8`, about 11 s; the plan asks for one after Step 10). `compare.sh base-1 after-step8` → `COMPARISON: PASS`: replies 0 files differ, all three variants; SQL counts 609/609, 0 risen, total 6536 both; same UTC date 2026-10-14.

## Step 9 — Loading moves out of the fat endpoints
**Files:**
- **New** loaders in `src/Services/Fokus/Fokus.Application/Analytics/{Area}/` (namespaces `Fokus.Application.Analytics.{Qa,Developers,Sprints}`, D14). Each file holds the loader class and the public records it returns (`…Source`, plus `QaWorkloadScope`):
  - `Qa/QaWorkloadInputs.cs` — `LoadScopeAsync(sprintId)` → `QaWorkloadScope?`, `LoadMultiSprintAsync(scope, last)` → `QaWorkloadMultiSprintSource?`, `LoadSingleSprintAsync(scope, sprint)` → `QaWorkloadSingleSprintSource?`; private `LoadPriorSprintAsync`, `LoadSparklineWindowAsync`.
  - `Qa/QaTrendsInputs.cs` — `LoadAsync(last)` → `QaTrendsSource?`; private `LoadBugRatioInputsAsync` (the next sprint and the all-closed load the bug ratio needs).
  - `Qa/QaMetricsInputs.cs` — `LoadAsync(sprintId)` → `QaMetricsSource?`; private `SelectWindowIds`, `LoadTesBySprintIdAsync`.
  - `Developers/DeveloperQualityInputs.cs` — `LoadAsync(sprintId, last)` → `DeveloperQualitySource?`; private `ResolveTargetSprintIdsAsync`, `LoadSingleSprintWindowsAsync`, `LoadPriorSprintAsync`, `CollectTransitionSprintIds`, `MergeLoadedSprints`.
  - `Developers/DeveloperDetailInputs.cs` — `LoadAsync(accountId)` → `DeveloperDetailSource`, `LoadActiveSprintAsync(analyticsSprints)` → `DeveloperActiveSprintSource`.
  - `Sprints/SprintSummaryInputs.cs` — `LoadAsync(sprintId)` → `SprintSummarySource?` (the `ZombieMinSprintCount` constant moved with the zombie-count load); private `SelectWindowIds`.
  - A `null` source means "send the empty reply"; the endpoint owns that reply.
- **Endpoints rewritten** (`Fokus.API/Features/Analytics/`): each injects its loader and its calculators only, normalises `subTeam`, calls the loader, the calculator and its mapping extension. Lines: `GetQaWorkload` 333 (base) → 76, `GetDeveloperQuality` 252 → 39, `GetQaTrends` 155 → 45, `GetQaMetrics` 152 → 50, `GetSprintSummary` 133 → 81, `GetDeveloperDetail` 117 → 68.
  - `GetQaWorkload` keeps two private `Send…SprintAsync` steps (single, multi); `GetSprintSummary` keeps the exclusion filter and a private `ComputeQuality` step returning a private `SprintQuality` record (the QA metrics and crunch flag it feeds the summary); `GetQaTrends` keeps the bug-ratio calculator call that feeds the trends.
  - `GetDeveloperDetail`: the any-sprint activity check stays inline in `HandleAsync` (D11), between the two loader calls.
- `src/Services/Fokus/Fokus.Application/DependencyInjection.cs` — six `AddScoped<…Inputs>()` lines and the three `Fokus.Application.Analytics.*` usings.
- `src/Services/Fokus/Fokus.Application/GlobalUsings.cs` — `Blocks.Core` (`OrThrowNotFound`) and `Blocks.Exceptions` (`NotFoundException`); both reach the project through `Fokus.Persistence` → `Blocks.EntityFrameworkCore`, so no project reference was added.
- `src/Services/Fokus/Fokus.API/GlobalUsings.cs` — the three `Fokus.Application.Analytics.*` namespaces.
- Outside the repo: `D:\temp\f39-golden\step8\step9_decompose.py`.
**Skills:** service-registration
**Decisions:**
- **Same queries, same order:** each loader issues the repository calls its endpoint issued, in the same order, with the same early exits — a 404 or an empty reply happens after the same reads as before. The `requestedSprint` 404 checks and the developer-detail "not found or inactive" 404 moved into the loaders with the reads they follow. Pure steps between reads (sub-team normalisation, capacity lookups, the exclusion filter) may sit on either side; they issue no SQL.
- **Names:** loader `{Endpoint subject}Inputs` (the skill's `QaWorkloadInputs`); the record a loader returns is `{subject}Source` — what the calculator computes from. `QaWorkloadScope` is the workload's shared first load (settings, requested sprint, closed sprints, developers) that both modes continue from.
- **Window selection moved with the loading** (plan: "multi-repository loading and window selection"): sparkline, streak, prior-sprint and next-sprint choices are in the loaders. Calculator calls and the small glue that feeds one calculator from another (`GetQaTrends`' bug-ratio dictionary, `GetSprintSummary`'s quality inputs) stay in the endpoint, which coordinates.
- **Method sizes:** every new method is at most 60 lines (`method-lengths.js` over `Fokus.Application`: 18 methods, longest 60 `QaMetricsInputs.LoadAsync`, 55 `DeveloperQualityInputs.LoadAsync`). Three-field returns are named records (`BugRatioInputs`, `SingleSprintWindows`, `SprintQuality`); two-field returns are tuples (csharp.md § Decomposition Judgment).
- **Equivalences a reader may check:**
  - `DeveloperQualityInputs.LoadPriorSprintAsync` returns early on the negation of the old guard: `!(closedAnchorIndex > 0 || (targetIndex < 0 && ascending.Count > 0))` = `closedAnchorIndex <= 0 && (targetIndex >= 0 || ascending.Count == 0)`;
  - `QaTrendsInputs`: `last is null or 0` = the old `last == null || last == 0`;
  - `GetSprintSummary`: the QA branch tests `source.SprintTes is not null`, which the loader sets exactly when `settings.XrayEnabled` (the repository returns a list, never null);
  - `GetDeveloperDetail`: `statusTransitions` is the closed-sprint list when no active sprint was loaded, else the same concatenation as before;
  - `QaMetricsInputs`: prior memberships are taken in the source constructor (`priorSprint?.Memberships.ToList()`), the same in-memory list as before.
- **Comments:** the rule comments (BR8, BR15, BR19, BR20, closed-sprints-only, the active-sprint anchors) moved with their code; step-number narration (`// 5. Load …`) was not carried over.
**Deviations:**
- None from the step's content. Outside it: no `graphify update .` (as at Steps 4, 7, 8); `boy-scout` not invoked (every touched file was rewritten in this step).
- **Accept:**
  - the six endpoint files under 120 lines (counts above);
  - per manifest entry, after ≤ base SQL commands: `compare.sh base-1 after-step9` → 609/609 entries, 0 risen or unmatched, total 6536 both;
  - step close `verify-run.js --profile fast`, one call: pass, 3.3 s.
- **Unplanned check:** the same capture's replies `diff -r` exit 0 (0 files differ, all three variants) and API document 56 operations, 1366 locations, 0 differences → `COMPARISON: PASS`, same UTC date 2026-10-14.

## Step 10 — The sync services move to Application
**Files:**
- **Moved** `src/Services/Fokus/Fokus.API/Features/Sync/SprintIssueSyncService.cs` → `src/Services/Fokus/Fokus.Application/Sync/SprintIssueSyncService.cs` (namespace `Fokus.Application.Sync`), with its records `SprintIssueSyncResult`, `SprintBatchSyncResult`, `SprintSyncFailure` and `EpicDiscoveryResult`. Changed lines only: `using Fokus.Application.Xray;` in place of `using Fokus.API.Features.Xray;`; `using Microsoft.Extensions.Logging;` added (the plain SDK has no implicit logging using; the web SDK did); the namespace.
- **Moved** `src/Services/Fokus/Fokus.API/Features/Xray/XrayIssueSyncService.cs` → `src/Services/Fokus/Fokus.Application/Xray/XrayIssueSyncService.cs` (namespace `Fokus.Application.Xray`), with `XraySyncResult`. Changed line: the namespace only. Both files keep their CRLF bytes.
- `src/Services/Fokus/Fokus.API/Features/Sync/SyncSprints/SyncSprintsCommand.cs` — `SyncSprintsResponse.Failures` is `List<SprintSyncFailureDto>`; the new `sealed record SprintSyncFailureDto(int SprintId, string Error)` is the reply's own failure type; the `using Fokus.API.Features.Sync;` line goes.
- `src/Services/Fokus/Fokus.API/Features/Sync/SyncSprints/SyncSprintsExtensions.cs` — NEW; `ToSprintSyncFailureDto(this SprintSyncFailure)`.
- `src/Services/Fokus/Fokus.API/Features/Sync/SyncSprints/SyncSprintsEndpoint.cs` — `using Fokus.Application.Sync;`; `Failures = result.Failures.Select(SyncSprintsExtensions.ToSprintSyncFailureDto).ToList()`. Nothing else changed; the broadcast `catch` is untouched.
- `src/Services/Fokus/Fokus.API/Features/Sync/SyncBacklogSprints/SyncBacklogSprintsEndpoint.cs` — `using Fokus.Application.Sync;`. Its reply carries only counts and the API's `XraySyncSummary`, so it embeds no Application type.
- `src/Services/Fokus/Fokus.API/Features/Xray/SyncXray/SyncXrayEndpoint.cs` — `using Fokus.Application.Xray;` in place of `using Fokus.API.Features.Xray;`. Its reply (`SyncXrayResponse`) is its own already.
- `src/Services/Fokus/Fokus.API/DependencyInjection.cs` — the two sync registrations and the two now-unused `Fokus.API.Features.{Sync,Xray}` usings removed. No registration of a moved type is left in the API.
- `src/Services/Fokus/Fokus.Application/DependencyInjection.cs` — `AddScoped<SprintIssueSyncService>()`, `AddScoped<XrayIssueSyncService>()` and their two usings.
**Skills:** service-registration
**Decisions:**
- **Per-file imports** of `Fokus.Application.Sync`/`.Xray` at the three callers (csharp.md § Namespace Imports), not a global using: three files use them.
- **`SprintSyncFailureDto` is a `sealed record`** (skill: new types are records; csharp.md § Type Conventions) beside the existing `SyncSprintsResponse` class, whose shape is kept. Same two members in the same order, so the wire is unchanged.
- **The sync classes keep `…SyncService`** (D3): each is a class around another system.
**Deviations:**
- None. No `graphify update .` (as at Steps 4, 7–9).
- **Accept:**
  - `find src/Services/Fokus/Fokus.API -name "*Service.cs"` (bin/obj aside) → none;
  - catch blocks: whitespace-normalised extraction of every `catch (…) { … }` from the `HEAD` file and the current file is identical for `SprintIssueSyncService` (3 of 3), `XrayIssueSyncService` (4 of 4) and `SyncSprintsEndpoint` (1 of 1). `diff -w` of each moved file against `git show HEAD:` shows only the using and namespace lines listed above;
  - step close `verify-run.js --profile fast`, one call: pass, 3.3 s.
- **Slice-end comparison** (Step 2 method): `harness/main` rebuilt, `capture after-step10`, `compare.sh base-1 after-step10` → `COMPARISON: PASS`:
  - replies `diff -r` exit 0, 0 files differ, all three variants (the sync-sprints failure still reads `"Golden fake: Jira sprint 106 is unavailable. | <stack>"`);
  - SQL counts 609/609, 0 risen, total 6536 both;
  - API document 56 operations, 1366 locations, 0 differences;
  - same UTC date as `base-1` (2026-10-14).

## Step 11 — The test project and the Persistence copies
**Files:**
- `src/Services/Fokus/Fokus.Tests/Features/Analytics/{DeveloperProgress,DeveloperThroughput,ScopeChange,SprintSummary}ServiceTests.cs` → `…CalculatorTests.cs` (file renamed with `mv`, no git write), and the class renamed to match: `{ClassUnderTest}Tests`. Nothing else changed in this step. `TransitionAttributionCheckerTests` already had its class-under-test name.
- `src/Services/Fokus/Fokus.Persistence/Repositories/TestExecutionRepository.cs` — `GetFeatureTicketsWithCoverageAsync`'s in-memory started-in-sprint filter now builds a `StatusTransitionIndex` from the loaded transitions once and calls `TransitionAttributionChecker.IsStartedInSprint(sm.TicketId, index, sprintStart, sprintEnd, orderedStages, startIndex).IsStarted`. The private `GetStageIndex` copy was used only by that filter, so it goes; the Domain `GetStageIndex` is what the helper uses. `using Fokus.Domain.Analytics.Shared;` added (per-file: one file in Persistence uses it). The stale comment "replicate logic inline — TransitionAttributionChecker is in API layer" goes with the code it described.
**Skills:** None — plan Skill Mapping `(none)`, TDD no
**Decisions:**
- **The index-based overload, not the list-based one:** the list-based `IsStartedInSprint(List<StatusTransition>, …)` would still need the per-ticket `Where(...).ToList()` scan of all loaded transitions for every membership; the plan names "building the index the helper takes". Provably equal: `startIndex < 0` → false in both; the index's `ForTicket` keys compare ordinally, as `t.TicketId == sm.TicketId` does; and "a first qualifying transition exists" (`FirstOrDefault` after the same `Where`) is the old `Any` over the same predicate. No copy stays, so no twin comment was needed.
- **Tests stay in `Fokus.Tests/Features/Analytics/` and namespace `Fokus.Tests.Features.Analytics`:** the plan renames classes only; a folder move would put namespace lines into the test diff.
**Deviations:**
- **`.claude/test-times.json` stale keys not removed.** The plan says to remove them; the dispatch's owner constraint forbids touching `.claude/test-times.json`, and the owner's instruction wins. The file still keys 33 tests under the four `…ServiceTests` names; they no longer exist. Carried over for the close gate (its delta pass re-times changed test files).
- No `graphify update .` (as at Steps 4, 7–10).
- **Accept:**
  - **Test diff against the base** (`git show HEAD:{old path} | diff - {new path}`, read-only; HEAD is the base `5fe66f8`):
    - `DeveloperProgressCalculatorTests.cs` — class name, `_sut` type `DeveloperProgressService` → `DeveloperProgressCalculator`, and in all 19 `ComputeProgress` calls `excludedDeveloperIds: …);` → `excludedDeveloperIds: …,` + `today: DateTime.UtcNow.Date);` (the explicit date argument, Step 6);
    - `DeveloperThroughputCalculatorTests.cs` — class name, `_sut` type, and 2 × `DeveloperThroughputService.SelectRollingWindowSprintIds` → `DeveloperThroughputCalculator.…`;
    - `ScopeChangeCalculatorTests.cs`, `SprintSummaryCalculatorTests.cs` — class name and `_sut` type only;
    - `TransitionAttributionCheckerTests.cs` — none (`diff --strip-trailing-cr` empty; `git diff` empty);
    - `GlobalUsings.cs` — `global using Fokus.API.Features.Analytics;` → the four `Fokus.Domain.Analytics.{Shared,Sprints,Developers,Qa}` usings; `Fokus.Tests.csproj` — the two `ProjectReference` lines (Step 3); `_Fixtures/` — none.
    - So: only `using` lines, type and class names, and the explicit date arguments. No assertion changed.
  - **Fact count 41 before and after:** `dotnet test … --no-build --list-tests` → 41 (DeveloperProgressCalculatorTests 19, TransitionAttributionCheckerTests 8, ScopeChangeCalculatorTests 6, SprintSummaryCalculatorTests 5, DeveloperThroughputCalculatorTests 3); `[Fact]`/`[Theory]` count in the base extract's `Fokus.Tests` → 41, in the tree → 41. No test carries a `Speed` trait, so the fast profile runs all 41.
  - step close `verify-run.js --profile fast`, one call: pass, 4.0 s. The Persistence change is pinned by the harness at Step 13 (it feeds the QA routes' feature-ticket coverage).

## Step 12 — Rules, the architecture doc, conventions, skills and KB
**Files:**
- `CLAUDE.md` (root) — line 34 replaced with the plan's text (Application when the logic outgrows the endpoints; capability classes, no mediator; the endpoint coordinates; calculators in Domain). Repo Structure gains `{Svc}.Application/` and the Domain line adds "pure calculators". Tech Stack's Mapster line → "Hand-written `To…()` mapping extensions in `{Op}Extensions.cs` — object mapping; no mapping library."
- `docs/architecture/v1.md` — the System Shape block: four-project tree with `Fokus.Application`, the new reference graph, and the coordinator paragraph in place of "No separate Application project". DDD § Constraints: "No service layer classes" → no generic service layer: Domain calculators or Application capability classes, `…Service` only around another system. "Thin domain in v1" → "Domain holds the analytics rules" (pure calculators in `Fokus.Domain/Analytics/`, the clock as a parameter).
- `docs/conventions/project-rules.md` — Guardrails line 22 per the plan; Repo Structure gains Application; the reference graph is API → Application → Domain, Application → Persistence → Domain, API → Persistence (D2), with bullets for API (also references Domain directly, as its csproj does), Application and Persistence.
- `docs/conventions/coding-conventions.md` — the cross-feature helpers line, per the plan.
- `src/Services/Fokus/CLAUDE.md` — new **Analytics calculators** line (`Fokus.Domain/Analytics/{Sprints,Developers,Qa,Workflow}/`, the `Shared/` kernel, purity, `today`); **Application layer** names `Fokus.Application` and its contents (the `…Inputs` loaders, the two sync classes; `AddFokusApplication` also registers the instance calculators); the Endpoint pattern says `HandleAsync` reads, loads (repository or loader), calls the calculator and maps in `{Operation}Extensions.cs`; Computed metrics says who computes, loads and maps; "Focused operation services" → **Shared logic** (calculator or capability class; `…Service` only around another system).
- `.claude/skills/analytics-computation-service/SKILL.md` — rewritten: frontmatter description, a three-homes table (calculator in Domain, loader in Application, reply and mapping in the endpoint), When to use, Reference files (the `Fokus.Domain/Analytics` calculators, the `Shared/` kernel, `QaWorkloadInputs`, `GetBugRatio/`), Calculator structure (results not replies, no dependencies, `today`, registration in `AddFokusApplication`), Endpoint wiring. The repository-query exception is gone. Delta triplet (now pointing at `DeltaResolver`), sparkline, rolling average, multi/single and Xray fast path kept.
- `.claude/skills/extract-feature-service/SKILL.md` — rewritten as "Extract Shared Endpoint Logic": a table choosing Domain calculator / Application loader / Application class around another system, the steps for each, the guardrails (no mediator added). The folder name stays, so no reference breaks and the skill count stays 43.
- `.claude/skills/create-service/SKILL.md` step 7, `workflows/ScaffoldApplicationProject.md` (eligibility = the root rule, pure rules go to Domain; the FastEndpoints exemplar is `Fokus.Application`; the FastEndpoints folder branch creates folders per capability with no `Dtos/`/`Mappings/`; a FastEndpoints "what goes in Application" list), `workflows/ScaffoldFolders.md` (the conditional-tree note), `workflows/ScaffoldCsprojFiles.md` (API keeps Persistence where endpoints load single rows — Fokus D2 — in the header list and as the one exception to "one reference path per concern").
- KB — 16 entries (the plan counted 15 files at plan time; `jira-sync.md` and `xray.md` carry the sync paths too): `analytics/{bug-ratio,carry-over,cycle-time,daily-progress,developer-detail,epic-progress,health-score,leaderboard,scope-change,test-timeline,throughput}.md`, `cross-cutting.md`, `domain/{developer,ticket}.md`, `jira-sync.md`, `xray.md`. Every `Fokus.API/Features/Analytics/{N}Service.cs` key file → `Fokus.Domain/Analytics/{Area}/{N}Calculator.cs`; the helper paths → `Fokus.Domain/Analytics/Shared/`; the sync paths → `Fokus.Application/{Sync,Xray}/`; prose `{N}Service` → `{N}Calculator`. Three lines whose meaning moved beyond a rename were rewritten: developer-detail's completed filter now names the shared `TransitionAttributionChecker.GetTransitionCompletedMemberships` (Step 7 merged the leaderboard copy); epic-progress's transition lookup is the calculator context's `TransitionsByTicket`, and "Service QA helpers" → "Calculator QA helpers"; leaderboard's `DeveloperSummary` record is in `SprintSummaryResult.cs`. No business rule or formula changed.
**Skills:** None — plan Skill Mapping `(none)`, TDD no (a doc sweep under the `kb-maintenance` rule)
**Decisions:**
- **The two skills keep their folder names.** The plan rewrites them; renaming `analytics-computation-service` or `extract-feature-service` would change the skill list, the `.claude/README.md` count and every pointer for no gain. Their descriptions and bodies carry the new homes.
- **`extract-feature-service` rewritten, not retired:** the plan allows either; the rewrite keeps a useful decision table (which home for which logic) that no other local skill holds in one place.
- **No graphify update** (as at Steps 4, 7–11); this step touches no code.
- **KB edits by `sed`, then CRLF restored.** Git Bash `sed -i` wrote the files back with LF; `unix2dos` restored CRLF on all 17 files it rewrote (`git ls-files --eol docs/kb` → 21 × `w/crlf`). `glossary.md` was in that set, but its content is unchanged by this step (its diff is Step 4's line).
**Deviations:**
- None against the plan's text. Additions inside the step's intent: `project-rules.md` says API also references Domain (true of `Fokus.API.csproj`; the old text said so too), and `ScaffoldCsprojFiles.md`'s "one reference path" rule gained the D2 exception, which the new project-rules graph would otherwise contradict.
- **Accept:**
  - the moved-class grep over `docs/kb .claude/skills docs/conventions docs/architecture CLAUDE.md src/Services/Fokus/CLAUDE.md` → 0 lines (the two sync services are not matched by the pattern, and their paths now read `Fokus.Application/…`);
  - `grep -rn "There is no separate Application project\|No separate Application project\|Thin domain" …` → 0;
  - `grep -ci "mapster" CLAUDE.md` → 0;
  - **KB lint:** broken-index-link 0 (20 links, each file exists); orphan-entry 0 (21 files, 20 entries linked, plus the index); stale-key-file 0 (all 56 backticked paths with a folder resolve, and every bare file name exists under `src/` or `client/src/`);
  - `find .claude/skills -mindepth 2 -maxdepth 2 -name SKILL.md | wc -l` → 43 (unchanged);
  - step close `verify-run.js --profile fast`, one call: pass, 3.4 s.

## Step 13 — The final comparison
**Files:** none in the repository. Outside it (owner constraint, never committed): `D:\temp\f39-golden\captures\base-final\`, `captures\final\`, and the comparator reports `captures\diff-base-final-vs-final.txt`, `diff-base-1-vs-base-final.txt`, `diff-base-1-vs-final.txt`. The `base` and `pristine` extracts were removed afterwards (plan § Cleanup), so `harness\base` no longer builds; `harness\main`, `compare.sh` and every capture stay.
**Skills:** None — plan Skill Mapping `(none)`, TDD no (gap: as Step 2)
**Decisions:**
- **Integrity before the base capture:** `diff -r --exclude=bin --exclude=obj base pristine` → identical, so the fresh base ran from an untouched base extract.
- **`harness/main` rebuilt against the finished tree;** its `Fokus.API`, `Fokus.Application` and `Fokus.Persistence` DLLs are newer than the Step 11 `TestExecutionRepository` edit, so the capture covers Steps 11–12's code (Step 12 touched none).
- **A cross-check against `base-1` as well**, beside the plan's fresh base: it shows the fresh base equals the Step 2 baseline, and the finished tree equals both.
**Deviations:**
- None. The first `compare.sh` call passed capture paths instead of names, so it found no files and printed `FAIL`. That was a usage error, not a comparison, and its stray report was deleted. The re-run with names is the result below.
- **Accept** — fixture variants A (209 entries), B (200) and C (200); both captures on UTC date **2026-10-14** (`run-info.json`: base `08:19:28Z`, final `08:19:40Z`), 609 entries, 29 routes, 0 status mismatches each:
  - `compare.sh base-final final` → **`COMPARISON: PASS`**:
    - replies: `diff -r` exit 0, 0 files differ (per variant: A, B and C each exit 0);
    - SQL counts: 609 base / 609 after entries, 0 risen or unmatched, total 6536 both;
    - API document: 56 operations, 1366 locations each side, 0 differences.
  - `compare.sh base-1 base-final` → `COMPARISON: PASS` (same figures);
  - `compare.sh base-1 final` → `COMPARISON: PASS` (same figures).
  - **Must-NOT-Change pins, checked at the end:** `git diff --stat 5fe66f8 -- src/Services/Fokus/Fokus.Persistence/Migrations` empty, and no untracked file there; `grep -rnE "ILogger|IOptions|DbContext|Repository"`, the clock grep and the `System.Text.Json` grep over `Fokus.Domain/Analytics` → 0 each; `find src/Services/Fokus/Fokus.API -name "*Service.cs"` → none.
  - step close `verify-run.js --profile fast`, one call: pass, 3.5 s.

## Fix Round 1
**Files:**
- `src/Services/Fokus/Fokus.Domain/Ticket/Behaviors/Ticket.cs` — NEW method `GetEffectiveSp(int defaultSpPerBug)` on the `Ticket` behaviour partial: the same rule as `SprintMembership.GetEffectiveSp`, over the ticket's own `StoryPoints` and `IssueType` (row 3).
- `src/Services/Fokus/Fokus.Domain/Analytics/Sprints/SprintSummaryCalculator.cs` — private `GetEffectiveTicketSp` and its "mirrors" comment deleted; its 3 call sites now call `t.GetEffectiveSp(defaultSpPerBug)` (row 3).
- `src/Services/Fokus/Fokus.Domain/Analytics/Sprints/EpicProgressCalculator.cs` — private `GetEffectiveTicketSp` and its comment deleted; its 8 call sites now call `t.GetEffectiveSp(…)` (row 3).
- `src/Services/Fokus/Fokus.Domain/Analytics/Shared/TestCoverageCalculator.cs` — private `GetEffectiveSp(SprintMembership, int)` deleted; its one caller uses `m.GetEffectiveSp(defaultSpPerBug)` (row 2).
- `src/Services/Fokus/Fokus.Persistence/Repositories/TestExecutionRepository.cs` — private `GetEffectiveSp(SprintMembership, int)` deleted; its one caller uses `sm.GetEffectiveSp(defaultSpPerBug)` (row 2). CRLF restored with `unix2dos` after the `sed` edit.
- `docs/kb/analytics/epic-progress.md`, `docs/kb/cross-cutting.md` — the `GetEffectiveTicketSp` mentions now name `Ticket.GetEffectiveSp`; cross-cutting states that no private copy remains (kb-maintenance).
- `src/Services/Fokus/Fokus.API/Features/Analytics/GetQaWorkload/GetQaWorkloadQuery.cs` — the local `SparklinePointDto` class deleted; `using Fokus.API.Features.Analytics.Shared;` added, so `QaWorkloadMetricCardDto.Sparkline` now holds the shared record (row 5).
- `src/Services/Fokus/Fokus.API/Features/Analytics/GetQaWorkload/GetQaWorkloadExtensions.cs` — the inline `new SparklinePointDto { … }` replaced by the shared `p.ToSparklinePointDto()` map; same `using` added (row 5).
- `.claude/skills/create-feature/SKILL.md` — `This Repo's Form` line "No `_Shared` folders yet" replaced by the `Features/Analytics/_Shared/` folder: the shared reply shapes it holds, each beside its map, used instead of same-shaped copies; shared logic still in `Fokus.Domain/Analytics/Shared/` (row 4).
**Skills:** domain-service (row 3: "logic of one aggregate stays in that aggregate's behaviour methods" — the reason the ticket rule went onto `Ticket`, not into `Shared/`). create-feature for row 5 — invoked after the edit, not before it (deviation: the code was written first; the skill was then checked against it). The edit matches its rule "a type another operation also needs moves to `_Shared` when the second user appears" and its `{Operation}Extensions.cs` mapping form. Row 2 had no mapped skill (plan Step 11: `(none)`); row 4 is a doc edit; row 1 is a process item. TDD no: behaviour-preserving merges proven by the golden comparison.
**Decisions:**
- **Row 3 home:** a `Ticket` behaviour method, the twin of the existing `SprintMembership.GetEffectiveSp`, rather than a `Shared/` static. The rule reads one entity, the fix list allowed either, and the call sites read the same way for both entities. `SprintMembership.GetEffectiveSp` cannot serve the ticket callers: they hold `Ticket`s, and a membership's `StoryPoints` is its own snapshot, not the ticket's.
- **Row 2:** the three bodies (`SprintMembership`, `TestCoverageCalculator`, `TestExecutionRepository`) were checked line by line as the same rule before the private copies were deleted.
- `grep -rn "GetEffectiveTicketSp\|GetEffectiveSp(m,\|GetEffectiveSp(sm," src` → 0.
**Deviations:** None. Row 1 is met by the one `verify-run.js --profile fast` call below; no raw test command ran this round.
- **Accept:**
  - step close `verify-run.js --profile fast`, one call: pass, 5.2 s (ts `2026-10-14T08:36:46Z`);
  - golden proof in `D:\temp\f39-golden`: `harness/main` rebuilt (its `Fokus.Domain.dll`/`Fokus.Persistence.dll` dated after the edits; `GetEffectiveTicketSp` absent from the Domain DLL), capture `fix-1` on UTC date 2026-10-14 (same date as `base-1`), 609 entries, 29 routes, 0 status mismatches; `compare.sh base-1 fix-1` → **`COMPARISON: PASS`** — replies `diff -r` exit 0 (0 files differ), SQL counts 609/609 entries, 0 risen, total 6536 both, API document 56 operations, 1366 locations, 0 differences.
  - The two carry-over rows `GetEffectiveSp twin in Persistence` and `GetEffectiveTicketSp pair` are resolved by this round.
  - **Rows 4–5 (second instalment):** step close `verify-run.js --profile fast`, one call: pass, 3.7 s (ts `2026-10-14T08:38:59Z`); `harness/main` rebuilt (`Fokus.API.dll` dated after the edits), capture `fix-2` (2026-10-14, 609 entries, 29 routes, 0 status mismatches); `compare.sh base-1 fix-2` → **`COMPARISON: PASS`** (replies 0 files differ; SQL 609/609, 0 risen, total 6536; API document 56 operations, 1366 locations, 0 differences). `grep -rn "class SparklinePointDto" src` → 0; the one `SparklinePointDto` is the shared record.
  - **What the API comparison does not show:** a raw `diff final/api.json fix-2/api.json` (18 lines) shows the QA workload sparkline `$ref` now naming `FokusAPIFeaturesAnalyticsSharedSparklinePointDto`, and the `…GetQaWorkloadSparklinePointDto` component gone. Its two properties (`sprintName` string, `value` number/decimal) are the shared schema's, which is why the shape comparison finds 0 differences. Schema ids are namespace-qualified and already differ from `base-1` across F39's moves (raw diff `base-1` vs `final`: 1203 lines), so the comparator's shape check is the measure the run has used since Step 2; the wire JSON is unchanged.
  - **Row 6 (third instalment, owner decision) — announced before it ran:** extend the external harness with a variant D, re-extract the base, capture base and current tree, compare; then run the five surviving mutants named in `review-second.md` § Gaps against a scratch copy of the current tree, to show the extension kills them. Expected cost: about 6–8 minutes of builds and captures, waiting included.
  - **Row 6 — what changed (outside the repo, never committed):** `D:\temp\f39-golden\harness\src\Fixture.cs` and `Manifest.cs` gained a variant **D** (`Extended`); A, B and C are untouched (their replies in the new base equal `base-1`'s, `diff -rq` per variant). D has:
    - 14 closed QA sprints (101–114), active 115, future 116; one assignee rotation (Alice, Carol, Bob) so Bob is below the team's median coverage in every sprint — his `belowMedianStreak` on `developer-quality?sprintId=114` is **12** (the cap);
    - `dev-erin` at 0% in sprint 103 owning the unfinished `FOK-E01` (5 SP), so she is an excluded developer who owns a ticket;
    - sprint 110 with exactly one Pass/Fail run (one Pass, one Todo) — its qa-metrics `passRate` is 100;
    - `dev-zed`, active, with a 0% record in every closed sprint and no tickets — `developer-detail/dev-zed` is the "no activity in closed sprints" 404. A 0 default capacity could not be used: `DeveloperConfiguration`'s `HasDefaultValue(100)` stores a CLR 0 on insert as 100;
    - a `qa-trends?last=0` request (14 sprints in the reply, as with no `last`).
    - Ticket keys skip the `FOK-9xx` block (the epics), and the future sprint's keys follow the sprint count; both formulas give A–C's old keys.
  - **Row 6 — base and comparison:** base re-extracted read-only (`git archive 5fe66f8 | tar -x` into `D:\temp\f39-golden\base`, the Q1/D12 route); `harness/base` built (its `Fokus.API.dll` holds the old `SprintSummaryService`, the main build does not). Captures `ext-base` and `ext-main`, both UTC 2026-10-14: 916 entries (D adds 307), 29 routes, 0 status mismatches each. `compare.sh ext-base ext-main` → **`COMPARISON: PASS`** — replies 0 files differ, SQL 916/916 entries, 0 risen, total 11965 both, API document 56 operations, 1366 locations, 0 differences.
  - **Row 6 — the extension kills the five surviving mutants:** a scratch copy of the current `src` (`D:\temp\f39-golden\mut`, `diff -rq` identical to the tree) with its own harness project; unmutated capture `mut-0` vs `ext-main` → PASS. One mutant at a time, restored from a byte copy after each, each compared against `ext-main`:
    - `QaTrendsInputs` `last is null or 0` → `last is null`: **FAIL** (1 reply, D `qa-trends?last=0`);
    - `DeveloperQualityInputs` `TakeLast(12)` → `TakeLast(11)`: **FAIL** (3 replies, D developer-quality);
    - `SubTeamFilter.FilterMemberships` excluded-set clause disabled (`Count: > 0` → `> 99`): **FAIL** (3 replies, D);
    - `TestCoverageCalculator.ComputePassRate` `totalCount > 0` → `> 1`: **FAIL** (16 replies, D);
    - `GetDeveloperDetailEndpoint` `effectiveCapacity > 0` → `>= 0`: **FAIL** (D `dev-zed` 404 → 200, SQL 8 → 12).
    - Every killed reply is in variant D, so the kills come from the extension. After the last mutant the scratch copy was again identical to the tree.
  - **Row 6 — cleanup:** `base`, the scratch copy, its harness project and the mutant backups removed; `compare.sh ext-base ext-main` re-run afterwards from the kept `harness/base` binary → PASS. Kept: `harness/main`, `compare.sh`, every capture. No repository source changed for row 6, so no step-close call was owed; no git write. Actual cost about 7 minutes.

## Key Decisions
- **Base snapshot without git writes** (Step 2, Q1, D12): two `git archive` extracts (`base`, `pristine`); a main-tree capture proved the extract behaves as the checkout. Removed at Step 13.
- **The harness** (Step 2): a console `WebApplicationFactory` host outside the repo, Production environment, fixed trace id, a test auth scheme, fake Jira/Xray clients with one failing item each, a SQL command interceptor, a deterministic fixture relative to the UTC date in three variants, 609 manifest entries over 29 routes. Its one normalization is the sync stack-trace text. Dummy `Jira:*` overrides were added to keep machine secrets out.
- **One `SprintRef`** for the nine byte-identical 4-field sprint records (Step 4 eight, Step 6 `SprintSummaryItem`; D13), with a glossary line.
- **`using static` imports** for the shared kernel, so moved call sites stayed byte-identical (Step 4).
- **Calculator shape** (Steps 5–6): `{N}Calculator.cs` + `{N}Result.cs` per area folder (`Sprints/`, `Developers/`, `Qa/`, `Workflow/`; D14). Results are renamed `…Result`. `WorkflowDetectionService` became `WorkflowStageResolver`. Wrappers stay in the API. `MetricCard` lives in `Shared/`.
- **The clock** (Step 6, D4): a required last `DateTime today` parameter; the endpoints pass `timeProvider.GetUtcNow().UtcDateTime.Date`.
- **Consolidation** (Step 7, D6): one declaration per helper family, or a named surviving variant. Two variants remain: `GetCapacity` (flat lookup) and `ComputeRollingAverage` ×2. Four sparkline builders and two alert evaluators stay, each with different inputs. The QA coverage helpers were merged into `TestCoverageCalculator`, beyond the plan's list, under the skill's shared-kernel rule. No method in `Fokus.Domain/Analytics` is over 80 lines.
- **Endpoint-owned replies** (Step 8): reply types generated member for member from the Domain records, maps in `{Op}Extensions.cs`, shared reply shapes in `Features/Analytics/_Shared/`; Domain `TestRunSummaryDto` renamed `TestRunSummary`.
- **Loaders** (Step 9): `{Subject}Inputs` classes in `Fokus.Application/Analytics/{Area}/`. Each issues its endpoint's queries in the same order with the same early exits and returns a `…Source` record; `null` means the endpoint's empty reply.
- **Syncs** (Step 10): moved to `Fokus.Application/{Sync,Xray}/` with their names (D3). `SyncSprints` owns `SprintSyncFailureDto`. Every `catch` block is unchanged.
- **Persistence copy** (Step 11): `TestExecutionRepository` uses the index-based Domain `IsStartedInSprint` over a `StatusTransitionIndex`; its private `GetStageIndex` copy is gone.
- **Docs** (Step 12): the two skills were rewritten under their existing folder names. `extract-feature-service` was rewritten rather than retired.

## Skills Used
| Step | Skill(s) invoked | Notes |
|------|------------------|-------|
| 1 | None | plan Skill Mapping `(none)`, TDD no — the step brings skills in |
| 2 | None | plan `(none)`, TDD no; gap logged: no golden-output-harness skill |
| 3 | service-registration | |
| 4 | domain-service | |
| 5 | domain-service | |
| 6 | domain-service | |
| 7 | domain-service | |
| 8 | create-feature | with `workflows/Mappings.md` |
| 9 | service-registration | |
| 10 | service-registration | |
| 11 | None | plan `(none)`, TDD no |
| 12 | None | plan `(none)`, TDD no — doc sweep under the `kb-maintenance` rule |
| 13 | None | plan `(none)`, TDD no; gap as Step 2 |

## Carry-Over Findings
| Title | Severity | For | Evidence | Note |
|-------|----------|-----|----------|------|
| Stale `.claude/test-times.json` keys | low | team-lead | 33 keys under the four `…ServiceTests` class names, which no longer exist (Step 11) | The owner constraint forbids the developer to touch the file; the coordinator confirmed that the close re-times the renamed tests |
| `graphify update .` not run | low | team-lead | Root `CLAUDE.md` asks for it after code edits; no step ran it (Steps 4–12) | `graphify-out/` is not developer-owned |
| `GetEffectiveSp` twin in Persistence | low | architect | `TestExecutionRepository.GetEffectiveSp` (private) has the same body as the private one in `Shared/TestCoverageCalculator.cs` | Outside Step 11's scope (only started-in-sprint and stage index were named). Making the Domain one public would let the repository use it |
| `GetEffectiveTicketSp` pair | low | reviewer | `SprintSummaryCalculator.cs:469-471` comment "mirrors EpicProgressCalculator.GetEffectiveTicketSp"; both private | Not one of the plan's helper families; worth a D6 check for a `Shared/` merge |
| Architecture doc delta line | low | architect | `docs/architecture/v1.md` § Computed Metrics: "Query endpoints must compute both current value and delta…" | Still true at the reply level (the endpoint gets both through the calculator), so it was left out of Step 12; it could say "calculators" |

## KB Changes
| Entry | Action | What changed |
|-------|--------|-------------|
| `docs/kb/glossary.md` | UPDATE | **Sprint Reference** (`SprintRef`) line beside **Sprint** (Step 4) |
| `docs/kb/analytics/{bug-ratio,carry-over,cycle-time,daily-progress,developer-detail,epic-progress,health-score,leaderboard,scope-change,test-timeline,throughput}.md` | UPDATE | Key files → `Fokus.Domain/Analytics/{Area}/{N}Calculator.cs`; prose class names; three meaning-level lines (shared `GetTransitionCompletedMemberships`, the epic context's `TransitionsByTicket`, `DeveloperSummary` in `SprintSummaryResult.cs`) (Step 12) |
| `docs/kb/cross-cutting.md` | UPDATE | Calculator names in the metric table, the helper paths under `Shared/` (Step 12) |
| `docs/kb/domain/{developer,ticket}.md` | UPDATE | `ExcludedDeveloperFilter` key file; calculator names (Step 12) |
| `docs/kb/jira-sync.md`, `docs/kb/xray.md` | UPDATE | Sync key files → `Fokus.Application/{Sync,Xray}/` (Step 12) |

## Unfinished Tests
- Step 1: whole unit run — cut at 30 s (Step 1 changed no code; every later step close ran the whole unit set and passed).

## Deviations from Plan
- **Step 2:** dummy `Jira:*` configuration overrides and a comparator self-test, both inside the step's intent.
- **Step 4:** `ResolveStartIndex` merged one step early (provably equal, uncoupled).
- **Step 5:** folder `Sprints/` by the Q4 amendment (D14). The move script's partial run lost the `MetricCard` text, which was recovered read-only from `git show HEAD:`. One extra capture was taken after Step 5 (PASS).
- **Step 6:** `ComputeThroughput` never read the clock, so its tests got no date argument. `SprintSummaryItem` was merged into `SprintRef`, and the dead `QaWorkloadResponse` was deleted.
- **Step 7:** QA coverage helpers and `GetTransitionCompletedMemberships` consolidated beyond the plan's family list (the skill's shared-kernel rule); dead code removed in `WorkflowStageResolver.Detect` and `DeveloperDetailCalculator`.
- **Step 8:** `GetDeveloperQuality`, `GetQaWorkload` and `GetQaMetrics` maps moved to `{Op}Extensions.cs`; Domain `TestRunSummaryDto` → `TestRunSummary`. One extra capture was taken after Step 8 (PASS).
- **Step 9:** an extra full comparison alongside the SQL check (PASS).
- **Step 11:** `.claude/test-times.json` stale keys not removed — the owner constraint forbids touching the file (carried over).
- **Step 12:** the architecture doc and `project-rules.md` also state that API references Domain directly, as its project file does. `ScaffoldCsprojFiles.md` gained the D2 exception that the new reference graph needs.
- **All steps:** no `graphify update .` (carried over); no step commits (no `per-step` strategy was declared, and the owner forbade git writes).

*Status: COMPLETE — developer, 2026-10-14*
