# F36 — Public Release Cleanup (tech-spec)

**Status:** Ready — review rounds 1–2 folded 2026-10-13 (owner away; see Open items)
**Type:** Technical — the architect owns the definition (no product "what" to shape)
**Rule list:** not applicable — the owner ruled out business-rule records; nothing here commits rule-shaped behaviour
**Plan:** [Phase 1 plan](../delivery/plan.md) (Phases 2–5 planned later)
**Owner:** Laurentiu · **Date:** 2026-10-13 · **Restore point:** tag `pre-public-cleanup` (d075998)
**Ideas:** [ideas.md](ideas.md) · **Review record:** [review-critic.md](../delivery/review-critic.md)

## Goal

Offer Fokus to Udemy students for free as "a repo built entirely with agents". The code and the architecture are the focus; the repo carries only what a developer needs.

## What ships

| # | Deliverable | Done when |
|---|---|---|
| 1 | Code with its high-severity problems fixed | The lean campaign (D9) is closed; every other finding is listed in `docs/known-open-work.md` (shipped, plain wording) |
| 2 | Architecture map | Mined after the campaign (Phase 4), so it describes the code as shipped |
| 3 | Product documentation | `docs/product/` refreshed; strategy material removed; people and employer identifiers redacted |
| 4 | Code conventions | `docs/conventions/` matches the code as shipped |
| 5 | In-repo AI setup | `.claude/` works in a fresh clone: hooks registered in the committed `settings.json`; no references to tools students do not have |
| 6 | Feature records | `docs/specs/` — how many stay (4, 10 or all) is open (O1); the suite records never ship (R1-02) |
| 7 | "How this repo was built" guide | `docs/how-it-was-built.md`, linked from the top of the README, written last |

## Out of scope

- Writing new tests (owner ruling 2026-10-13). The existing `Fokus.Tests` project (19 tests) ships untouched and is run as part of the safety net (D7).
- Business-rule registries (owner ruling 2026-10-13)
- The Nexus plugin as students' setup — maybe in a few weeks, as a separate feature
- Nexus's own records in the public repo (see Constraints)
- Sample data or a demo mode (D8)

## Decisions

Owner decisions, 2026-10-13:

| # | Decision |
|---|---|
| D1 | Students use the in-repo setup; Nexus stays usable on the repo (same `docs/specs/{slug}/definition|delivery` layout — verified; BUG/GAP folders carry `definition/` only) |
| D2 | Nexus does the mining and the cleanup; its records stay in the private repo and are left out of the public copy |
| D3 | Public history is rewritten feature by feature: ~45 commits, small plan/build/fix commits merged per feature, setup commits kept as "setup" commits, clear messages; bundled multi-feature commits stay bundled. The cleanup's own work lands as final commits at the end |
| D4 | Published as a new public repo; this repo stays private and is never rewritten |
| D5 | Scrub (idea C): personal and private material goes; `deploy.ps1` + `deploy-config.example.ps1` (placeholders only) and `overnight-pipeline.ps1` (with a short note) stay. Scope is defined by the two classes below (R1-01, R1-04) |
| D6 | Guide (idea F) is a docs page linked from the README, written after the cleanup and the history rewrite |
| D7 | Safety net for every cleanup change: `dotnet build src/Fokus.slnx`, `dotnet test src/Fokus.slnx` (the existing 19 tests), `npm ci && npm run build` in `client/`, and the manual page checklist the plan defines (each main page loads with synced data, result recorded) |
| D8 | No sample data or demo mode: the README states plainly that running the app needs your own Jira account (free tier works) and Google sign-in credentials |
| D9 | Lean campaign: one cleanup feature fixing only high-severity findings (~8 steps); every other finding is listed as known open work |

Architect decisions taken while the owner was away (2026-10-13, two-way doors — the owner may reverse any):

