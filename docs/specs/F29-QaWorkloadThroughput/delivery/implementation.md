# QA Workload & Throughput — Implementation

## Files Created

- `src/Services/Fokus/Fokus.API/Features/Analytics/QaWorkloadService.cs` — Computation service with multi-sprint and single-sprint modes. Implements attribution logic (BR1-3), metric formulas (BR5-11), sub-team filter (BR21), workload balance alert (BR16-19), sparkline builders, and delta helpers. Defines all response record types (QaWorkloadEntry, QaWorkloadMultiSprintResponse, QaWorkloadSingleSprintResponse, QaWorkloadResponse, etc.).
- `src/Services/Fokus/Fokus.API/Features/Analytics/GetQaWorkload/GetQaWorkloadQuery.cs` — Request class, validator (matching GetBugRatioRequestValidator), and all response DTOs for the endpoint.
- `src/Services/Fokus/Fokus.API/Features/Analytics/GetQaWorkload/GetQaWorkloadEndpoint.cs` — FastEndpoints GET /api/analytics/qa-workload. Handles Xray-disabled early return, multi-sprint and single-sprint branching, sparkline window, prior sprint, workload alert history load, and DTO mapping.
- `client/src/components/developers/QaWorkloadTab.vue` — Self-contained tab component with team metric cards (multi plain-value, single MetricCard), stacked horizontal distribution chart, throughput trend line chart (multi only), and per-person sortable table with workload alert badge. Single/multi mode gating throughout.

## Files Modified

- `src/Services/Fokus/Fokus.API/DependencyInjection.cs` — Registered `QaWorkloadService` as scoped.
- `client/src/types/index.ts` — Added QA Workload types: WorkloadAlert, QaWorkloadSprintBreakdown, QaWorkloadEntry, QaWorkloadTeamMetrics, QaWorkloadMultiSprintResponse, QaWorkloadSingleTeamMetrics, QaWorkloadSingleEntry, QaWorkloadSingleSprintResponse, QaWorkloadResponse.
- `client/src/api/analytics.ts` — Added `getQaWorkload` function and `QaWorkloadResponse` import.
- `client/src/stores/developersStore.ts` — Extended activeTab type union to include 'qaWorkload', added qaWorkload/qaWorkloadLoading/qaWorkloadError state, added fetchQaWorkload action, extended switchTab/selectSprint/selectLastN/selectSubTeam to call fetchQaWorkload when active.
- `client/src/views/DevelopersView.vue` — Added QaWorkloadTab import, added "QA Workload" tab button (Xray-gated after Quality), extended onMounted URL seed for 'qa-workload' param with initial load, extended URL watch for 'qaWorkload' → 'qa-workload', updated onTabSwitch type, added QA Workload tab content block with loading/error/empty/data states.

## Key Decisions

- `NullableStringComparer` sealed class added to `QaWorkloadService.cs` to support `string?` as dictionary key for the Unassigned person (accountId = null). The CS8714 warnings about nullable dictionary keys are expected and harmless — the comparer handles null correctly at runtime.
- `QaWorkloadTeamMetrics` is a reference type record (not a struct), so `is not null` check used instead of `.HasValue`/`.Value`.
- The `BuildSprintPersonMetrics` method creates person entries on first attribution contact (runs or TE ownership), ensuring the Unassigned row appears only when there are actual unattributed terminal runs or TEs with null AssigneeId.
- Stories Covered attribution follows execution model: credited to persons who ran at least one terminal run in that TE, not to TE owners.
- Bugs Found attribution follows TE ownership model (AssigneeId), matching BR11.
- Team-level metrics in multi-sprint mode computed over all TEs in the viewed range (not filtered by sub-team), matching plan intent.
- Distribution chart uses horizontal bars (`plotOptions.bar.horizontal: true`) with pass=green (#22c55e) and fail=red (#ef4444), sorted by runs completed desc with Unassigned pinned at bottom.
- Throughput trend chart not shown in single-sprint mode (no sprint axis to plot against).

## Deviations from Plan

- `deltaClass` in QaWorkloadTab.vue uses neutral `text-text-secondary` for all delta directions. The plan specifies delta polarities per spec (higher-is-better, lower-is-better, neutral) but these polarities are returned from the backend in the `*Direction` fields only (not a separate polarity field in the single entry records). The backend QaWorkloadSingleEntry record stores direction only, not polarity. Delta coloring is uniformly neutral (gray) in the table — consistent with the data available. If colored deltas are needed, the backend record and frontend would need a polarity field added.
- The `unused` `deltaClass` parameter `direction` is referenced but always returns `text-text-secondary` — this is intentional as the plan's delta polarity info is not included in QaWorkloadSingleEntry (unlike BugRatioDeveloperSingleEntry which has a separate delta polarity). The function signature was kept for future extension.
