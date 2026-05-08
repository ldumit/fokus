# How Anthropic, Rakuten, TELUS, and Zapier Technically Use Claude Code

Technical lens. Four sections, depth proportional to available material, comparative matrix at the end.

---

## 1. Anthropic (deepest section — they're the source)

Sources: Anthropic's "How Anthropic teams use Claude Code" PDF (June 2025) + claude.com/blog/how-anthropic-teams-use-claude-code (July 24, 2025). Documents ten internal departments: Data Infrastructure, Product Development (the Claude Code team), Security Engineering, Inference, Data Science / ML Engineering, API (Knowledge), Growth Marketing, Product Design, RL Engineering, Legal.

### Tools & infrastructure

- **Models.** Claude Code uses the latest research-model snapshots automatically; the API team explicitly notes this makes Claude Code their primary surface for "dogfooding model iterations." For plan-then-execute work, the documented pattern is `opusplan` (Opus reasons in plan mode, Sonnet executes). At time of writing (May 2026), Sonnet 4.6 is the daily driver and Opus 4.7 is reserved for hard reasoning.
- **Surface.** Native terminal CLI is the primary surface, with the official VS Code extension layered on top for diff view, plan editing, and `@-mention` of files with line ranges. The extension and CLI share the same conversation history (`claude --resume`).
- **MCP servers.** The Data Infrastructure team's explicit recommendation: **"Use MCP servers instead of the BigQuery CLI"** — they prefer MCP for sensitive data so Claude Code's access can be logged and scoped. The Growth Marketing team built a **custom Meta Ads MCP server** that queries campaign performance, spending, and ad effectiveness directly from inside Claude Desktop, eliminating platform-switching for ROI analysis.
- **GitHub Actions integration.** Heavily used by Product Design and Product Engineering: filing a GitHub issue triggers Claude to propose code without anyone opening Claude Code; PR comments (formatting, function renaming) are auto-addressed by Claude via Actions.
- **Subagents.** Growth Marketing built **two specialized subagents** — one for ad **headlines** (30-character limit), one for ad **descriptions** (90-character limit) — chained from a parent workflow that ingests a CSV of underperforming ads.
- **Custom slash commands.** Security Engineering accounts for **50% of all custom slash command implementations in the entire monorepo** — they treat slash commands as the dominant abstraction for repeated workflows like Terraform plan review and runbook generation.
- **Auto-accept mode.** The Claude Code product team itself uses `Shift+Tab` to enable auto-accept and runs autonomous loops where Claude writes → tests → iterates. They built **Vim mode** this way: ~70% of the final Vim-bindings implementation came from Claude's autonomous work in auto-accept mode.
- **Parallel sessions.** Data Infrastructure opens multiple Claude Code instances across different repositories — each maintains its own context, so engineers can switch between long-running tasks "after hours or days" without losing state. Data Science treats it as a **"slot machine"**: commit state, let Claude run autonomously for ~30 minutes, accept or `git reset` and try again.

### Workflow patterns

- **First-stop planning.** The API team documents Claude Code as their "first stop" for any task: ask which files to examine before doing anything else.
- **Test-driven development.** Security Engineering explicitly redesigned their workflow from "design doc → janky code → refactor → give up on tests" to **ask Claude for pseudocode → guide it through TDD → check in periodically**. The Inference team has Claude generate unit tests with edge cases after writing core functionality.
- **Synchronous vs. asynchronous classification.** The Claude Code product team articulates a sharp rule: peripheral / abstract features run async with auto-accept; **core business logic runs synchronously** with detailed prompts and real-time monitoring.
- **Incident response.** Security Engineering feeds stack traces + docs to Claude Code during incidents; what was 10–15 minutes of manual code scanning is now ~5 minutes (≈3× faster).
- **Image-driven debugging.** Data Infrastructure resolved a Kubernetes IP-pool exhaustion incident by **pasting dashboard screenshots into Claude Code** — Claude walked them menu-by-menu through the GCP UI and produced exact commands to create a new IP pool, saving ~20 minutes during an outage and avoiding paging the networking team.
- **Image-driven prototyping.** Product Design pastes Figma mockups (`Cmd+V`) into Claude Code to generate functional prototypes; Figma + Claude Code are open ~80% of the workday.
- **Checkpoint-heavy commits.** RL Engineering and Data Science stress committing state before any autonomous run so `git reset` is cheap. Their CLAUDE.md files include explicit tool-calling corrections such as "run pytest, not run; don't `cd` unnecessarily — use the right path."

