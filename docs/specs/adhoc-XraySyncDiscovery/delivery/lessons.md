# Xray Sync: Project-Wide TE Discovery — Lessons

## Developer Lessons

- When removing a method from a service, grep the entire codebase for callers — plan only listed the primary call sites (SyncXrayEndpoint, XrayIssueSyncService), but SprintIssueSyncService had a piggyback call that also needed updating.
- `GraphQLXrayClient` accumulated methods beyond the interface (e.g. `GetTeKeysForTestCasesAsync` was public but not on the interface). Check both the interface and the implementation class for dead code when removing a method.
- File-lock MSB errors during `dotnet build` on Windows mean the app is running and holding output DLLs — they are not compilation errors. Filter with `grep " error CS"` to distinguish CS compiler errors from MSB copy errors.
- The Write tool requires the file to have been Read in the current context window, even if it was read earlier in the session. Use Edit for replacing full file content when a prior Read has been evicted from context.

## Architect Lessons

- When a plan removes a public method, the plan must grep for all callers across the codebase — not just the known call sites. The developer correctly caught `SprintIssueSyncService` as an undocumented caller of `SyncXrayForIssuesAsync`. Future plans that remove methods should include a "Verify: grep for all callers of {method}" sub-step and list all known call sites explicitly.
- Step 5 (dead code removal) was folded into Steps 1-3 by the developer rather than done as a separate pass. This is fine — the plan's step ordering was a suggestion for logical grouping, not a hard sequence. The developer's approach of removing dead code alongside the replacement code that makes it dead is actually cleaner (avoids a temporary state where both old and new code coexist).
- The `knownTicketKeys` loading approach (Q1) was under-specified in the original plan — "loaded from DB before calling" without naming the repository or method. The updated plan with explicit `TicketRepository.GetExistingKeysAsync` guidance was necessary. Plans should always name the concrete type and method when referencing existing infrastructure, not leave it implicit.

## Reviewer Lessons

- KB Impact sections in plans are hard requirements, not suggestions — verify KB files were actually updated, not just that the plan listed them. The sync flow section in `docs/kb/xray.md` described the old Jira-driven path even after the full refactor to Xray-driven discovery.
- String interpolation into external API query strings (GraphQL JQL, SQL fragments) should be flagged even when the source is a trusted config value — the pattern propagates and the source can change. A format guard on project key format is the minimum bar.
- When a plan removes a method from a service interface, grep for all implementations too — `GraphQLXrayClient` had `GetTeKeysForTestCasesAsync` as public but not on the interface. The developer caught it; the reviewer should independently verify via grep on the implementation file.
- Non-fatal warning lists that are silently discarded (e.g., `SyncTestSetsFromIssuesAsync` internal `warnings`) are a recurring gap pattern worth calling out explicitly in Gap Analysis even when pre-existing.

## Skill Gaps

- **Missing skill:** No skill for "refactor sync service" or "update caller sites when removing a method" — the plan correctly flagged no matching skill, but the pattern of searching for all callers before removal is worth capturing in a future skill or checklist.
