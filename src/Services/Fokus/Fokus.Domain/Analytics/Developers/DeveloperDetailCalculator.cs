using Fokus.Domain.Analytics.Shared;
using static Fokus.Domain.Analytics.Shared.BusinessDayCalculator;
using static Fokus.Domain.Analytics.Shared.CapacityResolver;
using static Fokus.Domain.Analytics.Shared.IssueTypeChecker;
using static Fokus.Domain.Analytics.Shared.SubTeamFilter;
using static Fokus.Domain.Analytics.Shared.TransitionAttributionChecker;

namespace Fokus.Domain.Analytics.Developers;

public class DeveloperDetailCalculator
{
    public DeveloperDetailResult ComputeDetail(
        string accountId,
        Developer developer,
        List<Sprint> closedSprints,
        Sprint? activeSprint,
        Dictionary<string, Dictionary<int, int>> capacityLookup,
        List<DeveloperSprintCapacity> capacityRecords,
        AppSettings settings,
        List<StatusTransition> statusTransitions,
        int last,
        decimal bugRatioTarget,
        string jiraInstanceUrl,
        DateTime today)
    {
        var (orderedStages, startIndex) = TransitionAttributionChecker.ResolveStartIndex(settings);
        var endIndex = TransitionAttributionChecker.ResolveEndIndex(settings, orderedStages);
        var transitionIndex = StatusTransitionIndex.Build(statusTransitions);

        // Build all developers list for capacity resolution (single dev here)
        var devList = new List<Developer> { developer };

        // --- Cross-sprint trends ---
        // All sprints where developer has at least one non-removed membership, ordered chronologically.
        // The full qualifying list is used for rolling average look-back; TakeLast is applied at the end
        // so the `last` count is over post-exclusion sprints only (plan: "Range counts only sprints
        // passing inclusion rule").
        var qualifyingSprints = closedSprints
            .OrderBy(s => s.StartDate)
            .Where(s => s.Memberships.Any(m => m.Ticket?.AssigneeId == accountId && m.RemovedAt == null))
            .ToList();

        var trendContext = new TrendContext(
            accountId, qualifyingSprints, capacityLookup, transitionIndex, settings, devList, orderedStages, endIndex);

        // Compute per-sprint trends over all qualifying sprints, applying exclusion guard inline.
        // Rolling average receives the full qualifying list so look-back is stable regardless of TakeLast.
        var allSprintTrends = new List<SprintTrendEntry>();
        for (var i = 0; i < qualifyingSprints.Count; i++)
        {
            var trend = BuildSprintTrend(i, trendContext);
            if (trend is not null)
                allSprintTrends.Add(trend);
        }

        // Apply last filter after exclusion so the count is over qualifying sprints only (0 = all)
        var sprintTrends = last > 0
            ? allSprintTrends.TakeLast(last).ToList()
            : allSprintTrends;

        var workAllocation = SummarizeWorkAllocation(sprintTrends, bugRatioTarget);

        // --- Current sprint detail ---
        CurrentSprintDetail? currentSprintDetail = null;
        if (activeSprint is not null)
        {
            currentSprintDetail = ComputeCurrentSprintDetail(
                accountId, developer, activeSprint, capacityRecords, settings,
                transitionIndex, orderedStages, startIndex, endIndex, today);
        }

        // --- Developer info ---
        var devInfo = new DeveloperDetailInfo(
            developer.Id,
            developer.DisplayName,
            developer.SubTeam,
            developer.AvatarUrl,
            developer.Role,
            developer.DefaultCapacityPercent);

        return new DeveloperDetailResult(
            devInfo,
            sprintTrends,
            workAllocation,
            currentSprintDetail,
            bugRatioTarget,
            jiraInstanceUrl);
    }

