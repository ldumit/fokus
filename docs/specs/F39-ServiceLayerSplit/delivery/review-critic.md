# F39 plan review — critic record

## Round 1 — 2026-10-14 — plan — primary (codex)

Primary reader: Codex (read-only job, completed) — verdict and findings persisted verbatim in `review-critic-codex.md` § Round 1 (NO-GO: 11 HIGH, 9 MEDIUM, 1 LOW). Second reader: Sonnet, recovered from its transcript (message truncated); verbatim below.

## Second reader — round 1 (sonnet)

# F39 Service Layer Split — Critic Review (second reader)

## Mode: Plan Review
## Verdict: REVISE

The plan's Context counts are accurate and the Domain/Application shape matches the skills. Two HIGH gaps leave the golden comparison unable to pin what the plan says it pins. Several accept lines were never run against the live tree.

Round 1 stays non-adversarial: 2 HIGH, no CRITICAL. One systemic note: every Context fact I re-ran was exact, but Steps 4, 8, 9 and 10 carry accepts that cannot pass or cannot fail. Counts confirmed: 18 services, 9,397 lines, 41 facts, 15 + 1 + 2 by area, clock sites at `DeveloperDetail:211,329` and `DeveloperProgress:69,93`, and all six endpoint sizes.

## Pre-commitment Predictions
| Predicted | Found |
|---|---|
| Harness infeasible against Program.cs, auth or start-up | Feasible. There are no hosted services. The test scheme needs a matching active `AppUser` row, because `ActiveUserPreProcessor` checks the DB. The plan omits that. |
| Counts drift | Counts are exact. "FilterMemberships: 9 copies" is really 4 variants (see L1). |
| Test project breaks mid-plan | Confirmed (F3). |
| Clock fixture hides the only semantic edit | Confirmed (F6). |
| Request matrix too thin | Confirmed (F1, F2). |
| Echoes missed in docs and skills | Confirmed (F10). |

## Coverage ledger
Round: 1 · Depth: mechanism · Bar: mechanics · Ruler: seed `references/ruler.md` (no overlay; `docs/critic-calibration/` absent) · Read: full

| Requirement / area | Surfaces opened | Status |
|---|---|---|
| Context claims and counts | plan § Context; all `*Service.cs` (sizes, purity, helper defs, hash compare); Tests usings and fact count; `verify.json` | COVERED |
| Steps 1 and 3 (skills, Application project) | plugin `domain-service` and `service-registration`; local `service-registration` (grep); csproj ×4, `Fokus.slnx`, `Program.cs`, API `DependencyInjection.cs`, Persistence DI | COVERED |
| Step 2 (golden harness) | `Program.cs`; `Auth/*` ×4; Persistence Migration and Seed; `appsettings.json`; route inventory; request shapes; Domain entity setters; F38 plan (API-doc method, D11) | COVERED |
| Steps 4–7 (kernel, calculators, long methods) | helper bodies; `TransitionAttributionChecker`; `TestExecutionRepository` 120–185; method-length measurement; step-5/6 cross-type dependencies | COVERED |
| Step 8 (reply ownership) | `*Query.cs` ×16; plugin and local `create-feature` `Mappings.md` | COVERED |
| Steps 9–10 (loaders, sync) | `GetQaWorkloadEndpoint` (structure); `GetDeveloperDetailEndpoint` 1–75; sync services (heads, catches); `SyncSprintsEndpoint`; `DetectWorkflowStagesEndpoint` | PARTIAL |
| Steps 11–12 (tests, docs) | `GlobalUsings`; root `CLAUDE.md`; service `CLAUDE.md`; `project-rules.md`; `docs/architecture/v1.md` (excerpts); KB path refs; `extract-feature-service` skill | COVERED |
| Reference shape | knowledge-gateway StoreAssistant `CLAUDE.md`; ADR-F192b | COVERED |
| `GetDeveloperQuality`, `GetQaTrends`, `GetSprintSummary` endpoint bodies | — | NOT OPENED — sizes only; mapping and loading split inferred from `GetQaWorkload` |
| `QaWorkloadService` body; 5 test file bodies | — | NOT OPENED — not needed for the findings |
| `analytics-computation-service` skill body; `cqrs-patterns`; `csharp.md` beyond § Method Design; ADR-F192a | — | NOT OPENED — Step 12 rewrite and D1 citations not verified |
| `Jira.RestApi` and `Xray.GraphQL` registrations | — | NOT OPENED — not on the harness path I traced |

## Findings

