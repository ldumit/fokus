# Specs Flow Comparison: Fokus vs. Industry

How our feature specification process compares to documented practices at Anthropic, Rakuten, TELUS, and Zapier.

---

## Our Specs Flow (Current State)

```
Idea → PO agent (discussion + research + challenge) → Feature Spec → Architect (plan) → Developer (build)
```

**Detailed steps:**

1. User says "be po" — PO agent activates
2. PO does internal research (v1 spec, existing features, graphify report, codebase)
3. PO does external research (competitors, best practices) when relevant
4. PO interviews user — asks all clarifying questions in one batch
5. PO challenges assumptions ("what if we didn't do X?", "what's the simplest version?")
6. PO runs gap check (complete? testable? unambiguous? edge cases? guardrails?)
7. PO writes spec using `create-feature-spec` skill → `docs/features/{Feature}.md`
8. PO does voice check (no code artifacts leaked into product language)
9. PO cross-checks against source spec (v1.md)
10. Status set to Ready → architect can plan

**Spec template includes:** Purpose, Entities, User Flows, API Surface, SignalR Events, Business Rules, Acceptance Criteria, Out of Scope, Open Questions.

**Example output (JiraSync):** 14 business rules, 17 acceptance criteria, 5 user flows, explicit API surface with error conditions, clear out-of-scope with reasons.

---

## What Each Company Does Instead

### Anthropic

**No documented spec phase.** Their workflow starts at "first stop planning" — ask Claude which files to examine, then start working. The closest equivalent is:

- API team: "first stop for any task — ask which files to examine before doing anything else"
- Product team: classify sync vs. async, then start coding
- Security team: pseudocode → TDD → check in periodically

**Implicit spec source:** GitHub Issues. Filing an issue triggers Claude to propose code. The issue IS the spec.

**Gap:** No evidence of formalized business rules, acceptance criteria, or out-of-scope documentation before implementation begins.

### Rakuten

**No documented spec phase.** Their accelerated lifecycle (24→5 days) wraps Claude around: unit tests, API mocking, component scaffolding, bug fixes, docs. The spec equivalent is:

- Jira tickets (implied by their `work-on-ticket` patterns)
- CLAUDE.md conventions (the closest to a "living spec")
- TDD as specification — tests define behavior before code

**Gap:** No published evidence of a "define what to build" phase separate from "build it."

### TELUS

**Template-driven at platform level.** 21,000 custom copilots built suggests a template/wizard approach:

- Fuel iX template gallery as the "spec" for non-developers
- ISO 31700-1 certification implies formal requirements exist somewhere, but not documented publicly for developer workflows

**Gap:** Their rigor lives in platform governance and compliance, not per-feature specification.

### Zapier

**Jira ticket IS the spec.** Their `work-on-ticket` skill:

> "Pulls Jira ticket details, creates feature branches, handles initial planning"

The entire spec → plan → build flow is compressed into one skill invocation triggered by a ticket. Lisa Chapello's philosophy: "Skills encode best practices; MCP runs them at scale."

**Gap:** No evidence of a challenge/research phase before building. The assumption is that by the time something is a Jira ticket, the "what" is already decided.

---

## Comparison Matrix

| Dimension | Fokus | Anthropic | Rakuten | TELUS | Zapier |
|---|---|---|---|---|---|
| **Spec exists as a separate artifact** | Yes — dedicated markdown file with structured template | No — GitHub Issue is the spec | No — Jira ticket + TDD | Implied — platform templates | No — Jira ticket is the spec |
| **Dedicated "define what" phase** | Yes — PO agent owns it exclusively | No — jumps to planning/coding | No — jumps to TDD | Yes — but at enterprise/compliance level, not per-feature | No — ticket = definition |
| **Research before specification** | Internal (codebase, existing specs) + external (competitors, best practices) | Some — "ask Claude which files" | Not documented | Not documented | Not documented |
| **Challenge/pushback mechanism** | Built-in — PO challenges assumptions, tests essentiality | Not documented | Not documented | Not documented | Not documented |
| **Acceptance criteria before coding** | Yes — testable, observable outcomes in spec | Not documented pre-coding | Tests written first (TDD) serve this role | Compliance-level acceptance | Not documented |
| **Business rules documented** | Yes — numbered, enforceable, in domain language | Not documented as a separate artifact | Not documented | Not documented | Not documented |
| **Scope boundary defined** | Yes — explicit "Out of Scope" with reasons | Not documented | Not documented | Not documented | Not documented |
| **Voice separation (product vs. code)** | Enforced — spec uses domain language, plan uses code language | No separation documented | No separation documented | No separation documented | No separation documented |
| **Gap check before writing** | Yes — complete? testable? unambiguous? edge cases? | Not documented | Not documented | Not documented | Not documented |
| **Cross-reference to source spec** | Yes — "Traces to" field links back to product spec | Not documented | Not documented | Not documented | Not documented |
| **Time to first code** | Longest — spec must be Ready before plan starts | Shortest — immediate | Short — TDD starts fast | Medium — governance gates | Short — ticket triggers build |

