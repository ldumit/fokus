# Public Release Cleanup — Phase 1 (Clean)

**Feature Spec:** `docs/specs/F36-PublicReleaseCleanup/definition/spec.md`
**Model:** claude-opus-5-5

## Context

F36 prepares Fokus for a free public release. The tech-spec has five phases; this plan covers **Phase 1 only** — cleaning the private repo's working tree and writing the privacy scrub as one re-runnable definition. Phases 2–5 (mining, the lean campaign, the architecture map, publishing) get their own plans once their inputs exist: Phase 2 triage needs the owner, and Phase 5 needs open items O1, O3, O4 and O5.

Plan-time evidence (run 2026-10-13; plan review round 1 folded — record in `review-critic.md` § Round 1 — plan):
- Privacy inventory: `docs/specs/F36-PublicReleaseCleanup/delivery/identifier-inventory.md` (tree + full-history scan). Distinct values: `Omnishelf`/`OmniShelf`, `omniaz.atlassian.net`, `Alfamart` (history only); Jira keys `PD-4019`, `PD-4359`, `PD-4942`, `PD-5226`, `PD-5228`, `PD-5234`, `PD-5384` and bare key `PD` ("Jira project PD"); colleagues Taylor, Mateusz (in `docs/product/v2.md`) and Adam, Paweł, Ovidiu, Norbert, Calin, Maciej (only in the removed retro); paths `D:/src/fokus`, `D:\src\sprint-rituals`, `D:\src\dotnet-microservices`, `D:/src/code-forge`, `D:\Omnishelf\omnishelf-docs`, `C:/Users/Laurentiu`; private names `SprintRituals`/`sprint-rituals`, `Reflekt`/`reflekt` (prose and identifiers such as `Reflekt.API.Features.Cards.CreateCard`, `{ReflektService}`, `reflekt.db`), "Agentic Rituals Suite" (`docs/architecture/v1.md:11`, `docs/product/v1.md:6,348`); bare `code-forge` in `d075998:.claude/settings.json`; the slug example "shelf compliance".
- Historical names of the removed files (from `git log --all --name-only --format= | sort -u`): `docs/specs/agentic-rituals-suite-strategy.md` (renamed to `docs/product/…` in cf3f0a1), `check-xray/Program.cs`, `check-xray/check-xray.csproj`, `graphify-out/…`, `src/Services/Fokus/Fokus.API/wwwroot/…`.
- `.claude/settings.local.json` is tracked and is the only place the two in-repo hooks are registered. `.claude/settings.json` carries an uncommitted owner edit removing three `code-forge` deny entries — kept.
- `.gitignore` already ignores `graphify-out/` (443), `.omc` (436), `docs/audit/` (439), `.claude/.current-agent` (440) and `src/Services/*/*.API/wwwroot/` (52); not `.claude/audit/` or `.claude/settings.local.json`.
- `core.autocrlf=true`; `README.md`, `docs/backlog.md`, `docs/product/v2.md` are CRLF; docs contain em dashes and `ł`.
- Python 3.12 is installed (`py --version`; `python` resolves to it). `git filter-repo` is not installed (Phase 5).
- `.claude/verify.json` roles were declared at plan time (owner away, presumed): `verify-run.js --role full` passes in ~12 s (build, 19 tests, front-end build). `npm ci` in `client/` fails: lockfile out of sync with `package.json` (`EUSAGE`, "Missing: @emnapi/core@1.11.3 from lock file").
- Same-topic artifact: `docs/proposals/cc-setup-evaluation-and-plugin-extraction.md` (P1 row "`git rm --cached` settings.local.json") — superseded; the file is removed in Step 3.

## Scope

In scope (spec § Phases 1): ignore rules; hooks move; the A4 removals; the scrub definition (maps, exclusions, the applier); redaction of the working tree; README, script and lockfile fixes; backlog statuses reconciled with the code.

