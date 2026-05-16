# Agenting Setup Reference

A multi-agent coding pipeline built on Claude Code's native features (agents, rules, skills, hooks). No external orchestration — everything runs inside a single Claude Code session using subagent spawning and file-based handoffs.

---

## Directory Structure

```
.claude/
  agents/           # Agent definitions (frontmatter + instructions)
  rules/            # Auto-loaded rules (every session, every agent)
  skills/           # Reusable workflow skills (invoked via Skill tool)
  settings.local.json  # Permissions, hooks, env vars
  hooks/            # Hook scripts (audit logging, context injection)

docs/
  product/v1.md           # Product specification (source of truth)
  architecture/v1.md      # Technical architecture
  conventions/            # Stack-specific coding conventions
  backlog.md              # Feature sequence, dependencies, status
  kb/                     # Knowledge base (domain rules, formulas)
  specs/{slug}/
    definition/           # spec.md, help.tooltips.md, help.page.md
    delivery/             # plan.md, implementation.md, review.md,
                          # lessons.md, summary.md, communication-log.md
```

---

## Agents

Nine agents, each a `.claude/agents/{name}.md` file with YAML frontmatter (name, description, model) and markdown instructions. Activated by "be {agent}" or spawned as subagents.

### Roles and Models

| Agent | Model | Role | Writes |
|-------|-------|------|--------|
| **team-lead** | opus | Orchestrator, message hub, pipeline controller | communication-log.md, summary.md |
| **po** | opus | Shapes features from idea to spec | spec.md, help files |
| **architect** | opus | Plans, done checks, answers questions, escalation | plan.md, review.md (Step 1), lessons.md |
| **developer** | opus (plan mode) | Implements plans step-by-step | source code, implementation.md, lessons.md |
| **reviewer** | sonnet | Code review (plan conformance + quality) | review.md (Step 2), lessons.md |
| **critic** | opus | Cross-references specs and plans against source docs | structured findings (to invoker) |
| **solo** | opus (plan mode) | Small fixes, 1-3 files, discuss-then-implement | source code, implementation.md |
| **learner** | opus | Consolidates lessons into system files | updates to CLAUDE.md, agents, rules, skills |
| **codebase-scanner** | sonnet | Scans codebase, builds architecture reference | architecture-reference.md |

### Key Design Decisions

