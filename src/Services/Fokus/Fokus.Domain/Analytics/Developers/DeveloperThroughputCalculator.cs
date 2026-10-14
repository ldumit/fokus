using Fokus.Domain.Analytics.Shared;
using static Fokus.Domain.Analytics.Shared.CapacityResolver;
using static Fokus.Domain.Analytics.Shared.DeltaResolver;
using static Fokus.Domain.Analytics.Shared.IssueTypeChecker;
using static Fokus.Domain.Analytics.Shared.SubTeamFilter;

namespace Fokus.Domain.Analytics.Developers;

public class DeveloperThroughputCalculator
{
    public DeveloperThroughputResult ComputeThroughput(
        List<Sprint> allLoadedSprints,
        List<int> targetSprintIds,
        List<Developer> activeDevelopers,
        List<DeveloperSprintCapacity> capacityRecords,
        AppSettings settings,
        string? subTeam,
        List<Developer> allDevelopers,
        List<StatusTransition> statusTransitions)
    {
        var (orderedStages, startIndex) = TransitionAttributionChecker.ResolveStartIndex(settings);
        var endIndex = TransitionAttributionChecker.ResolveEndIndex(settings, orderedStages);

        // C2: sub-team filtering (active developers only for display)
        var filteredDevelopers = FilterDevelopers(activeDevelopers, subTeam);

        // Sort all loaded sprints ascending by start date
        var sortedAllSprints = allLoadedSprints.OrderBy(s => s.StartDate).ToList();

        // Target sprints in ascending order
        var targetSprints = sortedAllSprints
            .Where(s => targetSprintIds.Contains(s.Id))
            .OrderBy(s => s.StartDate)
            .ToList();

        var isSingleSprint = targetSprintIds.Count == 1;

        // Build capacity lookup: accountId -> sprintId -> capacityPercent
        var capacityLookup = capacityRecords
            .GroupBy(c => c.DeveloperAccountId)
            .ToDictionary(
                g => g.Key,
                g => g.ToDictionary(c => c.SprintId, c => c.CapacityPercent));

        // Find prior sprint for delta (single-sprint mode only)
        Sprint? priorSprint = null;
        if (isSingleSprint)
        {
            var targetSprint = targetSprints[0];
            var targetIndexInAll = sortedAllSprints.FindIndex(s => s.Id == targetSprint.Id);
            priorSprint = targetIndexInAll > 0 ? sortedAllSprints[targetIndexInAll - 1] : null;
        }

        var context = new ThroughputContext(
            sortedAllSprints, isSingleSprint ? priorSprint : null, capacityLookup,
            StatusTransitionIndex.Build(statusTransitions), settings, subTeam, allDevelopers,
            orderedStages, startIndex, endIndex);

        var sprintSummaryItems = targetSprints
            .Select(s => new SprintRef(s.Id, s.Name, s.StartDate, s.EndDate))
            .ToList();

        var developerEntries = filteredDevelopers.Select(developer =>
        {
            var breakdowns = targetSprints
                .Select(sprint => BuildBreakdown(developer.Id, sprint, context))
                .ToList();

            return new DeveloperThroughputEntry(developer.Id, developer.DisplayName, developer.SubTeam, developer.AvatarUrl, breakdowns);
        }).ToList();

        return new DeveloperThroughputResult(sprintSummaryItems, developerEntries);
    }

    public static IReadOnlyList<int> SelectRollingWindowSprintIds(
        IReadOnlyList<Sprint> ascendingClosedSprints,
        IReadOnlyList<int> targetSprintIds)
    {
        var earliestTargetIndex = ascendingClosedSprints.ToList().FindIndex(s => s.Id == targetSprintIds[0]);
        var rollingWindowBase = earliestTargetIndex >= 0 ? earliestTargetIndex : ascendingClosedSprints.Count;
        return ascendingClosedSprints
            .Take(rollingWindowBase)
            .Select(s => s.Id)
            .ToList();
    }

