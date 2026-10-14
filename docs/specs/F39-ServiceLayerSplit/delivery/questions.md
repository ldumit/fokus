# F39 — Service Layer Split — Questions

## Q1: Base snapshot without a git worktree
**From:** developer
**To:** architect
**Status:** Answered
**Step:** Step 2 — The golden harness and the base capture (also Step 13's cleanup)
**File:** none yet (harness lives in `D:\temp\f39-golden\`)

**Context:** The dispatch carries the owner constraint "No git writes of any kind". Step 2 creates the base tree with `git worktree add` (writes `.git/worktrees/` and a ref-locked checkout), Step 2's sabotage check proves the revert with `git -C "D:/temp/f39-golden/base" status --porcelain`, and Step 13 removes it with `git worktree remove`. All three need a worktree, so they conflict with the constraint.
A read-only route gives the same frozen tree: `git archive 5fe66f8 | tar -x -C "D:/temp/f39-golden/base"` reads objects only (5fe66f8 = HEAD now, no `src/` edit yet — `git status` shows none under `src/`). The sabotage revert is then proven by `diff -r --exclude=bin --exclude=obj "D:/temp/f39-golden/base/src" "D:/temp/f39-golden/pristine/src"` against a second, untouched extract (or `git archive 5fe66f8 -- {file} | tar -xO` per edited file compared with `cmp`). Step 13's cleanup becomes `rm -rf "D:/temp/f39-golden/base" "D:/temp/f39-golden/pristine"`.

**Question:** May Steps 2 and 13 use a `git archive` extract plus a pristine second extract in place of `git worktree add`/`status`/`remove`?

**Recommendation:**
Use `git archive` extracts (Recommended) — Same frozen base commit and same comparison method, with no git write; the revert check becomes a `diff -r` against the pristine extract.
Keep `git worktree` — Needs the owner to lift "no git writes" for the worktree commands only.
**Confidence:** high — the archive is byte-for-byte the committed tree, and the plan only uses the worktree as a frozen copy plus a cleanliness check, both of which the extract pair gives.

### Answer
**architect, 2026-10-14 — Yes: use the `git archive` extract pair.** Confidence: high — the worktree was only ever a frozen copy plus a cleanliness check, and "no git writes" is an owner constraint the plan must not breach.

- **Verified:** `git archive 5fe66f8 -- CLAUDE.md | tar -xf - -C {scratch}` gives a file `cmp`-identical to the checkout (the `core.autocrlf=true` conversion is applied, so the extract matches the checkout's line endings). No `.gitattributes`; no git-dependent build step (no SourceLink/Nerdbank/GitVersion) in any `.props`/`.csproj`, so the extract builds without a `.git` folder.
- **Form, in Git Bash:**
  - before extracting, `git status --porcelain -- src Directory.Packages.props` is empty (the extract is the committed tree only);
  - `git archive {base} | tar -xf - -C "/d/temp/f39-golden/base"` and the same into `/d/temp/f39-golden/pristine` — use the `/d/...` path form: GNU tar can read a `D:` prefix as a remote host;
  - the pristine extract is never built and never edited.
- **Sabotage revert check:** `diff -r --exclude=bin --exclude=obj "/d/temp/f39-golden/base" "/d/temp/f39-golden/pristine"` exits 0. If the build writes anything else into the tree, add that exclusion and name it in `implementation.md` (build output only).
- **One added proof:** the extract must behave as the checkout does. The determinism check's captures now include one from the main tree before any F39 source edit (main = base then); it must equal the two extract captures.
- **Step 13:** the fresh base capture runs in the extract; cleanup is `rm -rf "/d/temp/f39-golden/base" "/d/temp/f39-golden/pristine"`.
- **Plan amended (2026-10-14):** Step 2 (Base, Harness, Determinism, Sabotage), Step 13 (run, cleanup), Decision D12.

## Q2: The shared sprint-info record — which one, and its name
**From:** developer
**To:** architect
**Status:** Answered
**Step:** Step 4 — The shared kernel's independent parts
**File:** `src/Services/Fokus/Fokus.API/Features/Analytics/*Service.cs`

**Context:** Step 4 moves "the sprint-info record" to `Fokus.Domain/Analytics/Shared/`. The tree holds eight byte-identical 4-field records — `BugRatioSprintInfo`, `CarryOverSprintInfo`, `CycleTimeSprintInfo`, `DeveloperProgressSprintInfo`, `DeveloperQualitySprintInfo`, `LeaderboardSprintInfo`, `QaTrendsSprintInfo`, `ScopeChangeSprintInfo`, each `(int Id, string Name, DateTime StartDate, DateTime EndDate)` (§ Context says nine; grep finds eight) — plus `SprintSummaryService.cs:5` `SprintInfo` with two more fields (`DurationDays`, `SyncedAt`).
Merging the eight is D6-legal (identical), keeps the JSON bytes, and only changes swagger schema type names (allowed by the F38 method). The name `SprintInfo` is taken by the 6-field record, which lives in the API namespace that `Fokus.Tests/GlobalUsings.cs` imports globally, so reusing it would create an ambiguous reference until Step 5. The glossary has no term for this shape.

**Question:** Should Step 4 merge the eight 4-field records into one Domain record, and under what name, leaving the 6-field `SprintInfo` for Step 5 with `SprintSummary`?

**Recommendation:**
Merge the eight into one `SprintRef` in `Fokus.Domain.Analytics.Shared`, add a glossary line; `SprintInfo` (6 fields) moves with SprintSummary in Step 5 (Recommended) — One shared type, no name clash, wire unchanged.
Merge the eight under another name the architect picks — Same effect; only the type name differs.
Move nothing in Step 4; keep the eight per service until Step 7 — Smaller Step 4, but Step 7's helper-family consolidation then also owns the records.
**Confidence:** medium — the merge itself is clearly D6-legal; the name is a naming call with no glossary basis.

### Answer
**architect, 2026-10-14 — Yes: merge the eight into one `SprintRef` in `Fokus.Domain/Analytics/Shared/`, add a glossary line; the 6-field `SprintInfo` moves with `SprintSummary` in Step 5.** Confidence: high on the merge, medium on the name — no glossary term exists, and `SprintRef` says what the record is (a sprint's identity and dates carried by a result, not the aggregate).

- **Verified:** eight records, all `(int Id, string Name, DateTime StartDate, DateTime EndDate)`, declared at line 5 of `BugRatio`, `CarryOver`, `CycleTime`, `DeveloperProgress`, `DeveloperQuality`, `Leaderboard`, `QaTrends` and `ScopeChange` `*Service.cs`; `DeveloperDetailService.cs:33,300` uses `DeveloperProgressSprintInfo`. No test source names any of them. § Context's "nine" was wrong — corrected to eight.
- **Why the merge is safe:** same members in the same order, so the JSON bytes are unchanged; only the API-document schema type name changes, which the F38 method allows. After Step 8 the endpoints own their reply types, so `SprintRef` ends up off the wire anyway.
- **Glossary line** (`docs/kb/glossary.md`, beside **Sprint**): "**Sprint Reference** — A sprint's id, name, start and end dates, carried inside an analytics result. Code: `SprintRef`. Not the `Sprint` aggregate."
- **Not chosen:** reusing `SprintInfo` (the clash you found until Step 5); deferring to Step 7 (the records have no coupling, so they belong with Step 4's independent parts).
- **Plan amended (2026-10-14):** § Context (eight), Step 4 (the record, the glossary line, one accept grep), Decision D13.

## Q3: The root CLAUDE.md Mapster line — should Step 12 touch it?
**From:** developer (carry-over, relayed by the team lead)
**To:** architect
**Status:** Answered
**Step:** Step 12 — Rules, the architecture doc, conventions, skills and KB (also Step 1's accept)
**File:** `CLAUDE.md:44`

**Context:** The root `CLAUDE.md` Tech Stack still lists "- Mapster — object mapping" (line 44). Step 1 states Fokus has no Mapster use and drops the local Mapster workflow, but Step 12 does not touch that line.

**Question:** Should Step 12 rewrite the Tech Stack Mapster line?

**Recommendation:** Yes, in Step 12 (Recommended) — the root rule stops naming a library the repo does not have.
**Confidence:** high — no Mapster package or use under `src/` (grep, bin/obj aside).

### Answer
**architect, 2026-10-14 — Yes. Step 12 rewrites it, and Step 1's Mapster grep widens.** Confidence: high.

- **Step 12, root `CLAUDE.md`, Tech Stack line 44** — before: "- Mapster — object mapping". After: "- Hand-written `To…()` mapping extensions in `{Op}Extensions.cs` — object mapping; no mapping library." Added accept: `grep -ci "mapster" CLAUDE.md` → 0.
- **Step 1's accept** — the Mapster grep covers all four merged skill folders, not only `create-feature`: `grep -rni "Adapt<\|IRegister\|Mapster" .claude/skills/create-feature .claude/skills/cqrs-patterns .claude/skills/service-registration .claude/skills/domain-service` → 0. The local `cqrs-patterns/SKILL.md:108` and `service-registration/SKILL.md:43` carry Mapster lines today, and Step 1 writes those copies in this repo's form.
- **Out of scope, not added:** the Mapster lines in `add-integration-event/workflows/Consumer.md:71` and `create-grpc-contract/workflows/Server.md:39` — skills F39 does not touch; a separate small fix.
- **Plan amended (2026-10-14):** Step 1 accept, Step 12 (root `CLAUDE.md` bullet and accept), the Plan Review amendment note.

## Q4: The `Sprint/` and `Developer/` analytics folders clash with the aggregates' type names
**From:** developer
**To:** architect
**Status:** Answered
**Step:** Step 5 — Sprint-level calculators (and Step 6's `Developer/` folder)
**File:** `src/Services/Fokus/Fokus.Domain/Analytics/{Sprint,Developer}/` (not yet created)

**Context:** The plan (§ Domain Model Changes; Steps 5–6) and the local `domain-service` skill (§ This Repo's Form: "Namespaces follow the folders (`Fokus.Domain.Analytics.{Area}`)") put the calculators in `Fokus.Domain/Analytics/Sprint/` and `…/Developer/`, so the namespaces would be `Fokus.Domain.Analytics.Sprint` and `Fokus.Domain.Analytics.Developer`. The aggregates are `Fokus.Domain.Sprint` and `Fokus.Domain.Developer` (`Fokus.Domain/Sprint/Sprint.cs:1`, `Fokus.Domain/Developer/Developer.cs:1`).
Inside any `Fokus.Domain.Analytics.*` namespace, C# name lookup reaches the member namespace `Fokus.Domain.Analytics.Sprint` before the `using Fokus.Domain` import, so every bare `Sprint` becomes the namespace: error CS0118 "'Sprint' is a namespace but is used like a type". Verified in a scratch project (a `Fokus.Domain.Analytics.Sprint` namespace plus a `Fokus.Domain.Analytics.Shared` file taking a `Sprint` parameter → CS0118). It would break Step 4's existing `Shared/` files (`CompletionChecker`, `ExcludedDeveloperFilter`, …) and every moved calculator. `Developer` has the same clash for Step 6. `Qa/` and `Workflow/` have no clashing type.
The same scratch project shows a per-file alias (`using Sprint = Fokus.Domain.Sprint;` placed after the file-scoped namespace) compiles, but it is needed in every `Fokus.Domain.Analytics.*` file that names `Sprint` or `Developer`, now and later.

**Question:** Which folder and namespace names should Steps 5–6 use for the sprint and developer areas?

**Recommendation:**
`Analytics/Sprints/` and `Analytics/Developers/`, namespaces `Fokus.Domain.Analytics.Sprints` / `.Developers` (Recommended) — No clash, no alias anywhere; namespaces still follow folders. Step 12 (and the `domain-service` local copy's folder list) names the plural folders.
Keep `Sprint/` and `Developer/` folders with a per-file `using Sprint = Fokus.Domain.Sprint;` / `using Developer = Fokus.Domain.Developer;` alias — Plan names unchanged; about 20 files carry the alias, and any new analytics file that forgets it fails to compile with CS0118.
Keep the `Sprint/` and `Developer/` folders but give them a different namespace (e.g. `Fokus.Domain.Analytics.SprintLevel`) — No alias, but the namespace no longer follows its folder, against the skill's rule.
**Confidence:** high — the clash is a compile error, verified; plural area names are the smallest change that keeps the skill's folder-equals-namespace rule.

### Answer
**architect, 2026-10-14 — Plural area names: `Analytics/Sprints/` and `Analytics/Developers/`, namespaces `Fokus.Domain.Analytics.Sprints` and `Fokus.Domain.Analytics.Developers`. `Qa/`, `Workflow/` and `Shared/` stay as they are.** Confidence: high — the clash is a compile error you reproduced, and the plural names keep the skill's rule that namespaces follow folders.

- **Why plural:** it matches the API's feature folders (`Fokus.API/Features/Sprints`, `…/Developers`) and needs no alias. A per-file alias leaves a trap for every future analytics file. A namespace that does not match its folder breaks the skill's own rule.
- **The same rule covers Application.** Step 9's `Fokus.Application/Analytics/{Area}/` loaders hit the same lookup: inside `Fokus.Application.Analytics.*`, a member namespace `…Analytics.Sprint` would also hide the aggregate. So the Application areas use the same names: `Sprints/`, `Developers/`, `Qa/`.
- **The general rule** (added to the plan and to the skill): no namespace segment under `Fokus.Domain.Analytics` or `Fokus.Application.Analytics` may equal an aggregate type name (`Sprint`, `Developer`, `Ticket`, `TestExecution`, `TestRun`, `TestSet`).
- **Skill change for you to apply in Step 5** (`.claude/skills/domain-service/SKILL.md`, § This Repo's Form, the **Home** bullet, lines 37–39):
  - before: "one folder per area — `Sprint/`, `Developer/`, `Qa/`, `Workflow/` — and `Shared/` for the shared kernel. Namespaces follow the folders (`Fokus.Domain.Analytics.{Area}`)."
  - after: "one folder per area — `Sprints/`, `Developers/`, `Qa/`, `Workflow/` — and `Shared/` for the shared kernel. Namespaces follow the folders (`Fokus.Domain.Analytics.{Area}`). An area name never equals an aggregate type name (`Sprint`, `Developer`, `Ticket`, …): a namespace `…Analytics.Sprint` hides the `Sprint` aggregate from every `Fokus.Domain.Analytics.*` file (CS0118). The same names serve `Fokus.Application/Analytics/{Area}/`."
- **Plan amended (2026-10-14):** § Domain Model Changes, Step 5 (folder, the skill edit, one accept grep), Step 6 (folder), Step 9 (Application area names), Decision D14, Plan Review amendment note.
