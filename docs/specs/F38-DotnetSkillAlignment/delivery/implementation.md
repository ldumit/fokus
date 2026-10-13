# F38 — Dotnet Skill Alignment — Implementation

**Model:** claude-opus-5-5

## Step 1 — Bring the eight skills into `.claude/skills/` as self-contained copies
**Files:**
- `.claude/skills/central-package-management/SKILL.md` — NEW, copied from the plugin. Rewrote Look First for Fokus's shared places. Assumes now names `src/Fokus.slnx`, and the shared libraries and modules share one props file. The sample pin is Refit with the owner's pin reason. The bare-reference sample keeps EF Design's `PrivateAssets`/`IncludeAssets` children as `Fokus.API.csproj` has them. The search uses `grep --include`, and the nested-props check uses `find`, because `rg` is not assumed.
- `.claude/skills/framework-currency/SKILL.md` — NEW, copied from the plugin. Rewrote Look First. Added "no preview, no floating version, a held major is a pin". Gave the FluentValidation 12 example of a transitive move. Added the receiver-less grep used by Step 4's accept and the `return`ed-call case (`GetMeEndpoint`). The migration map marks `SendErrorsAsync` and `SendUnauthorizedAsync` as "removed", the same as NotFound/Forbidden, because no endpoint writes an error reply here. The two false positives are named with their live files.
- `.claude/skills/add-hosted-service/SKILL.md` — NEW, copied from the plugin. Rewrote Look First. Added "This repo:" to § Start-up Work: Fokus's start-up work is three start-up lines, not a `BackgroundService`. Reworded the references to absent skills (listed below) and gave Verify concrete greps.
- `.claude/skills/error-handling/SKILL.md` — MERGED, with plugin text as the base.
  - The Exception Hierarchy now teaches this repo's `Blocks.Exceptions` + `DomainException` family. The guards are shown with their real message-taking signatures. The three-lookup rule (required / rule-decided 404 / optional) is added.
  - § Prohibitions gains the no-hand-written-reply rule and this repo's two kept `catch` forms (D11).
  - § Validation Messages gains a "This repo" note: per-rule literals are open work (D12).
  - The mapper section is replaced by the live mapper's status table, envelope and logging, plus FastEndpoints' own validation reply.
- `.claude/skills/authorization-patterns/SKILL.md` — MERGED, with plugin text as the base.
  - Added § This Repo's Form: Google cookie sign-in, `AccessRole`/`AccessRoles`, `AccessPolicies.Admins`, the carve-outs of binding rule 2 / Phase 2's last sentence / Phase 3, the admin-only user and invitation lists, target-role comparisons as business rules, the active-user pre-processor, and identity stamping "not adopted yet" (D13).
  - Each phase is marked where it does not apply.
  - Added the repo's `AccessRole`/`AccessRoles`/`AccessPolicies` samples beside the generic ones.
- `.claude/skills/extract-endpoint-types/SKILL.md` — MERGED, with plugin text as the base.
  - Re-pointed the local Reference Examples at `Analytics/GetSprintSummary/`, `Auth/CreateInvitation/` and `Sprints/UpdateSprint/`.
  - Step 6 now gives the per-file public-type count in place of the convention check, with the `LoginEndpoint` exception.
  - Noted that there is no `_Shared` folder (`UserResponse` is reused from `Auth/GetUsers`).
  - Renames are type-only; wire names stay.
- `.claude/skills/persistence-patterns/SKILL.md` — MERGED, with plugin text as the base. Each section now states this repo's form:
  - SQLite;
  - `RepositoryBase<TContext,TEntity,TKey>` with public `Query()`, `FindByIdAsync` and the `FindByIdOrThrowAsync` extension;
  - no Tier-2 generic repository;
  - no read stores (repo convention `ef-core.md` § Cross-Aggregate Queries);
  - `EntityConfiguration`/`HasGeneratedId`, `AuditedEntityConfiguration`/`HasConcurrencyToken`;
  - explicit registration;
  - the interceptor as it is;
  - migrations and seeding as start-up lines.

  The old local copy's false facts were dropped: `IRepository.cs`/`Repository.cs` paths, `ApplicationDbContext` cache helpers and their `ref` warning, `DefaultDateSql`/`HasDefaultValueSql`, the automatic JSON seed hook, and `Seed.cs`/`ManualGenerateIdScope`.
- `.claude/skills/create-service-claude-md/` (`SKILL.md`, `workflows/CaptureAxes.md`, `workflows/WriteClaudeMd.md`) — REPLACED whole by the plugin copy, which includes `disable-model-invocation: true` (Step 8 reads the workflow directly).
- `.claude/README.md` — Information Layers row `(38 skills)` → `(42 skills)` (`ls .claude/skills/*/SKILL.md | wc -l` = 42; `.claude/skills/docs/` holds only `audit/`).

**Skills:** None — plan Skill Mapping row 1 `(none)`, TDD no.

**Repo facts in the merged copies:** each fact names the live file that confirms it, or reads `target — lands in Step N` (D14).
- *error-handling*
  - `HttpException(int, string)` with `BadRequest`/`NotFound`/`Unauthorized`/`Forbidden`/`Conflict`/`BadGateway` → `src/BuildingBlocks/Blocks.Exceptions/*.cs`.
  - `DomainException : Exception` → `src/BuildingBlocks/Blocks.Domain/Exceptions/DomainException.cs:3`.
  - `Guard.NotFound(value, message)`, `ThrowIfNullOrWhiteSpace`/`ThrowIfFalse` → `BadRequestException` → `src/BuildingBlocks/Blocks.Core/Guard.cs`.
  - `OrThrowNotFound(message)` → `src/BuildingBlocks/Blocks.Core/GuardExtensions.cs`.
  - `FindByIdOrThrowAsync` and its message → `src/BuildingBlocks/Blocks.EntityFrameworkCore/Extensions/RepositoryExtensions.cs`.
  - Mapper status table, no `HttpException` arm, `LogError` on every mapped error, fixed 500 message, `details` only in Development, 499 on cancellation, camel-case envelope → `src/BuildingBlocks/Blocks.AspNetCore/Middlewares/GlobalExceptionMiddleware.cs`.
  - Mapper registered first → `src/Services/Fokus/Fokus.API/Program.cs:46`.
  - Per-item sync catches → `Features/Sync/SprintIssueSyncService.cs:73,121,175` and `Features/Xray/XrayIssueSyncService.cs:39,171,207,302`.
  - Best-effort broadcast catch → `Features/Sync/SyncSprints/SyncSprintsEndpoint.cs:51`.
  - Unused message helpers → `src/BuildingBlocks/Blocks.Core/FluentValidation/Extensions.cs` (0 callers).
  - Length constants → `src/BuildingBlocks/Blocks.Core/MaxLength.cs`.
  - Literal `WithMessage("…")` in validators → `grep` under `src/Services`.
  - No endpoint or pre-processor writes an error reply; `ActiveUserPreProcessor` throws `UnauthorizedException` → target — lands in Step 3.
  - `AppSettingsRepository.GetAsync` throws `InvalidOperationException` → target — lands in Step 7.
