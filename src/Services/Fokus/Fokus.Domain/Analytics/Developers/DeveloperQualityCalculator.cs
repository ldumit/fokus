using Fokus.Domain.Analytics.Shared;
using static Fokus.Domain.Analytics.Shared.DeltaResolver;
using static Fokus.Domain.Analytics.Shared.SubTeamFilter;
using static Fokus.Domain.Analytics.Shared.TestCoverageCalculator;
using static Fokus.Domain.Analytics.Shared.TransitionAttributionChecker;

namespace Fokus.Domain.Analytics.Developers;

public class DeveloperQualityCalculator
{
    public DeveloperQualityResult ComputeDeveloperQuality(
        List<Sprint> allLoadedSprints,
        List<int> targetSprintIds,
        List<Developer> activeDevelopers,
        Dictionary<int, List<TestExecution>> tesBySprintId,
        List<StatusTransition> statusTransitions,
        AppSettings settings,
        string? subTeam,
        bool isSingleSprint,
        Sprint? priorSprint,
        List<TestExecution>? priorSprintTEs,
        List<Sprint> sparklineWindow,
        Dictionary<int, List<TestExecution>> sparklineTEsBySprintId,
        List<Sprint> streakWindow,
        Dictionary<int, List<TestExecution>> streakTEsBySprintId)
    {
        var (orderedStages, startIndex) = ResolveStartIndex(settings);

        // Sub-team filtering on developers
        var filteredDevelopers = FilterDevelopers(activeDevelopers, subTeam);

        // Sort all loaded sprints ascending
        var sortedAllSprints = allLoadedSprints.OrderBy(s => s.StartDate).ToList();

        // Target sprints in ascending order
        var targetSprints = sortedAllSprints
            .Where(s => targetSprintIds.Contains(s.Id))
            .OrderBy(s => s.StartDate)
            .ToList();

        // Pre-group transitions by ticketId once
        var transitionsByTicket = statusTransitions
            .GroupBy(t => t.TicketId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);

        // Sprint info list
        var sprintInfos = targetSprints
            .Select(s => new SprintRef(s.Id, s.Name, s.StartDate, s.EndDate))
            .ToList();

        // Check if there's any QA data at all
        var hasQaData = tesBySprintId.Values.Any(tes => tes.Count > 0);

        var context = new QualityContext(
            tesBySprintId, transitionsByTicket, settings, orderedStages, startIndex, subTeam,
            isSingleSprint && priorSprint is not null && priorSprintTEs is not null && priorSprintTEs.Count > 0
                ? priorSprint
                : null,
            priorSprintTEs,
            isSingleSprint && sparklineWindow.Count > 0 ? sparklineWindow : null,
            sparklineTEsBySprintId);

        // Build per-developer entries
        var developerEntries = filteredDevelopers.Select(developer =>
        {
            var breakdowns = targetSprints
                .Select(sprint => BuildBreakdown(developer.Id, sprint, context))
                .ToList();

            // Below-median streak (single-sprint only, computed after all breakdowns)
            int? belowMedianStreak = isSingleSprint && targetSprints.Count == 1
                ? ComputeBelowMedianStreak(
                    developer.Id, targetSprints[0], streakWindow, streakTEsBySprintId, transitionsByTicket,
                    filteredDevelopers, settings, orderedStages, startIndex, subTeam)
                : null;

            return new DeveloperQualityEntry(
                developer.Id, developer.DisplayName, developer.SubTeam, developer.AvatarUrl, breakdowns, belowMedianStreak);
        }).ToList();

        return new DeveloperQualityResult(hasQaData, sprintInfos, developerEntries);
    }

    private static DeveloperQualitySprintBreakdown BuildBreakdown(string developerId, Sprint sprint, QualityContext context)
    {
        var sprintTEs = context.TesBySprintId.GetValueOrDefault(sprint.Id, []);

        var (stories, covered, coveragePercent, passRatePercent, untested, bugsFound) = ComputeDevMetrics(
            developerId, sprint, sprintTEs, context.TransitionsByTicket, context.Settings,
            context.OrderedStages, context.StartIndex, context.SubTeam);

        var thresholds = context.Settings.QaHealthThresholds;
        var coverageRag = HealthScoreCalculator.MetricRag(coveragePercent, thresholds.CoverageGreen, thresholds.CoverageAmber, higherIsBetter: true);
        var passRateRag = HealthScoreCalculator.MetricRag(passRatePercent, thresholds.PassRateGreen, thresholds.PassRateAmber, higherIsBetter: true);

        // Deltas (single-sprint only, with prior sprint data)
        var deltas = context.PriorSprint is not null
            ? ComputeDeltas(developerId, coveragePercent, passRatePercent, bugsFound, context.PriorSprint, context)
            : null;

        var sparklineWindow = context.SparklineWindow;

        return new DeveloperQualitySprintBreakdown(
            sprint.Id,
            stories,
            covered,
            Math.Round(coveragePercent, 1),
            Math.Round(passRatePercent, 1),
            untested,
            bugsFound,
            coverageRag,
            passRateRag,
            deltas?.Coverage,
            deltas is null ? null : DeltaDirection(deltas.Coverage),
            deltas is null ? null : DeltaPolarity(deltas.Coverage, positiveUp: true),
            deltas?.PassRate,
            deltas is null ? null : DeltaDirection(deltas.PassRate),
            deltas is null ? null : DeltaPolarity(deltas.PassRate, positiveUp: true),
            deltas?.Bugs,
            deltas is null ? null : DeltaDirection(deltas.Bugs),
            deltas is null ? null : "neutral",
            sparklineWindow is null ? null : BuildDevSparkline(developerId, sparklineWindow, context, usePassRate: false),
            sparklineWindow is null ? null : BuildDevSparkline(developerId, sparklineWindow, context, usePassRate: true));
    }

