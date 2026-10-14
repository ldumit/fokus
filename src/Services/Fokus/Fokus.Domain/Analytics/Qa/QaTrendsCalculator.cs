using Fokus.Domain.Analytics.Shared;
using static Fokus.Domain.Analytics.Shared.SubTeamFilter;
using static Fokus.Domain.Analytics.Shared.TestCoverageCalculator;
using static Fokus.Domain.Analytics.Shared.TransitionAttributionChecker;

namespace Fokus.Domain.Analytics.Qa;

public class QaTrendsCalculator
{
    public QaTrendsResult ComputeTrends(
        List<Sprint> qaSprints,
        Dictionary<int, List<TestExecution>> tesBySprintId,
        Dictionary<int, List<SprintMembership>> membershipsBySprintId,
        Dictionary<int, decimal> bugRatioBySprintId,
        List<StatusTransition> statusTransitions,
        AppSettings settings,
        string? subTeam)
    {
        var sortedSprints = qaSprints.OrderBy(s => s.StartDate).ToList();

        if (sortedSprints.Count < 2)
        {
            return new QaTrendsResult(
                HasQaData: false,
                Sprints: [],
                QualityTrends: [],
                TestingVolume: [],
                DefectCorrelation: null);
        }

        var (orderedStages, startIndex) = ResolveStartIndex(settings);

        var transitionsByTicket = statusTransitions
            .GroupBy(t => t.TicketId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);

        var sprintInfos = sortedSprints
            .Select(s => new SprintRef(s.Id, s.Name, s.StartDate, s.EndDate))
            .ToList();

        var qualityTrends = new List<QualityTrendEntry>();
        var testingVolume = new List<TestingVolumeEntry>();

        foreach (var sprint in sortedSprints)
        {
            var sprintTEs = tesBySprintId.GetValueOrDefault(sprint.Id, []);
            var memberships = membershipsBySprintId.GetValueOrDefault(sprint.Id, []);

            qualityTrends.Add(BuildQualityTrend(
                sprint, sprintTEs, memberships, transitionsByTicket, orderedStages, startIndex, settings, subTeam));

            // TE count: unique non-cancelled TEs for this sprint
            // Bugs Found: unique bug ticket keys linked via Blocks from non-cancelled TEs
            testingVolume.Add(new TestingVolumeEntry(sprint.Id, sprint.Name, sprintTEs.Count, ComputeBugsFound(sprintTEs)));
        }

        var defectCorrelation = ComputeDefectCorrelation(sortedSprints, qualityTrends, bugRatioBySprintId);

        return new QaTrendsResult(
            HasQaData: true,
            Sprints: sprintInfos,
            QualityTrends: qualityTrends,
            TestingVolume: testingVolume,
            DefectCorrelation: defectCorrelation);
    }

    private static QualityTrendEntry BuildQualityTrend(
        Sprint sprint,
        List<TestExecution> sprintTEs,
        List<SprintMembership> memberships,
        Dictionary<string, List<StatusTransition>> transitionsByTicket,
        List<string> orderedStages,
        int startIndex,
        AppSettings settings,
        string? subTeam)
    {
        var filteredMemberships = FilterMemberships(memberships, subTeam);

        var activeKeys = GetActiveFeatureTicketKeys(
            filteredMemberships, transitionsByTicket,
            sprint.StartDate, sprint.EndDate,
            orderedStages, startIndex,
            settings.ExcludedFromScopeStatuses, settings.DefaultSpPerBug);

        var testsTesByTicket = BuildTestsTesByTicket(sprintTEs);
        var runsByTe = BuildRunsByTe(sprintTEs);

        var (coveredKeys, coverageRate) = ComputeCoverageRate(activeKeys, testsTesByTicket, memberships);
        var (_, executionRate) = ComputeExecutionRate(coveredKeys, testsTesByTicket, runsByTe);
        var passRate = ComputePassRate(sprintTEs);

        var thresholds = settings.QaHealthThresholds;
        var coverageRag = HealthScoreCalculator.MetricRag(coverageRate, thresholds.CoverageGreen, thresholds.CoverageAmber, higherIsBetter: true);
        var executionRag = HealthScoreCalculator.MetricRag(executionRate, thresholds.ExecutionGreen, thresholds.ExecutionAmber, higherIsBetter: true);
        var passRateRag = HealthScoreCalculator.MetricRag(passRate, thresholds.PassRateGreen, thresholds.PassRateAmber, higherIsBetter: true);

        return new QualityTrendEntry(
            sprint.Id, sprint.Name,
            coverageRate, passRate, executionRate,
            coverageRag, passRateRag, executionRag);
    }

    // --- Defect correlation ---

    private static DefectCorrelationResult ComputeDefectCorrelation(
        List<Sprint> sortedSprints,
        List<QualityTrendEntry> qualityTrends,
        Dictionary<int, decimal> bugRatioBySprintId)
    {
        var dataPoints = new List<DefectCorrelationDataPoint>();

        for (var i = 0; i < sortedSprints.Count; i++)
        {
            var sprint = sortedSprints[i];
            var trend = qualityTrends.First(t => t.SprintId == sprint.Id);

            decimal? nextBugRatio = null;
            string? nextSprintName = null;

            if (i + 1 < sortedSprints.Count)
            {
                var nextSprint = sortedSprints[i + 1];
                if (bugRatioBySprintId.TryGetValue(nextSprint.Id, out var ratio))
                {
                    nextBugRatio = ratio;
                    nextSprintName = nextSprint.Name;
                }
            }

            dataPoints.Add(new DefectCorrelationDataPoint(
                sprint.Id, sprint.Name, trend.CoverageRate, nextBugRatio, nextSprintName));
        }

        // Complete pairs: both coverage and next-sprint bug ratio present
        var completePairs = dataPoints
            .Where(dp => dp.NextSprintBugRatio.HasValue)
            .ToList();

        var pearsonR = ComputePearsonR(completePairs);

        return new DefectCorrelationResult(dataPoints, pearsonR, completePairs.Count);
    }

    private static decimal? ComputePearsonR(List<DefectCorrelationDataPoint> completePairs)
    {
        var n = completePairs.Count;
        if (n < 6) return null;

        var xs = completePairs.Select(p => p.CoverageRate).ToList();
        var ys = completePairs.Select(p => p.NextSprintBugRatio!.Value).ToList();

        var sumX = xs.Sum();
        var sumY = ys.Sum();
        var sumXY = xs.Zip(ys, (x, y) => x * y).Sum();
        var sumX2 = xs.Sum(x => x * x);
        var sumY2 = ys.Sum(y => y * y);

        var numerator = n * sumXY - sumX * sumY;
        var denominator = (decimal)Math.Sqrt((double)((n * sumX2 - sumX * sumX) * (n * sumY2 - sumY * sumY)));

        if (denominator == 0) return null;

        return Math.Round(numerator / denominator, 2);
    }
}