- *authorization-patterns*
  - Google + cookie, `OnRedirectToLogin` 401 / `OnRedirectToAccessDenied` 403, role claim from `AppUser.Role` → `src/Services/Fokus/Fokus.API/Auth/AuthConfiguration.cs:16-49,148`.
  - Anonymous endpoints → `Features/Auth/Login/LoginEndpoint.cs:21`, `Features/Auth/ValidateInvitation/ValidateInvitationEndpoint.cs:3`.
  - Target-role comparisons → `Features/Auth/UpdateRole/UpdateRoleEndpoint.cs:21`, `Features/Auth/UpdateStatus/UpdateStatusEndpoint.cs:21`.
  - Global pre-processor registration → `src/Services/Fokus/Fokus.API/DependencyInjection.cs:60`; its two messages → `Auth/ActiveUserPreProcessor.cs:24`.
  - No stamp hook; the caller is read from claims → `Features/Auth/CreateInvitation/CreateInvitationEndpoint.cs:37`, `Features/Auth/GetMe/GetMeEndpoint.cs:13`.
  - Team role → `src/Services/Fokus/Fokus.Domain/Developer/Developer.cs:9`.
  - Admin-only reads `GetUsers`, `GetInvitations` among the 23 gated endpoints → `grep 'Roles = "'` (23 files).
  - `AccessRole`/`AccessRoles` in `Fokus.Domain/Auth/` → target — lands in Step 5.
  - `AccessPolicies.Admins` registered in `AuthConfiguration.cs` and on the 23 endpoints → target — lands in Step 5.
- *persistence-patterns*
  - `UseSqlite(GetConnectionString("Database"))` → `src/Services/Fokus/Fokus.Persistence/DependencyInjection.cs:20`.
  - `Data Source=fokus.db` → `src/Services/Fokus/Fokus.API/appsettings.json:3`.
  - Project layout → `ls src/Services/Fokus/Fokus.Persistence`.
  - `RepositoryBase` public virtual `Query()`, `FindByIdAsync`, `UpsertAsync` with `SetValues(entity)` → `src/BuildingBlocks/Blocks.EntityFrameworkCore/Repositories/RepositoryBase.cs`.
  - 4 repositories derive the base, 3 do not; string keys → `src/Services/Fokus/Fokus.Persistence/Repositories/*.cs`.
  - Explicit scoped registration → `Fokus.Persistence/DependencyInjection.cs:24-30`.
  - `HasGeneratedId` → `src/BuildingBlocks/Blocks.EntityFrameworkCore/EntityConfigurations/EntityConfiguration.cs`; overrides in `Fokus.Persistence/Configurations/{Sprint,Ticket,Developer,TestExecution,TestRun,TestSet}Configuration.cs`.
  - `AuditedEntityConfiguration`/`HasConcurrencyToken`, no users → `EntityConfigurations/AuditedEntityConfiguration.cs`; grep finds 0 users in `Configurations/`.
  - Six direct `IEntityTypeConfiguration` → `Configurations/` (grep).
  - Length literals → `Configurations/AppSettingsConfiguration.cs:35`.
  - `HasConversion<string>` → `Configurations/SprintConfiguration.cs`.
  - `ToJson` owned groups → `Configurations/AppSettingsConfiguration.cs:10-12`.
  - `FokusDbContext : ApplicationDbContext<>` (empty base) → `Fokus.Persistence/FokusDbContext.cs`, `Blocks.EntityFrameworkCore/ApplicationDbContext.cs`.
  - Interceptor → `Blocks.EntityFrameworkCore/Interceptors/DispatchDomainEventsInterceptor.cs`.
  - `Mode.WaitForAll` → `src/BuildingBlocks/Blocks.FastEndpoints/DomainEventPublisher.cs`.
  - No `Data/` seed folder → `ls Fokus.Persistence`.
  - `AppSettingsRepository.SaveAsync` assigns owned members by hand → `Repositories/AppSettingsRepository.cs:17-31`.
  - Bases seed nothing (no automatic JSON hook) → target — lands in Step 7.
  - Folder-then-migrate start-up line, settings-row line in every environment, Xray line → target — lands in Step 7 (inline at `Program.cs:17-44` today).
  - `GetAsync` read-only and throwing → target — lands in Step 7.
- *extract-endpoint-types*
  - FastEndpoints → `src/Services/Fokus/CLAUDE.md:3`.
  - Query example → `Features/Analytics/GetSprintSummary/` (endpoint has 1 type; query has request and validator).
  - Command example → `Features/Auth/CreateInvitation/` (1 type; request, reply, validator).
  - `UpdateSprint/` two-file layout → target — lands in Step 6.
  - No convention check (the only test project is `src/Services/Fokus/Fokus.Tests`) → `find src -name '*Test*.csproj'`.
  - `LoginEndpoint` is a `public static class` → `Features/Auth/Login/LoginEndpoint.cs:6`.
  - `UserResponse` is shared → `Features/Auth/GetUsers/GetUsersQuery.cs`.
  - No `_Shared` folder → `find`.
- *new copies (the repo facts they carry)*
  - `src/Fokus.slnx` → live.
  - EF Design children → `src/Services/Fokus/Fokus.API/Fokus.API.csproj:16-19`.
  - False positives → `src/Modules/Xray/Xray.GraphQL/GraphQLXrayClient.cs:296`, `Features/Sync/SyncSprints/SyncSprintsEndpoint.cs:46`.
  - `ValidateOnStart` → `src/Modules/Jira/Jira.RestApi/DependencyInjection.cs:20`.
  - No hosted service; no message bus (0 `MassTransit` references in `*.csproj`) → grep.
  - `src/Directory.Packages.props` and the pinned set → target — lands in Step 2.
  - Error-reply calls removed → target — lands in Step 3.
  - `Send.*` everywhere → target — lands in Step 4.
  - Start-up lines → target — lands in Step 7.

**References to skills absent from `.claude/skills/`** (plugin text → what was done):
- `service-infra-conventions` § 3 Options binding (add-hosted-service Assumes; authorization Phase 5) → re-pointed to `service-registration` § Options Validation.
- `service-infra-conventions` § 1 claims reader (authorization Assumes, Phase 4, "does NOT do") → reworded to "a claims reader (`IClaimsProvider`) — this repo has none yet" and "a small class over `IHttpContextAccessor`".
- `create-host` (authorization Phase 5; persistence Spine Rules) → dropped (no host project in this repo).
- `consumer-patterns` (add-hosted-service "does NOT do") → reworded: "the bus runs them; this repo has no message bus".
- `add-typed-client` (add-hosted-service "does NOT do") → reworded: "a typed HTTP client … this repo's clients live in the modules".
- `add-conventions-check` (extract-endpoint-types Step 6) → replaced by the per-file public-type count.
- `project-lists.md` § Helpers (Look First, five files) and § Logging classes (add-hosted-service § Logging) → rewritten as the Fokus shared-places list; the logging-class row sentence dropped.

**Section pointers re-pointed or dropped:**
- `create-feature` § Requests, Replies and Names (authorization Phase 4) → no such heading; reworded to "`create-feature` owns the request shapes".
- `create-feature` § Shared Base Endpoint (extract-endpoint-types) → `create-feature/workflows/EndpointFastEndpoints.md` § Optional: Custom Base Endpoint, plus the old local copy's `extract-feature-service`.
- `create-aggregate` § The Aggregate Base (persistence Assumes) → no such heading; pointed at `src/BuildingBlocks/Blocks.Domain/Entities/`.
- `create-aggregate` § Field-Length Constants (persistence Entity Configuration) → no such heading; pointed at `src/BuildingBlocks/Blocks.Core/MaxLength.cs`.
- `create-domain-event-handler` § Decision 3 (persistence Save Interceptor) → § dropped; the skill name is kept.
- `conventions/csharp.md` § Comments and Logging (add-hosted-service § Logging) → removed (no such heading).
- `conventions/ef-core.md` § Queries (persistence Tier 1) → removed with the protected-`Query()` sentence. The new pointers `ef-core.md` § Migrations, § Cross-Aggregate Queries, § Additional Gotchas and § Migration Defaults all resolve.

