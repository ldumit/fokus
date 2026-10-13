# F38-DotnetSkillAlignment — Second Reader Review

## Step 2 — Code Review (second reader)

Second reader, round: first review. Codex is the primary reader this round; this file is the Sonnet second reader's copy of the Step-2 review. The primary's section lives in `review.md` and was not read or edited here. Lane: architect-led fast lane; the hand-back addresses the architect. Diff: working tree against `main` (`main` equals `HEAD` at 1fad3ec, so `git diff HEAD` is the F38 delta) plus the untracked files under `src/` and `.claude/skills/`.

## Reviewed By
reviewer (second reader, read-only, single-pass three-axis checklist: simplicity/DRY, bugs/correctness, conventions)
**Model:** claude-sonnet-5-5

## Verdict: APPROVED

No CRITICAL, HIGH or MEDIUM finding. Two LOW findings, three Open Questions. The one HIGH the developer hit during the build (`UpdateTeamConfig` 404 → 500) is fixed and re-verified. Two risks that could have been HIGH were checked by running them and did not materialise: FastEndpoints 8.3.0 enforces `[Authorize(Policy = …)]` exactly as it enforced `Roles`, and an exception thrown from a global pre-processor reaches the outer error middleware.

## Pre-commitment Predictions
| Predicted | Found |
|-----------|-------|
| `OrThrowNotFound` bound to the `Task` at more than the one site the developer caught | Not held (good). All 24 call sites listed; each either wraps its `await` in parentheses or runs on an already-materialised list or an in-memory `FirstOrDefault`. `UpdateTeamConfigEndpoint.cs:11-19` is fixed. Repo-wide grep for an un-parenthesised `await …` feeding the guard: 0. |
| FastEndpoints 8 silently ignores `[Authorize(Policy = …)]`, opening 23 admin endpoints to managers | Refuted by running it. Scratch host (outside the repo) on FastEndpoints 8.3.0 / FluentValidation 12.2.0, a role-only `Admins` policy, the same attribute form: anonymous → 401, Manager → 403, Admin → 200, identical to a parallel `[Authorize(Roles = "Admin")]` endpoint. The 23-file set before/after is identical (`diff` of the two file lists). See Finding 2 for the one visible side effect. |
| Step 6 split drops a property, rule or attribute; wire names drift | Not held. Independent check: for each of the 17 folders, the multiset of non-blank non-`using` lines of the HEAD files (with the plan's renames applied) equals the multiset in the new files, except the intended Step 3 / Step 5 lines. My own `$ref`-resolving diff of `api-before.json` / `api-after.json`: 56/56 operations, 0 differences in request-body property paths, 2xx reply property paths, status codes and parameters. |
| Start-up refactor (Step 7) changes order or drops the Xray line | Not held. Old inline block: folder → migrate → seed-if-empty → Xray copy. New: `MigrateFokusDatabase` (folder → migrate), `SeedDefaultAppSettingsAsync` (same `AnyAsync` guard), `ApplyConfiguredXrayCredentialsAsync` (same condition, same three assignments). Same order, every environment, before the middleware. |
| Merged skill copies carry false Fokus facts or plugin leftovers | Mostly held. About 40 repo facts re-checked against live files (repository base members, which repositories derive it, `HasGeneratedId` overrides, direct `IEntityTypeConfiguration` classes, the seven `catch` blocks and what each does, role claim, cookie 401/403 events, guard signatures, `FindByIdOrThrowAsync` message, mapper table, pre-processor registration, port, no Docker files): all true. The plan's accept greps (plugin markers, absent skill names, `conventions/…§` pointers, count 42) pass. Residue: Finding 1. |

## Findings

### [LOW] Seven references to "the plugin" in the self-contained skill copies
**File:** `.claude/skills/authorization-patterns/SKILL.md:41`; `.claude/skills/persistence-patterns/SKILL.md:18,85,89,96,141,161`
**Origin:** implementation
**Issue:** Step 1 asks for self-contained copies. These seven lines say "the plugin form …" / "which plugin rules it departs from" and never define the plugin, which does not exist in this repo. A student reading the skill meets a reference they cannot follow. The plan's accept grep (`nexus|{plugin}|claude-plugins|project-lists.md`) cannot catch the word, so the done check will pass it.
**Fix:** Reword each to name the generic form without the source: "the generic form …", "the usual form …", "the rules above". No content change.
**Confidence:** 80/100

### [LOW] The API document no longer advertises the `Admin` scope on the 23 admin operations
**File:** `docs/specs/F38-DotnetSkillAlignment/delivery/api-after.json` (every `security` entry of the 23 admin operations) against `api-before.json`
**Origin:** design
**Issue:** Before: `"security": [{"JWTBearerAuth": ["Admin"]}]`. After: `"security": [{"JWTBearerAuth": []}]`, for exactly the 23 operations that moved from `Roles` to the policy (FastEndpoints' document lists role scopes, not policy names). The Must-NOT-Change wire check compares property names and parameters only, so it could not see this, and `implementation.md` does not mention it. Runtime enforcement is unchanged (see Pre-commitment Predictions); the loss is documentation in the Development-only `/swagger` document.
**Fix:** No code change. Name it in `summary.md` Notes (a consequence of Decision D5's named policy), or accept it silently.
**Confidence:** 90/100

## Positive Observations
- Step 3 is complete and consistent: every `SendNotFoundAsync`, `?? throw new NotFoundException`, handler-side `AddError`/`SendErrorsAsync` and the hand-written 401 are gone (greps 0). The optional lookups that stay as 200 replies are exactly the two endpoints the plan named (`ValidateInvitation`, `GetDeveloperProgress`), and `implementation.md` lists each branch. Order of checks inside `GetFailingTickets` / `GetUntestedTickets` / `GetQaMetrics` is preserved (existence check, then the Xray-disabled shortcut, then the memberships load).
- The `ActiveUserPreProcessor` rewrite is behaviour-equivalent on status and message. I ran the shape on FastEndpoints 8.3.0 (scratch host, a global pre-processor that throws, an outer middleware that maps the exception): the exception reaches the middleware and the mapped 401 is written. FastEndpoints does not swallow it into a 500.
- Package work is clean and verified against NuGet, not memory: I read the flat-container index for the six packages that moved; 10.0.13, FastEndpoints 8.3.0, FluentValidation 12.2.0, Scalar 2.18.0 and FluentAssertions 8.11.0 are each the newest stable of their line. `dotnet list package --vulnerable --include-transitive` now reports nothing for all 14 projects (the earlier run record showed NU1903 warnings on the pre-F38 tree), no transitive preview remains, and `--outdated` lists only the four owner-held pins (Refit, Microsoft.NET.Test.Sdk, NSubstitute, xunit.runner.visualstudio). No file under `Migrations/` changed; `HasDefaultValue(AccessRoles.Manager)` is the same value the snapshot holds.
- Step 7 leaves one honest start-up shape: three extension calls, no `BackgroundService`, `GetAsync` only reads and throws `InvalidOperationException` (→ 500, deliberately not 404), and the dead JSON seed hook plus its reader are removed together with both call sites.
- The docs sweep is thorough: the repo-wide search for the removed forms (`Send…Async`, `Roles = `, `SeedFromJsonFile`, `AddError`, "seeded on startup", "Don't use exceptions", `UpdateSprintModels`) outside `docs/specs/` and `framework-currency` finds only the three deliberate "this is the forbidden form" mentions the developer listed. `ef-core.md:28` (not in the plan's file list) was a real echo and was fixed.
- No comment noise was added: the diff adds no comment line in `src/`; the four comments in new files are existing validator comments moved verbatim.

## Plan conformance notes
- Step 5: the plan's file list gives `UpdateRoleEndpoint.cs:21` / `UpdateStatusEndpoint.cs:21` for the target-role comparisons; the comparisons are at line 17 in both live files. Same code, only a line-number drift.
- Step 9: the plan's csharp.md "line 73" is line 72 in the live file; the text is the plan's after-text verbatim.
- Step 4: the plan expected "about 87" `SendOkAsync` plus two `SendNoContentAsync` and one `SendAsync`; the developer's counts (87, 2, 1) match, and the plan's accept grep is 0.
- Satisfies-annotations: the plan carries none (plan-only definition, no rule list), so there is no intent-drift check to run.
- Decisions D1 to D14 all hold in the code or the skill text as written; D12 and D13 are stated as open work in the local copies and need to reach `summary.md` Notes for F36 (architect's close).

## Gaps
- Nothing exercises FastEndpoints 8 with a signed-in caller inside the repo: the active-user pre-processor, the validators, the admin policy and request binding (route, query and body together) are verified only by build, the scratch hosts above, and the 56-route start-up. Plan Step 10 (operator) owns this; it is not yet recorded. The merge should wait for it, in particular item 4 (Manager gets 403 on a write and on `/api/users`), item 5 (the three 404s) and item 6 (the 400 message).
- No test covers `MigrateFokusDatabase`, `SeedDefaultAppSettingsAsync` or the new throwing `GetAsync`. The owner ruled no new tests for this release, and `Fokus.Tests` has no database harness, so this is a gap and not a finding.
- `graphify update .` is owed by the main session after the code change (CLAUDE.md); `graphify-out/` is outside the developer's file set.

## Open Questions
1. **Error-level logging for client errors (confidence 55).** The mapper logs every mapped error with `LogError` and the exception text. After Step 3 that includes 15 formerly empty 404s (13 lookups, 2 rule-based in `GetDeveloperDetail`) and the deactivated-user 401, which now logs a stack trace on each rejected request. The plan accepts mapper behaviour as it is; whether client errors should log at Warning is a change to `GlobalExceptionMiddleware`, outside F38. Architect to decide whether it goes to F36's known-open-work page.
2. **Implicit hosting dependency in `Fokus.Persistence` (confidence 50).** `MigrationExtensions` / `SeedExtensions` use `IHost` and `Microsoft.Extensions.Hosting`, which the project gets only through the shared-library chain (no `PackageReference` or `FrameworkReference` of its own). It builds, and `DependencyInjection.cs` already leans on the same chain, so this is consistent with the existing project; it would break if `Blocks.FastEndpoints` ever stopped bringing the shared framework. No action needed unless the architect wants it declared.
3. **Stale `create-feature/CHANGELOG.md` (confidence 50).** `EndpointFastEndpoints.md` changed in Step 9 and the skill's `[Unreleased]` list has no line for it. Only 7 of 42 skills keep a changelog and the existing one was already behind, so I did not file it.

## Carry-Over Findings (from implementation.md)
| Title | Disposition | Evidence |
|-------|-------------|----------|
| `UpdateTeamConfig` missing developer answers 500, not 404 | **Confirmed real, and confirmed fixed.** The un-parenthesised `await …OrThrowNotFound` bound the guard to the `Task`; the live file now reads `var developer = (await developerRepository.UpdateTeamConfigAsync(…)).OrThrowNotFound(…)` (`UpdateTeamConfigEndpoint.cs:11-19`). Every other site re-checked (list in Pre-commitment Predictions); none shares the defect. | `UpdateTeamConfigEndpoint.cs:11-19`; repo-wide grep for `await [^(]` feeding `.OrThrowNotFound` → 0 |
| Service CLAUDE.md "Thin domain (v1)" line is stale | **Confirmed, pre-existing, LOW, not F38's.** The line says "No behavior methods … Behaviors folder will be added"; `Fokus.Domain/Auth/Behaviors/AppUser.cs` exists at HEAD. Step 8's brief was "what Steps 3-7 make false", and this was already false. Leave for F36's architecture/conventions refresh. | `src/Services/Fokus/CLAUDE.md` Key patterns; `git ls-tree HEAD src/Services/Fokus/Fokus.Domain/Auth/Behaviors/` |
| Runtime under FastEndpoints 8 not yet exercised | **Partly confirmed, partly closed.** Start-up, the 56 registered routes and the swagger document are shown by the developer's runs. Policy enforcement and exception propagation out of a global pre-processor I now show on 8.3.0 with scratch hosts (Pre-commitment Predictions, Positive Observations). Still open for the operator: the real cookie sign-in, the validators' failure replies, request binding on the real DTOs, and the page sweep (Step 10). | scratch probe results above; plan Step 10 |
| Formerly empty 404 replies now carry a body | **Confirmed, LOW.** The 13 converted lookups and the 2 rule-based 404s in `GetDeveloperDetail` now answer the middleware envelope with a message, and the mapper logs them at Error level (see Open Question 1). Statuses are unchanged. The client throws on any non-OK response and shows the text; nothing in `client/src` branches on a 404 body (`DeveloperDetailView.vue` shows "Developer not found." from its own state). Name the class in `summary.md` Notes, as the architect's own lesson in `lessons.md` already says. | `client/src/api/client.ts:19-21`; `grep -rn "404" client/src` |

## Evidence
| Check | Result | Command | Output |
|-------|--------|---------|--------|
| Run record | pass | brief's `Run record:` line: `dotnet build src/Fokus.slnx && dotnet test src/Fokus.slnx --no-build && npm run build --prefix client`, unfiltered | verify-run complete · pass · 2026-10-13T21:00:22.856Z · 10.2 s · fast_green pass · session da426634. Written after the last source edit (the last source edits, `Program.cs` / `DependencyInjection.cs`, have a 20:59:17Z mtime, local 23:59:17 at UTC+3). Not re-run, per the brief. |
| Plan accept greps re-executed (no test run) | pass | `grep` over the tree | Step 2: three-form version search 0/0/0, one `Directory.Packages.props`; Step 3: `SendNotFoundAsync` 0, `SendErrorsAsync\|AddError(` 0, `?? throw new NotFoundException` 0, pre-processor hand-written reply 0; Step 4: legacy receiver-less `Send…Async(` 0, `FastEndpoints` 8.x line 1; Step 5: `Roles = "` 0, `"Admin"`/`"Manager"` outside migrations 0, 23 policy files = the 23 former role files; Step 6: public-type count 1 per endpoint file (`LoginEndpoint` 0, excepted), `UpdateSprint/` holds the two files; Step 7: `Program.cs` shows one extension call, `Add(`/`SaveChangesAsync` only in `SaveAsync`, `SeedFromJsonFile` 0, `Task.Run(`/discarded `_ =` 0; Step 8: `**Docker:**` 1, `## Modules` 1, `TBD (service index` 0; Step 1: plugin-marker grep 0, skill count 42 = README |
| Wire-name parity | pass | independent `$ref`-resolving script (node, scratch) over `api-before.json` and `api-after.json` | 56 / 56 operations; request-body and 2xx reply property paths, status codes, parameters: 0 differences. `security` scopes differ on 23 operations (Finding 2) |
| Split parity | pass | per-folder multiset comparison of HEAD lines (renames applied) against the new files, 17 folders | only the intended Step 3 / Step 5 / Step 4 lines differ |
| FastEndpoints 8.3.0 policy attribute | pass | scratch host in the session scratchpad, `[Authorize(Policy = Admins)]` beside `[Authorize(Roles = "Admin")]` and an open endpoint | anonymous 401 / 401 / open endpoint 401 (auth scheme, no header); Manager 403 / 403 / 200; Admin 200 / 200 / 200 |
| Exception thrown from a global pre-processor | pass | same scratch host with an outer mapping middleware | request with the trigger header → 401 body written by the middleware; without it → 200; Manager on the policy endpoint still 403 |
| Package currency and safety | pass | `curl` of NuGet flat-container index (6 packages); `dotnet list src/Fokus.slnx package --vulnerable --include-transitive`; `--outdated`; transitive preview grep | latest stable confirmed; 0 vulnerable in 14 projects; 0 preview; only the four owner-held pins outdated |
| Platform-conditional grep | n/a | no `process.platform`, `RuntimeInformation` or path-separator logic added | no branch to force |
| Mutation battery | not run | not triggered: the plan adds no tests and the record cites no mutation sweep | none |
| Plan-mandated tests under a filter that excludes them | n/a | the plan mandates no test (owner ruling) and the complete record is unfiltered | no exclusion hazard |

*Status: COMPLETE — reviewer (second reader), 2026-10-14*
