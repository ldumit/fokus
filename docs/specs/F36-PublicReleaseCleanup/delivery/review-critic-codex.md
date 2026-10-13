# Codex critic — F36 tech-spec, round 1 (2026-10-13)

Persisted by the architect (main session) from the read-only Codex job.

## Verdict: NO-GO

Locations below refer to [spec.md](D:/src/fokus/docs/specs/F36-PublicReleaseCleanup/definition/spec.md).

1. **High — spec.md:41, D5: Employer data is missing from the scrub scope.** [product/v2.md](D:/src/fokus/docs/product/v2.md:8) contains the live Jira hostname `omniaz.atlassian.net`, real ticket IDs, internal feature names, and named employees’ workload counts (lines 218, 342, 482). Ticket examples also appear in F25 records and the AI setup. Removing the literal employer name and suite strategy leaves this material exposed. Require anonymization across every published snapshot.

2. **High — spec.md:57: The private-only list omits private suite feature records.** `docs/specs/adhoc-SuiteScaffolding/` and `docs/specs/adhoc-SuiteInfrastructure/` contain plans and delivery records for the private SprintRituals repository, including its architecture and unavailable reference projects. Neither folder is excluded by the listed paths. O1’s “all” option would publish them despite D5’s strategy-removal decision.

3. **High — spec.md:41, D5: Personal machine paths remain outside the named scrub.** [check-xray.ps1](D:/src/fokus/check-xray.ps1:1) hardcodes DLL and database paths under `D:/src/fokus`. Retained feature reviews and lessons contain absolute build paths. Earlier snapshots also contain `D:/src/code-forge` in shared settings and a private `dotnet-microservices` reference in `service-registration`. Define a repository-wide path scrub covering retained records and historical configurations.

4. **High — spec.md:39–41: Personal email addresses in Git metadata are unaddressed.** `git log --all` shows the owner’s Gmail address in author and committer fields. File scrubbing and the listed acceptance commands do not remove or detect it. D3 needs an explicit metadata policy and verification.

5. **High — spec.md:72: The Graphify check contradicts the owner’s history-wide scrub decision.** It checks only the final tree, whereas ideas C explicitly requires every rewritten snapshot scrubbed. `graphify-out/graph.json` and `GRAPH_REPORT.md` were tracked in `05bb172` and contain absolute machine paths. Their current `.gitignore` exclusion does not remove historical blobs. Require absence across all published commits and refs.

6. **High — spec.md:39, 73: D3 does not resolve the pre-scaffolding commits.** `5de00f3` contains only `.gitignore`, LICENSE and README; `d030d9b` adds specs and setup. Neither contains `src/Fokus.slnx` or `client/package.json`; those arrive in `085e18a`. Keeping these root/setup snapshots separately conflicts with the stated build gate. Define a buildable bootstrap grouping/root or an explicit applicability exception.

7. **High — spec.md:22, 41: Untracking local settings removes the only hook registrations.** `SessionStart` and `PreToolUse` registrations exist solely in [settings.local.json](D:/src/fokus/.claude/settings.local.json:26); shared `settings.json` contains only permissions. Students would receive hook scripts without role restoration or audit logging being enabled. Specify preservation of portable hooks in shared configuration and verify them in a fresh clone.

8. **Medium — spec.md:41: “Untrack” does not establish a durable exclusion.** `.gitignore` has no rule for `.claude/settings.local.json`, and `git check-ignore --no-index` finds no matching exclusion. A later add can retrack it. D5 should require an ignore/exclusion rule as well as removal from published history.

9. **Medium — spec.md:22, 41: External setup assumptions survive the scrub.** `.claude/README.md` and `pipeline-guardrails.md` advertise available OMC specialists; `po.md` directs agents to fetch Jira and Confluence content through Atlassian MCP. The spec defines neither student prerequisites nor fallbacks for these integrations. Removing Graphify references alone does not establish a usable student setup.

