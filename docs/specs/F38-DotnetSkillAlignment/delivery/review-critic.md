# F38 plan review — critic record

## Round 1 — 2026-10-13 — plan — primary (codex)

Primary reader: Codex (read-only job, completed). Its verdict and findings are persisted verbatim in `review-critic-codex.md` § Round 1 (NO-GO, 16 findings: 1 HIGH, 10 MEDIUM, 5 LOW). Second reader: Sonnet (`critic-second`), recovered from its transcript by the salvage script because its message was truncated; reproduced verbatim below.

## Second reader — round 1 (sonnet)

# F38 Dotnet Skill Alignment — Critic Review (second reader, round 1)

## Mode: Plan Review
## Verdict: ACCEPT
There is no CRITICAL or HIGH at the level I can defend. Fix MEDIUM-1, 2, 3 and 4 in the plan before dispatch. MEDIUM-1 and MEDIUM-3 carry a band up to HIGH. I did not build or run tests: F37 is being built in the same working tree, and nothing here needed a build.

## Pre-commitment Predictions
| Predicted | Found |
|---|---|
| Accept commands mis-scoped | Yes, three (MEDIUM-5, MEDIUM-6, LOW-1). |
| A step leaves the build red | No. Order 2→3→4 holds, and the FastEndpoints 8.3.0 documentation (xml) has `Send`, `ResponseSender.OkAsync/NoContentAsync/ResponseAsync/ErrorsAsync`, `Mode.WaitForAll` and `IGlobalPreProcessor`. The only late-proof risk is MEDIUM-4. |
| Plan contradicts the skill it follows | Yes (MEDIUM-2, MEDIUM-7). |
| Counts have drifted | Yes, mildly (LOW-2, LOW-3). |
| Overlap with F37 | Benign. F37 uses an inline `?? throw` style that Step 3 does not reconcile (MEDIUM-7). |

## Coverage ledger
Round: 1 · Depth: mechanism · Bar: mechanics · Ruler: seed only (`docs/critic-calibration/ruler.md` absent) · Read: full

| Requirement / area | Surfaces opened | Status |
|---|---|---|
| Plan-time counts (111/55, 15 calls in 12 files, 23 `Roles = "`, 34 literals, 16 multi-type files, 20 refs in 9 csproj) | live greps over `src/` | COVERED. 108 lines in 55 files live; the other counts match. |
| Step 1: 8 skills and merge keep-lists | 8 plugin `SKILL.md`s; local `error-handling`, `authorization-patterns`, `extract-endpoint-types`, `persistence-patterns`, `create-service-claude-md` (diffed); `Blocks.*` sources | PARTIAL. See MEDIUM-1, MEDIUM-2, LOW-4, LOW-5. |
| Step 2: central versions | all csproj; `obj/project.assets.json` (Tests, API, Jira); NuGet cache; snapshot line 18 | COVERED. Resolved versions match the plan (17.14.1, 2.9.3, 2.8.2, 5.3.0, Refit 8.0.0). The EF proof is deferred (MEDIUM-4). |
| Step 3: throwing lookups | all 15 `SendNotFoundAsync` sites; `Guard*`, `RepositoryBase`, `RepositoryExtensions`, middleware, repositories; client `client.ts` | PARTIAL. See MEDIUM-7. |
| Step 4: FastEndpoints 8 and `Send.*` | 8.3.0 nuspec and xml; 6.0.0 vs 8.3.0 member diff; `Program.cs`, `DependencyInjection.cs`, `ActiveUserPreProcessor`, `DomainEventPublisher`, `GetMe`, `CreateInvitation` | COVERED. No repo-used member besides `AddError` (Step 3 removes it) is missing in 8.3.0. |
| Step 5: roles and policy | plugin `authorization-patterns`; local `authorization-patterns`; all role-literal sites; `AuthConfiguration` | PARTIAL. See MEDIUM-2 and LOW-6. |
| Step 6: one type per endpoint | accept loop run live over 56 `*Endpoint.cs`; both `extract-endpoint-types` copies | PARTIAL. See MEDIUM-3 and LOW-1. |
| Step 7: start-up lines, read-only read, seed hook | `Program.cs`, `AppSettingsRepository`, `EntityConfiguration`, `EntityTypeBuilderExtensions`, plugin `add-hosted-service` and `persistence-patterns` | PARTIAL. See MEDIUM-5. |
| Step 8: service CLAUDE.md | plugin vs local `create-service-claude-md` diff; current `Fokus/CLAUDE.md`; module CLAUDE.md files; Docker absence | COVERED. The plugin copy is a superset, and the accept greps match its template. |
| Step 9: echo sweep | `csharp.md` lines 12, 13, 73; `EndpointFastEndpoints.md`; the accept grep run live; `docs/kb` | PARTIAL. See MEDIUM-6 and LOW-1. |
| Step 10 and Must-NOT-Change | client `client.ts`; F37 plan Steps 6, 7, 9 and F37 `implementation.md`/`questions.md`; `.claude/verify.json` | PARTIAL. See MEDIUM-3. |
| F36 spec and fit note, backlog row F38 | F36 spec (grep); fit table | COVERED. See Open Questions. |
| FastEndpoints and FluentValidation 8/12 release notes | WebFetch returned the homepage only; `gh` is not installed | NOT OPENED. The plan and I both lack the release notes. |
| `create-feature/workflows/*` other than `EndpointFastEndpoints.md`, `.claude/agents`, hooks, client Vue beyond `client.ts` | — | NOT OPENED. Out of the steps' reach. |
| Round-0 fold verification | — | n/a, round 1. |

