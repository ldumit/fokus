# Xray QA Integration — Research & Discovery

**Author:** PO Agent  
**Date:** May 13, 2026  
**Status:** Discovery

---

## 1. What Fokus Is (Context)

Sprint Health Dashboard for Scrum Masters and Engineering Managers. Reads from Jira REST API and surfaces cross-sprint, per-developer, per-epic analytics that Jira cannot produce natively. Built for SMs managing split teams on limited Jira tiers.

**Primary consumers:** Scrum Masters and Engineering Managers who need data-backed arguments about sprint health, capacity, and scope changes.

**Existing analytics:** Sprint summary & health score, developer throughput, scope change & disruption, carry-over, bug ratio, epic progress, cycle time, team management. All 24 features (F1–F24) complete.

---

## 2. What Xray Is

Xray is a Test Management tool for Jira. It adds test-specific issue types to Jira and provides the structure to organize, plan, and report on testing progress. Over 4.5M users manage 100M+ test cases monthly.

**Two products:** Xray Server/DC and Xray Cloud. Different APIs — Cloud is where the GraphQL API lives.

---

## 3. Xray Entity Model

| Entity | Jira Issue? | Description |
|--------|-------------|-------------|
| **Test** | Yes | A test case (manual or automated). Has steps, preconditions, test type (Manual, Cucumber, Generic). |
| **Test Set** | Yes | A static grouping of tests (e.g., "Login tests", "Regression suite"). |
| **Test Plan** | Yes | A collection of tests planned for a release/sprint/milestone. Tracks which tests should be executed. |
| **Test Execution** | Yes | An assignable task to run a group of tests against a specific version/environment. |
| **Test Run** | **No** (virtual) | The actual result of running one test within one execution. Has status (PASS/FAIL/TODO/EXECUTING). Not a Jira issue — only accessible via Xray API. |
| **Pre-Condition** | Yes | Setup steps required before a test can run. |

**Linking model:** Tests link to requirements (Jira stories/tasks) via standard Jira issue links. This is how coverage traceability works — "Test X covers Story Y."

---

## 4. Xray Cloud API Surface

### 4.1 Authentication

Separate from Jira auth. Uses Client ID + Client Secret → bearer token.

- API keys created in Xray Global Settings → API Keys
- Keys don't expire; bearer token expires after 24 hours
- Auth endpoint: `POST https://xray.cloud.getxray.app/api/v2/authenticate`
- Token used for both REST and GraphQL requests

### 4.2 GraphQL API (Preferred)

Endpoint: `https://xray.cloud.getxray.app/api/v2/graphql`

**Key queries:**

| Query | Returns | Supports JQL filter? |
|-------|---------|---------------------|
| `getTests` | Test issues with type, status, preconditions, test runs | Yes |
| `getTestSets` | Test sets with their contained tests | Yes |
| `getTestPlans` | Test plans with their tests and executions | Yes |
| `getTestExecutions` | Test executions with their test runs | Yes |
| `getTestRun` | Single test run by test + execution issue IDs | N/A (by ID) |
| `getTestRunById` | Single test run by run ID | N/A (by ID) |

**Key mutations:** `createTestPlan`, `addTestsToTestExecution`, `removeTestsFromTestExecution`, `updateTestRunStatus`, `updateTestRunExampleStatus`

**What we can get per test run:**
- `status { name, color, description }` — the PASS/FAIL/TODO/EXECUTING result
- `examples` — for parameterized/Gherkin tests, each example has its own status
- `testExecution` — parent execution with its linked test plans
- `gherkin` — the scenario text at time of execution

**Nested traversal in one call:**
```
Test Execution → Test Runs → Status (PASS/FAIL)
                           → Test → linked stories (via Jira fields)
Test Plan → Tests → Test Runs → Status
```

### 4.3 Constraints

| Constraint | Value |
|------------|-------|
| Pagination limit | 1–100 items per connection |
| Max items per call | 10,000 |
| Max resolvers per call | 25 |
| Rate limit (Standard) | 300 requests / 5 minutes |
| Rate limit (Enterprise) | 1,000 requests / 5 minutes |
| JQL result cap | 100 issues (must refine if more) |

### 4.4 REST API (Secondary)

Mostly used for importing test automation results (JUnit, Cucumber, Robot Framework formats). Also used for the initial authentication step.

**Key REST endpoints:**
- `POST /api/v2/authenticate` — get bearer token
- `POST /api/v2/import/execution` — import test results
- Various entity CRUD endpoints (less flexible than GraphQL)

---

## 5. Opportunity Analysis

### What Jira + Xray can't show natively

Xray's UI serves QA engineers' day-to-day workflow. Jira dashboards show basic Xray gadgets. But neither gives the SM/EM:

1. **Cross-sprint QA trends** — test coverage trajectory, pass rate over time
2. **Sprint-level test health** — "of 30 stories this sprint, how many have tests? How many pass?"
3. **QA + delivery correlation** — connecting test results with bug ratio, disruption, carry-over
4. **Per-developer test coverage** — "which developers' stories consistently lack tests?"
5. **Aggregated quality gates** — "is this sprint shippable from a testing standpoint?"

