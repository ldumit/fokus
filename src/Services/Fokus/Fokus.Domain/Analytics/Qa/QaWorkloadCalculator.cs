using Fokus.Domain.Analytics.Shared;
using static Fokus.Domain.Analytics.Shared.DeltaResolver;
using static Fokus.Domain.Analytics.Shared.MetricCardFactory;

namespace Fokus.Domain.Analytics.Qa;

public class QaWorkloadCalculator
{
    private const string UnassignedKey = "";

    // --- Per-person metrics holder used internally ---

    private sealed record PersonMetrics(
        string? AccountId,
        string DisplayName,
        string? SubTeam,
        string? AvatarUrl)
    {
        public int TesOwned { get; set; }
        public int PassCount { get; set; }
        public int FailCount { get; set; }
        public int RunsCompleted => PassCount + FailCount;
        public decimal PassRate => RunsCompleted > 0 ? Math.Round((decimal)PassCount / RunsCompleted * 100, 1) : 0m;
        public HashSet<string> StoriesCoveredKeys { get; } = new(StringComparer.OrdinalIgnoreCase);
        public HashSet<string> BugsFoundKeys { get; } = new(StringComparer.OrdinalIgnoreCase);
        public int StoriesCovered => StoriesCoveredKeys.Count;
        public int BugsFound => BugsFoundKeys.Count;
    }

    // --- Public API ---

    public QaWorkloadMultiSprintResult ComputeMultiSprint(
        List<Sprint> targetSprints,
        Dictionary<int, List<TestExecution>> tesBySprintId,
        List<Developer> allDevelopers,
        List<Sprint> allClosedSprints,
        Dictionary<int, List<TestExecution>> allClosedTesBySprintId,
        AppSettings settings,
        string? subTeam)
    {
        var sortedTarget = targetSprints.OrderBy(s => s.StartDate).ToList();

        var sprintInfos = sortedTarget
            .Select(s => new SprintRef(s.Id, s.Name, s.StartDate, s.EndDate))
            .ToList();

        // Aggregate person metrics across all target sprints
        var personMap = new Dictionary<string, PersonMetrics>(StringComparer.OrdinalIgnoreCase);

        var sprintBreakdownsByPerson = new Dictionary<string, List<QaWorkloadSprintBreakdown>>(StringComparer.OrdinalIgnoreCase);

        foreach (var sprint in sortedTarget)
        {
            var sprintTEs = tesBySprintId.GetValueOrDefault(sprint.Id, []);
            var sprintPersonMap = BuildSprintPersonMetrics(sprint, sprintTEs, allDevelopers);

            foreach (var (accountId, metrics) in sprintPersonMap)
            {
                if (!personMap.TryGetValue(accountId, out var existing))
                {
                    personMap[accountId] = new PersonMetrics(metrics.AccountId, metrics.DisplayName, metrics.SubTeam, metrics.AvatarUrl);
                    existing = personMap[accountId];
                }

                existing.TesOwned += metrics.TesOwned;
                existing.PassCount += metrics.PassCount;
                existing.FailCount += metrics.FailCount;
                foreach (var key in metrics.StoriesCoveredKeys) existing.StoriesCoveredKeys.Add(key);
                foreach (var key in metrics.BugsFoundKeys) existing.BugsFoundKeys.Add(key);

                // Sprint breakdown entry
                if (!sprintBreakdownsByPerson.TryGetValue(accountId, out var breakdowns))
                {
                    breakdowns = [];
                    sprintBreakdownsByPerson[accountId] = breakdowns;
                }
                breakdowns.Add(new QaWorkloadSprintBreakdown(
                    sprint.Id, sprint.Name,
                    metrics.TesOwned,
                    metrics.RunsCompleted,
                    metrics.PassCount,
                    metrics.FailCount,
                    metrics.PassRate,
                    metrics.StoriesCovered,
                    metrics.BugsFound));
            }
        }

        // Apply sub-team filter
        FilterPersonMapBySubTeam(personMap, allDevelopers, subTeam);

        // Team-level metrics (BR21-24): over the full (unfiltered) view of all unique TEs
        var allSprintTEsFlat = tesBySprintId.Values.SelectMany(x => x).ToList();
        var teamMetrics = BuildTeamMetrics(allSprintTEsFlat);

        // Build developer entries
        var developerEntries = BuildDeveloperEntries(
            personMap,
            sprintBreakdownsByPerson,
            allClosedSprints,
            allClosedTesBySprintId,
            allDevelopers);

        return new QaWorkloadMultiSprintResult(sprintInfos, teamMetrics, developerEntries);
    }