## Findings

### [MEDIUM] MEDIUM-1 — Step 1 makes the executor keep "Fokus facts" that are false in the live code
**Anchor:** seed-0123 · above seed-0127 · below seed-0120
**Band:** MEDIUM–HIGH; driver: whether the executor treats the keep-list as a mandate (the accept requires "one line per fact") or checks each fact against the live tree first
**Shape:** shipped guidance asserts things the repo does not have
**Consequence:** Refreshed skills, the repo's authority on patterns, would document helpers and override points that do not exist, and students would copy them.
**Evidence:** quoted
**Surfaces:** plan § Step 1 persistence-patterns bullet and accept; `Blocks.EntityFrameworkCore/ApplicationDbContext.cs`; `.../AuditedEntityConfiguration.cs`; `.claude/skills/persistence-patterns/SKILL.md:80,105`
**Source:** The plan says to keep "`AuditedEntityConfiguration` and its three overrides, the `ApplicationDbContext` `ref`-parameter warning, the `HasDefaultValueSql` package note". The accept says "each merged copy keeps every Fokus fact listed above: one line per fact".
**Issue:**
- Live `AuditedEntityConfiguration` has one override point, `HasConcurrencyToken`. `HasGeneratedId` sits on `EntityConfiguration`, and there is no `DefaultDateSql`.
- Live `ApplicationDbContext<TDbContext>` is `... : DbContext(options) where TDbContext : DbContext;`, an empty class. It has no `GetAllCached`/`GetByIdCached`, so there is nothing for the `ref` warning to apply to.
- The `HasDefaultValueSql` package note hangs on the missing `DefaultDateSql`.
- The local text is stale. It also names `IRepository.cs` and `Repository.cs`, which do not exist.
- Conversely, the plugin persistence base is `RepositoryBase<TContext,TEntity>` with `GetByIdOrThrowAsync` and a protected `Query()`. Fokus has `RepositoryBase<,,TKey>` with `FindByIdAsync`, a public `Query()`, and an extension `FindByIdOrThrowAsync`. The plan says "say which form this repo uses" only for `error-handling`.
**Impact:** Steps 3 and 7 say "Follow … (local copy)", so a future agent follows these claims.
**Fix:**
- Replace "keep these facts" with "re-verify each against the live file; keep only those that hold".
- Add an accept row that the merged persistence and error text names only helpers present in `src/BuildingBlocks`.
- Add the same "this repo uses" sentence for the persistence base.

### [MEDIUM] MEDIUM-2 — Step 5 follows a skill whose binding rule forbids what D5 chooses, and the merge does not say to amend it
**Anchor:** seed-0130 · above seed-0127 · below seed-0120
**Shape:** rewritten passage keeps a rule the new case violates
**Consequence:** The refreshed skill would state a rule that Fokus's own role-only gate breaks, so Step 5's "Follow" and its review have no clear reference.
**Evidence:** quoted
**Surfaces:** plugin `authorization-patterns/SKILL.md` § Binding rules 2 and Phase 2; local `authorization-patterns/SKILL.md:80-96,107-113`; plan § Step 1, Step 5, D5
**Source:**
- Plugin rule 2: "A bare role check or a bare `RequireAuthorization()` never gates a write."
- Plugin Phase 2: "A role-only admin write is not this case."
- Plan D5: "one role-only named policy; no resource layer".
**Issue:**
- The plan says only "add a section saying what this repo uses … The other phases stay as the general pattern". Rule 2 and the Phase 2 sentence therefore stay in the same file.
- The local copy being replaced currently documents the sanctioned form: "Simple (role-only) … `[Authorize(Roles = Role.Admin)]`". The plugin-as-base merge drops it.
- D5 rejects keeping `Roles = Role.Admin` attributes, but its "Why" argues only against the resource layer. It never says why a policy beats constants on the attribute.
**Fix:**
- Name rule 2 and the Phase 2 sentence in the merge instruction, and have the local copy state the carve-out.
- Add one sentence to D5 on policy vs `Roles = Roles.Admin`.

