# F38 code review — Codex (primary)

First review — 2026-10-13 — persisted by the architect session from the Codex job's collected output (job task-mv76113q-lp17rf, completed).

## Verdict: GO

No blocking implementation defect found. Five LOW documentation findings remain:

- **LOW — [.claude/skills/persistence-patterns/SKILL.md:173](D:/src/fokus/.claude/skills/persistence-patterns/SKILL.md:173):** Table names follow `DbSet` names (`Sprints`, `Tickets`, `AppUsers`), rather than singular entity names; columns follow property mappings. **Suggested fix:** describe the actual naming conventions.
- **LOW — [.claude/skills/add-hosted-service/SKILL.md:17](D:/src/fokus/.claude/skills/add-hosted-service/SKILL.md:17):** The claim that modules use `ValidateOnStart()` overstates the implementation: Jira validates at startup; Xray only binds optional options. **Suggested fix:** distinguish those registrations.
- **LOW — [src/Services/Fokus/CLAUDE.md:42](D:/src/fokus/src/Services/Fokus/CLAUDE.md:42):** “Every other endpoint takes authentication only” omits anonymous sign-in and invitation validation. **Suggested fix:** name both exceptions.
- **LOW — [src/Services/Fokus/CLAUDE.md:39](D:/src/fokus/src/Services/Fokus/CLAUDE.md:39):** The “no behavior methods” statement remains stale; Auth behavior methods already exist. **Suggested fix:** correct it during the recorded F36 refresh.
- **LOW — [plan.md:298](D:/src/fokus/docs/specs/F38-DotnetSkillAlignment/delivery/plan.md:298):** The approved error-body changes omit formerly empty 404 responses becoming middleware envelopes with messages. **Suggested fix:** explicitly include the converted lookups and both developer-detail rule branches.

Each Carry-Over Findings row was checked:

| Row | Disposition |
|---|---|
| `UpdateTeamConfig` missing developer returns 500 | **Refuted on the current tree:** the awaited value is correctly parenthesized before the guard. |
| Stale “Thin domain” guidance | **Confirmed:** remains deferred to F36. |
| FastEndpoints 8 runtime unexercised | **Partially resolved:** startup, registration and Swagger are recorded; signed-in operator checks remain pending. |
| Formerly empty 404 responses gain bodies | **Confirmed:** statuses remain 404; envelopes, messages and Error logging are added. |

Verification found identical 56 route/verb pairs, unchanged resolved request/success JSON schemas, unchanged 294 request/response/DTO/validator types after intended renames, and identical 23 admin gates. Installed FastEndpoints 8.3 IL confirms policy attachment to ASP.NET authorization and pre-processor exception propagation to middleware, consistent with its [execution source](https://raw.githubusercontent.com/FastEndpoints/FastEndpoints/v8.3/Src/Library/Endpoint/Endpoint.cs). No incompatible FluentValidation 12 API usage was found. Startup seeding and Xray credential assignments preserve baseline behavior.

No files were modified. The suite was not rerun; the supplied complete passing run was accepted.

