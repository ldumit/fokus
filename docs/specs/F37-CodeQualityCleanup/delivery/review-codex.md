# F37-CodeQualityCleanup — Codex code review

Job task-mv71o2r5-ir60fx, persisted by team-lead (session 80d534fb), 2026-10-13. Read-only run.

## Verdict: NO-GO

1. **MEDIUM — [UsersTab.vue:39](/D:/src/fokus/client/src/components/settings/UsersTab.vue:39):** Load failures share `userMgmtError` with mutations. When users load successfully but invitations fail, changing a role or Active status clears the error while `invitationsLoadFailed` remains true. Reproduced with the component’s functions and mocked requests. **Fix:** separate load errors from mutation errors; clear each load error only after its successful reload.

2. **MEDIUM — [GetDeveloperQualityEndpoint.cs:31](/D:/src/fokus/src/Services/Fokus/Fokus.API/Features/Analytics/GetDeveloperQuality/GetDeveloperQualityEndpoint.cs:31), [GetQaWorkloadEndpoint.cs:30](/D:/src/fokus/src/Services/Fokus/Fokus.API/Features/Analytics/GetQaWorkload/GetQaWorkloadEndpoint.cs:30):** Both new sprint-ID checks occur after the Xray-disabled return. A nonexistent explicit ID therefore still returns 200 when Xray is disabled, contrary to Step 6’s unconditional 404 requirement. **Fix:** validate explicit IDs before that return, retaining empty 200 responses for valid IDs and requests without an ID.

3. **MEDIUM — [implementation.md:189](/D:/src/fokus/docs/specs/F37-CodeQualityCleanup/delivery/implementation.md:189):** **Confirmed carry-over:** Step 12’s operator check remains outstanding. The plan explicitly makes it a merge gate. **Fix:** perform and record the required page, failure-path and cookie checks before merging, applying Q1’s active-sprint exclusions.

4. **LOW — [implementation.md:188](/D:/src/fokus/docs/specs/F37-CodeQualityCleanup/delivery/implementation.md:188):** **Confirmed carry-over:** the graph refresh was not performed. The graph predates F37, and the communication log records the missing CLI. **Fix:** make the CLI available and have the main session run `graphify update .`.

The remaining Carry-Over Findings row—active-sprint membership gaps on Carry-Over, Bug Ratio and Leaderboard—is **confirmed but excluded from findings**, as required by Q1.

Used the supplied complete-suite pass record; did not rerun the suite or modify any file.
