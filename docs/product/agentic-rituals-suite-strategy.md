# Agentic Agile Ritual Suite — Product Strategy & Decisions

**Owner:** Laurentiu
**Status:** Paused (revisit after Claude Code for .NET course ships, June 2026)
**Last updated:** April 18, 2026 — merged suite extensions (meeting-to-spec, spec-to-tickets, spec+delivery tracker)
**Purpose:** Capture the full thinking, landscape research, and decisions so future-Laurentiu can pick this up without re-deriving the reasoning.

---

## 1. One-line summary

A suite of opinionated agile rituals (retro, spec-to-tickets, planning poker, and more) built for teams where AI agents are first-class participants — not a feature, not a data source, but actual team members with their own cards, votes, and accountability. A shared memory layer underneath ties everything together and creates the moat. Starts as course content / open-source, productizes only if signals warrant it.

---

## 2. Why this idea — the origin

The idea surfaced organically during a strategy session while evaluating which plugin to use for an OMC pilot:

- Needed a small warm-up app to test an OMC + Codex cross-model review setup on Windows-native.
- Landed on a scrum retro tool as a "few hours, ship to my team Monday" project.
- While discussing retro scope, recognised that existing retro tools (Parabol, GoRetro, TeamRetro, Rovo's helper) all treat AI as a feature bolted onto an existing workflow, not as a participant reshaping the workflow.
- Expanded to "what else in the agile ceremony stack is now subtly wrong because it assumed only humans do the work?" — answer: planning poker, standup, backlog refinement, PR review, post-incident review.
- Recognised that if those tools share memory and read from Jira/GitHub, they become a suite with data gravity — not just a collection of standalone toys.
- In a follow-up session, the thesis was stress-tested against an independent idea generation pass. Four ideas surfaced, three of which (meeting-to-spec, spec-to-tickets, spec+delivery tracker) independently pointed at the same suite — confirming the thesis is robust and the upstream (PO-facing) rituals had been underweighted in the original shape.

**The trigger insight:** agile rituals were designed in the early 2000s around the assumption that humans do all the implementation work. In 2026, agents write 30-70% of commits on real engineering teams. Every ritual designed for "a team of humans" is now subtly wrong for "a team of humans plus agents."

The incumbents cannot ship the fix because they have to stay neutral across their existing customers (who mostly don't have agentic teams yet). A .NET-opinionated, agent-native ritual suite is a real wedge for a small independent builder.

---

## 3. What we're building (and not)

### 3.1 In scope (eventually)

**PO-facing / upstream rituals:**
- **Meeting-to-spec** — paste a planning or refinement meeting transcript, get structured PRD + tickets + open questions. Fills the gap between "Zoom recording" and "something a team can act on." Feeds the memory layer; extraction schema links to retro outcomes and past ticket history.
- **Spec-to-tickets** — Socratic interview flow turning a rough feature idea into structured Jira/Linear tickets with acceptance criteria. The upstream feeder for planning poker. Memory layer learns which clarifying questions matter for which feature types.

**Engineer-facing / execution rituals:**
- **Retro** — notes, columns, voting, plus: agent-authored cards, evidence attached to cards (Jira/PR/commit links with inline preview), action items that become PRs against CLAUDE.md/skill files.
- **Planning poker** — voting plus: agent as a voting participant with visible reasoning, similar-ticket history, propose-and-argue UX (agent proposes, humans accept in one click or start full vote), feedback loop from estimate to actual.
- **Post-incident review** (later) — agent drafts timeline from logs + deployment history + related PRs, human edits and signs.
- **Agentic PR review ritual** (later) — multiple agents (different providers) review agent-authored PRs, structured disagreement resolution logged.

**The memory layer — the moat:**
- **Team memory / context store** — ingests Jira tickets, GitHub commits, retro outputs, poker estimates, sprint actuals, meeting transcripts, spec drafts. Correlates. Feeds each ritual with relevant historical context.
- **Spec + delivery tracker** — the concrete productized form of the memory layer. Two-sided tool: PO captures structured spec via interview, hands to agents, traces delivered code back to original requirements, flags drift. Requirements become navigable, searchable, agent-queryable across sprints. This is the piece that creates real switching cost if the suite productizes.

### 3.2 Explicitly NOT in scope

- **A Jira replacement.** This was considered and rejected. Jira's moat is 20+ years of integrations, permissions, roles, audit logs, SSO, imports, API contracts, compliance certs. The "fun" 5% of Jira (kanban board + tickets + comments) is not the product. You cannot displace Atlassian solo. The suite *reads from* Jira/GitHub; it does not replace them.
- **Roadmap tooling.** Dependency trees, Gantt views, capacity planning, what-if scenarios. Entire competitor stack (Productboard, Aha!, Jira Advanced Roadmaps). Rabbit hole. Wait until v3+.
- **Auto-estimation (fully automated).** Rejected on UX grounds. Estimation's value is the conversation it forces about scope and complexity — auto-estimation kills that. Correct pattern is propose-and-argue, not auto.
- **Generic (non-.NET) positioning.** At least for the wedge. Every competitor is generic. .NET-specific is the defensibility.
- **Consumer anything.** B2B engineering teams only.
- **CLAUDE.md / skills generator.** Considered, decided out. It's a side artifact, not a ritual, not a suite component. Belongs alongside Claude Code for .NET course as demo material, not in this product.

---

## 4. Positioning

**The one-sentence pitch:**
> A memory layer and agentic ritual suite on top of Jira or GitHub — so your team's specs, estimation, retros, and delivery traceability get smarter every sprint instead of starting from zero.

**The target customer:**
- Engineering team, 5-30 developers
- Primarily .NET shop (start) — expandable to JVM/TypeScript later if traction warrants
- Already using Jira or Linear + GitHub or Azure DevOps
- 2+ team members meaningfully using Claude Code / Cursor / Codex for implementation work (not just autocomplete)
- Has felt the specific pain of vague specs, consistently-wrong estimates, or retros missing context

**Why they'd buy:**
- Incumbents (Parabol, Appfire, Rovo) are generic. We speak .NET.
- Incumbents treat AI as a feature. We treat agents as teammates.
- Incumbents don't close the feedback loops between ritual data and agent infrastructure. We do (retro → CLAUDE.md PRs; spec → tickets → estimates → actuals → retro).
- Data gravity: after 6 months of correlated ritual data + commit history + spec-to-delivery traces, switching cost is high.

**Why incumbents won't ship this:**
- Atlassian/Linear can't be opinionated about specific agents (Claude vs Codex vs Cursor) or specific stacks — breaks neutrality across their 300k+ customers.
- The .NET TAM is too small to be interesting to them.
- The "retro output edits CLAUDE.md" pattern requires opinions about how teams structure agent infrastructure, which generic tools can't have.
- Spec-to-delivery traceability is product-sized and opinionated — the generic attempts (Jira Requirements, Polarion, Jama) bloat because they try to serve all of enterprise software. A narrow .NET + agentic-teams version is tractable; a universal version is not.

---

## 5. Landscape (condensed)

Full landscape research lives in `agentic-rituals-landscape.md`. The relevant summary:

### Already commoditized (do not rebuild)
- AI grouping of retro notes
- AI-generated retro summaries and action items
- AI-suggested estimates from historical Jira data (SprintPoker, Agile Poker, PlanningPokerAI all ship this)
- Auto-generated sprint reports from Jira data (Rovo, Linear Agent, many others)
- Git-activity-based standup summaries (Gitmore, Steady)
- Natural language → Jira issues / JQL (Rovo, Linear Agent) — *but they skip the Socratic clarification that makes specs real*
- Generic team health checks (Echometer, DailyBot, Reetro)
- Backlog readiness checkers (Rovo, Linear Triage Intelligence)

### Still open (our wedges)
1. **Retro output → agent infrastructure updates (CLAUDE.md, skills).** Martin Fowler named this pattern in "Feedback Flywheel" on Thoughtworks, April 2026. Not productized anywhere. 3-6 month first-mover window.
2. **Agents as first-class ritual participants.** DailyBot is closest (agents-report-to-standup framing). No tool has agent-cards-in-retros or agent-votes-in-poker.
3. **Cross-ritual memory layer.** Every tool is a silo today. Correlating spec + tickets + estimates + retro + sprint actuals into a single team learning record is the deepest moat.
4. **Socratic interview for spec-to-tickets.** Rovo and Linear Agent generate tickets from natural language but skip the hard part — asking the right clarifying questions before writing. A structured interview flow that exposes assumptions is genuinely novel.
5. **Spec-to-delivery traceability for agentic teams.** Agent-written code drifts from specs silently. Existing traceability tools (Jira Requirements, Polarion, Jama) are enterprise bloat. A narrow, agentic-team-native version does not exist.
6. **.NET-native agentic tooling.** Every competitor is language-agnostic. Nobody will build .NET-specific because the TAM is too small for them — which is exactly what makes it defensible for a solo builder.
7. **Self-hostable / open-source agentic suite.** Parabol is open-source retro only. No open-source agentic suite exists.

### Direct existential threats
- **Atlassian Rovo** — 5M MAU, bundled free into Jira Cloud Standard+. Covers retro prep, sprint planning, standup summaries. Cannot be opinionated about .NET or specific agents.
- **Linear Agent** — public beta March 2026, Skills + Automations + Code Intelligence. Linear CEO publicly stated "issue tracking is dead." Moving aggressively into this space.
- **DailyBot 3** — explicit "agents as team members" positioning. Closest conceptual competitor on agent-as-participant. Attacking from standup side.

### Key signal: Martin Fowler's "Feedback Flywheel"
Published April 2026 on martinfowler.com. Describes exactly the retro-output-edits-CLAUDE.md pattern as emerging best practice. Not productized. Historically, Fowler naming a pattern precedes productization by 6-9 months. This is the tightest timing window in the whole thesis.

---

## 6. Product architecture

```
           PO-facing inbound                    Engineer-facing execution
      ┌────────────────────────┐           ┌────────────────────────────┐
      │  Meeting-to-spec       │           │  Retro                      │
      │  Spec-to-tickets       │           │  Planning poker             │
      │  (Socratic interview)  │           │  Standup  /  PR review (v3) │
      └───────────┬────────────┘           └──────────────┬─────────────┘
                  │                                       │
                  └─────────────┐         ┌───────────────┘
                                ▼         ▼
                ┌─────────────────────────────────────┐
                │  Team Memory / Context Store         │   ← the moat
                │  (specs, tickets, estimates,         │
                │   actuals, retro notes, commits,     │
                │   meeting transcripts, agent logs)   │
                │                                      │
                │  Spec + Delivery Tracker             │   ← the memory
                │  (traceability, drift detection)     │      layer's
                │                                      │      concrete form
                └─────────────────────────────────────┘
                                 │
                ┌────────────────▼────────────────┐
                │  Jira / GitHub / Azure DevOps   │  ← the pipe
                │  MCP layer                       │
                │  (read mostly, opt-in writes)    │
                └─────────────────────────────────┘
```

Each ritual writes to the memory layer and reads from it. The memory layer reads from Jira/GitHub/Azure DevOps via MCP. Writes to upstream systems are rare, opt-in, and explicitly confirmed (e.g., pushing agreed estimates back to Jira, creating tickets from spec-to-tickets interviews).

The memory layer — concretized as the spec + delivery tracker — is the hard part and the moat. The rituals are thin UIs on top.

---

## 7. Build sequencing (when we restart)

Do not parallelize. Each step validates before triggering the next.

When the product is unpaused, **reconsider whether retro or spec-to-tickets is the better v1 anchor.** Original plan had retro v1 as the front door. After the suite extensions, spec-to-tickets has a stronger case: you use it every feature cycle (higher dogfooding pressure than weekly retros), it forces Jira integration from day one, it directly feeds planning poker, and it's demoable on LinkedIn without needing a full sprint cycle to show value. Retro stays as the warm-up build regardless (you're shipping it this weekend) — the question is whether it's also the product front door or just the course demo.

### Weekend 1 — Retro v1 (for your own team)
- Scope: columns + notes + voting + evidence links
- Stack: Vue 3 + .NET 9 minimal API + EF Core + SQLite + SignalR
- No memory layer yet, no agents yet
- Ship to team Monday, use for 3 real retros
- Gate to step 2: did your team actually use it? Did they ask for more?

### Weeks 2-3 — Memory layer (first real investment)
- Separate service/module
- Ingest Jira tickets + GitHub commits + the retro's own data
- Schema, ingestion jobs, embedding of similar tickets for semantic search
- No UI — this is plumbing
- Gate to step 3: can you answer "show similar tickets to X" with useful results?

### Weekend 4 — Spec-to-tickets v1 OR Planning poker v1

Decision point. Pick based on signals from steps 1-2:

- If the team is feeling ticket-quality pain → **spec-to-tickets** first. Socratic interview, one feature type (new capability), produces structured Jira-ready output. Natural demo. ~4-5 days.
- If the team is feeling estimation pain → **planning poker** first. Reads from memory, shows similar-ticket history at estimation time. No auto-propose yet, no agent participant yet. ~2-3 days.

Gate to step 4: did the chosen tool get used on a real feature / sprint cycle? Did it change team behavior?

### Month 2 — The other one, plus retro v2

Build whichever of spec-to-tickets or planning poker wasn't built in step 3. In parallel, retro v2: agent-participant cards based on sprint telemetry (retry counts, failed tests, skipped patterns). Gate: do the agent's cards generate real discussion?

### Month 3 — Poker v2: propose-and-argue + feedback loop

- Agent proposes estimate with visible reasoning
- One-click accept or trigger full vote
- When the actual comes in, memory layer captures estimate-vs-actual, agent reasoning gets marked right/wrong
- Gate: is the agent's proposed estimate getting more accurate over sprints?

### Month 4 — Meeting-to-spec v1

- Post-hoc mode only (paste transcript, not live participation)
- Outputs structured PRD + candidate tickets + open questions, flows into spec-to-tickets
- ~3-4 days with OMC

### Month 5-6 — Spec + delivery tracker (the product-sized piece)

- 2-3 weeks of real work
- Traceability schema, drift detection, spec-to-merged-code linking
- This is where the suite stops being tools and becomes a product

### Month 7+ — Decide

Based on actual usage with your team (and any external interest from LinkedIn/course audience):

- **If the agent-as-participant feature genuinely changed retros and the spec tracker changed delivery quality** → productize seriously, consider SaaS.
- **If it was useful but not transformative** → ship as open-source, monetize via consulting and course cross-sell.
- **If it was gimmicky** → quietly retire the agent feature, keep the memory layer + rituals as solid .NET-native free tooling that drives course and audit sales.

---

## 8. Decisions made

These are decided, do not re-litigate without new evidence:

### 8.1 Scope
- **Not building a Jira replacement.** The suite reads from Jira/GitHub; it does not replace them. Evaluated and rejected — Jira's moat is 20+ years of integrations and Atlassian can't be out-built solo.
- **Not building roadmap tooling.** Too adjacent, different competitor set, different audience. Out of scope for v1 and v2.
- **Not auto-estimating.** Propose-and-argue, not auto. Preserves the social function of estimation.
- **Not shipping a CLAUDE.md generator as a suite component.** Side artifact. Belongs with Claude Code for .NET course, not in this product.
- **Adding PO-facing rituals (meeting-to-spec, spec-to-tickets).** Confirmed during the April 18 extensions pass. Fills the upstream gap the original doc had.
- **Spec + delivery tracker is the concretization of the memory layer.** Not a separate product. Same moat, named specifically.

### 8.2 Technical
- **Stack: .NET 9 + Vue 3 + TypeScript + Pinia + SQLite + SignalR** for v1. Matches Laurentiu's expertise, demos well, aligns with course content.
- **Memory layer as a separate service**, not embedded in each ritual app. This is the architectural decision that enables the suite to be a suite rather than a collection of toys.
- **Read-first, write-last for upstream integrations.** We read from Jira/GitHub aggressively. We write to them only for explicitly user-confirmed actions (like syncing agreed estimates or creating tickets from a spec interview).
- **Built with the same OMC + Codex cross-model review pilot** that started this whole thread. Dogfood the tooling the course teaches. Every commit goes through the pilot loop.

### 8.3 Positioning
- **.NET-first, not language-agnostic.** Narrow wedge, defensible market.
- **Agents as teammates, not features.** This is the philosophical differentiator.
- **Memory is the product, rituals are the UI.** Architectural and marketing truth.
- **Full-loop is the marketing story.** Not "a retro tool" or "a poker tool" — spec → tickets → estimates → execution → retro → agent-infrastructure-update → next spec. The loop is the thing.

### 8.4 Strategic
- **Paused until after Claude Code for .NET course ships.** Course business is the income engine through 2027. Do not bet the financial bridge on a speculative product.
  - *Note:* the Claude Code for .NET / Agentic Microservices course was moved to May 2026; SOLID moved to ~July 2026.
- **Retro v1 built as course material first, product second.** The weekend build is a course demo. If it becomes a product later, that's gravy.
- **Open-source by default.** Monetization comes indirectly through courses, the audit service, and potential hosted/enterprise tier. Avoids SaaS ops burden during bridge period.
- **OMC speed does not weaken the pause.** OMC makes the *build* faster. It does not make course production faster, does not reduce attention cost, does not make client support faster, does not make SaaS ops cheaper. Validation gates still hold.

### 8.5 What we explicitly rejected
- Consumer mobile app (shelf-scanner idea) — wrong audience, wrong stack, wrong distribution channel.
- Rebuilding planning poker as a standalone AI-suggestion tool — already done by SprintPoker, Agile Poker, PlanningPokerAI.
- Generic AI retro tool — every incumbent already ships this.
- Aggressive parallel build of multiple rituals — fails the "ship one, learn, then expand" principle.
- CLAUDE.md / skills generator as a suite product — confirmed out during the April 18 extensions pass.

---

## 9. Open questions to resolve before unpausing

1. **Does Laurentiu's team actually have 2-3 members doing meaningful agentic coding in production?** Not piloting, not experimenting — shipping agent-written code. The entire "agents as participants" angle depends on this being real. If it's aspirational, v2 feature falls flat.
2. **Does LinkedIn engagement on the retro v1 launch post signal product interest, or just course interest?** The distinction matters. Course interest → stay on course-material path. Product interest → accelerate productization.
3. **Will DailyBot ship "agent as retro participant" before we do?** If yes, the window on that differentiator closes. Watch their changelog.
4. **Will Rovo ship explicit CLAUDE.md / coding-assistant-specific integration?** If yes, the .NET wedge narrows further. Watch Atlassian announcements.
5. **Can the memory layer answer useful questions after 3 real sprints of data?** If not, the whole architecture thesis is wrong, pivot to a simpler single-ritual product.
6. **Which v1 anchor — retro or spec-to-tickets — surfaces demand faster?** Retro is the weekend build regardless. Spec-to-tickets might be the better product front door when we unpause. Decide based on what Laurentiu's own team pulls on hardest.

---

## 10. What would change the recommendation

**Signals that would accelerate productization (unpause early, build seriously):**
- DailyBot ships "agent-as-retro-participant" → move faster, narrow scope to the most defensible single ritual, probably retro-with-agent-infrastructure-updates.
- Rovo ships CLAUDE.md editing or coding-assistant-specific integration → the .NET-native angle becomes the only differentiator; go harder on it.
- Linear ships a retro product → the space is being validated by serious money; probably worth joining the race with a differentiated angle.
- Martin Fowler's "Feedback Flywheel" article gets meaningful traction on HN / dev Twitter → demand signal for the pattern.
- Laurentiu's retro v1 launch post gets >50 meaningful comments or >10 "can I use this" requests → real demand.
- Someone ships a proper Socratic spec-to-tickets tool → the upstream wedge closes; reconsider anchor.

**Signals that would kill productization (drop permanently, extract course value):**
- Atlassian acquires Parabol → Rovo + Parabol kills most of the open-source positioning.
- An open-source equivalent ships (e.g., `oh-my-retro`, or Thoughtworks open-sources a reference impl) → reconsider; may be better to contribute to than compete with.
- Laurentiu's team doesn't use their own retro after 3 sprints → demand signal weaker than assumed.
- Claude Code for .NET course has LinkedIn/Udemy engagement but zero product-interest signals → pure course-content path validated; don't productize.

**Numbers to track:**
- LinkedIn post engagement on retro v1 launch (signal for product demand).
- Any mention of "CLAUDE.md evolves from retros" on dev Twitter / HN over the next 60 days.
- Pricing and adoption of SprintPoker and Linear Agent — closest competitors.
- Whether any competitor ships a structured Socratic interview for spec-to-tickets (vs. the one-shot "generate a ticket from natural language" pattern everyone has today).

---

## 11. Revenue scenarios (if productized)

Rough ranges, not forecasts:

### Scenario A — Open-source + courses (most likely if we proceed)
- **OSS repo**, MIT licensed, attracts .NET audience through course cross-sell and LinkedIn
- Direct product revenue: ~$0
- Indirect revenue: 20-40% lift on course sales from "battle-tested reference implementation" positioning
- Audit service leads: 2-5 qualified inquiries/month at $5-10k/engagement → $10-50k/mo potential
- Time commitment: 5-10h/week maintenance post-launch
- **Verdict:** realistic, low-risk, reinforces existing business

### Scenario B — Open-core SaaS
- Free OSS tier + hosted paid tier ($10-30/user/month) with memory layer + spec+delivery tracker SaaS-hosted
- Requires: 500+ paying users to replace course income — multi-year build
- Time commitment: 20-30h/week, conflicts with course production
- **Verdict:** realistic only after financial bridge is passed (2027+)

### Scenario C — Enterprise focus
- Self-hostable version sold as enterprise license, 10-50k/year per team
- Requires: 10-30 enterprise customers to replace course income
- Sales cycle: 6-12 months per deal
- Time commitment: 30+h/week, including sales
- **Verdict:** not a fit for solo-founder bridge period; consider only with a partner or later

### Scenario D — Productized consulting
- Don't sell the tool. Sell engagements that use the tool.
- $15-30k per engagement (assessment + setup + training + 3-month support)
- 1-2 engagements/month = $15-60k/mo, much higher than Udemy margin
- **Verdict:** realistic, leverages existing expertise, doesn't require product-level investment

Most likely path forward: **Scenario A first, Scenario D opportunistically, Scenario B only if A generates real demand signals.**

---

## 12. Why we paused

Explicit reasons — re-read before unpausing:

1. **Financial bridge is the priority through 2027.** Courses generate ~$7k/month target by Feb 2027. Product builds that take from course production time threaten the bridge. Non-negotiable.
2. **Claude Code for .NET / Agentic Microservices course (May 2026) and SOLID course (~July 2026) must ship on schedule.** These are the next two anchors of the 7-course catalog. A speculative product does not delay them.
3. **The retro v1 itself has standalone value as course material** (Claude Code for .NET can use the build as a live case study). We capture that value regardless of productization.
4. **Timing window on the differentiators (agent-as-participant, retro-edits-CLAUDE.md) is 6-12 months** — tight but not immediate. We can afford to ship SOLID first and still be early enough.
5. **We have no evidence yet that Laurentiu's team actually uses agentic coding at the depth the v2 features require.** Without that, the agent-as-participant feature is theoretical. Piloting during April-May also validates (or invalidates) the core premise.

---

## 13. Revisit checklist (for future-Laurentiu)

When returning to this doc — probably June 2026 or later — work through these before resuming:

- [ ] Has the Claude Code for .NET / Agentic Microservices course shipped?
- [ ] Has SOLID shipped (or is it cleanly on track)?
- [ ] Did the retro v1 get used by your team for at least 3 real sprints?
- [ ] Did LinkedIn engagement on the retro launch suggest product interest (not just course interest)?
- [ ] Have DailyBot or Rovo shipped features that close the windows named in Section 10?
- [ ] Is your team actually using agentic coding in production at 2-3 people minimum?
- [ ] Has Fowler's Feedback Flywheel pattern been productized by anyone else?
- [ ] Is the financial bridge on track (hitting forecast by month)?
- [ ] Between retro and spec-to-tickets — which does your own team pull on hardest as a repeat-use tool?

If most checkboxes are green and windows are still open → unpause, build seriously, use Section 7 sequencing (and reconsider the v1 anchor).
If most are red or windows have closed → keep as open-source reference, extract course value, don't productize.

---

## 14. What's still not covered, worth naming explicitly

Even after the suite extensions, two ritual-shaped gaps remain. **Neither is urgent. Do not build them now.** They exist so future-Laurentiu doesn't feel the suite is incomplete when re-reading.

### 14.1 Sprint review / demo ritual

The "show stakeholders what shipped" moment. Currently ad-hoc across most teams: screenshots in Slack, occasional live demos, a Confluence page nobody reads. Zero tooling specifically for "show what shipped in a structured, stakeholder-friendly way."

**Agentic angle:** agent watches merged PRs, auto-generates a changelog with screenshots (Playwright-driven or similar), ships to Slack/email. Optional live-demo mode: the agent can narrate a screen recording through a shipped feature.

**Competitive density:** low to medium. Changelog tools exist (ChangeLogfy, FeatureOS) but aren't ritual-native. Rovo touches adjacent territory with release note generation, but not demo-quality.

**Why not now:** it's stakeholder-facing, not team-facing. Different buyer, different content, different distribution. Adds scope without deepening the core moat. Park it.

### 14.2 Onboarding ritual (human or agent)

When a new developer joins, or when a new agent is added to the team, they need context. Current state is "read Confluence, shadow a senior dev, hope for the best." For agents it's "write a CLAUDE.md and pray."

**Agentic angle:** structured onboarding ritual driven by the memory layer. New member (human or agent) gets a curated sequence — recent retros, CLAUDE.md, key skills, current sprint context, recent spec decisions. They contribute their first questions back into the memory layer.

**Why it fits the suite thesis:** specifically valuable if you're running mixed human+agent teams, which is exactly the target audience. Generic onboarding tools don't consider agents. Also generates a natural moment to refresh stale CLAUDE.md — every onboarding is a forcing function.

**Competitive density:** very low. Not a mainstream ritual yet.

**Why not now:** it's a 2027+ ritual. The pattern "add a new agent to the team" is real but not yet mainstream enough to sell against. Revisit when the market is ready.

### 14.3 Deliberately out-of-scope (for the record)

To avoid re-discussing in future sessions, these were considered and are permanently out of scope for this suite:

- **Backlog grooming / refinement as a separate tool.** Partially covered by spec-to-tickets. A dedicated grooming tool would duplicate the Socratic interview pattern without adding a distinct ritual.
- **Team mood / sentiment analysis.** Echometer and DailyBot own this. Not a differentiator for .NET audience.
- **Skills gap / career development tracking.** Adjacent HR territory. Wrong buyer.
- **External stakeholder communication tools** beyond sprint review. Not engineering-team-native.
- **Anything consumer-facing.** See shelf-scanner rejection.

---

## 15. Links and references

- `agentic-rituals-landscape.md` — full competitor landscape research, April 17 2026
- `suite-extensions.md` — the extensions pass that surfaced meeting-to-spec, spec-to-tickets, spec+delivery tracker, April 18 2026
- `course-business-plan.md` — master course roadmap and financial forecast
- `oh-my-claudecode-reference.md` — OMC tooling reference (pilot infrastructure)
- Martin Fowler, "Feedback Flywheel," martinfowler.com/articles/reduce-friction-ai/feedback-flywheel.html — the intellectual validation of the retro-as-agent-infrastructure-update pattern
- Atlassian Rovo — atlassian.com/software/rovo
- Linear Agent announcement — linear.app/changelog/2026-03-24-introducing-linear-agent
- DailyBot 3 ("one place for team updates, AI reports, and agent activity") — dailybot.com

---

## 16. Closing thought

This suite is a real product idea that can wait. The key discipline: don't fall in love with it before there's evidence. The course business is the flywheel. If this suite becomes a real product, it will do so *because* of the course audience and *for* them — not instead of them.

The retro v1 ships as a tool for your team, a demo for your course, and a proof-of-concept for yourself. The extensions (meeting-to-spec, spec-to-tickets, spec+delivery tracker) wait for signals. The gaps in §14 wait for the market. Everything else is optional.
