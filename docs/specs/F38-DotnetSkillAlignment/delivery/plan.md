# F38 — Dotnet Skill Alignment

**Feature Spec:** None — plan-only definition (owner, 2026-10-13)
**Model:** claude-opus-5-5
**Build slices:** 1 · 2–4 · 5–9; why: step 1 (bringing in eight skills) is prose work with its own reading load and feeds every later step; steps 2–4 are one chain (central versions → errors thrown → FastEndpoints 8 and `Send.*`) that must stay together because step 4 builds only once steps 2–3 hold; steps 5–9 are independent per-skill passes plus the docs sweep
**Lane:** full — owner's level
**Rule list:** not applicable — plan-only definition

## Context

The owner wants Fokus to ship to students as a repo built with agents and aligned with the .NET skills it teaches. `docs/specs/F36-PublicReleaseCleanup/delivery/nexus-dotnet-skill-fit.md` rated 35 plugin skills against Fokus. The owner picked the eight rated 6+ with effort S or S–M (backlog row F38, `docs/backlog.md`).

- **Owner rulings (2026-10-13):** name "Dotnet Skill Alignment". **Step 1 copies the skills into `.claude/skills/`; every later step applies the local copy.** Plan-only definition, full checks. Branch off F37's branch; build after F37 merges, because F37 edits the same endpoint files, `Program.cs` (F37 Step 9) and the analytics not-found paths (F37 Step 6). F38 also merges before F36 Phase 4 (architecture map and conventions refresh) and Phase 5 (public history), so both describe the aligned code.
- **Version scope — owner ruling "middle" (2026-10-13, asked in session):**
  - stable .NET 10.0.x for every preview package;
  - floating versions pinned;
  - FastEndpoints → 8.3 (it brings FluentValidation 12);
  - Refit, Microsoft.NET.Test.Sdk, xunit.runner.visualstudio and NSubstitute stay on their current major, each pinned with a one-line reason.
  - Why FastEndpoints moves: plan-time check of the installed package: 6.0.0's `FastEndpoints.xml` has only the legacy `Endpoint.SendOkAsync` family and no `Send` property. In 8.3.0, `P:FastEndpoints.Endpoint`2.Send` and `ResponseSender`2.OkAsync`/`ErrorsAsync`/`NoContentAsync`/`ResponseAsync` exist, and the legacy instance methods are gone (0 hits for `Endpoint`2.SendOkAsync`). The `Send.*` move is impossible without the upgrade, and the upgrade breaks every legacy call.
- **Same-topic grep:** no plan covers these skills. F37 (`docs/specs/F37-CodeQualityCleanup/delivery/plan.md`) converts the 9 analytics "Sprint not found" 400s to `NotFoundException` (its Step 6) and plumbs the environment through `Program.cs` (its Step 9). F38 takes every remaining case on F37's merged tree.
- **The fit file was a summary, not the skills.** The skills' own text corrects it in two places. Each correction is a `## Decisions` row and was disclosed to the owner with this plan:
  - **#7 is not a background task.** `add-hosted-service` § Start-up Work says work the service must finish before it takes requests (migration, seeding) is a line in the start-up file. A `BackgroundService` is only for work the service may take requests during.
  - **#10's "SeedFromJsonFile throws" would break every table.** `EntityConfiguration` calls `SeedFromJsonFile()` on every configuration (`Blocks.EntityFrameworkCore/EntityConfigurations/EntityConfiguration.cs:22,40`), and no `Data/Master/*.json` exists. `persistence-patterns` § Seed Data has a configuration seed explicitly, with a reader that fails on a missing file.
- **Plan-time counts** were taken on F37's working tree while F37 was being built, so they move. Every accept below is a zero-hit or whole-set grep, never a count to match:
  - legacy receiver-less `Send*Async(` calls: 111 in 55 files;
  - `SendNotFoundAsync`: 12 files, 15 calls;
  - `Roles = "`: 23 lines;
  - `"Admin"`/`"Manager"` literals outside migrations: 34 lines;
  - endpoint files that declare more than one type: 16;
  - `PackageReference` lines with a version: 20, in 9 project files.
- **Test harness:** `Fokus.Tests` (xunit 2, FluentAssertions, NSubstitute) has service-level tests only, with no endpoint or repository harness. The owner ruled no new tests for this release. Every step is behaviour-preserving, pinned by the full suite, by greps, and by the operator check (Step 10).
- **Service:** Fokus (`src/Services/Fokus/*`), the two modules' project files (`src/Modules/*`, versions only) and `src/BuildingBlocks/*`. No schema change, no client change.

## Scope

**In scope:** the nine developer steps below and the operator check (Step 10).

**Out of scope:**
- every skill rated under 6, or rated 6+ but with M effort (`domain-patterns` enums for roles and statuses, `add-typed-client`, `create-domain-event-handler`);
- turning `AppUser.Role` / `Invitation.Role` from `string` into an enum (that is `domain-patterns`; Step 5 adds the vocabulary only);
- rebuilding the error family to the plugin's `DomainException` tree (Decision D6);
- the resource layer of `authorization-patterns` (Decision D5);
- `FastEndpoints.Swagger` beside Scalar (a separate question; it is only bumped with FastEndpoints);
- every front-end change.

