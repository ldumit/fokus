# Private identifier inventory - D:\src\fokus (read-only scan, 2026-10-13)

Scope: tracked tree (998 files, `git grep`) + full history (61 commits, all refs; added/removed lines of `git log --all -p`, excluding package-lock and graphify-out). Working-tree-only (untracked) files noted separately.
History author identity on all 61 commits: `ldumit <dumitrescu.laurentiu@gmail.com>` (60) and `Laurentiu Dumitrescu <dumitrescu.laurentiu@gmail.com>` committer GitHub (1). Personal Gmail is in every commit's metadata (not in file content).

## 1. Employer / customer identifiers

| Identifier | Where (tree files) | History only? | Suggested replacement |
|---|---|---|---|
| `Omnishelf` (7 tree hits / 3 files) | docs/evaluations/cross-project-skill-mining.md:4,11,123,274,364; docs/proposals/suite-integration-architecture.md:231 (`**Team:** Omnishelf`); docs/specs/adhoc-PipelineCraftImprovements/delivery/plan.md:7 | No | "a prior project" / "Acme" |
| `D:\Omnishelf\omnishelf-docs` | docs/evaluations/cross-project-skill-mining.md:4 | No | `<external-docs-repo>` |
| `omniaz.atlassian.net` (employer Jira tenant) | docs/product/v2.md:8 | No | `acme.atlassian.net` |
| `OmniShelf/Alfamart x Production/Staging` (employer product flavors + CUSTOMER name Alfamart) | none | YES - `.claude/agents/team-lead.md`, added 0b69ee2 (2026-05-14), removed 727072b (2026-05-16) | history rewrite required; no tree replacement needed |
| Jira project key `PD` ("live Jira project PD") | docs/product/v2.md:8 | No | `ABC` |
| Jira keys `PD-4019` (2), `PD-4359` (1), `PD-4942` (1), `PD-5226` (4), `PD-5228` (6), `PD-5234` (5), `PD-5384` (4) = 20 hits / 8 files, 7 distinct | .claude/agents/po.md:54,66,67; .claude/rules/agents-workflow.md:11,12; .claude/skills/create-feature-spec/SKILL.md:73,74; docs/agenting-setup-reference.md:416,417; docs/product/v2.md:127,161,182,206,246,488; docs/specs/F25-XrayIntegration/definition/spec.md:30,65,186; docs/specs/F25-XrayIntegration/delivery/plan.md:63; docs/specs/F35-EpicViewEnhancements/definition/help.page.md:5 | No (same files exist in history) | `ABC-NNN` (keep numbers consistent per key, e.g. PD-5234 -> ABC-123 everywhere; slugs `PD-5234-shelf-compliance-kpis` -> `ABC-123-shelf-compliance-kpis`, `PD-5226-split-config` -> `ABC-124-split-config`) |
| Domain hint "shelf compliance" (retail shelf product) in slug examples | .claude/agents/po.md:67, rules/agents-workflow.md:11, skills/create-feature-spec/SKILL.md:74, docs/agenting-setup-reference.md:416 | No | generic ("dashboard-kpis") optional |
| Third-party names (public research, likely OK): Rakuten, TELUS, Zapier, Sourcegraph/Amp, Xray/Testmo/BrowserStack | docs/proposals/anthropic-rakuten-telus-zapier-research.md, docs/agentic-setup-evaluation.md | No | keep (public) |

## 2. People

