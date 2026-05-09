---
name: create-implementation-plan
description: Creates implementation plans with mandatory skill mapping per step. Ensures plan steps reference skills instead of restating patterns. Extends the agents-workflow.md plan format.
user-invocable: false
---

# Create Implementation Plan (Architect Reference)

This skill is for the **architect agent**. It produces `docs/plans/{Feature}/plan.md` — the implementation plan that the developer executes. Each step either references a skill or explicitly justifies inline detail. No freeform plans.

## Purpose

Generate `docs/plans/{Feature}/plan.md` with a structural guarantee that each step either references a skill or explicitly justifies inline detail. Extends the plan format in `agents-workflow.md` with mandatory skill mapping.

## When to use

After a feature spec exists (`docs/features/{Feature}/spec.md` with `Status: Ready`). Replaces freeform plan writing.

## Reading protocol

Before writing, ensure you have:

1. **Feature spec** — read `docs/features/{Feature}/spec.md`
2. **Architecture doc** — read for system shape and existing decisions
3. **Skill inventory** — scan `.claude/skills/` — read every `SKILL.md` frontmatter. If the architecture doc has a Skill Inventory section, use it; otherwise build one from scratch.
4. **Existing plans** — read `docs/plans/*/plan.md` for format consistency

## Steps

1. **Pre-fill from conversation context.** The architect has typically been discussing the feature before invoking this skill. Pull every answer you can from the existing conversation. Do not re-ask what the user already said.

2. **Run the reading protocol.** Read existing feature specs, architecture doc, skill inventory, and existing plans.

3. **Draft the implementation steps.** Each step is one focused task with full file paths.

4. **Build the Skill Mapping** — for each step, determine the disposition:
   - **Follow** — a skill covers this pattern. Provide only feature-specific inputs (entity names, file paths, property types). Do NOT restate how the pattern works.
   - **Build** — a skill covers this pattern but references infrastructure that doesn't exist locally (BuildingBlocks, base classes). The step must include building the missing infrastructure first, then following the skill.
   - **None** — no skill covers this step. Provide full inline detail. Log to the Gaps column for future skill creation.

5. **Anti-pattern check.** Scan the draft for:
   - The word "adapted" or "adapt" near a skill reference — violation. Either Follow or Build, never Adapt.
   - A skill dismissed as "too simple" or "not needed for this case" — violation. Skills ensure consistency; complexity is not the criterion.
   - Implementation pattern details restated when a skill exists — over-specification. Delete and reference the skill.

6. **Write the plan** following `references/plan-template.md`. Output: `docs/plans/{Feature}/plan.md`.

7. **Apply the auto-approve gate** from `agents-workflow.md` (<=12 steps, no open questions — auto-approve and message developer).

## Arguments

Pass the feature name: `create-implementation-plan Sprint`

## What this skill does NOT do

- Write code. Implementation is the developer's job.
- Make architectural decisions — those come from the architecture doc.
- Override the `agents-workflow.md` coordination protocol — it extends the plan format, doesn't replace the pipeline.
- Replace the architect's judgment about step ordering, scope, or complexity classification.