    public QaWorkloadSingleSprintResult ComputeSingleSprint(
        Sprint targetSprint,
        List<TestExecution> targetTEs,
        Sprint? priorSprint,
        List<TestExecution>? priorTEs,
        List<Sprint> sparklineWindow,
        Dictionary<int, List<TestExecution>> sparklineTEsBySprintId,
        List<Sprint> allClosedSprints,
        Dictionary<int, List<TestExecution>> allClosedTesBySprintId,
        List<Developer> allDevelopers,
        AppSettings settings,
        string? subTeam)
    {
        var sprintInfo = new SprintRef(
            targetSprint.Id, targetSprint.Name, targetSprint.StartDate, targetSprint.EndDate);

        // Current sprint person metrics
        var currentPersonMap = BuildSprintPersonMetrics(targetSprint, targetTEs, allDevelopers);

        // Prior sprint person metrics (for deltas)
        Dictionary<string, PersonMetrics>? priorPersonMap = null;
        if (priorSprint is not null && priorTEs is { Count: > 0 })
            priorPersonMap = BuildSprintPersonMetrics(priorSprint, priorTEs, allDevelopers);

        // Apply sub-team filter to current
        FilterPersonMapBySubTeam(currentPersonMap, allDevelopers, subTeam);
        if (priorPersonMap is not null)
            FilterPersonMapBySubTeam(priorPersonMap, allDevelopers, subTeam);

        // Team-level MetricCards with sparkline
        var teamMetrics = BuildSingleTeamMetrics(
            targetTEs, priorTEs, sparklineWindow, sparklineTEsBySprintId, settings);

        // Build single-sprint developer entries
        var developerEntries = BuildSingleDeveloperEntries(
            currentPersonMap,
            priorPersonMap,
            allClosedSprints,
            allClosedTesBySprintId,
            allDevelopers);

        return new QaWorkloadSingleSprintResult(sprintInfo, teamMetrics, developerEntries);
    }

    // --- Attribution ---

    private static string? GetAttributedAccountId(TestRun run, TestExecution te)
    {
        if (run.ExecutedById is not null) return run.ExecutedById;
        if (te.AssigneeId is not null) return te.AssigneeId;
        return null; // Unassigned
    }

    // --- Per-sprint person metrics builder ---

    private static Dictionary<string, PersonMetrics> BuildSprintPersonMetrics(
        Sprint sprint,
        List<TestExecution> sprintTEs,
        List<Developer> allDevelopers)
    {
        var developerLookup = allDevelopers.ToDictionary(d => d.Id, StringComparer.OrdinalIgnoreCase);
        var personMap = new Dictionary<string, PersonMetrics>(StringComparer.OrdinalIgnoreCase);

        AttributeOwnershipAndRuns(sprintTEs, personMap, developerLookup);
        AttributeStoriesCovered(sprintTEs, personMap);
        AttributeBugsFound(sprintTEs, personMap, developerLookup);

        return personMap;
    }

    private static PersonMetrics GetOrCreatePerson(
        Dictionary<string, PersonMetrics> personMap,
        Dictionary<string, Developer> developerLookup,
        string? accountId)
    {
        var key = accountId ?? UnassignedKey;
        if (!personMap.TryGetValue(key, out var p))
        {
            string displayName;
            string? subTeam = null;
            string? avatarUrl = null;

            if (accountId is not null && developerLookup.TryGetValue(accountId, out var dev))
            {
                displayName = dev.DisplayName;
                subTeam = dev.SubTeam;
                avatarUrl = dev.AvatarUrl;
            }
            else if (accountId is null)
            {
                displayName = "Unassigned";
            }
            else
            {
                displayName = accountId;
            }

            p = new PersonMetrics(accountId, displayName, subTeam, avatarUrl);
            personMap[key] = p;
        }
        return p;
    }

    private static void AttributeOwnershipAndRuns(
        List<TestExecution> sprintTEs,
        Dictionary<string, PersonMetrics> personMap,
        Dictionary<string, Developer> developerLookup)
    {
        foreach (var te in sprintTEs)
        {
            // TEs Owned (BR5): count non-cancelled TEs by AssigneeId
            // (sprintTEs are already non-cancelled per GetTestExecutionsForSprintAsync)
            var ownerKey = te.AssigneeId; // null = Unassigned
            GetOrCreatePerson(personMap, developerLookup, ownerKey).TesOwned++;

            // Runs Completed, Pass/Fail attribution (BR6-BR8)
            foreach (var run in te.TestRuns)
            {
                if (run.Status != TestRunStatus.Pass && run.Status != TestRunStatus.Fail)
                    continue; // Only terminal runs

                var runnerKey = GetAttributedAccountId(run, te);
                var runner = GetOrCreatePerson(personMap, developerLookup, runnerKey);

                if (run.Status == TestRunStatus.Pass) runner.PassCount++;
                else runner.FailCount++;
            }
        }
    }