### [MEDIUM] MEDIUM-3 — Must-NOT-Change claims a wire-shape pin that no step provides, across a FastEndpoints 6→8 and FluentValidation 11→12 jump
**Anchor:** seed-0130 · above seed-0127 · below seed-0120
**Band:** MEDIUM–HIGH; driver: whether FastEndpoints 8 or FluentValidation 12 changes any serializer or error-body default (release notes unopened by the plan and by me)
**Shape:** pin not backed by a check
**Consequence:** A silently changed JSON shape or field name on any of 56 endpoints would reach the client, and the service-level suite cannot see it.
**Evidence:** quoted
**Surfaces:** plan § Must-NOT-Change row 1, § Step 6 accept, § Step 10; F37 plan § Step 12
**Untouchable:** silent-wrong-output
**Source:**
- Must-NOT-Change row 1: "Every route, verb and JSON property name | Step 6 accept (JSON names unchanged); full suite; Step 10 items 3–5".
- The Step 6 accept is only a count of `public class|record` per endpoint file.
- `Fokus.Tests` has no endpoint or serializer test.
- Step 10 samples settings saves, two 404s, one boundary error and the Xray tab. It has no Analytics, Dashboard or Developers page check.
**Issue:** F37 had a page-by-page operator check for far smaller changes. Step 4 here changes the framework under every route, and Step 6 renames types in 16 files.
**Fix:**
- Add a mechanical check: diff `/openapi/v1.json`, which `MapOpenApi` serves in Development, for property names before and after Steps 4 and 6.
- Add a Step 10 page sweep, or drop "JSON names unchanged" from the pinned-by cell.

### [MEDIUM] MEDIUM-4 — The only data-adjacent proof (EF preview.4 → stable model drift) is left to the very end
**Anchor:** seed-0129 · above seed-0127 · below seed-0116
**Shape:** late proof of an early step
**Consequence:** A pending-model-changes failure at Step 10 forces undoing Step 2 after Steps 3–9 were built on it.
**Evidence:** quoted
**Surfaces:** plan § Data Model Changes, § Step 2 accept, § Migration Notes
**Source:** "`dotnet-ef` is not installed … the operator check (Step 10) proves it". The snapshot is `10.0.0-preview.4.25258.110` (`FokusDbContextModelSnapshot.cs:18`, verified).
**Issue:** The plan's fallback is "stop and raise it in `questions.md`", with no decision on what Step 2 becomes.
**Fix:**
- Have the developer prove it at Step 2: install `dotnet-ef` with `--tool-path` on D: and run `dotnet ef migrations has-pending-model-changes`, a command available since EF 8.
- Or start the API once on a temporary SQLite file.
- Keep the operator run as the final proof.

### [MEDIUM] MEDIUM-5 — Step 7's missing-row throw "through the existing guard" yields the wrong status
**Anchor:** seed-0120 · above seed-0137 · below seed-0116
**Shape:** fixed set of outcomes has no fit for the new case
**Consequence:** If the settings row is ever absent, every settings-dependent route replies 404 "The requested resource was not found", and the old self-heal on read is gone.
**Evidence:** quoted
**Surfaces:** plan § Step 7 second bullet; `Blocks.Core/Guard.cs`; `GuardExtensions.cs`; `GlobalExceptionMiddleware.MapStatusCode`; plugin `error-handling` § Exception Hierarchy
**Source:** "When the row is missing, it throws through the existing guard". The live guards map to 404 (`Guard.NotFound`, `OrThrowNotFound`) or 400 (`ThrowIfFalse`, `ThrowIfNullOrWhiteSpace`).
**Issue:**
- A missing singleton row is a start-up invariant fault, not a missing client-supplied entity.
- The plugin skill routes configuration faults to a framework exception, which maps to 500.
- Fokus's middleware maps only `ArgumentException` to 400.
**Fix:** Name the exception, for example `InvalidOperationException("AppSettings row missing; start-up seeds it")` mapping to 500, and say it is deliberately not `OrThrowNotFound`.

