# Codex code review — F36 Phase 1

Persisted by the architect (main session) from the read-only Codex job.

## Verdict: NO-GO

- **MEDIUM — [.claude/settings.json:26](/D:/src/fokus/.claude/settings.json:26), also line 36:** Both hook commands depend on the current directory. From `client/`, the restore command fails with `MODULE_NOT_FOUND`; the audit command has the same path problem. **Fix:** Anchor both script paths to the project root and quote them, following [Claude’s hook-path guidance](https://code.claude.com/docs/en/hooks#reference-scripts-by-path).

Carry-over findings:

- **MEDIUM — [implementation.md:83](/D:/src/fokus/docs/specs/F36-PublicReleaseCleanup/delivery/implementation.md:83): Not independently confirmed.** The six high-severity npm findings are asserted without retained audit output. The supplied complete-run record verifies builds and tests; the registry lookup was blocked by network restrictions. **Fix:** Retain an audit report identifying affected packages and advisories during Phase 2 triage.
- **LOW — `docs/specs/GAP-1-excluded-statuses-ui/definition/spec.md:5`, `GAP-2-sprint-range-picker/definition/spec.md:5`, `GAP-3-sub-team-management-ui/definition/spec.md:5`: Confirmed.** All three still say `Open`, while the backlog says `Done (via I11)`. **Fix:** Reconcile the spec statuses in the documentation follow-up.
- **LOW — [adhoc-PipelineCraftImprovements/delivery/plan.md:3](/D:/src/fokus/docs/specs/adhoc-PipelineCraftImprovements/delivery/plan.md:3), `docs/skill-backlog.md:68`, `docs/conventions/known-deviations.md:10`: Confirmed.** Plain references to excluded records remain; these are historical mentions, not broken Markdown links. **Fix:** Label the referenced records as excluded from the public copy during Phase 5.
- **LOW — [README.md:62](/D:/src/fokus/README.md:62): Confirmed.** Local OAuth callback registration is missing. **Fix:** Document `http://localhost:5173/auth/callback` for the documented Vite flow, and the backend callback on port 5000 if used directly.
- **LOW — [docs/product/v1.md:331](/D:/src/fokus/docs/product/v1.md:331), also line 332: Confirmed.** The table still specifies .NET 9 and EF Core 9; the implementation targets version 10. **Fix:** Update it during the planned Phase 4 refresh.

Privacy checks found no remaining inventoried private identifiers in 1,852 retained blobs across 61 reachable commits after applying the maps and exclusions. All 21 redacted files matched the map replay byte for byte; bytes, CRLF, rule order and `--from-ref` checks passed. No changes exist under `src/` or `client/src/`.

Used the recorded 13.2-second complete pass. No suite rerun or file modifications. Untracking local settings and the operator page check remain explicitly assigned to the closing session and operator.

# Re-review after fixes — cycle 1 (2026-10-13)

## Verdict: NO-GO

- **MEDIUM — [README.md:62](/D:/src/fokus/README.md:62): OAuth fix uses the wrong port.** Vite’s string proxy enables `changeOrigin: true`; ASP.NET builds the callback from `Request.Host`. The configured behavior therefore produces `http://localhost:5000/auth/callback`, while the README registers only port 5173, causing a redirect mismatch. **Suggested fix:** Document the port-5000 callback, correct `implementation.md:66`, and verify the generated challenge URL. Sources: [Vite implementation](https://raw.githubusercontent.com/vitejs/vite/v8.0.10/packages/vite/src/node/server/middlewares/proxy.ts), [ASP.NET callback construction](https://raw.githubusercontent.com/dotnet/aspnetcore/v10.0.0/src/Security/Authentication/Core/src/AuthenticationHandler.cs).

- **MEDIUM — [scripts/step4.py:55](/D:/src/fokus/docs/specs/F36-PublicReleaseCleanup/delivery/scripts/step4.py:55): The retained driver fails after staging the new private records.** Its candidates include `.claude/verify.json` and `docs/skill-gaps/registry.md`, although both are excluded from publication and absent from `pre-public-cleanup`. Both corresponding read-only applier probes exit 1 with `cannot read pre-public-cleanup:…`. **Suggested fix:** Filter candidates through `exclusions.txt` before regeneration and explicitly handle retained files absent from the reference.

- **LOW — [implementation.md:21](/D:/src/fokus/docs/specs/F36-PublicReleaseCleanup/delivery/implementation.md:21), also [line 105](/D:/src/fokus/docs/specs/F36-PublicReleaseCleanup/delivery/implementation.md:105): The record still contains superseded claims.** Step 3 still says eleven deletions, and the carry-over table still lists fixed GAP statuses, missing OAuth documentation, and obsolete framework versions. **Suggested fix:** Correct the count to twelve and reconcile the table with the round’s outcomes, including the remaining OAuth defect.

- **MEDIUM — [implementation.md:104](/D:/src/fokus/docs/specs/F36-PublicReleaseCleanup/delivery/implementation.md:104): Confirmed carry-over package findings remain unresolved.** The retained audit reports six high-severity vulnerability entries affecting `@vue/server-renderer`, `vue`, `vite`, `postcss`, `nanoid`, and `source-map-js`; their installed versions match the lockfile. **Suggested fix:** Retain these for the already assigned Phase 2 owner triage. This deferral does not independently block Phase 1.

Fix verification:

| Item | Result |
|---|---|
| 1 | **Confirmed:** both anchored hooks exit 0 from `client/`; hooks otherwise match the local copy. |
| 2 | **Confirmed:** audit JSON retained; package names and advisory IDs recorded. |
| 3 | **Confirmed:** all three GAP specs say `Done (via I11)`. |
| 4 | **Confirmed:** all five public-copy annotations apply and are idempotent. |
| 5 | **Refuted:** callback is documented, but its port is wrong. |
| 6 | **Confirmed:** .NET and EF Core documentation now says version 10. |
| 7 | **Confirmed:** round record correctly reports the three prescribed regex matches. |
| 8 | **Partial:** round notes say twelve; Step 3 still says eleven. |
| 9 | **Confirmed:** neutral employer wording; replay and idempotence pass. |
| 10 | **Partial:** scripts and usage retained; regeneration fails after staging new excluded files. |

Carry-over table verification:

| Row | Assessment |
|---|---|
| npm findings | **Confirmed**, now supported by retained evidence. |
| GAP specs still Open | **Refuted**, fixed. |
| Removed-record mentions | **Confirmed** in the private tree; public annotations implemented. |
| Missing OAuth URI | **Refuted as written**; URI exists but is incorrect. |
| .NET 9 / EF Core 9 | **Refuted**, fixed. |

Independent checks passed: 20-file replay, 30-file idempotence, 16 publish probes, current-tree privacy sweeps, and redaction of 1,852 retained historical blobs with zero remaining inventoried identifiers. Application source and `client/package.json` are unchanged.

Used the recorded 5.6-second fast pass. No complete-suite rerun or file modifications. Untracking local settings and the operator page check remain assigned to the closing session and operator.

# Re-review after fixes — cycle 2 (2026-10-13)

## Verdict: GO

- **MEDIUM — [implementation.md:119](/D:/src/fokus/docs/specs/F36-PublicReleaseCleanup/delivery/implementation.md:119):** Six high-severity package findings remain confirmed by the retained audit and lockfile. **Suggested fix:** Resolve them through the assigned Phase 2 owner triage. This accepted deferral does not block this review.
- **LOW — [implementation.md:21](/D:/src/fokus/docs/specs/F36-PublicReleaseCleanup/delivery/implementation.md:21), also lines 120–123:** The record still says eleven deletions instead of twelve and lists several fixed issues as outstanding. **Suggested fix:** Correct the count and reconcile the carry-over table.

Both cycle-2 fixes are **confirmed**:

| Fix | Verification |
|---|---|
| 1 — OAuth callback | README correctly specifies `http://localhost:5000/auth/callback`; line 66’s earlier explanation is marked wrong. Local Vite configuration and [ASP.NET callback construction](https://raw.githubusercontent.com/dotnet/aspnetcore/v10.0.0/src/Security/Authentication/Core/src/AuthenticationHandler.cs) support this URI. |
| 2 — Regeneration driver | Excluded records and files absent from the reference are skipped with notes. A staged-file simulation checked 559 candidates, exited 0 and produced no output changes; writes were replaced with in-memory comparisons. |

Every carry-over row was checked:

| Row | Assessment |
|---|---|
| npm findings | **Confirmed; deferred.** |
| GAP specs still Open | **Refuted:** all three say `Done (via I11)`. |
| Removed-record mentions | **Confirmed in private history:** public annotations are implemented and idempotent. |
| Missing OAuth URI | **Refuted:** the correct URI is now documented. |
| .NET 9 / EF Core 9 | **Refuted:** both now specify version 10. |

Read-only checks passed: 20-file replay, 30-file idempotence, privacy sweeps and 16 publish-map probes. Used the recorded **15.1-second fast pass**; the complete suite was not rerun. No files modified.
