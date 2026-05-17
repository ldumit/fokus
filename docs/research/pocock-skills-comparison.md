# Matt Pocock Skills vs Fokus Pipeline — Deep Dive Comparison

**Date:** 2025-05-17  
**Source:** https://github.com/mattpocock/skills/tree/main/skills/engineering

---

## Executive Summary

Matt Pocock's skill set is a **developer-workflow toolkit** — 10 skills focused on how a solo developer (or AI agent) navigates, debugs, designs, and ships code. Our system is a **team-pipeline factory** — 31 skills focused on what artifacts to produce and how agents coordinate. The two are complementary, not competing. Our biggest gaps are in the *thinking* skills (diagnosis, architecture improvement, prototyping) that Pocock excels at.

---

## Structural Comparison

| Dimension | Pocock Skills | Fokus Skills |
|-----------|--------------|--------------|
| Count | 10 | 31 |
| Focus | Developer workflow & thinking | Artifact scaffolding & team coordination |
| Scope | Any codebase, language-agnostic | .NET/Vue, project-specific |
| Domain awareness | `CONTEXT.md` + ADRs (generic) | `docs/kb/`, graphify, architecture docs |
| Inter-skill references | Explicit handoffs (diagnose → improve-architecture) | Implicit via pipeline (plan step → skill mapping) |
| Agent model | Single agent, self-directed | Multi-agent pipeline with handoffs |
| File path philosophy | **No file paths in long-lived artifacts** (they rot) | Full file paths in plans (anchored, verified at use-time) |
| Trigger mechanism | Natural language triggers in description | Skill mapping in plans + manual invocation |

---

## Skill-by-Skill Gap Analysis

### What Pocock Has That We DON'T

| Pocock Skill | Gap in Our System | Impact | Priority |
|---|---|---|---|
| **`diagnose`** | No structured debugging skill. Developers debug ad-hoc. | When bugs hit, there's no phased protocol — hypothesize/instrument/fix/cleanup is left to developer judgment. | **HIGH** |
| **`grill-with-docs`** | Our PO + critic do spec cross-checks, but no Socratic stress-testing against domain vocabulary. | Domain terminology drift. We have `docs/kb/` but no skill that *challenges* it or updates it during design. | **MEDIUM** |
| **`improve-codebase-architecture`** | No "deepening opportunities" discovery skill. Architect only acts on request. | Architectural debt accumulates silently. No proactive seam/depth analysis. | **MEDIUM** |
| **`prototype`** | No throwaway-code skill. Everything goes through the full pipeline. | Can't quickly answer design questions with disposable code. Pipeline overhead for exploration. | **MEDIUM** |
| **`triage`** | No issue triage state machine. We have pipeline slugs but no issue lifecycle management. | Issues don't flow through ready-for-agent / needs-info states. No AFK/HITL classification. | **LOW** (we use Jira externally) |
| **`zoom-out`** | We have graphify + KB for navigation, but no explicit "reorient" skill. | Agents jump into code without building mental maps. Graphify partially fills this. | **LOW** |
| **`to-issues`** | No plan-to-issues decomposition skill. Plans go straight to developer. | Plans aren't broken into independently-grabbable vertical slices for parallel work. | **LOW** (single-dev team currently) |
| **`to-prd`** | Our `create-feature-spec` is close but more structured (PO-driven). | No quick "synthesize from conversation" mode. Our spec process is heavier by design. | **LOW** |

### What We Have That Pocock DOESN'T

| Our Skill | Pocock Equivalent | Why We Need It |
|---|---|---|
| 8 scaffolding skills (create-service, create-module, etc.) | None — Pocock is language-agnostic | Our .NET DDD structure requires precise scaffolding |
| Event/integration skills (add-integration-event, create-domain-event-handler) | None | Microservice coordination is complex |
| Pattern reference skills (domain-patterns, cqrs-patterns, etc.) | Partially in LANGUAGE.md/DEEPENING.md | Our patterns are tech-specific, not architectural philosophy |
| Multi-agent pipeline (7 agents + coordination protocol) | None — single-agent model | Our team pipeline handles complex features end-to-end |
| `improve-skills` / `improve-flow` (learner) | None | Self-improving system that promotes lessons |
| Frontend skills (vue-patterns, pinia-patterns, etc.) | UI.md within prototype only | Full Vue component architecture guidance |
| `frontend-review` checklist | None | Structured frontend code review |

### Overlapping Areas (Different Approaches)

