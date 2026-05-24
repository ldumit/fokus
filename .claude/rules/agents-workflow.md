# Agent Coordination Protocol

Three-agent pipeline for feature development. Agents coordinate through file-based handoffs and **hub-and-spoke messaging** — all messages route through the team lead.

**Deploy to:** `.claude/rules/agents-workflow.md` (auto-loads every session)

## Slug and Path Resolution

The `{slug}` identifies a unit of work and follows these conventions:
- Internal feature: `F{N}-{Name}` — e.g., `F5-SprintSummaryCard`
- Jira epic: `{KEY}-{2-3-words}` — e.g., `PD-5234-shelf-compliance-kpis`
- Jira issue: `{KEY}-{2-3-words}` — e.g., `PD-5226-split-config`
- Ad-hoc: `adhoc-{Name}` — e.g., `adhoc-SyncRefactoring`
- Bug: `BUG-{N}-{name}` — e.g., `BUG-1-bug-count-zero`
- Gap: `GAP-{N}-{name}` — e.g., `GAP-3-sub-team-management-ui`

The team lead or PO assigns the slug at the start of each pipeline run and passes it to all downstream agents. Agents never derive the slug — they use exactly what was passed.

Standard paths:
```
docs/backlog.md                              ← feature sequence, dependencies, status
docs/specs/{slug}/
  definition/   ← spec.md (or epic.md / bug.md), help.tooltips.md, images
  delivery/     ← plan.md, implementation.md, review.md, lessons.md, summary.md, communication-log.md
```

For a Jira issue nested under an epic:
```
docs/specs/{epic-slug}/{issue-slug}/definition/
docs/specs/{epic-slug}/{issue-slug}/delivery/
```
Ad-hoc work has no `definition/` folder — only `delivery/`.

## Communication Model: Hub-and-Spoke

All agent messages go through the team lead. Agents never message each other directly.

**Why:** The team lead triages all communication, intercepts decisions that need user approval, and prevents agents from silently overriding user requirements.

**How agents send messages:** Address the team lead, specifying the intended recipient and content:
```
To team-lead: "For {recipient}: {message content}"
```

**How the team lead dispatches:** Read the message, decide:
1. **Routine handoff** (e.g., "ready for review") → forward to recipient.
2. **Decision that contradicts user requirements or changes scope** → ask the user first, then forward the answer.
3. **Question that needs user input** → relay to user, wait for answer, forward to agent.

## All Agents

- **Rules go in files, not memory.** When a reusable rule or convention is identified, capture it in the appropriate rule or agent file — not in memory. Memory is for context that doesn't fit in rule files (user preferences, project state, external references). This applies to every agent, not just the team lead.

## Message Size Contract

Keep agent outputs focused. Content goes in files; messages are notifications with summaries.

| Output type | Max length | Rule |
|-------------|-----------|------|
| Analysis outputs (Phase 1) | ~500 words | Write detailed findings to questions.md or a notes file; message is the summary |
| Handoff messages | ~300 words | One paragraph of what was done + one paragraph of what's next |
| Checkpoint reports | Structured format (see below) | Use the Checkpoint Report Format |

**Write first, message second** — this applies to ALL artifacts, not just questions. Implementation details go in implementation.md, review findings go in review.md, questions go in questions.md. Messages are notifications.

## Team Lead Rules

- Do not read files before delegating to agents. Send the file path in the message and let the agent read it.
- Only read a file yourself when you need its content to make a routing or coordination decision — not to relay it.
- Relay user questions about plan content to the architect — do not investigate or answer them yourself.
- **Triage all inter-agent messages.** Before forwarding, check: does this message reverse a user decision, change scope, or remove a plan step? If yes, escalate to the user first.

## Agents

| Agent | Model | Scope | Managed by |
|-------|-------|-------|------------|
| architect | opus | Plans, Step 1 review (done check), question answers, escalation decisions | team lead |
| developer | opusplan | Implementation, implementation.md, questions.md | team lead |
| reviewer | sonnet | Step 2 review (code review), severity-rated conformance checks | team lead |
| critic | opus | Cross-reference review of feature specs (vs v1.md) and plans (vs feature spec) | PO (spec reviews), architect (plan reviews) |

## Pipeline

```
Human → PO (shape feature → write spec)
                    ↓
         PO offers: cross-check or critic?
          ↓ self                ↓ critic
     PO cross-checks      PO spawns critic (Mode 1: spec vs v1.md)
          ↓                     ↓ findings
     Status: Ready         PO fixes gaps → Status: Ready
                    ↓
Human → architect (analyze — Phase 1)
                    ↓
              questions checkpoint (team lead triages)
                    ↓
         architect (write plan — Phase 2)
                    ↓
         architect offers: self-review or critic?
          ↓ self                ↓ critic
     architect reviews     architect spawns critic (Mode 2: plan vs spec)
          ↓                     ↓ findings
     plan approved         architect fixes gaps → plan approved
                    ↓
         auto-approve always (split if >10 steps and each sub-plan >= 2 steps)
                    ↓
              developer (analyze plan — Phase 1)
                    ↓
              questions checkpoint (team lead triages)
                    ↓
              developer (implement — Phase 2 → implementation.md)
                    ↓
              architect (Step 1: done check + write lessons)
               ↓ fail          ↓ pass
        developer (fix)    reviewer (Step 2: code review)
                           ↓ approve       ↓ request changes
                    team lead (close)    developer ↔ reviewer
                                       (max 3 fix cycles)
                                            ↓ exhausted
                                      architect (escalation)
```