### [HIGH] F1 — The golden harness excludes the route that covers a long method and the moved input type
**Anchor:** seed-0117 · above seed-0123 · below seed-0189
**Shape:** pinned-row claims coverage the request matrix lacks
**Consequence:** A refactored 180-line detection routine and a moved input type ship with no output check, so a changed stage list goes unnoticed.
**Evidence:** quoted + path
**Surfaces:** plan § Step 2, § Step 7, § Must-NOT-Change row 1; `Fokus.API/Features/Settings/DetectWorkflowStages/DetectWorkflowStagesEndpoint.cs`; `Fokus.Persistence/Repositories/TicketRepository.cs:5-8,113`
**Untouchable:** silent-wrong-output
- **Source:** Step 2 says "It calls every GET route under `/api/analytics/*`, `/api/sprints*`, `/api/developers*` and `/api/team`." Must-NOT-Change row 1 says "Every GET reply … pinned".
- **Issue:** `GET /api/settings/workflow-stages/detect` is outside that list. It calls `WorkflowDetectionService.Detect`, which I measured at 180 lines. Step 4 moves `TransitionEdgeData` and Step 7 decomposes `Detect`. Step 14 also omits it ("every analytics tab, Dashboard, Developers, Sprints, Team").
- **Fix:** Add `/api/settings/workflow-stages/detect` to the route list. Seed transitions, settings done-statuses and a sidelined-stage case so the reply is non-trivial. Correct the row-1 wording to name what is actually covered.
- **Also:** `TransitionEdge` (`TicketRepository.cs:5`) must move with `TransitionEdgeData`. The plan names only the latter.

### [HIGH] F2 — The request matrix never sends a sub-team or window filter
**Anchor:** seed-0117 · above seed-0123 · below seed-0189
**Shape:** golden variants don't reach the changed branches
**Consequence:** The filter by team, which nine merged helper copies implement, is never requested by the comparison, so a changed team result ships unnoticed.
**Evidence:** quoted + path
**Surfaces:** plan § Step 2, § Step 4, § D6; `Features/Analytics/*Service.cs`; `Get*/Get*Query.cs`
**Untouchable:** silent-wrong-output
- **Source:** Step 2 says "Run each once without a sprint, once per sprint id in the database, and `developer-detail` once per developer."
- **Issue:** 13 of 15 analytics requests take `SubTeam`, and most take `Last` (default 5). The helpers Step 4 merges are exactly the sub-team code:
  - `FilterMemberships` in 9 files, in 4 variants (12-line ×4, 6-line ×3, `EpicProgress` 11 lines, `SprintSummary` 15 lines).
  - `FilterDevelopers` ×5 and `GetDeveloperMemberships` ×4.
  - Sub-teams appear in the seed list but never in a request.
- **Scenario:** A merge replaces `QaMetrics`' two-argument filter (returns the same list instance when `subTeam` is blank) with the three-argument version. The `?subTeam=X` branch changes. Every golden still passes, because they all send `subTeam=null`.
- **Fix:** Add `subTeam=<each seeded sub-team>` to every endpoint that accepts it. Add one `last=2` run per multi-sprint route. Add `sprintId` for the active sprint on each, which "once per sprint id" already implies. Add the `/api/developers/{accountId}/capacity` path parameter, which the plan doesn't specify.

### [MEDIUM] F3 — Steps 4–10 say "full suite green", but the test project cannot compile until Step 11
**Anchor:** seed-0130 · above seed-0127 · below seed-0116
**Band:** MEDIUM–HIGH; driver: whether a per-step commit gate runs the declared `build` role literally. `verify.json` declares roles only, no commit strategy.
**Shape:** step order breaks its own accept
**Consequence:** Every step from the first move to the test-fix step leaves the build broken, so the stated per-step green check cannot pass as ordered.
**Evidence:** quoted + path
**Surfaces:** plan § Step 4–7, § Step 11; `Fokus.Tests/GlobalUsings.cs`; `Fokus.Tests/Features/Analytics/TransitionAttributionCheckerTests.cs:18`; `.claude/verify.json`
- **Source:** Step 4 says "full suite green"; Step 11 says "update the 5 test files to the new namespaces".
- **Issue:** The `build` role is `dotnet build src/Fokus.slnx`, which includes `Fokus.Tests`.
  - Tests reference the moved types directly, via `global using Fokus.API.Features.Analytics`.
  - Step 4 moves `TransitionAttributionChecker`, which breaks 8 tests; Steps 5–6 break the rest.
  - Step 4's clock parameter also changes `ComputeProgress`'s arity. Step 11's "no assertion changes" doesn't say the call sites change.
  - The edit also reaches `GlobalUsings.cs`, which isn't one of "the 5 test files".
- **Fix:** Move the test namespace, type-name and `today` call-site edits into Steps 4, 5 and 6. Keep Step 11 for the fact-count check and the Persistence copy.

### [MEDIUM] F4 — Step 4's two accept lines cannot discriminate
**Anchor:** seed-0130 · above seed-0127 · below seed-0120
**Shape:** accept vacuous or unsatisfiable
**Consequence:** One check passes before any work and another can never pass, so neither can tell whether the duplicate removal or the purity goal was met.
**Evidence:** scenario + path
**Surfaces:** plan § Step 4 Accept, § Must-NOT-Change row 6; `Fokus.Domain/{Auth,Sprint,Ticket}/Behaviors/*.cs`
- **Dedupe grep:** as written (`grep -rn "private static .* (A|B|…)("`, no `-E`) it returns 0 on the unchanged tree. With `-E` it errors with "Unmatched (". Written correctly (`-E … \(`) it returns 38 today.
- **Purity grep:** `grep -rn "DateTime.UtcNow" …/Fokus.Domain` → 0 is unsatisfiable. Domain already has 9 hits in entity behaviour:
  - `AppUser.cs:14,28,35`
  - `Invitation.cs:14` and `Behaviors/Invitation.cs:16`
  - `Sprint.cs:21,22,26`
  - `Ticket.cs:17`
  - The row-6 scope (`Domain/Analytics`) is right; the Step 4 scope is wrong.
