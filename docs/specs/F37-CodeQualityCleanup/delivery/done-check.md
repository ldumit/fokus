# F37 — Code Quality Cleanup — Done Check

## Step 1 — Done-Check

### Check 1 — 2026-10-13 — after the build

**Model:** claude-opus-5-5

Pre-commitment predictions: (1) the Step 5 removal of a Scope-listed dictionary shows up as a decision, not a deviation — confirmed; (2) the Step 7 accept grep is left matching in three files — confirmed, covered by the owner's ruling Q1; (3) a raw test run outside the runner during the TDD steps — confirmed (four lines, steps 3–4).

| Step | Disposition | Notes |
|------|-------------|-------|
| 1 — Settings: Xray secret mask never round-trips; Re-detect guard | Implemented | Accept greps re-run: no `xrayClientSecret: s.xrayClientSecret`, no `if (store.detectionResult) {`, one `"****"` in the API (`XraySecretMask.cs:5`). Behaviour is Step 12's. |
| 2 — Burnup bug area enters before it leaves, leaves once | Implemented | `ScopeChangeServiceTests.cs` (6 facts), `TestData.Membership` optional params; red-first recorded. |
| 3 — Zombie flag over all synced sprints | Implemented | `GetZombieSprintCountsAsync`; `SprintSummaryServiceTests.cs` (5 facts); `allMembershipsFlat` → 0. |
| 4 — Throughput rolling average looks back past 0%-capacity sprints | Implemented | `SelectRollingWindowSprintIds`; `DeveloperThroughputServiceTests.cs` (3 facts); `TakeLast(2)` → 0. |
| 5 — Per-ticket transition index | Deviated (valid reason) | Removed `SprintSummaryService.ComputeMetrics`' case-insensitive grouping, which the plan's Scope listed as kept. Verified in the diff: it fed only the two checker calls, and the old checker re-filtered with ordinal `==` (`TransitionAttributionChecker.cs:100,130` at HEAD), so results are unchanged. Recorded under the step's Decisions, but the block and `## Deviations from Plan` say "None" — see notes. 45 production checker calls re-counted; index construction sites listed; `DeveloperProgressServiceTests.cs` untouched. |
| 6 — Missing sprint returns 404 | Implemented | `AddError(... "Sprint not found.")` → 0 files; `throw new NotFoundException` → 9 files; order of checks listed per endpoint. |
| 7 — No cross-query fix-up, then `AsNoTracking` | Deviated (valid reason) | The accept grep `analyticsSprints.FirstOrDefault(s => s.Id == req.SprintId.Value)` still matches once in Carry-Over, Bug Ratio and Leaderboard (Step 6 existence check, still feeding an Active target). Accepted by the owner's ruling, `questions.md` Q1. 7a verdicts recorded for all 17 audited endpoints; 7b Include coverage per method; `AsNoTracking` counts 5/4/6/5/2/2, `AppSettingsRepository.cs` 0. |
| 8 — Errors shown, not swallowed | Implemented | Accept greps re-run (`catch {` 0, `silently ignore` 0, `loadError` 4/2). Addition flagged by the developer: excluded statuses locked while loading — inside the step's data-loss rule. |
| 9 — Auth cookie `Secure` outside Development | Implemented | `AuthConfiguration.cs:23-24` ternary; `Program.cs:12` passes `builder.Environment`. |
| 10 — Front-end package advisories | Implemented | `npm audit fix` without `--force`; `package.json` unchanged. |
| 11 — KB entries | Implemented | Three entries edited in place; `hardcoded 2 days` → 0; `docs/kb/index.md` unchanged (`git diff --quiet`). |
| 12 — Operator page check | N/A (operator-owned) | `Owner: operator`; carried as OPERATOR ACTION REQUIRED in the carry-over table. Q1 removes the active sprint on Carry-Over, Bug Ratio and Leaderboard from its single-sprint items. |

