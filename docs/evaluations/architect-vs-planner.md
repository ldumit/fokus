# Architect vs OMC Planner — Side-by-Side Evaluation

## Context

This evaluation compares the project's **Architect agent** (`.claude/agents/architect.md`) against OMC's **Planner agent** (plugin marketplace). The trigger: the architect produced a plan (F25-XrayIntegration, Step 12) that prescribed a 235-line monolithic method — the developer implemented it literally, the reviewer passed it, and we only caught it during a manual refactoring session.

The goal: identify which agent produces better plans for code quality, and whether to adopt the planner wholesale or improve the architect.

---

## Scoring (1-10)

| Category | Architect | Planner | Winner | Notes |
|----------|:---------:|:-------:|:------:|-------|
| Plan Granularity Control | 5 | 9 | Planner | Architect's F25 Step 12 had 9 internal sub-steps under one method. Planner hard-caps at 3-6 steps. |
| Codebase Discovery | 9 | 6 | Architect | Architect reads files directly, builds full dependency graphs. Planner delegates to haiku explore agent — faster but shallower. |
| User Interaction Model | 7 | 8 | Planner | Planner enforces one question at a time, only preferences. Architect batches questions — efficient for experts but can miss nuance. |
| Quality Gates | 8 | 7 | Architect | Architect has critic review, self-review, spec cross-check, gap analysis. Planner has analyst + user confirmation. |
| Skill/Pattern Awareness | 9 | 2 | Architect | Architect scans full skill inventory, maps every step to a skill or justifies inline. Planner has zero skill awareness — relies on executor to discover patterns. |
| Over-specification Prevention | 5 | 9 | Planner | Planner's DNA: "stop when actionable." Architect has content budgets but ALSO has conflicting rules ("named identifiers are binding contracts", "specify line-number ranges") that pull toward over-specification. The F25 plan shows the over-specification force won. |
| Scope Control | 7 | 9 | Planner | Planner: "default to minimal scope" as a hard constraint with explicit failure mode. Architect flags out-of-scope but doesn't have a "stop over-detailing" brake. |
| Handoff Quality to Implementer | 8 | 6 | Architect | Architect gives full file paths, skill references, domain model changes, named identifiers. Planner gives 3-6 steps with acceptance criteria — implementer needs more autonomy/judgment. |
| Failure Mode Awareness | 4 | 9 | Planner | Planner has explicit `<Failure_Modes_To_Avoid>` with Good/Bad examples. Architect has no section describing what a BAD plan looks like. |
| Intent Classification | 7 | 7 | Tie | Both classify by complexity. Architect: Trivial/Scoped/Complex/Refactoring. Planner: Trivial/Refactoring/Build from Scratch/Mid-sized. |
| Pipeline Integration | 10 | 3 | Architect | Architect is deeply integrated: plans + done checks + answers questions + escalations + lessons. Planner is fire-and-forget — generates plan, hands off, disappears. |
| Learning/Feedback Loop | 9 | 1 | Architect | Architect writes lessons after every cycle. Learner consolidates them. Planner has zero feedback mechanism. |
| Code Structure Awareness | 4 | 3 | Architect* | Neither agent understands code structure (method decomposition, class design). Architect is marginally better because it reads code and references patterns. But F25 proves this awareness is insufficient. |

---

## Totals

| Agent | Score (out of 130) | Average |
|-------|:-----------------:|:-------:|
| **Architect** | 92 | 7.1 |
| **Planner** | 79 | 6.1 |

---

## Analysis: Why the Scores Are Misleading

The architect scores higher overall because it does MORE things (pipeline integration, learning, done checks). But for the **specific job of producing a good plan**, the planner wins on the dimensions that matter most:

| "Good plan" dimensions | Architect | Planner |
|------------------------|:---------:|:-------:|
| Granularity control | 5 | 9 |
| Over-specification prevention | 5 | 9 |
| Scope control | 7 | 9 |
| Failure mode awareness | 4 | 9 |
| **Subtotal (plan quality)** | **21/40** | **36/40** |

The architect's strength is pipeline integration and thoroughness. Its weakness is knowing when to STOP detailing. The planner's strength is discipline — it knows exactly when a plan is "done" and refuses to over-specify.

---

## Root Cause: Conflicting Forces in the Architect

The architect has two sets of rules that pull in opposite directions:

**Force 1: Restraint** (from `create-implementation-plan` skill)
- "What to do (not how to code it)"
- "Follow step content budget: 5-15 lines"
- "Over-specification test: delete structural patterns, keep only feature-specific inputs"

**Force 2: Precision** (from architect agent plan writing rules)
- "Named identifiers in plans are binding contracts"
- "Specify line-number ranges for extraction targets"
- "Specify the error reporting shape upfront"
- "Validate response model shapes against all consumers"

When a step has `Disposition: None` (no matching skill), Force 2 wins — the architect writes full inline implementation detail including pseudo-code logic flows. This is what happened in F25 Step 12: no skill existed, so the architect wrote 9 sequenced sub-steps describing method internals.

The planner avoids this by design: it has no "precision" force. It always stops at "what to accomplish + acceptance criteria." The executor fills in the how.

---

## What the Planner Does Better (Adopt)

1. **Hard cap on plan step internals.** The planner never describes method body logic. It describes operations and acceptance criteria. The executor decides code structure.

