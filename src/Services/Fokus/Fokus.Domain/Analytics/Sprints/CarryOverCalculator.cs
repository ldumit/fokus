using Fokus.Domain.Analytics.Shared;
using static Fokus.Domain.Analytics.Shared.SubTeamFilter;
using static Fokus.Domain.Analytics.Sprints.ScopeMetricCardFactory;

namespace Fokus.Domain.Analytics.Sprints;

public class CarryOverCalculator
{
    public CarryOverMultiSprintResult ComputeMultiSprint(
        List<Sprint> selectedSprints,
        List<Sprint> allSyncedSprints,
        List<StatusTransition> statusTransitions,
        AppSettings settings,
        string? subTeam,
        HashSet<string>? excludedDeveloperIds = null)
    {
        var sortedSelected = selectedSprints.OrderBy(s => s.StartDate).ToList();
        var context = BuildContext(settings, statusTransitions);

        var sprintInfos = sortedSelected
            .Select(s => new SprintRef(s.Id, s.Name, s.StartDate, s.EndDate))
            .ToList();

        var perSprintData = sortedSelected
            .Select(s => ComputePerSprintData(s, FilterMemberships(s.Memberships, subTeam, excludedDeveloperIds), context))
            .ToList();

        var avgCarryOverRate = perSprintData.Count > 0
            ? Math.Round(perSprintData.Average(d => d.CarryOverRate), 1)
            : 0;

        var avgCarryOverSp = perSprintData.Count > 0
            ? Math.Round(perSprintData.Average(d => d.CarryOverSp), 1)
            : 0;

        // Issue type breakdown: carry-over tickets across all selected sprints (feature-only)
        var allCarryOverMemberships = sortedSelected
            .SelectMany(s => GetCarryOverMemberships(FilterMemberships(s.Memberships, subTeam, excludedDeveloperIds), s, context))
            .ToList();

        var issueTypeBreakdown = BuildIssueTypeBreakdown(allCarryOverMemberships, context.ExcludedStatuses, context.DefaultSpPerBug);

        // Zombie detection
        var zombieTickets = CollectZombieTickets(
            sortedSelected, GetZombieTicketKeys(allSyncedSprints), subTeam, excludedDeveloperIds, context);

        var summaryMetrics = new CarryOverSummaryMetrics(
            avgCarryOverRate,
            avgCarryOverSp,
            zombieTickets.Count);

        return new CarryOverMultiSprintResult(
            sprintInfos,
            summaryMetrics,
            perSprintData,
            issueTypeBreakdown,
            zombieTickets);
    }

    public CarryOverSingleSprintResult ComputeSingleSprint(
        Sprint targetSprint,
        Sprint? priorSprint,
        List<Sprint> allSyncedSprints,
        List<StatusTransition> statusTransitions,
        AppSettings settings,
        string? subTeam,
        HashSet<string>? excludedDeveloperIds = null)
    {
        var context = BuildContext(settings, statusTransitions);

        var memberships = FilterMemberships(targetSprint.Memberships, subTeam, excludedDeveloperIds);
        var priorMemberships = priorSprint is not null
            ? FilterMemberships(priorSprint.Memberships, subTeam, excludedDeveloperIds)
            : null;

        var sprintInfo = new SprintRef(
            targetSprint.Id, targetSprint.Name, targetSprint.StartDate, targetSprint.EndDate);

        var zombieTicketKeys = GetZombieTicketKeys(allSyncedSprints);

        // Identify carry-over memberships for this sprint (feature-only: spec BR12)
        var carryOverMemberships = GetCarryOverMemberships(memberships, targetSprint, context);

        // Core carry-over metrics
        var current = ComputeCarryOverMetrics(memberships, targetSprint, context);
        CarryOverMetrics? prior = priorMemberships is not null && priorSprint is not null
            ? ComputeCarryOverMetrics(priorMemberships, priorSprint, context)
            : null;

        var metrics = new CarryOverSingleSprintMetrics(
            CarryOverRate: BuildMetricCard("Carry-Over Rate", current.CarryOverRate,
                $"{current.CarryOverRate:0.#}%",
                prior is not null ? current.CarryOverRate - prior.CarryOverRate : null,
                "positive-down"),
            CarryOverSp: BuildMetricCard("Carry-Over SP", current.CarryOverSp,
                $"{current.CarryOverSp:0.#}",
                prior is not null ? current.CarryOverSp - prior.CarryOverSp : null,
                "positive-down"),
            CarryOverTicketCount: BuildMetricCard("Carry-Over Tickets", current.CarryOverTicketCount,
                $"{current.CarryOverTicketCount}",
                prior is not null ? (decimal)(current.CarryOverTicketCount - prior.CarryOverTicketCount) : null,
                "positive-down"));

        var statusDistribution = BuildStatusDistribution(
            carryOverMemberships, context.WorkflowStages, context.ExcludedStatuses, context.DefaultSpPerBug);
        var issueTypeBreakdown = BuildIssueTypeBreakdown(carryOverMemberships, context.ExcludedStatuses, context.DefaultSpPerBug);

        // Carry-over destination (transition-based — spec BR22)
        var destination = BuildCarryOverDestination(priorSprint, priorMemberships, targetSprint, memberships, context);

        var tickets = BuildTicketTable(carryOverMemberships, zombieTicketKeys, context);
        var zombieTrajectories = BuildZombieTrajectories(tickets, allSyncedSprints, zombieTicketKeys);

        return new CarryOverSingleSprintResult(
            sprintInfo,
            metrics,
            statusDistribution,
            issueTypeBreakdown,
            destination,
            tickets,
            zombieTrajectories);
    }

