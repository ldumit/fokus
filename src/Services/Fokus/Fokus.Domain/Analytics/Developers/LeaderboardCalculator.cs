using Fokus.Domain.Analytics.Shared;
using static Fokus.Domain.Analytics.Shared.CapacityResolver;
using static Fokus.Domain.Analytics.Shared.DeltaResolver;
using static Fokus.Domain.Analytics.Shared.IssueTypeChecker;
using static Fokus.Domain.Analytics.Shared.SubTeamFilter;
using static Fokus.Domain.Analytics.Shared.TransitionAttributionChecker;

namespace Fokus.Domain.Analytics.Developers;

public class LeaderboardCalculator
{
    public LeaderboardMultiSprintResult ComputeMultiSprint(
        List<Sprint> targetSprints,
        List<Developer> activeDevelopers,
        AppSettings settings,
        List<StatusTransition> statusTransitions,
        string? subTeam,
        Dictionary<string, Dictionary<int, int>> capacityLookup,
        List<Developer> allDevelopers,
        HashSet<string>? excludedDeveloperIds = null)
    {
        var context = BuildContext(settings, statusTransitions, subTeam, capacityLookup, allDevelopers);

        var sortedTarget = targetSprints.OrderBy(s => s.StartDate).ToList();
        var filteredDevelopers = FilterDevelopers(activeDevelopers, subTeam);

        var sprintInfos = sortedTarget
            .Select(s => new SprintRef(s.Id, s.Name, s.StartDate, s.EndDate))
            .ToList();

        var developerEntries = filteredDevelopers
            .Select(dev => BuildMultiSprintEntry(dev, sortedTarget, context))
            .OrderByDescending(d => d.TotalSp)
            .ThenBy(d => d.DisplayName)
            .ToList();

        return new LeaderboardMultiSprintResult(sprintInfos, developerEntries);
    }

    public LeaderboardSingleSprintResult ComputeSingleSprint(
        Sprint targetSprint,
        Sprint? priorSprint,
        List<Developer> activeDevelopers,
        AppSettings settings,
        List<StatusTransition> statusTransitions,
        string? subTeam,
        Dictionary<string, Dictionary<int, int>> capacityLookup,
        List<Developer> allDevelopers,
        HashSet<string>? excludedDeveloperIds = null)
    {
        var context = BuildContext(settings, statusTransitions, subTeam, capacityLookup, allDevelopers);
        var filteredDevelopers = FilterDevelopers(activeDevelopers, subTeam);

        var sprintInfo = new SprintRef(
            targetSprint.Id, targetSprint.Name, targetSprint.StartDate, targetSprint.EndDate);

        var developerEntries = filteredDevelopers
            .Select(dev => BuildSingleSprintEntry(dev, targetSprint, priorSprint, context))
            .OrderByDescending(d => d.TotalSp)
            .ThenBy(d => d.DisplayName)
            .ToList();

        return new LeaderboardSingleSprintResult(sprintInfo, developerEntries);
    }

    private static LeaderboardContext BuildContext(
        AppSettings settings,
        List<StatusTransition> statusTransitions,
        string? subTeam,
        Dictionary<string, Dictionary<int, int>> capacityLookup,
        List<Developer> allDevelopers)
    {
        var (orderedStages, _) = ResolveStartIndex(settings);
        var endIndex = ResolveEndIndex(settings, orderedStages);
        return new LeaderboardContext(
            StatusTransitionIndex.Build(statusTransitions), orderedStages, endIndex,
            settings.ExcludedFromScopeStatuses, settings.DefaultSpPerBug, subTeam, capacityLookup, allDevelopers);
    }

    private static LeaderboardDeveloperEntry BuildMultiSprintEntry(
        Developer dev,
        List<Sprint> sortedTarget,
        LeaderboardContext context)
    {
        var sprintBreakdowns = sortedTarget.Select(s =>
        {
            var tally = Tally(dev.Id, s, context);
            var capacityPercent = GetCapacity(context.CapacityLookup, dev.Id, s.Id, context.AllDevelopers);
            return new LeaderboardDeveloperSprintBreakdown(
                s.Id,
                Math.Round(tally.FeatureSp, 1),
                Math.Round(tally.BugSp, 1),
                Math.Round(tally.TotalSp, 1),
                tally.FeatureTickets,
                tally.BugTickets,
                capacityPercent);
        }).ToList();

        var devFeatureSp = sprintBreakdowns.Sum(b => b.FeatureSp);
        var devBugSp = sprintBreakdowns.Sum(b => b.BugSp);
        var devTotalSp = devFeatureSp + devBugSp;
        var devFeatureTickets = sprintBreakdowns.Sum(b => b.FeatureTickets);
        var devBugTickets = sprintBreakdowns.Sum(b => b.BugTickets);
        var devTotalTickets = devFeatureTickets + devBugTickets;
        var avgCapacity = sprintBreakdowns.Count > 0
            ? (int)Math.Round(sprintBreakdowns.Average(b => b.CapacityPercent))
            : 100;

        return new LeaderboardDeveloperEntry(
            dev.Id,
            dev.DisplayName,
            dev.SubTeam,
            dev.AvatarUrl,
            Math.Round(devFeatureSp, 1),
            Math.Round(devBugSp, 1),
            Math.Round(devTotalSp, 1),
            devFeatureTickets,
            devBugTickets,
            devTotalTickets,
            avgCapacity,
            sprintBreakdowns);
    }