### [MEDIUM] MEDIUM-6 — Step 9's accept can never reach 0 and invites a blanket whitelist
**Anchor:** seed-0129 · above seed-0127 · below seed-0116
**Shape:** accept scope includes generated logs
**Consequence:** The echo gate fails on session logs and loses its force, so a real stale echo can be whitelisted along with them.
**Evidence:** path
**Surfaces:** plan § Step 9 accept; `.gitignore:443` (`.claude/audit/`)
**Source:** The accept greps `.claude`, which holds `.claude/audit/da426634-….log`. Run live, it hits that log at lines 932, 978, 1042, 1253 and others, because the log records every grep. `.claude/skills/docs/audit/*.log` is a second such dir.
**Issue:** `grep -r` ignores `.gitignore`. The log is appended to by every session, so the count is non-zero by construction. The plan's "Whitelist any other deliberate mention in `implementation.md`" would absorb the noise.
**Fix:** Use `git grep` on tracked files, or `--exclude-dir=audit`.

### [MEDIUM] MEDIUM-7 — Step 3 states a tree-wide prohibition but targets only `SendNotFoundAsync`, and its helpers do not cover every target
**Anchor:** seed-0130 · above seed-0137 · below seed-0120
**Shape:** prohibition broader than its scope and accept
**Consequence:** Two styles for the same not-found rule remain, and the merged skill's samples do not match the repo's guard signature.
**Evidence:** quoted
**Surfaces:** plan § Step 3; `GetDeveloperDetailEndpoint.cs:22,82`; `Blocks.Core/GuardExtensions.cs`; plugin `error-handling` Guard section; the 9 F37 analytics endpoints and `SyncSprintsEndpoint.cs:29,31`
**Source:**
- Prohibition: "no inline null or not-found check".
- Instruction: "Use the existing throwing lookups …; build none".
**Issue:**
- (a) The tree already holds 11 inline `?? throw new NotFoundException(...)` sites: 9 from F37, plus 2 in SyncSprints. They are neither re-formed nor excused.
- (b) `GetDeveloperDetail` has two rule-based 404s: `developer is null || !developer.IsActive`, and `!hasAnyActivity`. No existing helper expresses a condition, so the developer must write an inline `throw new NotFoundException`, which breaks the stated prohibition. `Guard.ThrowIfFalse` would give 400.
- (c) Fokus's `OrThrowNotFound<T>(this T? value, string message = ...)` takes a message. The plugin's samples pass an id: `OrThrowNotFound(command.Email)`. Copied, that either fails to compile (int) or makes the id the whole 404 message (string).
**Fix:**
- Scope the prohibition to the endpoints Step 3 touches, or list the 11 inline sites.
- Allow a direct `throw new NotFoundException` for rule-based 404s.
- State the real guard signature in the merged skill.

### [LOW] LOW-1 — Step 6 accept fails on `LoginEndpoint.cs`
**Anchor:** seed-0127 · above — (floor) · below seed-0123
**Shape:** accept not literally satisfiable
**Consequence:** The accept as written cannot pass for one file, so the executor must improvise an exception.
**Evidence:** scenario
**Surfaces:** plan § Step 6 accept; `Features/Auth/Login/LoginEndpoint.cs`
The accept says "for every `*Endpoint.cs` … → 1". I ran it live: `LoginEndpoint.cs` returns 0 because it is `public static class LoginEndpoint`, a minimal-API mapping. It is in `Features/`. The same run gives exactly the plan's 16 files above 1.
**Fix:** Say "1, except `LoginEndpoint.cs` (static mapping, 0)".

### [LOW] LOW-2 — The README skill count and its accept are polluted by a non-skill folder
**Anchor:** seed-0127 · above — (floor) · below seed-0123
**Shape:** count ambiguity
**Consequence:** The README would say 43 skills when 42 exist.
**Evidence:** path
**Surfaces:** plan § Step 1 README bullet and accept; `.claude/skills/docs/audit/…log`
`ls .claude/skills | wc -l` gives 40 because of an untracked `docs/` session-audit dir. `git ls-files .claude/skills` shows 39 skill folders, and `.claude/README.md` already says 38. Three new folders make 42.
**Fix:** Count folders with a `SKILL.md`: `ls .claude/skills/*/SKILL.md | wc -l`.

### [LOW] LOW-3 — Step 4's "plan-time mix" lists calls that Step 3 removes
**Anchor:** seed-0127 · above — (floor) · below seed-0123
**Shape:** stale descriptive count
**Consequence:** A reader sizing Step 4 expects six error replies that no longer exist.
**Evidence:** quoted
**Surfaces:** plan § Step 4 Calls; live grep
Step 4 lists "`SendErrorsAsync` 6". Live there are 3, all in `SaveCycleTimeBoundaries`, and D1/Step 3 remove them first. Live totals are 108 lines in 55 files (the plan says 111), and the 15 `SendNotFoundAsync` calls also leave before Step 4.
**Fix:** Restate the mix as what survives Step 3, roughly 87 `SendOkAsync`, 2 `SendNoContentAsync`, 1 bare `SendAsync` and 1 `return`ed call.

