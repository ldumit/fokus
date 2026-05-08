# Spec-Driven Development (SDD) Research

---

## What SDD Is

ThoughtWorks definition: "A development paradigm that uses well-crafted software requirement specifications as prompts, aided by AI coding agents, to generate executable code."

**Core idea:** Specifications — not code — are the primary artifact. Code is generated or verified from specs.

The term crystallized 2025-2026, positioned as the disciplined alternative to "vibe coding" (Andrej Karpathy, Feb 2025).

---

## Three Levels of SDD

| Level | Description | Code Status |
|-------|-------------|-------------|
| **Spec-First** | Spec written before coding to guide initial build; may drift afterward | Code is the maintained artifact |
| **Spec-Anchored** | Spec maintained alongside code throughout lifecycle; tests enforce alignment | Both spec and code are maintained |
| **Spec-As-Source** | Humans only edit specs; code is entirely generated and never manually modified | Code is disposable/regenerable |

---

## How SDD Works (GitHub Spec Kit Model)

Four phases, each requiring human validation before proceeding:

| Phase | Human Input | Agent Output |
|-------|------------|--------------|
| **Specify** | High-level "what/why" | Detailed specification |
| **Plan** | Stack, architecture, constraints | Comprehensive technical plan |
| **Tasks** | Review spec + plan | Individually testable work items |
| **Implement** | Task-by-task oversight | Focused, reviewable code changes |

### Artifacts Produced

1. **constitution.md** — non-negotiable project principles (tech stack, architecture, security)
2. **requirements.md** — user stories, acceptance criteria, constraints
3. **design.md** — architecture, sequence diagrams, schemas
4. **tasks.md** — discrete, independently testable implementation units

---

## Who Is Actually Doing SDD

### Confirmed SDD Practitioners

| Company | Evidence | Details |
|---------|----------|---------|
| **AWS** | Direct (Kiro IDE) | Built IDE around SDD; internal team cut 2-week feature to 2 days |
| **GitHub/Microsoft** | Direct (Spec Kit) | Open-sourced toolkit; published formal SDD guides |
| **Tessl** | Direct (product) | Spec-as-source platform; CEO predicts "devs won't look at code by 2027" |
| **ThoughtWorks** | Direct (published analysis) | Internal IT uses SPDD variant |
| **Red Hat** | Published guide | Enterprise SDD adoption guide |

### Zapier — NOT Doing SDD

Zapier is **ticket-driven, not specification-driven**. Their workflow:

```
Jira ticket → branch creation → implementation → automated review → commit
```

Skills encode team conventions ("best practices"), not specifications. The ticket is the input, not a maintained spec. No specification-as-source-of-truth pattern exists.

**Lisa Chapello's framing:** "Skills encode best practices; MCP runs them at scale." — this is convention-driven, not spec-driven.

### Anthropic — Partially, Not Formally SDD

Shares significant SDD traits but isn't formally SDD:
- CLAUDE.md = constitution (persistent project context)
- Product Note = lightweight specification
- Context files (`product_area_context.md`, `code_context.md`) = spec + plan
- No formal SDD tooling, no automated drift detection
- Better characterized as "context-as-architecture"

### Rakuten — NOT Doing SDD

Aggressive agentic development with TDD. No specification-as-source pattern. Engineering velocity focus, not specification rigor.

---

## SDD vs. What We Do

| SDD Concept | Our Equivalent | Match Level |
|---|---|---|
| Constitution | `CLAUDE.md` + rule files + guardrails | Strong |
| Specification | `docs/features/{Feature}.md` (PO agent output) | Strong |
| Technical Plan | `docs/plans/{Feature}/plan.md` (architect output) | Strong |
| Task Breakdown | Implementation steps in plan.md | Strong |
| Validation/Drift Detection | Architect done-check + reviewer code-review | Partial |
| Spec-to-implementation verification | Not automated — done by agents reading files | Gap |
| Living spec (maintained post-build) | Not maintained after implementation | Gap |

**We are at the "Spec-First" level** — specs guide implementation but aren't maintained as living documents post-build. Code becomes the source of truth after implementation.

---

## What SDD Adds That We Don't Have

1. **Automated drift detection** — CI checks that implementation still matches spec
2. **Living specs** — spec is updated when code changes (spec-anchored level)
3. **Machine-readable structure** — specs formatted for AI consumption, not just human
4. **Spec-based test generation** — acceptance criteria auto-generate test suites
5. **Branch-per-spec model** — one branch per specification (GitHub Spec Kit)

---

## Criticisms of SDD (Documented)

