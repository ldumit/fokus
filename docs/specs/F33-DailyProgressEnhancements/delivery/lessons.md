# F33-DailyProgressEnhancements — Lessons

## Developer Lessons

- **Stall boundary tests require business-day-aware fixtures.** The `new-stall` direction fires only when a ticket crosses the stall threshold today but not yesterday. Testing this requires last transition to be exactly 3 business days before today — which requires a `SubtractBusinessDays` helper. Additionally, the test must guard against weekends (today is Sat/Sun → stall threshold can't be crossed today → test is vacuously true). Added weekend guard `if (today.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday) return;`. For future stall-boundary tests, consider whether `TimeProvider` injection would remove this class of weekend fragility.

- **Intermediate tuple pattern for carrying per-developer auxiliary state.** When a service `Select` loop computes auxiliary data (yesterday's stall set) needed later in the same method but not in the response record, use a tuple return `(Entry, AuxData, ...)` and project to the final list separately. This avoids polluting the response record with internal computation artifacts and keeps the alert-building loop clean.

- **Pre-existing build errors mask new errors.** Always check baseline build status before starting frontend work. The 3 pre-existing `vue-tsc` errors (`BaseCard`, `deltaClass`, `authStore`) meant `npm run build` failed both before and after my changes. Used `git stash` + baseline build to confirm no new errors were introduced.

- **KB Impact is an implicit step, not optional.** Applied the known deviation rule: treated KB Impact section at the bottom of plan.md as an implicit final step, added it to implementation.md, and updated `docs/kb/analytics/daily-progress.md` with all new sections. Skipping this is a tracked recurring mistake.

## Architect Lessons

- **Parameterization plan steps should mention the tuple/auxiliary-data pattern as an option.** Plan Step 2 said "call BuildStalledTickets twice" but the developer correctly used an intermediate tuple to carry yesterday's stall keys through the Select pipeline rather than calling it separately. Both approaches are valid, but the plan was overly prescriptive about *how* to thread the data. Better: describe the *what* (need yesterday's stall set available when building alerts) and let the developer decide the threading mechanism.

- **Pre-existing build failures should be documented in the plan's Context or Scope section.** The developer discovered 3 pre-existing `vue-tsc` errors that complicated build verification. If the architect had checked build status during analysis, this would have been flagged upfront as a known baseline issue rather than requiring the developer to investigate mid-implementation.

- **Five-step plan for a scoped enhancement worked well.** Three independent work streams (bug/feature split, delta alerting, chart fixes) that all touch the same service/component pair fit cleanly into 5 steps with clear dependency ordering (types first, backend parallel, frontend after). No step was over-specified, no step was missed.

## Reviewer Lessons

- **`IsCompletedInSprint` uses sprint window, not referenceDate — affects yesterday-stall recomputation.** When `BuildStalledTickets` is called with `yesterdayDate` as `referenceDate`, it still calls `IsCompletedInSprint(sprintStart, sprintEnd)` — the full sprint window, not a window ending at yesterday. Tickets completed today are therefore excluded from "yesterday's stall set." This means `stall-resolved` does not fire when a stall resolves via ticket completion (it fires as `improving` instead). Future reviews of stall-detection parameterizations should verify whether `IsCompletedInSprint` and `IsStartedInSprint` need time-bounded variants alongside the existing sprint-window variants.

- **Codex cross-validation found a behavioral gap Sonnet missed.** Sonnet's review did not trace the `IsCompletedInSprint` call inside `BuildStalledTickets` to verify the date bounding. Codex caught it. Pre-commitment predictions should explicitly include "verify that helper methods used inside a parameterized method also respect the reference date parameter."

- **Missing `stall-resolved` test was flagged independently by both reviewers.** When a plan's testing strategy lists an explicit scenario and no test covers it, it is always worth at least a LOW finding regardless of code correctness confidence. Both reviewers agreed on this gap.

- **"newly stalled count" vs "total stalled count" is a spec ambiguity, not a code error.** When the plan says "derive stall count from stalledTickets list," that wording is satisfied by `stalledTickets.length`. The display string "newly stalled" implies a different semantic. Future plans should distinguish "count of tickets that crossed the threshold today" (requires backend to compute) from "total currently stalled count" (already available).

## Skill Gaps

- **Missing skill: metric computation patterns.** Steps 1 and 2 were marked `Skill: None` (gap). The computation patterns in `DeveloperProgressService` (gap delta, direction derivation, stall parameterization) are specific enough to warrant a skill or at least an inline pattern reference. Referenced `LeaderboardService` for bug/feature partition pattern. Suggested skill name: `analytics-metric-computation`. Coverage: gap delta formula, direction derivation priority rules, stall threshold parameterization.