    private static SprintTrendEntry? BuildSprintTrend(int index, TrendContext context)
    {
        var sprint = context.QualifyingSprints[index];
        var defaultSpPerBug = context.Settings.DefaultSpPerBug;
        var memberships = GetDeveloperMemberships(sprint, context.AccountId, subTeam: null);

        // Completed memberships (transition-based, excludedStatuses filter)
        var completed = GetTransitionCompletedMemberships(
            memberships, context.TransitionIndex, sprint.StartDate, sprint.EndDate,
            context.OrderedStages, context.EndIndex, context.Settings.ExcludedFromScopeStatuses);

        var featureSp = completed.Where(m => !IsBug(m)).Sum(m => m.GetEffectiveSp(defaultSpPerBug) ?? 0m);
        var bugSp = completed.Where(m => IsBug(m)).Sum(m => m.GetEffectiveSp(defaultSpPerBug) ?? 0m);
        var totalSp = featureSp + bugSp;

        // AssignedSp: all non-removed memberships, all types
        var assignedSp = memberships
            .Where(m => m.RemovedAt == null)
            .Select(m => m.GetEffectiveSp(defaultSpPerBug))
            .Where(sp => sp.HasValue)
            .Sum(sp => sp!.Value);

        var capacityPercent = GetCapacity(context.CapacityLookup, context.AccountId, sprint.Id, context.Developers);

        // Exclude sprint: capacity == 0 AND totalSp == 0 (developer absent from this sprint)
        if (capacityPercent == 0 && totalSp == 0)
            return null;

        var completionPercent = assignedSp > 0 ? totalSp / assignedSp * 100m : 0m;
        var bugPercent = totalSp > 0 ? bugSp / totalSp * 100m : 0m;

        // Rolling average: 3-sprint window over all-types SP, computed over full qualifying list
        // so the window is stable regardless of how many sprints TakeLast will return.
        decimal? rollingAverage = ComputeRollingAverage(
            context.AccountId, index, context.QualifyingSprints, context.CapacityLookup, context.TransitionIndex,
            context.Settings, context.Developers, defaultSpPerBug);

        return new SprintTrendEntry(
            sprint.Id,
            sprint.Name,
            sprint.StartDate,
            sprint.EndDate,
            Math.Round(featureSp, 1),
            Math.Round(bugSp, 1),
            Math.Round(totalSp, 1),
            Math.Round(assignedSp, 1),
            Math.Round(completionPercent, 1),
            capacityPercent,
            rollingAverage.HasValue ? Math.Round(rollingAverage.Value, 1) : null,
            Math.Round(bugPercent, 1));
    }

    // --- Work allocation summary ---
    private static WorkAllocationSummary SummarizeWorkAllocation(List<SprintTrendEntry> sprintTrends, decimal bugRatioTarget)
    {
        var sprintsWithSp = sprintTrends.Where(t => t.TotalSp > 0).ToList();
        var averageBugPercent = sprintsWithSp.Count > 0
            ? sprintsWithSp.Average(t => t.BugPercent)
            : 0m;
        var sprintsAboveTarget = sprintTrends.Count(t => t.BugPercent > bugRatioTarget);
        return new WorkAllocationSummary(
            Math.Round(averageBugPercent, 1),
            sprintsAboveTarget,
            sprintTrends.Count);
    }

    // --- Current sprint computation (mirrors DeveloperProgressCalculator) ---

    private static CurrentSprintDetail ComputeCurrentSprintDetail(
        string accountId,
        Developer developer,
        Sprint activeSprint,
        List<DeveloperSprintCapacity> capacityRecords,
        AppSettings settings,
        StatusTransitionIndex transitionIndex,
        List<string> orderedStages,
        int startIndex,
        int endIndex,
        DateTime today)
    {
        var totalDays = Math.Max(1, (activeSprint.EndDate.Date - activeSprint.StartDate.Date).Days + 1);
        var currentDay = Math.Min(totalDays, Math.Max(1, (today - activeSprint.StartDate.Date).Days + 1));

        var capacityLookup = capacityRecords
            .Where(c => c.SprintId == activeSprint.Id)
            .ToDictionary(c => c.DeveloperAccountId, c => c.CapacityPercent);

        var capacity = capacityLookup.TryGetValue(accountId, out var cap)
            ? cap
            : developer.DefaultCapacityPercent;

        var memberships = activeSprint.Memberships
            .Where(m => m.Ticket?.AssigneeId == accountId && m.RemovedAt == null)
            .ToList();

        var assignedSp = memberships
            .Select(m => m.GetEffectiveSp(settings.DefaultSpPerBug))
            .Where(sp => sp.HasValue)
            .Sum(sp => sp!.Value);

        var dailyPace = assignedSp > 0
            ? assignedSp * (capacity / 100m) / totalDays
            : 0m;

        var completions = TallyCompletions(
            memberships, transitionIndex, activeSprint, orderedStages, endIndex, settings.DefaultSpPerBug, currentDay);
        var dailyBreakdown = BuildDailyBreakdown(completions.ByDay, dailyPace, activeSprint, currentDay);

        var completionPercent = assignedSp > 0 ? completions.CompletedSp / assignedSp * 100m : 0m;
        var isGracePeriod = currentDay <= 2;
        bool isBehindPace = false;
        decimal? paceGapSp = null;

        if (!isGracePeriod && dailyPace > 0)
        {
            var expectedAtCurrentDay = dailyPace * currentDay;
            var gap = expectedAtCurrentDay - completions.CompletedSp;
            isBehindPace = gap > dailyPace;
            paceGapSp = Math.Round(gap, 1);
        }

        // Ticket state derivation (BR10)
        var tickets = BuildTicketDetails(
            memberships, transitionIndex, activeSprint, orderedStages, startIndex, endIndex, today);

        var sprintInfo = new SprintRef(
            activeSprint.Id, activeSprint.Name, activeSprint.StartDate, activeSprint.EndDate);

        return new CurrentSprintDetail(
            sprintInfo,
            currentDay,
            totalDays,
            Math.Round(assignedSp, 1),
            Math.Round(completions.CompletedSp, 1),
            Math.Round(completionPercent, 1),
            Math.Round(completions.FeatureCompletedSp, 1),
            Math.Round(completions.BugCompletedSp, 1),
            Math.Round(dailyPace, 10),
            isBehindPace,
            paceGapSp,
            dailyBreakdown,
            tickets);
    }

