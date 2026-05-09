# Bug Ratio — Help Content

Sibling of `docs/features/BugRatio/spec.md`. Each section has a **Short** (tooltip, under 150 chars) and **Long** (guide page paragraph) variant. Content appears in the app's info icons and help pages.

---

## Team Bug Ratio %

**Short:** Percentage of completed story points spent on bug fixes vs. all completed work across the selected sprints.

**Long:** Team Bug Ratio measures how much of your team's delivered capacity goes to fixing bugs rather than building new features. It's calculated as total completed Bug SP divided by total completed SP across all developers and selected sprints. A rising trend suggests growing quality problems upstream or accumulating technical debt. Use this metric to have data-backed conversations about investing in prevention (code reviews, testing, refactoring) vs. continuing to absorb reactive bug work.

---

## Bug SP / Non-Bug SP

**Short:** Story points completed on bug-type tickets vs. all other ticket types (stories, tasks, sub-tasks).

**Long:** Bug SP counts the story points on completed tickets whose Jira issue type is "Bug." Non-Bug SP counts everything else — stories, tasks, sub-tasks, and any custom issue types. Together they show the raw capacity split. A developer with high Bug SP isn't necessarily underperforming — they may be handling a critical quality backlog. Look at the trend over sprints, not a single sprint in isolation.

---

## Bug Ratio Trend Chart

**Short:** Bug ratio percentage per sprint over time. Spot sustained increases before they become the norm.

**Long:** This line chart plots the team's bug ratio for each sprint in the selected range. A stable or declining line means the team is keeping bug work in check. A rising line — especially over 3+ sprints — signals a systemic shift worth investigating. Common causes include aging code areas generating more defects, insufficient test coverage on recent features, or deferred tech debt catching up. Pair this with the Scope Change disruption view to see if bugs are also being injected mid-sprint.

---

## Per-Developer Stacked Bar Chart

**Short:** Bug SP (red) vs. Non-Bug SP (blue) per sprint for each developer. Compare allocation patterns.

**Long:** Each developer gets a set of stacked bars across sprints, colored red for bug work and blue for non-bug work. This chart makes individual capacity allocation visible over time. Look for developers whose red segment is growing sprint over sprint — they may be stuck in a reactive bug-fixing loop. Also look for developers with consistently zero bug work — is the bug load unevenly distributed? This chart is about patterns, not blame. Use it to rebalance workload or investigate why certain developers attract more bug assignments.

---

## Per-Developer Table

**Short:** Bug and non-bug SP, ticket counts, bug ratio %, and alert status for each active developer.

**Long:** The table shows each active developer's bug ratio breakdown for the selected sprint or range. Developers with zero completed tickets still appear with all-zero values — they are never hidden, so you always see the full team. In single-sprint view, every metric includes a delta (change vs. the prior sprint) with color coding — green means improvement, red means regression. The bug ratio percentage is the key column: it tells you what fraction of each developer's completed story points went to bugs. Ticket counts complement SP metrics by catching unestimated bugs that wouldn't show up in SP-based calculations.

---

## Alert Badge

**Short:** Appears when a developer's bug ratio exceeds the threshold for consecutive sprints.

**Long:** The alert badge warns you when a developer has been spending more than the configured percentage of their capacity on bugs for multiple sprints in a row. The default threshold is 50% for 2 consecutive sprints — both values are configurable in Settings. The alert evaluates against the full sprint history, not just what you're currently viewing. A developer can have an alert even when looking at a sprint range where only one sprint is above threshold, if prior sprints were also above. The alert resets when a developer's bug ratio drops below the threshold for even one sprint.

---

## Issue Type Breakdown

**Short:** Completed ticket counts grouped by raw Jira issue type. See composition beyond the bug/non-bug split.

**Long:** While the primary bug ratio view uses a binary split (Bug vs. Non-Bug), this breakdown shows the full picture by raw Jira issue type — Story, Bug, Task, Sub-task, Improvement, or any custom types your team uses. It helps answer questions like "is our non-bug work mostly stories or tasks?" and "how many sub-tasks are we completing relative to parent stories?" Only completed tickets are included.

---

## Sub-Team Filter

**Short:** Restrict all bug ratio content to developers in the selected sub-team.

**Long:** When a sub-team is selected, every metric on the Bug Ratio tab recalculates using only that sub-team's developers. The team-level bug ratio becomes the sub-team's ratio, the chart shows only those developers, and the issue type breakdown reflects their work. This lets you compare bug load distribution across sub-teams — one sub-team may be absorbing disproportionate bug work if they own an older or more complex area of the product.

---

## Sprint Selector

**Short:** Choose a single sprint for detail view, or a range (Last 3/5/All) for trend analysis.

**Long:** The sprint selector is shared across the Throughput and Bug Ratio tabs — changing it in one tab persists when you switch to the other. Selecting a single sprint shows detailed metrics with deltas comparing to the prior sprint. Selecting a range shows trend charts and aggregated values. "Last 5" is the default for the Bug Ratio tab. Use single-sprint view for retrospective deep-dives and multi-sprint view for spotting patterns over time.

---

## Bug Ratio Alert Settings

**Short:** Configure when the bug ratio alert triggers: threshold percentage and consecutive sprint count.

**Long:** Two settings control the alert behavior. The threshold percentage (default 50%) sets the bug ratio level that counts as "high." The consecutive sprint count (default 2) sets how many sprints in a row a developer must exceed the threshold before the alert activates. A higher threshold and count reduces noise — only sustained, significant bug ratios trigger alerts. A lower threshold catches problems earlier but may flag developers who are intentionally assigned to a bug backlog. Adjust based on your team's norms.