Out of scope: any change under `src/` or `client/src/`; graphify and oh-my-claudecode references in the shipped setup (A7 — Phase 5); the history rewrite and `git filter-repo` install (Phase 5); the guide (Phase 5).

**Not delivered in Phase 1:** removing the F36 backlog rows — they stay in the private tracker; `publish-only-map.txt` (Step 2) removes them from the public copy at Phase 5.

**Rule list:** not applicable — the tech-spec's header says so.

## Skill Mapping

| Step | Skill | Disposition | TDD | Feature-Specific Inputs | Gap? |
|------|-------|-------------|-----|------------------------|------|
| 1 | (none) | — | no | ignore rules, untrack, hooks move | — (one-off repo hygiene) |
| 2 | (none) | — | no | maps, exclusions, applier script | — (one-off) |
| 3 | (none) | — | no | A4 removals | — (one-off) |
| 4 | (none) | — | no | apply the privacy map to the working tree | — (one-off) |
| 5 | (none) | — | no | README, scripts, lockfile | — (one-off) |
| 6 | (none) | — | no | backlog status reconciliation | — (one-off) |
| 7 | (none) | — | no | safety net, sweeps, history replay | — (one-off) |
| 8 | (none) | — | no | owner's page check (Owner: operator) | — |

No pattern skill in the in-repo setup or the plugin covers repository hygiene, redaction or doc edits (checked against every skill description in context). Every step is wiring, tooling or documentation, so TDD is `no` throughout. The applier script (Step 2) is a private tool, checked by Step 7's replay checks rather than unit tests.

## Domain Model Changes

None.

## Data Model Changes

None.

## Implementation Steps

### Step 1 — Ignore rules, untrack local settings, move the hooks

Files: `.gitignore`, `.claude/settings.json`, `.claude/settings.local.json`.

- Add to `.gitignore` under the existing "Claude Code — local audit logs" block: `.claude/audit/`, `.claude/settings.local.json`, and the local pipeline-state files `.claude/.worktree-target` and `.claude/.approval-scope` (`.claude/.current-agent` is already ignored at line 440).
- Move the whole `hooks` object from `.claude/settings.local.json` into `.claude/settings.json`, byte-equivalent as JSON. Keep the owner's uncommitted removal of the three `code-forge` deny entries. Move nothing else: no `env`, no `permissions.allow`, no OMC or Atlassian entry (spec A6).
- Untrack `.claude/settings.local.json` (`git rm --cached`); the file stays on disk.

Accept:
- `git check-ignore .claude/audit/x .claude/settings.local.json` prints both paths.
- `git ls-files .claude/settings.local.json` prints nothing; `test -f .claude/settings.local.json` succeeds.
- `node -e "const a=require('./.claude/settings.local.json').hooks,b=require('./.claude/settings.json').hooks;process.exit(JSON.stringify(a)===JSON.stringify(b)?0:1)"` exits 0.
- `node -e "const s=require('./.claude/settings.json');process.exit(s.env||(s.permissions&&s.permissions.allow)?1:0)"` exits 0; `grep -c code-forge .claude/settings.json` prints `0`.

Satisfies: spec A6, § Constraints (machine-local paths)

### Step 2 — The scrub definition: two maps, the exclusion list, the applier

New files under `docs/specs/F36-PublicReleaseCleanup/delivery/`: `redaction-map.txt`, `publish-only-map.txt`, `exclusions.txt`, `apply-map.py`.

These are the single re-runnable definition of the privacy scrub (spec A2). **No hand edits:** every replacement the scrub makes comes from a rule; a hit that needs special wording gets its own rule placed before the general one.

