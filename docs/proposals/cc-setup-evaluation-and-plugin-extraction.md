# Setup Evaluation + Two-Plugin Extraction Plan

## Context

The user runs a mature, custom Claude Code setup at `D:\src\fokus` (agents, rules, conventions, skills, hooks, docs) layered on **oh-my-claudecode (OMC)**. They asked for: a deep scored (1-10) evaluation of *everything except technical/code skills*; a comparison vs the best public setups (OMC as a benchmark); a recommendation; and a plan to produce two **self-contained** (no OMC dependency) distributable plugins:
- **`cc-core`** — stack-agnostic: everything except code-pattern skills and stack conventions.
- **`cc-dotnet`** — everything (core + .NET/Vue skills + stack conventions).

Method: 3 parallel discovery agents (inventory, web research, stack classification) → my synthesis → **critic adversarial pass** (which caught a factual error — see below) → my own re-verification → this corrected plan. *(The analyst agent for the packaging design died on an API false-positive; I completed that design from the research agent's authoritative plugin-spec output.)*

**Correction log (integrity):** My first draft scored KB **6/10** on a claimed "broken index with ~15 dead links." The critic flagged it; I re-verified with Glob — **all KB entries exist, every link resolves** (`docs/kb/domain/*.md` ×4, `docs/kb/analytics/*.md` ×11). That claim was false; KB is corrected to **8**. Backlog confirmed current (F35).

---

## Part 1 — Scored Evaluation (corrected)

**Headline: the *core* (agents, conventions, skills, docs, KB) is elite — 8-9/10. The *periphery* (hooks, commands, settings hygiene, packaging) is where the real rot is, and it's worse on security/hygiene than first thought. Overall ≈ 7.5/10.** Plugin extraction is the forcing function to fix the periphery.

*Score columns: **Was** = original eval baseline · **Now** = realized after the 2026-05-31 live-setup work (see Part 5 → Realized status).*

| # | Category | Was | Now | Verdict |
|---|----------|:----:|:----:|---------|
| 1 | Agents / orchestration | 9 | **9** | Hub-and-spoke + two-phase Analyze/Act + cycle caps = Anthropic's own orchestrator-worker/evaluator-optimizer patterns, more disciplined than most public setups |
| 2 | Rules & guardrails | 8 | **8** | 10 tight auto-loaded rules + meta guardrail-enforcement; −1 for OMC coupling in `pipeline-guardrails.md` + every-session context cost |
| 3 | Conventions / artifact formats | 9 | **9** | 7 formats, each with a *consumer table* (who reads it, what breaks if stale) — genuinely rare |
| 4 | Skills (process layer) | 8 | **8** | Consistent schema + anti-patterns + maintained skill-backlog; −1 because CHANGELOGs cover only ~5 of 38 skills (claim was overstated) |
| 5 | Docs & spec workflow | 9 | **9** | F35 backlog w/ tiers+status+links, spec definition/delivery split, known-deviations, product v1/v2 + architecture |
| 6 | Memory & KB system | 8 | **8** | Healthy index, glossary-first naming, 5 lint checks, on-demand protocol; −2 only for no durable cross-session state once OMC is removed |
| 7 | Plugin-readiness / portability | 7 | **6** | *(corrected down — not a regression)* no `plugin.json`/`marketplace.json` yet; live agents still hardcode graphify; OMC touchpoints. Jumps to 9 once the plugin ships |
| 8 | Hooks & context injection | 5 | **6** | dead `$CC_AGENT` hooks dropped + `restore-agent.js` added; still observe-only audit, no `guard.js`/`inject-rules.js`/PostToolUse (ship with plugin → 8) |
| 9 | Settings / permissions | 3 | **7** | committed `settings.json` deny-list + finalized two-file split (deny=team, allow=personal); cleaned settings.local 43→16 lines. Remaining: untrack settings.local + scoped Hardened settings → 8 |
| 10 | Commands | 3 | **8** | `.claude/commands/` — 9 real slash-command launchers shipped, replacing the `be {agent}` text convention |

**Dimensions surfaced by the critic that the first 10 missed** (scored for completeness):

| Dimension | Was | Now | Note |
|-----------|:----:|:----:|------|
| Verification / testing integration | 7 | **7** | `tdd` + `diagnose` skills + reviewer's *fresh-build-evidence* gate — a real, unscored strength |
| Resilience / error recovery | 7 | **7** | Idempotency checks, resume-from-incremental-`implementation.md`, 3-attempt circuit breakers |
| Self-documentation / onboarding | 8 | **8** | `.claude/README.md` decision tree + agent table + consumer tables |
| Security / least-privilege | 3 | **5** | hardened deny-list (catastrophic/remote) + cross-repo & secret-read blocks shipped; still a blocklist — `guard.js` preventive hook (ships with plugin) takes it to 7 |

**Weighted overall: Was ≈ 7.5 → Now ≈ 7.9** (Commands +5, Settings +4, Security +2, Hooks +1). Full target **≈ 8.3-8.5 ships *with* the plugin** (guard.js, inject-rules.js, scoped Hardened settings, plugin.json). Core competence stays 9.

### Gaps — prioritized (corrected)

| Pri | Gap | Why it matters | Fix | Status (2026-05-31) |
|----|-----|----------------|-----|--------|
| P0 | OMC coupling (`$CC_AGENT`, `OMC_SKIP_HOOKS`, OMC tool perms, OMC agent-name refs) | Plugins must be self-contained; these break/no-op without OMC | native agent-restore (or drop); strip OMC env/perms; reword pipeline-guardrails generically. (`opusplan` is a Claude Code pattern — KEPT; map to `sonnet` only when packaging) | ⏳ **Partial** — dead `$CC_AGENT` hooks dropped, `opusplan` flagged; `OMC_SKIP_HOOKS` env + OMC-MCP grant + README/pipeline-guardrails refs remain → full decouple at packaging |
| P0 | `@`-import resolution from plugin-shipped agents | Agents `@`-load `@.claude/conventions/pipeline-protocol.md` etc.; those paths won't resolve from a plugin dir | **Validation spike** (see Part 4 risk) — likely ship conventions in plugin + reference via `${CLAUDE_PLUGIN_ROOT}` or inline | ⏳ **Spike** — unresolved; first thing the spike verifies |
| P1 | Always-on rules have no native plugin auto-load | A plugin can't drop files into a project's `.claude/rules/` | **SessionStart hook** emits rule text via `${CLAUDE_PLUGIN_ROOT}` (documented mechanism — *design decision, not a blocker*) | ⏳ **Plugin** — design confirmed (`inject-rules.js`); built at packaging |
| P1 | **Cross-project permission leakage** (live, today) | Allow-list writes into `D:/src/code-forge` + `git mv *` wildcard | Strip from settings now; ship clean committed `settings.json` | ✅ **Resolved** — `settings.json` deny blocks `code-forge` + `git push`/`reset --hard`; settings.local cleaned 43→16. Remaining: `git rm --cached` to untrack |
| P1 | No slash commands | Best-practice + plugin ergonomics gap | Add `commands/` wrapping each agent entry point | ✅ **Done** — 9 launchers in `.claude/commands/` |
| P2 | graphify refs hardcoded in **live** `architect.md`/`po.md` | Dead paths for non-graphify recipients (kit already fixed this) | Parameterize ("if a structural graph exists…") | ⏳ **Plugin** — parameterize at packaging (kit already done) |
| P3 | No durable memory/state once OMC removed | Loses cross-session state | Optional: small file-based memory or a bundled memory MCP | ⏳ **Optional / v2** |
| P3 | No automation runs the KB/cross-ref lint checks | Lints rely on a human remembering (this is *why* the false "broken index" belief was plausible) | Optional CI / a `lint-setup` skill | ⏳ **Optional / v2** |

---

## Part 2 — Industry Comparison

| Setup | Stars≈ | Philosophy | Agents | Skills | Hooks | Orchestration | vs this setup |
|-------|------:|-----------|:------:|:------:|:-----:|---------------|---------------|
| **oh-my-claudecode** | ~35k | Force-multiplier infra + autonomy | 29 (3-tier) | 38 | 20/11 events | hub + autonomous loops (ralph/ultrawork) + real Codex/Gemini parallelism | Broader infra & automation; **less hard-gated discipline** |
| **claude-flow / ruflo** | ~56k | Swarm / hive-mind + neural memory | 100+ | 30 | pre/post | queen+workers, consensus, vector memory, federation | Scale & learning memory; **less reproducible/auditable** |
| **wshobson/agents** | ~36k | Cross-harness agent library | 191 | 155 | none | none (native auto-route) + `plugin-eval` | Huge breadth; **no orchestration, no pipeline, no quality gates** |
| **SuperClaude** | ~23k | Behavioral config + commands/personas | 20 | — | modes | command-driven, persona auto-activate, deep-research | Rich **command vocabulary this lacks**; weaker delivery pipeline |
| **agent-os** | ~5k | Standards-alignment injector | — | — | — | defers to CC plan mode; `/shape-spec` | Closest in *philosophy* (standards/spec discipline); far smaller scope |
| **THIS SETUP** | private | Disciplined, spec-driven, review-gated delivery pipeline | 8 | 38 (11 process) | 3 | hub-and-spoke + hard cycle caps + learner | **Cleanest delivery discipline + artifact rigor + DDD/.NET depth**; thin on hooks/commands/scale |

**Finding (de-hyped):** the defensible niche is the *combination* — disciplined spec→plan→implement→review with **artifact-format rigor AND DDD/.NET depth**. The discipline alone is shared with SuperClaude/agent-os; the combination is not well-served by any single big setup. Borrow selectively: **commands** (SuperClaude), **richer hooks + explicit model-tiering** (OMC/wshobson), **optional memory** (claude-flow) — *without* adopting their autonomy/cost/complexity profile.

**Plugin packaging is well-supported and a clean fit** (verified against official docs): `.claude-plugin/plugin.json` + auto-discovered `skills/`, `agents/`, `commands/`, `hooks/hooks.json`; `${CLAUDE_PLUGIN_ROOT}` for portable paths; a single `marketplace.json` ships **both** plugins from one repo via local `source` paths.

### Part 2b — Comparative Scorecard (all setups, same rubric)

**Caveats:** (1) Other-setup scores are from research, **not hands-on inspection** → lower confidence (±1-2). The fokus row I verified directly. (2) Rubric is built for a *disciplined delivery pipeline*; library/swarm setups score low on pipeline dimensions (B, C) **by design** — that reflects different goals, not failure. (3) "Overall" is a purpose-weighted judgment, not a raw mean.

Dimensions: **A** Orchestration/agents · **B** Process discipline & quality gates · **C** Artifact/format rigor · **D** Skills/reusable patterns · **E** Hooks & context injection · **F** Commands/UX · **G** Memory & state · **H** Distribution/packaging · **I** Safety/least-privilege · **J** Breadth/ecosystem

| Setup | A | B | C | D | E | F | G | H | I | J | Overall | Best at |
|-------|:-:|:-:|:-:|:-:|:-:|:-:|:-:|:-:|:-:|:-:|:------:|---------|
| **fokus (eval baseline)** | 9 | **9** | **9** | 8 | 5 | 3 | 5 | 7 | 4 | 5 | **~7.5** | Discipline + artifact rigor |
| **fokus (now, 2026-05-31)** | 9 | **9** | **9** | 8 | 6 | **8** | 5 | 7 | 5 | 5 | **~7.9** | + Commands shipped, settings/security hardened |
| **fokus (after plugin)** | 9 | **9** | **9** | 8 | 8 | **8** | 5 | **9** | 7 | 5 | **~8.4** | + guard.js, inject-rules, plugin.json |
| **OMC** | 9 | 6 | 5 | 9 | **10** | 9 | 9 | 9 | 6 | 9 | **~8.0** | Hooks + autonomy + infra |
| **claude-flow/ruflo** | 9 | 5 | 4 | 7 | 8 | 7 | **10** | 8 | 7 | **10** | **~7.5** | Memory + scale + ecosystem |
| **wshobson/agents** | 5 | 3 | 2 | **9** | 2 | 8 | 4 | 9 | 6 | **10** | **~6.5** | Breadth + cross-harness distribution |
| **SuperClaude** | 7 | 6 | 6 | 6 | 5 | **9** | 6 | 6 | 6 | 7 | **~6.5** | Command/persona UX |
| **agent-os** | 4 | 7 | 7 | 5 | 3 | 6 | 4 | 5 | 6 | 4 | **~5.5** | Standards alignment |

**Reading it:** fokus *wins outright* on B (process discipline/quality gates) and C (artifact rigor) — its 9s are the highest in those columns. It *loses* on E (hooks), F (commands), G (memory), J (breadth) — exactly the periphery the extraction fixes. OMC edges ahead overall on infrastructure breadth (hooks, memory, distribution, ecosystem) but trails on hard-gated discipline. claude-flow ties on overall via memory+scale but is the least reproducible. The takeaway is unchanged: **fokus owns the discipline/rigor quadrant; borrow E/F/G from the others without inheriting their cost/complexity.**

---

## Part 3 — Recommendation

1. **One marketplace repo, two plugins** (`cc-core` + `cc-dotnet`), self-contained / OMC-independent. `cc-dotnet` declares a **`dependencies: ["cc-core"]`** in its `plugin.json` (the field exists in the spec); vendoring cc-core's files is the fallback if cross-plugin dependency resolution proves unreliable.
2. **Fix P0/P1 gaps *during* extraction** — decouple OMC, strip cross-project permission leakage, add commands, ship a clean committed `settings.json`, parameterize graphify.
3. **Add a `commands/` layer** — turn `be {agent}` into real namespaced slash commands (`/cc-core:architect`, …). Biggest ergonomic win + closes the clearest gap.
4. **Run a 1-day validation spike first** (Part 4) on the two genuine unknowns before bulk extraction: `@`-import resolution from plugin agents, and SessionStart rule-injection.
5. **Keep the disciplined pipeline as the differentiator** — do *not* bolt on OMC-style autonomy loops.
6. **Optional v2:** lightweight memory/state to replace what OMC provided; CI that runs the existing KB/cross-ref lints.

---

## Part 4 — Two-Plugin Architecture

### Repo layout (monorepo marketplace)
```
fokus-cc/                                  # marketplace repo
  .claude-plugin/
    marketplace.json                       # lists cc-core + cc-dotnet via local source paths
  plugins/
    cc-core/                               # PLUGIN 1 — stack-agnostic
      .claude-plugin/plugin.json
      agents/        # 8 agents: opusplan→opus/sonnet; graphify parameterized; OMC refs reworded
      commands/      # NEW: architect, developer, reviewer, team-lead, po, critic, learner, solo
      skills/        # 11 process skills
      rules/         # 10 rules (injected via SessionStart hook — see mechanism)
      conventions/   # 7 artifact formats
      hooks/
        hooks.json   # PreToolUse audit-logger + SessionStart rule-injector, via ${CLAUDE_PLUGIN_ROOT}
        scripts/{audit-logger.js, inject-rules.js}
      templates/     # CLAUDE.md.template + docs/ skeletons (from cc-setup-kit)
    cc-dotnet/                             # PLUGIN 2 — everything
      .claude-plugin/plugin.json           # "dependencies": ["cc-core"]  (fallback: vendor cc-core files)
      skills/        # 27 .NET + Vue code-pattern skills
      conventions/   # csharp, ef-core, vue, testing, project-rules, coding-conventions, known-deviations, kb-entry-schema
      templates/     # filled .NET CLAUDE.md
```

### Key spec facts that shape the design (verified vs official docs)
- **Plugin agent frontmatter supports `model` (haiku/sonnet/opus)** but **NOT** `hooks`, `mcpServers`, or `permissionMode`. Our agents use none of those → fine. **`opusplan` is not a valid plugin-agent model value** — but it is KEPT in the live setup (cost: Opus plans, Sonnet executes; full Opus is far more expensive). *(Decision: KEEP `opusplan` live; when packaging, map `opusplan`→`sonnet`, **NOT** `opus`. Do not move agents to full Opus. Flagged in-file in developer.md/solo.md.)*
- **Always-on rules:** plugins have no auto-loaded `rules/` folder or merged CLAUDE.md. Documented path = a **SessionStart hook** (`type: command`) that emits the concatenated rule text as context. Token cost ≈ today's auto-load. *(Resolves the earlier "P0 unknown" → P1 design decision.)*
- **`marketplace.json`** monorepo entries use `"source": "./plugins/cc-core"` (local path).
- **`${CLAUDE_PLUGIN_ROOT}`** resolves hook/script paths portably.
- **Commands vs skills are unified**; a `commands/architect.md` that instructs the main thread to adopt the architect persona replicates `be architect`.

### Agent execution-model decision (per-context, NOT global — user directive)
Today `be {agent}` makes the **main thread** adopt a persona (per `agent-invocation.md` + the `$CC_AGENT` hook), while team-lead *also* spawns helpers as subagents. As plugin `agents/*.md`, files become **subagents** (separate context, results return to caller).

**User directive: there is no single right model — it depends on the agent and the situation.** Ship the plugin so BOTH modes are available and choose per invocation:
- **Subagent mode** fits when the work is delegated, parallelizable, context-isolating, or model-tiered — e.g. team-lead spawning Explore/critic, or a reviewer doing a bounded pass whose result returns to the orchestrator.
- **Persona mode** (`be {agent}` / a command that makes the *main thread* adopt the role) fits when the human is driving a long interactive session as that role (e.g. sitting *as* the architect or developer), where a separate subagent context would fragment the conversation.

Concretely: ship the 8 files as plugin `agents/` (so subagent mode + auto-routing work), AND ship `commands/` launchers that adopt the persona on the main thread (so persona mode works). Per-agent guidance (which mode is the default for that role) lives in each agent file's header. Validate both flows in the spike rather than committing to one globally.

### OMC decoupling checklist
- `developer.md`/`solo.md`: KEEP `model: opusplan` (flagged in-file). For plugin packaging only, map to `sonnet` (NOT `opus` — cost). Do not move agents to full Opus.
- Drop SessionStart/UserPromptSubmit `echo "be $CC_AGENT"`; replace with native agent-restore (small SessionStart hook reading a `.current-agent` file) or drop.
- Strip `OMC_SKIP_HOOKS` and `mcp__plugin_oh-my-claudecode_t` from settings.
- Reword OMC agent-name references in `pipeline-guardrails.md` + `.claude/README.md` to generic "external orchestration/specialist agents."
- Parameterize `graphify-out/` refs in live `architect.md`/`po.md` (kit already done).
- Remove `D:/src/code-forge` and `git mv *` grants from the shipped `settings.json`.

### Migration steps
1. **Spike (½–1 day):** scaffold a throwaway `cc-core` plugin with 1 agent + 1 rule + audit hook; `claude --plugin-dir ./cc-core`; verify (a) `@`-imports resolve or determine the fix, (b) SessionStart rule-injection appears in context, (c) a command launches the agent.
2. Create marketplace repo + `marketplace.json` + both `plugin.json` files.
3. Build `cc-core`: copy the 8 agents / 10 rules / 7 conventions / 11 process skills / audit hook from `cc-setup-kit` (already classified); apply decoupling checklist; add `commands/`; add rule-injector hook.
4. Build `cc-dotnet`: add 27 .NET/Vue skills + `docs/conventions/*` stack files + filled CLAUDE.md template; wire `dependencies: ["cc-core"]` (or vendor).
5. `claude plugin validate` both; local-install test in a clean (non-OMC) checkout.
6. Publish marketplace; `/plugin marketplace add <repo>` → install both.

### Risks / unknowns to resolve in the spike
- **`@`-import resolution from plugin agents** — main technical risk; if `@docs/...`/`@.claude/conventions/...` don't resolve from a plugin, switch to `${CLAUDE_PLUGIN_ROOT}` references or inline the convention text into agent prompts.
- **Plugin-to-plugin `dependencies`** — field exists; confirm Claude Code installs cc-core when cc-dotnet is installed from the same marketplace; else vendor.
- **Subagent vs persona model** (option A vs B) — confirm the pipeline still flows when agents are subagents.

---

## Part 5 — Improvement Proposals for Below-7 Categories (consolidated)

Consolidates four independent sources: my eval, the **critic** (verified KB healthy, settings leakage real), the **research** agent (plugin spec), and the **OMC architect** (second opinion on weak areas). Below-7 categories: Hooks (5), Settings, Security, Commands — plus borderline Plugin-readiness (7).

### Score corrections from the OMC architect (accepted)
- **Settings 4 → 3** and **Security 4 → 3.** Reason I missed: I fixated on the 15 cross-project `code-forge` grants (cosmetic) but the **real hole is blanket `Bash`/`Write`/`Edit` grants** (`settings.local.json:9-11`) + blanket `mcp__plugin_oh-my-claudecode_t` (`:20`). These make the specific grants *and* the deny-list nearly moot. The deny-list itself is bypassable (`Bash(rm -rf /*)` doesn't stop `rm -rf ~/`, `rm -rf .`, `cd / && rm -rf *`). And audit-logger is `async:true` (`:86`) → observes, never prevents (so it's an *observability* control, not a *security* one — recategorized).
- **Plugin-readiness 7 → 6.** No `plugin.json`/`marketplace.json`; README.md is OMC-coupled throughout (`:1-152`), deeper than first noted.

**Net-new findings the architect surfaced (not in my eval or the critic):**
- **`settings.local.json` is git-TRACKED** → the 15 cross-repo grants + `git mv *` are in *committed history*, not local-only. Root fix is `git rm --cached` + gitignore, not just editing the list.
- **The `cc-setup-kit` carries the same rot** — it ships the OMC-coupled `audit-logger.js` verbatim and has **no `settings.json`, no `hooks.json`, no `commands/`**. The "head start" is thinner than scored; extraction must *build* these fresh, not copy.
- ~~**Audit `agent_type` is dead data**~~ — **RETRACTED (verified false 2026-05-30).** The audit logs show the field correctly captures `agent=oh-my-claudecode:critic`, `agent=main`, etc. — it works. No fix needed. (Both my eval and the OMC architect asserted this; the data refuted it.)
- **Deny-list matcher syntax is wrong** — `Bash(git push*)`/`Bash(docker rm*)` (`:53-54`) use bare-glob form; CC's documented form is `git push:*`. The denies may not fire — a latent gap *inside* the one thing scored "good."
- **No MCP/OMC-tool replacement plan** — `mcp__plugin_oh-my-claudecode_t` (`:20`) grants the whole OMC tool namespace; some agents lean on tools *from* it (`ast_grep_search`, `lsp_diagnostics`). Stripping it without a replacement decision degrades agents silently. **Missed P1.**

**Revised overall ≈ 7.3** (Settings/Security each −1).

### Proposals (prioritized punch-list)

**P0 — live setup now (zero/low risk, also sheds OMC coupling):**
0. **Untrack `settings.local.json`** — `git rm --cached .claude/settings.local.json` + add to `.gitignore`. It's currently committed, so the leak lives in history. *Root fix — do this first.*
1. **Ship a clean committed `.claude/settings.json`** — drop the blanket bare-tool grants (`Bash`/`Write`/`Edit`/`Task`), `mcp__plugin_oh-my-claudecode_t`, the 15 `code-forge cp` grants, `Bash(git mv *)`, and the one-shot `mkdir`/`mv` scaffolding grants. Scoped `allow` (`Read`,`Glob`,`Grep`, `Bash(git status:*|diff:*|log:*|add:*|commit:*)`, `Bash(dotnet build:*|test:*)`, `Bash(npm run build:*|test:*)`). Deny `Read(./**/*.env)`, `secrets/**`, `Edit/Write(D:/src/code-forge/**)`. **Fix the deny-matcher syntax** (`git push:*`, `docker rm:*` — current bare-glob form may not match). Deny precedence then protects against any future local re-grant. **Biggest single risk reduction.**
2. **Harden the deny-list** — add `rm -rf ~`, `rm -rf ~/*`, `rm -rf .`, `rm -rf *`, `git reset --hard*`, `curl*| sh`, `*| bash`, and secret-file reads (`Read(./**/.env)`, `*.pem`, `secrets*`).
3. **Remove the `UserPromptSubmit: echo "be $CC_AGENT"` hook** — dead OMC noise every turn.
4. **Create `.claude/commands/`** — 8 agent launchers (`architect`, `dev`, `review`, `po`, `critic`, `learner`, `solo`, `team-lead`) each = "read `.claude/agents/{x}.md` and adopt the role," + `status.md` (inline `` !`git status` `` + backlog) + `pipeline.md` ($ARGUMENTS → team-lead). Removes a `$CC_AGENT` dependency; highest UX/portability leverage. (Commands 3→~8.)
5. **`.gitignore`** `docs/audit/` and `.claude/.current-agent` (audit logs hold paths/args; shouldn't accrue in the repo).

**P1 — during extraction (needed for self-contained plugins):**
6. **`SessionStart inject-rules.js`** — concatenate `rules/*.md` → emit as context via `${CLAUDE_PLUGIN_ROOT}`. The mechanism that delivers always-on rules from a plugin.
7. **`SessionStart restore-agent.js`** — read a gitignored `.claude/.current-agent` file (written by the command launchers) → re-adopt role. OMC-free replacement for `$CC_AGENT`, survives `/clear`,`/compact`.
7b. **Decide OMC-tool replacements** — before stripping `mcp__plugin_oh-my-claudecode_t`, audit which agents actually call OMC tools (`ast_grep_search`, `lsp_diagnostics`, etc.) and either map them to native equivalents (Grep/Glob, an LSP MCP) or remove the references. Otherwise agents degrade silently in a non-OMC env. Also: `opusplan` → `opus` in `developer.md:4`/`solo.md:4` and sync `agents-workflow.md` + `README.md`.
8. **`PreToolUse guard.js`** (matcher `Bash`, **non-async**, fail-open) — normalize paths and block what static globs can't: recursive force-deletes outside the repo, history rewrites, `| sh`/`| bash` pipes, `chmod 777`, writes outside repo root. Logs blocks to `docs/audit/_blocked.log`. The headline *preventive* safety layer (and a plugin selling point). Hooks 5→~8, Security →7.
9. **Move hooks to committed `settings.json`**; ship `settings.json` (cc-dotnet) + `settings.json.template` (cc-core), both OMC-free.
10. **Rewrite README.md + pipeline-guardrails.md** to drop OMC references; **parameterize graphify** in `architect.md`/`po.md`.

**P2 — polish:**
11. **`PostToolUse post-edit-verify.js`** (matcher `Write|Edit`) — nudge build/lint per project-rules after source edits (closes the verification loop).
12. **`SubagentStop verify-deliverable.js`** — confirm the expected pipeline artifact (plan.md/implementation.md/review.md) exists when an agent finishes; fits this setup's artifact discipline.
13. **`plugin.json` + `marketplace.json`** (proper packaging). 14. **Audit-log rotation/retention.**

### Realized status (2026-05-31)

**Done in the live setup:**
- **Commands** — `.claude/commands/` 9 launchers shipped (`3 → 8`, target met).
- **Security baseline** — `.claude/settings.json` committed with a catastrophic/remote deny-list; `.gitignore` ignores `docs/audit/` + `.claude/.current-agent`.
- **Hooks** — dead `$CC_AGENT` hooks dropped; `restore-agent.js` (SessionStart persona-restore) added; `audit-logger.js` kept (observe-only).
- **`opusplan`** — KEPT + flagged in `developer.md`/`solo.md` (map to `sonnet` when packaging; never full Opus — cost).

**Settings two-file split (finalized):**
- `settings.json` (committed, team) = **deny list only** — guardrails everyone gets; deny always wins, can't be un-denied by a local allow.
- `settings.local.json` (gitignored, personal) = **allow + env + hooks** — the "allow everything"/Open posture (bare `Bash`), OMC env, Atlassian access. Cleaned 43 → 16 lines: removed 27 dead `cp`/`mv`/`mkdir`/`git mv` path-grants (redundant under bare `Bash`); collapsed 5 Atlassian tool grants → one whole-server `mcp__claude_ai_Atlassian`; `deny: []` (deny lives in `settings.json`).
- **`Bash(git mv:*)` removed from deny** — benign local refactor (tracked rename), miscategorized next to `rm -rf`/`sudo`. Deny now = catastrophic-local + remote (`git push`) + exfil + cross-repo.
- **Pending user y/n:** add remote denies `curl|sh`, `wget|sh`, `npm publish`, `docker push` (thicken the "remote" category). JSON allows no comments → group by ordering only.

**Realized scores (vs projected):** Commands `3→8` ✅ · Settings `3→~7` · Security `3→~5` · Hooks `5→~6` · Plugin-readiness still `6`. **Remaining gains ship *with* the plugin** (guard.js, inject-rules.js, scoped Hardened settings, plugin.json) — not worth doing standalone.

**Pending chores:** user pastes clean `settings.local.json` → untrack it (`git rm --cached`) → commit batch (branch off `main`) → spike.

### Security install modes (decided 2026-05-31, verified vs plugin docs)
Ship 3 install modes — "allow everything" maps to **Open**, not Off:

| Mode | Prompts | `guard.js` blocks | Allow-list | Who |
|------|:--:|---|:--:|-----|
| **Open** *(default)* | none | catastrophic only (`rm` outside repo, force-push, `\|sh`, secret reads, cross-repo writes) | broad | most devs |
| **Hardened** | on novel cmds | strict | scoped | teams, CI, regulated |
| **Off** | none | nothing | broad | "don't interfere" |

**Mechanism (verified):** a plugin's bundled `settings.json` honors only `agent`/`subagentStatusLine` keys — **NOT** `permissions`. So:
- **Enforcement** = ship `guard.js` as a PreToolUse hook (synchronous, returns block — unlike async audit-logger which only records). Reads a mode file, adjusts strictness.
- **Mode picker** = native `userConfig` prompt at plugin-enable, OR a `/cc-core:setup` command.
- **Hardened allow-list** = setup command writes into host `.claude/settings.json` (permissions reload live, no restart). Deny>allow precedence confirmed, merged across scopes.

**Settings precedence facts (verified vs settings docs):** rules eval order = deny → ask → allow, first match wins; permissions **merge** across scopes (not override) so a local allow can NOT un-deny a project deny; for scalar settings local > project > user; `settings.local.json` is auto-gitignored by CC on creation; live-reload applies to `permissions`/`hooks` without restart (`model`/`outputStyle` need restart). Plugin-bundled `settings.json` honors only `agent`/`subagentStatusLine`.

### Expected score movement after P0+P1
Commands 3→8, Settings 3→8, Security 3→7, Hooks 5→8, Plugin-readiness 6→9 → **overall ≈ 8.3-8.5**, with the discipline/rigor core untouched at 9.

---

## Verification
- `claude --plugin-dir ./plugins/cc-core` loads with no error; `/help` lists namespaced commands.
- `/cc-core:architect` activates the persona with zero `$CC_AGENT`/OMC dependency.
- Audit-logger writes via `${CLAUDE_PLUGIN_ROOT}` on a tool call; rules appear in context at session start.
- `cc-dotnet` exposes .NET skills; `cc-core` alone does not.
- Fresh clone in a non-OMC env: no dead `$CC_AGENT`, no `opusplan` resolution failure, no graphify dead paths, no cross-project permission grants.
- `claude plugin validate` passes for both.
