# Workflow Comparison: Fokus vs. Anthropic, Rakuten, TELUS, Zapier

Technical comparison of our Claude Code agent pipeline against documented practices at four companies.

---

## Where We're Aligned or Ahead

| Dimension | Us | Industry |
|---|---|---|
| **Agent pipeline** | Structured 3-agent pipeline (architect→developer→reviewer) with hub-and-spoke messaging, cycle caps, escalation paths | Anthropic uses similar multi-agent patterns but less formally documented. Rakuten's 24-session ambient agent is more about parallelism than structured review. |
| **Skills as policy** | Skills in `.claude/skills/` are authoritative — plans map to them, architecture docs defer to them | Zapier's exact philosophy: "Skills encode best practices; MCP runs them at scale." We got this right. |
| **CLAUDE.md as living doc** | Detailed, load-bearing, continuously improved | Matches Anthropic's "headline tip" and Rakuten's success criterion for long runs. |
| **Plan→execute model** | Architect (opus) plans, developer (opusplan) executes | Anthropic's documented `opusplan` pattern. Same idea. |
| **Structured review** | Severity-rated, checklist-driven, file-based verdicts with cycle caps and escalation | More rigorous than Rakuten's "AI-powered code review" or Zapier's `code-review` skill. |
| **File-based handoffs** | plan.md, implementation.md, review.md, summary.md, lessons.md — full audit trail | No company documents this level of traceability in their pipeline. |

---

## Where We're Behind or Missing Opportunities

| Gap | What they do | What we could consider |
|---|---|---|
| **Parallelism** | Rakuten runs 5 tasks in parallel / 24 sessions. Anthropic's "slot machine" pattern — commit, let Claude run 30 min, accept or reset. | Our pipeline is strictly sequential. Could parallelize independent plan steps across multiple developer agents. |
| **Auto-accept / autonomous loops** | Anthropic's sharp rule: peripheral features run async with auto-accept; core logic runs synchronous with monitoring. | We don't distinguish between "high-trust peripheral work" and "core logic that needs oversight." Everything gets full pipeline. |
| **Sync vs. async classification** | Anthropic explicitly categorizes tasks before choosing the workflow weight. | We always run Standard mode. The "Fast" mode exists but there's no guidance on *when* to choose it. |
| **MCP as action layer** | TELUS calls MCP "the most transformative technology in decades." Zapier's entire strategy is MCP-first. Anthropic uses MCP over CLI for sensitive data. | We have MCP tools available but no custom MCP servers for our domain (Jira sync, deployment status, environment provisioning). |
| **Image-driven workflows** | Anthropic pastes dashboards/Figma into Claude Code for debugging and prototyping. Product Design has Figma open 80% of the day. | Not leveraging this for frontend work. Could paste Figma mockups directly when building Vue components. |
| **Continuous CLAUDE.md refinement** | Anthropic asks Claude to suggest CLAUDE.md improvements at end of every session. | We update ours manually. Could formalize as a session-end hook or habit. |
| **GitHub Actions / CI triggers** | Anthropic: filing an issue triggers Claude to propose code. PR comments auto-addressed via Actions. Zapier: Slack emoji → merge request. | No event-driven automation. Our pipeline only starts when a human says "go." |
| **Non-developer enablement** | Rakuten: terminal with guardrails for non-engineers. TELUS: 21k copilots for 70k employees. Anthropic: Finance staff write plain-text workflow files. | Our flow is engineering-only. No thought yet about PMs/designers using the system. |
| **Parallel sessions as "slot machine"** | Anthropic Data Science: commit state, let Claude run autonomously ~30 min, accept or `git reset` and try again. | We don't have a "speculative run" pattern. Every run is intended to succeed on first pass. |
| **Custom slash commands density** | Anthropic Security Engineering: 50% of all custom commands in the monorepo. Treat slash commands as the dominant abstraction. | We have skills but haven't pushed density — most workflows still require manual orchestration rather than a single `/command`. |

---

## Detailed Category Analysis

### 1. Pipeline Rigor & Traceability

**Us:** Strongest in the group. Three-agent pipeline with explicit handoff files, cycle caps (3 fix cycles max), escalation protocols, communication logs, and lessons learned. Every pipeline run produces a full paper trail.

**Anthropic:** Uses multi-agent patterns but the documentation focuses on individual team workflows, not a universal pipeline contract. No public evidence of cycle caps or formal escalation.

**Rakuten:** "AI-powered code review" on PRs but no documented multi-stage pipeline. The 7-hour autonomous run had no intermediate review gates — just a final accuracy check.

**TELUS:** No documented developer pipeline. Their rigor lives at the platform level (Fuel iX governance, ISO certification) not the code-generation level.

**Zapier:** Three public Skills (work-on-ticket, code-review, git-commit) suggest a lightweight pipeline but no documented orchestration between them.

