# F39 — Service Layer Split — Second Reader Review

Second reader, round 1 (first review). Primary review: `review.md`. This file is the second reader's own record; the coordinator merges both into the fix list.

## Step 2 — Code Review

## Reviewed By
reviewer (second reader), with three read-only equivalence helpers per area (Sprints, Developers, Qa/Workflow/Shared) and two for the loaders/persistence copy and for the reply DTOs and maps. Every helper claim below was re-checked against the source or a run before it entered this file.
**Model:** claude-sonnet-5-5

## Verdict: APPROVED

No CRITICAL or HIGH. Three LOW findings and two reader-side gaps. The refactor changes no output value on any path I could construct or trace.

## Pre-commitment Predictions
- **Step 7 decomposition drifts behaviour on branches the seeded fixture does not reach** (empty or zero guards, `>` vs `>=`, tie order, summation order, a shared copy vs the original list instance). Investigated by five independent traces of old (`git show 5fe66f8:`) against new; **none found**. Four helpers plus my own reads agree, and my own mutation battery (below) shows which branches the harness cannot see, so the traces were aimed there.
- **D6 "provably equal" merges are not.** `FilterMemberships` (a)/(b), `GetDeveloperMemberships(subTeam: null)`, `ResolveCardDelta`, `PassRate` `0m` vs `0`, `IsStartedInSprint` list overload: all hold. No (a)-variant caller mutates or compares the list instance it gets back.
- **Loaders change query order, early exits or the SQL count.** Not found. Order, 404 timing and empty replies match for all six loaders; the SQL total is 6536 on both sides.
- **The Persistence copy replacement changes the started-in-sprint filter.** Not found: ordinal ticket key, `OrdinalIgnoreCase` stage index, `startIndex < 0`, a ticket with no transitions and first-qualifying vs `Any` all match. A negating mutant of it is killed.
- **Docs and skills contradict the code they describe.** Found one (L1 below).

## Findings