    private static CarryOverContext BuildContext(AppSettings settings, List<StatusTransition> statusTransitions)
    {
        // Resolve transition boundaries once
        var (orderedStages, startIndex) = TransitionAttributionChecker.ResolveStartIndex(settings);
        var endIndex = TransitionAttributionChecker.ResolveEndIndex(settings, orderedStages);
        return new CarryOverContext(
            StatusTransitionIndex.Build(statusTransitions), orderedStages, startIndex, endIndex,
            settings.ExcludedFromScopeStatuses, settings.WorkflowStages, settings.DefaultSpPerBug);
    }

    // Carry-over: feature tickets that started but did not complete in the sprint
    private static List<SprintMembership> GetCarryOverMemberships(
        List<SprintMembership> memberships,
        Sprint sprint,
        CarryOverContext context)
    {
        return memberships
            .Where(m =>
            {
                if (m.RemovedAt != null || m.Ticket?.IssueType == "Bug") return false;
                var (isStarted, _) = TransitionAttributionChecker.IsStartedInSprint(
                    m.TicketId, context.TransitionIndex, sprint.StartDate, sprint.EndDate, context.OrderedStages, context.StartIndex);
                var (isCompleted, _) = TransitionAttributionChecker.IsCompletedInSprint(
                    m.TicketId, context.TransitionIndex, sprint.StartDate, sprint.EndDate, context.OrderedStages, context.EndIndex);
                return TransitionAttributionChecker.IsCarryOver(isStarted, isCompleted);
            })
            .ToList();
    }

    private static List<CarryOverZombieSummary> CollectZombieTickets(
        List<Sprint> sortedSelected,
        Dictionary<string, int> zombieTicketKeys,
        string? subTeam,
        HashSet<string>? excludedDeveloperIds,
        CarryOverContext context)
    {
        var zombieTickets = new List<CarryOverZombieSummary>();
        var seenZombieKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var sprint in sortedSelected)
        {
            var memberships = FilterMemberships(sprint.Memberships, subTeam, excludedDeveloperIds);
            foreach (var m in GetCarryOverMemberships(memberships, sprint, context))
            {
                if (!zombieTicketKeys.TryGetValue(m.TicketId, out var sprintCount)) continue;
                if (!seenZombieKeys.Add(m.TicketId)) continue;

                zombieTickets.Add(new CarryOverZombieSummary(
                    m.TicketId,
                    m.Ticket?.Summary ?? m.TicketId,
                    m.Ticket?.IssueType ?? "Unknown",
                    m.FinalStatus,
                    m.GetEffectiveSp(context.DefaultSpPerBug),
                    sprintCount));
            }
        }