### 2. Parallelism & Throughput

**Us:** Strictly sequential. One feature flows through architect→developer→reviewer. No mechanism for splitting a plan into parallel work streams.

**Anthropic:** Multiple Claude Code instances per repo. "Slot machine" pattern for speculative runs. Parallel sessions across repos.

**Rakuten:** Most advanced — 24-session "ambient agent" for monorepo updates. 5-tasks-in-parallel as an explicit mental model taught to developers.

**TELUS:** ~100B tokens/month implies massive parallelism at the platform level, but no developer-workflow-level parallelism documented.

**Zapier:** 800+ internal agents implies high parallelism but architecture not documented.

### 3. Autonomy & Trust Classification

**Us:** Two modes (Fast, Standard) but no explicit criteria for when to use which. In practice, everything runs Standard.

**Anthropic:** Sharpest classification: "peripheral/abstract features run async with auto-accept; core business logic runs synchronously with detailed prompts." This is the key insight we're missing.

**Rakuten:** High autonomy tolerance — 7-hour unsupervised runs. But on validated, well-scoped tasks with clear success criteria (99.9% numerical accuracy).

**TELUS:** Model routing serves as implicit trust classification — Claude for complex/creative, Gemini Flash for low-latency. Not about autonomy but about capability matching.

**Zapier:** Slack-emoji triggers imply high trust for certain classes of work (formatting, small fixes). Autonomous MR creation from context.

### 4. MCP & External Integration

**Us:** MCP tools available (Atlassian, Figma, Google, etc.) but used ad-hoc. No custom domain-specific MCP servers.

**Anthropic:** Custom Meta Ads MCP server. Recommended MCP over CLI for sensitive data (logging, scoping).

**Rakuten:** Not documented.

**TELUS:** MCP is central to their strategy — "most transformative technology." Multi-model routing through Vertex AI.

**Zapier:** MCP-first. Public server with 9k+ apps / 40k+ actions. Internal MCP servers for codebase navigation and team tooling.

### 5. Skills & Reusable Patterns

**Us:** Rich skill library (create-feature, create-aggregate, create-module, cqrs-patterns, etc.) with mandatory skill mapping in plans. Skills are authoritative over inline instructions.