    // Stories Covered (BR10): unique Tests-linked ticket keys for persons with attributed runs
    private static void AttributeStoriesCovered(
        List<TestExecution> sprintTEs,
        Dictionary<string, PersonMetrics> personMap)
    {
        foreach (var te in sprintTEs)
        {
            var testKeys = te.Links
                .Where(l => l.LinkType == TestExecutionLinkType.Tests)
                .Select(l => l.TicketKey)
                .ToList();

            if (testKeys.Count == 0) continue;

            // Attribute stories coverage to persons who ran tests in this TE
            var attributedPersons = te.TestRuns
                .Where(r => r.Status == TestRunStatus.Pass || r.Status == TestRunStatus.Fail)
                .Select(r => GetAttributedAccountId(r, te))
                .Distinct(NullableStringComparer.Instance)
                .ToList();

            foreach (var personId in attributedPersons)
            {
                if (personMap.TryGetValue(personId ?? UnassignedKey, out var person))
                {
                    foreach (var key in testKeys)
                        person.StoriesCoveredKeys.Add(key);
                }
            }
        }
    }

    // Bugs Found (BR11): unique Blocks-linked bug ticket keys per TE owner
    private static void AttributeBugsFound(
        List<TestExecution> sprintTEs,
        Dictionary<string, PersonMetrics> personMap,
        Dictionary<string, Developer> developerLookup)
    {
        foreach (var te in sprintTEs)
        {
            var bugKeys = te.Links
                .Where(l => l.LinkType == TestExecutionLinkType.Blocks &&
                            l.Ticket?.IssueType == "Bug")
                .Select(l => l.TicketKey)
                .ToList();

            if (bugKeys.Count == 0) continue;

            // Ensure the owner entry exists for bug tracking
            var owner = GetOrCreatePerson(personMap, developerLookup, te.AssigneeId);
            foreach (var key in bugKeys)
                owner.BugsFoundKeys.Add(key);
        }
    }

    // --- Sub-team filter ---

    private static void FilterPersonMapBySubTeam(
        Dictionary<string, PersonMetrics> personMap,
        List<Developer> allDevelopers,
        string? subTeam)
    {
        if (string.IsNullOrWhiteSpace(subTeam)) return;

        var developerLookup = allDevelopers.ToDictionary(d => d.Id, StringComparer.OrdinalIgnoreCase);
        var keysToRemove = personMap.Keys
            .Where(key =>
            {
                if (key == UnassignedKey) return true; // Unassigned excluded when sub-team filter active
                return !developerLookup.TryGetValue(key, out var dev) || dev.SubTeam != subTeam;
            })
            .ToList();

        foreach (var key in keysToRemove)
            personMap.Remove(key);
    }

    // --- Team metrics ---

    private static QaWorkloadTeamMetrics BuildTeamMetrics(List<TestExecution> allTEs)
    {
        var totalTes = allTEs.Count; // Already non-cancelled (filtered by GetTestExecutionsForSprintAsync)

        var passCount = 0;
        var failCount = 0;
        foreach (var te in allTEs)
        {
            foreach (var run in te.TestRuns)
            {
                if (run.Status == TestRunStatus.Pass) passCount++;
                else if (run.Status == TestRunStatus.Fail) failCount++;
            }
        }

        var totalRunsCompleted = passCount + failCount;
        var teamPassRate = totalRunsCompleted > 0
            ? Math.Round((decimal)passCount / totalRunsCompleted * 100, 1)
            : 0m;

        return new QaWorkloadTeamMetrics(totalTes, totalRunsCompleted, passCount, failCount, teamPassRate);
    }

