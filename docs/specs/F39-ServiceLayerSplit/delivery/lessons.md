# F39 — Service Layer Split — Lessons

## Architect Lessons

- **A refactor proven by output comparison needs its harness specified like production code.** Round 1 found 6 HIGH gaps in a one-paragraph harness:
  - error bodies carrying trace ids and stack traces;
  - the signed-in user's database row;
  - start-up seeding;
  - feature flags off by default;
  - routes outside the request list;
  - filters never sent.

  Spell out the host, the identity, the fixture variants, the request manifest with expected statuses, and a sabotage check before calling it a pin.
- **Check every reply field for machine-specific text before promising byte equality.** A sync service put `ex.StackTrace` straight into its reply.
- **Move tests in the same step as the type they test** whenever each step must stay green. A trailing "fix tests" step leaves the build red for every step before it.
- **Count before scoping.** "Three long methods" was 25 methods over the plan's own limit; a method-length script at plan time would have sized the slice correctly.
- **Accept commands are written for the shell that runs them.** Under Git Bash, an unquoted Windows path loses its backslashes, and `find -name "{a,b}"` matches the braces literally.
- **A fact-count accept must name its instrument (done check, F39 Step 11).** "Fact count 41 before and after (runner output)" led the developer to a raw `dotnet test --list-tests`, which the test-entry gate logged unblocked — a test slip the done check must FAIL on, though it ran no test. Write such accepts as `verify-run.js` output or a `[Fact]`/`[Theory]` grep. Evidence: [F39-ServiceLayerSplit]
- **Scope the skill log by the implement window, not the token alone.** `developer:implement` is reused across features in one session (F38 and F39 share it), so a token-only query pulls in a previous feature's invocations. Bound it from the first `developer:implement` resume/spawn in `handoff.jsonl`. Evidence: [F39-ServiceLayerSplit]
- **Plan-time record counts by type name miss renamed twins.** Step 4 counted eight `{Area}SprintInfo` records; a ninth (`SprintSummaryItem`) carried the same four members under another name and surfaced in Step 6. When a merge is planned by shape, count by member list, not by name. Evidence: [F39-ServiceLayerSplit]

## Developer Lessons

- **A `WebApplicationFactory` harness built as a console app, not a test project, cannot find the content root.** It throws "Solution root could not be located" even though `MvcTestingAppManifest.json` is written. Fix: read the API project's path from that manifest and set `ASPNETCORE_TEST_CONTENTROOT_{ASSEMBLY_NAME}` (dots → underscores) before the first `CreateClient`.
- **A `git archive` extract is CRLF under `core.autocrlf=true`, while checkout files edited since may be LF.** `cmp` against the checkout fails on line endings alone. Prove equivalence by comparing behaviour (one capture from each tree), not bytes. And back up a file before a `sed -i` sabotage: GNU `sed` rewrites CRLF to LF, so only a byte copy restores it exactly. For a doc sweep, run `unix2dos -q {files}` (present in Git Bash) after the `sed -i`, and confirm with `git ls-files --eol {dir}` (all `w/crlf`). F39 Step 12 rewrote 17 KB files this way.
- **Moving a private helper to a shared static class: add `using static {Namespace}.{Class};` and delete the private copy, leaving call sites untouched.** The diff stays a pure deletion plus one using line, and method-group uses such as `Count(IsBug)` keep binding. A class's own same-named private member still wins name lookup, so a file that keeps a differing variant is unaffected. Used across 10 services in F39 Step 4.
- **Don't truncate `verify-run.js` output with `cut`.** The verdict is at the end of the one-line JSON record. If it was cut, read the run's record from `.claude/audit/verify-verdict.json` (match on its `ts`) rather than running the close again.
- **A sub-namespace named like a type in a parent namespace hides that type from every sibling namespace.** With `Fokus.Domain.Analytics.Sprint` present, a bare `Sprint` inside any `Fokus.Domain.Analytics.*` file binds to the namespace and fails with CS0118. Lookup reaches the parent's member namespace before any `using`; only an alias placed after the file-scoped namespace beats it. Check folder and area names against aggregate type names before planning a folder tree. A two-file scratch project proves the clash in seconds. Caught in F39 Step 5 (Q4, D14).
- **A Python move script that deletes its source files must back them up first, or be safe to re-run.** In F39 Step 5 a regex-group error stopped the script after its first file was deleted, and part of that file's text had to be recovered from `git show HEAD:`. Step 6 copied the sources to the scratchpad first.
- **Before merging a helper that returns the input list itself into one that returns a copy, check that no caller mutates either list.** If none does, the two are equal on every input and the merge is D6-legal. If one does, the merge changes behaviour. A grep for `.Add/.Remove/.Clear/.Sort/.Insert/.Reverse` on the result and the source settles it. A related trap: putting a `List<T>` overload beside an `IReadOnlyList<T>` overload rebinds every `List` caller to the first, so keep one signature. F39 Step 7 merged 9 `FilterMemberships` copies, in two variants, this way.
- **A method-length counter that matches the name on the modifier line misses methods whose name is on the next line,** such as a tuple return type `private static (int A, …)` with `Name(` below it. Join the next line before matching, and run the counter on the base tree to check it reproduces the plan's figures. F39 Step 7's first count missed `ComputeDevMetrics` (86 lines).
- **To give endpoints their own replies over a large Domain result tree, generate the mirror instead of hand-writing it.** A short script that reads the Domain records (positional parameters and `init` members), walks each root's nested records, and writes both the `sealed record …Dto` declarations and the `To…Dto()` maps copies every member in order and misses none. F39 Step 8 did this for about 120 records with a byte-identical wire on the first build. Two details: compare neighbour types with `?` stripped when you decide which arguments to name (`string` beside `string?` still swaps silently), and give a Domain record that is called `…Dto` a new name first, or it collides with its own reply type.
- **In this environment a Bash heredoc whose body contains an apostrophe can fail with "unexpected EOF while looking for matching `''", even with a quoted `'EOF'` delimiter.** It failed twice in F39 Steps 8–9: once on an `implementation.md` append, once on a Python edit script. Write the text or script to the scratchpad with the Write tool, then `cat >>` it or run it.
- **Seeding a `Developer` with `DefaultCapacityPercent = 0` through EF stores 100.** `DeveloperConfiguration` sets `HasDefaultValue(100)`, so EF treats the CLR default 0 as "not set" on insert and the column default wins. A fixture or test that needs a zero-capacity developer seeds 0% `DeveloperSprintCapacity` records instead, or sets the value after the insert. Found in F39 fix round 1 while building the "no activity" 404 case.
- **To prove a golden harness extension closes a gap, run the surviving mutants against a scratch copy of the tree, not the tree.** Copy `src` without `bin`/`obj`/`.vs`, give the harness a third project folder whose `ProjectReference` points at the copy, capture it unmutated first (must PASS), then apply one mutant at a time from a byte backup and compare. The working tree is never edited and no git write is needed. F39 fix round 1 killed 5 of 5 this way.

