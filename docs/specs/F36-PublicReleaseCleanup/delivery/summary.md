# Public Release Cleanup — Phase 1 (Clean) — Summary

Mode: architect-led fast lane

## Status: COMPLETE

## What Was Built
- Phase 1 of F36: the private working tree is cleaned for a public release — private files removed, colleague names, the employer's Jira tenant and keys, machine paths and private project names redacted by one re-runnable map, the in-repo hooks moved into the committed settings, README and scripts fixed for a fresh clone, the frontend lockfile synced, and backlog gaps reconciled with the code.
- The scrub is defined once (`delivery/redaction-map.txt`, `publish-only-map.txt`, `exclusions.txt`, `apply-map.py`, `scripts/`) so Phase 5 can apply the same rules to every commit of the public history.

## Key Outcomes
- 43 tracked files changed against `pre-public-cleanup` (12 deleted, 31 modified), plus the F36 records and scrub tooling; nothing under `src/` or `client/src/` changed.
- Build, the 19 tests and the front-end build pass; `npm ci` now works on a fresh clone.
- **Full suite:** pass — 13.4 s (recorded 2026-10-13T00:39:09.716Z; complete red on a fast-green tree: no; filtered: no)
- **Map delta:** none
- Privacy: tree sweep 0 hits; history replay 70 → 0 hits over 113,700 lines; Codex found no inventoried identifier in 1,852 retained historical blobs after the maps and exclusions.
- Reviews: done check PASS (Check 1); code review Codex primary + Sonnet second reader — 2 fix cycles, final Codex verdict GO.
- Decisions taken with the owner away (all reversible): verify roles declared (`.claude/verify.json`, step limit 30 s); `Reflekt`, `SprintRituals` and "Agentic Rituals Suite" genericized; employer wording "an employer"; the owner's own name kept.

## Deviations from Plan
- Steps 1 and 3: the developer made no git writes (lane rule); untracking `.claude/settings.local.json` and staging the 12 deletions were done at close.
- Step 4: `.claude/settings.local.json` left out of the redaction run (stays untouched on disk).
- Step 5 extended at plan time: the out-of-sync lockfile found while declaring verify roles was added to the step.

## Notes
- **OPERATOR ACTION REQUIRED — page check (plan Step 8, spec D7):** with a synced sprint, open Dashboard, Developers, Team, Sprints, Epics, QA, Cycle Time and Settings and record pass/fail in `implementation.md` § Operator check. Phase 1 stays on branch `F36-PublicReleaseCleanup`, unmerged, until every page passes.
- Owner items still open in the spec: O1 (which feature records stay), O2 (campaign scope, incl. TD-3), O3 (author email in the public history), O4 (public repo name), O5 (old snapshots that do not build).
- **Under the bar:**
  - [LOW] Record says eleven deletions (twelve) and its carry-over table lists items fixed later — implementation.md:21, :120-123 (Codex re-reviews)
  - [LOW] Two public-copy annotations end in a double bracket "…release))" — publish-only-map.txt (fix list cycle 2)
  — **Carry-over row:** TD-3 (the npm advisories, an in-bar MEDIUM deferred to owner triage)
