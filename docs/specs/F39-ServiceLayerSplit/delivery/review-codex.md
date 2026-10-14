# F39-ServiceLayerSplit — Codex code review

**Model:** codex
Persisted by team-lead (session e2672549), job task-mv7udj84-x61ufs, label "F39-ServiceLayerSplit code-review round 1", 2026-10-14.

## Verdict: GO

No blocking defects found. Accepted the supplied complete-suite pass and Step 13 golden comparison. Review remained read-only.

- **LOW — Confirmed — [.claude/test-times.json:8](/D:/src/fokus/.claude/test-times.json:8):** 33 timing keys reference deleted `…ServiceTests` classes; no renamed keys exist. **Fix:** regenerate timings for the renamed tests and remove obsolete keys during close.

- **LOW — Confirmed — [GRAPH_REPORT.md:14](/D:/src/fokus/graphify-out/GRAPH_REPORT.md:14):** The graph still references commit `1fad3ecb` and moved service types. **Fix:** run `graphify update .` where graphify is installed.

- **LOW — Confirmed — [TestExecutionRepository.cs:274](/D:/src/fokus/src/Services/Fokus/Fokus.Persistence/Repositories/TestExecutionRepository.cs:274):** `GetEffectiveSp` duplicates `TestCoverageCalculator` and the existing `SprintMembership.GetEffectiveSp` behavior. **Fix:** have both callers use the existing membership method.

- **LOW — Confirmed — [SprintSummaryCalculator.cs:471](/D:/src/fokus/src/Services/Fokus/Fokus.Domain/Analytics/Sprints/SprintSummaryCalculator.cs:471):** `GetEffectiveTicketSp` duplicates the private implementation in `EpicProgressCalculator`. **Fix:** consolidate the identical rule into one shared Domain helper.

**Carry-over refuted — [v1.md:73](/D:/src/fokus/docs/architecture/v1.md:73):** The delta sentence remains a valid request-level requirement: endpoints obtain current values and deltas through calculators. No fix is required.