| Name | Where | History only? | Suggested replacement |
|---|---|---|---|
| Laurentiu Dumitrescu (owner) | LICENSE:3 (copyright); `Laurentiu` x30 in 12 files: docs/backlog.md:3; docs/product/{v1,v2,agentic-rituals-suite-strategy}.md (Owner, "Laurentiu's team/Jira instance/expertise", "future-Laurentiu"); docs/proposals/suite-integration-architecture.md:233,262,264; BUG-1, BUG-2, GAP-1..4 `Reporter: PO (Laurentiu)`; README.md:43 `github.com/ldumit/fokus` | No | owner decision (public author) - keep or "Owner" |
| Taylor (4 hits) | docs/product/v2.md:218,342,482; docs/proposals/suite-integration-architecture.md:233 | No | "QA engineer A" |
| Mateusz (6 hits) | docs/product/v2.md:218,482; suite-integration-architecture.md:233,263,264 | No | "Developer B" |
| Adam (1) | suite-integration-architecture.md:233 | No | "Participant C" |
| Pawel (Paweł, 2) | suite-integration-architecture.md:233,266 | No | "Participant D" |
| Ovidiu (2) | suite-integration-architecture.md:233,264 | No | "Participant E" |
| Norbert (1) | suite-integration-architecture.md:233 | No | "Participant F" |
| Calin (1) | suite-integration-architecture.md:233 | No | "Participant G" |
| Maciej (2) | suite-integration-architecture.md:233,265 | No | "Participant H" |

Sprint 28 retro in docs/proposals/suite-integration-architecture.md:229-266 (dated 18/05/2026, "Team: Omnishelf", 9 participants, action items with @mentions) is the densest block: replace/remove wholesale. Workload counts "Taylor handles 9 of 10 TEs, Mateusz 1" at docs/product/v2.md:342,482 are real employer data. "Adam," hits in wwwroot JS bundles are false positives (library code). No names found in .claude, docs/kb, or src/client test data.

## 3. Paths, machine and private repo names