10. **Medium — spec.md:68–74: Acceptance mixes historical and final-release checks and omits deliverable gates.** The “every commit” heading includes guide existence, conflicting with D6 and ideas F’s requirement to write it last. Separate snapshot checks from final-tree checks. Acceptance also lacks checks for the architecture map, refreshed product documentation and conventions, selected feature records, the README’s top guide link, and the overnight-script note.

11. **Medium — spec.md:43: The only runtime safety check is insufficiently defined.** “Quick click-through” specifies no routes, representative data, expected outcomes or recorded evidence. With tests explicitly excluded, define a minimal reproducible manual checklist and require its result for each cleanup feature.

12. **Medium — spec.md:50–53, 73: Fresh-clone usability is not covered.** README still points students to the repository D4 says will remain private, advertises Node 18 although the lockfile requires `^20.19.0 || >=22.12.0`, and copies into an uncreated `.credentials/` directory. `deploy.ps1` directs users to a nonexistent `.credentials/deploy-config.example.ps1`; the example is at the root. Include startup-documentation corrections and dependency installation in clean-checkout verification.

13. **Medium — spec.md:66, O2: The triage inventory is incomplete and stale.** Backlog also lists TD-2 and planned I8. GAP-1–3 remain marked Open despite I11 being Done, its completion summary documenting those implementations, and corresponding UI existing in the live code. Reconcile these statuses before deciding campaign scope or publishing the open-work list.

14. **Medium — spec.md:23, 37, 65: Retained feature records contain unusable active references.** Candidate records F4 and F7 still point their `Plan` and `Feature Spec` metadata at nonexistent `docs/plans/` and `docs/features/` paths; F10, F18 and F22 have similar references. Checking only the new guide’s links will miss them. Require reference validation for the retained records after selection and exclusions.

Read-only review completed; no files were modified. Builds and click-throughs were not run because they would create artifacts.

# Round 2 (2026-10-13)

## Verdict: NO-GO

“Closed” below means the revised spec addresses the finding; it does not mean the cleanup has been implemented. Line numbers refer to [spec.md](D:/src/fokus/docs/specs/F36-PublicReleaseCleanup/definition/spec.md).

| Round-1 finding | Status | Spec lines and assessment |
|---|---|---|
| R1-01 | closed | 54–57, 68, 93–94: identifier inventory, history-wide redaction and exclusions now cover the reported disclosures. |
| R1-02 | closed | 23, 57–58, 85: suite records are excluded and other documentation has a stated disposition. |
| R1-03 | closed | 79, 87, 95: identity is an explicit owner decision; tags require authorization. |
| R1-04 | closed | 55, 57, 60, 68–72: repeatable filtering is specified, historical graph artifacts are excluded, and tool removal follows mining. |
| R1-05 | closed | 96: the two named pre-code commits are exempt. Other historical build failures remain; see R2-01. |
| R1-06 | closed | 22, 59, 100: portable hook registrations must move into shared settings and fire in a fresh clone. |
| R1-07 | closed | 77, 94: private paths must be ignored and absent from every published tree. |
| R1-08 | closed | 60, 99: OMC removal and optional Atlassian prerequisites are specified. The gate’s broader scope creates R2-03. |
| R1-09 | partial | 92–103: historical and final checks are separated, but acceptance still omits the public known-open-work list and checks documentation existence rather than completion. |
| R1-10 | partial | 28, 46, 103: existing tests and recorded manual checks are included, but the test command lacks a usable target; see R2-06. |
| R1-11 | closed | 68, 88, 101: clone identity, Node requirements and setup-path corrections are explicitly required. |
| R1-12 | closed | 68, 86: backlog reconciliation precedes triage; the missing candidates are included. |
| R1-13 | partial | 72, 102: relative links are covered, but executable Git references inside retained records are not; see R2-07. |
| R1-14 | closed | 19, 61, 71: mining follows the campaign and the published basis must resolve in public history. |
| R1-15 | partial | 55, 69: Java/fallback is addressed; provisioning the previously unavailable `git filter-repo` remains unspecified. |
| R1-16 | closed | 18, 48, 69: the deliverable now uses the campaign’s measure. |
| R1-17 | closed | 70, 86: package advisories are explicit triage candidates. |