2. **Explicit failure modes with examples.** Showing what a BAD plan looks like is as important as showing the template. The architect has no "bad plan" examples.

3. **"Stop when actionable" as a constraint.** The planner's termination condition is explicit. The architect's is implicit (when the template is filled).

4. **One question at a time.** Forces the planner to actually listen between questions rather than front-loading assumptions in a batch.

5. **Minimal scope default.** "Don't propose architecture redesign unless the task requires it" — stated as a constraint, not a suggestion.

---

## What the Architect Does Better (Keep)

1. **Skill mapping per step.** The planner has zero skill awareness. This means the executor must rediscover patterns every time. The architect's skill mapping is a genuine advantage — it prevents pattern drift and ensures consistency.

2. **Deep codebase analysis.** The architect reads actual code, analyzes dependency graphs, greps for type references. The planner's haiku explore agent is shallow by comparison.

3. **Quality gates with critic.** Independent cross-reference review catches spec-plan gaps. The planner relies on analyst + user — no independent verification.

4. **Pipeline lifecycle.** Done checks, question answering, escalation handling, lessons — these create a feedback loop that improves plans over time. The planner is stateless.

5. **Full file paths and domain model changes.** The developer gets precision about WHERE to put things. The planner's executor must figure this out from scratch.

---

## What Neither Agent Has (Gap)

1. **Code structure awareness.** Neither agent knows that "9 sub-steps in a method" = code smell. Neither suggests decomposition at the plan level. Neither loads C# coding conventions.

2. **Plan-to-code structural mapping.** No rule exists saying "a plan step with N operations maps to M methods, not 1." The translation from plan → code structure is undocumented.

3. **Negative examples in the plan template.** The template shows what TO include but never shows what happens when the plan over-specifies (the developer implements literally).

---

## Recommendation

**Don't adopt the planner.** It solves over-specification but loses skill mapping, pipeline integration, and codebase depth. Those are harder to rebuild than fixing over-specification.

**Improve the architect** by stealing the planner's best ideas:

### Fix 1: Add a "None" step content rule

When `Disposition: None` (no matching skill), the current behavior is "full inline detail required." Change to:

> **None steps describe operations and acceptance criteria — not method body logic.** State what the code must accomplish, what inputs/outputs look like, and what constraints apply. Do NOT describe sequential logic flow within a single method. The developer decides internal code structure.
>
> **Content budget for None steps:** Same as Follow steps (5-30 lines). If a None step exceeds 30 lines, it's either multiple steps pretending to be one (split it) or it's describing method internals (delete the how, keep the what).

### Fix 2: Add failure modes section to architect

```markdown
## Plan Failure Modes — Do Not

- **Method-body plans:** Describing sequential logic steps (1. do X, 2. do Y, 3. do Z) 
  under a single method signature. This produces monolithic implementations. 
  Instead: describe operations and constraints. Let developer decide decomposition.
  
- **30+ micro-steps:** A plan with >15 steps or sub-steps within steps is over-specified. 
  Instead: combine related operations into one step with acceptance criteria.
  
- **Pseudo-code in plans:** Writing "Logic flow: 1. Read X, 2. Filter Y, 3. Map to Z, 
  4. Persist." Instead: "Sync discovered entities to the database. Accept: all link 
  types persisted, partial failures don't block."
  
- **Implementation detail in None steps:** Just because no skill exists doesn't mean 
  you should write the implementation. The skill gap means MORE developer judgment 
  needed, not less.
```

### Fix 3: Resolve the conflicting forces

Demote the "precision" rules to apply only when disposition is Follow/Build (where the developer needs exact inputs for a skill). For None steps, the restraint rules dominate:

- "Named identifiers are binding contracts" → Keep for public API surfaces, class names, endpoint routes. Remove for private method names and internal structure.
- "Specify line-number ranges" → Keep for extraction/move refactors only. Remove for greenfield code.
- "Specify the error reporting shape" → Keep (this is API contract, not internal).

### Fix 4: Add `csharp.md` conventions file

Load via `@` in developer and reviewer agents. Contains method length rules, decomposition principles. This is the safety net when the plan is ambiguous — developer knows to decompose regardless.

### Fix 5: Add plan-writing rule to stack-rules.md

One line the architect sees:

> Plan steps describe operations and acceptance criteria — not method body logic flows. The developer decides internal code structure and decomposition.

---

## Implementation Priority

| Fix | Effort | Impact | Priority |
|-----|--------|--------|----------|
| Fix 5: stack-rules one-liner | 1 min | Prevents the specific F25 failure | Do first |
| Fix 2: Failure modes section | 10 min | Prevents class of plan quality issues | Do second |
| Fix 1: None step content rule | 5 min | Fixes the root cause in the skill | Do third |
| Fix 3: Resolve conflicting forces | 15 min | Removes the structural tension | Do fourth |
| Fix 4: csharp.md conventions | 30 min | Safety net for all agents | Do fifth |

---

## Final Verdict

The architect is the better agent for this project — but it has a specific blind spot (over-specification of None steps) that the planner avoids by design. The fix is surgical: add the planner's discipline around "stop when actionable" specifically to the None-disposition path, add failure mode examples, and resolve the conflicting precision/restraint forces. No need to replace the whole agent.