| # | Decision | Why |
|---|---|---|
| A1 | The scrub has two classes. **Privacy** (people, employer, machine paths, private projects) is removed from **every** published commit. **Usability** (references to tools students lack, dead hooks, wrong README steps, dead links) is fixed in the **final** tree only. Idea C's "every commit" critique concerned privacy; students work from the final tree, and older snapshots are read as history | Old snapshots showing how the setup evolved are the story; private data in them is not |
| A2 | Privacy scrub is a re-runnable transform — a path-exclusion list plus a redaction map — applied with `git filter-repo` to a fresh clone at publish time. The same redaction is applied to the working tree in Phase 1 so the final tree is clean too | One mechanism, checkable, repeatable after late edits; the private repo is never rewritten (D4) |
| A3 | People are anonymized to role labels ("QA engineer A"); the employer's Jira tenant becomes `acme.atlassian.net`; its project key `PD` becomes `ABC`; personal paths become repo-relative or `<home>` | Keeps the records readable and the examples real-looking |
| A4 | Removed outright, from every commit: `docs/product/agentic-rituals-suite-strategy.md`, `docs/proposals/suite-integration-architecture.md` (pasted sprint report with nine named people), `docs/proposals/cc-setup-evaluation-and-plugin-extraction.md` (private plugin plans), `docs/evaluations/cross-project-skill-mining.md` (analysis of the employer's internal skills), `docs/specs/adhoc-SuiteScaffolding/`, `docs/specs/adhoc-SuiteInfrastructure/`, `graphify-out/`, `.claude/settings.local.json`, `check-xray.ps1` (machine-specific debug script) | Each is private or about another project; redaction would leave little worth keeping |
| A5 | Every other doc ships, redacted: the remaining proposals, `docs/research/`, `docs/agentic-setup-evaluation.md`, `docs/agenting-setup-reference.md`, `docs/skill-backlog.md`, `docs/deployment-azure.md`, `docs/kb/`, the other evaluation file | The owner leans to keeping the full story (ideas B and E were parked, not killed) |
| A6 | The two hooks registered only in `settings.local.json` move into the committed `.claude/settings.json`; personal permissions (OMC, Atlassian MCP) do not | Students must get a working setup (R1-06) |
| A7 | Graphify and oh-my-claudecode references are removed from the shipped setup (`.claude/`, `CLAUDE.md`, `README.md`) in the final tree, after mining (the miner uses the graph tool); Atlassian MCP stays as an optional, documented prerequisite of the PO agent; Codex references stay (they degrade gracefully) | Graphify and OMC are not part of students' setup; Jira fetch is optional |
| A9 | Retained records that cite commit ids (`git show <sha>`, "commit <sha>") get the id remapped through the rewrite's commit map, or the note "(commit from the original private history)" where no single new commit corresponds | References stay honest after the rewrite (R2-07) |
| A8 | The architecture map is mined after the campaign; its `Basis:` revision and Nexus-only vocabulary are rewritten to plain terms at publish | The map must describe the shipped code and resolve in the public history (R1-14) |

## Phases

| Phase | Work | Output |
|---|---|---|
| 0 | Restore point | Tag `pre-public-cleanup` — done |
| 1 | Clean (working tree) | Identifier inventory (every person, tenant, key, path, private project found by grep over the tree and history); the redaction map and exclusion list (private files); redaction applied to the working tree; A4 removals; A6 hooks; ignore rules (Constraints); README fixes (clone URL placeholder, Node version from the lockfile, credentials paths, deploy path); backlog statuses reconciled with the code (R1-12) |
| 2 | Mine code quality | `mine-verify-repo` run (prerequisites: Java for Code Maat or its documented fallback; graphify graph generated); owner sorts the findings into the lean campaign (D9) or known open work; the open-work rows are exported in plain wording to `docs/known-open-work.md`, since `docs/tech-debt/` stays private |
| 3 | Lean campaign | One cleanup feature: the high-severity findings, plus candidates TD-1 (cookie not marked Secure) and the high-severity package advisories (R1-17) if the owner agrees at triage |
| 4 | Map and conventions | `mine-architecture` run on the post-campaign code; `docs/conventions/` and product docs updated to match |
| 5 | Publish | Final-tree usability pass (A7, dead links in retained records) → fresh clone → `git filter-repo` (installed with `pip install git-filter-repo`, on D:) with the exclusion list and redaction map → history regrouped per D3 → commit ids remapped (A9) → the guide written and committed on top (D6) → acceptance checks → new public repo, created only on the owner's word |

## Constraints

- Records shipped to students stay in the in-repo setup's formats.
- Machine-local paths, ignored in the private repo from Phase 1: `.claude/audit/`, `.claude/settings.local.json`, `.claude/.current-agent`, `graphify-out/`.
- Private records, committed to the private repo (D2) and excluded from every published commit by the Phase-5 exclusion list: `.claude/verify.json`, `docs/skill-gaps/`, `docs/tech-debt/`, `docs/critic-calibration/`, `docs/kb/research/`, `docs/specs/F36-*` and every cleanup feature's folder, the redaction map and exclusion list themselves, and historical build output (`src/Services/*/*.API/wwwroot/`). Backlog rows for F36 and the cleanup features are removed by the redaction step (a content edit, not a path rule).
- The architecture map ships (deliverable 2).
- No push, no public repo, no tag carried into the public copy without the owner's word.

## Open items (owner)

| # | Item | Needed by | Recommendation |
|---|---|---|---|
| O1 | Which feature records stay: 4, 10 or all. Candidate ten: F1, F4, F7, F10, F18, F22, F25, F32, adhoc-ExtractJiraModule, adhoc-TeamManagedSync | Phase 5 | All — owner's lean; the suite records are excluded regardless (A4) |
| O2 | Lean-campaign scope, from the Phase-2 findings plus the reconciled backlog (BUG-1, BUG-2, GAP-4, TD-1, TD-2, I8, package advisories) | Phase 2 triage | Fix TD-1 (one line, security) and the advisories; list the rest as open work |
| O3 | Author identity in the public history: keep the Gmail address, or use a GitHub noreply address; merge `ldumit` and `Laurentiu Dumitrescu` into one name | Phase 5 | GitHub noreply, one name |
| O4 | Public repo name and owner, and whether the private `fokus` repo is renamed first | Phase 5 | Decide with O3 |
| O5 | Some old snapshots do not build (F31 broke the front-end build; F33 had three failures, fixed by F34). D3 asked for every snapshot to build | Phase 5 | Keep history honest: record which snapshots fail and tell it in the guide ("F31 broke the build, F34 fixed it"); repairing old code would falsify the history |

## Acceptance

**Every published commit** (scanned with `git log --all -p` and `git log --all --format='%an %ae %cn %ce'` on the public repo):
- No token from the redaction map's deny list (employer name, tenant, `PD-` keys, colleague names, personal paths, private project names), scanned case-insensitively and in every path form (`D:\src`, `D:/src`, `/d/src`, JSON-escaped `D:\\src`) plus the Windows user name `dumit`
- No path from the exclusion list in any tree
- Author and committer identity as decided in O3; no tags unless the owner names one
- Every commit that contains `src/Fokus.slnx` is built (`dotnet build src/Fokus.slnx`; `npm ci && npm run build` in `client/`) and the result recorded; the first two commits hold no code and are exempt; failing snapshots are handled per O5

**Final tree:**
- `git grep -i -E "graphify|oh-my-claudecode|\bOMC\b"` `-- .claude CLAUDE.md README.md` returns nothing (commentary about these tools in `docs/` may stay, as history)
- `.claude/settings.json` registers both hooks; a fresh clone starts a session with them firing
- README clone URL, prerequisites (Node version, Jira, Google credentials) and setup paths work on a fresh clone
- Deliverables 2, 3, 4 and 7 exist and are dated after the campaign closed; `docs/known-open-work.md` lists every finding the campaign did not fix
- Every relative link in the README, the guide and the retained feature records resolves; every commit id cited in a retained record resolves in the public history or carries the A9 note
- `dotnet test src/Fokus.slnx` passes