**Handed to F36's known-open-work page:** F38 records these in `summary.md` Notes for F36 to carry. F38 writes no `docs/known-open-work.md`, which is F36's deliverable.
- Owner ruling "middle": moving Refit 8 → 16, Microsoft.NET.Test.Sdk 17 → 18, xunit.runner.visualstudio 2 → 4 and NSubstitute 5 → 6.
- Decision D12: the validators' per-rule message literals → the `error-handling` § Validation Messages helpers.
- Decision D13: an identity-stamp pre-processor (`authorization-patterns` Phase 4). Today `CreateInvitationEndpoint` and `GetMeEndpoint` read the caller from the claims.

## Skill Mapping

| Step | Skill | Disposition | TDD | Feature-Specific Inputs | Gap? |
|------|-------|-------------|-----|------------------------|------|
| 1 | (none) | — | no | 8 skills: 3 new, 5 merged; self-contained local copies | gap: no skill for bringing a plugin skill into a repo as a self-contained local copy (copy, merge, keep local edits) |
| 2 | central-package-management | Follow | no | 17 packages, 9 project files, the "middle" version set | — |
| 3 | error-handling | Follow | no | the `SendNotFoundAsync` files and `?? throw` sites left after F37; the two rule-based 404s; handler-side `AddError` in `SaveCycleTimeBoundaries`; the pre-processor's hand-written 401 | — |
| 4 | framework-currency | Follow | no | FastEndpoints 6.0.0 → 8.3.x, FluentValidation 11 → 12, every legacy `Send*Async` | — |
| 5 | authorization-patterns | Follow | no | roles Admin, Manager; one policy for admin writes | — |
| 6 | extract-endpoint-types | Follow | no | the 16 endpoint files listed in the step | — |
| 7 | add-hosted-service, persistence-patterns, service-registration | Follow | no | `Program.cs` start-up lines; `AppSettingsRepository.GetAsync`; the seed hook | — |
| 8 | create-service-claude-md | Follow | no | the axis values captured in the step | — |
| 9 | (none) | — | no | convention, skill, module, architecture-doc and glossary echoes of steps 2–8 | — (doc sweep; `kb-maintenance` rules apply to the glossary line) |
| 10 | (none) | — | no | operator check | — |

TDD is `no` throughout. Every step is a behaviour-preserving refactor or a doc change, under the owner's no-new-tests ruling. The existing suite and the operator check pin behaviour (§ Must-NOT-Change).

## Domain Model Changes

None to entities. Step 5 adds the access-role vocabulary (enum `AccessRole` and its constants `AccessRoles`) to `Fokus.Domain/Auth/`. `AppUser.Role` and `Invitation.Role` stay `string`.

## Data Model Changes

None, and no migration. Step 2 must leave the model unchanged under stable EF Core 10. The migration snapshot was written by `10.0.0-preview.4.25258.110` (`FokusDbContextModelSnapshot.cs:18`). EF Core 9+ refuses `Migrate()` when the model has pending changes. Step 2 proves it at once by starting the API on a temporary database file; `dotnet-ef` is not installed (plan-time `dotnet ef --version` → not found). The operator check (Step 10) repeats it on an existing database file.

## Implementation Steps

### Step 1 — Bring the eight skills into `.claude/skills/` as self-contained copies

Source: `D:/src/claude-plugins/nexus/plugins/nexus-dotnet/skills/{name}/`, the whole folder (SKILL.md plus `workflows/`/`references/` where present). Target: `.claude/skills/{name}/`.

