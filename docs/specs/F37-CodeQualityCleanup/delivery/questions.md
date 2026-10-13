# F37-CodeQualityCleanup — Questions

## Q1 — Active sprint as single target on Carry-Over, Bug Ratio and Leaderboard

**From:** developer (dev-s3, Step 7) · **To:** user (via team-lead) · **Status:** Answered

**Question:** On Carry-Over, Bug Ratio and Leaderboard, an active sprint picked as the single target computes over nothing (it is never loaded with memberships; those endpoints load closed sprints only). This predates F37. Fix it in F37, or leave it for later?

**Answer (user, verbatim, 2026-10-13):** "Leave for later (Recommended)"

**Consequences recorded by team-lead:**
- F37 keeps today's behaviour for the active sprint on these three pages.
- The Step 7 accept grep's remaining match in these three files (the Step 6 not-found line, which still feeds the active-sprint case) is accepted.
- The Step 12 operator check skips the active sprint on these three pages.
- A backlog row records the gap at close.