**Decisions:**
- New copies keep the plugin's generic samples (orders, clerks) and add Fokus facts beside them, never in place of the rule.
- In `framework-currency`, `SendErrorsAsync` and `SendUnauthorizedAsync` map to "removed" rather than to `Send.*`, consistent with Step 3 and `error-handling`.
- The `authorization-patterns` sample `AccessRole` enum numbers members `Admin = 1, Manager = 2`. The plan names the members only; the column is stored as text, so the numbers never reach the database.
- The Look First block names `create-building-blocks-package` as the way to add a shared project.

**Deviations:** None. Step 1 touches no `src/` file. The step close ran `verify-run.js --profile fast` once: pass, 8.2 s.

**Accept results:**
- plugin-reference grep → 0.
- Backticked skill names → all 16 exist under `.claude/skills/`.
- `conventions/*.md §` pointers → 4, all resolve.
- `ls .claude/skills/*/SKILL.md | wc -l` = 42 = README.
- Step 9's `Send*Async|Roles = |SeedFromJsonFile` git grep over the eight folders → 0 outside `framework-currency`.

## Step 2 — Central package versions
**Files:**
- `docs/specs/F38-DotnetSkillAlignment/delivery/api-before.json` — NEW, the baseline FastEndpoints document, captured first on the unchanged tree (API in Development on `Data Source=D:\temp\f38-before.db`, port 5099, `--no-launch-profile`; 52 paths, 236,649 bytes); the API was stopped after.
- `src/Directory.Packages.props` — NEW: `ManagePackageVersionsCentrally` and `CentralPackageTransitivePinningEnabled` on; 17 `PackageVersion` lines. Microsoft.* at 10.0.13 (latest stable 10.0.x on NuGet, 2026-10-13); Scalar.AspNetCore 2.18.0; FluentAssertions 8.11.0; xunit 2.9.3; FastEndpoints/FastEndpoints.Swagger 6.0.0 and FluentValidation 11.11.0 unchanged (Step 4 moves them); Refit.HttpClientFactory 8.0.0, Microsoft.NET.Test.Sdk 17.14.1, xunit.runner.visualstudio 2.8.2, NSubstitute 5.3.0 pinned, each with the one-line owner-decision comment. Test packages sit in their own `ItemGroup`.
- `src/BuildingBlocks/Blocks.Core/Blocks.Core.csproj`, `Blocks.Domain/Blocks.Domain.csproj`, `Blocks.EntityFrameworkCore/Blocks.EntityFrameworkCore.csproj`, `Blocks.FastEndpoints/Blocks.FastEndpoints.csproj`, `src/Modules/Jira/Jira.RestApi/Jira.RestApi.csproj`, `src/Modules/Xray/Xray.GraphQL/Xray.GraphQL.csproj`, `src/Services/Fokus/Fokus.API/Fokus.API.csproj`, `Fokus.Persistence/Fokus.Persistence.csproj`, `Fokus.Tests/Fokus.Tests.csproj` — the `Version` attribute removed from each of the 20 `PackageReference` lines; nothing else changed (EF Design keeps its `PrivateAssets`/`IncludeAssets` children).

**Skills:** central-package-management
**Decisions:**
- Versions read from the NuGet flat-container index the day of the edit, not from memory: newest stable 10.0.x is 10.0.13 for all seven Microsoft.* packages; Scalar 2.x newest is 2.18.0 (no 3.x stable); FluentAssertions 8.x newest is 8.11.0.
- The four pinned versions match the restore record (`Fokus.Tests/obj/project.assets.json`: Microsoft.NET.Test.Sdk 17.14.1, NSubstitute 5.3.0, xunit.runner.visualstudio 2.8.2; Refit 8.0.0 was already exact).
- Microsoft.AspNetCore.Authentication.Google moves 10.0.7 → 10.0.13 under the same "latest stable 10.0.x" rule.
**Deviations:** None.

