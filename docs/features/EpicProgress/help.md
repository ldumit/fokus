# Epic Progress — Help Content

Sibling of `docs/features/EpicProgress/spec.md`. Each section provides Short (tooltip, under 150 chars) and Long (guide page paragraph) variants for in-app help.

---

## Summary Cards

### Active Epics

**Short:** Number of epics with remaining work.

**Long:** Shows how many epics still have tickets that haven't reached a done status. An epic is considered active until every one of its tickets is complete. This count helps you gauge how much parallel epic work your team is juggling.

### Average Completion

**Short:** Mean SP completion across all active epics.

**Long:** The average completion percentage across all active epics, measured by story points. This gives a quick sense of how far along your epic portfolio is overall. A low average with many active epics may indicate the team is spread too thin across initiatives.

### Unlinked Work

**Short:** Tickets in sprints that aren't tied to any epic.

**Long:** Counts tickets that appeared in at least one sprint but have no epic assigned in Jira. A high unlinked count means sprint effort isn't tracked against any larger initiative — making it harder to measure progress toward goals. Consider linking these tickets to epics in Jira, then re-syncing.

---

## Epic Table

### Progress Bar

**Short:** SP completion with estimated portion shown separately.

**Long:** The filled segment shows actual story points completed versus the total estimated scope. When an epic has unestimated tickets, a lighter segment extends the bar to show imputed scope — an estimate based on the average story points of estimated tickets in the same epic. This gives a more realistic picture of true progress instead of ignoring unestimated work.

### SP Done / SP Total

**Short:** Completed story points versus total scope including estimates.

**Long:** Shows actual completed story points against the total scope. When unestimated tickets exist, the total includes an imputed estimate labeled separately (e.g., "+~12 SP est."). The imputed portion is calculated from the average story points of estimated tickets within the same epic. If no tickets in an epic have estimates, SP metrics are unavailable and only ticket counts are shown. Note: only remaining unestimated tickets receive imputed values — done tickets without estimates are not included in SP metrics. This means SP completion may appear lower than ticket completion when many remaining tickets lack estimates.

### Tickets Done / Total

**Short:** Completed tickets versus total ticket count in the epic.

**Long:** A simple count of how many tickets have reached a done status versus the total number of tickets in the epic. Unlike SP completion, this treats every ticket equally regardless of size. Comparing ticket completion to SP completion reveals whether remaining work is mostly small tasks or a few large items.

### Velocity

**Short:** Average SP completed per sprint (last 3 sprints with progress).

**Long:** The rolling average of story points completed per sprint, using only sprints where this epic had at least one story point completed. Sprints where the team worked on other epics are excluded so the number reflects actual delivery pace on this epic. When fewer than 3 data points exist, the velocity is computed from what's available but projections are marked as low confidence.

### Projected Remaining

**Short:** Estimated sprints to completion based on current velocity.

**Long:** Divides the remaining work (including imputed estimates for unestimated tickets) by the epic's velocity to project how many more sprints are needed. This is a rough estimate — it assumes future velocity matches recent velocity and that scope won't change. "Insufficient data" appears when no velocity data exists. "Low confidence" appears when fewer than 3 sprints of delivery data are available.

### Active Sprint Count

**Short:** Number of sprints this epic has had tickets in.

**Long:** Counts the distinct sprints where at least one ticket from this epic appeared. This shows how long the epic has been in flight. A high sprint count with low completion may indicate the epic is being deprioritized or is too large.

---

## Expanded Ticket Table

### Ticket Row

**Short:** Individual ticket details within the epic.

**Long:** Each row shows the ticket key, summary, issue type, story points, current status, and assignee. Tickets are grouped with remaining work first (so you can quickly see what's left) followed by completed tickets. Unestimated tickets show a dash instead of story points — no imputed value is displayed at the individual ticket level.

---

## Active / Completed Toggle

**Short:** Switch between in-progress and finished epics.

**Long:** The Active view (default) shows epics that still have remaining work, sorted by completion percentage so the epics needing the most attention appear first. The Completed view shows epics where every ticket has reached a done status, sorted alphabetically. Summary cards update to reflect whichever view is selected.

---

## Sub-Team Filter

**Short:** Scope all metrics to one sub-team's contributions.

**Long:** When a sub-team is selected, all epic metrics recalculate to show only that sub-team's contribution: ticket counts, story points, velocity, and projections reflect only tickets assigned to developers in the selected sub-team. Epics with no tickets assigned to the selected sub-team are hidden. This lets you see how much of an epic's progress comes from a specific sub-team, useful when multiple sub-teams contribute to the same epic.
