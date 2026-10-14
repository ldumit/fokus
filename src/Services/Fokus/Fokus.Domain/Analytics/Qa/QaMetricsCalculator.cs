using Fokus.Domain.Analytics.Shared;
using static Fokus.Domain.Analytics.Shared.MetricCardFactory;
using static Fokus.Domain.Analytics.Shared.SubTeamFilter;
using static Fokus.Domain.Analytics.Shared.TestCoverageCalculator;
using static Fokus.Domain.Analytics.Shared.TransitionAttributionChecker;

namespace Fokus.Domain.Analytics.Qa;

public class QaMetricsCalculator
{
    public QaMetricsResult ComputeQaMetrics(
        List<TestExecution> sprintTEs,
        List<SprintMembership> activeMemberships,
        List<StatusTransition> statusTransitions,
        Sprint selectedSprint,
        Sprint? priorSprint,
        List<TestExecution>? priorSprintTEs,
        List<SprintMembership>? priorMemberships,
        List<Sprint> sparklineWindow,
        Dictionary<int, List<TestExecution>> sparklineTEsBySprintId,
        Dictionary<int, List<SprintMembership>> sparklineMembershipsBySprintId,
        AppSettings settings,
        string? subTeam)
    {
        var (orderedStages, startIndex) = ResolveStartIndex(settings);

        // Apply sub-team filter to memberships
        var filteredMemberships = FilterMemberships(activeMemberships, subTeam);

        // Compute active scope (feature tickets in sprint with effective SP and IsStartedInSprint)
        var transitionsByTicket = statusTransitions
            .GroupBy(t => t.TicketId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);

        var activeTicketKeys = GetActiveFeatureTicketKeys(
            filteredMemberships, transitionsByTicket, selectedSprint.StartDate, selectedSprint.EndDate,
            orderedStages, startIndex, settings.ExcludedFromScopeStatuses, settings.DefaultSpPerBug);

        // sprintTEs is pre-filtered by the repository (Status != "Cancelled" excluded at the DB level).
        var nonCancelledTEs = sprintTEs;

        // Map from ticketKey -> list of TE IDs (Tests links only)
        var testsTesByTicket = BuildTestsTesByTicket(nonCancelledTEs);

        // Coverage rate (BR1)
        var (coveredKeys, coverageRate) = ComputeCoverageRate(activeTicketKeys, testsTesByTicket, activeMemberships);
        var untestedCount = activeTicketKeys.Count - coveredKeys.Count;

        // Execution rate (BR2)
        var runsByTe = BuildRunsByTe(nonCancelledTEs);
        var (executedKeys, executionRate) = ComputeExecutionRate(coveredKeys, testsTesByTicket, runsByTe);

        // Pass rate (BR3)
        var passRate = ComputePassRate(nonCancelledTEs);

        // Bugs found (BR4)
        var bugsFound = ComputeBugsFound(nonCancelledTEs);

        // Failing ticket count
        var failingCount = ComputeFailingCount(coveredKeys, testsTesByTicket, runsByTe);

        // Quality sub-score (BR16)
        var (qualitySubScore, qualityBreakdown) = ComputeQualitySubScore(coverageRate, passRate, settings);

        // Delta (BR21) — prior sprint
        var prior = priorSprint is not null && priorSprintTEs is not null && priorMemberships is not null
            ? ComputePriorRates(priorSprint, priorSprintTEs, priorMemberships, transitionsByTicket,
                orderedStages, startIndex, settings, subTeam)
            : new PriorRates(null, null, null);

        // Sparkline (BR22)
        var sparklines = new SparklineInputs(
            sparklineWindow, sparklineTEsBySprintId, sparklineMembershipsBySprintId,
            transitionsByTicket, settings, orderedStages, startIndex, subTeam);

        var thresholds = settings.QaHealthThresholds;
        return new QaMetricsResult(
            HasQaData: true,
            CoverageRate: BuildQaMetricCard("Coverage Rate", coverageRate, prior.CoverageRate, BuildCoverageSparkline(sparklines),
                HealthScoreCalculator.MetricRag(coverageRate, thresholds.CoverageGreen, thresholds.CoverageAmber, higherIsBetter: true)),
            ExecutionRate: BuildQaMetricCard("Execution Rate", executionRate, prior.ExecutionRate, BuildExecutionSparkline(sparklines),
                HealthScoreCalculator.MetricRag(executionRate, thresholds.ExecutionGreen, thresholds.ExecutionAmber, higherIsBetter: true)),
            PassRate: BuildQaMetricCard("Pass Rate", passRate, prior.PassRate, BuildPassRateSparkline(sparklines),
                HealthScoreCalculator.MetricRag(passRate, thresholds.PassRateGreen, thresholds.PassRateAmber, higherIsBetter: true)),
            BugsFound: bugsFound,
            QualitySubScore: qualitySubScore,
            QualityBreakdown: qualityBreakdown,
            UntestedCount: untestedCount,
            FailingCount: failingCount);
    }