    private static QualityDeltas ComputeDeltas(
        string developerId,
        decimal coveragePercent,
        decimal passRatePercent,
        int bugsFound,
        Sprint priorSprint,
        QualityContext context)
    {
        var (_, _, priorCoverage, priorPassRate, _, priorBugs) = ComputeDevMetrics(
            developerId, priorSprint, context.PriorSprintTEs!, context.TransitionsByTicket, context.Settings,
            context.OrderedStages, context.StartIndex, context.SubTeam);

        return new QualityDeltas(
            Math.Round(coveragePercent - priorCoverage, 1),
            Math.Round(passRatePercent - priorPassRate, 1),
            bugsFound - priorBugs);
    }

    private sealed record QualityContext(
        Dictionary<int, List<TestExecution>> TesBySprintId,
        Dictionary<string, List<StatusTransition>> TransitionsByTicket,
        AppSettings Settings,
        List<string> OrderedStages,
        int StartIndex,
        string? SubTeam,
        Sprint? PriorSprint,
        List<TestExecution>? PriorSprintTEs,
        List<Sprint>? SparklineWindow,
        Dictionary<int, List<TestExecution>> SparklineTEsBySprintId);

    private sealed record QualityDeltas(decimal Coverage, decimal PassRate, int Bugs);

    // --- Per-developer metrics for a single sprint ---

    private static (int Stories, int Covered, decimal CoveragePercent, decimal PassRatePercent, int Untested, int BugsFound)
        ComputeDevMetrics(
            string developerId,
            Sprint sprint,
            List<TestExecution> sprintTEs,
            Dictionary<string, List<StatusTransition>> transitionsByTicket,
            AppSettings settings,
            List<string> orderedStages,
            int startIndex,
            string? subTeam)
    {
        // Active scope for this developer: feature tickets that IsStartedInSprint
        var devMemberships = sprint.Memberships
            .Where(m => m.Ticket?.AssigneeId == developerId &&
                        (string.IsNullOrWhiteSpace(subTeam) || m.Ticket?.Assignee?.SubTeam == subTeam))
            .ToList();

        var activeTicketKeys = GetActiveFeatureTicketKeys(
            devMemberships, transitionsByTicket,
            sprint.StartDate, sprint.EndDate,
            orderedStages, startIndex,
            settings.ExcludedFromScopeStatuses, settings.DefaultSpPerBug);

        if (activeTicketKeys.Count == 0)
            return (0, 0, 0m, 0m, 0, 0);

        // Build parent lookup from all sprint memberships (for sub-task inheritance)
        var parentByTicket = sprint.Memberships
            .Where(m => m.Ticket?.ParentTicketKey != null)
            .ToDictionary(m => m.TicketId, m => m.Ticket!.ParentTicketKey!, StringComparer.OrdinalIgnoreCase);

        // Build tests-link map from non-cancelled TEs
        var testsTesByTicket = BuildTestsTesByTicket(sprintTEs);

        var coveredKeys = FindCoveredKeys(activeTicketKeys, testsTesByTicket, parentByTicket);

        var stories = activeTicketKeys.Count;
        var covered = coveredKeys.Count;
        var coveragePercent = stories > 0 ? (decimal)covered / stories * 100 : 0m;
        var untested = stories - covered;

        var (passRatePercent, bugsFound) = ComputeDirectTestResults(activeTicketKeys, testsTesByTicket, sprintTEs);

        return (stories, covered, coveragePercent, passRatePercent, untested, bugsFound);
    }