### [LOW] LOW-4 — Step 1's cross-reference check is not machine-checked and misses convention-doc pointers
**Anchor:** seed-0127 · above — (floor) · below seed-0123
**Shape:** dangling references after copy
**Consequence:** Student-facing skills point at skills and sections the repo does not have.
**Evidence:** quoted
**Surfaces:** plan § Step 1 self-contained bullets and accept; plugin skills
- The plugin copies cite 20 skill names. Five are absent locally: `service-infra-conventions`, `create-host`, `consumer-patterns`, `add-typed-client`, `add-conventions-check`.
- They also cite `conventions/ef-core.md § Queries` and `conventions/csharp.md § Comments and Logging`. `ef-core.md` has no "Queries" heading.
- The accept greps only `nexus|{plugin}|claude-plugins|project-lists.md`. Absent skills are covered only by "list each in implementation.md", and convention-doc pointers not at all.
**Fix:** Add a check that every backticked skill name in the eight folders exists in `.claude/skills`, and extend the rule to `conventions/` section pointers.

### [LOW] LOW-5 — Step 9 sweep misses `Roles = Role.{…}` in `EndpointFastEndpoints.md`
**Anchor:** seed-0153 · above — (floor) · below seed-0123
**Band:** LOW–MEDIUM; driver: whether the executor reads line 28 while editing lines 40/95/100 of the same file
**Shape:** echo pattern too narrow
**Consequence:** The create-feature skill would keep teaching the attribute form that Step 5 removes.
**Evidence:** path
**Surfaces:** `.claude/skills/create-feature/workflows/EndpointFastEndpoints.md:28,74`; plan § Step 9
Line 28 is `[Authorize(Roles = Role.{RequiredRole})]` and line 74 is `AllowAnonymous(); // or Roles(...)`. The plan's grep is `Roles = \"` with a literal quote and the plan lists lines 40, 95 and 100 only, so both lines escape it.
**Fix:** Grep `Roles = ` and `Roles(`, and list line 28.

### [LOW] LOW-6 — The new role names collide in meaning with an existing "Role"
**Anchor:** seed-0137 · above — (floor) · below seed-0123
**Shape:** naming overlap
**Consequence:** `Roles`/`UserRole` in `Fokus.Domain.Auth` sit beside `Developer.Role` (team role, default "Developer"), and a reader cannot tell which is meant.
**Evidence:** path
**Surfaces:** plan § Step 5; `Fokus.Domain/Developer/Developer.cs:9`; `docs/kb/glossary.md`
The glossary has no role entry, and the KB rules say to update it for a new domain concept.
**Fix:** Suggest an access-specific name such as `AccessRole`/`AccessRoles`, and add one glossary line.

## Gap Analysis
- **Skill vs live consistency:** Fokus's `GlobalExceptionMiddleware` logs every mapped exception with `LogError("Unhandled exception: …")` and stack, 404s included. The plugin skill logs only 500s. Step 3 adds about 15 routine 404 paths to that stream, and the merged copy would say neither. This is benign and not a finding, but D6 keeps it, so say so in the copy.
- **Backward impact:** `docs/kb` cites only file paths for the Step 6 files, which are unchanged, and `developer-detail.md` "404 Cases" matches the two rule-based 404s. "KB Impact: None" holds. `CLAUDE.md` asks for `graphify update .` after code changes, and the plan does not mention it.
- **Edge probe:** Step 5 `[Authorize(Policy=…)]` on FastEndpoints is spot-checked only by Step 10 item 3 (one admin-only save). If the attribute were silently ignored, the 23 endpoints would be open, which is a security class. The grep that all 23 carry the policy does not prove enforcement.
- **Verified claims:**
  - D3, no `Data/Master` and `EntityConfiguration.cs:22,40`.
  - The `FindByIdOrThrowAsync` extension target.
  - `client.ts:19-21`.
  - Snapshot line 18, and `AuthConfiguration` line 14.
  - The 11 non-attribute role hits.
  - Step 8 greps.
  - `SyncSprints` and `GraphQLXrayClient` as the only receiver-qualified `SendAsync` calls.

## Open Questions
- Should F38 merge before F36 Phase 4 (conventions and architecture map) and Phase 5 (public history)? The plan only says it builds after F37. Low confidence, so I am not filing it as a finding.
- Does FastEndpoints 8 or FluentValidation 12 change default serialization or validation-error bodies? Nobody has read the release notes. This is the driver behind MEDIUM-3's band.