---

## Strengths of Our Approach

1. **Prevents expensive rework.** The JiraSync spec has 14 business rules. Discovering even one of these mid-implementation (e.g., "commitment derivation depends on changelog analysis") could blow up a plan and require re-architecture.

2. **Clean handoff boundary.** Developer never touches the spec. Architect translates spec → plan. No "but I thought you meant..." problems because ambiguity was resolved during PO discussion.

3. **Scope is explicit.** "Out of Scope" with reasons means the developer doesn't gold-plate. The architect doesn't over-plan. Everyone knows what NOT to build.

4. **Domain language stays clean.** Voice check prevents code artifacts from leaking into product thinking. This keeps the spec useful for non-developer stakeholders.

5. **Research-backed decisions.** External research grounds decisions in what exists in the market. Internal research prevents conflicts with existing features.

---

## Weaknesses / Risks

1. **Overhead for small features.** A 14-rule, 17-criteria spec makes sense for JiraSync (the data ingress backbone). Does it make sense for "Bug Ratio chart" (F13)? That might need 3 rules and 5 criteria.

2. **Sequential bottleneck.** Nothing happens until the spec hits Ready. If the PO discussion takes 3 rounds, that's 3 back-and-forth cycles before any planning starts.

3. **No "spike" path.** Sometimes the right move is "build a prototype, learn, then spec." Rakuten's TDD-first approach and Anthropic's "slot machine" pattern work because exploration IS specification. Our flow assumes you know enough to spec before you code.

4. **Single-person bottleneck.** The PO agent requires the user to answer questions. If the user is busy, the pipeline stalls at step 0. Zapier's model (ticket = spec = trigger) removes this dependency.

5. **No lightweight tier.** Every feature gets the same spec ceremony regardless of risk or novelty. A well-understood CRUD feature shouldn't need the same gap check as a complex analytics derivation.

---

## What We Could Adopt

| Idea | From | Effort | Impact |
|---|---|---|---|
| **Tiered spec depth** — lightweight template for well-understood features, full template for novel/complex ones | General observation | Low — add a "Lite" template with Purpose + API + 3-5 acceptance criteria only | High — removes overhead for obvious features |
| **TDD as spec validation** — after spec is written, have Claude generate acceptance tests BEFORE the plan | Rakuten, Anthropic Security | Medium — new skill or step in pipeline | Medium — catches ambiguity earlier, gives developer runnable criteria |
| **Spike path** — "explore first, spec after" for features with high uncertainty | Anthropic "slot machine" | Low — add as an explicit option in PO flow | Medium — prevents over-specifying when you don't know enough yet |
| **Ticket-as-trigger** — allow a well-written Jira ticket to skip the PO discussion when it already has enough detail | Zapier `work-on-ticket` | Low — add a "fast-track" check in PO agent | Medium — removes bottleneck for obvious work |
| **Parallel spec + prototype** — PO specs while developer spikes in a worktree | Anthropic parallel sessions | Medium — coordination needed | High — removes the sequential bottleneck |

---

## Scorecard: Specs Flow Only (1-10)

| Category | Fokus | Anthropic | Rakuten | TELUS | Zapier |
|---|:---:|:---:|:---:|:---:|:---:|
| **Spec completeness & precision** | 10 | 4 | 3 | 5 | 4 |
| **Research depth before spec** | 9 | 5 | 3 | 4 | 3 |
| **Challenge / pushback quality** | 9 | 3 | 3 | 3 | 3 |
| **Scope boundary clarity** | 10 | 4 | 3 | 5 | 4 |
| **Speed to first code** | 3 | 9 | 8 | 6 | 9 |
| **Adaptability (light vs. heavy)** | 3 | 8 | 7 | 7 | 8 |
| **Exploration / spike support** | 2 | 8 | 7 | 5 | 6 |
| **Automation / trigger-driven** | 2 | 7 | 4 | 6 | 9 |
| **Scales to non-developers** | 4 | 6 | 5 | 9 | 7 |
| **Prevents rework downstream** | 9 | 5 | 6 | 6 | 5 |
| | | | | | |
| **TOTAL** | **61** | **59** | **49** | **56** | **58** |

---

## Summary

We're the most rigorous spec process in this group — and it shows in downstream quality (minimal rework, clean handoffs, no ambiguity). But we're also the slowest to first code and the least flexible.

The companies that ship faster don't skip specification — they **compress it into the ticket/TDD/prototype itself**. Their specs are implicit (tests, tickets, conventions) rather than explicit (dedicated documents).

**The strategic question:** Is the rework we prevent worth the time the spec ceremony costs? For a complex feature like JiraSync (14 business rules, multiple derivation algorithms), absolutely. For a chart that shows "bugs per developer per sprint" (F13), probably not.

**Recommended next step:** Introduce tiered spec depth — keep the full ceremony for novel/complex features, add a lightweight path for well-understood ones.
