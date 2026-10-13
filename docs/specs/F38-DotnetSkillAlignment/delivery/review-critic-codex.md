# F38 plan review — Codex (critic seat)

Round 1 — 2026-10-13 — plan — persisted by the architect session from the Codex job's collected output (job task-mv71itz6-ugmad4, completed).

## Verdict: NO-GO

1. **HIGH — [plan.md:145](D:/src/fokus/docs/specs/F38-DotnetSkillAlignment/delivery/plan.md:145) — Suggested `Roles` name conflicts with FastEndpoints.** Both installed versions declare the inherited `Endpoint<TRequest,TResponse>.Roles(string[])` method. Consequently, unqualified expressions such as `Roles.Admin` inside `CreateInvitationEndpoint`, `UpdateRoleEndpoint`, and `UpdateStatusEndpoint` bind to the method rather than the constants class, causing CS0119. **Fix:** choose `AuthRoles`/`AppRoles`, or require qualified references.

2. **MEDIUM — [plan.md:119](D:/src/fokus/docs/specs/F38-DotnetSkillAlignment/delivery/plan.md:119) — Lookup conversion needs explicit exceptions and predicate preservation.** `GetDeveloperDetailEndpoint.cs:22` returns 404 for inactive developers as well as missing ones; line 80 also returns 404 for developers without qualifying activity. A null-only throwing lookup loses these conditions, while `Guard.ThrowIfFalse` changes them to 400. Conversely, `ValidateInvitationEndpoint.cs:13` and `GetDeveloperProgressEndpoint.cs:22,30` intentionally return successful responses when optional lookups find nothing. The blanket prohibition conflicts with those behaviors and Step 9’s optional-lookup rule. **Fix:** explicitly preserve both developer exclusion predicates and whitelist optional lookup branches, with corresponding operator checks.

3. **MEDIUM — [plan.md:84](D:/src/fokus/docs/specs/F38-DotnetSkillAlignment/delivery/plan.md:84) — Several mandatory “Fokus facts” are obsolete.** Live `AuditedEntityConfiguration.cs` has no `DefaultDateSql` override; its applicable override points are `HasConcurrencyToken` and inherited `HasGeneratedId`. Live `ApplicationDbContext.cs` is an empty base declaration, with no cache helpers or `ref` parameters. Re-running searches under BuildingBlocks found none of the purported members. **Fix:** correct these preservation requirements before merging the skill; retain accurate descriptions of the live classes.

4. **MEDIUM — [plan.md:168](D:/src/fokus/docs/specs/F38-DotnetSkillAlignment/delivery/plan.md:168) — Step 6’s acceptance command cannot pass.** The exact regex returns **0** for `Features/Auth/Login/LoginEndpoint.cs`, whose declaration is `public static class LoginEndpoint`. **Fix:** include `static` declarations or explicitly exclude this minimal-API registration class from the FastEndpoints check.

5. **MEDIUM — [plan.md:165](D:/src/fokus/docs/specs/F38-DotnetSkillAlignment/delivery/plan.md:165) — Step 6 misses a layout explicitly covered by the skill.** `Sprints/UpdateSprint/UpdateSprintValidator.cs` remains separate from `UpdateSprintModels.cs`. The plugin skill explicitly applies when a validator occupies its own file, but neither the target list nor the endpoint-only acceptance search detects this case. **Fix:** include that operation and consolidate its request, response, and validator into `UpdateSprintCommand.cs`, updating references together.

6. **MEDIUM — [plan.md:117](D:/src/fokus/docs/specs/F38-DotnetSkillAlignment/delivery/plan.md:117) — A hand-written error response remains outside the searched directory.** `Auth/ActiveUserPreProcessor.cs:24–35` constructs and sends a 401 response directly. Both Step 3 acceptance searches can pass while the copied skill’s “one central mapper” rule remains violated. **Fix:** route this rejection through the existing unauthorized exception mapping, preserving 401, or document an explicit authentication-hook exception in the local skill and Decisions table.