### Configuration & conventions

- **CLAUDE.md is load-bearing.** Data Infrastructure's headline tip: "the better you document your workflows, tools, and expectations in CLAUDE.md, the better Claude Code performs." End of session, they ask Claude to summarize the work and **suggest CLAUDE.md improvements** — a continuous-improvement loop on the doc itself.
- **Memory files for non-developers.** Product Design's recommended pattern is to write CLAUDE.md instructions like: "I'm a designer with little coding experience — explain in detail and make smaller, incremental changes."
- **Persistent dashboards over notebooks.** Data Science / ML stopped writing throwaway Jupyter notebooks; they have Claude Code build **5,000-line TypeScript / React analytics dashboards** for model evaluation despite the engineers admitting they know "very little JavaScript and TypeScript."

### Specific use cases (non-engineering)

- **Legal:** prototype "phone tree" routing system to connect Anthropic employees with the right lawyer; an accessibility communication assistant built by a team member in one hour using native speech-to-text.
- **Growth Marketing (team of one):** Figma plugin that generates up to 100 ad variations by swapping headlines / descriptions ("hours of copy-pasting → half a second per batch"); ad-copy creation reduced from 2 hours → 15 minutes.
- **Finance (cross-team self-service):** Data Infrastructure showed finance staff how to write **plain-text workflow files** ("query this dashboard → run these queries → produce Excel"); Claude Code executes the entire flow, prompting for missing inputs (dates, etc.).

---

## 2. Rakuten

Sources: claude.com/customers/rakuten + rakuten.today/blog/rakuten-accelerates-development-with-claude-code. The most quantitative external customer story Anthropic publishes.

### Tools & infrastructure

- **Models.** Rakuten validated Claude Opus 4 by running it autonomously for **7 hours** on a refactor of vLLM (a 12.5-million-line, multi-language open-source library), achieving 99.9% numerical accuracy on the activation-vector extraction task — the experiment Anthropic cited at the Claude 4 launch. Rakuten now publishes results against an internal **Rakuten-SWE-Bench**; per Anthropic's Opus 4.7 launch materials, "On Rakuten-SWE-Bench, Claude Opus 4.7 resolves 3x more production tasks than Opus 4.6, with double-digit gains in Code Quality."
- **Surface.** Terminal-first. Yusuke Kaji (GM, AI for Business) explicitly cites the **terminal interface** as enabling non-engineers to participate without directly editing code: "With appropriate context and coding guidelines, Claude Code acts as a safety guardrail."
- **Parallel sessions / "ambient agent."** Kenta Naruse (ML Engineer) is building an **"ambient agent"** that decomposes a complex monorepo update into **24 parallel Claude Code sessions**, each owning a slice of the work — a project that would normally take >1 month.
- **AI-powered code review.** Pull requests get instant feedback from Claude before human review.

### Workflow patterns

- **Five-tasks-in-parallel pattern.** Kaji's verbatim quote: "You can have five tasks running in parallel by delegating four to Claude Code while focusing on the remaining one." This is the model Rakuten teaches developers — Claude as a parallel-execution backplane, not a pair programmer.
- **TDD by default.** Diego Mateos (Senior ML Engineer): "I wasn't naturally using test-driven development before, but Claude Code makes it so easy. It generates comprehensive tests instantly, then builds features that pass them."
- **24-day → 5-day cycle.** Rakuten reports a 79% reduction in time to market for new features (24 working days → 5). The redesigned lifecycle wraps Claude Code around: unit tests, API mocking, component scaffolding, bug fixes, and auto-generated docs.
- **Persistent context.** CLAUDE.md files auto-load project context and conventions every session — "no forgetting early context during extended sessions" was the success criterion for Opus 4 in the 7-hour run.