- **Rule syntax** — `git filter-repo --replace-text`, written **only** as `regex:X==>Y` rules (Python `re`; a literal is written as an escaped regex). filter-repo applies all literal rules before all regex rules, so an all-regex map is the one form whose order — file order — is the same in `apply-map.py` and in filter-repo (plan review P2-01).
- **`apply-map.py`** — Python 3, bytes in, bytes out, applying a map's rules in file order with `re.sub` on bytes, as filter-repo does. Two input modes:
  - `--from-ref <ref> <path>...` reads each file's original bytes with `git show <ref>:<path>` (a `subprocess` call inside the script — no shell process substitution), applies the map, converts LF to CRLF where the working file uses CRLF (`core.autocrlf=true` checks blobs out as CRLF), and writes the working file. Re-running it after a map change regenerates from the original, so a corrected rule always applies to original text (P2-02).
  - `<path>...` (no ref) applies the map to the current working file in place — used only for the files Steps 1, 5 and 6 also edit (`.gitignore`, `.claude/settings.json`, `README.md`, `deploy.ps1`, `overnight-pipeline.ps1`, `docs/backlog.md`).
  - `--stdout` prints the result for one file instead of writing.
  Phase 1 uses it on the tree; Phase 5 hands the same map files to filter-repo.
- **`redaction-map.txt`** (privacy class — applied now and at publish), in this order:
  - Identifiers before prose: `regex:Reflekt(?=[A-Z.{}/])==>SampleApp`, `regex:reflekt(?=[.\-_/])==>sampleapp`, `regex:SprintRituals(?=[A-Z.])==>SampleSuite`, then prose `regex:\bReflekt\b==>a sibling project`, `SprintRituals`/`sprint-rituals` → `a sibling project` / `sibling-project`. Plan-time cases: `adhoc-SkillCleanup/delivery/plan.md:1` "Remove Reflekt-Specific References" → a rule giving "Remove Sibling-Project-Specific References"; `:143` `{ReflektService}` → `{SampleAppService}`; `:152` `src/Services/Reflekt/Reflekt.*` → `src/Services/SampleApp/SampleApp.*`.
  - "Agentic Rituals Suite" → "a wider tool suite" (owner away: treated as private, spec A3 — listed for the owner).
  - Paths, separator forms first: `D:/src/fokus/` and `D:\src\fokus\` → `` (empty); then bare `D:/src/fokus` and `D:\src\fokus` → `<repo-root>`; `C:/Users/Laurentiu` → `<home>`; `D:\src\sprint-rituals`, `D:\src\dotnet-microservices`, `D:/src/code-forge`, `D:\Omnishelf\omnishelf-docs` → `<private-repo>`; then bare `code-forge` → `private-repo`.
  - Employer and customer: `omniaz.atlassian.net` → `acme.atlassian.net`; `Omnishelf`/`OmniShelf`/`omnishelf` → `a former employer` (an extra rule, placed first, for any hit that needs other wording); `Alfamart` → `a customer`.
  - Jira keys: one fixed `PD-NNNN` → `ABC-NNN` table, the same key everywhere; `regex:(Jira )?project PD\b==>\1project ABC`.
  - "shelf compliance" / `shelf-compliance-kpis` → "invoice export" / `invoice-export`.
  - People: `regex:\bTaylor\b==>QA engineer A`, `regex:\bMateusz\b==>Developer B`. The other six names live only in removed files (exclusions) and get no rule.
- **`publish-only-map.txt`** (applied only at Phase 5, never to the private tree): removes the `### Tier 13 — Public Release` heading, the F36 row and the `**F36:**` paragraph from `docs/backlog.md`, as `regex:` rules anchored on those lines.
- **`exclusions.txt`** — one path per line, filter-repo `--paths-from-file` syntax (plain prefix, or `glob:`/`regex:` where a wildcard is needed), covering every historical name: `docs/product/agentic-rituals-suite-strategy.md`, `docs/specs/agentic-rituals-suite-strategy.md`, `docs/proposals/suite-integration-architecture.md`, `docs/proposals/cc-setup-evaluation-and-plugin-extraction.md`, `docs/evaluations/cross-project-skill-mining.md`, `docs/specs/adhoc-SuiteScaffolding/`, `docs/specs/adhoc-SuiteInfrastructure/`, `check-xray.ps1`, `check-xray/`, `graphify-out/`, `.claude/settings.local.json`, `src/Services/Fokus/Fokus.API/wwwroot/`, and the private records of spec § Constraints (`.claude/verify.json`, `docs/skill-gaps/`, `docs/tech-debt/`, `docs/critic-calibration/`, `docs/kb/research/`, `docs/specs/F36-PublicReleaseCleanup/`).

