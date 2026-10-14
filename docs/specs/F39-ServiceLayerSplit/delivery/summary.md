# F39 — Service Layer Split — Summary

## Status: COMPLETE

## What Was Built
- The 18 API-layer `*Service` classes are gone: the 16 analytics computations and the workflow-stage logic are pure Domain calculators in `Fokus.Domain/Analytics/{Sprints,Developers,Qa,Workflow,Shared}`, and a new `Fokus.Application` project holds the six analytics loaders and the two sync services. Endpoints now coordinate (request → loader → calculator → own reply via hand-written `{Op}Extensions.cs` maps); no Domain type travels on the wire.
- Every method over 80 lines in the analytics Domain was split (26 → 0), duplicated helpers merged to one copy, and the rules, architecture doc, conventions, four skills and 16+ KB files rewritten to the new layering.

## Key Outcomes
- Files: under `src/` 45 modified, 27 deleted, 23 new paths (files and folders); plus skills, docs and KB.
- Build: green at every step close (fast profile, all 41 tests; per-step limit raised to 60 s by the owner).
- **Full suite:** pass — 13.7 s, 41 tests (recorded 2026-10-14T15:41:34.713Z; complete red on a fast-green tree: no; filtered: no)
- **Map delta:** none
- Behaviour proof: external golden comparison (never committed, `D:\temp\f39-golden`) — fresh base vs finished tree PASS on 609 requests (replies byte-identical, SQL count 6536 both sides, API document 0 differences); extended in fix round 1 with fixture variant D to 916 requests (SQL 11965 both sides) and five targeted mutants, all caught.
- Done check: Check 1 FAIL (one test slip — a raw `dotnet test --list-tests`), Check 2 PASS after fix cycle 1.
- Code review: Codex primary GO (4 LOW), second reader Sonnet APPROVED (3 LOW); 1 fix cycle of 3; follow-up review none due.

## Deviations from Plan
- Plan amended during the build by the architect: Q1 (read-only `git archive` base copies instead of a git worktree, D12), Q2 (`SprintRef` merges the identical sprint records, D13), Q3 (Step 12 rewrites the root `CLAUDE.md` Mapster line), Q4 (plural area folders `Sprints/`, `Developers/` to avoid the aggregate-name clash, D14).
- Beyond-plan changes, all scored Deviated-valid by the done check: a ninth identical record (`SprintSummaryItem`) merged into `SprintRef`; dead `QaWorkloadResponse` deleted; extra Shared/ consolidations in Step 7 (`TestCoverageCalculator`, `GetTransitionCompletedMemberships`); Domain `TestRunSummaryDto` renamed `TestRunSummary`; `ResolveStartIndex` merged in Step 4 instead of Step 7.
- Fix round 1 added `Ticket.GetEffectiveSp(int)` to the `Ticket` behaviour partial.

## Notes
- `.claude/verify.json` `stepLimitSeconds` is now 60 (owner decision, permanent); the slow-test first pass found no test at or over 2 s, and `.claude/test-times.json` was re-timed and pruned of the 33 old `…ServiceTests` keys at close.
- `graphify update .` was never run: graphify is not installed in this environment, so `graphify-out/` is stale until the owner runs it.
- Six analytics endpoints outside Step 9 (`GetScopeChange`, `GetDeveloperThroughput`, `GetLeaderboard`, `GetCycleTime`, `GetBugRatio`, `GetCarryOver`) keep `HandleAsync` methods of 85–95 lines — flagged by the developer, not in the plan's scope.
- Two skills outside F39's scope still mention Mapster (`add-integration-event/workflows/Consumer.md:71`, `create-grpc-contract/workflows/Server.md:39`) — a separate small fix.
- Rule list: not applicable — plan-only definition; nothing to promote.
- Operator check (plan Step 14) is owed by the owner.
- **Under the bar:** none — **Carry-over row:** none (no MEDIUM)