    private static CompletionTally TallyCompletions(
        List<SprintMembership> memberships,
        StatusTransitionIndex transitionIndex,
        Sprint activeSprint,
        List<string> orderedStages,
        int endIndex,
        int defaultSpPerBug,
        int currentDay)
    {
        var completionsByDay = new Dictionary<int, List<CompletedTicketEntry>>();
        var completedSp = 0m;
        var featureCompletedSp = 0m;
        var bugCompletedSp = 0m;

        foreach (var m in memberships)
        {
            var (isCompleted, completedAt) = TransitionAttributionChecker.IsCompletedInSprint(
                m.TicketId, transitionIndex,
                activeSprint.StartDate, activeSprint.EndDate,
                orderedStages, endIndex);

            if (!isCompleted || completedAt is null) continue;

            var effectiveSp = m.GetEffectiveSp(defaultSpPerBug);
            if (effectiveSp.HasValue)
            {
                completedSp += effectiveSp.Value;
                if (m.Ticket?.IssueType == "Bug")
                    bugCompletedSp += effectiveSp.Value;
                else
                    featureCompletedSp += effectiveSp.Value;
            }

            var completionDay = (completedAt.Value.Date - activeSprint.StartDate.Date).Days + 1;
            completionDay = Math.Min(completionDay, currentDay);

            if (!completionsByDay.ContainsKey(completionDay))
                completionsByDay[completionDay] = [];

            completionsByDay[completionDay].Add(new CompletedTicketEntry(
                m.TicketId,
                m.Ticket?.Summary ?? m.TicketId,
                effectiveSp,
                m.Ticket?.IssueType ?? "Unknown"));
        }

        return new CompletionTally(completionsByDay, completedSp, featureCompletedSp, bugCompletedSp);
    }

    private static List<DayBreakdownEntry> BuildDailyBreakdown(
        Dictionary<int, List<CompletedTicketEntry>> completionsByDay,
        decimal dailyPace,
        Sprint activeSprint,
        int currentDay)
    {
        var dailyBreakdown = new List<DayBreakdownEntry>();
        var cumulativeSp = 0m;
        for (var day = 1; day <= currentDay; day++)
        {
            var dayTickets = completionsByDay.TryGetValue(day, out var dayCompletions) ? dayCompletions : [];
            var daySp = dayTickets.Where(t => t.StoryPoints.HasValue).Sum(t => t.StoryPoints!.Value);
            cumulativeSp += daySp;
            var expectedCumulative = Math.Round(dailyPace * day, 10);
            var date = activeSprint.StartDate.Date.AddDays(day - 1);
            dailyBreakdown.Add(new DayBreakdownEntry(day, date, dayTickets, cumulativeSp, expectedCumulative));
        }

        return dailyBreakdown;
    }

    // --- Ticket state derivation (BR10) and ordering (BR12) ---