### [LOW] Skill states "No `_Shared` folders yet" while this feature created one
**File:** `.claude/skills/create-feature/SKILL.md:62`
**Origin:** implementation
**Issue:** The merged skill's `## This Repo's Form` ends with "No `_Shared` folders yet: shared analytics logic lives in `Fokus.Domain/Analytics/Shared/`." Step 8 created `src/Services/Fokus/Fokus.API/Features/Analytics/_Shared/` (four shared reply shapes with their maps, namespace `…Analytics.Shared`), as the same skill prescribes at lines 85 and 109. The skill is the authority for patterns, so the next developer is told the opposite of the tree.
**Fix:** Replace the bullet with: reply shapes several analytics operations share live in `Features/Analytics/_Shared/` (namespace `…Analytics.Shared`); shared analytics logic stays in `Fokus.Domain/Analytics/Shared/`.
**Confidence:** 90/100

### [LOW] Effective-SP rule is copied again instead of calling the entity's own method
**File:** `src/Services/Fokus/Fokus.Domain/Analytics/Shared/TestCoverageCalculator.cs:161` (also `src/Services/Fokus/Fokus.Persistence/Repositories/TestExecutionRepository.cs:274`; pair at `Sprints/EpicProgressCalculator.cs:311` and `Sprints/SprintSummaryCalculator.cs:471`)
**Origin:** implementation
**Issue:** `SprintMembership.GetEffectiveSp(int)` (`Fokus.Domain/Sprint/Behaviors/SprintMembership.cs:11`) already holds the rule. Step 7 merged three private copies into one new private copy in `TestCoverageCalculator` (body identical to the entity's), and left the Persistence twin. `GetEffectiveTicketSp` exists twice, byte-identical (checked by eye on both bodies), one comment saying "mirrors" the other. `docs/conventions/coding-conventions.md` says behaviour belongs with state. Each copy is a place for the rule to drift; the merged outcome is no worse than before, but a new copy was created in the kernel where the plan's aim was one declaration per family.
**Fix:** In `TestCoverageCalculator` and `TestExecutionRepository` call `m.GetEffectiveSp(defaultSpPerBug)`; move `GetEffectiveTicketSp` to `Shared/` (or onto `Ticket`) and use it from both calculators. D6-legal: byte-identical bodies. This confirms Carry-Over rows 3 and 4.
**Confidence:** 90/100

### [LOW] Two DTO types named `SparklinePointDto` with the same shape in one assembly
**File:** `src/Services/Fokus/Fokus.API/Features/Analytics/_Shared/MetricCardDto.cs:15` and `src/Services/Fokus/Fokus.API/Features/Analytics/GetQaWorkload/GetQaWorkloadQuery.cs:110`
**Origin:** implementation
**Issue:** Step 8 added the shared `sealed record SparklinePointDto(string SprintName, decimal Value)`. `GetQaWorkload` keeps its own pre-existing `class SparklinePointDto` with the same two members. `implementation.md` records the consequence: the `_Shared` namespace cannot be a global using, so every consumer imports it per file. The wire and the API document are unaffected (schema ids carry the namespace: `…Analytics.SharedSparklinePointDto` and `…GetQaWorkloadSparklinePointDto` in the final `api.json`), so this is a hazard for the next edit, not a defect today.
**Fix:** Let `QaWorkloadMetricCardDto.Sparkline` use the shared record (same members, same order, so the JSON is unchanged), or rename one of the two.
**Confidence:** 85/100

## Positive Observations
- Moves were checked mechanically, not by reading both copies: `diff -w` of both sync services against `git show 5fe66f8:` shows only using and namespace lines (the three `catch` blocks in each are untouched); the four moved test files differ from base only by class names, the `_sut` type, the `SelectRollingWindowSprintIds` owner and the 19 `today:` arguments, as `implementation.md` states.
- The kernel merges are written with care: variants that differ stay, named, with the difference (`GetCapacity` flat lookup, two `ComputeRollingAverage`, four sparkline builders, two alert evaluators). Every claimed equivalence I tried to break held.
- Domain purity is real: no logger, options, repository, DbContext, clock or JSON in `Fokus.Domain/Analytics`; no instance state in any calculator; the clock is a required last parameter, so a forgotten caller does not compile.
- Method-length claim reproduced with the developer's script: 0 methods over 80 in `Fokus.Domain/Analytics`, longest 80.
- The 130 reply DTOs and 114 `To…` maps were audited mechanically (member name, order, type, nullability against both the Domain record and the base wire type; same-named source member for every argument; empty-reply branches against base). No difference.
- Docs sweep is clean beyond the plan's own greps: no remaining reference to a moved class or the old "no Application project" text in `*.md`/`*.json` outside `docs/specs` and the audit, research, proposal and evaluation folders; the only Mapster lines left are the two skills the plan names as out of scope (`add-integration-event/workflows/Consumer.md:71`, `create-grpc-contract/workflows/Server.md:39`); every path the rewritten skills cite exists.

## Gaps
- **The golden harness has blind spots that the "byte-identical" claim does not cover.** My own battery of 17 single-line mutants of the new code, run in a scratch copy of the tree against the harness (unmutated copy first: `COMPARISON: PASS`), killed 10 and let 7 survive:
  - `last=0` on qa-trends (`QaTrendsInputs`: `last is null or 0` to `last is null`): the manifest sends `last=2` and nothing else.
  - the 12-sprint streak cap in `DeveloperQualityInputs` (to 11): the fixture has 6 closed sprints.
  - the excluded-developer set in `SubTeamFilter.FilterMemberships`: the fixture's one excluded developer (Bob, sprint 104) owns no tickets, so no reply depends on the filter.
  - the pass-rate guard at exactly one run (`TestCoverageCalculator.ComputePassRate`, `> 0` to `> 1`).
  - the "no activity in closed sprints" 404 in `GetDeveloperDetailEndpoint` (`> 0` to `>= 0`).
  - `GetDeveloperMemberships`' sub-team clause and `GetCapacity`'s `?? 100`: equivalent mutants in practice (callers pre-filter developers by sub-team; the developer is always in the list).
  Killed: the loaders' prior-sprint guard, both sparkline windows (quality, workload, summary), polarity inversion, card rounding, the list-overload start boundary, business days, a DTO field swap, and the Persistence started-in-sprint filter. For each surviving path the old and new code were traced equal (the loader and calculator helpers, and I re-read the old `last == null || last == 0`, `TakeLast(12)` and the activity check). So this is a statement about proof strength, not a defect.
- Branches the fixture cannot reach were verified by reading only. The operator check (Step 14) is the other net.
- Not checked: Step 14 itself (operator-owned).

## Open Questions
- **Extend the external fixture before the operator check?** Recommendation: add to the harness fixture and manifest (never committed, as the owner ruled) a `last=0` request, at least 12 QA sprints, and one excluded developer who owns a ticket, then re-run the comparison. **Confidence: medium** — the traces agree on all of these paths, so the extension is cheap insurance, not a known risk.

## Carry-Over Findings
| Title | Verdict | Evidence |
|-------|---------|----------|
| Stale `.claude/test-times.json` keys | Confirmed, no code action | 33 keys carry the four old `…ServiceTests` names (`grep -c "ServiceTests"` → 33); the developer was right not to touch the file under the owner constraint. The close's delta pass re-times the renamed tests. |
| `graphify update .` not run | Confirmed, LOW | Root `CLAUDE.md` asks for it after code edits and no step ran it. `graphify-out/` is gitignored (`.gitignore:447`), so it is a main-session action, not a repo diff. |
| `GetEffectiveSp` twin in Persistence | Confirmed | Body identical to the entity's own method; folded into the second LOW finding. |
| `GetEffectiveTicketSp` pair | Confirmed | Byte-identical in `EpicProgressCalculator.cs:311` and `SprintSummaryCalculator.cs:471`; a D6-legal merge to `Shared/` was left out. Folded into the second LOW finding. |
| Architecture doc delta line | Refuted as stale | `docs/architecture/v1.md:73` "Query endpoints must compute both current value and delta…" is still true at the reply level. Optional rewording only. |

## Evidence
| Check | Result | Command | Output |
|-------|--------|---------|--------|
| Run record | pass | complete-suite record named in the brief (`role: full`, `profile: complete`, slug `F39-ServiceLayerSplit`) | `.claude/audit/verify-verdict.json` · pass · 2026-10-14T08:21:55Z · tree_hash `3c4aeadf…` |
| Golden comparison on the reviewed tree | pass | `harness/main` rebuilt (up to date), `capture review-second`, `bash compare.sh base-final review-second` | replies `diff -r` exit 0, 0 files differ; SQL counts 609/609, 0 risen, total 6536 both; API document 56 operations, 1366 locations, 0 differences; UTC date 2026-10-14. (Not the suite: the external harness the plan names.) |
| Scratch-copy baseline | pass | unmutated copy of `src/` with a harness pointing at it | `COMPARISON: PASS` against `base-final` |
| Mutation battery (17 mutants) | 10 killed, 7 survived | one mutant at a time in the scratch copy; a kill counts only from a replies or SQL diff | survivors listed under Gaps; no compile failures, no adjudication bucket |
| Plan accept greps | pass | `*Service.cs` in API → none; purity grep → 0; clock grep → 0; aggregate-name namespace grep → 0; Mapster grep over the 4 skill folders → 0; `Migrations` diff → empty; `Fokus.Domain.csproj` diff → empty; 43 `SKILL.md` = README | live, this review |
| Method lengths | pass | `node D:/temp/f39-golden/method-lengths.js src/Services/Fokus/Fokus.Domain/Analytics 60 12` | 0 over 80; longest 80 (`QaMetricsCalculator.ComputeQaMetrics`) |
| Moved-file diffs | pass | `git show 5fe66f8:{old} \| diff -w --strip-trailing-cr - {new}` for both sync services and the four test files | using and namespace lines only; tests: names and `today:` arguments only |
| Platform-conditional grep | n/a | no `process.platform` or OS-conditional in the diff (C# only; none added) | — |

My scratch tree and the extra captures I made under `D:\temp\f39-golden\captures\` were removed afterwards; the repository was not written to.

*Status: COMPLETE — reviewer (second reader), 2026-10-14*