    private static (decimal QualitySubScore, QualityBreakdownInfo Breakdown) ComputeQualitySubScore(
        decimal coverageRate,
        decimal passRate,
        AppSettings settings)
    {
        var thresholds = settings.QaHealthThresholds;
        var subScoreWeights = settings.QualitySubScoreWeights;
        var coverageScore = HealthScoreCalculator.ScoreHigherIsBetter(coverageRate, thresholds.CoverageGreen, thresholds.CoverageAmber);
        var passRateScore = HealthScoreCalculator.ScoreHigherIsBetter(passRate, thresholds.PassRateGreen, thresholds.PassRateAmber);

        var totalSubScoreWeight = subScoreWeights.CoverageWeight + subScoreWeights.PassRateWeight;
        var qualitySubScore = totalSubScoreWeight > 0
            ? (coverageScore * subScoreWeights.CoverageWeight + passRateScore * subScoreWeights.PassRateWeight) / totalSubScoreWeight
            : 0;

        qualitySubScore = Math.Round(qualitySubScore, 1);
        coverageScore = Math.Round(coverageScore, 1);
        passRateScore = Math.Round(passRateScore, 1);

        var qualityBreakdown = new QualityBreakdownInfo(
            coverageScore, subScoreWeights.CoverageWeight,
            passRateScore, subScoreWeights.PassRateWeight);

        return (qualitySubScore, qualityBreakdown);
    }

    private static PriorRates ComputePriorRates(
        Sprint priorSprint,
        List<TestExecution> priorSprintTEs,
        List<SprintMembership> priorMemberships,
        Dictionary<string, List<StatusTransition>> transitionsByTicket,
        List<string> orderedStages,
        int startIndex,
        AppSettings settings,
        string? subTeam)
    {
        var priorFiltered = FilterMemberships(priorMemberships, subTeam);
        // priorSprintTEs is pre-filtered by the repository (non-cancelled only).
        var priorNonCancelled = priorSprintTEs;
        var priorActiveKeys = GetActiveFeatureTicketKeys(
            priorFiltered, transitionsByTicket,
            priorSprint.StartDate, priorSprint.EndDate,
            orderedStages, startIndex,
            settings.ExcludedFromScopeStatuses, settings.DefaultSpPerBug);
        var priorTestsTesByTicket = BuildTestsTesByTicket(priorNonCancelled);
        var (priorCovered, priorCov) = ComputeCoverageRate(priorActiveKeys, priorTestsTesByTicket, priorMemberships);
        var priorRunsByTe = BuildRunsByTe(priorNonCancelled);
        var (_, priorExec) = ComputeExecutionRate(priorCovered, priorTestsTesByTicket, priorRunsByTe);
        return new PriorRates(priorCov, priorExec, ComputePassRate(priorNonCancelled));
    }