    // Coverage per developer (BR2-BR3) with sub-task inheritance (BR10)
    private static HashSet<string> FindCoveredKeys(
        HashSet<string> activeTicketKeys,
        Dictionary<string, List<string>> testsTesByTicket,
        Dictionary<string, string> parentByTicket)
    {
        var coveredKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var key in activeTicketKeys)
        {
            if (testsTesByTicket.ContainsKey(key) && testsTesByTicket[key].Count > 0)
            {
                coveredKeys.Add(key);
                continue;
            }
            // Sub-task inheritance: if no own TEs, check parent
            if (parentByTicket.TryGetValue(key, out var parentKey) &&
                !string.IsNullOrEmpty(parentKey) &&
                testsTesByTicket.TryGetValue(parentKey, out var parentTeIds) &&
                parentTeIds.Count > 0)
            {
                coveredKeys.Add(key);
            }
        }
        return coveredKeys;
    }

    // Pass rate (BR4) and bugs found (BR6): only TEs directly linked to this developer's
    // active scope tickets. Sub-task inheritance (BR10) is coverage-only — it marks a story
    // as covered but does not pull the parent's test runs into the developer's pass rate or
    // bug count.
    private static (decimal PassRatePercent, int BugsFound) ComputeDirectTestResults(
        HashSet<string> activeTicketKeys,
        Dictionary<string, List<string>> testsTesByTicket,
        List<TestExecution> sprintTEs)
    {
        var directTeIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var key in activeTicketKeys)
        {
            if (testsTesByTicket.TryGetValue(key, out var teIds))
                foreach (var id in teIds) directTeIds.Add(id);
        }

        var devTEs = sprintTEs.Where(te => directTeIds.Contains(te.Id)).ToList();

        // Bugs found (BR6): unique bug tickets from Blocks links on this developer's TEs
        return (ComputePassRate(devTEs), ComputeBugsFound(devTEs));
    }

    // --- Sparkline for a developer ---

    private static List<SparklinePoint> BuildDevSparkline(
        string developerId,
        List<Sprint> window,
        QualityContext context,
        bool usePassRate)
    {
        var points = new List<SparklinePoint>();
        foreach (var sprint in window)
        {
            var sprintTEs = context.SparklineTEsBySprintId.GetValueOrDefault(sprint.Id, []);
            var (_, _, coveragePercent, passRatePercent, _, _) = ComputeDevMetrics(
                developerId, sprint, sprintTEs, context.TransitionsByTicket, context.Settings,
                context.OrderedStages, context.StartIndex, context.SubTeam);
            var value = usePassRate ? passRatePercent : coveragePercent;
            points.Add(new SparklinePoint(sprint.Name, Math.Round(value, 1)));
        }
        return points;
    }

    // --- Below-median streak (BR14-17, single-sprint only) ---

    private static int? ComputeBelowMedianStreak(
        string developerId,
        Sprint targetSprint,
        List<Sprint> streakSprints,
        Dictionary<int, List<TestExecution>> streakTEsBySprintId,
        Dictionary<string, List<StatusTransition>> transitionsByTicket,
        List<Developer> filteredDevelopers,
        AppSettings settings,
        List<string> orderedStages,
        int startIndex,
        string? subTeam)
    {
        // Walk backward from target sprint through consecutive closed sprints with QA data.
        // streakSprints is pre-filtered to only sprints with QA data (BR8-attributed),
        // ordered ascending, capped at up to 12 sprints ending at the target sprint.
        var targetIndex = streakSprints.FindIndex(s => s.Id == targetSprint.Id);
        if (targetIndex < 0) return null;

        var streak = 0;
        for (var i = targetIndex; i >= 0; i--)
        {
            var sprint = streakSprints[i];

            var sprintTEs = streakTEsBySprintId.GetValueOrDefault(sprint.Id, []);

            // Sprint with no QA data breaks the streak
            if (sprintTEs.Count == 0) break;

            // Get this developer's metrics for this sprint
            var (devStories, _, devCoverage, _, _, _) =
                ComputeDevMetrics(developerId, sprint, sprintTEs, transitionsByTicket, settings, orderedStages, startIndex, subTeam);

            // Sprint where developer has zero stories breaks the streak (BR15)
            if (devStories == 0) break;

            // Compute team median coverage for this sprint (BR14, BR16)
            var teamCoverageValues = new List<decimal>();
            foreach (var dev in filteredDevelopers)
            {
                var (s, _, cov, _, _, _) =
                    ComputeDevMetrics(dev.Id, sprint, sprintTEs, transitionsByTicket, settings, orderedStages, startIndex, subTeam);
                if (s > 0) // Exclude zero-story developers from median
                    teamCoverageValues.Add(cov);
            }

            if (teamCoverageValues.Count == 0) break;

            var median = ComputeMedian(teamCoverageValues);

            // Check if strictly below median
            if (devCoverage < median)
                streak++;
            else
                break;
        }

        return streak >= 2 ? streak : null;
    }

    private static decimal ComputeMedian(List<decimal> values)
    {
        if (values.Count == 0) return 0m;
        var sorted = values.OrderBy(v => v).ToList();
        var mid = sorted.Count / 2;
        return sorted.Count % 2 == 0
            ? (sorted[mid - 1] + sorted[mid]) / 2
            : sorted[mid];
    }
}
