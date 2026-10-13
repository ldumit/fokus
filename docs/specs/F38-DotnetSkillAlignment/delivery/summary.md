# F38 — Dotnet Skill Alignment — Summary

Mode: architect-led fast lane (plan only)

## Status: COMPLETE

## What Was Built
- The eight nexus-dotnet skills rated 6+ with S/S–M effort are now self-contained local copies in `.claude/skills/`: 3 new, 4 merged, 1 replaced. The code was then aligned from those copies:
  - central package versions, with stable .NET 10.0.13 and FastEndpoints 8.3;
  - required lookups throw, and one middleware writes every error reply;
  - the `Send.*` API;
  - an access-role vocabulary and one admin policy;
  - one type per endpoint file;
  - start-up work as start-up lines, and a settings read that only reads;
  - an accurate service CLAUDE.md, with the convention and doc echoes swept.

## Key Outcomes
- **Files:** about 150 changed or added across `src/`, `.claude/skills/`, `.claude/README.md`, `docs/conventions/`, `docs/architecture/v1.md`, `docs/kb/glossary.md` and the two module/service CLAUDE.md files. No migration.
- **Build:** green.
- **Full suite:** pass — 7.5 s (recorded 2026-10-13T21:17:16.412Z; complete red on a fast-green tree: not measured; filtered: no).
- **Map delta:** none.
- **Reviews:**
  - Plan review: 2 rounds (Codex + Sonnet, then Codex on the fold), 26 findings folded, none open.
  - Done check: PASS, 9 of 9 steps, 0 process slips.
  - Code review: Codex GO (5 LOW) and Sonnet APPROVED (2 LOW). One fix round (cycle 1 of 3) fixed all 6 LOW items it carried; none needed a follow-up review.
- **Wire contract:** 56 of 56 operations have identical request and success-reply property names; the `api-before.json` / `api-after.json` diff is empty.
- **Admin gates:** the 23 gated endpoints are the same set as before. The policy was enforced on FastEndpoints 8.3 in a throwaway host: anonymous 401, Manager 403, Admin 200.

## Deviations from Plan
- Owner-away decisions taken during the build: Q1–Q3 in `questions.md` (the target state in the skill copies; `AccessPolicies.Admins`; identity stamping deferred as Decision D13). Plan Decisions D13 and D14 were added.
- A build-time bug from Step 3 (`UpdateTeamConfig` answered 500 for a missing developer) was found and fixed by the Step 5–9 developer before Step 8.
- The plan's approved error-body row was widened at review to name the formerly empty 404s, which now carry a body.

## Notes
- **Operator check (plan Step 10) is still owed by the owner** before merge: an existing database starts cleanly, the page sweep, manager 403 and `/api/users` 403 from the browser console, developer-detail 404s, the cycle-time boundary 400, and Xray credentials. The suite cannot reach any of these.
- **Error replies changed body, not status:** the `SaveCycleTimeBoundaries` 400 drops the `errors` list; the inactive-user 401 gains `traceId`/`details`; the formerly empty 404s now carry the middleware envelope with a message. The middleware logs these at Error level, as it already did; whether client errors should log at Warning is open (outside F38).
- **The development API document no longer lists the `["Admin"]` scope** on the 23 admin operations: FastEndpoints documents role names, not policy names. Enforcement is unchanged.
- **Step closes cut at the 30 s step limit:** two of them, both covered by the complete runs. A full build now takes about 31 s, so `stepLimitSeconds: 30` in `.claude/verify.json` is now tight.
- **Two endpoint-naming styles remain:** folders untouched by Step 6 keep `{Op}Request` names.
- **For F36's known-open-work page:**
  - held on their current major by owner decision: Refit 8 → 16, Microsoft.NET.Test.Sdk 17 → 18, xunit.runner.visualstudio 2 → 4, NSubstitute 5 → 6;
  - validator message literals → § Validation Messages helpers (D12);
  - an identity-stamp pre-processor (D13);
  - the 4xx log level.
- **Not part of F38, left uncommitted:** `.claude/settings.json` was changed in the working tree before this build. The change removes the permission deny list and enables bypass-permissions mode. The owner should confirm it was intended.
- **Fixed, not re-verified:** the 6 LOW doc items of fix round 1 (`review.md` § Fix list, cycle 1). No reader re-read them; the close's complete run covers the tree.
- `graphify update .` was run at close.
- **Under the bar:** none — **Carry-over row:** none (no MEDIUM)
