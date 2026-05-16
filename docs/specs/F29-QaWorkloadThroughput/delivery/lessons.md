# QA Workload & Throughput — Lessons

## Developer Lessons

- `string?` as Dictionary key with a custom `IEqualityComparer<string?>` works at runtime but produces CS8714 warnings from the compiler. These are informational warnings only and do not affect correctness. The pattern is needed when null is a valid business key (Unassigned row).
- Reference type records (`public record Foo(...)`) use `is not null` for nullable checks, not `.HasValue`/`.Value` (those are for `Nullable<T>` value types). Confusing these causes CS1061 errors.
- When a plan says "inline delta with polarity", verify whether the backend record actually returns a polarity field. `QaWorkloadSingleEntry` stores only `*Delta` and `*Direction` — no separate `*Polarity` field. If colored delta indicators are needed client-side, a polarity field must be added to both the backend record and DTO, or polarity must be inferred client-side from the column's known direction convention.
- The ApexCharts horizontal bar chart requires `plotOptions.bar.horizontal: true` inside the options object (not a top-level chart type change). The `type: 'bar'` stays the same.

## Skill Gaps

- **Missing skill:** No skill for analytics metric computation services (pure computation, multi/single split, delta helpers, sparkline builders). Suggested name: `create-analytics-service`. Coverage: service class skeleton with multi/single methods, DeltaDirection/DeltaPolarity helpers, BuildMetricCard pattern, alert evaluation loop. Reference files: `BugRatioService.cs`, `DeveloperQualityService.cs`, `QaWorkloadService.cs`.
- **Missing skill:** No skill for frontend developer tab integration (adding a tab button to DevelopersView, URL sync extension, store wiring). All five F2x features follow the same pattern. Suggested name: `add-developers-tab`. Coverage: store extension checklist, DevelopersView tab button template, onMounted URL seed pattern, watch extension.
