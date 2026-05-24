# F34-DeveloperDetailPage — Lessons

## Developer Lessons

- **`dotnet build -q` masks a spurious MSB3492 stale-cache error on Fokus.API.** The `-q` flag suppresses the full output but still exits non-zero on the cache error. Full build (without `-q`) succeeds cleanly. Reliable pattern: build Fokus.Persistence first, then Fokus.API without `-q`, check for "Build succeeded." at the bottom.

- **Pre-existing TypeScript unused-import errors block the frontend build.** Three files (`DeveloperProgressCard.vue`, `QaWorkloadMetricCards.vue`, `UsersTab.vue`) had unused imports that were not caught before because they were not part of the build path previously. When wiring new components into the view caused these files to be included in the full build graph, they surfaced. Fix: remove unused imports as part of the step that first triggers the full build (Steps 6-8 wiring).

- **`JiraOptions` is not in Fokus GlobalUsings — add explicit usings.** `JiraOptions` lives in `Jira.RestApi` namespace and `IOptions<T>` lives in `Microsoft.Extensions.Options`. Neither is in the service-level GlobalUsings. Pattern confirmed from `SyncXrayEndpoint.cs`. Always add explicit usings for these two when injecting `IOptions<JiraOptions>`.

- **Variable name collision in nested scopes with `out var`.** When a method has `var tickets = BuildTicketDetails(...)` in an outer scope and later calls `completionsByDay.TryGetValue(day, out var tickets)` in an inner loop, CS0136 fires. Fix: rename the `out var` to something more specific (e.g., `out var dayCompletions`).

- **Rolling average diverges between DeveloperDetail and Throughput.** Throughput computes rolling average over feature-only SP; DeveloperDetail computes it over all-types SP. Both use the capacity-aware skip pattern. When composing patterns across services, check which SP scope is intended — the pattern is the same but the input differs.

- **`AppSettingsConfiguration.cs` needs no changes for simple scalar decimal properties.** EF Core auto-maps `decimal` scalars. Only complex types (value objects, JSON columns, owned entities) require explicit configuration in the entity type config.

## Architect Lessons

- **Critic review justified on complex features.** The critic caught the "(all types)" label gap and the 204-vs-200 status code inconsistency before implementation. Both were addressed in the plan, and the developer implemented them correctly without confusion. Complex multi-area features benefit from critic mode over self-review.
- **Acknowledged-divergence pattern works well.** Noting codebase-pattern-over-spec in the plan (200 vs 204) with explicit reasoning gave the developer clear direction. The implementation correctly followed the codebase pattern and documented the same reasoning. No ambiguity at review time.
- **Medium-confidence step with line-number references succeeded.** Step 3 was flagged Medium confidence (composing 3 services). The pattern references with specific line numbers (LeaderboardService line 287, DeveloperProgressService line 367) gave the developer enough anchor points. All computation rules were implemented correctly.
- **KB Impact tracking in plan produces results.** This is the first feature where KB updates were explicitly listed in the plan. Both `settings.md` update and new `developer-detail.md` entry were created, and the KB index was updated. Making KB updates a visible plan deliverable eliminates the "forgot to update KB" failure mode.

## Reviewer Lessons

- **Migration default value is a recurring gap in EF Core + SQLite features.** EF Core scaffolds `defaultValue: 0m` for decimals in the migration `Up()` method, but the domain default may be different (e.g., 30 for BugRatioTarget). Always compare the migration `defaultValue` against the domain entity's property initializer and `CreateDefault()` factory. This is a one-line fix that prevents incorrect data for existing rows on upgrade.

- **Rolling average look-back operates on the pre-exclusion list — subtle but inconsequential.** The service computes the rolling average before applying the sprint inclusion `continue` guard. In this case the `capacity == 0` skip inside `ComputeRollingAverage` covers the same condition, making the behavior correct. Future reviewers: when a service computes a secondary metric (like rolling average) inside a loop that later has a `continue`, check whether the secondary metric window can be distorted by sprints that are eventually excluded.

- **`border-l-2` on `<tr>` is a Tailwind/CSS table trap.** CSS borders on `<tr>` elements are not reliably rendered across browsers when `border-collapse` is active (Tailwind's Preflight sets this). Prefer applying left-border indicators to the first `<td>` in the row or using a background color on the row instead.

- **Pre-existing warnings/errors in adjacent files should be flagged in implementation.md even when they pre-date the feature.** The developer correctly documented fixing three unused-import TypeScript errors. This is good practice — always document what was fixed that was not strictly part of the feature scope, so reviewers don't attribute them as regressions.

- **TakeLast before vs after exclusion guard is a recurring analytics pitfall.** When a service filters a list with `TakeLast(n)` and then applies a secondary exclusion guard inside the loop, the returned count can be less than `n`. Pattern to check on any analytics service with a `last` parameter: confirm the exclusion guard runs before `TakeLast`, not after. Fix pattern: accumulate into `allEntries`, apply `TakeLast` at the end.

- **Codex cross-validation found the TakeLast/exclusion ordering bug that Sonnet missed.** The Sonnet review focused on the rolling-average pre-exclusion inconsistency (low practical impact) and did not independently identify that `last=5` could return fewer than 5 sprints. Codex caught it as a separate, higher-impact finding. Cross-validation is worth enabling on features with non-trivial filtering/pagination logic.

## Skill Gaps

- **Missing skill: metric computation service patterns.** Step 3 (`DeveloperDetailService`) had no matching skill. The service composes patterns from three existing services (Leaderboard, DeveloperProgress, DeveloperThroughput) with different scope models. A skill covering: how to compose cross-sprint analytics, rolling average variants, ticket state derivation, and business day counting would reduce exploration time and risk of subtle divergences.
  - Suggested name: `analytics-computation`
  - Coverage: rolling average (all-types vs feature-only), sprint inclusion rules, ticket state derivation (started/stalled/done/not-started), business day counting, capacity-aware computation
  - Reference files used: `LeaderboardService.cs`, `DeveloperProgressService.cs`, `DeveloperThroughputService.cs`, `TransitionAttributionChecker.cs`