    private static LeaderboardDeveloperSingleEntry BuildSingleSprintEntry(
        Developer dev,
        Sprint targetSprint,
        Sprint? priorSprint,
        LeaderboardContext context)
    {
        var current = Tally(dev.Id, targetSprint, context);
        var delta = priorSprint is not null ? BuildDelta(current, Tally(dev.Id, priorSprint, context)) : null;
        var capacityPercent = GetCapacity(context.CapacityLookup, dev.Id, targetSprint.Id, context.AllDevelopers);

        return new LeaderboardDeveloperSingleEntry(
            dev.Id,
            dev.DisplayName,
            dev.SubTeam,
            dev.AvatarUrl,
            Math.Round(current.FeatureSp, 1),
            Math.Round(current.BugSp, 1),
            Math.Round(current.TotalSp, 1),
            current.FeatureTickets,
            current.BugTickets,
            current.FeatureTickets + current.BugTickets,
            capacityPercent,
            delta);
    }

    private static LeaderboardDeveloperDelta BuildDelta(LeaderboardTally current, LeaderboardTally prior)
    {
        var featureSpDelta = current.FeatureSp - prior.FeatureSp;
        var bugSpDelta = current.BugSp - prior.BugSp;
        var totalSpDelta = current.TotalSp - prior.TotalSp;
        var featureTicketsDelta = current.FeatureTickets - prior.FeatureTickets;
        var bugTicketsDelta = current.BugTickets - prior.BugTickets;

        return new LeaderboardDeveloperDelta(
            Math.Round(featureSpDelta, 1), DeltaDirection(featureSpDelta), DeltaPolarity(featureSpDelta, positiveUp: true),
            Math.Round(bugSpDelta, 1), DeltaDirection(bugSpDelta), DeltaPolarity(bugSpDelta, positiveUp: false),
            Math.Round(totalSpDelta, 1), DeltaDirection(totalSpDelta), "neutral",
            featureTicketsDelta, DeltaDirection(featureTicketsDelta), DeltaPolarity(featureTicketsDelta, positiveUp: true),
            bugTicketsDelta, DeltaDirection(bugTicketsDelta), DeltaPolarity(bugTicketsDelta, positiveUp: false));
    }

    private static LeaderboardTally Tally(string developerId, Sprint sprint, LeaderboardContext context)
    {
        var memberships = GetDeveloperMemberships(sprint, developerId, context.SubTeam);
        var completed = GetTransitionCompletedMemberships(
            memberships, context.TransitionIndex, sprint.StartDate, sprint.EndDate,
            context.OrderedStages, context.EndIndex, context.ExcludedStatuses);
        var featureSp = completed.Where(m => !IsBug(m)).Sum(m => m.GetEffectiveSp(context.DefaultSpPerBug) ?? 0m);
        var bugSp = completed.Where(m => IsBug(m)).Sum(m => m.GetEffectiveSp(context.DefaultSpPerBug) ?? 0m);
        return new LeaderboardTally(featureSp, bugSp, completed.Count(m => !IsBug(m)), completed.Count(m => IsBug(m)));
    }

    private sealed record LeaderboardContext(
        StatusTransitionIndex TransitionIndex,
        List<string> OrderedStages,
        int EndIndex,
        List<string> ExcludedStatuses,
        int DefaultSpPerBug,
        string? SubTeam,
        Dictionary<string, Dictionary<int, int>> CapacityLookup,
        List<Developer> AllDevelopers);

    private sealed record LeaderboardTally(decimal FeatureSp, decimal BugSp, int FeatureTickets, int BugTickets)
    {
        public decimal TotalSp => FeatureSp + BugSp;
    }
}