Accept:
- `py docs/specs/F36-PublicReleaseCleanup/delivery/apply-map.py --stdout docs/specs/F36-PublicReleaseCleanup/delivery/redaction-map.txt docs/specs/adhoc-SkillCleanup/delivery/plan.md | grep -n -E "SampleApp|Sibling-Project"` shows lines 1, 143 and 152 rewritten, and its output has no `a sibling projectService` or `a sibling project-`.
- No rule's left side is a bare name or key without context: `grep -n -E "^(literal:|regex:)?(\\\\b)?(Adam|PD|Taylor|Mateusz|Reflekt)(\\\\b)?==>" redaction-map.txt` prints nothing.
- Every historical name of a removed file is matched: for each line of `git log --all --name-only --format= | sort -u` matching `suite-strategy|suite-integration|cc-setup-evaluation-and-plugin|cross-project-skill|adhoc-Suite|check-xray|graphify-out|settings\.local|wwwroot`, some `exclusions.txt` entry is its prefix (the developer runs this as a one-liner and pastes the zero-miss result).

Satisfies: spec A2, A3, A4, R1-01, R1-04

### Step 3 — Remove the private files (A4)

`git rm` (recursive where a folder): `docs/product/agentic-rituals-suite-strategy.md`, `docs/proposals/suite-integration-architecture.md`, `docs/proposals/cc-setup-evaluation-and-plugin-extraction.md`, `docs/evaluations/cross-project-skill-mining.md`, `docs/specs/adhoc-SuiteScaffolding/`, `docs/specs/adhoc-SuiteInfrastructure/`, `check-xray.ps1`.

Then remove or unlink every markdown link to a removed file in kept docs (link text stays as plain text). Plain historical mentions in kept records stay — they are history, and Step 4 redacts any private name in them.

Accept:
- `git ls-files` lists none of the seven paths.
- `git grep -n -E "\]\([^)]*(agentic-rituals-suite-strategy|suite-integration-architecture|cc-setup-evaluation-and-plugin-extraction|cross-project-skill-mining|adhoc-SuiteScaffolding|adhoc-SuiteInfrastructure|check-xray)" -- . ":!docs/specs/F36-PublicReleaseCleanup"` prints nothing.

Satisfies: spec A4, R1-02

### Step 4 — Apply the privacy map to the working tree

Run `apply-map.py --from-ref pre-public-cleanup` with `redaction-map.txt` on every tracked file **except** the protected set — `src/`, `client/` (source and lockfile), `docs/specs/F36-PublicReleaseCleanup/` (names the identifiers by design), binary files — and except the six files Steps 1, 5 and 6 edit, which take the in-place mode after those steps. A hit the map renders badly is fixed by adding or reordering a rule and re-running `--from-ref` — never by hand.

Accept:
- Replay check (mechanical record preservation): for every file in `git diff --name-only pre-public-cleanup` outside the protected set and the six step-edited files, `apply-map.py --stdout --from-ref pre-public-cleanup` output equals the working file after both sides are normalized CRLF→LF. The developer runs it as one script and pastes the summary (files checked, mismatches: 0).
- Idempotence: applying `redaction-map.txt` a second time to every changed file changes nothing (so the in-place mode on the six step-edited files is safe to repeat).

Satisfies: spec A1 (privacy, working tree), A3

### Step 5 — README, scripts, lockfile

Files: `README.md`, `deploy.ps1`, `overnight-pipeline.ps1`, `client/package-lock.json`.

