# Skill Design Proposal: Architecture Doc & Implementation Plan

## The Problem

Two failure modes compound into one:

1. **Architecture document written in a vacuum.** The Architect writes `docs/architecture/v1.md` without checking what skills already exist. It specifies implementation details — file structures, handler signatures, validation patterns — that duplicate or contradict what skills define. The architecture doc becomes an alternative source of truth that competes with skills.

2. **Plan over-specifies because the architecture doc over-specified.** The Architect.md already has the right rule for plans (line 77: "when a skill exists, provide only feature-specific inputs"). But by the time the plan is written, the Architect has internalized the over-specified architecture doc as gospel. It faithfully translates architecture doc details into plan steps, including implementation patterns the skills should own. It even dismisses skills as unnecessary ("this part is simple, no need for the building block skill").

**Root cause:** There's a rule for skill deference in plans, but no equivalent rule for architecture documents. And even the plan rule is prose in an agent file — not a structural constraint enforced by a template.

---

## What Already Exists (Research Findings)

### Planning skills/plugins

| Name | Author | Stars | What it solves | What it doesn't solve |
|---|---|---|---|---|
| `planning-with-files` | OthmanAdi | 22K+ | Session persistence, progress tracking (task_plan.md, findings.md, progress.md). Manus-style. | No plan template. No skill awareness. Generic task planner, not architecture-aware. |
| `deep-plan` (Deep Trilogy) | piercelamb | — | Thoroughness: Research → Interview → LLM Review → TDD Plan → Section Splitting. Multi-LLM review. | No concept of existing project skills. Designed for greenfield. Token-heavy. |
| Git-Flow `implementation-plan-generator` | mcpmarket | — | Generates structured plan.md from issue analysis. | Generic, no skill inventory check. |

### Architecture skills/plugins

| Name | Author | Stars | What it solves | What it doesn't solve |
|---|---|---|---|---|
| `elixir-architect` | maxim-ist | — | Generates 8 architecture docs + ADRs + guardrails + handoff materials. Director/Implementor workflow. | Elixir-specific. Over-specifies by design — no separate skill layer to defer to. |
| `dotnet-claude-kit` architecture-advisor | codewithmukesh | — | Asks 15 questions, recommends VSA/CA/DDD/Modular Monolith, then **defers to its 46 other skills** for implementation. Closest delegation model. | Decision advisor only, not an architecture document generator. |
| `dotnet-skills` routing snippet | Aaronontheweb | — | CLAUDE.md routing snippet that maps tasks → skills by name. | No architecture doc or plan skill. |
| `senior-architect` (alirezarezvani/claude-skills) | alirezarezvani | — | Generic architecture review and scalability analysis. 232+ skill collection. | No skill inventory awareness. No template constraint. |

### Key patterns worth stealing

- **`dotnet-claude-kit`:** Separates "which architecture" from "how to implement." The architecture-advisor skill makes the decision; 46 other skills own the patterns. This is the delegation model we want.
- **`deep-plan`:** Uses `references/` directory for step details, keeping SKILL.md concise. Also uses deterministic scripts for state management (TODO list prepopulation). Progressive disclosure pattern.
- **`elixir-architect`:** Generates a full docs/ tree with architecture/, design/, plans/, decisions/ separation. Good structural model for what gets generated.
- **`planning-with-files`:** "Read before decide" rule — re-read the plan file before major decisions. Applicable to our "re-read skills before writing plan steps."

### What nobody has built

No skill exists that:
- Writes an architecture document **while checking the skill inventory first** and deferring implementation details to existing skills
- Writes an implementation plan **with a template that structurally prevents over-specification** when a skill covers the pattern

This gap is specific to setups that have both an Architect agent AND a rich skill library. Most people have one or the other.

---

## Proposed Skills

### Skill 1: `create-architecture-doc`

**Purpose:** Generate or update the project's architecture document with a structural guarantee that implementation details are deferred to skills.

**Invocation:** Architect agent, when starting a new project or updating architecture after significant changes.

**Core mechanic — skill inventory as step 1:**
Before writing any architecture section, the skill requires running `ls .claude/skills/` and building a mapping table: skill name → what pattern it covers. This table is embedded in the architecture document itself. Every architectural decision that has a matching skill references the skill by name and does NOT restate the implementation pattern.

**Template structure (what the output looks like):**

The architecture document produced by this skill would have these sections:

- **Skill Inventory** — table mapping each skill name to what it covers. Built by scanning `.claude/skills/` at generation time. This is the forcing function.
- **System Shape** — services, boundaries, communication patterns. High-level only: what exists and why.
- **Architectural Decisions** — one subsection per decision area. Each has: Decision (what), Rationale (why), Constraints (non-negotiable rules), Skill Reference (which skill owns the implementation pattern OR "none — temporary inline detail"). A **"Not specified here"** field explicitly names what the architecture doc is NOT defining — forcing the Architect to acknowledge the boundary.
- **Cross-Cutting Concerns** — same decision structure for logging, auth, error handling, etc.
- **Gaps** — areas where no skill exists yet. These are the only sections allowed to contain implementation detail, and each gap gets logged to `lessons.md` for future skill creation.

**Key design decisions for the skill itself:**

- Should reference a template in `references/template.md` (progressive disclosure — keeps SKILL.md under 500 lines).
- Should NOT be `disable-model-invocation: true` — we want the Architect to auto-invoke it when writing architecture docs.
- The "Not specified here" field per decision is the structural trick. Without it, the Architect's natural tendency is to keep adding detail until the section "feels complete." This field makes the boundary explicit.
- The Gaps section creates a feedback loop: over time, as skills are created from gap entries, the architecture doc naturally gets less specific because more areas have skills to defer to.