    private static SprintBreakdown BuildBreakdown(string developerId, Sprint sprint, ThroughputContext context)
    {
        var memberships = GetDeveloperMemberships(sprint, developerId, context.SubTeam);
        var capacity = GetCapacity(context.CapacityLookup, developerId, sprint.Id, context.AllDevelopers);

        var (spAssigned, spCompleted, ticketsDone, ticketsCarriedOver) = ComputeMetrics(
            memberships, context.TransitionIndex, sprint.StartDate, sprint.EndDate,
            context.OrderedStages, context.StartIndex, context.EndIndex, context.Settings.DefaultSpPerBug);

        var completionPercent = spAssigned > 0 ? spCompleted / spAssigned * 100 : 0;

        var rollingAverage = ComputeRollingAverage(
            developerId, sprint.Id, context.SortedAllSprints, context.CapacityLookup, context.TransitionIndex,
            context.Settings, context.SubTeam, context.AllDevelopers, context.Settings.DefaultSpPerBug);

        // Deltas (single-sprint only)
        var deltas = context.PriorSprint is not null
            ? ComputeDeltas(developerId, context.PriorSprint, spAssigned, spCompleted, completionPercent,
                ticketsDone, ticketsCarriedOver, context)
            : null;

        return new SprintBreakdown(
            sprint.Id,
            Math.Round(spAssigned, 1),
            Math.Round(spCompleted, 1),
            Math.Round(completionPercent, 1),
            ticketsDone,
            ticketsCarriedOver,
            capacity,
            rollingAverage.HasValue ? Math.Round(rollingAverage.Value, 1) : null,
            deltas is null ? null : Math.Round(deltas.SpAssigned, 1),
            deltas is null ? null : Math.Round(deltas.SpCompleted, 1),
            deltas is null ? null : Math.Round(deltas.CompletionPercent, 1),
            deltas?.TicketsDone,
            deltas?.TicketsCarriedOver,
            deltas is null ? null : DeltaDirection(deltas.SpAssigned),
            deltas is null ? null : DeltaDirection(deltas.SpCompleted),
            deltas is null ? null : DeltaDirection(deltas.CompletionPercent),
            deltas is null ? null : DeltaDirection(deltas.TicketsDone),
            deltas is null ? null : DeltaDirection(deltas.TicketsCarriedOver),
            deltas is null ? null : "neutral",
            deltas is null ? null : DeltaPolarity(deltas.SpCompleted, positiveUp: true),
            deltas is null ? null : DeltaPolarity(deltas.CompletionPercent, positiveUp: true),
            deltas is null ? null : DeltaPolarity(deltas.TicketsDone, positiveUp: true),
            deltas is null ? null : DeltaPolarity(deltas.TicketsCarriedOver, positiveUp: false));
    }

    private static ThroughputDeltas ComputeDeltas(
        string developerId,
        Sprint priorSprint,
        decimal spAssigned,
        decimal spCompleted,
        decimal completionPercent,
        int ticketsDone,
        int ticketsCarriedOver,
        ThroughputContext context)
    {
        var priorMemberships = GetDeveloperMemberships(priorSprint, developerId, context.SubTeam);

        var (priorSpAssigned, priorSpCompleted, priorTicketsDone, priorTicketsCarriedOver) = ComputeMetrics(
            priorMemberships, context.TransitionIndex, priorSprint.StartDate, priorSprint.EndDate,
            context.OrderedStages, context.StartIndex, context.EndIndex, context.Settings.DefaultSpPerBug);

        var priorCompletionPercent = priorSpAssigned > 0 ? priorSpCompleted / priorSpAssigned * 100 : 0;

        return new ThroughputDeltas(
            spAssigned - priorSpAssigned,
            spCompleted - priorSpCompleted,
            completionPercent - priorCompletionPercent,
            ticketsDone - priorTicketsDone,
            ticketsCarriedOver - priorTicketsCarriedOver);
    }

