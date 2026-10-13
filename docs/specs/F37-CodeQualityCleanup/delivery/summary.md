# F37 — Code Quality Cleanup — Summary

## Status: COMPLETE

## What Was Built
- The F36 lean campaign: the owner-triaged code-quality findings fixed across the Fokus service and client — Xray secret mask no longer round-trips and Re-detect keeps its guard; burnup bug area enters before it leaves; zombie flag counts full history; throughput rolling window looks past 0%-capacity sprints; transition lookups read a per-ticket index; missing sprint returns 404; no cross-query EF fix-up and read-only queries use `AsNoTracking`; load errors are shown instead of swallowed; auth cookie `Secure` outside Development (TD-1); front-end advisories cleared (TD-3); three KB entries updated.

## Key Outcomes
- 49 files modified, 6 created (2 production types, 4 test files: 22 new tests; 41 tests in the suite).
- Build: green after every step close (fast profile) and in both complete runs.
- **Full suite:** pass — 21.7 s (recorded 2026-10-13T19:10:34Z; complete red on a fast-green tree: no; filtered: no)
- **Map delta:** none
- Review: done check FAIL (4 test slips) → PASS after cycle 1; code review Codex primary NO-GO (3 MEDIUM, 1 LOW) + Sonnet second reader APPROVED (2 LOW); one fix round (cycle 1/3), follow-up review none due.
- Registry promotion: nothing to promote — plan-only definition, rule list not applicable.

## Deviations from Plan
- Step 5: removed the `SprintSummaryService.ComputeMetrics` case-insensitive grouping the plan listed as leave-as-is — it fed only the checker; results unchanged (done check: valid reason).
- Step 7: the accept grep still matches once in Carry-Over, Bug Ratio and Leaderboard (the Step 6 not-found line, which still feeds the active-sprint case) — accepted by owner ruling questions.md Q1.
- Step 8: excluded statuses are also locked while their load is running (beyond the plan's lock-after-failure).
- Fix Round 1: on the two QA endpoints the sprint-id check now runs before the Xray-disabled 200; the closed Step 6 block was annotated to match.

## Notes
- **Owed before merge:** Step 12 operator page check (Owner: operator) — every page these steps touch, the failure paths, Development sign-in over HTTP and the production `Secure` cookie; skip the active sprint on Carry-Over, Bug Ratio and Leaderboard (Q1).
- **Owed:** `graphify update .` — the graphify CLI is not installed on this machine, so the graph predates F37.
- Fixed, not re-verified (cycle 1, follow-up review none due): [MEDIUM] Users tab load vs mutation errors — `UsersTab.vue`; [MEDIUM] missing sprint id 404 with Xray off — `GetDeveloperQualityEndpoint.cs`, `GetQaWorkloadEndpoint.cs`; [LOW] `SelectRollingWindowSprintIds` returns `IReadOnlyList<int>`; [LOW] `ComputeSprintMetrics` transition lookup moved into the removed-membership branch.
- New backlog row GAP-5 records the active-sprint gap (Q1); TD-1 and TD-3 marked done.
- `GetZombieSprintCountsAsync` has no kept test and `Fokus.Tests` has no database harness — owner added backlog row TD-4.
- Lessons: recorded, not processed (owner: skip for now).
- Slow-test first pass: 41 tests timed, none at or over 2000 ms, nothing tagged (`.claude/test-times.json`).
- **Under the bar:** none — **Carry-over row:** none (no MEDIUM)
