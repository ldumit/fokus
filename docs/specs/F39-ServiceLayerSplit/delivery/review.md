# F39-ServiceLayerSplit — Review

## Step 2 — Code Review

**Verdict: COMMENT**

Primary: Codex — GO, persisted by team-lead (session e2672549)
**Model:** codex
Run record: complete-suite record 2026-10-14T08:21:55Z, verdict pass (tree_hash 3c4aeadf…). Source: `review-codex.md`.

### [LOW] Stale timing keys for renamed test classes
`.claude/test-times.json:8` — 33 timing keys reference the deleted `…ServiceTests` classes; no keys exist for the renamed classes. Fix: regenerate timings for the renamed tests and remove obsolete keys during close.

### [LOW] Code graph not refreshed
`graphify-out/GRAPH_REPORT.md:14` — the graph still references commit `1fad3ecb` and moved service types. Fix: run `graphify update .` where graphify is installed.

### [LOW] Duplicate effective-story-points helper in the repository
`src/Services/Fokus/Fokus.Persistence/Repositories/TestExecutionRepository.cs:274` — `GetEffectiveSp` duplicates `TestCoverageCalculator` and the existing `SprintMembership.GetEffectiveSp` behaviour. Fix: have both callers use the existing membership method.

### [LOW] Duplicate effective-ticket-story-points helper across two calculators
`src/Services/Fokus/Fokus.Domain/Analytics/Sprints/SprintSummaryCalculator.cs:471` — `GetEffectiveTicketSp` duplicates the private implementation in `EpicProgressCalculator`. Fix: consolidate the identical rule into one shared Domain helper.

Carry-over refuted: `docs/architecture/v1.md:73` — the delta sentence remains a valid request-level requirement; no fix required.

*Status: COMPLETE — Codex primary, persisted by team-lead, 2026-10-14*

## Step 2 — Fix list, cycle 1

**Done check — Check 1 (`done-check.md`)**

1. [always-in] Test slip — a raw `dotnet test … --list-tests` ran at Step 11 (`violations.log:92`). Run `verify-run.js --profile fast` once inside this fix window; never a raw test command.

**Code review — Codex primary (`review-codex.md`)**

2. [LOW] `TestExecutionRepository.cs:274` — remove the private `GetEffectiveSp`; both callers use the existing `SprintMembership.GetEffectiveSp` (and `TestCoverageCalculator` likewise where it holds the same rule). Prove the replies unchanged: fresh capture, `compare.sh base-1 <capture>` in `D:\temp\f39-golden`.
3. [LOW] `SprintSummaryCalculator.cs:471` / `EpicProgressCalculator` — merge the two identical private `GetEffectiveTicketSp` into one shared Domain helper (or `SprintMembership.GetEffectiveSp` where it is the same rule). Same golden proof as row 2.

**Code review — second reader, Sonnet (`review-second.md`)** — its finding 2 duplicates rows 2–3 (adds `TestCoverageCalculator.cs:161`, already covered by row 2)

4. [LOW] `.claude/skills/create-feature/SKILL.md:62` — says "No `_Shared` folders yet", but Step 8 created `Fokus.API/Features/Analytics/_Shared/`. Update the line to describe the `_Shared` folder for reply shapes several operations use.
5. [LOW] Two same-shaped DTOs both named `SparklinePointDto` — the shared record in `Features/Analytics/_Shared/MetricCardDto.cs:15` and the class in `GetQaWorkloadQuery.cs:110`. Use the shared one in `GetQaWorkload` and delete the local copy; the API document must stay unchanged (golden proof as row 2).

**Owner decision (2026-10-14) on the second reader's harness-gap offer**

6. [owner] Extend the external golden harness in `D:\temp\f39-golden` (never committed) so the comparison exercises the five paths only read so far (`review-second.md` § gap): a `last=0` request on qa-trends; at least 12 closed QA sprints (the `DeveloperQualityInputs` 12-sprint streak cap); an excluded developer who owns a ticket (`SubTeamFilter.FilterMemberships`); a QA run set where the pass-rate guard sees exactly one run; the "no activity" 404 in `GetDeveloperDetailEndpoint`. The base extracts were removed at Step 13: re-extract the base read-only with `git archive 5fe66f8` (Q1/D12 route), capture a new base and the current tree on the same UTC day, and `compare.sh` them → PASS. Then remove the extract again.

**Follow-up review:** none due — every leveled row is LOW; no step was built or redone. Re-check due (row 1 is a done-check item).

Under the bar: none (round 1 carries every item).

**Owned by the close, not sent to the developer**

- Stale `.claude/test-times.json` keys — the team lead's slow-test delta pass at close re-times the renamed tests and removes the old keys.
- `graphify update .` — graphify is not installed in this environment; recorded for the owner.
