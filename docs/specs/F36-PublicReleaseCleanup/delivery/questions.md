# Public Release Cleanup — Questions

## Q1: npm steps write into paths the dispatch puts off limits
**From:** developer
**To:** architect
**Status:** Open
**Step:** Step 5 (lockfile) and Step 7 (safety net)
**File:** `client/package-lock.json`, `client/node_modules/`, `src/Services/Fokus/Fokus.API/wwwroot/`

**Context:** The dispatch lists `client/node_modules/` as unrelated dirt, "never touch or include", and says never edit anything under `src/`. Three plan commands write there:
- Step 5 `npm install` in `client/` and its accept `npm ci --prefix client` rewrite `client/node_modules/` (`npm ci` deletes and reinstalls it; it holds 68 top-level entries today).
- Step 7 `npm ci --prefix client` does the same; `npm run build --prefix client` writes `src/Services/Fokus/Fokus.API/wwwroot/` (`client/vite.config.*:14-15`, `outDir` there with `emptyOutDir: true`). That folder is ignored (`.gitignore:52`) and has 0 tracked files, so the Must-NOT-Change pin on `src` stays green either way.
- The handover `roles.full` runs `npm run build --prefix client` with no `npm ci`, so it builds against whatever `client/node_modules/` holds at that point.

**Question:** May the build touch `client/node_modules/` and the ignored `wwwroot/` output, or should the npm checks avoid them?

**Recommendation:**
Avoid them (Recommended) — Step 5 syncs the lockfile with `npm install --package-lock-only` (writes only `package-lock.json`); the `npm ci` accept runs in a scratch copy on `D:\temp` holding only `package.json` + the new lockfile, proving the lockfile installs clean; Step 7 skips `npm run build`, which the handover `roles.full` run covers. Risk: the existing `node_modules/` stays as it is, so if the synced lockfile resolves a package the old install lacks, the handover build is the first place that shows.
Allow them — run the plan's commands as written (`npm install`, `npm ci --prefix client`, `npm run build --prefix client`); `node_modules/` and `wwwroot/` change on disk but are ignored, never staged. The handover build then runs on a fresh install from the synced lockfile.
**Confidence:** medium — the "never touch" list reads as protecting the owner's dirt from edits and staging, and both folders are regenerated, ignored output; but the wording is explicit, so it is your call.

### Answer

**Answer (architect, 2026-10-13):** Run the plan's npm commands as written. `client/node_modules/` and `src/Services/Fokus/Fokus.API/wwwroot/` are ignored, regenerated output; the dispatch's "never touch" list means never edit by hand or include in the change — npm and vite regenerating them is fine. Nothing there is ever staged.
**Status:** Answered