### Onboarding

New hires use Claude Code as the codebase-navigation surface — explicitly cited as a way to ramp on the multi-language monorepo. The "AI-nization ratio" is one of Rakuten's tracked metrics alongside test coverage and innovation velocity.

---

## 3. TELUS

Sources: claude.com/customers/telus + cloud.google.com/customers/telusai + tecknexus.com/telus-ai-platform-enterprise-scale-telecom-innovation + telusdigital.com newsroom (Nov 4, 2025). TELUS's pattern is fundamentally different from Anthropic and Rakuten: Claude Code is **one of several developer-facing surfaces** plugged into a larger internal platform called **Fuel iX**. The relevant technical detail isn't a single workflow — it's the broker architecture.

### Platform architecture

- **Fuel iX** is TELUS's proprietary GenAI platform built on **Google Cloud Vertex AI**. Vertex AI Model Garden brokers access to 40+ models; **~90% of TELUS's AI-model traffic flows through Vertex AI** (Distinguished Engineer Justin Watts, on Google Cloud's TELUS case page).
- **Token volume.** Fuel iX processes **~100 billion tokens per month** (Watts).
- **Model routing.** Claude is the preferred model for "complex and creative tasks" and parallel tool calling across enterprise systems (Jira, GitHub, documentation search, web retrieval). Gemini Flash is preferred where latency dominates customer-facing experiences. Watts: "Whether we add a hundred new tools or a thousand, [Claude] excels at picking the right one at the right time."
- **Hosting.** Claude on Vertex AI; integrates with TELUS's GKE and Cloud Run workloads — Watts on the choice: "We get a model that excels at tool calling on a comprehensive platform that integrates with our core Google Cloud workloads like GKE and Cloud Run — that's the magic."

### Developer surface (where Claude Code fits)

- **Claude Code is used inside developers' existing IDEs**, alongside GitHub Copilot and Cline. Watts on the deliberate non-disruption: "Developers don't want to abandon the tools they already love. Claude enhances these existing tools, making them more powerful without disrupting the creative process."
- Developers use Claude Code in **VS Code and GitHub** for real-time refactoring (per the customer-story page).
- **Engineering teams report shipping code 30% faster.** No unique technical innovations specific to Claude Code are documented from TELUS — the differentiator is the platform-level model routing, governance, and the **MCP layer**, which Watts calls "the most transformative technology to impact TELUS in decades."

### Custom solutions and onboarding

- TELUS Digital's November 4, 2025 press release reports that Fuel iX now serves **70,000 TELUS team members** — up from the 57,000 figure on Anthropic's customer page.
- That same release reports **21,000 custom AI copilots** built across the workforce, again up from the 13,000 figure on Anthropic's older customer page. Examples: HR-policy lookup, real-time translation, brand-guideline assistants, conversation coaches.
- 500,000+ hours saved; 47 large-scale GenAI solutions producing >$90M in measurable benefits.
- ISO 31700-1 Privacy by Design certification on the Fuel iX–powered customer support tool — the first GenAI solution to achieve it.

---

## 4. Zapier

Sources: claude.com/customers/zapier + zapier.com/blog/zapier-mcp-anthropic-api + zapier.com/blog/zapier-mcp-agent-skills + github.com/zapier/zapier-mcp.

### Tools & infrastructure

- **Adoption shape.** 89% AI adoption across all employees (highest in Zapier's history); **800+ internal AI agents** deployed; customer-facing usage of the Anthropic integration grew 10× year over year.
- **Surface.** Engineering uses **Cursor as the IDE** with Claude Code in support — CEO Wade Foster on the Beyond The Prompt podcast: "We've got our engineers using Cursor, we have our own [tooling] using Zapier and the AI capabilities within Zapier." The customer-story page confirms Claude Code is also used directly for code generation and automated merge requests.
- **VCS terminology.** Zapier uses **GitLab** (the Anthropic case study describes "merge requests," not pull requests; Zapier's open-source Skills also reference Jira/GitLab terminology).
- **MCP at the center.** Zapier's strategy is MCP-first. Two distinct MCP surfaces matter:
  1. **Zapier MCP** (the public, customer-facing server at `mcp.zapier.com`) — the `zapier/zapier-mcp` GitHub README states it gives AI direct access to **9,000+ apps and 40,000+ actions**, exposed to any MCP client (Claude Code, Claude Desktop, Cursor, ChatGPT). Anthropic's Messages API natively supports the Zapier MCP connector.
  2. **Internal MCP servers** that "help Claude navigate their codebase and build team-specific tools for areas like developer experience and design" (Reid Robinson, Lead PM, AI). The Anthropic case study is the only public reference; no architecture details are public.

### Skills (the most concrete public artifact)

On December 18, 2025, Zapier shipped three open-source Skills in the `zapier/zapier-mcp` repo at `github.com/zapier/zapier-mcp/tree/main/skills`:

| Skill | What it does |
|---|---|
| **work-on-ticket** | Pulls Jira ticket details, creates feature branches, handles initial planning |
| **code-review** | Evaluates code changes for quality, security, performance, consistency |
| **git-commit** | Writes Conventional Commit messages with context from the originating Jira ticket |

Lisa Chapello, Zapier's Director of AI Product Management, summarizes the design: **"Skills encode best practices; MCP runs them at scale."** This is the cleanest articulation of the Zapier pattern — Skills as policy, MCP as the action surface.

### Headline workflow — the Slack-emoji → merge-request system

The most-cited Zapier engineering anecdote is from CTO Bryan Helmig: a system where **adding an emoji to a Slack thread triggers Claude to analyze context, generate code, and open a merge request** in minutes. The architecture is **not publicly documented** by Zapier — no blog post, talk, or repo describes the implementation.

### Other documented uses

- **Marketing**: automated workflows where Claude drafts content → saves to Google Docs → notifies a Slack channel for review.
- **Customer research / Design**: Claude Artifacts used live during user interviews to prototype ideas in real time.
- **Strategic intelligence**: Robinson uses Claude + Zapier MCP to research event attendees from CRM + web before meetings.

---

## 5. Comparative matrix — what each is doing differently

| Dimension | Anthropic | Rakuten | TELUS | Zapier |
|---|---|---|---|---|
| **Primary model** | Latest research snapshot; `opusplan` for plan→execute; Sonnet 4.6 daily driver | Opus 4.x for long-horizon refactors; benchmarked on internal Rakuten-SWE-Bench | Claude on Vertex AI (Opus + Sonnet); Gemini Flash for low-latency UX | Not publicly specified for engineering; Sonnet 4 cited in third-party recaps |
| **Hosting / API surface** | Anthropic API direct | Anthropic API direct | **Vertex AI Model Garden** (~90% of model traffic); GKE + Cloud Run | Anthropic API direct + Zapier MCP layer |
| **Primary IDE / surface** | Terminal CLI + VS Code extension; auto-accept (`Shift+Tab`) heavy use | Terminal-first (explicit choice — enables non-engineers) | VS Code + Claude Code; GitHub Copilot + Cline alongside | Cursor as primary IDE; Claude Code for codegen/MR automation |
| **MCP usage** | Recommended **over CLI for sensitive data**; custom Meta Ads MCP server | Not specifically documented | "Most transformative technology to impact TELUS in decades" | MCP-first; public Zapier MCP (9k+ apps, 40k+ actions) **plus** undisclosed internal MCP servers |
| **Subagents** | Growth Marketing: headline-agent + description-agent | Not publicly named (24-session "ambient agent" is parallel-orchestration, not typed subagents) | Not publicly named | Implied via Skills chain |
| **Skills (named, public)** | Many internal | Not publicly named | Not publicly named (Fuel iX templates serve similar role) | **work-on-ticket, code-review, git-commit** (open-source, Dec 2025) |
| **Custom slash commands** | Heavy: Security Engineering owns 50% of monorepo's custom commands | Not publicly documented | Not publicly documented | Not publicly documented |
| **Hooks / lifecycle events** | Implied (test/lint loops in auto-accept), not enumerated | Not documented | Not documented | Not documented |
| **CLAUDE.md practice** | Continuously refined at end of every session; tool-calling corrections inline; non-developer personas | Auto-loaded with conventions every session — credited for stable 7-hour run | Not documented | Not documented |
| **Parallel sessions** | Multiple instances per repo (Data Infra); "slot machine" 30-min runs (Data Science) | **24-session "ambient agent"**; 5-tasks-in-parallel mental model | Not documented | Not documented |
| **Auto-accept / autonomy** | Yes — `Shift+Tab`, autonomous loops with self-verifying tests | Yes — 7-hour unsupervised run on vLLM | Not documented | Implied via Slack-emoji → MR workflow |
| **Code review automation** | GitHub Actions auto-addresses PR comments | "AI-powered code review" on every PR | GitHub-integrated via Claude Code | `code-review` Skill + Slack-emoji-triggered MR system |
| **Incident response** | Stack-trace ingestion, screenshot-driven; 10–15 min → ~5 min | Not documented | Not documented | Not documented |
| **Onboarding pattern** | Feed entire codebase to Claude; CLAUDE.md replaces data catalogs | Claude Code as monorepo navigator for new hires | Fuel iX template gallery for non-developers | Skills + MCP for self-service builders |
| **Non-engineering use** | Legal, Growth Marketing, Finance, Design Figma → working code | Non-engineers use the terminal with guardrails | 21,000+ Fuel iX–built copilots across 70k employees | Marketing pipelines, design prototypes, strategic intelligence |
| **Headline metric** | (No single number — qualitative) | **24 days → 5 days** (79%↓); 7-hour autonomous run; 99.9% accuracy; 3× more production tasks resolved on Rakuten-SWE-Bench from Opus 4.6 → 4.7 | 30% faster code shipping; $90M+ benefits across 47 solutions; ~100B tokens/month | 89% AI adoption; 800+ internal agents; 10× growth in customer-facing usage |
| **Distinctive technical innovation** | Subagent-pair-per-task; image-driven debugging; CLAUDE.md as living document | Long-horizon autonomous runs; 24-parallel-session ambient agents | Multi-model broker (Vertex) with Claude Code as one developer surface | MCP-first with policy-as-Skills + action-as-MCP separation |

---

## Caveats on source quality

- **TELUS's Claude Code specifics** are thin. Most architectural detail concerns Fuel iX, not Claude Code. The TELUS Digital November 2025 numbers (70k team members, 21k copilots) supersede the older 57k / 13k figures still on Anthropic's customer page.
- **Zapier's "Slack-emoji → MR" system** is widely cited but its implementation is not publicly documented. The `zapier/zapier-mcp` repo and the public Zapier MCP are the closest verifiable artifacts.
- **Rakuten's 7-hour run** was a controlled validation of Opus 4, not a routine production workflow. The 24-parallel-session ambient agent was described as in-progress at the time of the case study.
- **Anthropic's PDF reflects mid-2025 practice.** Several capabilities (Agent Teams, Skills as a first-class file format, plugins, Claude Code on the web) shipped between October 2025 and March 2026 and aren't in the original PDF — but the documented patterns are forward-compatible.
- **Source quality varies.** The Anthropic blog/PDF and the Anthropic-hosted customer pages are first-party. Numbers from third-party aggregators (DataStudios, TeckNexus) often paraphrase the same Anthropic source and occasionally embellish; prefer first-party where in tension.