**Accept results:**
- Three-form search (`Version=`, `<Version>`, `VersionOverride=` in `*.csproj` under `src/`) → 0 (baseline 20).
- Nested-props check → exactly `src/Directory.Packages.props`.
- `grep -nE 'Version="[^"]*(preview|\*)' src/Directory.Packages.props` → 0.
- Restore resolved EF Core / Sqlite / Design / OpenApi 10.0.13, Scalar 2.18.0, FastEndpoints 6.0.0 (`Fokus.API/obj/project.assets.json`), no restore log entries.
- Step close `verify-run.js --profile fast` → pass, 11.1 s (the full suite is the close gate's).
- API on a new database (`Data Source=D:\temp\f38-step2.db`): all 15 migrations applied, the default `AppSettings` row inserted, "Application started", no pending-model-changes warning or error; stopped after. The one `warn` (EF 10103, `First` without `OrderBy`, from `Program.cs`'s Xray block) is in the baseline log too.

## Step 3 — Required lookups throw; no hand-written error replies
**Files:** all under `src/Services/Fokus/Fokus.API/`.
- `GlobalUsings.cs` — added `global using Blocks.Core;` so `OrThrowNotFound` resolves (Blocks.Core reaches Fokus.API through Blocks.Domain; no project reference added).
- Required lookups `SendNotFoundAsync` → `.OrThrowNotFound(message)`; these endpoints had no message before (the 6.0 `SendNotFoundAsync` sends an empty 404), so each gets one:
  - `Features/Analytics/GetFailingTickets/GetFailingTicketsEndpoint.cs`, `Features/Analytics/GetUntestedTickets/GetUntestedTicketsEndpoint.cs` — both the sprint-list lookup and the `GetSprintWithMembershipsAsync` lookup → `"Sprint {id} was not found."` (the F37 analytics wording). The unused `sprintInfo` local is gone; the list lookup stays as a bare guard call.
  - `Features/Analytics/GetQaMetrics/GetQaMetricsEndpoint.cs` — sprint-list lookup, same message, same bare guard call.
  - `Features/Sprints/GetTestTimeline/GetTestTimelineEndpoint.cs` — `sprintInfo` (still used), same message.
  - `Features/Sprints/UpdateSprint/UpdateSprintEndpoint.cs` — `GetByIdAsync` → `"Sprint {id} was not found."`.
  - `Features/Auth/RevokeInvitation/RevokeInvitationEndpoint.cs` → `"Invitation {id} was not found."`.
  - `Features/Auth/UpdateRole/UpdateRoleEndpoint.cs`, `Features/Auth/UpdateStatus/UpdateStatusEndpoint.cs` → `"User {id} was not found."`.
  - `Features/Developers/GetCapacity/GetCapacityEndpoint.cs`, `Features/Developers/SetCapacity/SetCapacityEndpoint.cs` → `"Developer {accountId} was not found."` (bare guard call; the developer value was only an existence check).
  - `Features/Team/UpdateTeamConfig/UpdateTeamConfigEndpoint.cs` — `UpdateTeamConfigAsync(...)` result → `"Developer {accountId} was not found."`.
- Rule-decided 404s → direct `throw new NotFoundException(message)`, conditions unchanged: `Features/Analytics/GetDeveloperDetail/GetDeveloperDetailEndpoint.cs` — missing or inactive developer (`"Developer {accountId} was not found or is inactive."`) and no qualifying activity (`"Developer {accountId} has no activity in closed sprints."`); both match `docs/kb` `developer-detail.md` § 404 Cases.
- `?? throw new NotFoundException(` → `.OrThrowNotFound(` with the same message: the 9 F37 analytics endpoints (`GetBugRatio`, `GetCarryOver`, `GetCycleTime`, `GetDeveloperQuality`, `GetDeveloperThroughput`, `GetLeaderboard`, `GetQaWorkload`, `GetScopeChange`, `GetSprintSummary`) and the 2 in `Features/Sync/SyncSprints/SyncSprintsEndpoint.cs`. The broadcast `catch` there is untouched.
- `Features/Settings/SaveCycleTimeBoundaries/SaveCycleTimeBoundariesEndpoint.cs` — the three `AddError` + `SendErrorsAsync(400)` branches → `throw new BadRequestException("…")` with the same three messages (D7).
- `Auth/ActiveUserPreProcessor.cs` — the hand-written 401 body → `throw new UnauthorizedException(user is null ? "Account not found." : "Your account has been deactivated.")` (D10); the now-unused `System.Text.Json` using removed.

**Optional lookups left as 200 replies** (a miss is a normal answer):
- `Features/Auth/ValidateInvitation/ValidateInvitationEndpoint.cs:13-17` — no invitation for the token → `Valid = false, Error = "not-found"` (and the used / revoked / expired branches after it).
- `Features/Analytics/GetDeveloperProgress/GetDeveloperProgressEndpoint.cs:22-26` — no active sprint → the empty progress reply; `:30-34` — the active sprint did not load with memberships → the same empty reply.

**Skills:** error-handling
**Decisions:**
- Every required lookup uses `.OrThrowNotFound(message)`, not `FindByIdOrThrowAsync`: the endpoints call the repositories' own `GetByIdAsync`/list methods, and an explicit message per entity reads the same across them.
- A required lookup whose value is not used afterwards is a bare `….OrThrowNotFound(…);` statement, never `_ = …` (Step 7's accept forbids discards).
- New not-found messages follow F37's `"Sprint {id} was not found."` shape for every entity.
**Deviations:** None. `Blocks.AspNetCore/Middlewares/GlobalExceptionMiddleware.cs:82` is the only `Response.WriteAsync` left under `src/` — the mapper itself.
**Fix (after Step 7, on the coordinator's instruction):** `Features/Team/UpdateTeamConfig/UpdateTeamConfigEndpoint.cs` — this step wrote `await developerRepository.UpdateTeamConfigAsync(...).OrThrowNotFound(...)`, so the guard bound to the `Task` and a missing developer answered 500 (null dereference, CS8602 at line 25) instead of 404. Now `(await developerRepository.UpdateTeamConfigAsync(...)).OrThrowNotFound(...)`. Every other `OrThrowNotFound` site was re-checked: each wraps its `await` in parentheses or runs on an in-memory `FirstOrDefault`. Verify: `dotnet build` of `Fokus.API` (compile-fix check) succeeded with CS8602 gone; the fast step close was cut at 30 s (`reason: "timeout"`, 31.2 s) — Unfinished: whole unit run, covered by the complete run.

**Accept results:**
- `grep -rn --include=*.cs "SendNotFoundAsync" src` → 0.
- `grep -rn --include=*.cs "SendErrorsAsync\|AddError(" src/Services/Fokus/Fokus.API/Features` → 0.
- `grep -rn --include=*.cs "?? throw new NotFoundException" src/Services/Fokus/Fokus.API` → 0.
- `grep -n "Response.WriteAsync\|StatusCode = 401" src/Services/Fokus/Fokus.API/Auth/ActiveUserPreProcessor.cs` → 0.
- Step close `verify-run.js --profile fast` → pass, 5.3 s (the full suite is the close gate's).

## Step 4 — FastEndpoints 8 and the `Send.*` API
**Files:**
- `src/Directory.Packages.props` — FastEndpoints and FastEndpoints.Swagger 6.0.0 → 8.3.0, FluentValidation 11.11.0 → 12.2.0 (latest stable 8.x / 12.x on NuGet, 2026-10-13; FastEndpoints 8.3.0's nuspec requires FluentValidation ≥ 12.1.1).
- 55 `*.cs` files under `src/Services/Fokus/Fokus.API/Features/` — every receiver-less legacy call migrated by the skill's map:
  - 87 `SendOkAsync(` → `Send.OkAsync(` (including `Auth/GetMe/GetMeEndpoint.cs:16`, which `return`s the call — now `return Send.OkAsync(...)`);
  - 2 `SendNoContentAsync(ct)` → `Send.NoContentAsync(ct)` (`Auth/Logout/LogoutEndpoint.cs:13`, `Auth/RevokeInvitation/RevokeInvitationEndpoint.cs:17`);
  - 1 bare three-argument `SendAsync(…, 201, ct)` → `Send.ResponseAsync(…, 201, ct)` (`Auth/CreateInvitation/CreateInvitationEndpoint.cs:46`), same status.

**Skills:** framework-currency
**Decisions:**
- Names verified against the installed package, not memory: `D:\caches\nuget\fastendpoints\8.3.0\lib\net9.0\FastEndpoints.xml` declares `ResponseSender`2.OkAsync(`1,CancellationToken)`, `OkAsync(CancellationToken)`, `NoContentAsync(CancellationToken)` and `ResponseAsync(`1,Int32,CancellationToken)`.
- Migration done as one receiver-anchored substitution (`(^|[^.A-Za-z])Send…Async(`), so the two receiver-qualified calls (Stage 2 false positives) were never candidates: `Modules/Xray/Xray.GraphQL/GraphQLXrayClient.cs:296` (`httpClient.SendAsync`) and `Features/Sync/SyncSprints/SyncSprintsEndpoint.cs:46` (`hubContext.Clients.All.SendAsync`) are unchanged.
**Deviations:** None. No compile break from FastEndpoints 6 → 8 or FluentValidation 11 → 12 surfaced (the build compiled against FastEndpoints 8.3.0.0 and FluentValidation 12.2.0 per `Fokus.API/obj/project.assets.json` and the output `FastEndpoints.dll` file version), so there is nothing to list; `Blocks.FastEndpoints/DomainEventPublisher.cs`'s `PublishAsync(Mode.WaitForAll, ct)` compiles unchanged.

**Accept results:**
- `grep -rnE --include=*.cs "(^|[^.A-Za-z])Send[A-Za-z]*Async\(" src` → 0 (baseline 90 after Step 3).
- `grep -n 'Include="FastEndpoints" Version="8\.' src/Directory.Packages.props` → 1.
- Step close `verify-run.js --profile fast` → pass, 5.6 s (the full suite is the close gate's).

## Step 5 — One role vocabulary; admin writes behind one named policy
**Files:**
- `src/Services/Fokus/Fokus.Domain/Auth/AccessRole.cs` — NEW: enum `AccessRole { Admin = 1, Manager = 2 }` (the skill's sample numbering; the role column stays text).
- `src/Services/Fokus/Fokus.Domain/Auth/AccessRoles.cs` — NEW: `Admin`/`Manager` string constants via `nameof(AccessRole.…)`.
- `src/Services/Fokus/Fokus.API/Auth/AccessPolicies.cs` — NEW: `Admins = nameof(Admins)` (binding name, Q2).
- `src/Services/Fokus/Fokus.API/Auth/AuthConfiguration.cs` — bare `AddAuthorization()` → `AddAuthorizationBuilder().AddPolicy(AccessPolicies.Admins, policy => policy.RequireRole(AccessRoles.Admin))`.
- `src/Services/Fokus/Fokus.API/GlobalUsings.cs` — `global using Fokus.API.Auth;` so the 23 endpoints reach `AccessPolicies` without a per-file using.
- 23 endpoint files under `src/Services/Fokus/Fokus.API/Features/` — `[Authorize(Roles = "Admin")]` → `[Authorize(Policy = AccessPolicies.Admins)]`: `Auth/CreateInvitation`, `Auth/GetInvitations`, `Auth/GetUsers`, `Auth/RevokeInvitation`, `Auth/UpdateRole`, `Auth/UpdateStatus`, `Developers/SetCapacity`, `Settings/SaveAnalyticsTargets`, `Settings/SaveBoard`, `Settings/SaveBugRatioAlerts`, `Settings/SaveCycleTimeBoundaries`, `Settings/SaveDoneStatuses`, `Settings/SaveExcludedStatuses`, `Settings/SaveHealthConfig`, `Settings/SaveSyncConfig`, `Settings/SaveWorkflowStages`, `Settings/SaveXraySettings`, `Sprints/UpdateSprint`, `Sync/SyncBacklogSprints`, `Sync/SyncSprints`, `Team/UpdateTeamConfig`, `Xray/SyncXray`, `Xray/TestConnection` (each `{Name}Endpoint.cs`).
- Role literals → constants, values unchanged: `Fokus.Domain/Auth/AppUser.cs:9`, `Fokus.Domain/Auth/Invitation.cs:6`, `Fokus.Domain/Auth/Behaviors/AppUser.cs:13`; `Fokus.Persistence/Configurations/AppUserConfiguration.cs:18` and `InvitationConfiguration.cs:13` (`HasDefaultValue(AccessRoles.Manager)` — same value, no migration); `Fokus.Persistence/Repositories/AppUserRepository.cs:21`; validators `Features/Auth/CreateInvitation/CreateInvitationCommand.cs:31`, `Features/Auth/UpdateRole/UpdateRoleCommand.cs:22`; `Features/Auth/CreateInvitation/CreateInvitationEndpoint.cs:18` (default role); `Features/Auth/UpdateRole/UpdateRoleEndpoint.cs:17` and `Features/Auth/UpdateStatus/UpdateStatusEndpoint.cs:17` (target-user comparisons — business rules, kept as comparisons).
**Skills:** authorization-patterns
**Decisions:**
- Enum and constants in two files (`AccessRole.cs`, `AccessRoles.cs`), one type per file, as the skill's Phase 1 samples.
- `AccessPolicies` reached through a global using rather than 23 per-file usings; `Fokus.API/Auth` is already used by `Program.cs` and `DependencyInjection.cs`.
**Deviations:** None.

**Accept results:**
- `grep -rn --include=*.cs 'Roles = "' src` → 0.
- `grep -rn --include=*.cs '"Admin"\|"Manager"' src | grep -v "/Migrations/\|/obj/"` → 0 (baseline 34).
- `grep -rl 'Policy = AccessPolicies.Admins'` → the 23 files listed above.
- No file under `Fokus.Persistence/Migrations/` changed.
- Step close `verify-run.js --profile fast` → pass, 10.3 s.

## Step 6 — One type per endpoint file
**Files:** all under `src/Services/Fokus/Fokus.API/Features/`; each `{Op}Endpoint.cs` now holds only the endpoint class (its `using` lines stay with it — only `SyncXray` and `TestConnection` had any).
- Writes → NEW `{Op}Command.cs` holding the request (renamed `{Op}Request` → `{Op}Command`), its reply and its validator (renamed `{Op}CommandValidator`): `Settings/SaveAnalyticsTargets`, `Settings/SaveBoard`, `Settings/SaveBugRatioAlerts`, `Settings/SaveCycleTimeBoundaries`, `Settings/SaveDoneStatuses`, `Settings/SaveExcludedStatuses`, `Settings/SaveHealthConfig`, `Settings/SaveSyncConfig`, `Settings/SaveWorkflowStages`, `Settings/SaveXraySettings`, `Team/UpdateTeamConfig` (`UpdateTeamConfigRequest`/`UpdateTeamConfigValidator` → `UpdateTeamConfigCommand`/`UpdateTeamConfigCommandValidator`), `Xray/SyncXray`.
- `Xray/TestConnection/TestConnectionCommand.cs` — NEW, holds `TestConnectionResponse` (the endpoint takes no request; a POST action, so a command file).
- Reads → NEW `{Op}Query.cs` holding the reply: `Settings/GetCycleTimeBoundaries/GetCycleTimeBoundariesQuery.cs` (`GetCycleTimeBoundariesResponse`), `Team/GetTeamRoster/GetTeamRosterQuery.cs` (`TeamRosterResponse` → `GetTeamRosterResponse`).
- DTOs a reply uses → own file in the operation folder: `Team/GetTeamRoster/TeamDeveloperDto.cs`, `Developers/GetDevelopers/DeveloperDto.cs` (the `GetDevelopers` reply is `List<DeveloperDto>`, so there is no query file).
- `Sprints/UpdateSprint/UpdateSprintCommand.cs` — NEW, folds `UpdateSprintModels.cs` and `UpdateSprintValidator.cs` (both DELETED); `UpdateSprintRequest`/`UpdateSprintValidator` → `UpdateSprintCommand`/`UpdateSprintCommandValidator`; `UpdateSprintEndpoint.cs` updated to the new names.
- `docs/specs/F38-DotnetSkillAlignment/delivery/api-after.json` — NEW, the FastEndpoints document after the split (235,804 bytes; the size moves with the renamed schema names only).
**Skills:** extract-endpoint-types
**Decisions:**
- Touched folders take the skill's names (D8): `{Op}Command`, `{Op}CommandValidator`, `{Op}Response`. Untouched older folders keep `{Op}Request` (the skill's own note).
- Types file order: request, reply, validator — as `Auth/CreateInvitation/CreateInvitationCommand.cs`.
- `DeveloperDto` and `TeamDeveloperDto` keep their names (they are reply items, not the reply); nothing outside their folder uses either (`grep -rnw` under `src`). The client's TypeScript types are hand-written (`client/src/types`), so no client file changes.
- The split was mechanical (a scratch block-splitter over each file, then word-boundary renames), so method bodies and attributes are byte-identical apart from the renamed type names.
**Deviations:** None.

**First run on FastEndpoints 8.3 / FluentValidation 12** (API in Development, `--no-launch-profile`, port 5099, `Data Source=D:\temp\f38-after.db`, bounded by `timeout 120`, stopped after): start-up worked — all 15 migrations applied, "Application started"; endpoint registration worked (56 path+verb pairs, as before); the swagger document was served at `/swagger/v1/swagger.json`. The only `warn` is EF 10103 from `Program.cs`'s Xray block, the same as the Step 2 baseline. Step 7 is not done yet, so the settings row on this run still came from the inline `Program.cs` block; the capture is re-run after Step 7.

**Accept results:**
- Public-type count per `*Endpoint.cs` under `Features/` → every file 1, except `Auth/Login/LoginEndpoint.cs` → 0 (excepted).
- `ls Features/Sprints/UpdateSprint/` → `UpdateSprintCommand.cs`, `UpdateSprintEndpoint.cs`.
- Wire names (scratch diff over both documents: each path+verb, its request-body properties and its 2xx reply properties resolved through `$ref` including nested schemas and array items, keyed by location; plus its parameters): path+verb set before 56 / after 56; request-body property paths 55 / 55; success-reply property paths 1260 / 1260; parameters 51 / 51; **diff lines: 0**.
- Step close `verify-run.js --profile fast` → pass, 12.4 s.

## Step 7 — Start-up work as start-up lines; a read that only reads
**Files:**
- `src/Services/Fokus/Fokus.Persistence/MigrationExtensions.cs` — NEW: `IHost.MigrateFokusDatabase()` — creates the SQLite file's folder (read with `SqliteConnectionStringBuilder`, as the old inline block did), then `Database.Migrate()`; one scope.
- `src/Services/Fokus/Fokus.Persistence/SeedExtensions.cs` — NEW: `IHost.SeedDefaultAppSettingsAsync()` — adds `AppSettings.CreateDefault()` when the table is empty, every environment (D4); an existing row is never touched.
- `src/Services/Fokus/Fokus.API/Features/Xray/XrayCredentialsStartupExtensions.cs` — NEW: `IHost.ApplyConfiguredXrayCredentialsAsync()` — the old inline Xray block moved as is (reads `XrayOptions`; when both id and secret are set, enables Xray on the settings row and copies them). An API concern, so it lives in the API's Xray area beside `XrayIssueSyncService.cs`.
- `src/Services/Fokus/Fokus.API/Program.cs` — the inline scope (old lines 17–44) → three start-up lines before the middleware: `app.MigrateFokusDatabase();`, `await app.SeedDefaultAppSettingsAsync();`, `await app.ApplyConfiguredXrayCredentialsAsync();`. The now-unused `Microsoft.EntityFrameworkCore`, `Microsoft.Extensions.Options` and `Xray.GraphQL` usings removed; `Fokus.API.Features.Xray` added. No `BackgroundService` (D2).
- `src/Services/Fokus/Fokus.Persistence/Repositories/AppSettingsRepository.cs` — `GetAsync` only reads: tracked `SingleOrDefaultAsync(s => s.Id == 1)` `?? throw new InvalidOperationException("The settings row is missing; start-up seeds it.")` (mapper → 500). `SaveAsync` unchanged.
- `src/BuildingBlocks/Blocks.EntityFrameworkCore/EntityConfigurations/EntityConfiguration.cs` — both `builder.SeedFromJsonFile();` calls and the `Blocks.EntityFrameworkCore.Extensions` using removed (D3).
- `src/BuildingBlocks/Blocks.EntityFrameworkCore/Extensions/EntityTypeBuilderExtensions.cs` — DELETED (the silent seed reader; its only callers were the two lines above).
- `docs/specs/F38-DotnetSkillAlignment/delivery/api-after.json` — re-captured after this step (unchanged size, 235,804 bytes).
**Skills:** add-hosted-service, persistence-patterns, service-registration
**Decisions:**
- The extensions take `IHost` and return it, as the skills' `Migrate<TContext>(this IHost)` / `SeedTestData(this IHost)` samples; `Fokus.Persistence` resolves `Microsoft.Extensions.Hosting` transitively (it built with no project-file change).
- `MigrateFokusDatabase` is service-specific (folder creation reads SQLite's `Data Source`) rather than the generic `Migrate<TContext>`: there is one context, and the folder step is SQLite-specific.
- The Xray line keeps `FirstAsync()` without `OrderBy` as it was ("behaviour unchanged"); its EF 10103 warning is the same as the baseline (Carry-Over).
- service-registration § Program.cs Structure: the start-up lines sit in the InitData position (after `Build()`, before the middleware).
**Deviations:** None from the plan.
**Unfinished:** whole unit run — cut at 30 s (the step close's `verify-run.js --profile fast` record reads `reason: "timeout"` at 31.1 s, inside the build, after `Fokus.Persistence` compiled). The API build was then proven by the start-up check below (`dotnet build` of `Fokus.API`, succeeded, 0 errors); the tests were not run at this step — the complete run covers them.

**Start-up check after Step 7** (Development, `--no-launch-profile`, port 5099, `Data Source=D:\temp\f38-after.db` deleted first, bounded by `timeout`, stopped after):
- Fresh file: all 15 migrations applied; the settings row was inserted by the new start-up line (`INSERT INTO "AppSettings"` in the log); the Xray line then updated it (credentials are configured on this machine); "Application started"; `GET /api/settings` unauthenticated → 401 (pipeline and auth intact). Only `warn`: EF 10103, as the baseline.
- Second start on the same file: 0 migrations, 0 settings inserts, 0 updates, started — an existing row is kept.
- Wire-name diff re-run on the new `api-after.json`: path+verb 56 / 56; request-body property paths 55 / 55; success-reply property paths 1260 / 1260; parameters 51 / 51; diff lines 0.

**Noted, not caused in this run (does not block Step 7):** `Features/Team/UpdateTeamConfig/UpdateTeamConfigEndpoint.cs:11-19` — Step 3 wrote `await developerRepository.UpdateTeamConfigAsync(...).OrThrowNotFound(...)`, so `.OrThrowNotFound` binds to the `Task`, never to the awaited `Developer?`; a missing developer now hits `developer.Id` (CS8602 at line 25 in the build) and answers 500 instead of 404. Every other `OrThrowNotFound` site was checked: each wraps the `await` in parentheses or calls it on an in-memory `FirstOrDefault`. Taken into the first-round review (Carry-Over).

**Accept results:**
- `grep -nE "Migrate\(|AppSettings|SaveChanges|GetRequiredService" Fokus.API/Program.cs` → one hit, `await app.SeedDefaultAppSettingsAsync();` — an extension call; no inline database work.
- `grep -n "Add(\|SaveChangesAsync" Fokus.Persistence/Repositories/AppSettingsRepository.cs` → lines 15 and 26, both inside `SaveAsync`.
- `grep -rn --include=*.cs "SeedFromJsonFile" src` → 0.
- `Task.Run(` and discarded `_ = ` greps (the skill's Verify) → 0.
- Fresh database gets its row → shown above; the operator's Step 10 item 2 repeats it.

## Step 8 — The service's CLAUDE.md records every axis
**Files:**
- `src/Services/Fokus/CLAUDE.md` — header follows the skill's template: **Port** → `http://localhost:5000` (development profile, `launchSettings.json:8`), no compose index because the service is not containerised (replaces "TBD (service index …)"); **Docker:** N added. New `## Modules` section (Jira: `Jira.Contracts` + `Jira.RestApi`; Xray: `Xray.Contracts` + `Xray.GraphQL`). `## Existing features` — the six-route list → the eight feature areas under `Fokus.API/Features/` with what each holds (D9; checked against `ls Features/*/`). Key patterns: the AppSettings line names the `SeedDefaultAppSettingsAsync()` start-up line and the read-only `GetAsync`; one line each added for errors (lookups throw; the middleware replies), access roles and the `AccessPolicies.Admins` policy, and the `Send.*` API. Endpoint pattern: validators now said to live with the request in `{Operation}Command.cs`/`{Operation}Query.cs` (Step 6 made "sibling classes" imprecise). gRPC, integration events, Application layer, domain-event and Jira sections unchanged.
**Skills:** create-service-claude-md — Read channel: the skill is `disable-model-invocation: true` (architect-only), so its local `.claude/skills/create-service-claude-md/workflows/WriteClaudeMd.md` was read directly before the edit, as the plan and dispatch direct; no Skill-tool call was attempted.
**Decisions:**
- Existing service-specific sections (Domain event infrastructure, Key patterns, Jira integration) are kept beside the template's sections; the template's section order is kept for the sections it names (`## Modules` sits before gRPC).
- "Thin domain (v1)" is left as it was: no Step 3–7 change touches it (see Carry-Over).
**Deviations:** None.

**Accept results:**
- `grep -n "^\*\*Docker:\*\*" src/Services/Fokus/CLAUDE.md` → 1 (line 6).
- `grep -n "^## Modules" src/Services/Fokus/CLAUDE.md` → 1.
- `grep -n "TBD (service index" src/Services/Fokus/CLAUDE.md` → 0.
- No code changed in this step, so no step close was run (doc-only).

## Step 9 — Sweep the echoes
**Files:**
- `docs/conventions/csharp.md` — line 13 → "A handler never writes an error reply (`AddError`, `Send.ErrorsAsync`); FluentValidation validators report input errors, and a handler throws."; line 72 → "**A required lookup that finds nothing throws `NotFoundException`** (the `error-handling` skill); an optional lookup returns null and its caller decides. Input validation stays in validators." (both the plan's after-text verbatim).
- `.claude/skills/create-feature/workflows/EndpointFastEndpoints.md` — line 28 `[Authorize(Roles = Role.{RequiredRole})]` → `[Authorize(Policy = AccessPolicies.{PolicyName})]`; line 40 `SendOkAsync` → `Send.OkAsync`; line 74 `// or Roles(...)` → `// or Policies(AccessPolicies.{PolicyName})`; line 95 → "Never write a `catch` that answers through `Send.*` (a status code, an unauthorized reply) in an endpoint."; line 100 → `Send.OkAsync()` for success replies, with `Send.NoContentAsync()`/`Send.ResponseAsync(reply, status)` for the others.
- `src/Modules/Xray/CLAUDE.md:18` — "Credentials are NOT read from appsettings.json …" → an optional `Xray` section (`XrayOptions`) is copied into the settings row by the Fokus start-up line `ApplyConfiguredXrayCredentialsAsync()` when both values are set; the settings row is the runtime source; credentials are passed per call to `IXrayClient`.
- `docs/architecture/v1.md:105` — "Global exception handler via FastEndpoints" → "`GlobalExceptionMiddleware` in `Blocks.AspNetCore`, which maps the `Blocks.Exceptions` family to replies".
- `docs/kb/glossary.md` — NEW entry **Access Role** under Core Entities, after Sub-Team: Admin (configures settings, syncs, manages users and invitations) or Manager (reads; changes nothing — the only non-admin write is sign-out), code `AccessRole`/`AccessRoles`, stored in `AppUser.Role`/`Invitation.Role`, distinct from `Developer.Role`. `docs/kb/index.md` already links the glossary; no new entry file, so no index change.
- `docs/conventions/ef-core.md:28` — other echo: "Use runtime seeding (`SeedTestData`) instead" → required data by a start-up line in every environment (`SeedDefaultAppSettingsAsync()`), test data by a development-only `SeedTestData` step (Step 7 made the settings row a start-up line).
**Skills:** None — plan Skill Mapping row 9 `(none)` (doc sweep), TDD no.
**Decisions:**
- Workflow line 74 uses FastEndpoints' `Policies(...)` in `Configure()` — the counterpart of the attribute form on line 28.
- Kept hits that teach the new rule by naming the old call: `authorization-patterns/SKILL.md:50` (`Roles(params string[])`, why the vocabulary is `AccessRoles`), `error-handling/SKILL.md:40` and `csharp.md:13` (`AddError`/`Send.ErrorsAsync` as the forbidden handler forms). None matches the accept regex.
**Deviations:** `ef-core.md:28` is an extra file the plan's list does not name; it is a live echo the plan's "Other echoes" re-grep asks for ("seeded"/seeding wording).

**Other-echo re-grep** (`Send*Async`, `Roles = `, `Roles(`, `Version="` in prose, `SeedFromJsonFile`, "seeded on startup", `AddError`, `SendErrors`, `Authorize(Roles`, "seed", "via FastEndpoints" over `.claude`, `docs/conventions`, `docs/architecture`, `docs/kb`, `CLAUDE.md`, `src/**/CLAUDE.md`; `docs/specs/` excluded): live hits fixed as above; remaining hits are the three explanatory mentions named under Decisions, `framework-currency` (on purpose), and generic seeding text in `create-service` workflows (scaffold guidance for new services, not a Fokus fact).

**Accept results:**
- `git grep -n --untracked -E "(^|[^.A-Za-z])Send[A-Za-z]*Async|Roles = |SeedFromJsonFile" -- .claude docs/conventions docs/architecture docs/kb CLAUDE.md "src/**/CLAUDE.md"` → 0 outside `.claude/skills/framework-currency/`.
- `grep -n "Don't use exceptions for expected control flow" docs/conventions/csharp.md` → 0.
- `grep -n "NOT read from appsettings" src/Modules/Xray/CLAUDE.md` → 0.
- `grep -n "via FastEndpoints" docs/architecture/v1.md` → 0.
- Doc-only step: no code changed, no step close run.

## Fix Round 1
**Files:**
- `.claude/skills/persistence-patterns/SKILL.md` — item 1 fixed: "Table and column names" now says EF Core's defaults — a table takes its `DbSet` property's name on `FokusDbContext` (`Sprints`, `Tickets`, `AppUsers`), a column its property's name. Item 5 fixed: the six "the plugin form" mentions (lines 18, 85, 89, 96, 141, 161) → "the general form".
- `.claude/skills/authorization-patterns/SKILL.md` — item 5 fixed: line 41 "which plugin rules it departs from" → "which general rules it departs from" (the only plugin-naming phrase in that file).
- `.claude/skills/add-hosted-service/SKILL.md` — item 2 fixed: § Assumes now says Jira validates its required options at start-up (`ValidateDataAnnotations().ValidateOnStart()`, `Jira.RestApi/DependencyInjection.cs`) while Xray only binds its optional options with no start-up validation (`Xray.GraphQL/DependencyInjection.cs`).
- `src/Services/Fokus/CLAUDE.md` — item 3 fixed: the access-roles line names the two anonymous sign-in endpoints, `Auth/Login` (`/auth/login`, `.AllowAnonymous()`) and `Auth/ValidateInvitation` (`/api/invitations/{token}/validate`, `[AllowAnonymous]`). Item 4 fixed: "Thin domain (v1) … No behavior methods" replaced by a "Domain split" line — state in `{Aggregate}.cs`, behaviour in `Behaviors/{Aggregate}.cs`; most behaviour is `FromJira`/`FromXray` factory mapping, the Auth aggregates carry real behaviour (`AppUser.CreateBootstrapAdmin`/`CreateFromInvitation`/`RecordLogin`, `Invitation.Create`/`Accept`/`Revoke`) — checked against `Fokus.Domain/*/Behaviors/`.
- `.claude/skills/create-feature/CHANGELOG.md` — item 6 fixed: an `[Unreleased]` line for the Step 9 edit to `workflows/EndpointFastEndpoints.md` (named policy in place of `Roles`, FastEndpoints 8 `Send.*` replies).
**Skills:** None — doc-only fix list, no plan step's skill re-applies.
**Decisions:** Item 5's "the general form" wording chosen over "the skill's general pattern" (both offered by the fix list) — shorter, and reads the same in every one of the seven sentences.
**Deviations:** None — all six items fixed. Doc-only round: no code changed, so no step close was run (as Steps 8–9). Accept greps: "plugin form"/"plugin rules" in both skills → 0; "Thin domain" in the service CLAUDE.md → 0; "F38" in the create-feature changelog → 1.

## Key Decisions
- Step 3: required lookups use `(await …).OrThrowNotFound(message)` with one message shape per entity ("{Entity} {id} was not found."); rule-decided 404s throw directly; optional lookups keep their 200 replies.
- Step 4: `SendErrorsAsync`/`SendUnauthorizedAsync` map to "removed", not to `Send.*` — no endpoint writes an error reply.
- Step 5: `AccessRole`/`AccessRoles` in two files; `AccessPolicies` reached through a global using.
- Step 6: touched folders take the skill names (`{Op}Command`, `{Op}CommandValidator`, `{Op}Response`); reply-item DTOs keep their names in files of their own; the split was mechanical, verified by line diff against HEAD.
- Step 7: start-up extensions take and return `IHost`; the Xray line moved unchanged (keeps `FirstAsync()`); no `BackgroundService`.
- Step 9: the create-feature workflow shows the policy in both forms (attribute and `Policies(...)` in `Configure()`).

## Carry-Over Findings
| Title | Severity | For | Evidence | Note |
|-------|----------|-----|----------|------|
| `UpdateTeamConfig` missing developer answers 500, not 404 | high | reviewer | `UpdateTeamConfigEndpoint.cs:11-19`: `.OrThrowNotFound` applied to the `Task`, not the awaited value (Step 3); CS8602 at line 25 | **fixed** after Step 7 (await parenthesised; Step 3 block, Fix line); was breaking Must-NOT-Change "missing entity → 404" |
| Service CLAUDE.md "Thin domain (v1)" line is stale | low | architect | `src/Services/Fokus/CLAUDE.md` Key patterns says "No behavior methods … Behaviors folder will be added"; `Fokus.Domain/Auth/Behaviors/AppUser.cs` exists | outside Step 8's "what Steps 3–7 make false"; left for F36's architecture/conventions refresh |
| Runtime under FastEndpoints 8 not yet exercised | low | reviewer | Step 4 closed on build + fast tests only; the API was not started after the 6 → 8 move (the dispatch allows API starts for Step 2 and the plan names the next one in Step 6) | **resolved in Step 6/7**: start-up, endpoint registration (56 routes) and the swagger document worked on FastEndpoints 8.3; signed-in paths (pre-processor, validators, the admin policy) stay with Step 10 |
| Formerly empty 404 replies now carry a body | low | architect | Step 3: the 6.0 `SendNotFoundAsync` sent an empty 404; the 13 converted lookups now answer the middleware envelope with a new message (e.g. "Invitation {id} was not found."), and the mapper logs them at Error level | statuses unchanged and Step 3 sends every error through the middleware, but plan § Must-NOT-Change lists only the 400 and 401 body changes as approved — name these in the table or in summary.md Notes |

## Skills Used
| Step | Skill(s) invoked | Notes |
|------|------------------|-------|
| 1 | None | plan Skill Mapping row 1 `(none)`, TDD no |
| 2 | central-package-management | local copy, invoked via the Skill tool before the edit; TDD no |
| 3 | error-handling | local copy, invoked via the Skill tool before the edit; TDD no |
| 4 | framework-currency | local copy, invoked via the Skill tool before the edit; TDD no |
| 5 | authorization-patterns | local copy, invoked via the Skill tool before the edit; TDD no |
| 6 | extract-endpoint-types | local copy, invoked via the Skill tool before the edit; TDD no |
| 7 | add-hosted-service, persistence-patterns, service-registration | local copies, all three invoked via the Skill tool before the edit; TDD no |
| 8 | create-service-claude-md | **Read-channel deviation:** not model-invocable (`disable-model-invocation: true`); its local `workflows/WriteClaudeMd.md` read before the edit, per plan Step 8 and the dispatch; TDD no |
| 9 | None | plan Skill Mapping row 9 `(none)` — doc sweep; TDD no |

## KB Changes
| Entry | Action | What changed |
|-------|--------|-------------|
| `docs/kb/glossary.md` | UPDATE | New **Access Role** entry (Admin / Manager, `AccessRole`/`AccessRoles`), distinct from a developer's team role (Step 9). `docs/kb/index.md` already links the glossary. |

## Unfinished Tests
- Step 7: whole unit run — cut at 30 s.
- Step 3 fix (after Step 7, `UpdateTeamConfigEndpoint.cs`): whole unit run — cut at 30 s.
- Both are covered by the self-review fold's step close, which passed on the final tree (below), and by the complete run at handover.

## Deviations from Plan
- Step 3 (fixed after Step 7, on the coordinator's instruction): the `UpdateTeamConfig` lookup was guarded on the `Task`, not the awaited value; parenthesised. Recorded on the Step 3 block's Fix line.
- Step 8: `create-service-claude-md` consumed through its local `workflows/WriteClaudeMd.md` (Read channel) — the skill is not model-invocable; plan-sanctioned.
- Step 9: `docs/conventions/ef-core.md:28` edited as an "other echo" the plan's file list does not name.
- Self-review fold: `using Fokus.API.Auth;` removed from `Program.cs` and `DependencyInjection.cs` (made redundant by Step 5's global using); `ef-core.md:28` reworded again so it names no `SeedTestData` method the repo lacks.

## Self-Review
**Verdict: COMMENT** — no CRITICAL or HIGH open; two LOW items folded; two LOW items carried to the architect (Carry-Over). The one HIGH found during the build (the `UpdateTeamConfig` 404 → 500) was fixed before Step 8.

**Channel 1 — in-context self-review** against the `review-format` Step 2 checklist, over the whole working diff (Steps 1–9):
- Plan conformance: each step's accept re-run and recorded in its block; all hold on the final tree.
- Relocation moves checked mechanically, not by reading: for `Settings/SaveHealthConfig`, `Xray/SyncXray`, `Team/GetTeamRoster`, `Developers/GetDevelopers` the HEAD endpoint file (with the planned renames applied) and the new folder's files hold the same non-blank lines — 0 differing; `Team/UpdateTeamConfig` and `Settings/SaveCycleTimeBoundaries` differ only by their Step 3 lines.
- Must-NOT-Change: wire names (diff 0, Step 7 re-capture); the 23 gated endpoints; no file under `Migrations/` changed; the sync services' `catch` blocks, `GraphQLXrayClient.cs` and the SignalR `SendAsync` line unchanged in the diff (`git diff HEAD` of the three files → 0 lines; `SyncSprintsEndpoint.cs` diff touches neither line 46 nor 51).
- `OrThrowNotFound` sites: every one wraps its `await` or runs on an in-memory `FirstOrDefault` (after the Step 3 fix).
- Policy enforcement: FastEndpoints 8.3's `FastEndpoints.dll` references `AuthorizeAttribute.get_Policy` beside `get_Roles` and `get_AuthenticationSchemes`, i.e. it reads the attribute's policy; the manager-gets-403 behaviour itself is the operator's Step 10 item 4.
- Comment noise: the diff adds no comment line in `src/`; the five comments in new files are pre-existing validator comments moved verbatim.
- Security: no secrets added; the Xray credentials path is unchanged.

**Channel 2 — fresh-context checker:** one read-only `general-purpose` helper on `sonnet`, framed as research (no verdict, no files), over `git diff HEAD` plus untracked files. It confirmed: the 23→23 policy mapping, every `OrThrowNotFound` site, the property lists of the 18 moved/renamed types against HEAD, validators bound to the renamed types, `Program.cs` start-up order, receiver-qualified calls and `catch` blocks unchanged, no comments/TODOs added, every skill-copy path and § pointer resolving, the 42-skill count. Its four observations, dispositioned:
1. `Program.cs` keeps `using Fokus.API.Auth;` that `GlobalUsings.cs` now supplies — **real (LOW), folded**: removed there and in `DependencyInjection.cs`.
2. `docs/conventions/ef-core.md:28` names a `SeedTestData` step no code has — **real (LOW), folded**: reworded to the `persistence-patterns` § Seed Data pointer.
3. Formerly empty 404 replies now carry the middleware body and a new message, and the mapper logs them at Error level — **real but plan-sanctioned (LOW)**: statuses unchanged; Step 3 routes every error through the middleware and asks for a message where none existed; the Must-NOT-Change table just doesn't list it → Carry-Over for the architect.
4. 404/401 logged at `LogError` — **dismissed**: pre-existing mapper behaviour (`GlobalExceptionMiddleware`, documented in the `error-handling` copy), not changed by this feature.

**Evidence**
| Check | Result | Command | Output |
|-------|--------|---------|--------|
| Step close after the fold (final tree) | pass | `verify-run.js --profile fast` | pass, 4.3 s |
| Wire names | pass | scratch `$ref`-resolving diff of `api-before.json` / `api-after.json` | 56/56 routes, 55/55 request, 1260/1260 reply property paths; 0 diff lines |
| Start-up on FastEndpoints 8.3 | pass | API in Development on `D:\temp\f38-after.db` | 15 migrations, settings row inserted by the start-up line, started; second start kept the row |
| Full suite | not run | close gate's | — |

*Status: COMPLETE — developer, 2026-10-14*