7. **MEDIUM — [plan.md:81](D:/src/fokus/docs/specs/F38-DotnetSkillAlignment/delivery/plan.md:81) — The copied catch prohibition conflicts with existing sync contracts.** The plugin forbids swallowing exceptions and permits boundary catches only for cleanup and rethrow. Fokus deliberately catches failures in `SyncSprintsEndpoint.cs:51`, `SprintIssueSyncService.cs:73,175`, and several `XrayIssueSyncService` paths, returning partial results or warnings. No step changes these paths or records a deviation. **Fix:** explicitly preserve and document the batch-sync and best-effort notification exceptions; removing their catches would change behavior.

8. **MEDIUM — [plan.md:117](D:/src/fokus/docs/specs/F38-DotnetSkillAlignment/delivery/plan.md:117) — Validation-message alignment is omitted.** The plugin’s `error-handling` § Validation Messages requires shared helpers/constants instead of per-rule literals. Live validators still contain many such literals, including `CreateInvitationCommand.cs:26,28,32` and `SaveHealthConfigEndpoint.cs:19,27`. Step 3 changes handler replies only. **Fix:** include a message-preserving helper/constants pass, using existing `Blocks.Core/FluentValidation/Extensions.cs` where appropriate, or explicitly defer this part in the local skill and plan.

9. **MEDIUM — [plan.md:82](D:/src/fokus/docs/specs/F38-DotnetSkillAlignment/delivery/plan.md:82) — The authorization adaptation needs a sensitive-read exception.** The plugin states that read models accept any authenticated caller. Fokus’s `GET /api/users` and `GET /api/invitations` are intentionally admin-only, and Step 5 preserves their policies. D5 addresses resource ownership but does not resolve this separate contradiction. **Fix:** state explicitly in the local skill that these administrative reads retain the admin policy.

10. **MEDIUM — [plan.md:217](D:/src/fokus/docs/specs/F38-DotnetSkillAlignment/delivery/plan.md:217) — The documentation sweep misses contradictory Xray guidance.** `src/Modules/Xray/CLAUDE.md:18` says credentials are not read from appsettings or through startup options. Live `Program.cs:35–42` does exactly that, and Step 7 preserves it. None of the proposed search patterns detects that paragraph. `docs/architecture/v1.md:105` also incorrectly attributes the exception mapper to FastEndpoints and lies outside the sweep’s directories. **Fix:** explicitly update both descriptions, including Xray’s config-to-settings startup copy and subsequent per-call credential use.

11. **MEDIUM — [plan.md:228](D:/src/fokus/docs/specs/F38-DotnetSkillAlignment/delivery/plan.md:228) — Operator status checks are not executable unambiguously through the UI.** Managers’ Settings controls are disabled, so an ordinary save action cannot exercise the server’s 403. The router has `/sprints`, but no `/sprints/:id` route; SPA navigation also does not directly establish an API’s HTTP status. **Fix:** specify exact authenticated API requests or browser Network/console actions for the 403 and 404 checks, including the filtered developer cases.

12. **LOW — [plan.md:197](D:/src/fokus/docs/specs/F38-DotnetSkillAlignment/delivery/plan.md:197) — “Port: none assigned” is inaccurate.** `Properties/launchSettings.json:8` assigns `http://localhost:5000`, consistent with the README. Docker and compose-index absence do not remove that development port. **Fix:** record HTTP 5000 for the development profile and separately state that no compose service index is assigned.

