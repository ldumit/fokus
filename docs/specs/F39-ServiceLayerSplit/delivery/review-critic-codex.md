# F39 plan review — Codex (critic seat)

Round 1 — 2026-10-14 — plan — persisted by the architect session from the Codex job's collected output (job task-mv7indm0-xqio41, completed).

## Verdict: NO-GO

Reviewed the live `F38-DotnetSkillAlignment` branch, the backlog, plugin and local skills, and the StoreAssistant reference. No files were modified. Builds, tests, and the proposed harness were not run because this review must leave every file unchanged.

Findings below refer to [plan.md](D:/src/fokus/docs/specs/F39-ServiceLayerSplit/delivery/plan.md). Other Fokus paths are relative to `D:/src/fokus`.

1. **HIGH — `plan.md:45,109,284`; `docs/backlog.md:296` — The two feature definitions disagree.**  
   The backlog requires comparison on a **real database**; the plan requires a **synthetic database**, explicitly rejecting real data.  
   **Suggested fix:** reconcile the backlog with Decision D9, or change the plan to satisfy the backlog. Record which definition governs before implementation.

2. **HIGH — `plan.md:143,155,163,208` — Tests are updated after the steps that break them.**  
   Step 4 moves `TransitionAttributionChecker`; Steps 5–6 rename four tested services. Their tests still reference the old types until Step 11, despite each intervening step requiring a green suite. Adding the clock parameter also changes existing `ComputeProgress` calls. The shared import in `Fokus.Tests/GlobalUsings.cs:4` is outside the stated five test files.  
   **Suggested fix:** update test imports, names, project references, and clock arguments alongside each move. Keep assertions unchanged; reserve Step 11 for final verification.

3. **HIGH — `plan.md:144` — Moving `TransitionEdgeData` alone cannot remove the Persistence dependency.**  
   `TicketRepository.cs:5–8` declares both `TransitionEdge` and `TransitionEdgeData`; the latter contains `List<TransitionEdge>`. Leaving the element type in Persistence prevents the Domain record from compiling without a forbidden reference.  
   **Suggested fix:** move both records together and update repository projections and workflow callers in that step.

4. **HIGH — `plan.md:143` — The kernel extraction precedes dependencies it needs.**  
   Several proposed helpers are not independently movable. For example, `DeveloperThroughputService.ComputeRollingAverage` calls its private `ComputeMetrics`; metric-card builders return feature-specific records still declared in API service files. D6 correctly permits differing implementations to survive, but does not resolve this ordering problem.  
   **Suggested fix:** initially move only independent helpers and prerequisite records. Consolidate coupled helpers after their calculators move, or explicitly move their complete dependency sets together.

5. **HIGH — `plan.md:115,119,123,238,258` — Required error responses defeat the byte-determinism check.**  
   Inactive and excluded developers produce 404s in `GetDeveloperDetailEndpoint`. `GlobalExceptionMiddleware` includes `context.TraceIdentifier`, which changes between requests. In Development it also includes `ex.ToString()`: paths, line numbers, and renamed methods necessarily change during this refactor. Same-day captures do not fix either problem.  
   **Suggested fix:** establish deterministic request trace IDs in the harness and define the comparison environment. Production error bodies can be compared with fixed trace IDs; obtain Swagger through a separate Development host. Explicitly define how Development diagnostic details are checked.

