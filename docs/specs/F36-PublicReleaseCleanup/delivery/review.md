# Review — F36-PublicReleaseCleanup (Phase 1)

## Step 2 — Code Review

**Verdict:** COMMENT
Primary: Codex — NO-GO, persisted by the architect (main session)
**Model:** codex

Full Codex output: [review-codex.md](review-codex.md). Privacy check: no inventoried identifier in 1,852 retained blobs across 61 reachable commits after the maps and exclusions; 21 redacted files replay byte for byte; nothing changed under `src/` or `client/src/`.

### [MEDIUM] Hook commands depend on the working directory
`.claude/settings.json:26` and `:36` — `node .claude/hooks/restore-agent.js` and `node .claude/hooks/audit-logger.js` fail with `MODULE_NOT_FOUND` when a session starts in a subfolder (e.g. `client/`). Fix: anchor both paths to the project root and quote them (`node "$CLAUDE_PROJECT_DIR/.claude/hooks/…"`).

### [MEDIUM] npm advisories asserted without retained output
`implementation.md:83` — six high-severity npm advisories are reported with no audit output kept. Fix: keep the `npm audit` report for the Phase 2 triage.

### [LOW] GAP spec statuses still Open
`docs/specs/GAP-1…3/definition/spec.md:5` — backlog says `Done (via I11)`. Fix in a documentation follow-up.

### [LOW] Plain mentions of excluded records
`adhoc-PipelineCraftImprovements/delivery/plan.md:3`, `docs/skill-backlog.md:68`, `docs/conventions/known-deviations.md:10` — historical mentions, not broken links. Fix: label them in Phase 5.

### [LOW] README lacks the local OAuth callback
`README.md:62` — document `http://localhost:5173/auth/callback`.

### [LOW] Product doc names .NET 9 / EF Core 9
`docs/product/v1.md:331-332` — update in the Phase 4 refresh.

*Status: COMPLETE — Codex, persisted by the architect, 2026-10-13*

## Step 2 — Fix list, cycle 1

Merged from: Codex (primary, [review-codex.md](review-codex.md)), Sonnet second reader ([review-second.md](review-second.md), APPROVED, 2 LOW + open questions), done check ([done-check.md](done-check.md), Check 1 PASS — no items). Round 1 carries every item.

