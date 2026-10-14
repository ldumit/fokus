using Fokus.Domain.Analytics.Shared;
using static Fokus.Domain.Analytics.Shared.DeltaResolver;
using static Fokus.Domain.Analytics.Shared.IssueTypeChecker;
using static Fokus.Domain.Analytics.Shared.SubTeamFilter;

namespace Fokus.Domain.Analytics.Sprints;

public class BugRatioCalculator
{
    public BugRatioMultiSprintResult ComputeMultiSprint(
        List<Sprint> targetSprints,
        List<Sprint> allClosedSprints,
        List<Developer> activeDevelopers,
        AppSettings settings,
        List<StatusTransition> statusTransitions,
        string? subTeam,
        HashSet<string>? excludedDeveloperIds = null)
    {
        var context = BuildContext(settings, statusTransitions, subTeam, allClosedSprints);

        var sortedTarget = targetSprints.OrderBy(s => s.StartDate).ToList();
        var filteredDevelopers = FilterDevelopers(activeDevelopers, subTeam);

        var sprintInfos = sortedTarget
            .Select(s => new SprintRef(s.Id, s.Name, s.StartDate, s.EndDate))
            .ToList();

        // Team-level per-sprint trend
        var perSprintTrend = sortedTarget.Select(s =>
        {
            var tally = Tally(FilterMemberships(s.Memberships, subTeam, excludedDeveloperIds), s, context);
            return new BugRatioTeamTrendEntry(
                s.Id,
                Math.Round(tally.Ratio, 1),
                Math.Round(tally.BugSp, 1),
                Math.Round(tally.NonBugSp, 1),
                Math.Round(tally.CompletedSp, 1));
        }).ToList();

        // Team-level totals (ratio of totals, not average of ratios — BR8)
        var totalBugSp = perSprintTrend.Sum(t => t.BugSp);
        var totalNonBugSp = perSprintTrend.Sum(t => t.NonBugSp);
        var totalCompletedSp = totalBugSp + totalNonBugSp;
        var teamRatio = totalCompletedSp > 0 ? totalBugSp / totalCompletedSp * 100 : 0m;

        var teamMetrics = new BugRatioTeamMetrics(
            Math.Round(teamRatio, 1),
            Math.Round(totalBugSp, 1),
            Math.Round(totalNonBugSp, 1),
            Math.Round(totalCompletedSp, 1),
            perSprintTrend);

        // Issue type breakdown across all target sprints (BR18)
        var allCompletedMemberships = sortedTarget
            .SelectMany(s => GetTransitionCompletedMemberships(
                FilterMemberships(s.Memberships, subTeam, excludedDeveloperIds), s, context))
            .ToList();
        var issueTypeBreakdown = BuildIssueTypeBreakdown(allCompletedMemberships, context.DefaultSpPerBug);

        // Per-developer entries
        var developerEntries = filteredDevelopers
            .Select(dev => BuildMultiSprintDeveloperEntry(dev, sortedTarget, context))
            .ToList();

        return new BugRatioMultiSprintResult(sprintInfos, teamMetrics, issueTypeBreakdown, developerEntries);
    }

    public BugRatioSingleSprintResult ComputeSingleSprint(
        Sprint targetSprint,
        Sprint? priorSprint,
        List<Sprint> allClosedSprints,
        List<Developer> activeDevelopers,
        AppSettings settings,
        List<StatusTransition> statusTransitions,
        string? subTeam,
        HashSet<string>? excludedDeveloperIds = null)
    {
        var context = BuildContext(settings, statusTransitions, subTeam, allClosedSprints);
        var filteredDevelopers = FilterDevelopers(activeDevelopers, subTeam);

        var sprintInfo = new SprintRef(
            targetSprint.Id, targetSprint.Name, targetSprint.StartDate, targetSprint.EndDate);

        // Team metrics for current sprint
        var memberships = FilterMemberships(targetSprint.Memberships, subTeam, excludedDeveloperIds);
        var current = Tally(memberships, targetSprint, context);

        // Prior sprint team metrics for deltas
        var prior = priorSprint is not null
            ? Tally(FilterMemberships(priorSprint.Memberships, subTeam, excludedDeveloperIds), priorSprint, context)
            : null;

        var teamMetrics = new BugRatioTeamSingleMetrics(
            BuildMetricCard("Bug Ratio %", current.Ratio, $"{current.Ratio:0.#}%",
                prior is not null ? current.Ratio - prior.Ratio : null, "positive-down"),
            BuildMetricCard("Bug SP", current.BugSp, $"{current.BugSp:0.#}",
                prior is not null ? current.BugSp - prior.BugSp : null, "positive-down"),
            BuildMetricCard("Non-Bug SP", current.NonBugSp, $"{current.NonBugSp:0.#}",
                prior is not null ? current.NonBugSp - prior.NonBugSp : null, "positive-up"));

        // Issue type breakdown for current sprint completed tickets (BR18)
        var issueTypeBreakdown = BuildIssueTypeBreakdown(current.Completed, context.DefaultSpPerBug);

        // Per-developer entries
        var developerEntries = filteredDevelopers
            .Select(dev => BuildSingleSprintDeveloperEntry(dev, targetSprint, priorSprint, context))
            .ToList();

        return new BugRatioSingleSprintResult(sprintInfo, teamMetrics, issueTypeBreakdown, developerEntries);
    }