    private static List<SparklinePoint> BuildCoverageSparkline(SparklineInputs inputs) =>
        BuildQaSparkline(inputs, (nonCancelled, activeKeys, memberships2) =>
        {
            var testsMap = BuildTestsTesByTicket(nonCancelled);
            var (_, rate) = ComputeCoverageRate(activeKeys, testsMap, memberships2);
            return rate;
        });

    private static List<SparklinePoint> BuildExecutionSparkline(SparklineInputs inputs) =>
        BuildQaSparkline(inputs, (nonCancelled, activeKeys, memberships2) =>
        {
            var testsMap = BuildTestsTesByTicket(nonCancelled);
            var runsMap = BuildRunsByTe(nonCancelled);
            var (covered, _) = ComputeCoverageRate(activeKeys, testsMap, memberships2);
            var (_, rate) = ComputeExecutionRate(covered, testsMap, runsMap);
            return rate;
        });

    private static List<SparklinePoint> BuildPassRateSparkline(SparklineInputs inputs) =>
        BuildQaSparkline(inputs, (nonCancelled, _, _) => ComputePassRate(nonCancelled));

    private sealed record PriorRates(decimal? CoverageRate, decimal? ExecutionRate, decimal? PassRate);

    private sealed record SparklineInputs(
        List<Sprint> Window,
        Dictionary<int, List<TestExecution>> TesBySprintId,
        Dictionary<int, List<SprintMembership>> MembershipsBySprintId,
        Dictionary<string, List<StatusTransition>> TransitionsByTicket,
        AppSettings Settings,
        List<string> OrderedStages,
        int StartIndex,
        string? SubTeam);

    /// <summary>
    /// Failing ticket count: covered tickets where any linked TE has at least one Fail run.
    /// </summary>
    private static int ComputeFailingCount(
        HashSet<string> coveredKeys,
        Dictionary<string, List<string>> testsTesByTicket,
        Dictionary<string, List<TestRun>> runsByTe)
    {
        var count = 0;
        foreach (var key in coveredKeys)
        {
            var teIds = testsTesByTicket.GetValueOrDefault(key, []);
            var hasFail = teIds.Any(teId =>
            {
                var runs = runsByTe.GetValueOrDefault(teId, []);
                return runs.Any(r => r.Status == TestRunStatus.Fail);
            });
            if (hasFail) count++;
        }
        return count;
    }

    // --- Sparkline (BR22) ---

    private static List<SparklinePoint> BuildQaSparkline(
        SparklineInputs inputs,
        Func<List<TestExecution>, HashSet<string>, List<SprintMembership>, decimal> valueSelector)
    {
        var points = new List<SparklinePoint>();
        foreach (var sprint in inputs.Window)
        {
            // sprintTEs is pre-filtered by the repository (non-cancelled only).
            var sprintTEs = inputs.TesBySprintId.GetValueOrDefault(sprint.Id, []);
            var sprintMemberships = inputs.MembershipsBySprintId.GetValueOrDefault(sprint.Id, []);
            var filteredMemberships = FilterMemberships(sprintMemberships, inputs.SubTeam);
            var nonCancelled = sprintTEs;

            var activeKeys = GetActiveFeatureTicketKeys(
                filteredMemberships, inputs.TransitionsByTicket,
                sprint.StartDate, sprint.EndDate,
                inputs.OrderedStages, inputs.StartIndex,
                inputs.Settings.ExcludedFromScopeStatuses, inputs.Settings.DefaultSpPerBug);

            var value = valueSelector(nonCancelled, activeKeys, sprintMemberships);
            points.Add(new SparklinePoint(sprint.Name, Math.Round(value, 1)));
        }
        return points;
    }

    // --- Metric card builder ---

    // All QA metrics are positive-up
    private static MetricCard BuildQaMetricCard(
        string name,
        decimal value,
        decimal? priorValue,
        List<SparklinePoint> sparkline,
        string rag)
    {
        decimal? delta = priorValue.HasValue ? value - priorValue.Value : null;
        return BuildMetricCard(name, value, $"{value:0.#}%", delta, "positive-up", sparkline) with
        {
            Rag = rag
        };
    }
}