## Checkpoint Report Format

Use this format at pipeline checkpoints: architect Phase 1 output, developer Phase 1 output, done check verdict, reviewer verdict.

```
{Phase Name} — {Slug}
================================================
{2-4 headline metrics — e.g., "10 steps analyzed, 0 questions, 3 patterns verified"}
{Table or list of key findings}
Needs your attention:
  1. {flagged item — or "None"}
Action options:
  1. {default action} (recommended)
  2. {alternative}
  3. Stop
```

Team lead rule: if agent output at a checkpoint does not include action options, append them before relaying to the user.

## Message Handoffs (all via team lead)

Each handoff: trigger → sender → team lead action → receiver.

### Developer → Team Lead: Analysis complete (Phase 1)
**Trigger:** Developer finishes reading plan and exploring codebase.
**Developer says:** "For architect: Questions before implementing {FeatureName}: {list}" OR "All clear — plan is unambiguous, patterns found, ready to implement."
**Team lead:** If questions → forward to architect. If architect's answer would reverse a user decision → ask user first. If "all clear" → resume developer with "Implement."

### Developer → Team Lead → Architect: Ready for review
**Trigger:** Any implementation round complete — implementation.md written/updated.
**Developer says:** "For architect: implementation.md written for {FeatureName}, ready for Step 1."
**Team lead:** Forward to architect.
**Architect:** Reads implementation.md against plan. Pass → messages team lead for reviewer. Fail → writes review.md, messages team lead for developer.

### Architect → Team Lead → Reviewer: Step 1 passed
**Trigger:** Done check passes.
**Architect:** Writes lessons to `docs/specs/{slug}/delivery/lessons.md` (has full context now — plan vs implementation fresh in mind), then messages team lead.
**Architect says:** "For reviewer: Step 1 passed for {FeatureName}. Plan: docs/specs/{slug}/delivery/plan.md"
**Team lead:** Forward to reviewer.

### Reviewer → Team Lead → Developer: Fixes needed
**Trigger:** CRITICAL or HIGH issues found.
**Reviewer says:** "For developer: Fixes needed for {FeatureName}, see review.md. Cycle {N}/3."
**Team lead:** Forward to developer.

### Developer → Team Lead → Reviewer: Fixes applied
**Trigger:** All review items addressed.
**Developer says:** "For reviewer: Fixes applied for {FeatureName}, ready for re-review. Cycle {N}/3."
**Team lead:** Forward to reviewer.

### Reviewer → Team Lead: Approved
**Trigger:** APPROVE verdict.
**Reviewer says:** "For team-lead: APPROVED: {FeatureName}."
**Team lead:** Writes summary.md, updates cross-references (spec Status, backlog). No architect wake-up needed — lessons already written after Step 1.

### Reviewer → Team Lead → Architect: Escalation
**Trigger:** 3 fix cycles exhausted OR architecture decision needed.
**Reviewer says:** "For architect: ESCALATION for {FeatureName}: {reason}."
**Team lead:** Forward to architect.

### Developer → Team Lead → Architect: Question
**Trigger:** Blocker mid-implementation.
**Developer says:** "For architect: Blocked on step {N} for {FeatureName}. Question in questions.md."
**Team lead:** Read questions.md. If the answer would reverse a user decision, change scope, or remove a plan step → **ask the user first** before forwarding. Otherwise, forward to architect.
**Architect:** Answers inline, updates plan if needed, messages team lead for developer.

### Architect answers that need user approval
**Trigger:** Architect's answer to a developer question would reverse a user decision, change scope, or remove a plan step.
**Architect says:** "For team lead: Question from developer requires user decision. Options: {A, B, C}. I recommend {X} because {reason}."
**Team lead:** Present options to user. Forward user's decision to architect. Architect updates plan and answers developer.

## Cycle Caps

| Loop | Max | Escalation |
|------|-----|------------|
| Reviewer ↔ Developer fix cycle | 3 | → Architect |
| Developer questions (same area) | 3 | → Human |
| Architect escalation resolution | 1 | → Human |

After escalation to human, agents STOP and wait.

## Implementation Plan Format

Plans created via the `create-implementation-plan` skill include an additional **Skill Mapping** section before Implementation Steps. See the skill's `references/plan-template.md` for the full template.

Plans saved to `docs/specs/{slug}/delivery/plan.md` by architect.