    private QaWorkloadSingleTeamMetrics BuildSingleTeamMetrics(
        List<TestExecution> targetTEs,
        List<TestExecution>? priorTEs,
        List<Sprint> sparklineWindow,
        Dictionary<int, List<TestExecution>> sparklineTEsBySprintId,
        AppSettings settings)
    {
        // Current team metrics
        var currentMetrics = BuildTeamMetrics(targetTEs);

        // Prior metrics for deltas
        QaWorkloadTeamMetrics? priorMetrics = null;
        if (priorTEs is { Count: > 0 })
            priorMetrics = BuildTeamMetrics(priorTEs);

        // Sparkline builders
        var totalTesSparkline = BuildTeamSparkline(sparklineWindow, sparklineTEsBySprintId, tes => BuildTeamMetrics(tes).TotalTes);
        var runsCompletedSparkline = BuildTeamSparkline(sparklineWindow, sparklineTEsBySprintId, tes => BuildTeamMetrics(tes).TotalRunsCompleted);
        var passRateSparkline = BuildTeamSparkline(sparklineWindow, sparklineTEsBySprintId, tes => (decimal)BuildTeamMetrics(tes).TeamPassRate);

        decimal? tesDelta = priorMetrics is not null ? currentMetrics.TotalTes - priorMetrics.TotalTes : null;
        decimal? runsDelta = priorMetrics is not null ? currentMetrics.TotalRunsCompleted - priorMetrics.TotalRunsCompleted : null;
        decimal? passRateDelta = priorMetrics is not null ? currentMetrics.TeamPassRate - priorMetrics.TeamPassRate : null;

        var thresholds = settings.QaHealthThresholds;
        var passRateRag = HealthScoreCalculator.MetricRag(
            currentMetrics.TeamPassRate,
            thresholds.PassRateGreen,
            thresholds.PassRateAmber,
            higherIsBetter: true);

        var totalTesCard = BuildMetricCard(
            "Total TEs",
            currentMetrics.TotalTes,
            currentMetrics.TotalTes.ToString(),
            tesDelta,
            "neutral",
            totalTesSparkline);

        var runsCard = BuildMetricCard(
            "Total Runs Completed",
            currentMetrics.TotalRunsCompleted,
            currentMetrics.TotalRunsCompleted.ToString(),
            runsDelta,
            "neutral",
            runsCompletedSparkline);

        var passRateCard = BuildMetricCard(
            "Team Pass Rate",
            currentMetrics.TeamPassRate,
            $"{currentMetrics.TeamPassRate:0.#}%",
            passRateDelta,
            "positive-up",
            passRateSparkline) with { Rag = passRateRag };

        return new QaWorkloadSingleTeamMetrics(totalTesCard, runsCard, passRateCard);
    }

    private static List<SparklinePoint> BuildTeamSparkline(
        List<Sprint> window,
        Dictionary<int, List<TestExecution>> tesBySprintId,
        Func<List<TestExecution>, decimal> valueSelector)
    {
        return window.Select(s =>
        {
            var tes = tesBySprintId.GetValueOrDefault(s.Id, []);
            return new SparklinePoint(s.Name, Math.Round(valueSelector(tes), 1));
        }).ToList();
    }

    // --- Workload balance alert (BR16-19) ---

    private static WorkloadAlert EvaluateWorkloadAlert(
        string? accountId,
        List<Sprint> allClosedSprints,
        Dictionary<int, List<TestExecution>> allClosedTesBySprintId)
    {
        const int thresholdPercent = 50;
        const int minConsecutive = 2;

        var sortedDesc = allClosedSprints.OrderByDescending(s => s.StartDate).ToList();

        var consecutive = 0;
        foreach (var sprint in sortedDesc)
        {
            var tes = allClosedTesBySprintId.GetValueOrDefault(sprint.Id, []);

            // Count total terminal runs and this person's runs
            var totalRuns = 0;
            var personRuns = 0;

            foreach (var te in tes)
            {
                foreach (var run in te.TestRuns)
                {
                    if (run.Status != TestRunStatus.Pass && run.Status != TestRunStatus.Fail)
                        continue;

                    totalRuns++;
                    var runnerKey = GetAttributedAccountId(run, te);
                    if (NullableStringComparer.Instance.Equals(runnerKey, accountId))
                        personRuns++;
                }
            }

            if (totalRuns == 0) break; // No QA data breaks the streak

            var sharePercent = (decimal)personRuns / totalRuns * 100;
            if (sharePercent > thresholdPercent)
                consecutive++;
            else
                break;
        }

        return new WorkloadAlert(consecutive >= minConsecutive, consecutive, thresholdPercent);
    }

    // --- Developer entries builder ---

