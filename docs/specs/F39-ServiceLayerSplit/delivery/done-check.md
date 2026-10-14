# F39 — Service Layer Split — Done Check

## Step 1 — Done-Check

### Check 1 — 2026-10-14 — after the build

**Model:** claude-opus-5-5

| Step | Disposition | Notes |
|------|-------------|-------|
| 1 — Bring the skills in | Implemented | Live re-run: 43 `SKILL.md` = README `(43 skills)`; Mapster/`Adapt<`/`IRegister` grep over the 4 folders → 0; self-containment grep → 0. |
| 2 — The golden harness and the base capture | Deviated (valid) | Additions inside the step's intent: dummy `Jira:*` overrides (keeps user secrets out, gives `SyncXray` its project key) and a comparator self-test. Base `5fe66f8`; determinism, fixture, sabotage and coverage results recorded. Captures and reports present under `D:\temp\f39-golden\captures\` (the sabotage diff is non-empty, the final diff empty). |
| 3 — The Application project | Implemented | Live: `Fokus.Application` → 1 each in slnx, API and Tests csproj; `Fokus.Domain.csproj` diff empty; `TryAddSingleton(TimeProvider.System)` once; no moved-type registration left in `Fokus.API/DependencyInjection.cs`. No `Blocks.*` reference needed (reached transitively at Step 9). |
| 4 — The shared kernel's independent parts | Deviated (valid) | `ResolveStartIndex` merged one step early — provably equal, uncoupled, D6-legal. Live greps: kernel classes in API → 0; `TransitionEdge` records in Persistence → 0; `…SprintInfo` records → 0; `SprintRef` in glossary → 1. Slice-end comparison PASS. |
| 5 — Sprint-level calculators | Superseded | Folder `Sprints/` per questions.md Q4 / D14 (plan amended mid-step). Live: no `{N}Service.cs` left; aggregate-name namespace grep → 0; `Sprints/` in the skill → 1. `MetricCard` text lost by a script run and recovered read-only from `git show`; the body diff and the extra after-step-5 capture (PASS) cover it. |
| 6 — Developer, QA and workflow calculators; the clock | Deviated (valid) | Beyond-plan, scored: **`SprintSummaryItem` → `SprintRef`** — a ninth byte-identical 4-field record the plan's count missed; same rule as D13/D6, wire unchanged (after-step-6 PASS). **`QaWorkloadResponse` deleted** — dead (declared, never used, never on the wire). `DeveloperThroughputServiceTests` got no date argument because `ComputeThroughput` never read the clock — a plan fact error, not a gap. Live: clock grep over `Fokus.Domain/Analytics` → 0. |
| 7 — Decompose the long methods; consolidate the coupled helpers | Deviated (valid) | 0 methods over 80 in `Fokus.Domain/Analytics` (longest 80). Live family grep: one declaration each, or the named variants (`GetCapacity` flat lookup; `IsStartedInSprint` index/list overloads; `DeltaDirection`/`DeltaPolarity` decimal/int overloads in one class; `BuildMetricCard` one per return type; `ComputeRollingAverage` ×2, not provably equal). Beyond-plan, scored: **extra `Shared/` consolidations** (`TestCoverageCalculator`, `GetTransitionCompletedMemberships`, in-file merges) and dead-code removal (`totalWeight`/`predecessors`, the unused `defaultSpPerBug` parameter of the *private* `ComputeCurrentSprintDetail`) — each argued byte-identical or provably equal under D6 and the `coding-conventions` Shared-kernel rule Step 12 adopted; no public signature changed. Pinned by the after-step-7 and final comparisons (replies, SQL counts, API document all equal). |
| 8 — Each endpoint owns its reply | Deviated (valid) | Beyond-plan, scored: **Domain `TestRunSummaryDto` → `TestRunSummary`** — a Domain result is not a DTO, and the reply side now owns `TestRunSummaryDto`; the plan's API-document method lets schema type names differ, and the property-location diff is empty. Maps of `GetDeveloperQuality`, `GetQaWorkload`, `GetQaMetrics` moved to `{Op}Extensions.cs` — the skill's rule and Step 9's line limit. Live: `System.Text.Json` in Domain → 0; 19 `{Op}Extensions.cs` under Analytics/Sprints. Record inventory present (121 public records, none a wrapper). |
| 9 — Loading moves out of the fat endpoints | Implemented | Live line counts: `GetQaWorkload` 76, `GetDeveloperQuality` 39, `GetQaTrends` 45, `GetQaMetrics` 50, `GetSprintSummary` 81, `GetDeveloperDetail` 68 (all < 120). Six `…Inputs` loaders in `Fokus.Application/Analytics/{Qa,Developers,Sprints}/`. SQL 609/609, 0 risen. D11 activity check inline. |
| 10 — The sync services move to Application | Implemented | Live: no `*Service.cs` in `Fokus.API`; both sync classes in `Fokus.Application/{Sync,Xray}/`. `SprintSyncFailureDto` owned by the reply. Catch-block extraction identical (3/3, 4/4, 1/1). Slice-end comparison PASS. |
| 11 — The test project and the Persistence copies | Deviated (valid reason) | Classes and files renamed; test diff limited to usings, type/class names and the explicit dates; 41 facts before and after; `TestExecutionRepository` now uses the Domain index-based `IsStartedInSprint` and its `GetStageIndex` copy is gone. **Not done: the 33 stale `.claude/test-times.json` keys.** Reason given: the file is the main session's tag-slow-tests record (it rides the feature commit) and the developer was told not to touch it; handed to the close in the Carry-Over table. Note: the launch-constraints line in `communication-log.md:13` names `.claude/settings.json`, not `test-times.json` — the constraint came from the dispatch, which I cannot see. Either way the removal is now the close's: confirm the delta pass actually **drops** the four `…ServiceTests` keys, not only re-times the renamed files. |
| 12 — Rules, architecture doc, conventions, skills, KB | Implemented | Live: moved-class grep over docs/skills/CLAUDE files → 0; "no separate Application project"/"Thin domain" → 0; `mapster` in `CLAUDE.md` → 0; line-34 and coding-conventions after-texts present. KB lint clean per record; 16 KB files (plan said 15 — the two sync entries). Two small additions inside the step's intent (API→Domain reference stated; D2 exception in `ScaffoldCsprojFiles.md`). |
| 13 — The final comparison | Implemented | `base-final` vs `final` on 2026-10-14, A/B/C: replies diff exit 0 (report file empty), SQL 6536 both / 0 risen, API document 0 differences; cross-checks against `base-1` PASS. Extracts removed (verified absent). Must-NOT-Change pins re-checked live: Migrations diff and untracked files → 0; purity grep → 0. |
| 14 — Operator check | N/A (operator-owned) | `Owner: operator` — verified by the owner after the close. |