    private static BugRatioContext BuildContext(
        AppSettings settings,
        List<StatusTransition> statusTransitions,
        string? subTeam,
        List<Sprint> allClosedSprints)
    {
        var (orderedStages, _) = TransitionAttributionChecker.ResolveStartIndex(settings);
        var endIndex = TransitionAttributionChecker.ResolveEndIndex(settings, orderedStages);
        return new BugRatioContext(
            StatusTransitionIndex.Build(statusTransitions), orderedStages, endIndex,
            settings.ExcludedFromScopeStatuses, settings.DefaultSpPerBug, subTeam, settings, allClosedSprints);
    }

    private BugRatioDeveloperEntry BuildMultiSprintDeveloperEntry(
        Developer dev,
        List<Sprint> sortedTarget,
        BugRatioContext context)
    {
        var sprintBreakdowns = sortedTarget.Select(s =>
        {
            var tally = Tally(GetDeveloperMemberships(s, dev.Id, context.SubTeam), s, context);
            return new BugRatioDeveloperSprintBreakdown(
                s.Id,
                Math.Round(tally.BugSp, 1),
                Math.Round(tally.NonBugSp, 1),
                Math.Round(tally.CompletedSp, 1),
                Math.Round(tally.Ratio, 1),
                tally.BugCount,
                tally.NonBugCount);
        }).ToList();

        var devBugSp = sprintBreakdowns.Sum(b => b.BugSp);
        var devNonBugSp = sprintBreakdowns.Sum(b => b.NonBugSp);
        var devCompletedSp = devBugSp + devNonBugSp;
        var devRatio = devCompletedSp > 0 ? devBugSp / devCompletedSp * 100 : 0m;
        var devBugCount = sprintBreakdowns.Sum(b => b.BugTicketCount);
        var devNonBugCount = sprintBreakdowns.Sum(b => b.NonBugTicketCount);

        return new BugRatioDeveloperEntry(
            dev.Id, dev.DisplayName, dev.SubTeam, dev.AvatarUrl,
            Math.Round(devBugSp, 1),
            Math.Round(devNonBugSp, 1),
            Math.Round(devCompletedSp, 1),
            Math.Round(devRatio, 1),
            devBugCount,
            devNonBugCount,
            sprintBreakdowns,
            EvaluateAlert(dev.Id, context));
    }

    private BugRatioDeveloperSingleEntry BuildSingleSprintDeveloperEntry(
        Developer dev,
        Sprint targetSprint,
        Sprint? priorSprint,
        BugRatioContext context)
    {
        var current = Tally(GetDeveloperMemberships(targetSprint, dev.Id, context.SubTeam), targetSprint, context);

        var delta = priorSprint is not null
            ? BuildDeveloperDelta(current, Tally(GetDeveloperMemberships(priorSprint, dev.Id, context.SubTeam), priorSprint, context))
            : null;

        return new BugRatioDeveloperSingleEntry(
            dev.Id, dev.DisplayName, dev.SubTeam, dev.AvatarUrl,
            Math.Round(current.BugSp, 1),
            Math.Round(current.NonBugSp, 1),
            Math.Round(current.CompletedSp, 1),
            Math.Round(current.Ratio, 1),
            current.BugCount,
            current.NonBugCount,
            delta,
            EvaluateAlert(dev.Id, context));
    }

    private static BugRatioDeveloperDelta BuildDeveloperDelta(BugTally current, BugTally prior)
    {
        var bugSpDelta = current.BugSp - prior.BugSp;
        var nonBugSpDelta = current.NonBugSp - prior.NonBugSp;
        var ratioDelta = current.Ratio - prior.Ratio;
        var bugCountDelta = current.BugCount - prior.BugCount;
        var nonBugCountDelta = current.NonBugCount - prior.NonBugCount;

        return new BugRatioDeveloperDelta(
            Math.Round(bugSpDelta, 1), DeltaDirection(bugSpDelta), DeltaPolarity(bugSpDelta, positiveUp: false),
            Math.Round(nonBugSpDelta, 1), DeltaDirection(nonBugSpDelta), DeltaPolarity(nonBugSpDelta, positiveUp: true),
            Math.Round(ratioDelta, 1), DeltaDirection(ratioDelta), DeltaPolarity(ratioDelta, positiveUp: false),
            bugCountDelta, DeltaDirection(bugCountDelta), DeltaPolarity(bugCountDelta, positiveUp: false),
            nonBugCountDelta, DeltaDirection(nonBugCountDelta), DeltaPolarity(nonBugCountDelta, positiveUp: true));
    }