**Plan must resolve (LOW, informational):** the guard-exception choice in MEDIUM-5, and the exact `Send.*` forms for the 2 `NoContent` and 1 `ResponseAsync(…,201,…)` calls.

Reviewed: D:/src/fokus/docs/specs/F38-DotnetSkillAlignment/delivery/plan.md

### Merge — round 1

Coverage: complete — every plan surface was opened by at least one reader (the second reader's NOT OPENED rows — FastEndpoints/FluentValidation release notes, other create-feature workflows, agents, client Vue — lie outside the plan's surfaces; Codex read the client router for the operator check).

| id | readers | level | anchor | title | shape | consequence | evidence | untouchable | tag | surfaces | source ids |
|---|---|---|---|---|---|---|---|---|---|---|---|
| R1-01 | codex, sonnet | HIGH | unranked — no anchor fits; Codex severity HIGH | Suggested `Roles` constants name binds to FastEndpoints' inherited `Roles(...)` method | name collision | CS0119 in every endpoint writing `Roles.Admin`; also ambiguous beside `Developer.Role` | quoted (both FE xml declare `Endpoint.Roles(string[])`) | — | naming | Step 5 | codex-1, sonnet LOW-6 |
| R1-02 | codex, sonnet | MEDIUM | seed-0130 | Step 3's blanket not-found rule loses rule-based 404s, hits optional lookups, ignores 11 `?? throw` sites and the real guard signature | prohibition broader than scope | 404→400 or 200→404 status changes; two styles stay | quoted (`GetDeveloperDetailEndpoint.cs:22,80`, `ValidateInvitationEndpoint.cs:13`, `GetDeveloperProgressEndpoint.cs:13,21`) | — | errors | Step 3, Step 1 | codex-2, sonnet MEDIUM-7 |
| R1-03 | codex, sonnet | MEDIUM | seed-0123 | Step 1 persistence keep-list names facts false in the live code | shipped guidance asserts absent helpers | refreshed skill teaches non-existent overrides/helpers | quoted (`ApplicationDbContext.cs` empty; `AuditedEntityConfiguration` one override) | — | skills | Step 1 | codex-3, sonnet MEDIUM-1 |
| R1-04 | codex, sonnet | LOW | seed-0127 | Step 6 accept cannot pass on `LoginEndpoint.cs` (static mapping class) | accept not satisfiable | executor improvises an exception | scenario | — | accept | Step 6 | codex-4, sonnet LOW-1 — contested: MEDIUM / LOW, settled LOW: one-file carve-out, no wrong output |
| R1-05 | codex | MEDIUM | unranked — no anchor fits; Codex severity MEDIUM | `UpdateSprint` keeps a separate validator file and a `Models` file the skill covers | missed target | one feature folder keeps the old layout | path | — | layout | Step 6 | codex-5 |
| R1-06 | codex | MEDIUM | unranked — no anchor fits; Codex severity MEDIUM | `ActiveUserPreProcessor` writes a 401 reply by hand | prohibition escape | the one-central-mapper rule stays broken | quoted (`ActiveUserPreProcessor.cs:24-35`) | — | errors | Step 3 | codex-6 |
| R1-07 | codex | MEDIUM | unranked — no anchor fits; Codex severity MEDIUM | Copied catch prohibition contradicts the sync services' partial-result catches | skill vs repo contract | a future agent removes the catches and changes sync behaviour | path (`SyncSprintsEndpoint.cs:51`, `SprintIssueSyncService.cs:73,121,175`, `XrayIssueSyncService.cs:39,171,207,302`) | — | errors | Step 1 | codex-7 |
| R1-08 | codex | MEDIUM | unranked — no anchor fits; Codex severity MEDIUM | `error-handling` § Validation Messages neither applied nor deferred | silent scope gap | the local copy teaches a rule the validators break | quoted | — | scope | Step 1, Step 3 | codex-8 |
| R1-09 | codex, sonnet | MEDIUM | seed-0130 | `authorization-patterns` rules (no role-only write; reads auth-only) contradict D5 and the admin-only reads; no carve-out; D5 lacks the policy-vs-attribute reason | rewritten passage keeps a rule the case violates | the local skill contradicts the code it governs | quoted | — | auth | Step 1, Step 5, D5 | codex-9, sonnet MEDIUM-2 |
| R1-10 | codex | MEDIUM | unranked — no anchor fits; Codex severity MEDIUM | Echo sweep misses `src/Modules/Xray/CLAUDE.md:18` and `docs/architecture/v1.md:105` | echo pattern too narrow | two docs keep teaching false facts | quoted | — | docs | Step 9 | codex-10 |
| R1-11 | codex | MEDIUM | unranked — no anchor fits; Codex severity MEDIUM | Operator 403/404 checks are not executable through the UI | check not executable | the auth and status pins are never exercised | path (manager settings controls disabled; no `/sprints/:id` route) | — | operator | Step 10 | codex-11 |
| R1-12 | sonnet | MEDIUM | seed-0130 | Must-NOT-Change pins JSON property names with no mechanical check across FE 6→8 and 16 type renames | pin not backed by a check | a silently changed wire name reaches the client | quoted | silent-wrong-output | pins | Must-NOT-Change, Step 6, Step 10 | sonnet MEDIUM-3 |
| R1-13 | sonnet | MEDIUM | seed-0129 | EF preview→stable model-drift proof left to Step 10 | late proof of an early step | Steps 3–9 built on a Step 2 that may need undoing | quoted | — | proof | Step 2, Data Model Changes | sonnet MEDIUM-4 |
| R1-14 | sonnet | MEDIUM | seed-0120 | Missing settings row would throw through a guard that replies 404 | wrong outcome class | a start-up fault surfaces as "not found" | quoted | — | errors | Step 7 | sonnet MEDIUM-5 |
| R1-15 | codex, sonnet | LOW | seed-0127 | Step 9 accept greps `.claude/audit/*.log` session logs | accept scope includes generated logs | gate can never reach 0 | path | — | accept | Step 9 | sonnet MEDIUM-6, codex-15 — contested: MEDIUM / LOW, settled LOW: an exclude fixes it, no wrong output |
| R1-16 | codex | LOW | unranked — no anchor fits; Codex severity LOW | "Port: none assigned" ignores the development profile's `http://localhost:5000` | fact error | service CLAUDE.md misstates the port | path (`Properties/launchSettings.json:8`) | — | docs | Step 8 | codex-12 |
| R1-17 | sonnet | LOW | seed-0153 | Step 9 misses `Roles = Role.{…}` and `Roles(...)` in `EndpointFastEndpoints.md:28,74` | echo pattern too narrow | create-feature keeps teaching the removed form | path | — | docs | Step 9 | sonnet LOW-5 |
| R1-18 | codex | LOW | unranked — no anchor fits; Codex severity LOW | Runner pin reason ("last major that runs xunit 2") is false | wrong reason | a false comment in the props file | cited (xunit.net v2 guide; runner 4.0.1) | — | versions | Step 2, Scope | codex-13 |
| R1-19 | codex | LOW | unranked — no anchor fits; Codex severity LOW | "plan-time: 10.0.13" not substantiated | unverified figure | a stale parenthetical | cited | — | versions | Step 2 | codex-14 |
| R1-20 | codex | LOW | unranked — no anchor fits; Codex severity LOW | Error envelope misdescribed; the 400/401 body changes contradict the JSON-name pin | contract misstatement | a reviewer flags an approved change as a regression | quoted | — | errors | Step 3, D7, Must-NOT-Change | codex-16 |
| R1-21 | sonnet | LOW | seed-0127 | README skill count polluted by the non-skill `docs/` folder | count ambiguity | README says 43 where 42 exist | path | — | accept | Step 1 | sonnet LOW-2 |
| R1-22 | sonnet | LOW | seed-0127 | Step 4 call mix lists calls Step 3 removes | stale count | mis-sizing | quoted | — | counts | Step 4 | sonnet LOW-3 |
| R1-23 | sonnet | LOW | seed-0127 | Step 1 cross-reference check not mechanical; misses `conventions/` pointers | dangling references | student skills point at absent skills/sections | quoted | — | skills | Step 1 | sonnet LOW-4 |