**Process slips:**
- test slip — `timeout 120 dotnet test src/services/fokus/fokus.tests/fokus.tests.csproj --no-build --list-tests 2>&1 | grep -e ^\s+fok` (`violations.log:92`, 2026-10-14T08:12:06Z, developer, `developer:implement`, no `blocked`)

**Rules:** 0 listed behaviour rules without a disposition · 0 `tested` rules whose named test does not exist (no rule list — plan-only definition)

**Skill conformance (log-scored).** Scope: `skill-invocations.log`, session `e2672549-…`, agent `developer`, token `developer:implement`, from the implement resume at 2026-10-14T04:07:43Z (the token string is reused across features, so the window bounds it). Every non-`None` mapping is logged at its step: Step 3 `service-registration` 07:02:51; Step 4 `domain-service` 07:04:22; Step 5 `domain-service` 07:10:30 (slice-2 developer); Step 6 07:18:51; Step 7 07:23:51; Step 8 `create-feature` 07:49:49; Step 9 `service-registration` 07:59:17; Step 10 08:07:37. Steps 1, 2, 11–13 are `(none)`; TDD column `no` throughout. `## Skills Used` matches the log. No skill slip.

**Notes:**
- The test slip is a list-only invocation (it enumerates the 41 facts, it runs none), but the rule is mechanical: an unblocked `test-entry` line on the developer seat is a slip. My plan invited it — Step 11's accept reads "fact count 41 before and after (**runner output**)" without naming the runner. Plan-hygiene note on my artifact: a fact-count accept should name `verify-run.js` or a `[Fact]`/`[Theory]` grep (the developer also did the grep: 41 in base and tree). It clears in a re-check on any `verify-run.js --profile fast` record in the fix window (whole-suite command, no file named).
- Beyond-plan changes (the team lead's question): all four — Step 6 `SprintSummaryItem` merge, Step 6 dead `QaWorkloadResponse`, Step 7 extra `Shared/` consolidations and dead code, Step 8 `TestRunSummary` rename — are documented with reasons, fall under D6/D13 or the Shared-kernel rule, and sit inside the slices whose golden comparisons are byte-equal. None is scope creep in the sense of new behaviour. The reviewer is the one to spot-check the equivalence arguments in code (`ComputePassRate` `0` vs `0m`, the variant-(a)→(b) `FilterMemberships` copy-vs-instance argument, `ResolveCardDelta`).
- An extra capture `after-step9b` exists in `D:\temp\f39-golden\captures\` with an empty replies diff; `implementation.md` does not mention it. Not a finding.
- Carry-overs for the close (from `implementation.md`): stale `test-times.json` keys (above); `graphify update .` not run (root `CLAUDE.md` asks for it after code edits); `GetEffectiveSp` twin in `TestExecutionRepository`; `GetEffectiveTicketSp` pair in `SprintSummaryCalculator`/`EpicProgressCalculator`; `v1.md` § Computed Metrics delta line could say "calculators". All LOW; none blocks.
- Plan hygiene: `## Decisions` present (D1–D14). Questions Q1–Q4 all Answered.

**Verdict: FAIL — test slip: one unblocked raw `dotnet test --list-tests` on the developer seat (Step 11). Every step is Implemented, Deviated with a valid reason, Superseded or N/A; no skill slip.**

*Status: COMPLETE — architect, 2026-10-14*

### Check 2 — 2026-10-14 — after fix cycle 1

**Model:** claude-opus-5-5
**Re-checks:** check 1

Scope: only what Check 1 failed — the one test slip. Window from `Fix dispatched: 2026-10-14T08:35:15Z`, session `e2672549-…`, read in the session root (no worktree round).

| Item | Result | Evidence |
|------|--------|----------|
| test slip — `timeout 120 dotnet test src/services/fokus/fokus.tests/fokus.tests.csproj --no-build --list-tests …` | Cleared | Whole-suite command (no test file named), so any fast-profile runner record in the window clears it: `verify-verdict.json` holds `source: "verify-run"`, `profile: "fast"`, `verdict: "pass"` at `2026-10-14T08:36:46Z` and `08:38:59Z`, matching `implementation.md` § Fix Round 1. `violations.log` has no `test-entry` line at or after 08:35:15Z. |

**Rules:** 0 listed behaviour rules without a disposition · 0 `tested` rules whose named test does not exist (no rule list)

**Notes (outside this re-check's scope, for the coordinator):**
- § Fix Round 1 says `create-feature` was invoked *after* the row-5 edit, not before it. That is a review-side matter for the round's code changes, not a step disposition: no plan step was built or redone, so it scores nothing here.
- The round also closed two of Check 1's carry-overs (the `GetEffectiveSp` twin in Persistence and the `GetEffectiveTicketSp` pair) and extended the external harness with a variant D (916 entries, `COMPARISON: PASS`; five reviewer-named surviving mutants killed). The stale `.claude/test-times.json` keys are still the close's.

**Verdict: PASS**

*Status: COMPLETE — architect, 2026-10-14*