1. **MEDIUM — `fold` — [spec.md:96](D:/src/fokus/docs/specs/F36-PublicReleaseCleanup/definition/spec.md:96): Historical front-end builds conflict with the history policy.** F31’s [review.md:127](D:/src/fokus/docs/specs/F31-EpicTestHealth/delivery/review.md:127) records a failed production build. Snapshot `2c0da8f` still imports unused `saveXraySettings` with `noUnusedLocals` enabled. F33’s [implementation.md:91](D:/src/fokus/docs/specs/F33-DailyProgressEnhancements/delivery/implementation.md:91) records three further failures, fixed later by F34. Feature grouping preserves these failing endpoints. D3 puts cleanup changes at the end, which cannot repair earlier trees. The spec needs an explicit policy for repairing historical code or changing this gate.

2. **MEDIUM — `fold` — [spec.md:54](D:/src/fokus/docs/specs/F36-PublicReleaseCleanup/definition/spec.md:54): A1 weakens the owner’s history-wide usability guarantee.** A1 expressly permits unavailable tools, dead hooks, incorrect setup instructions and dead links in earlier published snapshots. The picked [idea C](D:/src/fokus/docs/specs/F36-PublicReleaseCleanup/definition/ideas.md:21), still referenced by D5, promises that nothing private **or unusable** reaches students and applies the scrub to every rewritten snapshot. This changes an earlier guarantee without reconciling the owner decision.

3. **MEDIUM — `fold` — [spec.md:99](D:/src/fokus/docs/specs/F36-PublicReleaseCleanup/definition/spec.md:99): The global tool-name ban exceeds A7’s removal scope and conflicts with A5’s retention policy.** A7 removes references from the shipped setup; acceptance bans them from every shipped file. A5 explicitly retains [architect-vs-planner.md:1](D:/src/fokus/docs/evaluations/architect-vs-planner.md:1), whose subject is OMC, and `docs/agenting-setup-reference.md`, which contains a substantial Graphify section. Privacy redaction does not resolve these hits. The spec must decide whether historical commentary is permitted or require broader editorial rewriting.

4. **MEDIUM — `fold` — [spec.md:72](D:/src/fokus/docs/specs/F36-PublicReleaseCleanup/definition/spec.md:72): Phase 5 writes the guide before the history rewrite.** Its sequence places “the guide” before cloning, filtering and regrouping history. D6 at line 45 explicitly requires writing it **after** the history rewrite; deliverable 7 also says “written last.” Following the phase order breaks that guarantee and leaves the guide written against history that subsequently changes.

5. **MEDIUM — `fold` — [spec.md:77](D:/src/fokus/docs/specs/F36-PublicReleaseCleanup/definition/spec.md:77): The mining findings are excluded without a required public export.** The installed `mine-verify-repo` writes its confirmed findings to `docs/tech-debt/{area}.md`, which this constraint removes from every published commit. Lines 18 and 48 promise every remaining finding as known open work, but no phase requires copying those rows into a shipped artifact, and acceptance does not check that list. The specified pipeline can discard the findings while passing its release checks.

6. **MEDIUM — `fold` — [spec.md:46](D:/src/fokus/docs/specs/F36-PublicReleaseCleanup/definition/spec.md:46): The test safety-net command cannot locate the project from the stated build directory.** `dotnet build src/Fokus.slnx` establishes a repository-root invocation, but the following bare `dotnet test` has no solution or project at that root. The solution is under `src/`. The existing [F32 review](D:/src/fokus/docs/specs/F32-DailyDeveloperProgress/delivery/review.md:68) uses `dotnet test src/Fokus.slnx`. Specify the target or working directory so the required 19 tests actually execute.

7. **MEDIUM — `round-1 miss: feature-record bodies` — [adhoc-TeamManagedSync/delivery/plan.md:80](D:/src/fokus/docs/specs/adhoc-TeamManagedSync/delivery/plan.md:80): Retained records contain unusable Git references outside the link gate.** This candidate record directs readers to commit `1987027`; its [review.md:74](D:/src/fokus/docs/specs/adhoc-TeamManagedSync/delivery/review.md:74) presents `git show 1987027:…` as verification. That object already fails to resolve in the live repository. Further regrouping changes surviving commit IDs, while A8 repairs only the architecture map’s basis and acceptance checks only relative links. These executable references need validation and remapping or annotation.