- README, every command runnable from a fresh clone in the order given, with its working directory stated:
  - clone line → `git clone <public-repo-url>` plus one note that the URL is set at publish (O4 open);
  - Node prerequisite → `^20.19.0 || >=22.12.0` (the lockfile's engines range);
  - credentials: `src/Services/Fokus/Fokus.API/appsettings.Development.json` named by full path, with the command that creates it from `appsettings.json`; any `.credentials/` use preceded by its `mkdir`;
  - the deploy-config copy marked as needed only for Azure deployment;
  - one plain statement: running the app needs your own Jira account (free tier works) and Google OAuth credentials; there is no sample data (spec D8).
- `deploy.ps1`: its reference to `.credentials/deploy-config.example.ps1` points at the real file (`deploy-config.example.ps1`, repo root) or tells the reader to copy it there.
- `overnight-pipeline.ps1`: a short header comment — it ran three features (F30, F31, F27) back to back, unattended, with the in-repo setup; edit the slug list to use it.
- `client/package-lock.json`: run `npm install` in `client/` to bring the lockfile in sync; `client/package.json` unchanged.

Accept:
- `grep -c "github.com/ldumit" README.md` prints `0`; `grep -c "20.19" README.md` ≥ 1; `grep -c "appsettings.Development.json" README.md` ≥ 1 and every such line names `src/Services/Fokus/Fokus.API/`; `grep -i -c "sample data" README.md` ≥ 1.
- `git diff --quiet pre-public-cleanup -- client/package.json` exits 0; `npm ci --prefix client` exits 0 (run from Git Bash).
- `head -5 overnight-pipeline.ps1` names F30, F31 and F27.

Satisfies: spec D5, D8, R1-11

### Step 6 — Reconcile backlog statuses with the code

File: `docs/backlog.md`.

GAP-1, GAP-2, GAP-3 are marked Open while I11 "Settings Gap Fill (GAP-1 + GAP-2 + GAP-3)" is Done. Confirm each against the code and `docs/specs/adhoc-SettingsGapFill/delivery/summary.md`; mark a confirmed one `Done (via I11)`, leave an unconfirmed one Open with a one-line note. BUG-1, BUG-2, GAP-4, TD-1, TD-2, I8 and the F36 row are not touched.

Accept: each of GAP-1..3 is `Done (via I11)` or Open with a note; implementation.md cites file:line evidence per verdict.

Satisfies: spec R1-12, O2 input

### Step 7 — Safety net, sweeps, history replay

Run from Git Bash:
- `node "<plugin>/hooks/scripts/verify-run.js" --profile fast` — the declared `unit` role (build + the 19 tests) — verdict `pass`; `npm ci --prefix client` and `npm run build --prefix client` exit 0. The complete suite (`roles.full`) is not the developer's: it runs once from the main session at handover (P2-04; verify roles contract).
- **Tree deny sweep** (expected empty):
  `git grep -n -i -E "omnishelf|omniaz|alfamart|\bPD-[0-9]{3,5}\b|project PD\b|shelf.compliance|sprint-rituals|SprintRituals|Reflekt|rituals suite|dotnet-microservices|code-forge|C:/Users/Laurentiu|D:[/\\\\]src|\b(Taylor|Mateusz|Adam|Pawel|Paweł|Ovidiu|Norbert|Calin|Maciej)\b" -- . ":!docs/specs/F36-PublicReleaseCleanup" ":!client/package-lock.json" ":!src" ":!client/src"`
  Any hit is library or non-person text listed with file:line in implementation.md (none expected). `src/` and `client/src/` are swept separately with the same regex and must print nothing (plan-time: nothing).
- **History replay** (proves the map covers history before Phase 5): extract every added line of `git log --all -p --format= -- . ":(exclude)client/package-lock.json"` whose file is not under an `exclusions.txt` entry, apply `redaction-map.txt` to those lines with `apply-map.py`, and run the deny regex on the result: zero hits. Paste the counts (lines checked, hits before, hits after).

Accept: the three commands' results as stated, pasted into implementation.md.

Satisfies: spec D7 (automated half), § Acceptance (privacy), R1-01

### Step 8 — Owner's page check (Owner: operator)

The manual half of D7 needs the owner's Jira data and Google sign-in, which a build agent does not have. With a synced sprint in the local database, the owner runs the app and opens every main page — Dashboard, Developers, Team, Sprints, Epics, QA, Cycle Time (`client/src/components/AppSidebar.vue:29-82`) and Settings — and records, per page, loads-with-data pass/fail in implementation.md under `## Operator check`. The developer writes `OPERATOR ACTION REQUIRED: page check (spec D7)` in implementation.md. The done-check marks the step `N/A (operator-owned)` and names it as open; Phase 1 stays on its branch, unmerged, and its backlog row is not marked Done until the operator check is recorded with every page passing (P2-05).

Satisfies: spec D7 (manual half)

## Cross-Service Changes

None.

## Migration Notes

None.

## Must-NOT-Change

| Invariant | Pinned by | Disposition |
|-----------|-----------|-------------|
| No application code changes | `git status --porcelain -- src client/src` prints nothing and `git diff --stat pre-public-cleanup -- src client/src` prints nothing | pinned |
| The build and the 19 tests pass | Step 7 fast profile; the handover complete run (`roles.full`, main session) | pinned |
| Kept records change only by map rules (no hand edits, no encoding change) | Step 4 replay check (byte equality after CRLF→LF normalization) and idempotence check; deleted folders are excluded by construction (they are not in the working tree) | pinned |
| The owner's local settings file survives on disk | `test -f .claude/settings.local.json` | pinned |
| `client/package.json` unchanged | `git diff --quiet pre-public-cleanup -- client/package.json` | pinned |

## Testing Strategy

No new tests (owner ruling). The existing suite and both builds run in Step 7; the scrub is proven by the Step 2 fixture, the Step 4 replay and the Step 7 history replay.

## KB Impact

None — `docs/kb/` changes only where Step 4 redacts an identifier.

## Decisions

| Decision | Why | Rejected alternative | Status |
|---|---|---|---|
| Phase 1 gets its own plan; Phases 2–5 are planned later | Their inputs (triage, O1, O3–O5) need the owner | One plan for all phases now | decided |
| One applier (`apply-map.py`, Python bytes mode) with filter-repo rule syntax | Same rules and regex dialect for the tree now and the history later; no encoding drift | A Node or PowerShell script (different regex dialect, encoding risk) | decided |
| `Reflekt`, `SprintRituals` and "Agentic Rituals Suite" are genericized (`SampleApp` in identifiers, "a sibling project" / "a wider tool suite" in prose) | Unannounced private projects; reversible | Keep the names | decided (listed for the owner) |
| The owner's name and GitHub handle stay in content | The owner is the course author | Anonymize the owner | decided |
| Real sprint figures in BUG-1, BUG-2, GAP-4 stay | Numbers without the employer's name identify no one | Rewrite the figures | decided |
| F36 backlog rows are removed only in the public copy (`publish-only-map.txt`) | The private tracker keeps the record (spec D2) | A privacy-map rule (would delete the private row) | decided |
| The manual page check is owner-run (Step 8) | It needs the owner's Jira data and Google sign-in | Skip it | decided |
| Verify roles declared at plan time without the owner (`.claude/verify.json`, step limit 30 s) | The lane's runs need them; reversible | Leave undeclared | decided (listed for the owner) |

## Open Questions

None for Phase 1. Phase-level owner items are in the spec's § Open items (O1–O5).

## Plan Review

Critic review, reader pair Codex + Sonnet (picked by the owner, `reader-pair.md`). Round 1: Codex NO-GO (13) + Sonnet REVISE (2 HIGH, 4 MEDIUM, 5 LOW) → 12 merged findings, all folded. Round 2 (Codex, MEDIUM floor): 5 MEDIUM on fold text, all folded; no CRITICAL, so no round 3. Record: `review-critic.md` § Round 1 — plan, § Round 2 — plan; Codex verbatim in `review-critic-codex.md`.
