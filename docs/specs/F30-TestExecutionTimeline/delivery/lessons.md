# Test Execution Timeline — Lessons

## Developer Lessons

- **SprintMembership.AddedAt is non-nullable DateTime.** The plan and spec describe it without specifying nullability. Always check the domain entity directly before writing date arithmetic — `m.AddedAt.HasValue` will cause CS1061 if the field is non-nullable.
- **Namespace isolation in FastEndpoints features.** Endpoints auto-discover by convention but namespaces are isolated per feature folder. When an endpoint in `Features/Sprints/X/` needs types from `Features/Analytics/`, GlobalUsings won't cover it. Add an explicit `using` at the top of the file.
- **Static helpers avoid unnecessary DI coupling.** When a computation is needed in two endpoints with different DI graphs, making it a public static method (as done with `ComputeCrunchFlag`) avoids injecting a heavy service into a context that only needs one function. Check whether the method needs instance state before defaulting to DI injection.
- **CS9113 unread parameter warning catches dead DI.** The compiler correctly flagged the unused `testTimelineService` constructor parameter. Treating warnings as signals during build verification caught a real design mistake (plan said inject, but static call was correct).
- **sprintEndDayNumber derived in view, not in response.** Rather than adding a redundant field to the API response (the dates are already there), the view computes it as a `computed()`. Keeps the backend response lean and avoids a second source of truth.

## Architect Lessons

- When a plan step says "Add X as a dependency OR use a static helper," the developer will pick the simpler option (static). This is fine — offering alternatives in the plan works well when the decision genuinely does not matter architecturally. Keep doing this for implementation-level choices.
- The plan's endpoint placement decision (Features/Sprints/ vs Features/Analytics/) was called out as an assumption in Open Questions. The developer followed it without friction. Documenting placement rationale in the plan prevents unnecessary questions.
- Pure computation services with a static helper method for a subset of their logic (ComputeCrunchFlag) is a clean pattern for when multiple endpoints need a slice of the same computation without the full orchestration. Worth noting as a reusable pattern for future features.
- SprintMembership.AddedAt is non-nullable (DateTime, not DateTime?) — the developer discovered this during implementation. Future plans involving AddedAt should not suggest null-guarding it.

## Skill Gaps

- **Missing skill: metric computation service pattern.** Steps 1 and 3 had no matching skill. A skill covering the pattern of records-at-top + service class + private helpers for each sub-computation (as used by QaMetricsService, QaWorkloadService, TestTimelineService) would reduce exploration time for future analytics features. Suggested name: `analytics-computation-service`. Reference files: `Features/Analytics/QaMetricsService.cs`, `Features/Analytics/QaWorkloadService.cs`, `Features/Analytics/TestTimelineService.cs`.
- **Missing skill: frontend type/store patterns.** Step 4 had no matching skill. A skill for adding TypeScript interfaces to `types/index.ts`, API functions to `api/{resource}.ts`, and store refs/fetches to a Pinia store would reduce boilerplate. Suggested name: `frontend-data-layer`. Reference files: `client/src/types/index.ts`, `client/src/api/analytics.ts`, `client/src/stores/sprintsStore.ts`.
