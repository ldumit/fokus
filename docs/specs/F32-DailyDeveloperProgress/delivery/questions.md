# Daily Developer Progress — Questions

## Q1: SignalR infrastructure vs architecture doc out-of-scope
**From:** architect
**To:** PO
**Status:** Answered
**Step:** Phase 1 analysis
**File:** `docs/architecture/v1.md`

**Context:** The architecture doc v2 explicitly lists "SignalR / real-time updates" under "What's Explicitly Out of Scope (v2)." The spec for F32 requires building SignalR infrastructure (hub, client subscription) for auto-refresh on sync. The spec acknowledges this is "new infrastructure, not reuse of an existing pattern."

**Question:** The spec intentionally expands scope beyond the architecture doc's out-of-scope boundary. Should the plan include an explicit step to amend the architecture doc (remove SignalR from out-of-scope, add a SignalR section under Cross-Cutting Concerns)? Or should we defer the architecture doc update and just build the infrastructure?

### Answer
Yes, include a step to amend the architecture doc. The spec explicitly scopes SignalR as in-scope for this feature. The architecture doc should reflect what is actually built.

## Q2: Separate store or extend developersStore?
**From:** architect
**To:** PO
**Status:** Answered
**Step:** Phase 1 analysis
**File:** `client/src/stores/developersStore.ts`

**Context:** The Daily Progress tab has fundamentally different data flow from the other Developers tabs. Other tabs use sprint selection (single/multi), but Daily Progress always shows the active sprint with no sprint selector. The current `developersStore` already manages state for 5 tabs (throughput, bugRatio, leaderboard, quality, qaWorkload) with per-tab loading/error refs. Adding a 6th tab with different data flow (no sprint selection, separate API endpoint, SignalR subscription) would further bloat the store.

**Question:** Should Daily Progress use a dedicated `dailyProgressStore` (cleaner separation, independent lifecycle, SignalR subscription management) or extend the existing `developersStore` (consistent with current tab pattern, shared sub-team filter state)? I recommend a dedicated store because: (a) it has no sprint selection logic to share, (b) it needs SignalR subscription lifecycle management, (c) the sub-team filter can be read from `developersStore` via cross-store reference.

### Answer
Dedicated `dailyProgressStore`. The spec establishes fundamentally different data flows: no sprint selection (BR22), different API endpoint, different ticket scope (BR1 -- all ticket types vs feature-only), SignalR lifecycle unique to this tab, and unique computed state (pace, alerts, stalls). A dedicated store is the clean separation.
