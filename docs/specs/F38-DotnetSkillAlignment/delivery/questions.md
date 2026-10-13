# F38 — Dotnet Skill Alignment — Questions

## Q1: Current state or target state in the Step 1 copies
**From:** developer
**To:** architect
**Status:** Answered
**Step:** Step 1 — Bring the eight skills into `.claude/skills/`
**File:** `.claude/skills/{persistence-patterns,authorization-patterns,extract-endpoint-types,error-handling,central-package-management,framework-currency}/SKILL.md`

**Context:** Step 1 is built before Steps 2–7, but several facts it must write only become true later:
- `persistence-patterns`: "seeding as it stands after Step 7" — today `EntityConfiguration.cs:22,40` still calls `SeedFromJsonFile()` and `Program.cs:17-44` seeds `AppSettings` inline; Step 9's accept also forbids `SeedFromJsonFile` anywhere in `.claude/` outside `framework-currency`.
- `authorization-patterns`: the `AccessRole`/`AccessRoles` vocabulary and the one admin policy are Step 5's; today the 23 endpoints carry `Roles = "Admin"` and `AuthConfiguration.cs:14` is a bare `AddAuthorization()`.
- `extract-endpoint-types`: examples "re-pointed at folders that exist after Step 6".
- Every "Look First" block names `src/Directory.Packages.props`, which Step 2 creates.
- Step 1's accept asks for "one line per fact in `implementation.md`, naming the live file that confirms it" — a target-state fact has no confirming live file yet.

**Question:** Should the merged copies describe the target state the plan decides (so Steps 2–7 read their skill as the shape to build), or today's state (and then which step updates each copy, since no Step 2–7 file list names a skill file)?

**Recommendation:**
Target state (Recommended) — Each fact the plan decides is written as this repo's form now; in `implementation.md` each such fact line reads `target — lands in Step N` instead of naming a live file, and the done-check re-verifies them after Step 7. Facts that already hold name their live file as asked.
Today's state — The copies stay true at every moment, but Steps 3, 5 and 7 then each need a skill-file edit the plan does not list, and Step 9's sweep must catch `SeedFromJsonFile` in `persistence-patterns`.
**Confidence:** medium — the step's own wording ("as it stands after Step 7", "folders that exist after Step 6") points to target state, but the live-file accept points the other way.

### Answer
Target state. The copies describe the shape the plan decides, so Steps 2–7 read their local skill as the form to build. A fact that already holds names its live file; a decided fact's line in `implementation.md` reads `target — lands in Step N`. The done-check re-verifies every `target` line against the code after the last step. Step 9's `SeedFromJsonFile` sweep stays as written: the copy names the removed hook only if it explains why it is gone, and that mention is whitelisted in `implementation.md`. (architect, 2026-10-13)

## Q2: Name and home of the admin policy constant
**From:** developer
**To:** architect
**Status:** Answered
**Step:** Step 1 (the `authorization-patterns` copy shows the form) and Step 5 (builds it)
**File:** `src/Services/Fokus/Fokus.API/Auth/AuthConfiguration.cs`; the 23 endpoint files

**Context:** Step 5 binds the vocabulary names (`AccessRole`, `AccessRoles`, in `Fokus.Domain/Auth/`) because of name clashes, but leaves the policy constant unnamed. The plugin's form is a static class `Policy` with `nameof` constants, used as `[Authorize(Policy = Policy.Clerks)]`. If Q1 is answered "target state", the Step 1 copy writes the name, and the Step 5 developer must use the same one.

**Question:** What is the admin policy constant called, and which project holds it?

**Recommendation:**
`AccessPolicies.Admins` in `Fokus.API/Auth/AccessPolicies.cs` (Recommended) — Matches the `AccessRoles` naming the plan already chose; sits beside `AuthConfiguration.cs`, where it is registered; avoids a type named `Policy` next to `AuthorizeAttribute.Policy` in every attribute.
`Policy.Admins` in `Fokus.API/Auth/Policy.cs` — The plugin's exact name; reads `[Authorize(Policy = Policy.Admins)]`, which works but is easy to misread.
Leave it unnamed in the Step 1 copy — The copy says "one named policy" without a name; Step 5 picks it and Step 9's sweep updates the copy.
**Confidence:** medium — the name is a style choice, but the copy and Step 5 must agree, and the plan fixes neither.

### Answer
`AccessPolicies.Admins`, in `src/Services/Fokus/Fokus.API/Auth/AccessPolicies.cs`, a static class of `nameof` constants. The name is binding; plan Step 5 now says so. (architect, 2026-10-13)

## Q3: Identity stamping — carve-out or open work
**From:** developer
**To:** architect
**Status:** Answered
**Step:** Step 1 — the `authorization-patterns` copy
**File:** `.claude/skills/authorization-patterns/SKILL.md`

**Context:** The plugin's binding rule 4 and Phase 4 say the acting user is stamped onto the command by one framework hook (`StampActionPreProcessor` for FastEndpoints) and endpoints never read the caller. Fokus has no stamp hook: `Auth/CreateInvitation/CreateInvitationEndpoint.cs:37` and `Auth/GetMe/GetMeEndpoint.cs:13` read `ClaimTypes.NameIdentifier` from `HttpContext.User` directly. The plan's carve-out list (JWT, vocabulary, role-only admin policy, admin-only reads) does not cover this departure, and "the plugin rules this repo departs from are named and carved out in that section".

**Question:** Does the copy carve Phase 4 out for this repo, or name it as open work that is not adopted yet?

**Recommendation:**
Name it as open work (Recommended) — The copy keeps Phase 4 as the rule, states that this repo has no stamp hook yet and which two endpoints read the caller, and the item joins D12 in `summary.md` Notes for F36's known-open-work page. No code change in F38.
Carve it out — The copy says the rule does not apply here; students read the direct claim read as an accepted form.
**Confidence:** medium — open work keeps the taught rule intact, which suits the release goal, but it adds one item to the hand-over list the architect owns.

### Answer
Name it as open work. The copy keeps Phase 4 as the rule and states that this repo has no stamp hook yet and that `CreateInvitationEndpoint.cs` and `GetMeEndpoint.cs` read the caller from the claims. No code change in F38. The item joins the open-work list in plan § Scope (Decision D13). (architect, 2026-10-13)
