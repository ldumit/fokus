# F37-CodeQualityCleanup — Review

## Step 2 — Code Review

**Verdict: COMMENT**
Primary: Codex — NO-GO, persisted by team-lead (session 80d534fb)
**Model:** codex
Record: `review-codex.md` (job task-mv71o2r5-ir60fx). Run record cited: verify-run complete, 2026-10-13T18:58Z, pass.

### [MEDIUM] Users tab load failures share `userMgmtError` with mutations
`client/src/components/settings/UsersTab.vue:39` — when users load but invitations fail, a role or Active change clears the error while `invitationsLoadFailed` stays true. Fix: separate load errors from mutation errors; clear each load error only after its own successful reload.

### [MEDIUM] Missing sprint id still returns 200 when Xray is disabled
`GetDeveloperQualityEndpoint.cs:31`, `GetQaWorkloadEndpoint.cs:30` — the new sprint-id checks run after the Xray-disabled return, so a nonexistent explicit id returns 200 when Xray is off, against Step 6's 404 requirement. Fix: validate an explicit id before that return; keep the empty 200 for valid ids and for requests without an id.

### [MEDIUM] Step 12 operator check outstanding (carry-over confirmed)
`implementation.md:189` — the plan makes Step 12 a merge gate. Fix: perform and record the page, failure-path and cookie checks before merge, with Q1's active-sprint exclusions.

### [LOW] Graph refresh not performed (carry-over confirmed)
`implementation.md:188` — `graphify update .` not run; CLI missing. Fix: main session runs it once the CLI is available.

Carry-over row "active-sprint membership gaps" — confirmed, excluded per questions.md Q1.

*Status: COMPLETE — Codex, persisted by team-lead, 2026-10-13*

## Step 2 — Fix list, cycle 1

**From the done check (done-check.md Check 1) — always in:**

| # | Level | Item | Source |
|---|-------|------|--------|
| 1 | always-in | Test slips: four raw test runs outside `verify-run.js` (violations.log lines 82–85, dev-s1, 17:14–17:22Z). Cleared when this round's window holds a `verify-run.js --profile fast` record and no new unblocked `test-entry` line — run every test through `verify-run.js`. No code change. | done-check.md Check 1 |

**From the code review (Codex primary):**

| # | Level | Item | Source |
|---|-------|------|--------|
| 2 | MEDIUM | Users tab: separate load errors from mutation errors (`UsersTab.vue:39`); clear each load error only on its own successful reload. | review-codex.md #1 |
| 3 | MEDIUM | Missing explicit sprint id must 404 even when Xray is disabled (`GetDeveloperQualityEndpoint.cs:31`, `GetQaWorkloadEndpoint.cs:30`); keep empty 200 for valid ids and no-id requests. Update the Step 6 block's check-order note. | review-codex.md #2 |

**Routed to the close, not to the developer** (not developer-actionable): Step 12 operator check (Codex #3, Owner: operator — merge gate, with Q1 exclusions); `graphify update .` (Codex #4, main session, CLI not installed).

**From the second reader (Sonnet, review-second.md — APPROVED):**

| # | Level | Item | Source |
|---|-------|------|--------|
| 4 | LOW | `SelectRollingWindowSprintIds` returns `List<int>`; `csharp.md` § Return Types asks for `IReadOnlyList<int>` (`DeveloperThroughputService.cs:192`). | review-second.md LOW 1 |
| 5 | LOW | `ComputeSprintMetrics` looks up `ticketTransitions` for every membership; only the removed-membership branch reads it — move the lookup into that branch (`ScopeChangeService.cs:324`). | review-second.md LOW 2 |

Duplicates merged: second reader's open question 1 (Xray-off 200 vs 404; `GetQaMetrics` already 404s) = row 3; open question 3 (UsersTab error cleared) = row 2. Open question 2 (no kept test for `GetZombieSprintCountsAsync`; no DB harness — plan scoped it out) stays an open question for the architect at close, not a fix row.

**Follow-up review:** none due — no row at HIGH or above, no missing or failed step, no skill slip. Rows 2–5 reported fixed by dev-fix-c1 (`implementation.md` § Fix Round 1; verify-run fast pass 19:07Z and 19:08Z) count as closed, fixed not re-verified. Row 1 (test slips) is the done check's: re-check owed.

**Under the bar:** none — round 1 carries every item.