    private static List<TicketDetailEntry> BuildTicketDetails(
        List<SprintMembership> memberships,
        StatusTransitionIndex transitionIndex,
        Sprint sprint,
        List<string> orderedStages,
        int startIndex,
        int endIndex,
        DateTime today)
    {
        var entries = new List<TicketDetailEntry>();

        foreach (var m in memberships)
        {
            var (isStarted, _) = TransitionAttributionChecker.IsStartedInSprint(
                m.TicketId, transitionIndex,
                sprint.StartDate, sprint.EndDate,
                orderedStages, startIndex);

            var (isCompleted, _) = TransitionAttributionChecker.IsCompletedInSprint(
                m.TicketId, transitionIndex,
                sprint.StartDate, sprint.EndDate,
                orderedStages, endIndex);

            var ticketTransitions = transitionIndex.ForTicket(m.TicketId)
                .OrderByDescending(t => t.Timestamp)
                .ToList();

            var (state, isStalled, daysInCurrentStatus) =
                DeriveTicketState(isStarted, isCompleted, ticketTransitions, sprint, today);

            entries.Add(new TicketDetailEntry(
                m.TicketId,
                m.Ticket?.Summary ?? m.TicketId,
                m.Ticket?.CurrentStatus ?? m.FinalStatus,
                m.GetEffectiveSp(0),
                m.Ticket?.IssueType ?? "Unknown",
                daysInCurrentStatus,
                state,
                isStalled));
        }

        // BR12 ordering: stalled (most days desc), in-progress, not-started, done; within group SP desc
        var stateOrder = new Dictionary<string, int>
        {
            ["stalled"] = 0,
            ["in-progress"] = 1,
            ["not-started"] = 2,
            ["done"] = 3
        };

        return entries
            .OrderBy(t => stateOrder.GetValueOrDefault(t.State, 99))
            .ThenByDescending(t => t.IsStalled ? t.DaysInCurrentStatus : 0)
            .ThenByDescending(t => t.StoryPoints ?? 0m)
            .ToList();
    }

    private static (string State, bool IsStalled, int DaysInCurrentStatus) DeriveTicketState(
        bool isStarted,
        bool isCompleted,
        List<StatusTransition> ticketTransitions,
        Sprint sprint,
        DateTime today)
    {
        if (isCompleted)
        {
            // Days since last transition or sprint start
            var doneDays = ticketTransitions.Count > 0
                ? CountBusinessDays(ticketTransitions[0].Timestamp, today)
                : CountBusinessDays(sprint.StartDate, today);
            return ("done", false, doneDays);
        }

        if (!isStarted)
        {
            var notStartedDays = ticketTransitions.Count > 0
                ? CountBusinessDays(ticketTransitions[0].Timestamp, today)
                : CountBusinessDays(sprint.StartDate, today);
            return ("not-started", false, notStartedDays);
        }

        // Started and not completed — check stall (2+ business days since last transition)
        var daysInCurrentStatus = ticketTransitions.Count > 0
            ? CountBusinessDays(ticketTransitions[0].Timestamp, today)
            : CountBusinessDays(sprint.StartDate, today);
        var isStalled = daysInCurrentStatus > 2;
        return (isStalled ? "stalled" : "in-progress", isStalled, daysInCurrentStatus);
    }

    // --- Rolling average: 3-sprint window, all-types SP, skip 0-capacity sprints ---

    private static decimal? ComputeRollingAverage(
        string accountId,
        int currentIndexInDisplayed,
        List<Sprint> displayedSprints,
        Dictionary<string, Dictionary<int, int>> capacityLookup,
        StatusTransitionIndex transitionIndex,
        AppSettings settings,
        List<Developer> developers,
        int defaultSpPerBug)
    {
        var (orderedStages, _) = TransitionAttributionChecker.ResolveStartIndex(settings);
        var endIndex = TransitionAttributionChecker.ResolveEndIndex(settings, orderedStages);
        var excludedStatuses = settings.ExcludedFromScopeStatuses;

        var qualifyingTotals = new List<decimal>();

        for (var i = currentIndexInDisplayed; i >= 0 && qualifyingTotals.Count < 3; i--)
        {
            var sprint = displayedSprints[i];
            var capacity = GetCapacity(capacityLookup, accountId, sprint.Id, developers);
            if (capacity == 0) continue;

            var memberships = GetDeveloperMemberships(sprint, accountId, subTeam: null);
            var completed = GetTransitionCompletedMemberships(
                memberships, transitionIndex, sprint.StartDate, sprint.EndDate,
                orderedStages, endIndex, excludedStatuses);

            var totalSp = completed.Sum(m => m.GetEffectiveSp(defaultSpPerBug) ?? 0m);
            qualifyingTotals.Add(totalSp);
        }

        if (qualifyingTotals.Count < 3) return null;

        return qualifyingTotals.Average();
    }

    private sealed record TrendContext(
        string AccountId,
        List<Sprint> QualifyingSprints,
        Dictionary<string, Dictionary<int, int>> CapacityLookup,
        StatusTransitionIndex TransitionIndex,
        AppSettings Settings,
        List<Developer> Developers,
        List<string> OrderedStages,
        int EndIndex);

    private sealed record CompletionTally(
        Dictionary<int, List<CompletedTicketEntry>> ByDay,
        decimal CompletedSp,
        decimal FeatureCompletedSp,
        decimal BugCompletedSp);
}