    // --- Core metrics computation (transition-based, feature-only) ---

    private static (decimal SpAssigned, decimal SpCompleted, int TicketsDone, int TicketsCarriedOver) ComputeMetrics(
        List<SprintMembership> memberships,
        StatusTransitionIndex transitionIndex,
        DateTime sprintStart,
        DateTime sprintEnd,
        List<string> orderedStages,
        int startIndex,
        int endIndex,
        int defaultSpPerBug)
    {
        decimal spAssigned = 0;
        decimal spCompleted = 0;
        int ticketsDone = 0;
        int ticketsCarriedOver = 0;

        foreach (var m in memberships.Where(m => m.RemovedAt == null && !IsBug(m)))
        {
            var (isStarted, _) = TransitionAttributionChecker.IsStartedInSprint(
                m.TicketId, transitionIndex, sprintStart, sprintEnd, orderedStages, startIndex);

            if (!isStarted)
                continue;

            var sp = m.GetEffectiveSp(defaultSpPerBug) ?? 0m;
            spAssigned += sp;

            var (isCompleted, _) = TransitionAttributionChecker.IsCompletedInSprint(
                m.TicketId, transitionIndex, sprintStart, sprintEnd, orderedStages, endIndex);

            if (isCompleted)
            {
                spCompleted += sp;
                ticketsDone++;
            }
            else
            {
                ticketsCarriedOver++;
            }
        }

        return (spAssigned, spCompleted, ticketsDone, ticketsCarriedOver);
    }

    // --- Rolling average (capacity-aware, 3-sprint window, transition-based) ---

    private static decimal? ComputeRollingAverage(
        string developerId,
        int currentSprintId,
        List<Sprint> sortedAllSprints,
        Dictionary<string, Dictionary<int, int>> capacityLookup,
        StatusTransitionIndex transitionIndex,
        AppSettings settings,
        string? subTeam,
        List<Developer> developers,
        int defaultSpPerBug = 0)
    {
        var (orderedStages, startIndex) = TransitionAttributionChecker.ResolveStartIndex(settings);
        var endIndex = TransitionAttributionChecker.ResolveEndIndex(settings, orderedStages);

        var currentIndex = sortedAllSprints.FindIndex(s => s.Id == currentSprintId);
        if (currentIndex < 0) return null;

        // Look backward from current sprint (inclusive) collecting up to 3 qualifying sprints
        var qualifyingSpCompleted = new List<decimal>();
        for (var i = currentIndex; i >= 0 && qualifyingSpCompleted.Count < 3; i--)
        {
            var sprint = sortedAllSprints[i];
            var capacity = GetCapacity(capacityLookup, developerId, sprint.Id, developers);
            if (capacity == 0) continue;

            var memberships = GetDeveloperMemberships(sprint, developerId, subTeam);
            var (_, spCompleted, _, _) = ComputeMetrics(
                memberships, transitionIndex, sprint.StartDate, sprint.EndDate,
                orderedStages, startIndex, endIndex, defaultSpPerBug);

            qualifyingSpCompleted.Add(spCompleted);
        }

        if (qualifyingSpCompleted.Count < 3) return null;

        return qualifyingSpCompleted.Average();
    }

    private sealed record ThroughputContext(
        List<Sprint> SortedAllSprints,
        Sprint? PriorSprint,
        Dictionary<string, Dictionary<int, int>> CapacityLookup,
        StatusTransitionIndex TransitionIndex,
        AppSettings Settings,
        string? SubTeam,
        List<Developer> AllDevelopers,
        List<string> OrderedStages,
        int StartIndex,
        int EndIndex);

    private sealed record ThroughputDeltas(
        decimal SpAssigned,
        decimal SpCompleted,
        decimal CompletionPercent,
        int TicketsDone,
        int TicketsCarriedOver);
}