**Relationship to existing files:**

- Replaces the current unstructured `docs/architecture/v1.md` with a templated version.
- The `agents-workflow.md` "Skill Authority" section already says skills are authoritative — this skill operationalizes that principle for architecture docs.

---

### Skill 2: `create-implementation-plan`

**Purpose:** Generate implementation plans with a structural guarantee that each step either references a skill or explicitly justifies why inline detail is needed.

**Invocation:** Architect agent, when creating a plan for a feature (replaces the current freeform plan writing guided only by prose rules in Architect.md).

**Core mechanic — skill mapping as mandatory plan section:**
Before writing implementation steps, the skill requires:
1. Reading the feature spec
2. Running `ls .claude/skills/` (or reading the architecture doc's Skill Inventory if it exists)
3. For each planned step, declaring: "Skill: {name} — feature-specific inputs: {only what's unique to this feature}" OR "No skill — inline detail required — log to lessons.md"

This declaration goes in a **Skill Mapping** section at the top of the plan, before the implementation steps.

**Template structure (what the output looks like):**

The plan produced by this skill would follow the existing `agents-workflow.md` format but add:

- **Skill Mapping** (new, mandatory) — table with columns: Step #, Skill Name (or "none"), Feature-Specific Inputs Only, Justification (if no skill). This is the pre-flight check that prevents over-specification.
- **Implementation Steps** — same numbered format as today, but each step that maps to a skill contains ONLY: what to name things, what properties/types to use, which file paths — NOT how the pattern works. The template would include a reminder: "If you're describing how a pattern works and a skill exists for it, you're over-specifying. Delete the description and reference the skill."
- An **anti-pattern callout** embedded in the template: "Never call a skill's domain 'simple' to justify skipping it. Skills ensure consistency; complexity is not the criterion."

**Key design decisions for the skill itself:**

- Template goes in `references/plan-template.md`.
- Should be `disable-model-invocation: true` — invoked explicitly via `/create-implementation-plan` because plan creation is a deliberate workflow step, not something that should auto-trigger.
- The Skill Mapping table is the structural equivalent of what `dotnet-claude-kit` does with its architecture-advisor → implementation skill routing. But instead of routing to a different agent, it routes plan steps to skill references.
- The "Justification if no skill" column creates accountability. The Architect can't just skip skills silently — it has to explain why.
- Compatible with the existing `agents-workflow.md` plan format — this is an enhancement, not a replacement.

**Relationship to existing files:**

- The plan format in `agents-workflow.md` stays as-is (it's the shared contract between agents). The skill adds the Skill Mapping section and the template guardrails.
- The plan writing rules in `Architect.md` (lines 74-79) can be simplified once this skill exists — the rules move from prose into the template.
- The Developer still reads the plan the same way. The Skill Mapping section is additional context that tells the Developer "for this step, read skill X before implementing."

---

## How the Two Skills Work Together

The architecture doc (Skill 1) feeds the plan (Skill 2):

1. **Skill 1** generates the architecture doc with a Skill Inventory table and decision-level references to skills.
2. When the Architect writes a plan, **Skill 2** reads the architecture doc's Skill Inventory (or re-scans `.claude/skills/`) and uses it to populate the Skill Mapping.
3. If the architecture doc over-specified something (maybe it was written before a skill existed), the plan skill catches it: the Skill Mapping step forces a check against current skills, and any conflict gets flagged as a lessons entry.

This creates a self-correcting loop:
- Architecture doc defers to skills → plan defers to skills → gaps get logged → gaps become skills → architecture doc gets simpler over time.

---

## What Changes in Existing Files

### Architect.md

**Simplify, don't duplicate.** Once these skills exist:

- "Plan writing rules" section (lines 74-79) — the skill-related rules ("Reference relevant skills", "Missing skill → lessons entry", "Never inline what a skill defines") move into the `create-implementation-plan` skill template. Architect.md keeps a one-liner: "Use the `create-implementation-plan` skill when writing plans."
- Add a one-liner for architecture docs: "Use the `create-architecture-doc` skill when writing or updating architecture documentation."
- The "Before classifying the codebase" section stays — it's about verification behavior, not plan/doc structure.

### agents-workflow.md

- Plan format section gets a note: "Plans created via the `create-implementation-plan` skill include an additional Skill Mapping section at the top."
- Skill Authority section gets a note: "The `create-architecture-doc` and `create-implementation-plan` skills enforce this principle via mandatory skill inventory checks."

### CLAUDE.md

- No changes needed. CLAUDE.md stays minimal. The skills are auto-discovered from `.claude/skills/`.

---

## Open Questions

1. **Should `create-architecture-doc` be a one-time generation or an update-in-place workflow?** The `elixir-architect` generates once. But architecture docs evolve. An update workflow would re-scan skills and flag sections where a new skill now covers what was previously inline detail.

2. **Should the skill inventory in the architecture doc be auto-refreshed?** If a new skill is added after the architecture doc was written, the doc becomes stale. Options: re-run the skill periodically, or have a lightweight "refresh inventory" mode.

3. **How prescriptive should the plan template be about step granularity?** The current plan format leaves step size to the Architect's judgment. The `deep-plan` approach splits into parallelizable sections. Worth considering whether the template should enforce a max step size or leave it flexible.

4. **Should these skills be project-level (`.claude/skills/`) or user-level (`~/.claude/skills/`)?** Project-level means they're committed with the repo and specific to the project's conventions. User-level means they're portable across projects. Given that the template references project-specific skill inventories, project-level seems right — but the SKILL.md itself could be user-level with project-specific reference files.
