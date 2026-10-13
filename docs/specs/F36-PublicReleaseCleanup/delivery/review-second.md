# Public Release Cleanup — Phase 1 (Clean) — Second Reader

## Verdict: APPROVED

Round 1 — first review (Step 2, second reader). Primary reader: Codex (its verdict is persisted in `review.md`; this file does not repeat or depend on it).

## Reviewed By
reviewer-second, independent read of the working tree against tag `pre-public-cleanup` plus the four new files under `delivery/`.
**Model:** claude-sonnet-5-5

No CRITICAL or HIGH finding. No privacy token survives in the tree or in any history blob after the maps and exclusions are applied. Two LOW evidence-accuracy findings and seven Open Questions (all below the 80 cutoff, or Phase 5 advice).

## Pre-commitment Predictions
| Predicted | Found |
|-----------|-------|
| Rules are exact-case while the deny sweep is case-insensitive, so the map covers today's content, not future edits | Confirmed (OQ2). Today: 0 hits. |
| A private doc renamed from an excluded path into a kept path would carry its content past the exclusions | Refuted. Of 2,198 (path, blob) pairs, 76 blobs sit under excluded paths; only 2 are shared with kept paths (`favicon.svg`, `icons.svg`, public assets). The one rename involving excluded names (`cf3f0a1`) goes excluded to excluded. |
| The Step 7 history replay (added lines of `git log -p`) misses merge-resolved or renamed content | Refuted. No merge commits (61 commits, 0 merges). My replay walks every blob of every (path, blob) pair from `git log --all -m --no-renames --raw`. |
| `apply-map.py` mangles CRLF or mixed endings | Refuted. All 21 redacted files equal `to_crlf(apply(blob))` byte for byte, none has a lone LF, and every original blob is pure LF. |
| The plan's own accept checks are mis-specified | Confirmed (F-1). |
| README is incomplete for a fresh clone | Partly: the commands are right; the Google redirect URI is missing (carry-over 4, confirmed). |

## Findings

### [LOW] Step 2 accept (2) is contradicted by the plan's own rules, and the recorded result is not reproducible
**File:** `docs/specs/F36-PublicReleaseCleanup/delivery/implementation.md:15`; `plan.md:84,90,96`; `redaction-map.txt:21,43,44`
**Origin:** design (the accept grep contradicts the rules the same step mandates), implementation (the evidence line reports it as passing)
**Issue:** `implementation.md:15` records "(2) bare-name grep prints nothing (exit 1)". Run with the backslashes intact (I used Python with `chr(92)`, because Bash through this harness collapses doubled backslashes into a word-boundary and then prints nothing), the plan's pattern `^(literal:|regex:)?(\b)?(Adam|PD|Taylor|Mateusz|Reflekt)(\b)?==>` matches three rules: `redaction-map.txt:21` `regex:\bReflekt\b==>a sibling project`, `:43` `regex:\bTaylor\b==>QA engineer A`, `:44` `regex:\bMateusz\b==>Developer B`. All three are exactly the rules `plan.md:84` and `:90` prescribe, so the map is right and the accept line is wrong. The "prints nothing" result came from a mis-escaped invocation.
**Fix:** Correct the evidence line to say the check matches the three plan-mandated `\bName\b` rules and passes by reading them; at the next plan edit, reword accept (2) to "no rule whose left side is a bare name without word boundaries or context".
**Confidence:** 90/100

### [LOW] Step 3 evidence counts eleven deletions; there are twelve
**File:** `docs/specs/F36-PublicReleaseCleanup/delivery/implementation.md:21`
**Origin:** implementation
**Issue:** "`git status` shows all eleven tracked files as ` D`". `git status --porcelain` lists twelve: 4 docs, `check-xray.ps1`, 6 under `adhoc-SuiteScaffolding/`, 1 under `adhoc-SuiteInfrastructure/`. The same file's Files line (6 + 1 + 5 paths) already adds up to twelve. No effect on the tree.
**Fix:** "twelve".
**Confidence:** 95/100