### 1. Waterfall Risk
ThoughtWorks (Technology Radar Vol. 33, 2025) places SDD in "Assess" ring. Warns of "bias toward heavy up-front specification and big-bang releases."

**Counter:** When AI compresses build from months to minutes, waterfall objection loses force. The problem with waterfall was long feedback cycles, not front-loaded planning.

### 2. Specs Become Outdated
Specs that cannot change = waterfall. Specs that change without discipline = just user stories. Rapidly changing requirements don't benefit from detailed specifications.

### 3. False Sense of Control (Birgitta Bockeler, Martin Fowler's site)
Agents frequently don't follow all instructions. Larger context windows don't guarantee compliance. "I'd rather review code than all these markdown files."

### 4. Over-engineering for Small Tasks
Amazon Kiro turned a small bug fix into 4 user stories with 16 acceptance criteria. One-size workflows don't adapt to varying problem sizes.

### 5. Non-deterministic Generation
Even with a fixed spec, code generation varies across runs (documented with Tessl).

### 6. Natural Language Ambiguity
Natural language specs are inherently ambiguous. Unlike Model-Driven Development, natural-language specs cannot be validated for completeness by tooling.

### 7. No Automated Spec-to-Implementation Verification
**Notable gap:** None of Claude Code, Cursor, Windsurf, Aider, or Devin automatically verifies that implementation matches the original specification.

### 8. Team Adoption Friction
67% of teams report extra debugging time during learning phase. Specification writing is unfamiliar.

---

## When SDD Does NOT Work

- Highly exploratory work (research, prototyping)
- Rapidly changing requirements
- Novel algorithms requiring manual coding
- Performance-critical systems needing manual optimization
- Solo developers on well-understood problems
- Small bug fixes
- Aesthetic/UX decisions

---

## SDD Tools Landscape (2026)

| Tool | Type | Key Feature | Level |
|------|------|-------------|-------|
| **AWS Kiro** | IDE | Natural language → 3-phase spec workflow | Spec-first |
| **GitHub Spec Kit** | CLI toolkit | `/specify` → `/plan` → `/tasks` → implement | Spec-first to spec-anchored |
| **Tessl** | Platform | Spec Registry (10k+ pre-built specs); code marked "DO NOT EDIT" | Spec-as-source |
| **Cursor** | IDE | No native SDD; `.cursor/rules/` as workaround | Not SDD |
| **cc-sdd** | Harness | Minimal SDD for Claude Code, Codex, Cursor | Spec-first |
| **spec-coding-mcp** | MCP server | SDD workflows via MCP | Spec-first |

---

## How SDD Compares to Adjacent Approaches

| Dimension | User Stories | PRD | BDD | TDD | SDD | Our Approach |
|---|---|---|---|---|---|---|
| **Audience** | Agile teams | Cross-functional | QA + devs | Developers | AI agents + engineers | AI agents (architect, developer, reviewer) |
| **Granularity** | Feature-level | Product-level | Behavior-level | Unit-level | System-level | Feature-level |
| **Machine-readable?** | No | No | Partially (Gherkin) | Yes (tests) | Yes (by design) | Partially (structured markdown) |
| **Executable?** | No | No | Yes (Cucumber) | Yes | Aspires to | No |
| **Living document?** | In backlog | Can be | Usually | Always (tests) | By design | Not post-build |
| **Prevents rework?** | Low | Medium | Medium | High (at unit level) | High (at system level) | High (at feature level) |

---

## Positioning: Where We Sit

```
Vibe Coding    Ticket-Driven    Spec-First         Spec-Anchored       Spec-as-Source
     │              │                │                    │                    │
     ▼              ▼                ▼                    ▼                    ▼
 Anthropic       Zapier          US (Fokus)         GitHub Spec Kit        Tessl
 (PM prototypes) (Jira→Skills)   (PO→Feature Spec   (spec maintained      (code = generated,
                                  →Plan→Build)        alongside code)       never edited)
                  Rakuten
                  (TDD + parallel)
```

**We are Spec-First.** Our specs guide implementation but aren't maintained as living documents. Code becomes truth after build.

---

## Key Sources

- ThoughtWorks: Spec-Driven Development Unpacking (Dec 2025)
- Martin Fowler / Birgitta Bockeler: Understanding SDD Tools (Kiro, spec-kit, Tessl)
- GitHub Blog: Spec-Driven Development with AI (Sep 2025)
- arXiv: Spec-Driven Development: From Code to Contract (Feb 2026)
- Kiro IDE Documentation
- DeepLearning.AI: SDD with Coding Agents (Course)
- ThoughtWorks Technology Radar Vol. 33 (2025) — SDD in "Assess" ring
