# Public Release Cleanup — Phase 1 (Clean) — Done Check

## Step 1 — Done-Check

### Check 1 — 2026-10-13 — after the build

**Model:** claude-opus-5-5

Pre-commitment predictions: (1) the six step-edited files skip the redaction pass or drift from the replay; (2) the history replay leaves hits the tree sweep cannot see; (3) the hooks move carries extra keys (`env`, `permissions.allow`) or loses the owner's `code-forge` removal. None held — each was re-run independently below.

| Step | Disposition | Notes |
|------|-------------|-------|
| 1 — Ignore rules, untrack local settings, move the hooks | Deviated (valid reason) | `.gitignore` 441–444 add the four entries (`check-ignore --no-index` re-run: all four match). Hooks JSON equality exit 0; no `env`/`permissions.allow` exit 0; `code-forge` count 0; local file on disk. `git rm --cached .claude/settings.local.json` and its `ls-files` / plain `check-ignore` accepts are close-owned (lane: no developer git writes) — open for the close. |
| 2 — The scrub definition | Implemented | Re-run: fixture shows lines 1, 143, 152 rewritten, `a sibling projectService\|a sibling project-` count 0; bare-name rule grep empty; all 44 map lines are `regex:`; historical names 67 checked / 0 missed against `exclusions.txt`. Path rules placed first (documented reason: `sprint-rituals` would pre-empt its path rule) — the plan's relative orders still hold. |
| 3 — Remove the private files (A4) | Deviated (valid reason) | All seven paths (12 tracked files) deleted on disk, `git status` ` D`; link `git grep` empty (exit 1). Staging the removals and the `git ls-files` accept are close-owned. |
| 4 — Apply the privacy map to the working tree | Deviated (valid reason) | Re-run: replay 21 files / 0 mismatches; idempotence over every changed non-protected file / 0 changed. `.claude/settings.local.json` left out of the run on the coordinator's instruction — it is in `exclusions.txt` and leaves the index at close, so the public tree is unaffected. |
| 5 — README, scripts, lockfile | Implemented | Re-run: `github.com/ldumit` 0; `20.19` 1; `appsettings.Development.json` lines 56/58/61 all name `src/Services/Fokus/Fokus.API/`; `sample data` 1; `client/package.json` unchanged (exit 0); `overnight-pipeline.ps1` header names F30, F31, F27; `deploy.ps1:8` points at the repo-root example file. |
| 6 — Reconcile backlog statuses | Implemented | `docs/backlog.md:304-306` GAP-1..3 `Done (via I11)`, GAP-4 untouched; file:line evidence per verdict in implementation.md. |
| 7 — Safety net, sweeps, history replay | Implemented | Re-run: tree deny sweep empty (also with `.claude/settings.local.json` included); `src`/`client/src` unchanged; history replay (`D:\temp\f36\replay.py`, through the shipped `apply-map.py`) 113,700 lines, 70 hits before, 0 after. Fast-profile `verify-run` records in the window all `pass`; complete run `pass` at 23:36:12Z. |
| 8 — Owner's page check (Owner: operator) | N/A (operator-owned) | `OPERATOR ACTION REQUIRED: page check (spec D7)` written under `## Operator check`. **Open:** per the plan (P2-05) Phase 1 stays on its branch, unmerged, and its backlog row is not marked Done until the owner records every page passing. |

**Rules:** 0 listed behaviour rules without a disposition · 0 `tested` rules whose named test does not exist (no rule list — plan header: not applicable)

Notes:
- Skill log: every plan step maps `Skill: None` (all-`None` exemption) — no skill Fail possible; the developer's one logged call (`nexus:review-format`, 23:25:16Z, Fix Round 0 self-review) matches its `## Skills Used`. `## Self-Review` present with a verdict line (architect-led lane check).
- Test-entry log: no `rule: "test-entry"` line in the window, in the one log home (`D:/src/fokus/.claude/audit/`, the main checkout).
- `Satisfies:` referents all resolve — spec A1–A6, D5, D7, D8, O2; `R1-xx` are the spec-review finding ids in `review-critic.md` § Round 1.
- Plan hygiene: `## Decisions` present, eight rows.
- Process observations, not slips (they Fail nothing here): `violations.log` holds five `approval-scope` writes to run-only scripts in `D:\temp\f36\` (outside the repo), two same-round re-reads of `overnight-pipeline.ps1`, and one `general-purpose` helper flagged for a git write — `git init`/`commit` inside a throwaway probe repo at `D:\temp\f36-probe`, not this repository.
- For the close: stage the 12 deletions and `git rm --cached .claude/settings.local.json`, then re-run `git check-ignore .claude/audit/x .claude/settings.local.json` (both paths) and `git ls-files` (none of the Step 1/3 paths). Merge-at-close should be declined while Step 8 is open.

**Verdict: PASS**

*Status: COMPLETE — architect, 2026-10-13*