## Positive Observations
- The maps are a single re-runnable definition. `redaction-map.txt` is idempotent over all 1,926 text blobs in history (second pass changes none), and it alters no `src/` or `client/` blob in any commit, so no old snapshot's build can be affected by the scrub.
- Path rules before prose rules, with the reason recorded (`sprint-rituals` would otherwise pre-empt `D:\src\sprint-rituals`). The context rules land cleanly: all redaction contexts in redacted history read correctly ("You are the Architect for the SampleApp system", "Same as a sibling project", `<private-repo>\src\Services\`, `ABC-104-split-config`).
- The Jira key table is order-preserving and consistent across files (`PD-5228` -> `ABC-105` in every place it appears).
- `publish-only-map.txt` tempered body works on both CRLF and LF: applied to the working `docs/backlog.md` it yields the committed backlog plus the three GAP status edits, and the CRLF and LF results are identical.
- The applier fails loudly: bad ref, missing file and a non-`regex:` map line each exit 1 with a message.
- Fix Round 0 was a real self-review: two robustness defects in the publish map and one in the applier were found and fixed rather than dismissed.

## Gaps
- Author and committer identity (Gmail in all 61 commits, two display names), the `V1.0` and `pre-public-cleanup` tags, and the owner's handle in old README blobs (`github.com/ldumit/fokus.git`) are Phase 5 items (O3, "no tags") and the plan's decision to keep the owner's name. Not Phase 1 defects; the exclusion and map cannot fix them.
- The maps have not been run through `git filter-repo` (not installed, Phase 5). Equivalence rests on my reading of filter-repo's rule parsing and on the applier being bytes-in, bytes-out with the same NUL-in-first-8-KB skip; unproven end to end.
- Step 8 (page check) is operator-owned and open, as the plan says.
- The Google OAuth redirect URI is not in the README (carry-over 4).

## Open Questions
1. **"a former employer" may state something untrue.** `redaction-map.txt:29,30` turn "Omnishelf" into "a former employer"; the one surviving use is `docs/specs/adhoc-PipelineCraftImprovements/delivery/plan.md:7`. The spec (A3) says "the employer"; "former" is the plan's word. The owner's global setup describes `D:\omnishelf` as "company repos" (present tense), which suggests a current employer. Suggest the neutral "an employer" or "a company" if the owner still works there. Confidence 70/100; I cannot verify employment status from the repo. Origin would be design.
2. **The map covers today's content and one casing, not the deny sweep's coverage.** Rules for Reflekt, SprintRituals, Alfamart, PD keys, `project PD`, omniaz, "shelf compliance", Taylor and Mateusz are case-sensitive; the sweep is case-insensitive. Probes confirm `REFLEKT`, `reflekt db` (no following `.-_/`), `taylor`, `Sprint Rituals`, `sprint_rituals`, `ALFAMART`, `Project PD`, `pd-5226`, `OMNIAZ.ATLASSIAN.NET` all pass through unchanged. Path forms with no rule and no sweep coverage: `/d/src/fokus/...` (Git Bash, the form this repo's own lessons use), JSON-escaped `D:\\src\\fokus\\`, lowercase `d:/src/`. The OS username (`dumit`) is not in the sweep either. Today all of these are absent: I ran the plan regex plus `dumit`, `/d/src`, `C:.Users`, `D:.{1,4}src`, `omni`, `shelf`, `sprint.?rituals`, `code.?forge` and email and host patterns over every tracked and untracked file (986, none binary, none UTF-16) and every blob in history: 0 hits other than the owner's name in `LICENSE` and a generic `/d/src/...` in `docs/specs/F6-WorkflowAutoDetection/delivery/lessons.md`. Phases 2 to 4 will add new agent-written content (mining output, architecture map), where absolute paths in those forms are likely. Suggest the Phase 5 deny scan use this broader, case-insensitive regex. Confidence 65/100 on impact.
3. **The Step 4 driver and the Step 7 replay live outside the repo** (`D:\temp\f36\step4.py`, `replay.py`), and the list of files `--from-ref` must not be run on (the step-edited ones) exists only in `plan.md`. `--from-ref` write mode regenerates from the ref and silently discards working-tree edits; the usage text does not say so. Spec A2 asks for a repeatable scrub after late edits; copying both scripts into `delivery/` would make the Phase 5 acceptance mechanical. Confidence 70/100.
4. **Hooks use a relative command** (`.claude/settings.json:26,36`, `node .claude/hooks/...`). It is a verbatim move from `settings.local.json`, as the plan requires, and pre-existing. If a session's working directory is a subfolder, the script is not found; Claude Code's documented form is `$CLAUDE_PROJECT_DIR`. Not verified here. Confidence 45/100.
5. **Phase 5 will need one combined map.** I recall `git filter-repo --replace-text` taking a single expressions file, while the plan says Phase 5 hands both maps over. If so, the two files are concatenated, redaction first (all rules are `regex:`, so file order is preserved). Confirm when `git-filter-repo` is installed. Confidence 65/100.
6. **Domain terms of the sibling project remain** in the SkillCleanup records ("Article, Asset, Typesetter", "Submission/Review/Production/Auth/Journals/ArticleHub"). The project name is redacted; the terms hint at a scholarly-publishing product. Probably acceptable under the spec, which lists names only. Confidence 40/100.
7. **`docs/product/v2.md` keeps real employer figures** ("QA engineer A handles 9 of 10 TEs", Sprint 28). This is the plan's decision (numbers without the employer's name), noted so the owner sees it. Not a defect.

## Carry-Over Findings (implementation.md `## Carry-Over Findings`)
| Row | Verdict | Evidence |
|-----|---------|----------|
| npm: 6 high-severity advisories | Confirmed | `npm audit --prefix client` prints "6 high severity vulnerabilities" (vite among them); `npm ci --dry-run --prefix client` exits 0, so the lockfile is in sync. Owner call at Phase 2 triage (spec R1-17). |
| GAP spec files still say `Status: Open` | Confirmed | `docs/specs/GAP-1-excluded-statuses-ui/definition/spec.md:5`, `GAP-2-sprint-range-picker/definition/spec.md:5`, `GAP-3-sub-team-management-ui/definition/spec.md:5` all read `**Status:** Open`; `docs/backlog.md:304-306` now says `Done (via I11)`. Step 6 is scoped to the backlog. |
| Plain mentions of removed files in kept records | Confirmed | `docs/specs/adhoc-PipelineCraftImprovements/delivery/plan.md:3`, `docs/skill-backlog.md:68`, `docs/conventions/known-deviations.md:10` (also `adhoc-PipelineCraftImprovements/delivery/implementation.md:9` and `summary.md:19`). No markdown link, inline or reference-style, points at a removed file (Step 3 grep empty, exit 1). |
| README does not name the OAuth redirect URI | Confirmed, a real gap for the Phase 5 "setup paths work on a fresh clone" acceptance | The callback is `options.CallbackPath = "/auth/callback"` (`src/Services/Fokus/Fokus.API/Auth/AuthConfiguration.cs:40`); only `docs/deployment-azure.md:79` names it, for Azure. A student following the README gets a redirect-URI mismatch at sign-in. Suggest naming `http://localhost:5173/auth/callback` (browsing through Vite; `http://localhost:5000/auth/callback` if direct) in the Phase 5 usability pass. |
| `docs/product/v1.md` says .NET 9 / EF Core 9 | Confirmed | `docs/product/v1.md:331-332`; `CLAUDE.md` states .NET 10. Pre-existing; Phase 4. |

## Plan conformance and close-owned items
| Step | Result |
|------|--------|
| 1 | Conforms. `.gitignore` +4 lines (`.gitignore:441-444`), hooks equal to `settings.local.json` as JSON (node check exits 0), `settings.json` valid JSON, no `env`, no `permissions.allow`, no `code-forge`. `.claude/settings.local.json` is still tracked, so `git check-ignore` does not list it and `git ls-files` still does. |
| 2 | Conforms; two rules reordered ahead of the plan's list with the reason recorded. Exclusions: 18 paths as planned, prefix semantics match filter-repo's `filename_matches`. |
| 3 | Conforms. Seven targets removed from disk; link greps empty. The index still lists all twelve until staged. |
| 4 | Conforms. 21 files rewritten by the applier only; byte-exact; idempotent. |
| 5 | Conforms. README, `deploy.ps1`, `overnight-pipeline.ps1` header and lockfile as planned; `client/package.json` unchanged; lockfile registry URLs are `registry.npmjs.org` only. |
| 6 | Conforms. Spot-checked `JiraTab.vue:321`, `SyncTab.vue:39`, `TeamRow.vue:27,29` and the I11 summary. |
| 7 | Conforms; my independent replay is a superset of theirs (see Evidence). |
| 8 | Operator-owned, open. |

Close-owned, not findings: `git rm --cached .claude/settings.local.json`; staging the twelve deletions; adding the untracked `.claude/verify.json`, `docs/skill-gaps/` and the F36 folder (all three are on the exclusion list); until then Step 1's `git check-ignore`/`git ls-files` accepts and Step 3's `git ls-files` accept cannot pass.

Must-NOT-Change: `git diff --stat pre-public-cleanup -- src client/src` empty and `git status --porcelain -- src client/src` empty; `client/package.json` unchanged; `.claude/settings.local.json` on disk; Step 4 replay and idempotence reproduced independently.

## Evidence
| Check | Result | Command | Output |
|-------|--------|---------|--------|
| Run record | pass | `.claude/audit/verify-verdict.json`, role `full`, profile `complete`, slug F36-PublicReleaseCleanup | tree hash `02150e06acda4017038fa9d53cdb6746fab811829463b6be9d75f596185331ad`, 2026-10-12T23:36Z. Suite not re-run. |
| Tree sweep, plan regex | 0 hits | `git grep -n -i -E "<plan regex>" -- . ":!docs/specs/F36-PublicReleaseCleanup" ":!client/package-lock.json" ":!src" ":!client/src"` | empty. Same regex on `src`, `client/src`, and on the lockfile: empty. |
| Tree sweep, broad (Python) | 0 hits | plan regex + `dumit`, `ldumit`, `/d/src`, `/c/Users`, `C:.{1,4}Users`, `D:.{1,4}src`, `omni`, `sprint.?rituals`, `code.?forge` over `git ls-files --cached --others --exclude-standard` | 986 files, 0 binary, 0 UTF-16; hits: `LICENSE` owner name, one generic `/d/src/...` in F6 lessons |
| All-blob history replay | 171 hits before, 0 after | `git log --all -m --no-renames --raw --no-abbrev`, keep pairs outside `exclusions.txt` (filter-repo prefix rule), `git cat-file --batch`, the shipped `read_rules`/`apply_rules`, plan deny regex | 2,198 pairs, 2,120 kept, 1,852 unique kept blobs, 0 binary blobs |
| History, extra tokens after redaction | clean | same blobs + `dumit`, `omni`, `shelf`, `code.?forge`, `D:.{1,4}src`, emails, hosts, `@mentions`, issue-key prefixes, account ids, UUIDs, IPs | only placeholders (`@company.com`, `@example.com`, `@yourcompany.com`), the owner's name, `github.com/ldumit/fokus.git` (old README, decided), public hosts, key prefixes `FOK`/`PROJ`/`ABC`/`GAP`/`BUG`/`TD`/public Atlassian ids |
| Secret patterns, all blobs incl. excluded | none | Atlassian `ATATT`, GitHub, AWS, Slack, PEM, JWT, Google client/secret, Azure `AccountKey`, password assignments, 40+ hex | no match |
| Commit metadata | messages clean | `git log --all --format=%B`, `%an %ae %cn %ce`, `git tag -n`, all historic path names | no deny token in messages or paths outside excluded names; identity and tags are Phase 5 |
| Byte-exact tree check | 21/21 | per file: `to_crlf(apply_rules(git show pre-public-cleanup:path)) == working bytes` | ok 21, bad 0 |
| Idempotence | 0 non-idempotent | `apply_rules` twice over every text blob in history | 1,926 blobs, 0 |
| Blobs altered under `src/` or `client/` in history | 0 | kept blobs where `apply_rules(d) != d`, grouped by top-level path | only `.claude`, `docs/*`, `.gitignore` |
| Publish-only map | pass | `apply-map.py --stdout publish-only-map.txt docs/backlog.md` vs `git show pre-public-cleanup:docs/backlog.md` (CRLF-normalised); LF and CRLF inputs | only the 3 GAP status lines differ; no `F36` or `Tier 13` left; LF == CRLF result |
| Applier error paths | pass | missing file, bad ref, non-`regex:` map line | exit 1 each, clear message |
| Plan accept 1 (SkillCleanup lines) | pass | `apply-map.py --stdout redaction-map.txt docs/specs/adhoc-SkillCleanup/delivery/plan.md` | lines 1, 143, 152 (also 290, 316); `a sibling projectService`/`a sibling project-` count 0 |
| Plan accept 2 (bare-name rules) | does not print nothing | Python port of the plan grep | lines 21, 43, 44 (F-1) |
| Renames, copies and shared blobs | clean | `git log --all -M30% -C --find-copies-harder`; blob-id overlap between excluded and kept paths | one rename (`cf3f0a1`) excluded to excluded; 2 shared blobs, both public SVG assets |
| `npm ci` lockfile sync | pass | `npm ci --dry-run --prefix client` | exit 0 ("up to date") |
| src/client untouched | pass | `git diff --stat pre-public-cleanup -- src client/src`; `git status --porcelain -- src client/src` | empty |

Housekeeping: my first import of `apply-map.py` wrote an ignored `delivery/__pycache__/`; I removed it. My scratch scripts are in `D:\temp\f36-review\` (outside the repo). No other file was edited.

*Status: COMPLETE — reviewer-second, 2026-10-13*