- **Fix:** Use `-E` with the corrected pattern, and scope the clock grep to `Fokus.Domain/Analytics` with `DateTime\.(UtcNow|Now|Today)|DateTimeOffset\.UtcNow`. The Step 8 `System.Text.Json` grep is also 0 today; it is a guard, not a change check.

### [MEDIUM] F5 — Nothing proves the golden can go red or that its fixture exercises the branches
**Anchor:** seed-0129 · above seed-0127 · below seed-0120
**Shape:** no non-vacuity gate
**Consequence:** A comparison on a fixture that returns empty or disabled-data replies passes trivially, and nothing shows the check can ever fail.
**Evidence:** path + scenario
**Surfaces:** plan § Step 2 Accept; `GetQaWorkloadEndpoint.cs:25-37`
- **Issue:** Step 2's accept is "file count per route; the two before runs identical". If the settings row keeps its default `XrayEnabled=false`, the QA routes return `hasQaData:false`: `QaWorkload` returns early, and other QA routes behave similarly. The plan's seed list never mentions that flag. The golden is then green on empty replies.
- **Fix:** Add two checks to the Step 2 accept:
  - A fixture check: per route, the body shows data (`hasQaData:true`, non-empty lists, and so on).
  - A sabotage check: perturb one calculator constant and one filter once, confirm the diff goes red, then revert.

### [MEDIUM] F6 — The fixture is described as "no clock", so the clock plumbing is pinned only where the date is clamped
**Anchor:** seed-0129 · above seed-0127 · below seed-0120
**Band:** MEDIUM–HIGH; driver: whether the active sprint spans the run day
**Shape:** fixture design hides the one value-changing edit
**Consequence:** The only value-changing edit, passing today's date in, is checked only where the date is clamped, so an off-by-one date ships unnoticed.
**Evidence:** path
**Surfaces:** plan § Step 2, § Step 4, § D4; `DeveloperProgressService.cs:69,93`
- **Issue:** `currentDay` is clamped to `[1, totalDays]`. `isGracePeriod = currentDay <= 2`, and the stall dates use `todayDate` and `todayDate.AddDays(-1)`. A fixed-date active sprint in the past gives `currentDay = totalDays`, with no grace period. A wrong `today` (local date, or a plus-or-minus-one-day error) then yields identical bytes.
- **Fix:** Pick one explicitly. Either seed the active sprint relative to the run day, which makes the same-day rule necessary, or seed a past active sprint and accept that the mid-sprint branch is checked only by review. State which in Step 2.
- **Also:**
  - Step 2 doesn't say the test-auth scheme needs a matching active `AppUser` row; `ActiveUserPreProcessor` throws 401 without one.
  - It doesn't say the harness must set the connection string via env var (F38 learned a bare path fails).
  - Adding `public partial class Program;` to `Program.cs` is avoidable. Any public non-static type (for example `SprintHub`) works as `TEntryPoint`, and the line stays in the repo with no consumer.

### [MEDIUM] F7 — The comparison runs only at Step 13
**Anchor:** seed-0129 · above seed-0127 · below seed-0120
**Shape:** late detection, no bisect
**Consequence:** The comparison runs once after eleven steps of change, so a difference cannot be traced to a step and each day boundary forces a fresh baseline.
**Evidence:** quoted
**Surfaces:** plan § Step 13, § Step 2, § D7
- **Issue:** After-capture happens only at Step 13, after the Step 7 decomposition of 25+ methods and the Step 8 reply rewrite. The same-day rule means the before capture must usually be re-taken from a base tree. The plan says "a tree reverted to `main`", but the base is F38's branch. No base commit SHA is recorded.
- **Fix:** Record the base SHA in `implementation.md`. Run the comparison at each slice end (after Steps 4, 7 and 10), using a `git worktree` at that SHA for the same-day before capture. Keep Step 13 as the final run.