13. **LOW — [plan.md:108](D:/src/fokus/docs/specs/F38-DotnetSkillAlignment/delivery/plan.md:108) — The runner compatibility explanation is false.** Version 2 is not the last runner major supporting xUnit 2. The official xUnit 2 guide uses runner 3.1.1, and runner 4.0.1 supports xUnit versions from 1.9.2 onward. **Fix:** retain the owner-authorized pin, but explain it as an owner scope decision; correct the “with xunit v3” dependency claim in the open-work handoff. [xUnit 2 guide](https://xunit.net/docs/getting-started/v2/getting-started), [runner 4.0.1](https://www.nuget.org/packages/xunit.runner.visualstudio/4.0.1).

14. **LOW — [plan.md:103](D:/src/fokus/docs/specs/F38-DotnetSkillAlignment/delivery/plan.md:103) — EF Core 10.0.13 is not substantiated as the plan-time version.** The installed cache contains 10.0.12; the NuGet page checked during review identifies 10.0.12 as stable, while the 10.0.13 page could not be retrieved. **Fix:** remove or re-verify that parenthetical baseline and retain the implementation-day lookup requirement. [NuGet EF Core version listing](https://www.nuget.org/packages/Microsoft.EntityFrameworkCore/10.0.12).

15. **LOW — [plan.md:219](D:/src/fokus/docs/specs/F38-DotnetSkillAlignment/delivery/plan.md:219) — The documentation grep includes machine-local history.** Re-running it finds numerous `.claude/audit/*.log` entries containing historical commands. These survive all planned documentation edits and require additional whitelisting beyond `framework-currency`. **Fix:** search live Markdown files or explicitly exclude `.claude/audit/`; keep deliberate documentation whitelists bounded.

16. **LOW — [plan.md:122](D:/src/fokus/docs/specs/F38-DotnetSkillAlignment/delivery/plan.md:122) — The error-envelope description and invariant need correction.** The middleware emits camel-case `statusCode`, `message`, `traceId`, and `details`, rather than just `{StatusCode, Message}`. Replacing FastEndpoints validation replies also removes their `errors` property, despite the Must-NOT-Change table claiming every JSON property name is pinned. **Fix:** describe the actual envelope and explicitly exempt the approved error-contract changes from that invariant.

Read-only review completed. No files were modified; builds, restores, tests, and API starts were not run.


---

Round 2 — 2026-10-13 — plan (fold verification, delta read, floor MEDIUM) — persisted by the architect session from the Codex job's collected output (job task-mv72hc1u-aptxyu, completed).

## Verdict: NO-GO

19 findings are closed; 4 are partial. The remaining MEDIUM issues concern wire-name verification, executable database checks, and the SignalR catch carve-out.

All line references below are to [plan.md](D:/src/fokus/docs/specs/F38-DotnetSkillAlignment/delivery/plan.md). “Collateral” lists new MEDIUM-or-higher problems introduced by the fold.

| Round-1 id | Verification | Plan line(s) | Collateral |
|---|---|---|---|
| R1-01 | closed — binding `AccessRole`/`AccessRoles` names avoid both collisions | 69, 178–190, 263 | none |
| R1-02 | closed — required, rule-based and optional lookups distinguished; existing inline throws included | 86, 142–146, 297 | none |
| R1-03 | closed — stale persistence facts dropped; remaining facts require live verification | 83, 96–102, 115 | none |
| R1-04 | closed — static login mapping explicitly excepted | 205 | none |
| R1-05 | closed — UpdateSprint models and validator included | 202, 206 | none |
| R1-06 | closed — pre-processor throws the mapped 401 exception | 148, 154, 327 | none |
| R1-07 | partial — per-item catches covered; log-only SignalR fallback omitted | 89, 302, 328 | none |
| R1-08 | closed — validation-message migration explicitly deferred and handed off | 48, 89, 329 | none |
| R1-09 | closed — conflicting authorization rules carved out; policy choice explained | 90–94, 322 | none |
| R1-10 | closed — both missed documentation surfaces named | 261–262 | none |
| R1-11 | closed — console requests reach the actual protected and developer-detail routes | 273, 278–279 | none |
| R1-12 | partial — baseline and page sweep added, but global property-name set cannot prove the pin | 122, 207, 277, 295 | R2-01, R2-02 |
| R1-13 | partial — proof moved to Step 2, but database override is invalid as written | 73, 122, 136, 289 | R2-02 |
| R1-14 | closed — missing settings row explicitly maps to 500 | 219 | none |
| R1-15 | closed — ignored session logs excluded through `git grep --untracked` | 265–266 | none |
| R1-16 | closed — development URL replaces the unassigned-port claim | 236 | none |
| R1-17 | closed — both missed role examples explicitly targeted | 256, 258, 264 | none |
| R1-18 | closed — runner pin uses the owner decision as its reason | 47, 129 | none |
| R1-19 | closed — unsubstantiated version figure replaced with implementation-day selection | 124 | none |
| R1-20 | closed — envelope corrected; success-wire pin and approved error-body changes distinguished | 88, 149, 295–296, 324 | none |
| R1-21 | closed — count restricted to folders containing `SKILL.md` | 109, 114 | none |
| R1-22 | closed — call mix describes what survives Step 3 | 162–166 | none |
| R1-23 | partial — heading-pointer check added, but the proposed skill-name grep lacks recursion over its folder operands | 112–113 | none |

The remaining reconciled clauses have these collateral results:

| Touched clause | Plan line(s) | Collateral |
|---|---|---|
| Context | 13, 31–32 | none |
| Scope | 46–48 | none |
| Skill Mapping rows 3 and 9 | 56, 62 | none |
| Domain Model Changes | 69 | none |
| Data Model Changes | 73 | R2-02 |
| Migration Notes | 289 | R2-02 |
| Must-NOT-Change | 295–304 | R2-01 at 295; none elsewhere |
| KB Impact | 312 | none |
| D5, D7, D10–D12 | 322, 324, 327–329 | none |
| Plan Review | 337 | none |

**[MEDIUM] R1-07 — residual: SignalR fallback remains outside the catch carve-out.**  
**File:** [plan.md:89](D:/src/fokus/docs/specs/F38-DotnetSkillAlignment/delivery/plan.md:89).  
The carve-out and D11 cover per-item catches that report failures in replies. The existing [SyncSprints catch:51](D:/src/fokus/src/Services/Fokus/Fokus.API/Features/Sync/SyncSprints/SyncSprintsEndpoint.cs:51) instead logs a failed SignalR broadcast and preserves the successful sync reply. It fits neither description. This leaves the copied catch prohibition unresolved for a surface R1-07 explicitly named, while line 302 requires every catch block to remain unchanged. Removing that fallback would break the existing guarantee that notification failure does not fail the sync response.

**[MEDIUM] R2-01 — `fold`: global property-name comparison cannot verify each reply’s wire names.**  
**File:** [plan.md:207](D:/src/fokus/docs/specs/F38-DotnetSkillAlignment/delivery/plan.md:207).  
Flattening all schema property names into one sorted set discards their schema and operation associations. Renaming one response property from `name` to `id` can produce an empty diff when other schemas retain both names. The new gate therefore permits a changed success-reply property name despite line 295’s guarantee. Verification must retain each property’s location within the corresponding request or response schema, while allowing schema-type renames.

**[MEDIUM] R2-02 — `fold`: temporary-database instructions supply a path where SQLite requires a connection string.**  
**File:** [plan.md:122](D:/src/fokus/docs/specs/F38-DotnetSkillAlignment/delivery/plan.md:122).  
The instruction sets `ConnectionStrings__Database` to a filesystem path. [Persistence registration:20](D:/src/fokus/src/Services/Fokus/Fokus.Persistence/DependencyInjection.cs:20) passes that value directly to `UseSqlite`, and [startup:23](D:/src/fokus/src/Services/Fokus/Fokus.API/Program.cs:23) parses it with `SqliteConnectionStringBuilder`. The value must have a supported key, for example `Data Source=D:\temp\f38-before.db`. As written, the baseline capture and subsequent temporary-database checks cannot run, undermining the early model-drift proof promised by lines 73, 136 and 289.

No additional MEDIUM-or-higher contradictions or guarantee breaks were found in the permitted delta. No files were modified.

