# Cross-Project Skill Mining — Findings Report

**Date:** 2026-05-24
**Source project:** `D:\Omnishelf\omnishelf-docs`
**Target project:** `D:\src\fokus`
**Skills analyzed:** `docs-bootstrap`, `meeting-to-notes`, `kb-sync`
**Method:** 9 parallel agents (3 per skill: project architect, OMC critic, PO)

## Executive Summary

Three well-crafted skills from the Omnishelf project were mined for techniques, patterns, and structural ideas — not for domain adoption but for craft that could strengthen Fokus's pipeline setup, agent files, rule files, and skills. Nine agents produced ~70 total techniques, which consolidated into 15 convergent patterns ranked by how many agents independently identified them.

None of the three skills should be adopted as-is (different domains, different workflows). The value is in the structural craft: how they decompose phases, gate quality, track state, prevent errors, and present information.

---

## Convergence Table

Patterns ranked by independent agent convergence (how many of 9 agents identified the same pattern).

| # | Pattern | Agents | Effort | Target Files |
|---|---------|--------|--------|-------------|
| 1 | Anti-patterns per agent/phase | 7/9 | LOW-MED | agent files, skill files |
| 2 | Carry-over findings (cross-phase intel) | 5/9 | LOW | agents-workflow.md, reviewer.md |
| 3 | Self-check gate before handoff | 5/9 | LOW | developer.md |
| 4 | Structured state / resumability | 5/9 | LOW-MED | team-lead.md, communication-log format |
| 5 | KB lint checks | 4/9 | LOW | kb-maintenance.md, reviewer.md |
| 6 | Message/output size contract | 4/9 | LOW | agents-workflow.md, team-lead.md |
| 7 | Structured checkpoint reports + action options | 3/9 | LOW | agents-workflow.md, agent files |
| 8 | KB hard rules (preserve, deprecate, don't delete) | 4/9 | LOW | kb-maintenance.md |
| 9 | Phase I/O contracts on skills | 3/9 | LOW | skill sub-files |
| 10 | Downstream consumer tables | 3/9 | LOW | kb-maintenance.md, skill files |
| 11 | Idempotency gate at launch | 3/9 | LOW | architect.md, developer.md, team-lead.md |
| 12 | Confidence scoring (plan steps / findings) | 3/9 | LOW-MED | agents-workflow.md, reviewer.md |
| 13 | Convention extraction from agents-workflow.md | 2/9 | MED | docs/conventions/, agents-workflow.md |
| 14 | Known deviations / corrections catalog | 2/9 | MED | new file, developer.md |
| 15 | Skill changelogs | 2/9 | LOW | skill CHANGELOG.md files |

---

## Detailed Findings by Pattern

### 1. Anti-Patterns Per Agent/Phase

**Identified by:** Critic-DB, PO-DB, Arch-DB, PO-MTN, Critic-MTN, Arch-MTN, Arch-KBS (7 agents)

**Source craft:** Every phase file and template in docs-bootstrap and meeting-to-notes ends with a dedicated `## Anti-patterns` section listing 4-8 specific failure modes with concrete examples. These are not abstract rules — they are observed mistakes codified to prevent recurrence. Example from meeting-to-notes: "Hallucinating decisions from brainstorms — 'They seemed to agree' is not a decision."

**Current state in Fokus:**
- `architect.md` has "Plan Failure Modes — Do Not" (good, closest analogue)
- `po.md` and `team-lead.md` have "What You Never Do" (role boundaries, not cognitive anti-patterns)
- `developer.md` and `reviewer.md` have no anti-pattern sections
- Skills: only `create-implementation-plan` has an anti-pattern check step
- Pipeline templates (implementation.md, review.md, questions.md formats) have zero anti-patterns

**Gap:** Agents repeat the same mistakes across features. The lessons.md files capture them, but they're not promoted into agent-level guidance where they prevent the next occurrence.

**Recommended changes:**
- Add `## Anti-patterns` to `developer.md` (3-5 items: skipping plan steps silently, inventing patterns when a skill exists, writing implementation.md without explaining what changed)
- Add `## Anti-patterns` to `reviewer.md` (3-5 items: flagging style as HIGH severity, approving without fresh build, checking only latest commit)
- Add anti-pattern subsections after each template format in `agents-workflow.md`
- Seed from existing `docs/specs/*/delivery/lessons.md` files across completed features
- Add a learner pathway: recurring lessons get promoted into the relevant agent's anti-patterns section

---

### 2. Carry-Over Findings (Cross-Phase Intelligence)

**Identified by:** Critic-DB, PO-DB, Arch-DB, PO-MTN, Arch-KBS (5 agents)

**Source craft:** docs-bootstrap uses `_meta/phase_carryover.yaml` — a structured findings file that accumulates across phases. Phase 3 subagents write observations; Phase 5 subagents are contractually required to verify or refute each one with evidence. No silent skips.

**Current state in Fokus:**
- The architect's done-check and the reviewer's code review are independent
- If the architect notices a concern during done-check that passes the completeness bar, that observation dies at the handoff
- Developer discoveries go into implementation.md under "Key Decisions" — unstructured prose the reviewer may or may not notice

**Gap:** This is the single biggest intelligence loss in the pipeline. The architect has context (plan vs implementation) that the reviewer lacks. The developer has context (codebase discoveries) that both architect and reviewer lack. That context dies at handoff boundaries.

**Recommended changes:**
- Add `## Carry-Over Findings` section to the implementation.md format (developer writes)
- Add carry-over to review.md Step 1 output (architect writes during done-check)
- Structure: `**{title}** | Severity: {low/medium/high} | For: {reviewer/architect/developer}` + Evidence + Note
- Add to reviewer Step 2 checklist: "Each carry-over finding addressed (confirmed or refuted with evidence)"

---

### 3. Self-Check Gate Before Handoff

**Identified by:** Critic-MTN, PO-MTN, Arch-MTN, Arch-KBS, PO-KBS (5 agents)

**Source craft:** meeting-to-notes Phase 3 has a structured self-check table with explicit pass conditions, split into blocking vs warning-only checks. A hard gate prevents pipeline advancement if blocking checks fail.

**Current state in Fokus:**
- Developer has a "Completion Checklist" in developer.md (prose list, no pass/fail conditions)
- No distinction between blocking and warning checks
- No structured verification before claiming "done"
- The architect's Step 1 done-check catches issues, but by then a full round-trip has happened

**Gap:** Common Step 1 failures (missing plan steps, build failures) waste an architect round-trip that a developer self-check would catch.

**Recommended changes:**
- Convert developer's completion checklist into a structured self-check table:

```
| Check | Pass condition | Blocking? | On failure |
|-------|---------------|-----------|------------|
| Build | dotnet build exits 0 | Yes | Fix before proceeding |
| Plan coverage | Every plan step has an implementation.md entry | Yes | Add missing entries |
| Debug artifacts | No TODO/HACK/DEBUG in modified files | Yes | Remove artifacts |
| Deviations | Every deviation documented with reason | Yes | Document or revert |
| Lessons | lessons.md updated | No | Write lessons |
```

- Gate rule: do not message "ready for Step 1" until all blocking checks pass

---

### 4. Structured State / Resumability

**Identified by:** PO-DB, PO-KBS, Arch-DB, Arch-KBS, Arch-MTN (5 agents)

**Source craft:** All three Omnishelf skills persist structured state after every phase boundary. docs-bootstrap uses `_meta/bootstrap-state.yaml`, kb-sync uses `_meta/kb-sync-state.yaml`, meeting-to-notes uses extraction receipts with incremental updates.

**Current state in Fokus:**
- `communication-log.md` tracks Branch, Step, Cycle, and messages
- It's a message log, not a state machine — records what happened, not intermediate work products
- Timeout recovery requires `git diff --name-only` and inference
- Agent IDs are tracked in memory during a run but lost on session restart
- Developer writes implementation.md once at the end, not incrementally

**Gap:** Resume is narrative-reconstruction rather than deterministic. The team lead parses the message table and infers context.

**Recommended changes:**
- Extend communication-log.md header with structured state fields:

```
**Team Mode:** {fast/standard/standard+codex}
**Review Mode:** {self-review/critic}
**Architect ID:** {agent-id}
**Developer ID:** {agent-id}
**Reviewer ID:** {agent-id}
**Plan Steps Completed:** [1, 2, 3]
**Plan Steps Remaining:** [4, 5, 6]
**Questions Resolved:** [Q1, Q2]
```

- Developer writes implementation.md incrementally (after each step, not all at end)
- Consider YAML frontmatter on implementation.md for machine-parseable progress

---

### 5. KB Lint Checks

**Identified by:** Critic-KBS, PO-KBS, Arch-KBS, PO-DB (4 agents)

**Source craft:** kb-sync Phase 4 defines 12 named lint checks with severity levels, runnable standalone via `/kb-sync lint`. Checks include: orphan-term, stale-entry, broken-xref, duplicate-term, entry-count-regression.

**Current state in Fokus:**
- Reviewer instruction: "Flag stale or missing entries as HIGH severity" — no concrete checks
- No standalone KB health check capability
- KB already has inconsistencies (e.g., "Key file" vs "Key files" vs "Key Files" across entries)
- Glossary references entity names without validating they exist in KB entries

**Gap:** The reviewer has no actionable KB verification procedure. As the KB grows past 20 entries, silent rot is inevitable.

**Recommended changes:**
- Add `## Lint Checks` to `kb-maintenance.md`:

```
| Check | Severity | Condition |
|-------|----------|-----------|
| broken-index-link | error | index.md links to a file that doesn't exist |
| orphan-entry | warning | KB file not linked from index.md |
| stale-key-file | warning | "Key file:" path doesn't exist in codebase |
| duplicate-term | warning | Same concept in glossary and domain entry with conflicting definitions |
| empty-section | info | Section header with no content |
```

- Replace vague reviewer instruction with explicit reference to these checks
- Optionally: new `/kb-lint` skill for standalone use

---

### 6. Message/Output Size Contract

**Identified by:** Critic-DB, Arch-DB, PO-DB, Arch-MTN (4 agents)

**Source craft:** docs-bootstrap's subagent-dispatch playbook defines a strict contract: subagent reads from disk, writes results to disk, returns a 300-word report. The parent never receives raw data or full file contents. Includes a 10-item briefing checklist for every subagent prompt.

**Current state in Fokus:**
- Team-lead.md says "relay agent messages verbatim" with no size constraint
- Agents sometimes produce verbose output (full file dumps, verbose reasoning)
- No instruction to write results to disk first, then summarize
- Context bloat causes compaction-related amnesia on longer pipeline runs

**Gap:** Unbounded agent output inflates the team lead's context window.

**Recommended changes:**
- Add "Message Size Contract" to `agents-workflow.md`:
  - Analysis outputs: ~500 words max
  - Handoff messages: ~300 words max
  - Content goes in files (implementation.md, review.md, questions.md)
  - Messages are notifications with summaries, not full dumps
  - "Write first, message second" (already exists for questions — extend to all artifacts)
- Add to `agent-model-routing.md` for Explore/general-purpose agents: structured report, 300 words max

---

### 7. Structured Checkpoint Reports + Action Options

**Identified by:** PO-DB, PO-MTN, Arch-DB (3 agents)

**Source craft:** docs-bootstrap presents a consistent report shape at every checkpoint: headline metrics, table of findings, warnings section ("Needs your attention"), and numbered action options (1. proceed, 2. adjust, 3. stop). Default option clearly marked. User can reply with just a number.

**Current state in Fokus:**
- Phase transitions present open-ended questions or walls of text
- No consistent report format across phases
- User has to figure out their options from unstructured output

**Gap:** The user gets different shapes of information at every transition point.

**Recommended changes:**
- Add a checkpoint report template to `agents-workflow.md`:

```
{Phase Name} — {Slug}
================================================
{2-4 headline metrics}

{Table or list of findings}

Needs your attention:
  1. {flagged item}

Action options:
  1. {default} (recommended)
  2. {alternative}
  3. Stop
```

- Apply to: architect Phase 1 output, developer Phase 1 output, done check, reviewer verdict
- Team lead enforces: if agent output doesn't end with action options, team lead adds them

---

### 8. KB Hard Rules (Preserve, Deprecate, Don't Delete)

**Identified by:** Critic-KBS, PO-KBS, Arch-KBS, PO-DB (4 agents)

**Source craft:** kb-sync has 11 numbered hard rules specific to KB operations: "Never delete existing entries — mark as deprecated," "Never overwrite without diff," "Never overwrite with less content," "Cross-references must resolve."

**Current state in Fokus:**
- `kb-maintenance.md` describes WHAT to do but not WHAT NOT TO DO
- No deprecation policy — agents could silently remove KB content
- No preserve-on-write protection
- Guardrails in CLAUDE.md are application-level, not KB-specific

**Gap:** Silent KB degradation is hard to detect after the fact.

**Recommended changes:**
- Add `## Hard Rules` to `kb-maintenance.md`:
  1. Never delete a KB entry. Mark with `## Status: Deprecated` and note why.
  2. Never remove sections without replacing them. Update, don't truncate.
  3. Always update `docs/kb/index.md` when adding or renaming an entry.
  4. Use Edit tool for KB updates, not Write. Carry forward untouched sections.

---

### 9. Phase I/O Contracts on Skills

**Identified by:** Arch-DB, Arch-MTN, Arch-KBS (3 agents)

**Source craft:** Each Omnishelf phase file opens with explicit `## Inputs`, `## Outputs`, and `## Gate` blocks declaring exactly what the agent needs, what it produces, and what must be true before proceeding.

**Current state in Fokus:**
- Skills are monolithic SKILL.md files (adequate at current size)
- Skills with sub-files (`create-feature/workflows/`) don't declare I/O contracts
- No "required reading" declarations — agents are expected to know what to read
- No explicit gate conditions between phases

**Gap:** Context-loading is implicit. Agents load entire skills when they only need one section.

**Recommended changes:**
- For complex skills, add `## Required Reading` block at the top of SKILL.md
- For skills with sub-files, add `## Inputs` / `## Outputs` / `## Gate` to each sub-file
- For new multi-phase skills, adopt the `phases/` convention with a phase index table

---

### 10. Downstream Consumer Tables

**Identified by:** PO-MTN, Arch-MTN, Arch-KBS (3 agents)

**Source craft:** Skills explicitly document who consumes their output and what they do with it. meeting-to-notes lists PO agent, search-notes skill, bug-to-jira routing. This makes the output contract visible and the impact of quality concrete.

**Current state in Fokus:**
- Artifact consumption is only implicit through the pipeline protocol
- KB maintenance feels like busywork because nobody documents who reads KB and why
- Skills document "when to use" but not "who uses the output"

**Gap:** Agents don't know who reads their artifacts or what stale/bad output costs downstream.

**Recommended changes:**
- Add `## Consumers` table to `kb-maintenance.md`:

```
| Agent | What they read | Impact of stale data |
|-------|---------------|---------------------|
| Architect | Business rules, formulas | Plan steps miss edge cases |
| Developer | Key file paths | Wrong files modified |
| Reviewer | Business rules | Bugs missed in review |
```

- Add `## Downstream Consumers` to artifact format definitions in `agents-workflow.md`
- Add to skills that produce pipeline artifacts: create-implementation-plan, create-feature-spec, diagnose

---

### 11. Idempotency Gate at Launch

**Identified by:** PO-MTN, Arch-MTN, PO-KBS (3 agents)

**Source craft:** meeting-to-notes checks if the meeting was already processed before starting. Offers explicit choices: re-do or skip.

**Current state in Fokus:**
- Team lead has a Resume section that checks communication-log.md, but only when user says "resume"
- No proactive guard at launch time for completed features
- No architect check for existing plan.md before planning
- No developer check for existing implementation.md before implementing

**Gap:** User might say "implement F32" not knowing a prior run completed. Silent overwrites possible.

**Recommended changes:**
- Team lead pre-flight Step 0: check summary.md (completed) and communication-log.md (in-progress) before any agent spawn
- Architect Phase 1: check if plan.md exists, warn before overwriting
- Developer Phase 2: check if implementation.md exists, read it to determine completed steps

---

### 12. Confidence Scoring (Plan Steps / Findings)

**Identified by:** Critic-DB, Arch-DB, PO-MTN (3 agents)

**Source craft:** docs-bootstrap has a 5-dimension mechanical confidence rubric. Score = `min(mechanical_ceiling, llm_markdown)` — the LLM can only mark DOWN, never up. meeting-to-notes scores claims 1-10 with a disposition matrix.

**Current state in Fokus:**
- Reviewer uses flat CRITICAL/HIGH/MEDIUM/LOW severity (judgment-only, no rubric)
- Plan steps have no confidence signal
- Architect gap analysis is pass/fail, not scored
- No mechanical floor prevents charitable reviews

**Recommended changes:**
- Add optional `Confidence:` field to plan steps: `high` (clear pattern), `medium` (adaptation needed), `low` (no precedent)
- Add confidence qualifier to reviewer findings (separate from severity): "Confidence: Low — may be intentional"
- Consider a lightweight plan quality rubric (file paths present? skill mapped? scope clear?)

---

### 13. Convention Extraction from agents-workflow.md

**Identified by:** Arch-MTN, Arch-KBS (2 agents)

**Source craft:** Omnishelf separates output schemas into `conventions/` files. Multiple skills and agents share the same convention. Conventions evolve independently of skill versions.

**Current state in Fokus:**
- `agents-workflow.md` mixes coordination protocol, file formats, and review checklists (~300+ lines)
- Implementation.md, review.md, and questions.md formats are only defined inline
- Plan template is already extracted into `create-implementation-plan/references/plan-template.md`

**Gap:** agents-workflow.md is overloaded. Formats are not independently referenceable.

**Recommended changes:**
- Extract into `docs/conventions/`:
  - `implementation-format.md` — Implementation File Format
  - `review-format.md` — Review Output Format
  - `questions-format.md` — Questions File Format
- Replace inline definitions in agents-workflow.md with references
- Shrinks agents-workflow.md by ~80 lines

---

### 14. Known Deviations / Corrections Catalog

**Identified by:** Arch-MTN, Arch-KBS (2 agents)

**Source craft:** meeting-to-notes maintains a "Discovered corrections table" — known errors, corrections, frequency. Corrections recurring 2+ times get promoted to KB.

**Current state in Fokus:**
- Lessons.md captures per-feature observations
- Learner processes them periodically
- No running table of "things the developer keeps getting wrong"
- No intermediate accumulation between lessons.md and skill promotion

**Recommended changes:**
- Create `docs/conventions/known-deviations.md`:

```
| Pattern | Wrong | Correct | Frequency | First seen |
|---------|-------|---------|-----------|------------|
| DI registration | Manual for FastEndpoints | Auto-discovered | 3x | F28 |
| Domain events | Publishing in handler | Via SaveChangesInterceptor | 2x | F30 |
```

- Learner populates from lessons.md files
- Developer reads during Phase 1 as pre-flight check

---

### 15. Skill Changelogs

**Identified by:** Critic-MTN, Arch-MTN (2 agents)

**Source craft:** meeting-to-notes has a semver CHANGELOG.md documenting what changed between skill versions and why, connected to observed deficiencies.

**Current state in Fokus:**
- No skill has a changelog
- Skill evolution is invisible — only in git history
- When improve-skills modifies a skill, no record is kept
- Recurring lessons may re-discover issues already fixed

**Recommended changes:**
- Add CHANGELOG.md to frequently evolved skills: create-feature, create-implementation-plan, diagnose, tdd, create-aggregate
- Update improve-skills to append a changelog entry when modifying a skill
- Include evidence citations: `(Validated: F28 — manual registration caused double-validation)`

---

## Additional Techniques (Not Converged but Valuable)

These were identified by 1-2 agents and are worth noting for future reference:

| Technique | Source | Agent | Effort | Notes |
|-----------|--------|-------|--------|-------|
| Required reading lists on skills | docs-bootstrap | Arch-DB | LOW | Explicit context-loading declaration |
| Redirection tables in agent files | docs-bootstrap | Arch-DB | LOW | Convert "never do X" into "redirect to Y" |
| Front-loading all decisions | docs-bootstrap | PO-DB | LOW | Batch user choices before pipeline starts |
| Time-budget visibility | docs-bootstrap | PO-DB | LOW | Duration estimates at launch |
| Pipeline completion dashboard | meeting-to-notes | PO-MTN | LOW | Printed table at shutdown, not just summary.md |
| Pipeline lessons section | kb-sync | PO-KBS | LOW | Team lead meta-observations about the process |
| Structured improvement proposals in lessons | docs-bootstrap | Arch-DB | LOW | Pre-formed proposals with target+change+evidence |
| Systematic decision checklist for architect | docs-bootstrap | Arch-DB | LOW | Structured sweep instead of organic gap-finding |
| KB change report in implementation.md | kb-sync | PO-KBS | LOW | Table showing KB entries added/updated/removed |
| Change classification for KB updates | kb-sync | Critic-KBS | LOW | NEW/UPDATE/DELETE per KB change |
| Cite-or-flag conformance matrix | meeting-to-notes | Critic-MTN | MED | 5-row disposition table for plan conformance |
| KB entry schema | kb-sync | Arch-KBS, Critic-KBS | MED | Standardize section names and structure |
| Playbook extraction | docs-bootstrap | Arch-DB, Critic-DB | MED | Reusable decision trees for complex judgments |
| Provenance tracking on KB entries | kb-sync | Arch-KBS, Critic-KBS | LOW | Optional Source: field, accrete over time |
| Configuration constants table | kb-sync, meeting-to-notes | Arch-KBS, Arch-MTN | LOW | Named thresholds instead of magic numbers |
| Append-only project changelog | kb-sync | PO-KBS | LOW | docs/changelog.md with one entry per pipeline run |
| Validation evidence in skills | docs-bootstrap | Arch-DB | LOW | "Validated: .NET 10 + FE 6.x on {date}" |
| Preserve-on-write for KB | kb-sync | Arch-KBS, Critic-KBS, PO-KBS | LOW | Use Edit not Write; carry forward untouched sections |
| Incremental implementation.md | meeting-to-notes | PO-MTN | LOW | Write after each step, not all at end |
| "What's NOT asked" documentation | docs-bootstrap | PO-DB | LOW | Explicit defaults with override instructions |
| Reviewer stage gate | meeting-to-notes | Critic-MTN | LOW | Explicit gate between Stage 1 and Stage 2 |

---

## Implementation Grouping

Changes grouped by target file for efficient application:

### `agents-workflow.md`
- Checkpoint report template (#7)
- Message size contract (#6)
- Carry-over findings format (#2)
- Anti-pattern subsections per template (#1)
- Convention extraction to separate files (#13)
- Downstream consumer tables on artifact formats (#10)

### `developer.md`
- Self-check gate table (#3)
- Anti-patterns section (#1)
- Idempotency check in Phase 2 (#11)
- Incremental implementation.md writing (#4)

### `reviewer.md`
- Carry-over consumption rule (#2)
- Confidence qualifier on findings (#12)
- KB lint checks reference (#5)
- Anti-patterns section (#1)
- Stage 1/Stage 2 explicit gate (#additional)

### `team-lead.md`
- Structured state fields in communication-log header (#4)
- Idempotency gate at pre-flight Step 0 (#11)
- Pipeline completion dashboard (#additional)
- "What's NOT asked" defaults documentation (#additional)

### `architect.md`
- Idempotency check for existing plan.md (#11)
- Systematic decision checklist for Phase 1 (#additional)
- Redirection table (#additional)
- Verify Explore results (#additional)

### `kb-maintenance.md`
- Lint checks section (#5)
- Hard rules section (#8)
- Consumers table (#10)
- Preserve-on-write rule (#8)
- Bulk update checkpoint (#additional)

### New files
- `docs/conventions/kb-entry-schema.md` — KB entry structure (#additional)
- `docs/conventions/implementation-format.md` — extracted from agents-workflow.md (#13)
- `docs/conventions/review-format.md` — extracted from agents-workflow.md (#13)
- `docs/conventions/questions-format.md` — extracted from agents-workflow.md (#13)
- `docs/conventions/known-deviations.md` — recurring mistake catalog (#14)
- Skill CHANGELOG.md files for top 5 skills (#15)

### Skill files
- Add `## Required Reading` to complex skills (#9)
- Add `## Inputs` / `## Outputs` / `## Gate` to skill sub-files (#9)
- Add `## Downstream Consumers` to artifact-producing skills (#10)
- Add `## Anti-patterns` to all skills (#1)
- Add `## Hard Rules` to skills with constraints (#additional)
- Add `CHANGELOG.md` to frequently evolved skills (#15)