- **Model routing matters.** Opus for reasoning-heavy work (architect, critic, developer). Sonnet for pattern-matching work (reviewer, scanner). General-purpose/Explore subagents always get `model: "sonnet"`.
- **Agents never message each other directly.** All communication routes through the team lead (hub-and-spoke). This lets the team lead intercept scope changes and decisions that need user approval.
- **Each agent has a "What You Never Do" section.** Hard boundaries prevent role drift (architect doesn't code, reviewer doesn't implement fixes, PO doesn't read source code).
- **`@` directives** in agent files auto-load key docs into context (architecture, conventions, product spec).

---

## Pipeline

The full feature pipeline runs sequentially through file-based handoffs:

```
User idea
  -> PO (shape feature -> write spec.md)
     -> Critic (optional: cross-reference spec vs product spec)
  -> Architect (write plan.md)
     -> Critic (optional: cross-reference plan vs spec)
  -> Developer (implement -> write implementation.md)
  -> Architect (Step 1: done check against plan)
  -> Reviewer (Step 2: code review)
     -> Developer <-> Reviewer (fix cycles, max 3)
  -> Team Lead (write summary.md, close pipeline)
```

### Invocation

The team lead is the entry point. The user says what to build, the team lead:

1. Checks if a spec exists with `Status: Ready`
2. Checks if a plan already exists
3. Applies defaults (standard mode, background spawn, current branch)
4. Spawns agents sequentially as each phase completes

### Spawn Modes

**Background (default):** Agents spawned with `run_in_background: true`. Resumed via `SendMessage` between phases — never fresh-spawned unless resume fails. Agents retain full context across phases.

**Persistent:** Uses `TeamCreate` to spawn all agents upfront. All communication via `SendMessage`. Doesn't survive `/resume` or session restarts.

### Team Modes

| Mode | Agents | When |
|------|--------|------|
| Standard | architect + developer + reviewer | Default for most work |
| Standard + Codex | Same + Codex cross-validation in review | Security-sensitive, complex |
| Fast | architect + developer (developer self-reviews) | Simple UI, clear data, <=5 files |

### Auto-Approval

Plans with <=11 steps and no open questions are auto-approved. Larger plans go to the user.

### Cycle Caps

| Loop | Max | Escalation |
|------|-----|------------|
| Reviewer <-> Developer | 3 | Architect |
| Developer questions (same area) | 3 | User |
| Architect escalation | 1 | User |

---

## How Work Actually Flows

Work doesn't always go through the full team pipeline. There are three distinct modes of working, each a separate Claude Code session. The user picks the right mode for the job and agents produce artifacts that later modes can pick up.

### Mode 1: Direct Agent Sessions (User + One Agent)

Most work starts here. The user activates an agent with `be {agent}` and works with it directly — no team lead, no pipeline, no spawning. These are normal conversations where the agent follows its instructions.

```
┌──────┐     ┌───────┐
│ USER │◄───►│ AGENT │    (one session, one agent, direct conversation)
└──────┘     └───────┘
```

**Common direct sessions:**

| Session | What Happens | Artifact Produced |
|---------|-------------|-------------------|
| `be po` | User shapes a feature idea through discussion. PO researches, challenges assumptions, writes spec when ready. | `docs/specs/{slug}/definition/spec.md` (Status: Ready) |
| `be architect` | User discusses a refactoring, architecture change, or complex bug. Architect analyzes the codebase, asks questions, writes a plan. | `docs/specs/{slug}/delivery/plan.md` |
| `be solo` | User describes a small fix or scoped change. Solo explores, proposes approach, implements after user confirms. | Source code changes + `implementation.md` |
| `be learner` | User triggers lesson consolidation. Learner reads all lessons.md files, classifies, promotes proven patterns. | Updates to CLAUDE.md, agent files, rules, skills |

**Key point:** These sessions produce artifacts (specs, plans) that sit in `docs/specs/` waiting to be picked up. The user might shape a spec with the PO today, discuss the plan with the architect tomorrow, and launch the team pipeline next week. The artifacts connect the sessions.

### Mode 2: Team Pipeline (User + Team Lead + Spawned Agents)

When there's a spec and/or plan ready and the user wants the full implement-review cycle, they activate the team lead. The team lead spawns agents as subprocesses and orchestrates between them.

```
┌──────┐
│ USER │
└──┬───┘
   │
┌──▼───────┐
│ TEAM LEAD│◄──── all agent messages route here
└──┬──┬──┬─┘
   │  │  │
   ▼  ▼  ▼
 ARCH DEV REVIEWER    (spawned as background subagents)
   ▲
   │
 CRITIC               (spawned by architect when needed)
```

The team lead is a **message hub with triage authority.** It never does substantive work — it dispatches, guards against scope changes, and keeps a communication log.

**What triggers this mode:**
- "implement F26" → team lead finds the spec, launches architect → developer → reviewer
- "implement adhoc-SyncRefactoring" → team lead finds the existing plan, starts at developer (skips architect planning)
- User describes ad-hoc work without a plan → team lead spawns architect to plan first, then continues

### Mode 3: Solo (User + Solo Agent)

A lightweight alternative to Mode 2 for small, scoped changes (1-3 files). The solo agent discusses the approach, implements after confirmation, and optionally runs `/review`. No team lead, no spawned agents, no pipeline overhead.

### How the Modes Connect

The artifact system connects sessions across time:

```
Session 1: be po
  User shapes feature idea
  PO writes spec.md (Status: Ready)
  Session ends.
                                          ← days/weeks can pass
Session 2: be architect
  User discusses implementation approach
  Architect reads spec.md, writes plan.md
  Session ends.
                                          ← hours/days can pass
Session 3: be team-lead
  User says "implement F26"
  Team lead finds spec (Ready) + plan (exists)
  Starts pipeline at developer (plan already written)
  Developer → Architect (done check) → Reviewer
  Pipeline completes.
                                          ← after several features
Session 4: be learner
  User triggers lesson consolidation
  Learner reads all lessons.md files across completed features
  Promotes proven patterns into system files
```

Each session is independent. No shared memory or state between sessions — only the artifacts in `docs/specs/` and `docs/` connect them. This means you can close Claude Code, come back later, and pick up exactly where the artifacts left off.

### Team Lead Orchestration Detail

When the team lead is active (Mode 2), here's the actual decision logic it follows:

#### Pre-Flight (before any launch)

Run sequentially, apply defaults silently — inform the user what was chosen, don't ask:

1. **Dirty tree** — `git status`. Uncommitted changes? Inform and continue.
2. **Branch** — Stay on current. Inform: "Working on `{branch}`."
3. **Jira** — Fetch if user mentioned a key. Otherwise skip.
4. **Team mode** — Standard by default. Classify if ad-hoc.
5. **Spawn mode** — Background.

#### Launch Path Selection

```
User request
  │
  ├── Backlog feature ("implement F26")
  │     ├── spec exists with Status: Ready?
  │     │     ├── Yes + plan exists → start at Developer
  │     │     ├── Yes + no plan → start at Architect
  │     │     └── No → STOP: "No spec. Run `be po` first."
  │     └── (spec gate mandatory for backlog features)
  │
  ├── Ad-hoc with existing plan
  │     └── Start at Developer (architect available for questions/review)
  │
  └── Ad-hoc without plan
        └── Classify complexity → pick team mode → start at Architect
```

#### Message Triage

Every agent message comes to the team lead first. Before forwarding:

| Condition | Action |
|-----------|--------|
| Routine handoff ("ready for review") | Forward immediately |
| Contradicts user requirements | **STOP.** Ask the user |
| Needs product/spec context | Route to PO first (PO answers from spec). User only if PO can't |
| Scope change or step removal | **STOP.** Confirm with user |
| Non-blocking findings (MEDIUM/LOW) | Route to developer automatically |

**Rule:** Never forward a message that would silently reverse a user decision.

#### Question Routing Chain

```
Agent question → Team Lead → PO (answers from spec) → User (only if PO can't)
```

Most questions get answered without bothering the user.

#### Pipeline Sequence (Standard Mode)

1. **Architect** — reads spec, asks questions (routed through team lead to PO if needed), writes plan, critic reviews it
2. **Plan approval** — auto-approve if <=11 steps and no open questions; otherwise user approves
3. **Developer** — implements step by step, writes implementation.md
4. **Architect** (Step 1 done check) — compares implementation.md against plan. Pass → proceed. Fail → developer fixes.
5. **Reviewer** (Step 2 code review) — plan conformance + code quality. APPROVE / REQUEST CHANGES (max 3 fix cycles) / COMMENT
6. **Post-approval** — optional tester, teacher, builder phases
7. **Shutdown** — team lead writes summary.md, updates backlog, closes communication log

Agents are spawned once in background and **resumed via SendMessage** between phases — not re-spawned. This preserves their context.

#### Escalation

3 reviewer-developer fix cycles exhausted → architect recommends resolution → user chooses: continue / force-accept / abort.

#### Resume

If a pipeline was interrupted (session crash, user left), the team lead reads `communication-log.md` to find the last step and cycle, then resumes from there.

#### Unattended Mode

Adding `[UNATTENDED]` to the prompt enables fully autonomous operation (`claude -p`). All decisions auto-resolved: plans auto-approved, PO answers from spec, fix cycles force-accepted after 3, phase failures retry once then skip.

#### Commit Protocol

Auto-commits at checkpoints:

| When | Message |
|------|---------|
| Plan approved | `feat({slug}): add implementation plan` |
| Implementation done | `feat({slug}): implement {description}` |
| Review approved | `feat({slug}): finalize after review` |
| Pipeline complete | `feat({slug}): complete pipeline` |

#### Communication Log

The team lead maintains `communication-log.md` in real-time, logging every inter-agent message with a problem column. This is the audit trail and the resume mechanism.

---

## Graphify — Codebase Knowledge Graph

[Graphify](https://github.com/nicobailey/graphify) builds a structural knowledge graph from the codebase — an AST-based graph of types, functions, modules, and their relationships. It runs locally (no API calls for updates) and produces navigable output that agents use to understand the codebase before reading raw source files.

### What It Produces

```
graphify-out/
  GRAPH_REPORT.md    # God nodes, community clusters, corpus stats
  wiki/
    index.md         # Navigable wiki of the graph
    ...              # Community pages, entity pages
```

Key outputs in `GRAPH_REPORT.md`:
- **God nodes** — the most-connected types in the codebase (the types everything depends on)
- **Community clusters** — groups of tightly-coupled types (natural module boundaries)
- **Corpus stats** — file count, word count, edge count

### How Agents Use It

| Agent | Uses Graphify For |
|-------|-------------------|
| **PO** | Understanding codebase structure during feature shaping (reads GRAPH_REPORT.md, never source code) |
| **Critic** | Checking implementation feasibility — finding existing patterns that might conflict with a spec |
| **Architect** | Understanding dependency graphs before planning type moves or extractions |
| **All agents** | `kb-navigation.md` rule directs agents to check GRAPH_REPORT.md for structural questions before grepping |

### Rules in CLAUDE.md

```markdown
## graphify

Rules:
- Before answering architecture or codebase questions, read
  graphify-out/GRAPH_REPORT.md for god nodes and community structure
- If graphify-out/wiki/index.md exists, navigate it instead of
  reading raw files
- After modifying code files, run `graphify update .` to keep the
  graph current (AST-only, no API cost)
```

### Keeping It Current

After code changes, run `graphify update .` — this re-parses the AST locally (no API calls, no cost). The graph stays in sync with the codebase without manual effort.

### Why It Matters

Without graphify, an agent answering "what does Ticket touch?" would grep the entire codebase and read dozens of files. With graphify, it reads GRAPH_REPORT.md (~100 lines), finds that `Ticket` is a god node in Community 3 connected to 15 types, and knows exactly which files to read. This saves significant context window and time.

---

## Rules (`.claude/rules/`)

Rules auto-load every session for every agent. They enforce cross-cutting behavior without repeating instructions in each agent file.

| Rule | Purpose |
|------|---------|
| `agents-workflow.md` | The coordination protocol: pipeline stages, message formats, file formats, review checklist, severity ratings |
| `agent-invocation.md` | "be {agent}" activates the agent file |
| `agent-model-routing.md` | Forces `model: "sonnet"` on general-purpose/Explore subagents |
| `pipeline-guardrails.md` | No god folders, no Opus for research agents, project agents own the pipeline |
| `guardrail-enforcement.md` | Conflicts with guardrails must be flagged, never silently violated |
| `spec-authority.md` | Spec files describe current state; user decides changes |
| `research-before-asking.md` | Agents offer to research before asking clarifying questions |
| `help-tooltips.md` | Wire tooltip content from help.tooltips.md to UI elements |
| `kb-navigation.md` | Check knowledge base before reading source files |
| `kb-maintenance.md` | Update KB entries after implementing features that modify tracked concepts |

---

## Skills (`.claude/skills/`)

Reusable workflow recipes. Each skill is a `SKILL.md` file in its own folder. Agents check the skills directory before implementing and follow the matching skill if one exists. Plans map each step to a skill or justify inline detail.

### Skill Categories

**Scaffolding** — create-aggregate, create-feature, create-service, create-module, create-vue-feature, create-building-blocks-package, create-grpc-contract, create-domain-event-handler, add-integration-event, add-pipeline-behavior

**Extraction/Refactoring** — extract-endpoint-types, extract-feature-service

**Architecture** — create-architecture-doc, create-implementation-plan, create-feature-spec, create-service-claude-md, create-module-claude-md, system-design

**Pattern References** — domain-patterns, cqrs-patterns, persistence-patterns, redis-patterns, authorization-patterns, error-handling, vue-patterns, vue-component-architecture, pinia-patterns, tailwind-theme

**Reviews** — frontend-review

**Meta (self-improvement)** — improve-flow, improve-skills

### How Skills Work

1. Architect maps plan steps to skills in a **Skill Mapping** table
2. Developer checks `.claude/skills/` before each step
3. If a skill exists, the developer follows it (provides only feature-specific inputs)
4. If a skill exists but references missing infrastructure, the developer builds it first
5. If no skill exists, the developer uses inline detail from the plan and logs the gap in lessons.md
6. The learner later promotes recurring gaps into new skills

---

## File-Based Handoffs

All coordination happens through files in `docs/specs/{slug}/delivery/`. No state is held in memory between agents.

| File | Written By | Purpose |
|------|-----------|---------|
| `spec.md` | PO | Product definition — what to build |
| `plan.md` | Architect | Implementation plan — how to build it |
| `implementation.md` | Developer | What was built, files created/modified, deviations |
| `review.md` | Architect (Step 1), Reviewer (Step 2) | Done check findings, code review findings |
| `questions.md` | Developer | Blockers mid-implementation, answered by architect |
| `lessons.md` | All agents | Hard-won knowledge from each phase |
| `summary.md` | Team Lead | Pipeline completion marker |
| `communication-log.md` | Team Lead | All inter-agent messages, problems tracking |

### Slug Convention

Each unit of work gets a slug that determines its folder path:

| Type | Pattern | Example |
|------|---------|---------|
| Internal feature | `F{N}-{Name}` | `F5-SprintSummaryCard` |
| Jira epic | `{KEY}-{summary}` | `PD-5234-shelf-compliance` |
| Jira issue | `{KEY}-{summary}` | `PD-5226-split-config` |
| Ad-hoc | `adhoc-{Name}` | `adhoc-SyncRefactoring` |
| Bug | `BUG-{N}-{name}` | `BUG-1-bug-count-zero` |
| Gap | `GAP-{N}-{name}` | `GAP-3-sub-team-mgmt-ui` |

---

## Hooks and Settings

### settings.local.json

```jsonc
{
  "env": { ... },            // Environment variables (e.g., DISABLE_OMC)
  "permissions": {
    "allow": [ ... ],        // Pre-approved tools/commands
    "deny": [ ... ]          // Blocked operations (rm -rf, push, etc.)
  },
  "hooks": {
    "SessionStart": [ ... ], // Run on session start — agent activation
    "UserPromptSubmit": [ ... ], // Run on each user message
    "PreToolUse": [ ... ]    // Run before each tool call — audit logging
  }
}
```

### Agent Activation Hook

A `SessionStart` and `UserPromptSubmit` hook reads the `$CC_AGENT` environment variable and echoes "be {agent}" — this auto-activates the correct agent file when Claude Code is launched with a specific agent context.

### Audit Logger

A `PreToolUse` hook runs `.claude/hooks/audit-logger.js` asynchronously on every tool call. Captures tool usage for observability without blocking the pipeline.

---

## Knowledge Base (`docs/kb/`)

Domain rules, computation formulas, and data flows that are expensive to re-derive from source code. Agents check `docs/kb/index.md` (the topic map) before reading source files. A KB entry gives business logic in ~60 lines vs reading 3-5 source files (~300+ lines).

- Developer updates KB entries during implementation
- Reviewer verifies KB entries match implementation (HIGH severity if stale)

---

## Learning Loop

The system improves itself across feature pipelines:

1. **Every agent writes lessons** to `docs/specs/{slug}/delivery/lessons.md` during their phase (mandatory, not optional)
2. Lessons capture: ambiguities, missing skills, patterns discovered, calibration insights
3. The **learner agent** (`be learner`) processes all lessons files:
   - Classifies each item (CLAUDE.md, agent files, rules, skills)
   - Tracks recurrence across features (threshold: 2 occurrences to promote)
   - Promotes proven patterns via `improve-flow` and `improve-skills` skills
   - Tags items as `[APPLIED]` or `[TRACKED]` in source files
4. Critical items (security, data loss, build-breaking) promote immediately

This closes the feedback loop: what agents learn during one feature improves how they work on the next.

---

## Adapting for a New Project

### Minimum Viable Setup

1. **CLAUDE.md** — project-level architecture, tech stack, guardrails, repo structure
2. **`.claude/agents/`** — at minimum: team-lead.md, architect.md, developer.md, reviewer.md
3. **`.claude/rules/agents-workflow.md`** — the coordination protocol (pipeline, formats, review checklist)
4. **`.claude/rules/agent-invocation.md`** — the "be {agent}" activation rule
5. **`docs/backlog.md`** — feature sequence

### Scaling Up

| When you need... | Add... |
|-----------------|--------|
| Product definition phase | po.md agent + create-feature-spec skill |
| Quality gate on specs/plans | critic.md agent |
| Quick fixes without full pipeline | solo.md agent |
| Cross-pipeline learning | learner.md agent + improve-flow/improve-skills skills |
| Domain knowledge cache | docs/kb/ with topic map |
| Pattern consistency | .claude/skills/ with scaffolding skills |
| Guardrail enforcement | .claude/rules/ with enforcement rules |
| Audit trail | Hooks in settings.local.json |

### What to Customize Per Project

- **Agent `@` directives** — point to your project's architecture doc, conventions, product spec
- **Stack-specific conventions** — replace csharp.md/vue.md/ef-core.md with your stack
- **Skills** — replace scaffolding skills with your project's patterns (e.g., create-aggregate becomes create-model for Django)
- **CLAUDE.md guardrails** — your project's "DO NOT" list
- **Model routing** — adjust which agents get opus vs sonnet based on your quality/cost tradeoff
- **Review checklist** — customize Stage 2 checks in agents-workflow.md for your stack
- **Hooks** — customize for your CI/audit needs

### What Stays the Same Across Projects

- Hub-and-spoke communication (all messages through team lead)
- File-based handoffs (spec -> plan -> implementation -> review -> summary)
- Severity ratings (CRITICAL/HIGH/MEDIUM/LOW)
- Cycle caps (3 fix cycles, then escalation)
- Lessons system (every agent writes, learner consolidates)
- Slug convention for organizing work
- The pipeline sequence itself (PO -> Architect -> Developer -> Reviewer)

---

## Quick Reference: Commands

| Command | What Happens |
|---------|-------------|
| `be team-lead` | Activate orchestrator, ask what to build |
| `be po` | Shape a feature from idea to spec |
| `be architect` | Discuss architecture, write plans |
| `be solo` | Quick fix, discuss-then-implement |
| `be learner` | Process lessons from completed features |
| "implement {Feature}" (to team lead) | Launch the full pipeline |
| "what's next?" (to team lead) | Status check across all features |