### Fold — round 1 (architect, claude-opus-5-5, 2026-10-13)

| id | disposition | level after | re-grade (level · anchor · why) | where |
|---|---|---|---|---|
| R1-01 | fixed-real | HIGH | — | Step 5 (names, reason), Step 9 (glossary line), Domain Model Changes |
| R1-02 | fixed-real | MEDIUM | — | Step 3 (three lookup kinds, the `?? throw` sites, guard signature), Step 1 error-handling bullet, Must-NOT-Change |
| R1-03 | fixed-real | MEDIUM | — | Step 1 persistence bullet and accept |
| R1-04 | fixed-real | LOW | — | Step 6 accept |
| R1-05 | fixed-real | MEDIUM | — | Step 6 files |
| R1-06 | fixed-real | MEDIUM | — | Step 3, Decisions D10 |
| R1-07 | fixed-real | MEDIUM | — | Step 1 error-handling bullet, Decisions D11 |
| R1-08 | fixed-real | MEDIUM | — | Scope, Step 1 error-handling bullet, Decisions D12 |
| R1-09 | fixed-real | MEDIUM | — | Step 1 authorization bullet, Decisions D5 |
| R1-10 | fixed-real | MEDIUM | — | Step 9 |
| R1-11 | fixed-real | MEDIUM | — | Step 10 |
| R1-12 | fixed-real | MEDIUM | — | Step 2 (baseline document), Step 6 accept, Step 10 page sweep, Must-NOT-Change |
| R1-13 | fixed-real | MEDIUM | — | Step 2, Data Model Changes, Migration Notes |
| R1-14 | fixed-real | MEDIUM | — | Step 7 |
| R1-15 | fixed-real | LOW | — | Step 9 accept |
| R1-16 | fixed-real | LOW | — | Step 8 |
| R1-17 | fixed-real | LOW | — | Step 9 |
| R1-18 | fixed-real | LOW | — | Step 2, Scope |
| R1-19 | fixed-real | LOW | — | Step 2 |
| R1-20 | fixed-real | LOW | — | Step 3, D7, Must-NOT-Change |
| R1-21 | fixed-real | LOW | — | Step 1 |
| R1-22 | fixed-real | LOW | — | Step 4 |
| R1-23 | fixed-real | LOW | — | Step 1 accept |

