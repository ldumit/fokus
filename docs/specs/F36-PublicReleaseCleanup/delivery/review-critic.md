# Critic record — F36-PublicReleaseCleanup

## Round 1 — 2026-10-13 — spec — primary (codex)

Header note: primary is the Codex job; its verdict (NO-GO, 14 findings) is persisted verbatim in [review-critic-codex.md](review-critic-codex.md). Persisted by the architect (standalone main session). The second reader's message was truncated twice in the relay; its full text below was salvaged from its transcript.

## Second reader — round 1 (sonnet)


# F36 Public Release Cleanup — Critic Review

## Mode: Spec Review (technical spec, mechanism depth, second reader)
## Verdict: REVISE

There are 4 HIGH, 8 MEDIUM and 3 LOW findings, and no CRITICAL. Nothing was modified in the repo; the git status is the same as at the start. I tested the build claim by exporting two old snapshots to scratch and building them, and removed the scratch copies afterwards. All other checks were read-only greps and git reads.

## Pre-commitment predictions (expected vs found)
- **D5 scrub list incomplete:** found, and worse than expected. The employer leaks through its Jira tenant, ticket keys, colleague names and a pasted sprint retro, none of which contain the word "omnishelf".
- **Graphify acceptance does not match reality:** found. Graphify appears 51 times in 17 files, and the graph was committed to history.
- **History rewrite has no mechanism and "every commit builds" cannot hold:** found. The two first commits have no code. Sampled code-bearing snapshots do build.
- **Contradictions between spec parts:** found. D7's "no tests" is false, O2's open items are stale, and O1 "all" conflicts with D5.
- **Not predicted:**
  - Untracking `settings.local.json` orphans the shipped hooks.
  - The architecture map is stamped with a private-history sha.
  - Phase 1 scrubs graphify before Phase 2's mining skill needs it.
  - `.claude/audit/` is untracked but not ignored.

## Coverage ledger
Round: 1 · Depth: mechanism · Bar: mechanics (tech-spec) · Ruler: seed only (`docs/critic-calibration/ruler.md` absent in the repo) · Read: full

| Requirement / area | Surfaces opened | Status |
|---|---|---|
| D1 (Nexus can read the record layout) | docs/specs listing: 66 folders; 7 deviate (BUG-1/2 and GAP-1..4 have `definition/` only; adhoc-ClaudeSetupAudit is a flat file) | COVERED (minor) |
| D2 and D4 (private records, new public repo) | spec § Constraints; git remote/tags | PARTIAL (no repo name, tag and identity policy: F11, F13) |
| D3 (history rewrite) | `git log` (61 commits); `git ls-tree` on early commits; two snapshot builds; `git filter-repo` availability | PARTIAL (F3, F5) |
| D5 scrub list | HEAD and full-history greps for employer, personal and tool tokens; settings files; root scripts; docs/product, proposals, evaluations; .claude/ | PARTIAL (F1, F2, F7, F10) |
| D6 (guide written last) | spec only | COVERED |
| D7 (build plus click-through) | slnx, test project, docs/conventions/testing.md | PARTIAL (F6) |
| Phase 0 restore tag | `git cat-file tag` (annotated, resolves to d075998) | COVERED |
| Phases 1–4 sequencing | mine-verify-repo and mine-architecture SKILL.md | PARTIAL (F3, F9) |
| Constraints (private-only paths) | `git status --ignored`, `.gitignore`, `.claude/audit`, docs/skill-gaps | PARTIAL (F12) |
| O1 candidate ten | all ten folders exist | COVERED |
| O2 open items | backlog.md:193, 304–317; adhoc-SettingsGapFill summary; AuthConfiguration.cs:22 | PARTIAL (F8) |
| Acceptance (5 checks) | history patch stream greps | PARTIAL (F4, F5) |
| Deliverables 1–7 | conventions, README, product docs | PARTIAL (F9, F14) |
| `docs/kb`, `docs/architecture`, `client/src`, `src/` | token greps only (no employer or personal tokens found in code) | PARTIAL |
| `docs/specs/**` records | grep and layout only, not read | PARTIAL |
| Front-end `npm run build`, `dotnet build` at HEAD | — | NOT OPENED (not run; only two old backend snapshots built) |
| `docs/critic-calibration/` overlay | — | NOT OPENED (absent) |

## Findings

### [HIGH] F1 — The scrub list removes one literal word, but the employer leaks through other identifiers
**Anchor:** seed-0008 · above seed-0123 · below seed-0003
**Shape:** incomplete scrub inventory (nearest anchors are plan-kind and spec-kind; none fits a disclosure exactly)
**Consequence:** Identifiers that point to a former employer and its staff ship in public documents because the removal rule names only one literal word.
**Evidence:** quoted, and path
**Surfaces:** spec.md § D5 (L41), § Acceptance (L70); docs/product/v2.md; docs/proposals/suite-integration-architecture.md; .claude/agents/po.md; .claude/rules/agents-workflow.md
**Untouchable:** security (disclosure to a public repo is irreversible)
**Source / Issue:**
- D5 says "remove the former employer's name", and acceptance L70 greps only `omnishelf`. That matches 7 lines in 3 tracked files, 9 lines in the full history.
- `docs/product/v2.md:8` reads "Validated against live Jira project PD (omniaz.atlassian.net)". This is the employer's tenant and is not caught.
- `docs/product/v2.md` lines 127, 161, 182, 206, 246 and 488 carry real ticket keys such as PD-5228 and PD-5384. Lines 218, 342 and 482 name colleagues ("Taylor", "Mateusz").
- `docs/product/v2.md` is a kept deliverable (row 3, L20).
- `docs/proposals/suite-integration-architecture.md:229-266` is a pasted "Sprint 28 — Sprint Report" with "Team: Omnishelf" and nine participant names. It also holds action items assigned to named colleagues.
- Employer Jira keys and domain wording sit in the shipped setup: `.claude/agents/po.md:54,66,67`, `.claude/rules/agents-workflow.md:11,12`, `.claude/skills/create-feature-spec/SKILL.md:73,74` ("PD-5234-shelf-compliance-kpis"), and `docs/agenting-setup-reference.md:416,417`.
- More keys sit in `docs/specs/F25-XrayIntegration/definition/spec.md:30,65`, `delivery/plan.md:63`, and `F35-EpicViewEnhancements/definition/help.page.md:5`.
- Real sprint figures appear in BUG-1 and BUG-2 ("Sprint 26", "Completed SP = 202", "63 SP gap").
- History also holds `C:/Users/Laurentiu/` (6 lines in old `settings.local.json` versions) and `D:/src/code-forge` (37 lines, from `settings.json` L17-19).
- Beyond the pasted retro, `docs/evaluations/cross-project-skill-mining.md` (517 lines) analyses the employer's internal skills. A name-only replace would leave that content. Whether it ships is an owner call.

**Impact:** The acceptance check passes while the tenant, staff names and ticket keys go public.
**Fix:** Replace the single literal with an enumerated identifier class (name, tenant, Jira project key `PD-`, colleague names, `C:/Users/<name>`, `D:\src\*` paths) and extend the acceptance greps to match. Decide whether employer-derived analysis docs are deleted or redacted. **Confidence: high.**