| Identifier | Where (tree) | History only? | Suggested replacement |
|---|---|---|---|
| `D:/src/fokus/...` absolute (check-xray.ps1:1,3 - also hard-codes `fokus.db` location) | check-xray.ps1:1,3 | No | repo-relative (`$PSScriptRoot`) |
| `D:\src\fokus` / `D:/src/fokus` in prose and build commands | docs/specs/F11-CarryOverTracker/delivery/review.md:20,107; F12-CycleTime/lessons.md:13; F13-BugRatio/lessons.md:15; F20-.../lessons.md:10; adhoc-TeamManagedSync/delivery/{lessons,review}.md; adhoc-SuiteScaffolding/delivery/plan.md:141,192,197,199,244,252,259,264; docs/evaluations/cross-project-skill-mining.md:5; docs/proposals/cc-setup-evaluation-and-plugin-extraction.md:5 | No | `<repo-root>` / repo-relative |
| `D:\src\sprint-rituals\` + `SprintRituals` + `sprint-rituals` (private suite repo; 13+21 hits) | docs/specs/adhoc-SuiteScaffolding/delivery/{plan,implementation,lessons,review,summary}.md; docs/specs/adhoc-SuiteInfrastructure/delivery/plan.md:7 | No | `<suite-repo>` |
| `D:\src\dotnet-microservices` (private reference repo) | adhoc-SuiteScaffolding/delivery/{plan.md:31,220,230; lessons.md:31} | No | `<reference-repo>` |
| `D:/src/code-forge` (private repo; permission grants) | docs/proposals/cc-setup-evaluation-and-plugin-extraction.md:52,155,177,193 (4 hits) | Also in `.claude/settings.json` (d075998) and `.claude/settings.local.json` (0b69ee2, 15 hits) - history only | `<other-repo>` |
| `C:/Users/Laurentiu/.claude/.omc/...` (OS username) | none | YES - `.claude/settings.local.json`, commits d030d9b, 6928039, 0786bb7 | history rewrite |
| `D:\Omnishelf\omnishelf-docs` | docs/evaluations/cross-project-skill-mining.md:4 | No | `<external-docs-repo>` |
| `Reflekt` (private sibling project, 57 hits / 8 files) | e.g. docs/proposals/suite-integration-architecture.md:226, docs/product/agentic-rituals-suite-strategy.md | No | decide: public product name or genericize |
| `ldumit` (GitHub handle) | README.md:43, docs/proposals/suite-integration-architecture.md:226 ("published to Laurentiu's personal GitHub (`ldumit`) first (private), establishing independent authorship provenance") | No | README clone URL is fine if repo is public; reword proposal line |
| `/home/data/fokus.db` | docs/architecture/v1.md:196, docs/deployment-azure.md | No | keep (generic Azure path) |

No `C:\Users` paths in the tracked tree; no other drive letters.

## 4. Emails and private hosts

| Identifier | Where | History only? | Suggested replacement |
|---|---|---|---|
| `omniaz.atlassian.net` | docs/product/v2.md:8 | No | `acme.atlassian.net` |
| `dumitrescu.laurentiu@gmail.com` | none in tree content; present in all 61 commit author/committer fields | YES (metadata) | history rewrite via mailmap/filter-repo if privacy needed |
| Placeholders (OK): `your-email@example.com` (README:67), `user@company.com` (UsersTab.vue:98), `jane@yourcompany.com` (F18 help.page.md:10), `acme.atlassian.net` (docs/architecture/v1.md:205, docs/product/v1.md:50), `your-org.atlassian.net` (README:66, deployment-azure.md:69, appsettings.json:10), `yourcompany.atlassian.net`, `your-app-name.azurewebsites.net` | various | No | keep |
| `localhost:5000/5001/5173` | docs, launchSettings | No | keep |

No secrets, tokens, client IDs or keys found in history (pattern scan for api key/secret/password/ghp_/sk-/AIza/JWT/Google client-id: no hits).

## 5. Real sprint data (files only)

| File | Content |
|---|---|
| docs/specs/BUG-2-barchart-burnup-completed-sp-mismatch/definition/bug.md | Sprint 26 real SP: 202, 115, 24 bugs, 63 gap |
| docs/specs/GAP-4-sprint-cards-scope-mismatch/definition/spec.md | Sprint 26: 223/226, 201, 247 |
| docs/specs/BUG-1-bug-count-zero/definition/bug.md | Sprint 26 title and body |
| docs/backlog.md:308 | "Bug Count = 0 on Sprint 26" |
| docs/product/v2.md | Sprint 28, PD-5228, "1 of 10 TEs", "9 of 10 TEs" (xray audit) , customfield_10020 |
| docs/product/v1.md:79,141,397 | "Sprint 27"/"Sprint 26" examples (low risk) |
| docs/specs/F23-TransitionBasedSprintScope/definition/{spec,help.page}.md, delivery/plan.md | Sprint 26/27 examples (use PROJ-100 placeholder keys, sprint numbers real-looking) |
| docs/specs/F25-XrayIntegration/definition/spec.md:128,186 | "Sprint 28" |
| docs/proposals/suite-integration-architecture.md:229-266 | Sprint 28 retro (see section 2) |

Jira custom field ids `customfield_10016/10008/10014/10020` (docs/product/v2.md, F4-JiraSync/delivery/*) are Jira defaults, low risk.

## Working-tree-only (not tracked, not gitignored) - do not commit
- `.claude/audit/` (untracked, not in .gitignore): session logs + read-tracker/handoff/token-usage; contain Laurentiu/dumit/C:/Users strings (4 files matched). Add to .gitignore.
- `docs/specs/F36-PublicReleaseCleanup/` (this task's own spec/reviews) mentions omnishelf/PD/names by design - sanitize or exclude before publish.
- `.claude/settings.local.json` IS TRACKED (50 lines, currently no personal strings) but is machine-local by name; its history holds C:/Users/Laurentiu and code-forge grants. Untrack + gitignore.

## History-only summary
- Alfamart/OmniShelf flavors line in `.claude/agents/team-lead.md` (0b69ee2 -> removed 727072b).
- `C:/Users/Laurentiu/.claude/.omc` in settings.local.json (d030d9b, 6928039, 0786bb7).
- code-forge allow-list grants in settings.local.json (0b69ee2) and settings.json (d075998).
- Committed Vite build output (`src/Services/Fokus/Fokus.API/wwwroot/assets/*.js`, 5 commits touching wwwroot; none in tree now) - bloat, no identifiers found.
- Personal Gmail in commit metadata.

History scratch dumps: scratchpad/history.patch (13.9 MB), scratchpad/added.txt.