## Skill Gaps

### golden-output-harness
- **Kind:** missing
- **Searched for:** a recipe for a before/after output-comparison harness over a .NET API (host overrides, fakes, seeded fixture, request manifest, normalization, determinism and sabotage proof).
- **Why it would help:** F39 Step 2 built one from scratch (`D:\temp\f39-golden\harness`): the content-root workaround, fixed trace id, test auth scheme, SQL command counter, swagger property-location diff. A skill would make the next refactor's pin a half-hour job.
- **References:** `D:\temp\f39-golden\harness\src\*.cs` (outside the repo, kept until Step 13), `docs/specs/F39-ServiceLayerSplit/delivery/implementation.md` § Step 2.
- **Evidence:** [F39-ServiceLayerSplit]
- **Answering developer Phase-1 questions cannot amend plan.md while `.pipeline-state` reads `developer:analyze`** — `pipeline-gate.js` refuses every plan.md write as "written during an analyze phase", though the architect protocol ("update the plan if any answer changes it") expects the edit. F39: three answers each owed a plan change; the answers were written to questions.md as binding until the plan lands, and the amendment was handed back. A question-answering dispatch that may change the plan should set a plan-write state first, or the gate should allow an architect answer-round edit. **Evidence:** [F39-ServiceLayerSplit]
- **A plan's git-based scaffolding must be checked against the owner's no-git-writes constraint at plan time.** F39 Step 2 planned `git worktree add/remove` for a frozen base copy; under "no git writes" a `git archive | tar -xf - -C /d/...` extract pair (base + pristine, `diff -r` for the revert check) gives the same frozen tree read-only, byte-identical to the checkout with `core.autocrlf` applied. **Evidence:** [F39-ServiceLayerSplit]

### create-feature — mirroring a Domain result tree (workflows/Mappings.md)
- **Kind:** ill-fitting
- **Searched for:** how to give an endpoint its own reply when the Domain result is a deep tree of records: naming the mirrored types, where shared shapes go, and the named-argument rule for neighbours of the same type when one is nullable.
- **Why it would help:** `Mappings.md` shows one entity-to-reply map. F39 Step 8 mirrored about 120 records across 12 operations. The naming (`…Result` → `…Dto`; top-level `{Operation}Response`), the `_Shared` placement and per-file import, and the nullable-neighbour reading of the named-argument rule had to be decided in the step and recorded in `implementation.md`. A short "mirroring a result tree" section, pointing at a generator, would settle them up front.
- **References:** `src/Services/Fokus/Fokus.API/Features/Analytics/*/Get*Extensions.cs`, `src/Services/Fokus/Fokus.API/Features/Analytics/_Shared/`, `D:\temp\f39-golden\step8\gen.py` (outside the repo).
- **Evidence:** [F39-ServiceLayerSplit]

## Reviewer Lessons

- A before/after golden harness proves identity only on the data it seeds. When a record cites one as the sole proof, run a handful of single-line mutants of the NEW code against it in a scratch copy: F39's harness killed 10 of 17 and missed `last=0`, the 12-sprint streak cap, the excluded-developer filter, a one-run pass rate and a 404 branch, because the fixture had 6 closed sprints, no `last=0` request and an excluded developer with no tickets. The survivors name exactly which paths rest on reading alone. [F39-ServiceLayerSplit]
- A skill's "This Repo's Form" can contradict the tree the same feature built (create-feature said "No `_Shared` folders yet" after Step 8 created one). On a feature that rewrites skills and code together, grep each "no X yet" / "none" statement in the skill against the tree before approving. [F39-ServiceLayerSplit]