No files were modified. Front-end build configuration, dependency requirements, historical source and feature-record bodies were inspected. Builds, dependency installation and tests were not run because they would write artifacts.

# Plan review — round 1 (2026-10-13)

## Verdict: NO-GO

1. **HIGH — [plan.md:78](/D:/src/fokus/docs/specs/F36-PublicReleaseCleanup/delivery/plan.md:78): Repository-root paths are incompletely covered.** Both proposed prefixes require trailing separators. They miss `D:\src\fokus` in F12’s lessons and `D:/src/fokus` in F13’s lessons. Blindly removing a root prefix would also turn `cd "D:/src/fokus"` into `cd ""`. Define context-sensitive replacements for standalone roots.

2. **HIGH — [plan.md:78](/D:/src/fokus/docs/specs/F36-PublicReleaseCleanup/delivery/plan.md:78): Historical `code-forge` coverage is incomplete.** Committed `.claude/settings.json` contains `Bash(*code-forge*)`. Replacing `D:/src/code-forge` leaves that entry unchanged; excluding local settings does not cover shared settings. The owner’s current edit hides this gap from the working-tree sweep.

3. **HIGH — [plan.md:79](/D:/src/fokus/docs/specs/F36-PublicReleaseCleanup/delivery/plan.md:79): Prose replacements can corrupt technical text.** Historical `.claude/skills/create-domain-event-handler/workflows/Handler.md` contains `Reflekt.API.Features.Cards.CreateCard`; the prescribed replacement produces an invalid identifier. Retained SkillCleanup records also contain `{ReflektService}` and technical paths. Furthermore, the safety grep at line 86 prints `0` for unsafe `literal:Adam`, `literal:PD`, `literal:Taylor`, and broad regex rules. Require safe technical replacements and validate parsed rules. [Filter-repo’s parser and replacement implementation](https://github.com/newren/git-filter-repo/blob/main/git-filter-repo) confirm these forms are supported.

4. **HIGH — [plan.md:104](/D:/src/fokus/docs/specs/F36-PublicReleaseCleanup/delivery/plan.md:104): The redaction operation crosses the protected source boundary.** “Every tracked file” includes `src/` and `client/src/`, contrary to line 22. The Step 4 diff misses staged changes, while the restore-tag diff at line 162 misses untracked additions. Explicitly protect both trees and compare their complete file inventories and contents.

5. **HIGH — [plan.md:82](/D:/src/fokus/docs/specs/F36-PublicReleaseCleanup/delivery/plan.md:82): Exclusions are not concretely pinned.** The referenced Step 3 enumerates seven paths; A4 enumerates nine, including historical `graphify-out/` and `.claude/settings.local.json`. Also, copying `docs/specs/F36-*` as a plain path does not provide wildcard matching: filter-repo requires `glob:` or `regex:`. Enumerate all exclusions and verify matching against historical paths. [Filter-repo path syntax](https://github.com/newren/git-filter-repo/blob/main/git-filter-repo) supports this distinction.

6. **HIGH — [plan.md:73](/D:/src/fokus/docs/specs/F36-PublicReleaseCleanup/delivery/plan.md:73): The reusable transform omits private backlog content.** Constraints require removal of F36 and cleanup-feature backlog content. Live `docs/backlog.md:282` and `:284` contain the F36 row, private-folder link, and explanatory paragraph. Neither the map recipe nor Step 6 handles them; excluding feature folders leaves the content and dangling link.

7. **MEDIUM — [plan.md:142](/D:/src/fokus/docs/specs/F36-PublicReleaseCleanup/delivery/plan.md:142): The deny sweep has inventory blind spots.** It omits bare project key `PD` and colleague `Adam`. Read-only fixtures confirmed that `project PD`, `Jira project PD`, and `Adam` do not match. The required bare-key replacement can therefore fail while this gate passes.

8. **MEDIUM — [plan.md:140](/D:/src/fokus/docs/specs/F36-PublicReleaseCleanup/delivery/plan.md:140): Step 7 weakens D7 without authorization.** Line 144 omits the required manual page checklist and synced-data results. The pre-existing-failure waiver also conflicts with the final-tree passing-test requirement; O5 addresses historical snapshots.

9. **MEDIUM — [plan.md:164](/D:/src/fokus/docs/specs/F36-PublicReleaseCleanup/delivery/plan.md:164): Record preservation is neither mechanical nor internally consistent.** Its diff includes the two suite folders that Step 3 deletes outright, although only redactions and link fixes are permitted. Step 3’s token sweep also rejects harmless plain references already present in retained PipelineCraftImprovements records. Define deletion exceptions, check actual links, and mechanically compare retained content against permitted transformations.

10. **MEDIUM — [plan.md:62](/D:/src/fokus/docs/specs/F36-PublicReleaseCleanup/delivery/plan.md:62): Hook acceptance proves only property names.** Empty `SessionStart` and `PreToolUse` arrays produce the required output. It does not verify commands, matcher, `async`, timeout, or exclusion of local permissions and environment values. Compare the complete migrated hook payload and preserved shared settings.

11. **MEDIUM — [plan.md:124](/D:/src/fokus/docs/specs/F36-PublicReleaseCleanup/delivery/plan.md:124): README acceptance does not verify the required setup sequence.** Live instructions leave the reader in `client/` before copying a root-level example, and name an unqualified `appsettings.Development.json` that does not exist. The acceptance greps do not check working directories, configuration creation, `.credentials/`, or D8’s no-sample-data statement.

12. **MEDIUM — [plan.md:140](/D:/src/fokus/docs/specs/F36-PublicReleaseCleanup/delivery/plan.md:140): The frontend acceptance command does not parse in the supplied shell.** This environment runs Windows PowerShell 5.1. Its parser rejects `npm ci && npm run build`. Specify a supported shell or separate commands with failure handling.

13. **LOW — [plan.md:14](/D:/src/fokus/docs/specs/F36-PublicReleaseCleanup/delivery/plan.md:14): Recorded evidence is inaccurate.** `.claude/.current-agent` is already ignored at `.gitignore:440`; `git check-ignore` confirms it. The exact deny sweep currently matches 30 files, rather than the claimed 35.

Read-only acceptance commands were exercised; implementation-dependent failures are expected before cleanup. The 19 test declarations, solution inclusion, Node range, listed removal paths, and GAP implementation evidence were confirmed. The owner’s `.claude/settings.json` edit remained unchanged. Build, test, and npm commands were not executed because they write files. No file writes were attempted.

# Plan review — round 2 (2026-10-13)

## Verdict: NO-GO

Eight round-1 findings are closed; four remain partial. Five MEDIUM findings remain. “Closed” refers to the revised plan, not completed implementation. Lines below refer to [plan.md](D:/src/fokus/docs/specs/F36-PublicReleaseCleanup/delivery/plan.md).

| Finding | Status | Plan lines and assessment |
|---|---|---|
| P1-01 | closed | 12, 88–93: historical exclusions are enumerated. Read-only checking found zero misses across 67 matching historical paths. |
| P1-02 | partial | 75–86, 111: reported identifier and root-path cases are addressed, but rule ordering and corrective reruns remain inconsistent; P2-01/02 below. |
| P1-03 | closed | 82–83, 155: bare `code-forge`, history-only identifiers and history replay are covered. |
| P1-04 | closed | 111, 179: source trees are protected; status and restore-point diff cover untracked and staged changes. |
| P1-05 | closed | 26, 87, 202: public-only removal preserves the private backlog. |
| P1-06 | closed | 81, 84, 153: the sweep covers the missing identifiers and uses explicit Polish-name alternatives. |
| P1-07 | partial | 151, 163: the failure waiver is gone, but the automated command fails and the manual gate still weakens D7; P2-04/05. |
| P1-08 | partial | 78, 101–113, 181: link-only cleanup and bytes mode are specified, but the preservation replay cannot pass as written; P2-03. |
| P1-09 | closed | 60, 66–67: acceptance compares the complete hook payload and rejects local environment/allow settings. |
| P1-10 | closed | 121–132: setup order, working directories, configuration creation, credentials directory and D8 wording are required. |
| P1-11 | partial | 150–151: Git Bash resolves the PowerShell parsing issue, but its replacement command fails before verification; P2-04. |
| P1-12 | closed | 11–18, 59, 90–93: evidence is corrected, the unsupported count is removed, and the inventory has a repository-local path. |

1. **MEDIUM — `fold` — [plan.md:77](D:/src/fokus/docs/specs/F36-PublicReleaseCleanup/delivery/plan.md:77): File-order semantics contradict filter-repo.** Filter-repo applies all literal replacements before all regex replacements, preserving order within each group. The plan instead promises global file order while requiring the applier to match filter-repo. With the mixed SprintRituals rules described at line 80, a fixture containing `SprintRituals.API` becomes `SampleSuite.API` under file order but `a sibling project.API` under literal-first behavior. This undermines A2’s guarantee that the working-tree and publication transforms agree. Define the actual ordering and verify equivalence. [Upstream implementation](https://github.com/newren/git-filter-repo/blob/main/git-filter-repo).

2. **MEDIUM — `fold` — [plan.md:111](D:/src/fokus/docs/specs/F36-PublicReleaseCleanup/delivery/plan.md:111): Reordering rules cannot repair an already-transformed working file.** Line 78 defines an in-place applier; line 111 says to fix bad wording by adding/reordering a rule and rerunning. Once `Reflekt-Specific` becomes `a sibling project-Specific`, a newly added literal targeting the original phrase cannot match it. A read-only fixture reproduced this divergence from replaying the revised map against the original input. Specify regeneration from preserved original bytes while retaining other authorized edits. Otherwise the promised rerunnable transform cannot perform its prescribed correction workflow.

3. **MEDIUM — `fold` — [plan.md:113](D:/src/fokus/docs/specs/F36-PublicReleaseCleanup/delivery/plan.md:113): The preservation replay is incompatible with the live checkout.** Git blobs at `pre-public-cleanup` use LF; working files use CRLF. SkillCleanup’s plan has 334 CRLF lines on disk and none in its tag blob. A bytes-preserving transform therefore cannot make the required byte comparison pass. Also, native Windows Python cannot open Git Bash’s `<(...)` operand: the read-only check failed on `/dev/fd/63`. This prevents the gate from proving line 181’s preservation guarantee. Use Windows-readable inputs and compare equivalent byte representations.

4. **MEDIUM — `fold` — [plan.md:151](D:/src/fokus/docs/specs/F36-PublicReleaseCleanup/delivery/plan.md:151): The verification command omits mandatory `--slug`.** The installed runner rejects `--role full` without `--slug <slug>` before resolving or executing any commands. Running the prescribed invocation produced that rejection and no verification record. Consequently Step 7 cannot obtain its required pass or establish the build/test invariant at line 180. Supply the F36 slug, or use explicit verification commands.

5. **MEDIUM — `fold` — [plan.md:163](D:/src/fokus/docs/specs/F36-PublicReleaseCleanup/delivery/plan.md:163): The operator check still weakens D7 and omits live main pages.** Marking the step `N/A (operator-owned)` permits completion without its required result. The checklist also omits Dashboard, Team and Cycle Time, which appear in [AppSidebar.vue:27](D:/src/fokus/client/src/components/AppSidebar.vue:27), and does not require synced data. This contradicts [spec.md:46](D:/src/fokus/docs/specs/F36-PublicReleaseCleanup/definition/spec.md:46), which requires each main page to load with synced data and a recorded result. Operator ownership should leave the requirement pending until that evidence exists.

All new findings concern folded text. Full documents, build configuration, verification-runner behavior and upstream filter-repo behavior were inspected. Builds, tests and dependency installation were not run because they write artifacts. No files were modified.