        return zombieTickets;
    }

    // Full carry-over ticket table
    private static List<CarryOverTicketEntry> BuildTicketTable(
        List<SprintMembership> carryOverMemberships,
        Dictionary<string, int> zombieTicketKeys,
        CarryOverContext context)
    {
        return carryOverMemberships
            .Select(m =>
            {
                var sprintCount = zombieTicketKeys.TryGetValue(m.TicketId, out var sc) ? sc : 1;
                return new CarryOverTicketEntry(
                    m.TicketId,
                    m.Ticket?.Summary ?? m.TicketId,
                    m.Ticket?.IssueType ?? "Unknown",
                    m.GetEffectiveSp(context.DefaultSpPerBug),
                    m.FinalStatus,
                    MapWorkflowStage(m.FinalStatus, context.WorkflowStages),
                    sprintCount,
                    sprintCount >= 3,
                    IsExcluded(m.FinalStatus, context.ExcludedStatuses));
            })
            .ToList();
    }

    // --- Excluded status check (case-insensitive) ---

    private static bool IsExcluded(string status, List<string> excludedStatuses) =>
        excludedStatuses.Contains(status, StringComparer.OrdinalIgnoreCase);

    // --- Carry-over metrics per sprint (feature-only) ---

    private record CarryOverMetrics(
        decimal CarryOverSp,
        int CarryOverTicketCount,
        decimal TotalScopeSp,
        decimal CarryOverRate);

    private static CarryOverMetrics ComputeCarryOverMetrics(
        List<SprintMembership> memberships,
        Sprint sprint,
        CarryOverContext context)
    {
        var activeSp = 0m;
        var carryOverSp = 0m;
        var carryOverCount = 0;

        foreach (var m in memberships)
        {
            if (m.RemovedAt != null || m.Ticket?.IssueType == "Bug") continue;
            var sp = m.GetEffectiveSp(context.DefaultSpPerBug);

            var (isStarted, _) = TransitionAttributionChecker.IsStartedInSprint(
                m.TicketId, context.TransitionIndex, sprint.StartDate, sprint.EndDate, context.OrderedStages, context.StartIndex);
            var (isCompleted, _) = TransitionAttributionChecker.IsCompletedInSprint(
                m.TicketId, context.TransitionIndex, sprint.StartDate, sprint.EndDate, context.OrderedStages, context.EndIndex);

            if (isStarted)
            {
                if (sp.HasValue && !IsExcluded(m.FinalStatus, context.ExcludedStatuses))
                    activeSp += sp.Value;
            }

            if (TransitionAttributionChecker.IsCarryOver(isStarted, isCompleted))
            {
                carryOverCount++;
                if (sp.HasValue && !IsExcluded(m.FinalStatus, context.ExcludedStatuses))
                    carryOverSp += sp.Value;
            }
        }

        // totalScopeSp = activeSp (feature tickets that transitioned to startStage — spec BR21)
        var carryOverRate = activeSp > 0 ? carryOverSp / activeSp * 100 : 0;

        return new CarryOverMetrics(
            Math.Round(carryOverSp, 1),
            carryOverCount,
            Math.Round(activeSp, 1),
            Math.Round(carryOverRate, 1));
    }

    // --- Per-sprint data ---

    private static CarryOverPerSprintData ComputePerSprintData(
        Sprint sprint,
        List<SprintMembership> memberships,
        CarryOverContext context)
    {
        var metrics = ComputeCarryOverMetrics(memberships, sprint, context);

        var carryOverMemberships = GetCarryOverMemberships(memberships, sprint, context);

        var statusDistribution = BuildStatusDistribution(
            carryOverMemberships, context.WorkflowStages, context.ExcludedStatuses, context.DefaultSpPerBug);

        return new CarryOverPerSprintData(
            sprint.Id,
            metrics.CarryOverSp,
            metrics.CarryOverTicketCount,
            metrics.CarryOverRate,
            metrics.TotalScopeSp,
            statusDistribution);
    }

    // --- Status distribution ---

    private static List<CarryOverStatusDistributionEntry> BuildStatusDistribution(
        List<SprintMembership> carryOverMemberships,
        List<string> workflowStages,
        List<string> excludedStatuses,
        int defaultSpPerBug)
    {
        var visibleMemberships = carryOverMemberships
            .Where(m => !IsExcluded(m.FinalStatus, excludedStatuses))
            .ToList();

        var total = visibleMemberships.Count;
        var result = new List<CarryOverStatusDistributionEntry>();

        foreach (var stage in workflowStages)
        {
            var items = visibleMemberships
                .Where(m => string.Equals(m.FinalStatus, stage, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (items.Count == 0) continue;

            var spTotal = items
                .Where(m => m.GetEffectiveSp(defaultSpPerBug).HasValue)
                .Sum(m => m.GetEffectiveSp(defaultSpPerBug)!.Value);
            var percentage = total > 0 ? Math.Round((decimal)items.Count / total * 100, 1) : 0;

            result.Add(new CarryOverStatusDistributionEntry(
                stage, items.Count, Math.Round(spTotal, 1), percentage));
        }

        var stageSet = new HashSet<string>(workflowStages, StringComparer.OrdinalIgnoreCase);
        var otherItems = visibleMemberships
            .Where(m => !stageSet.Contains(m.FinalStatus))
            .ToList();

        if (otherItems.Count > 0)
        {
            var otherSp = otherItems
                .Where(m => m.GetEffectiveSp(defaultSpPerBug).HasValue)
                .Sum(m => m.GetEffectiveSp(defaultSpPerBug)!.Value);
            var otherPct = total > 0 ? Math.Round((decimal)otherItems.Count / total * 100, 1) : 0;
            result.Add(new CarryOverStatusDistributionEntry(
                "Other", otherItems.Count, Math.Round(otherSp, 1), otherPct));
        }

        return result;
    }

    // --- Issue type breakdown ---

    private static List<CarryOverIssueTypeEntry> BuildIssueTypeBreakdown(
        List<SprintMembership> carryOverMemberships,
        List<string> excludedStatuses,
        int defaultSpPerBug)
    {
        var includedMemberships = carryOverMemberships
            .Where(m => !IsExcluded(m.FinalStatus, excludedStatuses))
            .ToList();

        var total = includedMemberships.Count;

        return includedMemberships
            .GroupBy(m => m.Ticket?.IssueType ?? "Unknown")
            .Select(g =>
            {
                var spTotal = g
                    .Where(m => m.GetEffectiveSp(defaultSpPerBug).HasValue)
                    .Sum(m => m.GetEffectiveSp(defaultSpPerBug)!.Value);
                var percentage = total > 0 ? Math.Round((decimal)g.Count() / total * 100, 1) : 0;
                return new CarryOverIssueTypeEntry(
                    g.Key, g.Count(), Math.Round(spTotal, 1), percentage);
            })
            .OrderByDescending(e => e.TicketCount)
            .ToList();
    }

    // --- Zombie detection ---

    private static Dictionary<string, int> GetZombieTicketKeys(List<Sprint> allSyncedSprints)
    {
        return allSyncedSprints
            .SelectMany(s => s.Memberships.Select(m => new { m.TicketId, s.Id }))
            .GroupBy(x => x.TicketId, StringComparer.OrdinalIgnoreCase)
            .Select(g => new { TicketId = g.Key, SprintCount = g.Select(x => x.Id).Distinct().Count() })
            .Where(x => x.SprintCount >= 3)
            .ToDictionary(x => x.TicketId, x => x.SprintCount, StringComparer.OrdinalIgnoreCase);
    }

    // --- Workflow stage mapping ---

    private static string MapWorkflowStage(string finalStatus, List<string> workflowStages)
    {
        var match = workflowStages.FirstOrDefault(
            stage => string.Equals(stage, finalStatus, StringComparison.OrdinalIgnoreCase));
        return match ?? "Other";
    }

    // --- Carry-over destination (transition-based — spec BR22) ---

    private static CarryOverDestination? BuildCarryOverDestination(
        Sprint? priorSprint,
        List<SprintMembership>? priorMemberships,
        Sprint targetSprint,
        List<SprintMembership> targetMemberships,
        CarryOverContext context)
    {
        if (priorSprint is null || priorMemberships is null)
            return null;

        // Prior sprint carry-over: feature tickets that started but did not complete in prior sprint
        var priorCarryOverMemberships = GetCarryOverMemberships(priorMemberships, priorSprint, context);

        var priorCarryOverCount = priorCarryOverMemberships.Count;
        var priorCarryOverSp = priorCarryOverMemberships
            .Where(m => m.GetEffectiveSp(context.DefaultSpPerBug).HasValue && !IsExcluded(m.FinalStatus, context.ExcludedStatuses))
            .Sum(m => m.GetEffectiveSp(context.DefaultSpPerBug)!.Value);

        if (priorCarryOverCount == 0)
        {
            return new CarryOverDestination(
                priorSprint.Id,
                priorSprint.Name,
                0,
                Math.Round(priorCarryOverSp, 1),
                new CarryOverDestinationBucket(0, 0),
                new CarryOverDestinationBucket(0, 0),
                new CarryOverDestinationBucket(0, 0),
                new CarryOverDestinationBucket(0, 0));
        }

        var currentMembershipByTicket = targetMemberships
            .ToDictionary(m => m.TicketId, StringComparer.OrdinalIgnoreCase);

        var byDestination = priorCarryOverMemberships
            .Select(pm => (
                Destination: ResolveDestination(pm, currentMembershipByTicket, targetSprint, context),
                Sp: pm.GetEffectiveSp(context.DefaultSpPerBug) ?? 0m))
            .ToList();

        CarryOverDestinationBucket Bucket(CarryOverDestinationKind kind)
        {
            var items = byDestination.Where(x => x.Destination == kind).ToList();
            return new CarryOverDestinationBucket(items.Count, Math.Round(items.Sum(x => x.Sp), 1));
        }

        return new CarryOverDestination(
            priorSprint.Id,
            priorSprint.Name,
            priorCarryOverCount,
            Math.Round(priorCarryOverSp, 1),
            Bucket(CarryOverDestinationKind.Completed),
            Bucket(CarryOverDestinationKind.CarriedAgain),
            Bucket(CarryOverDestinationKind.Removed),
            Bucket(CarryOverDestinationKind.Dropped));
    }

    private static CarryOverDestinationKind ResolveDestination(
        SprintMembership priorMembership,
        Dictionary<string, SprintMembership> currentMembershipByTicket,
        Sprint targetSprint,
        CarryOverContext context)
    {
        if (!currentMembershipByTicket.TryGetValue(priorMembership.TicketId, out var cm))
            return CarryOverDestinationKind.Dropped;

        if (cm.RemovedAt != null)
            return CarryOverDestinationKind.Removed;

        // Transition-based: completed in current sprint = has a transition to endStage during current sprint
        var (isCompletedInCurrent, _) = TransitionAttributionChecker.IsCompletedInSprint(
            priorMembership.TicketId, context.TransitionIndex,
            targetSprint.StartDate, targetSprint.EndDate,
            context.OrderedStages, context.EndIndex);

        if (isCompletedInCurrent)
            return CarryOverDestinationKind.Completed;

        // Carried again: started in current sprint but not completed
        var (isStartedInCurrent, _) = TransitionAttributionChecker.IsStartedInSprint(
            priorMembership.TicketId, context.TransitionIndex,
            targetSprint.StartDate, targetSprint.EndDate,
            context.OrderedStages, context.StartIndex);

        // In current sprint but no qualifying transition — dropped into backlog
        return isStartedInCurrent ? CarryOverDestinationKind.CarriedAgain : CarryOverDestinationKind.Dropped;
    }

    // --- Zombie trajectories ---

    private static List<ZombieTrajectoryEntry> BuildZombieTrajectories(
        List<CarryOverTicketEntry> tickets,
        List<Sprint> allSyncedSprints,
        Dictionary<string, int> zombieTicketKeys)
    {
        var zombies = tickets.Where(t => t.IsZombie).ToList();
        if (zombies.Count == 0) return new List<ZombieTrajectoryEntry>();

        var result = new List<ZombieTrajectoryEntry>();

        foreach (var zombie in zombies)
        {
            var sprintAppearances = allSyncedSprints
                .Where(s => s.Memberships.Any(m =>
                    string.Equals(m.TicketId, zombie.TicketKey, StringComparison.OrdinalIgnoreCase)))
                .OrderBy(s => s.StartDate)
                .ToList();

            if (sprintAppearances.Count > 10)
                sprintAppearances = sprintAppearances.TakeLast(10).ToList();

            var sprintEntries = sprintAppearances
                .Select(s =>
                {
                    var m = s.Memberships.First(m =>
                        string.Equals(m.TicketId, zombie.TicketKey, StringComparison.OrdinalIgnoreCase));
                    return new ZombieTrajectorySprintEntry(s.Id, s.Name, m.FinalStatus);
                })
                .ToList();

            result.Add(new ZombieTrajectoryEntry(
                zombie.TicketKey,
                zombie.Summary,
                zombie.IssueType,
                zombie.StoryPoints,
                zombie.FinalStatus,
                zombie.SprintCount,
                sprintEntries));
        }

        return result.OrderByDescending(e => e.SprintCount).ToList();
    }

    private enum CarryOverDestinationKind
    {
        Completed,
        CarriedAgain,
        Removed,
        Dropped
    }

    private sealed record CarryOverContext(
        StatusTransitionIndex TransitionIndex,
        List<string> OrderedStages,
        int StartIndex,
        int EndIndex,
        List<string> ExcludedStatuses,
        List<string> WorkflowStages,
        int DefaultSpPerBug);
}