Reconciled: Context (merge order), Scope (open-work list), Skill Mapping rows 3 and 9, Domain/Data Model Changes, Steps 1-10, Migration Notes, Must-NOT-Change, KB Impact, Decisions D5, D7, D10-D12, Plan Review. Each was re-read against the clauses that cite it: Step 3 vs Must-NOT-Change rows 2-3 and D7/D10; Step 5 vs D5 and the Step 1 authorization bullet; Step 2 baseline vs Step 6 accept and Data Model Changes; Step 9 glossary vs KB Impact and Skill Mapping row 9.

## Round 2 — 2026-10-13 — plan — primary (codex)

Read: delta (round 1 coverage complete; slug number 38 is not a sample). Floor: MEDIUM. One reader, as the schedule sets for rounds 2 and later. Codex's verdict, fold-verification table and findings are persisted verbatim in `review-critic-codex.md` § Round 2: NO-GO; 19 closed, 4 partial (R1-07, R1-12, R1-13, R1-23).

### Merge — round 2

Coverage: complete — the fold-touched sections and both round-1 records were opened.

| id | readers | level | anchor | title | shape | consequence | evidence | untouchable | tag | surfaces | source ids |
|---|---|---|---|---|---|---|---|---|---|---|---|
| R2-01 | codex | MEDIUM | unranked — no anchor fits; Codex severity MEDIUM | Wire-name check flattens all property names into one set | pin not backed by a check | a renamed reply property can pass the gate | quoted | silent-wrong-output | fold | Step 6 accept, Must-NOT-Change row 1 | codex r2 R2-01 |
| R2-02 | codex | MEDIUM | unranked — no anchor fits; Codex severity MEDIUM | Temporary-database override given as a path, not a connection string | instruction cannot run | baseline capture and the early model proof fail | quoted (`Persistence/DependencyInjection.cs:20`, `Program.cs:23`) | — | fold | Step 2 | codex r2 R2-02 |
| R2-03 | codex | MEDIUM | unranked — no anchor fits; Codex severity MEDIUM | R1-07 residual: the best-effort SignalR broadcast catch is outside the carve-out | carve-out too narrow | the local skill still forbids a catch the repo needs | quoted (`SyncSprintsEndpoint.cs:51`) | — | round-1 miss: Step 1 error-handling bullet | Step 1, D11 | codex r2 R1-07 residual |

### Fold — round 2 (architect, claude-opus-5-5, 2026-10-13)

| id | disposition | level after | re-grade (level · anchor · why) | where |
|---|---|---|---|---|
| R2-01 | fixed-real | MEDIUM | — | Step 6 accept (per path, verb and property location, through `$ref`) |
| R2-02 | fixed-real | MEDIUM | — | Step 2 (`Data Source=D:\temp\f38-before.db`, `f38-step2.db`) |
| R2-03 | fixed-real | MEDIUM | — | Step 1 error-handling bullet, D11 |
| R1-23 (partial) | fixed-real | LOW | — | Step 1 accept (`grep -rohE`) |

Reconciled: Step 6 accept vs Must-NOT-Change row 1 (both now speak of success-reply property names per operation); Step 2 baseline vs Step 6 accept (same start method); Step 1 error-handling bullet vs D11 and Must-NOT-Change last row (both catch kinds stay unchanged). No round 3: round 2's merge holds no CRITICAL.