### [MEDIUM] F8 — The local `create-feature` skill teaches Mapster, but Step 8 says "hand-written Mappings"
**Anchor:** seed-0130 · above seed-0127 · below seed-0120
**Band:** MEDIUM–HIGH; driver: whether the developer follows the plan text or the local skill (the repo's Skill Authority rule says the skill wins)
**Shape:** mapped skill contradicts the step
**Consequence:** The skill the step is told to follow teaches the opposite mapping approach and location from the plan, so the builder gets two conflicting instructions.
**Evidence:** quoted + path
**Surfaces:** plan § Skill Mapping row 8, § Step 1, § Step 8; `.claude/skills/create-feature/workflows/Mappings.md`; plugin `create-feature/workflows/Mappings.md`
- **Source:** Step 8 says "Follow create-feature (local copy, the hand-written Mappings workflow)".
- **Issue:** The local `Mappings.md` is "Create Mappings (Mapster)": `IRegister`, `.Adapt<>`, and "Outputs: `{Svc}.Application/Mappings/{Domain}Mappings.cs`". That path is the new project. The plugin version says "Map by Hand" with `To{Destination}()` extensions in `{Subject}Extensions.cs`, and "Not in a `Mappings/` folder". F38 did not refresh `create-feature`, and Step 1 here refreshes only `domain-service` and `service-registration`. Fokus has no Mapster use today (grep over `src/Services`: 0).
- **Fix:** Add `create-feature` to Step 1's copy/merge, or state the departure.

### [MEDIUM] F9 — Step 9's size accept either already holds or cannot be met
**Anchor:** seed-0123 · above seed-0127 · below seed-0120
**Shape:** accept does not discriminate
**Consequence:** A size limit one file already meets and another cannot meet while keeping the mapping it is told to keep, so the check does not discriminate.
**Evidence:** path
**Surfaces:** plan § Step 9; `GetQaWorkloadEndpoint.cs:202-333`; `GetDeveloperDetailEndpoint.cs` (117 lines)
- **Issue:** "Each of the six endpoint files under 120 lines" is already true for `GetDeveloperDetail` (117). For `GetQaWorkload`, the `Map*` methods alone are lines 202–333, about 130 lines. The step says "The endpoint keeps … the reply mapping". Together with the request handling that is about 170 lines, so the limit is unreachable as written. The plugin's mapping workflow puts maps in a `…Extensions.cs` beside the feature.
- **Fix:** Put the hand mappers in `{Op}Extensions.cs` (per F8) and keep the line limit on the endpoint file.

### [MEDIUM] F10 — Step 12's echo sweep misses several statements of the reversed rule
**Anchor:** seed-0130 · above seed-0127 · below seed-0120
**Shape:** sibling artifacts still state the old rule
**Consequence:** Several documents and a skill still state the rule being reversed, so agents are told two opposite things and the one-phrase check cannot see it.
**Evidence:** path
**Surfaces:** plan § Step 12, § Context ("the service's thin-domain note"); `docs/architecture/v1.md:28-37,171`; `.claude/skills/extract-feature-service/SKILL.md:32,47,54`; `docs/conventions/project-rules.md:22`; `src/Services/Fokus/CLAUDE.md` (Endpoint pattern, Key patterns)
- **Architecture doc:** `v1.md` carries the reference graph, "No separate Application project", the constraint "No service layer classes", and "Thin domain in v1" at line 171. The plan's "service's thin-domain note" lives there, not in the service `CLAUDE.md`. Step 12 doesn't touch the file.
- **`extract-feature-service` skill:** it scaffolds `Features/{Area}/{ServiceName}.cs` and an API-layer `AddScoped`, which contradicts the new `…Service` rule. It isn't in the Skill Mapping or Step 12.
- **`project-rules.md:22`:** Step 12 appends to the guardrail, but "Focused operation services scoped to a feature area … are allowed" stays and contradicts the append.
- **Service `CLAUDE.md`:** "Handler logic lives directly in `HandleAsync`" and "Health scores are computed on-the-fly in endpoints" go stale after Step 9.
- **Accept gap:** Step 12's accept greps one phrase in one file, and a path-shaped pattern that misses class-name mentions.
- **Fix:** Add `v1.md` and `extract-feature-service` (rewrite or retire) to Step 12. Widen the greps.

### [MEDIUM] F11 — Step 7 is sized for three methods but has 25 over its own limit
**Anchor:** seed-0129 · above seed-0127 · below seed-0120
**Shape:** scope understated
**Consequence:** The plan names three long methods but twenty-five exceed its own limit, so the step is several times larger than its slice assumes.
**Evidence:** scenario
**Surfaces:** plan § Context "Long methods", § Step 7, § D7, § Build slices
- **Issue:** My measurement (signature to closing brace) found 180 methods in the moved classes, 35 over 60 lines and 25 over 80. Besides `ComputeEpicProgress` 293, `ComputeProgress` 242 and `Detect` 180, there are `ComputeSummary` 150, `ComputeThroughput` 144, `BuildBurnupData` 143, `ComputeTicketCycleTime` 142, `ComputeQaMetrics` 140, `ComputeDeveloperQuality` 139, and more. The Step 7 accept (none over 80) forces all 25. The Context says three, and slice "5–7" is sized for one developer's reading load.
- **Fix:** Say "25 methods over 80 lines" in the Context and in Step 7, and re-slice. The golden is the only net (D7), which is why F1, F2 and F5 matter.

### [MEDIUM] F12 — The sync move has no automated pin, and the broadcast catch sits outside the pinned files
**Anchor:** seed-0129 · above seed-0127 · below seed-0120
**Shape:** pin row cites the wrong surface
**Consequence:** The only moved code that writes data has no automated check, and the failure-broadcast handler the plan says is covered is not in the moved files.
**Evidence:** path
**Surfaces:** plan § Step 10, § Step 14 item 3, § Must-NOT-Change row 3; `SyncSprintsEndpoint.cs:42-55`
- **Broadcast catch:** the "broadcast failure logged" `catch` is in `SyncSprintsEndpoint.cs:51`, which Step 10 doesn't move. "Every `catch` in the two moved classes" covers the 7 per-item and Xray catches only.
- **Operator check:** Step 14 item 3 is "if configured", for Xray at least, and a real Jira sync.
- **DI registrations:** the 18 `AddScoped` lines in `Fokus.API/DependencyInjection.cs:29-46` have no stated destination in Steps 5, 6 or 10. A missing one fails only at request time. GETs are caught by the golden; the POSTs are not.
- **Fix:** Have the harness (already in-process) swap `IJiraClient` and `IXrayClient` for fakes. POST `/api/sync/sprints`, `/api/sync/backlog` and `/api/xray/sync` once each, with a failing item, and diff the replies. Name the registration destination in Step 3 and Steps 5, 6 and 10.

### [MEDIUM] F13 — "Each endpoint owns its reply" is not total, and the API-doc pin is taken once at Step 8
**Anchor:** seed-0130 · above seed-0127 · below seed-0120
**Shape:** scope list incomplete
**Consequence:** Two replies still embed shared records after the 'each endpoint owns its reply' ruling, so a later change to those records changes the wire.
**Evidence:** path
**Surfaces:** plan § Step 8, § Step 10, § Must-NOT-Change row 2; `GetQaMetricsQuery.cs:27-29`; `SyncSprintsCommand.cs:25`
- **`GetQaMetrics`:** it is listed as "already maps", but its reply declares `public MetricCard? CoverageRate/ExecutionRate/PassRate`, a service-declared record that moves into the shared kernel.
- **`SyncSprints`:** its reply embeds `List<SprintSyncFailure>`, a record declared in `SprintIssueSyncService.cs`, which Step 10 moves into Application.
- **API-doc pin:** the diff runs only at Step 8's accept. Steps 9–12 then move the sync reply types, with no re-run.
- **Fix:** Add both endpoints to Step 8, and re-run the API-doc diff at Step 13.

### [MEDIUM] F14 — `GetDeveloperDetail`'s inline check is not a duplicate of `ExcludedDeveloperFilter`
**Anchor:** seed-0130 · above seed-0127 · below seed-0120
**Band:** MEDIUM–LOW; driver: whether the seed has a developer active in some, but not all, closed sprints
**Shape:** equivalence asserted, not shown
**Consequence:** A per-period filter is swapped for an any-period check described as a duplicate, so equivalence is assumed rather than shown.
**Evidence:** quoted + path
**Surfaces:** plan § Context, § Step 9; `ExcludedDeveloperFilter.cs`; `GetDeveloperDetailEndpoint.cs:60-85`
- **Issue:** The filter returns the ids excluded in one sprint (zero effective capacity AND zero completions in that sprint). The endpoint computes `hasAnyActivity` over all closed sprints. They are equivalent only if the filter is called per sprint and the result negated per sprint. That needs N calls, each recomputing `orderedStages`. D6 requires "provably equal"; the plan shows no proof.
- **Fix:** Keep the inline check, or state the per-sprint call form and seed one such developer.

## Lower-severity items (below floor, recorded)
- **L1 — LOW.** The "byte-identical" claim is imprecise. `FilterMemberships` is 4 variants (F2). `BuildMetricCard` has 5 definitions with 3 return types (`MetricCard`, `ScopeMetricCard`, `BugRatioMetricCard`). The `DeltaDirection` and `DeltaPolarity` overload sets differ per service. D6 handles all of it, but Step 4's dedupe list reads as if one copy survives each.
- **L2 — LOW.** Step 3 names the method `AddFokusApplication`, but the skill says "named for the layer, never the service" (`AddApplicationServices`). The repo's existing `AddFokusServices` and `AddFokusPersistence` also depart, with no Decision logged. Where `TimeProvider` is registered is unstated.
- **L3 — LOW.** The backlog F39 row says "on a real database" (D9 is synthetic) and "14 in Analytics" (it is 15).
- **L4 — LOW.** The Decisions table lists D9 before D8.
- **L5 — LOW.** KB path references are "14 entries"; I count 19 references in 15 files.
- **L6 — LOW.** Step 11's "where EF can translate it" is moot: the `TestExecutionRepository` copy (lines 157–172) is in-memory LINQ. A second private `GetStageIndex` at `:291` isn't mentioned. The Domain helper takes a `StatusTransitionIndex`, not a list.
- **L7 — LOW.** Step 5 says "each result record in its own file", about 150 types in one flat `Fokus.Domain/Analytics/`. The skill's form is one result record per calculator, and the reference uses folders per capability.
- **L8 — LOW.** Step 9's "per-sprint loops keep their query count at most as today" has no accept.
- **L9 — LOW.** The test class names (`*ServiceTests`) appear as keys in `.claude/test-times.json`. They go stale on rename, and Step 11 says nothing about renaming them.

## Gap Analysis
- **Pinned vs claimed:** the Must-NOT-Change table's "pinned" column overstates the golden. F1 and F2 cover GET behaviour. F12 and F13 cover the mutating and wire-shape rows.
- **Backward impact:** the namespace move touches the 20 files declared `namespace Fokus.API.Features.Analytics;` and every endpoint, all needing explicit `using`s (`csharp.md` line 7). That is mechanical, but the F3 test fix rides on it.
- **Reference-shape deviation:** D2 knowingly departs from "API references Application only", and is disclosed.
- **Skill assumption:** the plugin's `domain-service` says "No shared library … in the Domain project", but `Fokus.Domain` references `Blocks.Domain` and `Jira.Contracts`. This is out of scope (G-01) and fine to leave.

## Open Questions
- Does the commit protocol gate each step on the declared `build` role? If yes, F3 is HIGH.
- Will the active-sprint fixture span the run day? That settles F6.
- Is a `git worktree` at a recorded SHA acceptable for the daily before re-capture (F7)?

Reviewed: D:/src/fokus/docs/specs/F39-ServiceLayerSplit/delivery/plan.md

### Merge — round 1

Coverage: complete — every plan surface was opened by at least one reader (the second reader's NOT OPENED rows are covered by Codex, which read the endpoints, the analytics skill, cqrs-patterns and the module registrations).

| id | readers | level | anchor | title | shape | consequence | evidence | untouchable | tag | surfaces | source ids |
|---|---|---|---|---|---|---|---|---|---|---|---|
| R1-01 | codex, sonnet | HIGH | seed-0130 | Backlog says real database; plan says synthetic | definition conflict | two definitions disagree | quoted | — | definition | Context, backlog | codex-1, sonnet L3 |
| R1-02 | codex, sonnet | HIGH | seed-0130 | Tests updated only at Step 11 while Steps 4–10 require green | step order breaks own accept | build red for 7 steps | quoted | — | order | Steps 4–6, 11 | codex-2, sonnet F3 — contested: HIGH / MEDIUM, settled HIGH: the declared build role compiles the test project |
| R1-03 | codex, sonnet | HIGH | seed-0130 | TransitionEdgeData cannot move without TransitionEdge | incomplete move | Domain cannot compile | path | — | move | Step 4 | codex-3, sonnet F1 note |
| R1-04 | codex | HIGH | unranked — no anchor fits; Codex severity HIGH | Kernel extraction precedes coupled helpers' dependencies | order | Step 4 cannot complete | quoted | — | order | Step 4 | codex-4 |
| R1-05 | codex | HIGH | unranked — no anchor fits; Codex severity HIGH | Trace ids and Development stack traces defeat byte determinism | harness determinism | false reds on every error reply | quoted | — | harness | Step 2 | codex-5 |
| R1-06 | codex, sonnet | HIGH | seed-0130 | Harness bootstrap underspecified (entry type, AppUser row, Google options, start-up seeding, Xray off by default, connection string) | underspecified | harness cannot run or proves nothing | quoted | — | harness | Step 2 | codex-6, sonnet F6 notes |
| R1-07 | codex, sonnet | HIGH | seed-0130 | Workflow detect route outside the manifest | coverage gap | 180-line move unpinned | path | silent-wrong-output | harness | Step 2, Step 7 | codex-7, sonnet F1 |
| R1-08 | codex, sonnet | HIGH | seed-0130 | Endpoint-owned reply incomplete (GetTestTimeline, GetQaMetrics MetricCard, SyncSprints SprintSyncFailure) | scope list incomplete | Domain records stay on the wire | path | — | replies | Step 8 | codex-8, sonnet F13 |
| R1-09 | codex, sonnet | HIGH | seed-0130 | Local create-feature Mappings teaches Mapster; local cqrs-patterns lacks the FastEndpoints wording | mapped skill contradicts step | conflicting instructions | quoted | — | skills | Step 1, Step 8, D1 | codex-9, sonnet F8 |
| R1-10 | codex, sonnet | HIGH | seed-0130 | Dedupe accept grep vacuous (basic regex) | accept vacuous | passes before any work | scenario | — | accept | Step 4 | codex-10, sonnet F4 |
| R1-11 | codex, sonnet | HIGH | seed-0130 | Clock grep and JSON grep unsatisfiable (9 Domain clock sites; bin/obj) | accept unsatisfiable | gate can never pass | path | — | accept | Steps 4, 8 | codex-11, sonnet F4 |
| R1-12 | sonnet | HIGH | seed-0130 | Manifest never sends sub-team or last filters | coverage gap | merged filter variants unpinned | scenario | silent-wrong-output | harness | Step 2 | sonnet F2 |
| R1-13 | codex | MEDIUM | unranked — no anchor fits; Codex severity MEDIUM | Acceptance commands assume PowerShell semantics | shell mismatch | wrong counts | scenario | — | accept | Steps 1, 2, 13 | codex-12 |
| R1-14 | codex, sonnet | MEDIUM | seed-0130 | Registration naming and relocation unspecified | underspecified | missing registration found only at runtime | path | — | wiring | Steps 3, 5, 6, 10 | codex-13, sonnet F12, L2 |
| R1-15 | codex | MEDIUM | unranked — no anchor fits; Codex severity MEDIUM | Reply wrappers would move into Domain | boundary breach | Domain holds reply shapes | quoted | — | replies | Domain Model, Steps 5, 8 | codex-14 |
| R1-16 | codex, sonnet | MEDIUM | seed-0130 | Endpoint-size gate unreachable with mapping kept in the endpoint | accept | gate cannot pass | path | — | accept | Step 9 | codex-15, sonnet F9 |
| R1-17 | codex | MEDIUM | unranked — no anchor fits; Codex severity MEDIUM | Request manifest lacks expected statuses and branch variants | coverage | vacuous passes | scenario | — | harness | Step 2 | codex-16 |
| R1-18 | codex, sonnet | MEDIUM | seed-0130 | Clock and baseline identity not pinned (UTC, base SHA, worktree, run-day sprint) | underspecified | false reds or hidden off-by-one | path | silent-wrong-output | harness | Steps 2, 6, 13 | codex-17, sonnet F6, F7 |
| R1-19 | codex, sonnet | MEDIUM | seed-0130 | Must-NOT-Change rows weaker than claimed (broadcast catch location, sync unexercised, tests, schema) | pin not backed | rows overstated | path | — | pins | Must-NOT-Change, Steps 10, 14 | codex-18, sonnet F12 |
| R1-20 | codex, sonnet | MEDIUM | seed-0130 | Query-count constraint has no check | pin not backed | N+1 regression unseen | quoted | — | pins | Step 9 | codex-19, sonnet L8 |
| R1-21 | codex, sonnet | MEDIUM | seed-0130 | Doc and skill sweep misses v1.md, extract-feature-service, create-service, project-rules:22, service CLAUDE.md lines | echo too narrow | agents told opposite rules | path | — | docs | Step 12 | codex-20, sonnet F10 |
| R1-22 | sonnet | MEDIUM | seed-0130 | Step 7 sized for 3 long methods; 25 exceed 80 lines | scope understated | slice overloaded | scenario | — | scope | Context, Step 7, slices | sonnet F11 |
| R1-23 | sonnet | MEDIUM | seed-0130 | No non-vacuity proof for the golden (fixture, sabotage) | no non-vacuity gate | green on empty replies | scenario | — | harness | Step 2 | sonnet F5 |
| R1-24 | sonnet | MEDIUM | seed-0130 | Comparison only at Step 13 | late detection | no bisect | quoted | — | harness | Steps 4, 7, 10, 13 | sonnet F7 |
| R1-25 | sonnet | MEDIUM | seed-0130 | GetDeveloperDetail inline check is not a duplicate of ExcludedDeveloperFilter | equivalence assumed | value change | path | — | errors | Step 9, D11 | sonnet F14 |
| R1-26 | codex, sonnet | LOW | seed-0130 | Inventory facts imprecise (15 analytics, method spans, variants, KB counts, test coverage) | fact drift | minor | path | — | facts | Context, KB Impact | codex-21, sonnet L1, L5, L6, L7, L9 |

### Fold — round 1 (architect, claude-opus-5-5, 2026-10-14)

Folded by rewriting plan.md whole (same sections; every touched section listed in `where`).

| id | disposition | level after | re-grade (level · anchor · why) | where |
|---|---|---|---|---|
| R1-01 | fixed-real | HIGH | — | Context, backlog |
| R1-02 | fixed-real | HIGH | — | Steps 4–6, 11 |
| R1-03 | fixed-real | HIGH | — | Step 4 |
| R1-04 | fixed-real | HIGH | — | Step 4 |
| R1-05 | fixed-real | HIGH | — | Step 2 |
| R1-06 | fixed-real | HIGH | — | Step 2 |
| R1-07 | fixed-real | HIGH | — | Step 2, Step 7 |
| R1-08 | fixed-real | HIGH | — | Step 8 |
| R1-09 | fixed-real | HIGH | — | Step 1, Step 8, D1 |
| R1-10 | fixed-real | HIGH | — | Step 4 |
| R1-11 | fixed-real | HIGH | — | Steps 4, 8 |
| R1-12 | fixed-real | HIGH | — | Step 2 |
| R1-13 | fixed-real | MEDIUM | — | Steps 1, 2, 13 |
| R1-14 | fixed-real | MEDIUM | — | Steps 3, 5, 6, 10 |
| R1-15 | fixed-real | MEDIUM | — | Domain Model, Steps 5, 8 |
| R1-16 | fixed-real | MEDIUM | — | Step 9 |
| R1-17 | fixed-real | MEDIUM | — | Step 2 |
| R1-18 | fixed-real | MEDIUM | — | Steps 2, 6, 13 |
| R1-19 | fixed-real | MEDIUM | — | Must-NOT-Change, Steps 10, 14 |
| R1-20 | fixed-real | MEDIUM | — | Step 9 |
| R1-21 | fixed-real | MEDIUM | — | Step 12 |
| R1-22 | fixed-real | MEDIUM | — | Context, Step 7, slices |
| R1-23 | fixed-real | MEDIUM | — | Step 2 |
| R1-24 | fixed-real | MEDIUM | — | Steps 4, 7, 10, 13 |
| R1-25 | fixed-real | MEDIUM | — | Step 9, D11 |
| R1-26 | fixed-real | LOW | — | Context, KB Impact |

Reconciled: Step 2 manifest vs Must-NOT-Change rows 1, 3, 4 and Steps 9, 13; Steps 4–6 test moves vs Step 11; Step 8 vs Domain Model Changes and D1; Step 12 vs root CLAUDE.md, v1.md, project-rules, coding-conventions; D10 vs Step 3; D11 vs Step 9; backlog row vs D9.

## Round 2 — 2026-10-14 — plan — primary (codex)

Read: delta (round 1 coverage complete; slug number 39 is not a sample). Floor: MEDIUM. One reader. Codex's verdict and table are in `review-critic-codex.md` § Round 2: NO-GO; 22 closed, 4 partial.

### Merge — round 2

Coverage: complete.

| id | readers | level | anchor | title | shape | consequence | evidence | untouchable | tag | surfaces | source ids |
|---|---|---|---|---|---|---|---|---|---|---|---|
| R2-01 | codex | HIGH | unranked — no anchor fits; Codex severity HIGH | Sync failure replies carry ex.StackTrace, defeating byte comparison | — | — | quoted | — | fold | Step 2 output normalization, Must-NOT-Change row 1 | codex r2 |
| R2-02 | codex | MEDIUM | unranked — no anchor fits; Codex severity MEDIUM | Sync POST fixtures lack BoardId and Xray credentials | — | — | quoted | — | fold | Step 2 settings | codex r2 |
| R2-03 | codex | MEDIUM | unranked — no anchor fits; Codex severity MEDIUM | Raw directory diff mixes SQL counts and swagger, contradicting the per-part rules | — | — | quoted | — | fold | Step 2 output and comparison method, Step 13 | codex r2 |
| R2-04 | codex | MEDIUM | unranked — no anchor fits; Codex severity MEDIUM | No no-active-sprint fixture; grace-period claim false at D-3 | — | — | quoted | — | fold | Step 2 dates, fixture variants B and C | codex r2 |
| R2-05 | codex | MEDIUM | unranked — no anchor fits; Codex severity MEDIUM | No comparison after slice 5–6 | — | — | quoted | — | fold | Step 6 accept, Step 13 | codex r2 |
| R2-06 | codex | MEDIUM | unranked — no anchor fits; Codex severity MEDIUM | Unquoted backslash path and a literal brace find in Bash | — | — | quoted | — | fold | Step 2 sabotage check, Steps 5, 6 accepts | codex r2 |
| R2-07 | codex | MEDIUM | unranked — no anchor fits; Codex severity MEDIUM | R1-08 residual: SyncSprints reply embeds the Application SprintSyncFailure | — | — | quoted | — | round-1 miss: Step 10 | Step 10 | codex r2 |
| R2-08 | codex | MEDIUM | unranked — no anchor fits; Codex severity MEDIUM | R1-10 residual: no final helper-declaration check | — | — | quoted | — | round-1 miss: Step 7 | Step 7 accept | codex r2 |

### Fold — round 2 (architect, claude-opus-5-5, 2026-10-14)

| id | disposition | level after | re-grade (level · anchor · why) | where |
|---|---|---|---|---|
| R2-01 | fixed-real | HIGH | — | Step 2 output normalization, Must-NOT-Change row 1 |
| R2-02 | fixed-real | MEDIUM | — | Step 2 settings |
| R2-03 | fixed-real | MEDIUM | — | Step 2 output and comparison method, Step 13 |
| R2-04 | fixed-real | MEDIUM | — | Step 2 dates, fixture variants B and C |
| R2-05 | fixed-real | MEDIUM | — | Step 6 accept, Step 13 |
| R2-06 | fixed-real | MEDIUM | — | Step 2 sabotage check, Steps 5, 6 accepts |
| R2-07 | fixed-real | MEDIUM | — | Step 10 |
| R2-08 | fixed-real | MEDIUM | — | Step 7 accept |

Reconciled: Step 2 comparison method vs Steps 4, 6, 7, 9, 10, 13 and Must-NOT-Change rows 1–3; Step 10 reply ownership vs Step 8 and the owner ruling. No round 3: round 2 holds no CRITICAL.