**Anthropic:** Custom slash commands are heavy (Security owns 50% of monorepo's commands). Subagents as specialized units (headline-agent, description-agent).

**Rakuten:** Not publicly documented.

**TELUS:** Fuel iX template gallery serves a similar role for non-developers (21k copilots built).

**Zapier:** Three open-source Skills. Clean articulation: "Skills encode best practices; MCP runs them at scale."

### 6. CLAUDE.md & Configuration Maturity

**Us:** Detailed CLAUDE.md with domain conventions, guardrails, architecture rules, port conventions, tech stack. Service-level CLAUDE.md files for each service. Rule files for cross-cutting concerns.

**Anthropic:** Continuously refined — end-of-session improvement suggestions. Tool-calling corrections inline. Non-developer persona instructions. "The better you document, the better Claude performs."

**Rakuten:** Auto-loaded every session. Credited as the enabler of the 7-hour stable run. Includes coding guidelines that act as "safety guardrails" for non-engineers.

**TELUS:** Not documented at this level.

**Zapier:** Not documented at this level.

### 7. Event-Driven / CI Automation

**Us:** Pipeline starts when a human says "go." No triggers from external events.

**Anthropic:** GitHub Issues trigger Claude to propose code. PR comments auto-addressed via Actions. Filing an issue = kicking off work.

**Rakuten:** AI-powered code review on every PR (automated trigger).

**TELUS:** Not documented at developer level (platform-level automation exists).

**Zapier:** Slack emoji → analyze context → generate code → open MR. Most innovative trigger pattern documented.

### 8. Developer Experience & Onboarding

**Us:** Skills + agent pipeline serve experienced users. No documented onboarding path for the system itself.

**Anthropic:** "First stop" for any task — ask Claude which files to examine first. Feed entire codebase to Claude for onboarding.

**Rakuten:** Claude Code as the codebase-navigation surface for new hires on multi-language monorepo.

**TELUS:** Template gallery + 21k copilots = self-service onboarding at massive scale.

**Zapier:** Skills + MCP for self-service builders. 89% AI adoption across all employees.

---

## Scorecard (1-10)

### Core Pipeline

| Category | Fokus | Anthropic | Rakuten | TELUS | Zapier | Notes |
|---|:---:|:---:|:---:|:---:|:---:|---|
| **Pipeline rigor & traceability** | 9 | 6 | 4 | 3 | 5 | was already strongest |
| **Parallelism & throughput** | 6 | 7 | 9 | 7 | 6 | Team lead can spawn multiple teams in parallel; worktrees isolate work. Not yet exercised at scale. |
| **Autonomy & trust classification** | 6 | 9 | 7 | 6 | 7 | Team lead has classification guide; not yet battle-tested |
| **MCP & external integration** | 5 | 7 | 3 | 9 | 10 | PO pulls Jira tickets via MCP as a starting point |
| **Skills & reusable patterns** | 9 | 8 | 3 | 6 | 7 | |
| **CLAUDE.md & configuration maturity** | 9 | 10 | 7 | 3 | 3 | Lessons consolidation skill planned but not built yet |
| **Event-driven / CI automation** | 2 | 8 | 5 | 5 | 9 | intentionally manual until flow is trusted |
| **Developer experience & onboarding** | 5 | 8 | 7 | 9 | 8 | |
| **Review quality & safety** | 9 | 6 | 5 | 7 | 6 | |

*Non-engineer enablement removed — TELUS and Zapier score high because they built internal bot-builder platforms (Fuel iX, Zapier automations), not developer pipelines. Different product category, not comparable.*

### SDD (Spec-Driven Development)

| Category | Fokus | Anthropic | Rakuten | TELUS | Zapier | Notes |
|---|:---:|:---:|:---:|:---:|:---:|---|
| **Spec completeness & precision** | 10 | 4 | 3 | 5 | 5 | Our feature specs are the most detailed — 14 business rules, 17 ACs on JiraSync |
| **Constitution (CLAUDE.md + rules)** | 9 | 9 | 7 | 4 | 4 | Anthropic matches us; both treat CLAUDE.md as load-bearing |
| **Spec → plan → task pipeline** | 9 | 6 | 3 | 4 | 6 | GitHub Spec Kit formalized this; we had it independently |
| **Input funnel (where specs originate)** | 7 | 7 | 4 | 6 | 7 | PO accepts Jira tickets as starting point, not just scratch |
| **Challenge & refinement before coding** | 9 | 3 | 3 | 3 | 3 | Our PO challenges assumptions, tests essentiality — unique in this group |
| **Drift detection (spec ↔ code)** | 4 | 4 | 6 | 4 | 4 | Nobody does this well. Rakuten's TDD gives slight edge. Our reviewer partially covers it. |
| **Spec maintained post-build** | 2 | 3 | 2 | 3 | 2 | Industry-wide weakness. We're spec-first, not spec-anchored. |
| **Machine-readable spec structure** | 7 | 5 | 3 | 4 | 5 | Our template is structured markdown — architect can parse it mechanically |

### Totals

| | Fokus | Anthropic | Rakuten | TELUS | Zapier |
|---|:---:|:---:|:---:|:---:|:---:|
| **Core Pipeline (9 categories)** | 60 | 69 | 50 | 55 | 61 |
| **SDD (8 categories)** | 57 | 41 | 31 | 33 | 36 |
| **Combined (17 categories)** | **117** | **110** | **81** | **88** | **97** |

---

## What the Scores Show

- **Combined score: we lead by 7 points over Anthropic** (117 vs. 110). SDD is where we dominate.
- Our profile is the inverse of Anthropic's: they lead on speed/flexibility (69 vs. 60 core), we lead on specification rigor (57 vs. 41 SDD).
- We're ahead of everyone combined, including Zapier (117 vs. 97).

### The tradeoff made visible
```
                    Core Pipeline    SDD Rigor    Combined
Fokus                   60              57          117    ← rigorous, structured specs
Anthropic               69              41          110    ← fast but loose specs
Zapier                  61              36           97    ← balanced, ticket-driven
TELUS                   55              33           88    ← platform play, thin specs
Rakuten                 50              31           81    ← pure speed, TDD-only
```

Anthropic optimizes for: **ship fast, learn from production.**
We optimize for: **get it right, ship once.**

Both are valid. The difference is risk tolerance and team culture.

---

## Key Takeaways (Updated)

1. **SDD reframes our position.** On core pipeline alone we trail Anthropic (57 vs. 69). Adding the SDD lens flips it — combined we lead 114 vs. 110. Our spec quality is a competitive advantage, not overhead.

2. **Remaining gaps (prioritized):**
   - **Event-driven automation** (2/10) — largest single gap. Intentionally deferred until flow is trusted. First step: PR-triggered reviews.
   - **Parallelism** (6/10) — infrastructure exists, not yet exercised at scale.
   - **Spec drift detection** (4/10) — industry-wide gap. No one solves this well yet.
   - **Living specs post-build** (2/10) — industry-wide gap. Future opportunity for lessons consolidation skill.

3. **We don't need to become Anthropic.** Their speed advantage comes from a culture (everyone dogfoods, PMs vibe-code, side quests) that doesn't transfer to most teams. Our rigor advantage serves a growing team better — it prevents the "but I thought you meant..." coordination failures that multiply with headcount.