### Natural integration points with existing Fokus features

| Fokus Feature | QA Extension |
|---------------|-------------|
| F8 Sprint Summary Card | Add test coverage % and pass rate to health score |
| F9 Developer Throughput | Per-developer test coverage of their stories |
| F13 Bug Ratio | Correlate bug ratio with test coverage — untested stories vs bugs |
| F14 Epic Progress | Per-epic test coverage and pass rate |
| Sprint Health Score | Quality sub-score based on test execution status |

### Initial recommendation

Start with **sprint-level test coverage and execution status** — highest value, lowest complexity. Extends F8 directly. Cross-sprint trends follow naturally using existing delta/sparkline patterns.

---

## 6. Sources

- [Xray GraphQL API docs](https://docs.getxray.app/display/XRAYCLOUD/GraphQL+API)
- [GraphQL Schema Reference](https://us.xray.cloud.getxray.app/doc/graphql/)
- [getTestExecutions](https://us.xray.cloud.getxray.app/doc/graphql/gettestexecutions.doc.html)
- [getTestPlans](https://us.xray.cloud.getxray.app/doc/graphql/gettestplans.doc.html)
- [Xray Postman Collections](https://github.com/Xray-App/xray-postman-collections)
- [Xray Cloud Authentication](https://docs.getxray.app/display/XRAYCLOUD/Authentication+-+REST)
- [Xray Knowledge Base — REST API usage](https://knowledgebase.getxray.app/en/articles/7830194-how-to-use-rest-api-with-xray-for-jira-cloud)
- [Xray Code Snippets — GraphQL usage](https://github.com/Xray-App/xray-code-snippets/blob/main/use_cases/graphql_api_usage/README.md)

---

## 7. Competitor Research

### 7.1 Landscape Overview

Nobody builds a standalone "QA analytics dashboard on top of Xray" the way Fokus builds sprint analytics on top of Jira. The space splits into three categories:

1. **Jira-native test management tools** (Xray, Zephyr Scale) — own the QA workflow, provide built-in reports inside Jira
2. **Test ops platforms** (Allure TestOps, BrowserStack QEI, Katalon TestOps) — aggregate CI/CD test results into dashboards
3. **BI layer** (Tableau, Power BI, Grafana) — teams export data from Jira + Xray + CI into a generic visualization tool

None of them do what Fokus does: cross-sprint, per-developer, SM-oriented analytics with health scores and delta trends. This is the gap.

---

### 7.2 Xray's Own Reporting

**What it offers:**
- Traceability Report — maps requirements → tests → executions → defects
- Overall Requirement Coverage Report — which stories have tests, which don't
- Historical Requirement Coverage Report — coverage over versions/releases (not sprints)
- Test Plans Report, Test Executions Report, Test Runs Report
- Jira dashboard gadgets (pie charts, bar charts for test status)

**What it doesn't offer:**
- No cross-sprint trend views (pass rate trajectory, coverage growth over sprints)
- No per-developer correlation (who writes stories without tests, whose stories fail)
- No composite quality score (like Fokus's health score)
- No delta indicators (↑↓ vs prior sprint)
- Reports are release/version-oriented, not sprint-oriented
- No integration with delivery metrics (throughput, disruption, carry-over)

**Verdict:** Xray's reports serve QA engineers managing test execution day-to-day. They don't serve the SM asking "is this sprint's quality better than last sprint's?"

---

### 7.3 Zephyr Scale

**Architecture difference:** Zephyr stores test data in its own DB (not as Jira issues). This makes it lighter on Jira but harder to query from external tools.

**Reporting:**
- 70+ built-in cross-project reports and dashboard gadgets
- Traceability Report, Test Case Execution Report, Test Execution Burndown Chart, Top Defects Impacting Testing
- Stronger out-of-the-box dashboarding than Xray

**What it lacks (same as Xray):**
- Sprint-oriented QA trend analysis
- Correlation with delivery metrics
- Per-developer quality attribution
- Composite health scoring

**API:** REST-only, less flexible than Xray's GraphQL. Would be harder to integrate with.

**Verdict:** Better built-in dashboards than Xray, but same fundamental gap — no SM-oriented sprint analytics.

Sources: [Xray vs Zephyr comparison](https://www.getxray.app/blog/best-test-management-tool-for-jira-xray-vs-zephyr), [SmartBear comparison](https://smartbear.com/blog/whats-the-difference-between-zephyr-and-xray/)

---

### 7.4 Allure TestOps

**What it is:** Test operations platform focused on CI/CD pipeline analytics. Not Jira-native — integrates via plugin.

**Key capabilities:**
- Live pipeline analytics — real-time test result dashboards from CI runs
- Custom dashboards with built-in query language (AQL)
- Flaky test detection via ML
- Smart test selection (runs only relevant tests based on code changes)
- One-way export integration with Xray (Allure → Xray test case sync)
- Jira integration for linking test cases to issues

**What's interesting for us:**
- Their trend analysis across launches/runs is the closest to what Fokus would do for QA
- AQL (Allure Query Language) for custom filtering — we'd do this via our existing API pattern instead
- Flaky test detection is valuable but out of scope for Fokus v1

**What it doesn't do:**
- No sprint-level aggregation (it thinks in "launches" and "pipelines", not sprints)
- No per-developer attribution
- No composite quality score combined with delivery metrics
- Requires its own infrastructure (separate from Jira/Xray)

**Verdict:** Closest competitor in analytics depth, but aimed at DevOps/CI engineers, not Scrum Masters. Doesn't correlate QA data with sprint delivery metrics.

Sources: [Allure TestOps docs](https://docs.qameta.io/allure-testops/), [Allure Xray integration](https://docs.qameta.io/allure-testops/integrations/xray/)

---

### 7.5 BrowserStack QEI (Quality Engineering Insights)

**What it is:** Unified quality intelligence platform. Aggregates data from Jira, Jenkins, TestRail, Xray, CI/CD pipelines.

**Key capabilities:**
- Net Quality Score (NQS) — composite score (out of 5) across coverage, stability, pass rate, velocity
- Sprint & release tracking with historical comparison
- Early warning alerts when metrics drop below thresholds
- "Key Wins" and "Focus Areas" highlights
- Executive-level dashboards

**What's interesting for us:**
- **NQS is exactly the pattern we'd follow** — composite quality score, like Fokus's sprint health score but for testing
- Sprint-level tracking with drill-down
- Historical trend comparison across sprints

**What it doesn't do:**
- Requires BrowserStack ecosystem (not standalone)
- Expensive enterprise pricing
- Overkill for teams that just want sprint-level QA visibility on top of Xray
- Doesn't integrate with team-specific delivery metrics (throughput, disruption, etc.)

**Verdict:** Best-in-class for the "quality intelligence" concept. The NQS pattern validates our approach of building a composite QA score. But it's a platform play — Fokus would deliver the same insight in a lighter, Xray-first way alongside existing delivery analytics.

Sources: [BrowserStack QEI](https://www.browserstack.com/quality-engineering-insights), [BrowserStack testing dashboard guide](https://www.browserstack.com/guide/software-testing-dashboard)

---

### 7.6 Generic BI (Tableau, Power BI, Grafana)

Some teams export Xray data (via API or Jira database connectors) into BI tools for custom dashboards.

**Pros:** Fully customizable, can combine any data source
**Cons:** Requires manual setup, data pipeline maintenance, no domain logic, no health scoring, no out-of-the-box sprint analytics. High effort for something that drifts as requirements change.

**Verdict:** This is what Fokus replaces for delivery metrics — same opportunity exists for QA metrics.

---

### 7.7 Industry-Standard QA Metrics

From research across multiple sources, the metrics that matter most for sprint-level QA:

| Metric | What it measures | Fokus relevance |
|--------|-----------------|-----------------|
| **Test Coverage Rate** | % of stories with linked tests | High — extends sprint summary |
| **Test Pass Rate** | % of executed tests that pass | High — core health indicator |
| **Test Execution Rate** | % of planned tests actually executed | High — sprint readiness signal |
| **Defect Leakage Rate** | Bugs found post-sprint vs during testing | Medium — correlates with bug ratio (F13) |
| **Defect Density** | Defects per story/SP | Medium — per-developer quality signal |
| **Flaky Test Rate** | Tests with inconsistent results | Low for v1 — needs execution history depth |
| **MTTR** | Mean time to resolve test failures | Low for v1 — needs timestamp precision |

Sources: [ARDURA QA Metrics](https://ardura.consulting/blog/qa-metrics-dashboard-what-to-measure/), [BrowserStack QA Metrics](https://www.browserstack.com/guide/essential-qa-metrics), [Testmo QA Reporting](https://www.testmo.com/qa-metrics-reporting/)

---

## 8. Key Takeaways

1. **Nobody occupies Fokus's niche for QA.** No tool provides sprint-oriented, SM-targeted QA analytics combined with delivery metrics. Xray reports are QA-engineer-facing. Allure/BrowserStack are DevOps/CI-facing. BI tools require manual build-out.

2. **Xray's GraphQL API gives us what we need.** Test plans, executions, runs with pass/fail status, and coverage links to stories — all queryable by JQL (filterable by sprint).

3. **The composite quality score pattern is validated.** BrowserStack's NQS proves the market wants a single number for quality health. Fokus already has this pattern with the sprint health score — extending it with QA sub-scores is natural.

4. **Sprint-level aggregation is the differentiator.** Every competitor thinks in releases, pipelines, or projects. Fokus thinks in sprints. That's the angle.

5. **Start simple: coverage + pass rate per sprint.** The highest-value, lowest-complexity starting point. Cross-sprint trends follow via existing patterns (delta, sparkline). Per-developer correlation is a powerful follow-up.