**Process slips:**
- test slip — `timeout 120 dotnet test src/services/fokus/fokus.tests/fokus.tests.csproj --no-build --filter fullyqualifiedname~sprints` (17:14:05Z, step 3)
- test slip — `cat > src/services/fokus/fokus.tests/zzscratchzombiequerycheck.cs <<eof using microsoft.data.sqlite; using microsoft.ent` (17:16:34Z, step 3 — the scratch SQLite translation check; the test invocation is past the log's 120-character cut)
- test slip — `pythonpath=c:/users/dumit/appdata/local/temp/claude/d--src-fokus/0a16a818-fee2-4285-a0da-2401a574cac5/scratchpad python ` (17:20:34Z, step 4; invocation past the cut)
- test slip — `timeout 120 dotnet test src/services/fokus/fokus.tests/fokus.tests.csproj --no-build 2>&1 | grep -e passed!|failed!; gre` (17:22:02Z, step 4)

All four are `rule: "test-entry"` lines in `.claude/audit/violations.log` (lines 82–85) with `spawn: nexus:developer` and no `blocked` — background calls the gate logged but could not deny, so the tests ran outside `verify-run.js`. Scope: token `developer:implement`, session `0a16a818-fee2-4285-a0da-2401a574cac5`; no worktree, one log home (`D:/src/fokus/.claude/audit/`). None of the four names a test file in its logged excerpt, so each clears in a re-check on any `verify-run.js --profile fast` record in the fix round's window, provided no new unblocked `test-entry` line sits in that window.

**Rules:** not applicable — no plan step carries `Satisfies:` (plan-only definition, rule list not applicable).

**Skill conformance:** scored against `.claude/audit/skill-invocations.log`, token `developer:implement`, agent `developer`. Every non-`None` mapping is in the window, in step order: step 1 pinia-patterns + vue-patterns (16:49), step 2 tdd (16:57), step 3 persistence-patterns + tdd (17:07), step 4 analytics-computation-service + tdd (17:19), step 5 tdd (17:24), step 6 error-handling (18:28), step 7 persistence-patterns (18:30), step 8 pinia-patterns + vue-patterns (18:37). `## Skills Used` matches the log. Steps 9–11 are `None`. No skill slip.

**Notes:**
- Record hygiene (developer, step 5): the step block and `## Deviations from Plan` say "None" although the step departed from the plan's Scope keep-list; the departure is documented only under Decisions and in the hand-back. Scored Deviated (valid reason); not a Fail.
- For the reviewer: step 8's `clearQaState` also clears the three QA errors on a sub-team change; the plan named the sprint selection reset only.
- Owed at close, not by the developer: the backlog row for Q1's active-sprint gap; `graphify update .` (graphify CLI not installed this session); the Step 12 operator check before merge.
- Plan hygiene: `## Decisions` present with 14 rows.

**Verdict: FAIL — four test slips (raw test commands run outside `verify-run.js` during steps 3–4); every step is Implemented, Deviated with a valid reason, or operator-owned**

*Status: COMPLETE — architect, 2026-10-13*

### Check 2 — 2026-10-13 — after fix cycle 1

**Model:** claude-opus-5-5
**Re-checks:** check 1

Window: from `Fix dispatched: 2026-10-13T19:06:01Z` to this check (19:09Z). Log home: `D:/src/fokus/.claude/audit/` (no worktree).

| Item from check 1 | Evidence in the window | Result |
|------|-------------|-------|
| test slip — `dotnet test … --filter fullyqualifiedname~sprints` (17:14:05Z) | `verify-run` records, `profile: fast`, verdict pass, 19:07:10Z and 19:08:30Z (`.claude/audit/verify-verdict.json`) | Cleared |
| test slip — scratch SQLite test (`cat > …zzscratchzombiequerycheck.cs`, 17:16:34Z) | same fast records; no test file named in the logged excerpt | Cleared |
| test slip — `pythonpath=… python` (17:20:34Z) | same fast records; no test file named in the logged excerpt | Cleared |
| test slip — `dotnet test … --no-build` (17:22:02Z) | same fast records | Cleared |

No `test-entry` line in `violations.log` falls in the window: the file still ends at line 85 (17:22:02Z), so the developer ran no raw test command this round. The `## Fix Round 1` block says every test went through `verify-run.js --profile fast`, and the log agrees.

**Rules:** not applicable — no plan step carries `Satisfies:`.

**Notes:**
- Out of this re-check's scope, recorded only: fix row 3 moved the explicit-id lookup above the Xray-disabled 200 in `GetDeveloperQualityEndpoint` and `GetQaWorkloadEndpoint`. Step 6's rule (explicit-id check before the empty-data shortcut) still holds. The developer also edited the closed Step 6 block to show the new order, annotated it, and disclosed the edit as a deviation in `## Fix Round 1`.

**Verdict: PASS**

*Status: COMPLETE — architect, 2026-10-13*