    private static List<QaWorkloadEntry> BuildDeveloperEntries(
        Dictionary<string, PersonMetrics> personMap,
        Dictionary<string, List<QaWorkloadSprintBreakdown>> sprintBreakdownsByPerson,
        List<Sprint> allClosedSprints,
        Dictionary<int, List<TestExecution>> allClosedTesBySprintId,
        List<Developer> allDevelopers)
    {
        var entries = new List<QaWorkloadEntry>();

        foreach (var metrics in personMap.Values)
        {
            var breakdowns = sprintBreakdownsByPerson.GetValueOrDefault(metrics.AccountId ?? UnassignedKey, []);
            var alert = EvaluateWorkloadAlert(metrics.AccountId, allClosedSprints, allClosedTesBySprintId);

            entries.Add(new QaWorkloadEntry(
                metrics.AccountId,
                metrics.DisplayName,
                metrics.SubTeam,
                metrics.AvatarUrl,
                metrics.TesOwned,
                metrics.RunsCompleted,
                metrics.PassCount,
                metrics.FailCount,
                metrics.PassRate,
                metrics.StoriesCovered,
                metrics.BugsFound,
                breakdowns,
                alert));
        }

        // Sort: assigned persons by runs completed desc, Unassigned pinned at bottom
        return entries
            .OrderBy(e => e.AccountId is null ? 1 : 0)
            .ThenByDescending(e => e.RunsCompleted)
            .ToList();
    }

    private static List<QaWorkloadSingleEntry> BuildSingleDeveloperEntries(
        Dictionary<string, PersonMetrics> currentPersonMap,
        Dictionary<string, PersonMetrics>? priorPersonMap,
        List<Sprint> allClosedSprints,
        Dictionary<int, List<TestExecution>> allClosedTesBySprintId,
        List<Developer> allDevelopers)
    {
        var entries = new List<QaWorkloadSingleEntry>();

        foreach (var current in currentPersonMap.Values)
        {
            PersonMetrics? prior = null;
            priorPersonMap?.TryGetValue(current.AccountId ?? UnassignedKey, out prior);

            var alert = EvaluateWorkloadAlert(current.AccountId, allClosedSprints, allClosedTesBySprintId);
            entries.Add(BuildSingleEntry(current, prior, alert));
        }

        // Sort: assigned persons by runs completed desc, Unassigned pinned at bottom
        return entries
            .OrderBy(e => e.AccountId is null ? 1 : 0)
            .ThenByDescending(e => e.RunsCompleted)
            .ToList();
    }

    private static QaWorkloadSingleEntry BuildSingleEntry(PersonMetrics current, PersonMetrics? prior, WorkloadAlert alert)
    {
        // Compute deltas and polarities
        var tes = DeltaOf(prior is not null ? current.TesOwned - prior.TesOwned : null, positiveUp: null);
        var runs = DeltaOf(prior is not null ? current.RunsCompleted - prior.RunsCompleted : null, positiveUp: null);
        var pass = DeltaOf(prior is not null ? current.PassCount - prior.PassCount : null, positiveUp: true);
        var fail = DeltaOf(prior is not null ? current.FailCount - prior.FailCount : null, positiveUp: false);
        var passRate = DeltaOf(prior is not null ? current.PassRate - prior.PassRate : null, positiveUp: true);
        var stories = DeltaOf(prior is not null ? current.StoriesCovered - prior.StoriesCovered : null, positiveUp: true);
        var bugs = DeltaOf(prior is not null ? current.BugsFound - prior.BugsFound : null, positiveUp: null);

        return new QaWorkloadSingleEntry(
            current.AccountId,
            current.DisplayName,
            current.SubTeam,
            current.AvatarUrl,
            current.TesOwned,
            current.RunsCompleted,
            current.PassCount,
            current.FailCount,
            current.PassRate,
            current.StoriesCovered,
            current.BugsFound,
            tes.Value, tes.Direction, tes.Polarity,
            runs.Value, runs.Direction, runs.Polarity,
            pass.Value, pass.Direction, pass.Polarity,
            fail.Value, fail.Direction, fail.Polarity,
            passRate.Value, passRate.Direction, passRate.Polarity,
            stories.Value, stories.Direction, stories.Polarity,
            bugs.Value, bugs.Direction, bugs.Polarity,
            alert);
    }

    private static WorkloadDelta DeltaOf(decimal? delta, bool? positiveUp)
    {
        if (!delta.HasValue)
            return new WorkloadDelta(null, null, null);

        var polarity = positiveUp.HasValue ? DeltaPolarity(delta.Value, positiveUp.Value) : "neutral";
        return new WorkloadDelta(Math.Round(delta.Value, 1), DeltaDirection(delta.Value), polarity);
    }

    private sealed record WorkloadDelta(decimal? Value, string? Direction, string? Polarity);
}

// --- Nullable string key comparer for person dictionaries ---

internal sealed class NullableStringComparer : IEqualityComparer<string?>
{
    public static readonly NullableStringComparer Instance = new();

    public bool Equals(string? x, string? y) =>
        string.Equals(x, y, StringComparison.OrdinalIgnoreCase);

    public int GetHashCode(string? obj) =>
        obj is null ? 0 : StringComparer.OrdinalIgnoreCase.GetHashCode(obj);
}
