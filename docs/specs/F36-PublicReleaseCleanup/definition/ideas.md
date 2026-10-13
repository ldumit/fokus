# Ideas — F36-PublicReleaseCleanup

## Topic: Cleaning (architect)

Problem: the repo mixes the app with the history of building its own AI setup and with private material; students should see the app, the setup and the feature records without wading through the rest.

### A — Five-item keep-list
- Problem: one rule for every file (code, in-repo setup, feature records, architecture map, product docs) instead of a judgement per file
- Status: parked
- Parked concerns: none
- Reason: topic closed in the ideas stage

### B — Remove the records of building the setup, keep the records of building the app
- Problem: setup history crowds out the app work
- Status: parked
- Parked concerns: none
- Reason: topic closed in the ideas stage

### C — Scrub what's personal or private
- Problem: nothing private or unusable reaches students
- Status: picked
- Parked concerns: none
- Critique: deploy scripts already hold only placeholders and the overnight script is part of the "built with agents" story, so both stay (overnight script with a short note); the scrub must cover every commit of the rewritten history, not only the final tree. Owner kept it as corrected (2026-10-13).

### D — Merge the knowledge base into the product documentation
- Problem: one place to read about the product instead of two
- Status: parked
- Parked concerns: the in-repo setup's rules point agents at docs/kb/ before reading source; merging it moves the path those rules depend on
- Reason: topic closed in the ideas stage (owner: "maybe D")

### E — Move setup-building records into one history folder
- Problem: keeps the "built with agents" story complete while the main folders hold only app work
- Status: parked
- Parked concerns: none
- Reason: topic closed in the ideas stage

### F — "How this repo was built" guide
- Problem: gives students a starting path through 60+ feature records
- Status: picked
- Parked concerns: none
- Critique: must be written last, after the cleanup and the history rewrite, or its links rot; a page in docs/ linked from the top of the README keeps the README a normal product README. Owner kept it as a docs page, written last (2026-10-13).