### [HIGH] F2 — Most documents outside the seven deliverables have no stated disposition
**Anchor:** seed-0004 · above seed-0022 · below seed-0003
**Shape:** unstated disposition / ambiguous removal scope
**Consequence:** A reader cannot tell which of a dozen background documents ship, so each builder removes a different set and links to the removed ones break.
**Evidence:** quoted, and path
**Surfaces:** spec.md § What ships (L18-24), § D5 (L41), § O1 (L65); docs/proposals/*; docs/evaluations/*; docs/research/*; root scripts
**Source / Issue:**
- D5 says "the suite strategy … and its proposals". Of the 8 files in `docs/proposals/`, only `suite-integration-architecture.md` is about the suite. The other 7 can be read as part of the same removal, or as staying.
- Ideas B and E ("remove setup-building records") were parked. That makes "stay" the default reading, yet nothing says so.
- These are never mentioned:
  - `docs/proposals/` ×8
  - `docs/evaluations/` ×2
  - `docs/research/pocock-skills-comparison.md`
  - `docs/agentic-setup-evaluation.md`
  - `docs/agenting-setup-reference.md`
  - `docs/skill-backlog.md`
  - `docs/deployment-azure.md`
  - root `check-xray.ps1`, which hard-codes `D:/src/fokus/...` and a local DB. D5 names the other three root scripts but not this one.
  - `.claudeignore` and `.graphifyignore`
- O1 "all" (the owner's lean) ships `adhoc-SuiteScaffolding` and `adhoc-SuiteInfrastructure`.
  - Both are plans for the suite monorepo, with `D:\src\sprint-rituals` paths (`adhoc-SuiteScaffolding/delivery/plan.md:14`) and `D:\src\dotnet-microservices`.
  - Both cite the removed proposal as their driver (`plan.md:3` in each).
  - `adhoc-PipelineCraftImprovements/delivery/plan.md:7` contains "Omnishelf".
  - D5 and O1 are never reconciled.

**Impact:** Dangling links, leftover suite and employer material, or an unrequested deletion of setup history.
**Fix:** Add a full disposition table (ship / remove / redact) for every tracked top-level doc and for the O1-all consequences. **Confidence: high.**

### [HIGH] F3 — The scrub is specified at two layers with no single re-runnable mechanism, and Phase 1 breaks Phase 2
**Anchor:** seed-0117 · above seed-0130 · below seed-0189
**Band:** HIGH–MEDIUM; driver: whether the spec means the private repo is permanently scrubbed or the scrub is a publish-time transform
**Shape:** two conflicting statements of one operation
**Consequence:** The cleanup is both a one-time edit of the working copy and a rule for every old snapshot, so the next phase loses a tool it needs.
**Evidence:** quoted, scenario, path
**Surfaces:** spec.md § Phases (L50, L51, L53), § D3 (L39), § D5 (L41), § Constraints (L57); mine-verify-repo/SKILL.md; .gitignore
**Source / Issue:**
- Phase 1 applies D5 "to the working tree" of the private repo.
  - Phase 4 applies it "to every snapshot".
  - D3 and the acceptance block treat the rewrite as one procedure. No tool or procedure is named.
- The graphify edits are semantic, not token substitutions. They span `CLAUDE.md`, `.claude/rules/kb-navigation.md:18`, `.claude/agents/architect.md:86` and `po.md:26,96,110,182`, and the whole `improve-architecture` skill (its step 1 is "Graphify God Nodes"). They also span a 10-hit section of `docs/agenting-setup-reference.md`, plus 17 files in total.
  - Re-applying them to about 45 older snapshots, in which those files differ, needs a defined transform. It cannot be a one-time edit.
- Phase 2's `mine-verify-repo` takes "graphify areas ∩ hotspot top-N" as its scope (SKILL.md:23, :38, :291).
  - `graphify-out/` does not exist on disk now.
  - The only guard against committing it is `.gitignore:442-443`, which the `graphify` grep in acceptance requires removing.
  - History shows the failure mode: `graphify-out/graph.json` (18,481 lines, containing `D:\src\fokus\...` paths) was committed at 05bb172 and carried through 28d85f5. It was removed at 117619e.
- Where the 3–6 Phase 3 cleanup commits land in the rewritten history is unstated. They could be folded into old snapshots or appended at the end.
- `backlog.md` mixes public and private rows. The "F36+ rows" exclusion is a content filter, not a path filter.
- Tool prerequisite: `git filter-repo` is not installed (`git: 'filter-repo' is not a git command`).

**Impact:** The private repo loses graphify before the mine that needs it. A later add-all commit can re-commit the graph artifacts. The result of the rewrite is non-reproducible.
**Fix:** Decide that the private repo stays intact and the scrub is a scripted, re-runnable transform run on a clone. Name Phase 3's place in the history, and name the tool (the tool choice is plan-level). **Confidence: high on the gap, medium on the recommendation.**

### [HIGH] F4 — The acceptance gate does not verify what D5, D3 and the Constraints promise
**Anchor:** seed-0008 · above seed-0021 · below seed-0003
**Shape:** verification present but too narrow
**Consequence:** The release gate checks one literal word, so employer tenant, ticket keys, staff names and personal paths pass it and reach the public copy.
**Evidence:** quoted
**Surfaces:** spec.md § Acceptance (L68-74), § D5 (L41), § Constraints (L57), ideas.md § C critique
**Untouchable:** security (same class as F1)
**Source / Issue:**
- ideas.md C says "the scrub must cover every commit … not only the final tree", but L72 checks graphify in "the final tree" only. History holds the graph artifacts and 96 graphify lines.
- No check covers any of these:
  - the removed `agentic-rituals-suite-strategy.md` and suite proposal being absent from all commits (`git log --all -- <paths>`);
  - the overnight script's "short note" existing;
  - private-only paths (L57) being absent from every commit;
  - commit author and email;
  - the employer identifiers from F1;
  - personal absolute paths (`D:\src\fokus` appears in 3402 patch lines, mostly the committed graph).
- L72 is unsatisfiable without editing kept records. `git grep -i graphify` currently hits `docs/specs/adhoc-ClaudeSetupAudit/claude-setup-audit.md`, four proposal and research files, and `docs/agenting-setup-reference.md`. That conflicts with keeping feature records as they are (O1).

**Impact:** The owner and the primary reader can approve a repo that still discloses the items in F1.
**Fix:** Make the acceptance list derive from the D5 and Constraints lists. Add a deny-token set plus a path allow-list check over `git log --all`, and state which records may be edited to satisfy the graphify grep. **Confidence: high.**

### [MEDIUM] F5 — "Every commit builds" is impossible for the first two commits
**Anchor:** seed-0022 · above seed-0041 · below seed-0008
**Shape:** acceptance criterion that cannot pass as written
**Consequence:** A check that every snapshot builds cannot pass for the first two snapshots, which hold no buildable code, so the gate fails or is quietly reinterpreted.
**Evidence:** path
**Surfaces:** spec.md § Acceptance (L73), § D3 (L39); `git ls-tree` 5de00f3 and d030d9b
**Source / Issue:**
- 5de00f3 holds 3 files (`.gitignore`, `LICENSE`, `README.md`). d030d9b holds 74 files and none under `src/` or `client/`.
- `dotnet build src/Fokus.slnx` and `npm run build` in `client/` have nothing to run on them.
- D3 keeps setup commits, so these two are the first snapshots in the repo.
- I built 085e18a (first code commit, no BuildingBlocks yet) and 8c75050. Both gave 0 errors (backend only; the front-end build was not run). So the criterion is plausible for code-bearing commits.
- NU1903 "known high severity" package advisories appear in every build. Examples are `Microsoft.OpenApi 2.0.0-preview.17` and `SQLitePCLRaw.lib.e_sqlite3 2.1.10`. They are visible to students.
- `npm run build` is `vue-tsc -b && vite build` and needs `npm ci` per snapshot.

**Fix:** Restrict the criterion to commits that contain `src/Fokus.slnx`, state the install step, and decide whether the advisory warnings are in scope. **Confidence: high.**

### [MEDIUM] F6 — D7 says there are no tests, but a test project with 19 tests exists
**Anchor:** seed-0022 · above seed-0041 · below seed-0008
**Shape:** two statements disagree
**Consequence:** The plan assumes no tests exist while a test project with nineteen tests is built by the gate, so the safety net and the rules about tests disagree.
**Evidence:** quoted, and path
**Surfaces:** spec.md § D7 (L43), § Out of scope (L28); src/Fokus.slnx:24; src/Services/Fokus/Fokus.Tests/; docs/conventions/testing.md; .claude/agents/developer.md:67
**Source / Issue:**
- D7 says "the only safety net — no tests". `Fokus.Tests` is in the solution, which acceptance L73 builds.
  - `DeveloperProgressServiceTests.cs` has 19 `[Fact]`/`[Theory]` methods.
- Out of scope says "Tests and test projects". It does not say whether the project ships, is removed (which changes the build), or is left alone.
- `docs/conventions/testing.md`, the `tdd` skill and `developer.md:67` (rule 14) all mandate TDD. Deliverable 4 says conventions must match the code.
- `dotnet test` is an unused cheap safety net.

**Fix:** State "Fokus.Tests ships untouched", or remove it from the slnx and conventions. Add `dotnet test` to D7. **Confidence: high.**

### [MEDIUM] F7 — Untracking `settings.local.json` orphans the two shipped hooks
**Anchor:** seed-0130 · above seed-0364 · below seed-0120
**Shape:** a removal breaks an unnamed dependent
**Consequence:** Removing the local settings file deletes the only place two shipped hooks are registered, so students receive hooks and commands that never fire.
**Evidence:** path
**Surfaces:** spec.md § D5 (L41), § What ships row 5 (L22); .claude/settings.local.json; .claude/settings.json; .claude/hooks/
**Source / Issue:**
- `settings.json` at HEAD has only `permissions.deny`.
- `.claude/hooks/restore-agent.js` and `audit-logger.js` are registered only in `settings.local.json` (SessionStart and PreToolUse).
- The nine `.claude/commands/*` write `.claude/.current-agent`, which only `restore-agent.js` reads.
- The same file carries `OMC_SKIP_HOOKS` and `mcp__plugin_oh-my-claudecode_t` permissions.
- The owner's own gap table (`docs/proposals/cc-setup-evaluation-and-plugin-extraction.md:~50-53`) records the "remaining: `git rm --cached`" step but not the loss of hook wiring.

**Fix:** Move a clean hooks block into the committed `settings.json`, or drop the hooks and their README mentions. **Confidence: high.**

### [MEDIUM] F8 — O2 open items were copied from the backlog without checking
**Anchor:** seed-0023 · above seed-0041 · below seed-0008
**Shape:** stale reference in a spec claim
**Consequence:** Open-work claims copied from a tracker row that a later completed task already closed send the owner to triage items that are done.
**Evidence:** quoted, and path
**Surfaces:** spec.md § O2 (L66); docs/backlog.md:193, 304-309, 317; docs/specs/adhoc-SettingsGapFill/delivery/summary.md; client/src/components/settings/JiraTab.vue
**Source / Issue:**
- Backlog row I11 (L193) says "Settings Gap Fill (GAP-1 + GAP-2 + GAP-3) | Done". The summary says "Status: COMPLETE" and describes the excluded-statuses UI, sync range and sub-team management. The client already references `excluded-statuses`.
- Rows 304–306 still say "Open", and so does O2. So the "GAP-1..4" list is probably only GAP-4.
- TD-1 is accurate (`AuthConfiguration.cs:22` uses `SameAsRequest`). It is a High security item that `docs/deployment-azure.md` implicitly pushes students to deploy behind HTTPS.

**Fix:** Verify each item against code before O2 goes to the owner. Recommend fixing TD-1 in the campaign, since it is a one-line change. **Confidence: medium on GAP-4 status, high on GAP-1..3.**

### [MEDIUM] F9 — The architecture map is mined before the cleanup and carries a sha that the rewrite erases
**Anchor:** seed-0123 · above seed-0364 · below seed-0120
**Shape:** shipped artifact points at something absent in the public copy
**Consequence:** A map mined before the cleanup and stamped with a revision that is rewritten away is stale and unresolvable when students read it.
**Evidence:** path
**Surfaces:** spec.md § Phase 2 (L51), § Phase 3 (L52), § Constraints (L58); mine-architecture/SKILL.md § Output artifact (the `Basis: {sha}` line), § BR-coverage join
**Source / Issue:**
- Phase 2 mines the map, then Phase 3 changes the code with 3–6 cleanup features. Deliverable 2 says "current-state". Nothing re-mines the map or checks freshness.
- Every `index.md` carries `Basis: {sha}` of the private history. After the D3 rewrite, no such sha exists.
- Per-row `last_verified` and `rule-coverage` fields follow the same pattern. With BR registries out of scope (L29), every row would read `no-registry-estate`.
- The output uses Nexus registry vocabulary ("sixth registry species", provenance). Constraints L57 says shipped records stay in the in-repo setup's formats.

**Fix:** Mine the map after Phase 3 (or re-run), and decide how `Basis:` and registry vocabulary are handled in the public copy. **Confidence: medium.**

### [MEDIUM] F10 — The unusable-tool sweep covers graphify only
**Anchor:** seed-0123 · above seed-0364 · below seed-0120
**Shape:** shipped text refers to material students lack
**Consequence:** Instructions for a third-party add-on and its permissions stay in the setup students copy, so they hit steps that cannot run.
**Evidence:** path
**Surfaces:** spec.md § D5 (L41); .claude/README.md:56-58; .claude/rules/pipeline-guardrails.md:20-22; .gitignore:436
**Source / Issue:**
- `.claude/README.md` has an "OMC Boundary" section, and `pipeline-guardrails.md` has three bullets on `oh-my-claudecode:*` agents. The owner's own gap table lists "OMC coupling" as P0 and "Partial".
- The Codex references (`reviewer.md:141-156`, `team-lead.md:98-106`) degrade gracefully when Codex is absent, so they are low risk.
- ideas.md C's problem statement is "nothing private or unusable reaches students", but D5 enumerates only graphify.

**Fix:** Add OMC to the D5 sweep and decide how Codex is described. **Confidence: medium (OMC is a public tool, so "unusable" is the owner's call).**

### [MEDIUM] F11 — Commit identity and tags in the rewritten history are undecided
**Anchor:** seed-0022 · above seed-0041 · below seed-0008
**Shape:** open decision not listed as open
**Consequence:** Every rewritten commit carries a personal email and the restore tag points into the private history, so identity and history can leak on push.
**Evidence:** path
**Surfaces:** spec.md § Phase 0 (L49), § D3, § D4; `git log --format='%an <%ae>'`; `git cat-file -p pre-public-cleanup`
**Source / Issue:**
- All 61 commits are authored as `ldumit` (60) or `Laurentiu Dumitrescu` (1), both `dumitrescu.laurentiu@gmail.com`. Nothing says whether the public history keeps it, uses a noreply address, or merges the two identities.
- `pre-public-cleanup` is an annotated tag (tagger email and message "…(Udemy)…") that resolves to d075998. Tag `V1.0` points at 117619e. If either is carried into the public clone, pushing drags private-history objects along. The spec says nothing about tags.

**Fix:** Add both decisions to Open items. **Recommended: noreply address, one identity, no tags carried. Confidence: medium (owner's call on identity).**

### [MEDIUM] F12 — Private-only paths are listed but not enforced
**Anchor:** seed-0130 · above seed-0364 · below seed-0120
**Shape:** list without enforcement
**Consequence:** Private-only folders are listed but not ignored or checked, so a routine add-all commit during the campaign can put them in the public history.
**Evidence:** scenario, path
**Surfaces:** spec.md § Constraints (L57); `git status`; `git check-ignore`; .gitignore
**Source / Issue:**
- `.claude/audit/handoff.jsonl`, `.claude/audit/read-tracker.json` and `docs/skill-gaps/registry.md` are untracked and not ignored (`git check-ignore` exit 1).
- Only `*.log` files are ignored. The graph precedent above (05bb172) shows how this goes wrong.
- "Mining scratch records" is not a path. The Nexus campaign will likely add other folders. Likely examples are `docs/critic-calibration/`, `docs/kb/research/`, `.claude/verify.json` and the per-feature worktree branches. I did not verify these.
- Scenario: a Phase 3 cleanup feature commits with `git add -A`; `.claude/audit/*` lands in the private branch and then in the squashed public snapshot.

**Fix:** Enumerate the paths, add ignore rules in the private repo, and add the path check from F4. **Confidence: medium.**

### [LOW] F13 — README clone URL is the private repo's address
**Anchor:** seed-0041 · above none · below seed-0021
**Shape:** dead pointer in shipped text
**Consequence:** The README tells students to clone an address that is the private repo's, so the first command a student runs fails.
**Evidence:** path
**Surfaces:** README.md:43; spec.md § D4 (L40)
**Issue:** `git clone https://github.com/ldumit/fokus.git` is `origin`, which stays private. The public repo name and owner are not stated. A new repo with the same name would collide, so the private repo would need renaming first.
**Route:** plan must resolve

### [LOW] F14 — Tool prerequisites for Phases 2 and 4 are not installed
**Anchor:** seed-0041 · above none · below seed-0021
**Shape:** environment prerequisite
**Consequence:** The mining and rewrite phases need tools the machine lacks, so they stop at the first step.
**Evidence:** path
**Surfaces:** spec.md § Phases (L51, L53); mine-verify-repo/SKILL.md:37,146
**Issue:** `which java` finds nothing (Code Maat needs it), while `lizard` is present. `git filter-repo` is absent.
**Route:** plan must resolve

### [LOW] F15 — Deliverable 1 and Phase 2 use different measures
**Anchor:** seed-0041 · above none · below seed-0021
**Shape:** deliverable vs tool mismatch
**Consequence:** The phase measures hotspots and debt, not conformance to the conventions, so the deliverable's quality bar goes unchecked.
**Evidence:** path
**Surfaces:** spec.md § What ships row 1 (L18), § Phase 2 (L51); mine-verify-repo/SKILL.md:203-215
**Issue:** mine-verify-repo uses the repo's conventions only to adjudicate "by design" findings. A convention-conformance check is a different tool.
**Route:** plan must resolve

## ADVERSARIAL pattern (round 1, 4 HIGH)
The spec states facts and lists that came from the owner's memory and the backlog, not from a sweep of the tree. These claims are wrong or incomplete when checked:
- "no tests" (F6)
- GAP-1..3 open (F8)
- "omnishelf" as the employer's footprint (F1)
- "graphify" as the only unusable tool (F10)
- "its proposals" (F2)
- acceptance coverage of the D5 list (F4)

The second systemic issue is that the scrub exists as two unrelated statements (Phase 1 and Phase 4) instead of one re-runnable artifact (F3). One inventory sweep by token class and by path would close F1, F2, F4 and F12 together.

## Gap analysis
- **Edge case, "scrub every snapshot":** three readings are plausible (replace listed tokens, delete listed paths, rewrite sections). The spec does not choose, and only the first is mechanical.
- **Edge case, squashed groups:** a squash group takes the tree of its last commit. If an older snapshot had `graphify-out/` or built `wwwroot/` bundles (5 commits tracked `wwwroot`), a path filter must remove them. The spec lists neither path.
- **Ambiguity split, "graphify references":**
  - (a) remove the `CLAUDE.md` section only, or
  - (b) remove all 51 hits including records, which is what acceptance enforces.
- **Backward impact:** removing the graphify rules changes behaviour in `architect.md`, `po.md`, `kb-navigation.md` and the `improve-architecture` skill. The spec lists none of them.
- **Minor, D1 "verified":** 7 of 66 spec folders deviate from the `definition` plus `delivery` layout, as listed in the coverage ledger. This is cosmetic.

## Open questions (low confidence)
- Whether `omniaz` and the `PD` project belong to the employer. I infer yes: the same Sprint 28 and the same colleague names appear in the pasted retro (`v2.md:182`, `suite-integration-architecture.md:229-233`).
- Whether the front-end `npm run build` passes at old snapshots. I did not run it.
- Whether GAP-4 is genuinely still open.
- Whether the owner wants employer-derived analysis docs redacted or deleted (F1).

Reviewed: D:\src\fokus\docs\specs\F36-PublicReleaseCleanup\definition\spec.md


### Merge — round 1

Coverage: front-end `npm run build` at any snapshot (neither reader ran it); `docs/specs/**` record bodies (grep only)

| id | readers | level | anchor | title | shape | consequence | evidence | untouchable | tag | surfaces | source ids |
|---|---|---|---|---|---|---|---|---|---|---|---|
| R1-01 | both | HIGH | seed-0008 | Scrub names one literal; tenant, ticket keys, colleague names, sprint retro, personal paths leak | incomplete scrub inventory | third-party data published irreversibly | v2.md:8,218,342,482; suite-integration-architecture.md:229-266; po.md:54; check-xray.ps1:1; history settings | security | scrub | D5, Acceptance | codex-1, codex-3, S-F1 |
| R1-02 | both | HIGH | seed-0004 | No disposition for docs outside the deliverables; suite records ship under O1-all | unstated disposition | each builder removes a different set | docs/proposals ×8, evaluations, research, adhoc-Suite* | — | scope | What ships, D5, O1 | codex-2, S-F2 |
| R1-03 | both | contested: HIGH / MEDIUM → HIGH | unranked — no anchor fits; Codex severity High | Commit email and tags in rewritten history undecided | missing decision | personal identity and private objects pushed | git log authors; tags V1.0, pre-public-cleanup | security | identity | D3, D4 | codex-4, S-F11 |
| R1-04 | both | HIGH | seed-0008 | Scrub has no re-runnable mechanism; graphify artifacts in history; Phase 1 removes graphify the miner needs | mechanism gap | private paths survive in history; Phase 2 breaks | 05bb172 graph.json; mine-verify-repo SKILL.md:23 | security | mechanism | D3, D5, Phases | codex-5, S-F3 |
| R1-05 | both | contested: HIGH / MEDIUM → MEDIUM | unranked — no anchor fits; Codex severity High | First two commits cannot build | unsatisfiable gate | gate fails or is quietly reinterpreted | 5de00f3, d030d9b trees | — | build | D3, Acceptance | codex-6, S-F5 |
| R1-06 | both | contested: HIGH / MEDIUM → HIGH | unranked — no anchor fits; Codex severity High | Untracking settings.local.json orphans the shipped hooks | broken deliverable | students get dead hooks and commands | settings.local.json:26; settings.json | — | setup | D5, deliverable 5 | codex-7, S-F7 |
| R1-07 | both | MEDIUM | seed-0022 | No ignore rules; private paths not enforced | unenforced constraint | add-all commits private records | .gitignore; git check-ignore exit 1 | — | scrub | D5, Constraints | codex-8, S-F12 |
| R1-08 | both | MEDIUM | seed-0022 | Unusable-tool sweep covers graphify only (OMC, Atlassian MCP) | incomplete sweep | students hit steps that cannot run | .claude/README.md:56; pipeline-guardrails.md:20; po.md | — | setup | D5 | codex-9, S-F10 |
| R1-09 | both | HIGH | seed-0003 | Acceptance checks one literal, mixes per-commit and final-tree checks, omits deliverable gates | weak gate | leaking repo passes the gate | spec Acceptance | security | gate | Acceptance | codex-10, S-F4 |
| R1-10 | both | MEDIUM | seed-0022 | Click-through undefined; 19 existing tests ignored | undefined safety net | regressions slip through | Fokus.Tests; slnx:24 | — | safety | D7, Out of scope | codex-11, S-F6 |
| R1-11 | both | MEDIUM | seed-0022 | Fresh clone fails: private clone URL, Node 18, .credentials paths | usability | student's first command fails | README; deploy.ps1; package-lock | — | usability | D4, D8 | codex-12, S-F13 |
| R1-12 | both | MEDIUM | seed-0022 | O2 copied stale from backlog (GAP-1..3 done; TD-2, I8 missing) | stale inventory | owner triages done work | backlog.md:193,304-317 | — | triage | O2 | codex-13, S-F8 |
| R1-13 | codex | MEDIUM | unranked — no anchor fits; Codex severity Medium | Retained records hold dead links | dangling references | broken links in shipped records | F4, F7, F10, F18, F22 metadata | — | records | O1, Acceptance | codex-14 |
| R1-14 | sonnet | MEDIUM | seed-0022 | Map mined before the campaign; sha and Nexus vocabulary do not survive the rewrite | stale deliverable | map wrong at publish | mine-architecture output | — | map | Phases, deliverable 2 | S-F9 |
| R1-15 | sonnet | LOW | seed-0123 | java and git filter-repo not installed | missing prerequisite | phases stop at first step | which java; git filter-repo | — | tooling | Phases | S-F14 |
| R1-16 | sonnet | LOW | seed-0123 | Deliverable 1 and Phase 2 measure different things | mismatch | quality bar unchecked | mine-verify-repo SKILL.md | — | scope | deliverable 1, Phase 2 | S-F15 |
| R1-17 | sonnet | MEDIUM | seed-0022 | NU1903 high-severity package advisories print on every build | unlisted debt | students see security warnings | build output | security | triage | O2 | S-F5 (note) |

### Fold — round 1 (architect, claude-opus-5-5, 2026-10-13)

| id | disposition | level after | re-grade (level · anchor · why) | where |
|---|---|---|---|---|
| R1-01 | fixed-real | HIGH | — | spec § Decisions A1, A3; § Phases 1 (identifier inventory); § Acceptance (deny list) |
| R1-02 | fixed-real | HIGH | — | spec § Decisions A4, A5; § What ships row 6 |
| R1-03 | fixed-real (owner decision raised) | HIGH | — | spec § Open items O3; § Acceptance (identity, no tags); § Constraints (no tag carried) |
| R1-04 | fixed-real | HIGH | — | spec § Decisions A1, A2, A7; § Phases 1, 5; D4 (private never rewritten) |
| R1-05 | fixed-real | MEDIUM | — | spec § Acceptance (slnx-bearing commits; first two exempt; npm ci) |
| R1-06 | fixed-real | HIGH | — | spec § Decisions A6; § What ships row 5; § Acceptance (hooks fire) |
| R1-07 | fixed-real | MEDIUM | — | spec § Constraints (enumerated, ignored from Phase 1) |
| R1-08 | fixed-real | MEDIUM | — | spec § Decisions A7; § Acceptance (final-tree grep) |
| R1-09 | fixed-real | HIGH | — | spec § Acceptance (split per-commit / final-tree; deliverable gates) |
| R1-10 | fixed-real | MEDIUM | — | spec D7; § Out of scope (Fokus.Tests ships untouched) |
| R1-11 | fixed-real | MEDIUM | — | spec § Phases 1 (README fixes); § Open items O4; § Acceptance (fresh clone) |
| R1-12 | fixed-real | MEDIUM | — | spec § Phases 1 (backlog reconciled); § Open items O2 |
| R1-13 | fixed-real | MEDIUM | — | spec § Phases 5 (dead links); § Acceptance (links resolve) |
| R1-14 | fixed-real | MEDIUM | — | spec § Decisions A8; § What ships row 2; § Phases 4 |
| R1-15 | fixed-real | LOW | — | spec § Phases 2 (prerequisites); plan names the install |
| R1-16 | fixed-real | LOW | — | spec § What ships row 1 (reworded to the lean measure) |
| R1-17 | fixed-real | MEDIUM | — | spec § Phases 3; § Open items O2 |

Reconciled: § What ships ↔ § Out of scope ↔ D7 (tests); D5 ↔ A1–A7 ↔ § Constraints ↔ § Acceptance (scrub scope and checks); § Phases ↔ A2, A7, A8 (order: graphify removal after mining, map after campaign); O1 ↔ A4 (suite records never ship). Correction disclosed to the owner: the architect's earlier "no tests at all" claim was wrong — `Fokus.Tests` exists.

## Round 2 — 2026-10-13 — spec — primary (codex)

Header note: delta questions, full read (round 1's merged coverage left the front-end build and record bodies unopened). One reader by schedule (round ≥ 2). Verdict NO-GO, 7 MEDIUM, no CRITICAL — persisted verbatim in [review-critic-codex.md](review-critic-codex.md) § Round 2. Round-1 verification: 12 closed, 5 partial (R1-09, R1-10, R1-13, R1-15 addressed by R2 fixes below). Round 3 does not run: no CRITICAL in this merge.

### Merge — round 2

Coverage: complete

| id | readers | level | anchor | title | shape | consequence | evidence | untouchable | tag | surfaces | source ids |
|---|---|---|---|---|---|---|---|---|---|---|---|
| R2-01 | codex | MEDIUM | unranked — no anchor fits; Codex severity Medium | Historical front-end builds fail; gate conflicts with D3 | unsatisfiable gate | gate fails or history is falsified | F31 review.md:127; F33 implementation.md:91 | — | fold | Acceptance, D3 | codex-r2-1 |
| R2-02 | codex | MEDIUM | unranked — no anchor fits; Codex severity Medium | A1 narrows idea C's "nothing unusable" to the final tree | weakened guarantee | owner decision silently reinterpreted | ideas.md:21 | — | fold | A1, D5 | codex-r2-2 |
| R2-03 | codex | MEDIUM | unranked — no anchor fits; Codex severity Medium | Tool-name ban broader than A7, conflicts with A5 | contradiction | gate cannot pass without editorial rewrites | evaluations/architect-vs-planner.md; agenting-setup-reference.md | — | fold | A5, A7, Acceptance | codex-r2-3 |
| R2-04 | codex | MEDIUM | unranked — no anchor fits; Codex severity Medium | Phase 5 writes the guide before the rewrite | ordering contradiction | guide links rot | spec Phase 5 vs D6 | — | fold | Phases, D6 | codex-r2-4 |
| R2-05 | codex | MEDIUM | unranked — no anchor fits; Codex severity Medium | Open-work findings live only in private docs/tech-debt | lost deliverable | known open work never ships | mine-verify-repo output path | — | fold | Constraints, deliverable 1 | codex-r2-5 |
| R2-06 | codex | MEDIUM | unranked — no anchor fits; Codex severity Medium | `dotnet test` has no target from repo root | wrong command | tests never run | F32 review.md:68 | — | fold | D7 | codex-r2-6 |
| R2-07 | codex | MEDIUM | unranked — no anchor fits; Codex severity Medium | Retained records cite commit ids that will not resolve | dangling references | broken verification steps | adhoc-TeamManagedSync plan.md:80, review.md:74 | — | round-1 miss: feature-record bodies | O1, Acceptance | codex-r2-7 |

### Fold — round 2 (architect, claude-opus-5-5, 2026-10-13)

| id | disposition | level after | re-grade (level · anchor · why) | where |
|---|---|---|---|---|
| R2-01 | open (owner decision O5) | MEDIUM | — | spec § Open items O5; § Acceptance (build recorded, O5) |
| R2-02 | fixed-real | MEDIUM | — | spec A1 (states idea C's critique scope); listed for the owner in the morning report |
| R2-03 | fixed-real | MEDIUM | — | spec A7 (scope named); § Acceptance (grep scoped to .claude, CLAUDE.md, README.md) |
| R2-04 | fixed-real | MEDIUM | — | spec § Phases 5 (guide after rewrite) |
| R2-05 | fixed-real | MEDIUM | — | spec § What ships row 1; § Phases 2 (export); § Acceptance (open-work list) |
| R2-06 | fixed-real | MEDIUM | — | spec D7; § Acceptance |
| R2-07 | fixed-real | MEDIUM | — | spec A9; § Acceptance (commit ids) |

Reconciled: D3 ↔ O5 ↔ Acceptance (build gate); A1 ↔ D5 ↔ ideas.md C; A5 ↔ A7 ↔ Acceptance (tool grep); D6 ↔ Phase 5 ↔ deliverable 7; deliverable 1 ↔ Phase 2 ↔ Constraints (tech-debt private, open-work exported); R1-15 closed by Phase 5's filter-repo install.

## Round 1 — 2026-10-13 — plan — primary (codex)

Header note: primary is the Codex job (NO-GO, 13 findings), persisted verbatim in [review-critic-codex.md](review-critic-codex.md) § Plan review — round 1. Second reader's message was truncated in relay; full text salvaged from its transcript.

## Second reader — round 1 (sonnet)

# F36 Phase 1 Plan — Critic Review (second reader)

## Mode: Plan Review
## Verdict: REVISE
Two HIGH findings and no CRITICAL. The adversarial re-sweep is not triggered (it needs a CRITICAL or three or more HIGH). Phase 1 scope matches the spec's Phase 1 row, with one exception (M3). Nothing out of scope slips in.

## Pre-commitment predictions
- **Redaction rules corrupting code or library text:** I expected it and found it, but in prose records, not in `src/`. The bare `Reflekt` rule mangles identifier fragments (H2). `Adam` and `Taylor` are safe.
- **Sweep and acceptance gaps:** found (M1, M2, L3).
- **Must-NOT-Change rows not mechanical:** row 3 is review-only, and its command is also wrong as written (M4).
- **Scope slippage:** none found.
- **Wrong factual claims about the live repo:** one (L1).

## Coverage ledger
Round: 1 · Depth: mechanism · Bar: mechanics · Ruler: critic-calibration seed. No overlay: `docs/critic-calibration/ruler.md` is absent. · Read: full

| Requirement / area | Surfaces opened | Status |
|---|---|---|
| Step 1: ignore rules, untrack, hooks move | `.gitignore` (lines 52, 436–443); `.claude/settings.json` plus its uncommitted diff; `.claude/settings.local.json`; `.claude/hooks/*.js` | PARTIAL (L1). The hooks-only-in-local claim holds. |
| Step 2: map and exclusions | inventory; `git log --all -p` token histogram; `git log --all --name-only` path aliases | PARTIAL (H1, H2, M2, L2) |
| Step 3: A4 removals and link fixes | `git ls-files` for all 7 paths; `git grep` of removed names | COVERED. The 7 paths are tracked and only 5 kept files cite them. |
| Step 4: apply map | tree sweep; autocrlf and CRLF state; the files that would be mangled | PARTIAL (H2, M3, M4) |
| Step 5: README and scripts | `README.md:25-100`; `deploy.ps1:7-8`; `overnight-pipeline.ps1:1-10`; `package-lock.json` engines; `deploy-config.example.ps1` | COVERED (L5) |
| Step 6: backlog | `docs/backlog.md` (diff, 186–196, 296–318); `adhoc-SettingsGapFill/delivery/summary.md`; `client/src` greps | COVERED. Code evidence for GAP-1..3 exists. |
| Step 7: sweep | ran the exact sweep command | PARTIAL (L3, L4) |
| Must-NOT-Change (4 rows) | the plan's pin commands | PARTIAL (M4) |
| Spec Phase 1 row, A2/A3/A4/A6, Constraints, R1-01/02/07/11/12 | spec; `review-critic.md` fold table | COVERED |
| `dotnet build`, `dotnet test`, `npm ci && npm run build` | — | NOT OPENED. They write build output, and I was told not to modify files. The baseline at tag `pre-public-cleanup` is unverified. |
| `git filter-repo` behaviour | — | NOT OPENED. The tool is not installed (Phase 5). |

## Findings

### [HIGH] H1 — Exclusion list keeps only current path names; former names of removed files survive in history
**Anchor:** seed-0116 · above seed-0123 · below seed-0189
**Band:** HIGH–HIGH; the privacy class never downgrades
**Shape:** incomplete exclusion; former path names missed
**Consequence:** A private strategy document stays in older published commits under its former name, and the acceptance scan passes because it only checks current names.
**Evidence:** path + quoted
- `git log --all --name-status -- docs/specs/agentic-rituals-suite-strategy.md` shows `05bb172` adds it at `docs/specs/…` and `cf3f0a1` renames it with `R100` to `docs/product/…`. That is 6 commits. The blob at `05bb172` has 10 hits for Reflekt, Laurentiu, Omnishelf or sprint-rituals.
- `check-xray/Program.cs` and `check-xray/check-xray.csproj` exist from `6c168f6` to `11c3102`. Only `check-xray.ps1` is listed.
- Step 2 reads: "every A4 path (Step 3)". Step 3 lists current names only.
- Step 2 also skips two A4 paths that Step 3 never removes: `graphify-out/` and `.claude/settings.local.json`. Both are in spec A4 and in history, yet "A4 path (Step 3)" can be read as excluding them.
- Spec Acceptance says "No path from the exclusion list in any tree". That check cannot see a path that is not on the list.
**Surfaces:** plan § Step 2 (exclusions.txt), § Step 3; spec § Decisions A4, § Acceptance
**Untouchable:** security
**Fix:** Build `exclusions.txt` from `git log --all --name-only --format= | sort -u`, matched against each A4 file by `--follow` and by name. Add `docs/specs/agentic-rituals-suite-strategy.md` and `check-xray/`. List all 9 A4 paths explicitly, not "Step 3's paths". Add an Accept line: no path in the history path list matches the strategy, check-xray, suite or settings.local names unless it is on the list.

### [HIGH] H2 — The map is declared the single re-runnable scrub, but Step 4 allows unrecorded hand fixes, and the `Reflekt` rule mangles identifiers
**Anchor:** seed-0117 · above seed-0130 · below seed-0169
**Band:** MEDIUM–HIGH; the driver is whether Phase 5 compares the rewritten tree to this phase's tree. Spec Acceptance only token-scans.
**Shape:** contradictory instructions; non-reproducible transform
**Consequence:** The history rewrite produces different text than the cleaned tree, and replacements inside identifiers leave broken names in old snapshots.
**Evidence:** quoted + scenario
- Step 2: "the single re-runnable definition of the privacy scrub". Step 4: "a replacement that breaks a sentence … is fixed by hand". Only `Omnishelf` hand-picks are required to become literal rules.
- `Reflekt → a sibling project` is a bare literal. The kept tree has:
  - `docs/specs/adhoc-SkillCleanup/delivery/plan.md:1`, which becomes "Remove a sibling project-Specific References".
  - `plan.md:143` `{ReflektService}`, which becomes `{a sibling projectService}`.
  - `plan.md:152` `src/Services/Reflekt/Reflekt.*`, which becomes a broken path.
- History holds more identifier forms, none of them prose: `Reflekt.API.Features.Cards.CreateCard`, `ReflektModule`, `ReflektEndpoints`, `Reflekt.Persistence` (in `.claude/skills/create-domain-event-handler/workflows/Handler.md`) and `reflekt.db`. The lowercase form is not matched by a case-sensitive literal.
- Path rules are given only with trailing separators. `docs/specs/F12-CycleTime/delivery/lessons.md:13` has `` (`D:\src\fokus`) `` and `F13-BugRatio/delivery/lessons.md:15` has `"D:/src/fokus"`. An empty replacement yields `cd "" && dotnet ef`.
- The plan's "no bare word" ban lists only Adam, PD and Taylor. `Reflekt` is not on it.
**Surfaces:** plan § Step 2 (rules, Reflekt line), § Step 4, § Decisions row 3; spec § A2
**Fix:** Make every Step 4 hand edit an extra literal or context rule in the map. Split Reflekt into prose (`\bReflekt\b → a sibling project`) and identifier (`Reflekt(?=[A-Z.]) → Sibling`, plus `reflekt`). Add an explicit rule for `D:/src/fokus` and `D:\src\fokus` with no trailing separator, replaced by `.` or `<repo-root>`, never empty. Add an Accept line: the list of Step 4 hand edits is empty.

### [MEDIUM] M1 — "Agentic Rituals Suite" is a private-project name that is outside the inventory, the map and the sweep
**Anchor:** seed-0123 · above seed-0127 · below seed-0116
**Band:** MEDIUM–HIGH; the driver is whether the owner classes the suite name as private. The plan already genericizes `SprintRituals` and removes the suite strategy document as private.
**Shape:** inventory gap
**Consequence:** A private umbrella project name stays in shipped product docs while its sibling project names are removed, and the sweep cannot see it.
**Evidence:** quoted
- `docs/architecture/v1.md:11` "It is part of the Agentic Rituals Suite".
- `docs/product/v1.md:6`, `:331`, `:335` ("Suite consistency") and `:348` ("Agentic Rituals Suite memory layer").
- No sweep token matches "rituals".
**Surfaces:** plan § Step 2, § Step 7 (sweep); inventory § 3
**Route:** needs the owner's ruling, relayed by the architect, per the "do not assume" rule. Recommendation: treat it as private and add the name and a `suite consistency` rewrite to the map and the sweep. Confidence: medium. Two of the five hits are structural wording, not just the name.

### [MEDIUM] M2 — History-only tokens have no rule, and Phase 1 has no history check
**Anchor:** seed-0130 · above seed-0127 · below seed-0116
**Shape:** map coverage gap
**Consequence:** Tokens that exist only in old commits have no rule, so the leak is found at the last step instead of in this phase.
**Evidence:** path + scenario
- `git show d075998:.claude/settings.json` holds `Bash(*code-forge*)`. The map has `D:/src/code-forge` only, which does not match the bare word. Step 7's own sweep includes `code-forge`.
- The Step 2 Accept checks only the inventory's values and the tree sweep is tree-only. The inventory is not a history scan.
- A one-line history histogram needs no installs: `git log --all -p --format= -- . ":!client/package-lock.json" ":!graphify-out" | grep -a -o -i -E "<sweep regex>" | sort | uniq -c`. I ran it: 35 `D:/src/code-forge`, 4 bare `code-forge`, 6 lowercase `reflekt`.
**Surfaces:** plan § Step 2 (Accept), § Step 7; spec § Acceptance (every published commit)
**Fix:** Add to Step 2 Accept: every token returned by that histogram, outside excluded paths, has a rule. Add bare `code-forge`.

### [MEDIUM] M3 — The spec's F36 backlog row removal is not covered by any step
**Anchor:** seed-0129 · above seed-0127 · below seed-0116
**Shape:** unaddressed requirement; private tracking conflict
**Consequence:** A private-only backlog row for the cleanup either ships in the public copy or vanishes from the private tracker, because the plan never says which.
**Evidence:** quoted
- Spec § Constraints: "Backlog rows for F36 and the cleanup features are removed by the redaction step".
- The uncommitted `docs/backlog.md` diff adds a Tier 13 heading, the F36 row and the `**F36:**` paragraph.
- The plan neither delivers this nor lists it as not delivered.
- If the removal were a map rule, Step 4 would also delete the row from the private tree.
**Surfaces:** plan § Scope, § Step 4, § Step 6; spec § Constraints
**Route:** architect decides. Recommended: list it as "Not delivered in Phase 1; Phase 5 transform on the fresh clone only". Confidence: medium.

### [MEDIUM] M4 — Step 4's tooling is unspecified, and Must-NOT-Change row 3 is neither mechanical nor correct as written
**Anchor:** seed-0129 · above seed-0127 · below seed-0116
**Shape:** unspecified mechanism; unpinned invariant
**Consequence:** The tree scrub can silently garble text or line endings, and the only guard on feature records is a person reading a large diff.
**Evidence:** path + scenario
- `core.autocrlf=true`; `docs/product/v2.md`, `README.md` and `docs/backlog.md` are CRLF. The documents use em dashes and `ł`. The plan says "a throwaway script" with no language, encoding or regex dialect.
- Windows PowerShell 5.1 `Get-Content`/`Set-Content` without an explicit encoding garbles non-ASCII text.
- filter-repo uses Python `re`, so a Node or .NET script can diverge on the same rule.
- Row 3's command, `git diff pre-public-cleanup -- docs/specs ':!…F36…'`, also contains the full deletions of `adhoc-SuiteScaffolding/` and `adhoc-SuiteInfrastructure/`. "Every changed line is a replacement or link fix" is false as written.
**Surfaces:** plan § Step 4, § Must-NOT-Change row 3
**Fix:** Mandate Python 3 in bytes mode and the same rule parser for both phases. Add a replay check: apply the map to `git show pre-public-cleanup:<f>` and require byte equality with the working file, except for files on the hand-fix list. Exclude the two deleted folders in row 3.

### [LOW] L1 — `.claude/.current-agent` is already ignored
**Anchor:** seed-0127 · above seed-0153 · below seed-0123
**Shape:** wrong factual claim
**Consequence:** A rule is added twice, and the plan's evidence list is wrong about what the ignore file contains.
**Evidence:** quoted. `.gitignore:440` is `.claude/.current-agent`, inside the very block Step 1 names. `git check-ignore -v` confirms it. Plan Context line 14 says the file is not ignored.
**Surfaces:** plan § Context, § Step 1
**Fix:** Drop `.current-agent` from Step 1's additions.

### [LOW] L2 — The Step 2 Accept expects "none" but the plan keeps four value groups
**Anchor:** seed-0127 · above seed-0153 · below seed-0123
**Shape:** internal inconsistency
**Consequence:** The acceptance line cannot be satisfied as written, so the developer must reinterpret it.
**Evidence:** quoted. "Every distinct value … appears on the left of a rule … (expected: none, or each with a reason)". The plan and Decisions keep the owner name, `ldumit`, `/home/data/fokus.db`, and the public third-party names (Rakuten, TELUS, Zapier). That is at least four groups.
**Surfaces:** plan § Step 2 (Accept), § Decisions
**Fix:** Say "expected: the keep-list above".

### [LOW] L3 — The sweep cannot verify the bare `PD` rule, and `Pawe[lł]` only works by accident
**Anchor:** seed-0127 · above seed-0153 · below seed-0123
**Shape:** acceptance cannot verify a rule
**Consequence:** After redaction a leaked two-letter project key passes the sweep.
**Evidence:** quoted + path
- Step 2 requires "Jira project PD → ABC" (`docs/product/v2.md:8`). The sweep has `\bPD-[0-9]{3,5}\b` only. After the tenant is renamed, "project PD" would pass.
- `git grep -o "\bPawe[lł]\b"` returns `Pawe\xC5`, a single byte. Detection works. As a bytes-regex rule it would orphan `\x82`. Paweł appears only in a removed file.
**Surfaces:** plan § Step 7 (sweep), § Step 2
**Fix:** Add `project PD\b` to the sweep. Write the name class as `(Pawel|Paweł)`.

### [LOW] L4 — The plan's "hit 35 files" claim does not reproduce
**Anchor:** seed-0127 · above seed-0153 · below seed-0123
**Shape:** unverifiable figure
**Consequence:** A baseline number the reviewer cannot reproduce makes the later "sweep is clean" check harder to trust.
**Evidence:** I ran the exact sweep on the tracked tree: 30 files. All hits were in inventory files or in the kept `adhoc-SkillCleanup` and `adhoc-SkillEnforcement` Reflekt records.
**Surfaces:** plan § Step 7
**Fix:** State the count the tooling returns, or drop the figure.

### [LOW] L5 — README setup paths and a session-scratch input
**Anchor:** seed-0123 · above seed-0127 · below seed-0116
**Shape:** usability; fragile input
**Consequence:** A fresh-clone reader cannot follow one setup step, and the scrub's input list depends on a temp folder that may not survive.
**Evidence:** path
- Step 5's path rule names only `.credentials/` (gitignored, absent from the clone).
- `appsettings.Development.json` is gitignored (`.gitignore` "Environment-specific settings"). The README gives no directory, and Glob finds only `appsettings.json`.
- README step 3 (the deploy-config copy) is not needed for a local run.
- Step 1 copies the inventory from a session scratch path under `C:/Users/…/Temp/`.
**Surfaces:** plan § Step 5, § Step 1; `README.md` § Setup 3–4
**Fix:** Name `appsettings.Development.json` with its full path and the copy command. Mark README step 3 as deploy-only. Copy the inventory before anything else, or confirm it persists.

## Checked and fine
- Ignore rule line numbers 52, 436, 439, 443 are correct (L1 aside).
- All 7 A4 paths are tracked.
- The hooks exist only in `settings.local.json` (lines 26–46); the owner's uncommitted diff removes exactly the three code-forge entries, and `grep -c code-forge` is already 0.
- The README Node range `^20.19.0 || >=22.12.0` is the strictest engine in the lockfile; `package.json` has no `engines`.
- `overnight-pipeline.ps1:2-4` holds the F30, F31 and F27 slugs; `deploy.ps1:8` points at a path that does not exist.
- GAP-1..3 have matching client code and an I11 summary.
- There are no sweep hits under `src/` or `client/src/`, so Must-NOT-Change row 1 holds.
- The `Omnishelf` per-hit rule is handled by the plan.

## Open questions
- Is "Agentic Rituals Suite" private (M1)? Owner ruling needed.
- The `adhoc-SkillCleanup` records list `Submission/Review/Production/Auth/Journals/ArticleHub`. That is a Reflekt fingerprint that survives genericizing the name. Confidence: low that it matters.

Reviewed: D:\src\fokus\docs\specs\F36-PublicReleaseCleanup\delivery\plan.md
### Merge — round 1

Coverage: `dotnet build` / `dotnet test` / `npm ci` (neither reader ran them; the architect ran the full role at plan time: pass, and found `npm ci` failing — folded into Step 5); `git filter-repo` behaviour (not installed)

| id | readers | level | anchor | title | shape | consequence | evidence | untouchable | tag | surfaces | source ids |
|---|---|---|---|---|---|---|---|---|---|---|---|
| P1-01 | both | HIGH | seed-0116 | Exclusions list current names only; renamed and historical paths (strategy doc's old path, `check-xray/`, graphify-out, settings.local) survive | incomplete exclusion | private doc ships in old commits | 05bb172, cf3f0a1 R100; 6c168f6..11c3102 | security | scrub | Step 2, Step 3 | codex-5, S-H1 |
| P1-02 | both | HIGH | seed-0116 | Rules corrupt identifiers (Reflekt.*, {ReflektService}, lowercase), bare root paths, unrecorded hand fixes; ban check misses `literal:` forms | unsafe transform | broken identifiers, tree ≠ history scrub | adhoc-SkillCleanup plan.md:1,143,152; F12/F13 lessons | security | scrub | Step 2, Step 4 | codex-1, codex-3, S-H2 |
| P1-03 | both | HIGH | unranked — no anchor fits; Codex severity High | Bare `code-forge` and other history-only tokens have no rule; no history check in Phase 1 | coverage gap | leak found only at publish | d075998:.claude/settings.json | security | scrub | Step 2, Step 7 | codex-2, S-M2 |
| P1-04 | codex | HIGH | unranked — no anchor fits; Codex severity High | Redaction "every tracked file" crosses src/ and client/src/; pin misses staged/untracked | boundary breach | code changed by a doc scrub | plan Step 4, row 1 | — | scope | Step 4, Must-NOT-Change | codex-4 |
| P1-05 | both | contested: HIGH / MEDIUM → HIGH | unranked — no anchor fits; Codex severity High | F36 backlog rows: no step removes them from the public copy; a map rule would delete them from the private tree | missing step | private row ships or private tracker loses it | backlog.md Tier 13 | — | scope | Step 2, Step 6 | codex-6, S-M3 |
| P1-06 | both | MEDIUM | seed-0022 | Deny sweep blind to `project PD`, `Adam`, "Agentic Rituals Suite"; `Pawe[lł]` byte issue | weak gate | leaks pass the sweep | product/v1.md:6,348; architecture/v1.md:11 | security | gate | Step 7 | codex-7, S-M1, S-L3 |
| P1-07 | codex | MEDIUM | unranked — no anchor fits; Codex severity Medium | Step 7 drops D7's manual page check and adds a pre-existing-failure waiver | weakened guarantee | unverified app behaviour | spec D7 | — | safety | Step 7 | codex-8 |
| P1-08 | both | MEDIUM | seed-0022 | Record-preservation row is review-only and wrong (includes deleted folders); Step 3 grep rejects plain historical mentions; tooling/encoding unspecified | unpinned invariant | silent garbling of records | plan row 3; autocrlf=true, CRLF files | — | safety | Step 3, Step 4, Must-NOT-Change | codex-9, S-M4 |
| P1-09 | codex | MEDIUM | unranked — no anchor fits; Codex severity Medium | Hook acceptance proves key names only | weak gate | empty hooks pass | plan Step 1 | — | gate | Step 1 | codex-10 |
| P1-10 | both | MEDIUM | seed-0022 | README setup sequence unverifiable (cwd, appsettings path, .credentials, D8 statement) | weak gate | fresh clone fails | README:25-100 | — | usability | Step 5 | codex-11, S-L5 |
| P1-11 | codex | MEDIUM | unranked — no anchor fits; Codex severity Medium | `npm ci && npm run build` does not parse in PowerShell 5.1 | wrong command | step 7 fails spuriously | plan Step 7 | — | gate | Step 7 | codex-12 |
| P1-12 | both | LOW | seed-0123 | Evidence errors: .current-agent already ignored; "35 files" does not reproduce; Step 2 accept expects "none" though keep-list exists; scratch input path | evidence | reviewer cannot reproduce | .gitignore:440 | — | evidence | Context, Step 1, Step 2, Step 7 | codex-13, S-L1, S-L2, S-L4, S-L5 |

### Fold — round 1 (architect, claude-opus-5-5, 2026-10-13)

| id | disposition | level after | re-grade (level · anchor · why) | where |
|---|---|---|---|---|
| P1-01 | fixed-real | HIGH | — | plan § Context (historical names); Step 2 exclusions.txt (enumerated, historical names) + accept |
| P1-02 | fixed-real | HIGH | — | plan Step 2 (identifier rules first, root path forms, no hand edits, applier); Step 2 accept fixture; Step 4 |
| P1-03 | fixed-real | HIGH | — | plan Step 2 (bare code-forge rule); Step 7 history replay |
| P1-04 | fixed-real | HIGH | — | plan Step 4 protected set; Must-NOT-Change row 1 (status + diff) |
| P1-05 | fixed-real | HIGH | — | plan § Scope "Not delivered in Phase 1"; Step 2 publish-only-map.txt |
| P1-06 | fixed-real | MEDIUM | — | plan Step 2 (Agentic Rituals Suite, project PD rules); Step 7 sweep regex |
| P1-07 | fixed-real | MEDIUM | — | plan Step 7 (no waiver); Step 8 owner page check |
| P1-08 | fixed-real | MEDIUM | — | plan Step 3 (links only); Step 4 replay check; Must-NOT-Change row 3; applier bytes mode |
| P1-09 | fixed-real | MEDIUM | — | plan Step 1 accept (deep-equal hooks, no env/allow) |
| P1-10 | fixed-real | MEDIUM | — | plan Step 5 README bullets + accept |
| P1-11 | fixed-real | MEDIUM | — | plan Step 7 (Git Bash, verify-run.js --role full) |
| P1-12 | fixed-real | LOW | — | plan § Context evidence; Step 1; Step 2 accept; inventory copied into delivery/ |

Reconciled: Step 2 ↔ Step 4 ↔ Step 7 ↔ Must-NOT-Change row 3 (one applier, one map, replay checks); Step 2 publish-only map ↔ § Scope ↔ spec § Constraints (backlog rows); Step 8 ↔ spec D7; spec A3 ↔ Step 2 people/identifier rules.

## Round 2 — 2026-10-13 — plan — primary (codex)

Header note: delta questions, full read (round-1 merged coverage left builds and filter-repo unopened). One reader by schedule. NO-GO, 5 MEDIUM, no CRITICAL — verbatim in [review-critic-codex.md](review-critic-codex.md) § Plan review — round 2. Round-1 verification: 8 closed, 4 partial (P1-02, P1-07, P1-08, P1-11 — closed by the fold below). Round 3 does not run: no CRITICAL.

### Merge — round 2

Coverage: complete

| id | readers | level | anchor | title | shape | consequence | evidence | untouchable | tag | surfaces | source ids |
|---|---|---|---|---|---|---|---|---|---|---|---|
| P2-01 | codex | MEDIUM | unranked — no anchor fits; Codex severity Medium | filter-repo applies literals before regexes; file-order promise diverges | semantic mismatch | tree and history scrubs disagree | upstream git-filter-repo | security | fold | Step 2 | codex-p2-1 |
| P2-02 | codex | MEDIUM | unranked — no anchor fits; Codex severity Medium | In-place rerun cannot correct already-transformed text | broken workflow | rule fix never reaches the tree | fixture reproduced | — | fold | Step 2, Step 4 | codex-p2-2 |
| P2-03 | codex | MEDIUM | unranked — no anchor fits; Codex severity Medium | Replay compares LF blobs to CRLF files; `<(...)` unreadable by Windows Python | unsatisfiable gate | preservation unprovable | adhoc-SkillCleanup plan.md 334 CRLF lines | — | fold | Step 4 | codex-p2-3 |
| P2-04 | codex | MEDIUM | unranked — no anchor fits; Codex severity Medium | `--role full` needs `--slug`; full is also main-session only | wrong command | step 7 cannot pass | verify-run.js usage | — | fold | Step 7 | codex-p2-4 |
| P2-05 | codex | MEDIUM | unranked — no anchor fits; Codex severity Medium | Operator check lets Phase 1 complete without evidence; misses pages | weakened guarantee | D7 unmet | AppSidebar.vue:27 | — | fold | Step 8 | codex-p2-5 |

### Fold — round 2 (architect, claude-opus-5-5, 2026-10-13)

| id | disposition | level after | re-grade (level · anchor · why) | where |
|---|---|---|---|---|
| P2-01 | fixed-real | MEDIUM | — | plan Step 2 rule syntax (all-regex map) |
| P2-02 | fixed-real | MEDIUM | — | plan Step 2 applier `--from-ref` mode; Step 4 |
| P2-03 | fixed-real | MEDIUM | — | plan Step 2 applier (git show inside the script, CRLF handling); Step 4 replay (normalized compare) + idempotence; Must-NOT-Change row 3 |
| P2-04 | fixed-real | MEDIUM | — | plan Step 7 (fast profile; complete run at handover, main session); Must-NOT-Change row 2 |
| P2-05 | fixed-real | MEDIUM | — | plan Step 8 (all eight pages, synced data, stays unmerged and not Done until recorded) |

Reconciled: Step 2 ↔ Step 4 ↔ Must-NOT-Change row 3 (applier modes, replay, idempotence); Step 7 ↔ Must-NOT-Change row 2 ↔ verify roles contract; Step 8 ↔ spec D7.
