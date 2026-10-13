# F38 — Dotnet Skill Alignment — Review

## Step 2 — Code Review

**Verdict: COMMENT**
Primary: Codex — GO, persisted by the architect session (architect-led fast lane)
**Model:** codex

Run record: complete run, pass, 2026-10-13T21:00:22.856Z.

### [LOW] persistence-patterns copy misdescribes table naming
`.claude/skills/persistence-patterns/SKILL.md:173` — table names follow the `DbSet` names (`Sprints`, `Tickets`, `AppUsers`), not singular entity names. Fix: describe the actual convention.

### [LOW] add-hosted-service copy overstates start-up options validation
`.claude/skills/add-hosted-service/SKILL.md:17` — Jira validates its options at start-up; Xray only binds optional options. Fix: distinguish the two.

### [LOW] Service CLAUDE.md says every other endpoint takes authentication
`src/Services/Fokus/CLAUDE.md:42` — anonymous sign-in and invitation validation are omitted. Fix: name both exceptions.

### [LOW] Stale "thin domain" line
`src/Services/Fokus/CLAUDE.md:39` — Auth behaviour methods exist. Fix: correct in F36's refresh (carry-over).

### [LOW] Plan's approved error-body changes omit the 404 bodies
`docs/specs/F38-DotnetSkillAlignment/delivery/plan.md:298` — formerly empty 404s now carry the middleware envelope. Fix: name them among the approved changes.

*Status: COMPLETE — Codex, persisted by the architect session, 2026-10-13*

## Step 2 — Second reader

Sonnet second reader: APPROVED — `review-second.md` (2 LOW, 3 open questions; FastEndpoints 8.3 policy enforcement and pre-processor exception mapping proven on a throwaway host).

## Step 2 — Done check

PASS — `done-check.md` § Check 1 (9 of 9 steps, 0 process slips).

## Step 2 — Fix list, cycle 1

Merged from Codex (5 LOW), the Sonnet second reader (2 LOW) and the done check (notes only). Round 1 carries every item.

**Codex**
1. [LOW] `.claude/skills/persistence-patterns/SKILL.md:173` — table names follow the `DbSet` names (`Sprints`, `Tickets`, `AppUsers`); describe the real convention.
2. [LOW] `.claude/skills/add-hosted-service/SKILL.md:17` — say Jira validates its options at start-up while Xray only binds optional options.
3. [LOW] `src/Services/Fokus/CLAUDE.md:42` — name the anonymous endpoints (`Auth/Login`, `Auth/ValidateInvitation`) beside "authentication only" (the done check raised the same).
4. [LOW] `src/Services/Fokus/CLAUDE.md:39` — the "thin domain, no behaviour methods" line is false (Auth behaviours exist); correct it now rather than carry it.

**Sonnet second reader**
5. [LOW] `.claude/skills/authorization-patterns/SKILL.md:41` and `.claude/skills/persistence-patterns/SKILL.md:18,85,89,96,141,161` — "the plugin form" names something this repo does not have; say "the general form" (or "the skill's general pattern").
6. [LOW] `.claude/skills/create-feature/CHANGELOG.md` — add a line for the Step 9 edit to `workflows/EndpointFastEndpoints.md` (named policy, `Send.*`).

Not sent (coordinator's): the plan's approved-error-body row now names the 404 bodies (Codex LOW 5, fixed in plan.md by the architect); the Admin scope no longer listed in the development API document goes to `summary.md` Notes (Sonnet LOW 2 — enforcement unchanged).

**Follow-up review:** none due — every item is LOW; the developer's own fast record and the close's complete run are the evidence.