6. **HIGH — `plan.md:109–125` — Harness bootstrap requirements are materially underspecified.**  
   The approach is feasible, but an Admin test scheme alone is insufficient:
   - `Program` currently lacks public accessibility; the plan addresses this conditionally.
   - A numeric `NameIdentifier` must identify a seeded, active `AppUser`, or `ActiveUserPreProcessor` rejects requests.
   - Google remains registered and requires valid nonempty options; supply controlled dummy values.
   - Startup always migrates, seeds default settings, and applies configured Xray credentials.
   - Default settings disable Xray; merely inserting test executions does not exercise QA calculations.
   
   **Suggested fix:** specify the external .NET 10 project and absolute API project reference, content-root/dependency-file setup, configuration overrides, isolated SQLite connection string, migration-before-seeding sequence, settings update, active Admin row, and authentication defaults. Suppress ambient Xray credentials. Standard MVC Testing targets can generate the content-root manifest for an external project reference. [MVC Testing targets](https://github.com/dotnet/aspnetcore/blob/v10.0.0/src/Mvc/Mvc.Testing/src/Microsoft.AspNetCore.Mvc.Testing.targets), [integration-test authentication](https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests?view=aspnetcore-10.0), [OAuth option validation](https://github.com/dotnet/aspnetcore/blob/v10.0.0/src/Security/Authentication/OAuth/src/OAuthOptions.cs).

7. **HIGH — `plan.md:119,172,200,267` — Workflow detection has no behavioral baseline.**  
   `/api/settings/workflow-stages/detect` falls outside the harness prefixes, yet its 180-line algorithm is moved and decomposed. None of the existing 41 facts tests it, and the operator checklist does not exercise it.  
   **Suggested fix:** include this route and fixtures covering empty graphs, terminal/sidelined statuses, cycles, and competing edge weights.

8. **HIGH — `plan.md:178` — The endpoint-owned output boundary is incomplete.**  
   `Sprints/GetTestTimeline/GetTestTimelineEndpoint.cs` returns the service’s `TestTimelineResponse` directly but sits outside Analytics. The supposedly already-mapped `GetQaMetrics` still exposes Domain-bound `MetricCard` records through its reply’s three card properties and assigns them directly. Its nested sparklines cross the same boundary.  
   **Suggested fix:** enumerate every consumer of moved result records across feature areas. Add API-owned timeline and metric-card DTOs and map every nested Domain result, including empty-response branches.

9. **HIGH — `plan.md:97–104,177` — Step 8 claims a local workflow that does not exist.**  
   The local `create-feature/workflows/Mappings.md` teaches Mapster, `.Adapt`, and Application mapping configurations. Hand-written mappings are in the **plugin** version. Step 1 refreshes only `domain-service` and `service-registration`. D1 similarly cites the plugin CQRS wording absent from the older local copy.  
   **Suggested fix:** refresh the relevant local `create-feature` workflows and reconcile `cqrs-patterns`, preserving Fokus decisions; otherwise explicitly declare the departure instead of marking it “Follow”.

10. **HIGH — `plan.md:148` — The deduplication acceptance grep falsely passes today.**  
    Plain `grep` uses basic regular expressions: the unescaped parentheses and pipes are literals. Re-running the exact pattern finds nothing despite the existing duplicate definitions. Searching only API also becomes ineffective once all calculators have moved.  
    **Suggested fix:** use a correctly escaped `rg` expression over source files in the final Domain location. Include the other listed helper families and verify declarations and retained variants, rather than mere absence from API.

11. **HIGH — `plan.md:149,182` — Two zero-hit gates cannot pass within the stated scope.**  
    The Domain clock grep finds **nine existing** `DateTime.UtcNow` sites in Auth, Sprint, and Ticket code, unrelated to analytics. The JSON grep also searches `bin` and `obj`; current binaries and NuGet assets contain `System.Text.Json`, although Domain C# source does not.  
    **Suggested fix:** target `Fokus.Domain/Analytics` source files and exclude generated output. Keep existing Domain dependencies and clocks outside this feature’s gate unless scope is explicitly expanded.

12. **MEDIUM — `plan.md:105,125,238` — Acceptance commands are wrong for the supplied PowerShell environment.**  
    `ls` and `diff` resolve to PowerShell aliases. `ls … | wc -l` counted **338 formatted output lines**, rather than the **42 skill files**. `diff -r before after` compares objects; it does not recursively compare directories.  
    **Suggested fix:** name the intended shell and executable explicitly, or use PowerShell file-count and recursive byte-comparison commands. Check comparison exit codes and distinguish “no matches” from command errors.

13. **MEDIUM — `plan.md:133,155–204` — Registration rules and relocation work need reconciliation.**  
    Both registration skills name the layer method `AddApplicationServices`; the plan introduces `AddFokusApplication` without an exception. The API currently contains all **18 scoped registrations**. The plan should explicitly relocate calculator, loader, and sync registrations as their types move. The imported skill also places the domain-event publisher in Application, while the live registration remains in Persistence.  
    **Suggested fix:** specify the final composition and per-layer registrations, using the skill’s naming or a documented Fokus exception. Include all callers: `GetTestTimeline`, workflow detection, `SyncSprints`, `SyncBacklogSprints`, `SyncXray`, and the `SprintSyncFailure` type used by `SyncSprintsCommand.cs`.

14. **MEDIUM — `plan.md:143,157,180` — Moving all records preserves forbidden reply-only shapes in Domain.**  
    Records such as `BugRatioResponse`, `CarryOverResponse`, `CycleTimeResponse`, `ScopeChangeResponse`, and `LeaderboardResponse` contain the HTTP mode and nullable single/multi reply alternatives. The domain-service output-boundary text explicitly assigns such wrappers to endpoints. A JSON-namespace grep cannot detect this violation.  
    **Suggested fix:** keep those wrappers in API. Separate computation results from reply assembly and presentation fields; inventory the final Domain records.

15. **MEDIUM — `plan.md:189–193` — Loading extraction alone does not meet the endpoint-size gate.**  
    `GetQaWorkloadEndpoint.cs:202–333` contains about **132 lines of mapping code alone**. Step 9 retains mapping in the endpoint, while Step 8 exempts this endpoint from changes. Removing loading therefore does not deliver a file under 120 lines.  
    **Suggested fix:** move manual mappings into feature-local extension files, consistent with the plugin mapping workflow, or revise the gate.

16. **MEDIUM — `plan.md:119,125,267` — Request counts do not establish useful behavioral coverage.**  
    The route matrix does not specify expansion of required path IDs, including capacity requests per developer. Supplying `sprintId` to routes that ignore it merely repeats a case. The fixture/request set also omits explicit `last` and sub-team variants, Xray-disabled behavior, missing IDs, no-active-sprint behavior, and relevant empty-data branches. Stable empty or unauthorized responses could otherwise satisfy the comparison.  
    **Suggested fix:** define a request manifest with expected statuses and nonempty-result checks, plus fixture/settings variants mapped to the branches being refactored.

17. **MEDIUM — `plan.md:123,145,237,280` — Clock and baseline identity are not pinned precisely enough.**  
    The four analytics sites use **UTC dates**, whereas “same calendar day” is ambiguous in Europe/Bucharest. The original code ignores an injected fake `TimeProvider`. Retaking a baseline against mutable `main` also does not establish the exact F38 version being compared; current `main` and this branch differ.  
    **Suggested fix:** record the baseline commit, fixture version, configuration, and UTC date. Recapture from that exact baseline in a separate checkout. Specify `GetUtcNow().UtcDateTime.Date` at the caller and matching explicit dates in existing test calls.

18. **MEDIUM — `plan.md:199,203,244,260–262` — Several Must-NOT-Change rows are weaker than their claimed proof.**  
    The broadcast catch lives in `SyncSprintsEndpoint.cs:51`, **not either moved sync class**. A successful operator sync does not exercise failure handling. Forty-one passing facts do not establish unchanged assertions or test meaning. “No new migration file” does not protect existing migrations, the snapshot, or the schema.  
    **Suggested fix:** compare the broadcast path explicitly and exercise deterministic failures through the external harness or a defined operator procedure. Review test diffs against allowed changes, and verify unchanged migration/model artifacts and SQLite schema.

19. **MEDIUM — `plan.md:192–193` — The query-count constraint lacks a check.**  
    Endpoint size and the existing calculator tests cannot establish that per-sprint loading still performs at most the original number of queries.  
    **Suggested fix:** record baseline and final SQLite command counts for representative single/multi sprint, QA-enabled, and QA-disabled cases in the external harness.

20. **MEDIUM — `plan.md:214–233` — The live documentation and skill sweep misses architectural contradictions.**  
    `docs/architecture/v1.md:20–37` remains labelled Current and declares three projects and no Application project. Local `extract-feature-service` explicitly says services stay in API and never move to another project; `create-service` workflows also retain older Application eligibility/layout rules. The analytics skill rewrite targets three sections but leaves its frontmatter and “When to use” examples promoting old `…Service` names; its repository-query exception must explicitly disappear from calculator guidance.  
    **Suggested fix:** extend Step 12 to the active architecture document and affected local skills. Clearly distinguish Application loaders from pure Domain calculators and remove obsolete naming and dependency instructions.

21. **LOW — `plan.md:14,25–26,230,271,281`; `docs/backlog.md:296` — Some inventory claims need correction.**  
    Re-runs confirm **18 services, 9,397 lines**, all five helper sizes, all six endpoint sizes, and **41 facts** with the stated per-file counts. Corrections:
    - Analytics contains **15** service classes, not the backlog’s 14.
    - Sixteen services have no injected dependencies or async, but two read the clock; “already pure” overstates this.
    - Method declaration-to-closing-brace spans are **293** lines for `ComputeEpicProgress`, **242** for `ComputeProgress`, and **180** for `Detect`.
    - Nine records share the four-field sprint shape; `SprintInfo` additionally carries duration and sync time.
    - The KB sweep finds **20 moved-path references across 15 files**.
    - Four of the 18 services have tests; the fifth test file covers a helper.
    
    **Suggested fix:** refresh these facts and specify counting conventions. Also update KB prose/type ownership after extraction, since replacing key-file paths alone leaves obsolete service names and split responsibilities.


---

Round 2 — 2026-10-14 — plan (fold verification, delta read, floor MEDIUM) — persisted by the architect session (job task-mv7jc3y4-2y567e, completed).

## Verdict: NO-GO

Plan lines refer to [plan.md](D:/src/fokus/docs/specs/F39-ServiceLayerSplit/delivery/plan.md). Collateral lists findings at MEDIUM or above.

| Round-one finding | Status | Plan lines | Verification | Collateral |
|---|---|---|---|---|
| R1-01 | closed | 56, 376 | Backlog now agrees on synthetic data. | none |
| R1-02 | closed | 187, 203, 216, 223, 228 | Test references and calls move alongside calculators. | none |
| R1-03 | closed | 197 | Both transition records move together. | none |
| R1-04 | closed | 195–201, 239 | Coupled helpers wait until their dependencies move. | none |
| R1-05 | closed | 134, 139 | Middleware errors use Production and fixed trace IDs. | R2-01 |
| R1-06 | closed | 129–157 | Original host, authentication and seeding requirements addressed. | R2-02 |
| R1-07 | closed | 149, 159, 172 | Workflow detection now has a baseline. | none |
| R1-08 | partial | 248–252, 275 | Analytics boundaries fixed; Application-owned sync failures remain exposed. | none |
| R1-09 | closed | 113–121, 368 | Mapping and CQRS skill refreshes included. | none |
| R1-10 | partial | 206, 239–241 | Replacement grep checks relocation, not final deduplication. | none |
| R1-11 | closed | 72, 232, 255, 354 | Purity checks target analytics C# source. | none |
| R1-12 | closed | 162–163 | Sub-team and window requests included. | none |
| R1-13 | closed | 59 | Intended shell explicitly identified. | R2-06 |
| R1-14 | closed | 182–187, 216, 223, 275, 377 | Registration ownership, callers and naming specified. | none |
| R1-15 | closed | 100, 215, 257 | Reply wrappers remain in API. | none |
| R1-16 | closed | 248, 264, 268 | Mapping extensions make the size gate reachable. | none |
| R1-17 | partial | 144–164 | Status/path/filter variants added; missing branches remain. | R2-04 |
| R1-18 | closed | 128, 144, 228, 321 | Baseline identity and UTC dates pinned. | R2-04 |
| R1-19 | closed | 276, 279, 286, 353 | Catch locations, test diffs and migration artifacts addressed. | R2-01, R2-02 |
| R1-20 | closed | 142, 168, 269, 325 | SQL counts now measured and compared. | R2-03 |
| R1-21 | closed | 292–317 | Architecture, conventions and affected skills included. | none |
| R1-22 | closed | 5–8, 238–241 | All 25 methods acknowledged; decomposition gets its own slice. | R2-05 |
| R1-23 | closed | 171–173 | Fixture and sabotage gates added. | R2-02, R2-06 |
| R1-24 | closed | 209, 243, 281, 327 | Intermediate comparisons added. | R2-05 |
| R1-25 | closed | 152, 265, 378 | Any-sprint activity check preserved. | none |
| R1-26 | closed | 17–45, 314, 362, 372 | Inventory and ownership descriptions refreshed. | none |

Findings:

- **R2-01 — HIGH — `fold` — [plan.md:134](D:/src/fokus/docs/specs/F39-ServiceLayerSplit/delivery/plan.md:134), lines 141, 165, 275–276, 323.** Production does not make the new failing sync captures stable. [SprintIssueSyncService.cs:75](D:/src/fokus/src/Services/Fokus/Fokus.API/Features/Sync/SprintIssueSyncService.cs:75) puts `ex.StackTrace` directly into the returned failure string, independently of exception middleware. Ordinary fake failures therefore expose different worktree paths, source locations and moved service frames. The required byte-identical comparison conflicts with the unchanged-catch guarantee.

- **R2-02 — MEDIUM — `fold` — [plan.md:156](D:/src/fokus/docs/specs/F39-ServiceLayerSplit/delivery/plan.md:156), lines 138, 165, 172.** The specified settings omit prerequisites for the newly added sync POSTs. Default `BoardId` is null, so both Jira sync endpoints return 400 before reaching the failing fake. Default database Xray credentials are null; setting only `XrayEnabled = true` leaves project execution sync returning immediately before calling `IXrayClient`. The prescribed fixture neither satisfies the per-route 200 gate nor exercises the promised sync failures.

- **R1-08 — MEDIUM — residual — [plan.md:248](D:/src/fokus/docs/specs/F39-ServiceLayerSplit/delivery/plan.md:248), line 275.** Step 8 covers replies containing **Domain** records. Step 10 moves `SprintSyncFailure` to **Application**, while [SyncSprintsCommand.cs:25](D:/src/fokus/src/Services/Fokus/Fokus.API/Features/Sync/SyncSprints/SyncSprintsCommand.cs:25) embeds that shared record in its reply. Updating its import preserves the ownership breach identified in round one and contradicts the endpoint-owned reply ruling at line 53.

- **R1-10 — MEDIUM — residual — [plan.md:206](D:/src/fokus/docs/specs/F39-ServiceLayerSplit/delivery/plan.md:206), lines 239–241.** The replacement grep proves that five helper classes left API. It does not inspect repeated helper declarations in their final Domain location. Step 7’s acceptance measures method length only, so the original deduplication verification was removed rather than repaired.

- **R2-03 — MEDIUM — `fold` — [plan.md:168](D:/src/fokus/docs/specs/F39-ServiceLayerSplit/delivery/plan.md:168), lines 169, 256, 269, 323–327.** The raw directory diff includes SQL counts and Swagger. A lower SQL count satisfies Step 9’s `after ≤ base` rule but fails Step 13’s equality rule. Similarly, the referenced F38 Swagger method allows schema type renames while comparing resolved property locations; raw `api.json` equality rejects those permitted renames. These acceptance rules contradict each other.

- **R2-04 — MEDIUM — `fold`; R1-17 remains partial — [plan.md:146](D:/src/fokus/docs/specs/F39-ServiceLayerSplit/delivery/plan.md:146), lines 157, 252.** Both fixture variants retain an active sprint, so neither exercises the no-active-sprint empty reply that Step 8 rewrites. The new grace-period coverage claim is also false: starting at `D-3` produces current day 4, while [DeveloperProgressService.cs:72](D:/src/fokus/src/Services/Fokus/Fokus.API/Features/Analytics/DeveloperProgressService.cs:72) defines grace as days 1–2. The promised branch coverage remains incomplete.

- **R2-05 — MEDIUM — `fold` — [plan.md:5](D:/src/fokus/docs/specs/F39-ServiceLayerSplit/delivery/plan.md:5), lines 56, 327.** The fold creates a separate 5–6 slice but schedules comparisons only after 4, 7, 10 and 13. This contradicts “before and after each slice.” All calculator moves and clock changes proceed into the separate decomposition slice without their own comparison.

- **R2-06 — MEDIUM — `fold` — [plan.md:173](D:/src/fokus/docs/specs/F39-ServiceLayerSplit/delivery/plan.md:173), lines 59, 218.** Two written checks are invalid under the declared Bash shell. Unquoted `D:\temp\f39-golden\base` becomes `D:tempf39-goldenbase`, breaking the sabotage cleanup check. The quoted brace expression passed to `find -name` is literal and falsely reports no remaining services. Read-only Bash verification returned zero matches for that expression while separately finding the existing `SprintSummaryService.cs`.

