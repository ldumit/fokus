# Agentic System Overview

Eight AI agents collaborate on software development through a file-based pipeline with a three-stage core (architect → developer → reviewer). The **team lead** orchestrates all communication (hub-and-spoke); agents never message each other directly.

## Pipeline Flow

```
User -> PO (spec) -> Architect (plan) -> Developer (implement)
     -> Architect (done check) -> Reviewer (code review) -> Done
```

Fix cycles (developer <-> reviewer) are capped at 3, then escalate to architect. The `solo` agent handles small changes (1-3 files) outside the pipeline.

## Agents

| Agent | Model | Role |
|-------|-------|------|
| team-lead | opus | Orchestrates pipeline, routes all messages, manages commits |
| architect | opus | Writes plans, done checks, answers developer questions |
| po | opus | Shapes features, writes specs, answers product questions |
| critic | opus | Reviews specs (vs product doc) and plans (vs spec) |
| developer | opusplan | Implements plan steps, writes implementation.md |
| reviewer | sonnet | Code review with severity ratings |
| learner | opus | Consolidates lessons, promotes patterns to system files |
| solo | opusplan | Small scoped changes without the full pipeline |

## Information Layers

| Layer | Location | Loading | Purpose |
|-------|----------|---------|---------|
| Rules | `.claude/rules/` | Auto-loaded every session | Universal behavioral constraints |
| Conventions | `.claude/conventions/` | Loaded via `@` by pipeline agents | Pipeline artifact formats, coordination protocol |
| Agent files | `.claude/agents/` | Loaded on `be {agent}` | Agent-specific instructions and boundaries |
| Skills | `.claude/skills/` (38 skills) | Invoked on demand during implementation | Reusable code patterns and workflows |
| Stack conventions | `docs/conventions/` | Loaded via `@` by agents | Coding standards, project rules |

## Where Do I Put This?

- **Universal rule** (all agents, all sessions) -> `.claude/rules/{name}.md`
- **Pipeline coordination** (handoffs, checkpoints, message routing) -> `.claude/conventions/pipeline-protocol.md`
- **Artifact format** (how to write implementation.md, review.md, etc.) -> `.claude/conventions/{name}-format.md`
- **Agent-specific behavior** (what one agent does or doesn't do) -> `.claude/agents/{agent}.md`
- **Reusable code pattern** (scaffolding, architecture pattern) -> `.claude/skills/{name}/SKILL.md`
- **Coding standard** (naming, project structure, tech stack) -> `docs/conventions/`
- **Domain knowledge** (business rules, formulas, edge cases) -> `docs/kb/` (see `docs/kb/index.md` for topic map)

## Key Files

- `CLAUDE.md` — project root config, always loaded. Architecture, tech stack, guardrails.
- `.claude/rules/agents-workflow.md` — slug conventions, agent table, cycle caps, artifact format index.
- `.claude/conventions/pipeline-protocol.md` — full pipeline flow diagram, checkpoint format, message handoffs.
- `.claude/conventions/team-lead-operations.md` — spawn modes, failure handling, commit protocol, communication log format, unattended mode. Read on-demand by team lead during pipeline runs.
- `docs/conventions/project-rules.md` — source file definitions, build verification, coding conventions.
- `docs/kb/index.md` — domain knowledge topic map. Read before diving into source files.

## OMC Boundary

This project uses oh-my-claudecode (OMC) for infrastructure (tools, model routing, hooks). The project's own pipeline in `.claude/agents/` owns feature development. Do **not** use OMC orchestration skills (team, autopilot, ralph) for pipeline work. OMC specialist agents (debugger, security-reviewer, tracer) are available for standalone tasks.

## Getting Started

- **Implement a feature:** `be team-lead` and describe the feature.
- **Small fix (1-3 files):** `be solo` and describe the change.
- **Write a spec:** `be po` with the feature idea.
- **Review the setup:** Read this file, then `agents-workflow.md`, then individual agent files as needed.

## Design Decisions

Intentional choices that evaluations frequently flag. These are tradeoffs, not oversights.

| Decision | Rationale |
|----------|-----------|
| Agent boundary rules repeated per agent | Agents are self-contained — each defines its own "never do" without cross-referencing others. Boundary violations are the highest-impact failure mode; duplication is cheaper than a missed guardrail. |
| Team lead as sole message hub | Auditability + user-decision interception. Scope changes can't bypass the user. Single point of failure is the accepted tradeoff. |
| Serial pipeline, no parallelism | Features are 3-10 steps. Parallel coordination overhead exceeds time saved at this scale. |
| Rules auto-load for all sessions | Missing a guardrail mid-implementation costs more than ~150 lines of context. Solo sessions pay a small tax. |
| No "guided" mode between solo and Fast | Solo + manual `/review` covers the gap. Adding a mode adds maintenance without solving a real problem. |
| Repo structure in 3 files | CLAUDE.md (quick ref), project-rules.md (full rules), v1.md (architectural context). Each audience needs it inline. |
| No skill index file | Architect scans skill frontmatter during planning — that IS the index. A separate file duplicates and drifts. |
| Communication log detail | Enables resume after context loss. Fields look heavy but the team lead needs them to reconstruct pipeline state in a fresh spawn. |