| Area | Pocock Approach | Our Approach | Assessment |
|---|---|---|---|
| **TDD** | Phased loop: plan → tracer bullet → incremental → refactor. Deep module theory. Mock only at boundaries. | No explicit TDD skill. Testing is "Testing Strategy" section in plans. | **Pocock is stronger.** We should adopt the phased loop and mocking philosophy. |
| **Planning** | `to-prd` (synthesize from context, no interview) + `to-issues` (vertical slices) | `create-feature-spec` (PO interviews) + `create-implementation-plan` (architect maps) | **We're more thorough** for complex features but **heavier** for small work. Both valid. |
| **Architecture review** | `improve-codebase-architecture` with deletion test, depth/leverage/locality vocabulary | Architect agent + `system-design` skill | **Pocock adds proactive discovery.** Our architect is reactive (acts on request). |
| **Domain grounding** | `CONTEXT.md` (glossary) + ADRs (decisions) | `docs/kb/` (business rules) + `docs/architecture/` (system shape) | **Different purposes.** We capture *rules*, they capture *vocabulary*. We're missing the vocabulary layer. |
| **Setup** | `setup-matt-pocock-skills` configures issue tracker, labels, domain docs | Our CLAUDE.md + service CLAUDE.md files | **Comparable.** Different shape, same goal. |

---

## Key Philosophical Differences

### 1. File Paths in Artifacts

**Pocock:** "No file paths in long-lived artifacts — they go stale."  
**Us:** Full paths in plans, implementation.md, KB entries.

**Assessment:** Both are defensible. Our plans are *short-lived* (consumed within one pipeline run), so paths are fine there. But our KB entries ARE long-lived and should probably reference types/concepts rather than exact paths. **Partial adoption recommended.**

### 2. Depth vs Scaffolding

**Pocock:** Skills teach *thinking* — how to debug, how to find deepening opportunities, how to stress-test designs.  
**Us:** Skills teach *producing* — what files to create, what patterns to follow, what format to use.

**Assessment:** We need both. Our scaffolding skills prevent mistakes during production. But we lack the thinking skills that prevent bad *decisions* before production starts. **Biggest gap.**

### 3. Single Agent vs Team

**Pocock:** One agent drives everything. Skills are personal workflows.  
**Us:** Multi-agent pipeline with handoffs, escalations, cycle caps.

**Assessment:** Our model is correct for our complexity level (microservices, DDD, multi-layer). But Pocock's skills could live *within* our developer agent — giving it better thinking tools during implementation.

### 4. AFK/HITL Classification

**Pocock:** Every issue is explicitly classified as agent-completable (AFK) or needs-human (HITL).  
**Us:** No such classification. All pipeline work is assumed to need human checkpoints.

**Assessment:** Interesting for future. As trust grows, some plan steps could be marked AFK (agent proceeds without confirmation). Not urgent.

### 5. Domain Vocabulary Layer

**Pocock:** `CONTEXT.md` is a strict glossary — every skill reads it before acting, updates it inline.  
**Us:** `docs/kb/` captures business rules and formulas but isn't a vocabulary glossary.

**Assessment:** We're missing a vocabulary layer. Our KB explains *how things work* but doesn't enforce *what things are called*. Domain terms drift between services. **Worth adopting.**

---

## Concrete Recommendations

### HIGH Priority — Adopt Now

#### 1. Create a `diagnose` skill

Our developer agent has no structured debugging protocol. When bugs hit mid-implementation, the developer ad-hocs. Pocock's phased approach (feedback loop → reproduce → hypothesize → instrument → fix → cleanup) is battle-tested.

**Adaptation needed:**
- Phase 1 strategies should include: failing xUnit test, HTTP request via `.http` file, SignalR test harness, EF query trace
- Phase 5 should integrate with our `lessons.md` writing (developer writes lessons after finding root cause)
- Phase 6 cleanup should include removing `[DEBUG-*]` tagged logs

**Effort:** ~2 hours to write the skill

#### 2. Create a `tdd` skill (or adopt Pocock's)

We have no test-first workflow. Our "Testing Strategy" section in plans is aspirational, not procedural. The vertical-slice TDD loop (one test → one implementation → refactor after green) directly prevents the "horizontal slicing" anti-pattern.

**Adaptation needed:**
- Integration test examples should use our xUnit + WebApplicationFactory pattern
- Mocking rules should reference our "mock only at system boundaries" (same philosophy!)
- Deep module vocabulary maps to our DDD aggregate boundaries

**Effort:** ~3 hours (more supporting files)

#### 3. Add a domain glossary (`CONTEXT.md` equivalent)

We have KB entries per concept but no single glossary that enforces vocabulary. When agents write code, they sometimes invent terms or use inconsistent naming across services.

**Where:** `docs/domain/glossary.md` (or per-service glossary in service CLAUDE.md)  
**Rule:** Agents read glossary before naming new types. Update glossary when new domain concepts emerge.

**Effort:** ~1 hour to seed from existing KB + add a rule

### MEDIUM Priority — Adopt When Relevant

#### 4. Create an `improve-architecture` skill

A proactive "find deepening opportunities" skill that the architect (or solo agent) can invoke between features. Not during pipeline runs — between them.

**What to adopt from Pocock:**
- The "deletion test" (if you delete this module, who breaks?)
- Depth/leverage/locality vocabulary
- The "Design It Twice" pattern using parallel sub-agents