**From the code review (Codex):**
1. [MEDIUM] `.claude/settings.json` hook commands are relative — anchor both to `"$CLAUDE_PROJECT_DIR/.claude/hooks/…"` (quoted) so they work from any subfolder. Keep the hooks byte-equal otherwise; update Step 1's record (the local file keeps its own copy).
2. [MEDIUM] Keep the npm advisory evidence: run `npm audit --json` in `client/` and save it to `docs/specs/F36-PublicReleaseCleanup/delivery/npm-audit.json`; cite package names and advisory ids in implementation.md.
3. [LOW] `docs/specs/GAP-1-excluded-statuses-ui/definition/spec.md`, `GAP-2-sprint-range-picker`, `GAP-3-sub-team-management-ui`: status → `Done (via I11)`, matching the backlog.
4. [LOW] Plain mentions of excluded records (`adhoc-PipelineCraftImprovements/delivery/plan.md:3`, `docs/skill-backlog.md:68`, `docs/conventions/known-deviations.md:10`): add `publish-only-map.txt` rules that append " (not included in the public release)" to each mention, so the public copy says why the record is missing and the private tree is untouched.
5. [LOW] `README.md`: name the Google OAuth redirect URI to register for local development (`http://localhost:5173/auth/callback`; and the backend one if the README's flow uses port 5000 directly — check `AuthConfiguration`).
6. [LOW] `docs/product/v1.md:331-332`: .NET 9 / EF Core 9 → .NET 10 / EF Core 10 (the code's target; check the csproj).

**From the second reader:**
7. [LOW] `implementation.md` Step 2 accept (2): the recorded "prints nothing" was a Bash backslash-collapse artefact — re-run the check from a script file and record the real result (the three prescribed `\bName\b` rules are context rules, not bare names; state so).
8. [LOW] `implementation.md:21`: "eleven" deletions → twelve.

**From the architect (owner away — decided, listed for the owner):**
9. [LOW] `redaction-map.txt`: employer wording "a former employer" → "an employer" (the owner's employment status is not stated anywhere; neutral is safe). Re-run Step 4 with `--from-ref` for the affected files and re-run the replay and idempotence checks.
10. [LOW] Copy the Step 4 driver script and the replay/history scripts from `D:\temp\f36` into `docs/specs/F36-PublicReleaseCleanup/delivery/scripts/` so the scrub stays re-runnable; record their usage in implementation.md.

Follow-up review: due — Codex re-reads (the round touches `apply-map` inputs and settings); no step is rebuilt, so no second reader.

## Step 2 — Re-review (cycle 1)

**Verdict:** REQUEST CHANGES
Primary: Codex — NO-GO, persisted by the architect (main session)
**Model:** codex

Full output: [review-codex.md](review-codex.md) § Re-review after fixes — cycle 1. Fixes confirmed: 1, 2, 3, 4, 6, 7, 9; refuted: 5; partial: 8, 10.

### [MEDIUM] OAuth callback documented on the wrong port
`README.md:62` — the Vite proxy sets `changeOrigin: true` and ASP.NET builds the callback from `Request.Host`, so the redirect URI Google sees is `http://localhost:5000/auth/callback`, not 5173.

### [MEDIUM] Retained Step 4 driver fails once private records are staged
`delivery/scripts/step4.py:55` — candidates include `.claude/verify.json` and `docs/skill-gaps/registry.md` (excluded, absent from `pre-public-cleanup`); `--from-ref` exits 1 on them.

### [MEDIUM] npm advisories unresolved (carry-over, confirmed)
`implementation.md:104` — six high-severity advisories (`@vue/server-renderer`, `vue`, `vite`, `postcss`, `nanoid`, `source-map-js`). Codex: does not block Phase 1; owner triage at Phase 2 (spec O2).

### [LOW] Superseded claims in the record
`implementation.md:21` still says eleven deletions; `:105` carry-over table still lists fixed items.

*Status: COMPLETE — Codex, persisted by the architect, 2026-10-13*

## Step 2 — Fix list, cycle 2

Round 2 carries the always-in items and the findings at MEDIUM and above. Done check: no open item.

1. [MEDIUM] `README.md`: register the callback the app actually generates. Confirm from `client/vite.config.ts` (proxy options) and `AuthConfiguration` (callback path) which origin Google receives; document that URI (expected `http://localhost:5000/auth/callback`; list both if both flows are real) and correct `implementation.md:66`.
2. [MEDIUM] `delivery/scripts/step4.py`: filter candidates through `exclusions.txt` before regeneration, and skip (with a printed note) any kept file absent from the reference ref instead of failing. Re-run it to prove it exits 0 with `.claude/verify.json` and `docs/skill-gaps/` present.
3. [MEDIUM] npm advisories — **deferred, not fixed in this round:** the spec puts them in the owner's Phase 2 triage (O2); they stay a carry-over row. No developer action.

Follow-up review: due — Codex re-reads items 1–2; no step rebuilt, no second reader.

**Under the bar (recorded, not fixed):**
- [LOW] `implementation.md:21` "eleven" → twelve; `:105` carry-over table lists items fixed in cycle 1.
- [LOW] Two public-copy annotations end in "…(not included in the public release))" (double bracket) — cosmetic, public copy only.

## Step 2 — Re-review (cycle 2)

**Verdict:** COMMENT
Primary: Codex — GO, persisted by the architect (main session)
**Model:** codex

Full output: [review-codex.md](review-codex.md) § Re-review after fixes — cycle 2. Both cycle-2 fixes confirmed (OAuth callback on port 5000; regeneration driver skips excluded and absent files).

### [MEDIUM] npm advisories (deferred — owner triage, spec O2)
`implementation.md:119` — six high-severity package advisories; accepted deferral, does not block.

### [LOW] Record still says eleven deletions; carry-over table lists fixed items
`implementation.md:21`, `:120-123` — recorded under the bar.

*Status: COMPLETE — Codex, persisted by the architect, 2026-10-13*
