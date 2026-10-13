# F36-PublicReleaseCleanup — Lessons

## Architect Lessons

- **Index-state accepts in a no-git-writes lane need `Owner: close` at plan time.** Steps 1 and 3 asked for `git rm` / `git rm --cached` with `git ls-files` and plain `git check-ignore` accepts, but the architect-led lane forbids developer git writes, so those accepts could only be met at close — the coordinator had to explain it in the done-check brief. When a plan for this lane changes the index, split the step: the developer does the disk-side work with `--no-index` checks, and the index accepts are marked close-owned in the plan itself. (Evidence: [F36-PublicReleaseCleanup])
- **A privacy scrub needs a history-shaped inventory before the spec is written.** The first tech-spec scrub named one literal word; both reviewers found the employer leaking through its Jira tenant, ticket keys, colleague names, a pasted retro and machine paths, plus renamed file paths in history. Run the identifier inventory (tree + `git log --all -p` + `git log --all --name-only`) before drafting the scrub rule, not after review. (Evidence: [F36-PublicReleaseCleanup] review-critic.md R1-01, P1-01)
- **Verify a "no tests" claim with a case-insensitive project-file glob, not a capped path grep.** The architect told the owner the repo had no tests; `Fokus.Tests` (19 tests) existed — a `head`-capped grep of lowercase paths missed it. Disclosed and corrected in the spec. (Evidence: [F36-PublicReleaseCleanup])
- **Don't assume an owner's employment status in redaction wording.** "a former employer" was chosen without basis; the owner's own setup calls the folder "company repos". Neutral wording ("an employer") is the safe default. (Evidence: [F36-PublicReleaseCleanup] review.md fix item 9)
- **Declaring verify roles surfaced a real student-facing defect.** Timing `full` with `npm ci` failed on a lockfile out of sync with package.json — the same failure a student's first install would hit. Run the fresh-clone install path when declaring roles. (Evidence: [F36-PublicReleaseCleanup])

- **A Vite proxy written as a plain string sets `changeOrigin: true`.** The backend then builds OAuth callbacks from its own host and port (5000), not the dev server's (5173); the first README fix documented the wrong port and a follow-up review caught it. Verify a documented callback against the proxy options and the bundled Vite source. (Evidence: [F36-PublicReleaseCleanup] fix cycles 1–2; relayed from the cycle-2 developer, who could not write this file)

## Skill Gaps

- **Repository privacy scrub / public-release preparation.** No skill covers building an identifier inventory, a filter-repo-compatible redaction map, an exclusion list from historical path names, and the tree/history replay checks. Built ad hoc in F36 (`delivery/apply-map.py`, maps, scripts). Candidate for a skill once Phase 5 proves the history rewrite end to end. (Evidence: [F36-PublicReleaseCleanup])

## Reviewer Lessons

- The Bash tool collapses doubled backslashes in a command (`\b` reaches grep as `\b`, a word boundary), so a plan accept grep written for literal backslashes silently prints nothing and reads as a pass. F36 Step 2 accept (2) was recorded as "prints nothing" while three plan-mandated `\bName\b` rules match. Check any backslash-bearing accept from Python with `chr(92)`, and write plan accepts for patterns that do not need a literal backslash.
- A history replay that reads only the added lines of `git log -p` is weaker than reading every (path, blob) pair from `git log --all -m --no-renames --raw`: the second also covers renames out of excluded paths and merge-resolved blobs. Use it for privacy sweeps.

## Developer Lessons

- **A Bash heredoc does not protect backslashes either.** Writing a regex-bearing script with `cat > f <<'EOF'` through the Bash tool still collapsed `\\\\` to `\\`, so the re-run grep printed nothing again. Write such scripts with the Write tool and check the bytes (`od -c`) before trusting an empty result. (Evidence: [F36-PublicReleaseCleanup] Fix Round 1 item 7)
- **Keep check scripts in the repo and re-run recorded probes from the kept copy.** The Fix Round 0 publish-map probe was recorded as passing, but re-running the same script in Fix Round 1 showed one case had never matched its expected bytes (off by a blank line). A probe that lives only in a scratch folder cannot be audited. (Evidence: [F36-PublicReleaseCleanup] Fix Round 1, `delivery/scripts/pubtest.py`)