```
# {Feature Name}

**Feature Spec:** `docs/specs/{slug}/definition/spec.md` | None

## Context
What problem this solves. Which service(s) impacted and why.

## Scope
In scope. Explicitly out of scope.

## Domain Model Changes
New/modified aggregates, entities, value objects, domain events.

## Data Model Changes
New tables, columns, relationships, migrations needed.

## Implementation Steps
Numbered steps — each step is one focused task.
For each step:
- What to do (not how to code it)
- Which files to create or modify (full paths)
- What pattern to follow (reference an existing file)
- Which skill to use if one applies
- Dependencies on previous steps

## Cross-Service Changes (if applicable)
gRPC contract changes, integration events, consumers.

## Migration Notes (if applicable)
Migration commands per relevant persistence convention. Seed data if needed.

## Testing Strategy
Key scenarios to test.

## Open Questions
Unresolved decisions needing input.
```

## Implementation File Format

See `docs/conventions/implementation-format.md` for the full format.

## Summary File Format

Written by team lead after reviewer approval: `docs/specs/{slug}/delivery/summary.md`. Its existence means the pipeline completed successfully.

```
# {Feature Name} — Summary

## Status: COMPLETE

## What Was Built
- [1-3 sentence description of the feature/change]

## Key Outcomes
- [Files created/modified count]
- [Build status]
- [Review verdict and cycle count]

## Deviations from Plan
- [Any deviations, or "None"]

## Notes
- [Anything the user should know before committing]
```

## Questions File Format

See `docs/conventions/questions-format.md` for the full format, rules, and routing chain.

## Lessons File Format

All pipeline agents append under their own heading: `docs/specs/{slug}/delivery/lessons.md`. The PO writes lessons during spec shaping (before the delivery/ folder exists) — create the folder and lessons file if needed.

```
# {Feature Name} — Lessons

## PO Lessons
- Spec gaps the critic caught
- Research that changed a decision
- Questions that should have been asked earlier or differently

## Architect Lessons
- Plan instructions that were ambiguous
- Architecture decisions needing documentation
- Skill gaps discovered

## Developer Lessons
- Patterns discovered not yet documented
- Inconsistencies found
- Steps missing from a skill

## Reviewer Lessons
- Review criteria that were unclear
- Recurring code quality issues
- Patterns that should become conventions
```

For systemic issues that recur across features, append an improvement proposal inside the relevant heading:

```
### Improvement Proposal (optional, for systemic issues)
**Target:** {file path — agent file, skill, convention, or rule}
**Change:** {what to add/modify}
**Evidence:** {which features demonstrated this, with links}
**Priority:** {low/medium/high}
```

Only add items not already in CLAUDE.md, convention files, skills, or agent files. Update before `/compact` or `/clear`.

**Mandatory:** Every agent must write lessons before finishing its work. This is not optional — if you learned something (a gap, a pattern, a mistake, an ambiguity), write it down. If the lessons file doesn't exist yet, create it. If your heading already exists, append to it. No agent exits without writing lessons.

## Review Checklist

### Step 1: Done check (architect, no code reading)
- Every plan step has a corresponding implementation.md entry
- No plan steps missing or silently skipped
- Reported deviations have reasons
- No unexpected files or scope creep

### Step 2: Code review (reviewer, reads implementation)
- Each plan instruction has corresponding code that matches
- Referenced patterns and skills were followed
- Naming conventions match coding standards
- Build passes (fresh output, not assumed)
- No new patterns invented
- Domain rules enforced in aggregates, not handlers
- Deviations flagged with verdict: plan wrong or code wrong
- Security: no hardcoded secrets, inputs validated, no injection vectors
- Logic: all branches reachable, no off-by-one, null handling correct
- Performance: check for performance anti-patterns per loaded conventions
- Skill mapping verified: any "None" disposition in the Skill Mapping was warranted (no existing skill actually covers the step)

### Severity Ratings (reviewer)
- **CRITICAL**: Security vulnerability, data loss risk, fundamentally wrong approach. Blocks merge.
- **HIGH**: Logic error, missing error handling, plan deviation without justification. Should fix.
- **MEDIUM**: Suboptimal pattern, minor inconsistency. Consider fixing.
- **LOW**: Style preference, minor improvement. Optional.

### Verdict
- **APPROVE**: No CRITICAL or HIGH issues.
- **REQUEST CHANGES**: Any CRITICAL or HIGH issue present.
- **COMMENT**: Only MEDIUM/LOW, no blockers.

## Review Output Format

See `docs/conventions/review-format.md` for the full format, anti-patterns, and consumer table.

## Skill Authority

Skills in `.claude/skills/` are the authoritative source for patterns.

- **Skill exists, codebase has what it needs:** Follow the skill.
- **Skill exists, codebase missing something it references:** Build it. Escalate to architect only if scope seems too large.
- **No matching skill:** Architect may inline snippets. Log the missing skill in lessons.md.

The `create-architecture-doc` and `create-implementation-plan` skills enforce this principle via mandatory skill inventory checks. Architecture docs defer implementation patterns to skills. Plans map each step to a skill or justify inline detail.