    // --- Alert evaluation (BR9, BR10, BR11) ---

    private BugRatioAlertStatus EvaluateAlert(string developerId, BugRatioContext context)
    {
        var threshold = context.Settings.BugRatioAlertThreshold;
        var consecutiveCount = context.Settings.BugRatioConsecutiveSprintCount;

        // Evaluate sprints from most recent backward (BR9)
        var sortedDesc = context.AllClosedSprints.OrderByDescending(s => s.StartDate).ToList();

        var consecutiveAbove = 0;
        foreach (var sprint in sortedDesc)
        {
            var memberships = GetDeveloperMemberships(sprint, developerId, context.SubTeam);
            var (bugSp, nonBugSp) = ComputeCompletedBugNonBugSp(memberships, sprint, context);
            var completedSp = bugSp + nonBugSp;

            // BR11: zero SP sprint resets streak
            if (completedSp == 0)
                break;

            var ratio = bugSp / completedSp * 100;
            if (ratio >= threshold)
            {
                consecutiveAbove++;
            }
            else
            {
                break;
            }
        }

        return new BugRatioAlertStatus(consecutiveAbove >= consecutiveCount, consecutiveAbove, threshold);
    }

    // --- Transition-based completed memberships ---

    private static List<SprintMembership> GetTransitionCompletedMemberships(
        List<SprintMembership> memberships,
        Sprint sprint,
        BugRatioContext context)
    {
        return TransitionAttributionChecker.GetTransitionCompletedMemberships(
            memberships, context.TransitionIndex, sprint.StartDate, sprint.EndDate,
            context.OrderedStages, context.EndIndex, context.ExcludedStatuses);
    }

    /// <summary>
    /// Computes bug and non-bug completed SP using transition-based attribution.
    /// </summary>
    private static (decimal BugSp, decimal NonBugSp) ComputeCompletedBugNonBugSp(
        List<SprintMembership> memberships,
        Sprint sprint,
        BugRatioContext context)
    {
        var tally = Tally(memberships, sprint, context);
        return (tally.BugSp, tally.NonBugSp);
    }

    private static BugTally Tally(List<SprintMembership> memberships, Sprint sprint, BugRatioContext context)
    {
        var completed = GetTransitionCompletedMemberships(memberships, sprint, context);

        var bugSp = completed.Where(IsBug).Sum(m => m.GetEffectiveSp(context.DefaultSpPerBug) ?? 0m);
        var nonBugSp = completed.Where(m => !IsBug(m)).Sum(m => m.GetEffectiveSp(context.DefaultSpPerBug) ?? 0m);
        return new BugTally(completed, bugSp, nonBugSp, completed.Count(IsBug), completed.Count(m => !IsBug(m)));
    }

    // --- Issue type breakdown (BR18) ---

    private static List<BugRatioIssueTypeEntry> BuildIssueTypeBreakdown(
        List<SprintMembership> completedMemberships,
        int defaultSpPerBug = 0)
    {
        return completedMemberships
            .GroupBy(m => m.Ticket?.IssueType ?? "Unknown")
            .Select(g =>
            {
                var spTotal = g.Sum(m => m.GetEffectiveSp(defaultSpPerBug) ?? 0m);
                return new BugRatioIssueTypeEntry(g.Key, g.Count(), Math.Round(spTotal, 1));
            })
            .OrderByDescending(e => e.TicketCount)
            .ToList();
    }

    // --- Metric card builder ---

    private static BugRatioMetricCard BuildMetricCard(
        string name,
        decimal value,
        string displayValue,
        decimal? delta,
        string polarity)
    {
        var (direction, deltaPolarity) = ResolveCardDelta(delta, polarity);

        return new BugRatioMetricCard(
            name,
            Math.Round(value, 1),
            displayValue,
            delta.HasValue ? Math.Round(delta.Value, 1) : null,
            direction,
            deltaPolarity);
    }

    private sealed record BugRatioContext(
        StatusTransitionIndex TransitionIndex,
        List<string> OrderedStages,
        int EndIndex,
        List<string> ExcludedStatuses,
        int DefaultSpPerBug,
        string? SubTeam,
        AppSettings Settings,
        List<Sprint> AllClosedSprints);

    private sealed record BugTally(
        List<SprintMembership> Completed,
        decimal BugSp,
        decimal NonBugSp,
        int BugCount,
        int NonBugCount)
    {
        public decimal CompletedSp => BugSp + NonBugSp;
        public decimal Ratio => CompletedSp > 0 ? BugSp / CompletedSp * 100 : 0m;
    }
}