- **New (copy):** `central-package-management`, `framework-currency`, `add-hosted-service`.
- **Merge (plugin text as the base, Fokus facts kept):**
  - **The copies describe the target state** the plan decides (Decision D14, questions.md Q1). A fact Steps 2–7 make true is logged `target — lands in Step N`.
  - **Every existing repo fact in a merged copy is re-verified against the live file before it is kept.** A fact that does not hold is dropped, never carried. The lists below are what to look for, not a mandate.
  - **`error-handling`** — say which form this repo uses wherever the plugin's form differs:
    - the error family is `Blocks.Exceptions` (`HttpException` → `BadRequestException`, `NotFoundException`, `UnauthorizedException`, `ConflictException`, `ForbiddenException`, `BadGatewayException`) plus `DomainException` in `Blocks.Domain`, not the plugin's `DomainException`-rooted tree (Decision D6);
    - the guards are `Blocks.Core/Guard.cs` and `GuardExtensions.cs`, whose `OrThrowNotFound` takes a **message**, not an id — samples use the real signature;
    - the lookup is `Blocks.EntityFrameworkCore/Extensions/RepositoryExtensions.cs` (`FindByIdOrThrowAsync`);
    - the mapper is `Blocks.AspNetCore/Middlewares/GlobalExceptionMiddleware.cs`: its status table and its camel-case envelope (`statusCode`, `message`, `traceId`, `details`) are re-read from the live file, and it logs every mapped error, not only 500s.
    - The local copy also states: the batch sync paths catch per item and report the failure in the reply. The SignalR broadcast after a successful sync (`SyncSprintsEndpoint.cs:51`) is best-effort: its failure is logged and the sync reply stands (Decision D11); and the validators' per-rule literals predate § Validation Messages, so the move is open work (Decision D12).
  - **`authorization-patterns`** — add a section saying what this repo uses. The plugin rules this repo departs from are named and carved out in that section:
    - Google cookie sign-in, so no JWT phase;
    - the access-role vocabulary;
    - one admin policy that checks the role only. The binding rule "a bare role check never gates a write" and Phase 2's "a role-only admin write is not this case" do not apply: Fokus has no per-resource ownership (Decision D5).
    - The user and invitation lists are reads that stay admin-only. The "read models take authentication only" rule does not cover them, because they expose other people's accounts.
  - **`extract-endpoint-types`** — keep the local "Reference Examples", re-pointed at folders that exist after Step 6.
  - **`persistence-patterns`** — say which form this repo uses:
    - SQLite as the provider (the plugin assumes SQL Server or PostgreSQL);
    - the live base `RepositoryBase<TContext,TEntity,TKey>`, with a public `Query()` and `FindByIdAsync`, and the extension `FindByIdOrThrowAsync` (the plugin's base has a protected `Query()` and `GetByIdOrThrowAsync`);
    - `EntityConfiguration` with its `HasGeneratedId` override, and `AuditedEntityConfiguration` with `HasConcurrencyToken`;
    - explicit repository registration;
    - seeding as it stands after Step 7.
    - Plan-time check: `ApplicationDbContext` is an empty base with no cache helpers, and `AuditedEntityConfiguration` has no `DefaultDateSql`. The old local copy's `ref`-parameter warning, `HasDefaultValueSql` note and `IRepository.cs`/`Repository.cs` paths are therefore false and are dropped.
  - **`create-service-claude-md`** — the plugin copy is a superset of the local one; take it whole.
- **Self-contained:** for each of the 8 folders:
  - no `nexus-gen` markers;
  - no plugin path or `{plugin}` token;
  - no `project-lists.md` pointer: rewrite the "Look First" block to name Fokus's shared places (`src/BuildingBlocks/Blocks.*`, the module contracts, `src/Directory.Packages.props`);
  - no mention of a skill absent from `.claude/skills/`: reword it plainly or drop it. List each such reference and what you did with it in `implementation.md`.
- **`.claude/README.md`:** the Information Layers row reads `(38 skills)`. Write the count of folders that hold a `SKILL.md` (`ls .claude/skills/*/SKILL.md | wc -l`; plan-time 39, so 42 after this step). `.claude/skills/docs/` holds session logs, not a skill.
- **Accept:**
  - `grep -rniE "nexus|\{plugin\}|claude-plugins|project-lists\.md" .claude/skills/{central-package-management,framework-currency,add-hosted-service,error-handling,authorization-patterns,extract-endpoint-types,persistence-patterns,create-service-claude-md}` → 0;
  - every backticked kebab-case name in the 8 folders that names a skill exists as a folder under `.claude/skills/`. Run `grep -rohE '\`[a-z]+(-[a-z]+)+\`' {the 8 folders} | sort -u` and check each skill name against `ls .claude/skills`. Plan-time absentees that must be reworded or dropped: `service-infra-conventions`, `create-host`, `consumer-patterns`, `add-typed-client`, `add-conventions-check`;
  - every `conventions/{file}.md § {section}` pointer resolves to a heading in `docs/conventions/{file}.md`, or is removed (plan-time: `ef-core.md` has no "Queries" heading);
  - `ls .claude/skills/*/SKILL.md | wc -l` equals the count in `.claude/README.md`;
  - each merged copy's repo facts: one line per fact in `implementation.md`, naming the live file that confirms it, or `target — lands in Step N`.

### Step 2 — Central package versions

Follow central-package-management (local copy).

- **Packages (17):** FluentValidation, FastEndpoints, FastEndpoints.Swagger, Microsoft.EntityFrameworkCore, Microsoft.EntityFrameworkCore.Sqlite, Microsoft.EntityFrameworkCore.Design, Microsoft.AspNetCore.OpenApi, Microsoft.AspNetCore.Authentication.Google, Microsoft.Extensions.Http, Microsoft.Extensions.Options.DataAnnotations, Scalar.AspNetCore, Refit.HttpClientFactory, Microsoft.NET.Test.Sdk, xunit, xunit.runner.visualstudio, FluentAssertions, NSubstitute. They come from 9 project files: `Blocks.Core`, `Blocks.Domain`, `Blocks.EntityFrameworkCore`, `Blocks.FastEndpoints`, `Jira.RestApi`, `Xray.GraphQL`, `Fokus.API`, `Fokus.Persistence`, `Fokus.Tests`.
- **First, before any edit, capture the API's baseline documents.** On the unchanged merged tree, start the API in Development on a temporary database file: set `ConnectionStrings__Database` to a connection string under `D:\temp`, for example `Data Source=D:\temp\f38-before.db`. The value goes straight to `UseSqlite` and `SqliteConnectionStringBuilder`, so a bare path fails. Also set `ASPNETCORE_ENVIRONMENT=Development`. Save `/swagger/v1/swagger.json` (the FastEndpoints document, served by `UseSwaggerGen()` in `DependencyInjection.cs:67`) as `docs/specs/F38-DotnetSkillAlignment/delivery/api-before.json`. Stop the API. Step 6's accept diffs against it.
- **Versions, by owner ruling "middle":**
  - Microsoft.* 10.0.x packages: the latest stable 10.0.x on NuGet the day you write the line.
  - Scalar.AspNetCore: latest stable 2.x.
  - FluentAssertions: latest stable 8.x.
  - xunit: 2.9.3.
  - **FastEndpoints, FastEndpoints.Swagger and FluentValidation stay at today's 6.0.0 / 6.0.0 / 11.11.0 in this step**; Step 4 moves them.
  - **Pinned with a one-line reason** ("held on this major by owner decision 2026-10-13; the upgrade is open work"): Refit.HttpClientFactory 8.0.0; Microsoft.NET.Test.Sdk 17.14.1; xunit.runner.visualstudio 2.8.2; NSubstitute 5.3.0. These are the versions resolved today (`obj/project.assets.json`).
- **Keep** `Microsoft.EntityFrameworkCore.Design`'s `PrivateAssets`/`IncludeAssets` children on its bare reference.
- **Accept:**
  - the skill's three-form search → 0 hits in `*.csproj` (plan-time baseline: 20 lines);
  - the nested-props check → exactly `src/Directory.Packages.props`;
  - `grep -nE 'Version="[^"]*(preview|\*)' src/Directory.Packages.props` → 0;
  - full suite green;
  - after the change, start the API again on a new temporary database (`Data Source=D:\temp\f38-step2.db`). It starts with no pending-model-changes error and creates the database, which proves migrations run under stable EF Core. If it reports pending model changes, stop and raise it in `questions.md`, and never add a migration to clear it. The existing-database run stays with the operator (Step 10).

### Step 3 — Required lookups throw; no hand-written error replies

Follow error-handling (local copy). No error reply is written by an endpoint or a pre-processor: the middleware writes every one. Three kinds of lookup, three forms:

- **Required lookup by key, returns null when missing → `.OrThrowNotFound(message)`** (or `FindByIdOrThrowAsync`). Keep each endpoint's current not-found message where it has one.
  - Targets: every `SendNotFoundAsync` in `src/Services/Fokus/Fokus.API/Features` (re-grep on the merged F37 tree; plan-time files: `Analytics/GetFailingTickets`, `Analytics/GetQaMetrics`, `Analytics/GetUntestedTickets`, `Auth/RevokeInvitation`, `Auth/UpdateRole`, `Auth/UpdateStatus`, `Developers/GetCapacity`, `Developers/SetCapacity`, `Sprints/GetTestTimeline`, `Sprints/UpdateSprint`, `Team/UpdateTeamConfig`).
  - Also every existing `?? throw new NotFoundException(` (9 from F37 Step 6, 2 in `Sync/SyncSprints/SyncSprintsEndpoint.cs`), so one form remains.
- **A 404 decided by a rule, not by a null** — a direct `throw new NotFoundException(message)` at the rule. Plan-time: `Analytics/GetDeveloperDetail/GetDeveloperDetailEndpoint.cs:22` (missing *or inactive* developer) and `:80` (no qualifying activity). Both keep 404 and their condition; `Guard.ThrowIfFalse` would answer 400, so it is not used.
- **Optional lookup whose miss is a normal answer** stays as it is: an `if` that sends a 200 reply. Plan-time: `Auth/ValidateInvitation/ValidateInvitationEndpoint.cs:13-33` (valid: false with a reason) and `Analytics/GetDeveloperProgress/GetDeveloperProgressEndpoint.cs:13,21` (no active sprint). List each such branch in `implementation.md`.
- **Handler-side rule checks:** `Settings/SaveCycleTimeBoundaries/SaveCycleTimeBoundariesEndpoint.cs` answers three stage-order rules with `AddError` + `SendErrorsAsync(400)` inside `HandleAsync` (lines 45–60). It now throws `BadRequestException` with the same messages (Decision D7). Any other `AddError`/`SendErrorsAsync` left in a handler after F37 merges gets the same treatment.
- **The pre-processor's 401:** `src/Services/Fokus/Fokus.API/Auth/ActiveUserPreProcessor.cs:24-35` writes a 401 body by hand. It throws `UnauthorizedException` with the same two messages instead (Decision D10).
- **Error bodies after this step:** the middleware's camel-case envelope (`statusCode`, `message`, `traceId`, `details`). The 400 from `SaveCycleTimeBoundaries` loses FastEndpoints' `errors` list; the pre-processor's 401 gains `traceId` and `details`. Statuses and messages are unchanged.
- **Accept:**
  - `grep -rn --include=*.cs "SendNotFoundAsync" src` → 0;
  - `grep -rn --include=*.cs "SendErrorsAsync\|AddError(" src/Services/Fokus/Fokus.API/Features` → 0;
  - `grep -rn --include=*.cs "?? throw new NotFoundException" src/Services/Fokus/Fokus.API` → 0;
  - `grep -n "Response.WriteAsync\|StatusCode = 401" src/Services/Fokus/Fokus.API/Auth/ActiveUserPreProcessor.cs` → 0;
  - full suite green.

### Step 4 — FastEndpoints 8 and the `Send.*` API

Follow framework-currency (local copy), Rule 2 and its two-stage detection.

- **Versions:** in `src/Directory.Packages.props`, move FastEndpoints and FastEndpoints.Swagger to the latest stable 8.x and FluentValidation to the latest stable 12.x. FastEndpoints 8.3.0 requires FluentValidation ≥ 12.1.1.
- **Calls:** migrate every receiver-less legacy call by the skill's map, verified against the installed 8.x. What survives Step 3, plan-time:
  - about 87 `SendOkAsync`;
  - 2 `SendNoContentAsync` (`Auth/Logout`, `Auth/RevokeInvitation`) → `Send.NoContentAsync`;
  - 1 bare three-argument `SendAsync(…, 201, …)` (`Auth/CreateInvitation/CreateInvitationEndpoint.cs:46`) → `Send.ResponseAsync` with the same status;
  - `Auth/GetMe/GetMeEndpoint.cs:16`, which `return`s the call instead of awaiting it.
- **Skip** the two receiver-qualified `.SendAsync(` calls (SignalR and HttpClient).
- **Fix** any other compile break from the 6 → 8 and FluentValidation 11 → 12 moves against their release notes, and list each one in `implementation.md`. `Blocks.FastEndpoints/DomainEventPublisher.cs` uses `PublishAsync(Mode.WaitForAll, ct)`, which 8.3's `EventExtensions` still declares.
- **Accept:**
  - `grep -rnE --include=*.cs "(^|[^.A-Za-z])Send[A-Za-z]*Async\(" src` → 0 (plan-time baseline: 111);
  - `grep -n 'Include="FastEndpoints" Version="8\.' src/Directory.Packages.props` → 1;
  - full suite green.

### Step 5 — One role vocabulary; admin writes behind one named policy

Follow authorization-patterns (local copy), Phase 1 and Phase 2 for FastEndpoints, role layer only (Decision D5).

- **Vocabulary:** in `src/Services/Fokus/Fokus.Domain/Auth/`, the enum `AccessRole` (Admin, Manager) and its string constants `AccessRoles`, derived with `nameof`. These names are binding. Why not the skill's `Role`/`Roles`:
  - `Role.Admin` inside `AppUser`/`Invitation` binds to their `string Role` property;
  - `Roles.Admin` inside any endpoint binds to FastEndpoints' inherited `Roles(params string[])` method (declared in both 6.0.0 and 8.3.0), which is CS0119;
  - `Developer.Role` is a team role, a different concept.
- **Use the constants** everywhere a role name is written:
  - the 23 `[Authorize(Roles = "Admin")]` attributes;
  - `AppUser.cs:9`, `Behaviors/AppUser.cs:13`, `Invitation.cs:6`;
  - `AppUserConfiguration.cs:18` and `InvitationConfiguration.cs:13` (`HasDefaultValue`; the value is unchanged, so there is no migration);
  - `AppUserRepository.cs:21`;
  - the validators `CreateInvitationCommand.cs:31` and `UpdateRoleCommand.cs:22`;
  - `CreateInvitationEndpoint.cs:18`;
  - `UpdateRoleEndpoint.cs:21` and `UpdateStatusEndpoint.cs:21`. These compare the *target* user's role, which is a business rule, not a caller check, so they keep the comparison with a constant.
- **Gate:** one named policy for "admins only", `AccessPolicies.Admins` (binding; a static class of `nameof` constants in `src/Services/Fokus/Fokus.API/Auth/AccessPolicies.cs`, questions.md Q2), requiring `AccessRoles.Admin`, registered once in `src/Services/Fokus/Fokus.API/Auth/AuthConfiguration.cs` (today a bare `services.AddAuthorization()` at line 14). `[Authorize(Policy = …)]` goes on the 23 endpoints, including the two admin-only reads `Auth/GetUsers` and `Auth/GetInvitations`.
- **Accept:**
  - `grep -rn --include=*.cs 'Roles = "' src` → 0;
  - `grep -rn --include=*.cs '"Admin"\|"Manager"' src | grep -v "/Migrations/\|/obj/"` → 0, or each remaining hit named in `implementation.md` as not a role (plan-time baseline: 34);
  - every endpoint that had the attribute now carries the policy (23 files, listed);
  - full suite green.

### Step 6 — One type per endpoint file

Follow extract-endpoint-types (local copy).

- **Files (plan-time, re-run the check on the merged tree)** — `Developers/GetDevelopers`, `Settings/GetCycleTimeBoundaries`, `Settings/SaveAnalyticsTargets`, `Settings/SaveBoard`, `Settings/SaveBugRatioAlerts`, `Settings/SaveCycleTimeBoundaries`, `Settings/SaveDoneStatuses`, `Settings/SaveExcludedStatuses`, `Settings/SaveHealthConfig`, `Settings/SaveSyncConfig`, `Settings/SaveWorkflowStages`, `Settings/SaveXraySettings`, `Team/GetTeamRoster`, `Team/UpdateTeamConfig`, `Xray/SyncXray`, `Xray/TestConnection`, all under `src/Services/Fokus/Fokus.API/Features/`.
- **Also** `Sprints/UpdateSprint/`, where the validator sits in its own file (`UpdateSprintValidator.cs`) beside `UpdateSprintModels.cs`. The skill's "a validator in a file of its own" case: both fold into `UpdateSprintCommand.cs`.
- **Renames to the skill's names** are allowed (Decision D8). JSON property names must not change.
- **Accept:**
  - for every `*Endpoint.cs` under `Features/`, `grep -cE "^\s*public (sealed )?(partial )?(class|record)"` → 1 (plan-time: 16 files above 1). `Auth/Login/LoginEndpoint.cs` is excepted: a `public static class` that maps a minimal-API route, it reads 0;
  - `ls src/Services/Fokus/Fokus.API/Features/Sprints/UpdateSprint/` → `UpdateSprintEndpoint.cs`, `UpdateSprintCommand.cs`;
  - **wire names unchanged:** start the API as in Step 2 and save `/swagger/v1/swagger.json` as `api-after.json` beside `api-before.json`. For each path and verb, the property names of its request body and of its success reply are identical in the two files. Resolve each through `$ref`, including nested schemas, and compare them per path, verb and property location. The set of paths with their verbs is identical too. Schema type names may differ. Paste the two set sizes and an empty diff into `implementation.md`;
  - full suite green.

### Step 7 — Start-up work as start-up lines; a read that only reads

Follow add-hosted-service (§ Start-up Work), persistence-patterns (§ Migrations at Start-up, § Seed Data) and service-registration (§ Program.cs Structure) — local copies.

- **`src/Services/Fokus/Fokus.API/Program.cs`:** its inline scope (lines 17–44 at plan time; F37 Step 9 edits line 12) becomes one call per concern, before the middleware. Each call is an extension in the project that owns the concern:
  - make sure the SQLite file's folder exists, then migrate;
  - add the default `AppSettings` row when the table is empty, in every environment (Decision D4);
  - copy configured Xray credentials into the settings row. This is an API concern (it reads `XrayOptions`), so it stays as its own start-up line, with its behaviour unchanged.
  - No `BackgroundService`; the service must not take requests before these finish (Decision D2).
- **`src/Services/Fokus/Fokus.Persistence/Repositories/AppSettingsRepository.cs`:** `GetAsync` (lines 5–15) only reads. A missing row is a start-up fault, not a missing resource, so it throws `InvalidOperationException` ("the settings row is missing; start-up seeds it"), which the middleware answers with 500. Deliberately not `OrThrowNotFound`, which would answer 404. It stays tracked: it is a write-path read (F37 Step 7b leaves it untouched for the same reason). `SaveAsync` is unchanged.
- **Seed hook:** remove the automatic `builder.SeedFromJsonFile()` calls (`EntityConfiguration.cs:22,40`) and the silent reader (`Blocks.EntityFrameworkCore/Extensions/EntityTypeBuilderExtensions.cs`). No master data exists today (Decision D3). The local persistence-patterns copy keeps the explicit pattern for when one does.
- **Accept:**
  - `grep -nE "Migrate\(|AppSettings|SaveChanges|GetRequiredService" src/Services/Fokus/Fokus.API/Program.cs` → only the extension calls, no inline database work;
  - `grep -n "Add(\|SaveChangesAsync" src/Services/Fokus/Fokus.Persistence/Repositories/AppSettingsRepository.cs` → only inside `SaveAsync`;
  - `grep -rn --include=*.cs "SeedFromJsonFile" src` → 0;
  - the skill's verify greps for `Task.Run(` and a discarded `_ = ` call → 0 (plan-time baseline: 0);
  - full suite green;
  - a fresh database file gets its settings row: operator check, Step 10.

### Step 8 — The service's CLAUDE.md records every axis

Follow create-service-claude-md (local copy). It is architect-only and not model-invocable, so read its `workflows/WriteClaudeMd.md` directly. The architect captured the axes here.

- **File:** `src/Services/Fokus/CLAUDE.md`.
- **Axes:**
  - **Docker:** N, with no Dockerfile or compose file in the repo.
  - **Port:** `http://localhost:5000`, the development profile (`Fokus.API/Properties/launchSettings.json:8`). No compose service index is assigned, because the service is not containerised. This replaces "TBD (service index not yet assigned in docker-compose)".
  - **Modules:** Jira (`Jira.Contracts` + `Jira.RestApi`) and Xray (`Xray.Contracts` + `Xray.GraphQL`).
  - gRPC, integration events and Application layer: unchanged.
- **Also correct what Steps 3–7 make false here:**
  - the "AppSettings singleton … seeded on startup if missing" line now names the start-up line;
  - "Existing features" lists six routes out of 37+ endpoints: replace the list with the feature areas under `Fokus.API/Features/` (Decision D9);
  - add one line each for the error rule (lookups throw; the middleware replies), the role vocabulary and policy, and the `Send.*` API.
- **Accept:**
  - `grep -n "^\*\*Docker:\*\*" src/Services/Fokus/CLAUDE.md` → 1;
  - `grep -n "^## Modules" src/Services/Fokus/CLAUDE.md` → 1;
  - `grep -n "TBD (service index" src/Services/Fokus/CLAUDE.md` → 0.

### Step 9 — Sweep the echoes

No skill (a doc sweep). Every file that still teaches the old way:

- **`docs/conventions/csharp.md`:**
  - **line 13** — before: `` `AddError`/`SendErrorsAsync` is only for FluentValidation pre-handler validation, not for handler business logic errors. `` → after: `` A handler never writes an error reply (`AddError`, `Send.ErrorsAsync`); FluentValidation validators report input errors, and a handler throws. ``
  - **line 73** — before: `` **Don't use exceptions for expected control flow.** Validation failures, "not found" — these are domain results, not exceptions. `` → after: `` **A required lookup that finds nothing throws `NotFoundException`** (the `error-handling` skill); an optional lookup returns null and its caller decides. Input validation stays in validators. `` This removes the conflict with line 12.
- **`.claude/skills/create-feature/workflows/EndpointFastEndpoints.md`:**
  - line 28 (`[Authorize(Roles = Role.{RequiredRole})]`) → the named-policy form;
  - line 40 (`SendOkAsync` → `Send.OkAsync`);
  - line 74 (`// or Roles(...)`) → the named-policy form;
  - line 95, which names a legacy call in a "never": say `Send.*`;
  - line 100 ("Use `SendOkAsync()`…" → `Send.OkAsync`).
- **`src/Modules/Xray/CLAUDE.md:18`:** "Credentials are NOT read from appsettings.json" is false. Say what happens: an optional `Xray` config section is copied into the settings row by a start-up line (Step 7); the settings row is the runtime source; credentials are passed per call to `IXrayClient`.
- **`docs/architecture/v1.md:105`:** "Global exception handler via FastEndpoints" → the `GlobalExceptionMiddleware` in `Blocks.AspNetCore`, which maps the `Blocks.Exceptions` family to replies.
- **`docs/kb/glossary.md`:** one entry for the access role (Admin or Manager; what a user may do in the app), distinct from a developer's team role (`Developer.Role`).
- **Other echoes:** re-grep for the patterns Steps 2–7 removed: `Send[A-Za-z]*Async`, `Roles = `, `Roles(`, `Version="` in prose, `SeedFromJsonFile`, "seeded on startup". Search `.claude/`, `docs/conventions/`, `docs/architecture/`, `docs/kb/`, `CLAUDE.md` and `src/**/CLAUDE.md`, excluding `docs/specs/` (historical records), and fix each live hit.
- **Accept:** search tracked and new files only, never the git-ignored session logs under `.claude/audit/` and `.claude/skills/docs/`:
  - `git grep -n --untracked -E "(^|[^.A-Za-z])Send[A-Za-z]*Async|Roles = |SeedFromJsonFile" -- .claude docs/conventions docs/architecture docs/kb CLAUDE.md "src/**/CLAUDE.md"` → 0 outside `.claude/skills/framework-currency/`, whose migration map and false-positive list name the legacy calls on purpose;
  - `grep -n "Don't use exceptions for expected control flow" docs/conventions/csharp.md` → 0;
  - `grep -n "NOT read from appsettings" src/Modules/Xray/CLAUDE.md` → 0;
  - `grep -n "via FastEndpoints" docs/architecture/v1.md` → 0.

### Step 10 — Operator check — Owner: operator

The owner runs this after the full suite is green, at `http://localhost:5000`. Status checks run as requests from the browser console while signed in; each line prints the status.

1. Start the API on a copy of an existing database file. It starts, with no pending-model-changes error.
2. Start the API on a fresh, missing database file. It starts, and the Settings page loads defaults.
3. **Page sweep (admin):** open every page and every tab once — Dashboard, Developers (and one developer's detail), every Analytics tab, Sprints, Team, every Settings tab — and save one setting on each Settings tab. Data shows, and no page shows an error that was not there before the build.
4. **403:** signed in as a manager, run `fetch('/api/settings/board',{method:'PUT',headers:{'Content-Type':'application/json'},body:'{}'}).then(r=>console.log(r.status))` → 403. Then `fetch('/api/users').then(r=>console.log(r.status))` → 403.
5. **404:** `fetch('/api/analytics/developer-detail/no-such-id').then(r=>console.log(r.status))` → 404. Repeat with an inactive developer's id → 404, and with an active developer who has no activity → 404, if one exists.
6. **400:** on the Workflow tab, set an end stage before the start stage and save. The error text shows the rule's message.
7. If Xray credentials are in config, the Xray tab shows them enabled after start.

## Cross-Service Changes

None.

## Migration Notes

No migration. Step 2's version move must leave the model unchanged; Step 2 proves it on a new database, and Step 10 item 1 on an existing one. If the API reports pending model changes, stop and raise it in `questions.md`. Never add a migration to make it go away.

## Must-NOT-Change

| Invariant | Pinned by | Disposition |
|-----------|-----------|-------------|
| Every route, verb and success-reply JSON property name | Step 6 accept (property-name and path sets diffed between `api-before.json` and `api-after.json`); Step 10 item 3 | pinned |
| Error replies: status and message unchanged. Approved body changes, all now the middleware's envelope:
- `SaveCycleTimeBoundaries` 400 drops the `errors` list;
- the pre-processor 401 gains `traceId`/`details`;
- the formerly empty 404s from the converted lookups and from `GetDeveloperDetail`'s two rule branches now carry a body with a message, and are logged at Error level by the existing middleware (added at the build's review, 2026-10-13) | Step 3 accepts; Step 10 items 4–6 | pinned, with the named exemptions |
| Missing entity → 404 (including the inactive and no-activity developer rules); failed input/rule → 400; optional lookups keep their 200 replies | Step 3 accepts and its `implementation.md` list; Step 10 items 5–6 | pinned |
| Admin-only endpoints stay admin-only, including the user and invitation lists; managers get 403 | Step 5 accept (23 files carry the policy); Step 10 item 4 | pinned |
| A fresh database gets its settings row; an existing one keeps its own | Step 2 and Step 7 accepts; Step 10 items 1–2 | pinned |
| Configured Xray credentials still reach the settings row at start | Step 10 item 7 | pinned (operator) |
| No schema change | no new file under `Fokus.Persistence/Migrations/` (`git status`); Step 2's start on a new database; Step 10 item 1 | pinned |
| Batch sync keeps reporting per-item failures in its reply | no step edits those catches: the diff of `SyncSprintsEndpoint.cs`, `SprintIssueSyncService.cs` and `XrayIssueSyncService.cs` leaves every `catch` block unchanged | pinned |
| Every existing test passes | full suite at close | pinned |
| SignalR and HttpClient `SendAsync` calls untouched | Step 4 skips receiver-qualified calls; the 2 `.SendAsync(` lines unchanged in the diff | pinned |

## Testing Strategy

No new tests (owner ruling). The full suite runs at the handover and at close. Per-step accepts are greps and the build. Behaviour the suite cannot reach (start-up, auth, status codes, the Settings page) is the operator's Step 10.

## KB Impact

One glossary entry: the access role, added in Step 9 (a new domain term, per the glossary-first rule). No other `docs/kb/` entry records start-up, error replies, roles or package versions (plan-time grep for `seeded|CreateDefault|AppSettingsRepository|Authorize|SendOkAsync|startup`: 0 hits). `docs/kb` `developer-detail.md` "404 Cases" already matches Step 3's two rule-based 404s. The service CLAUDE.md, the Xray module CLAUDE.md, the architecture doc and the conventions are updated in Steps 8–9.

## Decisions

| # | Decision | Why | Rejected alternative | Status |
|---|---|---|---|---|
| D1 | Order: errors thrown (Step 3) before the FastEndpoints upgrade (Step 4) | every step then builds green: Step 3 removes `SendNotFoundAsync`/handler `SendErrorsAsync` on 6.0, so Step 4 renames only what survives | upgrade first and rename everything, then convert again | decided |
| D2 | Start-up work stays as start-up lines (extension calls), not a `BackgroundService` | `add-hosted-service` § Start-up Work: work that must finish before requests is a start-up line | the fit file's "start-up task" | decided |
| D3 | Remove the automatic JSON seed hook rather than make it throw | it runs on every table configuration and no master data exists; throwing would break every configuration; the skill seeds master data explicitly per configuration | make `SeedFromJsonFile` throw in place | decided |
| D4 | The default settings row is seeded by a start-up line in every environment | it is required data, not test data; owned JSON columns rule out `HasData` (`docs/conventions/ef-core.md`, "HasData + ToJson") | dev-only test seeding; `HasData` | decided |
| D5 | Access-role vocabulary + one role-only named policy; no resource layer; the local skill carves out its "no role-only write" and "reads are authentication-only" rules for this repo | Fokus has one team and no per-resource ownership, so a resource check would always pass. The skill's FastEndpoints form is a named policy, which lists who is admin-gated in one place; a role constant on each attribute repeats that list 23 times | the full two-layer gate; `[Authorize(Roles = AccessRoles.Admin)]` attributes | decided |
| D6 | Keep Fokus's `Blocks.Exceptions` family; the local `error-handling` copy names it as this repo's form | the family already maps through one middleware; rebuilding it is a larger change than the S effort the owner scoped | the plugin's `DomainException`-rooted family | decided |
| D7 | Handler-side rule checks in `SaveCycleTimeBoundaries` throw `BadRequestException` | the skill and `csharp.md:12` forbid hand-written error replies. The status and message stay; the body becomes the middleware envelope, without the `errors` list. The client shows any non-OK body as text (`client/src/api/client.ts:19-21`) | leave them as `AddError` + `Send.ErrorsAsync` | decided |
| D8 | Step 6 renames requests/replies/validators to the skill's names | the skill renames in the same pass; type names are not on the wire | move without renaming | decided |
| D9 | The service CLAUDE.md lists feature areas instead of six routes | the six-route list is stale (37+ endpoints) and agents read this file | append every route | decided |
| D10 | `ActiveUserPreProcessor` throws `UnauthorizedException` instead of writing its 401 | one central mapper writes every error reply. Status and message stay; the client redirects on any 401 (`client.ts:14`) | keep the hand-written 401 as a declared exception | decided |
| D11 | The sync services' per-item `catch` blocks and the best-effort SignalR broadcast `catch` stay; the local `error-handling` copy names both as this repo's forms | a per-item catch turns one failed sprint or ticket into a reported failure in a partial result, which is the sync contract; a failed broadcast must not fail a sync that succeeded; removing either changes behaviour | strip them to match the plugin's catch rule | decided |
| D13 | Identity stamping (`authorization-patterns` Phase 4) stays the taught rule in the local copy, named as not yet adopted; no code change | Fokus has no command that carries an acting user beyond two claim reads; adding the hook is a design change beyond S scope | carve the rule out as not applying | deferred |
| D14 | The Step 1 copies describe the target state the plan decides; a fact not yet true is logged `target — lands in Step N` and re-verified by the done-check | Steps 2–7 then read their local skill as the form to build, and no later step needs an unlisted skill edit | today's state, with skill edits added to Steps 3, 5 and 7 | decided |
| D12 | The validators' message literals are not moved to § Validation Messages helpers in this feature; the local copy states it, and it goes to open work | it touches every validator, beyond the S scope the owner picked | include a message-helper pass now | deferred |

## Open Questions

None.

## Plan Review

- **Round 1 (2026-10-13):** Codex primary (NO-GO: 1 HIGH, 10 MEDIUM, 5 LOW) + Sonnet second reader (ACCEPT: 7 MEDIUM, 6 LOW). Merged to 23 findings, R1-01 to R1-23 (1 HIGH, 13 MEDIUM, 9 LOW); all folded as `fixed-real`. Record: `review-critic.md` § Round 1 (merge and fold) and `review-critic-codex.md`. Round 2 ran because round 1 held MEDIUM+.
- **Round 2 (2026-10-13):** Codex, a check of the fold only, MEDIUM and up. NO-GO: 19 closed, 4 partial, and 3 MEDIUM:
  - R1-07 residual: the SignalR broadcast `catch`;
  - R2-01: the wire-name check was flattened;
  - R2-02: the temporary-database value must be a connection string.
  - All folded as `fixed-real`, plus the R1-23 partial (recursive grep). No CRITICAL, so there is no round 3. Record: `review-critic.md` § Round 2.