**What to adapt:**
- Seam analysis should check our aggregate boundaries, not generic module boundaries
- Output should be a backlog entry (new feature spec), not an inline refactor
- Should read graphify god nodes to find coupling hotspots

**Effort:** ~3 hours

#### 5. Create a `prototype` skill

When exploring design options (UI variants, state machine shapes, sync strategies), we currently have no "throwaway code" workflow. Everything goes through the full pipeline or gets done as ad-hoc solo work without structure.

**What to adopt:**
- Two branches: logic (terminal/console app) vs UI (Vue variants with `?variant=` param)
- "Capture the answer, delete the code" philosophy
- One command to run, no tests, no error handling

**What to adapt:**
- Logic branch: .NET console app or xUnit test harness (not TUI)
- UI branch: Vue component variants with route query param switcher
- Capture goes into the feature spec's `definition/` folder as context

**Effort:** ~2 hours

#### 6. Adopt "no file paths in KB entries" rule

Our KB entries reference specific file paths. These rot when code moves. Switch to referencing type names and concepts — agents can grep for them.

**Rule change:** KB entries use type/class names (grepable), not full file paths.  
**Migration:** Update existing KB entries next time they're touched.

**Effort:** ~30 min for the rule, ongoing migration

### LOW Priority — Consider Later

#### 7. AFK/HITL classification for plan steps

Mark each plan step as "agent can proceed autonomously" vs "needs human checkpoint." Currently all steps are implicitly HITL (developer implements, architect checks). For mature patterns with strong skills, some steps could be AFK.

#### 8. `zoom-out` equivalent

Our graphify + KB navigation partially covers this. A lightweight "reorient me" skill that reads graphify communities + KB index + service CLAUDE.md could formalize it. Not urgent — agents already have navigation rules.

#### 9. `.out-of-scope/` directory for rejected features

Pocock's triage skill tracks rejected enhancements in `.out-of-scope/` files so the same request doesn't get re-triaged. We could adopt this for our backlog — when a feature is explicitly rejected, document why.

---

## Patterns Worth Stealing (Not Full Skills)

These aren't worth a dedicated skill but should be absorbed into existing skills/rules:

| Pattern | Source | Where to Apply |
|---|---|---|
| **Unique debug log prefix** (`[DEBUG-a4f2]`) | `diagnose` | Add to developer agent instructions: tag debug instrumentation for easy cleanup |
| **"Design It Twice" with parallel sub-agents** | `improve-codebase-architecture` | Add to architect agent for complex interface decisions |
| **Triage comment prefix** (`> *Generated by AI during triage*`) | `triage` | Any time agents post to external systems (Jira, GitHub) |
| **Agent brief principles** (no file paths, behavioral descriptions) | `triage/AGENT-BRIEF.md` | Improve our plan writing rules for the "What to do" descriptions |
| **Prototype capture → ADR** | `prototype` | When solo agent experiments produce insights, capture as decision record |
| **"Deletion test"** for module importance | `improve-codebase-architecture` | Add to architect's plan review checklist |

---

## What NOT to Adopt

| Pocock Element | Why Skip |
|---|---|
| `setup-matt-pocock-skills` | We have our own onboarding (CLAUDE.md + service CLAUDE.md). Different structure, same purpose. |
| `to-issues` (GitHub issue creation) | We use Jira externally. Our plans already decompose into steps. If we move to GitHub Issues, revisit. |
| `to-prd` ("don't interview, synthesize") | Our PO *should* interview. The Socratic spec process catches requirements that pure synthesis misses. |
| `CONTEXT-MAP.md` (multi-context routing) | Our service CLAUDE.md files + `docs/kb/index.md` already route per-service context. |
| `grill-with-docs` as a standalone skill | The *behavior* (stress-test against domain vocabulary) should be absorbed into our PO and critic agents, not exist separately. |
| Single-agent model | Our pipeline complexity (DDD, microservices, multi-layer) justifies multi-agent. |

---

## Implementation Roadmap

```
Phase 1 (next sprint):
  ├── Create diagnose skill (HIGH value, immediate use)
  ├── Create docs/domain/glossary.md + navigation rule
  └── Add debug-prefix pattern to developer agent

Phase 2 (following sprint):
  ├── Create tdd skill
  ├── Absorb "grill" behavior into critic agent
  └── Update KB rule: type names over file paths

Phase 3 (when needed):
  ├── Create improve-architecture skill
  ├── Create prototype skill
  └── Add AFK/HITL classification to plan format
```

---

## Conclusion

Pocock's skills fill a **thinking gap** in our system. We're excellent at *producing correct artifacts* (scaffolding, coordination, review) but weaker at *making better decisions* (debugging methodology, architecture discovery, design exploration). The highest-value adoptions are `diagnose` and `tdd` — they give our developer agent structured protocols for the messy parts of implementation that our pipeline currently leaves unguided.

The domain glossary is a quick win that compounds over time — every agent that reads it produces more consistent naming, which reduces reviewer findings, which speeds up the pipeline.
